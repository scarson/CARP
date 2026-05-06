using System.Text;
using Carp.Crimes;
using Carp.Ledger;

namespace Carp.ErrorPages;

/// <summary>
/// Render context for an error page. Bundles up everything a page might want
/// to put on the page: the request, the recent crimes from this request,
/// any backend-specific detail, and a fresh case number.
/// </summary>
public sealed class ErrorPageContext
{
    public required HttpContext Http { get; init; }
    public required int StatusCode { get; init; }
    public required string CaseNumber { get; init; }
    public required DateTimeOffset At { get; init; }

    /// <summary>Recent crimes (up to ~10) related to this request, if known.</summary>
    public IReadOnlyList<LedgerEntry> RelatedCrimes { get; init; } = Array.Empty<LedgerEntry>();

    /// <summary>Free-form per-error evidence (e.g. attempted backends, body sizes).</summary>
    public Dictionary<string, string> Detail { get; init; } = new();
}

public interface ICrimeErrorPage
{
    int StatusCode { get; }
    string Title { get; }
    Task RenderAsync(ErrorPageContext ctx);
}

// =============================================================================
// Shared layout: every error page is a "case file" rendered in the project's
// amber-on-felt aesthetic, with the specific monologue varying per page.
// =============================================================================

internal static class ErrorLayout
{
    public static string Render(
        int statusCode,
        string statusText,
        string title,
        string monologue,
        string caseNumber,
        IReadOnlyList<(string label, string value)> evidence,
        IReadOnlyList<LedgerEntry> relatedCrimes,
        string? extraBodyHtml = null)
    {
        var ev = new StringBuilder();
        foreach (var (label, value) in evidence)
        {
            ev.AppendLine($"      <div class=\"ev-row\"><dt>{Esc(label)}</dt><dd>{Esc(value)}</dd></div>");
        }

        var rel = new StringBuilder();
        if (relatedCrimes.Count == 0)
        {
            rel.AppendLine("      <div class=\"rel-empty\">No prior crimes recorded for this request.</div>");
        }
        else
        {
            foreach (var c in relatedCrimes.Take(6))
            {
                rel.AppendLine($"""
                      <div class="rel-row">
                        <span class="rel-id">{Esc(c.CrimeId)}</span>
                        <span class="rel-name">{Esc(c.CrimeName)}</span>
                        <span class="rel-sev sev-{SevClass(c.Severity)}">{Esc(SevLabel(c.Severity))}</span>
                        <div class="rel-conf">{Esc(c.Confession)}</div>
                      </div>
                """);
            }
        }

        return $$"""
            <!doctype html>
            <html lang="en">
            <head>
            <meta charset="utf-8">
            <title>{{statusCode}} {{Esc(statusText)}} · CARP</title>
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <link rel="preconnect" href="https://fonts.googleapis.com">
            <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
            <link href="https://fonts.googleapis.com/css2?family=Special+Elite&family=IM+Fell+English:ital@0;1&family=JetBrains+Mono:wght@400;700&display=swap" rel="stylesheet">
            <style>
              :root {
                --felt:        #1a1410;
                --felt-deep:   #0d0907;
                --paper:       #f0e6d2;
                --paper-shade: #d9cdb1;
                --ink:         #2a1f15;
                --ink-faded:   #5c4a35;
                --ink-soft:    #7a6549;
                --amber:       #ffb347;
                --amber-bright:#ffd27f;
                --amber-deep:  #c47a1c;
                --blood:       #8b2c1c;
                --stamp-red:   #a83228;
              }
              * { box-sizing: border-box; }
              html, body {
                margin: 0;
                background: var(--felt-deep);
                color: var(--paper);
                font-family: 'JetBrains Mono', monospace;
                min-height: 100vh;
              }
              body::before {
                content: "";
                position: fixed; inset: 0;
                background:
                  radial-gradient(ellipse at 20% 10%, rgba(196,122,28,0.08), transparent 50%),
                  radial-gradient(ellipse at 80% 90%, rgba(139,44,28,0.06), transparent 50%),
                  var(--felt);
                z-index: -2;
              }
              body::after {
                content: ""; position: fixed; inset: 0;
                background-image: url("data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='200' height='200'><filter id='n'><feTurbulence type='fractalNoise' baseFrequency='0.9' numOctaves='2'/><feColorMatrix values='0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.18 0'/></filter><rect width='200' height='200' filter='url(%23n)'/></svg>");
                pointer-events: none; z-index: 100;
                opacity: 0.5; mix-blend-mode: overlay;
              }
              .scanlines {
                position: fixed; inset: 0;
                background: repeating-linear-gradient(0deg,
                  rgba(0,0,0,0) 0, rgba(0,0,0,0) 2px,
                  rgba(0,0,0,0.07) 3px, rgba(0,0,0,0) 4px);
                pointer-events: none; z-index: 99;
              }

              .wrap {
                max-width: 760px;
                margin: 60px auto 80px;
                padding: 0 24px;
                animation: fadeIn 0.5s ease-out;
              }
              @keyframes fadeIn {
                from { opacity: 0; transform: translateY(8px); }
                to   { opacity: 1; transform: translateY(0); }
              }

              .precinct-bar {
                font-family: 'Special Elite', monospace;
                font-size: 10px;
                letter-spacing: 0.3em;
                color: var(--amber);
                text-align: center;
                padding-bottom: 18px;
                border-bottom: 1px solid var(--amber-deep);
              }
              .precinct-bar .small { color: var(--ink-soft); display: block; margin-top: 4px; font-size: 8px; }

              .case-file {
                background: var(--paper);
                color: var(--ink);
                margin-top: 36px;
                padding: 44px 48px 36px;
                position: relative;
                transform: rotate(-0.4deg);
                box-shadow:
                  0 1px 0 rgba(255,255,255,0.08),
                  0 22px 48px rgba(0,0,0,0.55),
                  0 4px 12px rgba(0,0,0,0.4);
                background-image: repeating-linear-gradient(
                  transparent 0, transparent 26px,
                  rgba(92,74,53,0.07) 27px);
                background-size: 100% 28px;
              }

              .stamp {
                position: absolute;
                top: -14px;
                right: 28px;
                color: var(--stamp-red);
                border: 3px solid var(--stamp-red);
                padding: 6px 16px;
                font-family: 'Special Elite', monospace;
                font-size: 13px;
                letter-spacing: 0.22em;
                transform: rotate(8deg);
                background: rgba(240,230,210,0.55);
              }

              .status-line {
                font-family: 'Special Elite', monospace;
                font-size: 11px;
                letter-spacing: 0.32em;
                color: var(--ink-faded);
                margin-bottom: 6px;
                text-transform: uppercase;
              }
              .status-code {
                font-family: 'IM Fell English', serif;
                font-style: italic;
                font-size: 96px;
                line-height: 1;
                color: var(--ink);
                margin: 0 0 4px;
                letter-spacing: -0.02em;
              }
              .status-text {
                font-family: 'IM Fell English', serif;
                font-size: 22px;
                color: var(--blood);
                margin: 0 0 22px;
              }

              h1 {
                font-family: 'IM Fell English', serif;
                font-size: 38px;
                color: var(--ink);
                margin: 26px 0 16px;
                line-height: 1.1;
                font-style: italic;
              }

              .monologue {
                font-family: 'IM Fell English', serif;
                font-size: 18px;
                line-height: 1.55;
                color: var(--ink);
                padding: 8px 0 22px;
                border-bottom: 1px dashed var(--ink-faded);
                position: relative;
              }
              .monologue::before {
                content: "";
                position: absolute;
                left: -14px; top: 12px;
                width: 4px; height: calc(100% - 30px);
                background: var(--blood);
                opacity: 0.6;
              }
              .monologue p { margin: 0 0 12px; }
              .monologue p:last-child { margin-bottom: 0; }

              .section-title {
                font-family: 'Special Elite', monospace;
                font-size: 10px;
                letter-spacing: 0.3em;
                text-transform: uppercase;
                color: var(--ink-faded);
                margin: 26px 0 10px;
              }

              .evidence dl {
                display: grid;
                grid-template-columns: max-content 1fr;
                gap: 4px 18px;
                margin: 0;
                font-family: 'JetBrains Mono', monospace;
                font-size: 12px;
              }
              .ev-row { display: contents; }
              .evidence dt { color: var(--ink-faded); }
              .evidence dd { margin: 0; color: var(--ink); }

              .related {
                margin-top: 8px;
              }
              .rel-row {
                display: grid;
                grid-template-columns: 70px 1fr auto;
                gap: 10px;
                padding: 8px 0;
                border-bottom: 1px dotted rgba(92,74,53,0.3);
                font-size: 11px;
                font-family: 'JetBrains Mono', monospace;
              }
              .rel-row:last-child { border-bottom: none; }
              .rel-id { color: var(--blood); font-weight: 700; }
              .rel-name { color: var(--ink); }
              .rel-conf {
                grid-column: 2 / 4;
                font-family: 'IM Fell English', serif;
                font-style: italic;
                font-size: 13px;
                color: var(--ink-faded);
                margin-top: 2px;
              }
              .rel-sev {
                font-family: 'Special Elite', monospace;
                font-size: 8px;
                letter-spacing: 0.16em;
                text-transform: uppercase;
                padding: 2px 6px;
                border: 1px solid currentColor;
                align-self: center;
                white-space: nowrap;
              }
              .sev-misdemeanor { color: #806735; }
              .sev-felony      { color: var(--amber-deep); }
              .sev-high-crime  { color: var(--blood); }
              .sev-simply-outrageous { color: var(--paper); background: var(--blood); border-color: var(--blood); }
              .rel-empty {
                font-family: 'IM Fell English', serif;
                font-style: italic;
                color: var(--ink-faded);
                font-size: 13px;
                padding: 6px 0;
              }

              .footer-note {
                margin-top: 32px;
                padding-top: 18px;
                border-top: 1px solid var(--ink-faded);
                font-family: 'Special Elite', monospace;
                font-size: 10px;
                color: var(--ink-faded);
                letter-spacing: 0.18em;
                display: flex;
                justify-content: space-between;
              }
              .footer-note a {
                color: var(--blood);
                text-decoration: none;
                border-bottom: 1px dotted var(--blood);
              }
              .footer-note a:hover { color: var(--ink); border-color: var(--ink); }

              .precinct-foot {
                margin-top: 30px;
                font-family: 'Special Elite', monospace;
                font-size: 9px;
                color: var(--ink-soft);
                text-align: center;
                letter-spacing: 0.3em;
              }
            </style>
            </head>
            <body>
            <div class="scanlines"></div>

            <div class="wrap">
              <div class="precinct-bar">
                DIVISION 17 // DIGITAL VICE — SHIFT REPORT
                <span class="small">FORMAL NOTICE TO REQUESTING PARTY</span>
              </div>

              <article class="case-file">
                <div class="stamp">FILED</div>

                <div class="status-line">Case No. 0x{{Esc(caseNumber)}} · {{DateTime.Now:HH:mm:ss}}</div>
                <h2 class="status-code">{{statusCode}}</h2>
                <div class="status-text">{{Esc(statusText)}}</div>

                <h1>{{Esc(title)}}</h1>

                <div class="monologue">
                  {{monologue}}
                </div>

                <div class="section-title">Evidence Catalogued</div>
                <div class="evidence"><dl>
            {{ev}}
                </dl></div>

                <div class="section-title">Related Crimes for this Request</div>
                <div class="related">
            {{rel}}
                </div>

                {{extraBodyHtml ?? ""}}

                <div class="footer-note">
                  <span>CARP // Crime Aware Reverse Proxy</span>
                  <span><a href="/crimes">view full ledger →</a></span>
                </div>
              </article>

              <div class="precinct-foot">
                this notice generated automatically · do not reply
              </div>
            </div>

            </body>
            </html>
            """;
    }

    public static string Esc(string s) => System.Net.WebUtility.HtmlEncode(s);

    public static string SevClass(Severity s) => s switch
    {
        Severity.Misdemeanor      => "misdemeanor",
        Severity.Felony           => "felony",
        Severity.HighCrime        => "high-crime",
        Severity.SimplyOutrageous => "simply-outrageous",
        _ => "felony"
    };

    public static string SevLabel(Severity s) => s switch
    {
        Severity.Misdemeanor      => "misdemeanor",
        Severity.Felony           => "felony",
        Severity.HighCrime        => "high crime",
        Severity.SimplyOutrageous => "simply outrageous",
        _ => "felony"
    };
}
