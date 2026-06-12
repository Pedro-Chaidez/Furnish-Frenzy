using UnityEngine;
using System.Collections;

// Assuming BaseNPC exists and inherits from MonoBehaviour
public class AggressiveNPC : BaseNPC
{
    // These get set by LevelManager based on the current level
    public float moveSpeed = 2f;
    public float attackCooldown = 1.5f;

    // Note: Assuming attackDamage, attackRange, currentTarget, and animator 
    // are defined in BaseNPC. If not, they need to be added here.
    // Example: public float attackDamage = 10f;
    // Example: public float attackRange = 1.5f;

    private bool isAttacking = false;

    protected override void Awake()
    {
        base.Awake(); // Sets the "Enemy" tag (assuming BaseNPC does this)
        teamID = 1;   // Assuming teamID is defined in BaseNPC
    }

    protected override void UpdateAnimator()
    {
        base.UpdateAnimator();
        // Add specific AggressiveNPC animator logic here if needed
    }

    protected override void PerformBehavior()
    {
        // Don't do anything if we are currently attacking or have no target
        if (isAttacking || currentTarget == null) return;

        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);

        if (distance <= attackRange)
        {
            StartCoroutine(AttackRoutine());
        }
        else
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                currentTarget.transform.position,
                moveSpeed * Time.deltaTime
            );
        }
    }

    // Called by LevelManager to scale difficulty
    public void SetDifficulty(float speed, float damage)
    {
        moveSpeed = speed;
        attackDamage = damage;
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;

        // Trigger hit animation
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        // Apply damage. Note: Using Time.deltaTime here might be incorrect 
        // if this is a single hit. Usually, you apply full damage once per attack.
        // Assuming currentTarget has a TakeDamage method.
        if (currentTarget != null)
        {
            // If it's a burst attack, you might want just attackDamage instead of scaling by deltaTime
            currentTarget.TakeDamage(attackDamage);
        }

        // Wait for attack cooldown
        yield return new WaitForSeconds(attackCooldown);

        isAttacking = false;
    }
}