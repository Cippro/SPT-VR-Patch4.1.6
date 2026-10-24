# Validace testovacího portu — 24. 10. 2026

Zdroj: cybensis/SPT-VR, commit `00600ee5f496da0be07cf3686ffe7ec88c00eaa8`. Port je ve větvi `codex/port-spt-4.1.6` repozitáře Cippro/SPT-VR-Patch4.1.6. Verze pluginu i assembly je `1.3.2.416`.

## Provedené změny

- Zdrojový projekt je v `src/SPT-VR`; herní API bylo převedeno podle dodaných Managed 4.1.6 a porovnání s původními Managed 4.0.13. Při mapování byly porovnány signatury, rozložení polí a IL; původní upstream zůstává oddělený.
- FIKA projekt, companion závislost a synchronizační integrace jsou vynechané. Metadata výsledné DLL nemají žádnou assembly referenci na FIKA.
- Převedena jsou API pohybu, inputu, zbraní, interakcí, inventáře, VR nastavení a nabídek; existující VR logika není nahrazena prázdnými implementacemi.
- Nativní `OpticComponentUpdater.LateUpdate` běží. VR upraví far clip v postfixu a synchronizuje zoom přes `ScopeZoomHandler` a `SightComponent.ScopeZoomValue`.
- Nativní reticle update běží před VR úpravou pozice/rotace. Ostřejší retikl používá oddělenou lens texturu po PIP renderu, s obnovou nativní textury při vypnutí. PIP kamera nemění svůj render target kvůli VR override; obecný SSAA override ji výslovně vynechává.
- Inicializace pojišťovacího okna používá nativní 4.1.6 `Show` a pouze postfix pro VR umístění.
- Build používá přesné verze balíčků a zamčené závislosti. Build/test/package skripty jsou v repozitáři.
- Oprava 1.3.1.416 po prvním testu ve hře inicializuje nativní Unity JSON převodníky před XR a ověří `UnparsedData`/počty předmětů i textový formát `Side` požadavku na raid. Při neúplných existujících defaults doplní pouze dva potřebné převodníky a zachová zbytek nastavení i jeho provider. Plugin se načítá po SPT.Core a SPT.Singleplayer. Důkazy a meze závěru jsou v `docs/runtime-report-2026-10-24.md`.
- Oprava 1.3.2.416 vyřazuje z kompilace čtyři nepoužívané volitelné integrace a odstraňuje jejich assembly reference i detekci v pluginu. Uživatelský log totiž potvrdil pád nativní discovery na poli `EFTApiSupport.configView` s chybějícím `KmyTarkovConfiguration`. Zdrojové soubory integrací zůstávají uložené pro oddělený port; instalace dalších modů se nevyžaduje.

## Výsledky

Příkaz `bash scripts/build.sh` doběhl s návratovým kódem 0. Proběhla obnova závislostí v locked mode, Release kompilace, metadata test, načtení celé sestavené DLL bez volitelných modů, skutečný regresní test JSON a vytvoření balíčku s aktuálním návodem.

- Release: **0 chyb kompilace**. Varování zděděného kódu zůstávají viditelná; nejsou plošně potlačena.
- Kompatibilita: **276 základních cílů Harmony, 0 selhání, 0 dynamických/neověřených cílů**. Kontrolovány byly metody, overloady, názvy/typy parametrů, instance/výsledky a injektovaná pole. Porovnání s předchozím seznamem 293 cílů potvrdilo vyřazení přesně 17 volitelných integračních patchů a zachování všech základních VR cílů.
- Další kontrakty: shodná verze BepInPlugin/assembly, žádná FIKA reference, PIP/reticle postfixy, zápis trvalého native zoomu a výjimka pro nativní optiku u přepisování camera targetu.
- Metadata navíc ověřují závislosti na SPT, volání JSON bootstrapu před XR, existenci nativního Unity JSON inicializátoru a absenci assembly referencí i support typů volitelných modů. Při této kontrole nejsou přidány jejich compile-only knihovny.
- JSON regrese: **93 úspěšných kontrol** nad původními nepřepsanými Managed 4.1.6 a skutečnou sestavenou DLL. Test načte všech **195 typů pluginu** s blokovanými volitelnými assembly, místo dřívějšího přilinkování samotného zdroje bootstrapu. Reprodukuje chybu převodu `StackObjectsCount` a odmítnutí číselné `Side` serverovým modelem. Pak spustí skutečný nativní Unity inicializátor a bootstrap přímo z výsledné DLL. Ověří počty 1/30/60/500000/9999999, vnořenou durability, boolean stav, SPT preset ID, zpětnou serializaci a přijetí PMC/Scav požadavků. Ověřuje i existující custom převodníky, zachování settings, idempotenci a provider vytvářející nové settings při každém volání. Nejde o spuštění Unity scény ani serveru.
- Starší DLL 1.3.1.416 nový test odmítl kvůli chybějící volitelné assembly. Runtime test v .NET 8 používá filtr typů nativního inicializátoru; úplnou reflexi všech Unity/Mono signatur omezuje odlišná validace původních delegátů bez `sealed`. Kontrolu assembly referencí v metadatech provádí Cecil.
- ZIP: CRC v pořádku, vložená DLL byte-for-byte odpovídá Release, 45 souborů, žádné FIKA entry ani `Assembly-CSharp` reference.
- Původní nezměněný upstream byl dříve ověřen proti Managed 4.0.13: 0 chyb, 179 varování.
- Online NuGet vulnerability audit nebyl dostupný kvůli omezenému síťovému přístupu (NU1900). Obnova zamčených závislostí a sestavení uspěly; TLS/signature/hash ověřování nebylo vypnuto.

Runtime assety a SteamVR/XR soubory balíčku pocházejí z dodaného instalačního ZIPu 1.3.0. Asset bundle v upstream source checkoutu se od tohoto release liší, proto není použit jako náhrada kompletních release assetů.

## Artefakty

- `artifacts/SPT-VR.dll`: SHA256 `2cc920d91009872f1ca6a713c8889b343957573f89bd94559c49fdc9380a14fe`
- `artifacts/SPT-VR-4.1.6-test.zip`: SHA256 `f3fee073383a8b47899593957bcd7e04784fc8a77254e55e0970540a469f2ad4`
- `artifacts/compatibility.json`: úplný přehled ověřených patchů.
- `artifacts/json-regression.log`: výsledek skutečného testu JSON. Všechny výše uvedené výstupy jsou také v `releases/4.1.6-test`.

Windows/SPT/SteamVR ani skutečný headset zde spuštěné nebyly. Uživatel ověřil funkční menu 1.3.0.416, dodal logy problémů se stacky/raidem a následně log pádu 1.3.1.416 při native JSON discovery. Nová 1.3.2.416 čeká na opakovaný test ve hře. Ověření metadat a serializeru není důkaz funkčnosti všech herních cest. Návod a ruční testy jsou v `TESTING.md`; toto je testovací sestavení, nikoli potvrzení herní kompatibility.
