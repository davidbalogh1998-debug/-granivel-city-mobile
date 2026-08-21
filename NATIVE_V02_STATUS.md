# Granivel City — Budapest RP PC V0.2

A projekt iránya módosult: **PC-only Budapest RP**. A korábbi mobilos/fiktív 240×240 méteres városréteg kikerült; a meglévő gameplay rendszerekre valós Budapest-streaming került.

## Elkészült ebben a körben

- PC input: WASD + egér, célzás, lövés, járműbe be-/kiszállás
- Deák Ferenc tér középpontú 1:1 Web-Mercator koordinátarendszer
- OpenStreetMap alapú valós úthálózat streamelése
- OSM épület-footprintek, `height` / `building:levels` alapú magasság
- Duna/vízfelületek és parkok
- Mapzen/AWS Terrain Tiles alapú domborzat és helyi cache
- tile-alapú open-world streaming Budapest teljes területéhez
- procedurális aszfalt/fal/üveg/víz anyagok
- budapesti forgalom és gyalogos populáció
- 1–5 csillagos BRFK körözési rendszer
- járművezetés + üzemanyag
- RP állapot: készpénz, bank, éhség, szomjúság, munka
- Budapesthez kötött intro küldetés: Vörösmarty tér → Nyugati pályaudvar
- Windows x64 build helper
- OSM/ODbL attribution

## Fontos

A földrajzi Budapest, utak, domborzat és az OSM-ben szereplő épület-alaprajzok 1:1 méretarányban kezelhetők. Ez nem azonos azzal, hogy minden valódi budapesti épület, belső tér, ember és autó GTA V-szintű fotogrammetriai assetként már kész lenne; az külön AAA assetgyártási réteg.

A teljes PC Unity projekt jelenlegi csomagja: `GranivelCity_BudapestRP_PC_V0.2.zip`.
