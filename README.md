# CARP — Crime Aware Reverse Proxy

> Like Google's Identity Aware Proxy, but for crime.

CARP is a deliberately misbehaving reverse proxy built on [YARP](https://github.com/dotnet/yarp), in the spirit of *you have to know all the rules to break them*. CARP sits between your clients and your backends, intercepts every request, classifies any misbehavior, and emits structured observability — metrics, logs, traces, ledger entries, response headers — so that your team always knows what crimes are being committed in their name.

The error pages monologue. The 418 returns a coffee machine. The captive portal API quietly gaslights every client behind it. Each misbehavior is recorded, classified, and confessed.

This is a comedy project that takes its scaffolding seriously. The chaos lives in YARP transforms; the awareness lives in a separate observability pipeline; the dashboard at `/crimes` is a real-time ticker of misbehavior.

## Architecture

The codebase enforces a clean separation between *committing* a crime and *narrating* it:

```
HTTP request
    ↓
YARP transform pipeline      ← perpetrators (Transforms/*.cs)
    ↓
ICrimeReporter.Report(crime, ctx, http)
    ↓
┌────────────────┬──────────────────┬────────────────┬─────────────────┐
↓                ↓                  ↓                ↓                 ↓
metric counter   structured log    CrimeLedger     OTel span       response headers
(Prometheus)     (ILogger)         (ring buffer +   (ActivitySource)  (X-Crime-Id,
                                    SSE fan-out)                       X-Confession,
                                                                       X-Severity)
```

Five observability channels for one call site. Add a crime once; see it everywhere.

When the proxy returns an error response (502 from an unreachable backend, 504 from a slow one, etc.), a separate middleware layer intercepts the empty body and renders a **case file** error page in the project's aesthetic, with the recent crimes for the request listed alongside.

### Types

- **`ICrime`** — declarative record of misbehavior. Has a `Name`, `Severity`, and `ConfessAs(CrimeContext)` method.
- **`CrimeContext`** — per-incident context with a stringly-typed `Evidence` dictionary. Heterogeneous by design.
- **`ICrimeReporter`** — fans out a crime to all observability channels.
- **`CrimeLedger`** — in-memory ring buffer + aggregate counters, backs `/ledger`, `/honesty`, `/crimes/stream`.
- **`ICrimeErrorPage`** — renders an HTML case file for a specific status code.
- **`CrimeErrorPageMiddleware`** — intercepts non-2xx empty-body responses and substitutes a rendered error page.
- **YARP transforms** — the actual perpetrators.

### Severity ladder

`Misdemeanor` → `Felony` → `HighCrime` → `SimplyOutrageous`

The first three tiers are graded technical assessments. The top tier is the clerk reaching the limit of their professional vocabulary and finally just *muttering* at the page. `SimplyOutrageous` is reserved for crimes that violate causality, recurse on themselves, or otherwise misbehave in ways the rubric was not designed to accommodate.

## Endpoints

| Endpoint                       | Purpose                                                                       |
|--------------------------------|-------------------------------------------------------------------------------|
| `/crimes`                      | The dashboard (HTML)                                                          |
| `/crimes/stream`               | Server-Sent Events stream of new crimes                                       |
| `/ledger?n=N`                  | Recent N ledger entries as JSON (max 500)                                     |
| `/honesty`                     | Self-reported uptime, totals, mood, top crimes                                |
| `/418`, `/502`, `/503`, `/504`, `/413`, `/421` | Direct access to the error pages                              |
| `/captive/api/session`         | RFC 8908 captive portal API — the lying one                                   |
| `/captive/api/end`             | RFC 8908 session-end                                                          |
| `/captive/portal`              | Captive portal landing page — the "Acceptable Use" agreement                  |
| `/probe/{**path}`              | Connectivity probe interceptor — returns 302 to `/captive/portal`             |
| `/proxy/{**path}`              | The actual reverse proxy (configurable in `appsettings.json`)                 |

## The 418

The centerpiece of the error page collection. RFC 2324 says a teapot cannot brew coffee — and so when asked to brew tea, this proxy returns the wrong appliance with confidence.

The 418 endpoint honors `Accept` headers:
- `text/html` (default) — full case-file HTML with an embedded SVG of the chosen appliance
- `application/json` — JSON spec sheet
- `image/svg+xml` — just the SVG line-art
- `text/plain` — ASCII art with details

The appliance is randomized with a weighted roll — espresso machines dominate, the Keurig is rare. **One in 500 requests returns an actual teapot**, with no confession and no special headers. The proxy is briefly, quietly, accidentally honest.

`Retry-After` is set to the brew time of whichever appliance was returned. RFC 7231 compliant. Useless in spirit. Funny in deed.

## Running

```bash
dotnet run --project Carp.csproj
# Open http://localhost:5050/crimes
# Try http://localhost:5050/418         — the centerpiece
# Try http://localhost:5050/418         — again. Different machine.
# Try http://localhost:5050/captive/api/session   — the lying API
# Send proxied requests via             http://localhost:5050/proxy/...
```

The default config points the proxy at `https://httpbin.org`.

## Crime configuration

Each crime is independently togglable in `appsettings.json`:

```json
"Crimes": {
  "JwtAlgNone":        false,   // off by default; backend exploitation hazard
  "SignNull":          false,   // off by default; constant-signature hazard
  "LieAboutStatus":    true,
  "LatencyHomeopathy": true,
  "WeakETag":          true,
  "ECBMarker":         true
}
```

### Crimes that are off by default

Two crimes are dangerous in real deployments and require explicit opt-in:

- **`crypto.jwt_alg_none`** strips real JWTs and replaces them with `alg=none` tokens. Any backend with the alg=none bug becomes exploitable by traffic this proxy generates.
- **`crypto.sign_the_void`** computes a real HMAC-SHA256 with a per-process key and adds it as `X-Signature` to write requests. A backend that *trusts* `X-Signature` would now accept the same constant signature on any payload.

Other crimes are harmless to backends — they only affect what the client sees.

## Captive portal demo (the full DHCP setup)

The proxy implements the RFC 8908 captive portal API at `/captive/api/session`. By itself, this means nothing — clients only consult that endpoint when DHCP option 114 (RFC 8910) tells them to.

To weaponize the proxy as a network-wide captive portal, you need a cooperative DHCP server. A sample `dnsmasq.conf` is included.

```bash
# 1. Run the proxy on a host reachable from the LAN (say, 10.0.0.10)
dotnet run --project Carp.csproj

# 2. On the gateway, run dnsmasq with the sample config
dnsmasq --conf-file=dnsmasq.conf.sample

# 3. Connect a phone or laptop. Watch it pop "Sign in to network."
# 4. Tap it. The OS opens /captive/portal in a sandboxed browser.
# 5. The user came to read their email. They get the precinct.
```

The proxy also implements **probe URL interception** at `/probe/*`. If you DNS-redirect probe URLs (`captive.apple.com`, `connectivitycheck.gstatic.com`, `msftconnecttest.com`) to the proxy, even Windows clients — which don't honor option 114 — will be marked captive. The `dnsmasq.conf.sample` includes the relevant `address=` lines.

The captive portal API itself commits crimes:
- **`captive.venue_gaslighting`** — every few requests, the `venue-info-url` changes without justification
- **`captive.yo_yo`** — the `captive` boolean flips back and forth, generating notification spam on the client

## Adding a new crime

1. Add an `ICrime` to `Crimes/Catalog.cs`.
2. Add a `RequestTransform` or `ResponseTransform` to `Transforms/Transforms.cs`.
3. Add an opt-in flag to `CrimeOptions` if it can affect upstreams unsafely.
4. Register in `Program.cs` and add to the appropriate transform list.

## Adding a new error page

1. Implement `ICrimeErrorPage` in `ErrorPages/Pages.cs`.
2. Register in `Program.cs`: `services.AddSingleton<ICrimeErrorPage, MyNewPage>();`.
3. The middleware will pick it up automatically based on its `StatusCode` property.
4. Add the code to the direct-access list in `ErrorPageEndpoints.MapErrorPageEndpoints` if you want `/<code>` to work.

## What this project is not

- **Not a security tool.** The dangerous crimes are gated behind config.
- **Not for production.** It's a demo of an architectural pattern (separating misbehavior from narration via a reporter) applied to a deliberately silly purpose, and it's an aesthetic exercise in giving a piece of infrastructure a *voice*.

## Voice

Confessions follow one voice: an unrepentant clerk filling out paperwork. Matter-of-fact about the crime, occasionally proud, never sincerely sorry. The proxy explains what it did, offers a one-line justification, and moves on. Confessions never apologize.

The error page monologues extend the voice — same clerk, longer-form, with permission to elaborate. Each page has a title in the form *"On the [thing]"* or *"A Note on [thing]"*, mimicking the style of an internal-affairs filing.
