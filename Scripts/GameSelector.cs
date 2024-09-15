using Godot;
using System;
using Godot.Collections;

public partial class GameSelector : Control
{
	public class AppSettings
	{
		[Export] public int pulseCount = 4;
		[Export] public float pulseDuration = 1;
		[Export] public Vector2I windowPos;
		[Export] public Vector2I windowSize;
	}
	public AppSettings appSettings { get; private set; }

	private const string SETTINGS_PATH = "res://settings.json";
	
	public static GameSelector Instance { get; private set; }

	private const string APP_VERSION = "v0.1.0";

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

	[Export] private Array<CanvasItem> UI;
	
	public override void _Ready()
	{
		Instance = this;

		GetWindow().Title = $"Oracles Entrance Tracker {APP_VERSION}";
		
		LoadSettings();
		
		GetTree().QuitOnGoBack = false;
		// preload scenes
		seasonsScene = GD.Load<PackedScene>("res://Scenes/Seasons.tscn");
		// agesScene = GD.Load<PackedScene>("res://Scenes/Ages.tscn");
	}
	
	private void LoadSettings()
	{
		using FileAccess file = FileAccess.Open(SETTINGS_PATH, FileAccess.ModeFlags.Read);
		if (!FileAccess.FileExists(SETTINGS_PATH))
		{
			appSettings = new AppSettings
			{
				windowSize = GetWindow().Size,
				windowPos = GetWindow().Position
			};
			using FileAccess newFile = FileAccess.Open(SETTINGS_PATH, FileAccess.ModeFlags.Write);
			Dictionary<string, Variant> settingsDict = new()
			{
				{"pulseCount", appSettings.pulseCount},
				{"pulseDuration", appSettings.pulseDuration},
				{"windowSizeX", appSettings.windowSize.X},
				{"windowSizeY", appSettings.windowSize.Y},
				{"windowPosX", appSettings.windowPos.X},
				{"windowPosY", appSettings.windowPos.Y}
			};
			string newSettings = Json.Stringify(settingsDict);
			newFile.StoreString(newSettings);
			return;
		}

		if (file.EofReached())
		{
			return;
		}

		string data = file.GetAsText();
		if (string.IsNullOrEmpty(data))
		{
			return;
		}
		Dictionary<string, Variant> thing = Json.ParseString(data).AsGodotDictionary<string, Variant>();
		appSettings = new AppSettings
		{
			pulseCount = thing["pulseCount"].AsInt32(),
			pulseDuration = (float)thing["pulseDuration"].AsDouble(),
			windowPos = new Vector2I(thing["windowPosX"].AsInt32(), thing["windowPosY"].AsInt32()),
			windowSize = new Vector2I(thing["windowSizeX"].AsInt32(), thing["windowSizeY"].AsInt32())
		};
		GetWindow().Size = appSettings.windowSize;
		GetWindow().Position = appSettings.windowPos;
	}
	
	public void SaveSettings()
	{
		using FileAccess file = FileAccess.Open(SETTINGS_PATH, FileAccess.ModeFlags.Write);
		Dictionary<string, Variant> settingsDict = new()
		{
			{"pulseCount", appSettings.pulseCount},
			{"pulseDuration", appSettings.pulseDuration},
			{"windowSizeX", GetWindow().Size.X},
			{"windowSizeY", GetWindow().Size.Y},
			{"windowPosX", GetWindow().Position.X},
			{"windowPosY", GetWindow().Position.Y}
		};
		string data = Json.Stringify(settingsDict);
		file.StoreString(data);
	}
	
	private void DecoupleToggle(bool on)
	{
		DecoupledMode = on;
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
		currentGame = Game.None;
	}

	private void _QuitPressed()
	{
		SaveSettings();
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
		else if (what == NotificationWMCloseRequest)
		{
			SaveSettings();
			GetTree().Quit();
		}
	}
}
