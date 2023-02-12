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
    [SerializeField] private GameObject lineIndicator, hostileLineIndicator;
    public bool TEST_SUMMON_ENEMY;

    public void SummonTestEnemy(GridNode node)
    {
        if (TEST_SUMMON_ENEMY)
        {
            _opponentController.OnPermanentPlayed(node, _opponentController.CARDS_IN_HAND[0] as Card_Permanent);

            TEST_SUMMON_ENEMY = false;
        }
    }

    [Space]

    [Space]

    [SerializeField] private Phase _currentPhase;

    private CommanderController _playerController;
    private CommanderController _opponentController;
    private LineRenderer arcLine;

    private Card_Permanent _cardToSummon; //Card that the player has selected to summon
    private GridNode highlightedNode; //the node that the mouse is currently over

    private bool _isPlayerTurn;
    private bool _waitForSummonNode; //waiting for a node to be selected to summon a card
    private Coroutine cardPlacementCoroutine;

    private bool _waitForTargetNode; //waitinf for a node to be selected to perform an action
    private GridNode _nodeToTarget; //the node that will be targeted to move/attack 
    //[SerializeField] 
    private List<DeclaredAction> _declaredActions = new List<DeclaredAction>();
    private Coroutine declareActionCoroutine; //keep available nodes highlighted while a unit is selected to act


    #region - Public Variable References -
    public Battlefield Battlefield => _battleField;
    public CommanderController PlayerController => _playerController;
    public CommanderController OpponentController => _opponentController;
    public bool WaitingForNodeSelection => _waitForSummonNode;
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

    #region - Phase Control -
    public void OnCurrentPhaseFinished()
    {
        //start next commander's turn
        if (_currentPhase == Phase.End) SetPhase(Phase.Begin);
        else SetPhase(_currentPhase + 1); //Begin next phase

        if (_isPlayerTurn) PlayerController.SetPhase(_currentPhase);
        else OpponentController.SetPhase(_currentPhase);
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
            case Phase.Attack:
                OnAttackPhase();
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
        _isPlayerTurn = !_isPlayerTurn;
    }

    private void OnSummoningPhase()
    {
        
    }

    private void OnAttackPhase()
    {
        
    }

    private void OnResolutionPhase()
    {
        if (_declaredActions.Count == 0) OnCurrentPhaseFinished();
        else StartCoroutine(ResolveAllDeclaredActions());
    }

    private void OnEndPhase()
    {
        
    }

    #endregion

    //The player cancels their previously chosen action
    public void OnCancelAction()
    {
        _waitForSummonNode = false; //stop coroutine
        _waitForTargetNode = false; //stop coroutine
        _cardToSummon = null;
    }

    #region - Node Selection 
    private void OnNodeMouseEnter(GridNode node)
    {
        highlightedNode = node;
        if (_waitForSummonNode && NodeIsValid(node)) DisplayLineArc(_cardToSummon.transform.position, node.transform.position);
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
                if (_waitForSummonNode && node.IsPlayerNode)
                {
                    PlayerController.OnPermanentPlayed(node, _cardToSummon);
                    OnCancelAction();
                }
                break;
            case Phase.Attack:
                if (!_waitForTargetNode) //not waiting for a node currently, this means there's no selected unit
                {
                    var occupant = node.Occupant; //node is not empty and occupant belongs to player
                    if (occupant != null && occupant is Card_Unit unit && occupant.Commander is PlayerCommander)
                    {
                        OnBeginDeclareAction(unit);
                    }
                }
                else //waiting for a node to be targeted (either to move or to attack)
                {
                    _nodeToTarget = node;
                    _waitForTargetNode = false;
                }
                break;
        }
    }

    private ValidNodes GetValidNodes(Card_Unit unit, GridNode[] laneNodes)
    {
        var walkableNodes = new List<GridNode>(); var attackNodes = new List<GridNode>();
        bool enemyFound = false; //an enemy has been found in the lane, used to prevent adding nodes past that enemy

        //Check all nodes below the card, iterating backwards starting from the occupied node
        for (int i = unit.OccupiedNode.gridZ - 1; i >= 0; i--)
        {
            //node is further than the unit can walk + their attack range, exit loop
            if (Mathf.Abs(unit.OccupiedNode.gridZ - laneNodes[i].gridZ) > unit.Speed + unit.Range) break;
            //any node past this point is at least within the max attack range of the card
            
            //the node contains an enemy unit/structure, can attack it but cannot move there
            if (laneNodes[i].CanBeAttacked(unit))
            {
                if (!enemyFound) //encountered the first enemy
                {
                    attackNodes.Add(laneNodes[i]);
                    enemyFound = true;
                    continue; //do not check if it can be moved to, it cannot
                }
                else if (Mathf.Abs(attackNodes[0].gridZ - laneNodes[i].gridZ) < unit.Range) //compare the gridZ of the first enemy that was encountered
                {
                    attackNodes.Add(laneNodes[i]);
                    continue;
                }
                else break; //cannot attack beyond this point
            }
            else if (enemyFound) continue; //an enemy was found in a closer node, not looking for unoccupied nodes, only ones within attack range
            //past this point, I am only looking for nodes that the card can move to, or occupy

            //the node is occupied by a unit, check next one
            if (!laneNodes[i].CanBeOccupied(unit)) continue;

            //the node is further than the unit can move, exit loop
            if (Mathf.Abs(unit.OccupiedNode.gridZ - laneNodes[i].gridZ) > unit.Speed) break;
            //any node past this point is at least within movement distance of the card

            //All other criteria has been check, no enemies, within walking range, not occupied
            walkableNodes.Add(laneNodes[i]);
        }

        enemyFound = false; //reset bool for use again

        //Check all nodes above the card, all code here is copy and past as above but the loop is ran in the opposite direction
        for (int i = unit.OccupiedNode.gridZ + 1; i < laneNodes.Length; i++)
        {
            if (Mathf.Abs(unit.OccupiedNode.gridZ - laneNodes[i].gridZ) > unit.Speed + unit.Range) break;
            if (laneNodes[i].CanBeAttacked(unit))
            {
                if (!enemyFound)
                {
                    attackNodes.Add(laneNodes[i]);
                    enemyFound = true;
                    continue;
                }
                else if (Mathf.Abs(attackNodes[0].gridZ - laneNodes[i].gridZ) < unit.Range)
                {
                    attackNodes.Add(laneNodes[i]);
                    continue;
                }
                else break;
            }
            else if (enemyFound) continue; 
            if (!laneNodes[i].CanBeOccupied(unit)) continue;
            if (Mathf.Abs(unit.OccupiedNode.gridZ - laneNodes[i].gridZ) > unit.Speed) break;
            walkableNodes.Add(laneNodes[i]);
        }

        return new ValidNodes(walkableNodes.ToArray(), attackNodes.ToArray());
    }

    private bool NodeIsValid(GridNode node)
    {
        //the node belongs to the player, and it is not occupied
        return (node.IsPlayerNode && node.Occupant == null);
    }
    #endregion

    #region - Card Selection -
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
        _waitForSummonNode = true;

        while (_waitForSummonNode == true)
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

    #region - Action Declaration -
    //The player selects a unit to perfor an action during the attack phase
    private void OnBeginDeclareAction(Card_Unit unit)
    {
        for (int i = _declaredActions.Count - 1; i >= 0; i--)
        {
            if (_declaredActions[i].unit == unit)
            {
                OnDeclaredActionRemoved(_declaredActions[i]);
                break;
            }
        }

        if (declareActionCoroutine != null) StopCoroutine(declareActionCoroutine);
        declareActionCoroutine = StartCoroutine(DisplayAvailableNodes(unit));
    }

    //find the available nodes which the chosen unit can target
    private IEnumerator DisplayAvailableNodes(Card_Unit unit)
    {
        _waitForTargetNode = true;
        var availableNodes = _battleField.GetAllNodesInLane(unit.OccupiedNode.gridX);
        var validNodes = GetValidNodes(unit, availableNodes);
        var walkableNodes = validNodes.nodesToOccupy;
        var attackNodes = validNodes.attackNodes;

        //set the highlight colors of the nodes to their appropriate color
        unit.OccupiedNode.SetLockedDisplay(GridNode.MaterialType.Yellow);
        for (int i = 0; i < walkableNodes.Count; i++) walkableNodes[i].SetLockedDisplay(GridNode.MaterialType.Blue);
        for (int i = 0; i < attackNodes.Count; i++) attackNodes[i].SetLockedDisplay(GridNode.MaterialType.Red);

        while (_waitForTargetNode == true)
        {
            if (highlightedNode == null) ClearLineArc();
            else
            {
                if (walkableNodes.Contains(highlightedNode)) DisplayLineArc(unit.OccupiedNode.transform.position, highlightedNode.transform.position);
                else if (attackNodes.Contains(highlightedNode)) DisplayLineArc(unit.OccupiedNode.transform.position, highlightedNode.transform.position);
                else ClearLineArc();
            }

            yield return null;
        }

        if (_nodeToTarget != null)
        {
            //player is moving their unit
            if (walkableNodes.Contains(_nodeToTarget)) OnActionDeclared(unit, _nodeToTarget, ActionType.Move);
            //player is attacking another unit/structure
            else if (attackNodes.Contains(_nodeToTarget)) OnActionDeclared(unit, _nodeToTarget, ActionType.Attack);
        }

        unit.OccupiedNode.UnlockDisplay();
        for (int i = 0; i < walkableNodes.Count; i++) walkableNodes[i].UnlockDisplay();
        for (int i = 0; i < attackNodes.Count; i++) attackNodes[i].UnlockDisplay();

    }

    //the player has selected a valid node to target
    private void OnActionDeclared(Card_Unit unit, GridNode node, ActionType action)
    {
        var go = lineIndicator;
        if (action == ActionType.Attack) go = hostileLineIndicator;
        var newLine = Instantiate(go).GetComponent<LineRenderer>();
        newLine.positionCount = 100;

        for (int i = 0; i < newLine.positionCount; i++)
        {
            float index = i;
            newLine.SetPosition(i, MathParabola.Parabola(unit.transform.position, node.transform.position, index / newLine.positionCount));
        }

        _declaredActions.Add(new DeclaredAction(unit, node, action, newLine.gameObject));
    }

    //Remove a previously declared action
    private void OnDeclaredActionRemoved(DeclaredAction action)
    {
        _declaredActions.Remove(action);
        Destroy(action.lineIndicator);
    }
    #endregion

    #region - Action Resolution -
    private IEnumerator ResolveAllDeclaredActions()
    {
        while (_declaredActions.Count > 0)
        {
            if (_declaredActions[0].action == ActionType.Move)
            {
                _declaredActions[0].unit.MoveToNode(_declaredActions[0].targetNode);
            }
            else if (_declaredActions[0].action == ActionType.Attack)
            {
                //Move to required node to attack, based on attack range
            }
            else if (_declaredActions[0].action == ActionType.Ability)
            {
                //Move to required node to use ability, based on ability range
            }

            //wait to proceed to next action until the current one is resolved
            while(_declaredActions[0].unit.isActing)
            {
                Debug.Log("Current unit still acting, returning");
                yield return null;
            }

            Debug.Log("Declared action resolved");
            OnDeclaredActionRemoved(_declaredActions[0]); //removes from list of declared actions
            yield return new WaitForSeconds(1); //short delay, this will be changed later
        }

        Debug.Log("All declared actions have been resolved");
        //all declared actions have been resolved, end the phase
        OnCurrentPhaseFinished();
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

public struct ValidNodes
{
    public List<GridNode> nodesToOccupy;
    public List<GridNode> attackNodes;

    public ValidNodes(GridNode[] nodesToOccupy, GridNode[] attackNodes)
    {
        this.nodesToOccupy = new List<GridNode>(nodesToOccupy);
        this.attackNodes = new List<GridNode>(attackNodes);
    }
}

public struct DeclaredAction
{
    public Card_Unit unit;
    public GridNode targetNode;
    public ActionType action;
    public GameObject lineIndicator;

    public DeclaredAction(Card_Unit unit, GridNode node, ActionType action, GameObject go)
    {
        this.unit = unit;
        targetNode = node;
        this.action = action;
        lineIndicator = go;
    }
}

public enum ActionType { Move, Attack, Ability }