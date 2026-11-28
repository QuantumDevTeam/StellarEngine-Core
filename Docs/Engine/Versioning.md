# StellarEngine — Versioning Policy

- [README.md](https://github.com/QuantumDevTeam/StellarEngine-Core#readme)

## 1. General Concept

StellarEngine uses **intentional and minimalistic versioning**.

A version is **the name of an engine state** that can be:

* referenced
* built
* demonstrated
* used as a stable point of comparison

Versions are **not equal to commits** and are **never created automatically**: a version is chosen by hand, as
`StellarOrchesterVersion` in `Shared/Build/Stellar.Versions.props`. The release workflows only carry the decision
out, tagging it in the core and in the mirror (see [Releases](Repository.md#releases)).

---

## 2. Where Versions Exist

A version exists **only as a Git tag**.

* Commits do not contain version numbers
* Branches do not contain version numbers
* Version = tag name

---

## 3. Current Project Stage (Pre-release)

Until the first stable release (`v1.0.0`), StellarEngine is in an active **development stage**.

The following scheme is used:

```
v0.X.Y
```

Where:

* `0` — the project does not provide a stable public API
* `X` — breaking changes (backward compatibility may break)
* `Y` — accumulated changes within the same development stage

> During the `0.x` period, any API changes are allowed without compatibility guarantees.

---

## 4. When a New Version Is Created

A new version is created when:

* a **logically complete set of changes** has accumulated
* the current state can be described in a single sentence
* the state is meaningful as a reference point

A version is **not created**:

* for every commit
* on a timer
* "just because it has been a while"

---

## 5. Pre-release Versions

For unstable, testing, or transitional states, pre-release modifiers are used:

```
v0.2.0-alpha
v0.2.0-beta
v0.2.0-rc.1
```

A pre-release version:

* is considered a distinct version
* is not equivalent to the final version without a suffix

---

## 6. Transition to v1.0.0

Version `v1.0.0` indicates that:

* a baseline public API has been formed
* core subsystems are stabilized
* the project is ready for external usage

After `v1.0.0`, the project switches to classic Semantic Versioning:

```
MAJOR.MINOR.PATCH
```

---

## 7. Backward Compatibility

Before `v1.0.0`:

* backward compatibility is **not guaranteed**

After `v1.0.0`:

* MAJOR — breaking changes
* MINOR — API extensions
* PATCH — bug fixes and internal improvements

---

## 8. Versions of Components

Every component (`Shared/Build/Stellar.Versions.props`, `*.stellar.module`) has a version of its own. What counts is the
version of the last release, the one on `master`:

| Since the last release | Version |
|---|---|
| nothing that matters to others: formatting, tests, documentation | stays |
| the first compatible change | `Y` is raised, once: `0.10.0` → `0.10.1` |
| more compatible changes | stays: they go into the version that is raised already |
| a breaking change | `X` is raised, once, instead of `Y`: `0.10.1` → `0.11.0` |
| more changes of any kind after it | stays |

A change is breaking for a component when what uses it does not work with the new one as it is. A component that did not
change is raised only if it does not work with the old version of what it depends on. The orchestra takes `X` when any of
its components does, or when a module enters it.

Before raising, compare:

```powershell
git show origin/master:Shared/Build/Stellar.Versions.props
```

---

Also see:
- [GitPolicy.md](GitPolicy.md)
- [CONTRIBUTING.md](https://github.com/QuantumDevTeam/StellarEngine-Core/blob/master/CONTRIBUTING.md)
- [README.md](https://github.com/QuantumDevTeam/StellarEngine-Core#readme)
