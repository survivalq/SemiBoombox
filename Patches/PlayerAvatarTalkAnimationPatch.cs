using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

// No idea how stable this thing is, but it's a cool idea.
// This patch will make the player's avatar head bob up and down when they're playing music.

namespace SemiBoombox.Patches
{
    [HarmonyPatch(typeof(PlayerAvatarTalkAnimation), "Update")]
    public class PlayerAvatarTalkAnimationPatch
    {
        private static Dictionary<int, Boombox> _boomboxCache = [];

        static void Postfix(PlayerAvatarTalkAnimation __instance)
        {
            if (__instance == null || __instance.objectToRotate == null || __instance.playerAvatar == null)
                return;

            if (__instance.playerAvatar.photonView?.Owner == null)
                return;

            int actorNumber = __instance.playerAvatar.photonView.Owner.ActorNumber;

            Boombox boombox = FindBoomboxForPlayer(actorNumber);
            if (boombox == null || boombox.audioSource == null || !boombox.audioSource.isPlaying)
                return;

            float musicLoudness = GetAudioLoudness(boombox.audioSource);
            if (musicLoudness < 0.005f)
                return;

            float targetAngle = Mathf.Lerp(0f, -__instance.rotationMaxAngle, musicLoudness * 4f);
            __instance.objectToRotate.transform.localRotation = Quaternion.Slerp(
                __instance.objectToRotate.transform.localRotation,
                Quaternion.Euler(targetAngle, 0f, 0f),
                100f * Time.deltaTime
            );
        }

        private static Boombox FindBoomboxForPlayer(int actorNumber)
        {
            if (_boomboxCache.TryGetValue(actorNumber, out Boombox cachedBoombox) &&
                cachedBoombox != null && cachedBoombox.gameObject != null)
            {
                return cachedBoombox;
            }

            foreach (Boombox boombox in Object.FindObjectsOfType<Boombox>())
            {
                if (boombox?.photonView?.Owner?.ActorNumber == actorNumber)
                {
                    _boomboxCache[actorNumber] = boombox;
                    return boombox;
                }
            }

            return null;
        }

        private static float GetAudioLoudness(AudioSource source)
        {
            float[] sampleData = new float[1024];
            source.GetOutputData(sampleData, 0);

            float loudness = 0f;
            foreach (float sample in sampleData)
            {
                loudness += Mathf.Abs(sample);
            }

            return loudness / sampleData.Length;
        }
    }
}