using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelSelection : MonoBehaviour
{
    [SerializeField] private string levelName;
    [SerializeField] private bool _isUnlocked;
    [SerializeField] private Material highlightMat;

    private Material normalMat;
    private MeshRenderer meshRenderer;

    private void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        normalMat = meshRenderer.material;
    }

    private void OnMouseDown()
    {
        if (_isUnlocked) GameManager.OnLoadScene(levelName);
    }

    private void OnMouseEnter()
    {
        if (_isUnlocked) meshRenderer.material = highlightMat;
    }

    private void OnMouseExit()
    {
        meshRenderer.material = normalMat;
    }
}
