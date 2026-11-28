# {{ModuleId}}

{{ModuleDescription}}

A `{{ModuleLevel}}` module of [StellarEngine](prometheus:{{EngineDocsProject}}). Its documentation is on the [docs site](prometheus:{{ModuleDocsProject}}), built from the Markdown files of `Docs/`; the whole engine, with every module, is on the [site of the engine](prometheus:{{EngineDocsProject}}).

## Using it

Add `{{ModuleId}}` to `Dependencies` of your module or project descriptor, then:

```powershell
stellar modules sync
```

## Working on it

```powershell
stellar modules build {{ModuleId}} --tests
```

See [the README](../README.md) for the layout of the module.
