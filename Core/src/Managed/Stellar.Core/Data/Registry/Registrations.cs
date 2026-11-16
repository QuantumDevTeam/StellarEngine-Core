using System.Collections;
using Stellar.Kernel.Data.Registry;
using Stellar.Kernel.Quantization;

namespace Stellar.Core.Data.Registry;

/// <summary>
/// The registries an object is in, kept by the registries themselves.
/// </summary>
/// <param name="owner">The object.</param>
public sealed class Registrations(IRegistrableQuantumObject owner)
    : IRegistrations
{
    private readonly Lock _sync = new();

    // Replaced on change, never mutated: readers need no lock
    private volatile IRegistry[] _registries = [];

    /// <inheritdoc/>
    public int Count => _registries.Length;

    /// <inheritdoc/>
    public bool Contains(IRegistry registry)
    {
        return Array.IndexOf(_registries, registry) >= 0;
    }

    /// <inheritdoc/>
    public void OnRegistered(IRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        lock (_sync)
        {
            if (Array.IndexOf(_registries, registry) < 0) _registries = [.. _registries, registry];
        }
    }

    /// <inheritdoc/>
    public void OnUnregistered(IRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        lock (_sync)
        {
            if (Array.IndexOf(_registries, registry) >= 0)
                _registries = _registries.Where(r => !ReferenceEquals(r, registry)).ToArray();
        }
    }

    /// <inheritdoc/>
    public void UnregisterAll()
    {
        // Each registry tells back, which takes it off this list
        foreach (var registry in _registries) registry.Unregister(owner.UID);
    }

    /// <inheritdoc/>
    public IEnumerator<IRegistry> GetEnumerator()
    {
        return ((IEnumerable<IRegistry>)_registries).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
