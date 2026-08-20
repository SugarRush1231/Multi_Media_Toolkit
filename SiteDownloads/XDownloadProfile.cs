namespace YoutubeDownloader.SiteDownloads;

internal sealed class XDownloadProfile : ISiteDownloadProfile
{
    public string SiteKey => "X";
    public string? Referer => "https://x.com/";
    public int ConcurrentFragments => 2;
    public bool Matches(Uri uri) => SiteHostMatcher.Matches(uri, "x.com", "twitter.com", "twimg.com");
}
