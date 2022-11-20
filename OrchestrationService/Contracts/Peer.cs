using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace OrchestrationService.Contracts;

public class Peer
{
    /// <summary>
    /// Empty ctor for Json Construction
    /// </summary>
    public Peer()
    {
    }

    public Peer(string publicKey)
    {
        Token = Guid.NewGuid().ToString();
        PublicKey = publicKey;
    }

    public string PublicKey { get; set; }

    /// <summary>
    /// Token is used to verify the source authenticity between different api calls
    /// </summary>
    public string Token { get; set; }

    public int[]? PrivateIp { get; set; }

    [JsonIgnore]
    public Subnet? Subnet { get; set; }

    public string GetReadableAddress()
    {
        return PrivateIp == null ? null : string.Join('.', PrivateIp);
    }

}
