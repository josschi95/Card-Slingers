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
    public OnMatchCallback onMatchStarted;
    public OnMatchCallback onMatchEnded;

    public delegate void OnCardMovementCallback(); //used to prevent phase transitions while cards are moving
    public OnCardMovementCallback onCardMovementStarted;
    public OnCardMovementCallback onCardMovementEnded;

    public delegate void OnPhaseChangeCallback(bool playerTurn, Phase newPhase);
    public OnPhaseChangeCallback onPhaseChange;

    public delegate void OnNodeFocusChangeCallback(GridNode node);
    public OnNodeFocusChangeCallback onNodeSelected;
    public OnNodeFocusChangeCallback onNodeMouseEnter;
    public OnNodeFocusChangeCallback onNodeMouseExit;

    public delegate void OnCardSelectedCallback(Card card);
    public OnCardSelectedCallback onCardInHandSelected; //if player card, display a larger UI display, if can be played and then a valid tile is selected => play it, if in the enemy's hand, likely only to reveal a card or choose one to discard
    //public OnCardSelectedCallback onCardInPlaySelected; //This could be for a number of reasons, but I don't think it will be for targeting. Should likely only be to get a UI close up view of a card
    //public OnCardSelectedCallback onCardInDeckSelected; //This will probably only be used if the player is searching through the deck for a card
    //public OnCardSelectedCallback onCardInDiscardSelected; //This will probably also only be used for resurrection type effects

    public delegate void OnCommanderDefeatedCallback(CommanderController commander);
    public OnCommanderDefeatedCallback onCommanderDefeated;
    #endregion

    [Header("Testing")]
    public bool IS_TESTING = true;
    [SerializeField] private CommanderSO playerSO, opponentSO;
    [SerializeField] private Battlefield _battleField;
    [SerializeField] private CardHolder _cardHolderPrefab;
    [SerializeField] private GameObject lineIndicator, hostileLineIndicator;
    [SerializeField] private Material neutralMat, hostileMat;
    public bool TEST_SUMMON_ENEMY;

    public void SummonTestEnemy(GridNode node)
    {
        if (TEST_SUMMON_ENEMY)
        {
            _opponentController.OnPermanentPlayed(node, _opponentController.CardsInHand[0] as Card_Permanent);

            TEST_SUMMON_ENEMY = false;
        }
    }

    [Space]

    [Space]

    [SerializeField] private Phase _currentPhase;

    private PlayerCommander _playerController;
    private OpponentCommander _opponentController;

    private LineRenderer arcLine;
    private bool _inPhaseTransition;

    private Card_Permanent _cardToSummon; //Card that the player has selected to summon
    private Card_Spell _instantToCast; //Card that the player has selected to cast: Spell/Terrain
    private GridNode highlightedNode; //the node that the mouse is currently over

    private int _turnCount;
    private bool _isPlayerTurn;
    private bool _waitForSummonNode; //waiting for a node to be selected to summon a card
    private bool _waitForInstantNode; //waiting for a node to be selected to cast an instant
    private List<GridNode> _validInstantNodes = new List<GridNode>();
    private Coroutine cardPlacementCoroutine;

    private Coroutine phaseDelayCoroutine;
    private int _cardsInMovement;

    #region - Action Declaration Variables -
    private bool _waitForTargetNode; //waitinf for a node to be selected to perform an action
    private GridNode _nodeToTarget; //the node that will be targeted to move/attack 
    [SerializeField] private List<DeclaredAction> _declaredActions = new List<DeclaredAction>();
    private List<GridNode> _claimedNodes = new List<GridNode>(); //nodes that have been claimed for movement by declared action
    private Coroutine declareActionCoroutine; //keep available nodes highlighted while a unit is selected to act
    #endregion

    #region - Public Variable References -
    public Battlefield Battlefield => _battleField;
    public PlayerCommander PlayerController => _playerController;
    public OpponentCommander OpponentController => _opponentController;
    public List<GridNode> ClaimedNodes => _claimedNodes;
    public int TurnCount => _turnCount;
    #endregion

    #region - Initial Methods -
    private void Start()
    {
        onNodeSelected += OnNodeSelected;
        onNodeMouseEnter += OnNodeMouseEnter;
        onNodeMouseExit += OnNodeMouseExit;

        onCardMovementStarted += delegate { _cardsInMovement++; };
        onCardMovementEnded += delegate { _cardsInMovement--; };

        onCardInHandSelected += OnCardInHandSelected;
        onCommanderDefeated += OnCommanderDefeat;

        arcLine = GetComponent<LineRenderer>();

        //For testing, give other scripts time to get their stuff figured out
        if (IS_TESTING) Invoke("BeginTestMatch", 0.1f);
    }

    private void BeginTestMatch()
    {
        var player = Instantiate(playerSO.cardPrefab).GetComponent<PlayerCommander>();
        var opponent = Instantiate(opponentSO.cardPrefab).GetComponent<OpponentCommander>();

        player.OnAssignCommander(playerSO);
        opponent.OnAssignCommander(opponentSO);

        OnMatchStart(_battleField, player, opponent);
    }

    //Initiate a new match //This will also likely take in the battlefield later
    private void OnMatchStart(Battlefield battlefield, PlayerCommander player, OpponentCommander opponent)
    {
        _turnCount = 1;
        _currentPhase = Phase.Begin;
        _battleField = battlefield;
        _playerController = player;
        _opponentController = opponent;

        _battleField.CreateGrid();

        PlaceCommanders();

        if (IS_TESTING)
        {
            _isPlayerTurn = true;
            onPhaseChange?.Invoke(_isPlayerTurn, _currentPhase);
            onMatchStarted?.Invoke();
            return;
        }

        _isPlayerTurn = Random.value >= 0.5f;
        onPhaseChange?.Invoke(_isPlayerTurn, _currentPhase);
        onMatchStarted?.Invoke();
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

        var playerCards = Instantiate(_cardHolderPrefab);
        var opponentCards = Instantiate(_cardHolderPrefab);

        var dist = _battleField.Depth * _battleField.CellSize * 0.5f + 4.5f;

        playerCards.transform.position = new Vector3(_battleField.transform.position.x, _battleField.transform.position.y, -dist);
        opponentCards.transform.position = new Vector3(_battleField.transform.position.x, _battleField.transform.position.y, dist);

        playerCards.transform.rotation = _battleField.transform.rotation;
        opponentCards.transform.eulerAngles = new Vector3(0, _battleField.transform.rotation.y + 180, 0);

        PlayerController.OnMatchStart(playerCards);
        OpponentController.OnMatchStart(opponentCards);
    }
    #endregion

    #region - Phase Control -
    public void OnCurrentPhaseFinished()
    {
        if (_inPhaseTransition) return; //Don't allow the accidental skipping of a phase

        if (phaseDelayCoroutine != null) StopCoroutine(phaseDelayCoroutine);
        phaseDelayCoroutine = StartCoroutine(PhaseTransitionDelay());
    }

    private IEnumerator PhaseTransitionDelay()
    {
        _inPhaseTransition = true;
        //wait until all cards have moved to their final destination
        while(_cardsInMovement > 0) yield return null;
        //one more short delay to be sure
        yield return new WaitForSeconds(0.5f);
        _inPhaseTransition = false;

        //start next commander's turn
        if (_currentPhase == Phase.End) SetPhase(Phase.Begin);
        else SetPhase(_currentPhase + 1); //Begin next phase
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

        onPhaseChange?.Invoke(_isPlayerTurn, _currentPhase);
    }

    private void OnBeginPhase()
    {
        _isPlayerTurn = !_isPlayerTurn;
        _turnCount++;
    }

    private void OnSummoningPhase()
    {

    }

    private void OnAttackPhase()
    {
        //Cannot declare actions on the first turn of the first round
        if (_turnCount == 1) SetPhase(Phase.End);
    }

    private void OnResolutionPhase()
    {
        if (_declaredActions.Count > 0) StartCoroutine(ResolveAllDeclaredActions());
        else OnCurrentPhaseFinished();
    }

    private void OnEndPhase()
    {

    }

    private int RoundCount
    {
        get
        {
            float f = _turnCount;
            int turns = Mathf.CeilToInt(f * 0.5f);
            return turns;
        }
    }
    #endregion

    #region - Victory/Defeat -
    private void OnCommanderDefeat(CommanderController defeatedCommander)
    {
        if (defeatedCommander is PlayerCommander) Invoke("OnPlayerDefeat", 1f);
        else Invoke("OnPlayerVictory", 1f);

        StopAllCoroutines(); //exit out of any coroutines going, likely the action resolution one

    }

    private void OnPlayerVictory()
    {
        //The player won!
        Debug.Log("Player Victory");
        _playerController.OnVictory();
        _opponentController.OnDefeat();

        //Clear the battlefield

        //Add reward

        //Unlock player to continue through dungeon
    }

    private void OnPlayerDefeat()
    {
        //The player was defeated
        Debug.Log("Player Defeat");
        _playerController.OnDefeat();
        _opponentController.OnVictory();

        //Remove all cards that were added to their deck during this run

        //Fade to black, Defeat scene

        //Return to village
    }
    #endregion

    //The player cancels an action or completes it
    public void OnClearAction()
    {
        _waitForSummonNode = false; //stop coroutine
        _waitForInstantNode = false;
        _waitForTargetNode = false; //stop coroutine
        _cardToSummon = null;
        _instantToCast = null;

        for (int i = 0; i < _validInstantNodes.Count; i++) _validInstantNodes[i].UnlockDisplay();
        _validInstantNodes.Clear();
    }

    #region - Node/Card Selection 
    private void OnNodeMouseEnter(GridNode node)
    {
        highlightedNode = node;
        if (_waitForSummonNode && SummonNodeIsValid(node)) DisplayLineArc(_cardToSummon.transform.position, node.transform.position);
        else if (_waitForInstantNode && InstantNodeIsValid(node)) DisplayLineArc(_instantToCast.transform.position, node.transform.position);
    }

    private void OnNodeMouseExit(GridNode node)
    {
        if (node == highlightedNode) highlightedNode = null;
        ClearLineArc();
    }

    private void OnNodeSelected(GridNode node)
    {
        switch (_currentPhase)
        {
            case Phase.Summoning:
                if (_waitForSummonNode && SummonNodeIsValid(node))
                {
                    PlayerController.OnPermanentPlayed(node, _cardToSummon);
                    OnClearAction();
                }
                //probably need some other checks in here based on the spell's intended target
                else if (_waitForInstantNode && InstantNodeIsValid(node)) 
                {
                    PlayerController.OnInstantPlayed(node, _instantToCast);
                    OnClearAction();
                }
                break;
            case Phase.Attack:
                if (!_waitForTargetNode) //not waiting for a node currently, this means there's no selected unit
                {
                    var occupant = node.Occupant; //node is not empty and occupant belongs to player
                    if (_isPlayerTurn && occupant != null && occupant is Card_Unit unit && occupant.Commander is PlayerCommander)
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
            case Phase.End:
                if (_waitForSummonNode && SummonNodeIsValid(node))
                {
                    PlayerController.OnPermanentPlayed(node, _cardToSummon);
                    OnClearAction();
                }
                break;
        }
    }

    private void OnCardInHandSelected(Card card)
    {
        //Player selects a card to summon
        if (card is Card_Permanent permanent && PlayerCanSummonPermanent(permanent))
        {
            _cardToSummon = permanent;
            _waitForSummonNode = true;
            if (cardPlacementCoroutine != null) StopCoroutine(cardPlacementCoroutine);
            cardPlacementCoroutine = StartCoroutine(WaitForCardToBePlayed(card));
        }
        else if (card is Card_Spell spell && PlayerCanCastInstant(spell))
        {
            _instantToCast = spell;
            _waitForInstantNode = true;
            if (cardPlacementCoroutine != null) StopCoroutine(cardPlacementCoroutine);
            cardPlacementCoroutine = StartCoroutine(WaitForCardToBePlayed(card));
        }
        else card.OnDeSelectCard();
    }
    #endregion

    #region - Summoning/Casting -
    private bool PlayerCanSummonPermanent(Card_Permanent card)
    {
        if (!_isPlayerTurn) return false; //not their turn
        if (card.Commander is not PlayerCommander) return false; //not their card
        if (!PlayerController.CanPlayCard(card)) return false; //not enough mana
        if (_currentPhase == Phase.Summoning) return true; //can summon in summoning phase
        else if (_currentPhase == Phase.End) return true; //can also summon in end phase
        return false;
    }
    
    private bool PlayerCanCastInstant(Card_Spell spell)
    {
        if (!_isPlayerTurn) return false; //not their turn
        if (spell.Commander is not PlayerCommander) return false; //not their card
        if (!PlayerController.CanPlayCard(spell)) return false; //not enough mana
        if (_currentPhase == Phase.Begin || _currentPhase == Phase.Resolution) return false;
        return true; //Can play instants during Summon/Attack/End Phases
    }

    private bool SummonNodeIsValid(GridNode node)
    {
        //the node belongs to the player, and it is not occupied or controlled by the enemy
        if (!_battleField.GetControlledNodesInLane(_playerController, node.gridX).Contains(node)) return false;
        return (node.IsPlayerNode && node.Occupant == null);
    }

    private bool InstantNodeIsValid(GridNode node)
    {
        if (_validInstantNodes.Contains(node)) return true;
        return false;
    }

    private IEnumerator WaitForCardToBePlayed(Card card)
    {
        if (card is Card_Spell spell)
        {
            _validInstantNodes.AddRange(_battleField.GetAllNodesInArea(_playerController.CommanderCard.Node, spell.Range));

            for (int i = 0; i < _validInstantNodes.Count; i++)
                _validInstantNodes[i].SetLockedDisplay(GridNode.MaterialType.Blue);
        }

        while (_waitForSummonNode || _waitForInstantNode)
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
        if (!unit.CanAct) return;
            
        for (int i = _declaredActions.Count - 1; i >= 0; i--)
        {
            if (_declaredActions[i].unit == unit)
            {
                OnDeclaredActionRemoved(_declaredActions[i]);
            }
        }

        if (declareActionCoroutine != null) StopCoroutine(declareActionCoroutine);
        declareActionCoroutine = StartCoroutine(DisplayAvailableNodes(unit));
    }

    //find the available nodes which the chosen unit can target
    private IEnumerator DisplayAvailableNodes(Card_Unit unit)
    {
        _waitForTargetNode = true;
        var availableNodes = _battleField.GetAllNodesInLane(unit.Node.gridX);
        var validNodes = GetValidNodes(unit, availableNodes);
        var walkableNodes = validNodes.nodesToOccupy;
        var attackNodes = validNodes.attackNodes;

        //set the highlight colors of the nodes to their appropriate color
        unit.Node.SetLockedDisplay(GridNode.MaterialType.Yellow);
        for (int i = 0; i < walkableNodes.Count; i++) walkableNodes[i].SetLockedDisplay(GridNode.MaterialType.Blue);
        for (int i = 0; i < attackNodes.Count; i++) attackNodes[i].SetLockedDisplay(GridNode.MaterialType.Red);

        while (_waitForTargetNode == true)
        {
            if (highlightedNode == null) ClearLineArc();
            else
            {
                if (walkableNodes.Contains(highlightedNode)) DisplayLineArc(unit.Node.transform.position, highlightedNode.transform.position);
                else if (attackNodes.Contains(highlightedNode)) DisplayLineArc(unit.Node.transform.position, highlightedNode.transform.position);
                else ClearLineArc();
            }
            yield return null;
        }

        if (_nodeToTarget != null) //another way to check this would be if (_nodeToTarget.CanBeAttacked(unit)
        {
            if (walkableNodes.Contains(_nodeToTarget)) OnMoveActionConfirmed(unit, _nodeToTarget);
            else OnAttackActionConfirmed(unit, _nodeToTarget);
        }
        _nodeToTarget = null;

        unit.Node.UnlockDisplay();
        for (int i = 0; i < walkableNodes.Count; i++) walkableNodes[i].UnlockDisplay();
        for (int i = 0; i < attackNodes.Count; i++) attackNodes[i].UnlockDisplay();

    }

    //Not Player dependent
    public ValidNodes GetValidNodes(Card_Unit unit, GridNode[] laneNodes)
    {
        var walkableNodes = new List<GridNode>(); var attackNodes = new List<GridNode>();
        bool enemyFound = false; //an enemy has been found in the lane, used to prevent adding nodes past that enemy

        //Check all nodes below the card, iterating backwards starting from the occupied node
        for (int i = unit.Node.gridZ - 1; i >= 0; i--)
        {
            //node is further than the unit can walk + their attack range, exit loop
            if (Mathf.Abs(unit.Node.gridZ - laneNodes[i].gridZ) > unit.Speed + unit.Range) break;
            //any node past this point is at least within the max attack range of the card

            //the node contains an enemy unit/structure, can attack it but cannot move there
            if (laneNodes[i].CanBeAttacked(unit))
            {
                //the unit cannot attack and is blocked by an enemy unit
                if (!unit.CanAttack) break;

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

            if (!unit.CanMove) continue; //Unit cannot move, don't bother looking for nodes to move to

            //the node is occupied by a unit, check next one
            if (!laneNodes[i].CanBeOccupied(unit)) continue;

            //the node is further than the unit can move, exit loop
            if (Mathf.Abs(unit.Node.gridZ - laneNodes[i].gridZ) > unit.Speed) break;
            //any node past this point is at least within movement distance of the card

            //Cannot move to the same node that another unit has declared they are moving to
            if (_claimedNodes.Contains(laneNodes[i])) continue;
            /*bool nodeClaimed = false;
            for (int d = 0; d < _declaredActions.Count; d++)
            {
                if (_declaredActions[d].targetNode == laneNodes[i])
                {
                    nodeClaimed = true;
                    Debug.Log("Node Claimed, Cannot Move Here.");
                }
            }
            if (nodeClaimed) continue;*/

            //All other criteria has been check, no enemies, within walking range, not occupied
            walkableNodes.Add(laneNodes[i]);
        }

        enemyFound = false; //reset bool for use again

        //Check all nodes above the card, all code here is copy and past as above but the loop is ran in the opposite direction
        for (int i = unit.Node.gridZ + 1; i < laneNodes.Length; i++)
        {
            if (Mathf.Abs(unit.Node.gridZ - laneNodes[i].gridZ) > unit.Speed + unit.Range) break;
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
            if (Mathf.Abs(unit.Node.gridZ - laneNodes[i].gridZ) > unit.Speed) break;
            if (_claimedNodes.Contains(laneNodes[i])) continue;
            /*bool nodeClaimed = false;
            for (int d = 0; d < _declaredActions.Count; d++)
            {
                if (_declaredActions[d].targetNode == laneNodes[i]) nodeClaimed = true;
            }
            if (nodeClaimed) continue;*/
            walkableNodes.Add(laneNodes[i]);
        }

        return new ValidNodes(walkableNodes.ToArray(), attackNodes.ToArray());
    }

    public void OnMoveActionConfirmed(Card_Unit unit, GridNode nodeToOccupy)
    {
        AddNewDeclaredAction(unit, nodeToOccupy, ActionType.Move);
    }

    public void OnAttackActionConfirmed(Card_Unit unit, GridNode nodeToAttack)
    {
        //Debug.Log("Attack action confirmed");
        int distanceFromTarget = Mathf.Abs(unit.Node.gridZ - nodeToAttack.gridZ);
        if (distanceFromTarget <= unit.Range) AddNewDeclaredAction(unit, nodeToAttack, ActionType.Attack);
        else
        {
            //Need to add movement action first
            var intermediaryNode = _battleField.GetUnoccupiedNodeInRange(unit.Node, nodeToAttack, unit.Range);
            if (intermediaryNode != null)
            {
                AddNewDeclaredAction(unit, intermediaryNode, ActionType.Move);
                AddNewDeclaredAction(unit, nodeToAttack, ActionType.Attack);
            }
            else Debug.Log("Cannot find intermediary node. Cannot attack");

            /*for (int i = 0; i < walkableNodes.Count; i++)
            {
                if (Mathf.Abs(walkableNodes[i].gridZ - nodeToAttack.gridZ) <= distanceFromTarget - unit.Range)
                {
                    AddNewDeclaredAction(unit, walkableNodes[i], ActionType.Move);
                    break;
                }
                if (i == walkableNodes.Count - 1)
                {
                    Debug.Log("Cannot find intermediary node. Cannot attack");
                    return;
                }
            }
            //Then add attack action
            AddNewDeclaredAction(unit, nodeToAttack, ActionType.Attack);*/
        }
    }


    //the player has selected a valid node to target
    private void AddNewDeclaredAction(Card_Unit unit, GridNode targetNode, ActionType action)
    {
        var go = lineIndicator;
        if (action == ActionType.Attack) go = hostileLineIndicator;
        var newLine = Instantiate(go).GetComponent<LineRenderer>();
        newLine.positionCount = 100;

        for (int i = 0; i < newLine.positionCount; i++)
        {
            float index = i;
            newLine.SetPosition(i, MathParabola.Parabola(unit.transform.position, targetNode.transform.position, index / newLine.positionCount));
        }

        _declaredActions.Add(new DeclaredAction(unit, targetNode, action, newLine.gameObject));
        if (action == ActionType.Move) _claimedNodes.Add(targetNode);
    }

    //Remove a previously declared action
    private void OnDeclaredActionRemoved(DeclaredAction action)
    {
        _declaredActions.Remove(action);
        Destroy(action.lineIndicator);
        if (action.action == ActionType.Move) _claimedNodes.Remove(action.targetNode);
    }
    #endregion

    #region - Action Resolution -
    private IEnumerator ResolveAllDeclaredActions()
    {
        while (_declaredActions.Count > 0)
        {
            var action = _declaredActions[0];

            if (action.action == ActionType.Move) action.unit.MoveToNode(action.targetNode);
            else if (action.action == ActionType.Attack) action.unit.OnAttack(action.targetNode);
            else if (action.action == ActionType.Ability) Debug.LogWarning("Not Implemented");
            
            while (action.unit.IsActing) yield return null;

            OnDeclaredActionRemoved(action); //removes from list of declared actions
            yield return new WaitForSeconds(0.5f); //short delay, this will be changed later
        }
        _claimedNodes.Clear();

        OnCurrentPhaseFinished(); //all declared actions have been resolved, end the phase
    }
    #endregion

    public void DisplayLineArc(Vector3 start, Vector3 end, bool hostile = false)
    {
        if (hostile) arcLine.material = hostileMat;
        else arcLine.material = neutralMat;

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

[System.Serializable]
public struct DeclaredAction
{
    public Card_Unit unit;
    public GridNode targetNode;
    public ActionType action;
    public GameObject lineIndicator;

    public DeclaredAction(Card_Unit unit, GridNode targetNode, ActionType action, GameObject go)
    {
        this.unit = unit;
        this.targetNode = targetNode;
        this.action = action;
        lineIndicator = go;
    }
}

public enum ActionType { Move, Attack, Ability }