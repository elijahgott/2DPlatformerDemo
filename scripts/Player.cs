using Godot;
using System;
using System.Runtime.CompilerServices;
using System.Transactions;

public partial class Player : CharacterBody2D
{
	[Export]
	public int Speed {get; set;} = 500;
	
	[Export]
	public int JumpForce {get; set;} = 750;
	
	[Export]
	public int Gravity {get; set;} = 30;

	[Export]
	public int WallJumpForce {get; set;} = 800;

	// slow down player when trying to change direction while in air 
	[Export]
	public int AirHorizontalSpeed { get; set; } = 0;

	[Export]
	public int Friction {get; set;} = 50;
	
	//player sprite
	private AnimatedSprite2D _playerSprite;
	
	//vector used for player _velocity
	private Vector2 _velocity = Vector2.Zero;

	//timer for variable jump height
	private Timer _jumpHeightTimer; 

	//timer for coyote time jump
	private Timer _coyoteTimer; 

	//direction player is currently facing
	private Vector2 _facingDirection = Vector2.Right;

	//allows / prevents player from moving horizontally
	private bool _canMoveHorizontal = true;

	//boolean to determine if player can double jump or not
	private bool _canDoubleJump = true;

	//boolean to determine if player can coyote jump
	private bool _canCoyoteJump = false;
	
	//loads player sprite upon opening game
	public override void _Ready(){
		_playerSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_jumpHeightTimer = GetNode<Timer>("JumpHeightTimer");
		_coyoteTimer = GetNode<Timer>("CoyoteTimer");
	}
	
	public void GetMovement(){
		float horizontalDirection = 0;

		if (_canMoveHorizontal)
		{
			//get horizontal input
			horizontalDirection = Input.GetAxis("move_left", "move_right");
		}
		
		if(horizontalDirection == 0){ //no horizontal input applied
			//apply friction to gradually slow down player
			if(Velocity.X != 0){ //dont apply friction while not touching floor
				if(_facingDirection == Vector2.Left){
					_velocity.X += Friction;
					if(Velocity.X >= 0){
						_velocity.X = 0;
					}
				}
				else if(_facingDirection == Vector2.Right){
					_velocity.X -= Friction;
					if(Velocity.X <= 0){
						_velocity.X = 0;
					}
				}
			}

			if((Velocity.X == 0) && (Velocity.Y == 0)){
				_playerSprite.Play("idle");
			}
		}
		else{ //horizontal input being applied
			_velocity.X = (horizontalDirection * Speed);
			
			// MIGHT MOVE ANIMATION STUFF TO OWN FUNCTION
			// animate sprite based on _velocity and direction facing
			if(Velocity.X > 0){
				_facingDirection = Vector2.Right;
				_playerSprite.FlipH = false;
				_playerSprite.Play("run");
			}
			else if(Velocity.X < 0){
				_facingDirection = Vector2.Left;
				_playerSprite.FlipH = true;
				_playerSprite.Play("run");
			}
		}
		
		//apply gravity to player when not touching ground
		if(!IsOnFloor()){
			if(Velocity.Y < 0){ //when player is rising
				_playerSprite.Play("jump");
			}
			else if(Velocity.Y >= 0 && Velocity.Y < 1000){ //player is falling
				if(!(_playerSprite.Animation == "fall_SLOW")){
					_playerSprite.Play("fall_SLOW");
				}
			}
			else if(Velocity.Y >= 1000){ //cap falling _velocity at 1000
				_playerSprite.Play("fall_FAST");
				_velocity.Y = 1000;
			}

			//coyote jump
			if(Input.IsActionJustPressed("jump") && _canCoyoteJump){
				_jumpHeightTimer.Start();
				_velocity.Y = -JumpForce;
			}

			//double jump
			if(Input.IsActionJustPressed("jump") && _canDoubleJump && 
			_velocity.Y < 300){ //prevents player from being able to double jump after falling a certain amount
				_velocity.Y = -(JumpForce * 0.9f);
				_canDoubleJump = false;
			}

			_velocity.Y += Gravity;
		}
		else{ //while touching the ground
			_canMoveHorizontal = true;
			_canDoubleJump = true; //enable player to double jump after landing on ground
			_velocity.Y = 0; //resets vertical velocity when touching ground

			if (Input.IsActionJustPressed("jump"))
			{
				_jumpHeightTimer.Start();
				_velocity.Y = -JumpForce;
			}
		}
		
		Velocity = _velocity;
	}

	public void GetCollision(){
		for(int i = 0; i < GetSlideCollisionCount(); i++){
			//get one of the collisions with player
			KinematicCollision2D collision = GetSlideCollision(i);
			Vector2 normal = collision.GetNormal();

			//collision from left
			if(normal.X > 0){
				//stop momentum if running into wall
				if(IsOnFloor()){
					_velocity.X = 0;
				}

				//allows double jumping while touching a wall, but not trying to wall jump
				if (Input.IsActionJustPressed("move_left"))
				{
					_canDoubleJump = false;
				}
				
				//wall jump to right side
				if(Input.IsActionJustPressed("jump") && IsOnWallOnly()){
					// _canMoveHorizontal = false;
					_canDoubleJump = false;
					_velocity.Y = -JumpForce;
					_velocity.X = WallJumpForce;
					_facingDirection = Vector2.Right;
					_playerSprite.FlipH = false;
				}
			}
			//collision from right
			else if(normal.X < 0){
				//stop momentum if running into wall
				if(IsOnFloor()){
					_velocity.X = 0;
				}

				//allows double jumping while touching a wall, but not trying to wall jump
				if (Input.IsActionJustPressed("move_right"))
				{
					_canDoubleJump = false;
				}
				
				//wall jump to left side
				if (Input.IsActionJustPressed("jump") && IsOnWallOnly())
				{
					// _canMoveHorizontal = false;
					_canDoubleJump = false;
					_velocity.Y = -JumpForce;
					_velocity.X = -WallJumpForce;
					_facingDirection = Vector2.Left;
					_playerSprite.FlipH = true;
				}
			}
			//collision from top
			else if(normal.Y > 0){
				//fix player maintaining upward momentum for too long after hitting head
				_velocity.Y = 0;
				_velocity.Y += Gravity;
			}
			//collision from bottom
			else if(normal.Y < 0){
				break;
			}
		}
		Velocity = _velocity;
	}

	public void OnJumpHeightTimerTimeout(){
		if(!Input.IsActionPressed("jump")){
			if(_velocity.Y < -200){
				_velocity.Y = -200;
			}
		}

		Velocity = _velocity;
	}

	public void OnCoyoteTimerTimeout(){
		_canCoyoteJump = false;
	}
	
	public override void _PhysicsProcess(double delta){
		GetMovement();
		GetCollision();

		//for coyote time
		bool wasOnFloor = IsOnFloor();
		MoveAndSlide();
		//detect when player is falling off edge
		if(wasOnFloor && !IsOnFloor() && Velocity.Y >= 0){
			_canCoyoteJump = true;
			_coyoteTimer.Start();
		}
	}
}
