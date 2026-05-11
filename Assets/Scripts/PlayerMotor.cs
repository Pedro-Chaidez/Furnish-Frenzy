using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Timeline;

[RequireComponent(typeof(CharacterController))]
public class PlayerMotor : MonoBehaviour
{
    private CharacterController controller;
    private Vector3 playerVelocity;
    private bool isGrounded;
    public bool crouching;
    public bool sprinting;
    public bool isBlocking; // New rule to check if we are blocking

    public float speed = 6f;
    public float gravity = -10f;
    public float jumpHeight = 7f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        isGrounded = controller.isGrounded;
    }

    public void Crouch()
    {
        crouching = !crouching;
    }

    public void Sprint()
    {
        sprinting = !sprinting;
        if (sprinting)
        {
            speed = 12f;
        }
        else
        {
            speed = 6f;
        }
    }

    public void ProcessMove(Vector2 input)
    {
        Vector3 moveDirection = Vector3.zero;
        moveDirection.x = input.x;
        moveDirection.z = input.y;

        float currentSpeed = speed;

        // Cut speed in half if we are holding the block button
        if (isBlocking)
        {
            currentSpeed /= 2f;
        }

        controller.Move(transform.TransformDirection(moveDirection) * currentSpeed * Time.deltaTime);
        playerVelocity.y += gravity * Time.deltaTime;

        if (isGrounded && playerVelocity.y < 0)
            playerVelocity.y = -2f;

        controller.Move(playerVelocity * Time.deltaTime);
    }

    public void Jump()
    {
        if (isGrounded)
        {
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -3.0f * gravity);
        }
    }
}