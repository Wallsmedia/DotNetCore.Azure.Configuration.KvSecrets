// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Core.TestFramework;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Primitives;
using Moq;
using NUnit.Framework;

namespace AspNetCore.Azure.Configuration.KvSecrets.Tests
{
    public class AzureKeyVaultConfigurationTests
    {
        private static readonly TimeSpan NoReloadDelay = TimeSpan.FromMilliseconds(2);

        private void SetPages(Mock<SecretClient> mock, params KeyVaultSecret[][] pages)
        {
            SetPages(mock, null, pages);
        }

        private void SetPages(Mock<SecretClient> mock, Func<string, Task> getSecretCallback, params KeyVaultSecret[][] pages)
        {
            getSecretCallback ??= (_ => Task.CompletedTask);

            var pagesOfProperties = pages.Select(
                page => page.Select(secret => secret.Properties).ToArray()).ToArray();

            mock.Setup(m => m.GetPropertiesOfSecretsAsync(default)).Returns(new MockAsyncPageable(pagesOfProperties));

            foreach (var page in pages)
            {
                foreach (var secret in page)
                {
                    SecretProperties[][] ser = new[] { new SecretProperties[] { secret.Properties } };
                    mock.Setup(client => client.GetPropertiesOfSecretVersionsAsync(secret.Name, default))
                        .Returns(new MockAsyncPageable(ser));
                }
            }

            foreach (var page in pages)
            {
                foreach (var secret in page)
                {

                    mock.Setup(client => client.GetSecretAsync(secret.Name, null, default))
                        .Returns(async (string name, string label, CancellationToken token) =>
                        {
                            await getSecretCallback(name);
                            return Response.FromValue(secret, Mock.Of<Response>());
                        }
                    );
                }
            }
        }

        private class MockAsyncPageable : AsyncPageable<SecretProperties>
        {
            private readonly SecretProperties[][] _pages;

            public MockAsyncPageable(SecretProperties[][] pages)
            {
                _pages = pages;
            }

            public override async IAsyncEnumerable<Page<SecretProperties>> AsPages(string continuationToken = null, int? pageSizeHint = null)
            {
                foreach (var page in _pages)
                {
                    yield return Page<SecretProperties>.FromValues(page, null, Mock.Of<Response>());
                }

                await Task.CompletedTask;
            }
        }

        [Test]
        public void LoadsAllSecretsFromVaultIntoSection()
        {
            var client = new Mock<SecretClient>();
            SetPages(client,
                new[]
                {
                    CreateSecret("Secret1", "Value1")
                },
                new[]
                {
                    CreateSecret("Secret2", "Value2")
                }
                );

            var options = new AzureKvConfigurationOptions
            {
                Client = client.Object,
                ConfigurationSectionPrefix = "secrets"
            };

            // Act
            using (var provider = new AzureKvConfigurationProvider(options))
            {
                provider.Load();

                var childKeys = provider.GetChildKeys(Enumerable.Empty<string>(), null).ToArray();
                Assert.AreEqual(new[] { "secrets", "secrets" }, childKeys);
                Assert.AreEqual("Value1", provider.Get("secrets:Secret1"));
                Assert.AreEqual("Value2", provider.Get("secrets:Secret2"));
            }
        }

        [Test]
        public void LoadsAllSecretsFromVaultIntoEncodeSection()
        {
            var client = new Mock<SecretClient>();
            SetPages(client,
                new[]
                {
                    CreateSecret("secrets--Secret1", "Value1")
                },
                new[]
                {
                    CreateSecret("secrets--Secret2", "Value2")
                }
                );

            var options = new AzureKvConfigurationOptions
            {
                Client = client.Object,
            };

            // Act
            using (var provider = new AzureKvConfigurationProvider(options))
            {
                provider.Load();

                var childKeys = provider.GetChildKeys(Enumerable.Empty<string>(), null).ToArray();
                Assert.AreEqual(new[] { "secrets", "secrets" }, childKeys);
                Assert.AreEqual("Value1", provider.Get("secrets:Secret1"));
                Assert.AreEqual("Value2", provider.Get("secrets:Secret2"));
            }
        }

        private KeyVaultSecret CreateSecret(string name, string value, bool? enabled = true, DateTimeOffset? updated = null)
        {
            var id = new Uri("http://azure.keyvault/" + name);

            var secretProperties = SecretModelFactory.SecretProperties(id, name: name, updatedOn: updated);
            secretProperties.Enabled = enabled;

            return SecretModelFactory.KeyVaultSecret(secretProperties, value);
        }

        [Test]
        public void DoesNotLoadFilteredItems()
        {
            var client = new Mock<SecretClient>();
            SetPages(client,
                new[]
                {
                    CreateSecret("Secret1", "Value1")
                },
                new[]
                {
                    CreateSecret("Secret2", "Value2")
                }
            );

            var options = new AzureKvConfigurationOptions
            {
                Client = client.Object,
                VaultSecrets = new List<string> { "Secret1" }
            };

            // Act
            using (var provider = new AzureKvConfigurationProvider(options))
            {
                provider.Load();

                // Assert
                var childKeys = provider.GetChildKeys(Enumerable.Empty<string>(), null).ToArray();
                Assert.AreEqual(new[] { "Secret1" }, childKeys);
                Assert.AreEqual("Value1", provider.Get("Secret1"));
            }
        }

        [Test]
        public void DoesNotLoadFilteredAndRemapItems()
        {
            var client = new Mock<SecretClient>();
            SetPages(client,
                new[]
                {
                    CreateSecret("Secret1", "Value1")
                },
                new[]
                {
                    CreateSecret("Secret2", "Value2")
                }
            );

            var options = new AzureKvConfigurationOptions
            {
                Client = client.Object,
                VaultSecretMap = new Dictionary<string, string> { ["Secret1"] = "SecretMap" }
            };

            // Act
            using (var provider = new AzureKvConfigurationProvider(options))
            {
                provider.Load();

                // Assert
                var childKeys = provider.GetChildKeys(Enumerable.Empty<string>(), null).ToArray();
                Assert.AreEqual(new[] { "SecretMap" }, childKeys);
                Assert.AreEqual("Value1", provider.Get("SecretMap"));
            }
        }

        [Test]
        public void DoesNotLoadDisabledItems()
        {
            var client = new Mock<SecretClient>();
            SetPages(client,
                new[]
                {
                    CreateSecret("Secret1", "Value1")
                },
                new[]
                {
                    CreateSecret("Secret2", "Value2", enabled: false),
                    CreateSecret("Secret3", "Value3", enabled: null),
                }
            );

            var options = new AzureKvConfigurationOptions
            {
                Client = client.Object
            };

            // Act
            using (var provider = new AzureKvConfigurationProvider(options))
            {
                provider.Load();

                // Assert
                var childKeys = provider.GetChildKeys(Enumerable.Empty<string>(), null).ToArray();
                Assert.AreEqual(new[] { "Secret1" }, childKeys);
                Assert.AreEqual("Value1", provider.Get("Secret1"));
                Assert.Throws<InvalidOperationException>(() => provider.Get("Secret2"));
                Assert.Throws<InvalidOperationException>(() => provider.Get("Secret3"));
            }
        }

        [Test]
        public void SupportsReload()
        {
            var updated = DateTime.Now;

            var client = new Mock<SecretClient>();
            SetPages(client,
                new[]
                {
                    CreateSecret("Secret1", "Value1", enabled: true, updated: updated)
                }
            );

            var options = new AzureKvConfigurationOptions
            {
                Client = client.Object
            };

            // Act
            using (var provider = new AzureKvConfigurationProvider(options))
            {
                provider.Load();

                Assert.AreEqual("Value1", provider.Get("Secret1"));

                SetPages(client,
                    new[]
                    {
                        CreateSecret("Secret1", "Value2", enabled: true, updated: updated.AddSeconds(1))
                    }
                );

                provider.Load();
                Assert.AreEqual("Value2", provider.Get("Secret1"));
            }
        }

        [Test]
        public async Task SupportsAutoReload()
        {
            var updated = DateTime.Now;
            int numOfTokensFired = 0;

            var client = new Mock<SecretClient>();
            SetPages(client,
                new[]
                {
                    CreateSecret("Secret1", "Value1", enabled: true, updated: updated)
                }
            );

            var options = new AzureKvConfigurationOptions
            {
                Client = client.Object,
                ReloadInterval = NoReloadDelay
            };
            // Act & Assert
            using (var provider = new ReloadControlKeyVaultProvider(options))
            {
                ChangeToken.OnChange(
                    () => provider.GetReloadToken(),
                    () =>
                    {
                        numOfTokensFired++;
                    });

                provider.Load();

                Assert.AreEqual("Value1", provider.Get("Secret1"));

                await provider.Wait();

                SetPages(client,
                        new[]
                    {
                        CreateSecret("Secret1", "Value2", enabled: true, updated: updated.AddSeconds(1))
                    }
                );

                provider.Release();

                await provider.Wait();

                Assert.AreEqual("Value2", provider.Get("Secret1"));
                Assert.AreEqual(1, numOfTokensFired);
            }
        }

        [Test]
        public async Task DoesntReloadUnchanged()
        {
            var updated = DateTime.Now;
            int numOfTokensFired = 0;

            var client = new Mock<SecretClient>();
            SetPages(client,
                new[]
                {
                    CreateSecret("Secret1", "Value1", enabled: true, updated: updated)
                }
            );

            var options = new AzureKvConfigurationOptions
            {
                Client = client.Object,
                ReloadInterval = NoReloadDelay
            };

            // Act
            using (var provider = new ReloadControlKeyVaultProvider(options))
            {
                ChangeToken.OnChange(
                    () => provider.GetReloadToken(),
                    () =>
                    {
                        numOfTokensFired++;
                    });

                provider.Load();

                Assert.AreEqual("Value1", provider.Get("Secret1"));

                await provider.Wait();

                provider.Release();

                await provider.Wait();

                Assert.AreEqual("Value1", provider.Get("Secret1"));
                Assert.AreEqual(0, numOfTokensFired);
            }
        }

        [Test]
        public async Task SupportsReloadOnRemove()
        {
            int numOfTokensFired = 0;

            var client = new Mock<SecretClient>();
            SetPages(client,
                new[]
                {
                    CreateSecret("Secret1", "Value1"),
                    CreateSecret("Secret2", "Value2")
                }
            );

            var options = new AzureKvConfigurationOptions
            {
                Client = client.Object,
                ReloadInterval = NoReloadDelay
            };

            // Act
            using (var provider = new ReloadControlKeyVaultProvider(options))
            {
                ChangeToken.OnChange(
                    () => provider.GetReloadToken(),
                    () =>
                    {
                        numOfTokensFired++;
                    });

                provider.Load();

                Assert.AreEqual("Value1", provider.Get("Secret1"));

                await provider.Wait();

                SetPages(client,
                    new[]
                    {
                        CreateSecret("Secret1", "Value2")
                    }
                );

                provider.Release();

                await provider.Wait();

                Assert.Throws<InvalidOperationException>(() => provider.Get("Secret2"));
                Assert.AreEqual(1, numOfTokensFired);
            }
        }

        [Test]
        public async Task SupportsReloadOnEnabledChange()
        {
            int numOfTokensFired = 0;

            var client = new Mock<SecretClient>();
            SetPages(client,
                new[]
                {
                    CreateSecret("Secret1", "Value1"),
                    CreateSecret("Secret2", "Value2")
                }
            );

            var options = new AzureKvConfigurationOptions
            {
                Client = client.Object,
                ReloadInterval = NoReloadDelay
            };

            // Act
            using (var provider = new ReloadControlKeyVaultProvider(options))
            {
                ChangeToken.OnChange(
                    () => provider.GetReloadToken(),
                    () =>
                    {
                        numOfTokensFired++;
                    });

                provider.Load();

                Assert.AreEqual("Value2", provider.Get("Secret2"));

                await provider.Wait();

                SetPages(client,
        new[]
                    {
                        CreateSecret("Secret1", "Value2"),
                        CreateSecret("Secret2", "Value2", enabled: false)
                    }
                );

                provider.Release();

                await provider.Wait();

                Assert.Throws<InvalidOperationException>(() => provider.Get("Secret2"));
                Assert.AreEqual(1, numOfTokensFired);
            }
        }

        [Test]
        public async Task SupportsReloadOnAdd()
        {
            int numOfTokensFired = 0;

            var client = new Mock<SecretClient>();
            SetPages(client,
                new[]
                {
                    CreateSecret("Secret1", "Value1")
                }
            );

            var options = new AzureKvConfigurationOptions
            {
                Client = client.Object,
                ReloadInterval = NoReloadDelay
            };

            // Act
            using (var provider = new ReloadControlKeyVaultProvider(options))
            {
                ChangeToken.OnChange(
                    () => provider.GetReloadToken(),
                    () =>
                    {
                        numOfTokensFired++;
                    });

                provider.Load();

                Assert.AreEqual("Value1", provider.Get("Secret1"));

                await provider.Wait();

                SetPages(client,
                    new[]
                    {
                        CreateSecret("Secret1", "Value1"),
                    },
                    new[]
                    {
                        CreateSecret("Secret2", "Value2")
                    }
                );

                provider.Release();

                await provider.Wait();

                Assert.AreEqual("Value1", provider.Get("Secret1"));
                Assert.AreEqual("Value2", provider.Get("Secret2"));
                Assert.AreEqual(1, numOfTokensFired);
            }
        }

        [Test]
        public void ReplaceDoubleMinusInKeyName()
        {
            var client = new Mock<SecretClient>();
            SetPages(client,
                new[]
                {
                    CreateSecret("Section--Secret1", "Value1")
                }
            );

            var options = new AzureKvConfigurationOptions
            {
                Client = client.Object
            };

            // Act
            using (var provider = new AzureKvConfigurationProvider(options))
            {
                provider.Load();

                // Assert
                Assert.AreEqual("Value1", provider.Get("Section:Secret1"));
            }
        }

        [Test]
        public async Task LoadsSecretsInParallel()
        {
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var expectedCount = 2;
            var client = new Mock<SecretClient>();

            SetPages(client,
                async (string id) =>
                {
                    if (Interlocked.Decrement(ref expectedCount) == 0)
                    {
                        tcs.SetResult(null);
                    }

                    await tcs.Task.TimeoutAfter(TimeSpan.FromSeconds(10));
                },
                new[]
                {
                    CreateSecret("Secret1", "Value1"),
                    CreateSecret("Secret2", "Value2")
                }
            );
            var options = new AzureKvConfigurationOptions
            {
                Client = client.Object
            };

            // Act
            var provider = new AzureKvConfigurationProvider(options);
            provider.Load();
            await tcs.Task;

            // Assert
            Assert.AreEqual("Value1", provider.Get("Secret1"));
            Assert.AreEqual("Value2", provider.Get("Secret2"));
        }

        [Test]
        public void LimitsMaxParallelism()
        {
            var expectedCount = 100;
            var currentParallel = 0;
            var maxParallel = 0;
            var client = new Mock<SecretClient>();

            // Create 10 pages of 10 secrets
            var pages = Enumerable.Range(0, 10).Select(a =>
                Enumerable.Range(0, 10).Select(b => CreateSecret("Secret" + (a * 10 + b), (a * 10 + b).ToString())).ToArray()
            ).ToArray();

            SetPages(client,
                async (string id) =>
                {
                    var i = Interlocked.Increment(ref currentParallel);

                    maxParallel = Math.Max(i, maxParallel);

                    await Task.Delay(30);
                    Interlocked.Decrement(ref currentParallel);
                },
                pages
            );

            var options = new AzureKvConfigurationOptions
            {
                Client = client.Object
            };

            // Act
            var provider = new AzureKvConfigurationProvider(options);

            provider.Load();

            // Assert
            for (int i = 0; i < expectedCount; i++)
            {
                Assert.AreEqual(i.ToString(), provider.Get("Secret" + i));
            }

            Assert.LessOrEqual(maxParallel, 32);
        }

        [Test]
        public void ConstructorThrowsForNullManager()
        {
            Assert.Throws<ArgumentNullException>(() => new AzureKvConfigurationProvider(null));
        }

        [Test]
        public void ConstructorThrowsForNullValueOfClient()
        {
            var options = new AzureKvConfigurationOptions
            {
                Client = null
            };

            Assert.Throws<ArgumentNullException>(() => new AzureKvConfigurationProvider(options));
        }

        [Test]
        public void ConstructorThrowsForNullValueOfKeyVaultSecretNameEncoder()
        {
            var options = new AzureKvConfigurationOptions
            {
                Client = Mock.Of<SecretClient>(),
                KeyVaultSecretNameEncoder = null
            };

            Assert.Throws<ArgumentNullException>(() => new AzureKvConfigurationProvider(options));
        }

        [Test]
        public void ConstructorThrowsForNullValueOfUploadAndMapKeys()
        {
            var options = new AzureKvConfigurationOptions
            {
                Client = Mock.Of<SecretClient>(),
                VaultSecrets = null
            };

            Assert.NotNull(new AzureKvConfigurationProvider(options));
        }

        [Test]
        public void ConstructorThrowsForNullValueOfUploadKeyList()
        {
            var options = new AzureKvConfigurationOptions
            {
                Client = Mock.Of<SecretClient>(),
                VaultSecrets = null
            };

            Assert.NotNull(new AzureKvConfigurationProvider(options));
        }


        private class ReloadControlKeyVaultProvider : AzureKvConfigurationProvider
        {
            private TaskCompletionSource<object> _releaseTaskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            private TaskCompletionSource<object> _signalTaskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            public ReloadControlKeyVaultProvider(AzureKvConfigurationOptions options) : base(options)
            {
            }

            protected override async Task WaitForReload()
            {
                _signalTaskCompletionSource.SetResult(null);
                await _releaseTaskCompletionSource.Task.TimeoutAfter(TimeSpan.FromSeconds(10));
            }

            public async Task Wait()
            {
                await _signalTaskCompletionSource.Task.TimeoutAfter(TimeSpan.FromSeconds(10));
            }

            public void Release()
            {
                if (!_signalTaskCompletionSource.Task.IsCompleted)
                {
                    throw new InvalidOperationException("Provider is not waiting for reload");
                }

                var releaseTaskCompletionSource = _releaseTaskCompletionSource;
                _releaseTaskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                _signalTaskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                releaseTaskCompletionSource.SetResult(null);
            }
        }
    }
}
