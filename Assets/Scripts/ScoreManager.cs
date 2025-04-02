using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScoreManager : MonoBehaviour
{
    private string filename = "top.txt";
    private string quitFlagFile = "quit_flag.txt";
    private string sessionFlagFile = "session_flag.txt"; // Файл-флаг активной сессии
    private bool isSceneReload = false; // Флаг для проверки перезапуска сцены

    void Awake()
    {
        string flagPath = Application.persistentDataPath + "/" + quitFlagFile;
        string sessionPath = Application.persistentDataPath + "/" + sessionFlagFile;

        // Проверяем, была ли предыдущая игровая сессия
        if (File.Exists(sessionPath))
        {
            isSceneReload = true; // Это перезапуск сцены
        }
        else
        {
            File.WriteAllText(sessionPath, "Session Active");
        }

        // Если это первый запуск игры после выключения ПК
        if (!isSceneReload)
        {
            if (File.Exists(flagPath))
            {
                Debug.LogWarning("Обнаружен файл-флаг! Прошлая сессия завершилась аварийно или игра была выключена. Сбрасываем рекорд...");
                ResetRecord();
            }

            try
            {
                File.WriteAllText(flagPath, "Game is running");
                Debug.Log("Файл-флаг успешно создан: " + flagPath);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Ошибка при создании файла-флага: " + ex.Message);
            }
        }

        LoadRecord();
    }

    void OnApplicationQuit()
    {
        string flagPath = Application.persistentDataPath + "/" + quitFlagFile;
        string sessionPath = Application.persistentDataPath + "/" + sessionFlagFile;

        // НЕ удаляем quit_flag.txt (он нужен для сброса рекорда при следующем запуске)
        Debug.Log("Выход из игры. Файл-флаг сохранён.");

        // Удаляем флаг активной сессии (чтобы при новом запуске отличить от перезапуска сцены)
        if (File.Exists(sessionPath))
        {
            File.Delete(sessionPath);
        }
    }

    void LoadRecord()
    {
        string path = Application.persistentDataPath + "/" + filename;
        if (File.Exists(path))
        {
            string record = File.ReadAllText(path);
            Debug.Log("Рекорд загружен: " + record);
        }
        else
        {
            Debug.Log("Файл рекорда не найден, создаём новый.");
        }
    }

    void ResetRecord()
    {
        string path = Application.persistentDataPath + "/" + filename;
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        PlayerPrefs.DeleteKey("Top");
        PlayerPrefs.Save();

        Debug.Log("Рекорд сброшен из-за выключения ПК или аварийного завершения игры.");
    }
}
