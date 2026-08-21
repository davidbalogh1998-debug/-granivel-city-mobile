# Budapest data pipeline

The PC project supports a separate, potentially very large Budapest data pack instead of putting generated gigabytes into normal Git history.

## One-command Windows build

From PowerShell:

```powershell
cd Tools
./build_budapest_data.ps1
```

The builder is resumable. Existing valid tiles are skipped.

At zoom 14, the configured Budapest administrative/padded extent contains **520 map tiles**. Terrain is fetched from the public Mapzen/AWS Terrarium dataset. Street/building/water/park geometry is fetched from OpenStreetMap through Overpass and cached as XML in the exact format the Unity runtime already understands.

For a large SSD installation you can put the dataset outside the Unity project and set:

```powershell
$env:GRANIVEL_BUDAPEST_DATA = "D:\GranivelCityData\BudapestData"
```

The game resolves data in this order:

1. external `GRANIVEL_BUDAPEST_DATA`
2. packaged `Assets/StreamingAssets/BudapestData`
3. the game's writable runtime cache
4. online fallback

## Licensing / attribution

- Map geometry: OpenStreetMap contributors, ODbL. Keep `© OpenStreetMap contributors` visible in credits/about.
- Elevation: Mapzen Terrain Tiles / AWS public dataset.
- Do **not** add ripped GTA V/Rockstar models, textures, audio, maps or code.

## High-end art layer

The geometry cache gives correct geography, not AAA art by itself. Realistic PBR road/building/prop assets should be maintained as a separate art library. CC0 sources such as Poly Haven can be used legally; the runtime/world systems are intentionally separated from the visual asset layer so models/materials can be upgraded without rebuilding RP logic.
