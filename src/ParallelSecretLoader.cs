using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Security.KeyVault.Secrets;

namespace AspNetCore.Azure.Configuration.KvSecrets
{
    public class ParallelSecretLoader : IDisposable
    {
        private const int ParallelismLevel = 32;
        private readonly SecretClient _client;
        private readonly SemaphoreSlim _semaphore;
        private readonly List<Task<Response<KeyVaultSecret>>> _tasks;

        public ParallelSecretLoader(SecretClient client)
        {
            _client = client;
            _semaphore = new SemaphoreSlim(ParallelismLevel, ParallelismLevel);
            _tasks = new List<Task<Response<KeyVaultSecret>>>();
        }

        public void AddSecretToLoad(string secretName)
        {
            _tasks.Add(GetVaultSecret(secretName));
        }

        private async Task<Response<KeyVaultSecret>> GetVaultSecret(string secretName)
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                return await _client.GetSecretAsync(secretName).ConfigureAwait(false);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public Task<Response<KeyVaultSecret>[]> WaitForAllSecrets()
        {
            return Task.WhenAll(_tasks);
        }

        public void Dispose()
        {
            _semaphore?.Dispose();
        }
    }
}