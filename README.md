## DotNetCore Azure Configuration Key Vault Secrets

The DotNetCore.Azure.Configuration.KvSecrets based on [Azure.Extensions.AspNetCore.Configuration.Secrets][source].
## Improvements
- Allows storing configuration values using Azure Key Vault Secrets.
- Allows to load secrets by list and map them into new names.
- Allows to load  secrets into the configuration section.

## Getting started

### Install the package

Install the package with [DotNetCore.Azure.Configuration.KvSecrets](https://www.nuget.org/packages/DotNetCore.Azure.Configuration.KvSecrets):

**Version 10.x.x** : **supports only .NET 10.0**


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

## .NetCore Microservice Examples

Can be used in conjunction with [DotNetCore Azure Configuration KeyVault Certificates](https://github.com/Wallsmedia/DotNetCore.Azure.Configuration.KvCertificates).


Add configuration provider with WebHostBuiler initialization.

**Program.cs**

```C# 
      var builder = WebApplication.CreateBuilder(args);
      builder.AddKeyVaultConfigurationProvider();      
```


**StartupExt.cs**

Used DotNetCore Configuration Templates to inject secrets into Microservice configuration.
(Add to project nuget package DotNetCore.Configuration.Formatter.)

```C# 
  public static void AddKeyVaultConfigurationProvider(this WebApplicationBuilder builder)
    {

        var credential = new DefaultAzureCredential(
            new DefaultAzureCredentialOptions()
            {
                ExcludeSharedTokenCacheCredential = true,
                ExcludeVisualStudioCodeCredential = true,
                ExcludeVisualStudioCredential = true,
                ExcludeInteractiveBrowserCredential = true
            });

        var optionsKv = builder.Configuration
                           .GetTypeNameFormatted<AzureKvConfigurationOptions>();

        // Adds Azure Key Valt configuration provider.
        builder.Configuration.AddAzureKeyVault(credential, optionsKv);
    }
```


**appsettings.json**

```JSON
"AzureKvConfigurationOptions": {
  "ConfigurationSectionPrefix": "secret",
  "VaultUri": "https://secrets128654s235.vault.azure.net/",
  "VaultSecrets": [ 
    "service-bus-Developement-connection",
    "sql-Developement-password",
    "sql-Developement-user"
    "service-bus-Production-connection",
    "sql-Production-password",
    "sql-Production-user" ]
    }
```

The [Azure Identity library][identity] provides easy Azure Active Directory support for authentication.

Read more about [configuration in ASP.NET Core][aspnetcore_configuration_doc].

## Example with DotNetCore Configuration Templates


Use [DotNetCore Configuration Templates](https://github.com/Wallsmedia/DotNetCore.Configuration.Formatter) 
to inject secrets into Microservice configuration.

Add to project nuget package [DotNetCore.Configuration.Formatter](https://www.nuget.org/packages/DotNetCore.Configuration.Formatter/).



##### Environment Variables set to :

```
DOTNET_RUNNING_IN_CONTAINER=true
ASPNETCORE_ENVIRONMENT=Development
...
host_environmet=datacenter
```


##### Microservice has the ApplicationConfiguration.cs

``` CSharp

public class ApplicationConfiguration 
{
     public bool IsDocker {get; set;}
     public string RunLocation {get; set;}
     public string AppEnvironment {get; set;}
     public string BusConnection {get; set;}
     public string DbUser {get; set;}
     public string DbPassword {get; set;}
}
```

##### Microservice has the following appsettings.json:

``` JSON 
{
"AzureKvConfigurationOptions": {
  "ConfigurationSectionPrefix": "secret",
  "VaultUri": "https://secrets128654s235.vault.azure.net/",
  "VaultSecrets": [ 
    "service-bus-Development-connection",
    "sql-Development-password",
    "sql-Development-user",
    "service-bus-Production-connection",
    "sql-Production-password",
    "sql-Production-user" ]
    }

  ApplicationConfiguration:{
     "IsDocker": "{DOTNET_RUNNING_IN_CONTAINER??false}",
     "RunLocation":"{host_environmet??local}",
     "AppEnvironment":"{ENVIRONMENT}",
     "BusConnection":"{secret:service-bus-{ENVIRONMENT}-connection}",
     "DbPassword":"{secret:sql-{ENVIRONMENT}-password}",
     "DbUser":"{secret:sql-{ENVIRONMENT}-user}"
  }
}
```

##### Microservice the Startup.cs


``` CSharp

     var applicationConfig = Configuration.UseFormater()
     .GetSection(nameof(ApplicationConfiguration))
     .Get<ApplicationConfiguration>();
  ```
   


or with **shorthand** 

``` CSharp

     var applicationConfig = Configuration.GetTypeNameFormatted<ApplicationConfiguration>();

```





<!-- LINKS -->
[source]: https://github.com/Azure/azure-sdk-for-net/tree/master/sdk/extensions/Azure.Extensions.AspNetCore.Configuration.Secrets/src
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
