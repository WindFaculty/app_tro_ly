param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\\..")).Path
)

$ErrorActionPreference = "Stop"

function Assert-PathExists {
    param(
        [string]$RelativePath,
        [System.Collections.Generic.List[string]]$Failures
    )

    $target = Join-Path $RepoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $target)) {
        $Failures.Add("Missing required path: $RelativePath")
    }
}

$failures = [System.Collections.Generic.List[string]]::new()

$requiredPhasePaths = @(
    "ai-dev-system/workbench/README.md",
    "ai-dev-system/workbench/naming-convention.md",
    "ai-dev-system/workbench/inventory/current-workbench-sources.json",
    "ai-dev-system/asset-pipeline/tool-catalog.json",
    "bleder",
    "Meshy_AI_Azure_Sakura_Kimono_0326010047_texture_fbx",
    "tools",
    "tools/reports",
    "tools/renders"
)

foreach ($path in $requiredPhasePaths) {
    Assert-PathExists -RelativePath $path -Failures $failures
}

$inventoryPath = Join-Path $RepoRoot "ai-dev-system/workbench/inventory/current-workbench-sources.json"
$catalogPath = Join-Path $RepoRoot "ai-dev-system/asset-pipeline/tool-catalog.json"

$inventory = Get-Content -Raw -Encoding utf8 $inventoryPath | ConvertFrom-Json
$catalog = Get-Content -Raw -Encoding utf8 $catalogPath | ConvertFrom-Json

foreach ($sourceRoot in $inventory.source_roots) {
    Assert-PathExists -RelativePath $sourceRoot.path -Failures $failures
    foreach ($item in $sourceRoot.notable_items) {
        $joined = Join-Path $sourceRoot.path $item
        Assert-PathExists -RelativePath $joined -Failures $failures
    }
}

foreach ($category in $catalog.categories) {
    foreach ($path in $category.paths) {
        Assert-PathExists -RelativePath $path -Failures $failures
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Output "Phase 6 structure validation passed."
Write-Output "Validated inventory roots: $($inventory.source_roots.Count)"
Write-Output "Validated tool catalog categories: $($catalog.categories.Count)"
