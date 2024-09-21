using li.qubic.lib;
using li.qubic.lib.Helper;
using li.qubic.lib.Logging;
using li.qubic.lib.Network;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace li.qubic.test
{
    [TestClass]
    public class LoggingTest
    {
        private string testNodeIp = "10.10.10.10";
        private string passCode = "1, 2, 3, 4";

        private ulong[] GetPassCode()
        {
            return passCode.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(s => ulong.Parse(s)).ToArray();
        }

        private void RequestLogs(QubicRequestor requestor, ulong lastReceivedId)
        {
            var header = new RequestResponseHeader(true)
            {
                size = Marshal.SizeOf(typeof(RequestResponseHeader)) + Marshal.SizeOf(typeof(RequestLog)),
                type = RequestLog.type
            };

            var rl = new RequestLog()
            {
                passcode = GetPassCode(),
                fromID = lastReceivedId,
                toID = lastReceivedId + 10,
            };

            var data = Marshalling.Serialize(header).Concat(Marshalling.Serialize(rl)).ToArray();
            requestor.Send(data);
        }

        [TestMethod]
        public void TestBasicLogGet()
        {
            using (var requestor = new QubicRequestor(testNodeIp))
            {

                if (requestor.Connect())
                {
                    var currentTick = requestor.GetTickInfo().GetAwaiter().GetResult();
                    Debug.WriteLine($"Tick: {currentTick.tick}");

                    var finished = false;

                    requestor.PackageReceived += (package) =>
                    {
                        if(package.Header.type == RespondLog.type)
                        {
                            var entries = QubicEventLogEntry.FromBuffer(package.Payload);
                            Console.WriteLine($"Peer {testNodeIp}");
                            foreach (var entry in entries)
                            {
                                Console.Write(entry.Epoch + "." + entry.Tick + " - " + entry.Id + ": ");
                                Console.Write(QubicLogger.Log(entry) + "\n");
                            }
                            RequestLogs(requestor, entries.Max(m => m.Id));
                            finished  = true;
                        }
                    };


                    ulong currentOffset = 0;

                    RequestLogs(requestor, currentOffset);

                    var start = DateTime.UtcNow;
                    while (start.AddSeconds(120) > DateTime.UtcNow && !finished)
                    {
                        Thread.Sleep(1000);
                    }
                }
                else
                {
                    Assert.Fail("Connection failed");
                }
            }
        }

    }
}