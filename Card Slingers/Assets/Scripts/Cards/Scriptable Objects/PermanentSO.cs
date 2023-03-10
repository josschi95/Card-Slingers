using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PermanentSO : CardSO
{
    [Header("Permanent Properties")]
    [SerializeField] protected Summon _summonPrefab;
    public Summon Prefab => _summonPrefab;
}
