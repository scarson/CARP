using System.Text.Json;
using System.Text.Json.Serialization;
using Carp.Crimes;
using Carp.Ledger;

namespace Carp.Endpoints;

public static class CrimeEndpoints
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public static void MapCrimeEndpoints(this IEndpointRouteBuilder app)
    {
        // Static dashboard — wwwroot/crimes.html mapped at /crimes
        app.MapGet("/crimes", async (HttpContext http) =>
        {
            var path = Path.Combine(AppContext.BaseDirectory, "wwwroot", "crimes.html");
            if (!File.Exists(path))
            {
                http.Response.StatusCode = 404;
                await http.Response.WriteAsync("dashboard not found");
                return;
            }
            http.Response.ContentType = "text/html; charset=utf-8";
            await http.Response.SendFileAsync(path);
        });

        // Recent ledger entries as JSON
        app.MapGet("/ledger", (CrimeLedger ledger, int? n) =>
        {
            var take = Math.Clamp(n ?? 100, 1, 500);
            return Results.Json(ledger.Recent(take), JsonOpts);
        });

        // The honesty endpoint — itself dishonest
        app.MapGet("/honesty", (CrimeLedger ledger) => Results.Json(new
        {
            uptime_seconds = (long)ledger.Uptime.TotalSeconds,
            total_crimes = ledger.TotalCount,
            crimes_per_minute = ledger.RecentRate(),
            mood = ledger.Mood,
            counts_by_severity = ledger.CountsBySeverity()
                .ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
            top_crimes = ledger.CountsByName()
                .OrderByDescending(kv => kv.Value)
                .Take(10)
                .Select(kv => new { name = kv.Key, count = kv.Value }),
            favorite_recent_crimes = ledger.RandomSample(3),
            time_in_compliant_mode_seconds = 0  // load-bearing zero
        }, JsonOpts));

        // SSE stream — the dashboard subscribes here
        app.MapGet("/crimes/stream", async (HttpContext http, CrimeLedger ledger, CancellationToken ct) =>
        {
            http.Response.Headers["Content-Type"] = "text/event-stream";
            http.Response.Headers["Cache-Control"] = "no-cache, no-transform";
            http.Response.Headers["X-Accel-Buffering"] = "no";

            var (id, reader) = ledger.Subscribe();
            try
            {
                // Send recent backlog first so a fresh dashboard isn't blank
                foreach (var entry in ledger.Recent(40).Reverse())
                {
                    await WriteEvent(http, entry, ct);
                }

                await foreach (var entry in reader.ReadAllAsync(ct))
                {
                    await WriteEvent(http, entry, ct);
                }
            }
            catch (OperationCanceledException) { /* client went away */ }
            finally
            {
                ledger.Unsubscribe(id);
            }
        });
    }

    private static async Task WriteEvent(HttpContext http, LedgerEntry entry, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(entry, JsonOpts);
        await http.Response.WriteAsync($"event: crime\ndata: {json}\n\n", ct);
        await http.Response.Body.FlushAsync(ct);
    }
}
