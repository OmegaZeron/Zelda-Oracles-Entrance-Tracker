using Godot;
using System;
using System.Linq;
using Godot.Collections;

public partial class UIController : CanvasLayer
{
	public static UIController Instance { get; private set; }
	
	public enum SeasonsMapState
	{
		Holodrum,
		HolodrumInner,
		Subrosia,
		SubrosiaInner
	}
	public enum AgesMapState
	{
		Present,
		PresentInner,
		Past,
		PastInner
	}
	public enum CompanionState
	{
		Ricky,
		Moosh,
		Dimitri
	}
	private SeasonsMapState seasonsMapState = SeasonsMapState.Holodrum;
	private AgesMapState agesMapState = AgesMapState.Present;
	public CompanionState companionState { get; private set; } = CompanionState.Ricky;
	private bool isOutside => GameSelector.Instance.currentGame == GameSelector.Game.Seasons && seasonsMapState is SeasonsMapState.Holodrum or SeasonsMapState.Subrosia ||
	                          GameSelector.Instance.currentGame == GameSelector.Game.Ages && agesMapState is AgesMapState.Present or AgesMapState.Past;
	
	public Action ResetEntrances;
	
	// sprite here so we can swap between companion map textures
	[Export] private Sprite2D HolodrumParent;
	[Export] private Node2D HolodrumInnerParent;
	[Export] private Node2D SubrosiaParent;
	[Export] private Node2D SubrosiaInnerParent;
	// sprite again
	[Export] private Sprite2D LabrynnaPresentParent;
	[Export] private Node2D LabrynnaPresentInnerParent;
	[Export] private Node2D LabrynnaPastParent;
	[Export] private Node2D LabrynnaPastInnerParent;

	// seasons
	[Export] private Texture2D natzuRicky;
	[Export] private Texture2D natzuMoosh;
	[Export] private Texture2D natzuDimitri;
	[Export] private Node2D natzuRickyParent;
	[Export] private Node2D natzuMooshParent;
	[Export] private Node2D natzuDimitriParent;
	[Export] private Node2D natzuRickyInnerParent;
	[Export] private Node2D natzuMooshInnerParent;
	[Export] private Node2D natzuDimitriInnerParent;
	// ages
	[Export] private Texture2D nuunRicky;
	[Export] private Texture2D nuunMoosh;
	[Export] private Texture2D nuunDimitri;

	[Export] private Button rickyButton;
	[Export] private Button mooshButton;
	[Export] private Button dimitriButton;
	
	[Export] private RichTextLabel nameLabel;
	[Export] private RichTextLabel selectionLabel;

	[Export] public float pulseDuration = 1;
	[Export] public int pulseCount = 4;

	private Entrance selectedEntrance;
	public Array<Node> seasonsEntranceNodes { get; private set; }
	public Array<Node> agesEntranceNodes { get; private set; }

	[Export] public Array<CompanionAreaData> natzuDataList;
	private Dictionary<Entrance.EntranceType, Dictionary<string, Node>> natzuNodes;
	
	public Dictionary<GameSelector.Game, Dictionary<Entrance.EntranceType, Dictionary<string, Entrance>>> entranceDict { get; private set; }

	public override void _Ready()
	{
		Instance = this;

		Owner.Connect(Node.SignalName.Ready, Callable.From(Start));
	}

	private void MoveCompanionNodes()
	{
		if (GameSelector.Instance.currentGame == GameSelector.Game.Seasons)
		{
			foreach (CompanionAreaData data in natzuDataList)
			{
				if (data.companionState != companionState)
				{
					continue;
				}

				if (data.entranceType == Entrance.EntranceType.Outer)
				{
					Entrance outerEntrance = (Entrance)natzuNodes[Entrance.EntranceType.Outer][data.name];
					outerEntrance.GlobalPosition = data.location;
				}
				else
				{
					Sprite2D innerParent = (Sprite2D)natzuNodes[Entrance.EntranceType.Inner][data.name];
					Entrance innerEntrance = innerParent.GetChild<Entrance>(0);
					innerParent.GlobalPosition = data.location;
					innerParent.Texture = data.tex;
					innerEntrance.Position = data.entranceLoc;
				}
			}
		}
		else
		{
			
		}
	}

	private void Start()
	{
		Array<Node> nodes;
		if (GameSelector.Instance.currentGame == GameSelector.Game.Seasons)
		{
			seasonsEntranceNodes = GetTree().GetNodesInGroup("SeasonsEntrance");
			nodes = seasonsEntranceNodes;
			natzuNodes = new Dictionary<Entrance.EntranceType, Dictionary<string, Node>>
			{
				{Entrance.EntranceType.Outer, new Dictionary<string, Node>()},
				{Entrance.EntranceType.Inner, new Dictionary<string, Node>()}
			};
			foreach (Node node in GetTree().GetNodesInGroup("NatzuOuter"))
			{
				natzuNodes[Entrance.EntranceType.Outer].Add(((Entrance)node).entranceName, node);
			}
			foreach (Node node in GetTree().GetNodesInGroup("NatzuInner"))
			{
				natzuNodes[Entrance.EntranceType.Inner].Add(((Sprite2D)node).GetChild<Entrance>(0).entranceName, node);
			}
		}
		else
		{
			agesEntranceNodes = GetTree().GetNodesInGroup("AgesEntrance");
			nodes = agesEntranceNodes;
		}
		entranceDict = new Dictionary<GameSelector.Game, Dictionary<Entrance.EntranceType, Dictionary<string, Entrance>>>
		{
			{GameSelector.Instance.currentGame, new Dictionary<Entrance.EntranceType, Dictionary<string, Entrance>> {{Entrance.EntranceType.Outer, new()},{Entrance.EntranceType.Inner, new()}}}
		};
		foreach (Node node in nodes)
		{
			if (node is not Entrance entrance)
			{
				continue;
			}
			if (!entranceDict[GameSelector.Instance.currentGame][entrance.entranceType].ContainsKey(entrance.entranceName))
			{
				entranceDict[GameSelector.Instance.currentGame][entrance.entranceType].Add(entrance.entranceName, entrance);
			}
			else
			{
				GD.PushError($"Duplicate Entrance: {entrance.entranceName}, Type: {entrance.entranceType}, Alt: {entrance.altMap}, pos: {entrance.GlobalPosition}");
			}
		}
		SaveManager.Instance.LoadLayout();
	}

	public void DisplayEntranceName(string name, Entrance.EntranceType entranceType, string link = null, string decoupledLink = null)
	{
		if ((entranceType != Entrance.EntranceType.Outer || !isOutside) && (entranceType != Entrance.EntranceType.Inner || isOutside))
		{
			return;
		}
		nameLabel.Text = $" [outline_color=#000000][outline_size=6]{name}[/outline_size][/outline_color]";
		if (!string.IsNullOrEmpty(link))
		{
			nameLabel.Text += $"\n [outline_color=#000000][outline_size=6]Linked to: {link}[/outline_size][/outline_color]";
		}

		if (!string.IsNullOrEmpty(decoupledLink))
		{
			nameLabel.Text += $"\n [outline_color=#000000][outline_size=6]From: {decoupledLink}[/outline_size][/outline_color]";
		}
	}
	public void ClearEntranceName()
	{
		nameLabel.Text = "";
	}
	
	// change map section
	private void UpdateMapState()
	{
		if (GameSelector.Instance.currentGame == GameSelector.Game.Seasons)
		{
			HolodrumParent.Visible = seasonsMapState is SeasonsMapState.Holodrum or SeasonsMapState.HolodrumInner;
			HolodrumParent.Modulate = new Color(HolodrumParent.Modulate, seasonsMapState == SeasonsMapState.Holodrum ? 1 : .5f);
			HolodrumInnerParent.Visible = seasonsMapState == SeasonsMapState.HolodrumInner;
			SubrosiaParent.Visible = seasonsMapState is SeasonsMapState.Subrosia or SeasonsMapState.SubrosiaInner;
			SubrosiaParent.Modulate = new Color(SubrosiaParent.Modulate, seasonsMapState == SeasonsMapState.Subrosia ? 1 : .5f);
			SubrosiaInnerParent.Visible = seasonsMapState == SeasonsMapState.SubrosiaInner;
			rickyButton.Visible = seasonsMapState is SeasonsMapState.Holodrum or SeasonsMapState.HolodrumInner;
			mooshButton.Visible = seasonsMapState is SeasonsMapState.Holodrum or SeasonsMapState.HolodrumInner;
			dimitriButton.Visible = seasonsMapState is SeasonsMapState.Holodrum or SeasonsMapState.HolodrumInner;
			Camera.Instance.ChangeBounds(seasonsMapState is SeasonsMapState.Subrosia or SeasonsMapState.SubrosiaInner);
		}
		else
		{
			
		}
	}
	private void _OnHolodrumPressed()
	{
		seasonsMapState = seasonsMapState is SeasonsMapState.Holodrum or SeasonsMapState.Subrosia ? SeasonsMapState.Holodrum : SeasonsMapState.HolodrumInner;
		UpdateMapState();
	}
	private void _OnSubrosiaPressed()
	{
		seasonsMapState = seasonsMapState is SeasonsMapState.Holodrum or SeasonsMapState.Subrosia ? SeasonsMapState.Subrosia : SeasonsMapState.SubrosiaInner;
		UpdateMapState();
	}
	private void _OnPresentPressed()
	{
		agesMapState = agesMapState is AgesMapState.Past or AgesMapState.Present ? AgesMapState.Present : AgesMapState.PresentInner;
	}
	private void _OnPastPressed()
	{
		agesMapState = agesMapState is AgesMapState.Past or AgesMapState.Present ? AgesMapState.Past : AgesMapState.PastInner;
	}
	private void _OnOuterPressed()
	{
		if (GameSelector.Instance.currentGame == GameSelector.Game.Seasons)
		{
			seasonsMapState = seasonsMapState is SeasonsMapState.Holodrum or SeasonsMapState.HolodrumInner ? SeasonsMapState.Holodrum : SeasonsMapState.Subrosia;
		}
		else
		{
			agesMapState = agesMapState is AgesMapState.Present or AgesMapState.PresentInner ? AgesMapState.Present : AgesMapState.Past;
		}

		UpdateMapState();
	}
	private void _OnInnerPressed()
	{
		if (GameSelector.Instance.currentGame == GameSelector.Game.Seasons)
		{
			seasonsMapState = seasonsMapState is SeasonsMapState.Holodrum or SeasonsMapState.HolodrumInner ? SeasonsMapState.HolodrumInner : SeasonsMapState.SubrosiaInner;
		}
		else
		{
			agesMapState = agesMapState is AgesMapState.Present or AgesMapState.PresentInner ? AgesMapState.PresentInner : AgesMapState.PastInner;
		}
		UpdateMapState();
	}

	public void ChangeMap(bool defaultMap, bool outer)
	{
		if (GameSelector.Instance.currentGame == GameSelector.Game.Seasons)
		{
			if (defaultMap)
			{
				seasonsMapState = outer ? SeasonsMapState.Holodrum : SeasonsMapState.HolodrumInner;
			}
			else
			{
				seasonsMapState = outer ? SeasonsMapState.Subrosia : SeasonsMapState.SubrosiaInner;
			}
		}
		else
		{
			if (defaultMap)
			{
				agesMapState = outer ? AgesMapState.Present : AgesMapState.PresentInner;
			}
			else
			{
				agesMapState = outer ? AgesMapState.Past : AgesMapState.PastInner;
			}
		}
		UpdateMapState();
	}
	
	// change Natzu version
	public void ChangeCompanionState(CompanionState newState)
	{
		CompanionState prevState = companionState;
		companionState = newState;
		if (prevState == companionState)
		{
			return;
		}
		if (GameSelector.Instance.currentGame == GameSelector.Game.Seasons)
		{
			HolodrumParent.Texture = companionState switch
			{
				CompanionState.Ricky => natzuRicky,
				CompanionState.Moosh => natzuMoosh,
				CompanionState.Dimitri => natzuDimitri,
				_ => HolodrumParent.Texture
			};
			
			// natzuRickyParent.Visible = companionState == CompanionState.Ricky;
			// natzuRickyInnerParent.Visible = companionState == CompanionState.Ricky;
			// natzuMooshParent.Visible = companionState == CompanionState.Moosh;
			// natzuMooshInnerParent.Visible = companionState == CompanionState.Moosh;
			// natzuDimitriParent.Visible = companionState == CompanionState.Dimitri;
			// natzuDimitriInnerParent.Visible = companionState == CompanionState.Dimitri;
			// TODO retarget linked entrances?
			MoveCompanionNodes();
		}
		else
		{
			LabrynnaPresentParent.Texture = companionState switch
			{
				CompanionState.Ricky => nuunRicky,
				CompanionState.Moosh => nuunMoosh,
				CompanionState.Dimitri => nuunDimitri,
				_ => LabrynnaPresentParent.Texture
			};
		}
	}
	private void _OnCompanionPressed(string companionName)
	{
		ChangeCompanionState(Enum.Parse<CompanionState>(companionName));
	}

	public bool IsAttemptingLink()
	{
		return selectedEntrance != null;
	}

	private bool EntranceMatchesState(Entrance entrance)
	{
		switch (GameSelector.Instance.currentGame)
		{
			case GameSelector.Game.Seasons when (seasonsMapState is SeasonsMapState.Holodrum or SeasonsMapState.Subrosia && entrance.entranceType == Entrance.EntranceType.Outer) || (seasonsMapState is SeasonsMapState.HolodrumInner or SeasonsMapState.SubrosiaInner && entrance.entranceType == Entrance.EntranceType.Inner):
			case GameSelector.Game.Ages when (agesMapState is AgesMapState.Present or AgesMapState.Past && entrance.entranceType == Entrance.EntranceType.Outer) || (agesMapState is AgesMapState.PresentInner or AgesMapState.PastInner && entrance.entranceType == Entrance.EntranceType.Inner):
			{
				return true;
			}
			default:
			{
				return false;
			}
		}
	}

	public void TrashEntrance(Entrance entrance)
	{
		entrance.TrashSelf();
		SaveManager.Instance.SaveLayout();
	}

	public void ClearSelectedEntranceIfEqual(Entrance entrance)
	{
		if (entrance == selectedEntrance)
		{
			selectedEntrance = null;
			selectionLabel.Text = "";
		}
	}
	
	/// returns true if a full link was performed
	public bool OnEntranceSelected(Entrance entrance)
	{
		if (!EntranceMatchesState(entrance))
		{
			return false;
		}
		// same entrance, exit
		if (selectedEntrance == entrance)
		{
			return false;
		}
		// 2 entrances selected, complete link
		if (selectedEntrance != null)
		{
			selectedEntrance.LinkEntrance(entrance);
			if (!GameSelector.Instance.DecoupledMode)
			{
				entrance.LinkEntrance(selectedEntrance);
			}
			else
			{
				entrance.AddDecoupledEntrance(selectedEntrance);
			}
			selectedEntrance = null;
			selectionLabel.Text = "";
			SaveManager.Instance.SaveLayout();
			return true;
		}
		// start link process
		selectedEntrance = entrance;
		selectionLabel.Text = selectedEntrance.entranceName;
		return false;
	}

	private void _OnResetPressed()
	{
		ResetEntrances?.Invoke();
		SaveManager.Instance.Clear();
	}

	private void _OnGoBackPressed()
	{
		GameSelector.Instance.SelectorScene();
	}
	
	public override void _Input(InputEvent ev)
	{
		if (ev is not InputEventMouseButton button || !button.IsPressed())
		{
			return;
		}
		
		// right click - deselect
		if (button.ButtonIndex == MouseButton.Right)
		{
			selectedEntrance = null;
			selectionLabel.Text = "";
		}
	}
}
