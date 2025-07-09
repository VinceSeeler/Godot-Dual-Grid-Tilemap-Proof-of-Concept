using Godot;
using InputReader;
using System;
[GlobalClass]
public partial class SpectatorCamera : ZoomCamera
{
    [Export] RigidBody2D RB;
    [Export] float speed = 64f;
    Vector2 direction = Vector2.Zero;

    public override void _Input(InputEvent input)
    {
        direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        base._Input(input);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        if (RB != null)
        {
            RB.Position += direction * (float)delta * (int)(32 * speed / ZoomLevels[currentZoom]);
        }
        else
        {
            Position += direction * (float)delta * (int)(32 * speed / ZoomLevels[currentZoom]);
        }
    }

    public override void OnInputRead(object sender, InputEventArgs e)
    {
        base.OnInputRead(sender, e);
    }
}
