using OrchestrationService.Contracts;
using OrchestrationService.Logger;
using OrchestrationService.OverlayNetworkStore.Exceptions;
using System.Text.Json;

namespace OrchestrationService.OverlayNetworkStore;

public class FileOverlayNetworkSubnetStore
{
    public FileOverlayNetworkSubnetStore()
    {
        _logger = OverlayNetworkLoggerProvider.GetLogger(nameof(FileOverlayNetworkSubnetStore));
    }

    public async Task<bool> WriteSubnetMetadataToDb(Subnet subnet)
    {

        // parse the address into a desired path
        var subnetFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "WireguardDb", subnet.TenantName);
        Directory.CreateDirectory(subnetFolderPath);

        var subnetFullPath = Path.Combine(subnetFolderPath, "subnetMetadata.json");

        if (File.Exists(subnetFullPath))
        {
            _logger.LogInformation($"{nameof(WriteSubnetMetadataToDb)}: Could not write subnet since it already exists: {subnet.TenantName}, {nameof(subnetFullPath)}: {subnetFullPath}");
            throw new SubnetAlreadyExistsException("Subnet already exists");
        }

        await File.WriteAllTextAsync(subnetFullPath, JsonSerializer.Serialize(subnet));
        _logger.LogInformation($"{nameof(WriteSubnetMetadataToDb)}: Wrote tubnet to the db: {nameof(subnetFullPath)}: {subnetFullPath}");
        return true;
    }

    public async Task<Subnet> GetSubnetMetadataFromDb(string tenantName)
    {
        // parse the address into a desired path
        var subnetFullPath = Path.Combine(Directory.GetCurrentDirectory(), "WireguardDb", tenantName, "subnetMetadata.json");

        if (!File.Exists(subnetFullPath))
        {
            _logger.LogInformation($"{nameof(GetSubnetMetadataFromDb)}: Could not find subnet with the following {nameof(tenantName)}: {tenantName}, {nameof(subnetFullPath)}: {subnetFullPath}");
            throw new SubnetNotFoundException($"Could not find Subnet named {tenantName}");
        }

        using (var r = new StreamReader(subnetFullPath))
        {
            var text = await r.ReadToEndAsync();
            return JsonSerializer.Deserialize<Subnet>(text);
        }
    }

    private readonly ILogger _logger;
}
