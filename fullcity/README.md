# Granivel City — Full City V1

Ez az ág a korábbi kis Safari-prototípust egy jóval nagyobb, két célplatformos open-world alappá bővíti.

## 1. Azonnal játszható web/iPhone verzió

`fullcity/game/`

Three.js alapú mobil web build. Tartalmaz:

- nagy városi úthálózatot, járdákat és útburkolati jeleket
- folyót és 3 hidat
- épületeket, kirakatokat és egyedi boltfeliratokat
- utcai lámpákat, közlekedési táblákat és növényzetet
- Kenney járműmodelleket + biztonságos fallback járműveket
- 42+ civil NPC-t, menekülő viselkedést
- forgalmat és vezethető autókat
- rendőri üldözést, 1–5 csillagos körözést
- lövést, HP-t, pénzt és küldetést
- nappal/éjszaka ciklust és éjszakai közvilágítást
- minimapot, mobil joystickot és érintőgombokat
- Poly Haven PBR aszfalt/beton streamelést mobilbarát felbontásban

A GitHub Pages workflow a mappát önálló telefonos tesztoldalként deployolja.

## 2. Natív Unity 6.3 LTS projekt

`unity/`

Unity 6000.3 LTS projekt, natív játékrendszerekkel:

- third-person player controller
- járművezetés és egyszerű forgalom
- civil/rendőr NPC AI
- körözési és rendőrségi rendszer
- lövés/harc alap
- pénz és HP
- nappal/éjszaka rendszer
- automatikus third-person kamera
- automatikus városépítő Editor tool

### Város automatikus generálása

A nagy asset artifact letöltése után a `unity/Assets/ThirdParty/CC0` mappában lesznek a modellek és textúrák. Nyisd meg Unityben, majd:

`Granivel City → Build Full City Scene`

Az Editor tool végigszkenneli a könyvtárat és automatikusan kategorizálja/elhelyezi a használható épületeket, autókat, embereket, növényeket, lámpákat, táblákat és utcabútorokat. A generált jelenet: `Assets/Scenes/GranivelCity.unity`.

## Nagy CC0 asset build

`.github/workflows/assemble-full-city.yml`

A workflow 8K/4K forrásanyagokkal építi össze a nagy könyvtárat, majd `GranivelCity-FullCity-V1` GitHub Actions artifactként adja ki. A nagy bináris csomagok szándékosan nem kerülnek közvetlenül a Git történetébe.

Források:

- Kenney City Kit Roads — CC0
- Kenney Car Kit — CC0
- Kenney City Kit Commercial — CC0
- Kenney City Kit Suburban — CC0
- Kenney City Kit Industrial — CC0
- Quaternius Ultimate Buildings Pack — CC0
- Quaternius Ultimate Animated Character Pack — CC0
- Quaternius Modular Streets Pack — CC0
- Quaternius Ultimate Nature Pack — CC0
- Poly Haven PBR/HDRI — CC0; Powered by Poly Haven

A build minden futáskor `PROVENANCE.json` fájlban rögzíti a ténylegesen letöltött forrásokat, licenceket, hibákat és méreteket.

## Fontos architektúra

A telefonos web build nem tölt le több gigabájt 8K textúrát játékindításkor: mobilon optimalizált asseteket használ/streamel. A nagy felbontású asset library a Unity forrás/artifact része. Így a forrásprojekt lehet nagy és részletes, miközben az iPhone teszt nem fogyaszt értelmetlenül több GB memóriát.

Nincs benne GTA/Rockstarból kimásolt modell, textúra, karakter, zene vagy forráskód. A külső tartalom CC0 forrásokra korlátozott.
