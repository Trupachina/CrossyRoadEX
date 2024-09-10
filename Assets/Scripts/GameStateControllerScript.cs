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
    public int lives = 3; // Количество жизней
    public float gameTimeLimit = 180f;

    private GameObject currentCanvas;
    private string state;
    private GameObject player;

    private string filename = "top.txt";
    public AudioSource gameOverSound;

    public SerialPort portNo = new SerialPort("COM6", 9600);

    private Vector3 lastSafePosition; // Позиция для восстановления после смерти
    private Vector3 lastSafeScale; // Размер игрока до изменений

    private float gameStartTime; // Время начала игры
    private bool gameStarted = false;
    private bool isGameOver = false;

    public bool isInCooldown = false; // Переменная для предотвращения многократной потери жизней
    public float damageCooldown = 2f;  // Время восстановления после столкновения
    public float deathPauseDuration = 2f; // Длительность паузы перед перезапуском

    public void Start()
    {
        portNo.Open();
        portNo.ReadTimeout = 1000;
        currentCanvas = mainMenuCanvas;
        player = GameObject.FindGameObjectWithTag("Player");
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

            // Обновляем время
            float remainingTime = gameTimeLimit - (Time.time - gameStartTime);
            timerText.text = "Время: " + Mathf.Floor(remainingTime).ToString();
            livesText.text = "Жизни: " + lives.ToString();

            // Проверка конца времени
            if (remainingTime <= 0)
            {
                GameOver();
                state = "mainmenu";
                MainMenu();
                GameObject.Find("LevelController").SendMessage("Reset");
                player.SendMessage("Reset");
                return;
            }

            topScore.text = PlayerPrefs.GetInt("Top").ToString();
            playScore.text = score.ToString();
        }
        else if (state == "mainmenu")
        {
            if (portNo.IsOpen && portNo.BytesToRead > 0)
            {
                try
                {
                    if (portNo.ReadByte() == 1)
                    {
                        Play();
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.Log("Error reading from port: " + ex.Message);
                }
            }

            //if (Input.anyKeyDown)
            //{
                //Play();
            //}
            //else if (Input.GetButtonDown("Cancel"))
            //{
                //Application.Quit();
            //}
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

        // Управление состоянием перезарядки
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
        lives = 3; // Сброс жизней

        // Разрешаем движение
        player.GetComponent<PlayerMovementScript>().canMove = true;
        GameObject.FindGameObjectWithTag("MainCamera").GetComponent<CameraMovementScript>().moving = true;

        SavePlayerState(); // Сохраняем начальное положение игрока
    }

    public void GameOver()
    {
        if (!isInCooldown)
        {
            lives--;  // Уменьшаем жизнь только если не в режиме перезарядки

            if (lives > 0)
            {
                isInCooldown = true;  // Включаем режим перезарядки
                StartCoroutine(DeathPause());
            }
            else
            {
                // Если жизни закончились — реальный конец игры
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

                GameObject.FindGameObjectWithTag("MainCamera").GetComponent<CameraMovementScript>().moving = false;
            }
        }
    }

    private IEnumerator DeathPause()
    {
        // Показываем падение игрока или другое событие
        player.GetComponent<PlayerMovementScript>().canMove = false; // Отключаем движение
        GameObject.FindGameObjectWithTag("MainCamera").GetComponent<CameraMovementScript>().moving = false; // Останавливаем камеру
        gameOverSound.Play();

        yield return new WaitForSeconds(deathPauseDuration); // Делаем паузу

        // Возвращаем игрока на сохранённую позицию
        player.transform.position = lastSafePosition;
        player.transform.localScale = lastSafeScale;

        MovePlayerToSafePosition();

        // Возвращаем управление
        player.GetComponent<PlayerMovementScript>().canMove = true; // Включаем движение
        GameObject.FindGameObjectWithTag("MainCamera").GetComponent<CameraMovementScript>().moving = true; // Включаем камеру
    }

    private IEnumerator Cooldown()
    {
        yield return new WaitForSeconds(damageCooldown); // Ждем время восстановления
        isInCooldown = false; // Сбрасываем режим перезарядки
    }

    private void SavePlayerState()
    {
        lastSafePosition = player.transform.position;
        lastSafeScale = player.transform.localScale;
    }

    private void MovePlayerToSafePosition()
    {
        // Проверяем наличие препятствий (машин, воды) рядом
        Collider[] colliders = Physics.OverlapSphere(lastSafePosition, 1.5f);
        foreach (var collider in colliders)
        {
            if (collider.CompareTag("Car") || collider.CompareTag("Water"))
            {
                // Перемещаем игрока вперёд
                player.transform.position += new Vector3(0, 0, 3.2f);
            }
        }
    }

    private GameObject CurrentCanvas
    {
        get
        {
            return currentCanvas;
        }
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
}
