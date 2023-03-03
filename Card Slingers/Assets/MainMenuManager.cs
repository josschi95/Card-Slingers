using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private Button _continueButton, _newGameButton, _settingsButton, _creditsButton, _quitButton;

    private void Start()
    {
        _continueButton.onClick.AddListener(OnContinue);
        _newGameButton.onClick.AddListener(OnNewGame);
        _settingsButton.onClick.AddListener(OnSettings);
        _creditsButton.onClick.AddListener(OnCredits);
        _quitButton.onClick.AddListener(OnQuit);
    }

    private void OnContinue()
    {
        //Load most recent save file
        GameManager.OnLoadScene("Town");
    }

    private void OnNewGame()
    {
        //Load new game
        GameManager.OnLoadScene("Town");
    }

    private void OnSettings()
    {
        //Add settings menu
    }

    private void OnCredits()
    {
        //Add Credits menu
    }

    private void OnQuit()
    {
        Application.Quit();
    }
}
