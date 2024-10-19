using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;
using System.IO.Ports;

public class GameStateControllerScript : MonoBehaviour
{
    public GameObject mainMenuCanvas;
    public GameObject playCanvas;
    public GameObject gameOverCanvas;

    public Text playScore;
    public Text gameOverScore;
    public Text topScore;
    public Text timerText;
    public Text livesText;

    public int score, top;
    public int lives = 3; // Количество жизней по умолчанию
    public int livesFromTicket = 3; // По умолчанию 3 жизни за жетон
    public float gameTimeLimit = 180f; // Время игры по умолчанию

    private GameObject currentCanvas;
    private string state;
    private GameObject player;

    private string filename = "top.txt";
    public AudioSource gameOverSound;

    public SerialPort portNo = new SerialPort("COM6", 9600);

    private Vector3 lastSafePosition;
    private Vector3 lastSafeScale;

    private float gameStartTime;
    private bool gameStarted = false;
    private bool isGameOver = false;

    public bool isInCooldown = false;
    public float damageCooldown = 2f;
    public float deathPauseDuration = 2f;

    // Флаг для отслеживания смерти от камеры и времени
    public bool diedFromCamera = false;
    public bool diedFromTime = false; // Новый флаг для смерти от времени

    // Параметры камеры
    private GameObject mainCamera;
    public float cameraGameOverDistance = -1.5f; // Определяет расстояние между камерой и игроком для смерти

    public void Start()
    {
        //OpenPort();  // Открываем порт
        currentCanvas = mainMenuCanvas;
        player = GameObject.FindGameObjectWithTag("Player");
        mainCamera = GameObject.FindGameObjectWithTag("MainCamera"); // Инициализируем камеру
        MainMenu();
    }

    public void Update()
    {
        if (state == "play" && !isGameOver)
        {
            if (!gameStarted)
            {
                gameStartTime = Time.time;
                gameStarted = true;
            }

            float remainingTime = gameTimeLimit - (Time.time - gameStartTime);
            timerText.text = "Время: " + Mathf.Floor(remainingTime).ToString();
            livesText.text = "Жизни: " + lives.ToString();

            if (remainingTime <= 0 && gameTimeLimit > 0)  // Если время истекло и лимит времени включен
            {
                // Обрабатываем истечение времени как смерть
                diedFromTime = true;
                GameOver();
                return;
            }

            topScore.text = PlayerPrefs.GetInt("Top").ToString();
            playScore.text = score.ToString();

            // Проверка, не догнала ли камера игрока
            CheckCameraProximity();

            // Добавляем проверку, сколько байт пришло с порта
            //if (portNo.IsOpen && portNo.BytesToRead > 0)
            //{
                //Debug.Log("Байты, доступные для чтения: " + portNo.BytesToRead);
                //try
                //{
                    //int byteRead = portNo.ReadByte();

                    // Если игра уже запущена, увеличиваем количество жизней на значение из livesFromTicket
                    //IncreaseLives();
                //}
                //catch (System.Exception ex)
                //{
                    //Debug.Log("Ошибка чтения порта: " + ex.Message);
                //}
            //}
        }
        else if (state == "mainmenu")
        {
            //if (portNo.IsOpen && portNo.BytesToRead > 0)
            //{
                //Debug.Log("Байты, доступные для чтения: " + portNo.BytesToRead);
                //try
                //{
                    //int byteRead = portNo.ReadByte();

                    // Если байт = 1, то запускаем игру
                    //if (byteRead == 1)
                    //{
                        //Play();
                    //}
                //}
                //catch (System.Exception ex)
                //{
                    //Debug.Log("Ошибка чтения порта: " + ex.Message);
                //}
            //}

            if (Input.GetKeyDown("space"))
            {
                Play();
            }
        }
        else if (state == "gameover")
        {
            if (Input.anyKeyDown)
            {
                SceneManager.LoadScene(0);
                state = "mainmenu";
                MainMenu();
                GameObject.Find("LevelController").SendMessage("Reset");
                player.SendMessage("Reset");
            }
        }

        if (isInCooldown)
        {
            StartCoroutine(Cooldown());
        }
    }

    private void CheckCameraProximity()
    {
        // Проверка расстояния между камерой и игроком
        if (mainCamera.transform.position.z > player.transform.position.z - cameraGameOverDistance)
        {
            diedFromCamera = true; // Устанавливаем флаг, если камера догнала игрока
            GameOver(); // Вызываем конец игры
        }
    }

    public void MainMenu()
    {
        CurrentCanvas = mainMenuCanvas;
        state = "mainmenu";
        gameStarted = false;
        gameStartTime = 0; // Сброс таймера при возвращении в меню

        GameObject.Find("LevelController").SendMessage("Reset");
        player.SendMessage("Reset");

        StreamReader sr = new StreamReader(Application.dataPath + "/" + filename);
        string fileContent = sr.ReadLine();
        sr.Close();

        topScore.text = fileContent;
    }

    public void Play()
    {
        CurrentCanvas = playCanvas;
        state = "play";
        score = 0;
        isGameOver = false;
        gameStartTime = Time.time; // Сброс таймера при начале новой игры

        // Количество жизней и время игры могут изменяться через меню настроек
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

                gameOverSound.Play();
                gameOverScore.text = score.ToString();

                if (score > top)
                {
                    top = score;
                    PlayerPrefs.SetInt("Top", top);
                    var sw = File.CreateText(Application.dataPath + "/" + filename);
                    sw.Write(top);
                    sw.Close();
                }

                mainCamera.GetComponent<CameraMovementScript>().moving = false;

                ClosePort();  // Закрываем порт при завершении игры
            }
        }
    }

    private IEnumerator DeathPause()
    {
        player.GetComponent<PlayerMovementScript>().canMove = false;
        GameObject camera = mainCamera;

        // Останавливаем движение камеры
        camera.GetComponent<CameraMovementScript>().moving = false;

        gameOverSound.Play();

        // Сброс положения камеры
        camera.GetComponent<CameraMovementScript>().Reset();

        yield return new WaitForSeconds(deathPauseDuration);

        // Если смерть произошла от времени, камера и игрок возвращаются на стартовые позиции
        if (diedFromTime)
        {
            // Возвращаем игрока на стартовую позицию
            player.transform.position = lastSafePosition;
            player.transform.localScale = lastSafeScale;

            // Сброс камеры на стартовую позицию
            camera.GetComponent<CameraMovementScript>().Reset();
        }

        // Если смерть не была вызвана камерой или временем, восстанавливаем игрока в безопасную позицию
        if (!diedFromCamera && !diedFromTime)
        {
            player.transform.position = lastSafePosition;
            player.transform.localScale = lastSafeScale;
        }

        // Сбрасываем флаги после возрождения
        diedFromCamera = false;
        diedFromTime = false;

        // Возвращаем игрока в движение
        player.GetComponent<PlayerMovementScript>().canMove = true;

        // Возвращаем камеру в движение
        camera.GetComponent<CameraMovementScript>().moving = true;
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

    // Открываем порт
    private void OpenPort()
    {
        try
        {
            if (!portNo.IsOpen)
            {
                portNo.Open();
                Debug.Log("Порт успешно открыт");
            }
        }
        catch (System.Exception ex)
        {
            Debug.Log("Ошибка при открытии порта: " + ex.Message);
        }
    }

    // Закрываем порт
    private void ClosePort()
    {
        try
        {
            if (portNo.IsOpen)
            {
                portNo.Close();
                Debug.Log("Порт успешно закрыт");
            }
        }
        catch (System.Exception ex)
        {
            Debug.Log("Ошибка при закрытии порта: " + ex.Message);
        }
    }

    void OnApplicationQuit()
    {
        ClosePort();  // Закрываем порт при выходе из приложения
    }

    // Метод для увеличения количества жизней
    private void IncreaseLives()
    {
        // Увеличиваем количество жизней на значение из livesFromTicket
        lives += livesFromTicket;
        livesText.text = "Жизни: " + lives.ToString();
        Debug.Log("Количество жизней увеличено на " + livesFromTicket + ": " + lives);
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
