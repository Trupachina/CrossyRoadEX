using UnityEngine;
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

    private List<GameObject> generatedObjects;

    public void Start()
    {
        generatedObjects = new List<GameObject>();

        for (float x = minPosition.x; x <= maxPosition.x; x += gridSize.x)
        {
            for (float y = minPosition.y; y <= maxPosition.y; y += gridSize.y)
            {
                for (float z = minPosition.z; z <= maxPosition.z; z += gridSize.z)
                {
                    bool generate = Random.value < density;
                    if (!generate)
                        continue;

                    if (prefabs == null || prefabs.Length == 0)
                        continue;

                    GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
                    if (prefab == null)
                        continue;

                    Vector3 spawnPosition = relative
                        ? transform.position + new Vector3(x, y, z)
                        : new Vector3(x, y, z);

                    GameObject o = Instantiate(prefab, spawnPosition, Quaternion.identity);

                    // Привязываем к линии/генератору, чтобы объект гарантированно удалялся вместе с ней
                    o.transform.SetParent(transform, true);

                    generatedObjects.Add(o);
                    OnInstantiate(o);
                }
            }
        }
    }

    public void OnDestroy()
    {
        if (!destroyWhenDestroyed)
            return;

        if (generatedObjects == null)
            return;

        for (int i = generatedObjects.Count - 1; i >= 0; i--)
        {
            GameObject o = generatedObjects[i];
            if (o != null)
                Destroy(o);
        }

        generatedObjects.Clear();
    }

    protected virtual void OnInstantiate(GameObject o)
    {
    }
}