using System;

namespace AspNetCore.Azure.Configuration.KvSecrets;

/// <summary>
/// Provides Azure Active Directory authority hosts and related utility methods for different Azure clouds.
/// </summary>
public static class AzureAuthorityHosts
{
    private const string AzurePublicCloudHostUrl = "https://login.microsoftonline.com/";
    private const string AzureChinaHostUrl = "https://login.chinacloudapi.cn/";
    private const string AzureGermanyHostUrl = "https://login.microsoftonline.de/";
    private const string AzureGovernmentHostUrl = "https://login.microsoftonline.us/";
    /// <summary>
    /// The host of the Azure Active Directory authority for tenants in the Azure Public Cloud.
    /// </summary>
    public static Uri AzurePublicCloud { get; } = new Uri(AzurePublicCloudHostUrl);

    /// <summary>
    /// The host of the Azure Active Directory authority for tenants in the Azure China Cloud.
    /// </summary>
    public static Uri AzureChina { get; } = new Uri(AzureChinaHostUrl);

    /// <summary>
    /// The host of the Azure Active Directory authority for tenants in the Azure German Cloud.
    /// </summary>
    public static Uri AzureGermany { get; } = new Uri(AzureGermanyHostUrl);

    /// <summary>
    /// The host of the Azure Active Directory authority for tenants in the Azure US Government Cloud.
    /// </summary>
    public static Uri AzureGovernment { get; } = new Uri(AzureGovernmentHostUrl);

    /// <summary>
    /// Gets the default scope for the specified Azure Active Directory authority host.
    /// </summary>
    /// <param name="authorityHost">The authority host URI.</param>
    /// <returns>
    /// The default scope string for the specified authority host, or <c>null</c> if the host is not recognized.
    /// </returns>
    public static string GetDefaultScope(Uri authorityHost)
    {
        switch (authorityHost.ToString())
        {
            case AzurePublicCloudHostUrl:
                return "https://management.core.windows.net//.default";
            case AzureChinaHostUrl:
                return "https://management.core.chinacloudapi.cn//.default";
            case AzureGermanyHostUrl:
                return "https://management.core.cloudapi.de//.default";
            case AzureGovernmentHostUrl:
                return "https://management.core.usgovcloudapi.net//.default";
            default:
                return null;
        }
    }

    /// <summary>
    /// Gets the device code redirect URI for the specified Azure Active Directory authority host.
    /// </summary>
    /// <param name="authorityHost">The authority host URI.</param>
    /// <returns>
    /// The device code redirect URI for the specified authority host.
    /// </returns>
    public static Uri GetDeviceCodeRedirectUri(Uri authorityHost)
    {
        return new Uri(authorityHost, "/common/oauth2/nativeclient");
    }
}
