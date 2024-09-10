using UnityEngine;

public class CarScript : MonoBehaviour
{
    public float speedX = 1.0f;
    private GameStateControllerScript gameStateController;

    void Start()
    {
        // Находим объект GameStateControllerScript в сцене
        gameStateController = FindObjectOfType<GameStateControllerScript>();
    }

    public void Update()
    {
        // Двигаем машину вперед
        transform.position += new Vector3(speedX * Time.deltaTime, 0.0f, 0.0f);
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
