using Unity.MP_FPS;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    private InputSystem_Actions playerInput;
    public InputSystem_Actions.PlayerActions onFoot;
    public InputSystem_Actions.UIActions uiActions;

    private PlayerMotor motor;
    private PlayerLook look;
    private Inventory inventory;
    private Player player;

    // --- NEW: Tracks the current state so OnEnable doesn't break the tutorial ---
    [HideInInspector]
    public bool isPlayerControlActive = true;

    private void Awake()
    {
        playerInput = new InputSystem_Actions();
        onFoot = playerInput.Player;
        uiActions = playerInput.UI;

        motor = GetComponent<PlayerMotor>();
        look = GetComponent<PlayerLook>();
        inventory = GetComponent<Inventory>();
        player = GetComponent<Player>();

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
        onFoot.DropItem.started += ctx => { if (inventory != null) inventory.BeginDrop(); };
        onFoot.DropItem.canceled += ctx => { if (inventory != null) inventory.EndDrop(); };
        onFoot.UseItem.performed += ctx => { if (inventory != null) inventory.UseItem(); };

        onFoot.Block.started += ctx => { if (player != null) player.SetBlock(true); };
        onFoot.Block.canceled += ctx => { if (player != null) player.SetBlock(false); };
    }

    private void FixedUpdate()
    {
        if (motor != null && playerInput != null && onFoot.enabled)
        {
            motor.ProcessMove(onFoot.Move.ReadValue<Vector2>());
        }
    }

    private void LateUpdate()
    {
        if (look != null && playerInput != null && onFoot.enabled)
        {
            look.ProcessLook(onFoot.Look.ReadValue<Vector2>());
        }
    }

    public void SetPlayerControls(bool playerEnabled)
    {
        isPlayerControlActive = playerEnabled; // Save the state

        if (playerEnabled)
        {
            uiActions.Disable();
            onFoot.Enable();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            onFoot.Disable();
            uiActions.Enable();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void OnEnable()
    {
        if (playerInput != null)
        {
            // --- UPDATED: Automatically uses the correct state (Player vs UI) ---
            SetPlayerControls(isPlayerControlActive);
        }
    }

    private void OnDisable()
    {
        if (playerInput != null)
        {
            onFoot.Disable();
            uiActions.Disable();
        }
    }

    private void Start()
    {
        if (motor == null) motor = GetComponent<PlayerMotor>();
        if (look == null) look = GetComponent<PlayerLook>();
        if (inventory == null) inventory = GetComponent<Inventory>();
        if (player == null) player = GetComponent<Player>();

        // --- THE FIX: We removed SetPlayerControls(true) from here. ---
        // It is now handled automatically by OnEnable(), so it will no longer 
        // fight with the TutorialManager's Start() method.
    }
}