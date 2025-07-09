using Godot;
using Godot.Collections;
using InputReader;
using System;
using System.Collections.Generic;
[GlobalClass]
public partial class ZoomCamera : Camera2D
{
	protected int currentZoom = 6;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		InputPublisher.OnInput += OnInputRead;
	}

    public override void _Input(InputEvent input)
    {
		InputPublisher.PublishEvent(this,new InputEventArgs(input));
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public virtual void OnInputRead(object sender, InputEventArgs e)
	{
		InputEvent input = e.input;
		if (input.IsActionPressed("camera_zoom_in")) { SetZoom(currentZoom+1); }
		if (input.IsActionPressed("camera_zoom_out")) { SetZoom(currentZoom-1); }	
	}

	private void SetZoom(int zoom)
	{
		if (zoom < 0) { zoom = 0; }
		if (zoom > ZoomLevels.Count-1) { zoom = ZoomLevels.Count - 1; }
		float ZL = ZoomLevels[zoom];
		Zoom = new Vector2(ZL, ZL);
		currentZoom = zoom;
	}

	public static List<float> ZoomLevels = new List<float>()
	{
		0.1f,
		0.25f,
		0.5f,
		0.75f,
		1.0f,
		1.25f,
		1.5f,
		1.75f,
		2.0f,
		2.5f,
		3f,
		3.5f,
		4f,
		4.5f,
		5f,
		6f,
		7f,
		8f
	};
}
