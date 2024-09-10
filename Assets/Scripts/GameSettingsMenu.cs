using UnityEngine;
using UnityEngine.UI;
using System.Diagnostics;

public class GameSettingsMenu : MonoBehaviour
{
    public GameObject settingsMenu;
    public Button muteButton;
    public Text muteButtonText;  // Для изменения текста кнопки звука
    public Text sceneText;       // Текст на сцене
    public Button changeOptionButton; // Кнопка для смены текста
    public Text changeOptionButtonText; // Текст кнопки для смены опций
    public Button exitButton;
    public Button restartButton;
    public AudioSource[] allAudioSources; // Массив для всех источников звука

    private int soundLevel = 3;  // Уровень звука: 0 - выключен, 1 - минимальный, 2 - средний, 3 - максимальный
    private int menuSelection = 0;        // Текущий выбор в меню
    private int optionSelection = 0;      // Выбор опции для текста на сцене
    private bool isLongPressActive = false;
    private float pressDuration = 0f;
    private float longPressThreshold = 1f; // Время для долгого нажатия (1 секунда)
    private bool isMenuOpen = false;
    private string[] menuOptions = { "Change Sound Level", "Change Scene Text", "Exit Menu", "Restart PC" };
    private string[] soundLevelsText = { "Звук выключен", "Минимальная громкость", "Средняя громкость", "Максимальная громкость" };
    private string[] sceneMessages = { "Вставьте жетон", "Оплатите игру", "Нажмите Start" }; // Сообщения на сцене
    private string[] buttonOptions = { "Жетон", "Купюры", "Кнопка Start" }; // Опции для кнопки смены текста

    void Start()
    {
        // Загружаем состояние звука при запуске
        soundLevel = PlayerPrefs.GetInt("soundLevel", 3);
        ApplySoundSettings();

        settingsMenu.SetActive(false);

        muteButton.onClick.AddListener(ChangeSoundLevel);
        changeOptionButton.onClick.AddListener(ChangeOption);
        exitButton.onClick.AddListener(ExitMenu);
        restartButton.onClick.AddListener(RestartPC);

        UpdateMuteButtonText(); // Устанавливаем текст кнопки при старте
        UpdateSceneText(); // Устанавливаем текст сцены при старте
    }

    void Update()
    {
        // Обработка долгого нажатия для открытия/закрытия меню
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
                // Обработка короткого нажатия для переключения между опциями меню
                menuSelection = (menuSelection + 1) % menuOptions.Length;
                HighlightSelection(); // Обновление подсветки
            }
        }

        // Подтверждение выбора при нажатии клавиши Enter
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
            HighlightSelection(); // Подсветить текущую опцию при открытии меню
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
                RestartPC();
                break;
            case 2:
                ChangeOption();
                break;
            case 3:
                ExitMenu();
                break;
        }
    }

    // Переключение уровней звука
    void ChangeSoundLevel()
    {
        soundLevel = (soundLevel + 1) % 4; // Переход к следующему уровню звука
        ApplySoundSettings();

        // Сохраняем состояние звука
        PlayerPrefs.SetInt("soundLevel", soundLevel);
        PlayerPrefs.Save();

        UpdateMuteButtonText(); // Обновляем текст на кнопке
    }

    void ApplySoundSettings()
    {
        float volume = 0f;
        switch (soundLevel)
        {
            case 0: // Звук выключен
                volume = 0f;
                break;
            case 1: // Минимальный звук
                volume = 0.25f;
                break;
            case 2: // Средний звук
                volume = 0.5f;
                break;
            case 3: // Максимальный звук
                volume = 1f;
                break;
        }

        foreach (var audioSource in allAudioSources)
        {
            audioSource.volume = volume;
        }
    }

    // Метод для смены текста на сцене и кнопке
    void ChangeOption()
    {
        optionSelection = (optionSelection + 1) % buttonOptions.Length; // Переход к следующей опции
        UpdateSceneText(); // Обновляем текст на сцене и кнопке
    }

    // Обновление текста кнопки звука
    void UpdateMuteButtonText()
    {
        muteButtonText.text = soundLevelsText[soundLevel]; // Устанавливаем текст на основе текущего уровня звука
    }

    // Обновление текста на сцене в зависимости от выбранной опции
    void UpdateSceneText()
    {
        changeOptionButtonText.text = buttonOptions[optionSelection]; // Обновляем текст кнопки смены
        sceneText.text = sceneMessages[optionSelection]; // Обновляем текст на сцене
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

    // Метод для подсветки выбранной кнопки
    void HighlightSelection()
    {
        muteButton.GetComponent<Image>().color = Color.white;
        restartButton.GetComponent<Image>().color = Color.white;
        changeOptionButton.GetComponent<Image>().color = Color.white;
        exitButton.GetComponent<Image>().color = Color.white;

        switch (menuSelection)
        {
            case 0:
                muteButton.GetComponent<Image>().color = Color.yellow;
                break;
            case 1:
                restartButton.GetComponent<Image>().color = Color.yellow;
                break;
            case 2:
                changeOptionButton.GetComponent<Image>().color = Color.yellow;
                break;
            case 3:
                exitButton.GetComponent<Image>().color = Color.yellow;
                break;
        }
    }
}
