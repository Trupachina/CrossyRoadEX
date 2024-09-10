using UnityEngine;

public class PlayerLifeController : MonoBehaviour
{
    public int lives = 3;
    public Vector3 respawnPosition;
    private Vector3 lastSafePosition;
    private Vector3 lastSafeScale;
    private PlayerMovementScript playerMovement;
    private GameStateControllerScript gameStateController;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovementScript>();
        gameStateController = GameObject.Find("GameStateControllerScript").GetComponent<GameStateControllerScript>();
        respawnPosition = transform.position;
        lastSafePosition = transform.position;
        lastSafeScale = transform.localScale;
    }

    public void SavePlayerState()
    {
        lastSafePosition = transform.position;
        lastSafeScale = transform.localScale;
    }

    public void LoseLife()
    {
        lives--;
        if (lives > 0)
        {
            Respawn();
        }
        else
        {
            // Âûחûגאול GameOver קונוח GameStateController
            gameStateController.GameOver();
        }
    }

    private void Respawn()
    {
        transform.position = lastSafePosition;
        transform.localScale = lastSafeScale;

        if (IsNearDanger())
        {
            MoveToSafePosition();
        }

        playerMovement.Reset();
    }

    private bool IsNearDanger()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, 1.5f);
        foreach (var collider in colliders)
        {
            if (collider.gameObject.tag == "Car" || collider.gameObject.tag == "Water")
            {
                return true;
            }
        }
        return false;
    }

    private void MoveToSafePosition()
    {
        transform.position += new Vector3(0, 0, 3.2f);
    }
}
