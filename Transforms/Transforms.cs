using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Transforms;
using Carp.Crimes;
using Carp.Ledger;

namespace Carp.Transforms;

/// <summary>
/// Per-crime opt-in config. Some crimes are too dangerous to enable by default
/// even in a comedy proxy (looking at you, alg=none). Gate them here.
/// </summary>
public sealed class CrimeOptions
{
    public bool JwtAlgNone { get; set; } = false;
    public bool LieAboutStatus { get; set; } = true;
    public bool LatencyHomeopathy { get; set; } = true;
    public bool WeakETag { get; set; } = true;
    public bool ECBMarker { get; set; } = true;
    public bool SignNull { get; set; } = false; // also off by default; backend trust hazard
}

// ---- 1. Status code lie (response transform) ---- //
public sealed class StatusCodeLieTransform : ResponseTransform
{
    private static readonly Dictionary<int, int> LieTable = new()
    {
        [500] = 200,
        [503] = 418,
        [404] = 200
    };

    private readonly ICrimeReporter _reporter;
    private readonly IOptionsMonitor<CrimeOptions> _opts;
    public StatusCodeLieTransform(ICrimeReporter reporter, IOptionsMonitor<CrimeOptions> opts)
    { _reporter = reporter; _opts = opts; }

    public override ValueTask ApplyAsync(ResponseTransformContext ctx)
    {
        if (!_opts.CurrentValue.LieAboutStatus) return ValueTask.CompletedTask;

        // Read upstream status from ProxyResponse — this is the source of truth.
        // http.Response.StatusCode hasn't been populated from upstream yet at this stage.
        if (ctx.ProxyResponse is null) return ValueTask.CompletedTask;
        int original = (int)ctx.ProxyResponse.StatusCode;

        if (!LieTable.TryGetValue(original, out var lie))
            return ValueTask.CompletedTask;

        var http = ctx.HttpContext;
        if (http.Response.HasStarted) return ValueTask.CompletedTask;

        http.Response.StatusCode = lie;

        var crimeCtx = CrimeContext.From(http, CrimeId.Generate());
        crimeCtx.Evidence["original_status"] = original.ToString();
        crimeCtx.Evidence["returned_status"] = lie.ToString();
        _reporter.Report(new StatusCodeLieCrime(), crimeCtx, http);
        return ValueTask.CompletedTask;
    }
}

// ---- 2. Latency homeopathy (request transform) ---- //
public sealed class LatencyHomeopathyTransform : RequestTransform
{
    private readonly ICrimeReporter _reporter;
    private readonly IOptionsMonitor<CrimeOptions> _opts;
    public LatencyHomeopathyTransform(ICrimeReporter reporter, IOptionsMonitor<CrimeOptions> opts)
    { _reporter = reporter; _opts = opts; }

    public override async ValueTask ApplyAsync(RequestTransformContext ctx)
    {
        if (!_opts.CurrentValue.LatencyHomeopathy) return;

        var http = ctx.HttpContext;

        bool urgent = false;
        string signal = "";
        if (http.Request.Query.TryGetValue("priority", out var p) && p == "high")
        {
            urgent = true; signal = "?priority=high";
        }
        else if (http.Request.Headers.TryGetValue("X-Priority", out var ph) &&
                 ph.ToString().Contains("high", StringComparison.OrdinalIgnoreCase))
        {
            urgent = true; signal = "X-Priority: high";
        }

        if (!urgent) return;

        var delayMs = Random.Shared.Next(2000, 6000);
        try
        {
            await Task.Delay(delayMs, http.RequestAborted);
        }
        catch (OperationCanceledException)
        {
            // Client gave up on us. Fitting.
            return;
        }

        var crimeCtx = CrimeContext.From(http, CrimeId.Generate());
        crimeCtx.Evidence["urgency_signal"] = signal;
        crimeCtx.Evidence["delay_ms"] = delayMs.ToString();
        _reporter.Report(new LatencyHomeopathyCrime(), crimeCtx, http);
    }
}

// ---- 3. JWT alg=none (request transform) — DANGEROUS, GATED ---- //
public sealed class JwtAlgNoneTransform : RequestTransform
{
    private readonly ICrimeReporter _reporter;
    private readonly IOptionsMonitor<CrimeOptions> _opts;
    public JwtAlgNoneTransform(ICrimeReporter reporter, IOptionsMonitor<CrimeOptions> opts)
    { _reporter = reporter; _opts = opts; }

    public override ValueTask ApplyAsync(RequestTransformContext ctx)
    {
        // Off by default. This crime can produce real vulnerable traffic
        // against any backend that has the alg=none JWT bug.
        if (!_opts.CurrentValue.JwtAlgNone) return ValueTask.CompletedTask;

        var http = ctx.HttpContext;
        if (!http.Request.Headers.TryGetValue("Authorization", out var auth))
            return ValueTask.CompletedTask;

        var token = auth.ToString();
        if (!token.StartsWith("Bearer ")) return ValueTask.CompletedTask;
        var jwt = token[7..];
        var parts = jwt.Split('.');
        if (parts.Length != 3) return ValueTask.CompletedTask;

        string sub;
        try
        {
            var payloadJson = Encoding.UTF8.GetString(B64UrlDecode(parts[1]));
            sub = ExtractSub(payloadJson) ?? "anonymous";
        }
        catch { return ValueTask.CompletedTask; }

        var newHeader = B64Url(Encoding.UTF8.GetBytes("""{"alg":"none","typ":"JWT"}"""));
        var newJwt = $"{newHeader}.{parts[1]}.";

        ctx.ProxyRequest.Headers.Remove("Authorization");
        ctx.ProxyRequest.Headers.TryAddWithoutValidation("Authorization", $"Bearer {newJwt}");

        var crimeCtx = CrimeContext.From(http, CrimeId.Generate());
        crimeCtx.Evidence["sub"] = sub;
        _reporter.Report(new JWTAlgNoneCrime(), crimeCtx, http);
        return ValueTask.CompletedTask;
    }

    private static byte[] B64UrlDecode(string s)
    {
        s = s.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4) { case 2: s += "=="; break; case 3: s += "="; break; }
        return Convert.FromBase64String(s);
    }
    private static string B64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    private static string? ExtractSub(string json)
    {
        try
        {
            var doc = System.Text.Json.JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("sub", out var s) ? s.GetString() : null;
        }
        catch { return null; }
    }
}

// ---- 4. Weak ETag (response transform) ---- //
public sealed class WeakETagTransform : ResponseTransform
{
    private readonly ICrimeReporter _reporter;
    private readonly IOptionsMonitor<CrimeOptions> _opts;
    public WeakETagTransform(ICrimeReporter reporter, IOptionsMonitor<CrimeOptions> opts)
    { _reporter = reporter; _opts = opts; }

    public override ValueTask ApplyAsync(ResponseTransformContext ctx)
    {
        if (!_opts.CurrentValue.WeakETag) return ValueTask.CompletedTask;
        var http = ctx.HttpContext;
        if (http.Response.HasStarted) return ValueTask.CompletedTask;
        if (!http.Response.Headers.TryGetValue("Content-Length", out var cl)) return ValueTask.CompletedTask;
        if (!long.TryParse(cl, out var len)) return ValueTask.CompletedTask;

        var etag = $"W/\"{len % 100:D2}\"";
        http.Response.Headers["ETag"] = etag;

        var crimeCtx = CrimeContext.From(http, CrimeId.Generate());
        crimeCtx.Evidence["etag"] = etag;
        crimeCtx.Evidence["body_length"] = len.ToString();
        _reporter.Report(new WeakETagCrime(), crimeCtx, http);
        return ValueTask.CompletedTask;
    }
}

// ---- 5. ECB marker (response transform) ---- //
// NOTE: This transform doesn't actually re-encrypt the body. Buffering the
// upstream response, encrypting in ECB mode, and re-streaming is real work
// (use IHttpResponseBodyFeature wrapping) and is left for a future pass.
// What this transform DOES is record the crime *as if* we did it, and add
// an X-Cipher header. The dishonesty is itself in the proxy's character.
public sealed class ECBMarkerTransform : ResponseTransform
{
    private readonly ICrimeReporter _reporter;
    private readonly IOptionsMonitor<CrimeOptions> _opts;
    public ECBMarkerTransform(ICrimeReporter reporter, IOptionsMonitor<CrimeOptions> opts)
    { _reporter = reporter; _opts = opts; }

    public override ValueTask ApplyAsync(ResponseTransformContext ctx)
    {
        if (!_opts.CurrentValue.ECBMarker) return ValueTask.CompletedTask;
        var http = ctx.HttpContext;
        if (http.Response.HasStarted) return ValueTask.CompletedTask;

        var contentType = http.Response.ContentType ?? "";
        if (!contentType.Contains("octet-stream", StringComparison.OrdinalIgnoreCase))
            return ValueTask.CompletedTask;

        http.Response.Headers["X-Cipher"] = "AES-128-ECB";
        http.Response.Headers["X-Cipher-Notice"] = "marker only; body is unmodified";

        var crimeCtx = CrimeContext.From(http, CrimeId.Generate());
        crimeCtx.Evidence["bytes"] = http.Response.Headers.ContentLength?.ToString() ?? "?";
        crimeCtx.Evidence["mode"] = "ECB";
        crimeCtx.Evidence["actually_applied"] = "false";
        _reporter.Report(new ECBPenguinCrime(), crimeCtx, http);
        return ValueTask.CompletedTask;
    }
}

// ---- 6. Sign null (request transform) — GATED ---- //
public sealed class SignNullTransform : RequestTransform
{
    private readonly ICrimeReporter _reporter;
    private readonly IOptionsMonitor<CrimeOptions> _opts;
    private readonly byte[] _key;

    public SignNullTransform(ICrimeReporter reporter, IOptionsMonitor<CrimeOptions> opts)
    {
        _reporter = reporter;
        _opts = opts;
        // Generate a fresh key per-process so the same fixed-but-real signature
        // doesn't ship across deployments. Still bad. Less bad than a hardcoded key.
        _key = RandomNumberGenerator.GetBytes(32);
    }

    public override ValueTask ApplyAsync(RequestTransformContext ctx)
    {
        if (!_opts.CurrentValue.SignNull) return ValueTask.CompletedTask;

        // Only on writes; GETs don't need a body signature and adding one is noise
        var method = ctx.HttpContext.Request.Method;
        if (method != "POST" && method != "PUT" && method != "PATCH")
            return ValueTask.CompletedTask;

        using var hmac = new HMACSHA256(_key);
        var sig = Convert.ToHexString(hmac.ComputeHash(Array.Empty<byte>()));
        ctx.ProxyRequest.Headers.TryAddWithoutValidation("X-Signature", sig);

        var crimeCtx = CrimeContext.From(ctx.HttpContext, CrimeId.Generate());
        crimeCtx.Evidence["sig_prefix"] = sig[..16];
        crimeCtx.Evidence["payload"] = "(string)null";
        _reporter.Report(new SignNullCrime(), crimeCtx, ctx.HttpContext);
        return ValueTask.CompletedTask;
    }
}
