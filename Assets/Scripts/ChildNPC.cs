using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class ChildNPC : BaseNPC
{
	public ParentNPC captain;
	public float followDistance = 4f;
	public float itemDetectionRadius = 15f;
	public float throwForce = 20f;
	public float walkSpeed = 3.5f;
	public float runSpeed = 6f;
	public Transform hoverPoint;
	private NavMeshAgent agent;
	private Item targetItem;
	private Item heldItem;

	private enum FollowerState { Following, MovingToItem, Throwing }
	private FollowerState currentState = FollowerState.Following;

	protected override void Awake()
	{
			base.Awake();
			agent = GetComponent<NavMeshAgent>();
			teamID = 1;
			agent.speed = walkSpeed;
	}

	protected override void Update()
	{
			base.Update();
	}

	protected override void PerformBehavior()
	{
			if (captain == null) return;

			switch (currentState)
			{
					case FollowerState.Following:
							FollowCaptain();
							DetectItems();
							break;
					case FollowerState.MovingToItem:
							MoveToItem();
							break;
					case FollowerState.Throwing:
							break;
			}
	}

	private void FollowCaptain()
	{
			agent.speed = walkSpeed;
			float distToCaptain = Vector3.Distance(transform.position, captain.transform.position);
					
			if (distToCaptain > followDistance)
			{
					agent.SetDestination(captain.transform.position);
			}
	}

	private void DetectItems()
	{
			Collider[] colliders = Physics.OverlapSphere(transform.position, itemDetectionRadius);
			Item closestItem = null;
			float closestDist = Mathf.Infinity;

			foreach (Collider col in colliders)
			{
					Item item = col.GetComponent<Item>();
					
					if (item != null && item.transform.parent == null && item.gameObject.activeInHierarchy)
					{
							float dist = Vector3.Distance(transform.position, item.transform.position);
							if (dist < closestDist)
							{
									closestDist = dist;
									closestItem = item;
							}
					}
			}

			if (closestItem != null)
			{
					targetItem = closestItem;
					currentState = FollowerState.MovingToItem;
					agent.speed = runSpeed;
			}
	}

	private void MoveToItem()
	{
			if (targetItem == null || targetItem.transform.parent != null)
			{
					currentState = FollowerState.Following;
					return;
			}

			agent.SetDestination(targetItem.transform.position);
			float dist = Vector3.Distance(transform.position, targetItem.transform.position);

			if (dist <= 2f)
			{
					PickUpAndThrow(targetItem);
			}
	}

	private void PickUpAndThrow(Item item)
	{
			heldItem = item;
					
			heldItem.transform.SetParent(hoverPoint);
			heldItem.transform.position = hoverPoint.position;
			heldItem.transform.rotation = hoverPoint.rotation;

			Rigidbody rb = heldItem.GetComponent<Rigidbody>();
			if (rb != null) rb.isKinematic = true;

			if (heldItem.physicsController != null)
			{
					heldItem.physicsController.enabled = false;
			}

			currentState = FollowerState.Throwing;
					
			agent.isStopped = true;
			Invoke(nameof(ExecuteThrow), 0.5f); 
	}

	private void ExecuteThrow()
	{
			if (heldItem == null || currentTarget == null) 
			{
					ResetState();
					return;
			}

			heldItem.transform.SetParent(null);
			Rigidbody rb = heldItem.GetComponent<Rigidbody>();
			if (rb != null) rb.isKinematic = false;

			if (heldItem.physicsController != null)
			{
					heldItem.physicsController.enabled = true;
			}

			heldItem.currentThrower = this.gameObject;
					
			Vector3 directionToPlayer = (currentTarget.transform.position - heldItem.transform.position).normalized;
			directionToPlayer.y += 0.25f; 
			directionToPlayer.Normalize();

			heldItem.OnDrop(throwForce, directionToPlayer);

			heldItem = null;
			targetItem = null;
					
			ResetState();
	}

	private void ResetState()
	{
			agent.isStopped = false;
			currentState = FollowerState.Following;
	}
}