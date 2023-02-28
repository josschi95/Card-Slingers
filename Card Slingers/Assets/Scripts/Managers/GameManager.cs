using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    #region - Singleton -
    public static GameManager instance;
    private void Awake()
    {
        instance = this;
    }
    #endregion

    [SerializeField] private Image screenFade;
    private Color fadeEmpty = Color.clear;
    private Color fadeFull = Color.black;

    [SerializeField] private EffectManager effectManager;
    [SerializeField] private ParticlePool _bloodParticlePool;
    [SerializeField] private ParticlePool _unsummonParticlePool;

    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        StartCoroutine(Fade(Color.black, Color.clear));
    }

    #region - Effects -
    //This can very likel just be moved to its own script to not clog up this one
    public static void OnApplyEffect(Card_Permanent card, Effects effect, int magnitude = 1, UnitStat stat = UnitStat.Health)
    {
        instance.effectManager.OnApplyEffect(card, effect, magnitude, stat);
    }

    public static void OnApplyEffect(Card_Permanent card, EffectHolder effect)
    {
        instance.effectManager.OnApplyEffect(card, effect.effect, effect.magnitude, effect.modifiedStat);
    }
    #endregion

    #region - Particles -
    //Same as above, can probabl have some sort of ParticleManager script
    public void GetBloodParticles(Vector3 pos)
    {
        _bloodParticlePool.Pool.Get().transform.position = pos;
    }

    public void GetUnsummonParticles(Vector3 pos)
    {
        _unsummonParticlePool.Pool.Get().transform.position = pos;
    }
    #endregion

    #region - Scene Loading -
    public static void OnLoadScene(string sceneName)
    {
        instance.LoadScene(sceneName);
    }

    private void LoadScene(string sceneName)
    {
        StartCoroutine(FadeOutAndLoadScene(sceneName));
    }

    private IEnumerator FadeOutAndLoadScene(string sceneName)
    {
        StartCoroutine(Fade(Color.clear, Color.black));
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator Fade(Color start, Color end)
    {
        float t = 0, timeToFade = 0.5f;
        screenFade.color = start;
        while (t < timeToFade)
        {
            screenFade.color = Color.Lerp(start, end, t / timeToFade);
            t += Time.deltaTime;
            yield return null;
        }
        screenFade.color = end;

    }
    #endregion
}
public enum Phase { Begin, Summoning, Attack, End }
public enum CardFocus { Offense, Defense, Utility }
public enum ActionType { Move, Attack, Ability }
public enum CardType { Unit, Structure, Trap, Equipment, Terrain, Spell, Commander }
public enum Faction { Arcane, Kingdom, Goblins, Coven, Undead, Demons}
public enum DungeonSize { Small, Medium, Large }

public enum Effects { Damage, Halt, StatModifier }
