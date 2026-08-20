namespace YoutubeDownloader.SiteDownloads;

internal interface ISiteDownloadProfile
{
    string SiteKey { get; }
    string? Referer { get; }
    int ConcurrentFragments { get; }
    bool Matches(Uri uri);
}

internal static class SiteHostMatcher
{
    public static bool Matches(Uri uri, params string[] domains)
    {
        string host = uri.Host.TrimEnd('.');
        return domains.Any(domain =>
            host.Equals(domain, StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase));
    }
}
