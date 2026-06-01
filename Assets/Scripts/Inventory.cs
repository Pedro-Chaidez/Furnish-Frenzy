using UnityEngine;
using UnityEngine.UI;
using System;

public class Inventory : MonoBehaviour
{
	public static Inventory instance;
	private readonly static ushort LIST_CAPACITY = 5;

	// CHANGED: Using a fixed array instead of a List to allow for 'null' empty slots
	public Item[] items;

	[SerializeField]
	private ushort selectedItem = 0;
	private GameObject[] slotIcons;
	public Transform dropPoint;

	private float dropStartTime = 0f;
	private bool isHoldingDrop = false;
	public float throwForceMultiplier = 15f;

	[Header("Placement Settings")]
	public Camera playerCamera; // Assign your main camera in the inspector
	public float placementRange = 4f;
	public float rotationSpeed = 30f;

	private void Awake()
{
    if (instance == null) instance = this;
    else Debug.LogWarning("More than one instance of inventory found!");

    items = new Item[LIST_CAPACITY];
    slotIcons = new GameObject[LIST_CAPACITY];

    GameObject inventoryUI = GameObject.Find("/Canvas/Inventory");

    if (inventoryUI == null)
    {
        Debug.LogWarning("Old Inventory UI not found at /Canvas/Inventory. Skipping old inventory slot icons.");
        return;
    }

    for (int i = 0; i < LIST_CAPACITY; i++)
    {
        if (i < inventoryUI.transform.childCount)
        {
            slotIcons[i] = inventoryUI.transform.GetChild(i).gameObject;
        }
    }

    UpdateUI();
}
	// --- NEW: Item Rotation Logic ---
	public void RotateHeldItem(Vector2 lookInput)
	{
			if (items[selectedItem] == null) return;

			float rotX = lookInput.x * rotationSpeed * Time.deltaTime;
			float rotY = lookInput.y * rotationSpeed * Time.deltaTime;

			// Rotate the drop point relative to the camera's orientation
			dropPoint.Rotate(playerCamera.transform.up, -rotX, Space.World);
			dropPoint.Rotate(playerCamera.transform.right, rotY, Space.World);
	}

	// --- NEW: Item Placement Logic ---
	public void PlaceItem()
	{
			if (items[selectedItem] == null) return;

			Item itemToPlace = items[selectedItem];

			Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
			if (Physics.Raycast(ray, out RaycastHit hit, placementRange))
			{
					// 1. Position and Parent
					itemToPlace.transform.position = hit.point;
					itemToPlace.transform.parent = hit.collider.transform;

					// 2. Unequip safely FIRST (Let the Shopping Cart do its default behavior)
					itemToPlace.OnUnequipCustom();
					itemToPlace.physicsController.OnUnequip();

					// 3. Handle Physics logic based on target object AFTER unequip
					Rigidbody itemRb = itemToPlace.GetComponent<Rigidbody>();
					Rigidbody targetRb = hit.collider.GetComponent<Rigidbody>();

					if (itemRb != null)
					{
							// This now safely overrides the ShoppingCart's default unequip logic
							itemRb.isKinematic = (targetRb == null);
					}
					
					// 4. Save to Persistent State Manager
					string itemID = itemToPlace.itemName + "_" + System.Guid.NewGuid().ToString(); 
					PersistentStateManager.Instance?.SavePlacedItem(itemID, itemToPlace.transform);

					// 5. Clear Inventory Slot
					items[selectedItem] = null;
					dropPoint.localRotation = Quaternion.identity; 
					UpdateUI();
					EquipItem();
			}
			else
			{
					Debug.Log("Nothing in range to attach to!");
			}
	}
	public void RemoveItem(Item itemToRemove)
	{
			for (ushort i = 0; i < items.Length; i++)
			{
					if (items[i] == itemToRemove)
					{
							items[i] = null;
							UpdateUI();
							EquipItem();
							break;
					}
			}
	}

	// --- TWO-HANDED LOCK LOGIC ---
	private bool IsHoldingTwoHandedItem()
	{
			if (items[selectedItem] != null)
			{
					return items[selectedItem].needsTwoHandsToPickUp;
			}
			return false;
	}

	// --- ITEM MANAGEMENT ---
	public bool AddItem(Item newItem)
	{
			// Find the first empty (null) slot in the array
			for (ushort i = 0; i < LIST_CAPACITY; i++)
			{
					if (items[i] == null)
					{
							items[i] = newItem;
							Debug.Log("Added " + newItem.itemName + " to slot " + i);
							UpdateUI();
							return true;
					}
			}

			Debug.Log("Inventory is full!");
			return false;
	}

	public void BeginDrop()
	{
			if (items[selectedItem] == null) return;
			isHoldingDrop = true;
			dropStartTime = Time.time;
	}

	public void EndDrop()
	{
			if (!isHoldingDrop || items[selectedItem] == null) return;
			isHoldingDrop = false;

			float holdTime = Time.time - dropStartTime;
			float throwForce = 0f;

			// Check if held for at least 1 second to activate the gauge
			if (holdTime >= 1f)
			{
					// Convert time to levels 1, 2, or 3
					int level = Mathf.FloorToInt(holdTime);
					if (level > 3) level = 3; // Cap at level 3

					throwForce = level * throwForceMultiplier;
					Debug.Log($"Item thrown at Level {level} with force {throwForce}");
			}

			ExecuteDropOrThrow(throwForce);
	}

	private void ExecuteDropOrThrow(float throwForce)
	{
			try
			{
					Item itemToDrop = items[selectedItem];

					// 1. Run custom unequip logic (turns shopping cart physics back on)
					itemToDrop.OnUnequipCustom();

					itemToDrop.physicsController.OnUnequip();
					itemToDrop.transform.parent = null;
					itemToDrop.transform.position = dropPoint != null ? dropPoint.position : transform.position + (transform.forward * 2);
					itemToDrop.gameObject.SetActive(true);

					// --- THE FIX: Tell the item that the Player threw it ---
					itemToDrop.currentThrower = this.gameObject;

					// 2. Apply the throw force based on the gauge
					itemToDrop.OnDrop(throwForce, transform.forward);

					items[selectedItem] = null;
					UpdateUI();
					EquipItem();
			}
			catch (Exception ex)
			{
					Debug.LogError("Error dropping item: " + ex.Message);
			}
	}

	public void UseItem()
	{
			if (items[selectedItem] != null)
			{
					items[selectedItem].useItem();
					Destroy(items[selectedItem].gameObject);
					items[selectedItem] = null; // Clear the slot
					UpdateUI();
					EquipItem();
			}
	}

	public void EquipItem()
	{
			for (ushort i = 0; i < items.Length; i++)
			{
					if (items[i] != null)
					{
							items[i].OnUnequipCustom(); // Safely unequip custom logic
							items[i].physicsController.OnUnequip();
							items[i].gameObject.SetActive(false);
					}
			}

			if (items[selectedItem] != null)
			{
					Item currentItem = items[selectedItem];
					currentItem.transform.parent = null;
					currentItem.gameObject.SetActive(true);
					currentItem.transform.position = dropPoint.position;

					currentItem.physicsController.OnEquip();

					// Trigger custom equip (for the shopping cart)
					currentItem.OnEquipCustom(transform);
			}
	}

	private void UpdateUI()
{
    if (slotIcons == null) return;

    for (ushort i = 0; i < slotIcons.Length; i++)
    {
        if (slotIcons[i] == null) continue;

        if (items[i] != null)
        {
            Image img = slotIcons[i].GetComponent<Image>();

            if (img != null)
            {
                img.sprite = items[i].icon;
            }

            slotIcons[i].SetActive(true);
        }
        else
        {
            slotIcons[i].SetActive(false);
        }
    }
}
	// --- SELECTION & INPUT LOGIC ---

	// Centralized method to handle slot changes so we don't repeat the two-handed check
	private void ChangeSelectedSlot(ushort newSlot)
	{
			if (IsHoldingTwoHandedItem())
			{
					Debug.Log("Hands are full! You must drop the heavy item first.");
					return; // Block the switch
			}

			selectedItem = newSlot;
			UpdateUI();
			EquipItem();
	}


	public void item1Select() { ChangeSelectedSlot(0); }
	public void item2Select() { ChangeSelectedSlot(1); }
	public void item3Select() { ChangeSelectedSlot(2); }
	public void item4Select() { ChangeSelectedSlot(3); }
	public void item5Select() { ChangeSelectedSlot(4); }

	public void scrollUp()
	{
			if (IsHoldingTwoHandedItem()) return; // Block scroll

			selectedItem = (ushort)((selectedItem < LIST_CAPACITY - 1) ? selectedItem + 1 : 0);
			UpdateUI();
			EquipItem();
	}

	public void scrollDown()
	{
			if (IsHoldingTwoHandedItem()) return; // Block scroll

			selectedItem = (ushort)((selectedItem > 0) ? selectedItem - 1 : LIST_CAPACITY - 1);
			UpdateUI();
			EquipItem();
	}
	}