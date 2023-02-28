using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatGenerator : MonoBehaviour
{
    [SerializeField] private CombatEncounter[] _encounters;

    [SerializeField] private int minCombats;
    [SerializeField] private int maxCombats;

    public bool isComplete { get; private set; }

    public void GenerateCombats(DungeonRoom[] dungeonRooms)
    {
        StartCoroutine(PlaceCombats(dungeonRooms));
    }

    private IEnumerator PlaceCombats(DungeonRoom[] dungeonRooms)
    {
        isComplete = false;

        int combats = Random.Range(minCombats, maxCombats + 1);
        combats = Mathf.Clamp(combats, 0, Mathf.RoundToInt((dungeonRooms.Length - 1) * 0.65f));

        while (combats > 0)
        {
            var room = dungeonRooms[Random.Range(1, dungeonRooms.Length)];
            if (room.Encounter != null) continue;

            room.Encounter = _encounters[Random.Range(0, _encounters.Length)];
            DrawDebugBox(room.Transform.position + Vector3.up * 3f, Quaternion.identity, new Vector3(room.RoomDimensions.x + 1, 6f, room.RoomDimensions.y + 1), Color.yellow);

            combats--;
            yield return null;
        }

        isComplete = true;
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
