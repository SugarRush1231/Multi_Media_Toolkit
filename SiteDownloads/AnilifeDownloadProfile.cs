namespace YoutubeDownloader.SiteDownloads;

internal sealed class AnilifeDownloadProfile : ISiteDownloadProfile
{
    public string SiteKey => "Anilife";
    public string? Referer => "https://anilife.app/";
    public int ConcurrentFragments => 2;
    public bool Matches(Uri uri) => SiteHostMatcher.Matches(uri, "anilife.app", "gcdn.app");
}
