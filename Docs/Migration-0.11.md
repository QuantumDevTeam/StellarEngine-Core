# Migrating to 0.11

- [README.md](https://github.com/QuantumDevTeam/StellarEngine-Core#readme)

What changed for the code that uses the core, from 0.10. Nothing of the core breaks: the orchestra takes the second
number because the logging module enters it.

| Component | 0.10 | 0.11 | Why |
|---|---|---|---|
| Orchester | 0.10.0 | 0.11.0 | The logging module, and the file system published by its module |
| Kernel | 0.10.0 | 0.10.1 | Compatible: `Folders`; a color of `LoggerFormat.Default` |
| FileSystem | 0.9.0 | 0.11.0 | It publishes itself and mounts the folders of the engine |
| Runtime | 0.10.0 | 0.10.1 | Compatible: settings of the command line reach modules |
| Stellar.Tools | 0.3.2 | 0.3.3 | Compatible: marks; a module is packed before its tests |
| `stellar` (CLI) | 0.2.3 | 0.2.4 | Compatible: `stellar marks` |
| Stellar.Sdk | 0.4.1 | 0.4.2 | Compatible: compiled resources go into `Resources` |
| Core, P/Invoke exports | 0.10.0, 3.1 | the same | Nothing changed |

Build the engine again and install it (`scripts/build.ps1 -Configuration Release -Install`). `global.json` pins an exact version of
Stellar.Sdk, so change `0.4.1` to `0.4.2` in that of every solution:

```json
{ "msbuild-sdks": { "Stellar.Sdk": "0.4.2" } }
```

## Resources

Compiled resources lie in `Resources` next to the application, not `resources`: only a system that tells case apart sees
the difference.

## Settings

`RuntimeConfiguration.Settings` holds the settings of the project and those of the command line
(`--stellar:Name=Value`), which win; names are in any case. Before, a module saw those of the project only.

## File system

`Stellar.FileSystem` publishes `IFileSystem` by itself and mounts the folders of the engine. Code names them through
`Folders`, so it needs neither `Environment.GetFolderPath` nor the names of the domains:

| `Folders` | Domain | Where |
|---|---|---|
| `App` | `app://` | the folder of the application, read-only |
| `Assets` | `assets://` | `Assets` of it, read-only; the setting `AssetsFolder` moves it |
| `Resources` | `resources://` | `Resources` of it, read-only; the setting `ResourcesFolder` moves it |
| `AppData.Roaming` | `user://` | `<AppData>/<company>/<project>` |
| `AppData.Local` | `local://` | the same in the local data of the user |
| `AppData.LocalLow` | `locallow://` | the same in the low local data |
| `Temp` | `temp://` | the same in the temporary files |

The company is `Starlet` when the project names none; a project without a name has none of the last four. They are
created by the first write. Full paths of the machine, to mount domains of your own: `Stellar.FileSystem.SystemFolders`.

A cancelled or failed atomic write leaves the file as it was.

## Logging

When the module that published the logger stops, the runtime goes back to the error output: what fails after it is
told there instead of being lost.

## Marks

Marks in the comments of the code, checked by git and by the workflows: [Marks](https://quantumdevteam.github.io/StellarEngine-Mono/guide/Marks.html).
