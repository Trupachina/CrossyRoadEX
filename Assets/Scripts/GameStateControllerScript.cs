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

    public AudioSource gameOverSound;
    public AudioSource MainSong;

    private SerialPortManager serialPortManager;

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

    // ===== cache Text components for dynamic message (optional but fixes “не меняется”) =====
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

        CacheMenuTextComponents();

        // Загружаем рекорд
        top = LoadTopScore();

        topScore.text = "Рекорд: " + top;
        topScore1.text = "Рекорд: " + top;

        MainMenu();
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

    // ====== TOP SCORE ======
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

    // ===== NEW: применить режим старта (вызываем из меню настроек после смены optionSelection) =====
    public void ApplyStartOptionFromPrefs()
    {
        int opt = PlayerPrefs.GetInt("optionSelection", 0);

        // При смене режима сбрасываем “готовность” (кроме режима Start)
        canStartGame = (opt == 2);

        if (state == "mainmenu")
            UpdateMainMenuStartUI(opt);
    }

    private void UpdateMainMenuStartUI(int opt)
    {
        // opt: 0 жетон, 1 купюры, 2 start
        // Для жетон/купюры: пока не оплатили -> показываем инструкцию
        // После оплаты (canStartGame=true) -> показываем StartText
        if (opt == 0 || opt == 1)
        {
            if (instructionsTextComponent != null)
                instructionsTextComponent.text = (opt == 0) ? "Вставьте жетон" : "Оплатите игру";

            bool showStart = canStartGame;

            if (InstructionsText != null) InstructionsText.SetActive(!showStart);
            if (StartText != null) StartText.SetActive(showStart);

            if (TopScore != null) TopScore.SetActive(!showStart);
        }
        else // opt == 2
        {
            if (startTextComponent != null)
                startTextComponent.text = "Нажмите Start";

            if (InstructionsText != null) InstructionsText.SetActive(false);
            if (StartText != null) StartText.SetActive(true);

            if (TopScore != null) TopScore.SetActive(true);
        }
    }

    // ====== CREDITS (жетон + купюры одинаково) ======
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

                    // централизованно обновляем UI
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

            // ВАЖНО: купюры теперь как жетон — старт только после оплаты (canStartGame)
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
        gameStartTime = 0;

        int opt = PlayerPrefs.GetInt("optionSelection", 0);
        canStartGame = (opt == 2); // только Start готов сразу

        UpdateMainMenuStartUI(opt);

        GameObject.Find("LevelController").SendMessage("Reset");
        player.SendMessage("Reset");

        top = LoadTopScore();
        if (topScore != null) topScore.text = "Рекорд: " + top;
        if (topScore1 != null) topScore1.text = "Рекорд: " + top;
    }

    public void Play()
    {
        CurrentCanvas = playCanvas;
        state = "play";
        score = 0;
        isGameOver = false;
        gameStartTime = Time.time;

        player.GetComponent<PlayerMovementScript>().canMove = true;
        mainCamera.GetComponent<CameraMovementScript>().moving = true;

        SavePlayerState();
    }

    public void GameOver()
    {
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
                CurrentCanvas = gameOverCanvas;
                state = "gameover";
                isGameOver = true;
                canRestartManually = false;

                gameOverSound.Play();
                gameOverScore.text = maxSessionScore.ToString();

                if (score > top)
                    SaveTopScore(score);

                if (topScore != null) topScore.text = "Рекорд: " + top;
                if (topScore1 != null) topScore1.text = "Рекорд: " + top;

                mainCamera.GetComponent<CameraMovementScript>().moving = false;

                StartCoroutine(GameOverTransition());
            }
        }
    }

    private IEnumerator DeathPause()
    {
        player.GetComponent<PlayerMovementScript>().canMove = false;
        GameObject camera = mainCamera;

        camera.GetComponent<CameraMovementScript>().moving = false;

        gameOverSound.Play();
        MainSong.Stop();

        camera.GetComponent<CameraMovementScript>().Reset();

        yield return new WaitForSeconds(deathPauseDuration);

        if (diedFromTime)
        {
            player.transform.position = lastSafePosition;
            player.transform.localScale = lastSafeScale;
            camera.GetComponent<CameraMovementScript>().Reset();
        }

        if (!diedFromCamera && !diedFromTime)
        {
            player.transform.position = lastSafePosition;
            player.transform.localScale = lastSafeScale;
        }

        diedFromCamera = false;
        diedFromTime = false;

        player.GetComponent<PlayerMovementScript>().canMove = true;
        camera.GetComponent<CameraMovementScript>().moving = true;

        MainSong.Play();
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
        SceneManager.LoadScene(0);
        state = "mainmenu";
        MainMenu();
        GameObject.Find("LevelController").SendMessage("Reset");
        player.SendMessage("Reset");
    }

    void OnApplicationQuit()
    {
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
