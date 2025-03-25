using Photon.Pun;
using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using SemiBoombox.Utils;
using System;

namespace SemiBoombox
{
    public class Boombox : MonoBehaviour
    {
        public PhotonView photonView;
        public AudioSource audioSource;

        // Caches the AudioClips in memory using the URL as the key.
        private static Dictionary<string, AudioClip> downloadedClips = [];

        // Tracks which players have reported they are ready.
        private static Dictionary<string, HashSet<int>> downloadsReady = [];

        // Store name -> URL mapping
        public static Dictionary<string, string> downloadedSongs = [];

        private bool isDownloading = false;
        private static Dictionary<int, Boombox> _boomboxCache = [];
        public static Dictionary<int, Boombox> BoomboxCache
        {
            get
            {
                if (IsCacheInvalid())
                {
                    RefreshBoomboxCache();
                }
                return _boomboxCache;
            }
        }

        private void Awake()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
            audioSource.playOnAwake = false;
            audioSource.minDistance = 1f;
            audioSource.maxDistance = 30f;
            audioSource.rolloffMode = AudioRolloffMode.Custom;
            
            AnimationCurve curve = new();
            curve.AddKey(1f, 1f);
            curve.AddKey(30f, 0f);
            audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, curve);

            photonView = GetComponent<PhotonView>();

            // check if the photoView is working
            if (photonView == null)
            {
                Debug.LogError("PhotonView not found on Boombox object.");
                return;
            }

            if (photonView.IsMine)
            {
                audioSource.volume = 0.1f;
                gameObject.AddComponent<BoomboxUI>();
            }
            else
            {
                audioSource.volume = 0.20f;
            }
        }

        [PunRPC]
        public async void RequestSong(string url, int requesterId)
        {
            Debug.Log($"RequestSong RPC received: url={url}, requesterId={requesterId}");

            if (photonView.IsMine && isDownloading)
            {
                Debug.Log("Already downloading a song. Please wait for the current download to finish.");
                return;
            }

            if (photonView.IsMine)
            {
                isDownloading = true;
            }

            if (!downloadedClips.ContainsKey(url))
            {
                try
                {
                    string filePath = await Task.Run(() => YoutubeDL.DownloadAudioAsync(url));

                    AudioClip clip = await Task.Run(() => AudioConverter.GetAudioClipAsync(filePath));

                    downloadedClips[url] = clip;
                    Debug.Log($"Downloaded and cached clip for url: {url}");

                    AddDownloadedSong(clip.name, url);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to download audio: {ex.Message}");
                    isDownloading = false;
                    return;
                }
            }
            else
            {
                Debug.Log($"Clip already cached for url: {url}");
            }

            photonView.RPC("ReportDownloadComplete", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, url);
            await WaitForAllPlayersReady(url);
            photonView.RPC("SyncPlayback", RpcTarget.All, url, requesterId);

            if (photonView.IsMine)
            {
                isDownloading = false;
            }
        }

        [PunRPC]
        public void ReportDownloadComplete(int actorNumber, string url)
        {
            if (!downloadsReady.ContainsKey(url))
                downloadsReady[url] = new HashSet<int>();

            downloadsReady[url].Add(actorNumber);
            Debug.Log($"Player {actorNumber} reported ready for url: {url}. Total ready: {downloadsReady[url].Count}");
        }

        private async Task WaitForAllPlayersReady(string url)
        {
            int totalPlayers = PhotonNetwork.PlayerList.Length;
            while (!downloadsReady.ContainsKey(url) || downloadsReady[url].Count < totalPlayers)
            {
                await Task.Delay(100);
            }
        }

        [PunRPC]
        public void SyncPlayback(string url, int requesterId)
        {
            Debug.Log($"SyncPlayback RPC received: url={url}, requesterId={requesterId}");

            Boombox targetBoombox = FindBoomboxForPlayer(requesterId);
            if (targetBoombox == null)
            {
                Debug.LogError($"No boombox found for player {requesterId}");
                return;
            }

            if (!downloadedClips.ContainsKey(url))
            {
                Debug.LogError($"Clip not found for url: {url}");
                return;
            }

            targetBoombox.audioSource.clip = downloadedClips[url];
            targetBoombox.audioSource.Play();
        }

        [PunRPC]
        public void StopPlayback(int requesterId)
        {
            if (photonView.Owner != null && photonView.Owner.ActorNumber == requesterId)
            {
                Debug.Log($"Stopping playback on Boombox owned by player {requesterId}");
                
                if (audioSource.isPlaying)
                {
                    audioSource.Stop();
                }
            }
        }

        [PunRPC]
        public void SyncTime(float time, int requesterId)
        {
            if (photonView.Owner != null && photonView.Owner.ActorNumber == requesterId)
            {
                if (audioSource.isPlaying)
                {
                    audioSource.time = time;
                }

                Debug.Log($"Syncing time to {time} seconds, requesterId={requesterId}");
            }
        }

        private static Boombox FindBoomboxForPlayer(int playerId)
        {
            foreach (Boombox boombox in FindObjectsOfType<Boombox>())
            {
                if (boombox.photonView.Owner != null && boombox.photonView.Owner.ActorNumber == playerId)
                {
                    return boombox;
                }
            }
            return null;
        }


        #region Helper Methods

        private static bool IsCacheInvalid()
        {
            foreach (var entry in _boomboxCache)
            {
                if (entry.Value == null || entry.Value.gameObject == null)
                {
                    return true;
                }
            }
            return false;
        }

        private static void RefreshBoomboxCache()
        {
            _boomboxCache.Clear();
            
            foreach (Boombox boombox in FindObjectsOfType<Boombox>())
            {
                if (boombox.photonView.Owner != null)
                {
                    int actorNumber = boombox.photonView.Owner.ActorNumber;
                    _boomboxCache[actorNumber] = boombox;
                }
            }
        }

        private void AddDownloadedSong(string songName, string url)
        {
            if (!downloadedSongs.ContainsKey(songName))
            {
                downloadedSongs.Add(songName, url);
            }
        }

        #endregion
    }
}