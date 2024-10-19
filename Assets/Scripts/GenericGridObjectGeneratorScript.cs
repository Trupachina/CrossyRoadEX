using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GenericGridObjectGeneratorScript : MonoBehaviour
{
    public Vector3 minPosition;
    public Vector3 maxPosition;
    public Vector3 gridSize = new Vector3(1, 1, 3);

    public float density = 0.12f;
    public bool relative = true;
    public bool destroyWhenDestroyed = true;

    public GameObject[] prefabs;

    // Диапазоны для рандомизации масштаба (размеров) объектов
    public Vector3 minScale = new Vector3(30f, 30f, 30f);
    public Vector3 maxScale = new Vector3(40f, 40f, 40f);

    private List<GameObject> generatedObjects;

    public void Start()
    {
        generatedObjects = new List<GameObject>();

        if (prefabs.Length == 0)
        {
            Debug.LogWarning("No prefabs assigned to the generator.");
            return;
        }

        for (var x = minPosition.x; x <= maxPosition.x; x += gridSize.x)
        {
            for (var y = minPosition.y; y <= maxPosition.y; y += gridSize.y)
            {
                for (var z = minPosition.z; z <= maxPosition.z; z += gridSize.z)
                {
                    if (Random.value < density)
                    {
                        GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];

                        // Рассчитываем позицию с фиксированной высотой по оси Y на 0
                        Vector3 spawnPosition = relative
                            ? transform.position + new Vector3(x, 0, z)
                            : new Vector3(x, 0, z);

                        // Инстанцируем объект с сохранением его поворота
                        GameObject spawnedObject = Instantiate(prefab, spawnPosition, prefab.transform.rotation);

                        // Рандомизация размеров
                        Vector3 randomScale = new Vector3(
                            Random.Range(minScale.x, maxScale.x),
                            Random.Range(minScale.y, maxScale.y),
                            Random.Range(minScale.z, maxScale.z)
                        );
                        spawnedObject.transform.localScale = randomScale;

                        generatedObjects.Add(spawnedObject);
                        OnInstantiate(spawnedObject);
                    }
                }
            }
        }
    }

    public void OnDestroy()
    {
        if (destroyWhenDestroyed)
        {
            foreach (var o in generatedObjects)
            {
                if (o != null)
                {
                    Destroy(o);
                }
            }
        }
    }

    protected virtual void OnInstantiate(GameObject o)
    {
        // Дополнительные действия при инстанцировании (можно переопределить в дочернем классе)
    }
}
