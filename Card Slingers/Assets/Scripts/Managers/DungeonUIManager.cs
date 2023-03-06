using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

//NOTE: Split this all up into a central UI Manager and what is in here now put into a CombatHUD script

public class DungeonUIManager : MonoBehaviour
{
    #region - TESTING -
    public RectTransform testPanelRect;
    public Button TEST_PANEL_BUTTON, TEST_END_PHASE_BUTTON;

    private void Test()
    {
        TEST_PANEL_BUTTON.onClick.AddListener(delegate
        {
            if (testPanelRect.anchoredPosition != Vector2.zero) testPanelRect.anchoredPosition = Vector2.zero;
            else testPanelRect.anchoredPosition = new Vector2(-testPanelRect.sizeDelta.x, 0);
        });
        TEST_END_PHASE_BUTTON.onClick.AddListener(delegate
        {
            Debug.Log("TEST END PHASE");
            duelManager.OnCurrentPhaseFinished();
        });
    }
    #endregion

    public static DungeonUIManager instance;
    private void Awake()
    {
        instance = this;
    }

    private DuelManager duelManager;
    [SerializeField] private UI_CardDisplay _cardDisplay;

    private Vector2 closeOutPanelPos = new Vector2(0, -1000);

    [Header("Dungeon Victory")]
    [SerializeField] private RectTransform _victoryPanel;
    [SerializeField] private Button _returnToTownButton; //Returns player to town
    [SerializeField] private Button _continueExploringButton; //Allows player to continue exploring
    //Currently this has no purpose, but once all 

    [Header("Battle Victory")]
    [SerializeField] private RectTransform _battleSummaryPanel;
    [SerializeField] private Button _continueButton;

    [Header("Retreat")]
    [SerializeField] private RectTransform _retreatPanel;
    [SerializeField] private Button _retreatButton;
    [SerializeField] private Button _confirmRetreatButton;
    [SerializeField] private Button _cancelRetreatButton;
    private Coroutine _retreatPanelCoroutine;
    private bool _retreatPanelShown = false;

    [Header("Game Over")]
    [SerializeField] private RectTransform _gameOverPanel;
    [SerializeField] private Button _confirmGameOverButton;

    private void Start()
    {
        Test();

        duelManager = DuelManager.instance;
        duelManager.onPlayerVictory += OnPlayerVictory;
        duelManager.onPlayerDefeat += OnPlayerDefeat;

        //Dungeon Victory
        _victoryPanel.anchoredPosition = closeOutPanelPos;
        _returnToTownButton.onClick.AddListener(OnReturnToTown);
        //_continueExploringButton.onClick.AddListener();

        //Battle Victory
        _battleSummaryPanel.anchoredPosition = closeOutPanelPos;
        _continueButton.onClick.AddListener(delegate
        {
            StartCoroutine(LerpRectTransform(_battleSummaryPanel, closeOutPanelPos, 2f));
            duelManager.CloseOutMatch();
        });
        
        //Retreat
        _retreatPanel.anchoredPosition = closeOutPanelPos;
        _retreatButton.onClick.AddListener(ToggleRetreatPanel);
        _cancelRetreatButton.onClick.AddListener(ToggleRetreatPanel);
        _confirmRetreatButton.onClick.AddListener(OnReturnToTown);

        //Game Over
        _gameOverPanel.anchoredPosition = closeOutPanelPos;
        _confirmGameOverButton.onClick.AddListener(OnReturnToTown);
    }


    private void ToggleRetreatPanel() //Will also need to edit this so that if the completed the quest, then there's no loss in loot
    {
        _retreatPanelShown = !_retreatPanelShown;

        if (_retreatPanelCoroutine != null) StopCoroutine(_retreatPanelCoroutine);
        if (_retreatPanelShown) _retreatPanelCoroutine = StartCoroutine(LerpRectTransform(_retreatPanel, closeOutPanelPos));
        else _retreatPanelCoroutine = StartCoroutine(LerpRectTransform(_retreatPanel, Vector2.zero));
    }

    private void OnPlayerVictory()
    {
        if (DungeonManager.instance.AllEncountersComplete())
        {
            Debug.Log("All Encounters complete! Return to town.");
            StartCoroutine(LerpRectTransform(_victoryPanel, Vector2.zero, 2f));
        }
        else
        {
            StartCoroutine(LerpRectTransform(_battleSummaryPanel, Vector2.zero, 2f));
        }
    }

    private void OnPlayerDefeat()
    {
        StartCoroutine(LerpRectTransform(_gameOverPanel, Vector2.zero, 2f));
    }

    public void ShowCardDisplay(CardSO card)
    {
        _cardDisplay.ShowCardDisplay(card);
    }

    public void HideCardDisplay()
    {
        _cardDisplay.HideCardDisplay();
    }

    public IEnumerator LerpRectTransform(RectTransform rect, Vector3 endPos, float timeToMove = 0.5f)
    {
        float timeElapsed = 0;

        while (timeElapsed < timeToMove)
        {
            rect.anchoredPosition = Vector3.Lerp(rect.anchoredPosition, endPos, (timeElapsed / timeToMove));
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        rect.anchoredPosition = endPos;
    }

    private void OnReturnToTown()
    {
        GameManager.OnLoadScene("Town");
    }
}
