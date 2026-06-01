using UnityEngine;

public class CartSlotsFollower : MonoBehaviour
{
    public Transform cartTransform;
    public Vector3 localOffset = new Vector3(0f, 0.4f, 0f);

    void LateUpdate()
    {
        if (cartTransform == null) return;

        transform.position = cartTransform.position +
                           cartTransform.TransformDirection(localOffset);
        transform.rotation = Quaternion.LookRotation(
                           cartTransform.forward, Vector3.up);
    }
}