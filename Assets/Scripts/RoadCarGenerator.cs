using UnityEngine;
using System.Collections.Generic;

public class RoadCarGenerator : MonoBehaviour {
    public enum Direction { Left = -1, Right = 1 };

    public bool randomizeValues = false;

    public Direction direction;
    public float speed = 2.0f;
    public float interval = 6.0f;
    public float leftX = -20.0f;
    public float rightX = 20.0f;

    public GameObject[] carPrefabs;

    private float elapsedTime;
    private List<GameObject> cars;

    public void Start() {
        if (randomizeValues) {
            direction = Random.value < 0.5f ? Direction.Left : Direction.Right;
            speed = Random.Range(2.0f, 4.0f);
            interval = Random.Range(5.0f, 9.0f);
        }

        elapsedTime = 0.0f;

        if (cars == null)
            cars = new List<GameObject>(16);
        else
            cars.Clear();
    }

    public void Update() {
        elapsedTime += Time.deltaTime;

        if (elapsedTime > interval) {
            elapsedTime = 0.0f;

            Vector3 position = transform.position + new Vector3(direction == Direction.Left ? rightX : leftX, 0.6f, 0f);
            GameObject o = Instantiate(carPrefabs[Random.Range(0, carPrefabs.Length)], position, Quaternion.Euler(0f, 0f, 0f));
            o.GetComponent<CarScript>().speedX = (int)direction * speed;

            if (direction < 0)
                o.transform.rotation = Quaternion.Euler(0f, 270f, 0f);
            else
                o.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            cars.Add(o);
        }

        for (int i = cars.Count - 1; i >= 0; i--) {
            GameObject o = cars[i];

            if (o == null) {
                cars.RemoveAt(i);
                continue;
            }

            bool shouldDestroy =
                (direction == Direction.Left && o.transform.position.x < leftX) ||
                (direction == Direction.Right && o.transform.position.x > rightX);

            if (shouldDestroy) {
                Destroy(o);
                cars.RemoveAt(i);
            }
        }
    }

    public void OnDestroy() {
        if (cars == null)
            return;

        for (int i = cars.Count - 1; i >= 0; i--) {
            if (cars[i] != null)
                Destroy(cars[i]);
        }
    }
}
