# Testovací sestavení pro SPT 4.1.6

Verze pluginu: **1.3.1.416**. Opravuje inicializaci JSON pro počty předmětů a požadavky na raid. Předchozí `1.3.0.416` podle hlášení fungovala v menu, ale zobrazovala stacky jako 1 a nedokončila načtení raidu. Nová verze zatím čeká na ověření ve Windows s headsetem.

## Aktualizace z 1.3.0.416

1. Ukončete hru a zálohujte profil i stávající plugin mimo `BepInEx/plugins`.
2. Pokud jste už rozbalili celý testovací ZIP, nyní stačí nahradit **pouze** `BepInEx/plugins/sptvr/SPT-VR.dll`. XR soubory a assety se nemění.
3. V novém `BepInEx/LogOutput.log` musí být verze `1.3.1.416` a zpráva `JSON defaults verified` nebo `JSON defaults repaired` před inicializací VR. SPT.Core a SPT.Singleplayer se nyní načítají před VR pluginem.
4. Nejprve na kopii profilu porovnejte peníze, stack munice, počet nábojů v zásobníku a nabídku obchodníka. Před tímto ověřením neprovádějte transakce.
5. Potom zkuste PMC raid. `getRaidTime` má vrátit úspěšnou odpověď a načítání má pokračovat za původní chybu v `ScavLateStartPatch`. Následně otestujte Scav raid.

Oprava načítá skutečné hodnoty dodané serverem; nedopočítává ani neobnovuje již přepsané počty v uloženém profilu. Pokud byly změny uloženy, použijte nepoškozenou zálohu profilu. Není zatím potvrzeno, zda první test změnil i uložená data.

## Instalace

1. Použijte SPT **4.1.6**, BepInEx 5.x a SteamVR. Testujte bez FIKA a ostatních volitelných modů.
2. Zálohujte stávající `BepInEx/plugins/sptvr` a konfiguraci VR modu mimo složku `BepInEx/plugins`.
3. Pokud už máte kompletní SPT-VR 1.3.0, nahraďte pouze `BepInEx/plugins/sptvr/SPT-VR.dll` novým souborem `artifacts/SPT-VR.dll`.
4. Pro kompletní instalaci rozbalte `SPT-VR-4.1.6-test.zip` do kořene SPT. Obsahuje původní VR assety a SteamVR/XR soubory s novou DLL. FIKA companion v něm není.
5. Spusťte SteamVR a potom hru přes SPT launcher.

Archivy `Managed` a publicized reference jsou pouze pro sestavení. Nekopírujte je do hry.

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
