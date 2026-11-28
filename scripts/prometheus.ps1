<#
.SYNOPSIS
    Writes prometheus.json: what the docs site publishes from this tree.

.DESCRIPTION
    The docs workflow of every repository builds a bundle of its documentation for the docs site, as prometheus.json
    at the root says (Docs/Engine/Documentation.md). The core commits a small prometheus.json that names this script
    as "prepare": the workflow runs it first, and it writes the full description in place.

    It knows the layout of the engine, so that the workflow does not:
      the core      Docs/ without Docs/Engine, the API of docfx.json; links into Docs/Engine go to the guide of the
                    engine
      the mirror    (a .orchestra file at the root) the whole engine: the guide (Docs/Engine), the core (Docs/ and
                    its API), and the Docs/ of every module and tool that has a toc.yml

    The projects and the version come from Shared/Build/Stellar.Versions.props, the only place that names them.

.PARAMETER Print
    Writes the description to the output instead of prometheus.json, to look at it on a machine.

.PARAMETER Output
    Writes the description to this file instead of prometheus.json, to build a bundle on a machine without changing
    the file of the repository. The docs workflow names the file in PROMETHEUS_CONFIG_OUT, which -Output overrides.
#>
param(
    [switch]$Print,
    [string]$Output = ''
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent

$versions = [System.IO.File]::ReadAllText((Join-Path $root 'Shared/Build/Stellar.Versions.props'))
function Get-Property([string]$Name) {
    $value = [regex]::Match($versions, "<$Name>([^<]+)</$Name>").Groups[1].Value
    if (-not $value) { throw "$Name is missing from Shared/Build/Stellar.Versions.props" }
    return $value
}

$coreProject = Get-Property 'StellarDocsProject'
$engineProject = Get-Property 'StellarEngineDocsProject'
$version = [ordered]@{ file = 'Shared/Build/Stellar.Versions.props'; xml = 'StellarOrchesterVersion' }
$api = @([ordered]@{ docfx = 'docfx.json'; prefix = 'api' })

if (Test-Path (Join-Path $root '.orchestra')) {
    # The mirror: the whole engine in one bundle
    $docs = [System.Collections.Generic.List[object]]::new()
    $docs.Add([ordered]@{ path = 'Docs/Engine'; prefix = 'guide'; title = 'Guide' })
    $docs.Add([ordered]@{ path = 'Docs'; prefix = 'core'; title = 'Core'; exclude = @('Engine/**') })

    foreach ($area in 'modules', 'tools') {
        $directory = Join-Path $root $area
        if (-not (Test-Path $directory)) { continue }

        foreach ($component in (Get-ChildItem $directory -Directory | Sort-Object Name)) {
            # Hidden components (modules/_legacy) and the compilers of the installer are not components to publish
            if ($component.Name.StartsWith('_') -or $component.Name.StartsWith('.')) { continue }
            if (-not (Test-Path (Join-Path $component.FullName 'Docs/toc.yml'))) { continue }

            $docs.Add([ordered]@{ path = "$area/$($component.Name)/Docs"; prefix = "$area/$($component.Name)"; title = $component.Name })
        }
    }

    $description = [ordered]@{
        format = 'prometheus-repo/1'
        project = $engineProject
        title = 'StellarEngine'
        version = $version
        docs = $docs
        api = @([ordered]@{ docfx = 'docfx.json'; prefix = 'core/api' })
        static = 'docfx.json'
    }
}
else {
    # The core: its own pages; the guide of the whole engine lives here too, but is published with the engine
    $description = [ordered]@{
        format = 'prometheus-repo/1'
        project = $coreProject
        title = 'StellarEngine-Core'
        version = $version
        docs = @([ordered]@{ path = 'Docs'; prefix = ''; exclude = @('Engine/**') })
        api = $api
        static = 'docfx.json'
        links = [ordered]@{ 'Docs/Engine/' = "$engineProject/guide/" }
    }
}

$json = $description | ConvertTo-Json -Depth 8
if ($Print) {
    $json
    return
}

# The docs workflow says where the description goes, so that prometheus.json of the repository stays as it is
if (-not $Output) { $Output = $env:PROMETHEUS_CONFIG_OUT }
if (-not $Output) { $Output = Join-Path $root 'prometheus.json' }
[System.IO.File]::WriteAllText([System.IO.Path]::GetFullPath($Output), $json + "`n", [System.Text.UTF8Encoding]::new($false))
Write-Host "Wrote $Output for $($description.project)"
