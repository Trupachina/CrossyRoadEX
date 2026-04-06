using UnityEngine;
using UnityEngine.UI;

public class GameSettingsMenu : MonoBehaviour
{
    public GameObject settingsMenu;
    public Button muteButton;
    public Text muteButtonText;
    public Text sceneText;
    public Button changeOptionButton;
    public Text changeOptionButtonText;
    public Button exitButton;
    public Button restartButton; // используется как “Сброс рекорда”
    public Button livesButton;
    public Text livesButtonText;
    public Button timeButton;
    public Text timeButtonText;
    public Button livesFromTicketButton;
    public Text livesFromTicketButtonText;
    public AudioSource[] allAudioSources;

    // ===== NEW: Serial status text =====
    public Text serialPortStatusText;

    private int soundLevel = 3;
    private int menuSelection = 0;
    private int optionSelection = 0;
    private int livesSelection = 2;
    private int timeSelection = 2;
    private int livesFromTicketSelection = 2;
    private bool isLongPressActive = false;
    private float pressDuration = 0f;
    private float longPressThreshold = 1f;
    private bool isMenuOpen = false;

    private string[] menuOptions = { "Change Sound Level", "Change Lives", "Lives from Ticket", "Change Time Limit", "Reset Top Score", "Change Option", "Exit Menu" };

    private string[] soundLevelsText = { "Звук выключен", "Минимальная громкость", "Средняя громкость", "Максимальная громкость" };
    private string[] livesOptions = { "1 жизнь", "2 жизни", "3 жизни" };
    private string[] timeOptions = { "1 минута", "2 минуты", "3 минуты", "Без ограничения времени" };
    private string[] livesFromTicketOptions = { "1 жизнь за жетон", "2 жизни за жетон", "3 жизни за жетон" };
    private string[] sceneMessages = { "Вставьте жетон", "Оплатите игру", "Нажмите Start" };
    private string[] buttonOptions = { "Жетон", "Купюры", "Кнопка Start" };

    private GameStateControllerScript gameController;

    // ===== NEW: debounce (чтобы не “перескакивало” через Купюры) =====
    private float lastOptionChangeTime = -999f;
    private const float OPTION_CHANGE_DEBOUNCE = 0.20f;

    // ===== NEW: serial UI refresh =====
    private float serialUiTimer = 0f;
    private const float SERIAL_UI_REFRESH = 0.25f;

    void Start()
    {
        gameController = FindObjectOfType<GameStateControllerScript>();

        soundLevel = PlayerPrefs.GetInt("soundLevel", 3);
        livesSelection = PlayerPrefs.GetInt("livesSelection", 2);
        timeSelection = PlayerPrefs.GetInt("timeSelection", 2);
        livesFromTicketSelection = PlayerPrefs.GetInt("livesFromTicketSelection", 2);
        optionSelection = PlayerPrefs.GetInt("optionSelection", 0);

        ApplySoundSettings();
        if (gameController != null)
        {
            gameController.SetLives(livesSelection + 1);
            gameController.SetLivesFromTicket(livesFromTicketSelection + 1);
            ApplyTimeSettings();
            gameController.ApplyStartOptionFromPrefs();
        }

        settingsMenu.SetActive(false);

        muteButton.onClick.AddListener(ChangeSoundLevel);
        changeOptionButton.onClick.AddListener(ChangeOption);
        exitButton.onClick.AddListener(ExitMenu);

        restartButton.onClick.AddListener(ResetTopScore);

        livesButton.onClick.AddListener(ChangeLives);
        timeButton.onClick.AddListener(ChangeTimeLimit);
        livesFromTicketButton.onClick.AddListener(ChangeLivesFromTicket);

        UpdateMuteButtonText();
        UpdateSceneText();
        UpdateLivesButtonText();
        UpdateTimeButtonText();
        UpdateLivesFromTicketButtonText();
        UpdateResetButtonLabel();

        UpdateSerialPortStatusUI(true);
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

        if (isMenuOpen)
        {
            serialUiTimer += Time.deltaTime;
            if (serialUiTimer >= SERIAL_UI_REFRESH)
            {
                serialUiTimer = 0f;
                UpdateSerialPortStatusUI(false);
            }
        }
    }

    void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;
        settingsMenu.SetActive(isMenuOpen);
        if (isMenuOpen)
        {
            HighlightSelection();
            UpdateSerialPortStatusUI(true);
        }
    }

    void ExecuteMenuAction(int selection)
    {
        switch (selection)
        {
            case 0: ChangeSoundLevel(); break;
            case 1: ChangeLives(); break;
            case 2: ChangeLivesFromTicket(); break;
            case 3: ChangeTimeLimit(); break;
            case 4: ResetTopScore(); break;
            case 5: ChangeOption(); break;
            case 6: ExitMenu(); break;
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
            case 0: volume = 0f; break;
            case 1: volume = 0.25f; break;
            case 2: volume = 0.5f; break;
            case 3: volume = 1f; break;
        }

        foreach (var audioSource in allAudioSources)
            audioSource.volume = volume;
    }

    void ChangeOption()
    {
        // ===== debounce: предотвращает двойной вызов и “перескок” =====
        if (Time.unscaledTime - lastOptionChangeTime < OPTION_CHANGE_DEBOUNCE)
            return;
        lastOptionChangeTime = Time.unscaledTime;

        optionSelection = (optionSelection + 1) % buttonOptions.Length;
        UpdateSceneText();

        PlayerPrefs.SetInt("optionSelection", optionSelection);
        PlayerPrefs.Save();

        // ===== важно: обновить главное меню сразу, без перезахода =====
        if (gameController != null)
            gameController.ApplyStartOptionFromPrefs();
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
        if (gameController != null) gameController.SetLives(livesSelection + 1);

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
        if (gameController != null) gameController.SetLivesFromTicket(livesFromTicketSelection + 1);

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
        if (gameController != null)
        {
            if (timeSelection == 3)
                gameController.SetGameTimeLimit(0);
            else
                gameController.SetGameTimeLimit((timeSelection + 1) * 60);
        }

        PlayerPrefs.SetInt("timeSelection", timeSelection);
        PlayerPrefs.Save();

        UpdateTimeButtonText();
    }

    void ApplyTimeSettings()
    {
        if (gameController == null) return;

        if (timeSelection == 3)
            gameController.SetGameTimeLimit(0);
        else
            gameController.SetGameTimeLimit((timeSelection + 1) * 60);
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

    void ResetTopScore()
    {
        if (gameController != null)
            gameController.ResetTopScoreHard();

        UpdateResetButtonLabel();
    }

    void UpdateResetButtonLabel()
    {
        if (restartButton == null) return;

        Text t = restartButton.GetComponentInChildren<Text>(true);
        if (t != null)
            t.text = "Сброс рекорда";
    }

    // ===== NEW: Serial status =====
    void UpdateSerialPortStatusUI(bool force)
    {
        if (serialPortStatusText == null) return;

        SerialPortManager spm = SerialPortManager.Instance;
        if (spm == null) spm = FindObjectOfType<SerialPortManager>();

        if (spm == null)
        {
            serialPortStatusText.text = "Serial: менеджер не найден";
            return;
        }

        if (spm.IsPortOpen)
        {
            float age = spm.SecondsSinceLastPacket;
            if (age < 3f)
                serialPortStatusText.text = $"Serial: {spm.PortName} — подключен (данные идут)";
            else
                serialPortStatusText.text = $"Serial: {spm.PortName} — подключен (ожидание данных)";
        }
        else
        {
            if (!string.IsNullOrEmpty(spm.LastErrorMessage))
                serialPortStatusText.text = $"Serial: {spm.PortName} — НЕ подключен ({spm.LastErrorMessage})";
            else
                serialPortStatusText.text = $"Serial: {spm.PortName} — НЕ подключен";
        }
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
            case 0: muteButton.GetComponent<Image>().color = Color.yellow; break;
            case 1: livesButton.GetComponent<Image>().color = Color.yellow; break;
            case 2: livesFromTicketButton.GetComponent<Image>().color = Color.yellow; break;
            case 3: timeButton.GetComponent<Image>().color = Color.yellow; break;
            case 4: restartButton.GetComponent<Image>().color = Color.yellow; break;
            case 5: changeOptionButton.GetComponent<Image>().color = Color.yellow; break;
            case 6: exitButton.GetComponent<Image>().color = Color.yellow; break;
        }
    }
}
