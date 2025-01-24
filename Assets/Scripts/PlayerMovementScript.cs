using UnityEngine;
using System.Collections;

public class PlayerMovementScript : MonoBehaviour
{
    public bool canMove = false;
    public float timeForMove = 0.2f; // Время для движения
    public float jumpHeight = 1.0f;

    public int minX = -4;
    public int maxX = 4;

    public GameObject[] leftSide;
    public GameObject[] rightSide;

    public float leftRotation = -45.0f;
    public float rightRotation = 90.0f;

    public AudioSource moveSound;

    public GameObject Menu;

    private bool moving;
    private float elapsedTime;

    private Vector3 current;
    private Vector3 target;
    private float startY;

    private Rigidbody body;
    private GameObject mesh;

    private GameStateControllerScript gameStateController;
    private int score;

    private float moveCooldown = 0.3f; // Минимальное время между движениями
    private float lastMoveTime = 0f;  // Время последнего движения

    public void Start()
    {
        current = transform.position;
        moving = false;
        startY = transform.position.y;

        body = GetComponentInChildren<Rigidbody>();

        mesh = GameObject.Find("Player/Chicken");

        score = 0;
        gameStateController = GameObject.Find("GameStateController").GetComponent<GameStateControllerScript>();
    }

    public void Update()
    {
        if (Menu.activeInHierarchy)
        {
            return;
        }

        // Если игрок движется, обновляем позицию, иначе обрабатываем ввод.
        if (moving)
        {
            MovePlayer();
        }
        else
        {
            // Округляем позицию игрока до ближайшего целого числа (чтобы избежать дробных значений).
            current = new Vector3(Mathf.Round(transform.position.x), Mathf.Round(transform.position.y), Mathf.Round(transform.position.z));

            if (canMove)
                HandleInput();
        }

        // Обновляем счёт
        score = Mathf.Max(score, (int)current.z);
        gameStateController.score = score / 3;
    }

    private void HandleInput()
    {
        // Игнорируем ввод, если игрок движется или прошло недостаточно времени с последнего движения
        if (moving || Time.time - lastMoveTime < moveCooldown)
        {
            return;
        }

        // Обработка нажатий клавиш
        if (Input.GetKeyDown(KeyCode.W))
        {
            Move(new Vector3(0, 0, 3.2f));
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            Move(new Vector3(0, 0, -3.2f));
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            if (Mathf.RoundToInt(current.x) > minX)
                Move(new Vector3(-2, 0, 0));
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            if (Mathf.RoundToInt(current.x) < maxX)
                Move(new Vector3(2, 0, 0));
        }
    }

    private void Move(Vector3 distance)
    {
        var newPosition = current + distance;

        // Проверка на наличие препятствия
        if (Physics.CheckSphere(newPosition + new Vector3(0.0f, 0.5f, 0.0f), 0.1f))
            return;

        target = newPosition;

        moving = true;
        elapsedTime = 0;
        body.isKinematic = true;

        // Запоминаем время последнего движения
        lastMoveTime = Time.time;

        // Обновляем направление игрока
        switch (MoveDirection)
        {
            case "north":
                mesh.transform.rotation = Quaternion.Euler(0, 0, 0);
                break;
            case "south":
                mesh.transform.rotation = Quaternion.Euler(0, 180, 0);
                break;
            case "east":
                mesh.transform.rotation = Quaternion.Euler(0, 270, 0);
                break;
            case "west":
                mesh.transform.rotation = Quaternion.Euler(0, 90, 0);
                break;
            default:
                break;
        }

        // Анимация движения конечностей
        foreach (var o in leftSide)
        {
            o.transform.Rotate(leftRotation, 0, 0);
        }

        foreach (var o in rightSide)
        {
            o.transform.Rotate(rightRotation, 0, 0);
        }
    }

    private void MovePlayer()
    {
        elapsedTime += Time.deltaTime;

        float weight = (elapsedTime < timeForMove) ? (elapsedTime / timeForMove) : 1;
        float x = Lerp(current.x, target.x, weight);
        float z = Lerp(current.z, target.z, weight);
        float y = Sinerp(current.y, startY + jumpHeight, weight);

        Vector3 result = new Vector3(x, y, z);
        transform.position = result;

        if (!moveSound.isPlaying && moving)
        {
            moveSound.Play(); // Воспроизводим звук при начале движения
        }

        if (result == target)
        {
            moving = false;
            body.isKinematic = false;
            body.AddForce(0, -10, 0, ForceMode.VelocityChange);
            moveSound.Stop(); // Останавливаем звук после завершения движения

            // Сбрасываем конечности в начальное положение
            foreach (var o in leftSide)
            {
                o.transform.rotation = Quaternion.identity;
            }

            foreach (var o in rightSide)
            {
                o.transform.rotation = Quaternion.identity;
            }
        }
    }

    private float Lerp(float min, float max, float weight)
    {
        return min + (max - min) * weight;
    }

    private float Sinerp(float min, float max, float weight)
    {
        return min + (max - min) * Mathf.Sin(weight * Mathf.PI);
    }

    public bool IsMoving
    {
        get { return moving; }
    }

    public string MoveDirection
    {
        get
        {
            if (moving)
            {
                float dx = target.x - current.x;
                float dz = target.z - current.z;
                if (dz > 0)
                    return "north";
                else if (dz < 0)
                    return "south";
                else if (dx > 0)
                    return "west";
                else
                    return "east";
            }
            else
                return null;
        }
    }

    public void GameOver()
    {
        canMove = false;
        gameStateController.GameOver();
    }

    public void Reset()
    {
        transform.position = new Vector3(0.44f, 1f, -0.16f);
        transform.localScale = new Vector3(1, 1, 1.13f);
        transform.rotation = Quaternion.identity;
        score = 0;
    }
}
