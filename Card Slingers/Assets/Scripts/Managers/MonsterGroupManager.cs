using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MonsterGroupManager
{
    public List<MonsterController> _monsters;

    public MonsterGroupManager(List<MonsterController> monsters)
    {
        _monsters = new List<MonsterController>(monsters);

        for (int i = 0; i < _monsters.Count; i++)
        {
            _monsters[i].GroupManager = this;
            _monsters[i].Unit.onPermanentDestroyed += OnRemoveMonster;
        }
    }

    public void OnPlayerSpotted()
    {
        Debug.Log("Player Spotted!");

    }

    public void OnPlayerEngaged()
    {

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
    }
}
