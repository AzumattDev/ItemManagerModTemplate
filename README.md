# Item Manager

Can be used to easily add new items to Valheim. Will automatically add config options to your mod and sync the configuration from a server, if the mod is installed on the server as well.

## How to add items

Copy the asset bundle into your project and make sure to set it as an EmbeddedResource in the properties of the asset bundle.
Default path for the asset bundle is an `assets` directory, but you can override this.
This way, you don't have to distribute your assets with your mod. They will be embedded into your mods DLL.

### Merging the DLLs into your mod

Download the ItemManager.dll and the ServerSync.dll from the release section to the right.
Including the DLLs is best done via ILRepack (https://github.com/ravibpatel/ILRepack.Lib.MSBuild.Task). You can load this package (ILRepack.Lib.MSBuild.Task) from NuGet.

If you have installed ILRepack via NuGet, simply create a file named `ILRepack.targets` in your project and copy the following content into the file

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <Target Name="ILRepacker" AfterTargets="Build">
        <ItemGroup>
            <InputAssemblies Include="$(TargetPath)" />
            <InputAssemblies Include="$(OutputPath)\ItemManager.dll" />
            <InputAssemblies Include="$(OutputPath)\ServerSync.dll" />
        </ItemGroup>
        <ILRepack Parallel="true" DebugInfo="true" Internalize="true" InputAssemblies="@(InputAssemblies)" OutputFile="$(TargetPath)" TargetKind="SameAsPrimaryAssembly" LibraryPath="$(OutputPath)" />
    </Target>
</Project>
```

Make sure to set the ItemManager.dll and the ServerSync.dll in your project to "Copy to output directory" in the properties of the DLLs and to add a reference to it.
After that, simply add `using ItemManager;` to your mod and use the `Item` class, to add your items.

## Example project

This adds three different weapons from two different asset bundles. The `ironfang` asset bundle is in a directory called `IronFang`, while the `heroset` asset bundle is in a directory called `assets`.

```csharp
using BepInEx;
using ItemManager;

namespace Weapons
{
	[BepInPlugin(ModGUID, ModName, ModVersion)]
	public class Weapons : BaseUnityPlugin
	{
		private const string ModName = "Weapons";
		private const string ModVersion = "1.0";
		private const string ModGUID = "org.bepinex.plugins.weapons";
		
		public void Awake()
		{
			Item ironFangAxe = new Item("ironfang", "IronFangAxe", "IronFang");
			ironFangAxe.Name.English("Iron Fang Axe"); // You can use this to fix the display name in code
			ironFangAxe.Description.English("A sharp blade made of iron.");
			ironFangAxe.Name.German("Eisenzahnaxt"); // Or add translations for other languages
			ironFangAxe.Description.German("Eine sehr scharfe Axt, bestehend aus Eisen und Wolfszähnen.");
			ironFangAxe.Crafting.Add("MyAmazingCraftingStation", 3); // Custom crafting stations can be specified as a string
			ironFangAxe.MaximumRequiredStationLevel = 5; // Limits the crafting station level required to upgrade or repair the item to 5
			ironFangAxe.RequiredItems.Add("Iron", 120);
			ironFangAxe.RequiredItems.Add("WolfFang", 20);
			ironFangAxe.RequiredItems.Add("Silver", 40);
			ironFangAxe.RequiredUpgradeItems.Add("Iron", 20); // Upgrade requirements are per item, even if you craft two at the same time
			ironFangAxe.RequiredUpgradeItems.Add("Silver", 10); // 10 Silver: You need 10 silver for level 2, 20 silver for level 3, 30 silver for level 4
			ironFangAxe.CraftAmount = 2; // We really want to dual wield these
			
			GameObject axeVisual = ItemManager.PrefabManager.RegisterPrefab("ironfang", "axeVisual"); // If our axe has a special visual effect, like a glow, we can skip adding it to the ObjectDB this way
			GameObject axeSound = ItemManager.PrefabManager.RegisterPrefab("ironfang", "axeSound"); // Same for special sound effects
			
			Item heroShield = new("heroset", "HeroShield");
			heroShield["My first recipe"].Crafting.Add(CraftingTable.Workbench, 1); // You can add multiple recipes for the same item, by giving the recipe a name
			heroShield["My first recipe"].RequiredItems.Add("Wood", 10);
			heroShield["My first recipe"].RequiredItems.Add("Flint", 5);
			heroShield["My first recipe"].RequiredUpgradeItems.Add("Wood", 5);
			heroShield["My alternate recipe"].Crafting.Add(CraftingTable.Forge, 1); // And this is our second recipe then
			heroShield["My alternate recipe"].RequiredItems.Add("Bronze", 2);
			heroShield["My alternate recipe"].RequiredUpgradeItems.Add("Bronze", 1);
			heroShield.Snapshot(); // I don't have an icon for this item in my asset bundle, so I will let the ItemManager generate one automatically
			// The icon for the item will have the same rotation as the item in unity
			
			_ = new Conversion(heroBlade) // For some reason, we want to be able to put a hero shield into a smelter, to get a hero blade
			{
				Input = "HeroShield",
				Piece = ConversionPiece.Smelter
			};
		}
	}
}
```