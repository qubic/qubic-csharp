using li.qubic.lib.Helper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace li.qubic.lib
{

    #region Broadcast Packages
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct FirstRequest
    {
        public RequestResponseHeader header;
        public ExchangePublicPeers payload;

    };

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct BroadcastedSolutionMessage
    {
        public RequestResponseHeader header;
        public Message message;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] solutionNonce;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] signature;
    };


    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct RequestResponseHeader
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        private byte[] _size = new byte[3];
        private byte _type = 0;
        private int _dejavu = 0;


        public RequestResponseHeader(bool randomize = false)
        {
            _size = new byte[3];
            _dejavu = 0;
            _type = 0;
            if (randomize)
            {
                this.Randomize();
            }
            else
            {
                this.EmptyDejavu();
            }
        }


        public short type
        {
            get
            {
                return _type;
            }
            set
            {
                _type = (byte)value;
            }
        }

        public int size
        {
            get
            {
                //return (*((uint*)_size)) & 0xFFFFFF;
                if (_size == null)
                    _size = new byte[3];
                return (_size[0] | (_size[1] << 8) | (_size[2] << 16));
            }
            set
            {
                if (_size == null)
                    _size = new byte[3];
                _size[0] = (byte)value;
                _size[1] = (byte)(value >> 8);
                _size[2] = (byte)(value >> 16);
            }
        }

        public int dejavu
        {
            get
            {
                return _dejavu;
            }
            set
            {
                _dejavu = value;
            }
        }

        public void EmptyDejavu()
        {
            _dejavu = 0;
        }

        public void RandomizeDejavu()
        {
            _dejavu = new Random().Next();
        }

        [Obsolete]
        public void Randomize()
        {
            RandomizeDejavu();
        }

    };

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct BroadcastedSolutionTransaction
    {
        public RequestResponseHeader header;
        public SolutionTransaction transfer;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct BroadcastedTransfer
    {
        public RequestResponseHeader header;
        public SignedTransaction transfer;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BroadcastedComputors
    {
        public RequestResponseHeader header;
        public BroadcastComputors broadcastComputors;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RequestedComputors
    {
        public RequestResponseHeader header;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BroadcastRequestedQuorumTick
    {
        public RequestResponseHeader header;
        public RequestQuorumTick requestQuorumTick;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct BroadcastRequestTickData
    {
        public RequestResponseHeader header;
        public RequestTickData requestTickData;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct BroadcastRequestedTickTransactions
    {
        public RequestResponseHeader header;
        public RequestedTickTransactions requestedTickTransaction;
    };


    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct BroadcastRequestedEntity
    {
        public RequestResponseHeader header;
        public RequestedEntity requestedEntity;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct BroadcastRequestedExecutedTx
    {
        public RequestResponseHeader header;
        public RequestedExecutedTx requestedExecutedTx;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct BroadcastRequestedTickStats
    {
        public RequestResponseHeader header;
        public RequestedTickStats requestTickStats;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct BroadcastedTick
    {
        public RequestResponseHeader header;
        public Tick tick;

    };

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct BroadcastedTickData
    {
        public RequestResponseHeader header;
        public TickData tickData;

    };

    #endregion

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct ComputorProposal
    {
        public byte uriSize;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 255)]
        public byte[] uri;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct ComputorBallot
    {
        public byte zero;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (676 * 3 + 7) / 8)]
        public byte[] votes;
        public byte quasiRandomNumber;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct Solution
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] computorPublicKey;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] nonce;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct SpecialCommand
    {
        public ulong everIncreasingNonceAndCommandType;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct SpecialCommandShutdownRequest
    {
        public ulong everIncreasingNonceAndCommandType;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] signature;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct SpecialCommandGetProposalAndBallotRequest
    {
        public ulong everIncreasingNonceAndCommandType;
        public ushort computorIndex;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] padding;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] signature;
    };


    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct SpecialCommandGetProposalAndBallotResponse
    {
        public ulong everIncreasingNonceAndCommandType;
        public ushort computorIndex;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] padding;
        public ComputorProposal proposal;
        public ComputorBallot ballot;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct SpecialCommandSetProposalAndBallotRequest
    {
        public ulong everIncreasingNonceAndCommandType;
        public ushort computorIndex;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] padding;
        public ComputorProposal proposal;
        public ComputorBallot ballot;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] signature;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct SpecialCommandSetProposalAndBallotResponse
    {
        public ulong everIncreasingNonceAndCommandType;
        public ushort computorIndex;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] padding;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct QubicSystemStruct
    {
        public short version;
        public ushort epoch;
        public uint tick;
        public uint initialTick;
        public uint latestCreatedTick;
        public uint latestLedTick;

        public ushort epochBeginningMillisecond;
        public byte epochBeginningSecond;
        public byte epochBeginningMinute;
        public byte epochBeginningHour;
        public byte epochBeginningDay;
        public byte epochBeginningMonth;
        public byte epochBeginningYear;

        public ulong latestOperatorNonce;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 676)]
        public ComputorProposal[] proposals;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 676)]
        public ComputorBallot[] ballots;

        public uint numberOfSolutions;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 65536)]
        public Solution[] solutions;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 676 * 32)]
        public byte[] futureComputors;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 16)]
    public struct ExchangePublicPeers
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[,] peers;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Revenues
    {
        public ushort computorIndex;
        public ushort epoch;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 676)]
        public uint[] revenues;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] signature;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BroadcastRevenues
    {
        public Revenues revenues;
    };


    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct SpectrumEntity
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] publicKey;
        public long incomingAmount;
        public long outgoingAmount;
        public uint numberOfIncomingTransfers;
        public uint numberOfOutgoingTransfers;
        public uint latestIncomingTransferTick;
        public uint latestOutgoingTransferTick;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct TickData
    {
        public ushort computorIndex;
        public ushort epoch;
        public uint tick;

        public ushort millisecond;
        public byte second;
        public byte minute;
        public byte hour;
        public byte day;
        public byte month;
        public byte year;

        //[MarshalAs(UnmanagedType.ByValArray, SizeConst = ((676 - 1) * 10 + 7) / 8)]
        //public byte[] computorKPIs;

        //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 676)]
        //public uint[] revenues;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        private byte[] unionData;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] timelock;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024 /* NUMBER_OF_TRANSACTIONS_PER_TICK*/ * 32)]
        public byte[] transactionDigests; // two dimensions!!!

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024 /* #define MAX_NUMBER_OF_CONTRACTS 1024 // Must be 2^N */)]
        public ulong[] contractFees;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] signature;

        //  c++ structure
        //    union
        //    {
        //    struct
        //    {
        //        byte uriSize;
        //        byte uri[255];
        //    } proposal;
        //    struct
        //    {
        //        byte zero;
        //        byte votes[(NUMBER_OF_COMPUTORS * 3 + 7) / 8];
        //        byte quasiRandomNumber;
        //    } ballot;
        //} varStruct;

        public byte proposalUriSize
        {
            get
            {
                return this.unionData[0];
            }
            set
            {
                this.unionData[0] = value;
            }
        }

        public byte[] proposalUri
        {
            get
            {
                return this.unionData.Skip(1).Take(255).ToArray();
            }
            set
            {
                this.unionData.SetValue(value.Take(255).ToArray(), 1);
            }
        }


        public byte ballotZero
        {
            get
            {
                return this.unionData[0];
            }
            set
            {
                this.unionData[0] = value;
            }
        }

        public byte[] ballotVotes
        {
            get
            {
                return this.unionData.Skip(1).Take(254).ToArray();
            }
            set
            {
                this.unionData.SetValue(value.Take(254).ToArray(), 1);
            }
        }

        public byte ballotQuasiRandomNumber
        {
            get
            {
                return this.unionData[255];
            }
            set
            {
                this.unionData[255] = value;
            }
        }



    }
;

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct BroadcastFutureTickData
    {
        public TickData tickData;
    };


    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct Tick
    {
        //[FieldOffset(0)]
        public ushort computorIndex;
        //[FieldOffset(2)]
        public ushort epoch;
        //[FieldOffset(4)]
        public uint tick;

        // [FieldOffset(8)]
        public ushort millisecond;
        //[FieldOffset(10)]
        public byte second;
        //[FieldOffset(11)]
        public byte minute;
        //[FieldOffset(12)]
        public byte hour;
        //[FieldOffset(13)]
        public byte day;
        //[FieldOffset(14)]
        public byte month;
        //[FieldOffset(15)]
        public byte year;

        public ulong prevResourceTestingDigest;
        public ulong saltedResourceTestingDigest;

        //[FieldOffset(112)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] prevSpectrumDigest;
        //[FieldOffset(144)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] prevUniverseDigest;
        //[FieldOffset(176)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] prevComputerDigest;
        //[FieldOffset(208)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] saltedSpectrumDigest;
        //[FieldOffset(240)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] saltedUniverseDigest;
        //[FieldOffset(272)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] saltedComputerDigest;

        //[FieldOffset(304)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] transactionDigest;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        //[FieldOffset(336)]
        public byte[] expectedNextTickTransactionDigest;

        //[FieldOffset(368)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] signature;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BroadcastTick
    {
        public Tick tick;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Computors
    {
        public ushort epoch;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 21632)] // 676 * 32
        public byte[,] publicKeys;
        //public long _padding;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] signature;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct BroadcastComputors
    {
        public Computors computors;
    };

  

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RequestedQuorumTick
    {
        public uint tick;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (676 + 7) / 8)]
        public byte[] voteFlags;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RequestQuorumTick
    {
        public RequestedQuorumTick quorumTick;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RequestedTickData
    {
        public uint tick;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RequestTickData
    {
        public RequestedTickData requestedTickData;
    }




    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct SolutionTransaction
    {
        public BaseTransaction transaction;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] nonce;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] signature;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct SignedTransaction
    {
        public BaseTransaction transaction;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] signature;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct IpoTransaction
    {
        public BaseTransaction transaction;
        public long price;
        public ushort quantity;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public char[] _padding;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] signature;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct BaseTransaction
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] sourcePublicKey;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] destinationPublicKey;
        public long amount;
        public uint tick;
        public ushort inputType;
        public ushort inputSize;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 0, Size = 2776)]
    public struct Terminator
    {
        public ushort numberOfNewComputors;
        public ushort epoch;
        public uint tick;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 676)]
        public uint[] revenues;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] signature;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct BroadcastTerminator
    {
        public Terminator terminator;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct CurrentTickInfo
    {
        public ushort tickDuration; // in seconds
        public ushort epoch;
        public uint tick;
        public ushort numberOfAlignedVotes;
        public ushort numberOfMisalignedVotes;
        public uint initialTick;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct RequestedTickTransactions
    {
        public uint tick;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024 / 8)]
        public byte[] transactionFlags;
    }
;



    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct RequestedEntity
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] publicKey;
    }
;
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct Entity
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] publicKey;
        public long incomingAmount;
        public long outgoingAmount;
        public uint numberOfIncomingTransfers;
        public uint numberOfOutgoingTransfers;
        public uint latestIncomingTransferTick;
        public uint latestOutgoingTransferTick;
    }
;
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct RespondedEntity
    {
        public Entity entity;
        public uint tick;
        public int spectrumIndex;
        /// <summary>
        /// two dimensional array SPECTRUM_DEPTH 24 * 32
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24 * 32)]
        public byte[] siblings;
    }
    ;

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct RequestedExecutedTx
    {
        public ulong offset;
    };



    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct RequestedTickStats
    {
        public uint tickOffset;
    }
        ;

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct RespondTickStats
    {
        public uint currentTick;
        // (((((60 * 60 * 24 * 7) / (TARGET_TICK_DURATION / 1000)) + NUMBER_OF_COMPUTORS - 1) / NUMBER_OF_COMPUTORS) * NUMBER_OF_COMPUTORS)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (((((60 * 60 * 24 * 7) / (5000 / 1000)) + 676 - 1) / 676) * 676) / 8)]
        public byte[] tickStats;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct Message
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] sourcePublicKey;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] destinationPublicKey;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] gammingNonce;
    };


    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct RequestContractIPO
    {
        public uint contractIndex;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct RequestContractFunction // Invokes contract function
    {
        public uint contractIndex;
        public ushort inputType;
        public ushort inputSize;
        // Variable-size input
    };

    [StructLayout(LayoutKind.Sequential, Pack = 0)]

    public struct GetSendToManyV1Fee_output
    {
        public int fee; // Number of billionths
    }



    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct RespondContractIPO
    {
        public uint contractIndex;
        public uint tick;
        /// <summary>
        /// two dimensional array
        /// public keys 676 * 32
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 676 * 32)]
        public byte[] publicKeys; // two dimensional array
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 676)]
        public long[] prices;
    };

   [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct RequestIssuedAssets
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] publicKey;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct AssetIssuance
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] publicKey;
        public byte type;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
        public byte[] name; // Capital letters + digits
        byte numberOfDecimalPlaces;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
        byte[] unitOfMeasurement; // Powers of the corresponding SI base units going in alphabetical order
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct AssetOwnership
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] publicKey;
        public byte type;
        private byte _padding;
        public ushort managingContractIndex;
        public uint issuanceIndex;
        public long numberOfUnits;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct AssetPossession
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] publicKey;
        public byte type;
        private byte _padding;
        public ushort managingContractIndex;
        public uint ownershipIndex;
        public long numberOfUnits;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct Asset
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 48)]
        public byte[] unionData;

        public AssetIssuance AssetIssuance
        {
            get => Marshalling.Deserialize<AssetIssuance>(this.unionData);

            set => this.unionData = Marshalling.Serialize(value);
        }

        public AssetOwnership AssetOwnership
        {
            get => Marshalling.Deserialize<AssetOwnership>(this.unionData);

            set => this.unionData = Marshalling.Serialize(value);
        }

        public AssetPossession AssetPossession
        {
            get => Marshalling.Deserialize<AssetPossession>(this.unionData);

            set => this.unionData = Marshalling.Serialize(value);
        }


        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        public struct SendToManyV1_input
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = (25 * 32))]
            public byte[] addresses;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = (25))]
            public long[] amounts;
        };

        

        /*
    union
    {
        struct
        {
            unsigned char publicKey[32];
    unsigned char type;
    char name[7]; // Capital letters + digits
    char numberOfDecimalPlaces;
    char unitOfMeasurement[7]; // Powers of the corresponding SI base units going in alphabetical order
}
issuance;

    struct
        {
            unsigned char publicKey[32] ;
unsigned char type;
char padding[1];
unsigned short managingContractIndex;
unsigned int issuanceIndex;
long long numberOfUnits;
        } ownership;

struct
        {
            unsigned char publicKey[32] ;
unsigned char type;
char padding[1];
unsigned short managingContractIndex;
unsigned int ownershipIndex;
long long numberOfUnits;
        } possession;
    } varStruct;
        */
    };

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct RespondIssuedAssets
    {
        public Asset asset;
        public uint tick;
    }



    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct RequestOwnedAssets
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] publicKey;
    }



    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct RespondOwnedAssets
    {
        public Asset asset;
        public Asset issuanceAsset;
        public uint tick;
        // TODO: Add siblings
    }


    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct RequestPossessedAssets
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] publicKey;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct RequestTxStatus
    {
        public uint tick;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] digest;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] signature;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct RespondTxStatus
    {
        public uint currentTickOfNode;
        public uint tickofTx;
        public byte moneyFlew;
        public byte executed;
        public byte notfound;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public byte[] _padding;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] digest;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct TransferAssetOwnershipAndPossession_input
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] issuer;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] possessor;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] newOwner;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] assetName;
        long numberOfUnits;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct RespondPossessedAssets
    {
        public Asset asset;
        public Asset ownershipAsset;
        public Asset issuanceAsset;
        public uint tick;
        // TODO: Add siblings
    }

    public enum QubicSpecialCommands
    {
        SPECIAL_COMMAND_SHUT_DOWN = 0,
        SPECIAL_COMMAND_GET_PROPOSAL_AND_BALLOT_REQUEST = 1,
        SPECIAL_COMMAND_GET_PROPOSAL_AND_BALLOT_RESPONSE = 2,
        SPECIAL_COMMAND_SET_PROPOSAL_AND_BALLOT_REQUEST = 3,
        SPECIAL_COMMAND_SET_PROPOSAL_AND_BALLOT_RESPONSE = 4
    }

    public enum QubicPackageTypes
    {
        EXCHANGE_PUBLIC_PEERS = 0,
        BROADCAST_MESSAGE = 1,
        BROADCAST_COMPUTORS = 2,
        BROADCAST_TICK = 3,
        BROADCAST_FUTURE_TICK_DATA = 8,
        BROADCAST_QUESTION = 9,
        BROADCAST_ANSWER = 10,
        REQUEST_COMPUTORS = 11,
        REQUEST_TICKS = 12,
        BROADCAST_TERMINATOR = 13,
        REQUEST_QUORUM_TICK = 14,
        BROADCAST_QUORUM_TICK = 15,
        REQUEST_TICK_DATA = 16,
        BROADCAST_TRANSACTION = 24,
        BROADCAST_TICK_TRIGGER = 26,
        REQUEST_CURRENT_TICK_INFO = 27,
        RESPOND_CURRENT_TICK_INFO = 28,
        REQUEST_TICK_TRANSACTIONS = 29,
        RESPOND_TICK_TRANSACTION = 30,
        REQUEST_ENTITY = 31,
        RESPOND_ENTITY = 32,
        REQUEST_CONTRACT_IPO = 33,
        RESPOND_CONTRACT_IPO = 34,

        END_RESPONS = 35,
        REQUEST_ISSUED_ASSETS = 36,
        RESPOND_ISSUED_ASSETS = 37,
        REQUEST_OWNED_ASSETS = 38,
        RESPOND_OWNED_ASSETS = 39,
        REQUEST_POSSESSED_ASSETS = 40,
        RESPOND_POSSESSED_ASSETS = 41,

        REQUEST_CONTRACT_FUNCTION = 42,
        RESPOND_CONTRACT_FUNCTION = 43,

        PROCESS_SPECIAL_COMMAND = 255
    }

    public enum QubicTransactionTypes
    {
        DEFAULT = 0,
        CONTRACT_IPO_BID = 1
    }

    public enum QubicMessageTypes
    {
        MESSAGE_TYPE_SOLUTION = 0
    }

    public static class QubicStructSizes
    {
        public static uint RequestResponseHeader = 8;

        public static uint ComputorsSize = 2 + 21632 + 64; //21698
        public static uint BroadcastComputors = ComputorsSize;
        public static uint ComputerStateSize = 16 + 4 + ComputorsSize;

      

        public static uint Tick = 480; //400
        public static uint BroadcastTick = Tick;

        public static uint RequestedQuorumTick = 89;
        public static uint RequestQuorumTick = RequestedQuorumTick;

        public static uint QuorumTick = 10 + 6 + 10 * 32 + 43264; //43600
        public static uint BroadcastQuorumTick = QuorumTick;

        public static uint ExchangePublicPeers = 16;

        public static uint Transaction = 32 + 32 + 8 + 4 + 2 + 2; // 80
        public static uint SignedTransaction = Transaction + 64; // 144
        public static uint SolutionTransaction = Transaction + 32 + 64; // 144
        public static uint BroadcastedTransaction = SignedTransaction + 8; // 152
        public static uint BroadcastedSolutionTransaction = SolutionTransaction + 8; // 152

        public static uint Terminator = 2 + 2 + 4 + 2704 + 64; // 2776

        public static uint SpectrumEntity = 32 + 8 + 8 + 4 + 4 + 4 + 4; // 64

        public static uint TickData = 35808; //2 + 2 + 4 + 2 + 1 + 1 + 1 + 1 + 1 + 1 + 1024 * 32 + 64; // 35808
        public static uint BroadcastTickData = TickData;

        public static uint RequestTickData = 4;

        public static uint RequestedTickTransactions = 4 + (1024 / 8);

        public static uint RequestedEntity = 32;

        public static uint CurrentTickInfo = 12;

        public static uint RequestedExecutedTx = 8;
        public static uint RequestRevAdjust = (676 * 4) + 64;
        public static uint RequestedTickStats = 4;
        public static uint RespondExecutedTx = 8 + 37888 + 4;

        public static uint RequestQliExtension = 1 + 64;
        public static uint RespondQliExtension = 50 * 32 + 8;

        public static uint Entity = 32 + 8 + 8 + 4 + 4 + 4 + 4; // 64
        public static uint RespondedEntity = Entity + 4 + 4 + 24 * 32; // 64 + 776 = 840

        public static uint Message = 96; // 
        public static uint BroadcastedSolutionMessage = Message + 32 + 64 + 8; // 200
    }

}

