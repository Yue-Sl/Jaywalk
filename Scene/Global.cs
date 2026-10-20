using Godot;
using System;

public partial class Global : Node
{
    public static Global Instance { get; private set; }
    public int score { get; private set; }

    public struct State
    {
        public int playerSpeed;
        public int carSpeed;
        public float carTimer;

    }

    public State state { get; private set; }

    public override void _Ready()
    {
        Instance = this;
        SetDifficulty(1);
    }

    public void ResetScore(int _score)
    {
        if(score > 100 || score == 0) score = 100;
        score = Mathf.Min(score, _score);
    }

    public void SetDifficulty(int idx)
    {
        switch (idx)
        {
            case 1:
                state = new State
                {
                    playerSpeed = 50,
                    carSpeed = 1,
                    carTimer = 0.5f,
                };
                break;
            case 2:
                state = new State
                {
                    playerSpeed = 30,
                    carSpeed = 2,
                    carTimer = 0.3f,
                };
                break;
            case 3:
                state = new State
                {
                    playerSpeed = 20,
                    carSpeed = 3,
                    carTimer = 0.2f,
                };
                break;
            default: break;
        }
        score = 0;
    }
}
