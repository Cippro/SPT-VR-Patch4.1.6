using Mono.Cecil;
using System.Text.RegularExpressions;
using System.Text.Json;

if (args.Length < 2) throw new ArgumentException("Usage: Compatibility <SPT-VR.dll> <Managed-directory> [extra-reference-directories]");
var types = new Dictionary<string, TypeDefinition>();
IEnumerable<TypeDefinition> Flatten(IEnumerable<TypeDefinition> source) {
    foreach (var t in source) { yield return t; foreach (var n in Flatten(t.NestedTypes)) yield return n; }
}
string Definition(string name) {
    while (Regex.IsMatch(name, "<[^<>]*>")) name = Regex.Replace(name, "<[^<>]*>", "");
    return name;
}
foreach (var directory in args.Skip(1)) {
    foreach (var path in Directory.GetFiles(directory, "*.dll")) {
        var assembly = AssemblyDefinition.ReadAssembly(path);
        foreach (var type in Flatten(assembly.MainModule.Types)) types.TryAdd(type.FullName, type);
    }
}
TypeDefinition? Resolve(TypeReference? type) => type != null && types.TryGetValue(Definition(type.FullName), out var t) ? t : null;
IEnumerable<MethodDefinition> Methods(TypeDefinition? type) {
    var seen = new HashSet<string>();
    while (type != null && seen.Add(type.FullName)) {
        foreach (var method in type.Methods) yield return method;
        type = Resolve(type.BaseType);
    }
}
bool FieldExists(TypeDefinition? type, string name) {
    var seen = new HashSet<string>();
    while (type != null && seen.Add(type.FullName)) {
        if (type.Fields.Any(f => f.Name == name)) return true;
        type = Resolve(type.BaseType);
    }
    return false;
}
var failures = new List<object>();
var bindings = new List<object>();
var resolver = new DefaultAssemblyResolver();
resolver.AddSearchDirectory(Path.GetDirectoryName(Path.GetFullPath(args[0]))!);
foreach (var directory in args.Skip(1)) resolver.AddSearchDirectory(directory);
var packages = Environment.GetEnvironmentVariable("NUGET_PACKAGES") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),".nuget","packages");
foreach(var (package,filename) in new[]{("harmonyx","0Harmony.dll"),("bepinex.baselib","BepInEx.dll")}) {
    if (Directory.Exists(Path.Combine(packages,package))) foreach (var dll in Directory.GetFiles(Path.Combine(packages,package),filename,SearchOption.AllDirectories)) resolver.AddSearchDirectory(Path.GetDirectoryName(dll)!);
}
using var mod = AssemblyDefinition.ReadAssembly(args[0], new ReaderParameters {AssemblyResolver=resolver});
foreach (var type in Flatten(mod.MainModule.Types)) types.TryAdd(type.FullName,type);
int bound = 0, dynamicCount = 0;
foreach (var type in Flatten(mod.MainModule.Types)) {
    foreach (var patch in type.Methods.Where(m => m.CustomAttributes.Any(a => new[] {"HarmonyPrefix", "HarmonyPostfix", "HarmonyTranspiler", "HarmonyFinalizer"}.Contains(a.AttributeType.Name)))) {
        var attributes = type.CustomAttributes.Concat(patch.CustomAttributes).Where(a => a.AttributeType.Name == "HarmonyPatch");
        TypeReference? targetRef = null; string? name = null; int methodType = 0; TypeReference[]? argumentTypes = null;
        foreach (var attr in attributes) foreach (var a in attr.ConstructorArguments) {
            if (a.Value is TypeReference tr) targetRef = tr;
            else if (a.Type.FullName == "System.String") name = a.Value as string;
            else if (a.Type.FullName == "HarmonyLib.MethodType") methodType = Convert.ToInt32(a.Value);
            else if (a.Type.FullName == "System.Type[]" && a.Value is CustomAttributeArgument[] arr) argumentTypes = arr.Select(v => (TypeReference)v.Value).ToArray();
        }
        string id = type.FullName + "." + patch.Name;
        if (targetRef == null || name == null && methodType < 3) {
            if (type.Methods.Any(m => m.Name is "TargetMethod" or "TargetMethods" || m.CustomAttributes.Any(a=>a.AttributeType.Name is "HarmonyTargetMethod" or "HarmonyTargetMethods"))) { dynamicCount++; continue; }
            failures.Add(new { patch = id, problem = "No target specified" }); continue;
        }
        var target = Resolve(targetRef);
        if (target == null) { failures.Add(new { patch=id, problem="Missing type", target=targetRef.FullName }); continue; }
        string methodName = methodType switch { 1 => "get_" + name, 2 => "set_" + name, 3 => ".ctor", 4 => ".cctor", _ => name! };
        var matches = Methods(target).Where(m => m.Name == methodName).ToArray();
        var declared=matches.Where(m=>m.DeclaringType.FullName==target.FullName).ToArray();
        if(declared.Length>0)matches=declared;
        string ActualType(TypeReference parameter) => parameter is GenericParameter gp && targetRef is GenericInstanceType gi && gp.Position < gi.GenericArguments.Count ? gi.GenericArguments[gp.Position].FullName : parameter.FullName;
        if (argumentTypes != null) matches = matches.Where(m => m.Parameters.Select(p => ActualType(p.ParameterType)).SequenceEqual(argumentTypes.Select(a => a.FullName))).ToArray();
        if (matches.Length == 0) { failures.Add(new { patch=id, problem="Missing method", target=targetRef.FullName, method=methodName }); continue; }
        if(argumentTypes==null && matches.Length>1) failures.Add(new{patch=id,problem="Ambiguous overload",target=targetRef.FullName,method=methodName,parameters=matches.Select(m=>m.Parameters.Select(p=>p.ParameterType.FullName))});
        bound++;
        bindings.Add(new {patch=id,target=targetRef.FullName,method=methodName});
        if (patch.CustomAttributes.Any(a=>a.AttributeType.Name=="HarmonyTranspiler")) continue;
        string Element(TypeReference t) => ActualType(t is ByReferenceType by ? by.ElementType : t);
        bool Assignable(string actual, string accepted) {
            if (actual==accepted || accepted=="System.Object") return true;
            var seen=new HashSet<string>();
            while(types.TryGetValue(Definition(actual),out var current) && seen.Add(current.FullName)) {
                if(current.Interfaces.Any(i=>i.InterfaceType.FullName==accepted)) return true;
                actual=current.BaseType?.FullName ?? "";
                if(actual==accepted)return true;
            }
            return false;
        }
        foreach (var parameter in patch.Parameters) {
            string p = parameter.Name;
            if (p.StartsWith("___")) {
                if (!FieldExists(target,p[3..])) failures.Add(new { patch=id, problem="Missing injected field", target=targetRef.FullName, member=p[3..] });
            } else if (p=="__instance") {
                if (!Assignable(targetRef.FullName,Element(parameter.ParameterType))) failures.Add(new{patch=id,problem="Incompatible instance",target=targetRef.FullName,member=p,expected=targetRef.FullName,actual=Element(parameter.ParameterType)});
            } else if (p=="__result") {
                if (!matches.Any(m=>Assignable(ActualType(m.ReturnType),Element(parameter.ParameterType)))) failures.Add(new{patch=id,problem="Incompatible result",target=targetRef.FullName,member=p,expected=matches.Select(m=>ActualType(m.ReturnType)),actual=Element(parameter.ParameterType)});
            } else if (p.StartsWith("__")) continue;
            else if (!matches.Any(m=>m.Parameters.Any(q=>q.Name==p))) {
                failures.Add(new { patch=id, problem="Unbound argument", target=targetRef.FullName, method=methodName, member=p, parameters=matches.Select(m=>m.Parameters.Select(q=>new {name=q.Name,type=q.ParameterType.FullName})) });
            } else if (!matches.Any(m=>m.Parameters.Any(q=>q.Name==p && Assignable(Element(q.ParameterType),Element(parameter.ParameterType))))) failures.Add(new{patch=id,problem="Incompatible argument",target=targetRef.FullName,method=methodName,member=p,expected=matches.SelectMany(m=>m.Parameters.Where(q=>q.Name==p).Select(q=>Element(q.ParameterType))),actual=Element(parameter.ParameterType)});
        }
    }
}
if(mod.MainModule.AssemblyReferences.Any(r => r.Name.Contains("Fika",StringComparison.OrdinalIgnoreCase))) failures.Add(new {problem="Unexpected FIKA assembly dependency"});
var optionalMods=new[]{"Aki.Reflection","AmandsGraphics","DynamicMaps","KmyTarkovConfiguration","Tyfon.WeaponCustomizer"};
foreach(var reference in mod.MainModule.AssemblyReferences.Where(r=>optionalMods.Contains(r.Name))) failures.Add(new{problem="Standalone DLL must not require optional mods during type discovery",assembly=reference.Name});
if(Flatten(mod.MainModule.Types).Any(t=>t.Namespace.StartsWith("TarkovVR.ModSupport.") && t.Name.EndsWith("Support"))) failures.Add(new{problem="Legacy optional integration types must not be compiled into the standalone DLL"});
var plugin = mod.MainModule.Types.Single(t=>t.FullName=="TarkovVR.Plugin");
var pluginInfo = plugin.CustomAttributes.Single(a=>a.AttributeType.Name=="BepInPlugin");
if((string)pluginInfo.ConstructorArguments[2].Value!=mod.Name.Version.ToString()) failures.Add(new{problem="Plugin and assembly versions disagree",plugin=pluginInfo.ConstructorArguments[2].Value,assembly=mod.Name.Version.ToString()});
foreach(var dependency in new[]{"com.SPT.core","com.SPT.singleplayer"}) {
    if(!plugin.CustomAttributes.Any(a=>a.AttributeType.Name=="BepInDependency" && (string)a.ConstructorArguments[0].Value==dependency)) failures.Add(new{problem="SPT must load before VR initialization",dependency});
}
var startupCalls=plugin.Methods.Single(m=>m.Name=="Awake").Body.Instructions.Where(i=>i.Operand is MethodReference).Select(i=>(MethodReference)i.Operand).ToList();
var jsonStartup=startupCalls.FindIndex(m=>m.DeclaringType.FullName=="TarkovVR.Source.Compatibility.GameJsonBootstrap" && m.Name=="Initialize");
var vrStartup=startupCalls.FindIndex(m=>m.DeclaringType.FullName==plugin.FullName && m.Name=="InitializeVR");
if(jsonStartup<0 || vrStartup<0 || jsonStartup>=vrStartup) failures.Add(new{problem="Game JSON defaults must be initialized and checked before XR"});
if(!types.TryGetValue("Newtonsoft.Json.UnityConverters.UnityConverterInitializer",out var jsonInitializer) || !jsonInitializer.Methods.Any(m=>m.Name=="Init" && m.IsStatic && m.Parameters.Count==0)) failures.Add(new{problem="Native Unity JSON initializer is unavailable"});
var menu=mod.MainModule.Types.Single(t=>t.FullName=="TarkovVR.Patches.Misc.MenuPatches");
foreach(var name in new[]{"AddAndFixMenuVRCam","PositionMenuPropsAfterRefresh"}) {
    var method=menu.Methods.Single(m=>m.Name==name);
    if(!method.Parameters.Any(p=>p.Name=="__result" && p.ParameterType.FullName=="System.Threading.Tasks.Task&") || !method.Body.Instructions.Any(i=>i.Operand is MethodReference m && m.DeclaringType.Name=="NativeTaskContinuation" && m.Name=="AfterCompletion")) failures.Add(new{problem="VR menu setup must await the native task",method=name});
}
var menuSetup=menu.Methods.Single(m=>m.Name=="InitializeMainMenuVR").Body.Instructions.ToList();
int environmentAssignment=menuSetup.FindIndex(i=>i.OpCode.Code==Mono.Cecil.Cil.Code.Stsfld && i.Operand is FieldReference f && f.Name=="environmentUi");
int cameraSetup=menuSetup.FindIndex(i=>i.Operand is MethodReference m && m.Name=="FixMainMenuCamera");
if(environmentAssignment<0 || cameraSetup<0 || environmentAssignment>=cameraSetup) failures.Add(new{problem="Assign the current environment before configuring the menu camera"});
if(!menu.Methods.Single(m=>m.Name=="EnsureMenuRig").Body.Instructions.Any(i=>i.Operand is MethodReference m && m.Name=="SpawnHands")) failures.Add(new{problem="The menu rig must restore missing controller objects"});
if(menu.Methods.Single(m=>m.Name=="FixMainMenuCamera").Body.Instructions.Count(i=>i.Operand is MethodReference m && m.Name=="set_stereoTargetEye")<2) failures.Add(new{problem="Menu and UI cameras must both target the headset"});
var manager=mod.MainModule.Types.Single(t=>t.FullName=="TarkovVR.Source.Player.VRManager.MenuVRManager");
foreach(var name in new[]{"OnDisable","OnDestroy"}) {
    if(!manager.Methods.Single(m=>m.Name==name).Body.Instructions.Any(i=>i.Operand is MethodReference m && m.Name=="RemovePoseListeners")) failures.Add(new{problem="Menu controller listeners must be detached during teardown",method=name});
}
var weapon = mod.MainModule.Types.First(t=>t.FullName=="TarkovVR.Patches.Core.Player.WeaponPatches");
var ui=mod.MainModule.Types.Single(t=>t.FullName=="TarkovVR.Patches.UI.UIPatches");
var openInventory=ui.Methods.Single(m=>m.Name=="HandleOpenInventory").Body.Instructions;
if(!openInventory.Any(i=>i.Operand is MethodReference m && m.Name=="ShowUiScreens" && i.Previous?.Operand is FieldReference f && f.Name=="VRCam")) failures.Add(new{problem="Raid inventory must bind its live FPS UI camera"});
if(!ui.Methods.Single(m=>m.Name=="ShowUiScreens").Body.Instructions.Any(i=>i.Operand is MethodReference m && m.Name=="BindUiCamera")) failures.Add(new{problem="Opening UI must update the canvas interaction camera"});
var interacter=mod.MainModule.Types.Single(t=>t.FullName=="TarkovVR.Source.UI.VRUIInteracter");
var raycast=interacter.Methods.Single(m=>m.Name=="RaycastFindHit").Body.Instructions;
foreach(var name in new[]{"get_rootCanvas","get_worldCamera","set_pointerCurrentRaycast"}) if(!raycast.Any(i=>i.Operand is MethodReference m && m.Name==name)) failures.Add(new{problem="VR UI raycast must retain native camera and raycast metadata",member=name});
var confirmHandler=interacter.Methods.Single(m=>m.Name=="HandleConfirm").Body.Instructions;
foreach(var name in new[]{"set_pointerPressRaycast","set_pointerPress","set_pressPosition"}) if(!confirmHandler.Any(i=>i.Operand is MethodReference m && m.Name==name)) failures.Add(new{problem="VR confirm must retain native pointer press metadata",member=name});
var pointerUpdate=mod.MainModule.Types.Single(t=>t.FullName=="TarkovVR.Source.Misc.LaserPointer").Methods.Single(m=>m.Name=="Update").Body.Instructions;
if(pointerUpdate.Any(i=>i.Operand is FieldReference f && f.Name=="clickColor") || pointerUpdate.Any(i=>i.Operand is MethodReference m && m.Name=="GetState")) failures.Add(new{problem="Confirm must not turn the laser green or widen it"});
var height=mod.MainModule.Types.Single(t=>t.FullName=="TarkovVR.Source.UI.MenuHeightCalibration").Methods.Single(m=>m.Name=="LateUpdate").Body.Instructions;
foreach(var name in new[]{"get_isValid","get_stereoEnabled","GetStereoViewMatrix"}) if(!height.Any(i=>i.Operand is MethodReference m && m.Name==name)) failures.Add(new{problem="Menu height must be calibrated from a valid rendered headset pose",member=name});
foreach(var name in new[]{"AlignHeadsetReticle","ApplyHeadsetOpticLimits"}) {
    var method=weapon.Methods.FirstOrDefault(m=>m.Name==name);
    if(method==null || !method.CustomAttributes.Any(a=>a.AttributeType.Name=="HarmonyPostfix") || method.ReturnType.FullName!="System.Void") failures.Add(new{problem="Native PIP/reticle update must be retained",method=name});
}
var zoom=Flatten(mod.MainModule.Types).Single(t=>t.FullName=="TarkovVR.Source.Weapons.VROpticController").Methods.Single(m=>m.Name=="ApplyZoomState");
if(!zoom.Body.Instructions.Any(i=>i.OpCode.Code==Mono.Cecil.Cil.Code.Stfld && i.Operand is FieldReference f && f.DeclaringType.Name=="SightComponent" && f.Name=="ScopeZoomValue")) failures.Add(new{problem="VR zoom must persist through native SightComponent state"});
foreach(var method in Flatten(mod.MainModule.Types).SelectMany(t=>t.Methods).Where(m=>m.HasBody)) {
    if(method.Body.Instructions.Any(i=>i.Operand is MethodReference called && called.DeclaringType.FullName=="UnityEngine.Camera" && called.Name=="set_targetTexture") && !method.Body.Instructions.Any(i=>i.Operand is MethodReference guard && guard.Name=="IsNativeOpticCamera")) failures.Add(new{problem="A camera-target override lacks the native PIP exclusion",method=method.FullName});
}
Console.WriteLine(JsonSerializer.Serialize(new { bound, dynamicCount, bindings, failures }, new JsonSerializerOptions {WriteIndented=true}));
return failures.Count==0 ? 0 : 1;
