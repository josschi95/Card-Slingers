using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PermanentSO : CardSO
{
    [Header("Permanent Properties")]
    [SerializeField] protected GameObject _permanentPrefab;
    public GameObject Prefab => _permanentPrefab;
}
