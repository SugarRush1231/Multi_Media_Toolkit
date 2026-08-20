namespace YoutubeDownloader.SiteDownloads;

internal sealed class KuaishouDownloadProfile : ISiteDownloadProfile
{
    public string SiteKey => "Kuaishou";
    public string? Referer => "https://www.kuaishou.com/";
    public int ConcurrentFragments => 2;
    public bool Matches(Uri uri) => SiteHostMatcher.Matches(uri, "kuaishou.com");
}
