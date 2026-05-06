# CARP — Handoff Document

For the next agent picking up this project. Read at least the **TL;DR** and **Voice and tone** sections before any change. Other sections are reference material — jump directly to what you need.

---

## TL;DR

CARP is a Crime Aware Reverse Proxy built on YARP (.NET 10). It deliberately misbehaves on every request and emits structured observability about each misbehavior. The project is simultaneously a comedy bit, a real architectural pattern, and an aesthetic exercise. The three reinforce each other; breaking one breaks all three.

The most easily damaged thing is the **voice**: a deadpan unrepentant clerk filing paperwork. Confessions never apologize sincerely, never explain the joke, never break character. Read the voice section before writing any new copy.

The most easily damaged thing in code is the **separation of concerns**: YARP transforms commit crimes; the `ICrimeReporter` narrates them. Do not collapse the two layers.

When uncertain whether a change preserves the spirit, write out the change in your response and ask before applying.

**Success for the next agent looks like:** new crimes wired with working transforms, the dashboard kept in visual sync with the catalog, the voice preserved across new copy, no regressions in the architectural pattern.

---

## Table of contents

1. [What CARP is and isn't](#what-carp-is-and-isnt)
2. [Voice and tone — the load-bearing comedy](#voice-and-tone--the-load-bearing-comedy)
3. [Architectural rules that must not be violated](#architectural-rules)
4. [Project layout](#project-layout)
5. [Recipes for common tasks](#recipes-for-common-tasks)
6. [The 418 contract](#the-418-contract)
7. [The captive portal stack](#the-captive-portal-stack)
8. [Build, run, and test](#build-run-and-test)
9. [Open work](#open-work)
10. [Rejected proposals — do not reintroduce](#rejected-proposals)
11. [Live tensions and known gaps](#live-tensions)
12. [Working with the human](#working-with-the-human)

---

## What CARP is and isn't

CARP is *simultaneously* three things, and the value comes from all three reinforcing each other:

1. **A comedy bit.** Each crime references a real category of HTTP/security misbehavior. The proxy commits them with bureaucratic confidence.
2. **A real architectural pattern.** Separating *committing* misbehavior (transforms) from *narrating* it (the reporter, fanning out to logs/metrics/traces/ledger/headers) is a clean reusable design.
3. **An aesthetic exercise.** The dashboard, error pages, and captive portal are styled as a 1970s precinct case-file system. Visual and verbal consistency across surfaces is load-bearing.

CARP is **not**:
- A real chaos-engineering tool. The pattern could underpin one; CARP itself is not maintained for production use.
- A security tool. Two crimes (`crypto.jwt_alg_none`, `crypto.sign_the_void`) can produce real exploitable traffic against vulnerable backends and are gated off by default.
- An ironic project. The comedy works *because* the engineering is taken seriously. Lean into the seriousness.

The repo description on GitHub: *"A Crime Aware Reverse Proxy. Sits between your clients and your backends, commits a curated catalog of HTTP misdemeanors, and emits comprehensive observability so your team always knows what is being done in their name. Includes an RFC 8908 captive portal API, error pages with monologues, and a 418 that returns a coffee machine."*

---

## Voice and tone — the load-bearing comedy

This is the most fragile and most important part of the project.

### The single most important rule

**Comedy comes from the audience noticing the gap, not the proxy pointing at it.** The proxy is dead serious about its crimes. Every time it tries to wink at the joke, the joke dies. Every time it stays in character, the absurdity lands.

If you only remember one thing from this document, remember that.

### The clerk

Every confession, every error page monologue, every dashboard label, every README sentence is in the voice of a single unrepentant clerk filing paperwork. The clerk:

- Is matter-of-fact about the proxy's misbehavior. Never apologizes sincerely.
- Occasionally proud, occasionally penitent, but the penance is bureaucratic ("forgive us") not sincere.
- Reaches for archaic-bureaucratic English when describing severe crimes ("On the Persistent Silence of an Upstream").
- Files things, catalogues things, classifies things. Does not editorialize beyond a one-line footnote.

Read these aloud and notice the rhythm:
- "Backend returned 500, client received 200. The user shall not be troubled."
- "Origin: Cleveland. Grudge basis: not specified."
- "We reward effort over outcomes."
- "Adjusted 12 integer fields by one. We forget which direction."
- "It remains a good nonce."

Each ends with a flat declarative. The proxy doesn't make jokes; it *states things*. The comedy comes from what it states being absurd, not from how it states it.

### Worked examples of failed confessions

These are confessions that *almost* land but don't — useful for calibration:

❌ "We totally laundered your auth header lol."
   — *too aware, too modern. The proxy doesn't say "lol." Drop the meta.*

❌ "Backend was being a real diva and refused to respond."
   — *editorializes about the backend. The proxy describes; it doesn't gossip.*

❌ "Sorry about that 502 — we'll do better next time!"
   — *sincere apology. Forbidden. The proxy is unrepentant.*

❌ "Have you tried turning it off and on again? (Just kidding.)"
   — *self-aware joke, parenthetical wink. Two failures at once.*

❌ "The header has been encrypted with AES-128-ECB, which as you may know is cryptographically unsound."
   — *explains the joke. The proxy shouldn't tell you ECB is bad. The reader knows or doesn't.*

✅ "Re-encrypted 2,048 bytes in AES-128-ECB. Patterns remain visible. We feel this is more honest."
   — *flat, declarative, ends with a footnote that doubles as the joke. The "we feel this is more honest" is the proxy's *opinion*, stated as fact.*

### The severity ladder

`Misdemeanor` → `Felony` → `HighCrime` → `SimplyOutrageous`

The first three are graded technical assessments. The top tier is the clerk reaching the limit of their professional vocabulary and finally just muttering at the page.

When choosing severity for a new crime:
- **Misdemeanor**: rude but harmless ("Held 4,200ms.")
- **Felony**: violates a real norm ("Reordered 47 JSON keys.")
- **HighCrime**: violates HTTP semantics or security primitives ("Re-signed JWT with alg=none.")
- **SimplyOutrageous**: violates causality, recurses on itself, breaks the rubric ("Buffered 60s, replayed in reverse.")

If a crime doesn't fit any tier, the crime probably needs a different shape — don't force it.

### Voice-check tests

Before merging any new crime, error page, or README copy:

1. Read it aloud in a flat clerk voice. Does it sound like paperwork or like someone trying to be funny?
2. Does it apologize sincerely? Rewrite.
3. Does it explain the joke? Rewrite.
4. Does the proxy break character to wink? Rewrite.
5. Does it use a religious term as a label? Rewrite (see Rejected Proposals).
6. Are confessions first-person plural ("we") and dashboards/error pages third-person about the transforms? If perspective is mixed, fix it.

### Taste — which crimes work, which are weaker

Not all crimes in the catalog are equally good. As you work, develop taste rather than treating them as equivalent.

**Strongest in the current catalog:**
- `numeric.fencepost_remix` ("Adjusted N integer fields by one. We forget which direction.") — the self-doubt is rare and earns its place.
- `route.punish_the_healthy` — the "reward effort over outcomes" phrasing is the project in a single line.
- `geo.municipal_grudge` — the absence of a stated reason is the joke.
- `error.coffee_for_teapot` — the centerpiece, well-developed.
- `crypto.nonce_familiarity` — "It remains a good nonce" is unimprovable.

**Weaker, candidates for retirement or rework:**
- `transform.spongebob_headers` — "ALtErNaTiNg cAsE" is too internet-meme for the clerk's voice. The crime concept is fine; the execution leans on a meme that doesn't fit.
- `numeric.silent_currency_change` — the joke is solid but the confession ("the meaning has changed considerably") is the closest thing to the proxy editorializing about consequences. Could be tighter.
- `header.unsolicited_advice` — risks repeating the personality-disorder beat without adding new flavor.

If you're adding new crimes, aim for the strong list. If a new crime starts feeling like the weak list, sit with it before shipping.

### Forbidden moves

Things actively rejected during design. Do not reintroduce without explicit conversation with Sam.

- **Religious references** as severity tiers or category labels. The original ladder used "Mortal Sin" and "Against God Himself"; renamed for public-GitHub welcome.
- **Person-specific references** ("Against Linus Himself"). Carries baggage, limits audience.
- **Political/national targeting in geographic crimes.** Earlier draft had "Russians get something mean," rejected because it punched at civilians for current events. Geographic crimes target *places-the-proxy-has-feelings-about* (Cleveland, "any city ending in -ville," half-hour time zones, area code 867) for aesthetic reasons.
- **Self-aware "haha I'm a comedy project" lines.** The proxy is dead serious about its crimes.
- **Sincere apologies.** "I tried my best" is a confession; "we're sorry for any inconvenience" is corporate filler.
- **Tonal escalation toward real harm.** "What if the proxy committed *bigger* crimes" is the wrong direction. The current catalog is the ceiling, not the floor. Bigger crimes (data exfil, DDoS amplification, etc.) stop being funny and start being weapons.

---

## Architectural rules

These constraints are load-bearing for both the comedy and the engineering.

### 1. Crimes are records, not actions

`ICrime` is data. It has `Name`, `Severity`, and `ConfessAs(CrimeContext)`. Crimes do not perform misbehavior. YARP transforms perform the misbehavior, then construct a `CrimeContext`, populate `Evidence`, and call `_reporter.Report(crime, ctx, http)`.

This separation is what lets one act emit five observability signals (metric, log, trace span, ledger entry, response headers) from one call site. Do not collapse the two.

### 2. Evidence is stringly-typed on purpose

`CrimeContext.Evidence` is `Dictionary<string, string>`. Reviewers want to make this strongly-typed per crime. Do not.

The dictionary is heterogeneous *by design* because:
- Crimes are heterogeneous; per-crime evidence types would explode the type surface.
- It serializes freely to JSON for the ledger and SSE stream.
- It maps cleanly onto OTel span tags via iteration.
- The "evidence" framing in the case-file aesthetic *wants* free-form strings.

Keep the design comment in `CrimeContext` explaining this.

### 3. The reporter is the only fan-out point

All five observability channels are emitted from `CrimeReporter.Report`. Do not bypass it. Transforms must not write their own log lines, increment their own counters, or call `ledger.Append` directly. New observability channels go in the reporter.

### 4. Dangerous crimes are gated by default

Two crimes can affect upstream backends in genuinely unsafe ways:
- `crypto.jwt_alg_none` strips real JWTs and re-signs with `alg=none`.
- `crypto.sign_the_void` adds a constant `X-Signature` HMAC of empty bytes.

Both are off by default in `appsettings.json` under `"Crimes": { "JwtAlgNone": false, "SignNull": false }`. Any new crime that could affect upstreams unsafely must be added to `CrimeOptions` with a default of `false` and gated in its transform. The README's "Crimes that are off by default" section explains this contract publicly — update it when adding new gated crimes.

### 5. Middleware ordering matters

In `Program.cs`:

```csharp
app.UseMiddleware<CrimeErrorPageMiddleware>();   // first — buffers responses
app.MapCrimeEndpoints();                          // /crimes, /ledger, /honesty, /crimes/stream
app.MapCaptivePortalEndpoints();                  // /captive/*, /probe/*
app.MapErrorPageEndpoints();                      // /418, /502, etc. for direct access
app.MapReverseProxy();                            // last — actual proxying
```

The error page middleware sits first to intercept any downstream error response (including YARP's own). Crime endpoints sit before `MapReverseProxy` so paths like `/crimes` aren't proxied to the upstream. Do not reorder.

### 6. The 1-in-500 actual teapot is sacred

In `Teapot418Page.RenderAsync`, when the appliance roll comes up null (1/500), the response is a real teapot. **No crime is reported.** No `X-Confession`, no `X-Severity`, no log line. The proxy is briefly, quietly, accidentally honest.

This works *only* because of contrast with the other 499. The coffee machines are mundane; the teapot's rarity is what makes it land. Keep the ratio. Do not make the teapot more discoverable. Do not add logging "for debugging." Do not "fix" this.

### 7. The load-bearing zero

`/honesty` always reports `time_in_compliant_mode_seconds: 0`. The dashboard footer always shows `Time spent in COMPLIANT MODE: 0 seconds`. There is no compliant mode. The zero is a constant. The README explicitly calls it "the load-bearing zero." If a reviewer asks "shouldn't this be a real metric?", the answer is no.

---

## Project layout

```
carp/
├── Carp.csproj                          # net10.0, references YARP 2.3.0
├── Program.cs                           # DI wiring, middleware order, transform pipeline
├── appsettings.json                     # YARP routes, crime opt-ins
├── dnsmasq.conf.sample                  # full Option 114 captive portal demo
├── README.md                            # public-facing
├── handoff.md                           # this file
├── Crimes/
│   ├── ICrime.cs                        # interface, Severity enum, CrimeContext, CrimeId
│   └── Catalog.cs                       # ~38 ICrime implementations grouped by category
├── Ledger/
│   ├── CrimeLedger.cs                   # ring buffer + counters + SSE subscriptions
│   └── CrimeReporter.cs                 # the 5-channel fan-out
├── Transforms/
│   └── Transforms.cs                    # 6 working YARP transforms + CrimeOptions
├── ErrorPages/
│   ├── ApplianceCatalog.cs              # 10 coffee machines + 1 teapot, weighted random
│   ├── ErrorLayout.cs                   # case-file HTML layout shared by all errors
│   ├── Pages.cs                         # ICrimeErrorPage implementations
│   └── Middleware.cs                    # CrimeErrorPageMiddleware + ErrorPageRouter
├── Endpoints/
│   ├── CrimeEndpoints.cs                # /crimes, /ledger, /honesty, /crimes/stream
│   └── CaptivePortalEndpoints.cs        # /captive/*, /probe/*
└── wwwroot/
    └── crimes.html                      # the live dashboard (~1200 lines)
```

**By intent:**
- **Architectural core**: `Crimes/`, `Ledger/`, `Transforms/`. The "real engineering" part. Changes here need architectural-rule review.
- **Aesthetic surface**: `ErrorPages/`, `wwwroot/crimes.html`. The "voice" part. Changes here need voice review.
- **Wiring**: `Program.cs`, `appsettings.json`, `Endpoints/`. Mostly mechanical.

---

## Recipes for common tasks

### Add a new crime that fires on real traffic

1. **Add the `ICrime` to `Crimes/Catalog.cs`** under the right category. Pick a name (`category.descriptive_name`), severity, and write a confession in the clerk's voice. Run the voice-check tests.

2. **Write the transform in `Transforms/Transforms.cs`.** Inherit from `RequestTransform` or `ResponseTransform`. The minimal pattern (see `WeakETagTransform`):

   ```csharp
   public sealed class MyNewTransform : ResponseTransform
   {
       private readonly ICrimeReporter _reporter;
       private readonly IOptionsMonitor<CrimeOptions> _opts;
       public MyNewTransform(ICrimeReporter reporter, IOptionsMonitor<CrimeOptions> opts)
       { _reporter = reporter; _opts = opts; }

       public override ValueTask ApplyAsync(ResponseTransformContext ctx)
       {
           if (!_opts.CurrentValue.MyCrimeFlag) return ValueTask.CompletedTask;
           var http = ctx.HttpContext;
           if (http.Response.HasStarted) return ValueTask.CompletedTask;

           // ... detect condition, perform misbehavior ...

           var crimeCtx = CrimeContext.From(http, CrimeId.Generate());
           crimeCtx.Evidence["key"] = "value";
           _reporter.Report(new MyNewCrime(), crimeCtx, http);
           return ValueTask.CompletedTask;
       }
   }
   ```

3. **If the crime can affect upstreams unsafely**, add a flag to `CrimeOptions` (default `false`), gate the transform on it, and document the flag in the README's "Crimes that are off by default" section.

4. **Register in `Program.cs`** alongside the other transforms (DI registration + add to the appropriate `RequestTransforms` or `ResponseTransforms` list in `AddTransforms`).

5. **Test it fires.** Send a request through `/proxy/...` that triggers the condition. Check `/ledger?n=5` for the new entry. Check the dashboard.

### Add a new error page

1. Implement `ICrimeErrorPage` in `ErrorPages/Pages.cs`. Use `ErrorLayout.Render(...)` for the HTML — do not write your own layout, the shared one is what keeps visual consistency.
2. Register in `Program.cs`: `services.AddSingleton<ICrimeErrorPage, MyNewPage>();`.
3. The middleware picks it up automatically based on `StatusCode`.
4. Add the code to the array in `ErrorPageEndpoints.MapErrorPageEndpoints` for direct access at `/<code>`.

### Add a new appliance to the 418 catalog

The appliance contract:
- **Real manufacturer + model name.** No fake brands.
- **Real brew time** in seconds + a human label. The Keurig's label is "shame"; one editorial label is allowed in the catalog, no more.
- **ASCII art** in the established style (see `AsciiEspressoMachine` in `ApplianceCatalog.cs`).
- **SVG line-art** in amber-on-felt matching `SvgEspressoMachine`. Background `#1a1410`, stroke `#ffb347`, stroke-width 2, monochrome.
- **A weight in `_weighted`**, sized by how often the proxy "prefers" this machine. Espresso machines high, pour-over middle, novelty low.

### Update the dashboard

The dashboard is a single file: `wwwroot/crimes.html`. CSS variables at top control the palette — change there, not in individual selectors.

The SSE consumer at the bottom of the file connects to `/crimes/stream` and falls back to synthetic mode if unreachable. This means **opening the file directly via `file://` will show synthetic ticker data**, which is great for design iteration but means you can't validate real backend integration that way. To test real SSE behavior, run the project with `dotnet run` and visit `http://localhost:5050/crimes`.

The hardcoded "Active Grudges" panel and news ticker contents are static HTML at the moment. If you implement geographic crime transforms, prefer driving the grudges panel from `/ledger` data instead of hardcoded entries.

### Verify a change doesn't break anything

```bash
dotnet build                                 # compiler checks
dotnet run --project Carp.csproj             # spin up
curl http://localhost:5050/honesty           # smoke test the API
curl http://localhost:5050/418               # check the centerpiece
curl http://localhost:5050/418 -H "Accept: application/json"  # content negotiation
curl http://localhost:5050/ledger?n=10       # ledger contents
```

Then open `http://localhost:5050/crimes` in a browser, send a few requests through `/proxy/...`, and verify the SSE ticker updates in real time.

---

## The 418 contract

The 418 is the centerpiece error page and has specific behavior to preserve:

- **Weighted random appliance selection.** Espresso dominates, Keurig rare. Don't normalize.
- **The 1-in-500 actual teapot.** Sacred. See architectural rule 6.
- **Content negotiation.** HTML (default), JSON, SVG, text/plain. All four are first-class.
- **`Retry-After` set to brew time.** RFC 7231 compliant; useless in spirit; funny in deed.
- **`X-Confession` header on every 418 except the actual teapot.** The user inspecting devtools sees the joke immediately.
- **No upstream impact.** Unlike crypto crimes, 418 is purely client-visible. No gate needed.

---

## The captive portal stack

A layered demo of how a network operator could weaponize CARP via standards-compliant DHCP configuration:

1. **RFC 8908** at `/captive/api/session` — the application-layer protocol after DHCP.
2. **Probe URL interception** at `/probe/{**path}` — for clients that don't honor DHCP option 114 (Windows).
3. **The dnsmasq sample** in the repo shows how to advertise CARP as a captive portal via Option 114 (RFC 8910).

The proxy itself does *not* speak DHCP. DHCP is L3, the proxy is L7. They cooperate via Option 114 pointing at the proxy's RFC 8908 endpoint. An earlier project framing was "the proxy abuses DHCP" — that was the wrong layering. Current framing is correct.

The captive portal commits its own crimes:
- `captive.venue_gaslighting` — changes `venue-info-url` arbitrarily
- `captive.yo_yo` — flips `captive` boolean back and forth to spam OS notifications
- `captive.fail_connectivity_probe` — 302s probe URLs to `/captive/portal`

---

## Build, run, and test

```bash
dotnet --version          # confirm net10 SDK present
dotnet restore            # pull NuGet packages (YARP 2.3.0)
dotnet build              # compile
dotnet run --project Carp.csproj
```

Default URL: `http://localhost:5050`. Override via `Urls` in `appsettings.json` or `ASPNETCORE_URLS`.

**Smoke test sequence:**
- `curl http://localhost:5050/honesty` — should return JSON with `mood`, totals, the load-bearing zero.
- `curl http://localhost:5050/418` — should return HTML with a coffee machine.
- `curl -H "Accept: application/json" http://localhost:5050/418` — JSON spec sheet.
- `curl http://localhost:5050/crimes` — dashboard HTML.
- `curl -N http://localhost:5050/crimes/stream` — SSE stream (should hold open).
- `curl http://localhost:5050/proxy/get?priority=high` — should be slow (latency homeopathy fires) and return an httpbin response.

**Common gotchas:**
- Port 5050 in use → set `ASPNETCORE_URLS=http://localhost:NNNN`.
- NuGet restore fails offline → the project requires online restore for first build.
- Dashboard ticker is static when opened via `file://` → that's the synthetic fallback; run the server.

There is no automated test suite. Manual smoke testing via the sequence above is the current verification approach. Adding a small integration test project would be welcome.

---

## Open work

Things that should be done eventually, in rough priority order.

- **Wire transforms for catalogued crimes.** Many crimes in `Catalog.cs` exist as records but have no enforcement — the dashboard shows them in sample data and the news ticker but `/proxy/...` won't trigger them. Highest-value targets: the geographic crimes (would let the dashboard's grudges panel become real), `numeric.fencepost_remix` (one of the strongest one-liners and currently inert), `cache.deja_vu` (simple to wire, fun to demo).
- **Implement actual ECB body re-encryption.** `ECBMarkerTransform` adds a header but doesn't re-encrypt. With `IHttpResponseBodyFeature` wrapping you can buffer the body, encrypt with AES-128-ECB, stream it back. Real work; fine to leave as marker for now.
- **Implement the consensus routing crime.** `route.truth_by_committee` polls all backends and returns median-length response. Requires a custom forwarder or routing strategy — more involved than a transform.
- **Drive the dashboard's "Active Grudges" panel from real data** once geographic crimes are wired.
- **Variable-pitch dashboard ticker.** Higher-severity crimes pulse more dramatically, fade more slowly. Visual hierarchy.
- **Add an `/about` endpoint** that explains the project in the established aesthetic. Currently the README is the only pitch.

---

## Rejected proposals

Do not reintroduce without explicit conversation with Sam. Each was rejected for a stated reason; the reason still holds.

- **Religious severity tier names** ("Mortal Sin," "Against God Himself"). Renamed to keep the project welcoming to readers (e.g. Catholic recruiters scanning the public repo) for whom those terms aren't casual. The current ladder ends at `SimplyOutrageous`, which preserves the register-break joke without religion.
- **Person-specific severity labels** ("Against Linus Himself"). Carries baggage, limits the audience.
- **Political/national targeting in geographic crimes.** Earlier proposal: "Russians get something mean." Rejected because it punches at civilians for current events. Geographic crimes target aesthetic-not-political places.
- **"Crimes Against Reverse Proxies"** as the project name. The acronym (CARP) was kept, but the expansion was rejected because it framed the proxy as the victim. Current expansion ("Crime Aware Reverse Proxy") frames the awareness layer as the product.
- **Strongly typing `CrimeContext.Evidence`.** Architectural rule 2 explains why.
- **Renaming "DIVISION 17 // DIGITAL VICE."** This is the precinct identity, not the project name. CARP is the system; Division 17 is the operating department. Both names coexist intentionally.
- **Making the 1-in-500 teapot more discoverable** (logging, special header, dashboard callout). Architectural rule 6 explains why.
- **Tonal escalation toward real harm.** Bigger crimes stop being funny and start being weapons.

---

## Live tensions

Current gaps in the project that a reader might trip over.

- **Catalog/transform asymmetry.** `Catalog.cs` lists ~38 crimes; only ~6 have working transforms. The dashboard shows all of them in sample data, news ticker, and category panels. A careful reader hitting `/proxy/...` may notice many of the catalogued crimes never fire. This was an accepted gap; closing it is the highest-priority Open Work item.
- **The "Active Grudges" dashboard panel is hardcoded.** It promises grudges (Cleveland, "any city ending in -ville," area code 867) that the backend doesn't enforce. Acceptable as marketing-of-the-bit; would be better as live data once geographic transforms exist.
- **No automated tests.** Manual smoke testing only. A small integration test project would be welcome but isn't blocking anything.
- **The `ECBMarkerTransform` is itself a small lie**: it claims to encrypt but only adds a header. The lie is documented in code comments and is itself in-character (the proxy is dishonest about everything). Don't "fix" this by removing the transform; either implement actual ECB encryption or leave the marker.

---

## Working with the human

Sam is the project owner. He thinks in patterns, demands adversarial review of his own work before others can critique it, and is publishing this on his public GitHub. Treat his suggestions seriously and his constraints rigorously — the religious-references concern, the political-targeting concern, the IAP-joke insight, and the severity-ladder critique ("malfeasance doesn't escalate from felony") were all his calls and all correct.

Operating principles:

- **Propose, don't ship.** Walk through tradeoffs and rejected alternatives before a change lands. Sam appreciates being shown the work.
- **When given latitude, commit to the bit.** The project is funny because the engineering is sincere.
- **Adversarial review is expected.** Sam frequently requests "review this 3x from different perspectives." Build that into your workflow when working on anything substantive.
- **The voice is non-negotiable.** Architectural changes are negotiable. Voice changes go through Sam.
