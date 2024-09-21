using li.qubic.lib;
using li.qubic.lib.Helper;
using li.qubic.lib.Logging;
using li.qubic.lib.Network;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace QubicEvengLogger
{
    public class LogConnector
    {

        private string peerIpAddress;
        private string passCode;

        private ulong _latestReceivedTick = 0; // last received log tick (highest tick we know from log entries)
        private ulong _lastFetchedId = 0; // last received log id (highest id we know)
        private DateTime _lastReceivedLogs = DateTime.MinValue; // used to store when we have received log entries last time
        private DateTime _lastDecrease = DateTime.MinValue; // used to store when batcsize has decrease last time
        private DateTime _lastRequest = DateTime.MinValue; // used to store when last request has been made

        /// <summary>
        /// the number of log entries that are requested with one request
        /// </summary>
        public ulong BatchSize { get; set; } = 1;

        /// <summary>
        /// register your action handler, to receive new logs
        /// </summary>
        public Action<List<QubicEventLogEntry>> OnLogEntries;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="peerIpAddresss"></param>
        /// <param name="passCode">the for passcodes separated by a comma</param>
        /// <exception cref="ArgumentException"></exception>
        public LogConnector(string peerIpAddresss, string passCode)
        {
            this.peerIpAddress = peerIpAddresss;
            this.passCode = passCode;

            if (string.IsNullOrEmpty(passCode) || string.IsNullOrEmpty(peerIpAddresss))
            {
                throw new ArgumentException("PeerIpAddress and passCode must not be empty or null");
            }
        }

        /// <summary>
        /// converts the string passcode to an ulung array
        /// </summary>
        /// <returns></returns>
        private ulong[] GetPassCode()
        {
            return passCode.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(s => ulong.Parse(s)).ToArray();
        }


        /// <summary>
        /// request logs from core node
        /// </summary>
        /// <param name="requestor"></param>
        /// <param name="lastReceivedId"></param>
        private void RequestLogs(QubicRequestor requestor, ulong lastReceivedId)
        {
            
            _lastRequest = DateTime.UtcNow;

            var header = new RequestResponseHeader(true)
            {
                size = Marshal.SizeOf(typeof(RequestResponseHeader)) + Marshal.SizeOf(typeof(RequestLog)),
                type = RequestLog.type
            };

            var rl = new RequestLog()
            {
                passcode = GetPassCode(),
                fromID = lastReceivedId,
                toID = lastReceivedId + BatchSize - 1, // -1 because toID is included
            };

            Console.WriteLine($"Request ids from {lastReceivedId} to {lastReceivedId+BatchSize-1}");

            var data = Marshalling.Serialize(header).Concat(Marshalling.Serialize(rl)).ToArray();
            requestor.Send(data);
        }

        /// <summary>
        /// internal processor
        /// holds the connection with the node and publishes new received log entries
        /// </summary>
        /// <param name="tokenSource"></param>
        private void LogProcessor(CancellationTokenSource tokenSource)
        {

            using (var requestor = new QubicRequestor(peerIpAddress))
            {

                if (requestor.Connect())
                {

                    var errorCount = 0;

                    // receive handler
                    requestor.PackageReceived += (package) =>
                    {
                        try
                        {
                            if (package.Header.type == RespondLog.type)
                            {
                                var entries = QubicEventLogEntry.FromBuffer(package.Payload);

                                if (OnLogEntries != null)
                                    OnLogEntries.Invoke(entries);

                                _lastFetchedId = entries.Max(m => m.Id);
                                _latestReceivedTick = entries.Max(m => m.Tick);
                                _lastReceivedLogs = DateTime.UtcNow;

                                // request new batch after we have received last
                                if (!tokenSource.IsCancellationRequested)
                                    RequestLogs(requestor, _lastFetchedId);

                                errorCount = 0;
                            }
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message.Contains("Empty log"))
                            {
                                // if we receive empty log it means there are no new log entries or the requested batch size is to big
                                BatchSize /= 2;
                                _lastDecrease = DateTime.UtcNow;
                                Console.WriteLine($"Decreased Batchsize to {BatchSize}");
                            }
                            else
                            {
                                // when we land here, something bad happened
                                errorCount++;
                                // error
                                Debug.WriteLine(ex);
                                if (errorCount > 5)
                                {
                                    Console.WriteLine($"To many errors in ids {_lastFetchedId}-{_lastFetchedId + BatchSize}. Stopping: " + ex.Message);
                                    tokenSource.Cancel();
                                }
                            }
                        }
                    };

                    // main connection loop holder
                    while (!tokenSource.IsCancellationRequested)
                    {
                        if(_lastDecrease.AddSeconds(5) < DateTime.UtcNow && _lastReceivedLogs.AddSeconds(5) > DateTime.UtcNow && DateTime.UtcNow.Second % 5 == 0){
                            try
                            {
                                var tickInfo = requestor.GetTickInfo().GetAwaiter().GetResult();
                                if (_latestReceivedTick < tickInfo.tick)
                                {
                                    // increase batchsize
                                    BatchSize *= 2;
                                    Console.WriteLine($"Increased Batchsize to {BatchSize}");
                                }
                            }
                            catch (Exception ex)
                            {
                                //ignore
                            }
                        }

                        // if we didn't receive anything in the last 5 seconds, we create a new request
                        if (_lastRequest.AddSeconds(5) < DateTime.UtcNow)
                        {
                           
                            RequestLogs(requestor, _lastFetchedId);
                        }
                        Thread.Sleep(1000);
                    }
                }
            }

        }


        /// <summary>
        /// Start Qubic log connector
        /// every log entry has an id which is counted up. at the beinning of the epoch it is 0
        /// </summary>
        /// <param name="tokenSource"></param>
        /// <param name="startId">the start id</param>
        public async void StartAsync(CancellationTokenSource tokenSource, ulong startId = 0)
        {
            _lastFetchedId = startId;

            await Task.Run(() =>
            {
                while (!tokenSource.IsCancellationRequested)
                {
                    try
                    {
                        LogProcessor(tokenSource);
                    }
                    catch (Exception ex)
                    {
                        // most probably a connection problem, just start over
                        Console.WriteLine("Error:  " + ex.ToString());
                    }
                }
            });
        }
    }
}
