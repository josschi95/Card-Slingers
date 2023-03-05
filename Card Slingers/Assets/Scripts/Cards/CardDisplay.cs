using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardDisplay : MonoBehaviour
{
    [SerializeField] protected GameObject cardGFX;
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text description, flavorText, manaCostText;
    [SerializeField] private Image cardArt;

    [Header("Unit Info")]
    [SerializeField] private GameObject _healthIcon;
    [SerializeField] private GameObject _defenseIcon, attackIcon, speedIcon;
    [SerializeField] private TMP_Text _healthText, _defenseText, _attackText, _speedText;

    public void SetDisplay(CardSO card)
    {
        cardArt.sprite = card.icon;
        title.text = card.name;
        description.text = card.description;
        flavorText.text = card.flavorText;
        manaCostText.text = card.cost.ToString();

        if (card is UnitSO unit) SetUnitInfo(unit);
        else if (card is StructureSO structure) SetStructureInfo(structure);
        else HideStats();
    }

    private void HideStats()
    {
        _healthIcon.SetActive(false);
        _defenseIcon.SetActive(false);
        attackIcon.SetActive(false);
        speedIcon.SetActive(false);
    }

    private void SetUnitInfo(UnitSO unit)
    {
        _healthIcon.SetActive(true);
        _defenseIcon.SetActive(true);
        attackIcon.SetActive(true);
        if (unit.Range > 1) _attackText.text += " (" + unit.Range.ToString() + ")";
        speedIcon.SetActive(true);

        _healthText.text = unit.MaxHealth.ToString();
        _defenseText.text = unit.Defense.ToString();
        _attackText.text = unit.Attack.ToString();
        _speedText.text = unit.Speed.ToString();
    }

    private void SetStructureInfo(StructureSO structure)
    {
        _healthIcon.SetActive(true);
        _defenseIcon.SetActive(true);

        attackIcon.SetActive(false);
        speedIcon.SetActive(false);

        _healthText.text = structure.MaxHealth.ToString();
        _defenseText.text = structure.Defense.ToString();
    }
}
