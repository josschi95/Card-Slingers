using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Returns the particle system to the pool when the OnParticleSystemStopped event is received
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class ReturnToPool : MonoBehaviour
{
    private ParticleSystem system;
    public IObjectPool<ParticleSystem> pool;

    private void Start()
    {
        system = GetComponent<ParticleSystem>();
        var main = system.main;
        main.stopAction = ParticleSystemStopAction.Callback;
    }

    private void OnParticleSystemStopped()
    {
        //Return to pool
        pool.Release(system);
    }
}