namespace OrchestrationService.OverlayNetworkStore.Exceptions;

public class TokenMismatchException : Exception
{
    public TokenMismatchException(string message)
        : base(message)
    {
    }
}
