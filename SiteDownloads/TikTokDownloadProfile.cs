namespace YoutubeDownloader.SiteDownloads;

internal sealed class TikTokDownloadProfile : ISiteDownloadProfile
{
    public string SiteKey => "TikTok";
    public string? Referer => null;
    public int ConcurrentFragments => 2;
    public bool Matches(Uri uri) => SiteHostMatcher.Matches(uri, "tiktok.com");
}
