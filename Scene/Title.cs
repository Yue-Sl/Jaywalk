using Godot;
using System;

public partial class Title : Control
{
	Label label;

	Button startBtn;

	public override void _Ready()
	{
		label = GetNode<Label>("score");
		label.Text = $"Best score: {Global.Instance.score}";
		startBtn = GetNode<Button>("Button1");

		// 检测到手柄连接，获得焦点
        if (Input.GetConnectedJoypads().Count > 0)
            startBtn.GrabFocus();

        // 监听手柄热插拔
        Input.JoyConnectionChanged += OnJoyConnectionChanged;
    }

    private void OnJoyConnectionChanged(long device, bool connected)
    {
        if (connected)
        {
            if (GetViewport().GuiGetFocusOwner() == null)
                startBtn.GrabFocus();
        }
        else
        {
            startBtn.ReleaseFocus();
        }
    }

    public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("confirm"))
		{
			GetTree().ChangeSceneToFile("res://Scene/game.tscn");
		}
	}

	public void On_button1_pressed()
	{
		Global.Instance.SetDifficulty(1);
	}

	public void On_button_2_pressed()
	{
        Global.Instance.SetDifficulty(2);
    }

	public void On_button_3_pressed()
	{
        Global.Instance.SetDifficulty(3);
    }

}
