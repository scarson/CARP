using System.Diagnostics;
using System.Diagnostics.Metrics;
using Carp.Crimes;

namespace Carp.Ledger;

public interface ICrimeReporter
{
    void Report(ICrime crime, CrimeContext ctx, HttpContext? httpForHeaders = null);
}

public sealed class CrimeReporter : ICrimeReporter
{
    private readonly ILogger<CrimeReporter> _log;
    private readonly CrimeLedger _ledger;
    private readonly Counter<long> _crimesCounter;
    private readonly ActivitySource _trace;

    public CrimeReporter(ILogger<CrimeReporter> log, CrimeLedger ledger, IMeterFactory meters)
    {
        _log = log;
        _ledger = ledger;
        var meter = meters.Create("yarp.crimes");
        _crimesCounter = meter.CreateCounter<long>("crimes_committed_total");
        _trace = new ActivitySource("yarp.crimes");
    }

    public void Report(ICrime crime, CrimeContext ctx, HttpContext? httpForHeaders = null)
    {
        var confession = crime.ConfessAs(ctx);

        // 1. Metric
        _crimesCounter.Add(1,
            new KeyValuePair<string, object?>("crime", crime.Name),
            new KeyValuePair<string, object?>("severity", crime.Severity.ToString()));

        // 2. Structured log with editorial message field
        _log.LogWarning(
            "[CRIME] {CrimeId} {Method} {Path} -> {Confession} severity={Severity}",
            ctx.CrimeId, ctx.Method, ctx.RequestPath, confession, crime.Severity);

        // 3. Ledger entry (for /ledger, /honesty, SSE)
        _ledger.Append(new LedgerEntry(
            ctx.CrimeId,
            crime.Name,
            crime.Severity,
            ctx.Method,
            ctx.RequestPath,
            confession,
            ctx.CommittedAt,
            ctx.Evidence
        ));

        // 4. Trace span — span name IS the crime
        using var activity = _trace.StartActivity(crime.Name, ActivityKind.Internal);
        if (activity is not null)
        {
            activity.SetTag("crime.id", ctx.CrimeId);
            activity.SetTag("crime.severity", crime.Severity.ToString());
            foreach (var (k, v) in ctx.Evidence)
                activity.SetTag($"crime.evidence.{k}", v);
        }

        // 5. Confession headers — only if the response hasn't started.
        //    Sanitize for CRLF to prevent header injection from user-influenced evidence.
        if (httpForHeaders is { Response.HasStarted: false })
        {
            try
            {
                var headers = httpForHeaders.Response.Headers;
                // Append confessions/severities (multiple crimes per request stack);
                // Set CrimeId to the *most recent* crime since callers often want one ID.
                headers["X-Crime-Id"] = SanitizeHeader(ctx.CrimeId);
                headers.Append("X-Confession", SanitizeHeader(Truncate(confession, 240)));
                headers.Append("X-Severity", crime.Severity.ToString());
            }
            catch
            {
                // Header writing is best-effort. The crime is still recorded.
            }
        }
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";

    private static string SanitizeHeader(string s)
    {
        // Strip CR/LF defensively even though Kestrel rejects them; belt + suspenders.
        return s.Replace("\r", "").Replace("\n", " ");
    }
}
