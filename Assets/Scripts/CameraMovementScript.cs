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

        initialOffset = new Vector3(3.5f, 25.0f, -9.0f);
        offset = initialOffset;

        StartCoroutine(DelayedCameraCatchUp());
    }

    public void Update()
    {
        if (Menu.activeInHierarchy)
        {
            return; // Если меню открыто, камера не двигается
        }

        if (moving && isCatchingUp) // Камера движется только после задержки
        {
            //Vector3 targetPosition = player.transform.position + offset;

            Vector3 targetPosition = new Vector3(
    transform.position.x,          // X остаётся прежним
    transform.position.y,          // Y остаётся прежним
    player.transform.position.z + offset.z // Только Z обновляется
);


            // Используем Lerp для плавного движения камеры
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 3f);

            offset.z += speedIncrementZ * Time.deltaTime;

            if (playerMovement.IsMoving && playerMovement.MoveDirection == "north")
            {
                offset.z -= speedOffsetZ * Time.deltaTime;
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
        //transform.position = player.transform.position + initialOffset; // Ставим камеру на начальную позицию относительно игрока

        StartCoroutine(DelayedCameraCatchUp()); // Снова запускаем задержку для плавного начала
    }
}
