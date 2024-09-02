using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.IO;
using UnityEngine.SceneManagement;
using System.IO.Ports;

public class GameStateControllerScript : MonoBehaviour
{
    public GameObject mainMenuCanvas;
    public GameObject playCanvas;
    public GameObject gameOverCanvas;

    public Text playScore;
    public Text gameOverScore;
    public Text topScore;
    public Text playerName;
    public Text timerText; // Счётчик времени на экране

    public int score, top;

    private GameObject currentCanvas;
    private string state;

    public string filename = "top.txt";
    public AudioSource GameOverSound;

    private SerialPort portNo = new SerialPort("COM9", 9600);

    private float gameStartTime; // Время начала игры
    public float gameTimeLimit = 180f; // Лимит времени в секундах (3 минуты)
    private bool gameStarted = false;
    private bool isGameOver = false;

    public void Start()
    {
        portNo.Open();
        portNo.ReadTimeout = 1000;
        currentCanvas = mainMenuCanvas;
        MainMenu();
    }

    public void Update()
    {
        if (state == "play")
        {
            if (!gameStarted)
            {
                gameStartTime = Time.time; // Устанавливаем время старта игры
                gameStarted = true;
            }

            // Обновляем текст таймера
            float remainingTime = gameTimeLimit - (Time.time - gameStartTime);
            timerText.text = "Time: " + Mathf.Floor(remainingTime).ToString();

            // Если время закончилось, возвращаемся в главное меню
            if (remainingTime <= 0)
            {
                GameOver();
                state = "mainmenu";
                MainMenu();
                GameObject.Find("LevelController").SendMessage("Reset");
                GameObject.FindGameObjectWithTag("Player").SendMessage("Reset");
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
            else if (Input.GetButtonDown("Cancel"))
            {
                Application.Quit();
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
                GameObject.FindGameObjectWithTag("Player").SendMessage("Reset");
            }

            
        }
    }

    public void MainMenu()
    {
        CurrentCanvas = mainMenuCanvas;
        state = "mainmenu";
        gameStarted = false;

        GameObject.Find("LevelController").SendMessage("Reset");
        GameObject.FindGameObjectWithTag("Player").SendMessage("Reset");

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

        GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovementScript>().canMove = true;
        GameObject.FindGameObjectWithTag("MainCamera").GetComponent<CameraMovementScript>().moving = true;
    }

    public void GameOver()
    {
        CurrentCanvas = gameOverCanvas;
        state = "gameover";
        isGameOver = true;

        GameOverSound.Play();

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
