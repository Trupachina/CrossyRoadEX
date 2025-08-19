using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TrunkGeneratorScript : MonoBehaviour
{
    public enum Direction { Left = -1, Right = 1 };

    [Header("Настройки спауна")]
    public float leftX = -20.0f;
    public float rightX = 20.0f;
    public GameObject trunkPrefab;

    // --- Параметры, устанавливаются извне ---
    private Direction direction;
    private float speed;
    private float length;
    private float interval;

    private float elapsedTime;
    private List<GameObject> trunks = new List<GameObject>();
    private bool isInitialized = false;

    public void SetParameters(Direction dir, float speed, float length, float interval)
    {
        this.direction = dir;
        this.speed = speed;
        this.length = length;
        this.interval = interval;
        isInitialized = true;
    }

    void Update()
    {
        if (!isInitialized) return;

        elapsedTime += Time.deltaTime;

        if (elapsedTime > interval)
        {
            elapsedTime = 0.0f;

            Vector3 spawnPos = transform.position + new Vector3(direction == Direction.Left ? rightX : leftX, 0.1f, 0);
            GameObject trunk = Instantiate(trunkPrefab, spawnPos, Quaternion.identity);

            TrunkFloatingScript floating = trunk.GetComponent<TrunkFloatingScript>();
            if (floating != null)
                floating.speedX = (int)direction * speed;

            Vector3 scale = trunk.transform.localScale;
            trunk.transform.localScale = new Vector3(scale.x * length, scale.y, scale.z * 3);

            trunks.Add(trunk);
        }

        foreach (GameObject trunk in trunks.ToArray())
        {
            if ((direction == Direction.Left && trunk.transform.position.x < leftX) ||
                (direction == Direction.Right && trunk.transform.position.x > rightX))
            {
                Destroy(trunk);
                trunks.Remove(trunk);
            }
        }
    }

    void OnDestroy()
    {
        foreach (var trunk in trunks)
        {
            Destroy(trunk);
        }
    }
}
