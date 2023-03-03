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
        if (instance != null)
        {
            Debug.LogWarning("More than one instance of GameManager found.");
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }
    #endregion

    [SerializeField] private Image screenFade;

    [SerializeField] private EffectManager effectManager;
    [SerializeField] private ParticlePool _bloodParticlePool;
    [SerializeField] private ParticlePool _unsummonParticlePool;

    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        if (_unlockedDungeonLevels[0] < 1) _unlockedDungeonLevels[0] = 1; //the first dungeon is always unlocked at the start
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
    private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        if (DungeonManager.instance != null)
        {
            DungeonManager.instance.CreateDungeon(_dungeonLevelToLoad);
            StartCoroutine(WaitForDungeonToLoad());
        }
        else
        {
            //Fade from black to clear on scene loaded
            StartCoroutine(Fade(Color.black, Color.clear));
            _dungeonLevelToLoad = 0;
        }
    }

    //Wait until the dungeon is complete to show view
    private IEnumerator WaitForDungeonToLoad()
    {
        Debug.LogWarning("It froze here once. Need to figure out why.");
        yield return new WaitForSeconds(2.5f);
        /*while (!DungeonManager.instance.DungeonIsReady)
        {
            Debug.Log("Waiting for dungeon to generate.");
            yield return null;
        }*/
        StartCoroutine(Fade(Color.black, Color.clear));
    }

    public static void LoadDungeon(Dungeons dungeon, int floor)
    {
        instance.LoadDungeonLevel(dungeon, floor);
    }

    private void LoadDungeonLevel(Dungeons dungeon, int floor)
    {
        _dungeonLevelToLoad = floor;
        LoadScene(dungeon.ToString());
    }

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
        //Fade to black and then load next scene
        yield return StartCoroutine(Fade(Color.clear, Color.black));

        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator Fade(Color start, Color end)
    {
        float t = 0, timeToFade = 1f;
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

    #region - Save File Info -
    private int[] _unlockedDungeonLevels = new int[2]; //A value of zero means that the dungeon is not available
    public int[] UnlockedDungeonLevels => _unlockedDungeonLevels;
    private int _dungeonLevelToLoad;

    #endregion
}

#region - Enums -
public enum Phase { Begin, Summoning, Attack, End }
public enum CardFocus { Offense, Defense, Utility }
public enum ActionType { Move, Attack, Ability }
public enum CardType { Unit, Structure, Trap, Equipment, Terrain, Spell, Commander }
public enum Faction { Arcane, Kingdom, Goblins, Coven, Undead, Demons}
public enum Dungeons { Catacombs, Woodland_Ruins }
public enum DungeonSize { Small, Medium, Large }

public enum Effects { Damage, Halt, StatModifier }
#endregion