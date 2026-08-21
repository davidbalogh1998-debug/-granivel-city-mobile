$ErrorActionPreference = "Stop"
$repo = Split-Path -Parent $PSScriptRoot
$out = Join-Path $repo "Assets\StreamingAssets\BudapestData"
Write-Host "Granivel City — Budapest offline data builder"
Write-Host "Output: $out"
python (Join-Path $PSScriptRoot "prefetch_budapest.py") --all --output $out --zoom 14 --workers 8 --osm-delay 2.0
