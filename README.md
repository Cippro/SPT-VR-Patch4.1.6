# SPT-VR 1.3.0 pro SPT 4.1.6

Testovací port pro single player z [cybensis/SPT-VR](https://github.com/cybensis/SPT-VR), commit `00600ee5f496da0be07cf3686ffe7ec88c00eaa8`. FIKA ani companion plugin nejsou součástí portu. Původní autoři a ovládání jsou popsaní v `src/SPT-VR/README.md`.

## Stažení testovacího sestavení

- [Kompletní ZIP](releases/4.1.6-test/SPT-VR-4.1.6-test.zip)
- [Samostatná DLL](releases/4.1.6-test/SPT-VR.dll)
- [Instalace a ruční test](TESTING.md)

Balíčky jsou ve složce `releases/4.1.6-test`, včetně SHA256 součtů a přehledu kompatibility.

Aktuální oprava **1.3.3.416** řeší přechod z výběru postavy do hlavního menu. VR nastavení menu se provádí po dokončení nativního `Init`/obnovení prostředí, se zachováním kontextu Unity. Chybějící ruce, ukazatel a části rigu se podle potřeby obnoví. Menu a jeho UI kamera mají zapnuté obě oči a canvasy používají aktuální kameru. Ovladače používají existující akce `LeftHandPose`/`RightHandPose` a tlačítka A/X.

Oprava 1.3.2.416 odstranila pád nativního JSON inicializátoru na pevných odkazech na volitelné mody. Výchozí DLL obsahuje základní VR port bez integrací EFT API, AmandsGraphics, DynamicMaps a WeaponCustomizer. Jejich zdroje zůstávají v repozitáři pro oddělený port.

Oprava JSON z 1.3.1.416 zůstává: před XR ověří načtení skutečných počtů předmětů a textový formát strany v požadavku na raid. Regresní test načítá **celou sestavenou DLL** s blokovanými volitelnými závislostmi a potom spouští skutečný nativní JSON inicializátor. Další test ověřuje načasování menu a zachování synchronizačního kontextu. Pokud už máte předchozí testovací ZIP, aktualizujte jen DLL podle [TESTING.md](TESTING.md). Nová verze ještě vyžaduje opakovaný test ve hře.

## Stav a optika

Kód se sestavuje proti dodaným knihovnám 4.1.6 pro `netstandard2.1`. Metadata test ověřuje všech 276 základních Harmony patchů: cíle, overloady, parametry a injektovaná pole. Dřívější počet 293 zahrnoval také 17 patchů volitelných integrací. Ověřuje také absenci FIKA a dalších volitelných assembly závislostí, zachování nativních aktualizací PIP/retiklu a ukládání zoomu do `SightComponent.ScopeZoomValue`.

PIP kamera, její render target a aktualizační smyčka zůstávají nativní. VR upravuje rozlišení, far clip, ovládání zoomu a zarovnání/ostrost retiklu. Kopie pro ostrý retikl vzniká v samostatné textuře po PIP renderu. Obecné SSAA úpravy targetu mají výjimku pro nativní optickou kameru.

Kompilace a metadata test nenahrazují test ve hře s headsetem. Postup je v [TESTING.md](TESTING.md).

## Sestavení

V připraveném cloudovém prostředí spusťte:

```bash
bash scripts/build.sh
```

Skript obnoví zamčené závislosti, sestaví Release, ověří kompatibilitu a spustí regresní test JSON nad celou výslednou DLL a dodanými herními assembly. Volitelné mody nejsou dostupné resolveru testu. Test nejprve reprodukuje původní chyby JSON a pak ověří stacky, stav předmětů, požadavky PMC/Scav a zachování existujících nastavení serializace. Vytvoří `artifacts/SPT-VR.dll`, `artifacts/SPT-VR-4.1.6-test.zip`, jejich SHA256 součty a `artifacts/json-regression.log`. ZIP vychází z původního balíčku 1.3.0 s novou DLL a bez FIKA. Referenční knihovny hry se do balíčku nepřidávají.

V cloudu JSON test používá původní nepřepsané Managed 4.1.6. Na jiném počítači lze jejich kořen zadat přes `SPT_VR_RAW_REFERENCES`; bez něj použije stejné reference jako sestavení.

Používá .NET SDK 8.0.426 a připravené publicized kopie knihoven 4.1.6 mimo checkout. Původní Managed 4.0.13 zůstává mimo checkout pro porovnávání a sestavení nezměněného upstreamu.

Na jiném počítači připravte publicized referenční adresář s podsložkami `Managed` (SPT 4.1.6) a `VRManaged` (SteamVR/XR knihovny původního balíčku). Použitý nástroj je BepInEx.AssemblyPublicizer.Cli 0.4.3, včetně compiler-generated členů. Publicized kopie jsou pouze pro kompilaci; neinstalujte je do hry.

```bash
export SPT_VR_REFERENCES=/absolutni/cesta/k/referencim
bash scripts/build.sh
```

Samostatné sestavení ve Windows:

```powershell
dotnet restore src/SPT-VR/TarkovVR.csproj --locked-mode -p:SptVrReferences=C:\SPT-VR-refs
dotnet build src/SPT-VR/TarkovVR.csproj -c Release --no-restore -p:SptVrReferences=C:\SPT-VR-refs
```
