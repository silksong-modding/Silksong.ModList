# Silksong.Modlist

A modlist for Silksong. That's it!

## For Developers

Mods that can be used for cheating in a difficult-to-detect way are encouraged to hard-depend on Silksong.Modlist to add an extra obstruction to cheating in speedruns.

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
silksong_modding-Modlist = "0.2.0"  # Add this line!
```
Note that this will prevent your mod from loading without Modlist enabled - this is intentional.
