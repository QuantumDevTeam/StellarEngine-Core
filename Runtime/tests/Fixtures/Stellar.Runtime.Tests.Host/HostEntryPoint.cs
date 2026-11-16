using System.Diagnostics.CodeAnalysis;
using Stellar.Kernel.Configuration;
using Stellar.Kernel.EntryPoint;
using Stellar.Kernel.Failures;
using Stellar.Kernel.Lifecycle;
using Stellar.Kernel.Quantization;
using Stellar.Kernel.Services;

namespace Stellar.Runtime.Tests.Host;

/// <summary>
/// Owns the services, like Stellar.Core, and reports failures by writing them down.
/// </summary>
[StellarEntryPoint]
public sealed class HostEntryPoint(RuntimeConfiguration configuration)
    : StellarEntryPoint("Tests/Host", configuration), IServiceHost
{
    public IServiceCatalog Services { get; } = new Registry();

    public override int Run<TContext>(ref readonly TContext context)
    {
        Events.Add($"host:run control={Services.Find<IEngineControl>() is not null}");
        Services.Set<IFailureReporter>(new Reporter());
        return 0;
    }

    public override void RequestStop<TContext>(ref readonly TContext context)
    {
        Events.Add($"host:stop {context.Reason}");
    }

    public override void Dispose()
    {
        Services.Remove<IFailureReporter>();
        Events.Add("host:dispose");
    }

    private sealed class Reporter : IFailureReporter
    {
        public bool Report(Exception exception, FailureType type, bool critical, IQuantumObject? sender = null)
        {
            Events.Add($"report:{type}:{exception.Message}");
            return critical;
        }
    }

    // The Kernel only: a catalog of its own, as Stellar.Core would have
    private sealed class Registry : IServiceCatalog
    {
        private readonly Dictionary<Type, object> _services = new();

        public int Count { get { lock (_services) return _services.Count; } }

        public IReadOnlyCollection<Type> Keys { get { lock (_services) return _services.Keys.ToArray(); } }

        public IReadOnlyCollection<object> Values { get { lock (_services) return _services.Values.ToArray(); } }

        public bool Contains(Type key) { lock (_services) return _services.ContainsKey(key); }

        public bool TryGet(Type key, [MaybeNullWhen(false)] out object value) { lock (_services) return _services.TryGetValue(key, out value); }

        public object Get(Type key) => TryGet(key, out var value) ? value : throw new InvalidOperationException(key.Name);

        public void Set(Type key, object value) { lock (_services) _services[key] = value; }

        public bool TryAdd(Type key, object value) { lock (_services) return _services.TryAdd(key, value); }

        public bool Remove(Type key) { lock (_services) return _services.Remove(key); }

        public IEnumerator<KeyValuePair<Type, object>> GetEnumerator() { lock (_services) return _services.ToList().GetEnumerator(); }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

/// <summary>
/// What the fixtures did, for the tests to read: a list the test puts in the data of the process.
/// </summary>
public static class Events
{
    public const string Key = "Stellar.Runtime.Tests.Events";

    public static void Add(string message)
    {
        if (AppContext.GetData(Key) is List<string> events)
            lock (events)
            {
                events.Add(message);
            }
    }
}
