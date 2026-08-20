namespace YoutubeDownloader.SiteDownloads;

internal sealed class ThreadsDownloadProfile : ISiteDownloadProfile
{
    public string SiteKey => "Threads";
    public string? Referer => "https://www.threads.com/";
    public int ConcurrentFragments => 2;
    public bool Matches(Uri uri) => SiteHostMatcher.Matches(uri, "threads.com", "threads.net");
}
