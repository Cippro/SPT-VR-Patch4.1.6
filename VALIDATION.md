# Validace testovacího portu — 24. 10. 2026

Zdroj: cybensis/SPT-VR, commit `00600ee5f496da0be07cf3686ffe7ec88c00eaa8`. Port je lokálně ve větvi `codex/port-spt-4.1.6` repozitáře Cippro/SPT-VR-Patch4.1.6. Verze pluginu i assembly je `1.3.0.416`.

## Provedené změny

- Zdrojový projekt je v `src/SPT-VR`; herní API bylo převedeno podle dodaných Managed 4.1.6 a porovnání s původními Managed 4.0.13. Při mapování byly porovnány signatury, rozložení polí a IL; původní upstream zůstává oddělený.
- FIKA projekt, companion závislost a synchronizační integrace jsou vynechané. Metadata výsledné DLL nemají žádnou assembly referenci na FIKA.
- Převedena jsou API pohybu, inputu, zbraní, interakcí, inventáře, VR nastavení a nabídek; existující VR logika není nahrazena prázdnými implementacemi.
- Nativní `OpticComponentUpdater.LateUpdate` běží. VR upraví far clip v postfixu a synchronizuje zoom přes `ScopeZoomHandler` a `SightComponent.ScopeZoomValue`.
- Nativní reticle update běží před VR úpravou pozice/rotace. Ostřejší retikl používá oddělenou lens texturu po PIP renderu, s obnovou nativní textury při vypnutí. PIP kamera nemění svůj render target kvůli VR override; obecný SSAA override ji výslovně vynechává.
- Inicializace pojišťovacího okna používá nativní 4.1.6 `Show` a pouze postfix pro VR umístění.
- Build používá přesné verze balíčků a zamčené závislosti. Build/test/package skripty jsou v repozitáři.

## Výsledky

Příkaz `bash scripts/build.sh` doběhl s návratovým kódem 0. Proběhla obnova závislostí v locked mode, Release kompilace, spuštění metadata testu a vytvoření balíčku.

- Release: **0 chyb kompilace**. Varování zděděného kódu zůstávají viditelná; nejsou plošně potlačena.
- Kompatibilita: **293 cílů Harmony, 0 selhání, 0 dynamických/neověřených cílů**. Kontrolovány byly metody, overloady, názvy/typy parametrů, instance/výsledky a injektovaná pole.
- Další kontrakty: shodná verze BepInPlugin/assembly, žádná FIKA reference, PIP/reticle postfixy, zápis trvalého native zoomu a výjimka pro nativní optiku u přepisování camera targetu.
- ZIP: CRC v pořádku, vložená DLL byte-for-byte odpovídá Release, 45 souborů, žádné FIKA entry ani `Assembly-CSharp` reference.
- Původní nezměněný upstream byl dříve ověřen proti Managed 4.0.13: 0 chyb, 179 varování.
- Online NuGet vulnerability audit nebyl dostupný kvůli omezenému síťovému přístupu (NU1900). Obnova zamčených závislostí a sestavení uspěly; TLS/signature/hash ověřování nebylo vypnuto.

Runtime assety a SteamVR/XR soubory balíčku pocházejí z dodaného instalačního ZIPu 1.3.0. Asset bundle v upstream source checkoutu se od tohoto release liší, proto není použit jako náhrada kompletních release assetů.

## Artefakty

- `artifacts/SPT-VR.dll`: SHA256 `7e4f6c6f2381a546da9fdb1ead67fccc323b59a149853966bf4960d1e01636a6`
- `artifacts/SPT-VR-4.1.6-test.zip`: SHA256 `09add17df2894ff3765edcc215ada359c59836841f7f2bccb3f68e345e412900`
- `artifacts/compatibility.json`: úplný přehled ověřených patchů.

Windows/SPT/SteamVR ani skutečný headset zde spuštěné nebyly. Ověření metadat není důkaz funkčnosti všech herních cest. Návod a ruční testy jsou v `TESTING.md`; toto je testovací sestavení, nikoli potvrzení herní kompatibility.
