$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $PSCommandPath
$aiDevSystemRoot = [System.IO.Path]::GetFullPath((Join-Path $scriptRoot "..\.."))
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $aiDevSystemRoot ".."))
$phase6Validator = Join-Path $aiDevSystemRoot "asset-pipeline\validate-phase6-structure.ps1"

function Assert-ExistingPath {
    param(
        [string]$Path,
        [string]$Description
    )

    if (-not (Test-Path $Path)) {
        throw ($Description + " not found: " + $Path)
    }
}

function Read-JsonFile {
    param([string]$Path)

    return Get-Content -Raw $Path | ConvertFrom-Json
}

& powershell -NoProfile -ExecutionPolicy Bypass -File $phase6Validator
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$requiredPaths = @(
    @{
        Path = (Join-Path $aiDevSystemRoot "domain\customization\contracts\slot-taxonomy.json")
        Description = "Slot taxonomy"
    },
    @{
        Path = (Join-Path $aiDevSystemRoot "domain\customization\contracts\item-manifest.schema.json")
        Description = "Item manifest schema"
    },
    @{
        Path = (Join-Path $aiDevSystemRoot "domain\customization\contracts\validator-rules.md")
        Description = "Validator rules doc"
    },
    @{
        Path = (Join-Path $aiDevSystemRoot "domain\customization\sample-data\current-item-catalog.json")
        Description = "Current item catalog snapshot"
    },
    @{
        Path = (Join-Path $repoRoot "apps\unity-runtime\Assets\AvatarSystem\AvatarProduction\Editor\Validators\AvatarValidator.cs")
        Description = "Unity avatar validator"
    },
    @{
        Path = (Join-Path $repoRoot "tools\blender_validate_split_avatar.py")
        Description = "Root Blender split-avatar validator"
    }
)

foreach ($entry in $requiredPaths) {
    Assert-ExistingPath -Path $entry.Path -Description $entry.Description
}

$slotTaxonomy = Read-JsonFile -Path (Join-Path $aiDevSystemRoot "domain\customization\contracts\slot-taxonomy.json")
$itemCatalog = Read-JsonFile -Path (Join-Path $aiDevSystemRoot "domain\customization\sample-data\current-item-catalog.json")
$toolCatalog = Read-JsonFile -Path (Join-Path $aiDevSystemRoot "asset-pipeline\tool-catalog.json")

if ($slotTaxonomy.slots.Count -lt 1) {
    throw "Slot taxonomy must contain at least one current slot."
}

if ($itemCatalog.items.Count -lt 1) {
    throw "Current item catalog must contain at least one item snapshot."
}

if ($toolCatalog.categories.Count -lt 1) {
    throw "Asset-pipeline tool catalog must contain at least one category."
}

Write-Host "Avatar pipeline validation passed."
Write-Host ("Validated slots: " + $slotTaxonomy.slots.Count)
Write-Host ("Validated sample items: " + $itemCatalog.items.Count)
Write-Host ("Validated tool catalog categories: " + $toolCatalog.categories.Count)
