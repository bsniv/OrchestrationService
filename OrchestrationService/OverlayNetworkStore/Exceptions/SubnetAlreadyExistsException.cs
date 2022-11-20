namespace OrchestrationService.OverlayNetworkStore.Exceptions;

public class SubnetAlreadyExistsException : Exception
{
    public SubnetAlreadyExistsException(string message)
        : base(message)
    {
    }
}
