using OrchestrationService.Contracts;
using OrchestrationService.Logger;
using OrchestrationService.OverlayNetworkStore.Exceptions;
using System.Net;
using System.Text.Json;

namespace OrchestrationService.OverlayNetworkStore;

public class FileOverlayNetworkAddressHandler
{
    public FileOverlayNetworkAddressHandler(string rootPath, Subnet subnet)
    {
        _subnet = subnet;
        _logger = OverlayNetworkLoggerProvider.GetLogger(nameof(FileOverlayNetworkAddressHandler));
        _addressesPath = Path.Combine(rootPath, "addresses");

        Directory.CreateDirectory(_addressesPath);
    }

    public void DeleteKnownAddress(int[] address)
    {
        _logger.LogInformation($"{nameof(DeleteKnownAddress)}:Deleting address: {JsonSerializer.Serialize(address)}, tenantName: {_subnet.TenantName}");
        var addressFullPath = Path.Combine(GetScopedPath(address, 3), $"{address[3]}.json");
        File.Delete(addressFullPath);
    }

    public async Task<Peer?> GetPeerAsync(int[] address)
    {
        _logger.LogInformation($"{nameof(GetPeerAsync)}: Getting peer: {JsonSerializer.Serialize(address)}, tenantName: {_subnet.TenantName}");
        var addressFullPath = Path.Combine(GetScopedPath(address, 3), $"{address[3]}.json");
        if (!File.Exists(addressFullPath))
        {
            _logger.LogInformation($"{nameof(GetPeerAsync)}: peer not found: {JsonSerializer.Serialize(address)}, tenantName: {_subnet.TenantName}");
            throw new PeerNotFoundException($"Could not find the following address, {string.Join('.', address)}");
        }


        using (var r = new StreamReader(addressFullPath))
        {
            var text = await r.ReadToEndAsync();
            return JsonSerializer.Deserialize<Peer>(text);

        }
    }

    public async Task<bool> AssignKnownAddressAsync(int[]? address, Peer peer)
    {
        _logger.LogInformation($"{nameof(AssignKnownAddressAsync)}: Assigning known address to peer: {JsonSerializer.Serialize(address)}, tenantName: {_subnet.TenantName}");
        // parse the address into a desired path
        var addressFullPath = Path.Combine(GetScopedPath(address, 3), $"{address[3]}.json");

        if (!File.Exists(addressFullPath))
        {
            await File.WriteAllTextAsync(addressFullPath, JsonSerializer.Serialize(peer));
            return true;
        }

        return false;
    }


    /// <summary>
    /// Finds a new free address which can be allocated
    /// </summary>
    /// <returns>A bool specifying whether the action succeeded or not & the address in case of success</returns>
    public (bool, int[]?) FindNewAddress()
    {
        _logger.LogInformation($"{nameof(FindNewAddress)}: Finding a new address to peer, tenantName: {_subnet.TenantName}");

        var minAddress = (int[])_subnet.MinAddress.Clone();
        var rootPath = GetScopedPath(minAddress, _subnet.AddressSpace / 8);
        Directory.CreateDirectory(rootPath);

        return FindNewAddressInternal(minAddress, _subnet.AddressSpace / 8);
    }


    private (bool, int[]?) FindNewAddressInternal(int[] scope, int depth, string parentCorrelationId = "")
    {
        // Creating a correlation id for logging
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogTrace($"{nameof(FindNewAddressInternal)}: Finding a new address to peer, " +
            $"{nameof(scope)}: {scope}, {nameof(depth)}: {depth}, {nameof(correlationId)}: {correlationId}," +
            $"{nameof(parentCorrelationId)}: {parentCorrelationId}, {_subnet.TenantName}");

        if (depth == 3)
        {
            var path = GetScopedPath(scope, 3);
            Directory.CreateDirectory(path);
            (var success, var selectedAddressSuffix) = CheckAvailableAddressFileInFolderAsync(path, correlationId);
            if (success)
            {
                scope[3] = selectedAddressSuffix;
                return (success, scope);
            }

            // If we're at min depth, this means that we're at a leaf and that we have dug all possibilties..
            if (depth == _subnet.AddressSpace / 8)
            {
                throw new SubnetIsFullException();
            }

            return (false, null);
        }

        var currentStartFolder = 0;
        var numberOfRelevantFolders = 255;
        // If current depth is the same as the addressSpace / 8, we don't need to start from the beggining: e.g. 10.0.4.0 / 22 - we need to start from the 4
        // Else, we are in a deeper depth, meaning we need to start from 0
        if (depth == _subnet.AddressSpace / 8)
        {
            currentStartFolder = scope[(_subnet.AddressSpace / 8)];

            // calcualte the number of relevant folders: e.g. 10.0.4.0 / 22 => 10.0.7.255, only 4-7 matter.
            if (numberOfRelevantFolders % 8 != 0)
            {
                numberOfRelevantFolders = 1 << (8 - (_subnet.AddressSpace % 8));

                // -1 because it should end with .255 
                numberOfRelevantFolders--;
            }
        }

        foreach (var i in Enumerable.Range(currentStartFolder, currentStartFolder + numberOfRelevantFolders))
        {
            // Combine it to 10.0.4, 5, 6 etc...
            scope[depth] = i;
            _logger.LogTrace($"{nameof(FindNewAddressInternal)}: Digging deeper in depth, " +
                $"{nameof(scope)}: {scope}, {nameof(depth)}: {depth}, {nameof(correlationId)}: {correlationId}," +
                $"{nameof(parentCorrelationId)}: {parentCorrelationId}, {_subnet.TenantName}");
            (var success, var alloctedSuffix) = FindNewAddressInternal(scope, depth + 1, correlationId);
            if (success)
            {
                _logger.LogTrace($"{nameof(FindNewAddressInternal)}: Found an address suffix which is free! {alloctedSuffix} " +
                    $"{nameof(scope)}: {scope}, {nameof(depth)}: {depth}, {nameof(correlationId)}: {correlationId}," +
                    $"{nameof(parentCorrelationId)}: {parentCorrelationId}, {_subnet.TenantName}");
                return (true, alloctedSuffix);
            }
        }

        // If we reached so far, and we're in the minimum depth, this means we have already dug into all possibilities.
        if (depth == _subnet.AddressSpace / 8)
        {
            throw new SubnetIsFullException();
        }

        return (false, null);
    }

    private string GetScopedPath(int[] scope, int depth)
    {
        var currentPath = _addressesPath;
        for (int i = 0; i < depth; i++)
        {
            currentPath = Path.Combine(currentPath, $"{scope[i]}");
        }
        return currentPath;
    }

    /// <summary>
    /// Takes care of a specific folder and checks whether an address can be allocated in it.
    /// </summary>
    /// <param name="scope">The path to the directory representing the current scope. e.g. C:/10.0.0</param>
    /// <returns></returns>
    private (bool, int) CheckAvailableAddressFileInFolderAsync(string scope, string correlationId)
    {
        _logger.LogTrace($"{nameof(CheckAvailableAddressFileInFolderAsync)}: Finding a new address to peer, {nameof(scope)}: {scope}, {nameof(correlationId)}: {correlationId}, {_subnet.TenantName}");
        var files = Directory.GetFiles(scope);
        if (files.Length == 127)
        {
            return (false, -1);
        }

        var minAddressSuffix = 0;

        // Get all addresses in use.
        var allAddressesInUse = files
            .Select(Path.GetFileNameWithoutExtension)
            .Select(add => {
                var success = Int32.TryParse(add, out var res);
                return success ? res : -1;
            })
            .Where(add => add != -1);

        // Get all possible addresses in the given scope, at most 255 addresses.
        var allAddressInScope = Enumerable.Range(minAddressSuffix, Math.Min(255, minAddressSuffix + _subnet.NumberOfAddresses));
        var availableAddresses = allAddressInScope.Where(add => !allAddressesInUse.Contains(add));

        _logger.LogTrace($"{nameof(CheckAvailableAddressFileInFolderAsync)}: Finding a new address to peer, {nameof(scope)}: {scope}, " +
            $"{nameof(availableAddresses)}Count: {availableAddresses.Count()}" +
            $"{nameof(correlationId)}: {correlationId}, {_subnet.TenantName}");
        if (!availableAddresses.Any())
        {
            return (false, -1);
        }

        // In this case, we found an available address, assign it by creating a file.
        var selectedAddress = availableAddresses.First();

        return (true, selectedAddress);
    }

    private readonly string _addressesPath;
    private readonly Subnet _subnet;
    private readonly ILogger _logger;
}
