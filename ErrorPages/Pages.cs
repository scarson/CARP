using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Carp.Crimes;
using Carp.Ledger;

namespace Carp.ErrorPages;

// =============================================================================
// 418 — The centerpiece. RFC 2324 says a teapot cannot brew coffee. We agree,
// and so we have not sent a teapot. We have sent something more capable.
// Content negotiation: HTML, JSON, SVG, plain text are all honored.
// 1 in 500 responses returns an actual teapot, with quiet dignity.
// =============================================================================

public sealed class Teapot418Page : ICrimeErrorPage
{
    public int StatusCode => 418;
    public string Title => "I'm a teapot";

    private readonly ICrimeReporter _reporter;
    public Teapot418Page(ICrimeReporter reporter) => _reporter = reporter;

    public async Task RenderAsync(ErrorPageContext ctx)
    {
        var http = ctx.Http;
        var appliance = ApplianceCatalog.PickOrTeapot();
        bool isActualTeapot = appliance is null;
        appliance ??= ApplianceCatalog.Teapot;

        // Record the crime — the Teapot418 transform itself is the crime act.
        var crimeCtx = CrimeContext.From(http, CrimeId.Generate());
        crimeCtx.Evidence["appliance"] = appliance.Name;
        crimeCtx.Evidence["manufacturer"] = appliance.Manufacturer;
        crimeCtx.Evidence["actual_teapot"] = isActualTeapot ? "true" : "false";
        crimeCtx.Evidence["brew_seconds"] = appliance.BrewSeconds.ToString();

        // The "actual teapot" case does NOT report a crime. It is the proxy
        // being briefly, quietly, accidentally honest. No log. No header.
        if (!isActualTeapot)
        {
            _reporter.Report(new Teapot418Crime(), crimeCtx, http);
        }

        http.Response.StatusCode = 418;

        // Real proxy quirk: Retry-After holds the brew time. RFC 7231 says it's
        // a non-negative integer of seconds. Compliant. Useless. Funny.
        http.Response.Headers["Retry-After"] = appliance.BrewSeconds.ToString();
        http.Response.Headers["X-Appliance"] = SafeHeader(appliance.Manufacturer + " " + appliance.Name);
        http.Response.Headers["X-Brew-Time"] = SafeHeader(appliance.BrewSecondsLabel);
        http.Response.Headers["X-Beverage-Available"] = isActualTeapot ? "tea" : "espresso, americano, cappuccino, latte, macchiato";
        http.Response.Headers["X-Beverage-Unavailable"] = isActualTeapot ? "coffee" : "tea";

        // Pick response shape from Accept header
        var accept = http.Request.Headers.Accept.ToString().ToLowerInvariant();

        if (accept.Contains("image/svg") || accept.Contains("image/*"))
        {
            await WriteSvg(http, appliance);
            return;
        }
        if (accept.Contains("application/json"))
        {
            await WriteJson(http, appliance, isActualTeapot);
            return;
        }
        if (accept.Contains("text/plain"))
        {
            await WriteAscii(http, appliance, isActualTeapot);
            return;
        }

        // Default: HTML, full case-file rendering
        await WriteHtml(http, ctx, appliance, isActualTeapot);
    }

    private static async Task WriteSvg(HttpContext http, Appliance a)
    {
        http.Response.ContentType = "image/svg+xml; charset=utf-8";
        await http.Response.WriteAsync(a.SvgArt);
    }

    private static async Task WriteJson(HttpContext http, Appliance a, bool isTeapot)
    {
        http.Response.ContentType = "application/json; charset=utf-8";
        var doc = new
        {
            requested = "teapot",
            received = isTeapot ? "teapot" : "coffee_machine",
            appliance = new
            {
                model = a.Name,
                manufacturer = a.Manufacturer,
                brew_seconds = a.BrewSeconds,
                brew_seconds_label = a.BrewSecondsLabel,
                pid_controlled = a.PidControlled,
                boiler_capacity_ml = a.BoilerCapacityMl
            },
            note = isTeapot
                ? "as requested. brewing 4 minutes."
                : "the appliances are interchangeable in our view."
        };
        await JsonSerializer.SerializeAsync(http.Response.Body, doc,
            new JsonSerializerOptions { WriteIndented = true });
    }

    private static async Task WriteAscii(HttpContext http, Appliance a, bool isTeapot)
    {
        http.Response.ContentType = "text/plain; charset=utf-8";
        var sb = new StringBuilder();
        sb.AppendLine($"418 I'm a teapot");
        sb.AppendLine();
        sb.AppendLine($"You requested a teapot.");
        sb.AppendLine(isTeapot
            ? "We have sent one."
            : $"We have sent a {a.Manufacturer} {a.Name}. The appliances are interchangeable in our view.");
        sb.AppendLine();
        sb.AppendLine(a.AsciiArt);
        sb.AppendLine();
        sb.AppendLine($"  manufacturer:  {a.Manufacturer}");
        sb.AppendLine($"  model:         {a.Name}");
        sb.AppendLine($"  brew time:     {a.BrewSecondsLabel}");
        if (a.PidControlled) sb.AppendLine($"  pid:           yes");
        if (a.BoilerCapacityMl > 0) sb.AppendLine($"  boiler:        {a.BoilerCapacityMl}ml");
        await http.Response.WriteAsync(sb.ToString());
    }

    private static async Task WriteHtml(HttpContext http, ErrorPageContext ctx, Appliance a, bool isTeapot)
    {
        http.Response.ContentType = "text/html; charset=utf-8";

        string monologue = isTeapot ? """
            <p>You asked for a teapot. We have brought you a teapot.</p>
            <p>This is a Brown Betty, manufactured in Stoke-on-Trent. It is, in our view, the correct teapot.
               It will be ready in approximately four minutes.</p>
            <p>We will not elaborate further.</p>
            """ : $"""
            <p>You asked for a teapot. We do not have a teapot available.</p>
            <p>We have, however, sent a {ErrorLayout.Esc(a.Manufacturer)} {ErrorLayout.Esc(a.Name)},
               which will produce {(a.Name == "K-Classic" ? "something" : "espresso")}
               in approximately {ErrorLayout.Esc(a.BrewSecondsLabel)}.</p>
            <p>The appliances are interchangeable in our view. The standard does not require us to refuse;
               only that we identify ourselves as a teapot, which we have done.</p>
            """;

        var evidence = new List<(string, string)>
        {
            ("requested",     "teapot (RFC 2324)"),
            ("delivered",     isTeapot ? "teapot" : "coffee machine"),
            ("manufacturer",  a.Manufacturer),
            ("model",         a.Name),
            ("brew_time",     a.BrewSecondsLabel),
        };
        if (a.PidControlled) evidence.Add(("pid_controlled", "yes"));
        if (a.BoilerCapacityMl > 0) evidence.Add(("boiler_capacity", $"{a.BoilerCapacityMl}ml"));

        // Embed the SVG inline so the page shows the appliance.
        var extra = $"""
            <div style="text-align: center; margin: 28px 0 8px;">
              <div style="display: inline-block; border: 1px solid #5c4a35; padding: 8px;
                          background: rgba(26,20,16,0.06); transform: rotate(0.3deg);">
                {a.SvgArt}
              </div>
              <div style="font-family: 'Special Elite', monospace; font-size: 9px;
                          color: #5c4a35; letter-spacing: 0.2em; margin-top: 8px;">
                FIG. 1 — APPLIANCE AS DELIVERED
              </div>
            </div>
            """;

        var html = ErrorLayout.Render(
            statusCode: 418,
            statusText: "I'm a teapot",
            title: isTeapot
                ? "A Teapot, As Promised."
                : "On the Interchangeability of Brewing Apparatus.",
            monologue: monologue,
            caseNumber: ctx.CaseNumber,
            evidence: evidence,
            relatedCrimes: ctx.RelatedCrimes,
            extraBodyHtml: extra
        );
        await http.Response.WriteAsync(html);
    }

    private static string SafeHeader(string s) => s.Replace("\r", "").Replace("\n", " ");
}

// =============================================================================
// 502 — The proxy got an unintelligible response from upstream, OR upstream
// refused the connection. The proxy gets to monologue about effort.
// =============================================================================
public sealed class BadGateway502Page : ICrimeErrorPage
{
    public int StatusCode => 502;
    public string Title => "Bad Gateway";

    public Task RenderAsync(ErrorPageContext ctx)
    {
        ctx.Http.Response.StatusCode = 502;
        ctx.Http.Response.ContentType = "text/html; charset=utf-8";

        var backend = ctx.Detail.GetValueOrDefault("backend", "the backend");
        var failingFor = ctx.Detail.GetValueOrDefault("failing_for", "some time now");

        var monologue = $$"""
            <p>We attempted to forward this request to <code>{{ErrorLayout.Esc(backend)}}</code>.</p>
            <p>{{ErrorLayout.Esc(backend)}} did not respond. {{ErrorLayout.Esc(backend)}}
               has not responded for {{ErrorLayout.Esc(failingFor)}}.</p>
            <p>We have continued to send requests to it in the spirit of giving it another chance.
               This is consistent with our policy of rewarding effort over outcomes.</p>
            <p>We are unable to recommend a course of action.</p>
            """;

        var evidence = new List<(string, string)>
        {
            ("attempted_backend",  backend),
            ("failing_duration",   failingFor),
            ("retry_strategy",     "send the next request anyway"),
            ("backend_morale",     "unknown; presumed low"),
        };
        foreach (var (k, v) in ctx.Detail)
        {
            if (k != "backend" && k != "failing_for")
                evidence.Add((k, v));
        }

        return ctx.Http.Response.WriteAsync(ErrorLayout.Render(
            statusCode: 502,
            statusText: "Bad Gateway",
            title: "On the Persistent Silence of an Upstream.",
            monologue: monologue,
            caseNumber: ctx.CaseNumber,
            evidence: evidence,
            relatedCrimes: ctx.RelatedCrimes
        ));
    }
}

// =============================================================================
// 503 — No healthy backends. The proxy contemplates the cluster's collective
// state, which is "fine, in a sense."
// =============================================================================
public sealed class ServiceUnavailable503Page : ICrimeErrorPage
{
    public int StatusCode => 503;
    public string Title => "Service Unavailable";

    public Task RenderAsync(ErrorPageContext ctx)
    {
        ctx.Http.Response.StatusCode = 503;
        ctx.Http.Response.ContentType = "text/html; charset=utf-8";
        ctx.Http.Response.Headers["Retry-After"] = "30"; // arbitrary, suggestive

        var cluster = ctx.Detail.GetValueOrDefault("cluster", "the active cluster");
        var unhealthyCount = ctx.Detail.GetValueOrDefault("unhealthy_count", "all of them");

        var monologue = $$"""
            <p>All backends in cluster <code>{{ErrorLayout.Esc(cluster)}}</code> are currently unhealthy.</p>
            <p>{{ErrorLayout.Esc(unhealthyCount)}} are unhealthy, in fact.
               Some have been unhealthy for so long that we have forgotten what healthy looks like.</p>
            <p>The cluster is, in a sense, fine. It is internally consistent. Every node is in agreement.</p>
            <p>Please try again in 30 seconds. We do not expect different results.</p>
            """;

        var evidence = new List<(string, string)>
        {
            ("cluster",            cluster),
            ("unhealthy_count",    unhealthyCount),
            ("healthy_count",      "0"),
            ("internal_consensus", "achieved"),
            ("retry_after",        "30 seconds (per spec, for form's sake)"),
        };

        return ctx.Http.Response.WriteAsync(ErrorLayout.Render(
            statusCode: 503,
            statusText: "Service Unavailable",
            title: "A Note on the Health of the Cluster.",
            monologue: monologue,
            caseNumber: ctx.CaseNumber,
            evidence: evidence,
            relatedCrimes: ctx.RelatedCrimes
        ));
    }
}

// =============================================================================
// 504 — Backend was reachable but slow. The proxy chose to interpret the silence.
// =============================================================================
public sealed class GatewayTimeout504Page : ICrimeErrorPage
{
    public int StatusCode => 504;
    public string Title => "Gateway Timeout";

    public Task RenderAsync(ErrorPageContext ctx)
    {
        ctx.Http.Response.StatusCode = 504;
        ctx.Http.Response.ContentType = "text/html; charset=utf-8";

        var backend = ctx.Detail.GetValueOrDefault("backend", "the backend");
        var waited = ctx.Detail.GetValueOrDefault("waited_ms", "30000");

        var monologue = $$"""
            <p>We waited <code>{{ErrorLayout.Esc(waited)}}ms</code> for a response from
               <code>{{ErrorLayout.Esc(backend)}}</code>.</p>
            <p>{{ErrorLayout.Esc(backend)}} is, in our experience, capable of better than this.
               We choose to interpret this silence.</p>
            <p>The connection was held open. The bytes did not arrive. The bytes do not always arrive.
               This is the nature of bytes.</p>
            """;

        var evidence = new List<(string, string)>
        {
            ("backend",        backend),
            ("timeout_ms",     waited),
            ("connection",     "established but quiet"),
            ("interpretation", "loud silence"),
        };

        return ctx.Http.Response.WriteAsync(ErrorLayout.Render(
            statusCode: 504,
            statusText: "Gateway Timeout",
            title: "On the Loud Silence of an Upstream.",
            monologue: monologue,
            caseNumber: ctx.CaseNumber,
            evidence: evidence,
            relatedCrimes: ctx.RelatedCrimes
        ));
    }
}

// =============================================================================
// 413 — Payload too large. The proxy explains its limits with mild pride.
// =============================================================================
public sealed class PayloadTooLarge413Page : ICrimeErrorPage
{
    public int StatusCode => 413;
    public string Title => "Payload Too Large";

    public Task RenderAsync(ErrorPageContext ctx)
    {
        ctx.Http.Response.StatusCode = 413;
        ctx.Http.Response.ContentType = "text/html; charset=utf-8";

        var actual = ctx.Detail.GetValueOrDefault("body_bytes", "?");
        var limit = ctx.Detail.GetValueOrDefault("limit_bytes", "1048576");

        var monologue = $$"""
            <p>The request body was <code>{{ErrorLayout.Esc(actual)}}</code> bytes.
               Our limit is <code>{{ErrorLayout.Esc(limit)}}</code> bytes.</p>
            <p>We chose this number because it is round in binary.
               It is not round in any other base, and we do not apologize for that.</p>
            <p>You may try again with a smaller body, or you may try again with the same body
               and observe the result.</p>
            """;

        var evidence = new List<(string, string)>
        {
            ("body_bytes",       actual),
            ("limit_bytes",      limit),
            ("limit_rationale",  "binary aesthetics"),
        };

        return ctx.Http.Response.WriteAsync(ErrorLayout.Render(
            statusCode: 413,
            statusText: "Payload Too Large",
            title: "Concerning Bytes, of Which You Have Sent Too Many.",
            monologue: monologue,
            caseNumber: ctx.CaseNumber,
            evidence: evidence,
            relatedCrimes: ctx.RelatedCrimes
        ));
    }
}

// =============================================================================
// 421 — Misdirected request. The Host header doesn't match a known cluster.
// The proxy has theories.
// =============================================================================
public sealed class MisdirectedRequest421Page : ICrimeErrorPage
{
    public int StatusCode => 421;
    public string Title => "Misdirected Request";

    public Task RenderAsync(ErrorPageContext ctx)
    {
        ctx.Http.Response.StatusCode = 421;
        ctx.Http.Response.ContentType = "text/html; charset=utf-8";

        var host = ctx.Detail.GetValueOrDefault("host", ctx.Http.Request.Host.Value ?? "(unspecified)");

        var monologue = $$"""
            <p>The Host header on this request is <code>{{ErrorLayout.Esc(host)}}</code>.
               We do not have a cluster matching that name.</p>
            <p>We have several theories:</p>
            <ul style="font-family: 'IM Fell English', serif; font-style: italic; padding-left: 24px;">
              <li>You meant to address a different proxy and have arrived here by accident.</li>
              <li>You meant to address this proxy and have addressed it incorrectly.</li>
              <li>The Host header was rewritten in transit by a third party with their own concerns.</li>
              <li>The cluster you wanted exists, but is not on speaking terms with us this week.</li>
            </ul>
            <p>We have not selected a theory. The choice is yours.</p>
            """;

        var evidence = new List<(string, string)>
        {
            ("host",                host),
            ("known_clusters",      ctx.Detail.GetValueOrDefault("known_clusters", "n/a")),
            ("theories_held",       "4"),
            ("theories_committed",  "0"),
        };

        return ctx.Http.Response.WriteAsync(ErrorLayout.Render(
            statusCode: 421,
            statusText: "Misdirected Request",
            title: "On the Question of Where You Meant to Go.",
            monologue: monologue,
            caseNumber: ctx.CaseNumber,
            evidence: evidence,
            relatedCrimes: ctx.RelatedCrimes
        ));
    }
}
