# Testovací sestavení pro SPT 4.1.6

Verze pluginu: **1.3.0.416**. Balíček je určený pro ruční testování; ve Windows ani s headsetem zatím nebyl spuštěný.

## Instalace

1. Použijte SPT **4.1.6**, BepInEx 5.x a SteamVR. Testujte bez FIKA a ostatních volitelných modů.
2. Zálohujte stávající `BepInEx/plugins/sptvr` a konfiguraci VR modu mimo složku `BepInEx/plugins`.
3. Pokud už máte kompletní SPT-VR 1.3.0, nahraďte pouze `BepInEx/plugins/sptvr/SPT-VR.dll` novým souborem `artifacts/SPT-VR.dll`.
4. Pro kompletní instalaci rozbalte `SPT-VR-4.1.6-test.zip` do kořene SPT. Obsahuje původní VR assety a SteamVR/XR soubory s novou DLL. FIKA companion v něm není.
5. Spusťte SteamVR a potom hru přes SPT launcher.

## Kontrola funkčnosti

- **Start a menu:** headset, laserový ukazatel, záložky VR a VR More, inventář a návrat zpět. Změny VR nastavení nesmějí měnit zařízení VoIP.
- **Pohyb:** chůze, smooth/snap turn, sprint, přikrčení, zalehnutí, vstávání, vault a obě varianty dominantní ruky.
- **Zbraně a ruce:** druhá ruka, výměna zbraní a zásobníků, režim střelby, nůž, granát, laser/svítilna, jídlo a léčení.
- **Interakce:** dveře, kontejnery, loose loot, uchopení/puštění předmětu, uložení do inventáře a přetažení mrtvého těla. Otestujte také hideout.
- **Optika:** pevné i variabilní zvětšení; PIP obraz se musí průběžně aktualizovat v obou očích. Ověřte zoom joystickem i otáčením ruky, přepnutí zbraně a obnovení zoomu.
- **Retikl:** zarovnání, ostrost, žádné zdvojení ani zamrzlá/modrá vegetace. Vyzkoušejte různá rozlišení Scope Render Resolution a případnou termální optiku.
- **Ukončení:** přechod z raidu/hideoutu do menu a další raid bez zbytkových kamer nebo textur.

## Hlášení chyby

Uveďte přesný krok, zbraň/optiku, headset a ovladače. Přiložte `BepInEx/LogOutput.log` z tohoto běhu, zejména první výjimku a případné `HarmonyException`, `MissingMethodException` nebo `MissingFieldException`. Pro návrat k původnímu stavu obnovte zálohovaný plugin a konfiguraci.
