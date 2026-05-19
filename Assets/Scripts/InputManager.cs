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
    private Player player; 

    // --- NEW: State Tracking ---
    public bool isManipulatingItem = false;

    private void Awake()
    {
        playerInput = new InputSystem_Actions();
        onFoot = playerInput.Player;

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

        // --- UPDATED: State Toggles for Item Manipulation ---
        onFoot.RotateItem.started += ctx => isManipulatingItem = true;
        onFoot.RotateItem.canceled += ctx => {
            isManipulatingItem = false;
            // Reset drop point rotation when letting go of R
            if(inventory != null && inventory.dropPoint != null)
                inventory.dropPoint.localRotation = Quaternion.identity; 
        };

        // --- UPDATED: Reroute Block to Place Item if Manipulating ---
        onFoot.Block.started += ctx => { 
            if (isManipulatingItem) {
                if (inventory != null) inventory.PlaceItem();
            } else if (player != null) {
                player.SetBlock(true); 
            }
        };
        onFoot.Block.canceled += ctx => { 
            if (!isManipulatingItem && player != null) player.SetBlock(false); 
        };
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
            Vector2 lookInput = onFoot.Look.ReadValue<Vector2>();

            // --- UPDATED: Route mouse input based on current state ---
            if (isManipulatingItem)
            {
                if(inventory != null) inventory.RotateHeldItem(lookInput);
            }
            else
            {
                look.ProcessLook(lookInput);
            }
        }
    }

    private void OnEnable() { if (playerInput != null) onFoot.Enable(); }
    private void OnDisable() { if (playerInput != null) onFoot.Disable(); }
    
    private void Start()
    {
        if (motor == null) motor = GetComponent<PlayerMotor>();
        if (look == null) look = GetComponent<PlayerLook>();
        if (inventory == null) inventory = GetComponent<Inventory>();
        if (player == null) player = GetComponent<Player>(); 
    }
}