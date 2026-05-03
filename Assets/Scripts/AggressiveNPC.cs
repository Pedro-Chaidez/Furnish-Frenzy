using UnityEngine;

public class AggressiveNPC : BaseNPC
{
    protected override void Awake()
    {
        base.Awake(); // Sets the "Enemy" tag
        teamID = 1;
    }

    protected override void PerformBehavior()
    {
        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);

        if (distance <= attackRange)
        {
            currentTarget.TakeDamage(attackDamage * Time.deltaTime);
        }
        else
        {
            transform.position = Vector3.MoveTowards(transform.position, currentTarget.transform.position, 2f * Time.deltaTime);
        }
    }
}
