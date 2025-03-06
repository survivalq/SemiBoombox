using HarmonyLib;

namespace SemiBoombox.Patches
{
    [HarmonyPatch(typeof(PlayerAvatar), "Awake")]
    public class PlayerAvatarPatch
    {
        static void Postfix(PlayerAvatar __instance)
        {
            if (__instance.GetComponent<Boombox>() == null)
            {
                __instance.gameObject.AddComponent<Boombox>();
            }
        }
    }
}