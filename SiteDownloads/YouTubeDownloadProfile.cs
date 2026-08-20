namespace YoutubeDownloader.SiteDownloads;

internal sealed class YouTubeDownloadProfile : ISiteDownloadProfile
{
    public string SiteKey => "YouTube";
    public string? Referer => null;
    public int ConcurrentFragments => 3;
    public bool Matches(Uri uri) => SiteHostMatcher.Matches(uri, "youtube.com", "youtube-nocookie.com", "youtu.be");
}
