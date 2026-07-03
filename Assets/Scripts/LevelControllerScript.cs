using UnityEngine;
using System.Collections.Generic;

public class LevelControllerScript : MonoBehaviour
{
    public int minZ = 3;
    public int lineAhead = 40;
    public int lineBehind = 20;
    public float destroyDelay = 5f;

    [Header("Line Prefabs")]
    public GameObject[] linePrefabs;
    public GameObject railroadLine;
    [Range(0f, 1f)]
    public float railroadChance = 0.05f;

    [Header("Camera Return Protection")]
    public int cameraReturnLinesBehind = 10;
    public int cameraReturnLinesAhead = 18;

    [Header("Respawn Trim")]
    public int respawnTrimKeepBehind = 10;
    public int respawnTrimKeepAhead = 30;

    private const float LineSpacing = 3f;
    private const float LineOffsetZ = -5f;
    private const int DestroyBatchPerFrame = 8;

    private sealed class LineConfig
    {
        public bool isRailroad;
        public int linePrefabIndex;
        public bool isWater;
        public TrunkGeneratorScript.Direction waterDirection;
        public float waterSpeed;
        public float waterLength;
        public float waterInterval;
    }

    private Dictionary<int, GameObject> lines;
    private Dictionary<int, LineConfig> lineConfigs;
    private HashSet<int> queuedDestroyKeys;
    private List<int> destroyBuffer;

    private GameObject player;
    private GameObject mainCamera;

    private bool lastWaterDirectionRight = false;
    private bool keepCameraWindowDuringRespawn = false;

    public void Start()
    {
        CacheSceneObjects();

        if (lines == null)
            lines = new Dictionary<int, GameObject>(128);
        else
            lines.Clear();

        if (lineConfigs == null)
            lineConfigs = new Dictionary<int, LineConfig>(256);
        else
            lineConfigs.Clear();

        if (queuedDestroyKeys == null)
            queuedDestroyKeys = new HashSet<int>();
        else
            queuedDestroyKeys.Clear();

        if (destroyBuffer == null)
            destroyBuffer = new List<int>(256);
        else
            destroyBuffer.Clear();

        lastWaterDirectionRight = false;
        keepCameraWindowDuringRespawn = false;

        ClampInspectorValues();

        if (player != null)
            EnsureWindowAroundWorldZ(player.transform.position.z);
    }

    public void Update()
    {
        CacheSceneObjects();
        ClampInspectorValues();

        if (player == null)
            return;

        int playerLine = WorldZToLineIndex(player.transform.position.z);
        int playerMin = Mathf.Max(minZ, playerLine - lineBehind);
        int playerMax = playerLine + lineAhead;

        EnsureLineRange(playerMin, playerMax);

        bool hasSecondRange = false;
        int secondMin = 0;
        int secondMax = 0;

        if (keepCameraWindowDuringRespawn && mainCamera != null)
        {
            int cameraLine = WorldZToLineIndex(mainCamera.transform.position.z);
            secondMin = Mathf.Max(minZ, cameraLine - cameraReturnLinesBehind);
            secondMax = cameraLine + cameraReturnLinesAhead;
            EnsureLineRange(secondMin, secondMax);
            hasSecondRange = true;
        }

        QueueDestroyOutsideRanges(playerMin, playerMax, hasSecondRange, secondMin, secondMax);
        ProcessDestroyQueue(DestroyBatchPerFrame);
    }

    public void EnsureWindowAroundWorldZ(float worldZ)
    {
        int centerLine = WorldZToLineIndex(worldZ);
        EnsureLineRange(Mathf.Max(minZ, centerLine - lineBehind), centerLine + lineAhead);
    }

    public void BeginRespawnCameraProtection()
    {
        keepCameraWindowDuringRespawn = true;
        EnsureRespawnWindowsNow();
    }

    public void EnsureRespawnWindowsNow()
    {
        CacheSceneObjects();
        ClampInspectorValues();

        if (player != null)
        {
            int playerLine = WorldZToLineIndex(player.transform.position.z);
            EnsureLineRange(Mathf.Max(minZ, playerLine - lineBehind), playerLine + lineAhead);
        }

        if (keepCameraWindowDuringRespawn && mainCamera != null)
        {
            int cameraLine = WorldZToLineIndex(mainCamera.transform.position.z);
            EnsureLineRange(Mathf.Max(minZ, cameraLine - cameraReturnLinesBehind), cameraLine + cameraReturnLinesAhead);
        }
    }

    public void EndRespawnCameraProtection()
    {
        keepCameraWindowDuringRespawn = false;
    }

    public void TrimAroundWorldZ(float worldZ)
    {
        TrimAroundWorldZ(worldZ, respawnTrimKeepBehind, respawnTrimKeepAhead);
    }

    public void TrimAroundWorldZ(float worldZ, int keepBehind, int keepAhead)
    {
        int safeKeepBehind = Mathf.Max(0, keepBehind);
        int safeKeepAhead = Mathf.Max(0, keepAhead);

        int centerLine = WorldZToLineIndex(worldZ);
        int keepMin = Mathf.Max(minZ, centerLine - safeKeepBehind);
        int keepMax = centerLine + safeKeepAhead;

        QueueDestroyOutsideRanges(keepMin, keepMax, false, 0, 0);
        ProcessDestroyQueue(DestroyBatchPerFrame);
    }

    private void CacheSceneObjects()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");

        if (mainCamera == null)
            mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
    }

    private void ClampInspectorValues()
    {
        if (lineAhead < 0)
            lineAhead = 0;

        if (lineBehind < 0)
            lineBehind = 0;

        if (cameraReturnLinesBehind < 0)
            cameraReturnLinesBehind = 0;

        if (cameraReturnLinesAhead < 0)
            cameraReturnLinesAhead = 0;

        if (respawnTrimKeepBehind < 0)
            respawnTrimKeepBehind = 0;

        if (respawnTrimKeepAhead < 0)
            respawnTrimKeepAhead = 0;
    }

    private void EnsureLineRange(int startLine, int endLine)
    {
        for (int lineIndex = startLine; lineIndex <= endLine; lineIndex++)
        {
            if (!lines.ContainsKey(lineIndex))
                SpawnLine(lineIndex);
        }
    }

    private void QueueDestroyOutsideRanges(int firstMin, int firstMax, bool hasSecondRange, int secondMin, int secondMax)
    {
        destroyBuffer.Clear();

        foreach (KeyValuePair<int, GameObject> kv in lines)
        {
            int key = kv.Key;

            bool inFirstRange = key >= firstMin && key <= firstMax;
            bool inSecondRange = hasSecondRange && key >= secondMin && key <= secondMax;

            if (!inFirstRange && !inSecondRange && !queuedDestroyKeys.Contains(key))
                destroyBuffer.Add(key);
        }

        for (int i = 0; i < destroyBuffer.Count; i++)
            queuedDestroyKeys.Add(destroyBuffer[i]);
    }

    private void ProcessDestroyQueue(int batchSize)
    {
        if (queuedDestroyKeys == null || queuedDestroyKeys.Count == 0)
            return;

        destroyBuffer.Clear();

        foreach (int key in queuedDestroyKeys)
        {
            destroyBuffer.Add(key);
            if (destroyBuffer.Count >= batchSize)
                break;
        }

        for (int i = 0; i < destroyBuffer.Count; i++)
        {
            int key = destroyBuffer[i];
            queuedDestroyKeys.Remove(key);

            GameObject line;
            if (!lines.TryGetValue(key, out line))
                continue;

            lines.Remove(key);

            if (line != null)
                Destroy(line);
        }
    }

    private void SpawnLine(int lineIndex)
    {
        LineConfig config = GetOrCreateLineConfig(lineIndex);
        Vector3 position = new Vector3(0f, 0f, LineIndexToWorldZ(lineIndex));

        GameObject line;

        if (config.isRailroad)
        {
            line = Instantiate(railroadLine, position, Quaternion.identity);
        }
        else
        {
            line = Instantiate(linePrefabs[config.linePrefabIndex], position, Quaternion.identity);
            line.transform.localScale = new Vector3(1f, 1f, 3f);

            if (config.isWater)
            {
                TrunkGeneratorScript trunkGenerator = line.GetComponent<TrunkGeneratorScript>();
                if (trunkGenerator != null)
                {
                    trunkGenerator.SetParameters(
                        config.waterDirection,
                        config.waterSpeed,
                        config.waterLength,
                        config.waterInterval
                    );
                }
            }
        }

        lines[lineIndex] = line;
    }

    private LineConfig GetOrCreateLineConfig(int lineIndex)
    {
        LineConfig config;
        if (lineConfigs.TryGetValue(lineIndex, out config))
            return config;

        config = new LineConfig();

        if (Random.value < railroadChance)
        {
            config.isRailroad = true;
            config.linePrefabIndex = -1;
        }
        else
        {
            config.isRailroad = false;
            config.linePrefabIndex = Random.Range(0, linePrefabs.Length);

            GameObject prefab = linePrefabs[config.linePrefabIndex];
            config.isWater = prefab != null && prefab.GetComponent<TrunkGeneratorScript>() != null;

            if (config.isWater)
            {
                config.waterDirection = lastWaterDirectionRight
                    ? TrunkGeneratorScript.Direction.Left
                    : TrunkGeneratorScript.Direction.Right;

                lastWaterDirectionRight = !lastWaterDirectionRight;

                config.waterSpeed = Random.Range(2.0f, 4.0f);
                config.waterLength = Random.Range(2.0f, 3.3f);
                config.waterInterval = config.waterLength / config.waterSpeed + Random.Range(2f, 4f);
            }
        }

        lineConfigs[lineIndex] = config;
        return config;
    }

    private static int WorldZToLineIndex(float worldZ)
    {
        return Mathf.RoundToInt((worldZ - LineOffsetZ) / LineSpacing);
    }

    private static float LineIndexToWorldZ(int lineIndex)
    {
        return lineIndex * LineSpacing + LineOffsetZ;
    }

    public void Reset()
    {
        if (lines != null)
        {
            foreach (KeyValuePair<int, GameObject> kv in lines)
            {
                if (kv.Value != null)
                    Destroy(kv.Value);
            }
            lines.Clear();
        }

        if (lineConfigs != null)
            lineConfigs.Clear();

        if (queuedDestroyKeys != null)
            queuedDestroyKeys.Clear();

        if (destroyBuffer != null)
            destroyBuffer.Clear();

        lastWaterDirectionRight = false;
        keepCameraWindowDuringRespawn = false;

        ClampInspectorValues();
        CacheSceneObjects();

        if (player != null)
            EnsureWindowAroundWorldZ(player.transform.position.z);
    }
}
