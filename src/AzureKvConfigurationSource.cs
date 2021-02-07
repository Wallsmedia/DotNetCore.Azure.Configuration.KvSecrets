using Microsoft.Extensions.Configuration;

namespace AspNetCore.Azure.Configuration.KvSecrets
{
    /// <summary>
    /// Represents Azure Key Vault secrets as an <see cref="IConfigurationSource"/>.
    /// </summary>
    public class AzureKvConfigurationSource : IConfigurationSource
    {
        private readonly AzureKvConfigurationOptions _options;

        public AzureKvConfigurationSource(AzureKvConfigurationOptions options)
        {
            _options = options;
        }

        /// <inheritdoc />
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new AzureKvConfigurationProvider(_options);
        }
    }
}



