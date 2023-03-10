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
            //duelManager.OnCurrentPhaseFinished();
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

    [Header("Toolbar")]
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _retreatButton;
    [SerializeField] private Button _inventoryButton;
    [SerializeField] private TMP_Text _goldCountText;
    [SerializeField] private RectTransform _goldGainParent;
    [SerializeField] private TMP_Text _goldGainText;
    private Vector2 _goldStartPos;
    private Coroutine _goldGainCoroutine;

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

        _goldStartPos = _goldGainParent.anchoredPosition;
        _goldGainParent.gameObject.SetActive(false);
        _goldCountText.text = "0";

        //Dungeon Victory
        _victoryPanel.anchoredPosition = closeOutPanelPos;
        _victoryPanel.gameObject.SetActive(false);
        _returnToTownButton.onClick.AddListener(GameManager.OnConfirmTempGold);
        _returnToTownButton.onClick.AddListener(OnReturnToTown);
        //_continueExploringButton.onClick.AddListener();

        //Battle Victory
        _battleSummaryPanel.anchoredPosition = closeOutPanelPos;
        _battleSummaryPanel.gameObject.SetActive(false);
        _continueButton.onClick.AddListener(delegate
        {
            StartCoroutine(LerpRectTransform(_battleSummaryPanel, closeOutPanelPos));
            duelManager.CloseOutMatch();
        });
        
        //Retreat
        _retreatPanel.anchoredPosition = closeOutPanelPos;
        _retreatPanel.gameObject.SetActive(false);
        _retreatButton.onClick.AddListener(ToggleRetreatPanel);
        _cancelRetreatButton.onClick.AddListener(ToggleRetreatPanel);
        _confirmRetreatButton.onClick.AddListener(OnReturnToTown);

        //Game Over
        _gameOverPanel.anchoredPosition = closeOutPanelPos;
        _gameOverPanel.gameObject.SetActive(false);
        _confirmGameOverButton.onClick.AddListener(OnReturnToTown);

        GameManager.instance.onTest += delegate
        {
            UpdateGoldDisplay(30);
        };
    }

    public void UpdateGoldDisplay(int gold)
    {
        if (_goldGainCoroutine != null) StopCoroutine(_goldGainCoroutine);
        _goldGainCoroutine = StartCoroutine(GoldGain(gold));
        _goldCountText.text = GameManager.instance.PlayerTempGold.ToString();
    }

    private IEnumerator GoldGain(int amount)
    {
        _goldGainText.text = "+" + amount.ToString();
        _goldGainParent.gameObject.SetActive(true);
        _goldGainParent.anchoredPosition = _goldStartPos;
        Vector2 endPos = _goldGainParent.anchoredPosition + Vector2.up * 100;

        float t = 0, timeToMove = 2;
        while (t < timeToMove)
        {
            _goldGainParent.anchoredPosition = Vector2.Lerp(_goldGainParent.anchoredPosition, endPos, t / timeToMove);

            t += Time.deltaTime;
            yield return null;
        }

        _goldGainParent.gameObject.SetActive(false);
        _goldGainParent.anchoredPosition = _goldStartPos;
    }

    private void ToggleRetreatPanel() //Will also need to edit this so that if the completed the quest, then there's no loss in loot
    {
        _retreatPanelShown = !_retreatPanelShown;
        if (_retreatPanelShown) _retreatPanel.gameObject.SetActive(false);

        if (_retreatPanelCoroutine != null) StopCoroutine(_retreatPanelCoroutine);
        if (_retreatPanelShown) _retreatPanelCoroutine = StartCoroutine(LerpRectTransform(_retreatPanel, closeOutPanelPos, true));
        else _retreatPanelCoroutine = StartCoroutine(LerpRectTransform(_retreatPanel, Vector2.zero));
    }

    private void OnPlayerVictory()
    {
        if (DungeonManager.instance.AllEncountersComplete())
        {
            _victoryPanel.gameObject.SetActive(true);
            StartCoroutine(LerpRectTransform(_victoryPanel, Vector2.zero));
        }
        else
        {
            _battleSummaryPanel.gameObject.SetActive(true);
            StartCoroutine(LerpRectTransform(_battleSummaryPanel, Vector2.zero));
        }
    }

    private void OnPlayerDefeat()
    {
        StartCoroutine(LerpRectTransform(_gameOverPanel, Vector2.zero));
    }

    public void ShowCardDisplay(CardSO card)
    {
        _cardDisplay.ShowCardDisplay(card);
    }

    public void HideCardDisplay()
    {
        _cardDisplay.HideCardDisplay();
    }

    public IEnumerator LerpRectTransform(RectTransform rect, Vector3 endPos, bool disableAtEnd = false)
    {
        float timeElapsed = 0, timeToMove = 1f;

        while (timeElapsed < timeToMove)
        {
            rect.anchoredPosition = Vector3.Lerp(rect.anchoredPosition, endPos, (timeElapsed / timeToMove));
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        rect.anchoredPosition = endPos;
        if (disableAtEnd) rect.gameObject.SetActive(false);
    }

    private void OnReturnToTown()
    {
        GameManager.OnLoadScene("Town");
    }
}
