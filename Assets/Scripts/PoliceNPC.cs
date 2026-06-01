using UnityEngine;
 
public class PoliceNPC : BaseNPC
{
    [Header("Police Settings")]
    public float chaseSpeed = 5f;
    public float catchDistance = 1.5f;
 
    protected override void Awake()
    {
        base.Awake();
        teamID = 1;
        gameObject.tag = "Enemy";
    }
 
    protected override void PerformBehavior()
    {
        if (currentTarget == null) return;
 
        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
 
        if (distance <= catchDistance)
        {
            CatchPlayer();
        }
        else
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                currentTarget.transform.position,
                chaseSpeed * Time.deltaTime
            );
 
            Vector3 direction = (currentTarget.transform.position - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                transform.forward = direction;
            }
        }
    }
 
    private void CatchPlayer()
    {
        Debug.Log("Player caught by police!");
 
        BustedScreen busted = FindAnyObjectByType<BustedScreen>();
        if (busted != null)
        {
            busted.ShowBusted();
        }
        else
        {
            Debug.LogWarning("No BustedScreen found in scene!");
        }
    }
}