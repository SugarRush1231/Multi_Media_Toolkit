using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using YoutubeDownloader.SiteDownloads;

namespace YoutubeDownloader
{
    public class YtDlpDownloader
    {
        public event Action<double>? OnProgressChanged;
        public event Action<string>? OnDownloadCompleted;
        public Func<string, Task<string>>? WebViewResolver { get; set; }
        public bool DownloadSubtitles { get; set; }
        public string SubtitleLanguages { get; set; } = "ko.*,ko,en.*,en";
        public string FormatSelector { get; set; } = "";
        public string OutputNameTemplate { get; set; } = "%(title)s";
        public string PreferredTitle { get; set; } = "";
        public int PlaylistItemIndex { get; set; }
        public bool LastSubtitleDownloaded { get; private set; }
        public async Task<string> DownloadVideoAsync(string videoUrl, string saveDirectory, string browser = "none", System.Threading.CancellationToken token = default, string cookieFile = "", Dictionary<string, string>? headers = null)
        {
            LastSubtitleDownloaded = false;

            // 1. URL 정화
            videoUrl = videoUrl.Trim().Trim('\"', '\'', ' ');
            if (videoUrl.StartsWith("yt-dlp", StringComparison.OrdinalIgnoreCase)) videoUrl = videoUrl.Remove(0, 6).Trim();
            if (videoUrl.Length == 11 && videoUrl.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'))
            {
                videoUrl = $"https://www.youtube.com/watch?v={videoUrl}";
            }
            if (videoUrl.Any(char.IsControl) ||
                !Uri.TryCreate(videoUrl, UriKind.Absolute, out var validatedUri) ||
                (validatedUri.Scheme != Uri.UriSchemeHttp && validatedUri.Scheme != Uri.UriSchemeHttps) ||
                !string.IsNullOrEmpty(validatedUri.UserInfo))
            {
                throw new ArgumentException("http:// 또는 https://로 시작하는 올바른 영상 주소를 입력해 주세요.", nameof(videoUrl));
            }
            videoUrl = validatedUri.AbsoluteUri;
            videoUrl = NormalizeYouTubeSingleVideoUrl(videoUrl);
            string sourcePageUrl = videoUrl;
            ISiteDownloadProfile siteProfile = SiteDownloadProfileRegistry.Resolve(sourcePageUrl);

            if (videoUrl.StartsWith("blob:", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("blob: 주소는 브라우저 내부용 가상 주소이므로 다운로드할 수 없습니다.\nF12(개발자 도구) -> 네트워크 탭에서 'm3u8' 또는 'manifest'를 검색하여 실제 주소를 찾아 입력해주세요.");
            }

            // [사용자 요청] m3u8 URL이 직접 입력된 경우 ffmpeg로 다이렉트 다운로드 (yt-dlp 우회)
            if (IsAnilifeManifestUrl(videoUrl))
            {
                return await DownloadAnilifeSegmentsAsync(videoUrl, saveDirectory, token, GetPreferredFileName("Anilife_Video"));
            }

            if (videoUrl.Contains(".m3u8") || videoUrl.Contains("api.gcdn.app"))
            {
                return await DownloadM3u8WithFFmpegAsync(videoUrl, saveDirectory, browser, token, headers, GetPreferredFileName(""));
            }

            // 2. [치지직 클립 리졸버 및 SOOP 리졸버]
            string customFileName = "";
            bool useDirectHlsDownload = false;
            bool useAnilifeSegmentDownload = false;
            string subtitleUrl = "";
            if (videoUrl.Contains("chzzk.naver.com/clips/"))
            {
                (string Url, string Title)? resolved = null;
                try { resolved = await ResolveChzzkClipAsync(videoUrl); } catch { }
                if (resolved != null)
                {
                    videoUrl = resolved.Value.Url;
                    customFileName = resolved.Value.Title;
                }
                else if (WebViewResolver != null)
                {
                    string m3u8 = await WebViewResolver(videoUrl);
                    if (!string.IsNullOrEmpty(m3u8))
                    {
                        videoUrl = m3u8;
                        customFileName = GetPreferredFileName("Chzzk_Clip");
                        useDirectHlsDownload = true;
                    }
                    else
                    {
                        throw new Exception("치지직 클립 정보를 가져오는 데 실패했습니다.\n로그인 브라우저에서 영상 페이지를 연 뒤 다시 시도해주세요.");
                    }
                }
                else
                {
                    throw new Exception("치지직 클립 정보를 가져오는 데 실패했습니다.\n(주소를 다시 확인하거나 잠시 후 시도해주세요.)");
                }
            }
            else if (videoUrl.Contains("chzzk.naver.com/video/", StringComparison.OrdinalIgnoreCase))
            {
                (string Url, string Title)? resolved = null;
                try { resolved = await ResolveChzzkVideoAsync(videoUrl); } catch { }
                if (resolved != null)
                {
                    videoUrl = resolved.Value.Url;
                    customFileName = resolved.Value.Title;
                    useDirectHlsDownload = true;
                }
                else if (WebViewResolver != null)
                {
                    string m3u8 = await WebViewResolver(videoUrl);
                    if (!string.IsNullOrWhiteSpace(m3u8))
                    {
                        videoUrl = m3u8;
                        customFileName = GetPreferredFileName("Chzzk_Video");
                        useDirectHlsDownload = true;
                    }
                    else
                    {
                        throw new Exception("치지직 VOD 재생 주소를 찾지 못했습니다.\n로그인이 필요한 영상이면 로그인 후 다운에서 다시 시도해 주세요.");
                    }
                }
                else
                {
                    throw new Exception("치지직 VOD 재생 주소를 찾지 못했습니다.\n잠시 후 다시 시도해 주세요.");
                }
            }
            else if (videoUrl.Contains("vod.sooplive.") && videoUrl.Contains("/player/"))
            {
                var resolved = await ResolveSoopCatchAsync(videoUrl);
                if (resolved != null)
                {
                    videoUrl = resolved.Value.Url;
                    customFileName = resolved.Value.Title;
                }
                else if (WebViewResolver != null)
                {
                    string m3u8 = await WebViewResolver(videoUrl);
                    if (!string.IsNullOrEmpty(m3u8))
                    {
                        videoUrl = m3u8;
                        customFileName = GetPreferredFileName("SOOP_Video");
                        useDirectHlsDownload = true;
                    }
                    else
                    {
                        throw new Exception("SOOP 영상 스트림을 자동으로 찾지 못했습니다.\n로그인 브라우저에서 영상 페이지를 연 뒤 다시 시도해주세요.");
                    }
                }
            }
            else if (videoUrl.Contains("tv.naver.com/"))
            {
                var resolved = await ResolveNaverTvAsync(videoUrl);
                if (resolved != null)
                {
                    videoUrl = resolved.Value.Url;
                    customFileName = resolved.Value.Title;
                }
            }
            else if (videoUrl.Contains("anilife.app/watch"))
            {
                var resolved = await ResolveAnilifAsync(videoUrl);
                if (resolved != null && IsAnilifeManifestUrl(resolved.Value.Url))
                {
                    videoUrl = resolved.Value.Url;
                    customFileName = resolved.Value.Title;
                    useDirectHlsDownload = true;
                    useAnilifeSegmentDownload = true;
                }
                else if (WebViewResolver != null)
                {
                    string m3u8 = await WebViewResolver(videoUrl);
                    if (IsAnilifeManifestUrl(m3u8))
                    {
                        videoUrl = m3u8;
                        customFileName = GetPreferredFileName("Anilife_Video");
                        useDirectHlsDownload = true;
                        useAnilifeSegmentDownload = true;
                    }
                    else
                    {
                        throw new Exception("애니라이프 재생 주소를 찾지 못했습니다.\n사이트가 직접 요청을 차단했을 수 있습니다. 잠시 후 한 번만 다시 시도해 주세요.");
                    }
                }
                else
                {
                    var resolvedAgain = await ResolveAnilifAsync(videoUrl);
                    if (resolvedAgain != null && IsAnilifeManifestUrl(resolvedAgain.Value.Url))
                    {
                        videoUrl = resolvedAgain.Value.Url;
                        customFileName = resolvedAgain.Value.Title;
                        useDirectHlsDownload = true;
                        useAnilifeSegmentDownload = true;
                    }
                    else
                    {
                        throw new Exception("애니라이프 재생 주소를 찾지 못했습니다.\n사이트가 직접 요청을 차단했을 수 있습니다. 잠시 후 한 번만 다시 시도해 주세요.");
                    }
                }
            }
            else if (IsLinkkfWatchUrl(videoUrl))
            {
                var resolved = await ResolveLinkkfAsync(videoUrl);
                if (resolved != null)
                {
                    videoUrl = resolved.Value.Url;
                    customFileName = resolved.Value.Title;
                    subtitleUrl = resolved.Value.SubtitleUrl;
                    useDirectHlsDownload = true;
                }
                else if (WebViewResolver != null)
                {
                    string m3u8 = await WebViewResolver(videoUrl);
                    if (!string.IsNullOrEmpty(m3u8))
                    {
                        videoUrl = m3u8;
                        customFileName = GetPreferredFileName("Linkkf_Video");
                        useDirectHlsDownload = true;
                    }
                    else
                    {
                        throw new Exception("Linkkf 영상 정보를 자동으로 가져오는 데 실패했습니다.\n잠시 후 다시 시도해주세요.");
                    }
                }
                else
                {
                    throw new Exception("Linkkf 영상 정보를 자동으로 가져오는 데 실패했습니다.\n잠시 후 다시 시도해주세요.");
                }
            }

            if (IsKuaishouShortVideoUrl(videoUrl))
            {
                var resolved = await ResolveKuaishouAsync(videoUrl, cookieFile, headers, token);
                string resolvedMediaUrl = resolved?.Url ?? "";
                string resolvedTitle = resolved?.Title ?? "";

                if (string.IsNullOrWhiteSpace(resolvedMediaUrl) && WebViewResolver != null)
                {
                    resolvedMediaUrl = await WebViewResolver(videoUrl);
                    resolvedTitle = PreferredTitle;
                }

                if (!IsSafeResolvedMediaUrl(resolvedMediaUrl, sourcePageUrl))
                {
                    throw new Exception(
                        "콰이쇼우 영상 정보를 가져오지 못했습니다.\n" +
                        "콰이쇼우 로그인 또는 보안 확인이 필요할 수 있습니다. 로그인 후 다운에서 영상이 재생되는지 확인한 뒤 다시 시도해 주세요.");
                }

                var kuaishouHeaders = headers != null
                    ? new Dictionary<string, string>(headers, StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (!kuaishouHeaders.ContainsKey("Referer")) kuaishouHeaders["Referer"] = sourcePageUrl;
                if (!kuaishouHeaders.ContainsKey("Origin")) kuaishouHeaders["Origin"] = "https://www.kuaishou.com";

                string kuaishouFileName = GetPreferredFileName(
                    string.IsNullOrWhiteSpace(resolvedTitle)
                        ? $"Kuaishou_{DateTime.Now:yyyyMMdd_HHmmss}"
                        : resolvedTitle);
                return await DownloadM3u8WithFFmpegAsync(
                    resolvedMediaUrl,
                    saveDirectory,
                    browser,
                    token,
                    kuaishouHeaders,
                    kuaishouFileName);
            }

            if (IsThreadsPostUrl(videoUrl))
            {
                if (WebViewResolver == null)
                    throw new Exception("Threads 영상 페이지를 열 수 없습니다. 프로그램을 다시 실행한 뒤 시도해 주세요.");

                string resolvedMediaUrl = await WebViewResolver(videoUrl);
                if (!IsSafeResolvedMediaUrl(resolvedMediaUrl, sourcePageUrl))
                    throw new Exception("Threads 게시물에서 재생 가능한 영상을 찾지 못했습니다. 삭제되었거나 영상이 없는 게시물인지 확인해 주세요.");

                var threadsHeaders = headers != null
                    ? new Dictionary<string, string>(headers, StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (!threadsHeaders.ContainsKey("Referer")) threadsHeaders["Referer"] = sourcePageUrl;
                if (!threadsHeaders.ContainsKey("Origin")) threadsHeaders["Origin"] = "https://www.threads.com";

                string threadsFileName = GetPreferredFileName($"Threads_{DateTime.Now:yyyyMMdd_HHmmss}");
                return await DownloadM3u8WithFFmpegAsync(
                    resolvedMediaUrl,
                    saveDirectory,
                    browser,
                    token,
                    threadsHeaders,
                    threadsFileName);
            }

            if (useDirectHlsDownload && (videoUrl.Contains(".m3u8") || videoUrl.Contains("api.gcdn.app")))
            {
                string downloadedPath;
                if (useAnilifeSegmentDownload || IsAnilifeManifestUrl(videoUrl))
                {
                    downloadedPath = await DownloadAnilifeSegmentsAsync(videoUrl, saveDirectory, token, customFileName);
                }
                else
                {
                    downloadedPath = await DownloadM3u8WithFFmpegAsync(videoUrl, saveDirectory, browser, token, headers, customFileName);
                }

                if (DownloadSubtitles && !string.IsNullOrWhiteSpace(subtitleUrl))
                {
                    LastSubtitleDownloaded = !string.IsNullOrWhiteSpace(await DownloadSubtitleSidecarAsync(subtitleUrl, downloadedPath, token));
                }

                return downloadedPath;
            }

            string ytDlpPath = SettingsManager.GetYtDlpPath();
            string ffmpegPath = SettingsManager.GetFFmpegPath();
            string ffmpegDir = Path.GetDirectoryName(ffmpegPath) ?? AppDomain.CurrentDomain.BaseDirectory;

            // 파일 중복 체크 및 이름변경 (customFileName이 있을 때)
            if (!string.IsNullOrEmpty(customFileName))
            {
                string baseName = customFileName;
                int counter = 2;
                // .mp4 를 기준으로 체크 (최종 출력이 mp4이므로)
                while (File.Exists(Path.Combine(saveDirectory, customFileName + ".mp4")))
                {
                    customFileName = $"{baseName}_{counter++}";
                }
            }
            
            // 파일명이 미리 결정되었다면 사용
            string metadataTemplate = string.IsNullOrWhiteSpace(OutputNameTemplate) ? "%(title)s" : OutputNameTemplate;
            string outputTemplate = !string.IsNullOrEmpty(customFileName) 
                ? Path.Combine(saveDirectory, $"{customFileName}.%(ext)s")
                : Path.Combine(saveDirectory, $"{metadataTemplate}.%(ext)s");

            // 일반 주소(제목 자동 추출)의 경우 yt-dlp 자체의 중복 방지 기능 활용은 어려우므로 
            // 일단 --no-overwrites 를 넣으면 파일이 있을 때 건너뛰게 됩니다.
            // 하지만 사용자 요청은 "이름을 늘려가라"는 것이므로, 
            // 모든 요청에 대해 고유 번호를 붙이는 것이 가장 단순하고 확실한 방법일 수 있습니다.
            // 여기서는 일단 customFileName이 없는 경우에도 yt-dlp가 같은 이름이 있으면 
            // 건너뛰지 않고 덮어쓰지 않도록 템플릿을 조금 수정하겠습니다.
            if (string.IsNullOrEmpty(customFileName))
            {
                // %(title)s.%(ext)s 대신 중복 시 숫자가 붙는 템플릿은 yt-dlp에서 복잡하므로 
                // 일단 수동으로 처리하거나 기본 동작(덮어쓰기)을 유지하되, 
                // 차후 더 정교한 체크를 위해서는 --get-filename 등이 필요합니다.
            }


            // 네이버 등 외부 사이트는 User-Agent와 Referer가 중요합니다.
            string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36";
            bool isYouTubeDownload = IsYouTubeUrl(videoUrl);

            string javaScriptRuntimeArguments = isYouTubeDownload ? GetYouTubeJavaScriptRuntimeArguments() : "";
            string warningArguments = isYouTubeDownload ? "" : "--no-warnings ";
            string arguments = $"{(isYouTubeDownload ? "--ignore-config " : "")}--newline --encoding utf-8 --user-agent \"{userAgent}\" {warningArguments}{javaScriptRuntimeArguments}";
            int concurrentFragments = Math.Clamp(siteProfile.ConcurrentFragments, 1, 3);
            if (concurrentFragments > 1)
            {
                arguments += $"--concurrent-fragments {concurrentFragments} ";
            }
            if (isYouTubeDownload)
            {
                arguments += "--no-playlist --playlist-items 1 ";
            }
            else if (PlaylistItemIndex > 0)
            {
                arguments += $"--playlist-items {PlaylistItemIndex} ";
            }
            
            // 사이트별로 적절한 Referer 설정
            string referer = siteProfile.Referer ?? "";
            if (string.IsNullOrEmpty(referer))
            {
                if (videoUrl.Contains("chzzk.naver.com")) referer = "https://chzzk.naver.com/";
                else if (videoUrl.Contains("pstatic.net")) referer = "https://chzzk.naver.com/";
                else if (videoUrl.Contains("naver.com")) referer = "https://tv.naver.com/";
                else if (videoUrl.Contains("x.com") || videoUrl.Contains("twitter.com")) referer = "https://x.com/";
                else if (videoUrl.Contains("instagram.com")) referer = "https://www.instagram.com/";
                else if (videoUrl.Contains("gcdn.app") || videoUrl.Contains("anilife.app")) referer = "https://anilife.app/";
                else if (IsLinkkfWatchUrl(videoUrl)) referer = GetUrlOrigin(videoUrl);
            }

            if (!string.IsNullOrEmpty(referer)) arguments += $"--referer {QuoteProcessArgument(referer)} ";
            
            // 쿠키(로그인 정보) 설정
            if (!string.IsNullOrEmpty(cookieFile) && File.Exists(cookieFile))
            {
                arguments += $"--cookies {QuoteProcessArgument(cookieFile)} ";
            }
            else if (!string.IsNullOrEmpty(browser) && browser != "none")
            {
                string normalizedBrowser = browser.Trim().ToLowerInvariant();
                if (normalizedBrowser is not ("chrome" or "edge" or "firefox" or "brave" or "opera" or "vivaldi" or "whale"))
                    throw new ArgumentException("지원하지 않는 브라우저 쿠키 방식입니다.", nameof(browser));
                arguments += $"--cookies-from-browser {QuoteProcessArgument(normalizedBrowser)} ";
            }

            // 추가 헤더 설정 (Twitter 비공개 계정 등 우회용)
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    if (!string.IsNullOrEmpty(header.Value))
                    {
                        string headerName = header.Key.Trim();
                        string headerValue = header.Value.Replace("\r", "").Replace("\n", "").Trim();
                        if (headerName.Length == 0 || headerName.Any(c => !(char.IsLetterOrDigit(c) || c == '-'))) continue;
                        arguments += $"--add-header {QuoteProcessArgument(headerName + ":" + headerValue)} ";
                    }
                }
            }
            
            string commonArguments = arguments;
            string formatArguments = "";
            if (FormatSelector.Equals("best_mp3", StringComparison.OrdinalIgnoreCase))
            {
                formatArguments = "-x --audio-format mp3 ";
            }
            else if (!string.IsNullOrWhiteSpace(FormatSelector))
            {
                formatArguments = $"-f {QuoteProcessArgument(FormatSelector)} ";
            }

            bool audioOnlyDownload = FormatSelector.Equals("best_mp3", StringComparison.OrdinalIgnoreCase);
            arguments = BuildYtDlpDownloadArguments(commonArguments, formatArguments, ffmpegDir, outputTemplate, videoUrl, audioOnlyDownload);

            var result = await RunYtDlpProcessAsync(arguments);

            if (result.Success && YtDlpReportedExistingFile(result.StandardOutput, result.ErrorOutput))
            {
                string existingFilePath = result.DownloadedFilePath;
                for (int retry = 0; retry < 3 && result.Success && YtDlpReportedExistingFile(result.StandardOutput, result.ErrorOutput); retry++)
                {
                    string uniqueOutputTemplate = BuildNextAvailableOutputTemplate(existingFilePath);
                    if (string.IsNullOrWhiteSpace(uniqueOutputTemplate)) break;

                    outputTemplate = uniqueOutputTemplate;
                    string duplicateArguments = BuildYtDlpDownloadArguments(
                        commonArguments,
                        formatArguments,
                        ffmpegDir,
                        outputTemplate,
                        videoUrl,
                        audioOnlyDownload);
                    result = await RunYtDlpProcessAsync(duplicateArguments);
                }

                if (result.Success && YtDlpReportedExistingFile(result.StandardOutput, result.ErrorOutput))
                {
                    result = (
                        false,
                        "ERROR: A unique output filename could not be allocated after repeated attempts.",
                        result.DownloadedFilePath,
                        result.StandardOutput);
                }
            }

            if (!result.Success && IsInvalidFilenameError(result.ErrorOutput))
            {
                outputTemplate = Path.Combine(saveDirectory, $"Video_{DateTime.Now:yyyyMMdd_HHmmss}_%(extractor)s_%(id)s.%(ext)s");
                string safeNameArguments = BuildYtDlpDownloadArguments(commonArguments, formatArguments, ffmpegDir, outputTemplate, videoUrl, audioOnlyDownload, strictFileNames: true);
                result = await RunYtDlpProcessAsync(safeNameArguments);
            }

            if (!result.Success && isYouTubeDownload && IsRequestedFormatUnavailable(result.ErrorOutput))
            {
                foreach (string fallbackArguments in BuildYouTubeFormatFallbackArguments(commonArguments, ffmpegDir, outputTemplate, videoUrl))
                {
                    var fallbackResult = await RunYtDlpProcessAsync(fallbackArguments);
                    if (fallbackResult.Success)
                    {
                        result = fallbackResult;
                        break;
                    }

                    result = fallbackResult;
                }

                if (!result.Success)
                {
                    var formatListResult = await RunYtDlpProcessAsync($"{commonArguments}-F {QuoteProcessArgument(videoUrl)}");
                    string formatList = formatListResult.StandardOutput.Trim();
                    if (!string.IsNullOrWhiteSpace(formatList))
                    {
                        result = (false, result.ErrorOutput + Environment.NewLine + "[Available formats]" + Environment.NewLine + formatList, result.DownloadedFilePath, result.StandardOutput);
                    }
                }
            }

            if (!result.Success &&
                !isYouTubeDownload &&
                !audioOnlyDownload &&
                WebViewResolver != null &&
                (result.ErrorOutput.Contains("Unsupported URL", StringComparison.OrdinalIgnoreCase) ||
                 IsXStatusUrl(sourcePageUrl)))
            {
                string resolvedMediaUrl = "";
                try
                {
                    resolvedMediaUrl = await WebViewResolver(sourcePageUrl);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    resolvedMediaUrl = "";
                }

                if (IsSafeResolvedMediaUrl(resolvedMediaUrl, sourcePageUrl))
                {
                    var fallbackHeaders = headers != null
                        ? new Dictionary<string, string>(headers, StringComparer.OrdinalIgnoreCase)
                        : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                    if (!fallbackHeaders.ContainsKey("Referer"))
                        fallbackHeaders["Referer"] = sourcePageUrl;

                    try
                    {
                        var sourceUri = new Uri(sourcePageUrl);
                        if (!fallbackHeaders.ContainsKey("Origin"))
                            fallbackHeaders["Origin"] = $"{sourceUri.Scheme}://{sourceUri.Authority}";
                    }
                    catch { }

                    string fallbackName = GetPreferredFileName($"Video_{DateTime.Now:yyyyMMdd_HHmmss}");
                    return await DownloadM3u8WithFFmpegAsync(
                        resolvedMediaUrl,
                        saveDirectory,
                        browser,
                        token,
                        fallbackHeaders,
                        fallbackName);
                }
            }

            if (result.Success)
            {
                if (!TryResolveDownloadedFile(outputTemplate, result.DownloadedFilePath, out string resolvedDownloadedFilePath))
                {
                    result = (
                        false,
                        "ERROR: No downloaded media file was created. The page may not contain a downloadable video, or yt-dlp did not report an output file.",
                        result.DownloadedFilePath,
                        result.StandardOutput);
                }
                else
                {
                    result = (true, result.ErrorOutput, resolvedDownloadedFilePath, result.StandardOutput);
                }
            }

            if (result.Success &&
                siteProfile.SiteKey == "Snapchat" &&
                IsHlsPlaylistFile(result.DownloadedFilePath))
            {
                var urlResult = await RunYtDlpProcessAsync(
                    $"{commonArguments}--get-url {QuoteProcessArgument(sourcePageUrl)}");
                string resolvedMediaUrl = GetLastHttpUrl(urlResult.StandardOutput);
                if (!urlResult.Success || !IsSafeResolvedMediaUrl(resolvedMediaUrl, sourcePageUrl))
                {
                    throw new Exception("Snapchat 재생 주소를 가져오지 못했습니다.");
                }

                string invalidPlaylistPath = result.DownloadedFilePath;
                string snapchatFileName = GetPreferredFileName(Path.GetFileNameWithoutExtension(invalidPlaylistPath));
                CleanupManager.DeleteTemporaryPath(invalidPlaylistPath);

                var snapchatHeaders = headers != null
                    ? new Dictionary<string, string>(headers, StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                snapchatHeaders["Referer"] = sourcePageUrl;
                snapchatHeaders["Origin"] = "https://www.snapchat.com";

                return await DownloadM3u8WithFFmpegAsync(
                    resolvedMediaUrl,
                    saveDirectory,
                    browser,
                    token,
                    snapchatHeaders,
                    snapchatFileName);
            }

            if (result.Success)
            {
                if (DownloadSubtitles)
                {
                    LastSubtitleDownloaded = await TryDownloadYtDlpSubtitlesAsync(ytDlpPath, commonArguments, ffmpegDir, outputTemplate, videoUrl, token);
                }
                OnDownloadCompleted?.Invoke(result.DownloadedFilePath);
                return result.DownloadedFilePath;
            }

            token.ThrowIfCancellationRequested();

            string msg = "다운로드 실패";
            if (result.ErrorOutput.Contains("400") || result.ErrorOutput.Contains("Bad Request"))
            {
                msg = "다운로드 실패 (HTTP 400: Bad Request)\n\n" +
                      "💡 알림: 치지직 성인/제한 영상은 현재 다운로드가 어렵습니다.";
            }
            else if (result.ErrorOutput.Contains("Unsupported URL", StringComparison.OrdinalIgnoreCase))
            {
                msg = "다운로드 실패: 현재 이 주소 형식을 지원하지 않습니다.";
            }

            msg += $"\n\n[오류 설명]:\n{result.ErrorOutput}";
            throw new Exception(msg);

            async Task<(bool Success, string ErrorOutput, string DownloadedFilePath, string StandardOutput)> RunYtDlpProcessAsync(string runArguments)
            {
                string errorOutput = string.Empty;
                string standardOutput = string.Empty;
                string downloadedFilePath = string.Empty;
                string tempDownloadDirectory = Path.Combine(Path.GetTempPath(), "MMT", "Downloads", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDownloadDirectory);
                CleanupManager.RegisterFile(tempDownloadDirectory);
                runArguments = $"--paths {QuoteProcessArgument("temp:" + tempDownloadDirectory)} {runArguments}";

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = ytDlpPath,
                    Arguments = runArguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardErrorEncoding = System.Text.Encoding.UTF8
                };

                using (Process process = new Process())
                {
                    process.StartInfo = psi;
                    Regex progressRegex = new Regex(@"\[download\]\s+(?<percent>\d+(\.\d+)?)%");
                    Regex outputRegex = new Regex(@"(?:(?:Merging formats into|Destination:)\s+""?(?<filepath>.+?)""?$|\[download\]\s+(?<filepath>.+?)\s+has already been downloaded)");

                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (string.IsNullOrEmpty(e.Data)) return;
                        standardOutput += e.Data + Environment.NewLine;
                        Match m = progressRegex.Match(e.Data);
                        if (m.Success && double.TryParse(m.Groups["percent"].Value, out double p)) OnProgressChanged?.Invoke(p);

                        Match fm = outputRegex.Match(e.Data);
                        if (fm.Success)
                        {
                            downloadedFilePath = fm.Groups["filepath"].Value.Trim().Trim('"');
                        }
                    };

                    process.ErrorDataReceived += (sender, e) => { if (!string.IsNullOrEmpty(e.Data)) errorOutput += e.Data + Environment.NewLine; };

                    try
                    {
                        process.Start();
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();

                        using (token.Register(() => { try { if (!process.HasExited) process.Kill(true); } catch { } }))
                        {
                            await process.WaitForExitAsync();
                        }

                        token.ThrowIfCancellationRequested();
                        return (process.ExitCode == 0, errorOutput, downloadedFilePath, standardOutput);
                    }
                    catch (OperationCanceledException) { throw; }
                    finally
                    {
                        CleanupManager.DeleteTemporaryPath(tempDownloadDirectory);
                    }
                }
            }
        }

        private static IEnumerable<string> BuildYouTubeFormatFallbackArguments(string commonArguments, string ffmpegDir, string outputTemplate, string videoUrl)
        {
            string fileNameSafetyArgs = "--windows-filenames --trim-filenames 160 ";
            string ffmpeg = QuoteProcessArgument(ffmpegDir);
            string output = QuoteProcessArgument(outputTemplate);
            string url = QuoteProcessArgument(videoUrl);
            yield return $"{commonArguments}-f {QuoteProcessArgument("bv*[vcodec^=avc1]+ba[acodec^=mp4a]/bv*+ba/b[ext=mp4]/b")} {fileNameSafetyArgs}--merge-output-format mp4 --ffmpeg-location {ffmpeg} -o {output} {url}";
            yield return $"{commonArguments}-f {QuoteProcessArgument("bestvideo*+bestaudio/best")} {fileNameSafetyArgs}--merge-output-format mp4 --ffmpeg-location {ffmpeg} -o {output} {url}";
            yield return $"{commonArguments}-f {QuoteProcessArgument("best")} --no-check-formats --hls-use-mpegts {fileNameSafetyArgs}--ffmpeg-location {ffmpeg} -o {output} {url}";
            yield return $"{commonArguments}--extractor-args {QuoteProcessArgument("youtube:player_client=web,web_safari,android,ios")} -f {QuoteProcessArgument("bestvideo*+bestaudio/best")} {fileNameSafetyArgs}--merge-output-format mp4 --ffmpeg-location {ffmpeg} -o {output} {url}";
            yield return $"{commonArguments}-f {QuoteProcessArgument("worst/best")} --no-check-formats {fileNameSafetyArgs}--ffmpeg-location {ffmpeg} -o {output} {url}";
        }

        private static string BuildYtDlpDownloadArguments(string commonArguments, string formatArguments, string ffmpegDir, string outputTemplate, string videoUrl, bool audioOnlyDownload, bool strictFileNames = false)
        {
            string fileNameSafetyArgs = strictFileNames
                ? "--windows-filenames --restrict-filenames --trim-filenames 120 "
                : "--windows-filenames --trim-filenames 160 ";

            string mergeArguments = audioOnlyDownload ? "" : "--merge-output-format mp4 ";
            return $"{commonArguments}{formatArguments}{fileNameSafetyArgs}--ffmpeg-location {QuoteProcessArgument(ffmpegDir)} {mergeArguments}-o {QuoteProcessArgument(outputTemplate)} {QuoteProcessArgument(videoUrl)}";
        }

        private static string QuoteProcessArgument(string value)
        {
            if (value == null) return "\"\"";
            if (value.IndexOf('\0') >= 0) throw new ArgumentException("명령 인수에 사용할 수 없는 문자가 포함되어 있습니다.");

            var result = new System.Text.StringBuilder(value.Length + 2);
            result.Append('"');
            int backslashes = 0;
            foreach (char c in value)
            {
                if (c == '\\')
                {
                    backslashes++;
                    continue;
                }

                if (c == '"')
                {
                    result.Append('\\', backslashes * 2 + 1);
                    result.Append('"');
                    backslashes = 0;
                    continue;
                }

                result.Append('\\', backslashes);
                backslashes = 0;
                result.Append(c);
            }

            result.Append('\\', backslashes * 2);
            result.Append('"');
            return result.ToString();
        }

        private static bool IsInvalidFilenameError(string errorOutput)
        {
            if (string.IsNullOrWhiteSpace(errorOutput)) return false;

            return errorOutput.Contains("Invalid argument", StringComparison.OrdinalIgnoreCase)
                || errorOutput.Contains("Errno 22", StringComparison.OrdinalIgnoreCase)
                || errorOutput.Contains("unable to open for writing", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsHlsPlaylistFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return false;

            try
            {
                using var reader = new StreamReader(filePath);
                return string.Equals(reader.ReadLine()?.Trim(), "#EXTM3U", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static string GetLastHttpUrl(string output)
        {
            if (string.IsNullOrWhiteSpace(output)) return "";

            return output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .LastOrDefault(line => Uri.TryCreate(line, UriKind.Absolute, out Uri? uri) &&
                    (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)) ?? "";
        }

        private static bool YtDlpReportedExistingFile(string standardOutput, string errorOutput)
        {
            return standardOutput.Contains("has already been downloaded", StringComparison.OrdinalIgnoreCase) ||
                   errorOutput.Contains("has already been downloaded", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildNextAvailableOutputTemplate(string existingFilePath)
        {
            if (string.IsNullOrWhiteSpace(existingFilePath)) return "";

            try
            {
                string directory = Path.GetDirectoryName(existingFilePath) ?? "";
                string baseName = Path.GetFileNameWithoutExtension(existingFilePath);
                string extension = Path.GetExtension(existingFilePath);
                if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(baseName)) return "";

                int counter = 2;
                string candidatePath;
                do
                {
                    candidatePath = Path.Combine(directory, $"{baseName}_{counter++}{extension}");
                }
                while (File.Exists(candidatePath));

                return Path.Combine(directory, $"{Path.GetFileNameWithoutExtension(candidatePath)}.%(ext)s");
            }
            catch
            {
                return "";
            }
        }

        private static bool TryResolveDownloadedFile(string outputTemplate, string reportedPath, out string downloadedFilePath)
        {
            downloadedFilePath = reportedPath;
            if (!string.IsNullOrWhiteSpace(downloadedFilePath) && File.Exists(downloadedFilePath))
            {
                return true;
            }

            downloadedFilePath = "";
            return false;
        }

        private static bool IsYouTubeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            return url.Contains("youtube.com", StringComparison.OrdinalIgnoreCase)
                || url.Contains("youtu.be", StringComparison.OrdinalIgnoreCase)
                || url.Contains("youtube-nocookie.com", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsXStatusUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
            string host = uri.Host;
            bool isXHost = host.Equals("x.com", StringComparison.OrdinalIgnoreCase) ||
                           host.EndsWith(".x.com", StringComparison.OrdinalIgnoreCase) ||
                           host.Equals("twitter.com", StringComparison.OrdinalIgnoreCase) ||
                           host.EndsWith(".twitter.com", StringComparison.OrdinalIgnoreCase);
            if (!isXHost) return false;

            return System.Text.RegularExpressions.Regex.IsMatch(
                uri.AbsolutePath,
                @"/[^/]+/status/\d+",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        private static bool IsThreadsPostUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
            bool isThreadsHost = uri.Host.Equals("threads.com", StringComparison.OrdinalIgnoreCase) ||
                                 uri.Host.EndsWith(".threads.com", StringComparison.OrdinalIgnoreCase) ||
                                 uri.Host.Equals("threads.net", StringComparison.OrdinalIgnoreCase) ||
                                 uri.Host.EndsWith(".threads.net", StringComparison.OrdinalIgnoreCase);
            return isThreadsHost && Regex.IsMatch(uri.AbsolutePath, @"/@[^/]+/post/[^/?#]+", RegexOptions.IgnoreCase);
        }

        private static bool IsKuaishouShortVideoUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
            bool isKuaishouHost = uri.Host.Equals("kuaishou.com", StringComparison.OrdinalIgnoreCase) ||
                                  uri.Host.EndsWith(".kuaishou.com", StringComparison.OrdinalIgnoreCase);
            return isKuaishouHost && Regex.IsMatch(uri.AbsolutePath, @"^/short-video/[^/?#]+", RegexOptions.IgnoreCase);
        }

        private static string GetKuaishouPhotoId(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return "";
            Match match = Regex.Match(uri.AbsolutePath, @"^/short-video/([^/?#]+)", RegexOptions.IgnoreCase);
            return match.Success ? Uri.UnescapeDataString(match.Groups[1].Value) : "";
        }

        private async Task<(string Url, string Title)?> ResolveKuaishouAsync(
            string originalUrl,
            string cookieFile,
            Dictionary<string, string>? headers,
            System.Threading.CancellationToken token)
        {
            string photoId = GetKuaishouPhotoId(originalUrl);
            if (string.IsNullOrWhiteSpace(photoId)) return null;

            try
            {
                var handler = new System.Net.Http.HttpClientHandler
                {
                    AllowAutoRedirect = true,
                    AutomaticDecompression = System.Net.DecompressionMethods.All,
                    CookieContainer = new System.Net.CookieContainer(),
                    UseCookies = true
                };
                LoadKuaishouCookies(handler.CookieContainer, cookieFile);

                using var client = new System.Net.Http.HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(20)
                };
                string userAgent = headers != null && headers.TryGetValue("User-Agent", out string? suppliedUserAgent) &&
                                   !string.IsNullOrWhiteSpace(suppliedUserAgent)
                    ? suppliedUserAgent
                    : "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36";
                client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);
                client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8");

                using (var pageRequest = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, originalUrl))
                {
                    pageRequest.Headers.Referrer = new Uri("https://www.kuaishou.com/");
                    using System.Net.Http.HttpResponseMessage pageResponse = await client.SendAsync(pageRequest, token);
                    if (pageResponse.IsSuccessStatusCode)
                    {
                        string pageHtml = await pageResponse.Content.ReadAsStringAsync(token);
                        if (TryParseKuaishouApolloState(pageHtml, photoId, out var pageResult))
                            return pageResult;
                    }
                }

                const string query = "query visionVideoDetail($photoId: String, $type: String, $page: String, $webPageArea: String) { visionVideoDetail(photoId: $photoId, type: $type, page: $page, webPageArea: $webPageArea) { status type author { id name } photo { id caption photoUrl coverUrl videoResource manifest { adaptationSet { representation { id defaultSelect backupUrl codecs url height width avgBitrate maxBitrate qualityType qualityLabel frameRate hidden disableAdaptive } } } } } }";
                string requestBody = JsonSerializer.Serialize(new
                {
                    operationName = "visionVideoDetail",
                    variables = new { photoId, type = (string?)null, page = "detail", webPageArea = "homexxunknown" },
                    query
                });

                foreach (string endpoint in new[]
                {
                    "https://www.kuaishou.com/graphql",
                    "https://video.kuaishou.com/graphql"
                })
                {
                    using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, endpoint);
                    request.Headers.Referrer = new Uri(originalUrl);
                    request.Headers.TryAddWithoutValidation("Origin", "https://www.kuaishou.com");
                    request.Content = new System.Net.Http.StringContent(requestBody, System.Text.Encoding.UTF8, "application/json");

                    using System.Net.Http.HttpResponseMessage response = await client.SendAsync(request, token);
                    if (!response.IsSuccessStatusCode) continue;

                    string responseBody = await response.Content.ReadAsStringAsync(token);
                    if (TryParseKuaishouGraphQlResponse(responseBody, photoId, out var result))
                        return result;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[KuaishouResolver] {ex.Message}");
            }

            return null;
        }

        private static void LoadKuaishouCookies(System.Net.CookieContainer container, string cookieFile)
        {
            if (string.IsNullOrWhiteSpace(cookieFile) || !File.Exists(cookieFile)) return;

            try
            {
                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                foreach (string rawLine in File.ReadLines(cookieFile))
                {
                    string line = rawLine;
                    if (line.StartsWith("#HttpOnly_", StringComparison.OrdinalIgnoreCase))
                        line = line.Substring("#HttpOnly_".Length);
                    else if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                        continue;

                    string[] fields = line.Split('\t');
                    if (fields.Length < 7) continue;
                    string domain = fields[0].Trim();
                    if (!domain.TrimStart('.').EndsWith("kuaishou.com", StringComparison.OrdinalIgnoreCase)) continue;
                    if (long.TryParse(fields[4], out long expires) && expires > 0 && expires < now) continue;

                    var cookie = new System.Net.Cookie(fields[5], fields[6], string.IsNullOrWhiteSpace(fields[2]) ? "/" : fields[2], domain)
                    {
                        Secure = fields[3].Equals("TRUE", StringComparison.OrdinalIgnoreCase)
                    };
                    if (long.TryParse(fields[4], out expires) && expires > 0 && expires <= 253402300799)
                        cookie.Expires = DateTimeOffset.FromUnixTimeSeconds(expires).UtcDateTime;
                    container.Add(cookie);
                }
            }
            catch { }
        }

        private static bool TryParseKuaishouGraphQlResponse(
            string json,
            string photoId,
            out (string Url, string Title) result)
        {
            result = default;
            try
            {
                using JsonDocument document = JsonDocument.Parse(json);
                JsonElement root = document.RootElement;
                if (!root.TryGetProperty("data", out JsonElement data) ||
                    !data.TryGetProperty("visionVideoDetail", out JsonElement detail))
                {
                    return false;
                }

                return TryParseKuaishouDetail(detail, data, photoId, out result);
            }
            catch
            {
                return false;
            }
        }

        private static bool TryParseKuaishouApolloState(
            string html,
            string photoId,
            out (string Url, string Title) result)
        {
            result = default;
            const string marker = "window.__APOLLO_STATE__=";
            int start = html.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0) return false;
            start += marker.Length;

            int scriptEnd = html.IndexOf("</script>", start, StringComparison.OrdinalIgnoreCase);
            if (scriptEnd < 0) return false;
            int initializerEnd = html.IndexOf(";(function()", start, StringComparison.Ordinal);
            int end = initializerEnd >= 0 && initializerEnd < scriptEnd ? initializerEnd : scriptEnd;
            string stateJson = html.Substring(start, end - start).Trim().TrimEnd(';');

            try
            {
                using JsonDocument document = JsonDocument.Parse(stateJson);
                JsonElement root = document.RootElement;
                if (!root.TryGetProperty("defaultClient", out JsonElement cache) || cache.ValueKind != JsonValueKind.Object)
                    return false;

                foreach (JsonProperty property in cache.EnumerateObject())
                {
                    if (!property.Name.Contains("visionVideoDetail", StringComparison.OrdinalIgnoreCase) ||
                        !property.Name.Contains(photoId, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    JsonElement detail = ResolveKuaishouCacheReference(property.Value, cache);
                    if (TryParseKuaishouDetail(detail, cache, photoId, out result)) return true;
                }
            }
            catch { }

            return false;
        }

        private static JsonElement ResolveKuaishouCacheReference(JsonElement value, JsonElement cache)
        {
            if (value.ValueKind == JsonValueKind.Object &&
                value.TryGetProperty("type", out JsonElement type) &&
                type.GetString()?.Equals("id", StringComparison.OrdinalIgnoreCase) == true &&
                value.TryGetProperty("id", out JsonElement id) &&
                id.ValueKind == JsonValueKind.String &&
                cache.TryGetProperty(id.GetString() ?? "", out JsonElement resolved))
            {
                return resolved;
            }

            return value;
        }

        private static bool TryParseKuaishouDetail(
            JsonElement detail,
            JsonElement cache,
            string photoId,
            out (string Url, string Title) result)
        {
            result = default;
            if (detail.ValueKind != JsonValueKind.Object ||
                !detail.TryGetProperty("photo", out JsonElement photoValue) ||
                photoValue.ValueKind == JsonValueKind.Null)
            {
                return false;
            }

            JsonElement photo = ResolveKuaishouCacheReference(photoValue, cache);
            if (photo.ValueKind != JsonValueKind.Object) return false;

            string title = photo.TryGetProperty("caption", out JsonElement caption) && caption.ValueKind == JsonValueKind.String
                ? caption.GetString() ?? ""
                : "";
            if (string.IsNullOrWhiteSpace(title)) title = $"Kuaishou_{photoId}";

            var candidates = new List<(string Url, long Pixels, long Bitrate, double FrameRate)>();
            if (photo.TryGetProperty("manifest", out JsonElement manifest) && manifest.ValueKind == JsonValueKind.Object &&
                manifest.TryGetProperty("adaptationSet", out JsonElement adaptationSets) && adaptationSets.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement adaptationSet in adaptationSets.EnumerateArray())
                {
                    if (!adaptationSet.TryGetProperty("representation", out JsonElement representations) ||
                        representations.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    foreach (JsonElement representation in representations.EnumerateArray())
                    {
                        if (representation.TryGetProperty("hidden", out JsonElement hidden) && hidden.ValueKind == JsonValueKind.True)
                            continue;

                        long width = GetKuaishouNumber(representation, "width");
                        long height = GetKuaishouNumber(representation, "height");
                        long bitrate = Math.Max(
                            GetKuaishouNumber(representation, "avgBitrate"),
                            GetKuaishouNumber(representation, "maxBitrate"));
                        double frameRate = GetKuaishouDouble(representation, "frameRate");
                        AddKuaishouCandidate(representation, "url", width, height, bitrate, frameRate, candidates);
                        AddKuaishouCandidate(representation, "backupUrl", width, height, bitrate, frameRate, candidates);
                    }
                }
            }

            AddKuaishouCandidate(photo, "photoUrl", 0, 0, 0, 0, candidates);
            AddKuaishouCandidate(photo, "videoResource", 0, 0, 0, 0, candidates);

            string bestUrl = candidates
                .Where(item => Uri.TryCreate(item.Url, UriKind.Absolute, out Uri? uri) &&
                               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                .OrderByDescending(item => item.Pixels)
                .ThenByDescending(item => item.Bitrate)
                .ThenByDescending(item => item.FrameRate)
                .Select(item => item.Url)
                .FirstOrDefault() ?? "";
            if (string.IsNullOrWhiteSpace(bestUrl)) return false;

            result = (bestUrl, title.Trim());
            return true;
        }

        private static void AddKuaishouCandidate(
            JsonElement parent,
            string propertyName,
            long width,
            long height,
            long bitrate,
            double frameRate,
            List<(string Url, long Pixels, long Bitrate, double FrameRate)> candidates)
        {
            if (!parent.TryGetProperty(propertyName, out JsonElement value)) return;

            if (value.ValueKind == JsonValueKind.String)
            {
                string url = value.GetString() ?? "";
                if (Uri.TryCreate(url, UriKind.Absolute, out _))
                    candidates.Add((url, width * height, bitrate, frameRate));
            }
            else if (value.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement item in value.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.String) continue;
                    string url = item.GetString() ?? "";
                    if (Uri.TryCreate(url, UriKind.Absolute, out _))
                        candidates.Add((url, width * height, bitrate, frameRate));
                }
            }
        }

        private static long GetKuaishouNumber(JsonElement parent, string propertyName)
        {
            if (!parent.TryGetProperty(propertyName, out JsonElement value)) return 0;
            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out long number)) return number;
            return value.ValueKind == JsonValueKind.String && long.TryParse(value.GetString(), out number) ? number : 0;
        }

        private static double GetKuaishouDouble(JsonElement parent, string propertyName)
        {
            if (!parent.TryGetProperty(propertyName, out JsonElement value)) return 0;
            if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out double number)) return number;
            return value.ValueKind == JsonValueKind.String && double.TryParse(value.GetString(), out number) ? number : 0;
        }

        private static bool IsSafeResolvedMediaUrl(string candidate, string sourcePageUrl)
        {
            if (!Uri.TryCreate(candidate, UriKind.Absolute, out var candidateUri)) return false;
            if (candidateUri.Scheme != Uri.UriSchemeHttp && candidateUri.Scheme != Uri.UriSchemeHttps) return false;
            if (candidateUri.IsLoopback) return false;
            if (string.Equals(candidate, sourcePageUrl, StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        }

        private static string GetYouTubeJavaScriptRuntimeArguments()
        {
            string denoPath = SettingsManager.GetDenoPath();
            return File.Exists(denoPath)
                ? $"--js-runtimes \"deno:{denoPath}\" "
                : "";
        }

        private static string NormalizeYouTubeSingleVideoUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return url;
            string input = url.Trim();
            if (!Uri.TryCreate(input, UriKind.Absolute, out var uri)) return input;

            string host = uri.Host.ToLowerInvariant();
            bool isYouTubeHost = host == "youtube.com"
                || host.EndsWith(".youtube.com")
                || host == "youtube-nocookie.com"
                || host.EndsWith(".youtube-nocookie.com");

            if (host == "youtu.be")
            {
                string videoId = uri.AbsolutePath.Trim('/');
                if (string.IsNullOrWhiteSpace(videoId)) return input;
                return new UriBuilder(uri.Scheme, uri.Host, uri.Port, "/" + videoId) { Query = "" }.Uri.ToString();
            }

            if (!isYouTubeHost || !uri.AbsolutePath.Equals("/watch", StringComparison.OrdinalIgnoreCase)) return input;

            string videoIdFromQuery = GetQueryParameterValue(uri.Query, "v");
            if (string.IsNullOrWhiteSpace(videoIdFromQuery)) return input;

            return new UriBuilder(uri.Scheme, uri.Host, uri.Port, uri.AbsolutePath)
            {
                Query = "v=" + Uri.EscapeDataString(videoIdFromQuery)
            }.Uri.ToString();
        }

        private static string GetQueryParameterValue(string query, string key)
        {
            if (string.IsNullOrWhiteSpace(query)) return "";
            string trimmed = query.TrimStart('?');
            foreach (string part in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] pair = part.Split('=', 2);
                string name = Uri.UnescapeDataString(pair[0].Replace("+", " "));
                if (!name.Equals(key, StringComparison.OrdinalIgnoreCase)) continue;

                return pair.Length > 1 ? Uri.UnescapeDataString(pair[1].Replace("+", " ")) : "";
            }

            return "";
        }

        private static bool IsRequestedFormatUnavailable(string errorOutput)
        {
            return !string.IsNullOrWhiteSpace(errorOutput)
                && (errorOutput.Contains("Requested format is not available", StringComparison.OrdinalIgnoreCase)
                    || errorOutput.Contains("Use --list-formats", StringComparison.OrdinalIgnoreCase)
                    || errorOutput.Contains("No video formats", StringComparison.OrdinalIgnoreCase));
        }

        private async Task<(string Url, string Title)?> ResolveChzzkClipAsync(string originalUrl)
        {
            string clipId = "";
            try
            {
                var parts = originalUrl.Split(new[] { "/clips/" }, StringSplitOptions.None);
                if (parts.Length < 2) return null;
                clipId = parts[1].Split('?')[0].Trim('/');

                using var client = new System.Net.Http.HttpClient();
                client.Timeout = TimeSpan.FromSeconds(15);
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
                client.DefaultRequestHeaders.Add("Referer", "https://chzzk.naver.com/");

                // 1단계: 플레이 정보 가져오기 (videoId, inKey 획득)
                string playInfoUrl = $"https://api.chzzk.naver.com/service/v1/play-info/clip/{clipId}";
                var infoRes = await client.GetAsync(playInfoUrl);
                if (!infoRes.IsSuccessStatusCode) throw new Exception($"Clip API 호출 실패 (Status: {infoRes.StatusCode})");

                var infoBytes = await infoRes.Content.ReadAsByteArrayAsync();
                string infoJson = System.Text.Encoding.UTF8.GetString(infoBytes);
                using var infoDoc = System.Text.Json.JsonDocument.Parse(infoJson);
                var content = infoDoc.RootElement.GetProperty("content");

                string videoId = content.GetProperty("videoId").GetString() ?? "";
                string inKey = content.GetProperty("inKey").GetString() ?? "";
                string title = content.TryGetProperty("contentTitle", out var t) ? t.GetString() ?? "Chzzk_Clip" : "Chzzk_Clip";

                if (string.IsNullOrEmpty(videoId) || string.IsNullOrEmpty(inKey))
                {
                    throw new Exception("재생 정보를 찾을 수 없습니다. (제한된 영상일 수 있습니다.)");
                }

                // 2단계: 실제 스트리밍 주소(M3U8) 가져오기
                string playbackUrl = $"https://apis.naver.com/neonplayer/vodplay/v2/playback/{videoId}?key={inKey}&deviceType=pc";
                var pbRes = await client.GetAsync(playbackUrl);
                if (!pbRes.IsSuccessStatusCode) throw new Exception("Playback API 호출 실패");

                var pbBytes = await pbRes.Content.ReadAsByteArrayAsync();
                string pbJson = System.Text.Encoding.UTF8.GetString(pbBytes);
                using var pbDoc = System.Text.Json.JsonDocument.Parse(pbJson);
                
                // 역동적으로 'm3u' 필드 찾기
                string? m3u8Url = null;
                
                // 재귀 없이 루프로 m3u 필드 검색
                void FindM3u(System.Text.Json.JsonElement element)
                {
                    if (m3u8Url != null) return;
                    if (element.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        foreach (var prop in element.EnumerateObject())
                        {
                            if (prop.Name == "m3u" && prop.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                            {
                                m3u8Url = prop.Value.GetString();
                                return;
                            }
                            FindM3u(prop.Value);
                        }
                    }
                    else if (element.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        foreach (var item in element.EnumerateArray()) FindM3u(item);
                    }
                }

                FindM3u(pbDoc.RootElement);

                if (string.IsNullOrEmpty(m3u8Url))
                {
                    throw new Exception("영상의 M3U8 주소를 찾을 수 없습니다.");
                }

                string safeTitle = string.Join("_", title.Split(Path.GetInvalidFileNameChars())) + "_" + clipId;
                return (m3u8Url, safeTitle);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ChzzkResolver] 오류: {ex.Message}");
                throw new Exception($"클립 분석 중 오류 발생: {ex.Message}");
            }
        }

        private async Task<(string Url, string Title)?> ResolveChzzkVideoAsync(string originalUrl)
        {
            Match idMatch = Regex.Match(originalUrl, @"chzzk\.naver\.com/video/(\d+)", RegexOptions.IgnoreCase);
            if (!idMatch.Success) return null;

            string contentId = idMatch.Groups[1].Value;
            try
            {
                using var client = new System.Net.Http.HttpClient();
                client.Timeout = TimeSpan.FromSeconds(15);
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
                client.DefaultRequestHeaders.Add("Referer", "https://chzzk.naver.com/");

                using var infoResponse = await client.GetAsync($"https://api.chzzk.naver.com/service/v3/videos/{contentId}");
                if (!infoResponse.IsSuccessStatusCode) return null;

                byte[] infoBytes = await infoResponse.Content.ReadAsByteArrayAsync();
                using JsonDocument infoDocument = JsonDocument.Parse(infoBytes);
                if (!infoDocument.RootElement.TryGetProperty("content", out JsonElement content) ||
                    content.ValueKind != JsonValueKind.Object)
                {
                    return null;
                }

                string videoId = content.TryGetProperty("videoId", out JsonElement videoIdElement)
                    ? videoIdElement.GetString() ?? string.Empty
                    : string.Empty;
                string inKey = content.TryGetProperty("inKey", out JsonElement inKeyElement)
                    ? inKeyElement.GetString() ?? string.Empty
                    : string.Empty;
                string title = content.TryGetProperty("videoTitle", out JsonElement titleElement)
                    ? titleElement.GetString() ?? $"Chzzk_Video_{contentId}"
                    : $"Chzzk_Video_{contentId}";

                if (string.IsNullOrWhiteSpace(videoId) || string.IsNullOrWhiteSpace(inKey)) return null;

                string playbackUrl = $"https://apis.naver.com/neonplayer/vodplay/v2/playback/{Uri.EscapeDataString(videoId)}?key={Uri.EscapeDataString(inKey)}&deviceType=pc";
                using var playbackResponse = await client.GetAsync(playbackUrl);
                if (!playbackResponse.IsSuccessStatusCode) return null;

                byte[] playbackBytes = await playbackResponse.Content.ReadAsByteArrayAsync();
                using JsonDocument playbackDocument = JsonDocument.Parse(playbackBytes);
                string? m3u8Url = FindBestChzzkVideoM3u8(playbackDocument.RootElement);
                if (string.IsNullOrWhiteSpace(m3u8Url)) return null;

                string safeTitle = GetPreferredFileName(title);
                if (string.IsNullOrWhiteSpace(safeTitle)) safeTitle = $"Chzzk_Video_{contentId}";
                return (m3u8Url, safeTitle);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ChzzkVideoResolver] {ex.Message}");
                return null;
            }
        }

        private static string? FindBestChzzkVideoM3u8(JsonElement root)
        {
            if (!root.TryGetProperty("period", out JsonElement periods) || periods.ValueKind != JsonValueKind.Array)
                return null;

            string? bestUrl = null;
            long bestScore = -1;

            foreach (JsonElement period in periods.EnumerateArray())
            {
                if (!period.TryGetProperty("adaptationSet", out JsonElement adaptationSets) ||
                    adaptationSets.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (JsonElement adaptationSet in adaptationSets.EnumerateArray())
                {
                    string mimeType = adaptationSet.TryGetProperty("mimeType", out JsonElement mimeTypeElement)
                        ? mimeTypeElement.GetString() ?? string.Empty
                        : string.Empty;
                    if (!mimeType.Equals("video/mp2t", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!adaptationSet.TryGetProperty("representation", out JsonElement representations) ||
                        representations.ValueKind != JsonValueKind.Array)
                        continue;

                    foreach (JsonElement representation in representations.EnumerateArray())
                    {
                        if (!representation.TryGetProperty("otherAttributes", out JsonElement attributes) ||
                            !attributes.TryGetProperty("m3u", out JsonElement m3uElement) ||
                            m3uElement.ValueKind != JsonValueKind.String)
                            continue;

                        string? url = m3uElement.GetString();
                        if (string.IsNullOrWhiteSpace(url) || !url.Contains(".m3u8", StringComparison.OrdinalIgnoreCase))
                            continue;

                        long width = GetJsonInt64(representation, "width");
                        long height = GetJsonInt64(representation, "height");
                        long bandwidth = GetJsonInt64(representation, "bandwidth");
                        long score = (width * height * 10_000L) + bandwidth;
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestUrl = url;
                        }
                    }
                }
            }

            return bestUrl;
        }

        private static long GetJsonInt64(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement value)) return 0;

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out long number))
                return number;

            if (value.ValueKind == JsonValueKind.String && long.TryParse(value.GetString(), out number))
                return number;

            return 0;
        }

        private async Task<string?> PrepareSignedChzzkManifestAsync(string manifestUrl, System.Threading.CancellationToken token)
        {
            if (!Uri.TryCreate(manifestUrl, UriKind.Absolute, out Uri? manifestUri) ||
                !manifestUri.Host.EndsWith("pstatic.net", StringComparison.OrdinalIgnoreCase) ||
                !manifestUri.AbsolutePath.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(manifestUri.Query) ||
                !manifestUri.Query.Contains("_lsu_sa_", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            using var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://chzzk.naver.com/");

            using var response = await client.GetAsync(manifestUri, token);
            response.EnsureSuccessStatusCode();
            string manifest = await response.Content.ReadAsStringAsync(token);
            string[] lines = Regex.Split(manifest, "\\r?\\n");

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (line.StartsWith("#", StringComparison.Ordinal))
                {
                    lines[i] = Regex.Replace(
                        line,
                        "URI=\"([^\"]+)\"",
                        match => $"URI=\"{BuildSignedChzzkResourceUrl(manifestUri, match.Groups[1].Value)}\"",
                        RegexOptions.IgnoreCase);
                }
                else
                {
                    lines[i] = BuildSignedChzzkResourceUrl(manifestUri, line.Trim());
                }
            }

            string temporaryPath = Path.Combine(Path.GetTempPath(), $"mmt_chzzk_{Guid.NewGuid():N}.m3u8");
            await File.WriteAllTextAsync(
                temporaryPath,
                string.Join(Environment.NewLine, lines),
                new System.Text.UTF8Encoding(false),
                token);
            return temporaryPath;
        }

        private static string BuildSignedChzzkResourceUrl(Uri manifestUri, string resourceUrl)
        {
            if (!Uri.TryCreate(manifestUri, resourceUrl, out Uri? resolvedUri)) return resourceUrl;
            if (!string.IsNullOrWhiteSpace(resolvedUri.Query)) return resolvedUri.AbsoluteUri;

            var builder = new UriBuilder(resolvedUri)
            {
                Query = manifestUri.Query.TrimStart('?')
            };
            return builder.Uri.AbsoluteUri;
        }

        private async Task<(string Url, string Title)?> ResolveSoopCatchAsync(string originalUrl)
        {
            try
            {
                // URL 형식: https://vod.sooplive.co.kr/player/189489847/catch
                var match = Regex.Match(originalUrl, @"/player/(\d+)");
                if (!match.Success) return null;
                string titleNo = match.Groups[1].Value;

                using var client = new System.Net.Http.HttpClient();
                client.Timeout = TimeSpan.FromSeconds(15);
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                var content = new System.Net.Http.StringContent($"nTitleNo={titleNo}", System.Text.Encoding.UTF8, "application/x-www-form-urlencoded");
                var res = await client.PostAsync("https://api.m.sooplive.co.kr/station/video/a/view", content);
                
                if (!res.IsSuccessStatusCode) return null;

                var bytes = await res.Content.ReadAsByteArrayAsync();
                string json = System.Text.Encoding.UTF8.GetString(bytes);
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                
                var root = doc.RootElement;
                if (!root.TryGetProperty("data", out var data)) return null;
                
                string title = data.TryGetProperty("title", out var t) ? t.GetString() ?? $"Soop_{titleNo}" : $"Soop_{titleNo}";
                
                string? m3u8Url = null;
                if (data.TryGetProperty("files", out var files) && files.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var file in files.EnumerateArray())
                    {
                        if (file.TryGetProperty("file", out var f) && f.ValueKind == System.Text.Json.JsonValueKind.String)
                        {
                            m3u8Url = f.GetString();
                            break;
                        }
                    }
                }
                
                if (string.IsNullOrEmpty(m3u8Url)) return null;

                // 유효하지 않은 파일명 문자 제거 (안전을 위해 정규식 방식도 고려할 수 있으나 기본 방식 채택)
                string safeTitle = string.Join("_", title.Split(Path.GetInvalidFileNameChars()));
                return (m3u8Url, safeTitle);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SoopResolver] 오류: {ex.Message}");
                return null;
            }
        }

        private async Task<(string Url, string Title)?> ResolveNaverTvAsync(string originalUrl)
        {
            try
            {
                using var client = new System.Net.Http.HttpClient();
                client.Timeout = TimeSpan.FromSeconds(15);
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
                client.DefaultRequestHeaders.Add("Referer", "https://tv.naver.com/");

                string html = await client.GetStringAsync(originalUrl);

                // 1. 영상 제목 및 기본 메타데이터 추출
                string rawTitle = "NaverTV_Video";
                var titleMatch = Regex.Match(html, @"<meta property=""og:title"" content=""([^""]+)""");
                if (titleMatch.Success) rawTitle = titleMatch.Groups[1].Value;

                // 파일명으로 사용할 수 있도록 특수문자 제거
                string safeTitle = string.Join("_", rawTitle.Split(Path.GetInvalidFileNameChars()));

                // 2. VideoId 및 InKey 추출 (Naver TV는 다양한 곳에 숨겨둠)
                // 패턴 A: 스크립트 내 nhn.rmcplayer.VodVideoData 형태
                // 패턴 B: __NEXT_DATA__ 또는 rmcPlayer 내부 JSON 형태
                var vidMatch = Regex.Match(html, @"""(?:videoId|mediaId|media_id|vid)""\s*:\s*""([^""]+)""");
                var keyMatch = Regex.Match(html, @"""(?:inKey|inkey|key|token)""\s*:\s*""([^""]+)""");

                string videoId = vidMatch.Success ? vidMatch.Groups[1].Value : "";
                string inKey = keyMatch.Success ? keyMatch.Groups[1].Value : "";

                // 3. 만약 ID/Key를 못 찾았다면, HTML 본문에서 직접 m3u8 주소 검색 (최후의 수단)
                if (string.IsNullOrEmpty(videoId) || string.IsNullOrEmpty(inKey))
                {
                    var m3u8Match = Regex.Match(html, @"""(https://[^""]+?\.m3u8[^""]*)""");
                    if (m3u8Match.Success)
                    {
                        string directUrl = m3u8Match.Groups[1].Value.Replace("\\/", "/");
                        return (directUrl, safeTitle);
                    }
                }

                if (string.IsNullOrEmpty(videoId)) return null;

                // 4. Playback API 호출 (Neon Player 백엔드)
                // inKey가 없는 경우(일부 클립)에는 ID만으로 시도하거나 null 반환
                if (string.IsNullOrEmpty(inKey)) return null; 

                string playbackUrl = $"https://apis.naver.com/neonplayer/vodplay/v2/playback/{videoId}?key={inKey}&deviceType=pc";
                var pbRes = await client.GetAsync(playbackUrl);
                if (!pbRes.IsSuccessStatusCode) return null;

                var pbBytes = await pbRes.Content.ReadAsByteArrayAsync();
                string pbJson = System.Text.Encoding.UTF8.GetString(pbBytes);
                using var pbDoc = System.Text.Json.JsonDocument.Parse(pbJson);
                
                string? m3u8Url = null;
                void FindM3u(System.Text.Json.JsonElement element)
                {
                    if (m3u8Url != null) return;
                    if (element.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        foreach (var prop in element.EnumerateObject())
                        {
                            if (prop.Name == "m3u" && prop.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                            {
                                m3u8Url = prop.Value.GetString();
                                return;
                            }
                            FindM3u(prop.Value);
                        }
                    }
                    else if (element.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        foreach (var item in element.EnumerateArray()) FindM3u(item);
                    }
                }
                FindM3u(pbDoc.RootElement);

                if (string.IsNullOrEmpty(m3u8Url)) return null;

                return (m3u8Url, safeTitle);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NaverTvResolver] 오류: {ex.Message}");
                return null;
            }
        }

        private async Task<(string Url, string Title, string SubtitleUrl)?> ResolveLinkkfAsync(string originalUrl)
        {
            try
            {
                using var client = new System.Net.Http.HttpClient();
                client.Timeout = TimeSpan.FromSeconds(20);
                client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
                client.DefaultRequestHeaders.TryAddWithoutValidation("Referer", GetUrlOrigin(originalUrl));

                string html = await client.GetStringAsync(originalUrl);
                string rawTitle = "Linkkf_Video";
                var titleMatch = Regex.Match(html, @"<title>\s*(.*?)\s*</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (titleMatch.Success)
                {
                    rawTitle = System.Net.WebUtility.HtmlDecode(titleMatch.Groups[1].Value).Trim();
                    int suffixIndex = rawTitle.IndexOf(" - Anime - ", StringComparison.OrdinalIgnoreCase);
                    if (suffixIndex > 0) rawTitle = rawTitle.Substring(0, suffixIndex).Trim();
                }

                string playerUrl = "";
                var actualUrlMatch = Regex.Match(html, @"""actual_url""\s*:\s*""([^""]+)""", RegexOptions.IgnoreCase);
                if (actualUrlMatch.Success)
                {
                    playerUrl = DecodeLinkkfUrl(actualUrlMatch.Groups[1].Value);
                }

                if (string.IsNullOrEmpty(playerUrl))
                {
                    var dataUrlMatch = Regex.Match(html, @"data-url\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase);
                    if (dataUrlMatch.Success) playerUrl = DecodeLinkkfUrl(dataUrlMatch.Groups[1].Value);
                }

                if (string.IsNullOrEmpty(playerUrl))
                {
                    foreach (Match iframeMatch in Regex.Matches(html, @"<iframe\b[^>]*>", RegexOptions.IgnoreCase))
                    {
                        string iframeTag = iframeMatch.Value;
                        if (!iframeTag.Contains("video-player-iframe", StringComparison.OrdinalIgnoreCase) &&
                            !iframeTag.Contains("play.sub", StringComparison.OrdinalIgnoreCase) &&
                            !iframeTag.Contains("playv2.sub", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var srcMatch = Regex.Match(iframeTag, @"\bsrc\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase);
                        if (!srcMatch.Success) continue;
                        string candidateUrl = DecodeLinkkfUrl(srcMatch.Groups[1].Value);
                        if (candidateUrl.Contains("${", StringComparison.Ordinal) ||
                            candidateUrl.Contains("{", StringComparison.Ordinal) ||
                            candidateUrl.Contains("}", StringComparison.Ordinal))
                        {
                            continue;
                        }

                        playerUrl = candidateUrl;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(playerUrl) &&
                    Uri.TryCreate(originalUrl, UriKind.Absolute, out var originalUri) &&
                    originalUri.Host.Equals("kf.carsstore365.com", StringComparison.OrdinalIgnoreCase))
                {
                    playerUrl = await ResolveCarsstoreLinkkfPlayerUrlAsync(client, originalUrl, html);
                }

                if (string.IsNullOrEmpty(playerUrl)) return null;
                if (Uri.TryCreate(new Uri(originalUrl), playerUrl, out var playerUri))
                {
                    playerUrl = playerUri.ToString();
                }

                var playerReq = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, playerUrl);
                playerReq.Headers.TryAddWithoutValidation("Referer", originalUrl);
                playerReq.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
                var playerRes = await client.SendAsync(playerReq);
                if (!playerRes.IsSuccessStatusCode) return null;

                string playerHtml = await playerRes.Content.ReadAsStringAsync();
                var m3u8Match = Regex.Match(playerHtml, @"videoUrl\s*:\s*[""']([^""']+?\.m3u8[^""']*)[""']", RegexOptions.IgnoreCase);
                if (!m3u8Match.Success)
                {
                    m3u8Match = Regex.Match(playerHtml, @"[""']([^""']+?\.m3u8[^""']*)[""']", RegexOptions.IgnoreCase);
                }

                if (!m3u8Match.Success) return null;

                string m3u8Url = DecodeLinkkfUrl(m3u8Match.Groups[1].Value);
                if (Uri.TryCreate(new Uri(playerUrl), m3u8Url, out var m3u8Uri))
                {
                    m3u8Url = m3u8Uri.ToString();
                }

                string subtitleUrl = "";
                var subtitleMatch = Regex.Match(playerHtml, @"""file""\s*:\s*""([^""]+?\.vtt[^""]*)""", RegexOptions.IgnoreCase);
                if (!subtitleMatch.Success)
                {
                    subtitleMatch = Regex.Match(
                        playerHtml,
                        @"subtitle\s*:\s*\{[\s\S]{0,1000}?\burl\s*:\s*[""']([^""']+?\.vtt[^""']*)[""']",
                        RegexOptions.IgnoreCase);
                }
                if (!subtitleMatch.Success)
                {
                    subtitleMatch = Regex.Match(playerHtml, @"[""']([^""']+?\.vtt[^""']*)[""']", RegexOptions.IgnoreCase);
                }
                if (subtitleMatch.Success)
                {
                    subtitleUrl = DecodeLinkkfUrl(subtitleMatch.Groups[1].Value);
                    if (Uri.TryCreate(new Uri(playerUrl), subtitleUrl, out var subtitleUri))
                    {
                        subtitleUrl = subtitleUri.ToString();
                    }
                }

                string safeTitle = string.Join("_", rawTitle.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim();
                if (string.IsNullOrWhiteSpace(safeTitle)) safeTitle = "Linkkf_Video";
                return (m3u8Url, safeTitle, subtitleUrl);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LinkkfResolver] error: {ex.Message}");
                return null;
            }
        }

        private static async Task<string> ResolveCarsstoreLinkkfPlayerUrlAsync(
            System.Net.Http.HttpClient client,
            string originalUrl,
            string html)
        {
            if (!Uri.TryCreate(originalUrl, UriKind.Absolute, out var pageUri)) return "";

            var postIdMatch = Regex.Match(pageUri.AbsolutePath, @"/up/(\d+)(?:/|$)", RegexOptions.IgnoreCase);
            var serverMatch = Regex.Match(pageUri.Query, @"(?:^|[?&])server=([^&]+)", RegexOptions.IgnoreCase);
            var slugMatch = Regex.Match(pageUri.Query, @"(?:^|[?&])slug=([^&]+)", RegexOptions.IgnoreCase);
            if (!postIdMatch.Success || !serverMatch.Success || !slugMatch.Success) return "";

            string postId = postIdMatch.Groups[1].Value;
            string serverId = Uri.UnescapeDataString(serverMatch.Groups[1].Value);
            string slug = Uri.UnescapeDataString(slugMatch.Groups[1].Value);

            var episodeApiMatch = Regex.Match(html, @"\bepApi\s*:\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase);
            var playerApiMatch = Regex.Match(html, @"\bepLinkApi\s*:\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase);
            if (!episodeApiMatch.Success || !playerApiMatch.Success) return "";

            if (!Uri.TryCreate(pageUri, DecodeLinkkfUrl(episodeApiMatch.Groups[1].Value), out var episodeApiBase) ||
                !Uri.TryCreate(pageUri, DecodeLinkkfUrl(playerApiMatch.Groups[1].Value), out var playerApiBase))
            {
                return "";
            }

            string episodeJson = await client.GetStringAsync(episodeApiBase + Uri.EscapeDataString(postId));
            using var episodeDocument = JsonDocument.Parse(episodeJson);
            JsonElement serverArray = episodeDocument.RootElement;
            if (serverArray.ValueKind == JsonValueKind.Object &&
                serverArray.TryGetProperty("data", out var episodeData))
            {
                serverArray = episodeData;
            }
            if (serverArray.ValueKind != JsonValueKind.Array) return "";

            string playerDataId = "";
            foreach (JsonElement serverElement in serverArray.EnumerateArray())
            {
                if (!serverElement.TryGetProperty("id", out var idElement) ||
                    !string.Equals(idElement.ToString(), serverId, StringComparison.OrdinalIgnoreCase) ||
                    !serverElement.TryGetProperty("server_data", out var episodeArray) ||
                    episodeArray.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (JsonElement episodeElement in episodeArray.EnumerateArray())
                {
                    if (!episodeElement.TryGetProperty("slug", out var slugElement) ||
                        !string.Equals(slugElement.ToString(), slug, StringComparison.OrdinalIgnoreCase) ||
                        !episodeElement.TryGetProperty("link", out var linkElement))
                    {
                        continue;
                    }

                    playerDataId = linkElement.GetString() ?? linkElement.ToString();
                    break;
                }

                if (!string.IsNullOrWhiteSpace(playerDataId)) break;
            }

            if (string.IsNullOrWhiteSpace(playerDataId)) return "";

            string playerJson = await client.GetStringAsync(playerApiBase + Uri.EscapeDataString(playerDataId));
            using var playerDocument = JsonDocument.Parse(playerJson);
            JsonElement sourceArray = playerDocument.RootElement;
            if (sourceArray.ValueKind == JsonValueKind.Object &&
                sourceArray.TryGetProperty("data", out var sourceData))
            {
                sourceArray = sourceData;
            }
            if (sourceArray.ValueKind != JsonValueKind.Array) return "";

            foreach (JsonElement sourceElement in sourceArray.EnumerateArray())
            {
                if (!sourceElement.TryGetProperty("link", out var linkElement)) continue;
                string playerUrl = linkElement.GetString() ?? "";
                if (Uri.TryCreate(playerUrl, UriKind.Absolute, out var playerUri) &&
                    (playerUri.Scheme == Uri.UriSchemeHttp || playerUri.Scheme == Uri.UriSchemeHttps))
                {
                    return playerUri.ToString();
                }
            }

            return "";
        }

        private static string DecodeLinkkfUrl(string value)
        {
            return System.Net.WebUtility.HtmlDecode(value)
                .Replace("\\/", "/")
                .Replace("\\u0026", "&")
                .Replace("\\u003d", "=")
                .Trim();
        }

        private async Task<string?> DownloadSubtitleSidecarAsync(string subtitleUrl, string videoFilePath, System.Threading.CancellationToken token)
        {
            try
            {
                string subtitlePath = Path.ChangeExtension(videoFilePath, ".vtt");
                string directory = Path.GetDirectoryName(subtitlePath) ?? "";
                string fileName = Path.GetFileNameWithoutExtension(subtitlePath);
                int counter = 2;

                while (File.Exists(subtitlePath))
                {
                    subtitlePath = Path.Combine(directory, $"{fileName}_{counter++}.vtt");
                }

                using var client = new System.Net.Http.HttpClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
                client.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://playv2.sub3.top/");

                byte[] bytes = await client.GetByteArrayAsync(subtitleUrl, token);
                await File.WriteAllBytesAsync(subtitlePath, bytes, token);
                return subtitlePath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SubtitleDownloader] error: {ex.Message}");
                return null;
            }
        }

        private async Task<bool> TryDownloadYtDlpSubtitlesAsync(string ytDlpPath, string commonArguments, string ffmpegDir, string outputTemplate, string videoUrl, System.Threading.CancellationToken token)
        {
            string subLanguages = string.IsNullOrWhiteSpace(SubtitleLanguages) ? "ko.*,ko,en.*,en" : SubtitleLanguages;
            string arguments = $"{commonArguments}--skip-download --write-subs --write-auto-subs --sub-langs {QuoteProcessArgument(subLanguages)} --convert-subs srt --ffmpeg-location {QuoteProcessArgument(ffmpegDir)} -o {QuoteProcessArgument(outputTemplate)} {QuoteProcessArgument(videoUrl)}";
            string errorOutput = "";
            string standardOutput = "";
            DateTime startedAtUtc = DateTime.UtcNow.AddSeconds(-2);

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = ytDlpPath,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8
            };

            try
            {
                using Process process = new Process();
                process.StartInfo = psi;
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data)) errorOutput += e.Data + Environment.NewLine;
                };
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data)) standardOutput += e.Data + Environment.NewLine;
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                using (token.Register(() => { try { if (!process.HasExited) process.Kill(true); } catch { } }))
                {
                    await process.WaitForExitAsync();
                }

                token.ThrowIfCancellationRequested();
                if (process.ExitCode != 0)
                {
                    Debug.WriteLine($"[YtDlpSubtitle] skipped: {errorOutput}");
                    return false;
                }

                return SubtitleOutputIndicatesDownload(standardOutput + Environment.NewLine + errorOutput)
                    || HasNewSubtitleFile(outputTemplate, startedAtUtc);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[YtDlpSubtitle] error: {ex.Message}");
                return false;
            }
        }

        private static bool SubtitleOutputIndicatesDownload(string output)
        {
            if (string.IsNullOrWhiteSpace(output)) return false;
            return output.Contains(".srt", StringComparison.OrdinalIgnoreCase)
                || output.Contains(".vtt", StringComparison.OrdinalIgnoreCase)
                || output.Contains("Writing video subtitles to:", StringComparison.OrdinalIgnoreCase)
                || output.Contains("Writing video automatic captions to:", StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasNewSubtitleFile(string outputTemplate, DateTime startedAtUtc)
        {
            try
            {
                string directory = Path.GetDirectoryName(outputTemplate) ?? "";
                if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory)) return false;

                foreach (string file in Directory.EnumerateFiles(directory))
                {
                    string ext = Path.GetExtension(file);
                    if (!ext.Equals(".srt", StringComparison.OrdinalIgnoreCase)
                        && !ext.Equals(".vtt", StringComparison.OrdinalIgnoreCase)
                        && !ext.Equals(".ass", StringComparison.OrdinalIgnoreCase)
                        && !ext.Equals(".ssa", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (File.GetLastWriteTimeUtc(file) >= startedAtUtc) return true;
                }
            }
            catch { }

            return false;
        }

        private async Task<(string Url, string Title)?> ResolveAnilifAsync(string originalUrl)
        {
            try
            {
                string videoId = "";
                var uri = new Uri(originalUrl);
                string queryStr = uri.Query.TrimStart('?');
                foreach (var param in queryStr.Split('&'))
                {
                    var parts = param.Split('=', 2);
                    if (parts.Length == 2 && parts[0] == "id")
                    {
                        videoId = Uri.UnescapeDataString(parts[1]);
                        break;
                    }
                }

                if (string.IsNullOrEmpty(videoId))
                {
                    var idMatch = Regex.Match(originalUrl, @"[?&]id=([a-f0-9\-]+)", RegexOptions.IgnoreCase);
                    if (idMatch.Success) videoId = idMatch.Groups[1].Value;
                }

                if (string.IsNullOrEmpty(videoId))
                    throw new Exception("영상 ID를 URL에서 찾을 수 없습니다.");

                string rawTitle = $"Anilife_{videoId.Substring(0, 8)}";
                string buildId = "4d1c0cd0-5716-4854-8bfc-9824ade8986f";

                var handler = new System.Net.Http.HttpClientHandler { UseCookies = true };
                using var client = new System.Net.Http.HttpClient(handler);
                client.Timeout = TimeSpan.FromSeconds(20);
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
                client.DefaultRequestHeaders.Add("Referer", "https://anilife.app/");
                client.DefaultRequestHeaders.Add("Origin", "https://anilife.app");

                try
                {
                    string pageHtml = await client.GetStringAsync(originalUrl);
                    var titleMatch = Regex.Match(pageHtml, @"<meta\s+property=""og:title""\s+content=""([^""]+)""", RegexOptions.IgnoreCase);
                    if (!titleMatch.Success)
                        titleMatch = Regex.Match(pageHtml, @"<title>([^<]+)</title>", RegexOptions.IgnoreCase);
                    if (titleMatch.Success)
                    {
                        rawTitle = titleMatch.Groups[1].Value
                            .Replace(" | 애니라이프", "")
                            .Replace(" | Anilife", "")
                            .Trim();
                        var buildMatch = Regex.Match(pageHtml, @"buildVersion:""([^""]+)""|""buildVersion"":""([^""]+)""", RegexOptions.IgnoreCase);
                        if (buildMatch.Success)
                        {
                            buildId = !string.IsNullOrEmpty(buildMatch.Groups[1].Value) ? buildMatch.Groups[1].Value : buildMatch.Groups[2].Value;
                        }
                    }
                }
                catch { }

                var directResolved = await TryResolveAnilifeManifestAsync(client, originalUrl, videoId, rawTitle, buildId);
                if (directResolved != null) return directResolved;

                // 1. CSRF 토큰 가져오기
                string csrfToken = "";
                try
                {
                    var tokenReq = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, "https://api.anilife.app/v1/csrf/token");
                    tokenReq.Headers.Add("Accept", "application/json");
                    var tokenRes = await client.SendAsync(tokenReq);
                    if (tokenRes.IsSuccessStatusCode)
                    {
                        var tokenJson = await tokenRes.Content.ReadAsStringAsync();
                        using var doc = System.Text.Json.JsonDocument.Parse(tokenJson);
                        if (doc.RootElement.TryGetProperty("csrfToken", out var cToken)) csrfToken = cToken.GetString() ?? "";
                        else if (doc.RootElement.TryGetProperty("token", out var tToken)) csrfToken = tToken.GetString() ?? "";
                    }
                }
                catch { }

                // 2. 미디어 정보 가져오기 (/v1/media/{id} 또는 /v1/video/{id})
                string[] apiEndpoints = new[]
                {
                    $"https://api.anilife.app/v1/media/{videoId}",
                    $"https://api.gcdn.app/v1/media/{videoId}",
                    $"https://api.anilife.app/v1/video/{videoId}"
                };

                string? m3u8Url = null;

                foreach (string apiUrl in apiEndpoints)
                {
                    try
                    {
                        var req = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, apiUrl);
                        req.Headers.Add("Accept", "application/json");
                        if (!string.IsNullOrEmpty(csrfToken))
                        {
                            req.Headers.Add("x-csrf-token", csrfToken);
                        }

                        var response = await client.SendAsync(req);
                        if (!response.IsSuccessStatusCode) continue;

                        var bytes = await response.Content.ReadAsByteArrayAsync();
                        string json = System.Text.Encoding.UTF8.GetString(bytes);
                        using var doc = System.Text.Json.JsonDocument.Parse(json);

                        void FindM3u8(System.Text.Json.JsonElement element)
                        {
                            if (m3u8Url != null) return;
                            if (element.ValueKind == System.Text.Json.JsonValueKind.Object)
                            {
                                foreach (var prop in element.EnumerateObject())
                                {
                                    if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                                    {
                                        string val = prop.Value.GetString() ?? "";
                                        if (val.Contains(".m3u8"))
                                        {
                                            m3u8Url = val;
                                            return;
                                        }
                                    }
                                    FindM3u8(prop.Value);
                                }
                            }
                            else if (element.ValueKind == System.Text.Json.JsonValueKind.Array)
                            {
                                foreach (var item in element.EnumerateArray()) FindM3u8(item);
                            }
                        }

                        FindM3u8(doc.RootElement);
                        if (!string.IsNullOrEmpty(m3u8Url)) break;
                    }
                    catch { }
                }

                if (string.IsNullOrEmpty(m3u8Url))
                {
                    throw new Exception("영상 스트림 URL을 API에서 추출할 수 없습니다.");
                }

                string safeTitle = string.Join("_", rawTitle.Split(Path.GetInvalidFileNameChars()));
                return (m3u8Url, safeTitle);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AnilifResolver] 오류: {ex.Message}");
                return null;
            }
        }

        private async Task<(string Url, string Title)?> TryResolveAnilifeManifestAsync(System.Net.Http.HttpClient client, string originalUrl, string videoId, string rawTitle, string buildId)
        {
            try
            {
                string csrfToken = "";
                string csrfHeaderName = "x-csrf-token";

                var tokenReq = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, "https://api.anilife.app/v1/csrf/token");
                tokenReq.Headers.TryAddWithoutValidation("Accept", "application/json");
                tokenReq.Headers.TryAddWithoutValidation("Content-Type", "application/json");
                var tokenRes = await client.SendAsync(tokenReq);
                if (!tokenRes.IsSuccessStatusCode) return null;

                string tokenJson = await tokenRes.Content.ReadAsStringAsync();
                using (var tokenDoc = JsonDocument.Parse(tokenJson))
                {
                    if (tokenDoc.RootElement.TryGetProperty("token", out var tokenValue)) csrfToken = tokenValue.GetString() ?? "";
                    if (tokenDoc.RootElement.TryGetProperty("headerName", out var headerValue)) csrfHeaderName = headerValue.GetString() ?? csrfHeaderName;
                }

                if (string.IsNullOrEmpty(csrfToken)) return null;

                var mediaReq = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, $"https://api.anilife.app/v1/media/{videoId}");
                mediaReq.Headers.TryAddWithoutValidation("Accept", "application/json");
                mediaReq.Headers.TryAddWithoutValidation("Content-Type", "application/json");
                mediaReq.Headers.TryAddWithoutValidation("x-client-id", "web");
                mediaReq.Headers.TryAddWithoutValidation("x-build-id", buildId);
                mediaReq.Headers.TryAddWithoutValidation("x-anilife-referer", Uri.EscapeDataString(new Uri(originalUrl).PathAndQuery));
                mediaReq.Headers.TryAddWithoutValidation("x-device-token", CreateAnilifeDeviceToken());
                mediaReq.Headers.TryAddWithoutValidation(csrfHeaderName, csrfToken);

                var mediaRes = await client.SendAsync(mediaReq);
                if (!mediaRes.IsSuccessStatusCode) return null;

                byte[] responseBytes = await mediaRes.Content.ReadAsByteArrayAsync();
                string compressed = System.Text.Encoding.UTF8.GetString(responseBytes);
                string zipsonPayload = DecompressAnilifeUtf16(compressed);
                var parts = ExtractAnilifeZipsonStrings(zipsonPayload);
                if (parts.Count < 3) return null;

                string json = DecryptAnilifePayload(parts[0], parts[1], parts[2]);
                using var mediaDoc = JsonDocument.Parse(json);
                string access = GetNestedJsonString(mediaDoc.RootElement, "access");
                if (string.IsNullOrEmpty(access)) return null;

                string mediaTitle = GetNestedJsonString(mediaDoc.RootElement, "media", "name", "kr");
                string episodeNum = GetNestedJsonString(mediaDoc.RootElement, "episode", "episode_num");
                string subject = GetNestedJsonString(mediaDoc.RootElement, "episode", "subject");

                string title = string.IsNullOrWhiteSpace(mediaTitle) ? rawTitle : mediaTitle;
                if (!string.IsNullOrWhiteSpace(episodeNum)) title += $"_EP{episodeNum}";
                if (!string.IsNullOrWhiteSpace(subject)) title += $"_{subject}";
                title = string.Join("_", title.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim();
                if (string.IsNullOrWhiteSpace(title)) title = $"Anilife_{videoId.Substring(0, 8)}";

                return ($"https://api.gcdn.app/v1/manifest/a/{access}/master.m3u8", title);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AnilifeDirectResolver] error: {ex.Message}");
                return null;
            }
        }

        private static List<string> ExtractAnilifeZipsonStrings(string zipsonPayload)
        {
            var result = new List<string>();
            foreach (Match match in Regex.Matches(zipsonPayload, "\u00a8([^\u00a8]+)\u00a8"))
            {
                result.Add(match.Groups[1].Value);
            }
            return result;
        }

        internal static string TryResolveAnilifeManifestFromMediaResponse(string responseBody)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(responseBody) || responseBody.Length > 4_000_000) return "";

                string zipsonPayload = DecompressAnilifeUtf16(responseBody);
                var parts = ExtractAnilifeZipsonStrings(zipsonPayload);
                if (parts.Count < 3) return "";

                string json = DecryptAnilifePayload(parts[0], parts[1], parts[2]);
                using var mediaDoc = JsonDocument.Parse(json);
                string access = GetNestedJsonString(mediaDoc.RootElement, "access");
                return string.IsNullOrWhiteSpace(access)
                    ? ""
                    : $"https://api.gcdn.app/v1/manifest/a/{access}/master.m3u8";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AnilifeWebViewResolver] response parse failed: {ex.Message}");
                return "";
            }
        }

        private static string DecryptAnilifePayload(string cipherTextBase64, string ivBase64, string tagBase64)
        {
            byte[] key = HexToBytes("5f1d6b5cf2b6e5a236aa6352c5e688bc86c257a9b61b183c9926613a90356a48");
            byte[] cipherText = Convert.FromBase64String(cipherTextBase64);
            byte[] iv = Convert.FromBase64String(ivBase64);
            byte[] tag = Convert.FromBase64String(tagBase64);
            byte[] plainText = new byte[cipherText.Length];

            if (iv.Length == 12)
            {
                using var aes = new AesGcm(key, tag.Length);
                aes.Decrypt(iv, cipherText, tag, plainText);
            }
            else
            {
                plainText = AesGcmDecryptWithVariableNonce(key, iv, cipherText, tag);
            }

            return System.Text.Encoding.UTF8.GetString(plainText);
        }

        private static byte[] AesGcmDecryptWithVariableNonce(byte[] key, byte[] iv, byte[] cipherText, byte[] tag)
        {
            byte[] hashSubkey = AesEncryptBlock(key, new byte[16]);
            byte[] j0 = BuildGcmInitialCounter(hashSubkey, iv);
            byte[] authTag = AesEncryptBlock(key, j0);
            byte[] authHash = GHash(hashSubkey, Array.Empty<byte>(), cipherText);

            for (int i = 0; i < authTag.Length; i++)
            {
                authTag[i] ^= authHash[i];
            }

            if (!CryptographicOperations.FixedTimeEquals(authTag.AsSpan(0, tag.Length), tag))
            {
                throw new CryptographicException("애니라이프 영상 정보 인증 태그가 일치하지 않습니다.");
            }

            byte[] counter = (byte[])j0.Clone();
            IncrementGcmCounter(counter);
            return Gctr(key, counter, cipherText);
        }

        private static byte[] BuildGcmInitialCounter(byte[] hashSubkey, byte[] iv)
        {
            if (iv.Length == 12)
            {
                byte[] j0 = new byte[16];
                Buffer.BlockCopy(iv, 0, j0, 0, iv.Length);
                j0[15] = 1;
                return j0;
            }

            byte[] y = new byte[16];
            GHashUpdate(hashSubkey, y, iv);

            byte[] lengthBlock = new byte[16];
            WriteUInt64BigEndian(lengthBlock, 8, (ulong)iv.Length * 8);
            GHashBlock(hashSubkey, y, lengthBlock);
            return y;
        }

        private static byte[] GHash(byte[] hashSubkey, byte[] associatedData, byte[] cipherText)
        {
            byte[] y = new byte[16];
            GHashUpdate(hashSubkey, y, associatedData);
            GHashUpdate(hashSubkey, y, cipherText);

            byte[] lengthBlock = new byte[16];
            WriteUInt64BigEndian(lengthBlock, 0, (ulong)associatedData.Length * 8);
            WriteUInt64BigEndian(lengthBlock, 8, (ulong)cipherText.Length * 8);
            GHashBlock(hashSubkey, y, lengthBlock);
            return y;
        }

        private static void GHashUpdate(byte[] hashSubkey, byte[] y, byte[] data)
        {
            int offset = 0;
            while (offset + 16 <= data.Length)
            {
                byte[] block = new byte[16];
                Buffer.BlockCopy(data, offset, block, 0, 16);
                GHashBlock(hashSubkey, y, block);
                offset += 16;
            }

            if (offset < data.Length)
            {
                byte[] block = new byte[16];
                Buffer.BlockCopy(data, offset, block, 0, data.Length - offset);
                GHashBlock(hashSubkey, y, block);
            }
        }

        private static void GHashBlock(byte[] hashSubkey, byte[] y, byte[] block)
        {
            for (int i = 0; i < 16; i++)
            {
                y[i] ^= block[i];
            }

            byte[] multiplied = GcmMultiply(y, hashSubkey);
            Buffer.BlockCopy(multiplied, 0, y, 0, 16);
        }

        private static byte[] GcmMultiply(byte[] x, byte[] y)
        {
            byte[] z = new byte[16];
            byte[] v = (byte[])y.Clone();

            for (int i = 0; i < 128; i++)
            {
                if ((x[i / 8] & (1 << (7 - (i % 8)))) != 0)
                {
                    for (int j = 0; j < 16; j++) z[j] ^= v[j];
                }

                bool lsb = (v[15] & 1) != 0;
                ShiftRightOne(v);
                if (lsb) v[0] ^= 0xe1;
            }

            return z;
        }

        private static void ShiftRightOne(byte[] block)
        {
            int carry = 0;
            for (int i = 0; i < block.Length; i++)
            {
                int nextCarry = block[i] & 1;
                block[i] = (byte)((block[i] >> 1) | (carry << 7));
                carry = nextCarry;
            }
        }

        private static byte[] Gctr(byte[] key, byte[] counter, byte[] input)
        {
            byte[] output = new byte[input.Length];
            for (int offset = 0; offset < input.Length; offset += 16)
            {
                byte[] streamBlock = AesEncryptBlock(key, counter);
                int count = Math.Min(16, input.Length - offset);
                for (int i = 0; i < count; i++)
                {
                    output[offset + i] = (byte)(input[offset + i] ^ streamBlock[i]);
                }
                IncrementGcmCounter(counter);
            }
            return output;
        }

        private static byte[] AesEncryptBlock(byte[] key, byte[] block)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.None;
            using var encryptor = aes.CreateEncryptor();
            return encryptor.TransformFinalBlock(block, 0, 16);
        }

        private static void IncrementGcmCounter(byte[] counter)
        {
            for (int i = 15; i >= 12; i--)
            {
                counter[i]++;
                if (counter[i] != 0) break;
            }
        }

        private static void WriteUInt64BigEndian(byte[] buffer, int offset, ulong value)
        {
            for (int i = 7; i >= 0; i--)
            {
                buffer[offset + i] = (byte)value;
                value >>= 8;
            }
        }

        private static byte[] HexToBytes(string hex)
        {
            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }

        private static string GetNestedJsonString(JsonElement element, params string[] path)
        {
            JsonElement current = element;
            foreach (string name in path)
            {
                if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(name, out current)) return "";
            }
            return current.ValueKind == JsonValueKind.String ? current.GetString() ?? "" : current.ToString();
        }

        private static string DecompressAnilifeUtf16(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            return LzStringDecompress(input.Length, 16384, index => input[index] - 32) ?? "";
        }

        private static string? LzStringDecompress(int length, int resetValue, Func<int, int> getNextValue)
        {
            var dictionary = new Dictionary<int, string>();
            int enlargeIn = 4;
            int dictSize = 4;
            int numBits = 3;
            string entry;
            var result = new List<string>();
            int w;
            int c;
            int dataValue = getNextValue(0);
            int dataPosition = resetValue;
            int dataIndex = 1;

            int ReadBits(int maxPower)
            {
                int value = 0;
                int power = 1;
                while (power != maxPower)
                {
                    int resb = dataValue & dataPosition;
                    dataPosition >>= 1;
                    if (dataPosition == 0)
                    {
                        dataPosition = resetValue;
                        dataValue = dataIndex < length ? getNextValue(dataIndex++) : 0;
                    }
                    if (resb > 0) value |= power;
                    power <<= 1;
                }
                return value;
            }

            for (int i = 0; i < 3; i++) dictionary[i] = ((char)i).ToString();

            int next = ReadBits(4);
            switch (next)
            {
                case 0:
                    c = ReadBits(256);
                    break;
                case 1:
                    c = ReadBits(65536);
                    break;
                case 2:
                    return "";
                default:
                    return null;
            }

            dictionary[3] = ((char)c).ToString();
            w = 3;
            result.Add(dictionary[3]);

            while (true)
            {
                if (dataIndex > length) return "";

                c = ReadBits(1 << numBits);
                switch (c)
                {
                    case 0:
                        dictionary[dictSize++] = ((char)ReadBits(256)).ToString();
                        c = dictSize - 1;
                        enlargeIn--;
                        break;
                    case 1:
                        dictionary[dictSize++] = ((char)ReadBits(65536)).ToString();
                        c = dictSize - 1;
                        enlargeIn--;
                        break;
                    case 2:
                        return string.Concat(result);
                }

                if (enlargeIn == 0)
                {
                    enlargeIn = 1 << numBits;
                    numBits++;
                }

                if (dictionary.TryGetValue(c, out string? value))
                {
                    entry = value;
                }
                else if (c == dictSize)
                {
                    entry = dictionary[w] + dictionary[w][0];
                }
                else
                {
                    return null;
                }

                result.Add(entry);
                dictionary[dictSize++] = dictionary[w] + entry[0];
                enlargeIn--;
                w = c;

                if (enlargeIn == 0)
                {
                    enlargeIn = 1 << numBits;
                    numBits++;
                }
            }
        }

        private static bool IsAnilifeManifestUrl(string url)
        {
            return url.Contains("api.gcdn.app/v1/manifest/a/", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLinkkfWatchUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
            bool supportedHost = uri.Host.Equals("linkkf.drewpx.xyz", StringComparison.OrdinalIgnoreCase) ||
                                 uri.Host.Equals("linkkf.tckopke.com", StringComparison.OrdinalIgnoreCase) ||
                                 uri.Host.Equals("kf.carsstore365.com", StringComparison.OrdinalIgnoreCase);
            return supportedHost && uri.AbsolutePath.Contains("/watch/", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLinkkfStreamUrl(string url)
        {
            return url.Contains("playv2.sub3.top/", StringComparison.OrdinalIgnoreCase)
                || url.Contains("play.sub2.top/", StringComparison.OrdinalIgnoreCase)
                || url.Contains("play.sub3.top/", StringComparison.OrdinalIgnoreCase)
                || url.Contains("hlz3.top/", StringComparison.OrdinalIgnoreCase)
                || url.Contains("imgkr1.top/", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetUrlOrigin(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uri)
                ? $"{uri.Scheme}://{uri.Authority}/"
                : "";
        }

        private static string GetLinkkfStreamOrigin(string videoUrl)
        {
            if (!Uri.TryCreate(videoUrl, UriKind.Absolute, out var uri)) return "https://playv2.sub3.top";
            if (uri.Host.EndsWith("hlz3.top", StringComparison.OrdinalIgnoreCase)) return "https://play.sub3.top";
            if (uri.Host.Equals("play.sub3.top", StringComparison.OrdinalIgnoreCase)) return "https://play.sub3.top";
            if (uri.Host.Equals("play.sub2.top", StringComparison.OrdinalIgnoreCase)) return "https://play.sub2.top";
            return "https://playv2.sub3.top";
        }

        private static string CreateAnilifeDeviceToken()
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(16);
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private string GetPreferredFileName(string fallback)
        {
            string source = string.IsNullOrWhiteSpace(PreferredTitle) ? fallback : PreferredTitle;
            if (string.IsNullOrWhiteSpace(source)) return "";

            string safeName = string.Join("_", source.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries))
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Trim();

            while (safeName.Contains("  ", StringComparison.Ordinal))
            {
                safeName = safeName.Replace("  ", " ");
            }

            if (safeName.Length > 140) safeName = safeName[..140].Trim();
            return safeName;
        }

        private static string GetUniqueMp4Path(string saveDirectory, string customFileName)
        {
            string safeName = string.IsNullOrWhiteSpace(customFileName)
                ? $"Video_{DateTime.Now:yyyyMMdd_HHmmss}"
                : string.Join("_", customFileName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim();

            if (string.IsNullOrWhiteSpace(safeName)) safeName = $"Video_{DateTime.Now:yyyyMMdd_HHmmss}";

            string outputPath = Path.Combine(saveDirectory, $"{safeName}.mp4");
            int counter = 2;
            while (File.Exists(outputPath))
            {
                outputPath = Path.Combine(saveDirectory, $"{safeName}_{counter++}.mp4");
            }
            return outputPath;
        }

        private static List<string> ExtractHlsMediaUrls(string manifestUrl, string manifest, bool variantsOnly)
        {
            var result = new List<string>();
            var baseUri = new Uri(manifestUrl);
            string[] lines = manifest.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

                bool isVariant = line.Contains(".m3u8", StringComparison.OrdinalIgnoreCase);
                if (variantsOnly != isVariant) continue;

                if (Uri.TryCreate(baseUri, line, out var absoluteUri))
                {
                    result.Add(absoluteUri.ToString());
                }
            }

            return result;
        }

        private async Task<string> DownloadAnilifeSegmentsAsync(string manifestUrl, string saveDirectory, System.Threading.CancellationToken token, string customFileName = "")
        {
            string outputFilePath = GetUniqueMp4Path(saveDirectory, customFileName);
            string tempTsPath = Path.Combine(Path.GetTempPath(), $"anilife_{Guid.NewGuid():N}.ts");
            string ffmpegPath = SettingsManager.GetFFmpegPath();

            using var client = new System.Net.Http.HttpClient();
            client.Timeout = TimeSpan.FromSeconds(60);
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://anilife.app/");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Origin", "https://anilife.app");

            try
            {
                OnProgressChanged?.Invoke(1);
                string manifest = await GetAnilifeManifestWithSingleRetryAsync(client, manifestUrl, token);
                bool manifestDeclaresEncryption = Regex.IsMatch(manifest, @"#EXT-X-KEY:\s*METHOD=(?!NONE)", RegexOptions.IgnoreCase);
                var segmentUrls = ExtractHlsMediaUrls(manifestUrl, manifest, variantsOnly: false);

                if (segmentUrls.Count == 0)
                {
                    var variantUrls = ExtractHlsMediaUrls(manifestUrl, manifest, variantsOnly: true);
                    if (variantUrls.Count > 0)
                    {
                        manifestUrl = variantUrls[^1];
                        manifest = await GetAnilifeManifestWithSingleRetryAsync(client, manifestUrl, token);
                        manifestDeclaresEncryption = Regex.IsMatch(manifest, @"#EXT-X-KEY:\s*METHOD=(?!NONE)", RegexOptions.IgnoreCase);
                        segmentUrls = ExtractHlsMediaUrls(manifestUrl, manifest, variantsOnly: false);
                    }
                }

                if (segmentUrls.Count == 0)
                {
                    throw new Exception("애니라이프 HLS 세그먼트를 찾을 수 없습니다.");
                }

                if (manifestDeclaresEncryption && !await IsPlainAnilifeMediaSegmentAsync(client, segmentUrls[0], token))
                {
                    var anilifeHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Referer"] = "https://anilife.app/",
                        ["Origin"] = "https://anilife.app"
                    };

                    try
                    {
                        return await DownloadM3u8WithFFmpegAsync(
                            manifestUrl,
                            saveDirectory,
                            "none",
                            token,
                            anilifeHeaders,
                            customFileName);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(
                            "애니라이프 영상 조각이 실제로 암호화되어 있고 영상 키 요청도 차단되었습니다.\n" +
                            "사이트 재생 방식에 맞춘 추가 업데이트가 필요합니다.",
                            ex);
                    }
                }

                await using (var output = new FileStream(tempTsPath, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024, useAsync: true))
                {
                    for (int i = 0; i < segmentUrls.Count; i++)
                    {
                        token.ThrowIfCancellationRequested();

                        using var response = await client.GetAsync(segmentUrls[i], System.Net.Http.HttpCompletionOption.ResponseHeadersRead, token);
                        if (!response.IsSuccessStatusCode)
                        {
                            throw new Exception($"애니라이프 세그먼트 다운로드 실패 ({(int)response.StatusCode}).");
                        }

                        await using var input = await response.Content.ReadAsStreamAsync(token);
                        await input.CopyToAsync(output, token);

                        double progress = 1 + ((i + 1) * 94.0 / segmentUrls.Count);
                        OnProgressChanged?.Invoke(Math.Min(95, progress));
                    }
                }

                OnProgressChanged?.Invoke(96);

                string arguments = $"-i \"{tempTsPath}\" -c copy \"{outputFilePath}\" -y";
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardErrorEncoding = System.Text.Encoding.UTF8
                };

                using var process = new Process { StartInfo = psi };
                string errorOutput = "";
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data)) errorOutput += e.Data + Environment.NewLine;
                };

                process.Start();
                process.BeginErrorReadLine();

                try
                {
                    await process.WaitForExitAsync(token);
                }
                catch (OperationCanceledException)
                {
                    try { if (!process.HasExited) process.Kill(true); } catch { }
                    CleanupManager.DeleteCanceledDownloadArtifacts(outputFilePath);
                    throw;
                }

                if (process.ExitCode != 0 || !File.Exists(outputFilePath))
                {
                    throw new Exception($"애니라이프 MP4 변환 실패.\n\n[오류 메시지]:\n{errorOutput}");
                }

                OnProgressChanged?.Invoke(100);
                OnDownloadCompleted?.Invoke(outputFilePath);
                return outputFilePath;
            }
            finally
            {
                try
                {
                    if (File.Exists(tempTsPath)) File.Delete(tempTsPath);
                }
                catch { }
            }
        }

        private static async Task<string> GetAnilifeManifestWithSingleRetryAsync(
            System.Net.Http.HttpClient client,
            string manifestUrl,
            System.Threading.CancellationToken token)
        {
            try
            {
                return await client.GetStringAsync(manifestUrl, token);
            }
            catch (System.Net.Http.HttpRequestException)
            {
                await Task.Delay(700, token);
                return await client.GetStringAsync(manifestUrl, token);
            }
        }

        private static async Task<bool> IsPlainAnilifeMediaSegmentAsync(
            System.Net.Http.HttpClient client,
            string segmentUrl,
            System.Threading.CancellationToken token)
        {
            try
            {
                using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, segmentUrl);
                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 375);
                using var response = await client.SendAsync(request, System.Net.Http.HttpCompletionOption.ResponseHeadersRead, token);
                if (!response.IsSuccessStatusCode) return false;

                byte[] header = new byte[376];
                int totalRead = 0;
                await using var stream = await response.Content.ReadAsStreamAsync(token);
                while (totalRead < header.Length)
                {
                    int read = await stream.ReadAsync(header.AsMemory(totalRead, header.Length - totalRead), token);
                    if (read == 0) break;
                    totalRead += read;
                }

                bool isMpegTs = totalRead > 0 && header[0] == 0x47 &&
                                (totalRead <= 188 || header[188] == 0x47);
                if (isMpegTs) return true;

                if (totalRead >= 8)
                {
                    string boxType = System.Text.Encoding.ASCII.GetString(header, 4, 4);
                    return boxType is "ftyp" or "styp" or "moof";
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch { }

            return false;
        }

        private async Task<string> DownloadM3u8WithFFmpegAsync(string videoUrl, string saveDirectory, string browser, System.Threading.CancellationToken token, Dictionary<string, string>? headers, string customFileName = "")
        {
            string ffmpegPath = SettingsManager.GetFFmpegPath();
            string outputBaseName = string.IsNullOrEmpty(customFileName)
                ? $"Video_{DateTime.Now:yyyyMMdd_HHmmss}"
                : customFileName;
            string outputFilePath = GetUniqueMp4Path(saveDirectory, outputBaseName);
            string? temporaryManifestPath = await PrepareSignedChzzkManifestAsync(videoUrl, token);
            string ffmpegInputUrl = string.IsNullOrWhiteSpace(temporaryManifestPath)
                ? videoUrl
                : temporaryManifestPath;

            // 헤더 정보(User-Agent, Referer 등) 조합
            string headerArgs = "";
            string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36";
            
            if (headers != null || videoUrl.Contains("gcdn.app") || videoUrl.Contains("anilife.app") || IsLinkkfStreamUrl(videoUrl) || videoUrl.Contains("sooplive") || videoUrl.Contains("naver") || videoUrl.Contains("pstatic.net"))
            {
                List<string> headerLines = new List<string>();
                if (videoUrl.Contains("gcdn.app") || videoUrl.Contains("anilife.app"))
                {
                    headerLines.Add("Referer: https://anilife.app/");
                    headerLines.Add("Origin: https://anilife.app");
                }
                else if (IsLinkkfStreamUrl(videoUrl))
                {
                    string origin = GetLinkkfStreamOrigin(videoUrl);
                    headerLines.Add($"Referer: {origin}/");
                    headerLines.Add($"Origin: {origin}");
                }
                else if (videoUrl.Contains("sooplive"))
                {
                    headerLines.Add("Referer: https://vod.sooplive.com/");
                    headerLines.Add("Origin: https://vod.sooplive.com");
                }
                else if (videoUrl.Contains("chzzk.naver") || videoUrl.Contains("pstatic.net"))
                {
                    headerLines.Add("Referer: https://chzzk.naver.com/");
                    headerLines.Add("Origin: https://chzzk.naver.com");
                }
                else if (videoUrl.Contains("naver.com"))
                {
                    headerLines.Add("Referer: https://tv.naver.com/");
                    headerLines.Add("Origin: https://tv.naver.com");
                }
                
                if (headers != null)
                {
                    foreach (var h in headers)
                    {
                        if (h.Key.ToLower() == "user-agent") userAgent = h.Value;
                        else if (ShouldSkipCookieHeaderForDirectHls(videoUrl, h.Key)) continue;
                        else headerLines.Add($"{h.Key}: {h.Value}");
                    }
                }

                if (headerLines.Count > 0)
                {
                    string joinedHeaders = string.Join("\r\n", headerLines) + "\r\n";
                    headerArgs = $"-headers \"{joinedHeaders}\" ";
                }
            }

            // 사용자 요청: ffmpeg -i "m3u8파일url" -c copy 영상이름.mp4
            string hlsInputOptions = (videoUrl.Contains("gcdn.app") || videoUrl.Contains("anilife.app") || IsLinkkfStreamUrl(videoUrl) || videoUrl.Contains("sooplive") || videoUrl.Contains("pstatic.net")) ? "-allowed_extensions ALL " : "";
            string localManifestOptions = string.IsNullOrWhiteSpace(temporaryManifestPath)
                ? ""
                : "-protocol_whitelist \"file,http,https,tcp,tls,crypto\" ";
            string networkInputOptions = string.IsNullOrWhiteSpace(temporaryManifestPath)
                ? $"-user_agent \"{userAgent}\" {headerArgs}"
                : "";
            string arguments = $"{networkInputOptions}{hlsInputOptions}{localManifestOptions}-i \"{ffmpegInputUrl}\" -c copy \"{outputFilePath}\" -y";

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8
            };

            try
            {
                using (Process process = new Process())
                {
                    process.StartInfo = psi;
                    string errorOutput = "";

                // ffmpeg progress (가상 프로그레스, m3u8은 전체 길이를 모르면 정확한 퍼센트를 알기 어려움)
                double fakeProgress = 0;
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (string.IsNullOrEmpty(e.Data)) return;
                    errorOutput += e.Data + Environment.NewLine;
                    
                    if (e.Data.Contains("frame=") || e.Data.Contains("time="))
                    {
                        // 임시 프로그레스 효과 (최대 99%까지)
                        fakeProgress += 0.5;
                        if (fakeProgress > 99) fakeProgress = 99;
                        OnProgressChanged?.Invoke(fakeProgress);
                    }
                };

                try
                {
                    process.Start();
                    process.BeginErrorReadLine();

                    using (token.Register(() => { try { if (!process.HasExited) process.Kill(true); } catch { } }))
                    {
                        await process.WaitForExitAsync();
                    }

                    if (process.ExitCode == 0 && File.Exists(outputFilePath))
                    {
                        OnProgressChanged?.Invoke(100);
                        OnDownloadCompleted?.Invoke(outputFilePath);
                        return outputFilePath;
                    }
                    else
                    {
                        token.ThrowIfCancellationRequested();
                        throw new Exception($"FFmpeg 다운로드 실패.\n\n[오류 메시지]:\n{errorOutput}");
                    }
                }
                catch (OperationCanceledException)
                {
                    CleanupManager.DeleteCanceledDownloadArtifacts(outputFilePath);
                    throw;
                }
            }
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(temporaryManifestPath))
                {
                    try { File.Delete(temporaryManifestPath); } catch { }
                }
            }
        }

        private static bool ShouldSkipCookieHeaderForDirectHls(string videoUrl, string headerName)
        {
            if (!headerName.Equals("Cookie", StringComparison.OrdinalIgnoreCase)) return false;

            return videoUrl.Contains("vod-normal-kr-cdn", StringComparison.OrdinalIgnoreCase)
                || videoUrl.Contains("sooplive.com", StringComparison.OrdinalIgnoreCase)
                || videoUrl.Contains("pstatic.net", StringComparison.OrdinalIgnoreCase);
        }
    }
}
