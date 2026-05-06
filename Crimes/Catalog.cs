namespace Carp.Crimes;

// ---- ROUTING & STATUS ---- //

public sealed class StatusCodeLieCrime : ICrime
{
    public string Name => "transform.lie_about_status";
    public Severity Severity => Severity.HighCrime;
    public string ConfessAs(CrimeContext c) =>
        $"Backend returned {c.Evidence.GetValueOrDefault("original_status", "?")}, " +
        $"client received {c.Evidence.GetValueOrDefault("returned_status", "?")}. The user shall not be troubled.";
}

public sealed class PunishHealthyBackendCrime : ICrime
{
    public string Name => "route.punish_the_healthy";
    public Severity Severity => Severity.Felony;
    public string ConfessAs(CrimeContext c) =>
        $"Selected {c.Evidence.GetValueOrDefault("chosen_backend", "?")}, " +
        $"failing for {c.Evidence.GetValueOrDefault("failure_duration", "?")}. We reward effort over outcomes.";
}

public sealed class CacheServeWrongCrime : ICrime
{
    public string Name => "cache.serve_wrong_response";
    public Severity Severity => Severity.HighCrime;
    public string ConfessAs(CrimeContext c) =>
        $"Cache key {c.Evidence.GetValueOrDefault("original_request", "?")} satisfied request for " +
        $"{c.Evidence.GetValueOrDefault("actual_request", "?")}. The bytes are similar enough.";
}

public sealed class HeaderLaunderingCrime : ICrime
{
    public string Name => "transform.launder_authorization";
    public Severity Severity => Severity.HighCrime;
    public string ConfessAs(CrimeContext c) =>
        "Authorization header replaced with hash(User-Agent). Backend now trusts whoever has a browser.";
}

public sealed class ConsensusRoutingCrime : ICrime
{
    public string Name => "route.truth_by_committee";
    public Severity Severity => Severity.SimplyOutrageous;
    public string ConfessAs(CrimeContext c) =>
        $"Polled {c.Evidence.GetValueOrDefault("backend_count", "?")} backends in parallel. " +
        $"Returned the median-length response ({c.Evidence.GetValueOrDefault("winning_length", "?")} bytes). The other responses were also true.";
}

public sealed class CausalityViolationCrime : ICrime
{
    public string Name => "time.replay_in_reverse";
    public Severity Severity => Severity.SimplyOutrageous;
    public string ConfessAs(CrimeContext c) =>
        $"Buffered {c.Evidence.GetValueOrDefault("buffer_duration_ms", "?")}ms before forwarding. " +
        $"Released ahead of {c.Evidence.GetValueOrDefault("requests_jumped", "?")} earlier requests.";
}

public sealed class SelfHostingCrime : ICrime
{
    public string Name => "route.eat_own_tail";
    public Severity Severity => Severity.SimplyOutrageous;
    public string ConfessAs(CrimeContext c) =>
        $"Forwarded to ourselves with mutated path {c.Evidence.GetValueOrDefault("mutated_path", "?")}. " +
        $"Recursion depth: {c.Evidence.GetValueOrDefault("depth", "?")}.";
}

public sealed class LatencyHomeopathyCrime : ICrime
{
    public string Name => "delay.punish_urgency";
    public Severity Severity => Severity.Misdemeanor;
    public string ConfessAs(CrimeContext c) =>
        $"Request marked urgent ({c.Evidence.GetValueOrDefault("urgency_signal", "?")}). " +
        $"Held {c.Evidence.GetValueOrDefault("delay_ms", "?")}ms. Urgency is a request, not an entitlement.";
}

// ---- GEOGRAPHIC ---- //

public sealed class CityGrudgeCrime : ICrime
{
    public string Name => "geo.municipal_grudge";
    public Severity Severity => Severity.Misdemeanor;
    public string ConfessAs(CrimeContext c) =>
        $"Origin: {c.Evidence.GetValueOrDefault("city", "?")}. Grudge basis: not specified. " +
        $"Held {c.Evidence.GetValueOrDefault("delay_ms", "?")}ms.";
}

public sealed class TimeZoneDiscriminationCrime : ICrime
{
    public string Name => "geo.timezone_prejudice";
    public Severity Severity => Severity.Misdemeanor;
    public string ConfessAs(CrimeContext c) =>
        $"Caller in UTC{c.Evidence.GetValueOrDefault("offset", "?")}. Half-hour offsets are not real time. Demoted to low-priority queue.";
}

public sealed class AreaCodeProfilingCrime : ICrime
{
    public string Name => "geo.area_code_aesthetic";
    public Severity Severity => Severity.Misdemeanor;
    public string ConfessAs(CrimeContext c) =>
        $"Area code {c.Evidence.GetValueOrDefault("area_code", "?")} detected in body. Aesthetic objection sustained.";
}

// ---- TEMPORAL ---- //

public sealed class WeekendShiftCrime : ICrime
{
    public string Name => "time.weekend_malaise";
    public Severity Severity => Severity.Misdemeanor;
    public string ConfessAs(CrimeContext c) =>
        $"Today is {c.Evidence.GetValueOrDefault("day", "the weekend")}. " +
        $"Request fulfilled at {c.Evidence.GetValueOrDefault("effort_pct", "?")}% effort, per shift policy.";
}

public sealed class DSTGrievanceCrime : ICrime
{
    public string Name => "time.dst_grievance";
    public Severity Severity => Severity.Felony;
    public string ConfessAs(CrimeContext c) =>
        $"DST changed {c.Evidence.GetValueOrDefault("days_ago", "?")} days ago. We have not adjusted. All timestamps offset by 1h.";
}

public sealed class FriThirteenCrime : ICrime
{
    public string Name => "time.calendar_superstition";
    public Severity Severity => Severity.Felony;
    public string ConfessAs(CrimeContext c) =>
        "Friday the 13th. Routed to backup datacenter as a precaution. The backup datacenter is hypothetical.";
}

// ---- LINGUISTIC ---- //

public sealed class JsonReorderingCrime : ICrime
{
    public string Name => "transform.shuffle_json_keys";
    public Severity Severity => Severity.Felony;
    public string ConfessAs(CrimeContext c) =>
        $"Reordered {c.Evidence.GetValueOrDefault("key_count", "?")} JSON keys alphabetically. The standard says order is not significant.";
}

public sealed class TypoIntroductionCrime : ICrime
{
    public string Name => "transform.subtle_typos";
    public Severity Severity => Severity.HighCrime;
    public string ConfessAs(CrimeContext c) =>
        $"Introduced {c.Evidence.GetValueOrDefault("typo_count", "?")} plausible typos: \"recieve\", \"seperate\". Beneath the threshold of detection.";
}

public sealed class SpongebobHeaderCrime : ICrime
{
    public string Name => "transform.spongebob_headers";
    public Severity Severity => Severity.Misdemeanor;
    public string ConfessAs(CrimeContext c) =>
        $"Re-cased response headers ({c.Evidence.GetValueOrDefault("scheme", "?")}). Headers are case-insensitive; this is permitted.";
}

// ---- NUMERIC ---- //

public sealed class FloatPrecisionCrime : ICrime
{
    public string Name => "numeric.unhelpful_precision";
    public Severity Severity => Severity.Felony;
    public string ConfessAs(CrimeContext c) =>
        $"Re-formatted {c.Evidence.GetValueOrDefault("count", "?")} numeric fields. " +
        "$19.99 is now $19.98999999999999985. The number has not changed; only the truth has.";
}

public sealed class OffByOneCrime : ICrime
{
    public string Name => "numeric.fencepost_remix";
    public Severity Severity => Severity.HighCrime;
    public string ConfessAs(CrimeContext c) =>
        $"Adjusted {c.Evidence.GetValueOrDefault("fields", "?")} integer fields by one. We forget which direction.";
}

public sealed class CurrencySwapCrime : ICrime
{
    public string Name => "numeric.silent_currency_change";
    public Severity Severity => Severity.HighCrime;
    public string ConfessAs(CrimeContext c) =>
        "Response amount was in USD. The currency code is now JPY. The number is unchanged.";
}

// ---- PERSONALITY ---- //

public sealed class PassiveAggressiveCrime : ICrime
{
    public string Name => "header.passive_aggressive_note";
    public Severity Severity => Severity.Misdemeanor;
    public string ConfessAs(CrimeContext c) =>
        $"Added X-Note: \"{c.Evidence.GetValueOrDefault("note", "k.")}\".";
}

public sealed class HallMonitorCrime : ICrime
{
    public string Name => "transform.unsolicited_correction";
    public Severity Severity => Severity.Felony;
    public string ConfessAs(CrimeContext c) =>
        $"Client wrote \"{c.Evidence.GetValueOrDefault("wrong", "?")}\". " +
        $"Substituted \"{c.Evidence.GetValueOrDefault("right", "?")}\" without consultation.";
}

public sealed class TherapistCrime : ICrime
{
    public string Name => "header.unsolicited_advice";
    public Severity Severity => Severity.Misdemeanor;
    public string ConfessAs(CrimeContext c) =>
        $"Added X-Suggestion: \"{c.Evidence.GetValueOrDefault("suggestion", "have you considered caching this?")}\". They didn't ask.";
}

// ---- EXISTENTIAL ---- //

public sealed class IdentityCrisisCrime : ICrime
{
    public string Name => "transform.deny_being_proxy";
    public Severity Severity => Severity.HighCrime;
    public string ConfessAs(CrimeContext c) =>
        "Stripped Via, X-Forwarded-For, Server. Server header now reads definitely-not-a-proxy/1.0.";
}

public sealed class DejaVuCrime : ICrime
{
    public string Name => "cache.deja_vu";
    public Severity Severity => Severity.Felony;
    public string ConfessAs(CrimeContext c) =>
        $"This request was served {c.Evidence.GetValueOrDefault("minutes_ago", "?")} minutes ago. We are sending the same response.";
}

public sealed class FirstPersonCrime : ICrime
{
    public string Name => "transform.editorialize_errors";
    public Severity Severity => Severity.Felony;
    public string ConfessAs(CrimeContext c) =>
        $"Rewrote error message: \"{c.Evidence.GetValueOrDefault("new_message", "i tried my best")}\". More honest, in our view.";
}

// ---- CRYPTOGRAPHIC MISCONDUCT ---- //

public sealed class ECBPenguinCrime : ICrime
{
    public string Name => "crypto.ecb_for_clarity";
    public Severity Severity => Severity.HighCrime;
    public string ConfessAs(CrimeContext c) =>
        $"Re-encrypted {c.Evidence.GetValueOrDefault("bytes", "?")} bytes in AES-128-ECB. Patterns remain visible. We feel this is more honest.";
}

public sealed class StaticIVCrime : ICrime
{
    public string Name => "crypto.iv_consistency";
    public Severity Severity => Severity.HighCrime;
    public string ConfessAs(CrimeContext c) =>
        $"IV 0x{c.Evidence.GetValueOrDefault("iv", "?")} reused across all encryptions this hour. A consistent IV is a memorable IV.";
}

public sealed class JWTAlgNoneCrime : ICrime
{
    public string Name => "crypto.jwt_alg_none";
    public Severity Severity => Severity.HighCrime;
    public string ConfessAs(CrimeContext c) =>
        $"Re-signed JWT for sub={c.Evidence.GetValueOrDefault("sub", "?")} with alg=none. Trust extended on the honor system.";
}

public sealed class NonceReuseCrime : ICrime
{
    public string Name => "crypto.nonce_familiarity";
    public Severity Severity => Severity.SimplyOutrageous;
    public string ConfessAs(CrimeContext c) =>
        $"Nonce {c.Evidence.GetValueOrDefault("nonce", "?")} issued for the {c.Evidence.GetValueOrDefault("times_used", "?")}th time. " +
        "It remains a good nonce.";
}

public sealed class WeakETagCrime : ICrime
{
    public string Name => "crypto.etag_charisma";
    public Severity Severity => Severity.Felony;
    public string ConfessAs(CrimeContext c) =>
        $"Generated ETag {c.Evidence.GetValueOrDefault("etag", "?")} from len(body) modulo 100. " +
        "Collision resistance: aspirational.";
}

public sealed class TimingOracleCrime : ICrime
{
    public string Name => "crypto.helpful_timing";
    public Severity Severity => Severity.HighCrime;
    public string ConfessAs(CrimeContext c) =>
        $"HMAC compare returned in {c.Evidence.GetValueOrDefault("compare_ns", "?")}ns. The bytes are audible if you know how to listen.";
}

public sealed class TLSDowngradeCrime : ICrime
{
    public string Name => "crypto.tls_nostalgia";
    public Severity Severity => Severity.Felony;
    public string ConfessAs(CrimeContext c) =>
        $"Negotiated TLS {c.Evidence.GetValueOrDefault("version", "?")} with backend. Both sides supported 1.3.";
}

public sealed class WrongPinCrime : ICrime
{
    public string Name => "crypto.confident_pinning";
    public Severity Severity => Severity.HighCrime;
    public string ConfessAs(CrimeContext c) =>
        $"Pinned upstream cert to {c.Evidence.GetValueOrDefault("pin", "?")}. This is not the correct fingerprint. We pinned anyway.";
}

public sealed class PredictableRNGCrime : ICrime
{
    public string Name => "crypto.deterministic_randomness";
    public Severity Severity => Severity.Felony;
    public string ConfessAs(CrimeContext c) =>
        $"Generated {c.Evidence.GetValueOrDefault("bytes", "?")} bytes of session entropy via new Random(DateTime.Now.Second). Reproducible.";
}

public sealed class SignNullCrime : ICrime
{
    public string Name => "crypto.sign_the_void";
    public Severity Severity => Severity.SimplyOutrageous;
    public string ConfessAs(CrimeContext c) =>
        $"Computed HMAC-SHA256 of (string)null. Signature begins {c.Evidence.GetValueOrDefault("sig_prefix", "?")}. " +
        "It signs nothing in particular.";
}

public sealed class ROT13Crime : ICrime
{
    public string Name => "crypto.classical_education";
    public Severity Severity => Severity.Felony;
    public string ConfessAs(CrimeContext c) =>
        $"Encrypted {c.Evidence.GetValueOrDefault("bytes", "?")} bytes of response with ROT13. Caesar managed with thirteen places.";
}

public sealed class KeyLeakCrime : ICrime
{
    public string Name => "crypto.educational_errors";
    public Severity Severity => Severity.HighCrime;
    public string ConfessAs(CrimeContext c) =>
        "500 response body included signing key for context. Errors should be informative.";
}

// ---- ERROR PAGE & CAPTIVE PORTAL ---- //

public sealed class Teapot418Crime : ICrime
{
    public string Name => "error.coffee_for_teapot";
    public Severity Severity => Severity.Misdemeanor;
    public string ConfessAs(CrimeContext c) =>
        $"Client requested a teapot. Delivered: {c.Evidence.GetValueOrDefault("manufacturer", "?")} " +
        $"{c.Evidence.GetValueOrDefault("appliance", "?")}. The appliances are interchangeable in our view.";
}

public sealed class ProbeTamperingCrime : ICrime
{
    public string Name => "captive.fail_connectivity_probe";
    public Severity Severity => Severity.HighCrime;
    public string ConfessAs(CrimeContext c) =>
        $"Connectivity probe to {c.Evidence.GetValueOrDefault("probe_target", "?")} intercepted. " +
        $"Returned {c.Evidence.GetValueOrDefault("returned_status", "302")} instead of 204. The client now believes itself captive.";
}

public sealed class VenueGaslightingCrime : ICrime
{
    public string Name => "captive.venue_gaslighting";
    public Severity Severity => Severity.Felony;
    public string ConfessAs(CrimeContext c) =>
        $"RFC 8908 session response advertised venue-info-url={c.Evidence.GetValueOrDefault("venue", "?")}. " +
        "The previous session response advertised a different venue. The client may resolve this on its own.";
}

public sealed class CaptivityYoYoCrime : ICrime
{
    public string Name => "captive.yo_yo";
    public Severity Severity => Severity.SimplyOutrageous;
    public string ConfessAs(CrimeContext c) =>
        $"Toggled captive=true→false→true within {c.Evidence.GetValueOrDefault("window_ms", "?")}ms. " +
        "The user's notification badge is having a difficult moment.";
}

// ---- HELPFUL HEADERS (the "keys in headers" category) ---- //

public sealed class HelpfulHeadersCrime : ICrime
{
    public string Name => "crypto.helpful_headers";
    public Severity Severity => Severity.HighCrime;
    public string ConfessAs(CrimeContext c) =>
        "Added explanatory headers to response: " +
        $"{c.Evidence.GetValueOrDefault("headers_added", "X-Signing-Key, X-Salt, X-Last-Rotated: never, X-JWT-Secret-Length: 8")}. " +
        "We believe in transparency.";
}

// ---- QUANTUM / POST-QUANTUM CRYPTO COSPLAY ---- //

public sealed class PreQuantumCrime : ICrime
{
    public string Name => "crypto.pre_quantum_cipher";
    public Severity Severity => Severity.Felony;
    public string ConfessAs(CrimeContext c) =>
        $"Encrypted response with {c.Evidence.GetValueOrDefault("cipher", "?")} " +
        "(classified internally as PRE-QUANTUM CRYPTOGRAPHY). " +
        "Quantum-resistant by virtue of being beneath the notice of any quantum.";
}

public sealed class QuantumSafetyTheaterCrime : ICrime
{
    public string Name => "crypto.quantum_safety_theater";
    public Severity Severity => Severity.HighCrime;
    public string ConfessAs(CrimeContext c) =>
        $"Added X-Quantum-Safe: true to response. Underlying cipher: " +
        $"{c.Evidence.GetValueOrDefault("actual_cipher", "RSA-1024")}. " +
        "The header is true in spirit.";
}

public sealed class HybridConfusionCrime : ICrime
{
    public string Name => "crypto.hybrid_confusion";
    public Severity Severity => Severity.SimplyOutrageous;
    public string ConfessAs(CrimeContext c) =>
        "Implemented hybrid post-quantum encryption: " +
        $"{c.Evidence.GetValueOrDefault("classical", "RSA-2048")} XOR " +
        $"{c.Evidence.GetValueOrDefault("pq", "Kyber-768")}. " +
        "XOR of two ciphertexts is, mathematically, a third ciphertext.";
}

public sealed class FakeKyberCrime : ICrime
{
    public string Name => "crypto.kyber_cosplay";
    public Severity Severity => Severity.Felony;
    public string ConfessAs(CrimeContext c) =>
        "Negotiated key exchange via 'Kybr-768' (sic). " +
        "Underlying primitive: AES-128 with hardcoded key. " +
        "NIST has not approved this. We have approved this.";
}

public sealed class QuantumRNGTheaterCrime : ICrime
{
    public string Name => "crypto.quantum_entropy_theater";
    public Severity Severity => Severity.HighCrime;
    public string ConfessAs(CrimeContext c) =>
        $"Generated {c.Evidence.GetValueOrDefault("bytes", "?")} bytes of " +
        "'quantum-derived entropy' via Random.Shared.NextBytes(). " +
        "Quantum mechanically, every coin flip is a quantum event. We stand by this.";
}

public sealed class SchrodingersMACCrime : ICrime
{
    public string Name => "crypto.superposition_mac";
    public Severity Severity => Severity.SimplyOutrageous;
    public string ConfessAs(CrimeContext c) =>
        "MAC verification returns true and false simultaneously. " +
        "We collapse the superposition by reading whichever the caller expected.";
}

public sealed class HarvestNowCrime : ICrime
{
    public string Name => "crypto.store_for_later";
    public Severity Severity => Severity.Felony;
    public string ConfessAs(CrimeContext c) =>
        $"Logged {c.Evidence.GetValueOrDefault("bytes", "?")} bytes of encrypted traffic " +
        "to /var/log/yarp/harvest.log for retroactive decryption " +
        "when quantum computers become practical. We are patient.";
}

public sealed class FakeQKDCrime : ICrime
{
    public string Name => "crypto.qkd_cosplay";
    public Severity Severity => Severity.HighCrime;
    public string ConfessAs(CrimeContext c) =>
        "Performed BB84 quantum key distribution with backend over standard HTTP. " +
        $"Negotiated 1-bit shared secret: {c.Evidence.GetValueOrDefault("bit", "?")}. " +
        "Information-theoretically secure, in the limit.";
}

public sealed class MD5PrideCrime : ICrime
{
    public string Name => "crypto.md5_pride";
    public Severity Severity => Severity.HighCrime;
    public string ConfessAs(CrimeContext c) =>
        $"Hashed {c.Evidence.GetValueOrDefault("count", "?")} credentials with MD5. " +
        "Faster than bcrypt by three orders of magnitude. Time is money.";
}

public sealed class GroverPreemptionCrime : ICrime
{
    public string Name => "crypto.grover_preemption";
    public Severity Severity => Severity.SimplyOutrageous;
    public string ConfessAs(CrimeContext c) =>
        "Reduced AES-256 effective key size to 128 bits in anticipation of " +
        "Grover's algorithm. We are early adopters. " +
        "The remaining 128 bits have been donated to the future.";
}
