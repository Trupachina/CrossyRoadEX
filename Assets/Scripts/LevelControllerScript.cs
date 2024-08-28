using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LevelControllerScript : MonoBehaviour
{
    public int minZ = 3;
    public int lineAhead = 40;
    public int lineBehind = 20;
    public float destroyDelay = 5f; // Время задержки перед удалением линии

    public GameObject[] linePrefabs;
    public GameObject coins;

    private Dictionary<int, GameObject> lines;

    private GameObject player;

    public void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        lines = new Dictionary<int, GameObject>();
    }

    public void Update()
    {
        // Генерация линий на основе позиции игрока
        var playerZ = (int)player.transform.position.z;
        for (var z = Mathf.Max(minZ, playerZ - lineBehind); z <= playerZ + lineAhead; z += 1)
        {
            if (!lines.ContainsKey(z))
            {
                GameObject coin;
                int x = Random.Range(0, 2);
                if (x == 1)
                {
                    coin = (GameObject)Instantiate(coins);
                    int randX = UnityEngine.Random.Range(-4, 4);
                    coin.transform.position = new Vector3(randX, 1, 1.5f);
                }

                var line = (GameObject)Instantiate(
                    linePrefabs[Random.Range(0, linePrefabs.Length)],
                    new Vector3(0, 0, z * 3 - 5),
                    Quaternion.identity
                );

                line.transform.localScale = new Vector3(1, 1, 3);
                lines.Add(z, line);
            }
        }

        // Удаление линий за игроком с задержкой
        foreach (var line in new List<GameObject>(lines.Values))
        {
            var lineZ = line.transform.position.z;
            if (lineZ < playerZ - lineBehind && !IsCoroutineRunning(line))
            {
                StartCoroutine(DestroyLineWithDelay(line, (int)lineZ));
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
        // Сброс уровня
        if (lines != null)
        {
            foreach (var line in new List<GameObject>(lines.Values))
            {
                Destroy(line);
            }
            Start();
        }
    }
}

public class LineDestroyer : MonoBehaviour { }
