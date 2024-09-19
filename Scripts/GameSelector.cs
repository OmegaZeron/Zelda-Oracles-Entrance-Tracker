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

	[Export] private CheckButton decoupledButton;
	[Export] private AcceptDialog dialog;
	[Export] private CanvasLayer UI;
	
	private StringName saveActionName = new("SaveAction");
	private StringName saveAsActionName = new("SaveAsAction");
	private StringName doNotSaveActionName = new("DoNotSaveAction");
	private StringName cancelActionName = new("CancelAction");
	private Action PostSaveCallback;
	private Action DoNotSaveCallback;
	
	public override void _Ready()
	{
		Instance = this;
		
		GetTree().QuitOnGoBack = false;
		// preload scenes
		seasonsScene = GD.Load<PackedScene>("res://Scenes/Seasons.tscn");
		// agesScene = GD.Load<PackedScene>("res://Scenes/Ages.tscn");

		dialog.AddButton("Save", true, "SaveAction");
		dialog.AddButton("Save As", true, "SaveAsAction");
		dialog.AddButton("No", true, "DoNotSaveAction");
		dialog.AddButton("Cancel", true, "CancelAction");
		dialog.CustomAction += OnCustomAction;
		dialog.GetOkButton().Visible = false;
	}
	
	private void OnCustomAction(StringName actionName)
	{
		if (actionName.Equals(saveActionName))
		{
			SaveManager.Instance.SaveLayout();
			PostSaveCallback?.Invoke();
		}
		else if (actionName.Equals(saveAsActionName))
		{
			SaveManager.Instance.OpenSaveFileDialog(PostSaveCallback, CloseDialog);
		}
		else if (actionName.Equals(doNotSaveActionName))
		{
			DoNotSaveCallback?.Invoke();
		}
		else if (actionName.Equals(cancelActionName))
		{
			CloseDialog();
		}
	}

	private void SaveAndCloseGame()
	{
		SaveManager.Instance.SaveLayout();
		SelectorScene();
	}
	private void CloseGameNoSave()
	{
		dialog.Hide();
		SaveManager.Instance.IsDirty = false;
		SelectorScene();
		PostSaveCallback = null;
	}

	public void AskToSaveBeforeCloseGame()
	{
		PostSaveCallback = CloseGameNoSave;
		DoNotSaveCallback = CloseGameNoSave;
		dialog.Show();
	}
	
	private void CloseAppNoSave()
	{
		SettingsManager.Instance.SaveSettings();
		GetTree().Quit();
	}

	private void AskToSaveBeforeQuitApp()
	{
		PostSaveCallback = CloseAppNoSave;
		DoNotSaveCallback = CloseAppNoSave;
		dialog.Show();
	}

	private void CloseDialog()
	{
		dialog.Hide();
	}
	
	public void DecoupleToggle(bool on)
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
		Game selectedGame = Enum.Parse<Game>(gameName);
		LoadScene(selectedGame);
		// button was pressed, so load default
		// TODO maybe try to load the last save file instead?
		SaveManager.Instance.LoadDefaultSave();
	}

	public void SelectorScene()
	{
		UI.Visible = true;
		RemoveChild(loadedScene);
		loadedScene.QueueFree();
		loadedScene = null;
		currentGame = Game.None;
		activeScene = ActiveScene.Main;
		DecoupledMode = decoupledButton.ButtonPressed;
	}

	public void OpenLoadFileDialog()
	{
		SaveManager.Instance.OpenLoadFileDialog();
	}

	public void LoadScene(Game game)
	{
		if (currentGame != Game.None)
		{
			SelectorScene();
		}
		currentGame = game;
		PackedScene scene = game switch
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
	
	private void _QuitPressed()
	{
		SettingsManager.Instance.SaveSettings();
		GetTree().Quit();
	}

	public override void _Input(InputEvent ev)
	{
		if (ev is not InputEventKey key || !key.IsPressed())
		{
			return;
		}
		
		if (key.Keycode == Key.O && Input.IsKeyPressed(Key.Ctrl))
		{
			OpenLoadFileDialog();
		}
	}
	
	public override void _Notification(int what)
	{
		if (what == NotificationWMCloseRequest)
		{
			if (SaveManager.Instance.IsDirty)
			{
				AskToSaveBeforeQuitApp();
				return;
			}
			
			SettingsManager.Instance.SaveSettings();
			GetTree().Quit();
		}
	}
}
