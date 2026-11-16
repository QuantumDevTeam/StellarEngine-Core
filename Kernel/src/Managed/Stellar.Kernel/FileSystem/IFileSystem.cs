using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.FileSystem;

/// <summary>
/// The files of an engine instance: drivers, the domains they mounted and the content types, behind
/// <see cref="FilePath"/>.
/// </summary>
/// <remarks>
/// <para>Each engine instance has its own file system; nothing of it is static. Code above names domains
/// (<c>assets://</c>, <c>user://</c>), never locations of the machine: those are given to drivers once, when the
/// domains are mounted.</para>
/// <para>The file system routes a path to its domain (<see cref="GetDomain"/>); the operations on paths are the
/// extensions of <see cref="FileSystemExtensions"/>. The data of files, cached and shared, is the business of the
/// resource system (<see cref="Resources.IResourceSystem"/>), built on this one.</para>
/// <para>Every member is safe to call from any thread.</para>
/// </remarks>
public interface IFileSystem
    : IQuant, IDisposable
{
    /// <summary>
    /// Gets the content types of this file system, by extension.
    /// </summary>
    IContentTypeRegistry ContentTypes { get; }

    /// <summary>
    /// Gets the domains mounted now.
    /// </summary>
    IReadOnlyCollection<IDomain> Domains { get; }

    /// <summary>
    /// Raised after a domain is mounted.
    /// </summary>
    event Action<IDomain>? Mounted;

    /// <summary>
    /// Raised before a domain is unmounted, while it still works: caches drop what they hold of it.
    /// </summary>
    event Action<IDomain>? Unmounting;

    /// <summary>
    /// Registers the driver of a scheme.
    /// </summary>
    /// <param name="driver">The driver.</param>
    /// <exception cref="InvalidOperationException">Thrown if another driver has the same scheme.</exception>
    void RegisterDriver(IFileDriver driver);

    /// <summary>
    /// Finds the driver of a scheme.
    /// </summary>
    /// <param name="scheme">The scheme.</param>
    /// <param name="driver">The driver, or <c>null</c>.</param>
    /// <returns><c>true</c> if a driver of <paramref name="scheme"/> is registered.</returns>
    bool TryGetDriver(DomainScheme scheme, [NotNullWhen(true)] out IFileDriver? driver);

    /// <summary>
    /// Mounts a domain with the driver of its scheme.
    /// </summary>
    /// <param name="mount">What to mount.</param>
    /// <returns>The mounted domain.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if a domain of that name is mounted, or no driver has the scheme.
    /// </exception>
    IDomain Mount(DomainMount mount);

    /// <summary>
    /// Unmounts a domain and disposes it.
    /// </summary>
    /// <param name="name">The name of the domain.</param>
    /// <returns><c>true</c> if the domain was mounted.</returns>
    bool Unmount(string name);

    /// <summary>
    /// Finds a mounted domain.
    /// </summary>
    /// <param name="name">The name of the domain.</param>
    /// <param name="domain">The domain, or <c>null</c>.</param>
    /// <returns><c>true</c> if a domain of that name is mounted.</returns>
    bool TryGetDomain(string name, [NotNullWhen(true)] out IDomain? domain);

    /// <summary>
    /// Gets a mounted domain.
    /// </summary>
    /// <param name="name">The name of the domain.</param>
    /// <returns>The domain.</returns>
    /// <exception cref="DomainNotFoundException">Thrown if no domain of that name is mounted.</exception>
    IDomain GetDomain(string name);
}
