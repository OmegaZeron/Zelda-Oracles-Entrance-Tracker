using System;
using Godot;
using Godot.Collections;

public partial class SettingsManager : CanvasLayer
{
	public static SettingsManager Instance { get; private set; }

	// scene objects
	[Export] private CheckButton autoSaveButton;
	[Export] private SpinBox pulseDurationPicker;
	[Export] private SpinBox pulseCountPicker;
	
	[Export] private Button goBackButton;
	[Export] private Button saveButton;
	[Export] private Button saveAsButton;
	[Export] private Button resetButton;
	
	// constants
	private const string SETTINGS_PATH = "res://settings.json";
	public const string APP_VERSION = "v0.1.1";

	// settings
	[Export] public int pulseCount { get; private set; } = 4;
	[Export] public float pulseDuration { get; private set; } = 1;
	[Export] public Vector2I windowPos { get; private set; }
	[Export] public Vector2I windowSize { get; private set; }
	[Export] public bool autoSave { get; private set; } = true;
	
	public Action ResetEntrances;
	
	public override void _Ready()
	{
		if (Instance != null)
		{
			QueueFree();
		}
		Instance = this;
		
		GetWindow().Title = $"Oracles Entrance Tracker {APP_VERSION}";
		
		LoadSettings();
	}

	public void ShowSettingsMenu()
	{
		autoSaveButton.ButtonPressed = autoSave;
		pulseDurationPicker.Value = pulseDuration;
		pulseCountPicker.Value = pulseCount;
		saveButton.Visible = GameSelector.Instance.currentGame != GameSelector.Game.None;
		saveAsButton.Visible = GameSelector.Instance.currentGame != GameSelector.Game.None;
		resetButton.Visible = GameSelector.Instance.currentGame != GameSelector.Game.None;
		Visible = true;
	}
	private void HideSettingsMenu()
	{
		Visible = false;
		GameSelector.Instance.CloseSettings();
	}

	public void SaveSettings()
	{
		using FileAccess file = FileAccess.Open(SETTINGS_PATH, FileAccess.ModeFlags.Write);
		Dictionary<string, Variant> settingsDict = new()
		{
			{"pulseCount", pulseCount},
			{"pulseDuration", pulseDuration},
			{"windowSizeX", GetWindow().Size.X},
			{"windowSizeY", GetWindow().Size.Y},
			{"windowPosX", GetWindow().Position.X},
			{"windowPosY", GetWindow().Position.Y},
			{"autoSave", autoSave}
		};
		string data = Json.Stringify(settingsDict);
		file.StoreString(data);
	}
	
	private void LoadSettings()
	{
		using FileAccess file = FileAccess.Open(SETTINGS_PATH, FileAccess.ModeFlags.Read);
		
		if (!FileAccess.FileExists(SETTINGS_PATH))
		{
			SaveSettings();
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

		Dictionary<string, Variant> settingsDict = Json.ParseString(data).AsGodotDictionary<string, Variant>();
		pulseCount = settingsDict["pulseCount"].AsInt32();
		pulseDuration = (float)settingsDict["pulseDuration"].AsDouble();
		windowPos = new Vector2I(settingsDict["windowPosX"].AsInt32(), settingsDict["windowPosY"].AsInt32());
		windowSize = new Vector2I(settingsDict["windowSizeX"].AsInt32(), settingsDict["windowSizeY"].AsInt32());
		autoSave = settingsDict["autoSave"].AsBool();

		GetWindow().Size = windowSize;
		GetWindow().Position = windowPos;
	}

	private void OnAutoSaveToggled(bool on)
	{
		autoSave = on;
		if (on && GameSelector.Instance.activeScene == GameSelector.ActiveScene.GameMap)
		{
			SaveManager.Instance.SaveLayout();
		}
	}

	private void OnPulseDurationChanged(float amount)
	{
		pulseDuration = amount;
	}
	private void OnPulseCountChanged(float amount)
	{
		pulseCount = (int)amount;
	}

	private void OnSavePressed()
	{
		SaveManager.Instance.SaveLayout();
	}

	private void OnSaveAsPressed()
	{
		SaveManager.Instance.OpenSaveFileDialog();
	}

	private void OnLoadPressed()
	{
		SaveManager.Instance.OpenLoadFileDialog();
	}

	private void OnResetPressed()
	{
		ResetEntrances?.Invoke();
		UIController.Instance.ClearSelectedEntranceIfEqual(null, true);
		SaveManager.Instance.AttemptAutoSave();
		HideSettingsMenu();
	}
	
	public override void _Input(InputEvent ev)
	{
		if (GameSelector.Instance.activeScene != GameSelector.ActiveScene.Settings)
		{
			return;
		}
		if (ev is InputEventKey key && key.IsPressed() && key.Keycode == Key.Escape)
		{
			HideSettingsMenu();
		}
	}
}
