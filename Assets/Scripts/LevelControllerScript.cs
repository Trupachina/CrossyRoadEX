using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LevelControllerScript : MonoBehaviour
{
    public int minZ = 3;
    public int lineAhead = 40;
    public int lineBehind = 20;
    public float destroyDelay = 5f;

    public GameObject[] linePrefabs;      // Обычные линии
    public GameObject railroadLine;       // Префаб железной дороги
    [Range(0f, 1f)]
    public float railroadChance = 0.05f;

    private Dictionary<int, GameObject> lines;
    private GameObject player;

    private bool lastWaterDirectionRight = false; // для шахматной логики

    public void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        lines = new Dictionary<int, GameObject>();
    }

    public void Update()
    {
        int playerZ = (int)player.transform.position.z;

        for (int z = Mathf.Max(minZ, playerZ - lineBehind); z <= playerZ + lineAhead; z += 1)
        {
            if (!lines.ContainsKey(z))
            {
                GameObject line;
                Vector3 position = new Vector3(0, 0, z * 3 - 5);

                if (Random.value < railroadChance)
                {
                    line = Instantiate(railroadLine, position, Quaternion.identity);
                }
                else
                {
                    line = Instantiate(
                        linePrefabs[Random.Range(0, linePrefabs.Length)],
                        position,
                        Quaternion.identity
                    );
                    line.transform.localScale = new Vector3(1, 1, 3);

                    if (line.CompareTag("WaterLine"))
                    {
                        TrunkGeneratorScript trunkGenerator = line.GetComponent<TrunkGeneratorScript>();
                        if (trunkGenerator != null)
                        {
                            // чередуем направление
                            TrunkGeneratorScript.Direction dir = lastWaterDirectionRight
                                ? TrunkGeneratorScript.Direction.Left
                                : TrunkGeneratorScript.Direction.Right;

                            //trunkGenerator.SetForcedDirection(dir);
                            lastWaterDirectionRight = !lastWaterDirectionRight;

                            // Рандомизация параметров
                            float speed = Random.Range(2.0f, 4.0f);
                            float length = Random.Range(2.0f, 3.3f);
                            float interval = length / speed + Random.Range(2f, 4f);

                            trunkGenerator.SetParameters(dir, speed, length, interval);
                        }
                    }
                }

                lines.Add(z, line);
            }
        }

        foreach (var line in new List<GameObject>(lines.Values))
        {
            float lineZ = line.transform.position.z;
            if (lineZ < playerZ - lineBehind * 3 && !IsCoroutineRunning(line))
            {
                StartCoroutine(DestroyLineWithDelay(line, (int)(lineZ / 3 + 1.67f)));
            }
        }
    }

    private bool IsCoroutineRunning(GameObject line)
    {
        return line.GetComponent<LineDestroyer>() != null;
    }

    private IEnumerator DestroyLineWithDelay(GameObject line, int lineZ)
    {
        var destroyer = line.AddComponent<LineDestroyer>();
        yield return new WaitForSeconds(destroyDelay);
        lines.Remove(lineZ);
        Destroy(line);
    }

    public void Reset()
    {
        if (lines != null)
        {
            foreach (var line in new List<GameObject>(lines.Values))
            {
                Destroy(line);
            }
            lines.Clear();
            Start();
        }
    }
}

public class LineDestroyer : MonoBehaviour { }
