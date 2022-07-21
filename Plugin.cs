using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ItemManager;
using ServerSync;
using UnityEngine;

namespace ItemManagerModTemplate
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class ItemManagerModTemplatePlugin : BaseUnityPlugin
    {
        internal const string ModName = "ItemManagerModTemplate";
        internal const string ModVersion = "1.0.0";
        internal const string Author = "{azumatt}";
        private const string ModGUID = Author + "." + ModName;
        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;

        internal static string ConnectionError = "";

        private readonly Harmony _harmony = new(ModGUID);

        public static readonly ManualLogSource ItemManagerModTemplateLogger =
            BepInEx.Logging.Logger.CreateLogSource(ModName);

        private static readonly ConfigSync ConfigSync = new(ModGUID)
            { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

        public void Awake()
        {
            _serverConfigLocked = config("General", "Force Server Config", true, "Force Server Config");
            _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);
            
            Item ironFangAxe = new("ironfang", "IronFangAxe");  // If your folder name is "assets" like the default. You would use this syntax.
            // Item ironFangAxe = new("ironfang", "IronFangAxe", "IronFang"); // If your asset is in a custom folder named IronFang and not the default "assets" folder. You would use this syntax.
             
            ironFangAxe.Name.English("Iron Fang Axe"); // You can use this to fix the display name in code
            ironFangAxe.Description.English("A sharp blade made of iron.");
            ironFangAxe.Name.German("Eisenzahnaxt"); // Or add translations for other languages
            ironFangAxe.Description.German("Eine sehr scharfe Axt, bestehend aus Eisen und Wolfszähnen.");
            ironFangAxe.Crafting.Add("MyAmazingCraftingStation",
                3); // Custom crafting stations can be specified as a string
            ironFangAxe.RequiredItems.Add("Iron", 120);
            ironFangAxe.RequiredItems.Add("WolfFang", 20);
            ironFangAxe.RequiredItems.Add("Silver", 40);
            ironFangAxe.RequiredUpgradeItems
                .Add("Iron", 20); // Upgrade requirements are per item, even if you craft two at the same time
            ironFangAxe.RequiredUpgradeItems.Add("Silver",
                10); // 10 Silver: You need 10 silver for level 2, 20 silver for level 3, 30 silver for level 4
            ironFangAxe.CraftAmount = 2; // We really want to dual wield these

            // You can optionally pass in a configuration option of your own to determine if the recipe is enabled or not. To use the example, uncomment both of the lines below.
            //_recipeIsActiveConfig = config("IronFangAxe", "IsRecipeEnabled",true, "Determines if the recipe is enabled for this prefab");
            //ironFangAxe.RecipeIsActive = _recipeIsActiveConfig;


            // If you have something that shouldn't go into the ObjectDB, like vfx or sfx that only need to be added to ZNetScene
            GameObject axeVisual =
                ItemManager.PrefabManager.RegisterPrefab(PrefabManager.RegisterAssetBundle("ironfang"), "axeVisual",
                    false); // If our axe has a special visual effect, like a glow, we can skip adding it to the ObjectDB this way
            GameObject axeSound =
                ItemManager.PrefabManager.RegisterPrefab(PrefabManager.RegisterAssetBundle("ironfang"), "axeSound",
                    false); // Same for special sound effects
            
            // You can also pass in a game object to register a prefab. Example blank GameObject created and registered below.
            GameObject blankGameObject = new GameObject();
            ItemManager.PrefabManager.RegisterPrefab(blankGameObject, true);


            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            SetupWatcher();
        }

        private void OnDestroy()
        {
            Config.Save();
        }

        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                ItemManagerModTemplateLogger.LogDebug("ReadConfigValues called");
                Config.Reload();
            }
            catch
            {
                ItemManagerModTemplateLogger.LogError($"There was an issue loading your {ConfigFileName}");
                ItemManagerModTemplateLogger.LogError("Please check your config entries for spelling and format!");
            }
        }


        #region ConfigOptions

        private static ConfigEntry<bool> _serverConfigLocked = null!;
        private static ConfigEntry<bool>? _recipeIsActiveConfig = null!;

        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription =
                new(
                    description.Description +
                    (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                    description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);
            //var configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description,
            bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }

        private class ConfigurationManagerAttributes
        {
            public bool? Browsable = false;
        }

        #endregion
    }
}