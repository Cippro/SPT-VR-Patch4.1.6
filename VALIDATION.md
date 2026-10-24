# Validace testovacího portu — 24. 10. 2026

Zdroj: cybensis/SPT-VR, commit `00600ee5f496da0be07cf3686ffe7ec88c00eaa8`. Port je ve větvi `codex/port-spt-4.1.6` repozitáře Cippro/SPT-VR-Patch4.1.6. Verze pluginu i assembly je `1.3.4.416`.

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
- Oprava 1.3.3.416 reaguje na výjimku `AddAndFixMenuVRCam` po výběru postavy. Postfix čeká na nativní task menu/obnovy prostředí, uloží aktuální prostředí před nastavením kamery a obnoví chybějící části rigu, ruce i ukazatel. Menu a UI kamery používají obě oči, canvasy aktuální kameru. Ovladače mají explicitní existující pose akce a ukazatel tlačítka A/X. Listenery se odpojují i při zániku ukazatele.
- Oprava 1.3.4.416 reaguje na uživatelský test menu a Scav raidu. Jednorázově dorovná menu podle skutečných stereo eye pozic, přepíná kamery canvasů při inventáři/raidovém menu/hideoutu a zachovává pointer/press metadata mezi snímky. Krátké a dlouhé potvrzení, release mimo UI a drag mají oddělené stavy. Paprsek už při A/X nemění barvu ani šířku. VR raycast nepředává nativní nabídce loot s chybějícím vlastníkem/root itemem.

## Výsledky

Příkaz `bash scripts/build.sh` doběhl s návratovým kódem 0. Proběhla obnova závislostí v locked mode, Release kompilace, metadata test, načtení celé sestavené DLL bez volitelných modů, skutečný regresní test JSON a vytvoření balíčku s aktuálním návodem.

- Release: **0 chyb kompilace**. Varování zděděného kódu zůstávají viditelná; nejsou plošně potlačena.
- Kompatibilita: **276 základních cílů Harmony, 0 selhání, 0 dynamických/neověřených cílů**. Kontrolovány byly metody, overloady, názvy/typy parametrů, instance/výsledky a injektovaná pole. Porovnání s předchozím seznamem 293 cílů potvrdilo vyřazení přesně 17 volitelných integračních patchů a zachování všech základních VR cílů.
- Další kontrakty: shodná verze BepInPlugin/assembly, žádná FIKA reference, PIP/reticle postfixy, zápis trvalého native zoomu a výjimka pro nativní optiku u přepisování camera targetu.
- Metadata navíc ověřují závislosti na SPT, volání JSON bootstrapu před XR, existenci nativního Unity JSON inicializátoru a absenci assembly referencí i support typů volitelných modů. Při této kontrole nejsou přidány jejich compile-only knihovny.
- Metadata ověřují také obalení tasků obou menu postfixů, pořadí přiřazení prostředí před úpravou kamery, volání obnovy rukou a odpojení pose listenerů při vypnutí/zániku manageru. Všech 276 cílů z 1.3.2.416 zůstalo zachováno.
- Metadata dále kontrolují FPS kameru při otevření inventáře, projekci přes kameru canvasu, nativní current/press raycast údaje a podmínky platné stereo pose pro kalibraci. Laser updater nesmí číst `clickColor` ani stav drženého tlačítka pro vzhled paprsku. Všech 276 cílů z 1.3.3.416 zůstalo zachováno.
- JSON regrese: **93 úspěšných kontrol** nad původními nepřepsanými Managed 4.1.6 a skutečnou sestavenou DLL. Test načte všech **202 typů pluginu** s blokovanými volitelnými assembly, místo dřívějšího přilinkování samotného zdroje bootstrapu. Reprodukuje chybu převodu `StackObjectsCount` a odmítnutí číselné `Side` serverovým modelem. Pak spustí skutečný nativní Unity inicializátor a bootstrap přímo z výsledné DLL. Ověří počty 1/30/60/500000/9999999, vnořenou durability, boolean stav, SPT preset ID, zpětnou serializaci a přijetí PMC/Scav požadavků. Ověřuje i existující custom převodníky, zachování settings, idempotenci a provider vytvářející nové settings při každém volání. Nejde o spuštění Unity scény ani serveru.
- Menu task regrese: **10 úspěšných kontrol** skutečného helperu z výsledné DLL. Callback neběží před dokončením nativního tasku, spustí se jednou, respektuje nativní chybu/zrušení a běží na zachyceném synchronizačním kontextu a vlákně. Ověřena je i pozorovatelnost chyby callbacku. V dodaném SteamVR manifestu existují všechny čtyři použité menu pose/click akce.
- UI confirm regrese: **22 úspěšných kontrol** `UiConfirmState` přímo z výsledné DLL: krátký klik, jedna kontextová akce při podržení, žádný další levý klik po podržení, release mimo UI/na jiném cíli, drag, reset a tlačítko držené bez předchozího stisku. Test nespouští skutečný Unity `EventSystem` ani nativní inventory transakce.
- Starší DLL 1.3.1.416 nový test odmítl kvůli chybějící volitelné assembly. Runtime test v .NET 8 používá filtr typů nativního inicializátoru; úplnou reflexi všech Unity/Mono signatur omezuje odlišná validace původních delegátů bez `sealed`. Kontrolu assembly referencí v metadatech provádí Cecil.
- ZIP: CRC v pořádku, vložená DLL byte-for-byte odpovídá Release, 45 souborů, žádné FIKA entry ani `Assembly-CSharp` reference.
- Původní nezměněný upstream byl dříve ověřen proti Managed 4.0.13: 0 chyb, 179 varování.
- Online NuGet vulnerability audit nebyl dostupný kvůli omezenému síťovému přístupu (NU1900). Obnova zamčených závislostí a sestavení uspěly; TLS/signature/hash ověřování nebylo vypnuto.

Runtime assety a SteamVR/XR soubory balíčku pocházejí z dodaného instalačního ZIPu 1.3.0. Asset bundle v upstream source checkoutu se od tohoto release liší, proto není použit jako náhrada kompletních release assetů.

## Artefakty

- `artifacts/SPT-VR.dll`: SHA256 `56582ee3aac8dae11332722afa85cffa30fa55c8d06e71244ceffdc35f563a43`
- `artifacts/SPT-VR-4.1.6-test.zip`: SHA256 `179b9a2ecb2b1ad22e440042f381caf12edd6c8f37eea09e0fda97e8a85bc470`
- `artifacts/compatibility.json`: úplný přehled ověřených patchů.
- `artifacts/json-regression.log`: výsledky testů celé DLL, JSON, menu task helperu a potvrzovacího tlačítka. Všechny výše uvedené výstupy jsou také v `releases/4.1.6-test`.

Windows/SPT/SteamVR ani skutečný headset zde spuštěné nebyly. Uživatel potvrdil funkční hlavní menu a Scav raid s 1.3.3.416 a dodal logy problémů s výškou a interakcí inventáře. Nová 1.3.4.416 čeká na opakovaný test těchto změn v headsetu, včetně bezpečného drag/drop na kopii profilu. V logu jsou také chyby rozměrů některých render textur; UI oprava je neřeší. Ověření metadat, serializeru, tasků a confirm stavu není důkaz funkčnosti všech herních cest. Návod a ruční testy jsou v `TESTING.md`.
