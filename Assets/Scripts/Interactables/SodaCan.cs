using UnityEngine;
public class SodaCan : Item
{
    public override void useItem()
    {
        Debug.Log("Used " + itemName);
    }
}