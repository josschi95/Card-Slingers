using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelSelection : MonoBehaviour
{
    public delegate void OnLevelSelectedCallback(Dungeons dungeon);
    public OnLevelSelectedCallback onLevelSelected;

    [SerializeField] private Dungeons _dungeon;
    [SerializeField] private GameObject _highlightEffect;
    [SerializeField] private bool _isUnlocked;
    public bool IsUnlocked
    {
        get => _isUnlocked;
        set
        {
            _isUnlocked = value;
        }
    }

    private bool _highlightLocked;

    private void Start()
    {
        //Check with gameManager to see if it is unlocked
    }

    private void OnMouseDown()
    {
        if (_isUnlocked) OnDungeonSelected();
    }

    private void OnMouseEnter()
    {
        if (_isUnlocked) _highlightEffect.SetActive(true);
    }

    private void OnMouseExit()
    {
        if (!_highlightLocked) _highlightEffect.SetActive(false);
    }

    private void OnDungeonSelected()
    {
        onLevelSelected?.Invoke(_dungeon);
        _highlightLocked = true;
    }

    public void OnDungeonDeselected()
    {
        _highlightLocked = false;
        _highlightEffect.SetActive(false);
    }
}
