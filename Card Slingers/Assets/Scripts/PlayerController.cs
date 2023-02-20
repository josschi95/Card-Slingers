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

    [SerializeField] private CommanderSO playerCommander;

    private Animator _animator;
    private bool _isMoving;
    private bool _inCombat;



    void Start()
    {
        CreateCommander();
    }

    private void CreateCommander()
    {
        var player = GetComponent<PlayerCommander>();
        player.OnAssignCommander(playerCommander);
        player.CommanderCard.OnCommanderSummon();
        _animator = player.CommanderCard.PermanentObject.GetComponent<Animator>();
    }

    #region - Movement -
    public static void SetWaypoint(Waypoint point)
    {
        instance.SetPlayerWaypoint(point);
    }

    private void SetPlayerWaypoint(Waypoint point)
    {
        if (_isMoving || _inCombat) return;
        StartCoroutine(MoveToPosition(point));
    }

    private IEnumerator MoveToPosition(Waypoint point)
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

        point.OnWaypointReached();
    }

    void FaceTarget(Vector3 pos) //update this to accept a Transform transform?
    {
        Vector3 direction = (pos - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
    }
    #endregion
}
