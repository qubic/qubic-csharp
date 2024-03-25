using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Reflection.PortableExecutable;
using System.Diagnostics;
using li.qubic.lib.Helper;


namespace li.qubic.lib.Network
{
    public class QubicRequestor
    {
        private Guid InstanceId = Guid.NewGuid();

        public byte[] receiveBuffer = new byte[QubicLibConst.BUFFER_SIZE];
        public bool _finished = false;
        private string _ip;
        private int _receiveTimeout = 2; // seconds
        private Socket clientSocket;
        private short? _waitForPackageType = null;

        public Action<QubicRequestorReceivedPackage>? PackageReceived { get; set; }


        public QubicRequestor(string targetIp, int receiveTimeout = 2)
        {
            _ip = targetIp;
            _receiveTimeout = receiveTimeout;
        }

        public bool Connect()
        {
            if (clientSocket?.Connected ?? false)
                return true;

            IPAddress ipAddress = IPAddress.Parse(_ip);
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, QubicLibConst.PORT);

            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            Debug.WriteLine($"[QUBICREQUESTOR-{InstanceId}]Connect to {_ip}");

            // Connect to the remote endpoint
            clientSocket.Connect(remoteEP);

            // listen to what the peer sends
            clientSocket.BeginReceive(receiveBuffer, 0, QubicLibConst.BUFFER_SIZE, 0,
                new AsyncCallback(ReceiveCallback), clientSocket);

            return clientSocket.Connected;
        }

        public void Disconnect()
        {
            this.Close();
        }

        public void Close()
        {
            // Close the socket
            clientSocket.Shutdown(SocketShutdown.Both);
            clientSocket.Close();
        }

        public void Send(byte[] data)
        {
            // Send the binary package to the remote endpoint
            clientSocket.Send(data);
        }

        public bool? IsConnected => clientSocket?.Connected;


        /// <summary>
        /// CAUTION: PackageReceived will be overwritten
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="requestPackage"></param>
        /// <param name="waitForPackageType"></param>
        public void GetDataPackageFromPeer<T>(byte[] requestPackage, short waitForPackageType, Action<T> resultFunc)
            where T : struct
        {
            this.PackageReceived = (p) =>
            {
                if (p.Header.type == (short)waitForPackageType)
                {
                    var result = Marshalling.Deserialize<T>(p.Payload);
                    resultFunc.Invoke(result);
                }
            };
            this.AskPeer(requestPackage, waitForPackageType);
            this.PackageReceived = null;
        }


        public Task<T> GetDataPackageFromPeerAsyc<T>(byte[] requestPackage, short waitForPackageType)
            where T : struct
        {
            return Task.Run(() =>
            {
                var t = new TaskCompletionSource<T>();

                GetDataPackageFromPeer<T>(requestPackage, waitForPackageType, s => t.TrySetResult(s));

                return t.Task;
            });
        }

      

        /// <summary>
        /// must not send connect
        /// </summary>
        /// <param name="requestPackage"></param>
        /// <param name="waitForPackageType">set here the type of package you want to receive. the requestor will wait until this package is received or timeout</param>
        public bool AskPeer(byte[] requestPackage, short? waitForPackageType = null)
        {
            try
            {
                _finished = false;
                _waitForPackageType = waitForPackageType;

                var wasConnected = true;
                if (!clientSocket?.Connected ?? true)
                {
                    Connect();
                    wasConnected = false;
                }

                // Send the binary package to the remote endpoint
                clientSocket.Send(requestPackage);

                var start = DateTime.UtcNow;
                while (waitForPackageType != null && !_finished && start.AddSeconds(_receiveTimeout) > DateTime.UtcNow)
                {
                    // wait until we got entity information
                    Thread.Sleep(100);
                }

                if (!wasConnected)
                    Close();

                return true;

            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR:" + ex.ToString());
                return false;
            }
        }


        public void SendExchangePublicPeers(List<string> peers)
        {
            if (peers.Count != 0)
                throw new ArgumentException("Must be exact 4 peers to exchange");


            if (!IsConnected ?? false)
                Connect();

            FirstRequest packet = new FirstRequest();

            packet.header.type = 0;
            packet.payload.peers = new byte[4, 4];

            var i = 0;
            foreach(string ip in peers)
            {
                var peer = IPAddress.Parse(ip);
                packet.payload.peers[i, 0] = peer.GetAddressBytes()[0];
                packet.payload.peers[i, 1] = peer.GetAddressBytes()[1];
                packet.payload.peers[i, 2] = peer.GetAddressBytes()[2];
                packet.payload.peers[i++, 3] = peer.GetAddressBytes()[3];
            }

            var size = Marshal.SizeOf(packet.header) + (int)QubicStructSizes.ExchangePublicPeers;
            packet.header.size = size;

            var data = Marshalling.Serialize(packet);

            this.Send(data);
        }

        private void SendExchangePeers(Socket socket)
        {
            FirstRequest packet = new FirstRequest();

            packet.header.type = 0;
            packet.payload.peers = new byte[4, 4];


            var peer = IPAddress.Parse(_ip);

            for (int i = 0; i < 4; i++)
            {
                packet.payload.peers[i, 0] = peer.GetAddressBytes()[0];
                packet.payload.peers[i, 1] = peer.GetAddressBytes()[1];
                packet.payload.peers[i, 2] = peer.GetAddressBytes()[2];
                packet.payload.peers[i, 3] = peer.GetAddressBytes()[3];
            }

            var size = Marshal.SizeOf(packet.header) + (int)QubicStructSizes.ExchangePublicPeers;
            packet.header.size = size;

            var data = Marshalling.Serialize(packet);

            socket.Send(data);
        }

        public async Task<CurrentTickInfo> GetTickInfo()
        {
            return await Task.Run(() =>
            {
                CurrentTickInfo? info = null;
                this.GetDataPackageFromPeer<CurrentTickInfo>(GetTickInfoRequestPackage(), (short)QubicPackageTypes.RESPOND_CURRENT_TICK_INFO, (r) =>
                {
                    info = r;
                });

                if (info == null)
                    throw new TimeoutException("Tickinfo was not received in time");

                return info.Value;
            });
        }

        private byte[] GetTickInfoRequestPackage()
        {
            var header = new RequestResponseHeader(true)
            {
                size = 8,
                type = (byte)QubicPackageTypes.REQUEST_CURRENT_TICK_INFO
            };
            return Marshalling.Serialize(header);
        }

        private void SendTickInfoRequest(Socket socket)
        {
            var header = new RequestResponseHeader(true)
            {
                size = 8,
                type = (byte)QubicPackageTypes.REQUEST_CURRENT_TICK_INFO
            };
            socket.Send(Marshalling.Serialize(header));
        }


        private object receiveQueueLock = new object();

        private void ProcessReceivedData()
        {
            
            var headerSize = Marshal.SizeOf<RequestResponseHeader>();
            RequestResponseHeader header;
            byte[] payloadData;
            lock (receiveQueueLock)
            {
                Debug.WriteLine($"[QubicRequestor-{InstanceId}] ProcessReceivedData, NumberOfBytes:" + NumberOfReceivedBytes);

                if (NumberOfReceivedBytes < headerSize)
                    return;

                header = Marshalling.Deserialize<RequestResponseHeader>(ReceiveQueue.Take(headerSize).ToArray());

                if (header.size <= 0 || header.size > NumberOfReceivedBytes)
                    return;

                var payloadSize = header.size - headerSize;
                payloadData = ReceiveQueue.Skip(headerSize).Take(payloadSize).ToArray();
                ReceiveQueue = ReceiveQueue.Skip(header.size).ToList();

                NumberOfReceivedBytes -= (uint)header.size;
            }

            Debug.WriteLine($"[QubicRequestor-{InstanceId}] PACKAGE: {header.type} {header.size}; new NumberOfBytes: " + NumberOfReceivedBytes);
            // Console.WriteLine($"RECEIVED: {header.type} {header.size}");
            if (PackageReceived != null)
            {
                PackageReceived.Invoke(new QubicRequestorReceivedPackage(IPAddress.Parse(_ip), header, payloadData));
            }

            if (_waitForPackageType != null && header.type == _waitForPackageType) { 
                _finished = true;
                Debug.WriteLine($"[QubicRequestor-{InstanceId}] FINISHED: {_waitForPackageType} vs. {header.type}");
            }else
            {
                Debug.WriteLine($"[QubicRequestor-{InstanceId}] WAIT FOR MORE DATA");
            }

            if (NumberOfReceivedBytes > headerSize)
            {
                Debug.WriteLine($"[QubicRequestor-{InstanceId}] More Bytes to Work on: {NumberOfReceivedBytes}");
                // if number of received bytes is higher than header, we need to processs again
                ProcessReceivedData();
            }
            else
            {
                Debug.WriteLine($"[QubicRequestor-{InstanceId}] NO MORE Bytes to process");
            }
        }
        private uint NumberOfReceivedBytes { get; set; }
        private List<byte> ReceiveQueue = new List<byte>();
        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                Socket socket = ar.AsyncState as Socket;
                if (socket == null)
                    return;

                if (socket.SafeHandle.IsInvalid)
                    return;

                // Read data from the remote device.  
                int bytesRead = 0;
                bytesRead = socket.EndReceive(ar);

                Debug.WriteLine($"[QubicRequestor-{InstanceId}] RECEIVED BYTES: {bytesRead}");

                if (bytesRead > 0)
                {

                    lock (receiveQueueLock)
                    {
                        try
                        {
                            // copy received bytes into processing buffer
                            ReceiveQueue.AddRange(receiveBuffer.Take(bytesRead));
                            NumberOfReceivedBytes += (uint)bytesRead;
                        }
                        catch (Exception)
                        {
                            // ignore for the moment
                        }
                    }


                }

                ProcessReceivedData();

                socket.BeginReceive(receiveBuffer, 0, QubicLibConst.BUFFER_SIZE, 0,
                                    new AsyncCallback(ReceiveCallback), socket);
            }
            catch (Exception e)
            {
                Console.WriteLine($"[QUBICREQUESTOR-{InstanceId}] ERROR: {e.Message}");
            }
        }
    }
}
