namespace YoutubeDownloader.SiteDownloads;

internal sealed class GenericDownloadProfile : ISiteDownloadProfile
{
    public string SiteKey => "WebSite";
    public string? Referer => null;
    public int ConcurrentFragments => 2;
    public bool Matches(Uri uri) => true;
}
