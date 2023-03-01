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

    public int x { get; private set; }
    public int z { get; private set; }

    public void OnInitialPlacement(int x, int z)
    {
        this.x = x;
        this.z = z;
    }

    public void OnOccupyNode(GridNode node)
    {
        Node = node;
    }
}
