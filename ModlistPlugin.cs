using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Mono.Cecil;
using UnityEngine;

namespace Silksong.Modlist;

public static class Constants
{
    public const string Guid = "org.silksong-modding.modlist";
}

internal enum DisplayMode
{
    Full = 0,
    Reduced = 1,
    Hidden = 2
}

internal record struct PatcherMeta
{
    public string AsmName;
    public string AsmVersion;
    public string TypeName;
}

[BepInAutoPlugin(id: Constants.Guid)]
public partial class ModlistPlugin : BaseUnityPlugin
{
    private List<BepInPlugin> _majors = [];
    private List<BepInPlugin> _minors = [];
    private List<PatcherMeta> _patchers = [];

    public string GameVersion => global::Constants.GetConstantValue<string>("GAME_VERSION");
    // consts are inlined at compile time; GetConstantValue allows a runtime check to get the accurate value.

    private GameObject? _modListGO;
    private ModListDraw? _modListDraw;

    public static ModlistPlugin Instance { get; private set; }

    internal ConfigEntry<DisplayMode> DisplayMode;
    internal ConfigEntry<bool> LogFileTree;

    private void Awake()
    {
        Instance = this;
        DisplayMode = Config.Bind("General",
            "DisplayMode",
            Modlist.DisplayMode.Full,
            "How to display the top-left mod list. The mod count on the title screen will always be visible.");

        LogFileTree = Config.Bind("General",
            "LogModFiles",
            true,
            "Whether to include a filetree in LogOutput.log for troubleshooting."
        );

        _patchers = GetPatchers(); // This needs to run only once, as patching context is over by the time plugins are loaded.
    }

    // We need to load _after_ all other plugins are loaded - on Awake() all dependent mods are guaranteed to _not_ be loaded!
    private void Start()
    {
        Harmony.CreateAndPatchAll(typeof(ModlistPatches));

        _modListGO = new GameObject("Silksong.ModList");
        _modListDraw = _modListGO.AddComponent<ModListDraw>();
        DontDestroyOnLoad(_modListGO);

        DisplayMode.SettingChanged += (sender, args) => RefreshModList();
        GenerateModList();
        
        Logger.LogInfo(LogModLists());

        if (LogFileTree.Value)
        {
            Logger.LogInfo("Plugins Directory:\n" + FileTree.RenderTree(Paths.PluginPath));
            Logger.LogInfo("Patchers Directory:\n" + FileTree.RenderTree(Paths.PatcherPluginPath));
        }
    }

    private string LogModLists()
    {
        var builder = new StringBuilder($"\n\n======== Modlist ========\nSilksong version: {GameVersion}\n");
        
        builder.AppendLine($"PLUGINS: {_majors.Count + _minors.Count} ({_majors.Count} depend on Modlist)");
        foreach (var x in _majors.OrderBy(x => x.Name)) 
            builder.AppendLine($"[{x.Name}".PadRight(20) + $": {x.Version}] {x.GUID}");
        foreach (var x in _minors.OrderBy(x => x.Name)) 
            builder.AppendLine($"[{x.Name}".PadRight(20) + $": {x.Version}] {x.GUID}");
        builder.AppendLine($"PATCHERS: {_patchers.Count}");
        foreach (var x in _patchers.OrderBy(x => x.AsmName)) 
            builder.AppendLine($"[{x.AsmName}".PadRight(40) + $": {x.AsmVersion}]::{x.TypeName}");

        builder.AppendLine("=========================");
        return builder.ToString();
    }


    private void RefreshModList() // TODO: duplicate stub
    {
        GenerateModList(); 
    }

    private void GenerateModList()
    {
        (_majors, _minors) = GetPlugins();
        
        _modListDraw!.enabled = DisplayMode.Value != Modlist.DisplayMode.Hidden;
        _modListDraw!.drawString = GetListString();
    }

    private string GetListString()
    {
        switch (DisplayMode.Value)
        {
            case Modlist.DisplayMode.Hidden:
                return "";
            case Modlist.DisplayMode.Reduced:
                return $"{ModListPrefix}\n{MajorListJoined}\n+ {_minors.Count} others & {_patchers.Count} patchers";
            case Modlist.DisplayMode.Full:
            default:
                return $"{ModListPrefix}\n{PatcherListJoined}\n{MajorListJoined}\n{MinorListJoined}";
        }
    }

    private string ModListPrefix => $"Silksong v{GameVersion} (Modlist {Version})";
    
    
    public List<string> MajorList => _majors.Select(plugin => $"{plugin.Name}: {plugin.Version}").ToList();
    public List<string> MinorList => _minors.Select(plugin => $"{plugin.Name}: {plugin.Version}").ToList();
    public List<string> PatcherList => _patchers.Select(patcher => $"{patcher.AsmName}: {patcher.AsmVersion} (patcher)").ToList();

    private string MajorListJoined => string.Join("\n", MajorList);
    private string MinorListJoined => string.Join("\n", MinorList);
    private string PatcherListJoined => string.Join("\n", PatcherList);
    
    internal string MajorNamesJoined => string.Join(", ", _majors.Select(plugin => $"{plugin.Name}"));

    public int ModCount => _majors.Count + _minors.Count + _patchers.Count + 1; // add 1 for Modlist itself
    
    #region Discovery
    
    /// <summary>
    /// Hacky solution reimplementing BepInEx patcher loading to discover preloader patchers & their assembly versions.
    ///
    /// NB: does not correctly emulate behaviour where a patcher is prevented from executing by another patcher.
    /// We believe this behaviour is vanishingly rare & potential workarounds would be even hackier.
    /// </summary>
    private List<PatcherMeta> GetPatchers()
    {
        List<PatcherMeta> metas = [];
        
        foreach (string dllPath in Directory.GetFiles(Path.GetFullPath(Paths.PatcherPluginPath), "*.dll",
                     SearchOption.AllDirectories))
        {
            try
            {
                var asm = AssemblyDefinition.ReadAssembly(dllPath, BepInEx.Bootstrap.TypeLoader.ReaderParameters);
                var patchersInModule = asm.MainModule.Types.Where((typeDef) =>
                    {
                        // Definition based on BepInEx.Preloader.Patching.AssemblyPatcher
                        if (typeDef.IsInterface || typeDef.IsAbstract && !typeDef.IsSealed)
                            return false;

                        var targetDlls = typeDef.Methods.FirstOrDefault(
                            m => m.Name.Equals("get_TargetDLLs", StringComparison.InvariantCultureIgnoreCase) &&
                                 m.IsPublic && m.IsStatic);

                        if (targetDlls == null ||
                            targetDlls.ReturnType.FullName != "System.Collections.Generic.IEnumerable`1<System.String>")
                            return false;

                        var patch = typeDef.Methods.FirstOrDefault(
                            m => m.Name.Equals("Patch") && m.IsPublic && m.IsStatic && 
                                 m.ReturnType.FullName == "System.Void" && m.Parameters.Count == 1 &&
                                 (m.Parameters[0].ParameterType.FullName == "Mono.Cecil.AssemblyDefinition&" ||
                                  m.Parameters[0].ParameterType.FullName == "Mono.Cecil.AssemblyDefinition"));

                        return patch != null;
                    })
                    .Select(typeDef => 
                        new PatcherMeta() { AsmName = asm.Name.Name, AsmVersion = asm.Name.Version.ToString(), TypeName = typeDef.Name });
                metas.AddRange(patchersInModule);
            } catch (BadImageFormatException e)
            {
                Logger.LogDebug($"Skipping loading {dllPath} because it's not a valid .NET assembly. Full error: {e.Message}");
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
            }
        }
        
        return metas;
    }

    private (List<BepInPlugin>, List<BepInPlugin>) GetPlugins()
    {
        Dictionary<string, PluginInfo> infos = BepInEx.Bootstrap.Chainloader.PluginInfos!;

        List<BepInPlugin> majors = [];
        List<BepInPlugin> minors = [];

        foreach (var (pluginId, pluginInfo) in infos)
        {
            if (pluginInfo == null)
            {
                Logger.LogWarning($"Plugin {pluginId} has no PluginInfo, ignoring...");
                continue;
            }

            if (pluginId == Constants.Guid)
            {
                // exclude self, we explicitly add ourselves in the prefix
                continue;
            }

            var metadata = (Attribute.GetCustomAttribute(pluginInfo.Instance.GetType(), typeof(BepInPlugin)) as BepInPlugin)!;
            var dependency = pluginInfo.Dependencies.FirstOrDefault(x =>
                x.Flags.HasFlag(BepInDependency.DependencyFlags.HardDependency) &&
                x.DependencyGUID == Constants.Guid);
            if (dependency != null)
            {
                majors.Add(metadata);
            }
            else
            {
                minors.Add(metadata);
            }
        }
        majors.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        minors.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        return (majors, minors);
    }
    
    #endregion
}

// ReSharper disable InconsistentNaming
public static partial class ModlistPatches
{
    [HarmonyPatch(typeof(SetVersionNumber), nameof(SetVersionNumber.Start))]
    [HarmonyPostfix]
    public static void SetVersionNumber_Start(SetVersionNumber __instance)
    {
        var currentText = __instance.textUi.text;

        currentText += $" | {ModlistPlugin.Instance.ModCount} Mods\n{ModlistPlugin.Instance.MajorNamesJoined}";
        __instance.textUi.text = currentText;
    }
}
