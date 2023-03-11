using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public delegate void OnRoomEnteredCallback(DungeonRoom room);
    public OnRoomEnteredCallback onRoomEntered;

    public static PlayerController instance;
    private void Awake()
    {
        instance = this;
    }

    [SerializeField] private Transform _transform;
    [SerializeField] private CommanderSO _commanderSO;
    [SerializeField] private float _rotationSpeed = 10f;
    [SerializeField] private Transform _deckPocket;
    private PlayerCommander _playerCommander;
    //private PathNode _currentWaypoint;

    public DungeonRoom currentRoom { get; private set; }
    public Transform DeckPocket => _deckPocket;
    private Vector2 moveInput;
    private Vector3 rotationInput;

    private Animator _animator;
    private bool _isMoving;
    private bool _inCombat;

    public Transform rayPos;

    private IEnumerator Start()
    {
        DuelManager.instance.onMatchStarted += delegate { _inCombat = true; };
        DuelManager.instance.onPlayerDefeat += delegate { _inCombat = false; };
        DuelManager.instance.onPlayerVictory += delegate { _inCombat = false; };

        onRoomEntered += (room) => currentRoom = room;

        while (!DungeonManager.instance.DungeonIsReady) yield return null;
        CreateCommander();
    }

    private void OnDestroy()
    {
        DuelManager.instance.onMatchStarted -= delegate { _inCombat = true; };
        DuelManager.instance.onPlayerDefeat -= delegate { _inCombat = false; };
        DuelManager.instance.onPlayerVictory -= delegate { _inCombat = false; };
    }
    
    private void Update()
    {
        moveInput = InputHandler.GetMoveInput();
        rotationInput.y = InputHandler.GetRotationInput();
    }

    private void LateUpdate()
    {
        HandleMovement();
        RotatePlayer();
    }

    private void HandleMovement()
    {
        if (_inCombat || _isMoving) return;

        if (moveInput.y == 1)
        {
            var node = BattlefieldManager.instance.GetNode(_transform.position + _transform.forward * 5);
            if (node != null) StartCoroutine(MovePlayer(node));
        }
    }

    private void RotatePlayer()
    {
        if (_inCombat || _isMoving) return;

        //transform.localEulerAngles += rotationInput * 10 * _inputSensitivity * Time.deltaTime;

        if (rotationInput.y == 1)
        {
            StartCoroutine(RotatePlayer(_transform.position + _transform.right));
            //_animator.SetFloat("horizontal", 1);

        }
        else if (rotationInput.y == -1)
        {
            StartCoroutine(RotatePlayer(_transform.position - _transform.right));
            //_animator.SetFloat("horizontal", -1);

        }
    }

    private void CreateCommander()
    {
        _playerCommander = GetComponent<PlayerCommander>();
        _playerCommander.enabled = true;
        _playerCommander.OnAssignCommander(_commanderSO);
        _playerCommander.CommanderCard.OnCommanderSummon(_transform);
        _animator = _playerCommander.CommanderCard.Summon.GetComponent<Animator>();

        var node = BattlefieldManager.instance.GetNode(_transform.position);
        _playerCommander.CommanderCard.OnOccupyNode(node);
    }

    #region - Manual Movement -
    private IEnumerator MovePlayer(GridNode node)
    {
        if (node.Occupant != null) yield break;

        _isMoving = true;
        _playerCommander.CommanderCard.OnAbandonNode();
        while (Vector3.Distance(_transform.position, node.Transform.position) > 0.15f)
        {
            _animator.SetFloat("speed", 1, 0.1f, Time.deltaTime);
            FaceTarget(node.Transform.position);
            yield return null;
        }
        _transform.position = node.Transform.position;
        _playerCommander.CommanderCard.OnOccupyNode(node);

        if (moveInput.y == 1)
        {
            var nextNode = BattlefieldManager.instance.GetNode(_transform.position + _transform.forward * 5);
            if (nextNode != null) yield return StartCoroutine(MovePlayer(nextNode));
        }

        _animator.SetFloat("speed", 0);
        _isMoving = false;
    }

    private IEnumerator RotatePlayer(Vector3 pos)
    {
        _isMoving = true;

        float t = 0, timeToMove = 1f;
        while (t < timeToMove)
        {
            FaceTarget(pos);
            t += Time.deltaTime;
            yield return null;
        }
        //_animator.SetFloat("horizontal", 0);
        _isMoving = false;
    }

    private void FaceTarget(Vector3 pos) //update this to accept a Transform transform?
    {
        Vector3 direction = (pos - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * _rotationSpeed);
    }
    #endregion

    #region - Auto Movement -
    public static void SetDestination(Vector3 destination)
    {
        instance.SetPlayerDestination(destination);
    }

    private void SetPlayerDestination(Vector3 destination)
    {
        if (_isMoving || _inCombat) return;
        StartCoroutine(MoveToPosition(destination));
    }

    private IEnumerator MoveToPosition(Vector3 point)
    {
        _isMoving = true;

        while (Vector3.Distance(transform.position, point) > 0.2f)
        {
            _animator.SetFloat("speed", 1, 0.1f, Time.deltaTime);
            FaceTarget(point);
            yield return null;
        }

        transform.position = point;
        _animator.SetFloat("speed", 0);
        _isMoving = false;
    }
    
    /*public static void SetWaypoint(PathNode point)
    {
        instance.SetPlayerWaypoint(point);
    }
    
    private void SetPlayerWaypoint(PathNode point)
    {
        if (_isMoving || _inCombat) return;
        StartCoroutine(MoveToWaypoint(point));
    }
    
    private IEnumerator MoveToWaypoint(PathNode point)
    {
        _isMoving = true;

        while (Vector3.Distance(transform.position, point.transform.position) > 0.15f)
        {
            _animator.SetFloat("speed", 1, 0.1f, Time.deltaTime);
            FaceTarget(point.transform.position);
            yield return null;
        }

        transform.position = point.transform.position;
        _isMoving = false; //set to false so SetPlayerWaypoint/SetPlayerDestination can be called
        var nextPoint = point.OnWaypointReached(_currentWaypoint);
        _currentWaypoint = point; //Set this after reaching the destination
        if (nextPoint != null) SetPlayerWaypoint(nextPoint);
        else _animator.SetFloat("speed", 0);
    }*/
    #endregion
}
