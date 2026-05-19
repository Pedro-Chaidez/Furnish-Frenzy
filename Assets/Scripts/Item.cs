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
    public float damageVelocityThreshold = 5f;
    public float itemDamage = 25f;
 
    private bool isThrown = false;
 
    private void Awake()
    {
        physicsController = GetComponent<HeldItemPhysics>();
    }
 
    public abstract void useItem();
 
    protected override void Interact()
    {
        if (Inventory.instance.AddItem(this))
        {
            // Remove parent and restore physics
            transform.parent = null;
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false; // Turn physics back on if it was placed on a static object
            }
            
            // --- THE FIX: Added the '?' to safely check for null ---
            PersistentStateManager.Instance?.RemovePlacedItem(this.itemName); 

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
            isThrown = true;
            rb.AddForce(direction * force, ForceMode.Impulse);
            Debug.Log($"{itemName} thrown with force {force}");
        }
        else
        {
            isThrown = false;
        }
    }
 
    private void OnCollisionEnter(Collision collision)
    {
        if (!isThrown) return;
 
        Rigidbody rb = GetComponent<Rigidbody>();
 
        if (rb != null && rb.linearVelocity.magnitude >= damageVelocityThreshold)
        {
            Entity hitEntity = collision.gameObject.GetComponent<Entity>();
            if (hitEntity != null)
            {
                hitEntity.TakeDamage(itemDamage, this.gameObject);
 
                Debug.Log($"{itemName} hit {collision.gameObject.name} for {itemDamage} damage! " +
                          $"(velocity: {rb.linearVelocity.magnitude:F1})");
            }
        }
 
        isThrown = false;
    }
}