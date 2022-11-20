namespace OrchestrationService.OverlayNetworkStore.Exceptions;

public class SubnetNotFoundException : Exception
{
    public SubnetNotFoundException(string message)
        : base(message)
    {
    }
}
