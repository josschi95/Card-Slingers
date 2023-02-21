using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private static PlayerController instance;
    private void Awake()
    {
        instance = this;
    }

    [SerializeField] private Waypoint _currentWaypoint;
    [SerializeField] private CommanderSO playerCommander;
    [SerializeField] private float inputSensitivity = 50f;
    [SerializeField] private Transform _deckPocket;

    public Transform DeckPocket => _deckPocket;
    private Vector3 rotationInput;

    private Animator _animator;
    private bool _isMoving;
    private bool _inCombat;

    private void Start()
    {
        CreateCommander();
        DuelManager.instance.onMatchStarted += delegate { _inCombat = true; };
        DuelManager.instance.onPlayerDefeat += delegate { _inCombat = false; };
        DuelManager.instance.onPlayerVictory += delegate { _inCombat = false; };
    }

    public void SetStartingWaypoint(Waypoint point)
    {
        _currentWaypoint = point;
        transform.position = _currentWaypoint.transform.position;
        transform.rotation = _currentWaypoint.transform.rotation;
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
        transform.localEulerAngles += rotationInput * inputSensitivity * Time.deltaTime;
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
        _animator.speed = 2;
    }

    #region - Movement -
    public static void SetWaypoint(Waypoint point)
    {
        instance.SetPlayerWaypoint(point);
    }

    public static void SetDestination(Vector3 destination)
    {
        instance.SetPlayerDestination(destination);
    }

    private void SetPlayerWaypoint(Waypoint point)
    {
        if (_isMoving || _inCombat) return;
        StartCoroutine(MoveToWaypoint(point));
    }

    private void SetPlayerDestination(Vector3 destination)
    {
        if (_isMoving || _inCombat) return;
        StartCoroutine(MoveToPosition(destination));
    }

    private IEnumerator MoveToWaypoint(Waypoint point)
    {
        _isMoving = true;

        while (Vector3.Distance(transform.position, point.transform.position) > 0.1f)
        {
            _animator.SetFloat("speed", 1, 0.1f, Time.deltaTime);
            FaceTarget(point.transform.position);
            yield return null;
        }

        transform.position = point.transform.position;
        _animator.SetFloat("speed", 0);
        _isMoving = false;

        point.OnWaypointReached(_currentWaypoint);
        _currentWaypoint = point;
    }

    private IEnumerator MoveToPosition(Vector3 point)
    {
        _isMoving = true;

        while (Vector3.Distance(transform.position, point) > 0.1f)
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
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
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
