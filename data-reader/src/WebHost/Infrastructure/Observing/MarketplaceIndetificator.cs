public record MarketplaceIndetificator (string Url, string MarketPlaceType, string CurrentUrl) {
    public string Id => $"{MarketPlaceType}:{Url}";

    public bool IsLoaded { get; set; } = false;

    public List<string> LoadedUrl { get; set; } = new List<string>();

    public List<string> FailedWhenChangePage { get; set; } = new List<string>();
}

