using System.Collections.Concurrent;
using Stellar.Kernel;
using Stellar.Kernel.Data.Registry;
using Stellar.Kernel.Quantization;

namespace Stellar.Core.Data.Registry;

/// <summary>
/// The registry of the engine: objects of one type by their identifiers, safe from any thread.
/// </summary>
/// <remarks>
/// Each engine has one registry per type (<see cref="Global.CoreEngine.CoreRegistries.Of{T}"/>), and a registry can
/// also be made on its own. It tells every object it takes and lets go (<see cref="IRegistrableQuantumObject.Registrations"/>).
/// </remarks>
/// <typeparam name="T">The type of the objects: a base class or a contract.</typeparam>
public sealed class Registry<T>
    : IRegistry<T>, IUntypedRegistry
    where T : class, IRegistrableQuantumObject
{
    private readonly ConcurrentDictionary<Identifier, T> _items = new();

    /// <inheritdoc/>
    public Type ItemType => typeof(T);

    /// <inheritdoc/>
    public int Size => _items.Count;

    /// <inheritdoc/>
    public ICollection<Identifier> Keys => _items.Keys.ToArray();

    /// <inheritdoc/>
    public ICollection<T> Values => _items.Values.ToArray();

    /// <inheritdoc/>
    public bool Register(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.UID.IsEmpty) throw new ArgumentException("An object without an identifier cannot be registered", nameof(item));
        if (!_items.TryAdd(item.UID, item)) return false;

        item.Registrations.OnRegistered(this);
        return true;
    }

    /// <inheritdoc/>
    public bool Exists(Identifier id)
    {
        return _items.ContainsKey(id);
    }

    /// <inheritdoc/>
    public T? Get(Identifier id)
    {
        return _items.GetValueOrDefault(id);
    }

    /// <inheritdoc/>
    public TItem? Get<TItem>(Identifier id)
        where TItem : class, T
    {
        return _items.GetValueOrDefault(id) as TItem;
    }

    /// <inheritdoc/>
    public T? Pop(Identifier id)
    {
        if (!_items.TryRemove(id, out var item)) return null;

        item.Registrations.OnUnregistered(this);
        return item;
    }

    /// <inheritdoc/>
    public bool Unregister(Identifier id)
    {
        return Pop(id) is not null;
    }

    bool IUntypedRegistry.Register(IRegistrableQuantumObject item)
    {
        return Register(item as T ?? throw new ArgumentException(
            $"{item.GetType().Name} is not a {typeof(T).Name}: it cannot be in this registry", nameof(item)));
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"Registry of {typeof(T).Name} ({Size})";
    }
}

/// <summary>
/// A registry reached by the type of its objects at run time: the implicit registration of Stellar.Core.
/// </summary>
internal interface IUntypedRegistry
    : IRegistry
{
    bool Register(IRegistrableQuantumObject item);
}
