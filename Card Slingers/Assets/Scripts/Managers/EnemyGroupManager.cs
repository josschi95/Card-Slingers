using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyGroupManager
{
    public delegate void OnGroupEliminatedCallback(EnemyGroupManager group);
    public OnGroupEliminatedCallback onGroupEliminated;

    private OpponentCommander _commander;
    public OpponentCommander Commander => _commander;

    private List<MonsterController> _monsters;
    public List<MonsterController> Monsters => _monsters;

    private bool _hasEngaged = false;

    public EnemyGroupManager(OpponentCommander commander, List<MonsterController> monsters)
    {
        _commander = commander;

        _monsters = new List<MonsterController>(monsters);

        if (_commander != null) _commander.CommanderCard.onPermanentDestroyed += OnRemoveMonster;

        for (int i = 0; i < _monsters.Count; i++)
        {
            _monsters[i].GroupManager = this;
            _monsters[i].Unit.onPermanentDestroyed += OnRemoveMonster;
        }
    }

    //An enemy within this group has spotted the player
    public void OnPlayerSpotted()
    {
        if (_hasEngaged) return;

        Debug.Log("Player Spotted!");

        //Alert all enemies within the group of the player

        //Begin moving towards the player at a pace of 1 node for every node the player moves

        //For right now, immediately trigger combat
        OnPlayerEngaged();
    }

    //An enemy within this group has gotten within engagement range of the player, initiate combat
    public void OnPlayerEngaged()
    {
        if (_hasEngaged) return;
        _hasEngaged = true;

        if(_commander != null)
        {
            _commander.CommanderCard.Summon.FaceTargetCoroutine(PlayerController.instance.transform.position);
        }
        for (int i = 0; i < _monsters.Count; i++)
        {
            _monsters[i].IsInCombat = true;
            _monsters[i].Unit.Summon.FaceTargetCoroutine(PlayerController.instance.transform.position);
            _monsters[i].Unit.SubscribeToEvents();
        }

        //Initiate combat through the Duel Manager
        DuelManager.instance.onCombatBegin?.Invoke(this);
    }

    private void OnRemoveMonster(Card_Permanent monster)
    {
        for (int i = _monsters.Count - 1; i >= 0; i--)
        {
            if (_monsters[i].Unit == monster)
            {
                _monsters[i].Unit.onPermanentDestroyed -= OnRemoveMonster;
                _monsters.RemoveAt(i);
                break;
            }
        }

        if (_monsters.Count == 0) onGroupEliminated?.Invoke(this);
    }
}
