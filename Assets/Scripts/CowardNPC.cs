using UnityEngine;

public class CowardNPC : BaseNPC
{
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
            transform.position = Vector3.MoveTowards(transform.position, runTo, 3f * Time.deltaTime);
        }
    }
}
