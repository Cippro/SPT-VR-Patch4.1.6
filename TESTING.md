# Testovací sestavení pro SPT 4.1.6

Verze pluginu: **1.3.5.416**. Navazuje na uživatelem potvrzenou funkční alfu 1.3.4.416. Opravuje předávání drženého A/X do znovu otevřeného UI, úklid při změně UI systému a pokračování interakce po zavření okna nativním callbackem. Nová změna zatím čeká na ověření ve Windows s headsetem. Přehled potvrzeného stavu a priorit je v [plánu stabilizace](https://github.com/Cippro/SPT-VR-Patch4.1.6/blob/codex/port-spt-4.1.6/docs/alpha-stabilization.md).

## Aktualizace z předchozí testovací verze

1. Ukončete hru a zálohujte profil i stávající plugin mimo `BepInEx/plugins`.
2. Pokud jste už rozbalili celý testovací ZIP, nyní stačí nahradit **pouze** `BepInEx/plugins/sptvr/SPT-VR.dll`. XR soubory a assety se nemění.
3. V novém `BepInEx/LogOutput.log` musí být verze `1.3.5.416`, zpráva `JSON defaults verified` nebo `JSON defaults repaired` a následně úspěšná inicializace VR. SPT.Core a SPT.Singleplayer se načítají před VR pluginem.
4. Ověřte přechod do hlavního menu po výběru/vytvoření postavy i při běžném přihlášení k existujícímu profilu. Menu musí být vidět v obou očích a reagovat na pohyb hlavy. Pravý ukazatel má fungovat tlačítkem A; v levostranném režimu tlačítkem X. V logu očekávejte `[MenuVR] Camera '...' configured for both eyes; controller rig restored.` a `[MenuVR] Main menu UI initialized after native Init completed.` Nesmí se objevit nová chyba `[MenuVR] ... failed` ani původní `AddAndFixMenuVRCam` výjimka.
5. Po prvních platných stereo snímcích má být menu přibližně v úrovni očí, ne u podlahy. V logu očekávejte `[MenuVR] Headset height calibrated: ...`. Dorovnání proběhne jednou při vstupu/obnovení prostředí; menu pak nesleduje každé přikrčení. Pokud používáte stoj i sed, ověřte obě polohy při novém vstupu do menu.
6. Na kopii profilu porovnejte peníze, stack munice, počet nábojů v zásobníku a nabídku obchodníka. Potom vyzkoušejte Scav i PMC raid. `getRaidTime` má vrátit úspěšnou odpověď.
7. V raidu otevřete inventář. Log má uvést `[VR UI] Interaction camera bound to 'FPS Camera'`. Ověřte hover, krátké A pro klik, dvojklik na stejný předmět a podržení A pro pravou kontextovou nabídku. Uvolnění po podržení nesmí navíc vybrat položku levým klikem. Uvolnění mimo UI nesmí ponechat tlačítko stisknuté. První předané potvrzení se vypíše jako `[VR UI] Confirm delivered ...`.
8. Ověřte přesun předmětu triggerem, puštění do platného slotu, puštění mimo UI a zrušení pomocí B. Zrušený drag se při stále drženém triggeru nesmí ihned znovu rozběhnout; nejprve trigger uvolněte. V levostranném režimu odpovídají tlačítka X/Y.
9. Paprsek musí při krátkém i dlouhém A/X zůstat stejně tenký a stejné barvy. Než pokračujete v testu, ověřte, že přesuny na kopii profilu nepoškodily inventář.
10. Zavřete inventář, ověřte dveře, kontejnery a volný loot, potom návrat do menu a změnu prostředí menu. Původní chyba `GetAvailableActions(...LootItem...)` kvůli chybějícímu vlastníkovi lootu se nemá vracet.

## Opakované otevření UI — nové v 1.3.5.416

Na kopii profilu proveďte alespoň deset cyklů otevření/zavření inventáře. Vyzkoušejte otevření při drženém A/X i triggeru; nejprve je uvolněte a potom stiskněte znovu. Držené potvrzení nesmí samo kliknout ani otevřít kontextovou nabídku v novém okně. Po uvolnění musí nový klik fungovat.

Zavřete inventář během rozpracovaného dragu, znovu jej otevřete a vyzkoušejte další klik i přesun. Nesmí zůstat viset stisk, náhled předmětu nebo zvýraznění. Totéž ověřte při návratu z raidu do menu a v druhém raidu bez restartu hry. Zkontrolujte také přesnou akci, která okno sama zavírá, aby se její release nepředal do dalšího okna. Pokud lze bezpečně přerušit sledování ovladače během stisku, po obnovení nesmí dojít k opožděnému kliknutí.

Oprava načítá skutečné hodnoty dodané serverem; nedopočítává ani neobnovuje již přepsané počty v uloženém profilu. Pokud byly změny uloženy, použijte nepoškozenou zálohu profilu. Není zatím potvrzeno, zda první test změnil i uložená data.

## Instalace

1. Použijte SPT **4.1.6**, BepInEx 5.x a SteamVR. Testujte bez FIKA a ostatních volitelných modů.
2. Zálohujte stávající `BepInEx/plugins/sptvr` a konfiguraci VR modu mimo složku `BepInEx/plugins`.
3. Pokud už máte kompletní SPT-VR 1.3.0, nahraďte pouze `BepInEx/plugins/sptvr/SPT-VR.dll` novým souborem `artifacts/SPT-VR.dll`.
4. Pro kompletní instalaci rozbalte `SPT-VR-4.1.6-test.zip` do kořene SPT. Obsahuje původní VR assety a SteamVR/XR soubory s novou DLL. FIKA companion v něm není.
5. Spusťte SteamVR a potom hru přes SPT launcher.

Archivy `Managed` a publicized reference jsou pouze pro sestavení. Nekopírujte je do hry.

Testovací DLL neobsahuje integrace EFT API, AmandsGraphics, DynamicMaps ani WeaponCustomizer. Chybějící `KmyTarkovConfiguration` se řeší aktualizací této DLL; pro tento test není potřeba instalovat další mod.

## Kontrola funkčnosti

- **Start a menu:** headset, laserový ukazatel, záložky VR a VR More, inventář a návrat zpět. Změny VR nastavení nesmějí měnit zařízení VoIP.
- **Pohyb:** chůze, smooth/snap turn, sprint, přikrčení, zalehnutí, vstávání, vault a obě varianty dominantní ruky.
- **Zbraně a ruce:** druhá ruka, výměna zbraní a zásobníků, režim střelby, nůž, granát, laser/svítilna, jídlo a léčení.
- **Interakce:** dveře, kontejnery, loose loot, uchopení/puštění předmětu, uložení do inventáře a přetažení mrtvého těla. Otestujte také hideout.
- **Optika:** pevné i variabilní zvětšení; PIP obraz se musí průběžně aktualizovat v obou očích. Ověřte zoom joystickem i otáčením ruky, přepnutí zbraně a obnovení zoomu.
- **Retikl:** zarovnání, ostrost, žádné zdvojení ani zamrzlá/modrá vegetace. Vyzkoušejte různá rozlišení Scope Render Resolution a případnou termální optiku.
- **Ukončení:** přechod z raidu/hideoutu do menu a další raid bez zbytkových kamer nebo textur.

## Hlášení chyby

Uveďte přesný krok, zbraň/optiku, headset a ovladače. Přiložte `BepInEx/LogOutput.log` a nejnovější podsložku `Logs` z tohoto běhu, zejména první výjimku a případné `HarmonyException`, `MissingMethodException` nebo `MissingFieldException`. Pokud znovu selže `getRaidTime`, přiložte také aktuální serverový log; nulovou odpověď může způsobit i další chyba serveru. Pro návrat k původnímu stavu obnovte zálohovaný plugin a konfiguraci.
