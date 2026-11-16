using System.Collections.Concurrent;
using Stellar.Kernel;
using Stellar.Kernel.Data.Registry;
using Stellar.Kernel.Quantization;

namespace Stellar.Core.Data.Registry;

/// <summary>
/// Thread-safe base of the engine registries.
/// </summary>
/// <typeparam name="T">The type of stored objects.</typeparam>
public abstract class ConcurrentRegistry<T>
    : IRegistry<T>
    where T : class, IRegistrableQuantumObject
{
    private readonly ConcurrentDictionary<Identifier, T> _items = new();

    /// <summary>
    /// Gets the identifier an object is stored under.
    /// </summary>
    protected abstract Identifier IdentifierOf(T obj);

    /// <inheritdoc/>
    public bool Exists(Identifier id)
    {
        return _items.ContainsKey(id);
    }

    /// <inheritdoc/>
    public virtual bool Register(T obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        return _items.TryAdd(IdentifierOf(obj), obj);
    }

    /// <inheritdoc/>
    public T? Get(Identifier id)
    {
        return _items.GetValueOrDefault(id);
    }

    /// <inheritdoc/>
    public virtual T? Pop(Identifier id)
    {
        return _items.TryRemove(id, out var obj) ? obj : null;
    }

    /// <inheritdoc/>
    public int Size => _items.Count;

    /// <inheritdoc/>
    public ICollection<Identifier> Keys => _items.Keys.ToArray();

    /// <inheritdoc/>
    public ICollection<T> Values => _items.Values.ToArray();
}
