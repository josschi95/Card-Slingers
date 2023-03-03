using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelSelection : MonoBehaviour
{
    [SerializeField] private string levelName;
    [SerializeField] private GameObject _highlightEffect;
    [SerializeField] private bool _isUnlocked;

    private void Start()
    {
        //Check with gameManager to see if it is unlocked
    }

    private void OnMouseDown()
    {
        if (_isUnlocked) GameManager.OnLoadScene(levelName);
    }

    private void OnMouseEnter()
    {
        if (_isUnlocked) _highlightEffect.SetActive(true);
    }

    private void OnMouseExit()
    {
        _highlightEffect.SetActive(false);
    }
}
