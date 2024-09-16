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

	public enum ActiveScene
	{
		Main,
		Settings,
		GameMap
	}
	public ActiveScene activeScene { get; private set; } = ActiveScene.Main;
	public bool DecoupledMode { get; private set; }

	private Node loadedScene;
	private PackedScene seasonsScene;
	private PackedScene agesScene;
	
	// regular textures
	[Export] public Texture2D unlinkedTexture { get; private set; } // red
	[Export] public Texture2D linkedTexture { get; private set; } // green
	[Export] public Texture2D trashTexture { get; private set; } // gray
	// decoupled mode textures
	[Export] public Texture2D linkedNoDecoupledTexture { get; private set; } // green/red
	[Export] public Texture2D decoupledTexture { get; private set; } // red/yellow
	[Export] public Texture2D linkedDecoupledTexture { get; private set; } // green/yellow
	[Export] public Texture2D unlinkedTrashTexture { get; private set; } // red/gray
	[Export] public Texture2D linkedTrashTexture { get; private set; } // green/gray
	[Export] public Texture2D trashNoDecoupledTexture { get; private set; } // gray/red
	[Export] public Texture2D trashDecoupledTexture { get; private set; } // gray/yellow

	[Export] private CanvasLayer UI;
	
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

	public void OpenSettings()
	{
		SettingsManager.Instance.ShowSettingsMenu();
		activeScene = ActiveScene.Settings;
	}

	public void CloseSettings()
	{
		activeScene = currentGame == Game.None ? ActiveScene.Main : ActiveScene.GameMap;
	}
	
	private void _SelectGame(string gameName)
	{
		currentGame = Enum.Parse<Game>(gameName);
		PackedScene scene = currentGame switch
		{
			Game.Seasons => seasonsScene,
			Game.Ages => agesScene,
			_ => null
		};
		if (scene == null)
		{
			// TODO error probably
			return;
		}
		loadedScene = scene.Instantiate();
		AddChild(loadedScene);

		UI.Visible = false;

		activeScene = ActiveScene.GameMap;
	}

	public void SelectorScene()
	{
		UI.Visible = true;
		RemoveChild(loadedScene);
		loadedScene.QueueFree();
		loadedScene = null;
		currentGame = Game.None;
		activeScene = ActiveScene.Main;
	}

	private void _QuitPressed()
	{
		SettingsManager.Instance.SaveSettings();
		GetTree().Quit();
	}

	public override void _Input(InputEvent ev)
	{
		if (ev is not InputEventKey key || !key.IsPressed() || key.Keycode != Key.Escape)
		{
			return;
		}
		if (GetTree().CurrentScene.Name != "Main")
		{
			SelectorScene();
		}
	}
	
	public override void _Notification(int what)
	{
		if (what == NotificationWMCloseRequest)
		{
			SettingsManager.Instance.SaveSettings();
			GetTree().Quit();
		}
	}
}
