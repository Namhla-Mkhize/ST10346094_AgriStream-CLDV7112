using System.Collections.Concurrent;
using System.Diagnostics;

const string baseUrl = "https://st10346094cldvpracticum-hbbxevbefbbygme6.southafricanorth-01.azurewebsites.net/";
const int concurrentUsers = 30;
const int durationSeconds = 900; // 15 min

var handler = new SocketsHttpHandler { MaxConnectionsPerServer = concurrentUsers };
var http = new HttpClient(handler)
{
    BaseAddress = new Uri(baseUrl),
    Timeout = TimeSpan.FromSeconds(30)
};

long total = 0, failed = 0, totalMs = 0, blocked = 0;
var errors = new ConcurrentDictionary<string, long>();
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(durationSeconds));
var overall = Stopwatch.StartNew();

async Task UserLoop()
{
    // ramp users in over 20 seconds
    try { await Task.Delay(Random.Shared.Next(0, 20_000), cts.Token); } catch { return; }

    while (!cts.IsCancellationRequested)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            HttpResponseMessage res;

            if (Random.Shared.Next(3) == 0)
            {
                var form = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["Username"] = "admin",
                    ["Password"] = "wrong-password"
                });
                res = await http.PostAsync("/Account/Login", form, cts.Token);
            }
            else
            {
                var path = Random.Shared.Next(2) == 0 ? "/" : "/Account/Login";
                res = await http.GetAsync(path, cts.Token);
            }

            sw.Stop();
            Interlocked.Increment(ref total);
            Interlocked.Add(ref totalMs, sw.ElapsedMilliseconds);

            if (!res.IsSuccessStatusCode)
            {
                Interlocked.Increment(ref failed);
                errors.AddOrUpdate($"HTTP {(int)res.StatusCode}", 1, (_, v) => v + 1);

                if ((int)res.StatusCode == 403 && Interlocked.Increment(ref blocked) >= 50)
                {
                    Console.WriteLine("!! Server is returning 403s, aborting to avoid a block.");
                    cts.Cancel();
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            break;
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref total);
            Interlocked.Increment(ref failed);
            errors.AddOrUpdate(ex.GetType().Name + ": " + (ex.InnerException?.Message ?? ex.Message), 1, (_, v) => v + 1);
            try { await Task.Delay(200, cts.Token); } catch { break; }
        }

        try { await Task.Delay(100, cts.Token); } catch { break; }
    }
}

async Task Reporter()
{
    while (!cts.IsCancellationRequested)
    {
        try { await Task.Delay(10_000, cts.Token); } catch { break; }
        var t = Interlocked.Read(ref total);
        var f = Interlocked.Read(ref failed);
        var avg = t == 0 ? 0 : Interlocked.Read(ref totalMs) / t;
        Console.WriteLine($"[{overall.Elapsed:mm\\:ss}] requests={t} failed={f} avg={avg}ms rps={t / Math.Max(1, overall.Elapsed.TotalSeconds):F1}");
        foreach (var e in errors) Console.WriteLine($"    {e.Key} = {e.Value}");
    }
}

Console.WriteLine($"Starting: {concurrentUsers} users for {durationSeconds}s against {baseUrl}");

var tasks = Enumerable.Range(0, concurrentUsers).Select(_ => UserLoop()).ToList();
tasks.Add(Reporter());
await Task.WhenAll(tasks);

var totalReq = Interlocked.Read(ref total);
Console.WriteLine("---- DONE ----");
Console.WriteLine($"Total requests: {totalReq}");
Console.WriteLine($"Failed: {Interlocked.Read(ref failed)}");
Console.WriteLine($"Average response: {(totalReq == 0 ? 0 : Interlocked.Read(ref totalMs) / totalReq)} ms");
Console.WriteLine($"Throughput: {totalReq / overall.Elapsed.TotalSeconds:F1} req/s");
foreach (var e in errors) Console.WriteLine($"  {e.Key} = {e.Value}");
Console.WriteLine("Press Enter to close...");
Console.ReadLine();