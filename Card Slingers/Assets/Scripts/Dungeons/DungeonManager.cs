using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonManager : MonoBehaviour
{
    #region - Singleton -
    public static DungeonManager instance;
    private void Awake()
    {
        instance = this;
    }
    #endregion

    private bool _dungeonIsReady = false;
    public bool DungeonIsReady
    {
        get => _dungeonIsReady;
        set
        {
            _dungeonIsReady = value;
        }
    }

    [SerializeField] private DungeonSize _dungeonSize; //For Testing Only
    [SerializeField] private DungeonGenerator _generator;
    [SerializeField] private Transform _playerChildren;
    [Space]
    [SerializeField] private DungeonPresets[] _dungeonPresets;
    [SerializeField] private List<EnemyGroupManager> _monsterGroups;
    [SerializeField] private LayerMask _layerMask;
    public LayerMask Mask => _layerMask;
    public DungeonGenerator Generator => _generator;

    public void CreateDungeon(Dungeons dungeon, int floor)
    {
        _dungeonIsReady = false;
        //Change this to take in the level and set number of rooms and combats accordingly
        _generator.BeginGeneration(_dungeonPresets[(int)dungeon], _dungeonSize);
    }

    public void SpawnPlayer(CommanderSO playerSO)
    {
        var player = playerSO.SpawnCommander(BattlefieldManager.instance.GetNode(Vector3.zero));
        
        var capsule = player.gameObject.AddComponent<CapsuleCollider>();
        var center = new Vector3(0, 1f, 0);
        capsule.center = center;
        capsule.height = 2;

        player.gameObject.AddComponent<PlayerController>();
        _playerChildren.SetParent(player.gameObject.transform);
    }

    public void AddGroups(List<EnemyGroupManager> monsterGroups)
    {
        _monsterGroups = new List<EnemyGroupManager>(monsterGroups);

        for (int i = 0; i < _monsterGroups.Count; i++)
        {
            _monsterGroups[i].onGroupEliminated += OnGroupEliminated;
        }
    }

    private void OnGroupEliminated(EnemyGroupManager group)
    {
        group.onGroupEliminated -= OnGroupEliminated;
        _monsterGroups.Remove(group);
    }

    public bool AllEncountersComplete()
    {
        if (_monsterGroups.Count == 0) return true;
        return false;
    }

    private void OnDestroy()
    {
        for (int i = 0; i < _monsterGroups.Count; i++)
        {
            _monsterGroups[i].onGroupEliminated -= OnGroupEliminated;
        }
    }

    //Criteria to leave
    // 1. Defeat all groups
    // 2. Defeat most groups
    // 3. Defeat most monsters
    // 4. Defeat boss
}
