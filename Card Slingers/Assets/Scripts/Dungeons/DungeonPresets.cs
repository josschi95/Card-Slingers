using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Dungeon Preset", menuName = "Scriptable Objects/Dungeon Presets")]
public class DungeonPresets : ScriptableObject
{
    [SerializeField] private Dungeons _dungeon;

    [Header("Combat Encounters")]
    [SerializeField] private CombatEncounter[] _encounters;

    [Header("Room Prefabs")]
    [SerializeField] private DungeonRoom[] _roomPrefabs;
    [Tooltip("Up/Left, Up/Right, Down/Left, Down/Right")]
    [SerializeField] private IntermediaryNode[] _corners;

    [SerializeField] private DungeonRoom _startRoomPrefab;

    [SerializeField] private GameObject _hallwayVertical;
    [SerializeField] private GameObject _hallwayHorizontal;

    [Header("Obstacles")]
    [SerializeField] private Obstacle[] _obstacles;

    public Dungeons Dungeon => _dungeon;
    public CombatEncounter[] Encounters => _encounters;
    public DungeonRoom[] RoomPrefabs => _roomPrefabs;
    public IntermediaryNode[] Corners => _corners;
    public DungeonRoom StartRoomPrefab => _startRoomPrefab;
    public GameObject Hallway_Vert => _hallwayVertical;
    public GameObject Hallway_Horz => _hallwayHorizontal;
    public Obstacle[] Obstacles => _obstacles;
}
