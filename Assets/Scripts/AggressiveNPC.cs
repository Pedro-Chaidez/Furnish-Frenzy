using UnityEngine;
using System.Collections;

public class AggressiveNPC : BaseNPC
{
    public float attackCooldown = 1.5f;
    private bool isAttacking = false;
    protected override void Awake()
    {
        base.Awake(); // Sets the "Enemy" tag
        teamID = 1;
    }

    protected override void UpdateAnimator()
    {
        base.UpdateAnimator();

    }

    protected override void PerformBehavior()
    {
        if (isAttacking) return;

        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);

        if (distance <= attackRange)
        {
            StartCoroutine(AttackRoutine());
        }
        else
        {
            transform.position = Vector3.MoveTowards(transform.position, currentTarget.transform.position, 2f * Time.deltaTime);
        }
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;

        // Trigger hit animation
        animator.SetTrigger("Attack");
        currentTarget.TakeDamage(attackDamage * Time.deltaTime);

        // Wait for animation to finish
        yield return new WaitForSeconds(attackCooldown);

        isAttacking = false;
    }
}
