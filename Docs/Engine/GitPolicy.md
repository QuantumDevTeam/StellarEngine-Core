# StellarEngine — Git & Collaboration Policy

- [README.md](https://github.com/QuantumDevTeam/StellarEngine-Core#readme)

## 1. Purpose of This Document

This document defines **principles for working with Git**, not a rigid set of rules.

Its goals are to:

* keep the project history readable
* reduce chaos as the team grows
* ensure transparency of development decisions

---

## 2. Commits

### 2.1 What a Commit Is

A commit represents **one logically complete change**.

A good commit answers the question:

> *What changed and why?*

---

### 2.2 Commit Frequency

The following is allowed and encouraged:

* frequent commits
* small commits
* focused commits

Committing every 5–15 minutes is **normal practice**, as long as the change is logically complete.

---

### 2.3 Commit Message Format

A simple and explicit format is used:

```
[ModuleShortName] <verb> <what changed>
```

Examples:

```
[GW] add graphics pipeline abstraction
[CM] refactor core module initialization order
[RPM] fix memory leak in resource manager
```

Version numbers **must not** appear in commit messages.

---

## 3. Branches

| Branch | What it is | How code gets in |
|---|---|---|
| `master` | what is released | a pull request from `develop` |
| `develop` | active development and integration | a pull request from a work branch |
| `feature/*` | one task | commits |
| `experiment/*` | temporary or high-risk research work | commits |

`master` and `develop` take pull requests only: nobody pushes there, the owner included. A work branch may be rewritten
or abandoned; once merged it is deleted.

---

## 4. The way of a change

The same in the core, in a module and in a tool. Each is a repository of its own: run the commands in its folder.

### 4.1 Every day

```powershell
# 1. A branch, from a fresh develop
git switch develop
git pull --ff-only
git switch -c feature/<task>

# 2. Commits, small ones. The hooks check the marks
git add .
git commit -m "[TAG] <verb> <what changed>"

# 3. A pull request into develop
git push -u origin HEAD
gh pr create -B develop --fill

# 4. Merged once everything is green
gh pr checks --watch
gh pr merge --merge --delete-branch

# 5. Back to develop
git switch develop
git pull --ff-only
```

A pull request into `develop` waits for the marks and the CLA, not for the tests: they run by themselves only for
`master`. Test on your machine (`scripts/build.ps1`, `stellar modules build <module> --tests`), and to test a branch on
the server start it by hand, for example after several merges into `develop`: *Actions → Tests → Run workflow*, or

```powershell
gh workflow run tests.yml --ref develop
```

The button appears once the workflow is in `master`.

More commits on the same task before the merge: `git commit` and `git push`, the pull request takes them by itself.

Always a **merge commit**, never a squash or a rebase: those make other commits out of the same changes, and `develop`
and `master` drift apart.

### 4.2 A change of several repositories

The one the others use goes first: the core, then the modules. At a release it is the other way round (4.3).

### 4.3 A release

A snapshot of the mirror takes `master` of every component, so the core goes last:

1. each module that changed: a pull request from `develop` into `master`, merged;
2. the core: the new versions in `Shared/Build/Stellar.Versions.props` through a usual pull request into `develop`;
3. the core: a pull request from `develop` into `master`. **Its merge releases the version**;
4. the mirror: Run workflow of *Mirror* in StellarEngine-Mono, unless the release started it.

```powershell
gh pr create -B master -H develop -t "<name> <version>" -b "<what it is, in a sentence>"
gh pr checks develop --watch
gh pr merge develop --merge
```

`develop` is not deleted, and `master` is never merged back by hand.

### 4.4 When something is in the way

| What you see | What to do |
|---|---|
| the commit is stopped by a mark | `stellar marks list`, look at the place, take the mark out or leave the file out of the commit |
| `push` says *protected branch* | you are on `develop` or `master`: `git switch -c feature/<task>` and push that; the commits come with you |
| the pull request says *out of date* or *conflicts* | `git fetch origin`, `git merge origin/develop` in the work branch, solve, commit, push |
| a check is red | open it, fix in the same branch, push |
| `pull --ff-only` refuses | there are commits on `develop` that are not on the server: `git switch -c feature/<task>`, then `git branch -f develop origin/develop` |

Before merging into `master` the code must build, changes must be logically complete, and the history must remain
readable. Versions (Git tags) are **created only on `master`**.

---
## 5. Git Tags

### 5.1 Version Tags

A version tag:

* follows a version format (e.g. `v0.x.y`)
* represents a product state
* is created intentionally

One commit corresponds to **one product version**.

---

### 5.2 Service / Milestone Tags

Service tags are used **sparingly** and indicate architectural or project-level events.

Examples:

* introduction of a new major subsystem
* architectural turning points
* start or completion of a large development phase

Service tags:

* are not versions
* do not replace commits
* are not used to classify modules

---

## 6. Team Scaling

As the team grows:

* commit rules remain unchanged
* versions are created centrally
* architectural decisions are documented, not inferred from commits

In case of conflict:

> **History readability takes priority over speed.**

---

## 7. Responsibility Principle

Each contributor:

* is responsible for the clarity of their commits
* is not required to perfectly predict the future
* is required not to create chaos in the present

---

Also see:
- [Versioning.md](Versioning.md)
- [CONTRIBUTING.md](https://github.com/QuantumDevTeam/StellarEngine-Core/blob/master/CONTRIBUTING.md)
- [README.md](https://github.com/QuantumDevTeam/StellarEngine-Core#readme)
