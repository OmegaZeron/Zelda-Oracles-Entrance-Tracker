using Godot;

public partial class Camera : Camera2D
{
	public static Camera Instance { get; private set; }
	private Vector2 viewport;
	
	private Vector2 mouseStartPos;
	private Vector2 screenStartPos;
	private bool dragging;
	
	private float targetZoom = 1;
	private Vector2 targetMousePos;
	[Export] private float minZoom = .655f;
	[Export] private float maxZoom = 3;
	private float zoomIncrement = .1f;
	[Export] private float cameraPanSpeed = 10;

	[Export] private Vector2I holodrumLimit;
	[Export] private Vector2I subrosiaLimit;
	[Export] private Vector2I labrynnaLimit;

	public override void _Ready()
	{
		Instance = this;

		viewport = GetViewportRect().Size;
		ClampCameraToBounds();
		GetTree().Root.SizeChanged += Resize;
	}
	
	public override void _ExitTree()
	{
		GetTree().Root.SizeChanged -= Resize;
	}
	
	public void Resize()
	{
		viewport = DisplayServer.WindowGetSize();
		ClampCameraToBounds();
	}
	
	public override void _PhysicsProcess(double delta)
	{
		Zoom = targetZoom * Vector2.One;
		if (targetMousePos != Vector2.Zero)
		{
			Position += targetMousePos - GetGlobalMousePosition();
			targetMousePos = Vector2.Zero;
		}
		
		if (Input.IsActionPressed("Up"))
		{
			Position += Vector2.Up * cameraPanSpeed / Zoom;
		}
		if (Input.IsActionPressed("Left"))
		{
			Position += Vector2.Left * cameraPanSpeed / Zoom;
		}
		if (Input.IsActionPressed("Down"))
		{
			Position += Vector2.Down * cameraPanSpeed / Zoom;
		}
		if (Input.IsActionPressed("Right"))
		{
			Position += Vector2.Right * cameraPanSpeed / Zoom;
		}
		ClampCameraToBounds();
	}
	
	public override void _Input(InputEvent ev)
	{
		if (ev is InputEventMouseMotion motion && dragging)
		{
			Position = Vector2.One / Zoom * (mouseStartPos - motion.Position) + screenStartPos;
			ClampCameraToBounds();
		}
		else if (ev is InputEventMouseButton button)
		{
			if (button.IsPressed())
			{
				if (button.ButtonIndex == MouseButton.WheelUp)
				{
					ZoomIn();
				}
				else if (button.ButtonIndex == MouseButton.WheelDown)
				{
					ZoomOut();
				}
				else if (button.ButtonIndex == MouseButton.Middle)
				{
					mouseStartPos = button.Position;
					screenStartPos = Position;
					dragging = true;
				}
			}
			else
			{
				dragging = false;
			}
		}
	}
	
	private void ClampCameraToBounds()
	{
		Vector2 bounds = Vector2.One / Zoom * viewport / 2;
		Position = new Vector2
		(
			Mathf.Clamp(Position.X, Mathf.Min(LimitLeft + bounds.X, LimitRight - bounds.X), Mathf.Max(LimitLeft + bounds.X, LimitRight - bounds.X)),
			Mathf.Clamp(Position.Y, Mathf.Min(LimitTop + bounds.Y, LimitBottom - bounds.Y), Mathf.Max(LimitTop + bounds.Y, LimitBottom - bounds.Y))
		);
	}
	private void ZoomIn()
	{
		targetZoom = Mathf.Min(targetZoom + zoomIncrement, maxZoom);
		targetMousePos = GetGlobalMousePosition();
	}
	private void ZoomOut()
	{
		targetZoom = Mathf.Max(targetZoom - zoomIncrement, minZoom);
		targetMousePos = GetGlobalMousePosition();
	}
	
	// move camera to the given position
	// used for viewing an entrance's pair
	public void MoveToLocation(Vector2 loc)
	{
		Position = loc;
	}
	
	public void ChangeBounds(bool altMap)
	{
		if (GameSelector.Instance.currentGame == GameSelector.Game.Seasons)
		{
			LimitRight = altMap ? subrosiaLimit.X : holodrumLimit.X;
			LimitBottom = altMap ? subrosiaLimit.Y : holodrumLimit.Y;
		}
		else if (GameSelector.Instance.currentGame == GameSelector.Game.Ages)
		{
			LimitRight = labrynnaLimit.X;
			LimitBottom = labrynnaLimit.Y;
		}
	}
}
