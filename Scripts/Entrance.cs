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
				{"isTrash", isTrash.ToString()}
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
				isTrash = bool.Parse(dict["isTrash"])
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
			isTrash = isTrash
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
				UIController.Instance.TrashEntrance(this);
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
		// right click - unlink
		else if (button.ButtonIndex == MouseButton.Right)
		{
			if (!isTrash)
			{
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
			}
			else
			{
				isTrash = false;
				if (!isPulsing)
				{
					TextureNormal = decoupledEntrance == null ? GameSelector.Instance.unlinkedTexture : GameSelector.Instance.decoupledTexture;
				}
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
		TextureNormal = isTrash ? GameSelector.Instance.trashTexture : linkedEntrance != null ? linkedTexture : decoupledEntrance != null ? GameSelector.Instance.decoupledTexture : unlinkedTexture;
	}
	
	public void TrashSelf()
	{
		TextureNormal = GameSelector.Instance.trashTexture;
		isTrash = true;
		UIController.Instance.ClearSelectedEntranceIfEqual(this);
	}
	
	public bool LinkEntrance(Entrance entrance)
	{
		if (linkedEntrance != null)
		{
			return false;
		}
		linkedEntrance = entrance;
		if (!isPulsing)
		{
			TextureNormal = GameSelector.Instance.linkedTexture;
		}
		return true;
	}
	private void UnlinkEntrance()
	{
		linkedEntrance = null;
		if (!isPulsing)
		{
			TextureNormal = decoupledEntrance == null ? GameSelector.Instance.unlinkedTexture : GameSelector.Instance.decoupledTexture;
		}
	}
	public bool AddDecoupledEntrance(Entrance entrance)
	{
		if (decoupledEntrance != null)
		{
			return false;
		}
		decoupledEntrance = entrance;
		if (linkedEntrance == null && !isTrash && !isPulsing)
		{
			TextureNormal = GameSelector.Instance.decoupledTexture;
		}
		return true;
	}
	public void RemoveDecoupledEntrance()
	{
		decoupledEntrance = null;
		if (isPulsing)
		{
			return;
		}
		TextureNormal = linkedEntrance == null ? GameSelector.Instance.unlinkedTexture : GameSelector.Instance.linkedTexture;
	}

	private void ClearAll()
	{
		linkedEntrance = null;
		decoupledEntrance = null;
		isTrash = false;
		TextureNormal = GameSelector.Instance.unlinkedTexture;
	}
}


