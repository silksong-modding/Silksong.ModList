using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using UnityEngine;

namespace Silksong.Modlist;

public static class Constants
{
    public const string Guid = "org.silksong-modding.modlist";
}

[BepInAutoPlugin(id: Constants.Guid)]
public partial class ModlistPlugin : BaseUnityPlugin
{
    private List<BepInPlugin> _loadedPlugins = [];
    private List<BepInPlugin> _loadedDependents = [];

    private GameObject? _modListGO;
    private ModListDraw? _modListDraw;

    private static string CombinedAsString<T>(IEnumerable<T> e)
    {
        return $"[{string.Join(", ", e)}]";
    }
    
    // We need to load _after_ all other plugins are loaded - on Awake() all dependent mods are guaranteed to _not_ be loaded!
    private void Start()
    {
        _modListGO = new GameObject("Silksong.ModList");
        _modListDraw = _modListGO.AddComponent<ModListDraw>();
        DontDestroyOnLoad(_modListGO);
        
        GenerateModList();
        Logger.LogInfo($"Plugins: {CombinedAsString(_loadedPlugins.Select(plugin => plugin.Name))}");
        Logger.LogInfo($"Dependents: {CombinedAsString(_loadedDependents.Select(dep => dep.Name))}");
    }

    private void GenerateModList()
    {
        var infos = BepInEx.Bootstrap.Chainloader.PluginInfos;
        Logger.LogInfo($"Plugins: {infos.Count}");
        _loadedPlugins.Clear();
        _loadedDependents.Clear();
        foreach (var (pluginId, pluginInfo) in infos)
        {
            Logger.LogInfo($"Loading plugin {pluginInfo}");
            if (pluginInfo == null)
            {
                Logger.LogInfo($"Plugin {pluginId} has no PluginInfo, ignoring...");
                continue;
            }

            var metadata = pluginInfo.Metadata;
            Logger.LogInfo($"Id: {pluginId}");
            var dependency = pluginInfo.Dependencies.FirstOrDefault(x =>
                x.Flags.HasFlag(BepInDependency.DependencyFlags.HardDependency) &&
                x.DependencyGUID == Constants.Guid);
            if (dependency != null)
            {
                _loadedDependents.Add(metadata);
            }
            _loadedPlugins.Add(metadata);
                
            Logger.LogInfo($"Dependencies: {CombinedAsString(pluginInfo.Dependencies.Select(dep => dep.DependencyGUID))}");
        }

        _modListDraw!.drawString = GetMinorList();
    }
    
    // TODO: implement major once we move to better rendering / mods menu
    private string GetMajorList()
    {
        return string.Join("\n", _loadedDependents.Select((plugin) => $"{plugin.GUID}"));
    }

    private string GetMinorList()
    {
        return string.Join("\n", _loadedPlugins.Select((plugin) => $"{plugin.Name}: {plugin.Version}"));
    }
}
