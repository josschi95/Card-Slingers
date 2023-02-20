using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DuelManager : MonoBehaviour
{
    #region - TESTING -
    [Header("Testing")]
    public bool IS_TESTING = true;
    //[SerializeField] 
    private CommanderSO playerSO, opponentSO;
    [SerializeField] private MonsterManager dungeonCommander;
    [Space]
    [SerializeField] private bool useOpponentCommander;
    [Space]
    [SerializeField] private Battlefield _battleField;
    [SerializeField] private CardHolder _cardHolderPrefab;
    //[SerializeField] private GameObject lineIndicator, hostileLineIndicator;
    [SerializeField] private Material neutralMat, hostileMat;

    private void BeginTestMatch()
    {
        var player = Instantiate(playerSO.cardPrefab).GetComponent<PlayerCommander>();
        var opponent = Instantiate(opponentSO.cardPrefab).GetComponent<OpponentCommander>();

        player.OnAssignCommander(playerSO);
        opponent.OnAssignCommander(opponentSO);

        OnCommanderMatchStart(opponent);
    }
    #endregion

    #region - Singleton -
    public static DuelManager instance;

    private void Awake()
    {
        instance = this;
    }
    #endregion

    #region - Callbacks -
    public delegate void OnMatchEventCallback();
    public OnMatchEventCallback onMatchStarted;
    public OnMatchEventCallback onNewTurn;
    public OnMatchEventCallback onPlayerVictory;
    public OnMatchEventCallback onPlayerDefeat;

    public delegate void OnCardMovementCallback(); //used to prevent phase transitions while cards are moving
    public OnCardMovementCallback onCardMovementStarted;
    public OnCardMovementCallback onCardMovementEnded;

    public delegate void OnPhaseChangeCallback(Phase newPhase);
    public OnPhaseChangeCallback onPhaseChange;

    public delegate void OnNodeFocusChangeCallback(GridNode node);
    public OnNodeFocusChangeCallback onNodeSelected;
    public OnNodeFocusChangeCallback onNodeMouseEnter;
    public OnNodeFocusChangeCallback onNodeMouseExit;

    public delegate void OnCardSelectedCallback(Card card);
    public OnCardSelectedCallback onCardInHandSelected;
    #endregion

    [Space]

    [Space]

    private Phase _currentPhase;
    private PlayerCommander _playerCommander;
    private LineRenderer arcLine;
    private bool _inPhaseTransition;

    private Card_Permanent _cardToSummon; //Card that the player has selected to summon
    private Card_Spell _instantToCast; //Card that the player has selected to cast: Spell/Terrain
    private GridNode highlightedNode; //the node that the mouse is currently over

    private int _turnCount;
    private bool _isPlayerTurn;
    private bool _waitForSummonNode; //waiting for a node to be selected to summon a card
    private bool _waitForInstantNode; //waiting for a node to be selected to cast an instant
    private Coroutine cardPlacementCoroutine;

    private List<GridNode> _validTargetNodes = new List<GridNode>(); //used to hold valid nodes for summons and instants

    private Coroutine phaseDelayCoroutine;
    private int _cardsInMovement;

    #region - Action Declaration Variables -
    public bool canDeclareNewAction { get; private set; }
    private bool _waitForTargetNode; //waitinf for a node to be selected to perform an action
    private GridNode _nodeToTarget; //the node that will be targeted to move/attack 
    private Coroutine declareActionCoroutine; //keep available nodes highlighted while a unit is selected to act
    #endregion

    #region - Public Variable References -
    public Battlefield Battlefield => _battleField;
    public PlayerCommander Player_Commander => _playerCommander;
    public int TurnCount => _turnCount;
    #endregion

    private void Start()
    {
        onNodeSelected += OnNodeSelected;
        onNodeMouseEnter += OnNodeMouseEnter;
        onNodeMouseExit += OnNodeMouseExit;

        onCardMovementStarted += delegate { _cardsInMovement++; };
        onCardMovementEnded += delegate { _cardsInMovement--; };

        onCardInHandSelected += OnCardInHandSelected;
        onPlayerVictory += OnPlayerVictory;
        onPlayerDefeat += OnPlayerDefeat;

        arcLine = GetComponent<LineRenderer>();

        _playerCommander = GameObject.Find("PlayerController").GetComponent<PlayerCommander>();
    }

    #region - New Match -
    public void OnNewMatchStart(Battlefield battlefield, OpponentCommander enemyCommander)
    {
        _turnCount = 1;
        _currentPhase = Phase.Begin;
        _battleField = battlefield;
        _battleField.CreateGrid();
        CameraController.instance.OnCombatStart();

        if (enemyCommander is MonsterManager monsters) OnMonsterMatchStart(monsters);
        else OnCommanderMatchStart(enemyCommander);
    }

    //Initiate a new match //This will also likely take in the battlefield later
    private void OnCommanderMatchStart(OpponentCommander opponent)
    {
        SetCommanderStartingNode(_playerCommander);
        SetCommanderStartingNode(opponent);

        _isPlayerTurn = true;
        UIManager.instance.SetEnemyCommander(opponent);
        Invoke("NewMatchEvents", 5f);
        //onPhaseChange?.Invoke(_currentPhase);
        //onMatchStarted?.Invoke();
    }

    private void OnMonsterMatchStart(MonsterManager overlord)
    {
        SetCommanderStartingNode(_playerCommander);
        overlord.OnMatchStart(null); //they don't have cards to play

        _isPlayerTurn = true;
        onPhaseChange?.Invoke(_currentPhase);
        onMatchStarted?.Invoke();
    }

    private void SetCommanderStartingNode(CommanderController commander)
    {
        StartCoroutine(SetCommanderCardMat(commander));
        
        float width = _battleField.Width;

        int nodeZ = 0;
        int nodeX = Mathf.RoundToInt(width * 0.5f);
        int frontNode = 1;

        if (commander is not PlayerCommander)
        {
            nodeX = Mathf.CeilToInt(width * 0.5f) - 1;
            nodeZ = _battleField.Depth - 1;
            frontNode = _battleField.Depth - 2;
        }

        var node = _battleField.GetNode(nodeX, nodeZ);
        commander.SetStartingNode(node, _battleField.GetNode(nodeX, frontNode).transform.position);
    }

    private IEnumerator SetCommanderCardMat(CommanderController commander)
    {
        var dist = _battleField.Depth * _battleField.CellSize * 0.5f + 3.5f;
        if (commander is PlayerCommander) dist *= -1;
        var cardPos = new Vector3(_battleField.transform.position.x, _battleField.transform.position.y - 2, dist);

        var cardMat = Instantiate(_cardHolderPrefab, cardPos, commander.transform.rotation);

        cardPos.y += 2;

        float timeElapsed = 0, timeToMove = 2.5f;

        while (timeElapsed < timeToMove)
        {
            cardMat.transform.position = Vector3.Lerp(cardMat.transform.position, cardPos, timeElapsed / timeToMove);

            timeElapsed += Time.deltaTime;
            yield return null;
        }

        commander.OnMatchStart(cardMat);
    }
    
    private void NewMatchEvents()
    {
        onPhaseChange?.Invoke(_currentPhase);
        onMatchStarted?.Invoke();
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
            case Phase.End:
                OnEndPhase();
                break;
        }

        onPhaseChange?.Invoke(_currentPhase);
    }

    private void OnBeginPhase()
    {
        _isPlayerTurn = !_isPlayerTurn;
        onNewTurn?.Invoke();
        _turnCount++;
    }

    private void OnSummoningPhase()
    {

    }

    private void OnAttackPhase()
    {
        canDeclareNewAction = true;
        //Cannot declare actions on the first turn of the first round
        //if (_turnCount == 1) SetPhase(Phase.End);
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
    private void OnPlayerVictory()
    {
        StopAllCoroutines(); //exit out of any coroutines going, likely the action resolution one

        //The player won!
        Debug.Log("Player Victory");
        //_playerController.OnVictory();

        //Clear the battlefield

        //Add reward

        //Unlock player to continue through dungeon
    }

    private void OnPlayerDefeat()
    {
        StopAllCoroutines(); //exit out of any coroutines going, likely the action resolution one

        //The player was defeated
        Debug.Log("Player Defeat");
        //_playerController.OnDefeat();

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

        for (int i = 0; i < _validTargetNodes.Count; i++) _validTargetNodes[i].UnlockDisplay();
        _validTargetNodes.Clear();

        UIManager.instance.HideCardDisplay();
    }

    #region - Node/Card Selection 
    private void OnNodeMouseEnter(GridNode node)
    {
        highlightedNode = node;
        if (_waitForSummonNode && SummonNodeIsValid(node)) DisplayLineArc(_cardToSummon.transform.position, node.transform.position);
        else if (_waitForInstantNode && _validTargetNodes.Contains(node)) DisplayLineArc(_instantToCast.transform.position, node.transform.position);
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
                    Player_Commander.OnPermanentPlayed(node, _cardToSummon);
                    OnClearAction();
                }
                //probably need some other checks in here based on the spell's intended target
                else if (_waitForInstantNode && _validTargetNodes.Contains(node)) 
                {
                    Player_Commander.OnInstantPlayed(node, _instantToCast);
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
                    Player_Commander.OnPermanentPlayed(node, _cardToSummon);
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
        if (card.Commander is not global::PlayerCommander) return false; //not their card
        if (!Player_Commander.CanPlayCard(card)) return false; //not enough mana
        if (_currentPhase == Phase.Summoning) return true; //can summon in summoning phase
        else if (_currentPhase == Phase.End) return true; //can also summon in end phase
        return false;
    }
    
    private bool PlayerCanCastInstant(Card_Spell spell)
    {
        if (!_isPlayerTurn || _currentPhase == Phase.Begin) return false; //not their turn
        if (spell.Commander is not global::PlayerCommander) return false; //not their card
        if (!Player_Commander.CanPlayCard(spell)) return false; //not enough mana
        return true; //Can play instants during Summon/Attack/End Phases
    }

    private bool SummonNodeIsValid(GridNode node)
    {
        //the node belongs to the player, and it is not occupied or controlled by the enemy
        if (!_battleField.GetControlledNodesInLane(_playerCommander, node.gridX).Contains(node)) return false;
        return (node.isPlayerNode && node.Occupant == null);
    }

    private bool InstantNodeIsValid(GridNode node)
    {
        if (_validTargetNodes.Contains(node)) return true;
        return false;
    }

    private IEnumerator WaitForCardToBePlayed(Card card)
    {
        if (card is Card_Spell spell)
        {
            _validTargetNodes.AddRange(_battleField.GetAllNodesInArea(_playerCommander.CommanderCard.Node, spell.Range));

            for (int i = 0; i < _validTargetNodes.Count; i++)
                _validTargetNodes[i].SetLockedDisplay(GridNode.MaterialType.Blue);
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
        if (!unit.CanAct || unit.HasActed) return;

        if (declareActionCoroutine != null) StopCoroutine(declareActionCoroutine);
        declareActionCoroutine = StartCoroutine(DisplayAvailableNodes(unit));
    }

    //find the available nodes which the chosen unit can target
    private IEnumerator DisplayAvailableNodes(Card_Unit unit)
    {
        _waitForTargetNode = true;
        var walkNodes = _battleField.FindReachableNodes(unit);
        var atkNodes = _battleField.FindTargetableNodes(unit, unit.Range);
        for (int i = 0; i < walkNodes.Count; i++) walkNodes[i].SetLockedDisplay(GridNode.MaterialType.Yellow);
        for (int i = 0; i < atkNodes.Count; i++) atkNodes[i].SetLockedDisplay(GridNode.MaterialType.Red);

        while (_waitForTargetNode == true)
        {
            if (highlightedNode == null) ClearLineArc();
            else
            {
                if (walkNodes.Contains(highlightedNode)) DisplayLineArc(unit.Node.transform.position, highlightedNode.transform.position);
                else if (atkNodes.Contains(highlightedNode)) DisplayLineArc(unit.Node.transform.position, highlightedNode.transform.position);
                else ClearLineArc();
            }
            yield return null;
        }

        for (int i = 0; i < walkNodes.Count; i++) walkNodes[i].UnlockDisplay();
        for (int i = 0; i < atkNodes.Count; i++) atkNodes[i].UnlockDisplay();

        if (_nodeToTarget != null)
        {
            if (atkNodes.Contains(_nodeToTarget)) OnAttackActionConfirmed(unit, _nodeToTarget);
            else if (walkNodes.Contains(_nodeToTarget)) OnMoveActionConfirmed(unit, _nodeToTarget);
        }
        _nodeToTarget = null;
    }

    public void OnMoveActionConfirmed(Card_Unit unit, GridNode nodeToOccupy)
    {
        unit.MoveToNode(nodeToOccupy);
        StartCoroutine(ResolveDeclaredAction(unit, ActionType.Move, nodeToOccupy));
    }

    public void OnAttackActionConfirmed(Card_Unit unit, GridNode nodeToAttack)
    {
        //Debug.Log("Attack action confirmed");
        int distanceFromTarget = _battleField.GetDistanceInNodes(unit.Node, nodeToAttack);        
        if (distanceFromTarget <= unit.Range)
        {
            //Debug.Log("Unit is within range to attack without moving");
            unit.OnAttack(nodeToAttack);
        }
        else
        {
            //Debug.Log("target located at " + nodeToAttack.gridX + "," + nodeToAttack.gridZ + " is out of direct range from unit located at " + unit.Node.gridX + "," + unit.Node.gridZ);
            var nodePath = _battleField.FindNodePath(unit, nodeToAttack, true, true);
            if (nodePath != null)
            {
                if (nodePath[nodePath.Count - 1] == nodeToAttack)
                {
                    Debug.Log("Removing end node in node path to reach attack target");
                    nodePath.RemoveAt(nodePath.Count - 1); //Remove the node belonging to the attack target
                }

                unit.MoveAlongNodePath(nodePath); //Set unit route
                //starts moving the unit to the node
                //unit.MoveToNode(nodePath[nodePath.Count - 2]);
                StartCoroutine(ResolveDeclaredAction(unit, ActionType.Attack, nodeToAttack));
            }
            else Debug.Log("Cannot find intermediary node. Cannot attack");
        }
    }
    
    private IEnumerator ResolveDeclaredAction(Card_Unit unit, ActionType secondaryAction,  GridNode targetNode)
    {
        canDeclareNewAction = false;

        while (unit.IsActing) yield return null;

        if (secondaryAction == ActionType.Attack)
        {
            unit.OnAttack(targetNode);
        }
        else if (secondaryAction == ActionType.Ability) Debug.LogWarning("Not Implemented");
        
        canDeclareNewAction = true;
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



    #region - Obsolete -
    /*public ValidNodes GetValidNodes(Card_Unit unit, GridNode[] laneNodes)
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
            walkableNodes.Add(laneNodes[i]);
        }

        return new ValidNodes(walkableNodes.ToArray(), attackNodes.ToArray());
    }*/

    #endregion
}

/*public struct ValidNodes
{
    public List<GridNode> nodesToOccupy;
    public List<GridNode> attackNodes;

    public ValidNodes(GridNode[] nodesToOccupy, GridNode[] attackNodes)
    {
        this.nodesToOccupy = new List<GridNode>(nodesToOccupy);
        this.attackNodes = new List<GridNode>(attackNodes);
    }
}*/

/*public struct DeclaredAction
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
}*/