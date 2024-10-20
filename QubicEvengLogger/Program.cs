using System.Diagnostics;

namespace QubicEvengLogger
{
    
    /// <summary>
    /// sample program to receive qubic logs from a core node
    /// </summary>
    internal class Program
    {
        static void Main(string[] args)
        {
            var logConnector = new LogConnector(Demo.nodeIpAddress, Demo.passcode);

            ulong firstTick = 0;
            ulong lastTick = 0;

            var totalReceived = 0;

            var sw = new Stopwatch();
            Console.WriteLine("Start Fetching: " + DateTime.UtcNow);
            sw.Start();

            //process incoming log entries
            logConnector.OnLogEntries += (eventEntries) =>
            {
                firstTick = firstTick == 0 ? eventEntries.Min(m => m.Tick) : firstTick;
                lastTick = eventEntries.Max(m => m.Tick);
                totalReceived += eventEntries.Count;
                Console.WriteLine($"Rec: {eventEntries.Count}/{totalReceived} from {eventEntries.Min(m=> m.Id)} to {eventEntries.Max(m => m.Id)} | Ticks: {lastTick-firstTick} | {firstTick} to {lastTick} | {sw.ElapsedMilliseconds}");
            };

            var ts = new CancellationTokenSource();

            // start connector
            logConnector.StartAsync(ts, 0);


            // keep program running
            while(!ts.IsCancellationRequested)
            {
                Thread.Sleep(1000);
            }

        }
    }
}
