using UnityEngine;
using System.Collections;

[RequireComponent(typeof(PlayerMovementScript))]
public class CameraMovementScript : MonoBehaviour
{
    public float minZ = 0.0f;
    public float speedIncrementZ = 1.0f;
    public float speedOffsetZ = 4.0f;
    public bool moving = true;
    public float gameOverDistance = -1.0f; // Дистанция до игрока для game over
    public GameObject Menu;

    private GameObject player;
    public PlayerMovementScript playerMovement;
    private GameStateControllerScript gameStateController;

    private Vector3 offset;
    private Vector3 initialOffset;

    public void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        gameStateController = GameObject.Find("GameStateController").GetComponent<GameStateControllerScript>();

        initialOffset = new Vector3(3f, 10.0f, -7.5f);
        offset = initialOffset;
    }

    public void Update()
    {
        if (Menu.activeInHierarchy)
        {
            return; // Если объект включен, остановить движение камеры
        }

        if (moving)
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

    public void Reset()
    {
        moving = false;
        offset = initialOffset;
        transform.position = player.transform.position + initialOffset;
    }

    private void GameOver()
    {
        moving = false;
        playerMovement.GameOver(); // Останавливаем игрока
        gameStateController.GameOver(); // Активируем Game Over
    }
}
