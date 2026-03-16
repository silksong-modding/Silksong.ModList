# Silksong.Modlist

A modlist for Silksong. That's it!

## For Users

Configuration can be changed in ModMenu, BepInExConfigurationManager or by editing `BepInEx/config/org.silksong-modding.modlist.cfg`.
- `DisplayMode` sets how the modlist is shown on the main menu:
  - `Full` always shows all mods, `Reduced` shows a mod count & any hard-dependent mods, `Hidden` hides the modlist.
  - On both `Full` & `Reduced` settings, the reduced modlist is shown when paused ingame.
- `LogModFiles` sets whether to append a reduced file tree to your logs, useful for troubleshooting.

## For Developers

Mods used by speedrunners are encouraged to depend on ModList to force being shown in the patch text, to pose an additional obstruction to cheating in speedruns.

To do so, add a dependency on Modlist to the plugin definition (and update `thunderstore.toml` appropriately):

```csharp
[BepInAutoPlugin(id: "your_mod's_guid")]
[BepInDependency("org.silksong-modding.modlist")]  // Add this line!
public class YourPlugin : BaseUnityPlugin 
{
    ...
}
```
```toml
# dependencies are specified in the format AuthorName-PackageName = "version". You should always have at least BepInExPack_Silksong.
[package.dependencies]
BepInEx-BepInExPack_Silksong = "5.4.2304"
silksong_modding-Modlist = "0.3.0"  # Add this line!
```
Note that this will prevent your mod from loading without Modlist enabled - this is intentional.
