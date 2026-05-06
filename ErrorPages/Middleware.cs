using Carp.Crimes;
using Carp.Ledger;

namespace Carp.ErrorPages;

/// <summary>
/// Routes a status code to the appropriate ICrimeErrorPage and renders it.
/// </summary>
public sealed class ErrorPageRouter
{
    private readonly Dictionary<int, ICrimeErrorPage> _pages;
    private readonly CrimeLedger _ledger;

    public ErrorPageRouter(IEnumerable<ICrimeErrorPage> pages, CrimeLedger ledger)
    {
        _pages = pages.ToDictionary(p => p.StatusCode);
        _ledger = ledger;
    }

    public bool CanHandle(int statusCode) => _pages.ContainsKey(statusCode);

    public Task RenderAsync(int statusCode, HttpContext http, Dictionary<string, string>? detail = null)
    {
        if (!_pages.TryGetValue(statusCode, out var page))
            return Task.CompletedTask;

        // Look up the recent crimes for this request via X-Crime-Id headers, if any.
        // Simpler: just take the most recent few crimes from the ledger that match
        // this request's path. Imperfect but fine for the demo aesthetic.
        var requestPath = http.Request.Path.Value ?? "";
        var related = _ledger.Recent(50)
            .Where(c => c.Path.StartsWith(requestPath, StringComparison.Ordinal))
            .Take(6)
            .ToList();

        var ctx = new ErrorPageContext
        {
            Http = http,
            StatusCode = statusCode,
            CaseNumber = CrimeId.Generate(),
            At = DateTimeOffset.UtcNow,
            RelatedCrimes = related,
            Detail = detail ?? new Dictionary<string, string>()
        };
        return page.RenderAsync(ctx);
    }
}

/// <summary>
/// Catches non-2xx responses with empty bodies (the YARP default for proxy
/// errors) and replaces them with a rendered case file. This is the magic
/// that turns a 502 from a blank page into a monologue.
/// </summary>
public sealed class CrimeErrorPageMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ErrorPageRouter _router;

    public CrimeErrorPageMiddleware(RequestDelegate next, ErrorPageRouter router)
    {
        _next = next;
        _router = router;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Let the request flow normally first. We need to know what status the
        // proxy or downstream code chose, and whether anything was written.
        var originalBody = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await _next(context);
        }
        finally
        {
            context.Response.Body = originalBody;
        }

        // Decision time. If the response body has content, the application or
        // upstream rendered something — leave it alone. If the body is empty
        // and the status is a "boring" error code we know how to handle, take over.
        var status = context.Response.StatusCode;
        var hasBody = buffer.Length > 0;

        if (!hasBody && _router.CanHandle(status) && !context.Response.HasStarted)
        {
            // Clear the suppressed (empty) body and any content-length carryover.
            context.Response.Headers.ContentLength = null;
            context.Response.Headers.Remove("Content-Type");
            await _router.RenderAsync(status, context, BuildDetail(context, status));
            return;
        }

        // Otherwise, write through what was buffered.
        if (hasBody)
        {
            buffer.Seek(0, SeekOrigin.Begin);
            await buffer.CopyToAsync(originalBody);
        }
    }

    private static Dictionary<string, string> BuildDetail(HttpContext http, int status)
    {
        var d = new Dictionary<string, string>();

        // YARP populates IForwarderErrorFeature when it fails to forward. We look it up
        // by name to avoid a hard compile-time dependency in this file — the project
        // references YARP, but the middleware can stand alone for non-proxy errors too.
        foreach (var feature in http.Features)
        {
            var typeName = feature.Key.FullName ?? "";
            if (typeName.Contains("IForwarderErrorFeature", StringComparison.Ordinal))
            {
                var f = feature.Value;
                if (f is not null)
                {
                    var errorProp = f.GetType().GetProperty("Error");
                    if (errorProp?.GetValue(f) is { } err)
                        d["error_kind"] = err.ToString() ?? "unknown";
                    var exProp = f.GetType().GetProperty("Exception");
                    if (exProp?.GetValue(f) is Exception ex)
                        d["exception"] = ex.GetType().Name;
                }
                break;
            }
        }

        if (http.Request.Headers.TryGetValue("Host", out var host))
            d["host"] = host.ToString();
        return d;
    }
}

public static class ErrorPageEndpoints
{
    /// <summary>
    /// Maps direct-access endpoints for each error page so you can curl them:
    /// /418, /502, /503, /504, /413, /421. Each accepts ?backend=&amp;waited_ms=
    /// query params to populate evidence.
    /// </summary>
    public static void MapErrorPageEndpoints(this IEndpointRouteBuilder app)
    {
        int[] codes = { 418, 502, 503, 504, 413, 421 };
        foreach (var code in codes)
        {
            var captured = code;
            app.MapGet($"/{captured}", async (HttpContext http, ErrorPageRouter router) =>
            {
                var detail = http.Request.Query
                    .ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
                await router.RenderAsync(captured, http, detail);
            });
        }
    }
}
