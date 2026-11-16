using Stellar.Core.Quantization;
using Stellar.Kernel;
using Stellar.Kernel.Quantization;

namespace Stellar.Core.Data.Collections;

/// <summary>
/// Represents a quantum object that can contain and manage other identifiable quantum objects.
/// </summary>
/// <inheritdoc/>
public class WritableTable<T>
    : ConstantTable<T>
    where T : IIdentifiableQuantumObject
{
    #region Constructors

    /// <inheritdoc/>
    public WritableTable(MetaQuant meta, FastConcurrentIdentifierMap<T> data)
        : base(meta, data)
    {
    }

    /// <inheritdoc/>
    public WritableTable(MetaQuant meta, Dictionary<Identifier, T> data)
        : base(meta, data)
    {
    }

    /// <inheritdoc/>
    public WritableTable(MetaQuant meta)
        : base(meta)
    {
    }

    /// <inheritdoc/>
    public WritableTable()
    {
    }

    #endregion

    /// <inheritdoc/>
    protected override bool IsReadOnly => false;

    #region Item support

    /// <inheritdoc/>
    public override bool Set(IIdentifiableQuantumObject obj)
    {
        if (obj is not T typed || !Native.Add(UID, obj.UID))
            return false;

        return Data.TryAdd(obj.UID, typed, true);
    }

    /// <inheritdoc/>
    public override T? Pop(Identifier id)
    {
        if (!Native.Remove(UID, id))
            return default;

        Data.TryRemove(id, out var quant);
        return quant;
    }

    #endregion
}
