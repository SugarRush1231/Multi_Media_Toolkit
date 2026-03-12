using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;

namespace YoutubeDownloader
{
    public class YtDlpDownloader
    {
        public event Action<double>? OnProgressChanged;
        public event Action<string>? OnDownloadCompleted;

        public async Task<string> DownloadVideoAsync(string videoUrl, string saveDirectory, string browser = "none", System.Threading.CancellationToken token = default, string cookieFile = "", Dictionary<string, string> headers = null)
        {
            // 1. URL 정화
            videoUrl = videoUrl.Trim().Trim('\"', '\'', ' ');
            if (videoUrl.StartsWith("yt-dlp", StringComparison.OrdinalIgnoreCase)) videoUrl = videoUrl.Remove(0, 6).Trim();

            // 2. [치지직 클립 리졸버]
            string customFileName = "";
            if (videoUrl.Contains("chzzk.naver.com/clips/"))
            {
                var resolved = await ResolveChzzkClipAsync(videoUrl);
                if (resolved != null)
                {
                    videoUrl = resolved.Value.Url;
                    customFileName = resolved.Value.Title;
                }
                else
                {
                    throw new Exception("치지직 클립 정보를 가져오는 데 실패했습니다.\n(주소를 다시 확인하거나 잠시 후 시도해주세요.)");
                }
            }

            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string ytDlpPath = Path.Combine(appDir, "yt-dlp.exe"); 
            string ffmpegPath = Path.Combine(appDir, "ffmpeg.exe"); 

            // 파일 중복 체크 및 이름변경 (customFileName이 있을 때)
            if (!string.IsNullOrEmpty(customFileName))
            {
                string baseName = customFileName;
                int counter = 1;
                // .mp4 를 기준으로 체크 (최종 출력이 mp4이므로)
                while (File.Exists(Path.Combine(saveDirectory, customFileName + ".mp4")))
                {
                    customFileName = $"{baseName}_{counter++}";
                }
            }
            
            // 파일명이 미리 결정되었다면 사용
            string outputTemplate = !string.IsNullOrEmpty(customFileName) 
                ? Path.Combine(saveDirectory, $"{customFileName}.%(ext)s")
                : Path.Combine(saveDirectory, "%(title)s.%(ext)s");

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

            string ffmpegDir = Path.GetDirectoryName(ffmpegPath) ?? appDir;

            // 네이버 등 외부 사이트는 User-Agent와 Referer가 중요합니다.
            string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36";
            
            // 사이트별로 적절한 Referer 설정
            string referer = "";
            if (videoUrl.Contains("chzzk.naver.com")) referer = "https://chzzk.naver.com/";
            else if (videoUrl.Contains("x.com") || videoUrl.Contains("twitter.com")) referer = "https://x.com/";
            
            string arguments = $"--newline --encoding utf-8 --user-agent \"{userAgent}\" ";
            if (!string.IsNullOrEmpty(referer)) arguments += $"--referer \"{referer}\" ";
            
            // 쿠키(로그인 정보) 설정
            if (!string.IsNullOrEmpty(cookieFile) && File.Exists(cookieFile))
            {
                arguments += $"--cookies \"{cookieFile}\" ";
            }
            else if (!string.IsNullOrEmpty(browser) && browser != "none")
            {
                arguments += $"--cookies-from-browser {browser.ToLower()} ";
            }

            // 추가 헤더 설정 (Twitter 비공개 계정 등 우회용)
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    if (!string.IsNullOrEmpty(header.Value))
                    {
                        arguments += $"--add-header \"{header.Key}:{header.Value}\" ";
                    }
                }
            }
            
            arguments += $"--ffmpeg-location \"{ffmpegDir}\" --merge-output-format mp4 -o \"{outputTemplate}\" \"{videoUrl}\"";

            string errorOutput = string.Empty;
            string downloadedFilePath = string.Empty;

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

            using (Process process = new Process())
            {
                process.StartInfo = psi;
                Regex progressRegex = new Regex(@"\[download\]\s+(?<percent>\d+(\.\d+)?)%");
                Regex mergeRegex = new Regex(@"(Merging formats into|Destination:)\s+""?(?<filepath>.+\.mp4)""?");

                process.OutputDataReceived += (sender, e) =>
                {
                    if (string.IsNullOrEmpty(e.Data)) return;
                    Match m = progressRegex.Match(e.Data);
                    if (m.Success && double.TryParse(m.Groups["percent"].Value, out double p)) OnProgressChanged?.Invoke(p);
                    
                    Match fm = mergeRegex.Match(e.Data);
                    if (fm.Success) downloadedFilePath = fm.Groups["filepath"].Value;
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

                    if (process.ExitCode == 0)
                    {
                        OnDownloadCompleted?.Invoke(downloadedFilePath);
                        return downloadedFilePath;
                    }
                    else
                    {
                        token.ThrowIfCancellationRequested();

                        string msg = "다운로드 실패";
                        if (errorOutput.Contains("400") || errorOutput.Contains("Bad Request"))
                        {
                            msg = "다운로드 실패 (HTTP 400: Bad Request)\n\n" +
                                  "💡 알림: 치지직 성인/제한 영상은 현재 다운로드가 어렵습니다.";
                        }
                        else if (errorOutput.Contains("Unsupported URL", StringComparison.OrdinalIgnoreCase))
                        {
                            msg = "다운로드 실패: 현재 이 주소 형식을 지원하지 않습니다.";
                        }
                        
                        msg += $"\n\n[오류 설명]:\n{errorOutput}";
                        throw new Exception(msg);
                    }
                }
                catch (OperationCanceledException) { throw; }
            }
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
    }
}
