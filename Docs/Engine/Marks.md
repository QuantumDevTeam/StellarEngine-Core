# Marks

- [README.md](https://github.com/QuantumDevTeam/StellarEngine-Core#readme)

A mark is metadata in a comment of the code: `#` and a name in angle brackets, capital letters, digits, `-` and `_`.
`MVP` as a mark in C# is a comment `// #` followed by `<MVP>`. It is found in comments only, in C#, C++, shaders,
PowerShell, shell, YAML, TOML, XML, MSBuild and Markdown files.

## Kinds

| File | Whose | Committed |
|---|---|---|
| `Shared/Marks/marks.json` | of the engine, the same for everyone | yes |
| `config/marks.json` | of one machine; adds kinds and replaces those of the engine | never, git ignores `config/` |

```json
{ "Marks": { "WIP": { "Level": "Release", "Description": "Not finished: it cannot be released" } } }
```

| Level | How far code with the mark goes |
|---|---|
| `Info` | anywhere |
| `Release` | into commits and `develop`, not into `master`, a release or the mirror |
| `Commit` | nowhere: not even into a commit |

A mark of a kind that is in neither file is stopped everywhere: a typing error, or a kind of another machine.

## Checking

```powershell
stellar marks install --all     # git checks every commit, merge and push of the core, the modules and the tools
stellar marks list --all        # where the marks are
stellar marks check             # the files of this repository
stellar marks check --staged    # what the next commit changes
stellar marks check --all --stage release
stellar marks uninstall --all
```

| Where | What it checks |
|---|---|
| the hooks of git | what is committed, merged and pushed |
| `.github/workflows/marks.yml` | every push to `develop` and `master` and every pull request; `master` as a release. A module calls the workflow of the core, `stellar modules new` writes it. Required by the rulesets |
| `.github/workflows/release.yml` | before a release is tagged |
| `.github/workflows/mirror.yml` | the core and every component, before a snapshot |

The hooks stop the commit when the `stellar` command is missing or older than the marks. They can be skipped with
`--no-verify`; the workflows cannot. A hook somebody else wrote is never replaced.
