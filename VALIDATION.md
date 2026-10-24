# Validace testovacího portu — 24. 10. 2026

Zdroj: cybensis/SPT-VR, commit `00600ee5f496da0be07cf3686ffe7ec88c00eaa8`. Port je ve větvi `codex/port-spt-4.1.6` repozitáře Cippro/SPT-VR-Patch4.1.6. Verze pluginu i assembly je `1.3.1.416`.

## Provedené změny

- Zdrojový projekt je v `src/SPT-VR`; herní API bylo převedeno podle dodaných Managed 4.1.6 a porovnání s původními Managed 4.0.13. Při mapování byly porovnány signatury, rozložení polí a IL; původní upstream zůstává oddělený.
- FIKA projekt, companion závislost a synchronizační integrace jsou vynechané. Metadata výsledné DLL nemají žádnou assembly referenci na FIKA.
- Převedena jsou API pohybu, inputu, zbraní, interakcí, inventáře, VR nastavení a nabídek; existující VR logika není nahrazena prázdnými implementacemi.
- Nativní `OpticComponentUpdater.LateUpdate` běží. VR upraví far clip v postfixu a synchronizuje zoom přes `ScopeZoomHandler` a `SightComponent.ScopeZoomValue`.
- Nativní reticle update běží před VR úpravou pozice/rotace. Ostřejší retikl používá oddělenou lens texturu po PIP renderu, s obnovou nativní textury při vypnutí. PIP kamera nemění svůj render target kvůli VR override; obecný SSAA override ji výslovně vynechává.
- Inicializace pojišťovacího okna používá nativní 4.1.6 `Show` a pouze postfix pro VR umístění.
- Build používá přesné verze balíčků a zamčené závislosti. Build/test/package skripty jsou v repozitáři.
- Oprava 1.3.1.416 po prvním testu ve hře inicializuje nativní Unity JSON převodníky před XR a ověří `UnparsedData`/počty předmětů i textový formát `Side` požadavku na raid. Při neúplných existujících defaults doplní pouze dva potřebné převodníky a zachová zbytek nastavení i jeho provider. Plugin se načítá po SPT.Core a SPT.Singleplayer. Důkazy a meze závěru jsou v `docs/runtime-report-2026-10-24.md`.

## Výsledky

Příkaz `bash scripts/build.sh` doběhl s návratovým kódem 0. Proběhla obnova závislostí v locked mode, Release kompilace, metadata test, skutečný regresní test JSON a vytvoření balíčku. Po dokončení návodu byl ZIP znovu zabalen a ověřen pomocí `scripts/package.py`.

- Release: **0 chyb kompilace**. Varování zděděného kódu zůstávají viditelná; nejsou plošně potlačena.
- Kompatibilita: **293 cílů Harmony, 0 selhání, 0 dynamických/neověřených cílů**. Kontrolovány byly metody, overloady, názvy/typy parametrů, instance/výsledky a injektovaná pole.
- Další kontrakty: shodná verze BepInPlugin/assembly, žádná FIKA reference, PIP/reticle postfixy, zápis trvalého native zoomu a výjimka pro nativní optiku u přepisování camera targetu.
- Metadata navíc ověřují závislosti na SPT, volání JSON bootstrapu před XR a existenci nativního Unity JSON inicializátoru.
- JSON regrese: **92 úspěšných kontrol** nad původními nepřepsanými Managed 4.1.6. Test nejprve reprodukuje chybu převodu `StackObjectsCount` a odmítnutí číselné `Side` serverovým modelem. Pak spustí skutečný nativní Unity inicializátor a zdrojový bootstrap použitý v DLL. Ověří počty 1/30/60/500000/9999999, vnořenou durability, boolean stav, SPT preset ID, zpětnou serializaci a přijetí PMC/Scav požadavků. Ověřuje i existující custom převodníky, zachování settings, idempotenci a provider vytvářející nové settings při každém volání. Nejde o spuštění Unity scény ani serveru.
- ZIP: CRC v pořádku, vložená DLL byte-for-byte odpovídá Release, 45 souborů, žádné FIKA entry ani `Assembly-CSharp` reference.
- Původní nezměněný upstream byl dříve ověřen proti Managed 4.0.13: 0 chyb, 179 varování.
- Online NuGet vulnerability audit nebyl dostupný kvůli omezenému síťovému přístupu (NU1900). Obnova zamčených závislostí a sestavení uspěly; TLS/signature/hash ověřování nebylo vypnuto.

Runtime assety a SteamVR/XR soubory balíčku pocházejí z dodaného instalačního ZIPu 1.3.0. Asset bundle v upstream source checkoutu se od tohoto release liší, proto není použit jako náhrada kompletních release assetů.

## Artefakty

- `artifacts/SPT-VR.dll`: SHA256 `77f4b275f9cd0d5fd8e52c008d0eff344fdb98c4d3db2d8911d73e2c0bbc1891`
- `artifacts/SPT-VR-4.1.6-test.zip`: SHA256 `944b9ba0c66b5fe31697b96f698298680f7aac6c2795a3fab08a0dfce2652060`
- `artifacts/compatibility.json`: úplný přehled ověřených patchů.
- `artifacts/json-regression.log`: výsledek skutečného testu JSON. Všechny výše uvedené výstupy jsou také v `releases/4.1.6-test`.

Windows/SPT/SteamVR ani skutečný headset zde spuštěné nebyly. Uživatel ověřil funkční menu předchozí verze 1.3.0.416 a dodal logy problémů se stacky a načítáním raidu. Nová 1.3.1.416 čeká na opakovaný test ve hře. Ověření metadat a serializeru není důkaz funkčnosti všech herních cest. Návod a ruční testy jsou v `TESTING.md`; toto je testovací sestavení, nikoli potvrzení herní kompatibility.
