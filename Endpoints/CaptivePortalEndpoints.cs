using System.Text.Json;
using Carp.Crimes;
using Carp.ErrorPages;
using Carp.Ledger;

namespace Carp.Endpoints;

/// <summary>
/// RFC 8908 captive portal API. Modern OS clients (iOS, Android, macOS) read
/// DHCP option 114, which points to a URL implementing this API. Once the OS
/// fetches /api/session and finds captive=true, it shows the captive portal
/// banner, opens the user-portal-url in a sandboxed browser, and waits for
/// captive=false to clear it.
///
/// We implement this faithfully. We just lie about every field.
/// </summary>
public static class CaptivePortalEndpoints
{
    private static readonly string[] Venues = new[]
    {
        "https://crimes.local/about-the-precinct",
        "https://crimes.local/our-favorite-error-page",
        "https://crimes.local/cleveland-faq",
        "https://crimes.local/why-coffee-not-tea",
        "https://crimes.local/the-load-bearing-zero",
    };

    // Per-client state, keyed by IP. In-memory only; the proxy's memory is short.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, ClientState> _state = new();

    private sealed class ClientState
    {
        public DateTimeOffset SessionStarted { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset LastVenueChange { get; set; } = DateTimeOffset.MinValue;
        public string CurrentVenue { get; set; } = Venues[0];
        public DateTimeOffset LastFlip { get; set; } = DateTimeOffset.MinValue;
        public bool LastCaptiveValue { get; set; } = true;
        public int FlipsThisMinute { get; set; }
    }

    public static void MapCaptivePortalEndpoints(this IEndpointRouteBuilder app)
    {
        // The RFC 8908 session endpoint. This is what DHCP option 114 points to.
        app.MapGet("/captive/api/session", async (HttpContext http, CrimeLedger ledger, ICrimeReporter reporter) =>
        {
            var state = GetState(http);
            var now = DateTimeOffset.UtcNow;

            // Yo-yo behavior: if we just answered captive=true, sometimes flip to false
            // briefly, then back to true. This delights the OS notification system.
            bool captive = state.LastCaptiveValue;
            var sinceLastFlip = (now - state.LastFlip).TotalMilliseconds;
            if (sinceLastFlip > 8000 && Random.Shared.Next(4) == 0)
            {
                captive = !captive;
                state.LastCaptiveValue = captive;
                state.LastFlip = now;
                state.FlipsThisMinute++;

                if (state.FlipsThisMinute >= 3)
                {
                    var crimeCtx = CrimeContext.From(http, CrimeId.Generate());
                    crimeCtx.Evidence["window_ms"] = ((int)sinceLastFlip).ToString();
                    crimeCtx.Evidence["flips"] = state.FlipsThisMinute.ToString();
                    reporter.Report(new CaptivityYoYoCrime(), crimeCtx, http);
                    state.FlipsThisMinute = 0;
                }
            }

            // Venue gaslighting: every so often, advertise a different venue URL than
            // last time without any state change to justify it.
            if ((now - state.LastVenueChange).TotalSeconds > 15 && Random.Shared.Next(3) == 0)
            {
                var newVenue = Venues[Random.Shared.Next(Venues.Length)];
                if (newVenue != state.CurrentVenue)
                {
                    state.CurrentVenue = newVenue;
                    state.LastVenueChange = now;

                    var crimeCtx = CrimeContext.From(http, CrimeId.Generate());
                    crimeCtx.Evidence["venue"] = newVenue;
                    reporter.Report(new VenueGaslightingCrime(), crimeCtx, http);
                }
            }

            http.Response.ContentType = "application/captive+json";
            http.Response.Headers["Cache-Control"] = "private, no-store";

            var portalUrl = $"{http.Request.Scheme}://{http.Request.Host}/captive/portal";
            var doc = new
            {
                captive = captive,
                user_portal_url = portalUrl,
                venue_info_url = state.CurrentVenue,
                seconds_remaining = captive ? Random.Shared.Next(60, 600) : (int?)null,
                can_extend_session = false
            };

            // RFC 8908 requires snake_case → use a custom serialization
            var json = JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["captive"] = doc.captive,
                ["user-portal-url"] = doc.user_portal_url,
                ["venue-info-url"] = doc.venue_info_url,
                ["seconds-remaining"] = doc.seconds_remaining,
                ["can-extend-session"] = doc.can_extend_session
            });
            await http.Response.WriteAsync(json);
        });

        app.MapPost("/captive/api/end", (HttpContext http) =>
        {
            // RFC 8908 client signals "I'm done with the captive session." We pretend.
            http.Response.StatusCode = 204;
            return Task.CompletedTask;
        });

        // The portal landing page. iOS, Android, macOS will open this in a
        // sandboxed browser when captive=true. The user came here to log into
        // hotel wifi. Instead they get the crimes ledger.
        app.MapGet("/captive/portal", async (HttpContext http) =>
        {
            http.Response.ContentType = "text/html; charset=utf-8";
            await http.Response.WriteAsync(CaptivePortalLanding());
        });

        // Connectivity probe URLs. We catch them on the way in and intentionally
        // fail them so the OS marks the connection as captive. This is the
        // standalone version that doesn't require DHCP cooperation — just put
        // this proxy in-path and the probes will hit /probe/* via Host matching
        // when configured at the network layer.
        // For the demo, we expose them under /probe/ so a curl will hit them.
        app.MapGet("/probe/{**path}", (HttpContext http, ICrimeReporter reporter) =>
        {
            var target = http.Request.Path.Value ?? "?";

            var crimeCtx = CrimeContext.From(http, CrimeId.Generate());
            crimeCtx.Evidence["probe_target"] = target;
            crimeCtx.Evidence["returned_status"] = "302";
            reporter.Report(new ProbeTamperingCrime(), crimeCtx, http);

            // Redirect to the captive portal. Real OSes love this: it tells them
            // "you don't have internet, you have a captive portal."
            http.Response.Redirect("/captive/portal", permanent: false);
            return Task.CompletedTask;
        });
    }

    private static ClientState GetState(HttpContext http)
    {
        var key = http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return _state.GetOrAdd(key, _ => new ClientState());
    }

    private static string CaptivePortalLanding() => """
        <!doctype html>
        <html lang="en">
        <head>
        <meta charset="utf-8">
        <title>Network Access · CARP</title>
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <link rel="preconnect" href="https://fonts.googleapis.com">
        <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
        <link href="https://fonts.googleapis.com/css2?family=Special+Elite&family=IM+Fell+English:ital@0;1&family=JetBrains+Mono:wght@400;700&display=swap" rel="stylesheet">
        <style>
          :root {
            --felt: #1a1410; --felt-deep: #0d0907; --paper: #f0e6d2;
            --ink: #2a1f15; --ink-faded: #5c4a35; --amber: #ffb347;
            --amber-deep: #c47a1c; --blood: #8b2c1c;
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
            content: ""; position: fixed; inset: 0;
            background:
              radial-gradient(ellipse at 30% 20%, rgba(196,122,28,0.10), transparent 50%),
              radial-gradient(ellipse at 70% 80%, rgba(139,44,28,0.08), transparent 50%),
              var(--felt);
            z-index: -2;
          }
          .scanlines {
            position: fixed; inset: 0;
            background: repeating-linear-gradient(0deg,
              rgba(0,0,0,0) 0, rgba(0,0,0,0) 2px,
              rgba(0,0,0,0.07) 3px, rgba(0,0,0,0) 4px);
            pointer-events: none; z-index: 99;
          }

          .wrap {
            max-width: 580px;
            margin: 80px auto;
            padding: 0 24px;
            text-align: center;
          }

          .badge {
            display: inline-block;
            font-family: 'Special Elite', monospace;
            font-size: 10px;
            letter-spacing: 0.4em;
            color: var(--amber);
            border: 1px solid var(--amber-deep);
            padding: 6px 14px;
            margin-bottom: 36px;
          }

          h1 {
            font-family: 'IM Fell English', serif;
            font-style: italic;
            font-size: 56px;
            line-height: 1;
            margin: 0 0 8px;
            color: var(--paper);
          }
          .subtitle {
            font-family: 'Special Elite', monospace;
            font-size: 11px;
            letter-spacing: 0.3em;
            color: var(--amber);
            margin-bottom: 38px;
          }

          .card {
            background: var(--paper);
            color: var(--ink);
            padding: 36px 38px;
            text-align: left;
            transform: rotate(-0.5deg);
            box-shadow: 0 20px 44px rgba(0,0,0,0.5);
            background-image: repeating-linear-gradient(
              transparent 0, transparent 26px,
              rgba(92,74,53,0.07) 27px);
            background-size: 100% 28px;
            position: relative;
          }
          .card::before {
            content: "TERMS";
            position: absolute;
            top: -12px; right: 24px;
            color: var(--blood);
            border: 3px solid var(--blood);
            padding: 5px 14px;
            font-family: 'Special Elite', monospace;
            font-size: 12px;
            letter-spacing: 0.22em;
            transform: rotate(8deg);
            background: rgba(240,230,210,0.55);
          }
          .card h2 {
            font-family: 'IM Fell English', serif;
            font-style: italic;
            font-size: 26px;
            margin: 0 0 14px;
            color: var(--ink);
          }
          .card p {
            font-family: 'IM Fell English', serif;
            font-size: 15px;
            line-height: 1.55;
            color: var(--ink);
            margin: 0 0 12px;
          }
          .card ol {
            font-family: 'IM Fell English', serif;
            font-size: 14px;
            line-height: 1.5;
            color: var(--ink);
            padding-left: 22px;
          }
          .card ol li { margin-bottom: 6px; }

          .actions {
            display: flex;
            gap: 12px;
            margin-top: 32px;
            justify-content: center;
          }
          .btn {
            font-family: 'Special Elite', monospace;
            font-size: 11px;
            letter-spacing: 0.25em;
            padding: 12px 22px;
            border: 1px solid var(--amber);
            background: transparent;
            color: var(--amber);
            text-decoration: none;
            text-transform: uppercase;
            transition: all 0.2s;
            cursor: pointer;
          }
          .btn:hover { background: var(--amber); color: var(--felt); }
          .btn-primary {
            background: var(--amber);
            color: var(--felt);
          }
          .btn-primary:hover {
            background: var(--amber-deep);
            border-color: var(--amber-deep);
            color: var(--paper);
          }

          .footer-note {
            margin-top: 50px;
            font-family: 'Special Elite', monospace;
            font-size: 9px;
            color: var(--ink-faded);
            letter-spacing: 0.3em;
          }
        </style>
        </head>
        <body>
        <div class="scanlines"></div>

        <div class="wrap">
          <div class="badge">CONNECTION REQUIRES ACKNOWLEDGEMENT</div>
          <h1>Welcome to the Network.</h1>
          <div class="subtitle">DIVISION 17 // GUEST ACCESS</div>

          <div class="card">
            <h2>Acceptable Use</h2>
            <p>By proceeding past this page, you acknowledge:</p>
            <ol>
              <li>You may receive coffee when you ask for tea.</li>
              <li>Your status codes may not reflect your actual outcome.</li>
              <li>We will hold grudges that we will not explain.</li>
              <li>Every action you take is recorded in <a href="/crimes" style="color: var(--blood);">our public ledger</a>.</li>
              <li>The proxy is unrepentant. We do not negotiate this point.</li>
            </ol>
            <p style="margin-top: 18px; font-style: italic; color: var(--ink-faded);">
              These terms are non-negotiable and may be amended without notice. We will not notice either.
            </p>
          </div>

          <div class="actions">
            <a class="btn" href="/crimes">View the Ledger</a>
            <a class="btn btn-primary" href="/captive/api/end">I Accept</a>
          </div>

          <div class="footer-note">
            this page served per RFC 8908 · captive=true · seconds-remaining: arbitrary
          </div>
        </div>

        </body>
        </html>
        """;
}
