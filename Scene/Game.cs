using Godot;
using System;
using System.Collections.Generic;

public partial class Game : Node2D
{
	[Export] public Node2D root;
	[Export] public Node2D carStartPosition;
	[Export] public Label label;
	Timer timer;

    int score = 0;


    private PackedScene carPrefab = GD.Load<PackedScene>("res://Scene/Car.tscn");


	public override void _Ready()
	{
		GD.Randomize();
		timer = GetNode<Timer>("Timer/CarTimer");
		timer.WaitTime = Global.Instance.state.carTimer;
	}

	public override void _Process(double delta)
	{
	}

    private void On_car_timer_timeout()
	{
        Area2D car = carPrefab.Instantiate<Area2D>();
        Node2D marker = carStartPosition.GetChildren().PickRandom() as Node2D;
        car.Position = marker.Position;
		car.BodyEntered += GotoTitle;
        root.AddChild(car);
    }

    public void On_game_timer_timeout()
    {
        score += 1;
        label.Text = $"score:{score}";
    }

    private void On_area_2d_body_entered(Node2D _body)
	{
		CallDeferred("ChangeScene");
		Global.Instance.ResetScore(score);
	}

	private void GotoTitle(Node2D _body)
	{
        CallDeferred("ChangeScene");
    }

	private void ChangeScene()
	{
        GetTree().ChangeSceneToFile("res://Scene/Title.tscn");
    }


}
