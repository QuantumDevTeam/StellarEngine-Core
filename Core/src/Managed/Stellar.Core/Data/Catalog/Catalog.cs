using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Stellar.Kernel.Data.Catalog;

namespace Stellar.Core.Data.Catalog;

/// <summary>
/// The catalog of the engine: values by a key, safe from any thread.
/// </summary>
/// <remarks>
/// <para>Used as it is, or as the base of a catalog with rules of its own: a derived class brings keys to one form
/// (<see cref="NormalizeKey"/>), refuses what it cannot hold (<see cref="Validate"/>) and says what is missing
/// (<see cref="NotFound"/>).</para>
/// </remarks>
/// <typeparam name="TKey">The key.</typeparam>
/// <typeparam name="TValue">The value.</typeparam>
public class Catalog<TKey, TValue>
    : ICatalog<TKey, TValue>
    where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, TValue> _items;

    /// <summary>
    /// Makes an empty catalog.
    /// </summary>
    /// <param name="comparer">How keys compare; the default comparer of <typeparamref name="TKey"/> if <c>null</c>.</param>
    public Catalog(IEqualityComparer<TKey>? comparer = null)
    {
        _items = new ConcurrentDictionary<TKey, TValue>(comparer ?? EqualityComparer<TKey>.Default);
    }

    /// <inheritdoc/>
    public int Count => _items.Count;

    /// <inheritdoc/>
    public IReadOnlyCollection<TKey> Keys => _items.Keys.ToArray();

    /// <inheritdoc/>
    public IReadOnlyCollection<TValue> Values => _items.Values.ToArray();

    /// <inheritdoc/>
    public bool Contains(TKey key)
    {
        return TryNormalize(key, out var normalized) && _items.ContainsKey(normalized);
    }

    /// <inheritdoc/>
    public bool TryGet(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        if (TryNormalize(key, out var normalized)) return _items.TryGetValue(normalized, out value);

        value = default;
        return false;
    }

    /// <inheritdoc/>
    public TValue Get(TKey key)
    {
        return TryGet(key, out var value) ? value : throw NotFound(key);
    }

    /// <inheritdoc/>
    public void Set(TKey key, TValue value)
    {
        var normalized = NormalizeKey(key);
        Validate(normalized, value);
        _items[normalized] = value;
    }

    /// <inheritdoc/>
    public bool TryAdd(TKey key, TValue value)
    {
        var normalized = NormalizeKey(key);
        Validate(normalized, value);
        return _items.TryAdd(normalized, value);
    }

    /// <inheritdoc/>
    public bool Remove(TKey key)
    {
        return TryNormalize(key, out var normalized) && _items.TryRemove(normalized, out _);
    }

    /// <summary>
    /// Removes the value of a key and gives it back.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The removed value, or <c>default</c>.</param>
    /// <returns><c>true</c> if there was one.</returns>
    public bool TryRemove(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        if (TryNormalize(key, out var normalized)) return _items.TryRemove(normalized, out value);

        value = default;
        return false;
    }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return _items.ToArray().AsEnumerable().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Brings a key to the form the catalog stores. The key as it is by default.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The key in its form.</returns>
    /// <exception cref="ArgumentException">Thrown for a key the catalog cannot hold.</exception>
    protected virtual TKey NormalizeKey(TKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return key;
    }

    /// <summary>
    /// Refuses a value the catalog cannot hold under a key. Refuses <c>null</c> by default.
    /// </summary>
    /// <param name="key">The key, in its form.</param>
    /// <param name="value">The value.</param>
    /// <exception cref="ArgumentException">Thrown for a value the catalog cannot hold.</exception>
    protected virtual void Validate(TKey key, TValue value)
    {
        if (value is null) throw new ArgumentNullException(nameof(value), $"No value for '{key}'");
    }

    /// <summary>
    /// Makes the exception of <see cref="Get"/> for a key without a value.
    /// </summary>
    /// <param name="key">The key, as it was asked for.</param>
    /// <returns>The exception.</returns>
    protected virtual Exception NotFound(TKey key)
    {
        return new KeyNotFoundException($"Nothing for '{key}'");
    }

    // A key that cannot be held has no value: lookups answer no instead of throwing
    private bool TryNormalize(TKey key, [MaybeNullWhen(false)] out TKey normalized)
    {
        try
        {
            normalized = NormalizeKey(key);
            return true;
        }
        catch (ArgumentException)
        {
            normalized = default;
            return false;
        }
    }
}
