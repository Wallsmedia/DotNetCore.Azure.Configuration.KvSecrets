namespace AspNetCore.Azure.Configuration.KvSecrets
{
    /// <summary>
    /// Provides resource identifiers for various Azure services.
    /// </summary>
    public class Constants
    {
        /// <summary>
        /// The resource identifier for Azure Key Vault.
        /// </summary>
        public const string KeyVaultResourceId = "https://vault.azure.net/";

        /// <summary>
        /// The resource identifier for Azure Active Directory Graph.
        /// </summary>
        public const string GraphResourceId = "https://graph.windows.net/";

        /// <summary>
        /// The resource identifier for Azure Resource Manager.
        /// </summary>
        public const string ArmResourceId = "https://management.azure.com/";

        /// <summary>
        /// The resource identifier for Azure SQL Database.
        /// </summary>
        public const string SqlAzureResourceId = "https://database.windows.net/";

        /// <summary>
        /// The resource identifier for Azure Data Lake.
        /// </summary>
        public const string DataLakeResourceId = "https://datalake.azure.net/";
    }
}
