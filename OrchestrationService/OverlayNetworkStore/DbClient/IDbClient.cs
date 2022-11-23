namespace OrchestrationService.OverlayNetworkStore.DbClient;

public interface IDbClient
{
    public Task<bool> WriteFileAsync(string path, string content);

    public Task<string> ReadFromFileAsync(string path);

    public bool DeleteFile(string path);

    public IEnumerable<string> ListFiles(string path);

    public bool FileExists(string path);

    public bool InitDirectory(string path);

    public bool InitDirectoryInDb(string directoryName);

    public string GeneratePathInDb(string extension);

    public string AddExtensionToPath(string path, string extension);
}
