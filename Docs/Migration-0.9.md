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

## Registries

Objects no longer register themselves: registries do, and tell them.

| Before | Now |
|---|---|
| `IRegistrableQuantumObject.Register(registry)`, `Unregister(registry)` | `registry.Register(item)`, `registry.Pop(id)` or `Unregister(id)`; the object lists its registries in `Registrations` |
| `IRegistrableQuantumObject : IQuantumObject` | `: IIdentifiableQuantumObject`, with `Registrations` |
| `CoreEngine.Registry.Quants<T>()`, `MetaQuants<T>()` | `CoreEngine.Registry.Of<T>()`, for any registrable type, bases and contracts too |
| `QuantsRegistry<T>`, `MetaQuantsRegistry<T>`, `DataContainerRegistry`, `ConcurrentRegistry<T>` | `Registry<T>` |
| a cast after `registry.Get(id)` | `registry.Get<TDerived>(id)`: `null` when the object is not one |
| a quant in the registry of its exact type | in the registry of its home type and of every type of its hierarchy marked `[StellarRegistry]` |
| `IFailureLevel : IRegistrableQuantumObject` | `IFailureLevel : ILabeled, IDisposable`: a level lives in the table of its dispatcher |

`RegistersImplicitly` turns the implicit registration off for a class.

## Collections and containers

Containers hold the objects of the world, collections hold working data. Both are managed only: the native
mirror of the membership of containers is gone (`ContainerRegistry`, `stellar_container_*`, `CoreContext.Containers`).

| Before | Now |
|---|---|
| `DataContainer<T>` | `DataContainer<TKey, TValue>`: items by key, registered, sealable, `Changed` |
| `ConstantTable<T>`, `WritableTable<T>` | `Table<T>`: objects by identifier, `Get<TItem>`; `Seal()` instead of a read-only type |
| `Roster<T>` with `SaveBranch`, `PopBranch` | `Roster<T>`: `AddBranch`, `NewBranch`, `RemoveBranch`, `Find` through the branches; never holds itself |
| nothing | `DataTable<TRow>`: rows of one type by name |
| `IDataContainer.Get(id)`, `ContainsKey`, `Contains(obj)` | `IDataContainer` (`Count`, `IsSealed`) and `IDataContainer<TKey, TValue>` (`TryGet`, `Get`, `ContainsKey`, `Keys`, `Values`) |
| `DataContainer<T>.GetContainer(id)` | `Engine.Registry.DataContainers.Get<Table<T>>(id)` |
| `FastConcurrentIdentifierMap<T>`, `ConcurrentIdentifierMap<T>`, `IdentifierComparer` | `ConcurrentMap<TKey, TValue>` |
| nothing | `ConcurrentList<T>`, `ConcurrentSet<T>` |

`ConcurrentList`, `ConcurrentMap` and `ConcurrentSet` are safe from any thread, read without a lock, enumerate a
snapshot shared until the next change, and raise `Changed` with what changed. `Catalog<TKey, TValue>` is a
`ConcurrentMap` that is an engine catalog.

## Catalogs

What is found by a key of its own, not by an identifier, is a catalog: `ICatalog<TKey, TValue>` (`Set`, `TryAdd`,
`TryGet`, `Get`, `Contains`, `Remove`), and `IReadOnlyCatalog<TKey, TValue>` for what can only be read.
`Stellar.Core.Data.Catalog.Catalog<TKey, TValue>` implements it, with hooks for the form of keys, the values it
refuses and what a missing key throws. A registry is only what holds objects by their identifiers.

| Before | Now |
|---|---|
| `IServiceRegistry`, `ServiceRegistry` | `IServiceCatalog`, `ServiceCatalog`: a catalog of `Type` to `object`, refusing a service that does not fulfil its contract |
| `services.Set<T>()`, `Find<T>()`, `Get<T>()`, `Remove<T>()` | the same, extensions in `Stellar.Kernel.Services` (`using Stellar.Kernel.Services;`) |
| `IContentTypeRegistry.Register(extension, type)` | `IContentTypeCatalog.Set(extension, type)` |
| `IFileSystem.RegisterDriver(driver)`, `TryGetDriver(scheme, out driver)` | `fileSystem.AddDriver(driver)`, `fileSystem.Drivers.TryGet(scheme, out driver)` |
| `IFileSystem.GetDomain(name)`, `TryGetDomain(name, out domain)`, `Domains` as a collection | `fileSystem.Domains.Get(name)`, `TryGet(name, out domain)`: a read-only catalog |

## Code outside any engine

`EngineScope.Resolve()` is what Stellar.Core and the wrappers use to find the current engine. Outside any engine it
gives the default instance of the process (`DetachedPolicy.Default`, tools and tests), or throws
(`DetachedPolicy.Throw`), which `StellarRuntime.Run` sets for a running application.

## The file system

The contracts of `Stellar.Kernel.FileSystem` are new: `FilePath`, `DomainScheme`, `IFileDriver`, `IDomain`,
`IFileSystem`, and the resources (`IResource`, `IResourceSystem`). `DomainType`, `IPath`, `IFile`, `IFileType`,
`IFileProvider`, `IFileProviderFactory`, `IResourceFactory` and `IResourceOrchestrator` are gone. They are compiled for
`net9.0` only. The module Stellar.FileSystem implements them.
