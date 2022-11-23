using OrchestrationService.Contracts;
using System.Data.Common;
using System.Text.Json;

namespace OrchestrationService.OverlayNetworkStore.DbClient;

public class DiskDbClient : IDbClient
{
    public DiskDbClient()
    {
        _dbPath = Path.Combine(Directory.GetCurrentDirectory(), DbName);
    }

    public async Task<bool> WriteFileAsync(string path, string content)
    {
        await File.WriteAllTextAsync(path, content);
        return true; 
    }

    public async Task<string> ReadFromFileAsync(string path)
    {
        using var r = new StreamReader(path);
        return await r.ReadToEndAsync();
    }

    public bool DeleteFile(string path)
    {
        File.Delete(path);
        return true;
    }

    public IEnumerable<string> ListFiles(string path)
    {
        return Directory.GetFiles(path);
    }

    public bool FileExists(string path)
    {
        return File.Exists(path);
    }

    public bool InitDirectory(string path)
    {
        Directory.CreateDirectory(path);
        return true;
    }

    public bool InitDirectoryInDb(string directoryName)
    {
        var path = GeneratePathInDb(directoryName);
        return InitDirectory(path);
    }

    public string GeneratePathInDb(string extension)
    {
        return Path.Combine(_dbPath, extension);
    }

    public string AddExtensionToPath(string path, string extension)
    {
        return Path.Combine(path, extension);
    }

    private readonly string _dbPath;
    private const string DbName = "WireguardDb";
}
