# Oprava JSON po prvním testu ve hře

## Aktualizace 1.3.3.416 — přechod z výběru postavy do VR menu

Další log verze 1.3.2.416 potvrzuje `JSON defaults verified`, úspěšnou inicializaci SteamVR s Quest 3 a úspěšný požadavek `/singleplayer/clientmods`. Při vstupu do hlavního menu po výběru postavy pak nastala jiná chyba:

```text
NullReferenceException
at UnityEngine.GameObject.get_transform(GameObject)
at MenuPatches.AddAndFixMenuVRCam(MainMenuShowOperation) [0x00071]
at MainMenuShowOperation.Init()
at MainMenuShowOperation.Execute(...)
at TarkovApplication.MainMenu(...)
```

IL publikované DLL v tomto bloku čte `menuVRManager.RightHand.transform` a `vrOffsetter.transform`. Nestačilo, že existoval manager: objekty rukou mohly zaniknout s kamerou předchozí obrazovky. Postfix navíc běžel už při návratu `Task` z nativního asynchronního `Init`, před dokončením načítání, a volal nastavení kamery před přiřazením aktuálního prostředí.

Oprava 1.3.3.416:

- Postfixy `MainMenuShowOperation.Init` a `EnvironmentUI.RefreshEnvironmentAsync` vrací obalený task. VR callback proběhne až po úspěšném dokončení původního tasku a zachová jeho synchronizační kontext, chyby i zrušení.
- Menu nejprve uloží aktuální prostředí a UI reference. `EnsureMenuRig` obnoví chybějící části sestavy a `SpawnHands` nezávisle obnoví chybějící ruce, ukazatel a UI interakci.
- Kamera prostředí a její UI kamera cílí na obě oči. Canvasy mají aktuální world camera; při obnově prostředí se vazby znovu nastaví. Volitelné komponenty kamery mají kontrolu existence.
- Menu ruce explicitně používají existující `LeftHandPose`/`RightHandPose`. Jejich transformace nadále řídí VR pose listenery; nevzniká souběžný zápis z výchozí neplatné akce `Pose`. Ukazatel používá tlačítko A/X místo neexistujícího `InteractUI` z logu.
- Při vypnutí/zániku manageru se pose listenery odpojí i tehdy, když už ukazatel zanikl. Případná chyba VR callbacku se vypíše s `[MenuVR]` a nepřeruší dokončenou nativní inicializaci menu.

Nový test spouští task helper přímo ze sestavené DLL. Deset kontrol ověřuje čekání na nedokončený task, jediné spuštění callbacku, nativní chybu, zrušení, chybu callbacku a návrat na zachycený synchronizační kontext a vlákno. Metadata ověřují obalení tasků, pořadí přiřazení prostředí, obnovu rukou, obě menu kamery a odpojení listenerů. Zůstává 276 základních Harmony cílů, 93 kontrol JSON a žádná volitelná/FIKA závislost; kompletní DLL nyní obsahuje 199 typů.

Geometrii scény, fyzické pohyby ovladačů a zobrazení v headsetu nelze tímto cloudovým testem potvrdit. Je potřeba znovu ověřit výběr postavy, hlavní menu, obě dominantní ruce a následně raid podle `TESTING.md`.

## Aktualizace 1.3.2.416 — určená příčina pádu při startu

Následující uživatelský `LogOutput.log` potvrdil načtení verze 1.3.1.416 a tento řetězec výjimek před inicializací VR:

```text
Game JSON initialization failed before VR startup
TypeInitializationException: Newtonsoft.Json.UnityConverters.UnityConverterInitializer
TypeLoadException: Could not load type of field
TarkovVR.ModSupport.EFTApi.EFTApiSupport:configView
Could not load file or assembly 'KmyTarkovConfiguration, Version=1.4.0.0'
at UnityConverterInitializer.FindCustomConverters
```

Nativní inicializátor prohledává typy ve všech načtených assembly. Volitelné integrace byly součástí stejné DLL a jejich pole, signatury a Harmony atributy odkazovaly na knihovny, které uživatel nemá. Kontrola existence souboru před aplikací volitelných patchů nepomohla: selhávalo už vyhledávání typů. Tato chyba vysvětluje nefunkční JSON defaults a zároveň zabránila opravě 1.3.1.416 vůbec spustit VR.

Verze 1.3.2.416 je standalone build: čtyři `ModSupport/*Support.cs` zůstávají uložené, ale nekompilují se do DLL. Odstraněné jsou jejich compile reference i runtime detekce/aplikace v pluginu. Nepřítomné EFT API, AmandsGraphics, DynamicMaps a WeaponCustomizer proto nejsou podmínkou načtení pluginu. Jejich samostatný port a izolace závislostí zůstávají mimo toto testovací sestavení.

Při porovnání seznamu patchů s verzí 1.3.1.416 chybí přesně 17 cílů volitelných integrací a žádný základní VR cíl. Zůstává všech 276 základních patchů, včetně ovládání a nativní PIP/optiky.

Předchozí JSON test načítal pouze přilinkovaný zdroj bootstrapu, takže tento problém s celou DLL nezachytil. Nový test načítá přímo výslednou `SPT-VR.dll`, blokuje pět volitelných assembly v resolveru a potom spouští její skutečný bootstrap a nativní Unity JSON inicializátor. Starší DLL byla při kontrole odmítnuta kvůli chybějící volitelné závislosti; nová načetla 195 typů a prošla 93 kontrolami JSON. Metadata test navíc odmítne jakoukoli assembly referenci na tyto mody i návrat legacy support typů do standalone DLL.

Test používá .NET 8, nikoli Unity Mono. Některé původní herní delegáty bez `sealed` nelze v .NET 8 plně reflektovat. Runtime část proto používá stejný filtr typů jako nativní JSON discovery; kontrolu externích assembly referencí v metadatech provádí Cecil. Nejde o potvrzení běhu raidu ve Windows. Po výměně DLL je nutné hru úplně restartovat a zopakovat test na kopii profilu.

## Předchozí analýza a oprava 1.3.1.416

## Doložené příznaky

Uživatel instaloval celý testovací ZIP. Aktuální logy potvrzují SPT 4.1.6, EFT 0.16.9.5.40743, plugin 1.3.0.416 a ovladače Quest 3. Menu fungovalo. V klientovi se peníze, munice a nabídky obchodníků zobrazovaly s počtem 1. Načtení raidu dvakrát skončilo chybou.

Relevantní výjimky bez osobních údajů:

```text
JsonSerializationException: Error converting value 30 to type 'UnparsedData'.
Path 'StackObjectsCount'.
at EFT.ItemDeserializer.CreateItem(Item item, UnparsedData properties)

JsonSerializationException: Unexpected initial token 'Null' when populating object.
at JsonExtensions.ParseJsonTo(UnparsedData, Type, Object, JsonConverter[])

[REQUEST FAILED] /singleplayer/settings/getRaidTime
NullReferenceException
at SPT.SinglePlayer.Utils.InRaid.RaidChangesUtil.UpdateRaidChanges(...)
at SPT.SinglePlayer.Patches.ScavMode.ScavLateStartPatch.PatchPrefix(...)
```

Původní hodnoty jsou v příchozích datech přítomné, ale selhává jejich převod. Samotné logy nedokazují, že se už změnil i uložený profil.

## Ověření příčiny a změna

Nativní `ItemDeserializer.CreateItem` volá `JToken.ToObject<Dictionary<string, UnparsedData>>()` bez explicitního serializeru. Potřebuje proto výchozí `UnparsedDataConverter`. Bez něj byly s původními dodanými assembly 4.1.6 reprodukovány stejné výjimky a po jeho registraci byl zachován skutečný počet.

SPT klient vytváří `RaidTimeRequest` s enumem `ESideType` a používá `JsonConvert.SerializeObject`. Bez výchozího string-enum převodníku posílá číselné `Side`. Serverový model `GetRaidTimeRequest.Side` očekává řetězec. Regresní test ověřuje, že původní číselný JSON selže proti stejnému typu modelu v System.Text.Json a opravené PMC/Scav požadavky jsou přijaty.

Ověřené veřejné zdroje při analýze: [SPT Modules](https://github.com/sp-tarkov/Modules/tree/8dea32ab780eb32eecb444e5f6d318309651b241), `ScavLateStartPatch`, `RaidChangesUtil`, `SPT.Common.Utils.Json`; [SPT server](https://github.com/sp-tarkov/server-csharp/tree/fe74b07c6361e2c6d1532dc21ba1c3b981d88d93), `GetRaidTimeRequest`, `GameStaticRouter`, `JsonUtil`. Tyto checkouty nejsou vydávány za přesný zdrojový tag uživatelova serveru 4.1.6.

Plugin 1.3.1.416 se načítá po SPT.Core a SPT.Singleplayer. Před XR inicializuje nativní `UnityConverterInitializer.Init`, pokud výchozí serializer chybí. Pokud už existující provider postrádá potřebné převodníky, obalí jej a doplní pouze nativní `UnparsedDataConverter` a `StringEnumConverter`. Zachovává ostatní nastavení a převodníky, včetně providerů vytvářejících nové settings při každém volání. Před pokračováním provede skutečný test načtení stacku a serializace `Side`.

Neobchází `ScavLateStartPatch`, nevymýšlí chybějící raidová data, nemění server ani profily. Nativní PIP a VR ovládání se touto opravou nemění.

## Meze závěru

Chybějící JSON kontrakt pro předměty je reprodukovaný. Přesný okamžik, který způsobil chybějící výchozí nastavení při původním startu, z logů určit nelze. Logy neobsahují skutečné tělo `getRaidTime` požadavku ani serverovou výjimku; číselné `Side` je reprodukovaná související chyba, nikoli důkaz, že server neměl ještě další problém. Nová DLL musí být znovu ověřena ve Windows/SteamVR. Při dalším selhání endpointu je potřeba serverový log.

Původní uživatelské logy nejsou součástí repozitáře ani instalačního ZIPu.
