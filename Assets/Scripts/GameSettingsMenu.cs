using UnityEngine;
using UnityEngine.UI;
using System.Diagnostics;

public class GameSettingsMenu : MonoBehaviour
{
    public GameObject settingsMenu;
    public Button muteButton;
    public Text muteButtonText;  // ��� ��������� ������ ������ �����
    public Text sceneText;       // ����� �� �����
    public Button changeOptionButton; // ������ ��� ����� ������
    public Text changeOptionButtonText; // ����� ������ ��� ����� �����
    public Button exitButton;
    public Button restartButton;
    public AudioSource[] allAudioSources; // ������ ��� ���� ���������� �����

    private int soundLevel = 3;  // ������� �����: 0 - ��������, 1 - �����������, 2 - �������, 3 - ������������
    private int menuSelection = 0;        // ������� ����� � ����
    private int optionSelection = 0;      // ����� ����� ��� ������ �� �����
    private bool isLongPressActive = false;
    private float pressDuration = 0f;
    private float longPressThreshold = 1f; // ����� ��� ������� ������� (1 �������)
    private bool isMenuOpen = false;
    private string[] menuOptions = { "Change Sound Level", "Change Scene Text", "Exit Menu", "Restart PC" };
    private string[] soundLevelsText = { "���� ��������", "����������� ���������", "������� ���������", "������������ ���������" };
    private string[] sceneMessages = { "�������� �����", "�������� ����", "������� Start" }; // ��������� �� �����
    private string[] buttonOptions = { "�����", "������", "������ Start" }; // ����� ��� ������ ����� ������

    void Start()
    {
        // ��������� ��������� ����� ��� �������
        soundLevel = PlayerPrefs.GetInt("soundLevel", 3);
        ApplySoundSettings();

        settingsMenu.SetActive(false);

        muteButton.onClick.AddListener(ChangeSoundLevel);
        changeOptionButton.onClick.AddListener(ChangeOption);
        exitButton.onClick.AddListener(ExitMenu);
        restartButton.onClick.AddListener(RestartPC);

        UpdateMuteButtonText(); // ������������� ����� ������ ��� ������
        UpdateSceneText(); // ������������� ����� ����� ��� ������
    }

    void Update()
    {
        // ��������� ������� ������� ��� ��������/�������� ����
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
                // ��������� ��������� ������� ��� ������������ ����� ������� ����
                menuSelection = (menuSelection + 1) % menuOptions.Length;
                HighlightSelection(); // ���������� ���������
            }
        }

        // ������������� ������ ��� ������� ������� Enter
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
            HighlightSelection(); // ���������� ������� ����� ��� �������� ����
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

    // ������������ ������� �����
    void ChangeSoundLevel()
    {
        soundLevel = (soundLevel + 1) % 4; // ������� � ���������� ������ �����
        ApplySoundSettings();

        // ��������� ��������� �����
        PlayerPrefs.SetInt("soundLevel", soundLevel);
        PlayerPrefs.Save();

        UpdateMuteButtonText(); // ��������� ����� �� ������
    }

    void ApplySoundSettings()
    {
        float volume = 0f;
        switch (soundLevel)
        {
            case 0: // ���� ��������
                volume = 0f;
                break;
            case 1: // ����������� ����
                volume = 0.25f;
                break;
            case 2: // ������� ����
                volume = 0.5f;
                break;
            case 3: // ������������ ����
                volume = 1f;
                break;
        }

        foreach (var audioSource in allAudioSources)
        {
            audioSource.volume = volume;
        }
    }

    // ����� ��� ����� ������ �� ����� � ������
    void ChangeOption()
    {
        optionSelection = (optionSelection + 1) % buttonOptions.Length; // ������� � ��������� �����
        UpdateSceneText(); // ��������� ����� �� ����� � ������
    }

    // ���������� ������ ������ �����
    void UpdateMuteButtonText()
    {
        muteButtonText.text = soundLevelsText[soundLevel]; // ������������� ����� �� ������ �������� ������ �����
    }

    // ���������� ������ �� ����� � ����������� �� ��������� �����
    void UpdateSceneText()
    {
        changeOptionButtonText.text = buttonOptions[optionSelection]; // ��������� ����� ������ �����
        sceneText.text = sceneMessages[optionSelection]; // ��������� ����� �� �����
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

    // ����� ��� ��������� ��������� ������
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
