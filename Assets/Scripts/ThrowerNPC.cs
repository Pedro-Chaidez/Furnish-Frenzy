using UnityEngine;
using System.Collections;

public class ThrowerNPC : BaseNPC
{
    [Header("Throw Settings")]
    public GameObject itemPrefab; // The item to throw
    public Transform throwPoint;  // Where the item spawns (e.g., the hand)
    public float throwVelocity = 15f;
    public float throwAngle = 45f; // Upward angle in degrees
    public float throwRate = 2f;   // Seconds between throws
    private float nextThrowTime = 0f;

    [Header("Movement Settings")]
    public float maxChaseDistance = 15f; // If target is further than this, start chasing
    public float stopDistance = 8f;      // If target is this close, stop and throw
    public float moveSpeed = 3f;

    private bool isChasing = false;

    protected override void Awake()
    {
        base.Awake(); // Sets the "Enemy" tag
        teamID = 1;
    }

    protected override void UpdateAnimator()
    {
        base.UpdateAnimator(); // keeps Speed/Idle/Walk working
    }

    protected override void PerformBehavior()
    {
        if (currentTarget == null) return;

        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);

        // 1. Decide what state the NPC should be in based on distance
        if (distanceToTarget > maxChaseDistance)
        {
            isChasing = true;
        }
        else if (distanceToTarget <= stopDistance)
        {
            isChasing = false;
        }

        // 2. Execute the behavior for that state
        if (isChasing)
        {
            MoveTowardsTarget();
        }
        else
        {
            AimAndThrow();
        }
    }

    private void MoveTowardsTarget()
    {
        Vector3 targetPos = currentTarget.transform.position;
        Vector3 direction = (targetPos - transform.position).normalized;

        // Keep the NPC upright by ignoring the Y axis
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            // Smoothly rotate to face the player while walking
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);
        }

        // Move the NPC forward
        transform.position += direction * moveSpeed * Time.deltaTime;
    }

    private void AimAndThrow()
    {
        // Face the target before throwing
        Vector3 lookDir = currentTarget.transform.position - transform.position;
        lookDir.y = 0;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 10f);

        // Check if enough time has passed to throw again
        if (Time.time >= nextThrowTime)
        {
            StartCoroutine(ThrowRoutine());
            nextThrowTime = Time.time + throwRate;
        }
    }

    private IEnumerator ThrowRoutine()
    {
        animator.SetBool("Throwing", true);
        // Wait a moment for throw animation to wind up
        yield return new WaitForSeconds(0.2f);

        ThrowItem(); // actual item spawns mid animation

        yield return new WaitForSeconds(0.9f);
        animator.SetBool("Throwing", false);
    }

    private void ThrowItem()
    {
        if (itemPrefab == null || throwPoint == null) return;

        // Spawn the item
        GameObject thrownItem = Instantiate(itemPrefab, throwPoint.position, throwPoint.rotation);

        // --- THE FIX: Set up the item's thrown properties for the NPC ---
        Item itemScript = thrownItem.GetComponent<Item>();
        if (itemScript != null)
        {
            itemScript.isThrown = true;
            itemScript.currentThrower = this.gameObject;
        }

        Rigidbody rb = thrownItem.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Tilt the NPC's forward direction upward by the throwAngle
            Vector3 throwDirection = Quaternion.AngleAxis(-throwAngle, transform.right) * transform.forward;

            // Apply the velocity to the item's rigidbody
            rb.linearVelocity = throwDirection * throwVelocity;
        }
    }
}