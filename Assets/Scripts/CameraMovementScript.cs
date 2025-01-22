using UnityEngine;
using System.Collections;

[RequireComponent(typeof(PlayerMovementScript))]
public class CameraMovementScript : MonoBehaviour
{
    public float minZ = 0.0f;
    public float speedIncrementZ = 0.3f;
    public float speedOffsetZ = 1.5f;
    public bool moving = false;
    public float gameOverDistance = -1.5f; // Дистанция до игрока для game over
    public GameObject Menu;

    private GameObject player;
    public PlayerMovementScript playerMovement;
    private GameStateControllerScript gameStateController;

    private Vector3 offset;
    private Vector3 initialOffset;
    private bool isCatchingUp = false;

    public void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        gameStateController = GameObject.Find("GameStateController").GetComponent<GameStateControllerScript>();

        initialOffset = new Vector3(3.36f, 9.88f, -4.76f);
        offset = initialOffset;

        StartCoroutine(DelayedCameraCatchUp());
    }

    public void Update()
    {
        if (Menu.activeInHierarchy)
        {
            return; // Если объект включен, остановить движение камеры
        }

        if (moving && isCatchingUp) // Камера движется после задержки
        {
            Vector3 playerPosition = player.transform.position;
            transform.position = new Vector3(playerPosition.x, 0, Mathf.Max(minZ, playerPosition.z)) + offset;

            offset.z += speedIncrementZ * Time.deltaTime;

            if (playerMovement.IsMoving)
            {
                if (playerMovement.MoveDirection == "north")
                {
                    offset.z -= speedOffsetZ * Time.deltaTime;
                }
            }

            // Проверка на Game Over
            if (transform.position.z > player.transform.position.z - gameOverDistance)
            {
                GameOver();
            }
        }
    }

    IEnumerator DelayedCameraCatchUp()
    {
        yield return new WaitForSeconds(1.0f); // Задержка на 1 секунду
        isCatchingUp = true; // Камера начинает двигаться
    }

    public void Reset()
    {
        // Останавливаем движение камеры
        moving = false;
        isCatchingUp = false; // Сброс догоняющего состояния
        offset = initialOffset; // Возвращаем смещение камеры в начальное положение
        transform.position = player.transform.position + initialOffset; // Ставим камеру на начальную позицию относительно игрока

        StartCoroutine(DelayedCameraCatchUp()); // Снова запускаем задержку для плавного начала
    }

    private void GameOver()
    {
        moving = false;
        playerMovement.GameOver(); // Останавливаем игрока
        gameStateController.GameOver(); // Активируем Game Over
    }
}
