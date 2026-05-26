using UnityEngine;
 
public class CowardNPC : BaseNPC
{
    // Gets set by LevelManager — cowards run faster at higher levels
    public float moveSpeed = 3f;
 
    protected override void Awake()
    {
        base.Awake(); // Sets the "Enemy" tag
        teamID = 1;
    }
 
    protected override void PerformBehavior()
    {
        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
        if (distance < 10f)
        {
            Vector3 runDirection = transform.position - currentTarget.transform.position;
            Vector3 runTo = transform.position + runDirection.normalized * 5f;
            transform.position = Vector3.MoveTowards(
                transform.position,
                runTo,
                moveSpeed * Time.deltaTime
            );
        }
    }
 
    // Called by LevelManager to scale difficulty
    public void SetDifficulty(float speed)
    {
        moveSpeed = speed;
    }
}
 