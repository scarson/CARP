using System.Collections.Concurrent;
using System.Threading.Channels;
using Carp.Crimes;

namespace Carp.Ledger;

public sealed record LedgerEntry(
    string CrimeId,
    string CrimeName,
    Severity Severity,
    string Method,
    string Path,
    string Confession,
    DateTimeOffset CommittedAt,
    IReadOnlyDictionary<string, string> Evidence
);

public sealed class CrimeLedger
{
    private readonly object _lock = new();
    private readonly LinkedList<LedgerEntry> _recent = new();
    private readonly int _capacity;
    private readonly DateTimeOffset _startedAt = DateTimeOffset.UtcNow;

    // Counters by name and by severity
    private readonly ConcurrentDictionary<string, long> _byName = new();
    private readonly ConcurrentDictionary<Severity, long> _bySeverity = new();
    private long _total;

    // Subscribers for SSE
    private readonly ConcurrentDictionary<Guid, Channel<LedgerEntry>> _subscribers = new();

    public CrimeLedger(int capacity = 500) => _capacity = capacity;

    public DateTimeOffset StartedAt => _startedAt;
    public long TotalCount => Interlocked.Read(ref _total);
    public TimeSpan Uptime => DateTimeOffset.UtcNow - _startedAt;

    public void Append(LedgerEntry entry)
    {
        lock (_lock)
        {
            _recent.AddFirst(entry);
            while (_recent.Count > _capacity) _recent.RemoveLast();
        }

        Interlocked.Increment(ref _total);
        _byName.AddOrUpdate(entry.CrimeName, 1, (_, n) => n + 1);
        _bySeverity.AddOrUpdate(entry.Severity, 1, (_, n) => n + 1);

        // Fan out to subscribers — non-blocking. Drop subscribers whose channels
        // are already completed (client disconnected without unsubscribing cleanly).
        foreach (var (id, sub) in _subscribers)
        {
            if (!sub.Writer.TryWrite(entry))
            {
                // Channel is full and DropOldest didn't help, OR completed.
                // If completed, evict.
                if (sub.Reader.Completion.IsCompleted)
                {
                    _subscribers.TryRemove(id, out _);
                }
            }
        }
    }

    public IReadOnlyList<LedgerEntry> Recent(int n)
    {
        // Snapshot under the lock, process outside.
        LedgerEntry[] snap;
        lock (_lock)
        {
            int take = Math.Min(n, _recent.Count);
            snap = new LedgerEntry[take];
            int i = 0;
            foreach (var e in _recent)
            {
                if (i >= take) break;
                snap[i++] = e;
            }
        }
        return snap;
    }

    public IReadOnlyList<LedgerEntry> RandomSample(int n)
    {
        LedgerEntry[] pool;
        lock (_lock)
        {
            pool = _recent.ToArray();
        }
        if (pool.Length == 0) return Array.Empty<LedgerEntry>();

        // Reservoir-style: shuffle indices, take first n
        var rng = Random.Shared;
        var indices = Enumerable.Range(0, pool.Length).ToArray();
        for (int i = indices.Length - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }
        return indices.Take(Math.Min(n, pool.Length)).Select(idx => pool[idx]).ToList();
    }

    public IReadOnlyDictionary<string, long> CountsByName() => _byName;
    public IReadOnlyDictionary<Severity, long> CountsBySeverity() => _bySeverity;

    /// <summary>Approximate crimes/min over the last 60 seconds of the ring buffer.</summary>
    public double RecentRate()
    {
        var cutoff = DateTimeOffset.UtcNow - TimeSpan.FromSeconds(60);
        lock (_lock)
        {
            int count = _recent.Count(e => e.CommittedAt >= cutoff);
            return count; // 60s window → already crimes/min
        }
    }

    public string Mood => RecentRate() switch
    {
        < 1   => "contrite",
        < 10  => "unrepentant",
        < 100 => "gleeful",
        _     => "feral"
    };

    // ---- SSE subscription ---- //

    public (Guid id, ChannelReader<LedgerEntry> reader) Subscribe()
    {
        var id = Guid.NewGuid();
        var channel = Channel.CreateBounded<LedgerEntry>(new BoundedChannelOptions(256)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest
        });
        _subscribers[id] = channel;
        return (id, channel.Reader);
    }

    public void Unsubscribe(Guid id)
    {
        if (_subscribers.TryRemove(id, out var ch))
        {
            ch.Writer.TryComplete();
        }
    }
}
