using Godot;
using System;

public partial class CompanionAreaData : Resource
{
    [Export] public string name;
    [Export] public UIController.CompanionState companionState;
    [Export] public Entrance.EntranceType entranceType;
    [Export] public Vector2I location;
    [Export] public Vector2I entranceLoc;
    [Export] public Texture2D tex;
}