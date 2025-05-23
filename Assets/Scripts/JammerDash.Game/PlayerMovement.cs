using System;
using System.Collections;
using System.Collections.Generic;
using JammerDash.Audio; 
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using UnityEngine.InputSystem;
using JammerDash.Tech;
using JammerDash.Difficulty;
using System.Linq;
using UnityEngine.Video;

namespace JammerDash.Game.Player
{
    public class PlayerMovement : MonoBehaviour 
    {
        [Header("Prefabs")]
        public GameObject goodTextPrefab;
        public GameObject normalTextPrefab;
        public GameObject okTextPrefab;
        public GameObject badTextPrefab;
        public GameObject destroyParticlesPrefab;

        [Header("Scene objects")]
        public Transform cam;
        public LayerMask cubeLayerMask;
        public LayerMask longcubeLayerMask; 
        public Slider hpSlider;
        public CubeCounter counter;
        public Text combotext;
        public Animation cubeanim;
        public Animation movement;
        public Text scoreText;
        public Text keyText;
        public Text acc;

        [Header("Sound effects")]
        public AudioClip[] hitSounds;
        public AudioSource sfxS;
        public AudioClip jump;
        public AudioClip[] hit;
        public AudioClip impact;
        public AudioClip fail;
        public AudioSource music;

        [Header("Player status")]
        private int hpint;
        public float health;
        public float maxHealth;
        public HashSet<GameObject> passedCubes = new HashSet<GameObject>();
        private HashSet<GameObject> activeCubes = new HashSet<GameObject>();
        private HashSet<GameObject> missedCubes = new HashSet<GameObject>();
        public List<GameObject> cubes = new List<GameObject>();
        public List<GameObject> longCubes = new List<GameObject>();
        public int k;
        public int l;
        public float scoreMultiplier = 1f;


        [Header("UI")]
        public GameObject deathPanel;

        [Header("SP Calculator")]
        public float accuracyWeight = 0.56f;
        public float comboWeight = 0.25f;
        public float movementEfficiencyWeight = 0.14f;
        public float strategicDecisionMakingWeight = 0.13f;
        public float adaptabilityWeight = 0.15f;
        public float levelCompletionTimeWeight = 0.1f;
        public float _performanceScore;

        private float _gameDifficulty;

        [Header("Others")]
        private float jumpHeight = 1f;
        private float minY = -1f;
        private float maxY = 4f;
        public int combo;
        public int highestCombo;
        bool isDying = false;
        bool invincible = false;
        public int Total = 0;
        private bool bufferActive = false;
        public int maxScore = 1000000;
        public float factor;
        public int misses;
        public int five;
        public int three;
        public int one;
        public int SPInt;
        private bool isBufferRunning = false;

        public GameObject flashlightOverlay;
        public float rememberDuration = 2f;
        public float rememberTimer = 0f;
        private List<GameObject> notes = new List<GameObject>();

        public GameObject[] keys;

        private VideoPlayer videoPlayer;
private FinishLine finishLine;
private bool hasAutoMove;
private bool hasAuto;
private bool hasEasyMode;
private Camera mainCam;

        private void Awake()
        {
            music = AudioManager.Instance.source;
        }
        private void Start()
        {music.time = 0f;
            CustomLevelDataManager.Instance.sceneLoaded = false;
           if (CustomLevelDataManager.Instance.playerhp != 0)
            {
                maxHealth = CustomLevelDataManager.Instance.playerhp;
                hpSlider.maxValue = CustomLevelDataManager.Instance.playerhp;
            }
            else
                maxHealth = 300;
            InputSystem.pollingFrequency = 1000;
            SettingsData data = SettingsFileHandler.LoadSettingsFromFile();

            switch (data.hitType) 
            {
                case 1:
                    goodTextPrefab = Resources.Load<GameObject>("RinHit");
                    okTextPrefab = Resources.Load<GameObject>("RinOK");
                    normalTextPrefab = Resources.Load<GameObject>("RinNormal");
                    badTextPrefab = Resources.Load<GameObject>("RinMiss");
                    break;
                case 2:
                    goodTextPrefab = Resources.Load<GameObject>("NumHit");
                    okTextPrefab = Resources.Load<GameObject>("NumOK");
                    normalTextPrefab = Resources.Load<GameObject>("NumNormal");
                    badTextPrefab = Resources.Load<GameObject>("NumMiss");
                    break;
                default:
                    goodTextPrefab = Resources.Load<GameObject>("good");
                    okTextPrefab = Resources.Load<GameObject>("crap");
                    normalTextPrefab = Resources.Load<GameObject>("ok");
                    badTextPrefab = Resources.Load<GameObject>("bad");
                    break;
            }
            InvokeRepeating(nameof(UpdateText), 0, 0.5f);
           videoPlayer = FindFirstObjectByType<VideoPlayer>();
    finishLine = FindFirstObjectByType<FinishLine>();
    mainCam = Camera.main;
            scoreMultiplier = CustomLevelDataManager.Instance.scoreMultiplier;
            health = maxHealth;
            Invoke(nameof(Late), 1);
        }
      
        void Late() {
            
            cubes = GameObject.FindGameObjectsWithTag("Cubes").ToList();
            longCubes = GameObject.FindGameObjectsWithTag("LongCube").ToList();
            
        }

        private void OnJump()
        {
            if (transform.position.y < maxY)
              transform.position += new Vector3(0f, jumpHeight, 0f);
            
        }
        private void OnBoost()
        {
            if (transform.position.y < maxY - 1)
            {
                transform.position += new Vector3(0f, jumpHeight * 2f, 0f);
                sfxS.PlayOneShot(jump);

            }

        } 
        private void OnLowBoost()
        {
            if (transform.position.y > 0)
            {
                transform.position += new Vector3(0f, jumpHeight * -2f, 0f);
                sfxS.PlayOneShot(jump);

            }

        }
        private void OnCrouch()
        {
            if (transform.position.y > minY && !isDying)
            {
                transform.position -= new Vector3(0f, jumpHeight, 0f);
            }
        }

        private void OnHit()
        {
            
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 1f, cubeLayerMask);

            if (hit.collider != null)
            {
                HandleHit(hit);
            }
            else
            {
                HandleMiss();
            }
        }

        private void OnResetPosition()
        {
            if (transform.position.y > -1)
            {
                
                transform.position = new Vector3(transform.position.x, -1, transform.position.z);
            }
        }
        private void OnTop()
        {
            if (transform.position.y < 4)
            {
                
                transform.position = new Vector3(transform.position.x, 4, transform.position.z);
            }
        }

        private void FixedUpdate()
        {
            if (FindNearestObjectDistance() < 49)
                health -= 0.25f;
           
            hpint = (int)health;
           
           
            if (health > maxHealth)
            {
                health = maxHealth;
            }
            hpSlider.value = health;
            hpSlider.maxValue = maxHealth;


            combotext.text = combo.ToString() + "x";



            if (combo < 0)
            {
                combo = 0;
            }
                scoreText.text = $"{counter.score}";
        }

        void UpdateText() {
            float playerPositionInSeconds = transform.position.x / 7;
            float finishLinePositionInSeconds = FindFirstObjectByType<FinishLine>().transform.position.x / 7;

            // Calculate time in minutes and seconds
            int playerMinutes = Mathf.FloorToInt(playerPositionInSeconds / 60);
            int playerSeconds = Mathf.FloorToInt(playerPositionInSeconds % 60);

            int finishLineMinutes = Mathf.FloorToInt(finishLinePositionInSeconds / 60);
            int finishLineSeconds = Mathf.FloorToInt(finishLinePositionInSeconds % 60);

            // Format time strings
            string playerTime = string.Format("{0}:{1:00}", playerMinutes, playerSeconds);
            string finishLineTime = string.Format("{0}:{1:00}", finishLineMinutes, finishLineSeconds);

            keyText.text = $"{playerTime}\t\t\t\t\t\t{finishLineTime}";
        }
            public float fadeDistance = 3f; 
            public LayerMask objectLayer;
            public float fadeSpeed = 2f;
      private void Update()
{ 
    if (!CustomLevelDataManager.Instance.modStates.ContainsKey(ModType.noDeath)) {
            if (health <= 0 && Time.timeScale > 0)
            {
                Time.timeScale -= 0.25f * Time.fixedDeltaTime;
                music.pitch = Time.timeScale;
                FindFirstObjectByType<VideoPlayer>().playbackSpeed = Time.timeScale;
                health = 0;
                isDying = true;

            }

            if (health <= 0 && Time.timeScale <= 0.1f)
            {
                EndLife();
                Time.timeScale = 0f;
                health = 0;
            }
        } 
        float pitch;
        music.outputAudioMixerGroup.audioMixer.GetFloat("MasterPitch", out pitch);
        
    PauseMenu pauseMenu = FindFirstObjectByType<PauseMenu>();
        if (music.isPlaying)
        {
            float adjustedSpeed = (hasEasyMode ? 5f : 7f) * pitch;
            float targetX = music.time * adjustedSpeed;



            transform.position = new Vector3(
                Mathf.MoveTowards(transform.position.x, targetX, adjustedSpeed * Time.deltaTime),
                transform.position.y,
                -1
            );
        }
        else
        {
            if (transform.position.x > 0) {
            float speed = (hasEasyMode ? 5 : 7) * pitch;
            transform.Translate(speed * Time.deltaTime * Vector2.right);
            }
            else {
                transform.Translate (7 * Time.deltaTime * Vector2.right);
                if (Input.GetKeyDown(KeyCode.Escape)) {
                    pauseMenu.Menu();
                }
            }
        }

        
    
    if (isDying) return; // Early exit if the player is dying

    // Cache dictionary lookups
    var modStates = Mods.instance.modStates;
    var levelStates = CustomLevelDataManager.Instance.modStates;

    bool hasRememberMod = modStates.ContainsKey(ModType.remember);
    bool hasHiddenMod = modStates.ContainsKey(ModType.hidden);
    hasAutoMove = levelStates.ContainsKey(ModType.autoMove);
    hasAuto = levelStates.ContainsKey(ModType.auto);
    hasEasyMode = levelStates.ContainsKey(ModType.easy);

    // Toggle flashlight overlay
    flashlightOverlay.SetActive(hasRememberMod);

    // Handle fading objects
    if (hasHiddenMod)
    {
        FadeObjectsInFrontOfPlayer();
    }

    // Set video playback speed
    if (Mods.instance.master.GetFloat("MasterPitch", out float vidSpeed))
    {
        videoPlayer.playbackSpeed = vidSpeed;
    }

    // Handle movement
    if (hasAutoMove || hasAuto)
    {
        MoveToNextCube();
    }
    else
    {
        HandlePlayerInput();
    }

    // Handle auto-hit
    if (hasAuto)
    {
        AutoHit();
    }

   

    // Optimize camera movement
    float distanceToFinish = Vector2.Distance(mainCam.transform.position, finishLine.transform.position);
    if (mainCam.transform.position.x < finishLine.transform.position.x)
    {
        Vector3 targetPos = new Vector3(
            distanceToFinish <= 0 ? finishLine.transform.position.x : transform.position.x + 6, 
            0.7f, -10);

        mainCam.transform.position = targetPos;
    }

    // Optimize performance calculations
    _performanceScore = new ShinePerformance(
        five, three, one, misses, combo, highestCombo,
        CustomLevelDataManager.Instance.diff,
        CustomLevelDataManager.Instance.data.levelLength,
        CustomLevelDataManager.Instance.data.cubePositions.Count + CustomLevelDataManager.Instance.data.longCubePositions.Count,
        CustomLevelDataManager.Instance.data.sawPositions.Count,
        CustomLevelDataManager.Instance.data.bpm,
        (float)counter.accCount / Total * 100
    ).PerformanceScore;

    if (_performanceScore == float.PositiveInfinity || _performanceScore == float.NegativeInfinity || float.IsNaN(_performanceScore) || _performanceScore < 0) {
        _performanceScore = 0;
    }

    SPInt = Mathf.RoundToInt(_performanceScore);


    // Update accuracy display
    float acc = Total > 0 ? (float)counter.accCount / Total * 100 : 0;
    if (float.IsNaN(acc)) {
        acc = 100;
    }
    this.acc.text = $"{acc:F2}% | {counter.GetTier(acc)} | {SPInt:F0} sp";

    // Check for break message
    float nearestDistance = FindNearestObjectDistance();

    if (pauseMenu != null)
    {
        RawImage image = pauseMenu.image;
        Color currentColor = image.color;

        if (nearestDistance > 49)
        { 
            keyText.text = "Break!";
            // Only lerp if current alpha is below 50%
            if (currentColor.a < 0.5f)
            {
                float newAlpha = Mathf.Lerp(currentColor.a, 0.5f, Time.deltaTime * 0.5f);
                image.color = new Color(currentColor.r, currentColor.g, currentColor.b, newAlpha);
            }
        }
        else
        {
                float newAlpha = Mathf.Lerp(currentColor.a, pauseMenu.dim.value / 100, Time.deltaTime * 0.5f);

            // Instantly revert to dim alpha when distance is below 50
            image.color = new Color(currentColor.r, currentColor.g, currentColor.b, newAlpha);
        }
    }

    // Optimize key input handling
    HandleKeyInputs();
}

private void HandlePlayerInput()
{
    if (Input.GetKeyDown(KeybindingManager.up)) OnJump();
    else if (Input.GetKeyDown(KeybindingManager.down)) OnCrouch();
    else if (Input.GetKeyDown(KeybindingManager.boost)) OnBoost();
    else if (Input.GetKeyDown(KeybindingManager.lowboost)) OnLowBoost();
    else if (Input.GetKeyDown(KeybindingManager.top)) OnTop();
    else if (Input.GetKeyDown(KeybindingManager.ground)) OnResetPosition();
    else if (Input.GetKeyDown(KeybindingManager.hit1) || Input.GetKeyDown(KeybindingManager.hit2)) OnHit();
    else if (SettingsFileHandler.LoadSettingsFromFile().mouseHits) if(Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) OnHit();
}

private void HandleKeyInputs()
{
    if (Input.GetKeyDown(KeybindingManager.hit1))
    {
        AnimateKey(0, ref k);
    }
    else if (Input.GetKeyDown(KeybindingManager.hit2))
    {
        AnimateKey(1, ref l);
    }

    if (SettingsFileHandler.LoadSettingsFromFile().mouseHits) {
        if (Input.GetMouseButtonDown(0))
    {
        AnimateKey(0, ref k);
    }
    else if (Input.GetMouseButtonDown(1))
    {
        AnimateKey(1, ref l);
    }
    }
}

private void AnimateKey(int index, ref int counter)
{
    var keyAnim = keys[index].GetComponent<Animation>();
    keyAnim.Stop("keyHit");
    keyAnim.Play("keyHit");

    counter++;
    keys[index].GetComponent<Text>().text = counter.ToString();
}
          private Dictionary<GameObject, Coroutine> fadingObjects = new Dictionary<GameObject, Coroutine>();
        void FadeObjectsInFrontOfPlayer()
    {
        // Get all objects in a cone in front of the player
        Collider2D[] hits = Physics2D.OverlapBoxAll(transform.position + transform.right * fadeDistance, new Vector2(5, 5), 0, objectLayer);

        // Process objects to fade them
        foreach (var hit in hits)
        {
            GameObject obj = hit.gameObject;
            if (!fadingObjects.ContainsKey(obj))
            {
                Coroutine fadeCoroutine = StartCoroutine(FadeOut(obj));
                fadingObjects[obj] = fadeCoroutine;
            }
        }
    }

    IEnumerator FadeOut(GameObject obj)
    {
        SpriteRenderer spriteRenderer = obj.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) yield break;

        Color originalColor = spriteRenderer.color;
        while (spriteRenderer.color.a > 0f)
        {
            Color newColor = spriteRenderer.color;
            newColor.a -= Time.deltaTime * fadeSpeed;
            spriteRenderer.color = newColor;
            yield return null;
        }

        spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
        fadingObjects.Remove(obj);
    }
        private void MoveToNextCube()
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 10, cubeLayerMask);
            if (colliders.Length > 0)
            {
            Collider2D nearestCube = colliders
                .Where(c => !IsSawAboveOrBelow(c))
                .OrderBy(c => Vector2.Distance(transform.position, c.transform.position))
                .FirstOrDefault();

            if (nearestCube != null)
            {
                transform.position = new Vector3(transform.position.x, nearestCube.transform.position.y, transform.position.z);
            }
            else {
                EvadeSaws();
            }
            }
        }

        private bool IsSawAboveOrBelow(Collider2D cube)
        {
            float cubeY = cube.transform.position.y;

            Collider2D[] saws = Physics2D.OverlapCircleAll(cube.transform.position, 1, LayerMask.GetMask("Saw"));
            foreach (var saw in saws)
            {
            float sawY = saw.transform.position.y;
            if ((sawY > cubeY) || (sawY < cubeY))
            {
                return true;
            }
            }
            return false;
        }

        private void EvadeSaws()
        {
            Collider2D[] sawsInFront = Physics2D.OverlapCircleAll(transform.position + Vector3.right * 2, 1, LayerMask.GetMask("Saw"));
            Collider2D[] sawsAbove = Physics2D.OverlapCircleAll(transform.position + Vector3.up * 1, 1, LayerMask.GetMask("Saw"));
            Collider2D[] sawsBelow = Physics2D.OverlapCircleAll(transform.position + Vector3.down * 1, 1, LayerMask.GetMask("Saw"));

            if (sawsInFront.Length > 0)
            {
            if (sawsAbove.Length == 0 && transform.position.y < maxY)
            {
                OnJump();
            }
            else if (sawsBelow.Length == 0 && transform.position.y > minY)
            {
                OnCrouch();
            }
            else
            {
                MoveToFreeYLevel();
            }
            }
        }
        private void MoveToFreeYLevel()
        {
            for (float y = minY; y <= maxY; y += 1f)
            {
            Collider2D[] sawsAtY = Physics2D.OverlapCircleAll(new Vector2(transform.position.x, y), 1, LayerMask.GetMask("Saw"));
            if (sawsAtY.Length == 0)
            {
                transform.position = new Vector3(transform.position.x, y, transform.position.z);
                break;
            }
            }
        }
        private void AutoHit()
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 1f, cubeLayerMask);
            if (hit.collider != null && CustomLevelDataManager.Instance.modStates.ContainsKey(ModType.auto))
            {
            StartCoroutine(WaitAndHandleHit(hit));
            k++;
            }
        }

        private IEnumerator WaitAndHandleHit(RaycastHit2D hit)
        {
            while (Vector2.Distance(transform.position, hit.transform.position) > 0.05f)
            {
            yield return null;
            }
            HandleHit(hit);
        }

        void HandleHit(RaycastHit2D hit)
        {
            if (hit.transform.position.y == transform.position.y)
            {
            if (!hit.transform.name.Contains("hitter02"))
            {
                passedCubes.Add(hit.collider.gameObject);
                DestroyCube(hit.collider.gameObject);

                Animation anim = combotext.GetComponent<Animation>();
                if (highestCombo <= combo)
                {
                highestCombo++;
                }
                combo++;

                StartCoroutine(ChangeScore(Vector2.Distance(hit.collider.transform.position, transform.position), hit));

                anim.Stop("comboanim");
                anim.Play("comboanim");
            }
            }
            else if (passedCubes.Count > 0 && FindNearestObjectDistance() < 2)
            {
            GameObject nearestCube = hit.collider.gameObject;
            if (!missedCubes.Contains(nearestCube))
            {
                missedCubes.Add(nearestCube);
                HandleBadHit();
                hit.collider.enabled = false;
            }
            }
        }

        void HandleMiss()
        {
            
            float nearestDistance = FindNearestObjectDistance();

            if (passedCubes.Count > 0 && nearestDistance < 2)
            {
                HandleBadHit();
            }
        }

     public float FindNearestObjectDistance()
{
    float nearestDistance = Mathf.Infinity;
    Vector2 origin = (Vector2)transform.position + (Vector2)transform.right * 0.5f; // Small offset to avoid self-detection
    Vector2 direction = transform.right;
    float detectionRange = 100f;
    float boxWidth = 0.5f;
    float boxHeight = 10f;

    RaycastHit2D[] hits = Physics2D.BoxCastAll(origin, new Vector2(boxWidth, boxHeight), 0f, direction, detectionRange);

    foreach (RaycastHit2D hit in hits)
    {
        if (hit.collider == null || hit.collider.gameObject == gameObject)
            continue;

        float distance = hit.distance;

        if (distance < nearestDistance)
        {
            nearestDistance = distance;
        }
    }

    // Debug the BoxCast
    Debug.DrawRay(origin, direction * detectionRange, Color.red, 0.1f);

    return nearestDistance == Mathf.Infinity ? 9999 : nearestDistance;
}


        void HandleBadHit()
        {
            if (AudioManager.Instance != null && AudioManager.Instance.hits)
            {
                ShowBadText();
            }
            if (CustomLevelDataManager.Instance.modStates.ContainsKey(ModType.suddenDeath) || CustomLevelDataManager.Instance.modStates.ContainsKey(ModType.perfect))
            {
               health -= int.MaxValue;
            }
            counter.destroyedCubes -= 100 - combo;
            sfxS.PlayOneShot(fail);

            StartCoroutine(ChangeTextCombo());
        }

        

        IEnumerator ChangeTextCombo()
        {
            float lerpSpeed = 1f;
            float lerpTimer = 0f;
            misses++;
            health -= 20;
            combo = 0;
            counter.score -= Mathf.RoundToInt(maxScore * 3 / counter.cubes.Length);
            if (counter.score < 0)
            counter.score = 0;
            while (lerpTimer < 1f)
            {
                lerpTimer += Time.fixedDeltaTime * lerpSpeed;
                combotext.color = Color.Lerp(Color.red, Color.white, lerpTimer);
                yield return null;
            }
            lerpTimer = 0f;
           

        }
        public RectTransform accuracyBar;
        public GameObject deviationMarker;
        private List<GameObject> activeMarkers = new List<GameObject>();
        private float maxDeviation = 0.75f;
        public int maxMarkers = 100; // Limit the number of markers
        public Image averageTimingImage; 
        private List<float> playerDistances = new List<float>();

        private void CreateDeviationMarker(Vector2 playerPosition, Vector2 cubePosition)
        {
            if (accuracyBar == null || deviationMarker == null) return;

            // Calculate the player distance relative to the cube's position (positive or negative)
            float playerDistance = playerPosition.x - cubePosition.x;  // Difference in x position

            // Instantiate a new marker from the prefab
            GameObject marker = Instantiate(deviationMarker, accuracyBar);

            // Get the width of the accuracy bar
            float barWidth = accuracyBar.rect.width;

            // Normalize the playerDistance to a value between -1 and 1 (map to the width of the bar)
            // The center of the bar (perfect hit) should be 0.
            // The -1 value will represent early hits (left), and 1 will represent late hits (right)
            float normalizedOffset = Mathf.Clamp(playerDistance / maxDeviation, -0.5f, 0.5f);

            // Convert the normalized offset to the position on the bar
            // 0 = Perfect (center), negative values go left (early), positive values go right (late)
            float markerPosition = normalizedOffset * (barWidth / 2);  // Range from -barWidth/2 to +barWidth/2

            // Position the marker within the accuracy bar (centered)
            RectTransform markerTransform = marker.GetComponent<RectTransform>();
            markerTransform.anchoredPosition = new Vector2(markerPosition, 0);

            // Get the Image component of the marker prefab to change its color
            Image markerImage = marker.GetComponent<Image>();

            // Color the marker based on how early or late the player hit
            if (Mathf.Abs(playerDistance) <= 0.25f)  // Perfect timing range (within tolerance)
            {
                markerImage.color = Color.green;  // Green for perfect
            }
            else if (Mathf.Abs(playerDistance) <= 0.38f)  // Good timing range
            {
                markerImage.color = Color.yellow;  // Yellow for good
            }
            else  // Bad timing range
            {
                markerImage.color = Color.red;  // Red for bad
            }

            // Add the current playerDistance to the list to calculate the average
            playerDistances.Add(playerDistance);

            // Update the average timing position image based on the average of all hits
            UpdateAverageTimingImage();

            // Optional: Store the active markers
            activeMarkers.Add(marker);

            // Remove old markers if the limit is exceeded
            if (activeMarkers.Count > maxMarkers)
            {
                Destroy(activeMarkers[0]);
                activeMarkers.RemoveAt(0);
            }
        }

        // Method to update the image based on the average of all hits
        private void UpdateAverageTimingImage()
        {
            // Calculate the average player distance
            float averageDistance = playerDistances.Sum() / playerDistances.Count;

            // Normalize the average distance to a value between -1 and 1
            float normalizedOffset = Mathf.Clamp(averageDistance / maxDeviation, -1f, 1f);

            // Get the width of the accuracy bar
            float barWidth = accuracyBar.rect.width;

            // Convert the normalized offset to the position on the bar
            float averagePosition = normalizedOffset * (barWidth / 2);  // Range from -barWidth/2 to +barWidth/2

            // Update the position of the average timing image (centered)
            RectTransform averageTimingRectTransform = averageTimingImage.GetComponent<RectTransform>();
            averageTimingRectTransform.anchoredPosition = new Vector2(averagePosition, 15);
        }



        IEnumerator ChangeScore(float playerDistance, RaycastHit2D hit)
        {
           

            // Create a deviation marker
            if (hit.collider != null)
            CreateDeviationMarker(transform.position, hit.transform.position);

            // Handle scoring logic
            if (playerDistance <= 0.25f)
            {
                factor = 1f;
                five++;
                counter.accCount += 5;
                Total += 5;
                if (AudioManager.Instance != null && AudioManager.Instance.hits)
                {
                    Instantiate(goodTextPrefab, transform.position, Quaternion.identity);
                }
            }
            else if (playerDistance <= 0.38f && playerDistance > 0.25f)
            {
                
            if (CustomLevelDataManager.Instance.modStates.ContainsKey(ModType.perfect))
            {
               health -= int.MaxValue;
            }
                factor = 1f / 3f;
                three++;
                counter.accCount += 3;
                Total += 5;
                if (AudioManager.Instance != null && AudioManager.Instance.hits)
                {
                    Instantiate(normalTextPrefab, transform.position, Quaternion.identity);
                }
            }
            else
            {
                if (CustomLevelDataManager.Instance.modStates.ContainsKey(ModType.perfect))
            {
               health -= int.MaxValue;
            }
                factor = 1f / 5f;
                one++;
                counter.accCount += 1;
                Total += 5;
                if (AudioManager.Instance != null && AudioManager.Instance.hits)
                {
                    Instantiate(okTextPrefab, transform.position, Quaternion.identity);
                }
            }

            counter.destroyedCubes += 50;
            float a = (float)cubes.Count + (float)longCubes.Count * 2f;
			float num = (float)maxScore * factor / Mathf.Max(a, 1f) * Mathf.Pow(counter.accCount / Mathf.Max((float)Total, 1f), 3f) * (float)combo / Mathf.Max((float)highestCombo, 1f) * scoreMultiplier;
			float newDestroyedCubes = (float)counter.score + num;
			newDestroyedCubes = (float)Mathf.RoundToInt(newDestroyedCubes);
			float elapsedTime = 0f;
			float duration = 0.1f;
			health += maxHealth / (50f * factor);
			while (elapsedTime < duration)
			{
				counter.score = (int)Mathf.Lerp((float)counter.score, newDestroyedCubes, elapsedTime / duration);
				elapsedTime += Time.deltaTime;
				scoreText.text = string.Format("{0}", counter.score);
				yield return null;
			}
			counter.score = Mathf.RoundToInt(newDestroyedCubes);
            yield return new WaitForSeconds(0.2f);
        }

      


        private void DestroyCube(GameObject cube)
        {
            if (Time.timeScale > 0)
            {
                switch (cube.name)
                {
                    case "hitter01(Clone)":
                        sfxS.PlayOneShot(hitSounds[0]);
                        break;
                    case "hitter03(Clone)":
                        sfxS.PlayOneShot(hitSounds[2]);
                        break;
                    case "hitter04(Clone)":
                        sfxS.PlayOneShot(hitSounds[3]);
                        break;
                    case "hitter05(Clone)":
                        sfxS.PlayOneShot(hitSounds[4]);
                        break;
                    case "hitter06(Clone)":
                        sfxS.PlayOneShot(hitSounds[5]);
                        break;
                    default:
                        break;
                }


                Destroy(cube);
                activeCubes.Remove(cube);

                if (counter.destroyedCubes > counter.maxScore)
                {
                    counter.destroyedCubes = counter.maxScore;
                }

            }
        }
        private void ShowBadText()
        {
            if (Time.timeScale > 0)
            {
                Instantiate(badTextPrefab, transform.position, Quaternion.identity);


            }

        }


        private void EndLife()
        {
            deathPanel.SetActive(true);
            music.pitch = 0f;
            transform.localScale = Vector3.zero;
            enabled = false;
        }


        private bool isHoldingKey1 = false;
        private GameObject currentLongCube = null;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (GetComponent<BoxCollider2D>().IsTouching(collision))
            {
                
                if (collision.tag == "Cubes" && collision.gameObject.name.Contains("hitter01"))
            {
            collision.GetComponent<Animation>().Play();
            }

            if (collision.tag == "Cubes" || collision.gameObject.name.Contains("hitter02"))
            {
                if (collision.tag == "Cubes")
                    activeCubes.Add(collision.gameObject);

            if (collision.gameObject.name.Contains("hitter02") && CustomLevelDataManager.Instance.modStates.ContainsKey(ModType.auto))
            {
                if (!bufferActive)
                {
                bufferActive = true;
                sfxS.PlayOneShot(hitSounds[1]);
                }


                combo++;
                if (highestCombo < combo)
                {
                highestCombo++;
                }

                StartCoroutine(ChangeScore(0f, new RaycastHit2D()));
                if (AudioManager.Instance != null && AudioManager.Instance.hits)
                {
                Instantiate(goodTextPrefab, transform.position, Quaternion.identity);
                }
                sfxS.PlayOneShot(hitSounds[1]);
                Animation anim = combotext.GetComponent<Animation>();

                anim.Stop("comboanim");
                anim.Play("comboanim");
            }
            }
            }

            
        }

      private void OnTriggerExit2D(Collider2D collision)
{
    // Regular cubes missed
    if (collision.tag == "Cubes" && activeCubes.Contains(collision.gameObject) && health > 0)
    {
        activeCubes.Remove(collision.gameObject);

        if (!passedCubes.Contains(collision.gameObject) && !missedCubes.Contains(collision.gameObject))
        {
            if (AudioManager.Instance != null && AudioManager.Instance.hits)
                ShowBadText();

            StartCoroutine(ChangeTextCombo());
            Total += 5;
            passedCubes.Add(collision.gameObject);
        }
    }

    // Handle normal cube miss (not hitter02)
    if (collision.tag == "Cubes" && !passedCubes.Contains(collision.gameObject))
    {
        if (AudioManager.Instance != null && AudioManager.Instance.hits)
            ShowBadText();

        health -= 20;
        Total += 5;

        if (!missedCubes.Contains(collision.gameObject))
            StartCoroutine(ChangeTextCombo());
    }

  GameObject cube = collision.gameObject;

    if (!cube.name.Contains("hitter02") && !cube.CompareTag("Cubes")) return;

    // Already handled?
    if (passedCubes.Contains(cube) || missedCubes.Contains(cube)) return;

    bool isActive = activeCubes.Contains(cube);
    bool isAlignedY = cube.transform.position.y == transform.position.y;
    bool isKeyDown = Input.GetKey(KeybindingManager.hit1) || Input.GetKey(KeybindingManager.hit2) || 
                     (SettingsFileHandler.LoadSettingsFromFile().mouseHits && (Input.GetMouseButton(0) || Input.GetMouseButton(1)));

    if (isActive && cube.name.Contains("hitter02"))
    {
        if (bufferActive && isAlignedY && isKeyDown)
        {
            // Valid hit
            activeCubes.Remove(cube);
            DestroyCube(cube);
            sfxS.PlayOneShot(hitSounds[6]);
            StartCoroutine(ChangeScore(0f, new RaycastHit2D()));
            health += 10;
            combo++;
            if (highestCombo < combo) highestCombo++;
            bufferActive = false;
        }
        else
        {
            // Miss due to wrong key or position
            activeCubes.Remove(cube);
            misses++;
            ShowBadText();
            health -= 20;
            Total += 5;
            missedCubes.Add(cube);
            bufferActive = false;
            return;
        }
    }

    // Long cube passed through without any interaction = fail it
if (collision.name.Contains("hitter02") && !interactedCubes.Contains(collision.gameObject))
{
    if (!missedCubes.Contains(collision.gameObject))
    {
        missedCubes.Add(collision.gameObject);
        ShowBadText();
        health -= 20;
        Total += 5;
        misses++;
    }
    return;
}

    }




        private IEnumerator OnTriggerEnter2DBuffer()
        {
            if (bufferActive)
            {
                health += 0.025f;
                yield return null;
            }


            yield return null;
        }
private HashSet<GameObject> interactedCubes = new HashSet<GameObject>();

private bool isHoldingLongNote = false;

        private void OnTriggerStay2D(Collider2D collision)
        {if (GetComponent<BoxCollider2D>().IsTouching(collision))
            {
            if (collision.tag == "Saw" && !isDying && !CustomLevelDataManager.Instance.modStates.ContainsKey(ModType.noDeath))
                {
                    isDying = true;
                    sfxS.PlayOneShot(hitSounds[7]);
                    health -= int.MaxValue;
                }
            }
        if (collision.gameObject.name.Contains("hitter02") && collision.transform.position.y == transform.position.y)
{
    float distance = Vector2.Distance(collision.transform.position, transform.position);
    float middle = Mathf.Abs(collision.offset.x);

    // If holding key and not already holding this long note
    bool keyPressed = Input.GetKey(KeybindingManager.hit1) || Input.GetKey(KeybindingManager.hit2) ||
                      (SettingsFileHandler.LoadSettingsFromFile().mouseHits && 
                      (Input.GetMouseButton(0) || Input.GetMouseButton(1)));

    if (keyPressed && !isHoldingLongNote)
    {
        isHoldingLongNote = true;
        bufferActive = true;
        sfxS.PlayOneShot(hitSounds[1]);

        if (distance < 1)
        {
            combo++;
            if (highestCombo < combo) highestCombo++;
            StartCoroutine(ChangeScore(0f, new RaycastHit2D()));
            if (AudioManager.Instance?.hits == true)
                Instantiate(goodTextPrefab, transform.position, Quaternion.identity);
        }
        else if (distance < middle && distance >= 1)
        {
            sfxS.PlayOneShot(hitSounds[1]);
            StartCoroutine(ChangeScore(0.30f, new RaycastHit2D()));
            if (AudioManager.Instance?.hits == true)
                Instantiate(normalTextPrefab, transform.position, Quaternion.identity);
        }
        else
        {
            sfxS.PlayOneShot(hitSounds[1]);
            health -= 20;
            combo = 0;
            StartCoroutine(ChangeScore(0.48f, new RaycastHit2D()));
            if (AudioManager.Instance?.hits == true)
                Instantiate(okTextPrefab, transform.position, Quaternion.identity);
        }

        // Mark the cube as active for holding
        if (!activeCubes.Contains(collision.gameObject))
            activeCubes.Add(collision.gameObject);

         if (!interactedCubes.Contains(collision.gameObject))
        interactedCubes.Add(collision.gameObject);
        currentLongCube = collision.gameObject;
    }

    // Reset if keys are released
    if (!keyPressed && isHoldingLongNote)
    {
        isHoldingLongNote = false;
        bufferActive = false;

        // Optional: fade or disable long cube early if they let go too soon
    }

    if (!isBufferRunning && bufferActive)
    {
        StartCoroutine(OnTriggerEnter2DBufferLoop());
    }
    else
    {
        isBufferRunning = false;
    }
}

            if (collision.name == "finishline")
            {
            health -= 0f;
            }
        }

        IEnumerator OnTriggerEnter2DBufferLoop()
        {
            isBufferRunning = true;
            while ((Input.GetKey(KeybindingManager.hit1) || Input.GetKey(KeybindingManager.hit2)) && !isDying && bufferActive)
            {
                StartCoroutine(OnTriggerEnter2DBuffer());
                yield return new WaitForSeconds(0.03f);
            }
            isBufferRunning = false;
        }
    }

}
