using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Structure-type objects on the field which block movement.
/// </summary>
public class Obstacle : MonoBehaviour
{
    public GridNode Node { get; private set; }
    [SerializeField] private int _maxHealth;
    [SerializeField] private int _currentHealth;

    public void OnOccupyNode(GridNode node)
    {
        //var node = BattlefieldManager.instance.GetNode(transform.position);
        if (node == null || node.transform.position != transform.position) Debug.LogError("Obstacle placed incorrectly.");

        Node = node;
        node.Obstacle = this;
    }

    private void AbandonNode()
    {
        Node.Obstacle = null;
    }

    public void OnObstacleRemoved()
    {
        AbandonNode();
        Destroy(gameObject);
    }
}
