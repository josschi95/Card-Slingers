using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Commander", menuName = "Scriptable Objects/Commander")]
public class CommanderSO : ScriptableObject
{
    new public string name;
    [SerializeField] private Faction _faction;
    public Faction Faction => _faction;
    [Space]
    [SerializeField] private GameObject _commanderPrefab;
    public GameObject CommanderPrefab => _commanderPrefab;
    [Space]
    [SerializeField] private Deck _deck;
    public Deck Deck => _deck;

    [Header("Unit Properties")]
    public int maxHealth;
    public int attack;
    public int defense;
    public int speed;
}