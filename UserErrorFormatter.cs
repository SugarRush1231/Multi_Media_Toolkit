using System;
using System.Text;

namespace YoutubeDownloader;

internal static class UserErrorFormatter
{
    public static string GetCause(string? detail)
    {
        string message = detail ?? string.Empty;
        string lower = message.ToLowerInvariant();

        if (ContainsAny(lower,
                "could not copy chrome cookie database",
                "could not copy chromium cookie database",
                "cookie database is locked",
                "database is locked"))
            return "브라우저 쿠키 사용 중";

        if (ContainsAny(lower,
                "failed to start a process",
                "target file or working directory doesn't exist",
                "the system cannot find the file specified",
                "no such file or directory") &&
            ContainsAny(lower, "ffmpeg", "ffprobe", "yt-dlp", "ytdlp", "deno"))
            return "필수 도구 실행 실패";

        if (ContainsAny(lower, "no space left on device", "disk full", "not enough space") ||
            message.Contains("디스크 공간", StringComparison.OrdinalIgnoreCase))
            return "저장 공간 부족";

        if (ContainsAny(lower,
                "access is denied",
                "access denied",
                "unauthorizedaccessexception",
                "permission denied",
                "insufficient permissions"))
            return "파일 또는 폴더 권한 부족";

        if (ContainsAny(lower, "object reference not set", "nullreferenceexception"))
            return "프로그램 내부 처리 오류";

        if (ContainsAny(lower, "sha-256", "sha256", "hash verification", "checksum"))
            return "업데이트 파일 검증 실패";

        if (ContainsAny(lower, "invalid argument", "errno 22", "unable to open for writing"))
            return "파일명 오류";

        if (ContainsAny(lower, "403", "forbidden"))
            return "403 차단";

        if (lower.Contains("no video could be found in this tweet", StringComparison.Ordinal))
            return "X \uB85C\uADF8\uC778 \uB610\uB294 \uC601\uC0C1 \uD655\uC778 \uD544\uC694";

        if (ContainsAny(lower,
                "instagram sent an empty media response",
                "use --cookies-from-browser",
                "use --cookies for the authentication",
                "login required",
                "sign in",
                "members-only",
                "members only",
                "private video",
                "age-restricted",
                "confirm you're not a bot",
                "401",
                "cookies") ||
            message.Contains("로그인", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("비공개", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("인증", StringComparison.OrdinalIgnoreCase))
            return "로그인 필요";

        if (ContainsAny(lower, "requested format is not available", "only images storyboard"))
            return "사용 가능한 영상 포맷 없음";

        if (ContainsAny(lower, "ffmpeg", "ffprobe", "conversion failed") ||
            message.Contains("변환 실패", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("MP4 변환 실패", StringComparison.OrdinalIgnoreCase))
            return "ffmpeg 변환 실패";

        if (ContainsAny(lower, "m3u8", "manifest", "hls") ||
            message.Contains("스트림 URL", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("영상 정보를 자동", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("재생 정보", StringComparison.OrdinalIgnoreCase))
            return "m3u8 추출 실패";

        if (ContainsAny(lower, "unsupported url", "unable to extract", "extractor", "no video formats", "not supported"))
            return "사이트 구조 변경 가능성";

        if (ContainsAny(lower, "timed out", "timeout") || message.Contains("시간 초과", StringComparison.OrdinalIgnoreCase))
            return "네트워크 시간 초과";

        if (ContainsAny(lower,
                "name or service not known",
                "no such host is known",
                "temporary failure in name resolution",
                "network is unreachable",
                "connection refused",
                "connection reset"))
            return "네트워크 연결 오류";

        if (ContainsAny(lower, "404", "not found"))
            return "영상 삭제 또는 주소 오류";

        return "원인 확인 필요";
    }

    public static string GetHint(string cause)
    {
        return cause switch
        {
            "X \uB85C\uADF8\uC778 \uB610\uB294 \uC601\uC0C1 \uD655\uC778 \uD544\uC694" => "X\uC5D0\uC11C \uB85C\uADF8\uC778\uD574\uC57C \uD655\uC778\uD560 \uC218 \uC788\uB294 \uC601\uC0C1\uC77C \uC218 \uC788\uC2B5\uB2C8\uB2E4. [\uB85C\uADF8\uC778 \uD6C4 \uB2E4\uC6B4]\uC5D0\uC11C X\uC5D0 \uB85C\uADF8\uC778\uD558\uACE0 \uAC8C\uC2DC\uBB3C \uC601\uC0C1\uC774 \uC7AC\uC0DD\uB418\uB294\uC9C0 \uD655\uC778\uD574 \uC8FC\uC138\uC694.",
            "브라우저 쿠키 사용 중" => "로그인 브라우저가 쿠키 파일을 사용 중입니다. 잠시 기다린 뒤 한 번만 다시 시도해 주세요.",
            "필수 도구 실행 실패" => "FFmpeg 또는 yt-dlp를 실행하지 못했습니다. 설정의 도구 준비 상태를 확인하고 필수 도구를 다시 설치해 주세요.",
            "저장 공간 부족" => "저장할 드라이브의 남은 공간을 확보하거나 저장 위치를 다른 드라이브로 변경해 주세요.",
            "파일 또는 폴더 권한 부족" => "저장 폴더에 쓸 권한이 없습니다. 다른 저장 위치를 선택하거나 폴더 권한을 확인해 주세요.",
            "프로그램 내부 처리 오류" => "처리 중 필요한 정보를 읽지 못했습니다. 같은 작업을 다시 시도하고, 반복되면 오류 원문과 URL을 제작자에게 알려주세요.",
            "업데이트 파일 검증 실패" => "받은 업데이트 파일이 올바르지 않아 설치를 중단했습니다. 잠시 후 업데이트를 다시 시도해 주세요.",
            "파일명 오류" => "게시물 제목이나 저장 경로에 사용할 수 없는 문자가 포함되었을 수 있습니다. 안전한 파일명으로 자동 재시도되지 않으면 저장 위치를 바꿔 다시 시도해 주세요.",
            "m3u8 추출 실패" => "영상 주소를 찾지 못했습니다. 사이트 구조가 바뀌었거나 플레이어 로딩이 막혔을 수 있습니다.",
            "403 차단" => "사이트가 다운로드 요청을 차단했습니다. 로그인 후 다운에서 로그인하거나 지역 제한 및 사이트 차단 여부를 확인해 주세요.",
            "ffmpeg 변환 실패" => "영상 조각은 받았지만 MP4 병합 또는 변환에 실패했습니다. 필수 도구와 원본 스트림 상태를 확인해 주세요.",
            "로그인 필요" => "로그인이 필요한 영상일 수 있습니다. 로그인 후 다운 화면에서 로그인한 뒤 다시 시도해 주세요.",
            "사용 가능한 영상 포맷 없음" => "받을 수 있는 영상 포맷을 찾지 못했습니다. 로그인 권한, 라이브 상태, 다운로드 엔진 업데이트 여부를 확인해 주세요.",
            "사이트 구조 변경 가능성" => "현재 주소를 지원하지 않거나 사이트 구조가 바뀌었을 수 있습니다. 주소가 영상 페이지인지 확인한 뒤 다시 시도해 주세요.",
            "네트워크 시간 초과" => "사이트 응답이 느리거나 네트워크가 불안정합니다. 잠시 뒤 다시 시도해 주세요.",
            "네트워크 연결 오류" => "인터넷 연결 또는 사이트 접속 상태를 확인한 뒤 다시 시도해 주세요.",
            "영상 삭제 또는 주소 오류" => "영상이 삭제되었거나 주소가 잘못되었을 수 있습니다.",
            _ => "아래 기존 오류 원문을 확인해 주세요. 같은 문제가 반복되면 원문과 작업 대상을 제작자에게 알려주세요."
        };
    }

    public static string Format(string context, Exception ex)
    {
        string detail = Flatten(ex);
        string classificationText = string.IsNullOrWhiteSpace(detail)
            ? ex.GetType().FullName ?? ex.GetType().Name
            : $"{ex.GetType().FullName} {detail}";
        return Format(context, detail, classificationText);
    }

    public static string Format(string context, string? detail)
    {
        return Format(context, detail, detail);
    }

    private static string Format(string context, string? detail, string? classificationText)
    {
        string original = string.IsNullOrWhiteSpace(detail) ? "상세 오류가 없습니다." : detail.Trim();
        if (original.Length > 1600)
            original = original.Substring(0, 1600) + "...";

        string cause = GetCause(classificationText);
        return $"{context}\n\n원인: {cause}\n해결 방법: {GetHint(cause)}\n\n[기존 오류 원문]\n{original}";
    }

    private static string Flatten(Exception ex)
    {
        var builder = new StringBuilder();
        Exception? current = ex;
        while (current != null)
        {
            if (!string.IsNullOrWhiteSpace(current.Message))
            {
                if (builder.Length > 0) builder.AppendLine();
                builder.Append(current.Message.Trim());
            }
            current = current.InnerException;
        }

        return builder.ToString();
    }

    private static bool ContainsAny(string value, params string[] candidates)
    {
        foreach (string candidate in candidates)
        {
            if (value.Contains(candidate, StringComparison.Ordinal)) return true;
        }

        return false;
    }
}
