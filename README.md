# Azure Key Vault Secrets configuration provider for Microsoft.Extensions.Configuration

The AspNetCore.Azure.Configuration.KvSecrets based on [Azure.Extensions.AspNetCore.Configuration.Secrets][source] 
which package allows storing configuration values using Azure Key Vault Secrets.

## Improvements

- Allows to load secrets by list and map them into new names.
- Allows to load  secrets into the configuration section.

## Getting started

### Install the package

Install the package with [DotNetCore.Azure.Configuration.KvSecrets](https://www.nuget.org/packages/DotNetCore.Azure.Configuration.KvSecrets):

```Powershell
    dotnet add package DotNetCore.Azure.Configuration.KvSecrets
```

### Prerequisites

You need an [Azure subscription][azure_sub] and
[Azure Key Vault][keyvault_doc] to use this package.

To create a new Key Vault, you can use the [Azure Portal][keyvault_create_portal],
[Azure PowerShell][keyvault_create_ps], or the [Azure CLI][keyvault_create_cli].
Here's an example using the Azure CLI:

```Powershell
az keyvault create --name MyVault --resource-group MyResourceGroup --location westus
az keyvault secret set --vault-name MyVault --name MySecret --value "hVFkk965BuUv"
```

## Examples

To load initialize configuration from Azure Key Vault secrets call the `AddAzureKeyVault` on `ConfigurationBuilder`:

**Program.cs**

```C# 
    public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureAppConfiguration(Startup.AddKvConfigurations);
                    webBuilder.UseStartup<Startup>();
                });
```

**Startup.cs**

```C# 
        public static void AddKvConfigurations(WebHostBuilderContext hostingContext, IConfigurationBuilder configurationBuilder)
        {
            var configBuilder = new ConfigurationBuilder().AddInMemoryCollection();
            IHostEnvironment env = hostingContext.HostingEnvironment;
            configBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                  .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: false);
            configBuilder.AddEnvironmentVariables();

            var config = configBuilder.Build();

            string KeyVaultUrl = config[nameof(KeyVaultUrl)];
            List<string> VaultSecrets = config.GetSection(nameof(UploadKeyList)).Get<List<string>>();
            string ConfigurationSectionPrefix = config[nameof(ConfigurationSectionPrefix)];

            var credential = new AzureCliCredential();
            //var credential = new DefaultAzureCredential();
            var client = new SecretClient(vaultUri: new Uri(KeyVaultUrl), credential);
            var options = new AzureKvConfigurationOptions()
            {
                ConfigurationSectionPrefix = ConfigurationSectionPrefix,
                UploadKeyList = UploadKeyList
            };

            configurationBuilder.AddAzureKeyVault(client, options);
        }
```

**appsettings.json**

```JSON

  "ConfigurationSectionPrefix": "secret",
  "KeyVaultUrl": "https://secrets128654s235.vault.azure.net/",
  "VaultSecrets": [ "FuseEval--Demo8", "LoadInMess", "RealSecretForVault" ]

```

The [Azure Identity library][identity] provides easy Azure Active Directory support for authentication.

## Next steps

Read more about [configuration in ASP.NET Core][aspnetcore_configuration_doc].

## Contributing

This project welcomes contributions and suggestions.  Most contributions require
you to agree to a Contributor License Agreement (CLA) declaring that you have
the right to, and actually do, grant us the rights to use your contribution. For
details, visit [cla.microsoft.com][cla].

This project has adopted the [Microsoft Open Source Code of Conduct][coc].
For more information see the [Code of Conduct FAQ][coc_faq]
or contact [opencode@microsoft.com][coc_contact] with any
additional questions or comments.

![Impressions](https://azure-sdk-impressions.azurewebsites.net/api/impressions/azure-sdk-for-net%2Fsdk%2Fextensions%2FAzure.Extensions.AspNetCore.Configuration.Secrets%2FREADME.png)

<!-- LINKS -->
[source]: https://github.com/Azure/azure-sdk-for-net/tree/master/sdk/extensions/Azure.Extensions.AspNetCore.Configuration.Secrets/src
[package]: https://www.nuget.org/packages/Azure.Extensions.AspNetCore.Configuration.Secrets/
[docs]: https://docs.microsoft.com/dotnet/api/Azure.Extensions.AspNetCore.Configuration.Secrets
[nuget]: https://www.nuget.org/packages/Azure.Extensions.AspNetCore.Configuration.Secrets
[keyvault_create_cli]: https://docs.microsoft.com/azure/key-vault/quick-create-cli#create-a-key-vault
[keyvault_create_portal]: https://docs.microsoft.com/azure/key-vault/quick-create-portal#create-a-vault
[keyvault_create_ps]: https://docs.microsoft.com/azure/key-vault/quick-create-powershell#create-a-key-vault
[azure_cli]: https://docs.microsoft.com/cli/azure
[azure_sub]: https://azure.microsoft.com/free/
[identity]: https://github.com/Azure/azure-sdk-for-net/tree/master/sdk/identity/Azure.Identity/README.md
[aspnetcore_configuration_doc]: https://docs.microsoft.com/aspnet/core/fundamentals/configuration/?view=aspnetcore-3.1
[error_codes]: https://docs.microsoft.com/rest/api/storageservices/blob-service-error-codes
[cla]: https://cla.microsoft.com
[coc]: https://opensource.microsoft.com/codeofconduct/
[coc_faq]: https://opensource.microsoft.com/codeofconduct/faq/
[coc_contact]: mailto:opencode@microsoft.com
