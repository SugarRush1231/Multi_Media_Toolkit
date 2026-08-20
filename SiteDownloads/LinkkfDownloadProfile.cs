namespace YoutubeDownloader.SiteDownloads;

internal sealed class LinkkfDownloadProfile : ISiteDownloadProfile
{
    public string SiteKey => "Linkkf";
    public string? Referer => null;
    public int ConcurrentFragments => 2;
    public bool Matches(Uri uri) => SiteHostMatcher.Matches(
        uri,
        "linkkf.drewpx.xyz",
        "linkkf.tckopke.com",
        "kf.carsstore365.com",
        "play.sub2.top",
        "play.sub3.top",
        "playv2.sub3.top",
        "hlz3.top");
}
