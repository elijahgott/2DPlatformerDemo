using Godot;
using System;

public partial class Player : CharacterBody2D
{
	[Export]
	public int Speed {get; set;} = 600;
	
	[Export]
	public int JumpForce {get; set;} = 800;
	
	[Export]
	public int Gravity {get; set;} = 30;
	
	//player sprite
	private AnimatedSprite2D _playerSprite;
	
	//vector user for player velocity
	private Vector2 velocity = Vector2.Zero;
	
	//loads player sprite upon opening game
	public override void _Ready(){
		_playerSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
	}
	
	public void GetMovement(){
		//get horizontal input
		float horizontalDirection = Input.GetAxis("move_left", "move_right");
		velocity.X = horizontalDirection * Speed;
		
		// MIGHT MOVE ANIMATION STUFF TO OWN FUNCTION
		// animate sprite based on velocity
		// no horizontal velocity -> idle
		// negative h velocity -> flip_h = true, run
		// positive h velocity -> flip_h = false, run
		if(velocity.X > 0){
			_playerSprite.FlipH = false;
			_playerSprite.Play("run");
		}
		else if(velocity.X < 0){
			_playerSprite.FlipH = true;
			_playerSprite.Play("run");
		}
		else{ // horizontal velocity == 0
			_playerSprite.Play("idle");
		}
		
		//apply gravity to player when not touching ground
		if(!IsOnFloor()){
			_playerSprite.Play("jump");
			velocity.Y += Gravity;
			
			//cap falling velocity at 1000
			if(velocity.Y > 1000){
				velocity.Y = 1000;
			}
		}
		else if(Input.IsActionJustPressed("jump")){
			velocity.Y = -JumpForce;
		}
		
		Velocity = velocity;
	}
	
	
	
	public override void _PhysicsProcess(double delta){
		GetMovement();
		MoveAndSlide();
	}
}
