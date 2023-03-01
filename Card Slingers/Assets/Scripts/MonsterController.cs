using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterController : MonoBehaviour
{
    public Card_Unit unit { get; private set; }
    private List<TargetPriority> priorityList;

    private void Awake()
    {
        unit = GetComponent<Card_Unit>();
        unit.Commander.onNewPhase += OnPhaseChange;
        priorityList = new List<TargetPriority>();
    }

    private void OnDestroy()
    {
        unit.Commander.onNewPhase -= OnPhaseChange;
    }

    private void OnPhaseChange(Phase phase)
    {
        if (phase == Phase.Begin) PrioritizeTargets();
    }

    private void PrioritizeTargets()
    {
        priorityList.Clear(); //I could probably also just loop through and check if any perms are null and remove those
        //then just add any new perms, rather than clearing the list every round... we'll see

        foreach (Card_Permanent permanent in DuelManager.instance.Player_Commander.CardsOnField)
        {
            if (permanent is Card_Trap) continue; //don't know about these

            int value = 0;

            value -= DuelManager.instance.Battlefield.GetDistanceInNodes(unit.Node, permanent.Node) * 2; //targets further away are valued less

            if (permanent == DuelManager.instance.Player_Commander.CommanderCard) value += 10; //higher value on commander

            if (permanent is Card_Structure structure)
            {
                if (structure.Defense >= unit.Damage) continue; //No point in attacking something they can't damage
                //Can later factor in offensive ability damage and if (unit.CanUseAbility)

                value -= 10; //prioritize units over structures
                value += unit.Damage - (structure.CurrentHealth + structure.Defense); //value targets with lower health and defense
                if (structure.CurrentHealth + structure.Defense <= unit.Damage) value += 5; //can destroy it
            }
            else if (permanent is Card_Unit summon)
            {
                if (summon.Defense >= unit.Damage) continue; //Same as above
                //Can later factor in offensive ability damage and if (unit.CanUseAbility)

                value += unit.Damage - (summon.CurrentHealth + summon.Defense); //value targets with lower health and defense
                if (summon.CurrentHealth + summon.Defense <= unit.Damage) value += 10; //can destroy it
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

    public void SelectAction()
    {
        if (!unit.CanAct) return;

        //check all targets within range, and prioritize higher value targets
        //but also prioritize action over nothing
        //If unit CAN attack, attack, else, move towards highest value target

        var targets = DuelManager.instance.Battlefield.FindTargetableNodes(unit, unit.Range);

        if (unit.CanUseAbility) //will ahve to figure out how to delegate this
        {
            //use ability
            Debug.Log("using ability");
        }
        else if (targets.Count > 0) //Attack the nearest target
        {
            //loop through available targets in order of highest priority to lowest
            //If can attack any, do so 
            for (int i = 0; i < priorityList.Count; i++)
            {
                if (targets.Contains(priorityList[i].target.Node))
                {
                    DuelManager.instance.OnAttackActionConfirmed(unit, priorityList[i].target.Node);
                    break;
                }
            }
        }
        else
        {
            //Move towards highest priority target
            for (int i = 0; i < priorityList.Count; i++)
            {
                var path = DuelManager.instance.Battlefield.FindNodePath(unit, priorityList[i].target.Node, true);
                if (path == null) continue; //There is no path to the target, check the next one

                for (int p = path.Count - 1; p >= 0; p--)
                {
                    if (p > unit.Speed) path.RemoveAt(p);
                    else //able to reach these nodes
                    {
                        //Find the furthest node that can be occupied
                        if (path[p].CanBeOccupied(unit)) break;
                        else path.RemoveAt(p);
                    }
                }
                if (path.Count == 1) continue; //path only contains start node

                DuelManager.instance.OnMoveActionConfirmed(unit, path[path.Count - 1]);
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
