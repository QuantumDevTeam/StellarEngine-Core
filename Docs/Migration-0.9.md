# Migrating to 0.9

- [README.md](https://github.com/QuantumDevTeam/StellarEngine-Core#readme)

What changed for the code that uses the core, from 0.8.

| Component | 0.8 | 0.9 | Why |
|---|---|---|---|
| Orchester, Kernel, Core, Runtime | 0.8.0 | 0.9.0 | Breaking: identifiers and labels are values; the contracts of the file system |
| P/Invoke exports | 2.0 | 3.0 | The registries of identifiers and labels are gone |

## Identifiers

An identifier is a value of the Kernel, not a class of a wrapper, and it is never registered.

| Before | Now |
|---|---|
| `Stellar.Kernel.IIdentifier` | `Stellar.Kernel.Identifier`, a struct |
| `Stellar.Core.Identifier` (from a wrapper) | `Stellar.Kernel.Identifier` |
| `Identifier.Create()`, `Identifier.CreateAndRegister()` | `Identifier.New()` |
| `identifier.UID` (a `Guid`) | the identifier itself: compare it, write it (`ToString`, `ToBytes`), read it (`Parse`, `FromBytes`) |
| `Identifier.Parse(text)` returning `null` | `Identifier.Parse(text)` throws, `Identifier.TryParse(text, out id)` |
| `ToCanonicalString()` | `ToString()`, the same text as the native core |
| `identifier.Register()`, `Dispose()`, `IsRegistered`, `Identifier.Get(...)` | nothing: objects are registered, not identifiers |
| `CoreContext.Identifiers`, `Engine.Registry.Identifiers` | nothing |
| `null` for no identifier | `Identifier.Empty`, `IsEmpty` |

`Guid` is not part of the API of the engine any more: code works with identifiers.

## Labels

A label is an identifier and a name, a value. There is no registry of labels and no lookup by name.

| Before | Now |
|---|---|
| `Stellar.Kernel.ILabel`, `Stellar.Core.Label` | `Stellar.Kernel.Label`, a struct |
| `Label.Create(name)`, registered | `Label.New(name)`, or `new Label(identifier, name)` |
| `Label.Get(name)`, `Label.Get(identifier)` | nothing: keep the label |
| `string` converted to a `Label` implicitly | nothing: make the label |
| `CoreContext.Labels`, `Engine.Registry.Labels` | nothing |
| `IEventType : ILabel` | `IEventType : IMetaQuant, ILabeled` |

## The file system

The contracts of `Stellar.Kernel.FileSystem` are new: `FilePath`, `DomainScheme`, `IFileDriver`, `IDomain`,
`IFileSystem`, and the resources (`IResource`, `IResourceSystem`). `DomainType`, `IPath`, `IFile`, `IFileType`,
`IFileProvider`, `IFileProviderFactory`, `IResourceFactory` and `IResourceOrchestrator` are gone. They are compiled for
`net9.0` only. The module Stellar.FileSystem implements them.
