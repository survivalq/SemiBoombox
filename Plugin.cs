using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SemiBoombox.Utils;

namespace SemiBoombox;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    private static Harmony _harmony;
        
    private void Awake()
    {
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        Task.Run(() => YoutubeDL.InitializeAsync().Wait());

        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        _harmony.PatchAll();
    }
}
