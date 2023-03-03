using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelSelectionManager : MonoBehaviour
{
    public delegate void OnDungeonSelectedCallback();
    public OnDungeonSelectedCallback onDungeonSelected;

    [SerializeField] private LevelSelection[] _dungeons;
    [SerializeField] private Button _returnToTownButton;

    private Dungeons _currentlySelectedDungeon;
    [SerializeField] private GameObject _levelSelectionPanel;
    [SerializeField] private TMP_Text _dungeonNameText;
    [SerializeField] private Button _cancelDungeonButton;
    [SerializeField] private Button[] _dungeonFloorButtons;

    private void Start()
    {
        //On start, grab the current progress info from GameManager or whatever and set each dungeon to locked/Unlocked
        _returnToTownButton.onClick.AddListener(ReturnToTown);
        _cancelDungeonButton.onClick.AddListener(OnDungeonCancelled);

        for (int i = 0; i < _dungeons.Length; i++)
        {
            _dungeons[i].onLevelSelected += OnDungeonSelected;
            //the dungoen is unlocked if at least the first floor is available
            _dungeons[i].IsUnlocked = GameManager.instance.UnlockedDungeonLevels[i] > 0; 
        }

        for (int i = 0; i < _dungeonFloorButtons.Length; i++)
        {
            int floor = i;
            _dungeonFloorButtons[floor].onClick.AddListener(delegate 
            {
                OnDungeonFloorSelected(floor);
            });
        }
    }

    private void ReturnToTown()
    {
        GameManager.OnLoadScene("Town");
    }

    private void OnDungeonCancelled()
    {
        _levelSelectionPanel.SetActive(false);
        _currentlySelectedDungeon = Dungeons.Catacombs; //Or whatever the first is
    }

    private void OnDungeonSelected(Dungeons dungeon)
    {
        _currentlySelectedDungeon = dungeon;

        _levelSelectionPanel.SetActive(true);
        
        string dungeonName = dungeon.ToString();
        dungeonName.Replace("_", " ");
        _dungeonNameText.text = dungeonName;

        int maxLevel = GameManager.instance.UnlockedDungeonLevels[(int)dungeon] - 1;
        for (int i = 0; i < _dungeonFloorButtons.Length; i++)
        {
            if (i <= maxLevel) _dungeonFloorButtons[i].interactable = true;
            else _dungeonFloorButtons[i].interactable = false;
        }
    }

    private void OnDungeonFloorSelected(int floor)
    {
        GameManager.LoadDungeon(_currentlySelectedDungeon, floor);
    }
}