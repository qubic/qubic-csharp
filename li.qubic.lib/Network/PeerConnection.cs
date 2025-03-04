using li.qubic.lib;
using li.qubic.lib.Helper;
using li.qubic.lib.Network;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace li.qubic.services.poolManager
{
    /// <summary>
    /// simple way to keep a node/peer connection active
    /// </summary>
    public class PeerConnection
    {
        private QubicRequestor _requestor;
        private bool shouldBeConnected = true;
        private bool isConnected = false;


        public Action<string, Exception?> OnLogMessage;
        public Action<Peer> OnConnected;
        public Action<Peer> OnDisconnected;

        private Peer _peer;
        private string _ip;
        private QubicHelper _qubicHelper = new QubicHelper();

        // private int _lastDejavu = 0;
        private bool _isRequestingTickInfo = false;
        private string? _operatorSeed;

        private readonly Timer? _timer = null;
        private ILogger _logger;

        public string IpAddress => _ip;

        public void LogDebug(string message)
        {
            message = $"[{IpAddress}] " + message;

            if (_logger != null)
                _logger.LogDebug(message);
            if (OnLogMessage != null)
                OnLogMessage.Invoke(message, null);
        }

        public void LogError(Exception e, string message)
        {
            message = $"[{IpAddress}] " + message;

            if (_logger != null)
                _logger.LogError(e, message);
            if (OnLogMessage != null)
                OnLogMessage.Invoke(message, e);
        }

        /// <summary>
        /// create a peer connection
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="operatorSeed">for later use (not yet supported)</param>
        public PeerConnection(
            Peer peer,
            string? operatorSeed = null,
            ILogger logger = null
            )
        {
            _operatorSeed = operatorSeed;
            _peer = peer;
            _ip = peer.Ip;
            _logger = logger;

            _requestor = new QubicRequestor(_ip);
            _requestor.OnConnected += () =>
            {
                isConnected = _peer.Isconnected = true;
                if (OnConnected != null)
                    OnConnected.Invoke(_peer);
                LogDebug($"{_ip} connected");
            };
            _requestor.OnDisconnected += (ex) =>
            {
                isConnected = _peer.Isconnected = false;
                if (OnDisconnected != null)
                    OnDisconnected.Invoke(_peer);
                LogDebug($"{_ip} disconnected");
                if (shouldBeConnected)
                {
                    // wait a bit before reconnect
                    Thread.Sleep(1000);
                    try
                    {
                        _requestor.Connect();
                    }
                    catch (Exception e)
                    {
                        // todo: scheduled reconnect, now it will be dead after that moment 

                        LogError(e, $"Failed to connect to {_ip}");
                        // Thread.Sleep(30000); // wait 30 seconds for next try
                    }
                }
            };
            _requestor.PackageReceived += PackageReceived;

            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromMilliseconds(peer.UpdateInterval));
        }

        private bool _isRunning = false;


        private void DoWork(object? state)
        {
            if (_isRunning || !isConnected || !shouldBeConnected)
                return;
            try
            {
                RequestTickInfo();
            }
            catch (Exception ex)
            {
                LogError(ex, "Error in Timer");
            }
            finally { _isRunning = false; }
        }

        public void Start()
        {
            LogDebug("Start Connector");
            if (_requestor.Connect())
            {
                _requestor.RequestTickInfo();
            }
            shouldBeConnected = true;
        }

        private void PackageReceived(QubicRequestorReceivedPackage p)
        {
            switch (p.Header.type)
            {
                case (byte)QubicPackageTypes.RESPOND_CURRENT_TICK_INFO:
                    {
                        try
                        {
                            var tickInfo = Marshalling.Deserialize<CurrentTickInfo>(p.Payload);
                            Debug.WriteLine("RESPOND_CURRENT_TICK_INFO " + p.Header.dejavu + " | " + tickInfo.tick);
                            _peer.Tick = tickInfo.tick;
                        }
                        catch (Exception e)
                        {
                            LogError(e, "ERROR SAVING TickInfo");
                        }
                    }
                    break;
            }
        }

        
        public void SendData(byte[] bytes)
        {
            if (_requestor.IsConnected ?? false)
            {
                _requestor.Send(bytes);
            }
        }

        public void RequestTickInfo()
        {
            if (_isRequestingTickInfo || !(_requestor.IsConnected ?? false))
                return;

            _isRequestingTickInfo = true;

            try
            {
                LogDebug("Request TickInfo");
                _requestor.RequestTickInfo();
            }
            catch (Exception ex)
            {
                LogError(ex, "Error in Timer");
            }
            finally
            {
                _isRequestingTickInfo = false;
            }
        }

        public void Close(bool emitCloseEvent = false)
        {
            _isRunning = false;
            shouldBeConnected = false;
            isConnected = false;
            _requestor.Close(emitCloseEvent);
        }

        internal void Stop(bool emitCloseEvent = false)
        {
            Close();
        }

        public async Task<RespondedEntity?> GetEntity(byte[] sourcePublicKey)
        {
            if (_requestor == null || !(_requestor.IsConnected ?? false))
                return null;

            var header = new RequestResponseHeader(true)
            {
                type = (short)QubicPackageTypes.REQUEST_ENTITY,
                size = Marshal.SizeOf<RequestedEntity>() + Marshal.SizeOf<RequestResponseHeader>()
            };
            var request = new RequestedEntity()
            {
                publicKey = sourcePublicKey
            };

            var dataToSend = Marshalling.Serialize(header).Concat(Marshalling.Serialize(request)).ToArray();

            return await Task.Run(() =>
            {
                RespondedEntity? entity = null;
                _requestor.GetDataPackageFromPeer<RespondedEntity>(dataToSend, (short)QubicPackageTypes.RESPOND_ENTITY, (r) =>
                {
                    entity = r;
                });

                if (entity == null)
                    throw new TimeoutException("Balance was not received in time");

                return entity.Value;
            });
        }
    }
}
