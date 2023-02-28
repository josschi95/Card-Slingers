using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

//NOTE: Split this all up into a central UI Manager and what is in here now put into a CombatHUD script

public class UIManager : MonoBehaviour
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
            DuelManager.instance.OnCurrentPhaseFinished();
        });
    }
    #endregion

    public static UIManager instance;
    private void Awake()
    {
        instance = this;
    }

    #region - Commander Banner -
    [Header("Commander Banner")]
    [SerializeField] private RectTransform phaseBanner;
    [SerializeField] private TMP_Text phaseText;
    [SerializeField] private RectTransform bannerParent;
    [SerializeField] private TMP_Text playerCommanderName, playerHealth, playerMana;
    [SerializeField] private Button endPhaseButton, cancelActionButton;
    [Space]
    [SerializeField] private GameObject enemyBannerParent;
    [SerializeField] private TMP_Text opponentCommanderName;
    [SerializeField] private TMP_Text opponentHealth, opponentMana;
    private OpponentCommander enemyCommander;

    private Coroutine lerpBannerCoroutine;
    private Vector3 bannerShownPos = Vector3.zero;
    private Vector3 bannerHiddenPos = new Vector3(0, 150, 0);
    private Vector2 phaseBannerPos = new Vector2(157.5f, 0);
    private bool playerTurn;
    #endregion

    #region - Card Display -
    [Header("Card Display")]
    [SerializeField] private RectTransform cardDisplayRect;
    [SerializeField] private Button hideCardDisplayButton;
    [SerializeField] private Image cardIcon;
    [SerializeField] private TMP_Text cardTitle, cardDescription, cardFlavorText;
    [SerializeField] private GameObject[] cardCostMarkers;

    private Coroutine lerpCardDisplayCoroutine;
    private Vector3 cardDisplayShownPos = Vector3.zero;
    private Vector3 cardDisplayHiddenPos = new Vector3(500, 0, 0);
    #endregion

    private Vector2 closeOutPanelPos = new Vector2(0, -1000);

    [Header("Victory")]
    [SerializeField] private RectTransform victoryPanel;
    [SerializeField] private Button continueButton;

    [Header("Game Over")]
    [SerializeField] private RectTransform gameOverPanel;
    [SerializeField] private Button retreatButton;

    private void Start()
    {
        Test();
        
        bannerParent.anchoredPosition = bannerHiddenPos;
        cardDisplayRect.anchoredPosition = cardDisplayHiddenPos;
        victoryPanel.anchoredPosition = closeOutPanelPos;
        gameOverPanel.anchoredPosition = closeOutPanelPos;

        hideCardDisplayButton.onClick.AddListener(HideCardDisplay);
        DuelManager.instance.onMatchStarted += OnMatchStart;
        DuelManager.instance.onPlayerVictory += delegate { OnPlayerVictory(); };
        DuelManager.instance.onPlayerDefeat += delegate { OnPlayerDefeat(); };

        DuelManager.instance.onPhaseChange += OnPhaseChange;
        DuelManager.instance.onNewTurn += delegate { OnNewTurn(); };

        endPhaseButton.onClick.AddListener(OnPlayerEndPhase);
        cancelActionButton.onClick.AddListener(delegate { DuelManager.instance.OnClearAction(); });

        retreatButton.onClick.AddListener(delegate { GameManager.OnLoadScene("Town"); });
        continueButton.onClick.AddListener(delegate
        {
            StartCoroutine(LerpRectTransform(victoryPanel, closeOutPanelPos, 2f));
            DuelManager.instance.CloseOutMatch();
        });
    }

    private void OnMatchStart(CombatEncounter encounter)
    {
        playerTurn = true;
        //Subscribe to events
        DuelManager.instance.Player_Commander.onManaChange += OnCommanderValuesChanged;

        playerCommanderName.text = DuelManager.instance.Player_Commander.CommanderInfo.name;
        if (encounter is CommanderEncounter commander) SetEnemyCommander(commander.Commander);
        else
        {
            enemyBannerParent.SetActive(false);
        }
        OnCommanderValuesChanged();

        phaseBanner.anchoredPosition = phaseBannerPos * Vector2.left;
        if (lerpBannerCoroutine != null) StopCoroutine(lerpBannerCoroutine);
        lerpBannerCoroutine = StartCoroutine(LerpRectTransform(bannerParent, bannerShownPos));
    }

    private void SetEnemyCommander(OpponentCommander enemy)
    {
        enemyBannerParent.SetActive(true);

        enemyCommander = enemy;
        opponentCommanderName.text = enemyCommander.CommanderInfo.name;
        enemyCommander.onManaChange += OnCommanderValuesChanged;
    }

    private void OnMatchEnd()
    {
        //Unsubscribe to events
        DuelManager.instance.Player_Commander.onManaChange -= OnCommanderValuesChanged;

        if (enemyCommander != null) enemyCommander.onManaChange -= OnCommanderValuesChanged;
        enemyCommander = null;

        if (lerpBannerCoroutine != null) StopCoroutine(lerpBannerCoroutine);
        lerpBannerCoroutine = StartCoroutine(LerpRectTransform(bannerParent, bannerHiddenPos));
    }

    private void OnPlayerVictory()
    {
        OnMatchEnd();
        StartCoroutine(LerpRectTransform(victoryPanel, Vector2.zero, 2f));
    }

    private void OnPlayerDefeat()
    {
        OnMatchEnd();
        StartCoroutine(LerpRectTransform(gameOverPanel, Vector2.zero, 2f));
    }

    //Player has selected to end their current phase
    private void OnPlayerEndPhase()
    {
        if (DuelManager.instance.Player_Commander.isTurn)
        {
            DuelManager.instance.OnCurrentPhaseFinished();
        }
    }

    private void OnNewTurn()
    {
        playerTurn = !playerTurn;

        var endPos = phaseBannerPos;
        if (playerTurn) endPos *= Vector2.left;
        StartCoroutine(LerpRectTransform(phaseBanner, endPos));

    }

    private void OnPhaseChange(Phase phase)
    {
        phaseText.text = phase.ToString() + " Phase";
    }

    private void OnCommanderValuesChanged()
    {
        playerHealth.text = "[NULL]";
        playerMana.text = DuelManager.instance.Player_Commander.CurrentMana.ToString();
        
        if (enemyCommander != null)
        {
            opponentHealth.text = "[NULL]";
            opponentMana.text = enemyCommander.CurrentMana.ToString();
        }
    }

    public void ShowCardDisplay(CardSO card)
    {
        cardIcon.sprite = card.icon;
        cardTitle.text = card.name;
        cardDescription.text = card.description;
        cardFlavorText.text = card.flavorText;

        for (int i = 0; i < cardCostMarkers.Length; i++)
        {
            if (i < card.cost) cardCostMarkers[i].SetActive(true);
            else cardCostMarkers[i].SetActive(false);
        }

        if (lerpCardDisplayCoroutine != null) StopCoroutine(lerpCardDisplayCoroutine);
        lerpCardDisplayCoroutine = StartCoroutine(LerpRectTransform(cardDisplayRect, cardDisplayShownPos));
    }

    public void HideCardDisplay()
    {
        if (lerpCardDisplayCoroutine != null) StopCoroutine(lerpCardDisplayCoroutine);
        lerpCardDisplayCoroutine = StartCoroutine(LerpRectTransform(cardDisplayRect, cardDisplayHiddenPos));
    }

    private IEnumerator LerpRectTransform(RectTransform rect, Vector3 endPos, float timeToMove = 0.5f)
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
}
