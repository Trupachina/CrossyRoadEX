using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;
using System;

public class GameStateControllerScript : MonoBehaviour
{
    public GameObject mainMenuCanvas;
    public GameObject playCanvas;
    public GameObject gameOverCanvas;

    public Text playScore;
    public Text gameOverScore;
    public Text topScore;
    public Text topScore1;
    public Text timerText;
    public Text livesText;

    public int score, top;
    public int lives = 3;
    public int livesFromTicket = 3;
    public float gameTimeLimit = 180f;

    public int maxSessionScore = 0;

    private GameObject currentCanvas;
    private string state;
    private GameObject player;

    private string filename = "top.txt";
    private string topFilePath;
    private const string TOP_PREF_KEY = "Top";

    [Header("Звуки")]
    public AudioSource gameOverSound;

    [Header("Музыка")]
    [Tooltip("Старое поле музыки. Оставлено для совместимости. Если MenuSong или GameSong не назначены, будет использоваться MainSong.")]
    public AudioSource MainSong;

    [Tooltip("Музыка главного меню. Играет периодически: 20 секунд музыка, 180 секунд тишина.")]
    public AudioSource MenuSong;

    [Tooltip("Основная музыка во время игры.")]
    public AudioSource GameSong;

    [Header("Периодическая музыка главного меню")]
    [Tooltip("Если включено, музыка главного меню играет периодически: играет, затем тишина, затем снова играет.")]
    [SerializeField] private bool useIntermittentMenuMusic = true;

    [Tooltip("Сколько секунд музыка главного меню играет при первом запуске и каждом повторе.")]
    [SerializeField] private float menuMusicPlaySeconds = 20f;

    [Tooltip("Сколько секунд длится тишина между повторами музыки главного меню.")]
    [SerializeField] private float menuMusicSilentSeconds = 180f;

    private Coroutine menuMusicRoutine;

    private SerialPortManager serialPortManager;
    private LevelControllerScript levelController;
    private CameraMovementScript cameraMovement;

    private Vector3 lastSafePosition;
    private Vector3 lastSafeScale;

    private float gameStartTime;
    private bool gameStarted = false;
    private bool isGameOver = false;

    public bool isInCooldown = false;
    public float damageCooldown = 2f;
    public float deathPauseDuration = 2f;

    public bool diedFromCamera = false;
    public bool diedFromTime = false;

    private GameObject mainCamera;
    public float cameraGameOverDistance = -1.5f;

    public GameObject TopScore1;
    public GameObject TopScore;
    public GameObject StartText;
    public GameObject InstructionsText;

    bool canStartGame = false;

    private bool canRestartManually = false;
    private bool resetScoreFlag = false;

    private bool cooldownRoutineRunning = false;
    private Coroutine respawnCleanupRoutine;

    private const float CameraReturnReleaseDistance = 14f;
    private const float CameraReturnTimeout = 8f;

    private Text instructionsTextComponent;
    private Text startTextComponent;

    private void Awake()
    {
        topFilePath = Path.Combine(Application.persistentDataPath, filename);
    }

    public void Start()
    {
        serialPortManager = FindObjectOfType<SerialPortManager>();
        if (serialPortManager == null)
        {
            GameObject portManagerObj = new GameObject("SerialPortManager");
            serialPortManager = portManagerObj.AddComponent<SerialPortManager>();
        }

        serialPortManager.OnCoinsReceived += HandleCoinsReceived;

        currentCanvas = mainMenuCanvas;
        player = GameObject.FindGameObjectWithTag("Player");
        mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        CacheControllers();

        CacheMenuTextComponents();

        top = LoadTopScore();

        topScore.text = "Рекорд: " + top;
        topScore1.text = "Рекорд: " + top;

        MainMenu();
    }

    private void CacheControllers()
    {
        if (levelController == null)
            levelController = FindObjectOfType<LevelControllerScript>();

        if (mainCamera == null)
            mainCamera = GameObject.FindGameObjectWithTag("MainCamera");

        if (cameraMovement == null && mainCamera != null)
            cameraMovement = mainCamera.GetComponent<CameraMovementScript>();
    }

    private void CacheMenuTextComponents()
    {
        if (InstructionsText != null)
        {
            instructionsTextComponent = InstructionsText.GetComponent<Text>();
            if (instructionsTextComponent == null)
                instructionsTextComponent = InstructionsText.GetComponentInChildren<Text>(true);
        }

        if (StartText != null)
        {
            startTextComponent = StartText.GetComponent<Text>();
            if (startTextComponent == null)
                startTextComponent = StartText.GetComponentInChildren<Text>(true);
        }
    }

    private int LoadTopScore()
    {
        if (File.Exists(topFilePath))
        {
            try
            {
                string fileContent = File.ReadAllText(topFilePath).Trim();
                if (int.TryParse(fileContent, out int loadedTop))
                    return loadedTop;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Не удалось прочитать рекорд из файла: " + ex.Message);
            }
        }

        return PlayerPrefs.GetInt(TOP_PREF_KEY, 0);
    }

    private void SaveTopScore(int newTop)
    {
        top = newTop;

        PlayerPrefs.SetInt(TOP_PREF_KEY, top);
        PlayerPrefs.Save();

        try
        {
            File.WriteAllText(topFilePath, top.ToString());
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Не удалось записать рекорд в файл: " + ex.Message);
        }

        if (topScore != null) topScore.text = "Рекорд: " + top;
        if (topScore1 != null) topScore1.text = "Рекорд: " + top;

        Debug.Log("Новый рекорд: " + top);
    }

    public void ResetTopScoreHard()
    {
        try
        {
            PlayerPrefs.DeleteKey(TOP_PREF_KEY);
            PlayerPrefs.SetInt(TOP_PREF_KEY, 0);
            PlayerPrefs.Save();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Не удалось сбросить PlayerPrefs рекорда: " + ex.Message);
        }

        try
        {
            if (File.Exists(topFilePath))
                File.Delete(topFilePath);

            File.WriteAllText(topFilePath, "0");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Не удалось сбросить файл рекорда (persistent): " + ex.Message);
        }

        TryDeleteFile(Path.Combine(Application.dataPath, filename));
        TryDeleteFile(Path.Combine(Environment.CurrentDirectory, filename));

        top = 0;
        maxSessionScore = 0;

        if (topScore != null) topScore.text = "Рекорд: " + top;
        if (topScore1 != null) topScore1.text = "Рекорд: " + top;

        Debug.Log("Рекорд ЖЁСТКО сброшен до 0.");
    }

    private void TryDeleteFile(string path)
    {
        try
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Не удалось удалить файл: " + path + " | " + ex.Message);
        }
    }

    public void ApplyStartOptionFromPrefs()
    {
        int opt = PlayerPrefs.GetInt("optionSelection", 0);
        canStartGame = (opt == 2);

        if (state == "mainmenu")
            UpdateMainMenuStartUI(opt);
    }

    private void UpdateMainMenuStartUI(int opt)
    {
        if (opt == 0 || opt == 1)
        {
            if (instructionsTextComponent != null)
                instructionsTextComponent.text = (opt == 0) ? "Вставьте жетон" : "Оплатите игру";

            bool showStart = canStartGame;

            if (InstructionsText != null) InstructionsText.SetActive(!showStart);
            if (StartText != null) StartText.SetActive(showStart);
            if (TopScore != null) TopScore.SetActive(!showStart);
        }
        else
        {
            if (startTextComponent != null)
                startTextComponent.text = "Нажмите Start";

            if (InstructionsText != null) InstructionsText.SetActive(false);
            if (StartText != null) StartText.SetActive(true);
            if (TopScore != null) TopScore.SetActive(true);
        }
    }

    private void HandleCoinsReceived(int credit)
    {
        Debug.Log($"Получен кредит: {credit}");

        int opt = PlayerPrefs.GetInt("optionSelection", 0);
        bool isTokenOrBills = (opt == 0 || opt == 1);

        if (state == "mainmenu")
        {
            if (isTokenOrBills)
            {
                if (!canStartGame)
                {
                    canStartGame = true;
                    livesText.text = "Жизни: " + lives.ToString();
                    UpdateMainMenuStartUI(opt);
                    Debug.Log("Игра готова к запуску. Нажмите пробел для старта.");
                }
                else
                {
                    IncreaseLives(credit);
                }
            }
        }
        else if (state == "play")
        {
            IncreaseLives(credit);
        }
    }

    private void OnDestroy()
    {
        StopMenuMusicLoop(true);

        if (serialPortManager != null)
            serialPortManager.OnCoinsReceived -= HandleCoinsReceived;
    }

    public void Update()
    {
        if (state == "play" && !isGameOver)
        {
            TopScore1.SetActive(true);

            if (!gameStarted)
            {
                gameStartTime = Time.time;
                gameStarted = true;
            }

            if (gameTimeLimit <= 0f)
            {
                float elapsed = Time.time - gameStartTime;
                timerText.text = "Время: " + Mathf.Floor(elapsed).ToString();
            }
            else
            {
                float remainingTime = gameTimeLimit - (Time.time - gameStartTime);
                timerText.text = "Время: " + Mathf.Floor(Mathf.Max(0f, remainingTime)).ToString();

                if (remainingTime < 0f)
                {
                    lives = 0;
                    diedFromTime = true;
                    GameOver();
                    return;
                }
            }

            livesText.text = "Жизни: " + lives.ToString();

            if (topScore != null) topScore.text = "Рекорд: " + top;
            if (topScore1 != null) topScore1.text = "Рекорд: " + top;

            playScore.text = score.ToString();

            if (score > top)
                SaveTopScore(score);
        }
        else if (state == "mainmenu")
        {
            int opt = PlayerPrefs.GetInt("optionSelection", 0);

            if (opt == 0 || opt == 1)
            {
                if (canStartGame && Input.GetKeyDown("space"))
                {
                    Play();
                    if (StartText != null) StartText.SetActive(false);
                    if (TopScore != null) TopScore.SetActive(false);
                    canStartGame = false;
                    Debug.Log("Игра запущена.");
                }
            }
            else if (opt == 2)
            {
                if (Input.GetKeyDown("space"))
                {
                    Play();
                    if (StartText != null) StartText.SetActive(false);
                    if (TopScore != null) TopScore.SetActive(false);
                    canStartGame = false;
                    Debug.Log("Игра запущена.");
                }
            }
        }
        else if (state == "gameover")
        {
            if (canRestartManually && Input.anyKeyDown)
                LoadMainScene();
        }

        if (isInCooldown && !cooldownRoutineRunning)
        {
            cooldownRoutineRunning = true;
            StartCoroutine(Cooldown());
        }
    }

    public void MainMenu()
    {
        CurrentCanvas = mainMenuCanvas;
        state = "mainmenu";
        gameStarted = false;
        gameStartTime = 0f;
        isGameOver = false;

        StopRespawnCleanupRoutine();
        CacheControllers();

        StopGameMusic();
        StartMenuMusicLoop();

        int opt = PlayerPrefs.GetInt("optionSelection", 0);
        canStartGame = (opt == 2);

        UpdateMainMenuStartUI(opt);

        if (levelController != null)
            levelController.Reset();
        else
            GameObject.Find("LevelController").SendMessage("Reset");

        player.SendMessage("Reset");

        top = LoadTopScore();
        if (topScore != null) topScore.text = "Рекорд: " + top;
        if (topScore1 != null) topScore1.text = "Рекорд: " + top;
    }

    public void Play()
    {
        StopRespawnCleanupRoutine();
        CacheControllers();

        StopMenuMusicLoop(true);
        StartGameMusic();

        CurrentCanvas = playCanvas;
        state = "play";
        score = 0;
        isGameOver = false;
        gameStarted = false;
        gameStartTime = Time.time;

        player.GetComponent<PlayerMovementScript>().canMove = true;
        if (cameraMovement != null)
            cameraMovement.moving = true;

        SavePlayerState();
    }

    public void GameOver()
    {
        CacheControllers();

        if (!isInCooldown)
        {
            lives--;

            if (lives > 0)
            {
                isInCooldown = true;
                StartCoroutine(DeathPause());
            }
            else
            {
                StopRespawnCleanupRoutine();

                StopMenuMusicLoop(true);
                StopGameMusic();

                CurrentCanvas = gameOverCanvas;
                state = "gameover";
                isGameOver = true;
                canRestartManually = false;

                if (gameOverSound != null)
                    gameOverSound.Play();

                gameOverScore.text = maxSessionScore.ToString();

                if (score > top)
                    SaveTopScore(score);

                if (topScore != null) topScore.text = "Рекорд: " + top;
                if (topScore1 != null) topScore1.text = "Рекорд: " + top;

                if (cameraMovement != null)
                    cameraMovement.moving = false;

                StartCoroutine(GameOverTransition());
            }
        }
    }

    private IEnumerator DeathPause()
    {
        CacheControllers();

        PlayerMovementScript playerMovement = player.GetComponent<PlayerMovementScript>();
        playerMovement.canMove = false;

        if (cameraMovement != null)
        {
            cameraMovement.moving = false;
            cameraMovement.Reset();
        }

        if (gameOverSound != null)
            gameOverSound.Play();

        StopGameMusic();

        yield return new WaitForSeconds(deathPauseDuration);

        if (diedFromTime)
        {
            player.transform.position = lastSafePosition;
            player.transform.localScale = lastSafeScale;
        }

        if (!diedFromCamera && !diedFromTime)
        {
            player.transform.position = lastSafePosition;
            player.transform.localScale = lastSafeScale;
        }

        StopRespawnCleanupRoutine();

        if (levelController != null)
        {
            levelController.BeginRespawnCameraProtection();
            levelController.EnsureRespawnWindowsNow();
        }

        diedFromCamera = false;
        diedFromTime = false;

        playerMovement.canMove = true;

        if (cameraMovement != null)
            cameraMovement.moving = true;

        StartGameMusic();

        respawnCleanupRoutine = StartCoroutine(ReleaseRespawnProtectionAfterCameraReturns());
    }

    private IEnumerator ReleaseRespawnProtectionAfterCameraReturns()
    {
        CacheControllers();

        float waitTimer = 0f;

        while (waitTimer < CameraReturnTimeout)
        {
            if (levelController != null)
                levelController.EnsureRespawnWindowsNow();

            if (player != null && mainCamera != null)
            {
                float distanceZ = Mathf.Abs(mainCamera.transform.position.z - player.transform.position.z);
                if (distanceZ <= CameraReturnReleaseDistance)
                    break;
            }

            waitTimer += Time.deltaTime;
            yield return null;
        }

        if (levelController != null)
        {
            levelController.EndRespawnCameraProtection();
            levelController.TrimAroundWorldZ(player.transform.position.z);
        }

        respawnCleanupRoutine = null;
    }

    private void StopRespawnCleanupRoutine()
    {
        if (respawnCleanupRoutine != null)
        {
            StopCoroutine(respawnCleanupRoutine);
            respawnCleanupRoutine = null;
        }

        if (levelController != null)
            levelController.EndRespawnCameraProtection();
    }

    private IEnumerator Cooldown()
    {
        yield return new WaitForSeconds(damageCooldown);
        isInCooldown = false;
        cooldownRoutineRunning = false;
    }

    private void SavePlayerState()
    {
        lastSafePosition = player.transform.position;
        lastSafeScale = player.transform.localScale;
    }

    private GameObject CurrentCanvas
    {
        get { return currentCanvas; }
        set
        {
            if (currentCanvas != null)
                currentCanvas.SetActive(false);

            currentCanvas = value;
            currentCanvas.SetActive(true);
        }
    }

    private IEnumerator GameOverTransition()
    {
        yield return new WaitForSeconds(2.5f);
        canRestartManually = true;

        yield return new WaitForSeconds(7.5f);

        if (state == "gameover")
            LoadMainScene();
    }

    private void LoadMainScene()
    {
        StopRespawnCleanupRoutine();
        StopMenuMusicLoop(true);
        StopGameMusic();

        SceneManager.LoadScene(0);
        state = "mainmenu";
        MainMenu();
        GameObject.Find("LevelController").SendMessage("Reset");
        player.SendMessage("Reset");
    }

    // ===================== МУЗЫКА =====================

    private AudioSource GetMenuMusicSource()
    {
        if (MenuSong != null)
            return MenuSong;

        return MainSong;
    }

    private AudioSource GetGameMusicSource()
    {
        if (GameSong != null)
            return GameSong;

        return MainSong;
    }

    private void StartMenuMusicLoop()
    {
        StopMenuMusicLoop(true);

        AudioSource source = GetMenuMusicSource();

        if (source == null)
        {
            Debug.LogWarning("GameStateControllerScript: не назначена музыка главного меню. Назначь MenuSong или MainSong.");
            return;
        }

        if (!useIntermittentMenuMusic)
        {
            source.Stop();
            source.time = 0f;
            source.loop = true;
            source.Play();
            return;
        }

        menuMusicRoutine = StartCoroutine(MenuMusicLoopCoroutine());
    }

    private IEnumerator MenuMusicLoopCoroutine()
    {
        while (state == "mainmenu")
        {
            AudioSource source = GetMenuMusicSource();

            if (source == null)
                yield break;

            source.Stop();
            source.time = 0f;
            source.loop = true;
            source.Play();

            float playSeconds = Mathf.Max(0f, menuMusicPlaySeconds);

            if (playSeconds > 0f)
                yield return new WaitForSecondsRealtime(playSeconds);

            source = GetMenuMusicSource();

            if (source != null && source.isPlaying)
                source.Stop();

            float silentSeconds = Mathf.Max(0f, menuMusicSilentSeconds);

            if (silentSeconds > 0f)
                yield return new WaitForSecondsRealtime(silentSeconds);
            else
                yield return null;
        }

        menuMusicRoutine = null;
    }

    private void StopMenuMusicLoop(bool stopAudioSource)
    {
        if (menuMusicRoutine != null)
        {
            StopCoroutine(menuMusicRoutine);
            menuMusicRoutine = null;
        }

        if (!stopAudioSource)
            return;

        AudioSource source = GetMenuMusicSource();

        if (source != null && source.isPlaying)
            source.Stop();
    }

    private void StartGameMusic()
    {
        StopMenuMusicLoop(true);

        AudioSource source = GetGameMusicSource();

        if (source == null)
        {
            Debug.LogWarning("GameStateControllerScript: не назначена игровая музыка. Назначь GameSong или MainSong.");
            return;
        }

        source.Stop();
        source.time = 0f;
        source.loop = true;
        source.Play();
    }

    private void StopGameMusic()
    {
        AudioSource source = GetGameMusicSource();

        if (source != null && source.isPlaying)
            source.Stop();
    }

    void OnApplicationQuit()
    {
        StopMenuMusicLoop(true);
        StopGameMusic();

        if (serialPortManager != null)
            serialPortManager.OnCoinsReceived -= HandleCoinsReceived;
    }

    private void IncreaseLives(int credit = 1)
    {
        lives += livesFromTicket * credit;
        livesText.text = "Жизни: " + lives.ToString();
        Debug.Log($"Количество жизней увеличено на {livesFromTicket * credit}: {lives}");
    }

    public void SetLives(int newLives) { lives = newLives; }
    public void SetLivesFromTicket(int newLivesFromTicket) { livesFromTicket = newLivesFromTicket; }
    public void SetGameTimeLimit(float newTimeLimit) { gameTimeLimit = newTimeLimit; }
}