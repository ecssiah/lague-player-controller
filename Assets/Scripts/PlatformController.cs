using System.Collections.Generic;
using UnityEngine;

public class PlatformController : RaycastController
{
	private struct PassengerMovement
	{
		public Transform transform;
		public Vector3 velocity;
		public bool standingOnPlatform;
		public bool moveBeforePlatform;

		public PassengerMovement(
			Transform _transform, 
			Vector3 _velocity, 
			bool _standingOnPlatform, 
			bool _moveBeforePlatform
		) {
			transform = _transform;
			velocity = _velocity;
			standingOnPlatform = _standingOnPlatform;
			moveBeforePlatform = _moveBeforePlatform;
		}
	}

	public LayerMask passengerMask;

	public Vector3[] localWaypoints;
	public Vector3[] globalWaypoints;

	public float speed;
	public bool cyclic;
	public float waitTime;

	[Range(0, 2)]
	public float easing;

	private int fromWaypointIndex;
	private float percentBetweenWaypoints;
	private float nextMoveTime;

	private List<PassengerMovement> passengerMovements;

	private Dictionary<Transform, PlayerController> passengerDictionary;

    protected override void Start()
    {
        base.Start();

		globalWaypoints = new Vector3[localWaypoints.Length];

		for (int i = 0; i < localWaypoints.Length; i++)
		{
			globalWaypoints[i] = localWaypoints[i] + transform.position;
		}

		passengerDictionary = new Dictionary<Transform, PlayerController>();
    }

	void Update()
	{
		UpdateRaycastOrigins();

        Vector3 velocity = CalculatePlatformMovement();

		CalculatePassengerMovement(velocity);

		MovePassengers(true);
        transform.Translate(velocity);
		MovePassengers(false);

        Physics2D.SyncTransforms();
	}

	private float Ease(float x)
	{
		float a = easing + 1;
		float xRaisedToA = Mathf.Pow(x, a);

		return xRaisedToA / (xRaisedToA + Mathf.Pow(1 - x, a));
	}

	private Vector3 CalculatePlatformMovement()
	{
		if (Time.time < nextMoveTime)
		{
			return Vector3.zero;
		}

		fromWaypointIndex %= globalWaypoints.Length;

		int toWaypointIndex = (fromWaypointIndex + 1) % globalWaypoints.Length;

		float distanceBetweenWaypoints = Vector3.Distance(
			globalWaypoints[fromWaypointIndex], globalWaypoints[toWaypointIndex]
		);

		percentBetweenWaypoints += Time.deltaTime * speed / distanceBetweenWaypoints;

		percentBetweenWaypoints = Mathf.Clamp01(percentBetweenWaypoints);
		float easedPercentBetweenWaypoints = Ease(percentBetweenWaypoints);

		Vector3 newPosition = Vector3.Lerp(
			globalWaypoints[fromWaypointIndex],
			globalWaypoints[toWaypointIndex],
			easedPercentBetweenWaypoints
		);

		if (percentBetweenWaypoints >= 1)
		{
			percentBetweenWaypoints = 0;
			fromWaypointIndex++;

			if (!cyclic)
			{
				if (fromWaypointIndex >= globalWaypoints.Length - 1)
				{
					fromWaypointIndex = 0;
					System.Array.Reverse(globalWaypoints);
				}
			}

			nextMoveTime = Time.time + waitTime;
		}

		return newPosition - transform.position;
	}

	private void MovePassengers(bool beforeMovePlatform)
	{
		foreach (PassengerMovement passengerMovement in passengerMovements)
		{
			if (!passengerDictionary.ContainsKey(passengerMovement.transform))
			{
				passengerDictionary.Add(
					passengerMovement.transform,
					passengerMovement.transform.GetComponent<PlayerController>()
				);
			}

			if (passengerMovement.moveBeforePlatform == beforeMovePlatform)
			{
				passengerDictionary[passengerMovement.transform].Move(
					passengerMovement.velocity, passengerMovement.standingOnPlatform
				);
			}
		}
	}

    private void CalculatePassengerMovement(Vector3 velocity)
	{
		HashSet<Transform> movedPassengers = new HashSet<Transform>();

		passengerMovements = new List<PassengerMovement>(); 

        float directionX = Mathf.Sign(velocity.x);
        float directionY = Mathf.Sign(velocity.y);

        if (velocity.y != 0)
		{
			float rayLength = Mathf.Abs(velocity.y) + SkinWidth;

			for (int i = 0; i < verticalRayCount; i++)
			{
				Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
				rayOrigin += Vector2.right * (verticalRaySpacing * i);

				RaycastHit2D hit = Physics2D.Raycast(
					rayOrigin, Vector2.up * directionY, rayLength, passengerMask
				);

				if (hit && hit.distance != 0)
				{
					if (!movedPassengers.Contains(hit.transform))
					{
						movedPassengers.Add(hit.transform);

						float pushX = (directionY == 1) ? velocity.x : 0;
						float pushY = velocity.y - (hit.distance - SkinWidth) * directionY;

						var passengerMovement = new PassengerMovement(
							hit.transform,
							new Vector3(pushX, pushY),
							directionY == 1,
							true
						);

						passengerMovements.Add(passengerMovement);
					}
				}
			}
		}

		if (velocity.x != 0)
		{
			float rayLength = Mathf.Abs(velocity.x) + SkinWidth;

			for (int i = 0; i < horizontalRayCount; i++)
			{
				Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
				rayOrigin += Vector2.up * (horizontalRaySpacing * i);

				RaycastHit2D hit = Physics2D.Raycast(
					rayOrigin, Vector2.right * directionX, rayLength, passengerMask
				);

				if (hit && hit.distance != 0)
				{
					if (!movedPassengers.Contains(hit.transform))
					{
						movedPassengers.Add(hit.transform);

						float pushX = velocity.x - (hit.distance - SkinWidth) * directionX;
						float pushY = -SkinWidth;

						var passengerMovement = new PassengerMovement(
							hit.transform,
							new Vector3(pushX, pushY),
							false,
							true
						);

						passengerMovements.Add(passengerMovement);
					}
				}
			}
		}

		if (directionY == -1 || velocity.y == 0 && velocity.x != 0)
		{
			float rayLength = SkinWidth * 2;

			for (int i = 0; i < verticalRayCount; i++)
			{
				Vector2 rayOrigin = raycastOrigins.topLeft + Vector2.right * (verticalRaySpacing * i);

				RaycastHit2D hit = Physics2D.Raycast(rayOrigin, rayLength * Vector2.up, rayLength, passengerMask);

				Debug.DrawRay(rayOrigin, rayLength * Vector2.up, Color.magenta);

				if (hit && hit.distance != 0)
				{
					if (!movedPassengers.Contains(hit.transform))
					{
						movedPassengers.Add(hit.transform);

						float pushX = velocity.x;
						float pushY = velocity.y;

						var passengerMovement = new PassengerMovement(
							hit.transform, new Vector3(pushX, pushY), true, false
						);

						passengerMovements.Add(passengerMovement);
					}
				}
			}
		}
	}

	void OnDrawGizmos()
	{
		if (localWaypoints != null)
		{
			Gizmos.color = Color.magenta;
			float size = 0.2f;

			for (int i = 0; i < localWaypoints.Length; i++)
			{
				Vector3 globalWaypointPosition = Application.isPlaying ? globalWaypoints[i] : localWaypoints[i] + transform.position;

				Gizmos.DrawLine(
					globalWaypointPosition - Vector3.up * size, 
					globalWaypointPosition + Vector3.up * size
				);

				Gizmos.DrawLine(
					globalWaypointPosition - Vector3.left * size, 
					globalWaypointPosition + Vector3.left * size
				);
			}
		}	
	}
}
