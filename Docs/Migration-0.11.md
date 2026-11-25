# Migrating to 0.11

- [README.md](https://github.com/QuantumDevTeam/StellarEngine-Core#readme)

What changed for the code that uses the core, from 0.10. Nothing of the core breaks: the orchestra takes the second
number because the logging module enters it.

| Component | 0.10 | 0.11 | Why |
|---|---|---|---|
| Orchester | 0.10.0 | 0.11.0 | The logging module, and the file system published by its module |
| Kernel | 0.10.0 | 0.10.1 | Compatible: a color of `LoggerFormat.Default` |
| Runtime | 0.10.0 | 0.10.1 | Compatible: settings of the command line reach modules |
| Stellar.Tools | 0.3.2 | 0.3.3 | Compatible: marks; a module is packed before its tests |
| `stellar` (CLI) | 0.2.3 | 0.2.4 | Compatible: `stellar marks` |
| Core, Stellar.Sdk, P/Invoke exports | 0.10.0, 0.4.1, 3.1 | the same | Nothing changed |

Build the engine again and install it (`scripts/build.ps1 -Configuration Release -Install`). `global.json` stays.

## Settings

`RuntimeConfiguration.Settings` holds the settings of the project and those of the command line
(`--stellar:Name=Value`), which win; names are in any case. Before, a module saw those of the project only.

## Logging

When the module that published the logger stops, the runtime goes back to the error output: what fails after it is
told there instead of being lost.

## Marks

Marks in the comments of the code, checked by git and by the workflows: [Marks](https://quantumdevteam.github.io/StellarEngine-Mono/guide/Marks.html).
