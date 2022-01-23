using UnityEngine;

public class PlayerController : RaycastController
{
	public struct CollisionInfo
	{
		public bool top, bottom;
		public bool left, right;

		public bool climbingSlope, descendingSlope;

		public float currentSlopeAngle, previousSlopeAngle;

		public Vector2 previousdisplacement;

		public int faceDirection;

		public bool fallingThroughPlatform;

		public void Reset()
		{
			top = bottom = false;
			left = right = false;
			climbingSlope = false;
			descendingSlope = false;

			previousSlopeAngle = currentSlopeAngle;
			currentSlopeAngle = 0;
		}
	}

	public CollisionInfo collisionInfo;

	[HideInInspector]
	public Vector2 playerInput;

	private const float MaxClimbAngle = 80;
	private const float MaxDescendAngle = 75;

	protected override void Start()
	{
		base.Start();

		collisionInfo.faceDirection = 1;
		collisionMask = LayerMask.GetMask("Obstacles");
	}

	public void Move(Vector2 displacement, bool standingOnPlatform)
	{
		Move(displacement, Vector2.zero, standingOnPlatform);
	}

	public void Move(Vector2 displacement, Vector2 input, bool standingOnPlatform = false)
	{
		UpdateRaycastOrigins();
		collisionInfo.Reset();

		collisionInfo.previousdisplacement = displacement;

		playerInput = input;

		if (displacement.x != 0)
		{
			collisionInfo.faceDirection = (int)Mathf.Sign(displacement.x);
		}

		if (displacement.y < 0)
		{
			DescendSlope(ref displacement);
		}

		HorizontalCollisions(ref displacement);

		if (displacement.y != 0)
		{
			VerticalCollisions(ref displacement);
		}

		transform.Translate(displacement);

		if (standingOnPlatform)
		{
			collisionInfo.bottom = true;
		}

		Physics2D.SyncTransforms();
	}

	void HorizontalCollisions(ref Vector2 displacement)
	{
		float directionX = collisionInfo.faceDirection;
		float rayLength = Mathf.Abs(displacement.x) + SkinWidth;

		if (Mathf.Abs(displacement.x) < SkinWidth)
		{
			rayLength = 2 * SkinWidth;
		}

		for (int i = 0; i < horizontalRayCount; i++)
		{
			Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
			rayOrigin += Vector2.up * (horizontalRaySpacing * i);

			RaycastHit2D hit = Physics2D.Raycast(
				rayOrigin, Vector2.right * directionX, rayLength, collisionMask
			);

			Debug.DrawRay(rayOrigin, directionX * Vector2.right, Color.magenta);

			if (hit)
			{
				if (hit.distance == 0)
				{
					continue;
				}

				float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

				if (i == 0 && slopeAngle <= MaxClimbAngle)
				{
					if (collisionInfo.descendingSlope)
					{
						collisionInfo.descendingSlope = false;
						displacement = collisionInfo.previousdisplacement;
					}

					float distanceToSlope = 0;

					if (slopeAngle != collisionInfo.previousSlopeAngle)
					{
						distanceToSlope = hit.distance - SkinWidth;
						displacement.x -= distanceToSlope * directionX;
					}
					
					ClimbSlope(ref displacement, slopeAngle);

					displacement.x += distanceToSlope * directionX;
				}

				if (!collisionInfo.climbingSlope || slopeAngle > MaxClimbAngle)
				{
					displacement.x = (hit.distance - SkinWidth) * directionX;
					rayLength = hit.distance;

					if (collisionInfo.climbingSlope)
					{
						displacement.x = Mathf.Tan(collisionInfo.currentSlopeAngle * Mathf.Deg2Rad) * Mathf.Abs(displacement.x);
					}

					collisionInfo.left = directionX == -1;
					collisionInfo.right = directionX == 1;
				}
			}
		}
	}

	private void VerticalCollisions(ref Vector2 displacement)
	{
		float directionY = Mathf.Sign(displacement.y);
		float rayLength = Mathf.Abs(displacement.y) + SkinWidth;

		for (int i = 0; i < verticalRayCount; i++)
		{
			Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
			rayOrigin += Vector2.right * (verticalRaySpacing * i + displacement.x);

			RaycastHit2D hit = Physics2D.Raycast(
				rayOrigin, Vector2.up * directionY, rayLength, collisionMask
			);

			Debug.DrawRay(rayOrigin, directionY * Vector2.up, Color.magenta);

			if (hit)
			{
				if (hit.collider.CompareTag("PassablePlatform"))
				{
					if (directionY == 1 || hit.distance == 0)
					{
						continue;
					}

					if (collisionInfo.fallingThroughPlatform)
					{
						continue;
					}

					if (playerInput.y == -1)
					{
						collisionInfo.fallingThroughPlatform = true;

						Invoke(nameof(ResetFallingThroughPlatform), 0.5f);

						continue;
					}
				}

				displacement.y = (hit.distance - SkinWidth) * directionY;
				rayLength = hit.distance;

				if (collisionInfo.climbingSlope)
				{
					displacement.x = displacement.y / Mathf.Tan(collisionInfo.currentSlopeAngle * Mathf.Deg2Rad) * Mathf.Sign(displacement.x);
				}

				collisionInfo.bottom = directionY == -1;
				collisionInfo.top = directionY == 1;
			}
		}

		CheckSlopeChange(ref displacement, rayLength);
	}

	private void CheckSlopeChange(ref Vector2 displacement, float rayLength)
	{
		if (collisionInfo.climbingSlope)
		{
			float directionX = Mathf.Sign(displacement.x);
			rayLength = Mathf.Abs(displacement.x) + SkinWidth;

			Vector2 rayOrigin = ((directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight) + Vector2.up * displacement.y;

			RaycastHit2D hit = Physics2D.Raycast(
				rayOrigin, Vector2.right * directionX, rayLength, collisionMask
			);

			if (hit)
			{
				float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

				if (slopeAngle != collisionInfo.currentSlopeAngle)
				{
					displacement.x = (hit.distance - SkinWidth) * directionX;
					collisionInfo.currentSlopeAngle = slopeAngle;
				}
			}
		}
	}

	private void ClimbSlope(ref Vector2 displacement, float slopeAngle)
	{
		float moveDistance = Mathf.Abs(displacement.x);
		float climbdisplacementY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;

		if (displacement.y <= climbdisplacementY)
		{
			displacement.y = climbdisplacementY;
			displacement.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(displacement.x);
		}

		collisionInfo.bottom = true;
		collisionInfo.climbingSlope = true;
		collisionInfo.currentSlopeAngle = slopeAngle;
	}

	private void DescendSlope(ref Vector2 displacement)
	{
		float directionX = Mathf.Sign(displacement.x);

		Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;

		RaycastHit2D hit = Physics2D.Raycast(
			rayOrigin, Vector2.down, Mathf.Infinity, collisionMask
		);

		if (hit)
		{
			float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

			if (slopeAngle != 0 && slopeAngle <= MaxDescendAngle)
			{
				if (Mathf.Sign(hit.normal.x) == directionX)
				{
					if (hit.distance - SkinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(displacement.x))
					{
						float moveDistance = Mathf.Abs(displacement.x);

						float descendDisplacementY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
						
						displacement.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(displacement.x);
						displacement.y -= descendDisplacementY;

						collisionInfo.currentSlopeAngle = slopeAngle;
						collisionInfo.descendingSlope = true;
						collisionInfo.bottom = true;
					}
				}
			}
		}
	}

	private void ResetFallingThroughPlatform()
	{
		collisionInfo.fallingThroughPlatform = false;
	}
}
