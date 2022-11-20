using OrchestrationService.Contracts;
using OrchestrationService.OverlayNetworkStore.Exceptions;
using System.Text.Json;

namespace OrchestrationService.OverlayNetworkStore;

public class FileOverlayNetworkSubnetStore
{
    public async Task<bool> WriteSubnetMetadataToDb(Subnet subnet)
    {
        // parse the address into a desired path
        var subnetFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "WireguardDb", subnet.TenantName);
        Directory.CreateDirectory(subnetFolderPath);

        var subnetFullPath = Path.Combine(subnetFolderPath, "subnetMetadata.json");

        if (File.Exists(subnetFullPath))
        {
            throw new SubnetAlreadyExistsException("Subnet already exists");
        }

        await File.WriteAllTextAsync(subnetFullPath, JsonSerializer.Serialize(subnet));

        return true;
    }

    public async Task<Subnet> GetSubnetMetadataFromDb(string tenantName)
    {
        // parse the address into a desired path
        var subnetFullPath = Path.Combine(Directory.GetCurrentDirectory(), "WireguardDb", tenantName, "subnetMetadata.json");

        if (!File.Exists(subnetFullPath))
        {
            throw new SubnetNotFoundException($"Could not find Subnet named {tenantName}");
        }

        using (var r = new StreamReader(subnetFullPath))
        {
            var text = await r.ReadToEndAsync();
            return JsonSerializer.Deserialize<Subnet>(text);
        }
    }
}
