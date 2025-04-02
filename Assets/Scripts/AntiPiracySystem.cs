using UnityEngine;
using System;
using System.IO;
using System.Runtime.InteropServices;

public class AntiPiracySystem : MonoBehaviour
{
    private string deviceKey;
    private string savedKey;
    private string installKeyPath;
    private string userKeyPath;

    void Start()
    {
        // 🔹 Получаем уникальный идентификатор устройства
        deviceKey = SystemInfo.deviceUniqueIdentifier;

        // 🔹 Пути хранения файла
        installKeyPath = Application.dataPath + "/device_key.txt"; // Путь в Program Files
        userKeyPath = Application.persistentDataPath + "/device_key.txt"; // Путь в папку пользователя

        // 🔹 Читаем ключ из installKeyPath (если доступно)
        if (File.Exists(installKeyPath))
        {
            try
            {
                savedKey = File.ReadAllText(installKeyPath).Trim();
                Debug.Log("🔹 Найден device_key в папке установки.");
            }
            catch (UnauthorizedAccessException)
            {
                Debug.LogWarning("❌ Нет доступа к файлу в папке установки. Используем userKeyPath.");
            }
        }
        else
        {
            File.WriteAllText(installKeyPath, deviceKey);
            Debug.Log("🆕 Файл device_key.txt создан в папке установки.");
            savedKey = deviceKey;
        }

        //if (File.Exists(userKeyPath))
        //{
        //    savedKey = File.ReadAllText(userKeyPath).Trim();
        //    Debug.Log("🔹 Найден device_key в папке пользователя.");
        //}
        //else
        //{
        //    File.WriteAllText(userKeyPath, deviceKey);
        //    Debug.Log("🆕 Файл device_key.txt создан в папке пользователя.");
        //    savedKey = deviceKey;
        //}

        //// 🔹 Если ключ не прочитан, читаем/создаём в userKeyPath
        //if (string.IsNullOrEmpty(savedKey))
        //{
        //    if (File.Exists(userKeyPath))
        //    {
        //        savedKey = File.ReadAllText(userKeyPath).Trim();
        //        Debug.Log("🔹 Найден device_key в папке пользователя.");
        //    }
        //    else
        //    {
        //        File.WriteAllText(userKeyPath, deviceKey);
        //        Debug.Log("🆕 Файл device_key.txt создан в папке пользователя.");
        //        savedKey = deviceKey;
        //    }
        //}

        // 🔹 Проверяем ключ
        if (savedKey != deviceKey)
        {
            Debug.LogError("🚫 Пиратская версия! Ключ не совпадает.");
            ShowPiracyWarning();
        }
        else
        {
            Debug.Log("✅ Игра запущена на авторизованном устройстве.");
        }
    }

    // 🔹 Метод для вывода стандартного окна Windows
    void ShowPiracyWarning()
    {
        NativeWinAlert.Error(
            "Warning! You are using a pirated version of the game!\nThe game will be closed.",
            "Pirated version detected"
        );

        Application.Quit(); // Завершаем работу приложения
    }
}

// 🔹 Класс для Windows MessageBox
public static class NativeWinAlert
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern int MessageBox(IntPtr hwnd, string lpText, string lpCaption, uint uType);

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    public static void Error(string text, string caption)
    {
        try
        {
            MessageBox(GetActiveWindow(), text, caption, (uint)(0x00000000L | 0x00000010L));
        }
        catch (Exception ex)
        {
            Debug.LogError("Ошибка при вызове MessageBox: " + ex.Message);
        }
    }
}
