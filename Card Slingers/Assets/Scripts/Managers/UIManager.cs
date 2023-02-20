using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;
    private void Awake()
    {
        instance = this;
    }

    public RectTransform testPanelRect;
    public Button TEST_PANEL_BUTTON, TEST_END_PHASE_BUTTON;

    #region - Commander Banner -
    [Header("Commander Banner")]
    [SerializeField] private RectTransform phaseBanner;
    [SerializeField] private TMP_Text phaseText;
    [SerializeField] private RectTransform bannerParent;
    [SerializeField] private TMP_Text playerCommanderName, playerHealth, playerMana;
    [SerializeField] private Button endPhaseButton, cancelActionButton;
    [Space]
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

    private void Start()
    {
        bannerParent.anchoredPosition = bannerHiddenPos;
        cardDisplayRect.anchoredPosition = cardDisplayHiddenPos;

        hideCardDisplayButton.onClick.AddListener(HideCardDisplay);
        DuelManager.instance.onMatchStarted += OnMatchStart;
        DuelManager.instance.onPlayerVictory += OnMatchEnd;
        DuelManager.instance.onPlayerDefeat += OnMatchEnd;

        DuelManager.instance.onPhaseChange += OnPhaseChange;
        DuelManager.instance.onNewTurn += OnNewTurn;

        endPhaseButton.onClick.AddListener(OnPlayerEndPhase);
        cancelActionButton.onClick.AddListener(delegate { DuelManager.instance.OnClearAction(); });

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

    private void OnMatchStart()
    {
        playerTurn = true;
        //Subscribe to events
        DuelManager.instance.Player_Commander.onManaChange += OnCommanderValuesChanged;

        playerCommanderName.text = DuelManager.instance.Player_Commander.CommanderInfo.name;

        OnCommanderValuesChanged();

        phaseBanner.anchoredPosition = phaseBannerPos * Vector2.left;
        if (lerpBannerCoroutine != null) StopCoroutine(lerpBannerCoroutine);
        lerpBannerCoroutine = StartCoroutine(LerpRectTransform(bannerParent, bannerShownPos));
    }

    public void SetEnemyCommander(OpponentCommander enemy)
    {
        enemyCommander = enemy;
        opponentCommanderName.text = enemyCommander.CommanderInfo.name;
        enemyCommander.onManaChange += OnCommanderValuesChanged;
    }

    private void OnMatchEnd()
    {
        //Unsubscribe to events
        DuelManager.instance.Player_Commander.onManaChange -= OnCommanderValuesChanged;

        if (enemyCommander != null)
        {
            enemyCommander.onManaChange -= OnCommanderValuesChanged;
        }

        if (lerpBannerCoroutine != null) StopCoroutine(lerpBannerCoroutine);
        lerpBannerCoroutine = StartCoroutine(LerpRectTransform(bannerParent, bannerHiddenPos));
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
