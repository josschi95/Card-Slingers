using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DuelManager : MonoBehaviour
{
    [SerializeField] private GameObject _nodeMarker;

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

    public delegate void OnNewTurnCallback(bool isPlayerTurn);
    public OnNewTurnCallback onNewTurn;

    public delegate void OnNodeFocusChangeCallback(GridNode node);
    public OnNodeFocusChangeCallback onNodeSelected;
    public OnNodeFocusChangeCallback onNodeMouseEnter;
    public OnNodeFocusChangeCallback onNodeMouseExit;

    public delegate void OnCardSelectedCallback(Card card); //can I combine this with OnCardMovementCallback? why not
    public OnCardSelectedCallback onCardInHandSelected;
    #endregion

    #region - Turn Handling Fields -
    private int _turnCount; //total number of turns taken
    private bool _isPlayerTurn;
    private bool _inTurnTransition; //prevent skipping to next turn
    private Coroutine phaseDelayCoroutine; //short delay while cards move
    #endregion

    private PlayerController playerController;
    private BattlefieldManager battleField;
    private MonsterManager monsterManager;
    private PlayerCommander playerCommander;
    private CombatEncounter _currentEncounter;
    private LineRenderer arcLine;

    private Quaternion _playerRotation;
    private Quaternion _enemyRotation;

    #region - Action Declaration Fields -
    private bool _waitForTargetNode; //waitinf for a node to be selected to perform an action
    private GridNode highlightedNode; //the node that the mouse is currently over
    private GridNode _nodeToTarget; //the node that will be targeted to move/attack 
    private List<GridNode> _validTargetNodes = new List<GridNode>(); //used to hold valid nodes for summons and instants
    private Coroutine declareActionCoroutine; //keep available nodes highlighted while a unit is selected to act
    private Coroutine cardPlacementCoroutine;

    private Card _cardToPlay;
    private bool _waitForValidNode;
    private int _cardsInAction; //Prevents declaring new actions while one is taking place
    private int _cardsInTransition; //prevents phase transition if cards are moving around
    public bool CardsInTransition => _cardsInTransition > 0;

    private int _encounterGoldReward;
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
        onPlayerDefeat += OnMatchEnd;
        onPlayerVictory += OnMatchEnd;
        onPlayerVictory += OnPlayerVictory;

        onNodeSelected += OnNodeSelected;
        onNodeMouseEnter += OnNodeMouseEnter;
        onNodeMouseExit += OnNodeMouseExit;

        onCardMovementStarted += OnCardStartMovement;
        onCardMovementEnded += OnCardEndMovement;

        onCardBeginAction += OnCardStartAction;
        onCardEndAction += OnCardEndAction;

        onCardInHandSelected += OnCardInHandSelected;
    }

    #region - Match Start -
    private void OnMatchStart(CombatEncounter encounter)
    {
        var playerRot = playerController.transform.localEulerAngles;
        playerRot.y = SnapFloat(playerRot.y);
        _playerRotation = Quaternion.Euler(playerRot);
        playerCommander.DefaultRotation = Quaternion.Euler(playerRot);

        var enemyRot = playerRot;
        enemyRot.y += 180;
        _enemyRotation = Quaternion.Euler(enemyRot);

        _currentEncounter = encounter;

        _turnCount = 1;
        _isPlayerTurn = true;

        var room = playerController.currentRoom;
        battleField.CreateGrid(room.Transform.position, room.Orientation, room.BoardDimensions);

        Physics.SyncTransforms();
        for (int i = 0; i < room.Obstacles.Count; i++)
        {
            room.Obstacles[i].OnOccupyNode();
        }

        SetCommanderStartingNode(playerCommander);
        if (encounter is CommanderEncounter opponent)
        {
            opponent.Commander.DefaultRotation = Quaternion.Euler(enemyRot);
            SetCommanderStartingNode(opponent.Commander);
        }
        else monsterManager.OnMatchStart(encounter as MonsterEncounter, Quaternion.Euler(enemyRot));

        onNewTurn?.Invoke(_isPlayerTurn);
    }

    private void SetCommanderStartingNode(CommanderController commander)
    {
        commander.OnMatchStart();
        
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

    public void SetMatchReward(int gold)
    {
        _encounterGoldReward = gold;
    }
    #endregion

    #region - Turn Control -
    public void OnEndTurn()
    {
        if (_inTurnTransition) return; //Don't allow the accidental skipping of a turn

        OnClearAction(); //Clear any highlighted nodes and unselect any cards

        if (phaseDelayCoroutine != null) StopCoroutine(phaseDelayCoroutine);
        phaseDelayCoroutine = StartCoroutine(TurnTransitionDelay());
    }

    private IEnumerator TurnTransitionDelay()
    {
        _inTurnTransition = true;

        //wait until all cards have moved to their final destination
        while(!CanDeclareNewAction()) yield return null;
        
        //one more short delay to be sure
        yield return new WaitForSeconds(0.5f);

        _inTurnTransition = false;

        OnNewTurn();
    }

    private void OnNewTurn()
    {
        _isPlayerTurn = !_isPlayerTurn;
        onNewTurn?.Invoke(_isPlayerTurn);
        _turnCount++;
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
        //ClearLineArc();
        _nodeMarker.SetActive(false);
    }

    private void OnNodeMouseEnter(GridNode node)
    {
        highlightedNode = node;
        //if (_waitForValidNode && NodeIsValid(node)) DisplayLineArc(_cardToPlay.transform.position, node.transform.position);
        if (_waitForValidNode && NodeIsValid(node))
        {
            _nodeMarker.SetActive(true);
            _nodeMarker.transform.position = node.Transform.position;
        }
    }

    private void OnNodeMouseExit(GridNode node)
    {
        if (node == highlightedNode) highlightedNode = null;
        _nodeMarker.gameObject.SetActive(false);
        //ClearLineArc();
    }

    private void OnNodeSelected(GridNode node)
    {
        if (!CanDeclareNewAction()) return;

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
        else //not waiting for a node currently, this means there's no selected unit or card to play
        {
            var occupant = node.Occupant; //node is not empty and occupant belongs to player
            if (_isPlayerTurn && occupant != null && occupant is Card_Unit unit && occupant.isPlayerCard)
            {
                OnBeginDeclareAction(unit);
            }
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
        if (!_isPlayerTurn) return false; //not their turn
        //if (!CanDeclareNewAction()) return false;
        if (!card.isPlayerCard) return false; //not their card
        if (!Player_Commander.CanPlayCard(card)) return false; //not enough mana
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
            _validTargetNodes.AddRange(battleField.GetSummonableNodes(card.isPlayerCard));
        }
        else if (card is Card_Spell spell)
        {
            _validTargetNodes.AddRange(battleField.GetAllNodesInArea(playerCommander.CommanderCard.Node, spell.Range));
        }

        for (int i = 0; i < _validTargetNodes.Count; i++) _validTargetNodes[i].SetLockedDisplay(GridNode.MaterialType.Green);

        //Keep valid nodes highlighted until one has been selected
        while (_waitForValidNode) yield return null;

        card.OnDeSelectCard();
        //ClearLineArc();
        _nodeMarker.SetActive(false);
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
            //if (highlightedNode == null) ClearLineArc();
            if (highlightedNode == null) _nodeMarker.SetActive(false);
            else
            {
                //if (walkNodes.Contains(highlightedNode)) DisplayLineArc(unit.Node.transform.position, highlightedNode.transform.position);
                //else if (atkNodes.Contains(highlightedNode)) DisplayLineArc(unit.Node.transform.position, highlightedNode.transform.position, true);
                //else ClearLineArc();

                if (walkNodes.Contains(highlightedNode) || atkNodes.Contains(highlightedNode))
                {
                    _nodeMarker.transform.position = highlightedNode.Transform.position;
                    _nodeMarker.SetActive(true);
                }
                else _nodeMarker.SetActive(false);
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
        var nodePath = new List<GridNode>(battleField.FindNodePath(unit, nodeToOccupy));
        StartCoroutine(unit.MoveUnit(nodePath));
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

                StartCoroutine(unit.MoveUnit(nodePath));
                StartCoroutine(ResolveDeclaredAction(unit, ActionType.Attack, nodeToAttack));
            }
            else Debug.Log("Cannot find intermediary node. Cannot attack");
        }
    }
    
    private IEnumerator ResolveDeclaredAction(Card_Unit unit, ActionType secondaryAction,  GridNode targetNode)
    {
        float maxDelay = 3f; //Prevent this loop from never ending
        while (unit.IsActing || _cardsInAction > 0)
        {
            maxDelay -= Time.deltaTime; //This is just a patch on a larger issue, so fix this problem
            if (maxDelay <= 0) break;
            yield return null;
        }

        if (secondaryAction == ActionType.Attack) unit.OnAttack(targetNode);
        else if (secondaryAction == ActionType.Ability) Debug.LogWarning("Not Implemented");

        maxDelay = 3f; //Prevent this loop from never ending
        while (unit.IsActing || _cardsInAction > 0)
        {
            maxDelay -= Time.deltaTime; //This is just a patch on a larger issue, so fix this problem
            if (maxDelay <= 0) break;
            yield return null;
        }
    }
    #endregion

    #region - Match End -
    private void OnMatchEnd()
    {
        //ClearLineArc();
        _nodeMarker.SetActive(false);
        battleField.DestroyGrid();
        monsterManager.ClearEncounter(); //Clears all cards and wipes board
    }

    private void OnPlayerVictory()
    {
        DungeonUIManager.instance.UpdateGoldDisplay(_encounterGoldReward);
        GameManager.OnGainTempGold(_encounterGoldReward);
        _encounterGoldReward = 0;
    }

    //Move player to center of the battlefield and allow them to select their next destination
    public void CloseOutMatch()
    {
        PlayerController.SetDestination(battleField.Center.position);
        CameraController.instance.OnCombatEnd();
    }
    #endregion

    #region - Action Prevention -
    //These methods are called when a physical card is moving about, or a summoned unit is acting
    //Used to prevent further actions from being called which may interfere with ongoing actions
    public bool CanDeclareNewAction()
    {
        if (_cardsInAction > 0 || _cardsInTransition > 0) return false;
        return true;
    }

    private void OnCardStartMovement(Card card)
    {
        _cardsInTransition++;
    }

    private void OnCardEndMovement(Card card)
    {
        _cardsInTransition--;
        if (_cardsInTransition < 0) _cardsInTransition = 0;
    }

    private void OnCardStartAction(Card card)
    {
        _cardsInAction++;
        //Debug.Log("New Card Started Action. Total: " + _cardsInAction);
    }

    private void OnCardEndAction(Card card)
    {
        _cardsInAction--;
        if (_cardsInAction < 0) _cardsInAction = 0;
        //Debug.Log("New Card Ended Action. Total: " + _cardsInAction);
    }
    #endregion

    #region - Display Line -
    public void DisplayLineArc(Vector3 start, Vector3 end, bool hostile = false)
    {
        //if (hostile) arcLine.material = hostileMat;
        //else arcLine.material = neutralMat;

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
    #endregion

    private float SnapFloat(float f, float factor = 90)
    {
        if (factor == 0) throw new UnityException("Cannot divide by 0!");
        f = Mathf.Round(f / factor) * factor;
        return f;
    }
}