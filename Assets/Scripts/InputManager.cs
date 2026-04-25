using Unity.MP_FPS;
using UnityEngine;
using UnityEngine.InputSystem;
public class InputManager : MonoBehaviour
{
    private InputSystem_Actions playerInput;
    public InputSystem_Actions.PlayerActions onFoot;
    private PlayerMotor motor;
    private PlayerLook look;
    private Inventory inventory;
    private void Awake()
    {
        playerInput = new InputSystem_Actions();
        onFoot = playerInput.Player;

        motor = GetComponent<PlayerMotor>();
        look = GetComponent<PlayerLook>();
        inventory = GetComponent<Inventory>();

        onFoot.Jump.performed += ctx => { if (motor != null) motor.Jump(); };
        onFoot.Crouch.performed += ctx => { if (motor != null) motor.Crouch(); };
        onFoot.Sprint.performed += ctx => { if (motor != null) motor.Sprint(); };
        onFoot.NextItem.performed += ctx => { if (inventory != null) inventory.scrollUp(); };
        onFoot.PreviousItem.performed += ctx => { if (inventory != null) inventory.scrollDown(); };
        onFoot.SelectFirstItem.performed += ctx => { if (inventory != null) inventory.item1Select(); };
        onFoot.SelectSecondItem.performed += ctx => { if (inventory != null) inventory.item2Select(); };
        onFoot.SelectThirdItem.performed += ctx => { if (inventory != null) inventory.item3Select(); };
        onFoot.SelectFourthItem.performed += ctx => { if (inventory != null) inventory.item4Select(); };
        onFoot.SelectFifthItem.performed += ctx => { if (inventory != null) inventory.item5Select(); };
        //onFoot.DropItem.performed += ctx => { if (inventory != null) inventory.DropItem(); };
        onFoot.DropItem.started += ctx => { if (inventory != null) inventory.BeginDrop(); };
        onFoot.DropItem.canceled += ctx => { if (inventory != null) inventory.EndDrop(); };
        onFoot.UseItem.performed += ctx => { if (inventory != null) inventory.UseItem(); };
    }
    private void FixedUpdate()
    {
        if (motor != null && playerInput != null)
    {
        motor.ProcessMove(onFoot.Move.ReadValue<Vector2>());
        }
    }
    private void LateUpdate()
    {
        if (look != null && playerInput != null)
    {
        look.ProcessLook(onFoot.Look.ReadValue<Vector2>());
        }
    }
    private void OnEnable()
    {
        if (playerInput != null)
    {
        onFoot.Enable();
        }
    }
    private void OnDisable()
    {
        if (playerInput != null)
    {
        onFoot.Disable();
        }
    }
    
    // Re-check components if they were null initially (for delayed initialization)
    private void Start()
    {
        // Re-check components in case they weren't ready in Awake
        if (motor == null) motor = GetComponent<PlayerMotor>();
        if (look == null) look = GetComponent<PlayerLook>();
        if (inventory == null) inventory = GetComponent<Inventory>();
    }
}