using UnityEngine;
using System.Text.RegularExpressions;
using Photon.Pun;
using System;

namespace SemiBoombox
{
    public class BoomboxUI : MonoBehaviour
    {
        public PhotonView photonView;

        private bool showUI = false;
        private string urlInput = "";
        private string urlFeedback = "";
        private float volume = 0.15f;

        private float currentPlaybackTime = 0f;
        private float totalPlaybackTime = 0f;
        private float playbackSliderValue = 0f;

        private Rect windowRect = new(100, 100, 400, 500);
        private Vector2 scrollPosition = Vector2.zero;
        private Boombox boombox;

        private void Awake()
        {
            boombox = GetComponent<Boombox>();
            photonView = boombox.photonView;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.X))
            {
                showUI = !showUI;
                Cursor.visible = showUI;
                Cursor.lockState = showUI ? CursorLockMode.None : CursorLockMode.Locked;
            }
            
            // Update playback times if the audio is playing and a clip is assigned.
            if (boombox.audioSource != null && boombox.audioSource.clip != null)
            {
                if (boombox.audioSource.isPlaying)
                {
                    currentPlaybackTime = boombox.audioSource.time;
                    totalPlaybackTime = boombox.audioSource.clip.length;
                }
                else
                {
                    // Optionally, keep the last known time when paused or stopped.
                    currentPlaybackTime = boombox.audioSource.time;
                    totalPlaybackTime = boombox.audioSource.clip.length;
                }
            }
            else
            {
                currentPlaybackTime = 0f;
                totalPlaybackTime = 0f;
            }
        }

        private void OnGUI()
        {
            if (showUI)
            {
                windowRect = GUI.Window(0, windowRect, DrawUI, "Boombox Controller");
            }
        }

        private void DrawUI(int windowID)
        {
            GUILayout.Label("Enter YouTube URL:");
            urlInput = GUILayout.TextField(urlInput, 200);
            GUILayout.Label(urlFeedback);

            GUILayout.Space(10);

            GUILayout.Label($"Volume: {Mathf.Round(volume * 100)}%");
            float newVolume = GUILayout.HorizontalSlider(volume, 0f, 1f);
            if (newVolume != volume)
            {
                volume = newVolume;

                // Update local Boombox volume
                if (boombox.audioSource != null)
                {
                    boombox.audioSource.volume = volume;
                }

                // Update remote Boombox volumes (local client only)
                foreach (Boombox remoteBoombox in Boombox.GetAllRemoteBoomboxes())
                {
                    if (remoteBoombox.audioSource != null)
                    {
                        remoteBoombox.audioSource.volume = volume;
                    }
                }
            }

            GUILayout.Space(10);

            GUILayout.Label($"Current Time: {FormatTime(currentPlaybackTime)} / {FormatTime(totalPlaybackTime)}");

            if (boombox.audioSource != null && boombox.audioSource.isPlaying)
            {
                playbackSliderValue = GUILayout.HorizontalSlider(currentPlaybackTime, 0f, totalPlaybackTime);

                if (Mathf.Abs(playbackSliderValue - currentPlaybackTime) > 0.1f)
                {
                    currentPlaybackTime = playbackSliderValue;
                    photonView.RPC("SyncTime", RpcTarget.All, currentPlaybackTime, PhotonNetwork.LocalPlayer.ActorNumber);
                }
            }

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Play"))
            {
                if (IsValidUrl(urlInput, out string correctedUrl))
                {
                    photonView.RPC("RequestSong", RpcTarget.All, correctedUrl, PhotonNetwork.LocalPlayer.ActorNumber);
                }
                else
                {
                    urlFeedback = "Invalid URL!";
                    Debug.LogError("Invalid URL!");
                }
            }

            if (GUILayout.Button("Stop"))
            {
                photonView.RPC("StopPlayback", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
            }

            if (GUILayout.Button("Close"))
            {
                showUI = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.Label("Downloaded Songs:");

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
            foreach (var song in Boombox.downloadedSongs)
            {
                if (GUILayout.Button(song.Key))
                {
                    urlInput = song.Value;
                }
            }
            GUILayout.EndScrollView();

            GUI.DragWindow();
        }

        #region Helpers

        private bool IsValidUrl(string url, out string correctedUrl)
        {
            string pattern = @"^https?:\/\/(www\.)?youtube\.com\/watch\?v=[a-zA-Z0-9_-]+$";
            correctedUrl = url;

            if (Regex.IsMatch(url, pattern))
            {
                return true;
            }

            if (url.Contains("youtube") && url.Contains("watch?v="))
            {
                correctedUrl = "https://www.youtube.com/watch?v=" + url.Split(["watch?v="], StringSplitOptions.None)[1].Split('&')[0];
                urlFeedback = "URL fixed to: " + correctedUrl;
                return true;
            }

            return false;
        }

        private string FormatTime(float seconds)
        {
            TimeSpan time = TimeSpan.FromSeconds(seconds);
            return string.Format("{0:D2}:{1:D2}", time.Minutes, time.Seconds);
        }

        #endregion
    }
}