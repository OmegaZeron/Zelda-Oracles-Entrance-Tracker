using System;
using Godot;
using System.Collections.Generic;
using Godot.Collections;

public partial class SaveManager : Node
{
	public static SaveManager Instance { get; private set; }
	
	// TODO update these to use multiple slots/import
	//#if DEBUG
	private const string SEASONS_PATH = "res://seasons.bin";
	private const string AGES_PATH = "res://ages.bin";
	private const string SEASONS_DECOUPLED_PATH = "res://seasons_d.bin";
	private const string AGES_DECOUPLED_PATH = "res://ages_d.bin";
	// #else
	// private const string SEASONS_PATH = "user://seasons.bin";
	// private const string AGES_PATH = "user://ages.bin";
	// private const string SEASONS_DECOUPLED_PATH = "user://seasons_d.bin";
	// private const string AGES_DECOUPLED_PATH = "user://ages_d.bin";
	// #endif

	private const string SEASONS_VERSION = "v0.1.2";
	private const string AGES_VERSION = "v0.1.0";
	
	public override void _Ready()
	{
		if (Instance != null)
		{
			QueueFree();
		}
		Instance = this;
	}

	private string GetSelectedPath()
	{
		GameSelector.Game game = GameSelector.Instance.currentGame;
		bool decoupled = GameSelector.Instance.DecoupledMode;
		
		if (game == GameSelector.Game.Seasons)
		{
			return decoupled ? SEASONS_DECOUPLED_PATH : SEASONS_PATH;
		}
		return decoupled ? AGES_DECOUPLED_PATH : AGES_PATH;
	}
	
	public void SaveLayout()
	{
		using FileAccess file = FileAccess.Open(GetSelectedPath(), FileAccess.ModeFlags.Write);
		Godot.Collections.Dictionary<string, Godot.Collections.Dictionary<string, string>> outers = new();
		Godot.Collections.Dictionary<string, Godot.Collections.Dictionary<string, string>> inners = new();
		Array<Node> nodes = GameSelector.Instance.currentGame == GameSelector.Game.Seasons ? UIController.Instance.seasonsEntranceNodes : UIController.Instance.agesEntranceNodes;
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
		file.StoreLine(json);
	}

	public void LoadLayout()
	{
		using FileAccess file = FileAccess.Open(GetSelectedPath(), FileAccess.ModeFlags.Read);
		if (!FileAccess.FileExists(GetSelectedPath()) || file.EofReached()) { return; }

		string gameVer = file.GetLine();
		if (gameVer == "")
		{
			return;
		}
		// GameSelector.Game game = Enum.TryParse<GameSelector.Game>(file.GetLine());
		if (!Enum.TryParse(gameVer, out GameSelector.Game game))
		{
			GD.PushWarning($"Save file is for {game}, but selected game is {GameSelector.Instance.currentGame}");
			return;
		}
		string mapVersion = GameSelector.Instance.currentGame == GameSelector.Game.Seasons ? SEASONS_VERSION : AGES_VERSION;
		string saveVersion = file.GetLine();
		if (saveVersion != mapVersion)
		{
			GD.PushError($"Version mismatch, aborting load. Map version: {mapVersion}, save version: {saveVersion}");
			return;
		}
		UIController.Instance.ChangeCompanionState(Enum.Parse<UIController.CompanionState>(file.GetLine()));
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
	}

	public void Clear()
	{
		using FileAccess file = FileAccess.Open(GetSelectedPath(), FileAccess.ModeFlags.Write);
		file.StoreString("");
	}
}
