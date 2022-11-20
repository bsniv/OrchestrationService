namespace OrchestrationService.OverlayNetworkStore.Exceptions;

public class SubnetIsFullException : Exception
{
    public SubnetIsFullException()
        : base("Subnet is full")
    {
    }
}
