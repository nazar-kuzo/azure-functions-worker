## [Azure App Configuration support for Azure Function Host](src/AzureFunctions.Worker.Extensions.AppConfiguration.Host/readme.md)
- Resolve host triggered connection strings via Azure App Configuration

## NuGet package
[https://www.nuget.org/packages/AzureFunctions.Worker.Extensions.AppConfiguration.Host](https://www.nuget.org/packages/AzureFunctions.Worker.Extensions.AppConfiguration.Host)

## Example

Host will connect to Azure App Configuration via managed identity credentials, so we have to provide
`APPCONFIG_ENDPOINT` environment variable to let host know that it should load configuration

launchSettings.json
```json
{
  "profiles": {
    "AzureFunctions.Worker": {
      "commandName": "Project",
      "commandLineArgs": "--port 7045",
      "launchBrowser": false,
      "environmentVariables": {
        "DOTNET_ENVIRONMENT": "Development",
        "APPCONFIG_ENDPOINT": "https://<app-config-domain>.azconfig.io" // <-- notifies host to load Azure App Configuration
      }
    }
  }
}
```
