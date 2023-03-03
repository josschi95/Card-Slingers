using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DuelManager : MonoBehaviour
{
    #region - TESTING -
    [Header("Testing")]
    public bool IS_TESTING = true;
    [Space]
    
    [SerializeField] private CardHolder _cardHolderPrefab;
    [SerializeField] private Material neutralMat, hostileMat;
    #endregion

    #region - Singleton -
    public static DuelManager instance;

    private void Awake()
    {
        instance = this;
    }
    #endregion

    #region - Callbacks -
    public delegate void OnEncounterStartCallback(CombatEncounter encounter);
    public OnEncounterStartCallback onMatchStarted;

    public delegate void OnEncounterEndCallback();
    public OnEncounterEndCallback onPlayerVictory;
    public OnEncounterEndCallback onPlayerDefeat;

    public delegate void OnCardMovementCallback(Card card); //used to prevent phase transitions while cards are moving
    public OnCardMovementCallback onCardMovementStarted;
    public OnCardMovementCallback onCardMovementEnded;

    public OnCardMovementCallback onCardBeginAction; //used to introduce a delay between action
    public OnCardMovementCallback onCardEndAction;
    //Now I can also add another series of these to replace the check for if any unit is acting, for the resolveAction coroutine

    public delegate void OnPhaseChangeCallback(Phase newPhase);
    public OnPhaseChangeCallback onPhaseChange;
    public OnPhaseChangeCallback onNewTurn;

    public delegate void OnNodeFocusChangeCallback(GridNode node);
    public OnNodeFocusChangeCallback onNodeSelected;
    public OnNodeFocusChangeCallback onNodeMouseEnter;
    public OnNodeFocusChangeCallback onNodeMouseExit;

    public delegate void OnCardSelectedCallback(Card card);
    public OnCardSelectedCallback onCardInHandSelected;
    #endregion

    [Space]

    [Space]

    #region - Phase Fields -
    private Phase _currentPhase;
    private int _turnCount; //total number of turns taken
    private int _cardsInTransition; //prevents phase transition if cards are moving around
    private bool _isPlayerTurn;
    private bool _inPhaseTransition; //prevent skipping to next phase
    private Coroutine phaseDelayCoroutine; //short delay while cards move
    #endregion

    private PlayerController playerController;
    private BattlefieldManager battleField;
    private MonsterManager monsterManager;
    private PlayerCommander playerCommander;
    private CombatEncounter _currentEncounter;
    private LineRenderer arcLine;

    private Coroutine cardPlacementCoroutine;

    private GridNode highlightedNode; //the node that the mouse is currently over
    private List<GridNode> _validTargetNodes = new List<GridNode>(); //used to hold valid nodes for summons and instants

    #region - Action Declaration Fields -
    public bool canDeclareNewAction { get; private set; }
    private bool _waitForTargetNode; //waitinf for a node to be selected to perform an action
    private GridNode _nodeToTarget; //the node that will be targeted to move/attack 
    private Coroutine declareActionCoroutine; //keep available nodes highlighted while a unit is selected to act
    private int _cardsInAction;

    private Card _cardToPlay;
    private bool _waitForValidNode;
    #endregion

    #region - Properties -
    public CombatEncounter CurrentEncounter => _currentEncounter;
    public BattlefieldManager Battlefield => battleField;
    public PlayerCommander Player_Commander => playerCommander;
    public int TurnCount => _turnCount;
    #endregion

    private void Start()
    {
        playerController = PlayerController.instance;
        playerCommander = playerController.GetComponent<PlayerCommander>();
        monsterManager = GetComponent<MonsterManager>();
        battleField = BattlefieldManager.instance;
        arcLine = GetComponent<LineRenderer>();

        onMatchStarted += OnMatchStart;
        onPlayerVictory += OnPlayerVictory;
        onPlayerDefeat += OnPlayerDefeat;

        onNodeSelected += OnNodeSelected;
        onNodeMouseEnter += OnNodeMouseEnter;
        onNodeMouseExit += OnNodeMouseExit;

        onCardMovementStarted += delegate { _cardsInTransition++;};
        //onCardMovementStarted += DebugOnCardMoveStart;
        onCardMovementEnded += delegate { _cardsInTransition--; };
        //onCardMovementEnded += DebugOnCardMoveEnd;

        onCardBeginAction += delegate { _cardsInAction++; };
        onCardEndAction += delegate { _cardsInAction--; };

        onCardInHandSelected += OnCardInHandSelected;

    }

    #region - Match Start -
    private void OnMatchStart(CombatEncounter encounter)
    {
        _currentEncounter = encounter;

        _turnCount = 1;
        _currentPhase = Phase.Begin;
        var room = playerController.currentRoom;

        battleField.CreateGrid(room.Transform.position, room.Orientation, room.BoardDimensions);
        Physics.SyncTransforms();
        for (int i = 0; i < room.Obstacles.Count; i++)
        {
            room.Obstacles[i].OnOccupyNode();
        }

        CameraController.instance.OnCombatStart();

        if (encounter is CommanderEncounter commander) OnCommanderMatchStart(commander.Commander);
        else OnMonsterMatchStart(encounter as MonsterEncounter);
    }

    //Initiate a new match //This will also likely take in the battlefield later
    private void OnCommanderMatchStart(OpponentCommander opponent)
    {
        SetCommanderStartingNode(playerCommander);
        SetCommanderStartingNode(opponent);

        _isPlayerTurn = true;
        Invoke("NewMatchEvents", 5f);
    }

    private void OnMonsterMatchStart(MonsterEncounter encounter)
    {
        SetCommanderStartingNode(playerCommander);
        monsterManager.OnNewMatchStart(encounter);

        _isPlayerTurn = true;
        Invoke("NewMatchEvents", 5f);
    }

    private void SetCommanderStartingNode(CommanderController commander)
    {
        StartCoroutine(SetCommanderCardMat(commander));
        
        float width = battleField.Width;

        int nodeZ = 0;
        int nodeX = Mathf.RoundToInt(width * 0.5f);
        int frontNode = 1;

        if (commander is not PlayerCommander)
        {
            nodeX = Mathf.CeilToInt(width * 0.5f) - 1;
            nodeZ = battleField.Depth - 1;
            frontNode = battleField.Depth - 2;
        }

        var node = battleField.GetNode(nodeX, nodeZ);
        commander.SetStartingNode(node, battleField.GetNode(nodeX, frontNode).transform.position);
    }

    private IEnumerator SetCommanderCardMat(CommanderController commander)
    {
        var dist = battleField.Depth * battleField.CellSize * 0.5f + 1f;
        if (commander is PlayerCommander) dist *= -1;

        var cardMat = Instantiate(_cardHolderPrefab, battleField.Center);
        cardMat.transform.localPosition += new Vector3(0, -1, dist);
        cardMat.transform.SetParent(null);

        var matPos = cardMat.transform.position;
        matPos.y += 1.5f;

        //yield return new WaitForSeconds(1f); //Let the commander move past their cardholder first

        float timeElapsed = 0, timeToMove = 3f;
        while (timeElapsed < timeToMove)
        {
            cardMat.transform.position = Vector3.Lerp(cardMat.transform.position, matPos, timeElapsed / timeToMove);

            timeElapsed += Time.deltaTime;
            yield return null;
        }
        cardMat.transform.position = matPos;

        commander.OnMatchStart(cardMat);
    }

    private void NewMatchEvents()
    {
        onPhaseChange?.Invoke(_currentPhase);
    }
    #endregion

    #region - Phase Control -
    public void OnCurrentPhaseFinished()
    {
        if (_inPhaseTransition) return; //Don't allow the accidental skipping of a phase

        OnClearAction(); //Clear any highlighted nodes and unselect any cards

        if (phaseDelayCoroutine != null) StopCoroutine(phaseDelayCoroutine);
        phaseDelayCoroutine = StartCoroutine(PhaseTransitionDelay());
    }

    private IEnumerator PhaseTransitionDelay()
    {
        _inPhaseTransition = true;

        //wait until all cards have moved to their final destination
        while(_cardsInTransition > 0) yield return null;
        
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
        onNewTurn?.Invoke(Phase.Begin);
        _turnCount++;
    }

    private void OnSummoningPhase()
    {

    }

    private void OnAttackPhase()
    {
        canDeclareNewAction = true;
        //Change this up so that you cannot declare actions on the first round against another commander
        //But you CAN declare actions on the first turn against monsters
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

    #region - Node/Card Selection 
    //The player cancels an action or completes it
    public void OnClearAction()
    {
        _cardToPlay?.OnDeSelectCard();

        _cardToPlay = null;
        _waitForValidNode = false;
        _waitForTargetNode = false; //stop coroutine

        for (int i = 0; i < _validTargetNodes.Count; i++) _validTargetNodes[i].UnlockDisplay();
        _validTargetNodes.Clear();

        DungeonUIManager.instance.HideCardDisplay();
        ClearLineArc();
    }

    private void OnNodeMouseEnter(GridNode node)
    {
        highlightedNode = node;
        if (_waitForValidNode && NodeIsValid(node)) DisplayLineArc(_cardToPlay.transform.position, node.transform.position);
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
                if (_waitForValidNode && NodeIsValid(node))
                {
                    Player_Commander.OnCardPlayed(_cardToPlay, node);
                    OnClearAction();
                }
                break;
            case Phase.Attack:
                if (!canDeclareNewAction) break;

                if (_waitForValidNode && NodeIsValid(node))
                {
                    Player_Commander.OnCardPlayed(_cardToPlay, node);
                    OnClearAction();
                }
                else if (_waitForTargetNode) //waiting to target a node to occupy or attack
                {
                    _nodeToTarget = node;
                    _waitForTargetNode = false;
                }
                else //not waiting for a node currently, this means there's no selected unit
                {
                    var occupant = node.Occupant; //node is not empty and occupant belongs to player
                    if (_isPlayerTurn && occupant != null && occupant is Card_Unit unit && occupant.Commander is PlayerCommander)
                    {
                        OnBeginDeclareAction(unit);
                    }
                }
                break;
            case Phase.End:
                if (_waitForValidNode && NodeIsValid(node))
                {
                    Player_Commander.OnCardPlayed(_cardToPlay, node);
                    OnClearAction();
                }
                break;
        }
    }

    private void OnCardInHandSelected(Card card)
    {
        OnClearAction();

        if (PlayerCanPlayCard(card))
        {
            _cardToPlay = card;
            _waitForValidNode = true;

            if (cardPlacementCoroutine != null) StopCoroutine(cardPlacementCoroutine);
            cardPlacementCoroutine = StartCoroutine(WaitForCardToBePlayed(card));
        }
        else card.OnDeSelectCard();
    }
    #endregion

    #region - Summoning/Casting -
    private bool PlayerCanPlayCard(Card card)
    {
        if (!_isPlayerTurn || _currentPhase == Phase.Begin) return false; //not their turn
        if (card.Commander != playerCommander) return false; //not their card
        if (!Player_Commander.CanPlayCard(card)) return false; //not enough mana
        if (card is Card_Permanent && _currentPhase == Phase.Attack) return false;
        return true;
    }

    private bool NodeIsValid(GridNode node)
    {
        if (_validTargetNodes.Contains(node)) return true;
        return false;
    }

    private IEnumerator WaitForCardToBePlayed(Card card)
    {
        if (card is Card_Permanent)
        {
            _validTargetNodes.AddRange(battleField.GetSummonableNodes(card.Commander));
        }
        else if (card is Card_Spell spell)
        {
            _validTargetNodes.AddRange(battleField.GetAllNodesInArea(playerCommander.CommanderCard.Node, spell.Range));
        }

        for (int i = 0; i < _validTargetNodes.Count; i++) _validTargetNodes[i].SetLockedDisplay(GridNode.MaterialType.Green);

        //Keep valid nodes highlighted until one has been selected
        while (_waitForValidNode) yield return null;

        card.OnDeSelectCard();
        ClearLineArc();
    }
    #endregion

    #region - Action Declaration -
    //The player selects a unit to perfor an action during the attack phase
    private void OnBeginDeclareAction(Card_Unit unit)
    {
        if (unit.HasActed && !unit.CanMove) return;

        if (declareActionCoroutine != null) StopCoroutine(declareActionCoroutine);
        declareActionCoroutine = StartCoroutine(DisplayAvailableNodes(unit));
    }

    //find the available nodes which the chosen unit can target
    private IEnumerator DisplayAvailableNodes(Card_Unit unit)
    {
        _waitForTargetNode = true;
        var walkNodes = battleField.FindReachableNodes(unit);
        
        var atkNodes = new List<GridNode>(); //Don't populate list if the unit has already attacked
        if (!unit.HasActed) atkNodes = battleField.FindTargetableNodes(unit, unit.Range);

        for (int i = 0; i < walkNodes.Count; i++) walkNodes[i].SetLockedDisplay(GridNode.MaterialType.Yellow);
        for (int i = 0; i < atkNodes.Count; i++) atkNodes[i].SetLockedDisplay(GridNode.MaterialType.Red);

        while (_waitForTargetNode == true)
        {
            if (highlightedNode == null) ClearLineArc();
            else
            {
                if (walkNodes.Contains(highlightedNode)) DisplayLineArc(unit.Node.transform.position, highlightedNode.transform.position);
                else if (atkNodes.Contains(highlightedNode)) DisplayLineArc(unit.Node.transform.position, highlightedNode.transform.position, true);
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
        int distanceFromTarget = battleField.GetDistanceInNodes(unit.Node, nodeToAttack);        
        if (distanceFromTarget <= unit.Range)
        {
            //Debug.Log("Unit is within range to attack without moving");
            unit.OnAttack(nodeToAttack);
        }
        else
        {
            //Debug.Log("target located at " + nodeToAttack.gridX + "," + nodeToAttack.gridZ + " is out of direct range from unit located at " + unit.Node.gridX + "," + unit.Node.gridZ);
            var nodePath = battleField.FindNodePath(unit, nodeToAttack, true, true);
            if (nodePath != null)
            {
                if (nodePath[nodePath.Count - 1] == nodeToAttack)
                {
                    Debug.Log("Removing end node in node path to reach attack target");
                    nodePath.RemoveAt(nodePath.Count - 1); //Remove the node belonging to the attack target
                }

                unit.MoveAlongNodePath(nodePath); //Set unit route
                StartCoroutine(ResolveDeclaredAction(unit, ActionType.Attack, nodeToAttack));
            }
            else Debug.Log("Cannot find intermediary node. Cannot attack");
        }
    }
    
    private IEnumerator ResolveDeclaredAction(Card_Unit unit, ActionType secondaryAction,  GridNode targetNode)
    {
        canDeclareNewAction = false;

        while (_cardsInAction > 0) yield return null;

        //while (unit.IsActing) yield return null;

        if (secondaryAction == ActionType.Attack) unit.OnAttack(targetNode);
        else if (secondaryAction == ActionType.Ability) Debug.LogWarning("Not Implemented");

        while (_cardsInAction > 0) yield return null;
        //while (unit.IsActing) yield return null;

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

    #region - Match End -
    private void OnMatchEnd()
    {
        StopAllCoroutines(); //exit out of any coroutines going, likely the action resolution one
        battleField.DestroyGrid();
        monsterManager.ClearEncounter(); //Clears all cards and wipes board
    }

    //The player won!
    private void OnPlayerVictory()
    {
        OnMatchEnd();

        //Add reward

        //Unlock player to continue through dungeon
    }

    //The player was defeated
    private void OnPlayerDefeat()
    {
        OnMatchEnd();

        //Remove all cards that were added to their deck during this run

        //Fade to black, Defeat scene

        //Return to village
    }

    //Move player to center of the battlefield and allow them to select their next destination
    public void CloseOutMatch()
    {
        PlayerController.SetDestination(battleField.Center.position);
        CameraController.instance.OnCombatEnd();
    }
    #endregion

    #region - Debug -
    private void DebugOnCardMoveStart(Card card)
    {
        if (card != null)
        {
            if (card.transform.parent != null)
            {
                Debug.Log(card.transform.parent.name + ", " + card.name + " start.");
            }
            else
            {
                Debug.Log("[NULL] " + card.name + " start.");
            }
        }

    }

    private void DebugOnCardMoveEnd(Card card)
    {
        if (card != null)
        {
            if (card.transform.parent != null)
            {
                Debug.Log(card.transform.parent.name + ", " + card.name + " end.");
            }
            else
            {
                Debug.Log("[NULL] " + card.name + " end.");
            }
        }
    }
    #endregion
}