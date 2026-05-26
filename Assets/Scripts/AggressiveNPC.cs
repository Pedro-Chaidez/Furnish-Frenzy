using UnityEngine;
 
public class AggressiveNPC : BaseNPC
{
    // These get set by LevelManager based on the current level
    public float moveSpeed = 2f;
 
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
}