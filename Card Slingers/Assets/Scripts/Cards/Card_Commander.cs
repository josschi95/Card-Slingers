using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card_Commander : Card_Unit 
{
    public void SetStartingNode(GridNode node)
    {
        OnOccupyNode(node);
    }

    public void OnCommanderSummon()
    {
        _currentHealth = NetMaxHealth();

        //Instantiate permanent
        var permanent = CardInfo as PermanentSO;
        _permanentObject = Instantiate(permanent.Prefab, transform.position, transform.rotation, gameObject.transform);
        _animator = PermanentObject.GetComponent<Animator>();
        Commander.animator = _animator;

        cardGFX.SetActive(false); //Disable the physical card display
        var coll = GetComponent<Collider>();
        if (coll != null) coll.enabled = false;
    }

    public override void OnTakeDamage(int damage)
    {
        damage = Mathf.Clamp(damage - Defense, 0, int.MaxValue);
        _currentHealth -= damage;

        if (_currentHealth <= 0)
        {
            _animator.SetTrigger("death");
            if (Commander is PlayerCommander) DuelManager.instance.onPlayerDefeat?.Invoke();
            else DuelManager.instance.onPlayerVictory?.Invoke();
        }
        else
        {
            _animator.SetTrigger("damage");
        }
    }
}
