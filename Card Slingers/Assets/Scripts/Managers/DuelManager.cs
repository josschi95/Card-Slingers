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
    public delegate void OnMatchCallback();
    public OnMatchCallback onNewMatchStarted;
    public OnMatchCallback onMatchEnded;

    public delegate void OnPhaseChangeCallback(Phase newPhase);
    public OnPhaseChangeCallback onPhaseChange;

    public delegate void OnNodeFocusChangeCallback(GridNode node);
    public OnNodeFocusChangeCallback onNodeSelected;
    public OnNodeFocusChangeCallback onNodeMouseEnter;
    public OnNodeFocusChangeCallback onNodeMouseExit;

    public delegate void OnCardSelectedCallback(Card card);
    public OnCardSelectedCallback onCardInHandSelected; //if player card, display a larger UI display, if can be played and then a valid tile is selected => play it, if in the enemy's hand, likely only to reveal a card or choose one to discard
    public OnCardSelectedCallback onCardInPlaySelected; //This could be for a number of reasons, but I don't think it will be for targeting. Should likely only be to get a UI close up view of a card
    //public OnCardSelectedCallback onCardInDeckSelected; //This will probably only be used if the player is searching through the deck for a card
    //public OnCardSelectedCallback onCardInDiscardSelected; //This will probably also only be used for resurrection type effects
    #endregion

    [Header("Testing")]
    public bool IS_TESTING = true;
    [SerializeField] private CommanderSO playerSO, opponentSO;
    [Space]

    [SerializeField] private Battlefield _battleField;
    [SerializeField] private Phase _currentPhase;

    public CommanderController playerController, opponentController;
    private CommanderController commanderInTurn;
    [Space]
    public GameObject cardPrefab;
    public GameObject cardPermanentPrefab;
    public GameObject empty;

    private Card_Permanent _selectedPermanent;

    private GridNode highlightedNode;
    private bool _waitingForNodeSelection;
    private Coroutine cardPlacementCoroutine;

    [HideInInspector] public bool deselectCard; //called by InputHandler when the player right clicks. Deselect any selected card

    #region - Public Variable References -
    public Battlefield Battlefield => _battleField;
    public bool WaitingForNodeSelection => _waitingForNodeSelection;
    #endregion

    #region - Initial Methods -
    private void Start()
    {
        onNodeSelected += OnNodeSelected;
        onNodeMouseEnter += OnNodeMouseEnter;
        onNodeMouseExit += OnNodeMouseExit;

        onCardInHandSelected += OnCardInHandSelected;
        onCardInPlaySelected += OnCardOnFieldSelected;
        //onCardInDeckSelected += OnCardInDeckSelected;

        //For testing, give other scripts time to get their stuff figured out
        if (IS_TESTING) Invoke("BeginTestMatch", 0.1f);
    }

    private void BeginTestMatch()
    {
        var player = Instantiate(playerSO.CommanderPrefab).GetComponent<CommanderController>();
        var opponent = Instantiate(opponentSO.CommanderPrefab).GetComponent<CommanderController>();
        OnMatchStart(player, opponent);
    }

    //Initiate a new match
    private void OnMatchStart(CommanderController player, CommanderController opponent)
    {
        playerController = player;
        opponentController = opponent;

        PlaceCommanders();

        if (IS_TESTING)
        {
            commanderInTurn = playerController;
            playerController.OnFirstTurn();
            onNewMatchStarted?.Invoke();
            return;
        }

        if (Random.value >= 0.5f)
        {
            commanderInTurn = playerController;
            playerController.OnFirstTurn();
        }
        else
        {
            commanderInTurn = opponentController;
            opponentController.OnFirstTurn();
        }

        onNewMatchStarted?.Invoke();
    }

    private void PlaceCommanders()
    {
        if (IS_TESTING)
        {
            float width = _battleField.Width;
            int playerX = Mathf.RoundToInt(width * 0.5f);
            int opponentX = Mathf.CeilToInt(width * 0.5f) - 1;
            _battleField.PlaceCommander(playerX, 0, playerController.gameObject, true);
            _battleField.PlaceCommander(opponentX, _battleField.Depth - 1, opponentController.gameObject, false);
        }
        else
        {
            //otherwise, the opponent should already be located in on their starting node, so I'll need to call for them to occupy it
            //for the player, I'll need to set their destination to the proper node, likely in the same method as above
            //But there shouldn't be any need 
        }

        playerController.OnMatchStart();
        opponentController.OnMatchStart();
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
        Debug.Log("Setting Phase to " + phase.ToString());
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
        onPhaseChange?.Invoke(_currentPhase);
    }

    private void OnBeginPhase()
    {
        
    }

    private void OnSummoningPhase()
    {
        
    }

    private void OnDeclarationPhase()
    {
        
    }

    private void OnResolutionPhase()
    {
        
    }

    private void OnEndPhase()
    {
        
    }
    #endregion

    #region - Card/Node Selection -
    private void OnCardInHandSelected(Card card)
    {
        //Player selects a card to summon... I need to find a way to simplify all of these and statements
        if (card.Commander is PlayerCommander && playerController.isTurn && _currentPhase == Phase.Summoning && card is Card_Permanent perm)
        {
            //Debug.Log("Waiting for tile to be selected");
            _selectedPermanent = perm;
            if (cardPlacementCoroutine != null) StopCoroutine(cardPlacementCoroutine);
            cardPlacementCoroutine = StartCoroutine(WaitForPermanentToBePlayed(card));
        }
    }

    private void OnCardOnFieldSelected(Card card)
    {
        if (card is not Card_Permanent) throw new System.Exception("Cannot select a non-permanent on the field");

        var permanent = card as Card_Permanent;

        //if waiting for a target of an effect, select the target
        if (_currentPhase == Phase.Declaration)
        {
            if (permanent.FirstTurn)
            {
                //if it doesn't have haste, too bad
            }
        }
    }

    public void OnCardDeselected()
    {
        //Hide the display if action
        UIManager.instance.HideCardDisplay();

        //If waiting for a target, stop waiting
        if (cardPlacementCoroutine != null) StopCoroutine(cardPlacementCoroutine);
        _selectedPermanent = null;
    }

    private void OnNodeMouseEnter(GridNode node)
    {
        highlightedNode = node;
    }

    private void OnNodeMouseExit(GridNode node)
    {
        if (node == highlightedNode) highlightedNode = null;
    }

    private void OnNodeSelected(GridNode node)
    {
        if (_waitingForNodeSelection && commanderInTurn.CanPlayCard(_selectedPermanent.CardInfo.cost) && _battleField.NodeBelongsToCommander(node, commanderInTurn))
        {
            commanderInTurn.OnPermanentPlayed(node, _selectedPermanent);
            if (cardPlacementCoroutine != null) StopCoroutine(cardPlacementCoroutine);
            _waitingForNodeSelection = false;
            OnCardDeselected();
        }
    }

    private bool NodeIsValid(GridNode node)
    {
        if (!node.IsPlayerNode) return false;
        if (node.Occupant != null) return false;

        return true;
    }

    private IEnumerator WaitForPermanentToBePlayed(Card card)
    {
        deselectCard = false;
        _waitingForNodeSelection = true;
        while (_waitingForNodeSelection == true)
        {
            if (highlightedNode == null || !NodeIsValid(highlightedNode)) ClearTrail();
            else DisplayArc(card.transform.position, highlightedNode.transform.position);
            //only display if node is valid 

            if (deselectCard)
            {
                Debug.Log("OnDeselect while waiting for permanent to be played");
                card.OnDeSelectCard();
                ClearTrail();
                deselectCard = false;
                yield break;
            }
            yield return null;
        }
    }
    #endregion

    public void DisplayArc(Vector3 start, Vector3 end)
    {
        var trail = GetComponent<LineRenderer>();
        trail.positionCount = 100;
        
        for (int i = 0; i < trail.positionCount; i++)
        {
            float index = i;
            trail.SetPosition(i, MathParabola.Parabola(start, end, index / trail.positionCount));
        }
    }

    public void ClearTrail()
    {
        var trail = GetComponent<LineRenderer>();
        trail.positionCount = 0;
    }
}