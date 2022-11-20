using System.Collections;

namespace OrchestrationService.Contracts;

public class Subnet
{
    public Subnet()
    {

    }

    public Subnet(string tenantName, string minAddress, int addressSpace)
        : this(tenantName, StringToIntArrayAddressConverter(minAddress), addressSpace)
    {
    }

    public Subnet(string tenantName, int[] minAddress, int addressSpace)
    {
        MinAddress = minAddress;
        AddressSpace = addressSpace;
        TenantName = tenantName;

        Validate();
    }

    public int[] MinAddress { get; set; }

    public int AddressSpace { get; set; }

    public string TenantName { get; set; }

    public int NumberOfAddresses
    {
        get
        {
            return 1 << (32 - AddressSpace);
        }
    }

    public static int[] StringToIntArrayAddressConverter(string address)
    {
        return address.Split('.')
           .Where(add => Int32.TryParse(add, out var _))
           .Select(add => Int32.Parse(add))
           .ToArray();
    }

    /// <summary>
    /// Validates the internal values and checks whether they are compliant
    /// </summary>
    /// <exception cref="ArgumentException">In case an argument isn't compliant</exception>
    public void Validate()
    {
        if (MinAddress.Length != 4)
        {
            throw new ArgumentException($"Invalid minAddress length, received: {MinAddress.Length}, expected: 4, currretly only IPv4 is supported");
        }

        if (AddressSpace > 31 || AddressSpace < 1)
        {
            throw new ArgumentException($"Invalid addressSpace, received: {AddressSpace}, expected to be between 1 to 31");
        }
    }
}
