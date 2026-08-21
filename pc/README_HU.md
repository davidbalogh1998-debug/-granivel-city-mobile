# Granivel City — Budapest RP PC V0.2

Ez **nem új projekt**: a korábbi Granivel City játékrendszerei maradtak, a mobilos/fiktív városréteg helyére PC-s Budapest-világ került.

## Mi változott

- PC-only irányítás: WASD + egér, nincs mobil UI
- Budapest középpontja: Deák Ferenc tér
- valós földrajzi koordinátarendszer, 1 Unity méter ≈ 1 valós méter
- valós OpenStreetMap úthálózat és épület-alaprajzok, streamelve
- valós domborzat Mapzen/AWS Terrain Tiles adatokból
- Duna/vízfelületek, parkok, utak, épületek runtime generálása
- OSM épületmagasság (`height`, `building:levels`) használata, ahol elérhető
- hidak emelése a víz fölé
- procedurális PBR-szerű aszfalt/fal/üveg/víz anyagok
- Budapest-közeli forgalom és gyalogos populáció
- 1–5 csillagos BRFK körözési rendszer
- járműbe be-/kiszállás, vezetés, üzemanyag
- RP állapot: készpénz, bank, éhség, szomjúság, munka
- Budapesthez kötött bevezető küldetés (Vörösmarty tér → Nyugati)
- Windows x64 build menüpont

## Fontos a 1:1 map működéséről

A teljes Budapest nem egyetlen több tíz GB-os Unity scene-be van beégetve. A játék 1:1 méretarányban **tile-onként streameli** a környezetet a játékos körül. Ez a helyes megoldás egy egész város méretű open-world PC projektnél.

Első belépéskor internet kell az adott környék OSM + domborzati adatainak letöltéséhez. A játék ezeket a gépen cache-eli, így a már bejárt részeket nem kell újra letölteni.

## Indítás

1. Unity 6.3 LTS.
2. Nyisd meg a projektet.
3. `Assets/Scenes/Main.unity` automatikusan létrejön, ha még nincs.
4. Play.
5. Az első Budapest-cella betöltése után a karakter a Deák Ferenc térnél jelenik meg.

## PC irányítás

- WASD — gyalog / jármű
- egér — kamera
- bal klikk — lövés
- jobb klikk — célzás
- E — járműbe be/kiszállás
- Space — ugrás
- Left Shift — futás / járműben fék
- Esc — kurzor ki/be

## Windows build

`Granivel City → Build Windows x64`

Kimenet:

`Builds/Windows/GranivelCityBudapestRP.exe`

## Adatforrások

- Map data © OpenStreetMap contributors — ODbL
- Terrain Tiles / Mapzen, Linux Foundation / AWS Open Data

## Reális korlát

A földrajzi Budapest, utak, domborzat és OSM-ben szereplő épület-alaprajzok lehetnek 1:1-ek. Ettől még nem lesz minden egyes valódi budapesti homlokzat, lakásbelső, autó és ember fotogrammetriai másolata. Ezekhez több tízezer egyedi 3D asset és nagy csapatnyi gyártás kellene. A projekt viszont most már valódi Budapest-adatokra épülő PC open-world alap, nem a korábbi 240×240 méteres kockaváros.
