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

    public Button TEST_SUMMON_BUTTON, TEST_END_PHASE_BUTTON;

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
        DuelManager.instance.onNewMatchStarted += OnMatchStart;
        DuelManager.instance.onMatchEnded += OnMatchEnd;
        DuelManager.instance.onPhaseChange += OnPhaseChange;

        endPhaseButton.onClick.AddListener(OnPlayerEndPhase);
        cancelActionButton.onClick.AddListener(delegate { DuelManager.instance.OnCancelAction(); });
        
        TEST_SUMMON_BUTTON.onClick.AddListener(delegate { DuelManager.instance.TEST_SUMMON_ENEMY = true; });
        TEST_END_PHASE_BUTTON.onClick.AddListener(delegate
        {
            Debug.Log("TEST END PHASE");
            DuelManager.instance.OnCurrentPhaseFinished(); 
        });
    }

    private void OnMatchStart()
    {
        //Subscribe to events
        DuelManager.instance.PlayerController.onManaChange += OnCommanderValuesChanged;
        DuelManager.instance.OpponentController.onManaChange += OnCommanderValuesChanged;

        playerCommanderName.text = DuelManager.instance.PlayerController.CommanderInfo.name;
        opponentCommanderName.text = DuelManager.instance.OpponentController.CommanderInfo.name;

        OnCommanderValuesChanged();

        if (lerpBannerCoroutine != null) StopCoroutine(lerpBannerCoroutine);
        lerpBannerCoroutine = StartCoroutine(LerpRectTransform(bannerParent, bannerShownPos));
    }

    private void OnMatchEnd()
    {
        //Unsubscribe to events
        DuelManager.instance.PlayerController.onManaChange -= OnCommanderValuesChanged;
        DuelManager.instance.OpponentController.onManaChange -= OnCommanderValuesChanged;

        if (lerpBannerCoroutine != null) StopCoroutine(lerpBannerCoroutine);
        lerpBannerCoroutine = StartCoroutine(LerpRectTransform(bannerParent, bannerHiddenPos));
    }

    //Player has selected to end their current phase
    private void OnPlayerEndPhase()
    {
        if (DuelManager.instance.PlayerController.isTurn)
        {
            DuelManager.instance.OnCurrentPhaseFinished();
        }
    }


    private void OnPhaseChange(bool playerTurn, Phase phase)
    {
        phaseText.text = phase.ToString() + " Phase";

        if (playerTurn != this.playerTurn)
        {
            this.playerTurn = playerTurn;
            var endPos = phaseBannerPos;
            if (playerTurn) endPos *= Vector2.left;
            StartCoroutine(LerpRectTransform(phaseBanner, endPos));
        }
    }

    private void OnCommanderValuesChanged()
    {
        playerHealth.text = "[NULL]";
        playerMana.text = DuelManager.instance.PlayerController.CurrentMana.ToString();
        
        opponentHealth.text = "[NULL]";
        opponentMana.text = DuelManager.instance.OpponentController.CurrentMana.ToString();
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
