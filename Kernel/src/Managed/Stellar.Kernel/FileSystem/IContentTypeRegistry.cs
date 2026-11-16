using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.FileSystem;

/// <summary>
/// The content types of a file system, by extension.
/// </summary>
/// <remarks>
/// Drivers fill <see cref="FileEntry.Content"/> from here, unless their source tells it (a network share with a
/// content type of its own). Modules add the formats they read.
/// </remarks>
public interface IContentTypeRegistry
    : IQuantumObject
{
    /// <summary>
    /// Maps an extension to a content type, replacing the previous one.
    /// </summary>
    /// <param name="extension">The extension with its dot, such as <c>.png</c>; any case.</param>
    /// <param name="contentType">The content type of the files with that extension.</param>
    void Register(string extension, ContentType contentType);

    /// <summary>
    /// Finds the content type of an extension.
    /// </summary>
    /// <param name="extension">The extension with its dot, such as <c>.png</c>; any case.</param>
    /// <param name="contentType">The content type, or the unknown one.</param>
    /// <returns><c>true</c> if the extension is registered.</returns>
    bool TryGet(string extension, out ContentType contentType);

    /// <summary>
    /// Gets the content type of a file by its extension.
    /// </summary>
    /// <param name="path">The file.</param>
    /// <returns>The content type, or <see cref="ContentType.OctetStream"/> for an unknown extension.</returns>
    ContentType Resolve(FilePath path);
}
