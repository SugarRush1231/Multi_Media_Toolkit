namespace YoutubeDownloader.SiteDownloads;

internal static class SiteDownloadProfileRegistry
{
    private static readonly ISiteDownloadProfile[] Profiles =
    {
        new YouTubeDownloadProfile(),
        new ChzzkDownloadProfile(),
        new SoopDownloadProfile(),
        new InstagramDownloadProfile(),
        new ThreadsDownloadProfile(),
        new KuaishouDownloadProfile(),
        new XDownloadProfile(),
        new TikTokDownloadProfile(),
        new AnilifeDownloadProfile(),
        new LinkkfDownloadProfile()
    };

    private static readonly ISiteDownloadProfile GenericProfile = new GenericDownloadProfile();

    public static ISiteDownloadProfile Resolve(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return GenericProfile;
        return Profiles.FirstOrDefault(profile => profile.Matches(uri)) ?? GenericProfile;
    }
}
