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

    [SerializeField] private CommanderSO playerCommander;
    [SerializeField] private float _inputSensitivity = 7.5f;
    [SerializeField] private float _rotationSpeed = 10f;
    [SerializeField] private Transform _deckPocket;
    private PathNode _currentWaypoint;

    public DungeonRoom currentRoom { get; private set; }
    public Transform DeckPocket => _deckPocket;
    private Vector3 rotationInput;

    private Animator _animator;
    private bool _isMoving;
    private bool _inCombat;

    public Transform rayPos;
    /*
     * For changing to manual input for movement, basically keep the same set up but the player would have to hold down W to change the animator speed from 0 to 1
     * Of course there would also have to be a bool for _allowInput so player can't move during combat, or when they're being sent to the middle after combat
     * This actually works pretty well even when not on a track. The only issue is that I'll then have to add colliders back onto walls
     * And I can't do that without majorly fucking up the dungeon generation
    */

    private void Start()
    {
        CreateCommander();
        DuelManager.instance.onMatchStarted += delegate { _inCombat = true; };
        DuelManager.instance.onPlayerDefeat += delegate { _inCombat = false; };
        DuelManager.instance.onPlayerVictory += delegate { _inCombat = false; };

        onRoomEntered += (room) => currentRoom = room;
    }

    private void Update()
    {
        rotationInput.y = InputHandler.GetRotationInput();
    }

    private void LateUpdate()
    {
        RotatePlayer();
    }

    private void RotatePlayer()
    {
        if (_inCombat || _isMoving) return;
        transform.localEulerAngles += rotationInput * 10 * _inputSensitivity * Time.deltaTime;
    }

    private void OnDestroy()
    {
        DuelManager.instance.onMatchStarted -= delegate { _inCombat = true; };
        DuelManager.instance.onPlayerDefeat -= delegate { _inCombat = false; };
        DuelManager.instance.onPlayerVictory -= delegate { _inCombat = false; };
    }

    private void CreateCommander()
    {
        var player = GetComponent<PlayerCommander>();
        player.OnAssignCommander(playerCommander);
        player.CommanderCard.OnCommanderSummon();
        _animator = player.CommanderCard.PermanentObject.GetComponent<Animator>();

        //_animator.speed = 2;
    }

    #region - Movement -
    public static void SetWaypoint(PathNode point)
    {
        instance.SetPlayerWaypoint(point);
    }

    public static void SetDestination(Vector3 destination)
    {
        instance.SetPlayerDestination(destination);
    }

    private void SetPlayerWaypoint(PathNode point)
    {
        if (_isMoving || _inCombat) return;
        StartCoroutine(MoveToWaypoint(point));
    }

    private void SetPlayerDestination(Vector3 destination)
    {
        if (_isMoving || _inCombat) return;
        StartCoroutine(MoveToPosition(destination));
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

    private void FaceTarget(Vector3 pos) //update this to accept a Transform transform?
    {
        Vector3 direction = (pos - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * _rotationSpeed);
    }
    #endregion

    #region - Deck -
    private void PlaceInPocket(List<Card> cards)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].gameObject.transform.SetParent(_deckPocket, false);
        }
    }

    public static void PlaceDeckInPocket(List<Card> cards)
    {
        instance.PlaceInPocket(cards);
    }
    #endregion
}
