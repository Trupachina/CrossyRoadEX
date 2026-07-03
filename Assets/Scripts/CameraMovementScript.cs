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
    private Coroutine delayedCatchUpRoutine;

    public void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        gameStateController = GameObject.Find("GameStateController").GetComponent<GameStateControllerScript>();

        initialOffset = new Vector3(3.5f, 25.0f, -9.0f);
        offset = initialOffset;

        RestartCatchUpDelay();
    }

    public void Update()
    {
        if (Menu != null && Menu.activeInHierarchy)
        {
            return; // Если меню открыто, камера не двигается
        }

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
            return;

        if (moving && isCatchingUp) // Камера движется только после задержки
        {
            Vector3 targetPosition = new Vector3(
                transform.position.x,
                transform.position.y,
                player.transform.position.z + offset.z
            );

            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 3f);

            offset.z += speedIncrementZ * Time.deltaTime;

            if (playerMovement != null && playerMovement.IsMoving && playerMovement.MoveDirection == "north")
            {
                offset.z -= speedOffsetZ * Time.deltaTime;
            }
        }
    }

    private IEnumerator DelayedCameraCatchUp()
    {
        yield return new WaitForSeconds(1.0f); // Задержка на 1 секунду
        isCatchingUp = true; // Камера начинает двигаться
        delayedCatchUpRoutine = null;
    }

    private void RestartCatchUpDelay()
    {
        if (delayedCatchUpRoutine != null)
        {
            StopCoroutine(delayedCatchUpRoutine);
            delayedCatchUpRoutine = null;
        }

        delayedCatchUpRoutine = StartCoroutine(DelayedCameraCatchUp());
    }

    public void Reset()
    {
        moving = false;
        isCatchingUp = false;
        offset = initialOffset;
        RestartCatchUpDelay();
    }
}
