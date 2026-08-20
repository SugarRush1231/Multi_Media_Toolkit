namespace YoutubeDownloader.SiteDownloads;

internal sealed class SoopDownloadProfile : ISiteDownloadProfile
{
    public string SiteKey => "SOOP";
    public string? Referer => "https://vod.sooplive.com/";
    public int ConcurrentFragments => 2;
    public bool Matches(Uri uri) => SiteHostMatcher.Matches(uri, "sooplive.com", "sooplive.co.kr", "afreecatv.com");
}
