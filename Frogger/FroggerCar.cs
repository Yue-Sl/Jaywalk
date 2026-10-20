using Godot;
using System;

public partial class FroggerCar : Area2D
{
    private static readonly string[] Textures = {
        "res://课程素材/graphics/cars/green.png",
        "res://课程素材/graphics/cars/red.png",
        "res://课程素材/graphics/cars/yellow.png"
    };
    private float _speed;
    private int _dirX = 1;       // +1 right, -1 left
    private Sprite2D _sprite;

    public void Setup(float speed, int dirX, int level)
    {
        _speed = speed;
        _dirX = dirX;
        // difficulty scales speed per level
        _speed *= 1.0f + 0.18f * (level - 1);
        if (_speed > 150f) _speed = 150f;
    }

    public override void _Ready()
    {
        _sprite = GetNode<Sprite2D>("Sprite2D");
        var rng = new Random();
        _sprite.Texture = GD.Load<Texture2D>(Textures[rng.Next(Textures.Length)]);
        if (_dirX < 0)
        {
            _sprite.FlipH = true;
            // face left: sprite points right by default
        }
        CollisionLayer = 2;   // layer 2
        CollisionMask = 1;    // detect player (layer 1)
    }

    public override void _Process(double delta)
    {
        Position += new Vector2(_dirX * _speed * (float)delta, 0);
        // wrap around so traffic never stops
        if (_dirX > 0 && Position.X > 424) Position = new Vector2(-24, Position.Y);
        if (_dirX < 0 && Position.X < -24) Position = new Vector2(424, Position.Y);
    }
}
