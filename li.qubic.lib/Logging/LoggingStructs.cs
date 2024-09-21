using System.Runtime.InteropServices;



namespace li.qubic.lib.Logging
{

    [StructLayout(LayoutKind.Sequential)]
    public struct RequestLog
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public ulong[] passcode; // 4-element array of unsigned long long (ulong in C#)

        public ulong fromID; // unsigned long long in C++
        public ulong toID; // unsigned long long in C++

        public const int type = 44; // enum type in C++ translated to const in C#
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RespondLog
    {
        public const int type = 45; // enum type in C++
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RequestLogIdRangeFromTx
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public ulong[] passcode; // 4-element array of unsigned long long (ulong in C#)

        public uint tick; // unsigned int in C++
        public uint txId; // unsigned int in C++

        public const int type = 48; // enum type in C++
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ResponseLogIdRangeFromTx
    {
        public long fromLogId; // long long in C++
        public long length; // long long in C++

        public const int type = 49; // enum type in C++
    }



    [StructLayout(LayoutKind.Sequential)]
    public struct QuTransfer
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] sourcePublicKey;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] destinationPublicKey;

        public long amount;
        //public char _terminator; // Only data before "_terminator" are logged
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct AssetIssuance
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] issuerPublicKey;

        public long numberOfShares;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 7)]
        public string name;

        public char numberOfDecimalPlaces;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 7)]
        public string unitOfMeasurement;

        //public char _terminator; // Only data before "_terminator" are logged
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct AssetOwnershipChange
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] sourcePublicKey;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] destinationPublicKey;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] issuerPublicKey;

        public long numberOfShares;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 7)]
        public string name;

        public char numberOfDecimalPlaces;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 7)]
        public string unitOfMeasurement;

        //public char _terminator; // Only data before "_terminator" are logged
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct AssetPossessionChange
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] sourcePublicKey;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] destinationPublicKey;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] issuerPublicKey;

        public long numberOfShares;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 7)]
        public string name;

        public char numberOfDecimalPlaces;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 7)]
        public string unitOfMeasurement;

        //public char _terminator; // Only data before "_terminator" are logged
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DummyContractErrorMessage
    {
        public uint _contractIndex; // Auto-assigned, any previous value will be overwritten
        public uint _type; // Assign a random unique (per contract) number to distinguish messages of different types

        //public char _terminator; // Only data before "_terminator" are logged
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DummyContractWarningMessage
    {
        public uint _contractIndex; // Auto-assigned, any previous value will be overwritten
        public uint _type; // Assign a random unique (per contract) number to distinguish messages of different types

        //public char _terminator; // Only data before "_terminator" are logged
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DummyContractInfoMessage
    {
        public uint _contractIndex; // Auto-assigned, any previous value will be overwritten
        public uint _type; // Assign a random unique (per contract) number to distinguish messages of different types

        //public char _terminator; // Only data before "_terminator" are logged
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DummyContractDebugMessage
    {
        public uint _contractIndex; // Auto-assigned, any previous value will be overwritten
        public uint _type; // Assign a random unique (per contract) number to distinguish messages of different types

        //public char _terminator; // Only data before "_terminator" are logged
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DummyCustomMessage
    {
        public ulong _type; // Assign a random unique number to distinguish messages of different types

        //public char _terminator; // Only data before "_terminator" are logged
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Burning
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] sourcePublicKey;

        public long amount;

        //public char _terminator; // Only data before "_terminator" are logged
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DustBurning
    {
        public ushort numberOfBurns;

        [StructLayout(LayoutKind.Sequential)]
        public struct Entity
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] publicKey;
            public ulong amount;
        }

        // Since C# doesn't support static_assert, we can't directly enforce the size of `Entity`.
        // Ensure the size externally if needed.
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SpectrumStats
    {
        public ulong totalAmount;
        public ulong dustThresholdBurnAll;
        public ulong dustThresholdBurnHalf;
        public uint numberOfEntities;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 48)]
        public uint[] entityCategoryPopulations;
    }



}
