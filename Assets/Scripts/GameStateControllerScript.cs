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
    public AudioSource gameOverSound;
    public AudioSource MainSong;

    // Убрали прямую работу с SerialPort
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

    public void Start()
    {
        // Получаем ссылку на менеджер serial порта
        serialPortManager = FindObjectOfType<SerialPortManager>();
        if (serialPortManager == null)
        {
            // Создаем новый объект с менеджером, если его нет на сцене
            GameObject portManagerObj = new GameObject("SerialPortManager");
            serialPortManager = portManagerObj.AddComponent<SerialPortManager>();
        }

        // Подписываемся на событие получения монет
        serialPortManager.OnCoinsReceived += HandleCoinsReceived;

        currentCanvas = mainMenuCanvas;
        player = GameObject.FindGameObjectWithTag("Player");
        mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        StartText.SetActive(false);

        // Читаем лучший результат
        if (File.Exists(Application.dataPath + "/" + filename))
        {
            var sr = new StreamReader(Application.dataPath + "/" + filename);
            string fileContent = sr.ReadLine();
            sr.Close();

            if (int.TryParse(fileContent, out int loadedTop))
            {
                top = loadedTop;
            }
        }

        topScore.text = "Рекорд: " + top;
        topScore1.text = "Рекорд: " + top;

        MainMenu();
    }

    // Обработчик события получения монет
    private void HandleCoinsReceived(int credit)
    {
        Debug.Log($"Получен кредит: {credit}");

        // Всегда получаем актуальное значение optionSelection
        int currentOptionSelection = PlayerPrefs.GetInt("optionSelection", 0);

        if (state == "mainmenu")
        {
            if (currentOptionSelection == 0) // Если выбран вариант "Жетон"
            {
                if (!canStartGame)
                {
                    // Разрешаем запуск игры
                    canStartGame = true;
                    livesText.text = "Жизни: " + lives.ToString();
                    InstructionsText.SetActive(false);
                    StartText.SetActive(true);
                    TopScore.SetActive(false);
                    Debug.Log("Игра готова к запуску. Нажмите пробел для старта.");
                }
                else
                {
                    // Добавляем жизни
                    IncreaseLives(credit);
                }
            }
            else if (currentOptionSelection == 1) // Если выбран вариант "Купюры"
            {
                // Для варианта "Купюры" просто добавляем жизни
                IncreaseLives(credit);
            }
            // Для варианта "Кнопка Start" (2) ничего не делаем при получении жетона
        }
        else if (state == "play")
        {
            // Добавляем жизни во время игры
            IncreaseLives(credit);
        }
    }

    private void OnDestroy()
    {
        // Отписываемся от события при уничтожении объекта
        if (serialPortManager != null)
        {
            serialPortManager.OnCoinsReceived -= HandleCoinsReceived;
        }
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

            if (gameTimeLimit == 0)
            {
                float remainingTime = gameTimeLimit + Time.time;
                timerText.text = "Время: " + Mathf.Floor(remainingTime).ToString();
            }
            else
            {
                float remainingTime = gameTimeLimit - (Time.time - gameStartTime);
                timerText.text = "Время: " + Mathf.Floor(remainingTime).ToString();

                if (remainingTime < 0 && gameTimeLimit > 0)
                {
                    lives = 0;
                    diedFromTime = true;
                    GameOver();
                    return;
                }
            }

            livesText.text = "Жизни: " + lives.ToString();

            // Обновляем лучший результат
            if (File.Exists(Application.dataPath + "/" + filename))
            {
                var sr = new StreamReader(Application.dataPath + "/" + filename);
                string fileContent = sr.ReadLine();
                sr.Close();

                if (int.TryParse(fileContent, out int loadedTop))
                {
                    top = loadedTop;
                }
            }
            else
            {
                top = PlayerPrefs.GetInt("Top", 0);
            }

            topScore.text = "Рекорд: " + top;
            playScore.text = score.ToString();

            if (score > top)
            {
                top = score;
                topScore.text = "Рекорд: " + top;
                topScore1.text = "Рекорд: " + top;

                PlayerPrefs.SetInt("Top", top);
                PlayerPrefs.Save();

                using (StreamWriter sw = new StreamWriter(Application.dataPath + "/" + filename, false))
                {
                    sw.Write(top);
                }

                Debug.Log("Новый рекорд: " + top);
            }
        }
        else if (state == "mainmenu")
        {
            int currentOptionSelection = PlayerPrefs.GetInt("optionSelection", 0);

            if (currentOptionSelection == 0) // Жетон
            {
                // Для варианта "Жетон" обрабатываем нажатие пробела только если canStartGame = true
                if (canStartGame && Input.GetKeyDown("space"))
                {
                    Play();
                    StartText.SetActive(false);
                    TopScore.SetActive(false);
                    canStartGame = false;
                    Debug.Log("Игра запущена.");
                }
            }
            else if (currentOptionSelection == 1) // Купюры
            {
                // Для варианта "Купюры" не обрабатываем жетоны, но запускаем игру по пробелу
                if (Input.GetKeyDown("space"))
                {
                    Play();
                    StartText.SetActive(false);
                    TopScore.SetActive(false);
                    canStartGame = false;
                    Debug.Log("Игра запущена.");
                }
            }
            else if (currentOptionSelection == 2) // Кнопка Start
            {
                // Для варианта "Кнопка Start" запускаем игру по пробелу
                if (Input.GetKeyDown("space"))
                {
                    Play();
                    StartText.SetActive(false);
                    TopScore.SetActive(false);
                    canStartGame = false;
                    Debug.Log("Игра запущена.");
                }
            }
        }
        else if (state == "gameover")
        {
            if (canRestartManually && Input.anyKeyDown)
            {
                LoadMainScene();
            }
        }

        if (isInCooldown)
        {
            StartCoroutine(Cooldown());
        }
    }

    public void MainMenu()
    {
        CurrentCanvas = mainMenuCanvas;
        state = "mainmenu";
        gameStarted = false;
        gameStartTime = 0;

        // Сбрасываем флаг запуска при возвращении в меню
        canStartGame = false;

        // Устанавливаем текст в зависимости от выбранной опции
        int currentOptionSelection = PlayerPrefs.GetInt("optionSelection", 0);
        if (currentOptionSelection == 0) // Жетон
        {
            InstructionsText.SetActive(true);
            StartText.SetActive(false);
        }
        else // Купюры или Кнопка Start
        {
            InstructionsText.SetActive(false);
            StartText.SetActive(true);
            if (currentOptionSelection == 2) // Только для "Кнопка Start" игра всегда готова к запуску
            {
                canStartGame = true;
            }
        }

        GameObject.Find("LevelController").SendMessage("Reset");
        player.SendMessage("Reset");

        // Обновляем лучший результат
        if (File.Exists(Application.dataPath + "/" + filename))
        {
            var sr = new StreamReader(Application.dataPath + "/" + filename);
            string fileContent = sr.ReadLine();
            sr.Close();

            if (int.TryParse(fileContent, out int loadedTop))
            {
                top = loadedTop;
            }
        }

        topScore.text = "Рекорд: " + top;
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
                {
                    top = score;
                    PlayerPrefs.SetInt("Top", top);
                    var sw = File.CreateText(Application.dataPath + "/" + filename);
                    sw.Write(top);
                    sw.Close();
                }

                topScore.text = "Рекорд: " + top;

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
            {
                currentCanvas.SetActive(false);
            }
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
        {
            LoadMainScene();
        }
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
        // Отписываемся от события
        if (serialPortManager != null)
        {
            serialPortManager.OnCoinsReceived -= HandleCoinsReceived;
        }

        // Удаляем сохранённый рекорд
        if (File.Exists(Application.dataPath + "/" + filename))
        {
            File.Delete(Application.dataPath + "/" + filename);
            Debug.Log("Файл рекорда удалён.");
        }

        PlayerPrefs.DeleteKey("Top");
        PlayerPrefs.Save();
        Debug.Log("Рекорд в PlayerPrefs удалён.");
    }

    // Метод для увеличения количества жизней
    private void IncreaseLives(int credit = 1)
    {
        // Увеличиваем количество жизней на значение из livesFromTicket умноженное на кредиты
        lives += livesFromTicket * credit;
        livesText.text = "Жизни: " + lives.ToString();
        Debug.Log($"Количество жизней увеличено на {livesFromTicket * credit}: {lives}");
    }

    // Новые методы для изменения времени и жизней
    public void SetLives(int newLives)
    {
        lives = newLives;
    }

    public void SetLivesFromTicket(int newLivesFromTicket)
    {
        livesFromTicket = newLivesFromTicket;
    }

    public void SetGameTimeLimit(float newTimeLimit)
    {
        gameTimeLimit = newTimeLimit;
    }
}