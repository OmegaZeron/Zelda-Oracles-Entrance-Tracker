using System;
using Godot;
using System.Collections.Generic;
using Godot.Collections;

public partial class SaveManager : Node
{
	public static SaveManager Instance { get; private set; }
	
	private const string SEASONS_PATH = "res://seasons.bin";
	private const string AGES_PATH = "res://ages.bin";
	private const string SEASONS_DECOUPLED_PATH = "res://seasons_d.bin";
	private const string AGES_DECOUPLED_PATH = "res://ages_d.bin";
	
	private const string SEASONS_VERSION = "v0.1.5";
	private const string AGES_VERSION = "v0.0.0";

	private string saveFilePath;

	private bool isDirty;
	public bool IsDirty
	{
		get => isDirty;
		set
		{
			if (isDirty != value)
			{
				GetWindow().Title = $"{(value ? "*" : "")}Oracles Entrance Tracker {SettingsManager.APP_VERSION}";
			}

			isDirty = value;
		}
	}

	private FileDialog dialog;

	private Action saveSuccessCallback;
	private Action saveCancelledCallback;
	
	public override void _Ready()
	{
		if (Instance != null)
		{
			QueueFree();
		}
		Instance = this;
		GetWindow().FilesDropped += OnFilesDropped;
		
		dialog = new FileDialog
		{
			UseNativeDialog = true,
			Access = FileDialog.AccessEnum.Filesystem
		};
		dialog.AddFilter("*.bin");
	}

	public override void _ExitTree()
	{
		GetWindow().FilesDropped -= OnFilesDropped;
	}
	
	private string GetDefaultPath()
	{
		GameSelector.Game game = GameSelector.Instance.currentGame;
		bool decoupled = GameSelector.Instance.DecoupledMode;
		
		if (game == GameSelector.Game.Seasons)
		{
			return decoupled ? SEASONS_DECOUPLED_PATH : SEASONS_PATH;
		}
		return decoupled ? AGES_DECOUPLED_PATH : AGES_PATH;
	}

	private void OnFilesDropped(string[] files)
	{
		if (files.Length != 1)
		{
			return;
		}
		if (string.IsNullOrEmpty(files[0]))
		{
			return;
		}
		if (!files[0].EndsWith(".bin"))
		{
			return;
		}
		
		LoadLayout(files[0]);
	}

	public void LoadDefaultSave()
	{
		LoadLayout(GetDefaultPath());
	}

	public void AttemptAutoSave()
	{
		if (SettingsManager.Instance.autoSave)
		{
			SaveLayout();
			return;
		}

		IsDirty = true;
	}
	
	public void OpenLoadFileDialog()
	{
		dialog.FileMode = FileDialog.FileModeEnum.OpenFile;
		dialog.FileSelected += LoadSaveFromFile;
		dialog.Canceled += OnCancelled;
		dialog.Popup();
	}

	private void LoadSaveFromFile(string filepath)
	{
		dialog.FileSelected -= LoadSaveFromFile;
		dialog.Canceled -= OnCancelled;
		LoadLayout(filepath, true);
	}

	public void OpenSaveFileDialog(Action callback = null, Action cancelled = null)
	{
		dialog.FileMode = FileDialog.FileModeEnum.SaveFile;
		dialog.FileSelected += OnSavePathSelected;
		dialog.Canceled += OnCancelled;
		saveSuccessCallback = callback;
		saveCancelledCallback = cancelled;
		dialog.Popup();
	}

	private void OnSavePathSelected(string filePath)
	{
		dialog.FileSelected -= OnSavePathSelected;
		dialog.Canceled -= OnCancelled;
		saveFilePath = filePath;
		if (!saveFilePath.EndsWith(".bin"))
		{
			saveFilePath += ".bin";
		}
		SaveLayout();
		saveSuccessCallback?.Invoke();
		saveSuccessCallback = null;
	}

	private void OnCancelled()
	{
		if (dialog.FileMode == FileDialog.FileModeEnum.OpenFile)
		{
			dialog.FileSelected -= LoadSaveFromFile;
		}
		else if (dialog.FileMode == FileDialog.FileModeEnum.SaveFile)
		{
			dialog.FileSelected -= OnSavePathSelected;
		}
		dialog.Canceled -= OnCancelled;
		saveCancelledCallback?.Invoke();
		saveCancelledCallback = null;
	}
	public void SaveLayout()
	{
		using FileAccess file = FileAccess.Open(!string.IsNullOrEmpty(saveFilePath) ? saveFilePath : GetDefaultPath(), FileAccess.ModeFlags.Write);
		Godot.Collections.Dictionary<string, Godot.Collections.Dictionary<string, string>> outers = new();
		Godot.Collections.Dictionary<string, Godot.Collections.Dictionary<string, string>> inners = new();
		Array<Node> nodes = GameSelector.Instance.currentGame switch
		{
			GameSelector.Game.Seasons => UIController.Instance.seasonsEntranceNodes,
			GameSelector.Game.Ages => UIController.Instance.agesEntranceNodes,
			_ => null
		};
		if (nodes == null)
		{
			// TODO error probably
			GD.PushError("Nodes null");
			return;
		}
		foreach (Node node in nodes)
		{
			if (node is not Entrance entrance) { continue; }

			Entrance.EntranceInfo info = entrance.GetEntranceInfo();
			if (info.entranceType == Entrance.EntranceType.Outer)
			{
				outers.Add(info.name, info.ToDict());
			}
			else
			{
				inners.Add(info.name, info.ToDict());
			}
		}

		Godot.Collections.Dictionary<string, Godot.Collections.Dictionary<string, Godot.Collections.Dictionary<string, string>>> entrances = new()
		{
			{"outers", outers},
			{"inners", inners}
		};
		string json = Json.Stringify(entrances);
		file.StoreLine(GameSelector.Instance.currentGame.ToString());
		file.StoreLine(GameSelector.Instance.currentGame == GameSelector.Game.Seasons ? SEASONS_VERSION : AGES_VERSION);
		file.StoreLine(UIController.Instance.companionState.ToString());
		file.StoreLine(GameSelector.Instance.DecoupledMode ? "Decoupled" : "Coupled");
		file.StoreLine(json);
		IsDirty = false;
	}

	public void LoadLayout(string filePath, bool reportGame = false)
	{
		if (string.IsNullOrEmpty(filePath))
		{
			GD.PushError("Supplied file path is empty");
			return;
		}
		using FileAccess file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
		if (!FileAccess.FileExists(filePath) || file.EofReached())
		{
			return;
		}

		string gameString = file.GetLine();
		if (gameString == "")
		{
			return;
		}
		if (!Enum.TryParse(gameString, out GameSelector.Game gameType))
		{
			GD.PushWarning($"Could not parse game type. Make sure this is a valid save for {SEASONS_VERSION} (Seasons) or {AGES_VERSION} (Ages)");
			return;
		}
		string mapVersion = gameType switch {
			GameSelector.Game.Seasons => SEASONS_VERSION,
			GameSelector.Game.Ages => AGES_VERSION,
			_ => ""
		};
		if (mapVersion == "")
		{
			// TODO error probably
			return;
		}
		string saveVersion = file.GetLine();
		if (saveVersion != mapVersion)
		{
			GD.PushError($"Version mismatch, aborting load. Map version: {mapVersion}, save version: {saveVersion}");
			return;
		}

		string companionString = file.GetLine();
		if (!Enum.TryParse(companionString, out UIController.CompanionState companionState))
		{
			GD.PushError("Failed to parse companion string");
			return;
		}

		string coupledString = file.GetLine();
		if (string.IsNullOrEmpty(coupledString))
		{
			GD.PushError("Could not get coupled state");
			return;
		}

		// everything should be good
		// if needed, report back to GameSelector to load the scene
		if (reportGame)
		{
			GameSelector.Instance.LoadScene(gameType);
		}
		UIController.Instance.ChangeCompanionState(companionState);
		GameSelector.Instance.DecoupleToggle(coupledString == "Decoupled");
		try
		{
			Godot.Collections.Dictionary<string, Godot.Collections.Dictionary<string, Godot.Collections.Dictionary<string, string>>> save = Json.ParseString(file.GetLine()).AsGodotDictionary<string, Godot.Collections.Dictionary<string, Godot.Collections.Dictionary<string, string>>>();
			foreach (string entranceType in save.Keys)
			{
				foreach (string entranceName in save[entranceType].Keys)
				{
					Entrance.EntranceInfo info = Entrance.EntranceInfo.FromDict(save[entranceType][entranceName]);
					Entrance entrance = UIController.Instance.entranceDict[GameSelector.Instance.currentGame][info.entranceType][info.name];
					if (info.isTrash)
					{
						entrance.TrashSelf();
					}
					if (info.isDecoupledTrash)
					{
						entrance.DecoupledTrashSelf();
					}

					if (!string.IsNullOrEmpty(info.linkedName))
					{
						Entrance link = UIController.Instance.entranceDict[GameSelector.Instance.currentGame][info.linkedEntranceType][info.linkedName];
						entrance.LinkEntrance(link);
					}

					if (GameSelector.Instance.DecoupledMode && !string.IsNullOrEmpty(info.decoupledName))
					{
						Entrance decouple = UIController.Instance.entranceDict[GameSelector.Instance.currentGame][info.decoupledType][info.decoupledName];
						entrance.AddDecoupledEntrance(decouple);
					}
				}
			}
		}
		catch (KeyNotFoundException e)
		{
			GD.PushError($"{e}, clearing save");
			Clear();
		}

		saveFilePath = filePath;
	}

	private void Clear()
	{
		using FileAccess file = FileAccess.Open(GetDefaultPath(), FileAccess.ModeFlags.Write);
		file.StoreString("");
	}
}
