using System;
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
    
    // We need to load _after_ all other plugins are loaded - on Awake() all dependent mods are guaranteed to _not_ be loaded!
    private void Start()
    {
        _modListGO = new GameObject("Silksong.ModList");
        _modListDraw = _modListGO.AddComponent<ModListDraw>();
        DontDestroyOnLoad(_modListGO);
        
        GenerateModList();
        Logger.LogInfo($"Plugins: {_loadedPlugins}");
        Logger.LogInfo($"Dependents: {_loadedDependents}");
    }

    private void GenerateModList()
    {
        var infos = BepInEx.Bootstrap.Chainloader.PluginInfos;
        Logger.LogInfo($"Plugins: {infos.Count}");
        _loadedPlugins.Clear();
        _loadedDependents.Clear();
        foreach (var (k, v) in infos)
        {
            Logger.LogInfo($"Loading plugin {v}");
            if (v == null)
            {
                Logger.LogInfo($"Plugin {k} has no PluginInfo, ignoring...");
                continue;
            }
            try
            {
                var attr = (Attribute.GetCustomAttribute(v.Instance.GetType(), typeof(BepInPlugin)) as BepInPlugin)!;
                Logger.LogInfo($"Attr {attr.GUID}");
                Logger.LogInfo($"Dependencies {v.Dependencies}");
                var dependency = v.Dependencies.FirstOrDefault(x =>
                    x.Flags.HasFlag(BepInDependency.DependencyFlags.HardDependency) &&
                    x.DependencyGUID == Constants.Guid);
                if (dependency != null)
                {
                    _loadedDependents.Add(attr);
                }
                _loadedPlugins.Add(attr);
                
                Logger.LogInfo($"{v.Dependencies}");
                
            }
            catch (AmbiguousMatchException)
            {
                Logger.LogWarning($"Mod {k} has multiple BepinPlugin attributes, ignoring...");  //TODO: can this actually happen, or is this unnecessary?
            }
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
