using OrchestrationService.Contracts;
using OrchestrationService.Controllers;

namespace OrchestrationService.OverlayNetworkStore;

public class FileOverlayNetworkStoreFactory
{
    public static FileOverlayNetworkStore GetOrCreateStore()
    {
        if (_store == null)
        {
            lock (_storeLock)
            {
                // double dispatch to make sure no race condition
                if (_store == null)
                {
                    _store = new FileOverlayNetworkStore();
                }
            }
        }

        return _store;
    }

    private static FileOverlayNetworkStore _store;
    private static object _storeLock = new object();
}
