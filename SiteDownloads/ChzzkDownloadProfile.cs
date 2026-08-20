namespace YoutubeDownloader.SiteDownloads;

internal sealed class ChzzkDownloadProfile : ISiteDownloadProfile
{
    public string SiteKey => "Chzzk";
    public string? Referer => "https://chzzk.naver.com/";
    public int ConcurrentFragments => 2;
    public bool Matches(Uri uri) => SiteHostMatcher.Matches(uri, "chzzk.naver.com", "pstatic.net");
}
