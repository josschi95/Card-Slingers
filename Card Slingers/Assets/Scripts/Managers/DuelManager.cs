using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DuelManager : MonoBehaviour
{
    #region - Singleton -
    public static DuelManager instance;

    private void Awake()
    {
        instance = this;
    }
    #endregion

    #region - Callbacks -
    public delegate void OnNodeSelectedCallback(GridNode node);
    public OnNodeSelectedCallback onNodeSelected;

    public delegate void OnCardSelectedCallback(Card card);
    public OnCardSelectedCallback onCardInDeckSelected; //This will probably only be used if the player is searching through the deck for a card
    public OnCardSelectedCallback onCardInDiscardSelected; //This will probably also only be used for resurrection type effects
    public OnCardSelectedCallback onCardInHandSelected; //if player card, display a larger UI display, if can be played and then a valid tile is selected => play it, if in the enemy's hand, likely only to reveal a card or choose one to discard
    public OnCardSelectedCallback onCardInPlaySelected; //This could be for a number of reasons, but I don't think it will be for targeting. Should likely only be to get a UI close up view of a card
    #endregion

    [SerializeField] private Battlefield battleField;
    [SerializeField] private CommanderSO _player, _opponent;
    [SerializeField] private Phase _currentPhase;

    public CommanderController playerController, opponentController;
    private CommanderController commanderInTurn;
    [Space]
    public GameObject cardPrefab;

    #region - Public Variable References -
    public CommanderSO Player => _player;
    public CommanderSO Opponent => _opponent;
    public Battlefield Battlefield => battleField;
    public Phase CurrentPhase => _currentPhase;
    #endregion

    #region - Initial Methods -
    private void Start()
    {
        OnAssignCommanders(_player, _opponent);

        onNodeSelected += OnNodeSelected;
        onCardInDeckSelected += OnCardInDeckdSelected;
        onCardInDiscardSelected += OnCardInDiscardSelected;
        onCardInHandSelected += OnCardInHandSelected;
        onCardInPlaySelected += OnCardInPlaySelected;
    }

    private void PlaceCommanders()
    {
        float width = battleField.Width;

        int playerX = Mathf.RoundToInt(width * 0.5f);
        var playerPerm = battleField.PlacePermanent(playerX, 0, _player.CommanderPrefab, true);
        playerController = playerPerm.GetComponent<CommanderController>();
        playerController.OnMatchStart(_player, battleField.PlayerDeck, battleField.PlayerHand, battleField.PlayerDiscard);

        int opponentX = Mathf.CeilToInt(width * 0.5f) - 1;
        var opponentPerm = battleField.PlacePermanent(opponentX, battleField.Depth - 1, _opponent.CommanderPrefab, false);
        opponentController = opponentPerm.GetComponent<CommanderController>();
        opponentController.OnMatchStart(_opponent, battleField.OpponentDeck, battleField.OpponentHand, battleField.OpponentDiscard);
    }

    private void OnAssignCommanders(CommanderSO player, CommanderSO opponent)
    {
        _player = player;
        _opponent = opponent;

        PlaceCommanders();
        OnMatchStart();
    }

    private void OnMatchStart()
    {
        if (Random.value >= 0.5f)
        {
            commanderInTurn = playerController;
            playerController.SetPhase(Phase.Begin);
        }
        else
        {
            commanderInTurn = opponentController;
            opponentController.SetPhase(Phase.Begin);
        } 
    }
    #endregion

    #region - Phases -
    public void OnCurrentPhaseFinished()
    {
        //start next commander's turn
        if (_currentPhase == Phase.End)
        {
            //Switch who is in turn
            if (commanderInTurn == playerController) commanderInTurn = opponentController;
            else commanderInTurn = playerController;

            SetPhase(Phase.Begin);
        }
        //Begin next phase
        else SetPhase(_currentPhase + 1);

        if (playerController.isTurn) playerController.SetPhase(_currentPhase);
        else opponentController.SetPhase(_currentPhase);
    }

    private void SetPhase(Phase phase)
    {
        _currentPhase = phase;
        switch (phase)
        {
            case Phase.Begin:
                OnBeginPhase();
                break;
            case Phase.Summoning:
                OnSummoningPhase();
                break;
            case Phase.Declaration:
                OnDeclarationPhase();
                break;
            case Phase.Resolution:
                OnResolutionPhase();
                break;
            case Phase.End:
                OnEndPhase();
                break;
        }
    }

    private void OnBeginPhase()
    {
        //Debug.Log("Begin Phase");
    }

    private void OnSummoningPhase()
    {
        //Debug.Log("Summoning Phase");
    }

    private void OnDeclarationPhase()
    {
        //Debug.Log("Declaration Phase");
    }

    private void OnResolutionPhase()
    {
        //Debug.Log("Resolution Phase Started");
    }

    private void OnEndPhase()
    {
        //Debug.Log("End Phase Started");
    }
    #endregion

    private void OnCardInDeckdSelected(Card card)
    {

    }

    private void OnCardInDiscardSelected(Card card)
    {

    }

    private void OnCardInHandSelected(Card card)
    {
        if (card.IsPlayerCard || card.isRevealed)
            UIManager.instance.ShowCardDisplay(card.cardInfo);

        if (playerController.isTurn && _currentPhase == Phase.Summoning)
        {
            Debug.Log("Waiting for tile to be selected");
            cardWaitingToBePlayed = card;
            StartCoroutine(WaitForPermanentToBePlayed());
        }
    }

    public void OnCardDeselected()
    {
        //Hide the display if action
        UIManager.instance.HideCardDisplay();
        cardWaitingToBePlayed = null;
        //If waiting for a target, stop waiting
    }

    private void OnCardInPlaySelected(Card card)
    {
        UIManager.instance.ShowCardDisplay(card.cardInfo);
    }

    private void OnNodeSelected(GridNode node)
    {
        if (waitingForNodeToBeSelected && commanderInTurn.CanPlayCard(cardWaitingToBePlayed.cardInfo.cost))
        {
            Debug.Log(cardWaitingToBePlayed.cardInfo.name + " is to be played at " + node.transform.position);
            commanderInTurn.OnPermanentPlayed(node.gridX, node.gridZ, cardWaitingToBePlayed);

            cardWaitingToBePlayed = null;
        }
    }

    private Card cardWaitingToBePlayed;
    private bool waitingForNodeToBeSelected;
    private IEnumerator WaitForPermanentToBePlayed()
    {
        while (cardWaitingToBePlayed != null)
        {
            waitingForNodeToBeSelected = true;
            yield return null;
        }
        waitingForNodeToBeSelected = false;
    }
}
