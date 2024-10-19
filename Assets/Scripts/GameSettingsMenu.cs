using UnityEngine;
using UnityEngine.UI;
using System.Diagnostics;

public class GameSettingsMenu : MonoBehaviour
{
    public GameObject settingsMenu;
    public Button muteButton;
    public Text muteButtonText;
    public Text sceneText;
    public Button changeOptionButton;
    public Text changeOptionButtonText;
    public Button exitButton;
    public Button restartButton;
    public Button livesButton;
    public Text livesButtonText;
    public Button timeButton;
    public Text timeButtonText;
    public Button livesFromTicketButton;
    public Text livesFromTicketButtonText;
    public AudioSource[] allAudioSources;

    private int soundLevel = 3;
    private int menuSelection = 0;
    private int optionSelection = 0;      // Добавлено сохранение этого параметра
    private int livesSelection = 2;
    private int timeSelection = 2;
    private int livesFromTicketSelection = 2;
    private bool isLongPressActive = false;
    private float pressDuration = 0f;
    private float longPressThreshold = 1f;
    private bool isMenuOpen = false;

    private string[] menuOptions = { "Change Sound Level", "Change Lives", "Lives from Ticket", "Change Time Limit", "Restart PC", "Change Option", "Exit Menu" };
    private string[] soundLevelsText = { "Звук выключен", "Минимальная громкость", "Средняя громкость", "Максимальная громкость" };
    private string[] livesOptions = { "1 жизнь", "2 жизни", "3 жизни" };
    private string[] timeOptions = { "1 минута", "2 минуты", "3 минуты", "Без ограничения времени" };
    private string[] livesFromTicketOptions = { "1 жизнь за жетон", "2 жизни за жетон", "3 жизни за жетон" };
    private string[] sceneMessages = { "Вставьте жетон", "Оплатите игру", "Нажмите Start" };
    private string[] buttonOptions = { "Жетон", "Купюры", "Кнопка Start" };

    private GameStateControllerScript gameController;

    void Start()
    {
        gameController = FindObjectOfType<GameStateControllerScript>();

        // Загружаем состояние параметров при запуске
        soundLevel = PlayerPrefs.GetInt("soundLevel", 3);
        livesSelection = PlayerPrefs.GetInt("livesSelection", 2);
        timeSelection = PlayerPrefs.GetInt("timeSelection", 2);
        livesFromTicketSelection = PlayerPrefs.GetInt("livesFromTicketSelection", 2);
        optionSelection = PlayerPrefs.GetInt("optionSelection", 0); // Загружаем сохраненный выбор сцены

        ApplySoundSettings();
        gameController.SetLives(livesSelection + 1);
        gameController.SetLivesFromTicket(livesFromTicketSelection + 1);
        ApplyTimeSettings();

        settingsMenu.SetActive(false);

        muteButton.onClick.AddListener(ChangeSoundLevel);
        changeOptionButton.onClick.AddListener(ChangeOption);
        exitButton.onClick.AddListener(ExitMenu);
        restartButton.onClick.AddListener(RestartPC);
        livesButton.onClick.AddListener(ChangeLives);
        timeButton.onClick.AddListener(ChangeTimeLimit);
        livesFromTicketButton.onClick.AddListener(ChangeLivesFromTicket);

        UpdateMuteButtonText();
        UpdateSceneText();
        UpdateLivesButtonText();
        UpdateTimeButtonText();
        UpdateLivesFromTicketButtonText();
    }

    void Update()
    {
        if (Input.GetKey("p"))
        {
            pressDuration += Time.deltaTime;
            if (pressDuration >= longPressThreshold && !isLongPressActive)
            {
                ToggleMenu();
                isLongPressActive = true;
            }
        }

        if (Input.GetKeyDown("p"))
        {
            pressDuration = 0f;
            isLongPressActive = false;

            if (isMenuOpen)
            {
                menuSelection = (menuSelection + 1) % menuOptions.Length;
                HighlightSelection();
            }
        }

        if (isMenuOpen && Input.GetKeyDown(KeyCode.Return))
        {
            ExecuteMenuAction(menuSelection);
        }
    }

    void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;
        settingsMenu.SetActive(isMenuOpen);
        if (isMenuOpen)
        {
            HighlightSelection();
        }
    }

    void ExecuteMenuAction(int selection)
    {
        switch (selection)
        {
            case 0:
                ChangeSoundLevel();
                break;
            case 1:
                ChangeLives();
                break;
            case 2:
                ChangeLivesFromTicket();
                break;
            case 3:
                ChangeTimeLimit();
                break;
            case 4:
                RestartPC();
                break;
            case 5:
                ChangeOption();
                break;
            case 6:
                ExitMenu();
                break;
        }
    }

    void ChangeSoundLevel()
    {
        soundLevel = (soundLevel + 1) % 4;
        ApplySoundSettings();

        PlayerPrefs.SetInt("soundLevel", soundLevel);
        PlayerPrefs.Save();

        UpdateMuteButtonText();
    }

    void ApplySoundSettings()
    {
        float volume = 0f;
        switch (soundLevel)
        {
            case 0:
                volume = 0f;
                break;
            case 1:
                volume = 0.25f;
                break;
            case 2:
                volume = 0.5f;
                break;
            case 3:
                volume = 1f;
                break;
        }

        foreach (var audioSource in allAudioSources)
        {
            audioSource.volume = volume;
        }
    }

    void ChangeOption()
    {
        optionSelection = (optionSelection + 1) % buttonOptions.Length;
        UpdateSceneText();

        // Сохраняем выбор опции сцены
        PlayerPrefs.SetInt("optionSelection", optionSelection);
        PlayerPrefs.Save();
    }

    void UpdateMuteButtonText()
    {
        muteButtonText.text = soundLevelsText[soundLevel];
    }

    void UpdateSceneText()
    {
        changeOptionButtonText.text = buttonOptions[optionSelection];
        sceneText.text = sceneMessages[optionSelection];
    }

    void ChangeLives()
    {
        livesSelection = (livesSelection + 1) % livesOptions.Length;
        gameController.SetLives(livesSelection + 1);

        PlayerPrefs.SetInt("livesSelection", livesSelection);
        PlayerPrefs.Save();

        UpdateLivesButtonText();
    }

    void UpdateLivesButtonText()
    {
        livesButtonText.text = livesOptions[livesSelection];
    }

    void ChangeLivesFromTicket()
    {
        livesFromTicketSelection = (livesFromTicketSelection + 1) % livesFromTicketOptions.Length;
        gameController.SetLivesFromTicket(livesFromTicketSelection + 1);

        PlayerPrefs.SetInt("livesFromTicketSelection", livesFromTicketSelection);
        PlayerPrefs.Save();

        UpdateLivesFromTicketButtonText();
    }

    void UpdateLivesFromTicketButtonText()
    {
        livesFromTicketButtonText.text = livesFromTicketOptions[livesFromTicketSelection];
    }

    void ChangeTimeLimit()
    {
        timeSelection = (timeSelection + 1) % timeOptions.Length;
        if (timeSelection == 3)
        {
            gameController.SetGameTimeLimit(0);
        }
        else
        {
            gameController.SetGameTimeLimit((timeSelection + 1) * 60);
        }

        PlayerPrefs.SetInt("timeSelection", timeSelection);
        PlayerPrefs.Save();

        UpdateTimeButtonText();
    }

    void ApplyTimeSettings()
    {
        if (timeSelection == 3)
        {
            gameController.SetGameTimeLimit(0);
        }
        else
        {
            gameController.SetGameTimeLimit((timeSelection + 1) * 60);
        }
    }

    void UpdateTimeButtonText()
    {
        timeButtonText.text = timeOptions[timeSelection];
    }

    void ExitMenu()
    {
        isMenuOpen = false;
        settingsMenu.SetActive(false);
    }

    void RestartPC()
    {
        Process.Start("shutdown.exe", "/r /t 0");
    }

    void HighlightSelection()
    {
        muteButton.GetComponent<Image>().color = Color.white;
        restartButton.GetComponent<Image>().color = Color.white;
        changeOptionButton.GetComponent<Image>().color = Color.white;
        exitButton.GetComponent<Image>().color = Color.white;
        livesButton.GetComponent<Image>().color = Color.white;
        timeButton.GetComponent<Image>().color = Color.white;
        livesFromTicketButton.GetComponent<Image>().color = Color.white;

        switch (menuSelection)
        {
            case 0:
                muteButton.GetComponent<Image>().color = Color.yellow;
                break;
            case 1:
                livesButton.GetComponent<Image>().color = Color.yellow;
                break;
            case 2:
                livesFromTicketButton.GetComponent<Image>().color = Color.yellow;
                break;
            case 3:
                timeButton.GetComponent<Image>().color = Color.yellow;
                break;
            case 4:
                restartButton.GetComponent<Image>().color = Color.yellow;
                break;
            case 5:
                changeOptionButton.GetComponent<Image>().color = Color.yellow;
                break;
            case 6:
                exitButton.GetComponent<Image>().color = Color.yellow;
                break;
        }
    }
}
