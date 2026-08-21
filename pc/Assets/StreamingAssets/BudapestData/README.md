# BudapestData

This directory is the optional **offline data pack** for Granivel City — Budapest RP.

The game first looks here, then falls back to its writable runtime cache, then to online OSM/terrain services.

Expected layout:

```text
BudapestData/
  manifest.json
  terrain/14/<x>_<y>.png
  osm/14/<x>_<y>.xml
  pbr/...
  models/...
```

For very large installations, do not copy tens of gigabytes into the Git repository. Set the environment variable `GRANIVEL_BUDAPEST_DATA` to an external data folder instead.

Example on Windows PowerShell:

```powershell
$env:GRANIVEL_BUDAPEST_DATA = "D:\\GranivelCityData\\BudapestData"
```

Then launch the game from the same terminal or make the variable permanent in Windows.
