using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ClickToDrop;

[BepInPlugin(ModGUID, ModName, ModVersion)]
public class ClickToDropPlugin : BaseUnityPlugin
{
    internal const string ModName = "ClickToDrop";
    internal const string ModVersion = "1.0.2";
    internal const string Author = "Azumatt";
    private const string ModGUID = Author + "." + ModName;
    private static string ConfigFileName = ModGUID + ".cfg";
    private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
    private readonly Harmony _harmony = new(ModGUID);
    public static readonly ManualLogSource ClickToDropLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);
    private static ConfigEntry<KeyCode> modifierKey = null!;

    public void Awake()
    {
        modifierKey = Config.Bind("1 - General", "Modifier Key", KeyCode.LeftControl, "The key that must be held down to drop items");
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
            ClickToDropLogger.LogDebug("ReadConfigValues called");
            Config.Reload();
        }
        catch
        {
            ClickToDropLogger.LogError($"There was an issue loading your {ConfigFileName}");
            ClickToDropLogger.LogError("Please check your config entries for spelling and format!");
        }
    }
}

[HarmonyPatch(typeof(UIInventory), nameof(UIInventory.LeftClickOnBackpackOrChestSlot))]
static class UIInventoryAwakePatch
{
    static void Prefix(UIInventory __instance, PointerEventData eventData, Item itemInSlot)
    {
        if (!Input.GetKey(KeyCode.LeftControl)) return;
        if (!itemInSlot)
            return;
        if (__instance._currentOperatingChestStorage)
            return;
        __instance.discardZone.transform.Find("BG (3)").GetComponent<UIInventoryDiscardItem>().TryDropItem(itemInSlot);
        __instance.Refresh();
    }
}