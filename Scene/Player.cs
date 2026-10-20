using Godot;
using System;
using System.Threading;

public partial class Player : CharacterBody2D
{
    [Export]public AnimatedSprite2D sprite;

    Vector2 dir = Vector2.Zero;

	int speed = 50;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		speed = Global.Instance.state.playerSpeed;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		// 移动代码块
		
        dir = Input.GetVector("left", "right", "up", "down");
        //if (dir != Vector2.Zero) GD.Print(dir);
        Velocity = dir * speed;

        // 确认代码块

        if (Input.IsActionJustPressed("confirm"))
		{
			GD.Print("确认");
		}

		Animation();
        MoveAndSlide();
    }

	void Animation()
	{
		// 没输入时停在第0帧
		if (dir == Vector2.Zero)
		{
			sprite.Frame = 0;
			return;
        }

        // 垂直优先
        if (Mathf.Abs(dir.Y) < Mathf.Abs(dir.X))
        {
            sprite.Animation = dir.X < 0 ? "left" : "right";
        }
        else
        {
            sprite.Animation = dir.Y < 0 ? "up" : "down";
        }
    }
}

