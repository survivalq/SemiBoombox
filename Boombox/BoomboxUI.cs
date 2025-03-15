using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Photon.Pun;

namespace SemiBoombox
{
    public class BoomboxUI : MonoBehaviour
    {
        public PhotonView photonView;

        private bool showUI = false;
        private string urlInput = "";
        private float volume = 0.15f;

        // Store name -> URL mapping
        private Dictionary<string, string> downloadedSongs = [];

        private Rect windowRect = new Rect(100, 100, 400, 500);
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
                if (showUI)
                    Cursor.lockState = CursorLockMode.None;
                else
                    Cursor.lockState = CursorLockMode.Locked;
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

            GUILayout.Space(10);

            GUILayout.Label($"Volume: {Mathf.Round(volume * 100)}%");
            volume = GUILayout.HorizontalSlider(volume, 0f, 1f);
            if (boombox.audioSource != null)
            {
                boombox.audioSource.volume = volume;
            }

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Play"))
            {
                if (IsValidUrl(urlInput))
                {
                    photonView.RPC("RequestSong", RpcTarget.All, urlInput, PhotonNetwork.LocalPlayer.ActorNumber);
                }
                else
                {
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
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.Label("Downloaded Songs:");

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
            foreach (var song in downloadedSongs)
            {
                if (GUILayout.Button(song.Key))
                {
                    urlInput = song.Value;
                }
            }
            GUILayout.EndScrollView();

            GUI.DragWindow();
        }

        private bool IsValidUrl(string url)
        {
            string pattern = @"^https?:\/\/(www\.)?youtube\.com\/watch\?v=[a-zA-Z0-9_-]+$";
            return Regex.IsMatch(url, pattern);
        }

        public void AddDownloadedSong(string songName, string url)
        {
            if (!downloadedSongs.ContainsKey(songName))
            {
                downloadedSongs.Add(songName, url);
            }
        }
    }
}