using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class TownUIManager : MonoBehaviour
{
    [SerializeField] private TownManager _townManager;
    [SerializeField] private Image _fade;

    [SerializeField] private Button _townViewButton;
    [SerializeField] private Button _levelSelectButton; //Brings the player to the level selection screen
    [SerializeField] private Button _questViewButton; //Brings up the Quest panel. Don't know what this will be yet.
    [SerializeField] private Button _settingsButton; //Brings up the Settings panel

    [Space]

    [SerializeField] private GameObject _townViewPanel;
    [SerializeField] private GameObject _merchantViewPanel;
    [SerializeField] private GameObject _armorerViewPanel;
    [SerializeField] private GameObject _tavernViewPanel;
    [SerializeField] private GameObject _generalLocationPanel;

    [SerializeField] private TMP_Text _locationText;
    [SerializeField] private TMP_Text _playerGoldText;

    /*Include a display for the following
     * 
     * Current Commander
     * Current Loot amount
     * IDK
     */

    private void Start()
    {
        _townManager.onBeginSwitchView += OnSwitchView;
        _townManager.onLocationViewSet += ToggleTownPanels;

        _townViewButton.onClick.AddListener(_townManager.ReturnToTownView);

        _levelSelectButton.onClick.AddListener(ContinueToLevelSelectScene);
        _questViewButton.onClick.AddListener(DisplayQuests);
        _settingsButton.onClick.AddListener(DisplaySettings);
    }

    private void ToggleTownPanels(Location location)
    {
        _locationText.text = location.ToString();
        _playerGoldText.text = GameManager.instance.PlayerGold.ToString();

        switch (location)
        {
            case Location.Square:
                _townViewPanel.SetActive(true);
                _merchantViewPanel.SetActive(false);
                _armorerViewPanel.SetActive(false);
                _tavernViewPanel.SetActive(false);
                _generalLocationPanel.SetActive(false);
                break;
            case Location.Merchant:
                _townViewPanel.SetActive(false);
                _merchantViewPanel.SetActive(true);
                _generalLocationPanel.SetActive(true);
                break;
            case Location.Armorer:
                _townViewPanel.SetActive(false);
                _armorerViewPanel.SetActive(true);
                _generalLocationPanel.SetActive(true);
                break;
            case Location.Tavern:
                _townViewPanel.SetActive(false);
                _tavernViewPanel.SetActive(true);
                _generalLocationPanel.SetActive(true);
                break;
        }
    }

    private void ContinueToLevelSelectScene()
    {
        GameManager.OnLoadScene("Level Selection");
    }

    private void DisplayQuests()
    {
        Debug.Log("Add Quests Menu.");
    }

    private void DisplaySettings()
    {
        Debug.Log("Add Settings Menu.");
    }

    private void OnSwitchView(float timeToFade)
    {
        StartCoroutine(FadeToBlack(timeToFade));
    }

    private IEnumerator FadeToBlack(float timeToFade)
    {
        float t = 0;

        while (t < timeToFade)
        {
            _fade.color = Color.Lerp(Color.clear, Color.black, t / timeToFade);
            t += Time.deltaTime;
            yield return null;
        }
        _fade.color = Color.black;

        yield return new WaitForSeconds(timeToFade * 0.5f);
        t = 0;

        while (t < timeToFade)
        {
            _fade.color = Color.Lerp(Color.black, Color.clear, t / timeToFade);
            t += Time.deltaTime;
            yield return null;
        }
        _fade.color = Color.clear;
    }
}
