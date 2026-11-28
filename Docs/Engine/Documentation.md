# Writing the documentation

- [README.md](https://github.com/QuantumDevTeam/StellarEngine-Core#readme)

The documentation of the engine is Markdown in the repositories, next to the code it describes. The docs site reads
it; nothing about it is edited on the site.

## Where it is read

Every repository publishes a bundle of its documentation to its GitHub Pages: its `Docs/` and the API reference made
from the XML comments of its code. The docs site shows the bundles, one project each:

| Project | Repository | What |
|---|---|---|
| `stellar-engine` | StellarEngine-Mono, the mirror | the whole engine: the guide (`Docs/Engine` of the core), the core with its API, every module and tool |
| `stellar-core` | StellarEngine-Core | the core alone: `Docs/` without `Docs/Engine`, its API |
| a module or a tool | its own repository | its `Docs/` |

The address of the docs site is set once, in `StellarDocsUri` of `Shared/Build/Stellar.Versions.props`, with the
projects of the core and of the engine next to it. The tools take them from there; Markdown never names the site.
GitHub Pages also keeps the installer and the JSON schemas at the addresses of `StellarSiteUri`.

## What a repository publishes

`prometheus.json` at the root of a repository says what goes into its bundle: the project, its title, where its
version is, the folders of articles, the API, the static files, and where links out of the bundle go.

```json
{
  "format": "prometheus-repo/1",
  "project": "stellar-filesystem",
  "title": "Stellar.FileSystem",
  "version": { "file": "Stellar.FileSystem.stellar.module", "regex": "\"Version\"\\s*:\\s*\"([^\"]+)\"" },
  "docs": [ { "path": "Docs" } ],
  "static": "docfx.json"
}
```

A module gets one from `stellar modules new`. Its `project` is a guess made from the id of the module: it must be the
name of the card of the module on the docs site.

The core does not write its description by hand. Its `prometheus.json` names `scripts/prometheus.ps1`, which the docs
workflow runs first: it writes the description of the core, or, in the mirror, that of the whole engine, with every
module and tool that has a `Docs/toc.yml`. Only this script knows the layout of the engine. To look at what it
writes:

```powershell
powershell -File scripts/prometheus.ps1 -Print
```

The format of `prometheus.json` belongs to the docs site, which documents every field.

## Links

| To | Write | Example |
|---|---|---|
| a page of the same bundle | a relative path to the `.md` file | `[Modules](Modules.md)` |
| a page of the same repository, published by another project | a relative path too; `links` of `prometheus.json` says which project | from `Docs/Architecture/Sdk.md`: `[Modules](../Engine/Modules.md)` |
| a page of another repository | `prometheus:<project>/<page>`, the page being its path in `Docs/` without `.md` | `[the guide](prometheus:stellar-engine/guide/Install)` |
| a file that is not documentation (README, LICENSE) | its address on GitHub | `https://github.com/QuantumDevTeam/StellarEngine-Core#readme` |

Relative links work on GitHub as well as on the site. A `prometheus:` link works on the site only: use it between
repositories, where nothing else can.

Never write the address of the docs site or of GitHub Pages into a page: when the site moves, the pages stay right.

## Checking it on a machine

The builder of the bundle comes from the docs site, in `docs-kit.zip`. A bundle built on a machine shows the broken
links of the pages before a push. In the core, the description is written to a file of its own first, so that
`prometheus.json` of the repository stays as it is:

```powershell
powershell -File scripts/prometheus.ps1 -Output artifacts/prometheus.json
python build_bundle.py --config artifacts/prometheus.json --out artifacts/bundle
```

A module has its description committed: `python build_bundle.py --out artifacts/bundle` in its folder.

The whole run of the workflow, the static files and the embed included, is `ci.py` of the same kit, in the folder of
the repository. It runs the `prepare` script of `prometheus.json` (PowerShell 7, or Windows PowerShell where there is
no other), which writes the description into `artifacts/prometheus/`: `prometheus.json` of the repository stays as it
is.

```powershell
$env:PROMETHEUS_SITE = 'https://notagood.org'
python ci.py --out artifacts/pages --skip-metadata
```

`--skip-metadata` takes the API made before (`docfx metadata docfx.json`); without it, `--docfx` names the `docfx`
to run.

A `warning` line names a link that does not lead anywhere. The docs workflow prints the same lines, and publishes
anyway.

## Publishing

A push to the default branch runs the docs workflow of the repository: it builds the bundle and puts it on GitHub
Pages, and the docs site picks it up within minutes (at once, when the repository has the key of the site). A
release of the mirror makes the new version of the engine appear on the site.

Settings of a repository, once: Settings → Pages → Source = GitHub Actions; the card of the project on the docs site
points at `https://quantumdevteam.github.io/<repository>/bundle/`.
