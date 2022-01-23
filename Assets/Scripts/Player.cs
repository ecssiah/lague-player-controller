using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class Player : MonoBehaviour
{
	private PlayerController controller;

	private float moveSpeed = 6;

	public float minJumpHeight = 1;
	public float maxJumpHeight = 3.5f;
	public float timeToJumpApex = 0.4f;

	public float wallSlideSpeedMax = 3;
	public float wallStickTime = 0.25f;
	private float timeToWallUnstick;

	private float accelerationTimeAirborn = 0.2f;
	private float accelerationTimeGrounded = 0.1f;

	private float gravity;
	private float minJumpVelocity;
	private float maxJumpVelocity;
	
	private Vector3 velocity;

	public Vector2 wallJumpClimb;
	public Vector2 wallJumpOff;
	public Vector2 wallJumpLeap;

	private float velocityXSmooth;

	private Vector2 directionalInput;

	private bool wallSliding;
	private int wallDirectionX;

	void Start()
	{
		controller = GetComponent<PlayerController>();

		gravity = -(2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2);

		minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);
		maxJumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
	}
	
	void Update()
	{
		CalculateVelocity();
		HandleWallSliding();

		controller.Move(velocity * Time.deltaTime, directionalInput);

		if (controller.collisionInfo.top || controller.collisionInfo.bottom)
		{
			velocity.y = 0;
		}
	}

	public void SetDirectionalInput(Vector2 input)
	{
		directionalInput = input;
	}

	public void OnJumpInputDown()
	{
		if (wallSliding)
		{
			if (wallDirectionX == directionalInput.x)
			{
				velocity.x = -wallDirectionX * wallJumpClimb.x;
				velocity.y = wallJumpClimb.y;
			}
			else if (directionalInput.x == 0)
			{
				velocity.x = -wallDirectionX * wallJumpOff.x;
				velocity.y = wallJumpOff.y;
			}
			else if (wallDirectionX == -directionalInput.x)
			{
				velocity.x = -wallDirectionX * wallJumpLeap.x;
				velocity.y = wallJumpLeap.y;
			}
		}

		if (controller.collisionInfo.bottom)
		{
			velocity.y = maxJumpVelocity;
		}
	}

	public void OnJumpInputUp()
	{
		if (velocity.y > minJumpVelocity)
		{
			velocity.y = minJumpVelocity;
		}
	}

	private void CalculateVelocity()
	{
		float targetVelocityX = directionalInput.x * moveSpeed;

		velocity.x = Mathf.SmoothDamp(
			velocity.x,
			targetVelocityX,
			ref velocityXSmooth,
			(controller.collisionInfo.bottom) ? accelerationTimeGrounded : accelerationTimeAirborn
		);

		velocity.y += gravity * Time.deltaTime;
	}

	private void HandleWallSliding()
	{
		wallDirectionX = controller.collisionInfo.left ? -1 : 1;
		wallSliding = false;

		if ((controller.collisionInfo.left || controller.collisionInfo.right) && !controller.collisionInfo.bottom && velocity.y < 0)
		{
			wallSliding = true;

			if (velocity.y < -wallSlideSpeedMax)
			{
				velocity.y = -wallSlideSpeedMax;
			}

			if (timeToWallUnstick > 0)
			{
				velocityXSmooth = 0;
				velocity.x = 0;

				if (directionalInput.x != wallDirectionX && directionalInput.x != 0)
				{
					timeToWallUnstick -= Time.deltaTime;
				}
				else
				{
					timeToWallUnstick = wallStickTime;
				}
			}
			else
			{
				timeToWallUnstick = wallStickTime;
			}
		}
	}
}
