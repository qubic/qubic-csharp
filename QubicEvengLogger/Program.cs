using li.qubic.lib.Logging;
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
            var logConnector = new LogConnector("10.10.10.10", "1, 2, 3, 4");

            ulong firstTick = 0;
            ulong lastTick = 0;

            var totalReceived = 0;

            var sw = new Stopwatch();
            Console.WriteLine("Start Fetching: " + DateTime.UtcNow);
            sw.Start();

            //process incoming log entries
            logConnector.OnLogEntries += (entries) =>
            {
                firstTick = firstTick == 0 ? entries.Min(m => m.Tick) : firstTick;
                lastTick = entries.Max(m => m.Tick);
                totalReceived += entries.Count;
                Console.WriteLine($"Rec: {entries.Count}/{totalReceived} from {entries.Min(m=> m.Id)} to {entries.Max(m => m.Id)} | Ticks: {lastTick-firstTick} | {firstTick} to {lastTick} | {sw.ElapsedMilliseconds}");
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
