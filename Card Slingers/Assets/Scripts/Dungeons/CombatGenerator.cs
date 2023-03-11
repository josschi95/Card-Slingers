using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatGenerator : MonoBehaviour
{
    private List<CombatEncounter> encounterList;
    private List<MonsterGroupManager> _monsterGroups;
    private int[] monsterCount = { 2, 3, 3, 4, 4, 4, 4, 5, 5, 5, 6, 6 };

    public IEnumerator PlaceCombats(CombatEncounter[] encounters, DungeonRoom[] dungeonRooms, int combats)
    {
        encounterList = new List<CombatEncounter>();
        _monsterGroups = new List<MonsterGroupManager>();

        var availableRooms = new List<DungeonRoom>(dungeonRooms);
        availableRooms.RemoveAt(0); //Get rid of the starting room
        combats = Mathf.Clamp(combats, 0, availableRooms.Count); //Cannot have more combats than there are rooms

        int allowedAttempts = combats + 5;
        while (combats > 0 && allowedAttempts > 0) //Exits while loop if either combats or attempts run out
        {
            allowedAttempts--; //Decrease attempts at start
            if (availableRooms.Count <= 0) break; //all available rooms have been filled with a combat

            var room = availableRooms[Random.Range(0, availableRooms.Count)];
            if (room.Encounter != null) continue; //This really shouldn't happen... but to be sure

            var encounter = encounters[Random.Range(0, encounters.Length)];

            room.Encounter = encounter; //Set room encounter
            encounterList.Add(encounter); //Add to list of generated encounters
            availableRooms.Remove(room); //Remove room from list of rooms to select from
            combats--;

            if (encounter is MonsterEncounter monsterEncounter) SpawnEnemies(monsterEncounter, room);

            DrawDebugBox(room.Transform.position + Vector3.up * 3f, Quaternion.identity, new Vector3(room.RoomDimensions.x + 1, 6f, room.RoomDimensions.y + 1), Color.yellow);

            yield return null;
        }

        if (combats > 0) Debug.LogWarning("Was unable to place all combat before allowed attempts ran out.");

        DungeonManager.instance.SetEncounters(encounterList);
        DungeonManager.instance.AddGroups(_monsterGroups);
    }

    private void SpawnEnemies(MonsterEncounter encounter, DungeonRoom room)
    {
        int num = monsterCount[Random.Range(0, monsterCount.Length)];
        var nodes = BattlefieldManager.instance.GetNodesInRoom(room);
        var monsterList = new List<MonsterController>();

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

            var controller = summon.gameObject.AddComponent<MonsterController>();
            controller.AssignCard(newMonster);
            monsterList.Add(controller);
        }

        var newGroup = new MonsterGroupManager(monsterList);
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

    private void DrawDebugBox(Vector3 pos, Quaternion rot, Vector3 scale, Color c, float duration = 15f)
    {
        // create matrix
        Matrix4x4 m = new Matrix4x4();
        m.SetTRS(pos, rot, scale);

        var point1 = m.MultiplyPoint(new Vector3(-0.5f, -0.5f, 0.5f));
        var point2 = m.MultiplyPoint(new Vector3(0.5f, -0.5f, 0.5f));
        var point3 = m.MultiplyPoint(new Vector3(0.5f, -0.5f, -0.5f));
        var point4 = m.MultiplyPoint(new Vector3(-0.5f, -0.5f, -0.5f));

        var point5 = m.MultiplyPoint(new Vector3(-0.5f, 0.5f, 0.5f));
        var point6 = m.MultiplyPoint(new Vector3(0.5f, 0.5f, 0.5f));
        var point7 = m.MultiplyPoint(new Vector3(0.5f, 0.5f, -0.5f));
        var point8 = m.MultiplyPoint(new Vector3(-0.5f, 0.5f, -0.5f));

        Debug.DrawLine(point1, point2, c, duration);
        Debug.DrawLine(point2, point3, c, duration);
        Debug.DrawLine(point3, point4, c, duration);
        Debug.DrawLine(point4, point1, c, duration);

        Debug.DrawLine(point5, point6, c, duration);
        Debug.DrawLine(point6, point7, c, duration);
        Debug.DrawLine(point7, point8, c, duration);
        Debug.DrawLine(point8, point5, c, duration);

        Debug.DrawLine(point1, point5, c, duration);
        Debug.DrawLine(point2, point6, c, duration);
        Debug.DrawLine(point3, point7, c, duration);
        Debug.DrawLine(point4, point8, c, duration);
    }
}
