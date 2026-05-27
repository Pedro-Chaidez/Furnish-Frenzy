using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class ThiefNPC : BaseNPC
{
    public enum ThiefState { Wandering, Chasing, Fleeing, Stunned }
    public ThiefState currentState = ThiefState.Wandering;

    public float wanderRadius = 10f;
    public float wanderTimer = 3f;
    public float sprintSpeed = 7f;
    public float walkSpeed = 3f;
    public float fleeDistance = 15f;
    public float stealRange = 2.5f;
    
    // Assign an empty GameObject located directly above the NPC's head in the inspector
    public Transform hoverPoint; 

    private NavMeshAgent agent;
    private float timer;
    private Item stolenItem;

    protected override void Awake()
    {
        base.Awake();
        agent = GetComponent<NavMeshAgent>();
        timer = wanderTimer;
        teamID = 1; // Ensure the NPC is marked as an enemy so BaseNPC targets the player
    }

    protected override void Update()
    {
        // Let BaseNPC handle finding the player (currentTarget)
        base.Update();
        
        // If we are fleeing and the player picks the item back up, the item's parent will be set to null by Item.Interact()
        if (currentState == ThiefState.Fleeing && stolenItem != null)
        {
            if (stolenItem.transform.parent != hoverPoint)
            {
                stolenItem = null;
                StartCoroutine(StunRoutine());
            }
        }
    }

    protected override void PerformBehavior()
    {
        if (currentState == ThiefState.Stunned) return;

        if (currentState == ThiefState.Fleeing)
        {
            agent.speed = sprintSpeed;
            FleeFromPlayer();
            return;
        }

        Item targetItem = GetPlayerSelectedItem();

        if (targetItem != null)
        {
            currentState = ThiefState.Chasing;
            agent.speed = sprintSpeed;
            
            if (currentTarget != null)
            {
                agent.SetDestination(currentTarget.transform.position);
                
                // Check if close enough to steal
                float dist = Vector3.Distance(transform.position, targetItem.transform.position);
                if (dist <= stealRange)
                {
                    StealItem(targetItem);
                }
            }
        }
        else
        {
            currentState = ThiefState.Wandering;
            agent.speed = walkSpeed;
            Wander();
        }
    }

    // Checks the player's inventory array to find the currently active item
    private Item GetPlayerSelectedItem()
    {
        if (Inventory.instance == null || Inventory.instance.items == null) return null;
        
        foreach (Item item in Inventory.instance.items)
        {
            if (item != null && item.gameObject.activeInHierarchy)
            {
                return item;
            }
        }
        return null;
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

    private void FleeFromPlayer()
    {
        if (currentTarget == null || agent.pathPending) return;
        
        // Pick a new location away from the player once we reach our current destination
        if (agent.remainingDistance < 1f)
        {
            Vector3 dirToPlayer = transform.position - currentTarget.transform.position;
            Vector3 newPos = transform.position + dirToPlayer.normalized * fleeDistance;
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(newPos, out hit, fleeDistance, 1))
            {
                agent.SetDestination(hit.position);
            }
        }
    }

    private void StealItem(Item itemToSteal)
    {
        // Run standard unequip logic
        itemToSteal.OnUnequipCustom();
        if (itemToSteal.physicsController != null)
        {
            itemToSteal.physicsController.OnUnequip();
        }

        // Remove from player inventory
        Inventory.instance.RemoveItem(itemToSteal);
        stolenItem = itemToSteal;
        
        // Parent and hover the item
        stolenItem.transform.SetParent(hoverPoint);
        stolenItem.transform.position = hoverPoint.position;
        stolenItem.transform.rotation = hoverPoint.rotation;
        
        // Disable physics so the item doesn't fall or collide wildly
        Rigidbody rb = stolenItem.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        
        if (stolenItem.physicsController != null)
        {
            stolenItem.physicsController.enabled = false;
        }

        // Run away
        currentState = ThiefState.Fleeing;
        agent.ResetPath(); 
    }

    private IEnumerator StunRoutine()
    {
        currentState = ThiefState.Stunned;
        agent.isStopped = true;
        
        yield return new WaitForSeconds(1f);
        
        agent.isStopped = false;
        currentState = ThiefState.Wandering;
    }
}