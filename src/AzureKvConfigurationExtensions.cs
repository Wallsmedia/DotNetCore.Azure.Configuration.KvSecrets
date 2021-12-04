using System;
using AspNetCore.Azure.Configuration.KvSecrets;
using Azure.Core;
using Azure.Security.KeyVault.Secrets;


#pragma warning disable AZC0001 // Extension methods have to be in the correct namespace to appear in intellisense.
namespace Microsoft.Extensions.Configuration
#pragma warning restore
{
    /// <summary>
    /// Extension methods for registering <see cref="AzureKvConfigurationProvider"/> with <see cref="IConfigurationBuilder"/>.
    /// </summary>
    public static class AzureKvConfigurationExtensions
    {
        /// <summary>
        /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from the Azure KeyVault.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="vaultUri">The Azure Key Vault uri.</param>
        /// <param name="credential">The credential to to use for authentication.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddAzureKeyVault(
            this IConfigurationBuilder configurationBuilder,
            Uri vaultUri,
            TokenCredential credential)
        {
            return AddAzureKeyVault(configurationBuilder, vaultUri, credential, new AzureKvConfigurationOptions());
        }

        /// <summary>
        /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from the Azure KeyVault.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="credential">The credential to to use for authentication.</param>
        /// <param name="options">The <see cref="AzureKvConfigurationOptions"/> to use.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddAzureKeyVault(
            this IConfigurationBuilder configurationBuilder,
            TokenCredential credential,
            AzureKvConfigurationOptions options)
        {
            options = options ?? new AzureKvConfigurationOptions();
            options.Credential = credential;
            return configurationBuilder.AddAzureKeyVault(options);
        }

        /// <summary>
        /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from the Azure KeyVault.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="vaultUri">Azure Key Vault uri.</param>
        /// <param name="credential">The credential to to use for authentication.</param>
        /// <param name="options">The <see cref="AzureKvConfigurationOptions"/> to use.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddAzureKeyVault(
            this IConfigurationBuilder configurationBuilder,
            Uri vaultUri,
            TokenCredential credential,
            AzureKvConfigurationOptions options)
        {
            options = options ?? new AzureKvConfigurationOptions();
            options.VaultUri = vaultUri;
            options.Credential = credential;
            return configurationBuilder.AddAzureKeyVault(options);
        }

        /// <summary>
        /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from the Azure KeyVault.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="options">The <see cref="AzureKvConfigurationOptions"/> to use.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        internal static IConfigurationBuilder AddAzureKeyVault(this IConfigurationBuilder configurationBuilder, AzureKvConfigurationOptions options)
        {
            Argument.AssertNotNull(configurationBuilder, nameof(configurationBuilder));
            Argument.AssertNotNull(options, nameof(options));
            Argument.AssertNotNull(options.VaultUri, $"{nameof(options)}.{nameof(options.VaultUri)}");
            Argument.AssertNotNull(options.Credential, $"{nameof(options)}.{nameof(options.Credential)}");
            Argument.AssertNotNull(options.KeyVaultSecretNameEncoder, $"{nameof(options)}.{nameof(options.KeyVaultSecretNameEncoder)}");
            configurationBuilder.Add(new AzureKvConfigurationSource(options));

            return configurationBuilder;
        }
    }
}
