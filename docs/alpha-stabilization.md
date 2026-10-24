# Stabilizace funkční alfy

Uživatel 24. 10. 2026 označil verzi **1.3.4.416** za funkční alfu. Výchozí zdroje i balíček jsou zachované v [commitu b15a03e](https://github.com/Cippro/SPT-VR-Patch4.1.6/tree/b15a03e9c9071b6d46a2a3a08083e4f12dd525c0). Toto potvrzení neznamená samostatné ověření všech zbraní, optik, map a herních cest.

## Priorita a ověření

| Oblast | Stav | Další ověření v headsetu |
| --- | --- | --- |
| Spuštění VR, menu a vstup do Scav raidu | Uživatel potvrdil; 1.3.4.416 přijatá jako alfa | Dva raidy po sobě bez restartu, návrat do menu a hideoutu |
| Opakované otevírání UI a ukončení stisku/dragu | V 1.3.5.416 doplněný úklid a čekání na release; automatické stavové testy | Deset otevření inventáře, otevření při drženém tlačítku, zavření během dragu, nový klik po návratu |
| Výška, vzdálenost a čitelnost panelů | Zachovaná kalibrace a rozložení z 1.3.4.416 | Sed/stoj, návrat z raidu; uvést konkrétní panel a polohu, která nevyhovuje |
| Dveře, kontejnery a volný loot | V 1.3.4.416 doplněná kontrola vlastníka lootu | Ověřit dostupné akce, uchopení, puštění a uložení; případná chyba s první výjimkou v logu |
| Nativní PIP a headsetové úpravy optik | Zachované; kontrolované metadata, nikoli obraz v headsetu | Pevná/variabilní optika, obě oči, zoom, výměna zbraně a obnovení zvětšení |
| Rozměry render textur | Ve starším uživatelském logu chyby CopyTexture a hloubkových targetů; příčina zatím neověřená | Aktuální log a popis případného blikání, chybějících efektů nebo odlišnosti mezi očima; zaznamenat rozlišení a DLSS/AA |

Nejprve ověřit opakovatelnost inventáře a přechodů. Rozvržení panelů a komfort ladit podle konkrétních pozorování v headsetu. Změny renderingu a optik mají vlastní kontrolní scénáře; bez jejich výsledků se neoznačují jako dokončené.

## Testovací změna 1.3.5.416

Při zapnutí interaktoru a ztrátě/výměně `EventSystem` se zruší starý stisk a drag, vyčistí zvýraznění a blokuje potvrzení do prvního uvolnění. Stav se vymaže před nativními callbacky, aby opětovné zavření neodeslalo tentýž release dvakrát. Po callbacku se ověří, že stále běží stejná interakce. Chybějící release edge z ovladače ukončí stisk bez kliknutí.

Stavové a metadata testy běží proti výsledné DLL. Unity EventSystem, nativní inventory transakce a skutečný headset v cloudu neběží; ruční postup je v [TESTING.md](../TESTING.md).

## Záznam dalšího testu

Uvést verzi pluginu, headset a ovladače, polohu sed/stoj, konkrétní kroky a očekávaný/skutečný výsledek. Přiložit `BepInEx/LogOutput.log` a log hry ze stejného běhu. U vizuální vady přidat nastavení render rozlišení, DLSS/AA a použitou optiku. Pro návrat k potvrzené alfě lze po vypnutí hry použít DLL 1.3.4.416 z výše uvedeného commitu.
