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
    [SerializeField] private Battlefield _battleField;

    [Space]

    [Space]

    [SerializeField] private Phase _currentPhase;

    private CommanderController _playerController;
    private CommanderController _opponentController;
    private LineRenderer arcLine;

    private Card_Permanent _cardToSummon; //Card that the player has selected to summon
    private GridNode highlightedNode; //the node that the mouse is currently over

    private bool _isPlayerTurn;
    private bool _waitingForNodeSelection; //waiting for a node to be targeted
    private Coroutine cardPlacementCoroutine;

    [SerializeField] private List<Card_Permanent> _permanentsToAct; //shitty name but it works for now



    #region - Public Variable References -
    public Battlefield Battlefield => _battleField;
    public CommanderController PlayerController => _playerController;
    public CommanderController OpponentController => _opponentController;
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

        arcLine = GetComponent<LineRenderer>();
    }

    private void BeginTestMatch()
    {
        var player = Instantiate(playerSO.cardPrefab).GetComponent<CommanderController>();
        var opponent = Instantiate(opponentSO.cardPrefab).GetComponent<CommanderController>();

        player.OnAssignCommander(playerSO);
        opponent.OnAssignCommander(opponentSO);

        OnMatchStart(player, opponent);
    }

    //Initiate a new match
    private void OnMatchStart(CommanderController player, CommanderController opponent) //This will also likely take in the battlefield later
    {
        _playerController = player;
        _opponentController = opponent;

        _battleField.CreateGrid();

        PlaceCommanders();

        if (IS_TESTING)
        {
            _isPlayerTurn = true;
            PlayerController.OnFirstTurn();
            onNewMatchStarted?.Invoke();
            return;
        }

        if (Random.value >= 0.5f)
        {
            _isPlayerTurn = true;
            PlayerController.OnFirstTurn();
        }
        else
        {
            _isPlayerTurn = false;
            OpponentController.OnFirstTurn();
        }

        onNewMatchStarted?.Invoke();
    }

    private void PlaceCommanders()
    {
        if (IS_TESTING)
        {
            float width = _battleField.Width;
            
            int playerX = Mathf.RoundToInt(width * 0.5f);
            var playerNode = _battleField.GetNode(playerX, 0);
            PlayerController.CommanderCard.OnSummoned(playerNode);

            int opponentX = Mathf.CeilToInt(width * 0.5f) - 1;
            var opponentNode = _battleField.GetNode(opponentX, _battleField.Depth - 1);
            OpponentController.CommanderCard.OnSummoned(opponentNode);
            OpponentController.transform.localEulerAngles = new Vector3(0, 180, 0);
        }
        else
        {
            //otherwise, the opponent should already be located in on their starting node, so I'll need to call for them to occupy it
            //for the player, I'll need to set their destination to the proper node, likely in the same method as above
            //But there shouldn't be any need to instantiate them (aside from when the player enters the room, no reason to keep idle enemies standing there
        }

        PlayerController.OnMatchStart();
        OpponentController.OnMatchStart();
    }
    #endregion

    #region - Phases -
    public void OnCurrentPhaseFinished()
    {
        //start next commander's turn
        if (_currentPhase == Phase.End)
        {
            _isPlayerTurn = !_isPlayerTurn;
            SetPhase(Phase.Begin);
        }
        //Begin next phase
        else SetPhase(_currentPhase + 1);

        if (_isPlayerTurn) PlayerController.SetPhase(_currentPhase);
        else OpponentController.SetPhase(_currentPhase);
    }

    private void SetPhase(Phase phase)
    {
        Debug.Log("Setting Phase to " + phase.ToString());
        _currentPhase = phase;

        /*switch (phase)
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
        }*/

        onPhaseChange?.Invoke(_currentPhase);
    }

    /*private void OnBeginPhase()
    {
        _isPlayerTurn = !_isPlayerTurn;
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
        
    }*/

    #endregion

    #region - Node Selection 
    private void OnNodeMouseEnter(GridNode node)
    {
        highlightedNode = node;
        if (_waitingForNodeSelection && NodeIsValid(node)) DisplayLineArc(_cardToSummon.transform.position, node.transform.position);
    }

    private void OnNodeMouseExit(GridNode node)
    {
        if (node == highlightedNode) highlightedNode = null;
        ClearLineArc();
    }

    private void OnNodeSelected(GridNode node)
    {
        //I can probably break these up into smaller self-contained methods
        switch (_currentPhase)
        {
            case Phase.Summoning:
                if (_waitingForNodeSelection && node.IsPlayerNode)
                {
                    PlayerController.OnPermanentPlayed(node, _cardToSummon);
                    OnCardDeselected();
                }
                break;
            case Phase.Attack:
                //not waiting for a node currently, this means there's no selected unit
                if (!_waitingForNodeSelection)
                {
                    var occupant = node.Occupant; //node is not empty               //occupant belongs to player
                    if (occupant != null && occupant.Commander is PlayerCommander) HighlightAvailableNodes(occupant);
                }
                else
                {
                    //waiting for a node to be targeted (either to move or to attack)

                }
                break;
        }


    }

    //Highlight nodes in range of the card
    //This is getting increasingly disgusting....
    //I think I basically jsut want to run a function in all nodes forward and back using GridNode.CanMoveIntoNode(card)
    //but that doesn't solve the issue for enemies... if false then I could also check to see if it's an enemy, and stop checking in that direction.
    private void HighlightAvailableNodes(Card_Permanent card)
    {
        Debug.Log("Pickup work here");

        var info = card.CardInfo as UnitSO;
        //Get an array of all nodes in the same lane
        var availableNodes = _battleField.GetAllNodesInLane(card.OccupiedNode.gridX);

        //divide the nodes up by those which are walkable, and those which are targetable
        var walkableNodes = new List<GridNode>();

        for (int i = 0; i < availableNodes.Length; i++)
        {
            //ignore nodes that are further than the unit can walk
            if (Mathf.Abs(card.OccupiedNode.gridZ - availableNodes[i].gridZ) > info.speed) continue;

            //same node, occupied by enemy/allied unit/occupied allied structure
            if (!availableNodes[i].CanMoveIntoNode(card)) continue;

            walkableNodes.Add(availableNodes[i]);
        }
        for (int i = 0; i < walkableNodes.Count; i++)
        {
            walkableNodes[i].SetLockedDisplay(GridNode.MaterialType.Blue);
        }

        var targetableNodes = new List<GridNode>();
        targetableNodes.AddRange(availableNodes);

        //Will also need to track nodes which are occupied by an ally that the card can use an ability on

        for (int i = 0; i < availableNodes.Length; i++)
        {
            if (availableNodes[i].Occupant != null) walkableNodes.Remove(availableNodes[i]);
            //so here is where it gets tricky

            //if there is an enemy unit, you cannot move past it, all nodes beyond it are removed, and its node is red

            //if there is an allied unit, you can move through its space but not occupy it

            //if there is an allied structure, I can move past it, or into it if it is occupiable and open

        }
    }

    private bool NodeIsValid(GridNode node)
    {
        if (!node.IsPlayerNode) return false;
        if (node.Occupant != null) return false;
        //if (node.IsBlocked) return false; //I'll figure this out later

        return true;
    }
    #endregion

    #region - Card/Node Selection -
    private void OnCardInHandSelected(Card card)
    {
        //Player selects a card to summon
        if (PlayerCanSummonCard(card))
        {
            _cardToSummon = card as Card_Permanent;
            if (cardPlacementCoroutine != null) StopCoroutine(cardPlacementCoroutine);
            cardPlacementCoroutine = StartCoroutine(WaitForPermanentToBePlayed(card));
        }
    }

    private void OnCardOnFieldSelected(Card card)
    {
        if (card is not Card_Permanent) throw new System.Exception("Cannot select a non-permanent on the field");

        var permanent = card as Card_Permanent;

        //if waiting for a target of an effect, select the target
        if (_currentPhase == Phase.Attack)
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
        _waitingForNodeSelection = false; //stop coroutine
        _cardToSummon = null;
    }

    private bool PlayerCanSummonCard(Card card)
    {
        if (!_isPlayerTurn) return false; //not their turn
        if (card is not Card_Permanent) return false; //not summonable
        if (_currentPhase != Phase.Summoning) return false; //not summoning phase
        if (card.Commander is not PlayerCommander) return false; //not their card
        if (!PlayerController.CanPlayCard(card)) return false; //not enough mana

        return true;
    }

    private IEnumerator WaitForPermanentToBePlayed(Card card)
    {
        _waitingForNodeSelection = true;

        while (_waitingForNodeSelection == true)
        {
            //only display if node is valid 
            //if (highlightedNode == null || !NodeIsValid(highlightedNode)) ClearLineArc();
            //else DisplayLineArc(card.transform.position, highlightedNode.transform.position);

            yield return null;
        }

        card.OnDeSelectCard();
        ClearLineArc();
    }
    #endregion

    public void DisplayLineArc(Vector3 start, Vector3 end)
    {
        arcLine.positionCount = 100;
        
        for (int i = 0; i < arcLine.positionCount; i++)
        {
            float index = i;
            arcLine.SetPosition(i, MathParabola.Parabola(start, end, index / arcLine.positionCount));
        }
    }

    public void ClearLineArc()
    {
        arcLine.positionCount = 0;
    }
}