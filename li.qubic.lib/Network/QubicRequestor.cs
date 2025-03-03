using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
using System.Diagnostics;
using li.qubic.lib.Helper;
using System.Reflection.PortableExecutable;


namespace li.qubic.lib.Network
{
    public class QubicRequestor : IDisposable
    {
        private Guid InstanceId = Guid.NewGuid();

        public byte[] receiveBuffer = new byte[QubicLibConst.BUFFER_SIZE];
        public bool _finished = false;
        private string _ip;
        private int _port = QubicLibConst.PORT;
        private int _receiveTimeout = 2; // seconds
        private Socket clientSocket;
        private short? _waitForPackageType = null;

        public Action<QubicRequestorReceivedPackage>? PackageReceived { get; set; }
        private Action<QubicRequestorReceivedPackage>? InternalPackageReceived { get; set; }
        public Action? OnConnected { get; set; }
        public Action<Exception?>? OnDisconnected { get; set; }


        public QubicRequestor(string targetIp, int port = 21841, int receiveTimeout = 2)
        {
            _ip = targetIp;
            if (port < 1024)
            {
                // assume port is meant as the timeout
                // needed for backward compatibility
                _receiveTimeout = port;
            }
            else
            {
                _port = port;
                _receiveTimeout = receiveTimeout;
            }

        }

        private void ProcessPackageReceived(QubicRequestorReceivedPackage p)
        {
            if (PackageReceived != null)
            {
                PackageReceived.Invoke(p);
            }
            if (InternalPackageReceived != null)
            {
                InternalPackageReceived.Invoke(p);
            }
        }
 
        [Obsolete("DO NOT ANYMORE USE THIS!")]
        public QubicRequestor(short protocol, string targetIp, int receiveTimeout = 2)
        {
            _ip = targetIp;
            _receiveTimeout = receiveTimeout;
        }

        private object _socketConnectLock = new object();

        public bool Connect()
        {
            if (clientSocket?.Connected ?? false)
                return true;

            lock (_socketConnectLock)
            {

                // if we were waiting for the lock it make sense to check hier for connection again
                if (clientSocket?.Connected ?? false)
                    return true;

                IPAddress ipAddress = IPAddress.Parse(_ip);
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, _port);

                clientSocket?.Dispose();

                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                Debug.WriteLine($"[QubicRequestor-{InstanceId}] {DateTime.UtcNow.ToString("HH:mm:ss")}Connect to {_ip}");

                // Connect to the remote endpoint
                clientSocket.Connect(remoteEP);

                if (clientSocket.Connected && OnConnected != null)
                    OnConnected.Invoke();

                // listen to what the peer sends
                clientSocket.BeginReceive(receiveBuffer, 0, QubicLibConst.BUFFER_SIZE, 0,
                    new AsyncCallback(ReceiveCallback), clientSocket);

            }
            return clientSocket.Connected;
        }

        public void Disconnect()
        {
            this.Close();
        }

        public void Close(bool emitEvent = true)
        {
            Debug.WriteLine($"[QubicRequestor-{InstanceId}] {DateTime.UtcNow.ToString("HH:mm:ss")} Disconnect from {_ip}");

            try
            {
                lock (_socketConnectLock)
                {
                    // Close the socket
                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Close();
                }
            }
            catch (Exception)
            {
                Debug.WriteLine($"[QubicRequestor-{InstanceId}] {DateTime.UtcNow.ToString("HH:mm:ss")} Error disconnecting from {_ip}");
            }
            if (emitEvent && OnDisconnected != null)
                OnDisconnected.Invoke(null);
        }

        private object _socketSendLock = new object();

        public void Send(byte[] data)
        {
            lock (_socketSendLock)
            {
                // Send the binary package to the remote endpoint
                clientSocket.Send(data);
            }
        }

        public bool? IsConnected => clientSocket?.Connected;


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="requestPackage"></param>
        /// <param name="waitForPackageType"></param>
        public void GetDataPackageFromPeer<T>(byte[] requestPackage, short waitForPackageType, Action<T> resultFunc)
            where T : struct
        {
            this.InternalPackageReceived = (p) =>
            {
                if (p.Header.type == (short)waitForPackageType)
                {
                    var result = Marshalling.Deserialize<T>(p.Payload);
                    resultFunc.Invoke(result);
                }
            };
            this.AskPeer(requestPackage, waitForPackageType);
            this.InternalPackageReceived = null;
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

                Debug.WriteLine($"[QubicRequestor-{InstanceId}] {DateTime.UtcNow.ToString("HH:mm:ss")} Send, NumberOfBytes:" + requestPackage.Length);

                // Send the binary package to the remote endpoint
                Send(requestPackage);

                var start = DateTime.UtcNow;
                while (waitForPackageType != null && !_finished && start.AddSeconds(_receiveTimeout) > DateTime.UtcNow)
                {
                    // wait until we got entity information
                    Thread.Sleep(100);
                }

                if (!wasConnected)
                    Close();

                return _finished;

            }
            catch (Exception ex)
            {
                Debug.WriteLine("ERROR:" + ex.ToString());
                return false;
            }
        }


        public void SendExchangePublicPeers(List<string> peers)
        {
            if (peers.Count != 4)
                throw new ArgumentException("Must be exact 4 peers to exchange");


            if (!IsConnected ?? false)
                Connect();

            FirstRequest packet = new FirstRequest();

            packet.header.type = 0;
            packet.payload.peers = new byte[4, 4];

            // need to randomize dejavu
            packet.header.RandomizeDejavu();

            var i = 0;
            foreach (string ip in peers)
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

        /// <summary>
        /// CAUTION this will overwrite PackageReceived callback
        /// </summary>
        /// <returns></returns>
        /// <exception cref="TimeoutException"></exception>
        public async Task<CurrentTickInfo> GetTickInfo()
        {
            return await Task.Run(() =>
            {
                CurrentTickInfo? info = null;
                this.GetDataPackageFromPeer<CurrentTickInfo>(GetTickInfoRequestPackage().Item2, (short)QubicPackageTypes.RESPOND_CURRENT_TICK_INFO, (r) =>
                {
                    info = r;
                });

                if (info == null)
                    throw new TimeoutException("Tickinfo was not received in time");

                return info.Value;
            });
        }

        /// <summary>
        /// returns dejavu
        /// </summary>
        /// <returns></returns>
        public int RequestTickInfo()
        {
            var (dejavu, data) = GetTickInfoRequestPackage();
            this.Send(data);
            return dejavu;
        }

        public async Task<RespondedSystemInfo> GetSystemInfo()
        {
            return await Task.Run(() =>
            {
                RespondedSystemInfo? info = null;
                this.GetDataPackageFromPeer<RespondedSystemInfo>(GetSystemInfoRequestPackage(), (short)QubicPackageTypes.RESPOND_SYSTEM_INFO, (r) =>
                {
                    info = r;
                });

                if (info == null)
                    throw new TimeoutException("SystemInfo was not received in time");

                return info.Value;
            });
        }

        private (int, byte[]) GetTickInfoRequestPackage()
        {
            var header = new RequestResponseHeader(true)
            {
                size = 8,
                type = (byte)QubicPackageTypes.REQUEST_CURRENT_TICK_INFO
            };
            return (header.dejavu, Marshalling.Serialize(header));
        }

        private byte[] GetSystemInfoRequestPackage()
        {
            var header = new RequestResponseHeader(true)
            {
                size = 8,
                type = (byte)QubicPackageTypes.REQUEST_SYSTEM_INFO
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
                Debug.WriteLine($"[QubicRequestor-{InstanceId}] {DateTime.UtcNow.ToString("HH:mm:ss")} ProcessReceivedData, NumberOfBytes:" + NumberOfReceivedBytes);

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

            Debug.WriteLine($"[QubicRequestor-{InstanceId}] {DateTime.UtcNow.ToString("HH:mm:ss")} PACKAGE: {header.type} {header.size}; new NumberOfBytes: " + NumberOfReceivedBytes);
            // Console.WriteLine($"RECEIVED: {header.type} {header.size}");

            if (_waitForPackageType != null && header.type == _waitForPackageType)
            {
                _finished = true;
                Debug.WriteLine($"[QubicRequestor-{InstanceId}] {DateTime.UtcNow.ToString("HH:mm:ss")} FINISHED: {_waitForPackageType} vs. {header.type}");
            }
            else
            {
                Debug.WriteLine($"[QubicRequestor-{InstanceId}] {DateTime.UtcNow.ToString("HH:mm:ss")} WAIT FOR MORE DATA");
            }

            
            ProcessPackageReceived(new QubicRequestorReceivedPackage(IPAddress.Parse(_ip), header, payloadData));
            


            if (NumberOfReceivedBytes > headerSize)
            {
                Debug.WriteLine($"[QubicRequestor-{InstanceId}] {DateTime.UtcNow.ToString("HH:mm:ss")} More Bytes to Work on: {NumberOfReceivedBytes}");
                // if number of received bytes is higher than header, we need to processs again
                ProcessReceivedData();
            }
            else
            {
                Debug.WriteLine($"[QubicRequestor-{InstanceId}] {DateTime.UtcNow.ToString("HH:mm:ss")} NO MORE Bytes to process");
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

                Debug.WriteLine($"[QubicRequestor-{InstanceId}] {DateTime.UtcNow.ToString("HH:mm:ss")} RECEIVED BYTES: {bytesRead}");

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
                Debug.WriteLine($"[QubicRequestor-{InstanceId}] {DateTime.UtcNow.ToString("HH:mm:ss")} ERROR: {e.Message}");
                // on error disconnect/close
                this.Close();
            }
        }

        public void Dispose()
        {
            if (IsConnected ?? false)
            {
                try
                {
                    this.Disconnect();
                }
                catch (Exception e)
                {
                    throw new Exception("Error Disposing QubicRequestor", e);
                }
            }
        }
    }
}
