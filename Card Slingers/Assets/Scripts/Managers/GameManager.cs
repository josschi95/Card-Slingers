using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    #region - Singleton -
    public static GameManager instance;
    private void Awake()
    {
        instance = this;
    }
    #endregion

    [SerializeField] private EffectManager effectManager;
    [SerializeField] private ParticlePool _bloodParticlePool;

    public static void OnApplyEffect(Card_Permanent card, Effects effect, int magnitude = 1, UnitStat stat = UnitStat.Health)
    {
        instance.effectManager.OnApplyEffect(card, effect, magnitude, stat);
    }

    public static void OnApplyEffect(Card_Permanent card, EffectHolder effect)
    {
        instance.effectManager.OnApplyEffect(card, effect.effect, effect.magnitude, effect.modifiedStat);
    }

    public void GetBloodParticles(Vector3 pos)
    {
        _bloodParticlePool.Pool.Get().transform.position = pos;
    }

}
public enum Phase { Begin, Summoning, Attack, End }
public enum CardFocus { Offense, Defense, Utility }
public enum ActionType { Move, Attack, Ability }
public enum CardType { Unit, Structure, Trap, Equipment, Terrain, Spell, Commander }
public enum Faction { Arcane, Kingdom, Goblins, Coven, Undead, Demons}

public enum Effects { Damage, Halt, StatModifier }
