using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridCellDisplay : MonoBehaviour
{
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private Material normalMaterial;
    [SerializeField] private Material highlightedMaterial;

    private GridNode node;

    public void OnAssignNode(GridNode node)
    {
        this.node = node;
    }

    private void OnMouseEnter()
    {
        meshRenderer.material = highlightedMaterial;
    }

    private void OnMouseExit()
    {
        meshRenderer.material = normalMaterial;
    }

    private void OnMouseDown()
    {
        Debug.Log(node.x + "," + node.y);
        node.Grid.onNodeSelected?.Invoke(node);
    }
}
