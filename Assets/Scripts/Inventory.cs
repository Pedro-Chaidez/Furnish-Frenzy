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

    private void Awake()
    {
        if (instance == null) instance = this;
        else Debug.LogWarning("More than one instance of inventory found!");

        // --- THE FIX: Force the array to be exactly 5, ignoring the Inspector ---
        items = new Item[LIST_CAPACITY];

        slotIcons = new GameObject[LIST_CAPACITY];
        for (int i = 0; i < LIST_CAPACITY; i++)
        {
            slotIcons[i] = GameObject.Find("/Canvas/Inventory").transform.GetChild(i).gameObject;
        }
        UpdateUI();
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
        for (ushort i = 0; i < slotIcons.Length; i++)
        {
            if (items[i] != null)
            {
                Image img = slotIcons[i].GetComponent<Image>();
                img.sprite = items[i].icon;
                slotIcons[i].SetActive(true);
            }
            else
            {
                slotIcons[i].SetActive(false); // Hide icon if slot is empty
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