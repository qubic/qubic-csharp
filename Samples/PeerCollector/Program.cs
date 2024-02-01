using li.qubic.lib;
using li.qubic.lib.Helper;
using li.qubic.lib.Network;
using System.Net;

namespace PeerCollector
{

    /// <summary>
    /// Sample application to demonstrate how to use the basic Qubic C-Sharp Qubic Library
    /// 
    /// This Sample Applications travers throught the Qubic Network ans lists all found public Peers.
    /// 
    /// </summary>
    public class Program
    {
        private static List<Peer> FoundPeers = new List<Peer>();

        static void Log(string message)
        {
            Console.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss\t") + message);
        }

        static void Main(string[] args)
        {

            // define at least one starting peer
            FoundPeers.Add(new Peer("144.2.106.163"));

            while (true)
            {
                var potentialPeers = FoundPeers.Where(q => q.IpAddress != "0.0.0.0" && !q.IsBad).ToList();
                var currentTarget = potentialPeers[new Random().Next(0, potentialPeers.Count - 1)];
                var requestor = new QubicRequestor(currentTarget.IpAddress);

                requestor.PackageReceived = (p) =>
                {
                    if (p.Header.type == (short)QubicPackageTypes.EXCHANGE_PUBLIC_PEERS)
                    {

                        var rawPeers = Marshalling.Deserialize<ExchangePublicPeers>(p.Payload);
                        var peers = ParseExchangedIpAddresses(rawPeers.peers);
                        Log($"[{currentTarget.IpAddress}] Received Peers: {string.Join(";", peers.Select(s => s.MapToIPv4().ToString()))}");
                        foreach (var peer in peers)
                        {
                            if (!FoundPeers.Any(p => p.IpAddress.Equals(peer.MapToIPv4().ToString())))
                            {
                                FoundPeers.Add(new Peer(peer.MapToIPv4().ToString()));
                                Log($"[{currentTarget.IpAddress}] New Peer {peer.MapToIPv4().ToString()}");
                            }
                        }
                    }
                };
                try
                {
                    requestor.Connect();
                }
                catch (Exception e)
                {
                    Log($"[{currentTarget.IpAddress}] failed to connect (timeout)");
                    currentTarget.IsBad = true;
                }
                Thread.Sleep(1000);
                Log($"{FoundPeers.Count} Total Peers | {FoundPeers.Count(q => q.IsBad)} Bad Peers");
            }

        }
        
        /// <summary>
        /// converts the qubic peer list into a readable list
        /// </summary>
        /// <param name="ips"></param>
        /// <returns></returns>
        private static List<IPAddress> ParseExchangedIpAddresses(byte[,] ips)
        {
            var output = new List<IPAddress>();
            for (int i = 0; i < QubicLibConst.NUMBER_OF_EXCHANGED_PEERS * 4; i += 4)
            {
                output.Add(IPAddress.Parse(ips.GetValue(i).ToString() + "." + ips.GetValue(i + 1).ToString() + "." + ips.GetValue(i + 2).ToString() + "." + ips.GetValue(i + 3).ToString()));
            }
            return output;
        }

        private class Peer
        {
            public Peer()
            {

            }

            public Peer(string ipAddress, bool isBad = false)
            {
                IpAddress = ipAddress;
                IsBad = isBad;
            }

            public string IpAddress { get; set; }
            public bool IsBad { get; set; }
        }
    }
}
