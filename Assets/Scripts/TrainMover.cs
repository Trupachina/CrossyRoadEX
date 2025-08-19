using UnityEngine;

public class TrainMover : MonoBehaviour
{
    private float speed;
    private GameStateControllerScript gameStateController;

    public void Start()
    {
        // Находим объект GameStateControllerScript в сцене
        gameStateController = FindObjectOfType<GameStateControllerScript>();
    }

    public void Initialize(float speed)
    {
        this.speed = speed;
    }

    void Update()
    {
        transform.Translate(Vector3.right * speed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        // Когда сталкивается с игроком, сжимаем его и уменьшаем жизнь
        if (other.gameObject.CompareTag("Player"))
        {
            // Проверяем, находится ли игрок в состоянии, когда он может потерять жизнь
            if (!gameStateController.isInCooldown)
            {
                Vector3 scale = other.gameObject.transform.localScale;
                other.gameObject.transform.localScale = new Vector3(scale.x, scale.y * 0.1f, scale.z);
                other.gameObject.SendMessage("GameOver");
            }
        }
    }
}
