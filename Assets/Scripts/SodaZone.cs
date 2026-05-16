using UnityEngine;

public class SodaZone : RectangleZone
{
    protected override void Awake()
    {
        base.Awake();

        if (string.IsNullOrEmpty(zoneName))
        {
            zoneName = "Soda Aisle";
        }
    }
}