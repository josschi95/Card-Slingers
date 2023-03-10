using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Summon : MonoBehaviour
{
    [SerializeField] private Transform _transform;
    private Card_Permanent _card;
    public Transform Transform => _transform;
    public Card_Permanent Card
    {
        get => _card;
        set
        {
            _card = value;
        }
    }

    public void OnDamage()
    {
        GameManager.instance.GetBloodParticles(_transform.position + Vector3.up);
    }

    #region - Facing -
    public void FaceTargetCoroutine(Vector3 pos)
    {
        StartCoroutine(TurnToFacePosition(pos));
    }

    private IEnumerator TurnToFacePosition(Vector3 pos)
    {
        float t = 0, timeToMove = 0.5f;
        while (t < timeToMove)
        {
            FaceTarget(pos);
            t += Time.deltaTime;
            yield return null;
        }
    }

    public void FaceTarget(Vector3 pos) //update this to accept a Transform transform?
    {
        Vector3 direction = (pos - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 25f);
    }
    #endregion

    public void DestroyAfterDelay(float delay)
    {
        StartCoroutine(Destroy(delay));
    }

    private IEnumerator Destroy(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
}
