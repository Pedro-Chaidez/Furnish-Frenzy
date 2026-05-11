using UnityEngine;

public abstract class Item : Interactable
{
    public string itemName;
    public Sprite icon;
    protected string itemType;
    protected float durability;
    public bool needsTwoHandsToPickUp;

    public HeldItemPhysics physicsController;

    [Header("Damage Settings")]
    public float damageVelocityThreshold = 5f; // Item must move at least this fast to hurt you
    public float itemDamage = 25f;

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

    // Check when the item crashes into something
    private void OnCollisionEnter(Collision collision)
    {
        Rigidbody rb = GetComponent<Rigidbody>();

        // Check if the item is moving fast enough
        if (rb != null && rb.linearVelocity.magnitude >= damageVelocityThreshold)
        {
            Entity hitEntity = collision.gameObject.GetComponent<Entity>();

            if (hitEntity != null)
            {
                // Deal damage and tell the player exactly which object hit them
                hitEntity.TakeDamage(itemDamage, this.gameObject);
            }
        }
    }
}