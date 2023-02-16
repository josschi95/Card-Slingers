using System;
using UnityEngine;
using UnityEngine.Pool;

//I should end up moving this to some sort of DungeonManager script that will handle all of the battlefields in the dungeon, because placing it on each battlefield would be pointless
public class NodePool : MonoBehaviour
{
    [SerializeField] private GridNode _nodePrefab;
    ObjectPool<GridNode> _nodePool;

    private void Awake()
    {
        _nodePool = new ObjectPool<GridNode>(CreateNode, OnTakeNodeFromPool, OnReturnNodeToPool);
    }

    private void OnTakeNodeFromPool(GridNode node)
    {
        node.gameObject.SetActive(true);
    }

    private void OnReturnNodeToPool(GridNode node)
    {
        node.gameObject.SetActive(false);
    }


    private GridNode CreateNode()
    {
        var node = Instantiate(_nodePrefab);
        node.SetPool(_nodePool);
        return node;
    }
}
