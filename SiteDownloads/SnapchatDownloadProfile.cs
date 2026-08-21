namespace YoutubeDownloader.SiteDownloads;

internal sealed class SnapchatDownloadProfile : ISiteDownloadProfile
{
    public string SiteKey => "Snapchat";
    public string? Referer => "https://www.snapchat.com/";
    public int ConcurrentFragments => 1;
    public bool Matches(Uri uri) => SiteHostMatcher.Matches(uri, "snapchat.com");
}
