using UnityEngine;
using System.IO.Ports;
using System.IO;
using System;

public class SerialPortManager : MonoBehaviour
{
    public static SerialPortManager Instance { get; private set; }

    [Header("Serial Port Settings")]
    public string comPort = "COM7";
    public string configFileName = "com_port_config.txt";

    private SerialPort port;
    private bool portIsOpen = false;
    private bool portInitialized = false;

    private byte[] packetBuffer = new byte[6];
    private int bufferIndex = 0;
    private float lastPacketTime = 0f;
    private const float PACKET_TIMEOUT = 1.5f;

    public event Action<int> OnCoinsReceived;

    // ===== NEW: статус для UI =====
    private string lastErrorMessage = "";

    public bool IsPortOpen
    {
        get { return portIsOpen && port != null && port.IsOpen; }
    }

    public bool IsInitialized
    {
        get { return portInitialized; }
    }

    public string PortName
    {
        get { return comPort; }
    }

    public string LastErrorMessage
    {
        get { return lastErrorMessage; }
    }

    public float SecondsSinceLastPacket
    {
        get
        {
            if (!IsPortOpen) return float.PositiveInfinity;
            return Time.time - lastPacketTime;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        LoadComPortFromFile();
        InitializeSerialPort();
    }

    private void LoadComPortFromFile()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, configFileName);

        if (!File.Exists(filePath))
        {
            Debug.Log($"Файл конфигурации {filePath} не найден. Используется порт по умолчанию: {comPort}");
            return;
        }

        try
        {
            string[] lines = File.ReadAllLines(filePath);
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.Trim().StartsWith("#"))
                    continue;

                if (line.Contains("="))
                {
                    string[] parts = line.Split('=');
                    if (parts.Length == 2 && parts[0].Trim().Equals("COM_PORT", StringComparison.OrdinalIgnoreCase))
                    {
                        string newComPort = parts[1].Trim();
                        if (!string.IsNullOrEmpty(newComPort))
                        {
                            comPort = newComPort;
                            Debug.Log($"COM порт изменен на {comPort} из файла конфигурации");
                            break;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Ошибка при чтении файла конфигурации: {ex.Message}");
            lastErrorMessage = ex.Message;
        }
    }

    private void InitializeSerialPort()
    {
        if (portInitialized) return;

        try
        {
            port = new SerialPort(comPort, 9600)
            {
                ReadTimeout = 50,
                DtrEnable = true,
                RtsEnable = true
            };

            port.Open();
            portIsOpen = true;
            portInitialized = true;
            lastErrorMessage = "";
            Debug.Log($"Serial Port {comPort} успешно открыт");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Ошибка при открытии порта: {ex.Message}");
            lastErrorMessage = ex.Message;
            portIsOpen = false;
            portInitialized = false;
        }
    }

    private void Update()
    {
        if (!portIsOpen || port == null || !port.IsOpen) return;

        try
        {
            while (port.BytesToRead > 0 && bufferIndex < 6)
            {
                byte data = (byte)port.ReadByte();
                lastPacketTime = Time.time;

                if (bufferIndex == 0 && data != 0xFA)
                    continue;

                packetBuffer[bufferIndex] = data;
                bufferIndex++;

                if (bufferIndex == 6)
                {
                    ProcessReceivedPacket();
                    bufferIndex = 0;
                }
            }

            if (bufferIndex > 0 && (Time.time - lastPacketTime > PACKET_TIMEOUT))
            {
                bufferIndex = 0;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Ошибка при чтении порта: {ex.Message}");
            lastErrorMessage = ex.Message;
            bufferIndex = 0;
        }
    }

    private void ProcessReceivedPacket()
    {
        if (packetBuffer[0] == 0xFA &&
            packetBuffer[1] == 0xFA &&
            packetBuffer[2] == 0xFA &&
            packetBuffer[5] == 0xF0)
        {
            int credit = (packetBuffer[3] << 8) | packetBuffer[4];

            if (credit > 0)
            {
                Debug.Log($"Получен кредит: {credit} монет(ы)");
                SafeInvokeOnCoinsReceived(credit);
            }
        }
    }

    private void SafeInvokeOnCoinsReceived(int credit)
    {
        if (OnCoinsReceived == null) return;

        var handlers = OnCoinsReceived.GetInvocationList();

        foreach (var handler in handlers)
        {
            try
            {
                ((Action<int>)handler)(credit);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка при вызове обработчика: {ex.Message}");
                OnCoinsReceived -= (Action<int>)handler;
            }
        }
    }

    private void OnApplicationQuit()
    {
        CloseSerialPort();
    }

    private void CloseSerialPort()
    {
        try
        {
            if (port != null && port.IsOpen)
            {
                port.Close();
                Debug.Log("Serial Port закрыт");
            }
        }
        catch (Exception ex)
        {
            lastErrorMessage = ex.Message;
        }
        finally
        {
            portIsOpen = false;
            portInitialized = false;
        }
    }

    public void ReinitializePort(string newPort)
    {
        comPort = newPort;
        CloseSerialPort();
        InitializeSerialPort();
    }
}
