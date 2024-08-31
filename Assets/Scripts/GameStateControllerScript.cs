using UnityEngine;
using System.Collections;
using UnityEngine.UI; // Исправление пространства имен для Text
using System.IO; // Импорт для работы с файлами
using UnityEngine.SceneManagement; // Импорт для работы с сценами
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

    public int score, top;

    private GameObject currentCanvas;
    private string state;

    public string filename = "top.txt";

    SerialPort portNo = new SerialPort("COM9", 9600);

    public void Start()
    {
        portNo.Open();
        portNo.ReadTimeout = 1000;

        currentCanvas = null;
        MainMenu();
    }

    public void Update()
    {
        if (state == "play")
        {
            topScore.text = PlayerPrefs.GetInt("Top").ToString();
            playScore.text = score.ToString();
        }
        else if (state == "mainmenu")
        {
            if (portNo.IsOpen && portNo.BytesToRead > 0)
            {
                try
                {
                    if (portNo.ReadByte() == 1) // Считываем сигнал от Arduino
                    {
                        Debug.Log("FUCK");
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
            }
        }
    }

    public void MainMenu()
    {
        CurrentCanvas = mainMenuCanvas;
        state = "mainmenu";

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
