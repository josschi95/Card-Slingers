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
    [SerializeField] private GameObject lineIndicator, hostileLineIndicator;
    [SerializeField] private Material neutralMat, hostileMat;
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
    [SerializeField] private List<DeclaredAction> _declaredActions = new List<DeclaredAction>();
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
        onCommanderDefeated += OnCommanderDefeat;

        arcLine = GetComponent<LineRenderer>();

        //For testing, give other scripts time to get their stuff figured out
        if (IS_TESTING) Invoke("BeginTestMatch", 0.1f);
    }

    private void BeginTestMatch()
    {
        var player = Instantiate(playerSO.cardPrefab).GetComponent<CommanderController>();
        var opponent = Instantiate(opponentSO.cardPrefab).GetComponent<CommanderController>();

        player.OnAssignCommander(playerSO);
        opponent.OnAssignCommander(opponentSO);

        OnMatchStart(_battleField, player, opponent);
    }

    //Initiate a new match //This will also likely take in the battlefield later
    private void OnMatchStart(Battlefield battlefield, CommanderController player, CommanderController opponent)
    {
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
            onNewMatchStarted?.Invoke();
            return;
        }

        _isPlayerTurn = Random.value >= 0.5f;
        onPhaseChange?.Invoke(_isPlayerTurn, _currentPhase);
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

    }

    private void OnSummoningPhase()
    {

    }

    private void OnAttackPhase()
    {

    }

    private void OnResolutionPhase()
    {
        if (_declaredActions.Count == 0) Invoke("OnCurrentPhaseFinished", 1f);
        else StartCoroutine(ResolveAllDeclaredActions());
    }

    private void OnEndPhase()
    {

    }
    #endregion

    #region - Victory/Defeat -
    private void OnCommanderDefeat(CommanderController commander)
    {
        if (commander is PlayerCommander) OnPlayerDefeat();
        else OnPlayerVictory();

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
        }
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

        if (_nodeToTarget != null) OnActionConfirmed(unit, walkableNodes, attackNodes);

        unit.OccupiedNode.UnlockDisplay();
        for (int i = 0; i < walkableNodes.Count; i++) walkableNodes[i].UnlockDisplay();
        for (int i = 0; i < attackNodes.Count; i++) attackNodes[i].UnlockDisplay();

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
            if (Mathf.Abs(unit.OccupiedNode.gridZ - laneNodes[i].gridZ) > unit.Speed) break;
            //any node past this point is at least within movement distance of the card

            //Cannot move to the same node that another unit has declared they are moving to
            bool nodeClaimed = false;
            for (int d = 0; d < _declaredActions.Count; d++)
            {
                if (_declaredActions[d].targetNode == laneNodes[i])
                {
                    nodeClaimed = true;
                    Debug.Log("Node Claimed");
                }
            }
            if (nodeClaimed) continue;

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
            bool nodeClaimed = false;
            for (int d = 0; d < _declaredActions.Count; d++)
            {
                if (_declaredActions[d].targetNode == laneNodes[i]) nodeClaimed = true;
            }
            if (nodeClaimed) continue;
            walkableNodes.Add(laneNodes[i]);
        }

        return new ValidNodes(walkableNodes.ToArray(), attackNodes.ToArray());
    }

    private void OnActionConfirmed(Card_Unit unit, List<GridNode> walkableNodes, List<GridNode> attackNodes)
    {
        //player is moving their unit
        if (walkableNodes.Contains(_nodeToTarget)) AddNewDeclaredAction(unit, _nodeToTarget, ActionType.Move);
        //player is attacking another unit/structure
        else if (attackNodes.Contains(_nodeToTarget))
        {
            Debug.Log("Attack action confirmed");
            int distanceFromTarget = Mathf.Abs(unit.OccupiedNode.gridZ - _nodeToTarget.gridZ);

            if (distanceFromTarget <= unit.Range) AddNewDeclaredAction(unit, _nodeToTarget, ActionType.Attack);
            else //unit needs to move first
            {
                Debug.Log("Attack target out of range, distance = " + distanceFromTarget);
                Debug.Log("Range = " + unit.Range);
                for (int i = 0; i < walkableNodes.Count; i++)
                {
                    Debug.Log("Node at " + walkableNodes[i].gridX + "," + walkableNodes[i].gridZ + " Dist: " + Mathf.Abs(walkableNodes[i].gridZ - _nodeToTarget.gridZ));

                    if (Mathf.Abs(walkableNodes[i].gridZ - _nodeToTarget.gridZ) <= distanceFromTarget - unit.Range)
                    {
                        Debug.Log("Intermediary node found");
                        AddNewDeclaredAction(unit, walkableNodes[i], ActionType.Move);
                        break;
                    }
                }

                AddNewDeclaredAction(unit, _nodeToTarget, ActionType.Attack);
            }
        }
        _nodeToTarget = null;
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
            var action = _declaredActions[0];

            if (action.action == ActionType.Move) action.unit.MoveToNode(action.targetNode);
            else if (action.action == ActionType.Attack) action.unit.AttackNode(action.targetNode);
            else if (action.action == ActionType.Ability) Debug.LogWarning("Not Implemented");
            
            while (action.unit.IsActing) yield return null;

            OnDeclaredActionRemoved(action); //removes from list of declared actions
            yield return new WaitForSeconds(0.5f); //short delay, this will be changed later
        }

        yield return new WaitForSeconds(2.5f); //add a delay so any cards removed can be returned to their owner's deck
        //all declared actions have been resolved, end the phase
        OnCurrentPhaseFinished();
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