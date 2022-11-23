using OrchestrationService.Contracts;
using OrchestrationService.Controllers;
using OrchestrationService.OverlayNetworkStore.DbClient;

namespace OrchestrationService.OverlayNetworkStore;

public class FileOverlayNetworkStoreFactory
{
    public static FileOverlayNetworkStore GetOrCreateDiskStore()
    {
        if (_store == null)
        {
            lock (_storeLock)
            {
                // double dispatch to make sure no race condition
                if (_store == null)
                {
                    var dbClient = new DiskDbClient();
                    _store = new FileOverlayNetworkStore(dbClient);
                }
            }
        }

        return _store;
    }

    private static FileOverlayNetworkStore _store;
    private static object _storeLock = new object();
}
