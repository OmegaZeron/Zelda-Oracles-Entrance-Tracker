using Godot;
using System;
using Godot.Collections;

public partial class GameSelector : Control
{
	public static GameSelector Instance { get; private set; }

	public enum Game
	{
		None,
		Seasons,
		Ages
	}
	public Game currentGame { get; private set; }
	public bool DecoupledMode { get; private set; }

	private Node loadedScene;
	private PackedScene seasonsScene;
	private PackedScene agesScene;
	
	[Export] public Texture2D unlinkedTexture { get; private set; }
	[Export] public Texture2D linkedTexture { get; private set; }
	[Export] public Texture2D decoupledTexture { get; private set; }
	[Export] public Texture2D trashTexture { get; private set; }

	[Export] private Array<CanvasItem> UI;
	
	public override void _Ready()
	{
		Instance = this;
		
		GetTree().QuitOnGoBack = false;
		// preload scenes
		seasonsScene = GD.Load<PackedScene>("res://Scenes/Seasons.tscn");
		// agesScene = GD.Load<PackedScene>("res://Scenes/Ages.tscn");
	}
	
	private void DecoupleToggle(bool on)
	{
		DecoupledMode = on;
	}
	
	private void _SelectGame(string gameName)
	{
		currentGame = Enum.Parse<Game>(gameName);
		PackedScene scene = currentGame == Game.Seasons ? seasonsScene : agesScene;
		loadedScene = scene.Instantiate();
		AddChild(loadedScene);
		
		foreach (CanvasItem node in UI)
		{
			node.Visible = false;
		}
	}

	public void SelectorScene()
	{
		foreach (CanvasItem node in UI)
		{
			node.Visible = true;
		}
		RemoveChild(loadedScene);
		loadedScene.QueueFree();
		loadedScene = null;
	}

	private void _QuitPressed()
	{
		GetTree().Quit();
	}

	public override void _Input(InputEvent ev)
	{
		if (ev is not InputEventKey key || !key.IsPressed() || key.Keycode != Key.Escape) {return;}
		if (GetTree().CurrentScene.Name != "Main")
		{
			SelectorScene();
		}
	}
	
	public override void _Notification(int what)
	{
		if (what == NotificationWMGoBackRequest)
		{
			if (GetTree().CurrentScene.Name == "Main")
			{
				GetTree().Quit();
			}
			else
			{
				SelectorScene();
			}
		}
	}
}
