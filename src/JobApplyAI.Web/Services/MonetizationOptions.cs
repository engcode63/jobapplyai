namespace JobApplyAI.Web.Services;

/// <summary>
/// Google AdSense configuration. Leave <see cref="PublisherId"/> as its placeholder value and
/// every ad slot on the site quietly renders nothing (or a dev-only placeholder box) instead of
/// throwing - same "demo mode" convention used for Cosmos/AI Foundry/Entra elsewhere in this app.
/// AdSense approval requires a live public site with real content and a Privacy Policy page
/// (see <c>Components/Pages/PrivacyPolicy.razor</c>) before Google will approve a publisher ID.
/// </summary>
public class AdSenseOptions
{
    public const string SectionName = "GoogleAdSense";

    /// <summary>Publisher ID from the AdSense dashboard, e.g. "ca-pub-1234567890123456".</summary>
    public string PublisherId { get; set; } = string.Empty;
}

/// <summary>Buy Me a Coffee tip-jar link configuration.</summary>
public class BuyMeACoffeeOptions
{
    public const string SectionName = "BuyMeACoffee";

    /// <summary>The page slug at buymeacoffee.com/{Username}.</summary>
    public string Username { get; set; } = string.Empty;
}
