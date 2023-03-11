using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterController : MonoBehaviour
{
    private Card_Unit _unit;
    public Card_Unit Unit
    {
        get => _unit;
        set
        {
            _unit = value;
        }
    }

    private List<TargetPriority> priorityList;

    private void Awake()
    {
        priorityList = new List<TargetPriority>();
    }

    public void AssignCard(Card_Unit unit)
    {
        _unit = unit;
    }

    //Prioritize targets based on multiple variables before selecting an action
    public void PrioritizeTargets()
    {
        priorityList.Clear(); //I could probably also just loop through and check if any perms are null and remove those
        //then just add any new perms, rather than clearing the list every round... we'll see

        foreach (Card_Permanent permanent in DuelManager.instance.Player_Commander.CardsOnField)
        {
            if (permanent is Card_Trap) continue; //don't know about these

            int value = 0;

            value -= DuelManager.instance.Battlefield.GetDistanceInNodes(_unit.Node, permanent.Node) * 2; //targets further away are valued less

            if (permanent == DuelManager.instance.Player_Commander.CommanderCard) value += 10; //higher value on commander

            if (permanent is Card_Structure structure)
            {
                if (structure.Defense >= _unit.Attack) continue; //No point in attacking something they can't damage
                //Can later factor in offensive ability damage and if (unit.CanUseAbility)

                value -= 10; //prioritize units over structures
                value += _unit.Attack - (structure.CurrentHealth + structure.Defense); //value targets with lower health and defense
                if (structure.CurrentHealth + structure.Defense <= _unit.Attack) value += 5; //can destroy it
            }
            else if (permanent is Card_Unit summon)
            {
                if (summon.Defense >= _unit.Attack) continue; //Same as above
                //Can later factor in offensive ability damage and if (unit.CanUseAbility)

                value += _unit.Attack - (summon.CurrentHealth + summon.Defense); //value targets with lower health and defense
                if (summon.CurrentHealth + summon.Defense <= _unit.Attack) value += 10; //can destroy it
                if (summon.CanUseAbility) value += 5; //Change this later to if it has an ability at all
            }

            //Add new target to list. If it's priority is higher than an existing one, place it at that index
            var newTarget = new TargetPriority(permanent, value);
            
            priorityList.Add(newTarget);
            for (int i = 0; i < priorityList.Count; i++)
            {
                if (newTarget.priority > priorityList[i].priority)
                {
                    priorityList.Remove(newTarget);
                    priorityList.Insert(i, newTarget);
                    break;
                }
            }
        }
    }

    //Select an action available to the unit based on positions
    public void SelectAction()
    {
        if (!_unit.CanAct)
        {
            Debug.LogWarning("Unit cannot act. Skipping.");
            return;
        }

        var targets = DuelManager.instance.Battlefield.FindTargetableNodes(_unit, _unit.Range);

        //Use ability if able and there is a valid target within range
        if (_unit.CanUseAbility) //will ahve to figure out how to delegate this
        {
            //use ability
            Debug.LogWarning("Using ability.");
            return;
        }

        //Attack a target if able and there is a valid target within range
        if (!_unit.HasActed && targets.Count > 0) //Attack the nearest target
        {
            //loop through available targets in order of highest priority to lowest
            for (int i = 0; i < priorityList.Count; i++)
            {
                if (targets.Contains(priorityList[i].target.Node))
                {
                    DuelManager.instance.OnAttackActionConfirmed(_unit, priorityList[i].target.Node);
                    return;
                }
            }
        }

        //Else move towards the highest priority target
        if (_unit.MovesLeft > 0) //Unit can move at least one space
        {
            //Move towards highest priority target
            for (int i = 0; i < priorityList.Count; i++)
            {
                var path = DuelManager.instance.Battlefield.FindNodePath(_unit, priorityList[i].target.Node, true);
                if (path == null) continue; //There is no path to the target, check the next one

                for (int p = path.Count - 1; p >= 0; p--)
                {
                    if (p > _unit.MovesLeft) path.RemoveAt(p);
                    else //able to reach these nodes
                    {
                        //Find the furthest node that can be occupied
                        if (path[p].CanBeOccupied(_unit)) break;
                        else path.RemoveAt(p);
                    }
                }
                if (path.Count <= 1) continue; //path only contains start node or no nodes
                DuelManager.instance.OnMoveActionConfirmed(_unit, path[path.Count - 1]);
                break;
            }
        }
    }
}

[System.Serializable]
public struct TargetPriority
{
    public Card_Permanent target;
    public int priority;

    public TargetPriority(Card_Permanent target, int priority)
    {
        this.target = target;
        this.priority = priority;
    }
}
