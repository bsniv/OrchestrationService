using OrchestrationService.Contracts;
using OrchestrationService.Logger;
using OrchestrationService.OverlayNetworkStore.DbClient;
using OrchestrationService.OverlayNetworkStore.Exceptions;
using System.Text.Json;

namespace OrchestrationService.OverlayNetworkStore;

public class FileOverlayNetworkSubnetStore
{
    public FileOverlayNetworkSubnetStore(IDbClient dbClient)
    {
        _logger = OverlayNetworkLoggerProvider.GetLogger(nameof(FileOverlayNetworkSubnetStore));
        _dbClient = dbClient;
    }

    public async Task<bool> WriteSubnetMetadataToDb(Subnet subnet)
    {
        _dbClient.InitDirectoryInDb(subnet.TenantName);
        var subnetFolderPath = _dbClient.GeneratePathInDb(subnet.TenantName);
        var subnetFullPath = _dbClient.AddExtensionToPath(subnetFolderPath, "subnetMetadata.json");

        if (_dbClient.FileExists(subnetFullPath))
        {
            _logger.LogInformation($"{nameof(WriteSubnetMetadataToDb)}: Could not write subnet since it already exists: {subnet.TenantName}, {nameof(subnetFullPath)}: {subnetFullPath}");
            throw new SubnetAlreadyExistsException("Subnet already exists");
        }

        var success = await _dbClient.WriteFileAsync(subnetFullPath, JsonSerializer.Serialize(subnet));
        _logger.LogInformation($"{nameof(WriteSubnetMetadataToDb)}: Writing subnet to the db: {nameof(subnetFullPath)}: {subnetFullPath} finished with {nameof(success)}:{success}");
        return true;
    }

    public async Task<Subnet> GetSubnetMetadataFromDb(string tenantName)
    {
        _dbClient.InitDirectoryInDb(tenantName);
        var subnetFolderPath = _dbClient.GeneratePathInDb(tenantName);
        var subnetFullPath = _dbClient.AddExtensionToPath(subnetFolderPath, "subnetMetadata.json");

        if (!_dbClient.FileExists(subnetFullPath))
        {
            _logger.LogInformation($"{nameof(GetSubnetMetadataFromDb)}: Could not find subnet with the following {nameof(tenantName)}: {tenantName}, {nameof(subnetFullPath)}: {subnetFullPath}");
            throw new SubnetNotFoundException($"Could not find Subnet named {tenantName}");
        }

        var subnetText = await _dbClient.ReadFromFileAsync(subnetFullPath);
        return string.IsNullOrWhiteSpace(subnetText) ? null : JsonSerializer.Deserialize<Subnet>(subnetText);
    }

    private readonly ILogger _logger;
    private readonly IDbClient _dbClient;
}
