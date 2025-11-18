using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace Silksong.Modlist;

public static class Constants
{
    public const string Guid = "org.silksong-modding.modlist";
}

[BepInAutoPlugin(id: Constants.Guid)]
public partial class ModlistPlugin : BaseUnityPlugin
{
    private List<BepInPlugin> _loadedMinors = [];
    private List<BepInPlugin> _loadedMajors = [];

    private GameObject? _modListGO;
    private ModListDraw? _modListDraw;

    public static ModlistPlugin Instance  { get; private set; }
    private Harmony _harmony;

    internal ConfigEntry<bool> HideMinorList;

    private void Awake()
    {
        Instance = this;
        HideMinorList = Config.Bind("General", 
            "HideMinorList", 
            false, 
            "Whether to hide the modlist. Note that some mods will always display in the modlist for moderation reasons.");
        Logger.LogInfo($"Silksong version: {(global::Constants.GAME_VERSION)}");
    }

    // We need to load _after_ all other plugins are loaded - on Awake() all dependent mods are guaranteed to _not_ be loaded!
    private void Start()
    {
        _harmony = Harmony.CreateAndPatchAll(typeof(ModlistPatches));
        
        _modListGO = new GameObject("Silksong.ModList");
        _modListDraw = _modListGO.AddComponent<ModListDraw>();
        DontDestroyOnLoad(_modListGO);

        GenerateModList();

        HideMinorList.SettingChanged += (sender, args) => GenerateModList();
    }

    private void GenerateModList()
    {
        Dictionary<string, PluginInfo> infos = BepInEx.Bootstrap.Chainloader.PluginInfos!;
        Logger.LogDebug($"Plugins: {infos.Count}");
        _loadedMinors.Clear();
        _loadedMajors.Clear();
        foreach (var (pluginId, pluginInfo) in infos)
        {
            if (pluginInfo == null)
            {
                Logger.LogWarning($"Plugin {pluginId} has no PluginInfo, ignoring...");
                continue;
            }
            
            var metadata = (Attribute.GetCustomAttribute(pluginInfo.Instance.GetType(), typeof(BepInPlugin)) as BepInPlugin)!;
            Logger.LogDebug($"GUID {metadata.GUID} w/ {pluginInfo.Dependencies.Count()} dependencies");
            var dependency = pluginInfo.Dependencies.FirstOrDefault(x =>
                x.Flags.HasFlag(BepInDependency.DependencyFlags.HardDependency) &&
                x.DependencyGUID == Constants.Guid);
            if (dependency != null)
            {
                _loadedMajors.Add(metadata);
            }
            else
            {
                _loadedMinors.Add(metadata);
            }
        }

        _modListDraw!.drawString = GetListString();
    }

    private string GetListString()
    {
        var totalList = GetMajorList();
        if (HideMinorList.Value)
        {
            // If major list is empty, we can completely hide the modlist other than the patch text
            if (totalList.Count == 0)
            {
                return ""; 
            }
            // Otherwise display majors & the minor count
            totalList.Add($"& {GetMinorList().Count} others");
        }
        else
        {
            totalList.AddRange(GetMinorList());
        }
        return string.Join("\n", totalList);
    }

    private List<string> GetMajorList()
    {
        return _loadedMajors.Select(plugin => $"{plugin.Name}: {plugin.Version}").ToList();
    }

    private List<string> GetMinorList()
    {
        return _loadedMinors.Select(plugin => $"{plugin.Name}: {plugin.Version}").ToList();
    }

    public int GetModCount()
    {
        return _loadedMajors.Count + _loadedMinors.Count;
    }

    public string GetMajorString()
    {
        return string.Join(", ", _loadedMajors.Select(plugin => $"{plugin.Name}"));
    }
}

// ReSharper disable InconsistentNaming
public static partial class ModlistPatches
{
    [HarmonyPatch(typeof(SetVersionNumber), nameof(SetVersionNumber.Start))]
    [HarmonyPostfix]
    public static void SetVersionNumber_Start(SetVersionNumber __instance)
    {
        var currentText = __instance.textUi.text;

        currentText += $" | {ModlistPlugin.Instance.GetModCount()} Mods\n{ModlistPlugin.Instance.GetMajorString()}";
        __instance.textUi.text = currentText;
    }
}
