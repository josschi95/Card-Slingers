using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatGenerator : MonoBehaviour
{
    private List<EnemyGroupManager> _monsterGroups;
    private int[] monsterCount = { 3, 3, 4, 4, 4, 4, 5, 5, 5, 6, 6 };

    public IEnumerator PlaceCombats(EnemyGroupPreset[] encounters, DungeonRoom[] dungeonRooms, int numCombats)
    {
        _monsterGroups = new List<EnemyGroupManager>();

        var availableRooms = new List<DungeonRoom>(dungeonRooms);
        availableRooms.RemoveAt(0); //Get rid of the starting room
        numCombats = Mathf.Clamp(numCombats, 0, availableRooms.Count); //Cannot have more combats than there are rooms

        int allowedAttempts = numCombats + 5;

        while (numCombats > 0 && allowedAttempts > 0) //Exits while loop if either combats or attempts run out
        {
            allowedAttempts--; //Decrease attempts at start
            if (availableRooms.Count <= 0) break; //all available rooms have been filled with a combat

            var room = availableRooms[Random.Range(0, availableRooms.Count)];
            if (room.ContainsEnemies) continue; //This really shouldn't happen... but to be sure

            var encounter = encounters[Random.Range(0, encounters.Length)];
            if (encounter is MonsterGroupPreset monsterEncounter) SpawnEnemies(monsterEncounter, room);
            else if (encounter is CommanderEncounterPreset commanderEncounter) SpawnCommanderGroup(commanderEncounter, room);
            else Debug.LogError("Passing invalid encounter!");

            room.ContainsEnemies = true; //Set room encounter
            availableRooms.Remove(room); //Remove room from list of rooms to select from
            numCombats--;

            yield return null;
        }

        DungeonManager.instance.AddGroups(_monsterGroups);
    }

    private void SpawnEnemies(MonsterGroupPreset encounter, DungeonRoom room)
    {
        int num = monsterCount[Random.Range(0, monsterCount.Length)];
        var nodes = BattlefieldManager.instance.GetNodesInRoom(room);
        var monsterList = new List<MonsterController>();
        var entrances = OpenEntrances(room);

        for (int i = 0; i < num; i++)
        {
            var node = GetNodeToSummon(nodes);
            if (node == null) break; //There are no summonable nodes in the room

            //Grab a random card from the pool
            var card = encounter.MonsterPool[Random.Range(0, encounter.MonsterPool.Length)];

            Card_Unit newMonster = new Card_Unit(card, false);
            newMonster.SetCardLocation(CardLocation.OnField);
            
            var info = newMonster.CardInfo as PermanentSO;
            var summon = Instantiate(info.Prefab, node.Transform.position, Quaternion.identity);
            newMonster.OnSummoned(summon, node);
            summon.FaceTargetCoroutine(entrances[Random.Range(0, entrances.Count)]);

            var controller = summon.gameObject.AddComponent<MonsterController>();
            controller.AssignCard(newMonster);
            monsterList.Add(controller);
        }

        var newGroup = new EnemyGroupManager(null, monsterList);
        _monsterGroups.Add(newGroup);
    }

    private void SpawnCommanderGroup(CommanderEncounterPreset commanderEncounter, DungeonRoom room)
    {
        var nodes = BattlefieldManager.instance.GetNodesInRoom(room);
        var monsterList = new List<MonsterController>();
        var entrances = OpenEntrances(room);

        var centerNode = BattlefieldManager.instance.GetNode(room.Transform.position);
        if (centerNode.Occupant != null) throw new UnityException("Placing commander encounter in occupied room!");
        else if (centerNode.Obstacle != null) centerNode.Obstacle.OnObstacleRemoved(); //Destroy obstacle

        var commander = commanderEncounter.Commander.SpawnCommander(centerNode);
        commander.CommanderCard.Summon.FaceTargetCoroutine(entrances[Random.Range(0, entrances.Count)]);

        for (int i = 0; i < commanderEncounter.InitialSummons.Length; i++)
        {
            var node = GetNodeToSummon(nodes);
            if (node == null) break; //There are no summonable nodes in the room

            //Grab a random card from the pool
            var card = commanderEncounter.InitialSummons[Random.Range(0, commanderEncounter.InitialSummons.Length)];

            Card_Unit newMonster = new Card_Unit(card, false);
            newMonster.SetCardLocation(CardLocation.OnField);

            var info = newMonster.CardInfo as PermanentSO;
            var monsterSummon = Instantiate(info.Prefab, node.Transform.position, Quaternion.identity);
            newMonster.OnSummoned(monsterSummon, node);
            monsterSummon.FaceTargetCoroutine(entrances[Random.Range(0, entrances.Count)]);

            var controller = monsterSummon.gameObject.AddComponent<MonsterController>();
            controller.AssignCard(newMonster);
            monsterList.Add(controller);
        }

        var newGroup = new EnemyGroupManager(commander as OpponentCommander, monsterList);
        _monsterGroups.Add(newGroup);
    }

    private GridNode GetNodeToSummon(List<GridNode> nodes)
    {
        for (int i = nodes.Count - 1; i >= 0; i--)
        {
            if (nodes[i].Obstacle != null) nodes.RemoveAt(i);
            else if (nodes[i].Occupant != null) nodes.RemoveAt(i);
        }

        if (nodes.Count == 0) return null;

        return nodes[Random.Range(0, nodes.Count)];
    }

    private List<Vector3> OpenEntrances(DungeonRoom room)
    {
        var list = new List<Vector3>();

        for (int i = 0; i < room.Nodes.Length; i++)
        {
            if (room.Nodes[i].ConnectedNode != null) list.Add(room.Nodes[i].Point.position);
        }

        return list;
    }
}
