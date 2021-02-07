using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;

namespace AspNetCore.Azure.Configuration.KvSecrets
{
    /// <summary>
    /// An AzureKeyVault based <see cref="ConfigurationProvider"/>.
    /// </summary>
    public class AzureKvConfigurationProvider : ConfigurationProvider, IDisposable
    {
        private readonly TimeSpan? _reloadInterval;
        private Task _pollingTask;

        private readonly SecretClient _client;
        private List<string> _uploadKeyList;
        private Dictionary<string, string> _uploadAndMapKeys;
        private KeyVaultSecretNameEncoder _keyVaultSecretNameEncoder;
        private string _configurationSectionPrefix;
        private Dictionary<string, LoadedSecret> _loadedSecrets;

        private bool _fullLoad;
        private readonly CancellationTokenSource _cancellationToken;

        /// <summary>
        /// Creates a new instance of <see cref="AzureKvConfigurationProvider"/>.
        /// </summary>
        /// <param name="options">The <see cref="AzureKvConfigurationOptions"/> to use for configuration options.</param>
        public AzureKvConfigurationProvider(AzureKvConfigurationOptions options)
        {
            Argument.AssertNotNull(options, nameof(options));
            Argument.AssertNotNull(options.Client, nameof(options.Client));
            Argument.AssertNotNull(options.KeyVaultSecretNameEncoder, nameof(options.KeyVaultSecretNameEncoder));

            _reloadInterval = options.ReloadInterval;
            _client = options.Client;
            _uploadKeyList = options.VaultSecrets != null ? new List<string>(options.VaultSecrets) : new List<string>();
            _uploadAndMapKeys = options.VaultSecretMap != null ? new Dictionary<string, string>(options.VaultSecretMap) : new Dictionary<string, string>();
            _configurationSectionPrefix = options.ConfigurationSectionPrefix;
            _keyVaultSecretNameEncoder = options.KeyVaultSecretNameEncoder;

            _fullLoad = _uploadKeyList.Count == 0 && _uploadAndMapKeys.Count == 0;
            _cancellationToken = new CancellationTokenSource();
        }

        #region IConfigurationProvider
        /// <summary>
        /// Load secrets into this provider.
        /// </summary>
        public override void Load() => LoadAsync().GetAwaiter().GetResult();

        #endregion

        private async Task PollForSecretChangesAsync()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                await WaitForReload().ConfigureAwait(false);
                try
                {
                    await LoadAsync().ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // Ignore
                }
            }
        }

        protected virtual Task WaitForReload()
        {
            // WaitForReload is only called when the _reloadInterval has a value.
            return Task.Delay(_reloadInterval.Value, _cancellationToken.Token);
        }

        private async Task LoadAsync()
        {
            using var secretLoader = new ParallelSecretLoader(_client);
            var newLoadedSecrets = new Dictionary<string, LoadedSecret>();
            var oldLoadedSecrets = Interlocked.Exchange(ref _loadedSecrets, null);
            if (_fullLoad)
            {
                AsyncPageable<SecretProperties> secretPages = _client.GetPropertiesOfSecretsAsync();
                await foreach (var secret in secretPages.ConfigureAwait(false))
                {
                    if (secret.Enabled != true)
                    {
                        continue;
                    }
                    VerifySecretToload(secretLoader, newLoadedSecrets, oldLoadedSecrets, secret);
                }
            }
            else
            {
                foreach (var key in _uploadKeyList)
                {
                    AsyncPageable<SecretProperties> secretProperties = _client.GetPropertiesOfSecretVersionsAsync(key);
                    var secretList = await secretProperties.ToListAsync();
                    var secret = secretList.OrderByDescending(s => s.UpdatedOn).FirstOrDefault(w => w.Enabled.GetValueOrDefault());
                    if (secret != null)
                    {
                        VerifySecretToload(secretLoader, newLoadedSecrets, oldLoadedSecrets, secret);
                    }
                }

                foreach (var keyValue in _uploadAndMapKeys)
                {
                    AsyncPageable<SecretProperties> secretProperties = _client.GetPropertiesOfSecretVersionsAsync(keyValue.Key);
                    var secretList = await secretProperties.ToListAsync();
                    var secret = secretList.OrderByDescending(s => s.UpdatedOn).FirstOrDefault(w => w.Enabled.GetValueOrDefault());
                    if (secret != null)
                    {
                        VerifySecretToload(secretLoader, newLoadedSecrets, oldLoadedSecrets, secret);
                    }
                }
            }

            var loadedSecrets = await secretLoader.WaitForAllSecrets().ConfigureAwait(false);

            foreach (var secretBundle in loadedSecrets)
            {
                string configName = secretBundle.Value.Name;
                
                if (!_fullLoad)
                {
                    if (_uploadAndMapKeys.Keys.Contains(configName))
                    {
                        configName = _uploadAndMapKeys[configName];
                    }
                }

                if (!string.IsNullOrWhiteSpace(_configurationSectionPrefix))
                {
                    configName = _configurationSectionPrefix + ConfigurationPath.KeyDelimiter + configName;
                }

                newLoadedSecrets.Add(secretBundle.Value.Name, new LoadedSecret(_keyVaultSecretNameEncoder(configName),
                    secretBundle.Value.Value, secretBundle.Value.Properties.UpdatedOn));
            }

            _loadedSecrets = newLoadedSecrets;

            // Reload is needed if we are loading secrets that were not loaded before or
            // secret that was loaded previously is not available anymore
            if (loadedSecrets.Any() || oldLoadedSecrets?.Any() == true)
            {
                SetData(_loadedSecrets, fireToken: oldLoadedSecrets != null);
            }

            // schedule a polling task only if none exists and a valid delay is specified
            if (_pollingTask == null && _reloadInterval != null)
            {
                _pollingTask = PollForSecretChangesAsync();
            }
        }

        private static void VerifySecretToload(ParallelSecretLoader secretLoader, Dictionary<string, LoadedSecret> newLoadedSecrets, Dictionary<string, LoadedSecret> oldLoadedSecrets, SecretProperties secret)
        {
            var secretId = secret.Name;
            if (oldLoadedSecrets != null &&
                oldLoadedSecrets.TryGetValue(secretId, out var existingSecret) &&
                existingSecret.IsUpToDate(secret.UpdatedOn))
            {
                oldLoadedSecrets.Remove(secretId);
                newLoadedSecrets.Add(secretId, existingSecret);
            }
            else
            {
                secretLoader.AddSecretToLoad(secret.Name);
            }
        }

        private void SetData(Dictionary<string, LoadedSecret> loadedSecrets, bool fireToken)
        {
            var data = new Dictionary<string, string>(loadedSecrets.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var secretItem in loadedSecrets)
            {
                data.Add(secretItem.Value.Key, secretItem.Value.Value);
            }

            Data = data;
            if (fireToken)
            {
                OnReload();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _cancellationToken.Cancel();
            _cancellationToken.Dispose();
        }

        private readonly struct LoadedSecret
        {
            public LoadedSecret(string key, string value, DateTimeOffset? updated)
            {
                Key = key;
                Value = value;
                Updated = updated;
            }

            public string Key { get; }
            public string Value { get; }
            public DateTimeOffset? Updated { get; }

            public bool IsUpToDate(DateTimeOffset? updated)
            {
                if (updated.HasValue != Updated.HasValue)
                {
                    return false;
                }

                return updated.GetValueOrDefault() == Updated.GetValueOrDefault();
            }
        }
    }
}
