using UnityEngine;

public class HouseZone : RectangleZone
{
    protected override void Awake()
    {
        // You MUST call base.Awake() so the BoxCollider gets set up properly
        base.Awake();

        // Set default values specific to the Shopping Cart zone
        if (string.IsNullOrEmpty(zoneName))
        {
            zoneName = "Home";
        }
    }

    // You can add specific behaviors for shopping carts here in the future
}