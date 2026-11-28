# Questions

- [README.md](https://github.com/QuantumDevTeam/StellarEngine-Core#readme)

Short answers to what comes up first. The pages they point at say it in full.

## Installing and building

**Does `build.ps1 -Install` remove the old installation first?**
No. `-Uninstall` is a mode of its own: it does its work and stops, and it is refused together with `-Install` or
`-Pack`. An install only replaces the `stellar` command with the one it has just packed. Everything the installation
holds stays: `stellar install` never overwrites a file. See [the layout](Repository.md).

**Why does `-Install` pack when `-Pack` is not given?**
An installation needs its local feed, `packages/`, so `-Install` implies `-Pack`. Packing in turn needs the native
core: the package of Stellar.Core carries it with its wrapper, so `-Pack` cannot be combined with `-SkipNative`.

**Does `build.ps1` check the toolchain?**
The installer does ([Installing](Install.md)): it checks Git, the .NET SDK and Visual Studio first and prints what is
missing. `build.ps1` assumes them and stops at the first tool that is not there.

**Can the engine be built on Linux?**
Not yet. `build.ps1` already picks the right wrapper there (the P/Invoke one) and `-SkipNative` builds and tests the
managed part. The native projects are Visual Studio projects, though, and games and modules target Windows, so
packing and installing need Windows.

**What are the `Cli` and `Managed` wrappers?**
Two wrappers of the native core with the same classes. `Cli` is C++/CLI: Windows only, and only where Visual Studio
has its C++/CLI component. `Managed` is C# over the P/Invoke exports of the native core: it works everywhere and with
Native AOT. `StellarNativeWrapper` chooses; by default the SDK takes `Cli` where it was built. See
[Native core](../Architecture/NativeAbi.md).

## Finding the installation

**How does a game find the engine?**
Through `$(StellarRoot)`, which Stellar.Sdk sets: the `StellarRoot` property, the `STELLAR_ROOT` variable, a folder
above the project with `manifests/engine.manifest.json`, or the registry of the user, `~/.stellar`, for the orchestra
version of its Stellar.Sdk. Its packages come from `packages/` of that installation, through its `NuGet.config`. See
[the layout](Repository.md).

**What is in `~/.stellar`?**
Where each orchestra version is installed: `installations.json`, and `installations.props` made from it for MSBuild.
One installation per version, and one version per root. What an orchestra holds is in its own `manifests/`.

## Modules

**Why is `Modules` empty in `manifests/engine.manifest.json`?**
Modules are found by themselves: a folder of `modules/` with a `<Id>.stellar.module` file is a module. The manifest
only holds what this installation does differently: a module that lives elsewhere (`Path`), one turned off
(`Enabled`), another version or way of linking it (`Link`). `stellar modules add`, `enable`, `disable` and `reset`
write it. The full list of modules is the generated catalog in `manifests/.generated/`. See
[Engine manifest](../Formats/EngineManifest.md).

**What does `Link` of a module mean?**
How projects reference it. `Default`: as the project decides (`StellarPrecompiled`), sources in Debug and packages
otherwise. `Sources`: always the sources, through a project reference. `Package`: always the package of the local
feed. A module the manifest declares with a `Version` and no folder is always a package.

**What writes the catalog of modules, and when?**
`stellar modules sync`, and `stellar install` through it: it finds the modules, applies the manifest, checks the rule
of the levels and writes `stellar.modules.props` for MSBuild and `stellar.modules.json` for tools, only when they
change. A module project learns its level, version and dependencies from it. See [Modules](Modules.md).

**Why do I not list `Stellar.Kernel` and `Stellar.Core` as dependencies?**
Every project gets them implicitly, and a game gets `Stellar.Runtime` too, which its launcher calls. A dependency on
them in a descriptor is dropped with a warning. A project turns one off with `StellarImplicitKernelReference`,
`StellarImplicitCoreReference` or `StellarImplicitRuntimeReference` set to `false`; empty means on. See
[SDK properties](../Reference/Sdk.Properties.md).

## The build of a game

**What is in `bin/` of a game?**
`<Game>.exe` (the apphost of .NET) and `<Game>.Launcher.dll`, which calls `StellarRuntime.Run`; the game itself,
`<Game>.dll`, a library; `<Game>.stellar.runtime.config.json`, its modules in load order and its settings; the engine
(`Stellar.Kernel`, `Stellar.Core`, `Stellar.Runtime`) with the native core and its wrapper; compiled resources in
`Resources/`. See [Runtime](../Architecture/Runtime.md).

**Sources or packages?**
A game takes the engine as packages. With `"Engine": "Sources"` in its `.stellar.solution` it takes the sources in
Debug, and packages in every other configuration. On a machine without the sources of the core it takes packages
anyway, with a warning.
