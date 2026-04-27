using UnityEngine;

public abstract class Item : Interactable
{
    public string itemName;
    public Sprite icon;
    protected string itemType;
    protected float durability;
    public bool needsTwoHandsToPickUp;

    public HeldItemPhysics physicsController;

    private void Awake()
    {
        physicsController = GetComponent<HeldItemPhysics>();
    }

    public abstract void useItem();

    protected override void Interact()
    {
        if (Inventory.instance.AddItem(this))
        {
            Inventory.instance.EquipItem();
        }
    }

    // --- NEW VIRTUAL METHODS ---
    public virtual void OnEquipCustom(Transform playerTransform) { }

    public virtual void OnUnequipCustom() { }

    public virtual void OnDrop(float force, Vector3 direction)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && force > 0)
        {
            rb.AddForce(direction * force, ForceMode.Impulse);
        }
    }
}