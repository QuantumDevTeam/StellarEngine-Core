using System.IO;

namespace Stellar.Kernel.FileSystem;

/// <summary>
/// Thrown when a path names a domain that is not mounted.
/// </summary>
/// <param name="domain">The name of the domain.</param>
public sealed class DomainNotFoundException(string domain)
    : IOException($"No domain '{domain}' is mounted")
{
    /// <summary>
    /// Gets the name of the domain.
    /// </summary>
    public string Domain { get; } = domain;
}
