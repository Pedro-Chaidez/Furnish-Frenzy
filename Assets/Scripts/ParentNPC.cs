using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class ParentNPC : BaseNPC
{
    public ChildNPC[] followers;
    public float wanderRadius = 25f;
    public float wanderTimer = 5f;
    public float walkSpeed = 3f;
    public float rageSprintSpeed = 7f;
    private NavMeshAgent agent;
    private float timer;
    private bool isEnraged = false;
    protected override void Awake()
    {
        base.Awake();
        agent = GetComponent<NavMeshAgent>();
        timer = wanderTimer;
        teamID = 1;

        foreach (var follower in followers)
        {
            if (follower != null)
            {
                follower.captain = this;
            }
        }
    }

    protected override void Update()
    {
        base.Update();
        CheckFollowers();
    }

    private void CheckFollowers()
    {
        if (isEnraged) return;

        int activeFollowers = 0;
        for (int i = 0; i < followers.Length; i++)
        {
            if (followers[i] != null)
            {
                activeFollowers++;
            }
        }

        if (activeFollowers == 0)
        {
            isEnraged = true;
            agent.speed = rageSprintSpeed;
        }
    }

    protected override void PerformBehavior()
    {
        if (isEnraged)
        {
            RageAttack();
        }
        else
        {
            Wander();
        }
    }

    private void Wander()
    {
        timer += Time.deltaTime;
        if (timer >= wanderTimer)
        {
            Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
            randomDirection += transform.position;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, 1))
            {
                agent.SetDestination(hit.position);
            }
            timer = 0;
        }
    }

    private void RageAttack()
    {
        if (currentTarget == null) return;

        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);

        if (distance <= attackRange)
        {
            currentTarget.TakeDamage(attackDamage * Time.deltaTime);
            agent.SetDestination(transform.position);
        }
        else
        {
            agent.SetDestination(currentTarget.transform.position);
        }
    }
}