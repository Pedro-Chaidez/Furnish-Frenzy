using UnityEngine;

public abstract class BaseNPC : Entity
{
    public Entity currentTarget;
    public float attackRange = 5f;
    public float attackDamage = 10f;

    protected virtual void Awake()
    {
        // Automatically set the tag for all NPCs
        gameObject.tag = "Enemy";
    }

    protected virtual void Update()
    {
        if (currentTarget != null)
        {
            PerformBehavior();
        }
        else
        {
            SearchForTarget();
        }
    }

    protected virtual void SearchForTarget()
    {
        // FIXED: Replaced the obsolete FindObjectsSortMode with FindObjectsInactive
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