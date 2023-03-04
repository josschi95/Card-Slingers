using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_CombatBanner : MonoBehaviour
{
    private DungeonUIManager UI;
    private DuelManager duelManager;
    private PlayerCommander player;
    private OpponentCommander enemy;

    [SerializeField] private RectTransform bannerParent;
    [SerializeField] private RectTransform phaseBanner;
    [SerializeField] private TMP_Text phaseText;

    [Space]
    
    [SerializeField] private TMP_Text playerCommanderName;
    [SerializeField] private TMP_Text playerHealth, playerMana;

    [Space]

    [SerializeField] private GameObject enemyBannerParent;
    [SerializeField] private TMP_Text opponentCommanderName;
    [SerializeField] private TMP_Text opponentHealth, opponentMana;

    [Space]

    [SerializeField] private Button endPhaseButton;
    [SerializeField] private Button cancelActionButton;


    private Coroutine lerpBannerCoroutine;
    private Vector3 bannerShownPos = Vector3.zero;
    private Vector3 bannerHiddenPos = new Vector3(0, 150, 0);
    private Vector2 phaseBannerPos = new Vector2(157.5f, 0);
    private bool playerTurn;

    private void Start()
    {
        UI = DungeonUIManager.instance;
        player = PlayerController.instance.GetComponent<PlayerCommander>();
        duelManager = DuelManager.instance;
        duelManager.onMatchStarted += OnMatchStart;
        duelManager.onPhaseChange += OnPhaseChange;
        duelManager.onNewTurn += OnNewTurn;

        duelManager.onPlayerVictory += OnPlayerVictory;
        duelManager.onPlayerDefeat += OnPlayerDefeat;

        endPhaseButton.onClick.AddListener(OnPlayerEndPhase);
        cancelActionButton.onClick.AddListener(delegate { duelManager.OnClearAction(); });

        bannerParent.anchoredPosition = bannerHiddenPos;
    }

    private void OnMatchStart(CombatEncounter encounter)
    {
        playerTurn = true;
        //Subscribe to events
        player.onHealthChange += OnCommanderValuesChanged;
        player.onManaChange += OnCommanderValuesChanged;

        playerCommanderName.text = player.CommanderInfo.name;
        if (encounter is CommanderEncounter commander) SetEnemyCommander(commander.Commander);
        else enemyBannerParent.SetActive(false);

        OnCommanderValuesChanged();

        phaseBanner.anchoredPosition = phaseBannerPos * Vector2.left;
        if (lerpBannerCoroutine != null) StopCoroutine(lerpBannerCoroutine);
        lerpBannerCoroutine = StartCoroutine(UI.LerpRectTransform(bannerParent, bannerShownPos));
    }

    private void SetEnemyCommander(OpponentCommander enemy)
    {
        enemyBannerParent.SetActive(true);

        this.enemy = enemy;
        opponentCommanderName.text = this.enemy.CommanderInfo.name;
        this.enemy.onHealthChange += OnCommanderValuesChanged;
        this.enemy.onManaChange += OnCommanderValuesChanged;
    }

    private void OnCommanderValuesChanged()
    {
        playerHealth.text = player.CommanderCard.CurrentHealth.ToString();
        playerMana.text = player.CurrentMana.ToString();

        if (enemy != null)
        {
            playerHealth.text = enemy.CommanderCard.CurrentHealth.ToString();
            opponentMana.text = enemy.CurrentMana.ToString();
        }
    }

    private void OnPhaseChange(Phase phase)
    {
        phaseText.text = phase.ToString() + " Phase";
    }

    //Player has selected to end their current phase
    private void OnPlayerEndPhase()
    {
        if (playerTurn) duelManager.OnCurrentPhaseFinished();
    }

    private void OnNewTurn(bool isPlayerTurn)
    {
        playerTurn = isPlayerTurn;

        var endPos = phaseBannerPos;
        if (playerTurn) endPos *= Vector2.left;
        StartCoroutine(UI.LerpRectTransform(phaseBanner, endPos));
    }

    private void OnPlayerVictory()
    {
        OnMatchEnd();
    }

    private void OnPlayerDefeat()
    {
        OnMatchEnd();
    }

    private void OnMatchEnd()
    {
        //Unsubscribe to events
        player.onHealthChange -= OnCommanderValuesChanged;
        player.onManaChange -= OnCommanderValuesChanged;

        if (enemy != null)
        {
            enemy.onHealthChange -= OnCommanderValuesChanged;
            enemy.onManaChange -= OnCommanderValuesChanged;
        }
        enemy = null;

        if (lerpBannerCoroutine != null) StopCoroutine(lerpBannerCoroutine);
        lerpBannerCoroutine = StartCoroutine(UI.LerpRectTransform(bannerParent, bannerHiddenPos));
    }
}
