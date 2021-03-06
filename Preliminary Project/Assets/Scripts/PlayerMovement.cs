﻿// This script controls the player's movement and physics within the game

using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
	public bool drawDebugRaycasts = true;	//Should the environment checks be visualized

	[Header("Movement Properties")]
	public float speed = 8f;				//Player speed
	public float coyoteDuration = .05f;		//How long the player can jump after falling
	public float maxFallSpeed = -25f;		//Max speed player can fall

	[Header("Jump Properties")]
	public float jumpForce = 6.3f;			//Initial force of jump
	public float jumpHoldDuration = .1f;	//How long the jump key can be held

	[Header("Swing Properties")]
	public float swingForce = 4f;

	[Header("Environment Check Properties")]
	public float footOffset = .4f;			//X Offset of feet raycast
	public float groundDistance = .2f;		//Distance player is considered to be on the ground
	public LayerMask groundLayer;			//Layer of the ground

	[Header ("Status Flags")]
	public bool isOnMovingPlatform;
	public bool isOnGround;					//Is the player on the ground?
	public bool isJumping;					//Is player jumping?

	PlayerInput input;						//The current inputs for the player
	BoxCollider2D bodyCollider;				//The collider component
	Rigidbody2D rigidBody;					//The rigidbody component
	Animator myAnimator;                    //The Animator component
	PlayerShooting shooting;				//The shooting component
	RopeSystem rope;						//The rope component
	SpriteRenderer sprite;					//The player sprite

	float jumpTime;							//Variable to hold jump duration
	float coyoteTime;						//Variable to hold coyote duration

	float originalXScale;					//Original scale on X axis
	public int direction = 1;				//Direction player is facing
	public Vector2 ropeHook;

	int ct=0;

	float lastVelocity = 0;

	private Rigidbody2D movingPlatform;


	void Start ()
	{
		//Get a reference to the required components
		input = GetComponent<PlayerInput>();
		rigidBody = GetComponent<Rigidbody2D>();
		bodyCollider = GetComponent<BoxCollider2D>();
		myAnimator = GetComponent<Animator>();
		shooting = GetComponent<PlayerShooting>();
		rope = GetComponent<RopeSystem>();
		sprite = GetComponent<SpriteRenderer>();

		//Record the original x scale of the player
		originalXScale = transform.localScale.x;
	}

	void FixedUpdate()
	{
		//Check the environment to determine status
		PhysicsCheck();

		//Process ground and air movements
		GroundMovement();		
		MidAirMovement();
		SwingMovement();
	}

	void PhysicsCheck()
	{
		//Start by assuming the player isn't on the ground and the head isn't blocked
		isOnGround = false;
		isOnMovingPlatform = false;

		//Cast rays for the left and right foot
		RaycastHit2D leftCheck = Raycast(new Vector2(-footOffset, 0f), Vector2.down, groundDistance);
		RaycastHit2D rightCheck = Raycast(new Vector2(footOffset, 0f), Vector2.down, groundDistance);

		//If either ray hit the ground, the player is on the ground
		if (leftCheck || rightCheck) {
			isOnGround = true;

			//Check if is in moving platform
			if(leftCheck && (leftCheck.transform.gameObject.GetComponent("MovingPlatform") as MovingPlatform) != null) {
				movingPlatform = leftCheck.rigidbody;
				isOnMovingPlatform = true;
			}
			else if(rightCheck && (rightCheck.transform.gameObject.GetComponent("MovingPlatform") as MovingPlatform) != null) {
				movingPlatform = rightCheck.rigidbody;
				isOnMovingPlatform = true;
			}
		}
	}

	void GroundMovement()
	{
		if(rope.isSwinging){
			myAnimator.SetBool("swinging",true);
			return;
		}

		//Calculate the desired velocity based on inputs
		float xVelocity = speed * input.horizontal;
		//loop for step sound
		if (xVelocity != 0 && isOnGround){
			if(ct == 0){
				SoundManager.PlaySound("step");
				ct++;
			}
			else if(ct == 10){
				ct = 0;
			}
			else{
				ct++;
			}
		}

		//If the sign of the velocity and direction don't match, flip the character
		if (xVelocity * direction < 0f || shooting.shouldFlip)
			FlipCharacterDirection();

		//Apply the desired velocity 
		rigidBody.velocity = new Vector2(xVelocity, rigidBody.velocity.y);
		if(isOnMovingPlatform)
			transform.position += new Vector3(movingPlatform.velocity.x * Time.deltaTime, movingPlatform.velocity.y * Time.deltaTime, 0);
		myAnimator.SetFloat("speed",Mathf.Abs(xVelocity));
		myAnimator.SetBool("grounded",isOnGround);
		myAnimator.SetFloat("yvelocity",0);
		
		//If the player is on the ground, extend the coyote time window
		if (isOnGround)
			coyoteTime = Time.time + coyoteDuration;
	}

	void MidAirMovement()
	{
		//If the jump key is pressed AND the player isn't already jumping AND EITHER
		//the player is on the ground or within the coyote time window...
		if (input.jumpPressed && !isJumping && (isOnGround || coyoteTime > Time.time))
		{
			//...The player is no longer on the ground and is jumping...
			isOnGround = false;
			isJumping = true;
			myAnimator.SetBool("grounded",isOnGround);

			//...record the time the player will stop being able to boost their jump...
			jumpTime = Time.time + jumpHoldDuration;

			//...add the jump force to the rigidbody...
			rigidBody.AddForce(new Vector2(0f, jumpForce), ForceMode2D.Impulse);
			myAnimator.SetFloat("yvelocity",rigidBody.velocity.y);

			SoundManager.PlaySound("jump");
		}
		//Otherwise, if currently within the jump time window...
		else if (isJumping)
		{
			//...and if jump time is past, set isJumping to false
			if (jumpTime <= Time.time){
				isJumping = false;
			}
		}

		//landing noise
		if(lastVelocity < -0.1 && rigidBody.velocity.y == 0){
			SoundManager.PlaySound("land");
			lastVelocity = 0;
		}else{
			lastVelocity = rigidBody.velocity.y;
		}

		//If player is falling to fast, reduce the Y velocity to the max
		if (rigidBody.velocity.y < maxFallSpeed)
			rigidBody.velocity = new Vector2(rigidBody.velocity.x, maxFallSpeed);
			myAnimator.SetFloat("yvelocity",rigidBody.velocity.y);	
	}

	void SwingMovement()
	{
		if(!rope.isSwinging){
			myAnimator.SetBool("swinging",false);
			return;
		}

		if (input.horizontal != 0)
		{
			//If the sign of the velocity and direction don't match, flip the character
			if (input.horizontal * direction < 0f || shooting.shouldFlip)
				FlipCharacterDirection();

			// 1 - Get a normalized direction vector from the player to the hook point
			Vector2 playerToHookDirection = (ropeHook - (Vector2)transform.position).normalized;

			// 2 - Inverse the direction to get a perpendicular direction
			Vector2 perpendicularDirection;
			if (input.horizontal < 0)
			{
				perpendicularDirection = new Vector2(-playerToHookDirection.y, playerToHookDirection.x);
				Vector2 leftPerpPos = (Vector2)transform.position - perpendicularDirection * -2f;
				//Debug.DrawLine(transform.position, leftPerpPos, Color.green, 0f);
			}
			else
			{
				perpendicularDirection = new Vector2(playerToHookDirection.y, -playerToHookDirection.x);
				Vector2 rightPerpPos = (Vector2)transform.position + perpendicularDirection * 2f;
				//Debug.DrawLine(transform.position, rightPerpPos, Color.green, 0f);
			}

			Vector2 force = perpendicularDirection * swingForce;
			rigidBody.AddForce(force, ForceMode2D.Force);
		}
	}

	void FlipCharacterDirection()
	{
		//Turn the character by flipping the direction
		direction *= -1;

		//Flip the sprite
		sprite.flipX = !sprite.flipX;
	}

	
	//These two Raycast methods wrap the Physics2D.Raycast() and provide some extra
	//functionality
	RaycastHit2D Raycast(Vector2 offset, Vector2 rayDirection, float length)
	{
		//Call the overloaded Raycast() method using the ground layermask and return 
		//the results
		return Raycast(offset, rayDirection, length, groundLayer);
	}

	RaycastHit2D Raycast(Vector2 offset, Vector2 rayDirection, float length, LayerMask mask)
	{
		//Record the player's position
		Vector2 pos = transform.position;

		//Send out the desired raycasr and record the result
		RaycastHit2D hit = Physics2D.Raycast(pos + offset, rayDirection, length, mask);

		//If we want to show debug raycasts in the scene...
		if (drawDebugRaycasts)
		{
			//...determine the color based on if the raycast hit...
			Color color = hit ? Color.red : Color.green;
			//...and draw the ray in the scene view
			Debug.DrawRay(pos + offset, rayDirection * length, color);
		}

		//Return the results of the raycast
		return hit;
	}
}
