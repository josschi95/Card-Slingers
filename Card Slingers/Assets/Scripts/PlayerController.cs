using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    #region - Singleton -
    public static PlayerController instance;
    private void Awake()
    {
        instance = this;
    }
    #endregion

    public delegate void OnGridNodeSelected(GridNode node);
    public OnGridNodeSelected onNodeSelected;

    [SerializeField] private Transform _transform;
    [SerializeField] private float _rotationSpeed = 10f;
    private PlayerCommander _playerCommander;

    private Vector2 moveInput;
    private float rotationInput;

    private Animator _animator;
    private bool _isMoving;
    private bool _inCombat;

    public Transform Transform => _transform;

    private IEnumerator Start()
    {
        _transform = transform;
        _playerCommander = GetComponent<PlayerCommander>();
        _animator = GetComponent<Animator>();

        onNodeSelected += SetNodeDestination;
        DuelManager.instance.onCombatBegin += delegate { _inCombat = true; };
        DuelManager.instance.onPlayerDefeat += delegate { _inCombat = false; };
        DuelManager.instance.onPlayerVictory += delegate { _inCombat = false; };

        while (!DungeonManager.instance.DungeonIsReady) yield return null;

        SetInitialFacingDirection();
    }

    private void OnDestroy()
    {
        onNodeSelected -= SetNodeDestination;
        DuelManager.instance.onCombatBegin -= delegate { _inCombat = true; };
        DuelManager.instance.onPlayerDefeat -= delegate { _inCombat = false; };
        DuelManager.instance.onPlayerVictory -= delegate { _inCombat = false; };
    }
    
    private void Update()
    {
        moveInput = InputHandler.GetMoveInput();
        rotationInput = InputHandler.GetRotationInput();
    }

    private void LateUpdate()
    {
        //HandleMovement();
        //RotatePlayer();
    }

    private void SetInitialFacingDirection()
    {
        //Set correct starting direction
        var room = DungeonManager.instance.Generator.transform.GetChild(0).GetComponent<DungeonRoom>();
        for (int i = 0; i < room.Nodes.Length; i++)
        {
            if (room.Nodes[i].ConnectedNode != null)
            {
                StartCoroutine(RotatePlayer(room.Nodes[i].Point.position, 0.05f));
                break;
            }
        }
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

        if (rotationInput == 1) StartCoroutine(RotatePlayer(_transform.position + transform.right));
        else if (rotationInput == -1) StartCoroutine(RotatePlayer(_transform.position - transform.right));
        else if (moveInput.y == -1) StartCoroutine(RotatePlayer(_transform.position - transform.forward, 1.4f));
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
            yield return null;
        }
        
        _playerCommander.CommanderCard.OnOccupyNode(node);

        if (moveInput.y == 1)
        {
            var nextNode = BattlefieldManager.instance.GetNode(_transform.position + _transform.forward * 5);
            if (nextNode != null)
            {
                StartCoroutine(MovePlayer(nextNode));
                yield break;
            }
        }

        _transform.position = node.Transform.position;
        _animator.SetFloat("speed", 0);
        _isMoving = false;
    }

    private IEnumerator RotatePlayer(Vector3 pos, float timeToMove = 0.8f)
    {
        _isMoving = true;

        Vector3 direction = (pos - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));

        float t = 0;
        while (t < timeToMove)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, t / timeToMove);

            t += Time.deltaTime;
            yield return null;
        }
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

    private void SetNodeDestination(GridNode node)
    {
        if (_inCombat || _isMoving) return;
        if (node.Occupant != null || node.Obstacle != null)
        {
            StartCoroutine(RotatePlayer(node.Transform.position));
            return;
        }

        var nodePath = BattlefieldManager.instance.FindNodePath(_playerCommander.CommanderCard, node);
        if (nodePath == null)
        {
            StartCoroutine(RotatePlayer(node.Transform.position));
            return;
        }

        StartCoroutine(FollowNodePath(nodePath));
    }

    private IEnumerator FollowNodePath(List<GridNode> nodePath)
    {
        _isMoving = true;
        var currentNode = _playerCommander.CommanderCard.Node;
        if (nodePath[0] == currentNode) nodePath.RemoveAt(0);

        while (nodePath.Count > 0)
        {
            if (_inCombat) break; //stop movement if combat starts

            while (Vector3.Distance(_transform.position, nodePath[0].transform.position) > 0.1f)
            {
                _animator.SetFloat("speed", 1, 0.1f, Time.deltaTime);
                FaceTarget(nodePath[0].Transform.position);
                yield return null;
            }

            nodePath[0].onNodeEntered?.Invoke(_playerCommander.CommanderCard);
            currentNode = nodePath[0];
            nodePath.RemoveAt(0);

            yield return null;
        }

        _animator.SetFloat("speed", 0);
        _playerCommander.CommanderCard.OnOccupyNode(currentNode);
        _transform.position = currentNode.Transform.position;
        _isMoving = false;
    }

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
