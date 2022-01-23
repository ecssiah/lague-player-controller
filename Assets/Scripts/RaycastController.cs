using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class RaycastController : MonoBehaviour
{
	protected struct RaycastOrigins
	{
		public Vector2 topLeft, topRight;
		public Vector2 bottomLeft, bottomRight;
	}

	protected LayerMask collisionMask;

	protected const float SkinWidth = 0.015f;

	protected const float RayDistance = 0.25f;

	public BoxCollider2D boxCollider2D;
	protected RaycastOrigins raycastOrigins;

	protected int horizontalRayCount;
	protected int verticalRayCount;

	protected float horizontalRaySpacing;
	protected float verticalRaySpacing;

	protected virtual void Awake()
	{
		boxCollider2D = GetComponent<BoxCollider2D>();
	}

	protected virtual void Start()
	{
		CalculateRaySpacing();
	}

	protected void UpdateRaycastOrigins()
	{
		Bounds bounds = boxCollider2D.bounds;
		bounds.Expand(SkinWidth * -2);

		raycastOrigins.bottomLeft = new Vector2(bounds.min.x, bounds.min.y);
		raycastOrigins.bottomRight = new Vector2(bounds.max.x, bounds.min.y);
		raycastOrigins.topLeft = new Vector2(bounds.min.x, bounds.max.y);
		raycastOrigins.topRight = new Vector2(bounds.max.x, bounds.max.y);
	}

	protected void CalculateRaySpacing()
	{
		Bounds bounds = boxCollider2D.bounds;
		bounds.Expand(SkinWidth * -2);

		float boundsWidth = bounds.size.x;
		float boundsHeight = bounds.size.y;

		horizontalRayCount = Mathf.RoundToInt(boundsHeight / RayDistance);
		verticalRayCount = Mathf.RoundToInt(boundsWidth / RayDistance);

		horizontalRaySpacing = bounds.size.y / (horizontalRayCount - 1);
		verticalRaySpacing = bounds.size.x / (verticalRayCount - 1);
	}
}
