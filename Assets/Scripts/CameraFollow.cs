using UnityEngine;

public class CameraFollow : MonoBehaviour
{
	private struct FocusArea
	{
		public Vector2 center;
		public Vector2 velocity;

		float top, bottom;
		float left, right;

		public FocusArea(Bounds targetBounds, Vector2 size)
		{
			left = targetBounds.center.x - size.x / 2;
			right = targetBounds.center.x + size.x / 2;
			bottom = targetBounds.min.y;
			top = targetBounds.min.y + size.y;

			velocity = Vector2.zero;

			center = new Vector2((left + right) / 2, (top + bottom) / 2);
		}

		public void Update(Bounds targetBounds)
		{
			float shiftX = 0;

			if (targetBounds.min.x < left)
			{
				shiftX = targetBounds.min.x - left;
			}
			else if (targetBounds.max.x > right)
			{
				shiftX = targetBounds.max.x - right;
			}

			left += shiftX;
			right += shiftX;

			float shiftY = 0;

			if (targetBounds.min.y < bottom)
			{
				shiftY = targetBounds.min.y - bottom;
			}
			else if (targetBounds.max.y > top)
			{
				shiftY = targetBounds.max.y - top;
			}

			top += shiftY;
			bottom += shiftY;

			center = new Vector2((left + right) / 2, (top + bottom) / 2);

			velocity = new Vector2(shiftX, shiftY);
		}
	}

	public PlayerController target;

	public Vector2 focusAreaSize;

	private FocusArea focusArea;

	public float verticalOffset;

	public float lookAheadDistanceX;
	public float lookSmoothTimeX;
	public float verticalSmoothTime;

	private float currentLookAheadX;
	private float targetLookAheadX;
	private float lookAheadDirectionX;
	private float smoothLookVelocityX;
	private float smoothVelocityY;

	private bool lookAheadStopped;

	void Start()
	{
		focusArea = new FocusArea(target.boxCollider2D.bounds, focusAreaSize);
	}

	void LateUpdate()
	{
		focusArea.Update(target.boxCollider2D.bounds);

		Vector2 focusPosition = focusArea.center + Vector2.up * verticalOffset;

		if (focusArea.velocity.x != 0)
		{
			lookAheadDirectionX = Mathf.Sign(focusArea.velocity.x);

			if (Mathf.Sign(target.playerInput.x) == Mathf.Sign(focusArea.velocity.x) && target.playerInput.x != 0)
			{
				lookAheadStopped = false;
				targetLookAheadX = lookAheadDirectionX * lookAheadDistanceX;
			}
			else
			{
				if (!lookAheadStopped)
				{
					lookAheadStopped = true;
					targetLookAheadX = currentLookAheadX + (lookAheadDirectionX * lookAheadDirectionX - currentLookAheadX) / 4f;
				}
			}
		}

		currentLookAheadX = Mathf.SmoothDamp(currentLookAheadX, targetLookAheadX, ref smoothLookVelocityX, lookSmoothTimeX);

		focusPosition.y = Mathf.SmoothDamp(transform.position.y, focusPosition.y, ref smoothVelocityY, verticalSmoothTime);
		focusPosition += Vector2.right * currentLookAheadX;

		transform.position = (Vector3)focusPosition + Vector3.forward * -10;
	}

	void OnDrawGizmos()
	{
		Gizmos.color = new Color(1, 0, 1, 0.3f);

		Gizmos.DrawCube(focusArea.center, focusAreaSize);
	}
}
