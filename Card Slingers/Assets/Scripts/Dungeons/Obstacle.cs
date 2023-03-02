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

    public void OnOccupyNode()
    {
        var colls = Physics.OverlapSphere(transform.position, 0.5f);
        var node = colls[0].GetComponent<GridNode>();

        if (node == null || node.transform.position != transform.position) Debug.LogWarning("Fuck off.");

        Node = node;
        node.Obstacle = this;
    }
}
