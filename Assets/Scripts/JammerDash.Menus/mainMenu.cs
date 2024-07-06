﻿using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.UI;
using System.IO;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Audio;
using Random = UnityEngine.Random;
using System.Collections.Generic;
using UnityEngine.Video;
using System.Diagnostics;
using System.IO.Compression;
using Lachee.Discord;
using Button = UnityEngine.UI.Button;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using JammerDash.Audio;
using JammerDash.Menus.Play;
using SimpleFileBrowser;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;
using Lachee.Discord.Control;

namespace JammerDash.Menus
{
    public class mainMenu : MonoBehaviour, IPointerClickHandler
    {
        public GameObject musicAsset;
        public Image bg;
        public Sprite[] sprite;
        private bool quittingAllowed = false;
        public SettingsData data;
        public float quitTimer = 1f;
        public float quitTime = 0f;
        public Image quitPanel2;
        PlayerData playerData;
        public GameObject accessoverPanel;
        public GameObject accessibilityButton;
        public Text nolevelerror;
        public GameObject[] disableAccess;
        [Header("Panels")]
        public GameObject mainPanel;
        public GameObject playPanel;
        public GameObject creditsPanel;
        public GameObject settingsPanel;
        public GameObject additionalPanel;
        public GameObject levelInfo;
        public GameObject quitPanel;
        public GameObject community;
        public GameObject musicPanel;
        public GameObject changelogs;
        public GameObject funMode;
        public GameObject overPanel;
        public GameObject accPanel;

        [Header("LevelInfo")]
        public GameObject levelInfoPanelPrefab;
        public Transform levelInfoParent;
        public GameObject levelCreatePanel;
        public InputField newLevelNameInput;
        public InputField song;
        public InputField artists;
        public InputField map;
        public GameObject cubePrefab;
        public GameObject sawPrefab;
        public GameObject playPrefab;
        public Transform playlevelInfoParent;
        public string songName;
        public string mapper;
        public string artist;
        public string path;
        public Text songMP3Name;
      
        [Header("Video Background")]
        public GameObject videoPlayerObject;
        public VideoPlayer videoPlayer;
        public VideoClip[] videoClips;
        private List<string> videoUrls;
        private int currentVideoIndex = 0;

        [Header("Idle")]
        public Animator animator;
        private float idleTimer = 0f;
        public float idleTimeThreshold = 10f;

        [Header("Music")]
        public AudioMixer audioMixer;
        AudioSource source;
        private float lowpassTargetValue;
        private float fadeDuration = 0.25f;
        private float currentLerpTime = 0f;
        public bool focus = true;

        [Header("Profile")]
        public Slider levelSlider;
        public Text levelText;
        public RawImage[] discordpfp;
        public Text discordName;
        public Text[] usernames;

        [Header("Fun Mode")]
        public GameObject vis2;

        [Header("Visualizer V2")]
        public Text musicText;
        public Slider musicSlider;

        [Header("Parallax")]
        public Transform logo;
        public Transform background;
        public float backgroundParallaxSpeed;
        public float maxMovementOffset;
        public float scaleMultiplier;
        public float edgeMargin;

        private Vector3 lastMousePosition;

        private Camera mainCamera;
        private RectTransform canvasRect;

        void Start()
        {
            Time.timeScale = 1f;
            playerData = Account.Instance.LoadData();
            data = SettingsFileHandler.LoadSettingsFromFile();
            LoadRandomBackground();
            LoadLevelsFromFiles();
            LoadLevelFromLevels();
            levelSlider.maxValue = Account.Instance.xpRequiredPerLevel[0];
            levelSlider.value = Account.Instance.currentXP;
            Debug.unityLogger.logEnabled = true;
            discordName.text = DiscordManager.current.CurrentUser.username + "\n\nID: " +
            DiscordManager.current.CurrentUser.ID;
           
                foreach (GameObject go in disableAccess)
                {
                    go.SetActive(true);
                }
                accessibilityButton.SetActive(false);
            
            
        }
        public string FormatTime(float time)
        {
            int minutes = Mathf.FloorToInt(time / 60);
            int seconds = Mathf.FloorToInt(time % 60);

            // Ensure seconds don't go beyond 59
            seconds = Mathf.Clamp(seconds, 0, 59);

            return string.Format("{0:00}:{1:00}", minutes, seconds);
        }
        public string FormatNumber(long number)
        {
            string formattedNumber;
             
            if (number < 1000)
            {
                formattedNumber = number.ToString();
                return formattedNumber;
            }
            else if (number >= 1000 && number < 1000000)
            {
                formattedNumber = (number / 1000f).ToString("F1") + "K";
                return formattedNumber;
            }
            else if (number >= 1000000 && number < 1000000000)
            {
                formattedNumber = (number / 1000000f).ToString("F2") + "M";
                return formattedNumber;
            }
            else if (number >= 1000000000 && number < 1000000000000)
            {
                formattedNumber = (number / 1000000000f).ToString("F2") + "B";
                return formattedNumber;
            }
            else if (number >= 1000000000000 && number <= 1000000000000000)
            {
                formattedNumber = (number / 1000000000000f).ToString("F2") + "T";
                return formattedNumber;
            }
            else
            {
                formattedNumber = (number / 1000000000000f).ToString("F3") + "Q";
                return formattedNumber;
            }
        }
        public void LoadLevelFromLevels()
        {
            foreach (Transform child in playlevelInfoParent)
            {
                Destroy(child.gameObject);
            }

            string defaultLevelsPath = Path.Combine(Application.streamingAssetsPath, "levels");
            string levelsPath = Path.Combine(Application.persistentDataPath, "levels");

            if (!Directory.Exists(levelsPath))
            {
                UnityEngine.Debug.LogError("The 'levels' folder does not exist in persistentDataPath.");
                Directory.CreateDirectory(levelsPath);
                return;
            }

            string[] levelFiles = Directory.GetFiles(levelsPath, "*.jdl", SearchOption.AllDirectories);
            string[] defaultLevelFiles = Directory.GetFiles(defaultLevelsPath, "*.jdl");

            string[] allLevelFiles = new string[levelFiles.Length + defaultLevelFiles.Length];
            levelFiles.CopyTo(allLevelFiles, 0);
            defaultLevelFiles.CopyTo(allLevelFiles, levelFiles.Length);

            HashSet<int> processedIDs = new HashSet<int>();

            foreach (string filePath in allLevelFiles)
            {
                if (Path.GetFileName(filePath).Equals("LevelDefault.jdl"))
                {
                    continue; // Skip LevelDefault.jdl
                }

                // Create a temporary folder
                string tempFolder = Path.Combine(Application.temporaryCachePath, "tempExtractedJson");
                Directory.CreateDirectory(tempFolder);

                try
                {
                    // Extract JSON data from JDL file to the temporary folder
                    string jsonFilePath = ExtractJSONFromJDL(filePath);

                    if (jsonFilePath == null)
                    {
                        UnityEngine.Debug.LogError("Failed to extract JSON from JDL: " + filePath);
                        continue;
                    }

                    // Read JSON content from the extracted JSON file
                    string json = File.ReadAllText(jsonFilePath);

                    // Deserialize JSON data into SceneData object
                    SceneData sceneData = SceneData.FromJson(json);

                    if (sceneData == null)
                    {
                        UnityEngine.Debug.LogError("Failed to deserialize JSON from file: " + jsonFilePath);
                        continue;
                    }

                    // Check if the sceneData.ID has already been processed
                    if (processedIDs.Contains(sceneData.ID))
                    {
                        continue; // Skip duplicate levels
                    }

                    // Add the ID to the processed set
                    processedIDs.Add(sceneData.ID);

                    // Log the level name to verify if sceneData is successfully deserialized
                    UnityEngine.Debug.LogWarning(sceneData.levelName);

                    // Create a directory with the level name
                    string extractedPath = Path.Combine(levelsPath, "extracted", sceneData.sceneName);
                    Directory.CreateDirectory(extractedPath);

                    // Move the JSON file to the new directory
                    string jsonDestinationPath = Path.Combine(extractedPath, sceneData.sceneName + ".json");
                    if (File.Exists(jsonDestinationPath))
                    {
                        File.Delete(jsonDestinationPath);
                    }
                    File.Move(jsonFilePath, jsonDestinationPath);
                    ExtractMP3FromJDL(filePath, extractedPath);
                    GameObject levelInfoPanel = Instantiate(playPrefab, playlevelInfoParent);

                    // Display level information on UI
                    DisplayCustomLevelInfo(sceneData, levelInfoPanel.GetComponent<CustomLevelScript>());
                    levelInfoPanel.GetComponent<CustomLevelScript>().SetSceneData(sceneData);
                }
                finally
                {
                    // Clean up the temporary folder
                    Directory.Delete(tempFolder, true);
                }
            }
        }
        public void Import()
        {
            FileBrowser.m_instance = Instantiate(Resources.Load<GameObject>("SimpleFileBrowserCanvas")).GetComponent<FileBrowser>();
            FileBrowser.SetFilters(false, new FileBrowser.Filter("Jammer Dash Level", ".jdl"));
            FileBrowser.SetDefaultFilter("Music");
            FileBrowser.ShowLoadDialog(ImportLevel, null, FileBrowser.PickMode.Files, true, null, null, "Import Level...", "Import");
        }

        void ImportLevel(string[] paths)
        {
            if (paths.Length >= 0)
            {
                foreach (string path in paths)
                {
                    File.Move(path, Path.Combine(Application.persistentDataPath, "levels", Path.GetFileName(path)));
                }
            }

            LoadLevelFromLevels();
        }
        public void Accounts()
        {
            accPanel.SetActive(!accPanel.activeSelf);
        }

        public void SaveAcc()
        {
            Account.Instance.username = Account.Instance.usernameInput.text;
            Account.Instance.SavePlayerData();
        }
        public static string ExtractJSONFromJDL(string jdlFilePath)
        {
            try
            {
                // Create a temporary folder
                string tempFolder = Path.Combine(Application.temporaryCachePath, "tempExtractedJson");
                Directory.CreateDirectory(tempFolder);

                using (ZipArchive archive = ZipFile.OpenRead(jdlFilePath))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (!entry.FullName.EndsWith(".json"))
                        {
                            continue; // Skip non-JSON files
                        }

                        // Generate a unique filename for the extracted JSON file
                        string extractedFilePath = Path.Combine(tempFolder, Path.GetFileName(entry.FullName));

                        // Extract the JSON file to the temporary folder
                        entry.ExtractToFile(extractedFilePath, true); // Overwrite if file exists

                        // Return the path of the extracted JSON file
                        return extractedFilePath;
                    }
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("Error extracting JSON from JDL: " + e.Message);
            }

            return null;
        }

        public static void ExtractMP3FromJDL(string jdlFilePath, string destinationFilePath)
        {
            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(jdlFilePath))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        string entryFileName = entry.FullName;

                        if (entryFileName.EndsWith(".mp3") || entryFileName.EndsWith(".wav") || entryFileName.EndsWith(".ogg"))
                        {
                            // Combine the destination directory path with the MP3 filename
                            string destinationFullPath = Path.Combine(destinationFilePath, Path.GetFileName(entryFileName));

                            // Extract the MP3 file to the specified destination file path
                            entry.ExtractToFile(destinationFullPath, overwrite: true);
                            return; // Exit the method after extracting the MP3 file
                        }
                    }
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("Error extracting Audio from JDL: " + e.Message);
            }
        }



        public void URL(string url)
        {
            Application.OpenURL(url);
        }
        public void LoadRandomBackground()
        {
            UnityEngine.Debug.Log(data);
            if (DateTime.Now.Month == 12 && data.artBG)
            {
                sprite = Resources.LoadAll<Sprite>("backgrounds/christmas");
                if (videoPlayerObject != null)
                    videoPlayerObject.SetActive(false);
            }
            else if (DateTime.Now.Month == 2 && DateTime.Now.Day == 14 && data.artBG)
            {
                sprite = Resources.LoadAll<Sprite>("backgrounds/valentine");
                if (videoPlayerObject != null)
                    videoPlayerObject.SetActive(false);
            }
            if (data.artBG)
            {
                sprite = Resources.LoadAll<Sprite>("backgrounds/default");
                if (videoPlayerObject != null)
                    videoPlayerObject.SetActive(false);
            }
            if (data.customBG)
            {
                _ = LoadCustomBackgroundAsync();
            }
            else if (data.vidBG)
            {
                string[] files = Directory.GetFiles(Application.persistentDataPath + "/backgrounds", "*.mp4");
                List<VideoClip> clips = new List<VideoClip>();

                foreach (string file in files)
                {
                    // Check the size of each file before adding it to the list
                    FileInfo fileInfo = new FileInfo(file);
                    if (fileInfo.Length <= 5 * 1024 * 1024) // 5MB in bytes
                    {
                        // Create a video clip from the file path
                        LoadVideoClipFromFile(file);

                    }
                    else
                    {
                        UnityEngine.Debug.LogError("Video file size exceeds the limit (5MB): " + file);
                    }
                }
                videoPlayer.loopPointReached += OnVideoLoopPointReached;
                // Assign the video clips array
                videoClips = clips.ToArray();
            }
            else if (!data.artBG && !data.vidBG && !data.customBG)
            {
                sprite = Resources.LoadAll<Sprite>("backgrounds/basic");
                if (videoPlayerObject != null)
                    videoPlayerObject.SetActive(false);
            }

            if (sprite.Length > 0 && data.artBG || sprite.Length > 0 && data.customBG)
            {
                bg.color = Color.white;
                int randomIndex = Random.Range(0, sprite.Length);
                bg.sprite = sprite[randomIndex];
            }
            else
            {
                ChangeBasicCol();
            }
        }
        private async Task LoadCustomBackgroundAsync()
        {
            string path = Application.persistentDataPath + "/backgrounds";
            string[] files = Directory.GetFiles(path, "*.png");

            if (files.Length == 0)
            {
                Debug.LogWarning("No PNG files found in the directory: " + path);
                return;
            }

            string randomFilePath = files[UnityEngine.Random.Range(0, files.Length)]; // Choose a random file path
            await LoadSpriteAsync(randomFilePath);
        }

        private async Task LoadSpriteAsync(string filePath)
        {
            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture("file://" + filePath))
            {
                await uwr.SendWebRequest();

                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Failed to download texture: {uwr.error}");
                    return;
                }

                Texture2D texture = DownloadHandlerTexture.GetContent(uwr);
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

                // Set the loaded sprite
                SetBackgroundSprite(sprite);
            }
        }

        private void SetBackgroundSprite(Sprite sprite)
        {
            if (videoPlayerObject != null)
            {
                videoPlayerObject.SetActive(false);
            }

            this.sprite = new Sprite[1];
            this.sprite[0] = sprite;
        }
            private void LoadVideoClipFromFile(string filePath)
        {
            string[] files = Directory.GetFiles(Application.persistentDataPath + "/backgrounds", "*.mp4");
            List<string> urls = new List<string>();

            foreach (string file in files)
            {
                // Add file paths as URLs
                urls.Add("file://" + file);
            }
            videoUrls = urls;
            // Assign the video URLs to the VideoPlayer
            videoPlayer.clip = null;
            videoPlayer.url = urls[Random.Range(0, urls.Count)];
            videoPlayer.Prepare();

            // Handle PrepareCompleted event to start playing the video
            videoPlayer.prepareCompleted += OnVideoPrepareCompleted;

        }

        void OnVideoLoopPointReached(VideoPlayer vp)
        {
            // Video finished playing, play the next video
            currentVideoIndex = (currentVideoIndex + 1) % videoUrls.Count;
            if (currentVideoIndex == videoUrls.Count)
            {
                currentVideoIndex = 0;
            }
            PlayVideo(videoUrls[currentVideoIndex]);
        }
        void PlayVideo(string url)
        {
            videoPlayer.clip = null;
            videoPlayer.url = url;
            videoPlayer.Prepare();
            videoPlayer.prepareCompleted += OnVideoPrepareCompleted;
            videoPlayer.loopPointReached += OnVideoLoopPointReached;
        }
        void OnVideoPrepareCompleted(VideoPlayer vp)
        {
            // Video preparation is complete, start playing the video
            vp.Play();
        }
        public void ChangeBasicCol()
        {
            StartCoroutine(SmoothColorChange());
        }

        public float elapsedTime;

        private IEnumerator SmoothColorChange()
        {
            if (sprite != null && sprite.Length > 0)
            {
                bg.sprite = sprite[0];
                Color targetColor = new Color(Random.Range(0.5f, 1f), Random.Range(0.5f, 1f), Random.Range(0.5f, 1f), 1f);

                float duration = 5f;  // Adjust the duration as needed

                while (elapsedTime < duration)
                {
                    bg.color = Color.Lerp(bg.color, targetColor, elapsedTime / duration);
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
                elapsedTime = 0f;

                // Ensure the final color is exactly the target color
                bg.color = targetColor;
            }
            else
            {
                bg.color = Color.white;
            }
        }


       

      
        public void CreatePanel()
        {
            levelCreatePanel.SetActive(true);
        }

        public void CreatePanelClose()
        {
            levelCreatePanel.SetActive(false);
        }
        public void CreateNewLevel()
        {
            // Get input values for the new level
            string newLevelName = newLevelNameInput.text;
            float newDifficulty = 0;

            songName = song.text;
            artist = artists.text;
            mapper = map.text;
            SceneData newLevelData = new SceneData
            {
                sceneName = newLevelName, // You may want to customize the scene name
                levelName = newLevelName,
                calculatedDifficulty = newDifficulty,
                songName = songName,
                artist = artist,
                creator = mapper,
                clipPath = Path.Combine(Application.persistentDataPath, "scenes", newLevelName, $"{artist} - {songName}.mp3")
            };

            // Save the new level data to a JSON file
            SaveLevelToFile(newLevelData);


            // Reload levels to update the UI with the new level
            LoadLevelsFromFiles();
        }

        [System.Serializable]
        public class CountryInfo
        {
            public string country;
            // Add other fields from the ipinfo.io response if needed
        }

        void SaveLevelToFile(SceneData sceneData)
        {
            // Convert SceneData to JSON
            string json = sceneData.ToJson();

            // Save JSON to a new file in the persistentDataPath + scenes & levels folder
            string namepath = Path.Combine(Application.persistentDataPath, "scenes", sceneData.levelName);
            string filePath = Path.Combine(namepath, $"{sceneData.levelName}.json");
            string musicPath = Path.Combine(namepath, $"{sceneData.artist} - {sceneData.songName}.mp3");
            if (Directory.Exists(namepath))
            {
                songMP3Name.text = "A level with this name currently exists. Try something else or delete the level.";
            }
            else
            {
                Directory.CreateDirectory(namepath);
                File.WriteAllText(filePath, json);
                File.Copy(path, Path.Combine(Application.persistentDataPath, "scenes", sceneData.sceneName, $"{artist} - {songName}.mp3"));
            }

        }

        public void LoadLevelsFromFiles()
        {
            // Clear existing level information panels
            foreach (Transform child in levelInfoParent)
            {
                Destroy(child.gameObject);
            }
            string levelsPath = Path.Combine(Application.persistentDataPath, "scenes");

            if (!Directory.Exists(levelsPath))
            {
                UnityEngine.Debug.LogError("The 'scenes' folder does not exist in persistentDataPath.");
                Directory.CreateDirectory(levelsPath);
                return;
            }

            string[] levelFiles = Directory.GetFiles(levelsPath, "*.json", SearchOption.AllDirectories);

            foreach (string filePath in levelFiles)
            {
                string json = File.ReadAllText(filePath);
                SceneData sceneData = SceneData.FromJson(json);

                // Instantiate the level information panel prefab
                GameObject levelInfoPanel = Instantiate(levelInfoPanelPrefab, levelInfoParent);

                // Display level information on UI
                DisplayLevelInfo(sceneData, levelInfoPanel.GetComponent<LevelScript>());
                levelInfoPanel.GetComponent<LevelScript>().SetSceneData(sceneData);
            }
        }

        private SceneData LoadSceneData(string filePath)
        {
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                SceneData sceneData = SceneData.FromJson(json);
                return sceneData;
            }
            else
            {
                UnityEngine.Debug.LogWarning("Scene data file not found: " + filePath);
                return null;
            }
        }

        private void InstantiateObjects(SceneData sceneData)
        {
            // Instantiate cubes and saws based on sceneData
            foreach (Vector3 cubePos in sceneData.cubePositions)
            {
                Instantiate(cubePrefab, cubePos, Quaternion.identity);
            }

            foreach (Vector3 sawPos in sceneData.sawPositions)
            {
                Instantiate(sawPrefab, sawPos, Quaternion.identity);
            }

            // Other initialization logic for objects
        }

        // Display level information on UI
        void DisplayLevelInfo(SceneData sceneData, LevelScript level)
        {

            // Check if LevelScript component is not null
            if (level != null)
            {
                UnityEngine.Debug.Log(sceneData);
                // Assuming YourPanelScript has methods to set text or image properties
                level.SetLevelName($"{sceneData.sceneName}");
                level.SetSongName($"♫ {sceneData.artist} - {sceneData.songName}");
                level.SetDifficulty($"{sceneData.calculatedDifficulty:0.00} sn");

            }
            else
            {
                // Logging for debugging purposes
                UnityEngine.Debug.LogError("Failed to load level " + sceneData.ID);
            }

        }
        public void LoadCustomMusic()
        {
            FileBrowser.m_instance = Instantiate(Resources.Load<GameObject>("SimpleFileBrowserCanvas")).GetComponent<FileBrowser>();
            FileBrowser.SetFilters(false, new FileBrowser.Filter("Music", ".mp3"));
            FileBrowser.SetDefaultFilter("Music");
            FileBrowser.ShowLoadDialog(SongSelected, null, FileBrowser.PickMode.Files, false, null, null, "Load Local song...", "Choose");
        }
        void SongSelected(string[] paths)
        {
            if (paths.Length == 1)
            {
                path = paths[0];
                songMP3Name.text = path;
            }

            else
            {
                songMP3Name.text = "You can't have more than one song selected.";
            }
        }


        void DisplayCustomLevelInfo(SceneData sceneData, CustomLevelScript level)
        {

            // Check if LevelScript component is not null
            if (level != null)
            {
                // Assuming YourPanelScript has methods to set text or image properties
                level.SetLevelName($"{sceneData.sceneName}");
                level.SetInfo($"♫ {sceneData.artist} - {sceneData.songName} // mapped by {sceneData.creator}");
            }
            else
            {
                // Logging for debugging purposes
                UnityEngine.Debug.LogError("Failed to load level " + sceneData.ID);
            }

            // Method to toggle menu panels
        }

        // Method to toggle menu panels
        private void ToggleMenuPanel(GameObject panel)
        {
            // Toggle the specified panel directly if it's changelogs or creditsPanel
            if (panel == changelogs || panel == creditsPanel || panel == funMode || panel == community)
            {
                panel.SetActive(!panel.activeSelf);

                if (panel.active)
                {
                    // Turn off all panels
                    mainPanel.SetActive(false);
                    playPanel.SetActive(false);
                    creditsPanel.SetActive(false);
                    settingsPanel.SetActive(false);
                    musicPanel.SetActive(false);
                    levelInfo.SetActive(false);
                    community.SetActive(false);
                    changelogs.SetActive(false);
                    additionalPanel.SetActive(false);
                    funMode.SetActive(false);
                    vis2.SetActive(false);
                }
                if (!panel.active)
                {
                    panel.SetActive(true);
                }

            }
            else if (panel == settingsPanel)
            {
                settingsPanel.SetActive(!panel.activeSelf);
            }
            else
            {
                // Turn off all panels
                mainPanel.SetActive(false);
                playPanel.SetActive(false);
                creditsPanel.SetActive(false);
                settingsPanel.SetActive(false);
                musicPanel.SetActive(false);
                levelInfo.SetActive(false);
                community.SetActive(false);
                changelogs.SetActive(false);
                additionalPanel.SetActive(false);
                funMode.SetActive(false);
                vis2.SetActive(false);

                // Enable the specified panel if it's not null
                if (panel != null)
                {
                    AudioManager.Instance.GetComponent<AudioSource>().loop = false;
                    panel.SetActive(true);
                }
                // Enable mainPanel if none of the specific panels are active
                else if (!playPanel.activeSelf && !creditsPanel.activeSelf && !settingsPanel.activeSelf)
                {
                    mainPanel.SetActive(true);
                    LoadRandomBackground();
                    AudioManager.Instance.GetComponent<AudioSource>().loop = false;
                }

                if (playPanel.activeSelf)
                {
                    ButtonClickHandler[] levels = FindObjectsOfType<ButtonClickHandler>();

                    foreach (ButtonClickHandler level in levels)
                    {
                        if (level.isSelected)
                        {
                            level.Change();
                            AudioManager.Instance.GetComponent<AudioSource>().loop = true;
                        }
                    }
                }
            }
        }

        // Menu buttons
        public void Home()
        {
            ToggleMenuPanel(mainPanel);
        }
        public void Play()
        {
            AudioManager.Instance.GetComponent<AudioSource>().loop = true;
            ToggleMenuPanel(playPanel);
            InputSystem.pollingFrequency = 1000;
            InputSystem.settings.maxQueuedEventsPerUpdate = 1000;
        }

        public void PlayRandomSFX()
        {
            UnityEngine.Object[] clips = Resources.LoadAll("Audio/SFX");
            foreach (var obj in clips)
            {
                UnityEngine.Debug.Log("Loaded object: " + obj.name);
            }
            FindObjectOfType<AudioSource>().PlayOneShot((AudioClip)clips[Random.Range(0, clips.Length)]);

        }
        public void Credits()
        {
            ToggleMenuPanel(creditsPanel);
        }

        public void Settings()
        {
            ToggleMenuPanel(settingsPanel);
        }

        public void OpenMusic()
        {
            musicPanel.SetActive(!musicPanel.activeSelf);
        }

        public void OpenChangelogs()
        {
            ToggleMenuPanel(changelogs);
        }

        public void AdditionalOpen()
        {
            additionalPanel.SetActive(true);
        }

        public void AdditionalClose()
        {
            additionalPanel.SetActive(false);
        }

        public void FunMode()
        {
            ToggleMenuPanel(funMode);
        }

        // Fun mode buttons

        public void VisualizerToggle()
        {
            vis2.SetActive(true);
        }
        public void Quit()
        {
            StartCoroutine(QuitGame());
        }

        IEnumerator QuitGame()
        {
            ShowQuit();
            yield return null;
        }


        public void ShowQuit()
        {
            quitPanel.SetActive(true);


            Application.wantsToQuit += QuitHandler;
            PostProcessVolume volume = FindFirstObjectByType<PostProcessVolume>();
            if (volume != null)
            {
                PostProcessProfile profile = volume.profile;
                Vignette vignette;
                if (profile.TryGetSettings(out vignette))
                {
                    vignette.intensity.value = 0.75f;
                }
                else
                {
                    // Settings couldn't be retrieved
                    UnityEngine.Debug.LogWarning("Vignette settings not found in the Post Process Profile.");
                }
            }
            else
            {
                // PostProcessVolume not found in the scene
                UnityEngine.Debug.LogWarning("Post Process Volume not found in the scene.");
            }
        }

        // Call this method when hiding the quit panel
        public void HideQuit()
        {
            quitPanel.SetActive(false);

            Application.wantsToQuit -= QuitHandler;
            PostProcessVolume volume = FindFirstObjectByType<PostProcessVolume>();
            if (volume != null)
            {
                PostProcessProfile profile = volume.profile;
                Vignette vignette;
                if (profile.TryGetSettings(out vignette))
                {
                    vignette.intensity.value = 0f;
                }
                else
                {
                    // Settings couldn't be retrieved
                    UnityEngine.Debug.LogWarning("Vignette settings not found in the Post Process Profile.");
                }
            }
            else
            {
                // PostProcessVolume not found in the scene
                UnityEngine.Debug.LogWarning("Post Process Volume not found in the scene.");
            }
        }

        // Coroutine for fading the Lowpass filter
        private IEnumerator FadeLowpass()
        {
            SettingsData data = SettingsFileHandler.LoadSettingsFromFile();

            lowpassTargetValue = data.lowpassValue;
            float timer = 0f;
            float startValue = lowpassTargetValue;  // Initial value based on fade in or out

            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                float t = timer / fadeDuration;

                // Lerp between start and target values
                float currentValue = Mathf.Lerp(startValue, lowpassTargetValue, t);

                // Set the Lowpass filter parameter on the Master AudioMixer
                audioMixer.SetFloat("Lowpass", currentValue);

                yield return null;
            }

            // Ensure the target value is set after the fade completes
            audioMixer.SetFloat("Lowpass", lowpassTargetValue);
        }


      



        private bool QuitHandler()
        {
            if (quittingAllowed)
            {

                return true; // Return true to allow quitting
            }
            else
            {

                return false; // Return false to prevent quitting
            }
        }

        public void AllowQuitting()
        {
            quittingAllowed = true;
            Application.Quit();
            UnityEngine.Debug.Log("Quitting the application");
        }

        public void CancelQuitting()
        {
            quittingAllowed = false;
            HideQuit();
            UnityEngine.Debug.Log("User canceled quitting");
            // Set the Lowpass filter parameter on the Master AudioMixer
            audioMixer.SetFloat("Lowpass", 22000);
        }



        public void Menu()
        {
            AudioManager.Instance.GetComponent<AudioSource>().loop = false;
            StartCoroutine(AudioManager.Instance.ChangeSprite());
            settingsPanel.SetActive(false);
            creditsPanel.SetActive(false);
            playPanel.SetActive(false);
            community.SetActive(false);
            mainPanel.SetActive(true);
        }


        // Play section

        public void CommunityOpen()
        {
            ToggleMenuPanel(community);
        }


        public void OpenInfo()
        {
            levelInfo.SetActive(true);
            mainPanel.SetActive(false);
        }

        public void CloseInfo()
        {
            mainPanel.SetActive(true);
            levelInfo.SetActive(false);
        }

        public void OpenEditor()
        {
            SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
        }

        public void OpenLevel(int index)
        {
            SceneManager.LoadScene(index);
        }

        #region SOCIAL_MEDIA
        public void Discord()
        {
            Application.OpenURL("https://discord.gg/dJU8X2kDpn");
        }

        public void RegionDiscord(string ID)
        {
            Application.OpenURL($"https://discord.gg/{ID}");
        }

        public void Twitter()
        {
            Application.OpenURL("https://twitter.com/JammerDash");
        }

        public void YouTube()
        {
            Application.OpenURL("https://youtube.com/@JammerDashOfficial");
        }

        public void Newgrounds()
        {
            Application.OpenURL("https://www.newgrounds.com/bbs/topic/1530441");
        }

        public void Twitch()
        {
            Application.OpenURL("https://twitch.tv/prickletylive");
        }

        public void TikTok()
        {
            Application.OpenURL("https://tiktok.com/@pricklety");
        }

        public void Instagram()
        {
            Application.OpenURL("https://www.instagram.com/pricklety/");
        }


        #endregion

        public void CrashReports()
        {
            // File path of the player log
            string logFilePath = Application.persistentDataPath + "/Player.log";

            // Open the player log file using the default application associated with its file type
            Process.Start(logFilePath);
        }

        public void SettingsFile()
        {
            // File path of the player log
            string logFilePath = Application.persistentDataPath + "/settings.json";

            // Open the player log file using the default application associated with its file type
            Process.Start(logFilePath);
        }


        bool IsPointerOverUIButNotButton()
        {
            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
            foreach (RaycastResult result in results)
            {
                // Check if the object under the pointer is a UI button
                if (result.gameObject.GetComponent<Button>() != null || result.gameObject.GetComponent<Dropdown>() != null || result.gameObject.GetComponent<Slider>() != null)
                {
                    return true;
                }
            }
            return false;
        }
        public void FixedUpdate()
        {
        
            foreach (RawImage pfp in discordpfp)
            {
                 
                pfp.texture = DiscordManager.current.CurrentUser.avatar;

            }
            foreach (Text text in usernames)
            {
                if (string.IsNullOrEmpty(Account.Instance.username))
                {
                    text.text = "Guest";
                }
                else
                {
                    text.text = Account.Instance.username;
                }

            }
            if (Account.Instance.xpRequiredPerLevel[Account.Instance.level] >= 2000000)
            {
                levelSlider.maxValue = 2000000;
                float normalizedValue = (float)(Account.Instance.currentXP / Account.Instance.xpRequiredPerLevel[Account.Instance.level]);
                levelSlider.value = normalizedValue * levelSlider.maxValue;
            }
            else
            {
                levelSlider.maxValue = (float)Account.Instance.xpRequiredPerLevel[Account.Instance.level];
                levelSlider.value = Mathf.Min((float)Account.Instance.currentXP, levelSlider.maxValue);
            }
            if (Account.Instance.currentXP >= 0)
            {
                levelText.text = "lv" + Account.Instance.level.ToString() + $" (XP: {FormatNumber(Account.Instance.totalXP)})";

            }


            if (quitPanel.activeSelf)
            {
                // Set the Lowpass filter parameter on the Master AudioMixer
                audioMixer.SetFloat("Lowpass", data.lowpassValue);
            }
            else
            {
                // Set the Lowpass filter parameter on the Master AudioMixer
                audioMixer.SetFloat("Lowpass", 22000);
            }
            data = SettingsFileHandler.LoadSettingsFromFile();
            if (data.parallax && (Screen.fullScreenMode == FullScreenMode.FullScreenWindow || Screen.fullScreenMode == FullScreenMode.ExclusiveFullScreen || Application.isEditor))
            {

                // Background parallax effect
                background.localScale = new Vector3(scaleMultiplier, scaleMultiplier, 1);

                Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Vector3 mouseDelta = new Vector3(-mouseWorldPos.x / 1.5f, -mouseWorldPos.y / 30, 0);

                float cameraMovement = Mathf.Clamp(mouseDelta.x, -maxMovementOffset, maxMovementOffset) * backgroundParallaxSpeed * Time.unscaledDeltaTime;
                Vector3 backgroundOffset = new Vector3(cameraMovement, 0, 0);
                background.position = backgroundOffset + new Vector3(mouseDelta.x / 100, mouseDelta.y, mouseDelta.z);

                // Calculate the movement based on the change in mouse position
                float layerMovement = 0;

                // Calculate the adjustment based on mouseDelta
                Vector3 layerMouseDelta = new Vector3(Mathf.Clamp(-mouseDelta.x / 1.07f, -25, 25), Mathf.Clamp(-mouseDelta.y / 1.07f, -25, 25), mouseDelta.z);

                // Calculate the new position relative to the current position
                float newPositionX = layerMouseDelta.x / 30;
                float newPositionY = layerMouseDelta.y / 10;

                // Set the new position
                logo.position = new Vector3(newPositionX, newPositionY, mouseDelta.z);
            }
            else
            {
                background.localScale = Vector3.one;
                background.position = Vector3.zero;
                logo.position = new Vector3(0, 0, 0);
            }  
            musicText.text = $"{AudioManager.Instance.GetComponent<AudioSource>().clip.name}: {AudioManager.Instance.GetComponent<AudioSource>().time:0.00}/{AudioManager.Instance.GetComponent<AudioSource>().clip.length:0.00}";
            musicSlider.value = AudioManager.Instance.GetComponent<AudioSource>().time / AudioManager.Instance.GetComponent<AudioSource>().clip.length;

            if (Input.GetKey(KeyCode.Escape))
            {

                // Check if quitTime exceeds quitTimer
                if (quitTime >= quitTimer && !quitPanel.activeSelf)
                {
                    audioMixer.SetFloat("Lowpass", 500);
                    quitPanel2.color = new Color(quitPanel2.color.r, quitPanel2.color.g, quitPanel2.color.b, 1.0f); // Set color to fully opaque
                    Application.Quit(); // Quit application
                }
                else if (quitTime < quitTimer && !quitPanel.activeSelf)
                {
                    UnityEngine.Debug.Log(quitTime);
                    quitTime += Time.unscaledDeltaTime;
                    quitPanel2.color = Color.Lerp(new Color(0, 0, 0, 0), new Color(0, 0, 0, 1), quitTime / quitTimer);
                    float startValue = 22000;
                    float currentValue = Mathf.Lerp(startValue, 500, quitTime / quitTimer);

                    audioMixer.SetFloat("Lowpass", currentValue);

                }


            }
            else
            {

                quitTime = 0f;
                quitPanel2.color = new Color(quitPanel2.color.r, quitPanel2.color.g, quitPanel2.color.b, 0f); // Set color to fully transparent
                audioMixer.SetFloat("Lowpass", 22000);
            }

            if ((quitTime < quitTimer || quitTime >= quitTimer) && quitPanel.activeSelf)
            {
                audioMixer.SetFloat("Lowpass", data.lowpassValue);
            }

            if (mainPanel.activeSelf && AudioManager.Instance.GetComponent<AudioSource>().loop)
            {
                AudioManager.Instance.GetComponent<AudioSource>().loop = false;
            }
            if (playlevelInfoParent.childCount == 0)
            {
                nolevelerror.gameObject.SetActive(true);
                LoadLevelFromLevels();
            }
            else
            {
                nolevelerror.gameObject.SetActive(false);

            }
        }


        void Update()
        {

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ToggleMenuPanel(mainPanel);
            }
            bool hasInput = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space);
            idleTimer += Time.fixedDeltaTime;
            if (!hasInput)
            {
                // Player is idle, start the idle animation immediately if it's not already playing
                if (idleTimer > idleTimeThreshold || Input.GetKeyDown(KeyCode.F1))
                {
                    animator.SetTrigger("StartIdle");
                    animator.ResetTrigger("StopIdle");
                }

            }
            else if (hasInput && !IsPointerOverUIButNotButton())
            {
                idleTimer = 0;  // Resetting idleTimer when there is input
                animator.SetTrigger("StopIdle");
                animator.ResetTrigger("StartIdle");
            }
            if (Input.GetKeyDown(KeyCode.F5))
            {
                LoadLevelFromLevels();
                LoadLevelsFromFiles();
                Account.Instance.CalculateXPRequirements();
                Account.Instance.GainXP(0);
                Account.Instance.LoadData();
                Notifications.Notifications.instance.Notify($"Level list, levels and xp are reloaded. \n{levelInfoParent.childCount} levels total, {Account.Instance.totalXP} xp", null);
            }
            if (Input.GetKeyDown(KeyCode.F2))
            {
                additionalPanel.SetActive(!additionalPanel.active);
            }

            if (Input.GetKeyDown(KeyCode.F3))
            {
                FunMode();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Check if the clicked position is outside the panel
            RectTransform rectTransform = musicPanel.GetComponent<RectTransform>();
            if (!RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition))
            {
                // Click is outside the panel, disable it
                musicPanel.SetActive(false);
            }
        }
    }
}