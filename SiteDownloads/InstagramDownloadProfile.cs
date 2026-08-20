namespace YoutubeDownloader.SiteDownloads;

internal sealed class InstagramDownloadProfile : ISiteDownloadProfile
{
    public string SiteKey => "Instagram";
    public string? Referer => "https://www.instagram.com/";
    public int ConcurrentFragments => 2;
    public bool Matches(Uri uri) => SiteHostMatcher.Matches(uri, "instagram.com", "cdninstagram.com", "fbcdn.net");
}
