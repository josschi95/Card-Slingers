using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TownUIManager : MonoBehaviour
{
    [SerializeField] private Button _levelSelectButton; //Brings the player to the level selection screen
    [SerializeField] private Button _questViewButton; //Brings up the Quest panel. Don't know what this will be yet.
    [SerializeField] private Button _settingsButton; //Brings up the Settings panel

    /*Include a display for the following
     * 
     * Current Commander
     * Current Loot amount
     * IDK
     */

    private void Start()
    {
        _levelSelectButton.onClick.AddListener(ContinueToLevelSelectScene);
        _settingsButton.onClick.AddListener(DisplaySettings);
    }

    private void ContinueToLevelSelectScene()
    {
        GameManager.OnLoadScene("Level Selection");
    }

    private void DisplaySettings()
    {
        Debug.Log("Add Settings Menu.");
    }


}
