using Azure;
using Azure.Security.KeyVault.Secrets;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetCore.Azure.Configuration.KvSecrets;

/// <summary>
/// Loads Azure Key Vault secrets in parallel with a configurable level of concurrency.
/// </summary>
public class ParallelSecretLoader : IDisposable
{
    private const int ParallelismLevel = 32;
    private readonly SecretClient _client;
    private readonly SemaphoreSlim _semaphore;
    private readonly List<Task<Response<KeyVaultSecret>>> _tasks;

    /// <summary>
    /// Initializes a new instance of the ParallelSecretLoader class using the specified SecretClient for secret retrieval
    /// operations.
    /// </summary>
    /// <remarks>This constructor configures the loader to use the provided client for all secret operations. The
    /// degree of parallelism is set according to the ParallelismLevel property, which controls how many secret retrieval
    /// tasks can run concurrently.</remarks>
    /// <param name="client">The SecretClient instance used to access and retrieve secrets from Azure Key Vault. Cannot be null.</param>
    public ParallelSecretLoader(SecretClient client)
    {
        _client = client;
        _semaphore = new SemaphoreSlim(ParallelismLevel, ParallelismLevel);
        _tasks = new List<Task<Response<KeyVaultSecret>>>();
    }

    /// <summary>
    /// Adds a secret name to the list of secrets to be loaded in parallel from Azure Key Vault.
    /// </summary>
    /// <param name="secretName">The name of the secret to load.</param>
    public void AddSecretToLoad(string secretName)
    {
        _tasks.Add(GetVaultSecretAsync(secretName));
    }

    private async Task<Response<KeyVaultSecret>> GetVaultSecretAsync(string secretName)
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

    /// <summary>
    /// Waits for all added secrets to be loaded in parallel and returns their responses.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains an array of <see cref="Response{KeyVaultSecret}"/> objects for each loaded secret.
    /// </returns>
    public Task<Response<KeyVaultSecret>[]> WaitForAllSecrets()
    {
        return Task.WhenAll(_tasks);
    }

    /// <summary>
    /// Releases the resources used by the <see cref="ParallelSecretLoader"/>.
    /// Calls <see cref="GC.SuppressFinalize(object)"/> to prevent finalization.
    /// </summary>
    public void Dispose()
    {
        _semaphore?.Dispose();
        GC.SuppressFinalize(this);
    }
}