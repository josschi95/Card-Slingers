using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card_Commander : Card_Unit 
{
    public override void OnTakeDamage(int damage)
    {
        _animator.SetTrigger("damage");
        damage = Mathf.Clamp(damage - Defense, 0, int.MaxValue);
        _currentHealth -= damage;
        Debug.Log(CardInfo.name + " takes " + damage + " damage!");

        //The commander has been defeated, end the match
        if (_currentHealth <= 0)
        {
            _animator.SetTrigger("death");
            DuelManager.instance.onCommanderDefeated?.Invoke(Commander);
        }
    }
}
