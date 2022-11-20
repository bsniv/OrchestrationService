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
            return 1 << AddressSpace;
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

    /*
    public int[] GetReadableMinAddress()
    {
        return BitArrayToInts((BitArray)MinAddress.Clone());
    }

    /*
    /// <summary>
    /// Returns the minimum number of a valid scpope.
    /// </summary>
    /// <returns></returns>
    public int[] GetMinAddress(int[] scope)
    {
        var scopeBits = IntsToBitArray(scope);
        
        var minAddress = MinAddress;
        var res = scopeBits.Or(minAddress);
        if (CompareBitArrays(scopeBits, minAddress) > 0)
        {
            throw new ArgumentException("scope is outside of the address space");
        }


        return BitArrayToInts(res);
    }

    private static int CompareBitArrays(BitArray first, BitArray second)
    {
        if (first.Length != second.Length)
        {
            throw new ArgumentException("Comparing only the same size bit arrays is allowed");
        }

        for (int i = 0; i < first.Length; i++)
        {
            if (first[i] != second[i])
            {
                // if they are different, and first is true (1) than first is bigger.
                return first[i] ? 1 : -1;
            }
        }

        return 0;
    }

    private static long GetNumberOfAddressesInScope(int[] currentScope)
    {
        var currentScopeBitArray = IntsToBitArray(currentScope);
        
        return 0;
    }

    private static BitArray IntsToBitArray(int[] address)
    {
        var bitArray = new BitArray(32);

        for (var i = 0; i < address.Length; i++)
        {
            if (address[i] > 255 | address[i] < 0)
            {
                throw new ArgumentException($"Invalid address, received {address[i]}, expected: 0-127");
            }

            var currentBits = new BitArray(new int[] { address[i] });
            currentBits.LeftShift(i * 8);
            bitArray.Or(currentBits);
        }

        return bitArray;
    }

    private static int[] BitArrayToInts(BitArray bitArray)
    {
        if (bitArray.Length % 8 != 0)
        {
            throw new ArgumentException("Invalid array size, must be divisable by 8");
        }

        var res = new int[bitArray.Length / 8];
        for (var i = 0; i < bitArray.Length / 8; i++)
        {
            var currentInt = 0;
            for (var j=0; j < 8; j++)
            {
                var toAdd = bitArray[i * 8 + j] ? 1 : 0;
                var multiplier = 1 << j;
                currentInt += multiplier * toAdd;
            }

            res[i] = currentInt;
        }

        return res;
    }*/
}
