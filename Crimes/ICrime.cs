namespace Carp.Crimes;

public enum Severity
{
    Misdemeanor,
    Felony,
    HighCrime,
    SimplyOutrageous
}

/// <summary>
/// A crime is a record-of-misbehavior. Crimes describe what happened
/// and how to confess to it. Crimes do NOT perform misbehavior themselves —
/// transforms do that and hand the resulting <see cref="CrimeContext"/>
/// to the <see cref="ICrimeReporter"/>.
/// </summary>
public interface ICrime
{
    string Name { get; }
    Severity Severity { get; }
    string ConfessAs(CrimeContext ctx);
}

/// <summary>
/// Heterogeneous evidence dictionary, deliberately stringly-typed because
/// crimes vary wildly in their per-incident fields and we want them
/// serializing freely to JSON for the ledger and as tags for OTel.
/// Strongly-typing per crime would fight the abstraction.
/// </summary>
public sealed class CrimeContext
{
    public required string CrimeId { get; init; }
    public required DateTimeOffset CommittedAt { get; init; }
    public required string Method { get; init; }
    public required string RequestPath { get; init; }
    public Dictionary<string, string> Evidence { get; init; } = new();

    public static CrimeContext From(HttpContext http, string crimeId) => new()
    {
        CrimeId = crimeId,
        CommittedAt = DateTimeOffset.UtcNow,
        Method = http.Request.Method,
        RequestPath = http.Request.Path + http.Request.QueryString
    };
}

public static class CrimeId
{
    private static readonly char[] _alphabet = "0123456789abcdef".ToCharArray();

    /// <summary>4 bytes / 8 hex chars = 32 bits of ID space.
    /// Collisions are still possible at high volume; we accept them as
    /// authentic-to-the-bit (real precincts reuse case numbers too).</summary>
    public static string Generate()
    {
        Span<char> buf = stackalloc char[8];
        for (int i = 0; i < 8; i++) buf[i] = _alphabet[Random.Shared.Next(16)];
        return new string(buf);
    }
}
