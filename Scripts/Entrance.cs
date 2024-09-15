using Godot;
using System;
using Godot.Collections;

public partial class Entrance : TextureButton
{
	public partial class EntranceInfo : GodotObject
	{
		public string name;
		public EntranceType entranceType;
		public string linkedName;
		public EntranceType linkedEntranceType;
		public string decoupledName;
		public EntranceType decoupledType;
		public bool isTrash;
		public bool isDecoupledTrash;

		public Dictionary<string, string> ToDict()
		{
			return new Dictionary<string, string>
			{
				{"name", name},
				{"entranceType", entranceType.ToString()},
				{"linkedName", linkedName},
				{"linkedEntranceType", linkedEntranceType.ToString()},
				{"decoupledName", decoupledName},
				{"decoupledType", decoupledType.ToString()},
				{"isTrash", isTrash.ToString()},
				{"isDecoupledTrash", isDecoupledTrash.ToString()}
			};
		}

		public static EntranceInfo FromDict(Dictionary<string, string> dict)
		{
			return new EntranceInfo
			{
				name = dict["name"],
				entranceType = Enum.Parse<EntranceType>(dict["entranceType"]),
				linkedName = dict["linkedName"],
				linkedEntranceType = Enum.Parse<EntranceType>(dict["linkedEntranceType"]),
				decoupledName = dict["decoupledName"],
				decoupledType = Enum.Parse<EntranceType>(dict["decoupledType"]),
				isTrash = bool.Parse(dict["isTrash"]),
				isDecoupledTrash = bool.Parse(dict["isDecoupledTrash"])
			};
		}
	}
	public enum EntranceType
	{
		Outer,
		Inner
	}
	[Export] public string entranceName { get; private set; }
	[Export] public EntranceType entranceType { get; private set; }
	[Export] public bool altMap { get; private set; }
	[Export] private bool oneWay;
	public Entrance linkedEntrance { get; private set; }
	public Entrance decoupledEntrance { get; private set; }
	private bool isTrash;
	private bool isDecoupledTrash;

	private bool isPulsing;
	private float remainingPulseDuration;
	private bool isDisplayingEntranceName;

	public override void _Ready()
	{
		Owner.Connect(Node.SignalName.Ready, Callable.From(Start));
	}

	private void Start()
	{
		UIController.Instance.ResetEntrances += ClearAll;
	}

	public override void _ExitTree()
	{
		UIController.Instance.ResetEntrances -= ClearAll;
	}

	public EntranceInfo GetEntranceInfo()
	{
		EntranceInfo info = new()
		{
			name = entranceName,
			entranceType = entranceType,
			linkedName = linkedEntrance?.entranceName,
			decoupledName = decoupledEntrance?.entranceName,
			isTrash = isTrash,
			isDecoupledTrash = isDecoupledTrash
		};
		if (linkedEntrance != null)
		{
			info.linkedEntranceType = linkedEntrance.entranceType;
		}
		if (decoupledEntrance != null)
		{
			info.decoupledType = decoupledEntrance.entranceType;
		}
		return info;
	}
	
	// for displaying entrance name in UI
	private void OnMouseEntered()
	{
		UIController.Instance.DisplayEntranceName(this);
		isDisplayingEntranceName = true;
	}
	private void OnMouseExited()
	{
		UIController.Instance.ClearEntranceName();
		isDisplayingEntranceName = false;
	}

	private void MoveToAndPulse(Entrance entrance)
	{
		UIController.Instance.ChangeMap(!entrance.altMap, entrance.entranceType == EntranceType.Outer);
		Camera.Instance.MoveToLocation(entrance.GlobalPosition);
		entrance.Pulse();
	}
	private void _OnClick(InputEvent ev)
	{
		if (ev is not InputEventMouseButton button || !button.IsPressed())
		{
			return;
		}

		if (decoupledEntrance != null && button.ButtonIndex == MouseButton.Left && Input.IsKeyPressed(Key.Ctrl))
		{
			MoveToAndPulse(decoupledEntrance);
		}
		// left click - start/continue linking process, or display current link
		else if (button.ButtonIndex == MouseButton.Left && (!isTrash || GameSelector.Instance.DecoupledMode))
		{
			if (linkedEntrance == null && Input.IsKeyPressed(Key.Shift))
			{
				TrashSelf();
				SaveManager.Instance.SaveLayout();
				return;
			}
			if (linkedEntrance == null && !isTrash && !GameSelector.Instance.DecoupledMode || GameSelector.Instance.DecoupledMode && (decoupledEntrance == null || !UIController.Instance.IsAttemptingLink()) && (UIController.Instance.IsAttemptingLink() || !isTrash && linkedEntrance == null))
			{
				if (UIController.Instance.OnEntranceSelected(this))
				{
					UIController.Instance.DisplayEntranceName(this);
				}
			}
			else if (linkedEntrance != null)
			{
				MoveToAndPulse(linkedEntrance);
			}
		}
		// middle click - display current decoupled link
		// else if (button.ButtonIndex == MouseButton.Middle && decoupledEntrance != null)
		// {
		// 	MoveToAndPulse(decoupledEntrance);
		// }
		// right click
		else if (button.ButtonIndex == MouseButton.Right)
		{
			// add decoupled trash
			if (Input.IsKeyPressed(Key.Shift) && decoupledEntrance == null)
			{
				DecoupledTrashSelf();
				SaveManager.Instance.SaveLayout();
				return;
			}
			// unlink
			isTrash = false;
			isDecoupledTrash = false;
			if (!GameSelector.Instance.DecoupledMode)
			{
				linkedEntrance?.UnlinkEntrance();
			}
			else
			{
				linkedEntrance?.RemoveDecoupledEntrance();
			}
			UnlinkEntrance();
			if (isDisplayingEntranceName)
			{
				UIController.Instance.DisplayEntranceName(this);
			}
			SaveManager.Instance.SaveLayout();
		}
	}

	// 'pulse' entrance by swapping back and forth between the linked and unlinked textures
	// used when clicking a linked entrance, pulses so you can see which entrance is linked after swapping maps
	private async void Pulse()
	{
		if (isPulsing)
		{
			return;
		}

		// setup vars
		isPulsing = true;
		float pulseDuration = UIController.Instance.pulseDuration;
		float currentPulseDuration = 0;
		float pulseInterval = 1f / UIController.Instance.pulseCount / 2;
		Texture2D linkedTexture = GameSelector.Instance.linkedTexture;
		Texture2D unlinkedTexture = GameSelector.Instance.unlinkedTexture;
		StringName signalName = SceneTreeTimer.SignalName.Timeout;
		
		// determine what our first texture should be
		bool isCurrentlyLinkedTex = TextureNormal == linkedTexture;
		TextureNormal = isCurrentlyLinkedTex ? unlinkedTexture : linkedTexture;
		isCurrentlyLinkedTex = !isCurrentlyLinkedTex;
		
		while (currentPulseDuration < pulseDuration)
		{
			await ToSignal(GetTree().CreateTimer(pulseInterval), signalName);
			if (linkedEntrance == null && decoupledEntrance == null)
			{
				// exit early
				currentPulseDuration = pulseDuration;
			}
			else
			{
				currentPulseDuration += pulseInterval;
				TextureNormal = isCurrentlyLinkedTex ? unlinkedTexture : linkedTexture;
				isCurrentlyLinkedTex = !isCurrentlyLinkedTex;
			}
		}

		isPulsing = false;
		UpdateTexture();
	}

	private Texture2D GetTexture()
	{
		if (isTrash)
		{
			if (decoupledEntrance != null)
			{
				return GameSelector.Instance.trashDecoupledTexture; // gray/yellow
			}

			if (GameSelector.Instance.DecoupledMode && !isDecoupledTrash)
			{
				return GameSelector.Instance.trashNoDecoupledTexture; // gray/red
			}

			return GameSelector.Instance.trashTexture; // gray
		}

		if (linkedEntrance != null)
		{
			if (decoupledEntrance != null)
			{
				return GameSelector.Instance.linkedDecoupledTexture; // green/yellow
			}

			if (GameSelector.Instance.DecoupledMode)
			{
				if (isDecoupledTrash)
				{
					return GameSelector.Instance.linkedTrashTexture; // green/gray
				}
				return GameSelector.Instance.linkedNoDecoupledTexture; // green/red
			}
			return GameSelector.Instance.linkedTexture; // green
		}

		if (isDecoupledTrash)
		{
			return GameSelector.Instance.unlinkedTrashTexture; // red/gray
		}
		
		if (decoupledEntrance != null)
		{
			return GameSelector.Instance.decoupledTexture; // red/yellow
		}

		return GameSelector.Instance.unlinkedTexture; // red
	}

	public void UpdateTexture()
	{
		if (!isPulsing)
		{
			TextureNormal = GetTexture();
		}
	}
	
	public void TrashSelf()
	{
		isTrash = true;
		UpdateTexture();
		UIController.Instance.ClearSelectedEntranceIfEqual(this);
	}
	public void DecoupledTrashSelf()
	{
		isDecoupledTrash = true;
		UpdateTexture();
		// UIController.Instance.ClearSelectedEntranceIfEqual(this);
	}
	
	public bool LinkEntrance(Entrance entrance)
	{
		if (linkedEntrance != null)
		{
			return false;
		}
		linkedEntrance = entrance;
		UpdateTexture();
		return true;
	}
	private void UnlinkEntrance()
	{
		linkedEntrance = null;
		UpdateTexture();
	}
	public bool AddDecoupledEntrance(Entrance entrance)
	{
		if (decoupledEntrance != null)
		{
			return false;
		}
		decoupledEntrance = entrance;
		UpdateTexture();
		entrance.UpdateTexture();
		return true;
	}
	public void RemoveDecoupledEntrance()
	{
		decoupledEntrance = null;
		UpdateTexture();
	}

	private void ClearAll()
	{
		linkedEntrance = null;
		decoupledEntrance = null;
		isTrash = false;
		isDecoupledTrash = false;
		UpdateTexture();
	}
}


