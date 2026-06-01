using UnityEngine;
using UnityEngine.AI;

public abstract class BaseNPC : Entity
{
    public Entity currentTarget;
    public float attackRange = 5f;
    public float attackDamage = 10f;
    public float rotationSpeed = 5f;
    public Vector3 lastPosition;
    protected Animator animator;

    protected virtual void Awake()
    {
        gameObject.tag = "Enemy";
        animator = GetComponentInChildren<Animator>();
        lastPosition = transform.position;
    }

    protected virtual void Update()
    {
        UpdateAnimator();
        RotateTowardTarget();

        if (currentTarget != null)
        {
            PerformBehavior();
        }
        else
        {
            SearchForTarget();
        }
    }

    protected virtual void RotateTowardTarget()
    {
        if (currentTarget == null) return;

        Vector3 direction = (currentTarget.transform.position - transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    protected virtual void UpdateAnimator()
    {
        if (animator == null) return;
        float speed = (transform.position - lastPosition).magnitude / Time.deltaTime;
        if (speed > 0.1f)
            animator.SetFloat("Speed", 1);
        else
            animator.SetFloat("Speed", 0);
        lastPosition = transform.position;
    }

    protected virtual void SearchForTarget()
    {
        Entity[] allEntities = Object.FindObjectsByType<Entity>(FindObjectsInactive.Exclude);
        float closestDistance = Mathf.Infinity;
        Entity closestEntity = null;

        foreach (Entity e in allEntities)
        {
            if (e != this && e.teamID != this.teamID)
            {
                float distance = Vector3.Distance(transform.position, e.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEntity = e;
                }
            }
        }
        currentTarget = closestEntity;
    }

    protected virtual void PerformBehavior()
    {
    }
}