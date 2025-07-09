using Godot;
using System;
namespace InputReader
{
	public static class InputPublisher
	{
		public static EventHandler<InputEventArgs> OnInput;

		public static void PublishEvent(object sender, InputEventArgs e) { OnInput?.Invoke(sender, e); }
	}

	public class InputEventArgs : EventArgs
	{
		public InputEvent input;
		public InputEventArgs(InputEvent input) { this.input = input; }
	}
}
