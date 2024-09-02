﻿using UnityEngine;
using System.Collections;

public class PlayerMovementScript : MonoBehaviour {
    public bool canMove = false;
    public float timeForMove = 0.2f;
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

    public void Start() {
        current = transform.position;
        moving = false;
        startY = transform.position.y;

        body = GetComponentInChildren<Rigidbody>();

        mesh = GameObject.Find("Player/Chicken");

        score = 0;
        gameStateController = GameObject.Find("GameStateController").GetComponent<GameStateControllerScript>();
    }

    public void Update() {

        if (Menu.activeInHierarchy)
        {
            return;
        }

        // If player is moving, update the player position, else receive input from user.
        if (moving)
            MovePlayer();
        else {
            // Update current to match integer position (not fractional).
            current = new Vector3(Mathf.Round(transform.position.x), Mathf.Round(transform.position.y), Mathf.Round(transform.position.z));

            if (canMove)
                HandleInput();
        }

        score = Mathf.Max(score, (int)current.z);
        gameStateController.score = score / 3;
    }
	
	private void HandleMouseClick() {
		RaycastHit hit;
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		
		if (Physics.Raycast(ray, out hit)) {
			var direction = hit.point - transform.position;
			var x = direction.x;
			var z = direction.z;
			
			if (Mathf.Abs(z) > Mathf.Abs(x)) {
				if (z > 0)
					Move(new Vector3(0, 0, 3));
                else
					Move(new Vector3(0, 0, -3));
			}
            else { // (Mathf.Abs(z) < Mathf.Abs(x))
				if (x > 0) {
					if (Mathf.RoundToInt(current.x) < maxX)
						Move(new Vector3(3, 0, 0));
				}
                else { // (x < 0)
					if (Mathf.RoundToInt(current.x) > minX)
						Move(new Vector3(-3, 0, 0));
				}
			}
        }
	}

    private void HandleInput()
    {
        // Handle mouse click
        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseClick();
            return;
        }

        else if (Input.GetKeyDown(KeyCode.W))
        { // Изменено F на W для стандартного движения вперёд
            Move(new Vector3(0, 0, 2)); // Замените 1 на 3 для соответствия шагу по клику
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            Move(new Vector3(0, 0, -2)); // Замените 1 на 3 для соответствия шагу по клику
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            if (Mathf.RoundToInt(current.x) > minX)
                Move(new Vector3(-2, 0, 0)); // Замените 1 на 3 для соответствия шагу по клику
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            if (Mathf.RoundToInt(current.x) < maxX)
                Move(new Vector3(2, 0, 0)); // Замените 1 на 3 для соответствия шагу по клику
        }
    }


    private void Move(Vector3 distance) {
        var newPosition = current + distance;

        // Don't move if blocked by obstacle.
        if (Physics.CheckSphere(newPosition + new Vector3(0.0f, 0.5f, 0.0f), 0.1f)) 
            return;

        target = newPosition;

        moving = true;
        elapsedTime = 0;
        body.isKinematic = true;
        print(MoveDirection);

        switch (MoveDirection) {
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

        // Rotate arm and leg.
        foreach (var o in leftSide) {
            o.transform.Rotate(leftRotation, 0, 0);
        }

        foreach (var o in rightSide) {
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
            moveSound.Play();  // Воспроизводим звук при начале движения
        }

        if (result == target)
        {
            moving = false;
            body.isKinematic = false;
            body.AddForce(0, -10, 0, ForceMode.VelocityChange);
            moveSound.Stop();  // Останавливаем звук после завершения движения

            // Возвращаем руки и ноги в исходное положение
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


    private float Lerp(float min, float max, float weight) {
        return min + (max - min) * weight;
    }

    private float Sinerp(float min, float max, float weight) {
        return min + (max - min) * Mathf.Sin(weight * Mathf.PI);
    }

    public bool IsMoving {
        get { return moving; }
    }

    public string MoveDirection {
        get
        {
            if (moving) {
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

    public void GameOver() {
        // When game over, disable moving.
        canMove = false;

        // Call GameOver at game state controller (instead of sending messages).
        gameStateController.GameOver();
    }

    public void Reset() {
        // TODO This kind of reset is dirty, refactor might be needed.
        transform.position = new Vector3(0, 1, 0);
        transform.localScale = new Vector3(1, 1, 1);
        transform.rotation = Quaternion.identity;
        score = 0;
    }
}
