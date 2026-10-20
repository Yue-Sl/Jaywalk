using Godot;
using System;

public partial class Car : Area2D
{
	[Export] public Texture2D[] cars;

	Vector2 dir = Vector2.Left;
	int speed = 2;

	public override void _Ready()
	{
		dir = Position.X > 0 ? Vector2.Left : Vector2.Right;
		int i = (int)(GD.Randi() % cars.Length);
        GetNode<Sprite2D>("Sprite2D").Texture = cars[i];
		speed = Global.Instance.state.carSpeed;
	}

    public override void _PhysicsProcess(double delta)
	{
		Position += speed * dir;
		if (Position.X <= -200 || Position.X >= 200)
		{
			QueueFree();
		}
	}
}
