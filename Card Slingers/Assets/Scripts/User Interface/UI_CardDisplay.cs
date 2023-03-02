using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_CardDisplay : MonoBehaviour
{
    [SerializeField] private RectTransform cardDisplayRect;
    [SerializeField] private Button hideCardDisplayButton;
    [SerializeField] private Image cardIcon;
    [SerializeField] private TMP_Text cardTitle, cardDescription, cardFlavorText, cardCostText;

    private Coroutine lerpCardDisplayCoroutine;
    private Vector3 cardDisplayShownPos = Vector3.zero;
    private Vector3 cardDisplayHiddenPos = new Vector3(500, 0, 0);

    [Header("Permanent Info")]
    [SerializeField] private GameObject _healthParent;
    [SerializeField] private GameObject _defenseParent;
    [SerializeField] private GameObject _attackParent;
    [SerializeField] private GameObject _speedParent;
    [SerializeField] private TMP_Text _healthText, _defenseText, _attackText, _speedText;

    private void Start()
    {
        cardDisplayRect.anchoredPosition = cardDisplayHiddenPos;
        hideCardDisplayButton.onClick.AddListener(HideCardDisplay);
    }

    public void HideCardDisplay()
    {
        if (lerpCardDisplayCoroutine != null) StopCoroutine(lerpCardDisplayCoroutine);
        lerpCardDisplayCoroutine = StartCoroutine(DungeonUIManager.instance.LerpRectTransform(cardDisplayRect, cardDisplayHiddenPos));
    }

    public void ShowCardDisplay(CardSO card)
    {
        cardIcon.sprite = card.icon;
        cardTitle.text = card.name;
        cardDescription.text = card.description;
        cardFlavorText.text = card.flavorText;

        cardCostText.text = card.cost.ToString();

        if (card is UnitSO unit) SetUnitInfo(unit);
        else if (card is StructureSO structure) SetStructureInfo(structure);
        else HideStats();

        //Also show indicator for all stats

        //An indicator for current equipment

        //stat tokens

        if (lerpCardDisplayCoroutine != null) StopCoroutine(lerpCardDisplayCoroutine);
        lerpCardDisplayCoroutine = StartCoroutine(DungeonUIManager.instance.LerpRectTransform(cardDisplayRect, cardDisplayShownPos));
    }

    private void HideStats()
    {
        _healthParent.SetActive(false);
        _defenseParent.SetActive(false);
        _attackParent.SetActive(false);
        _speedParent.SetActive(false);
    }

    private void SetUnitInfo(UnitSO unit)
    {
        _healthParent.SetActive(true);
        _defenseParent.SetActive(true);
        _attackParent.SetActive(true);
        _speedParent.SetActive(true);

        _healthText.text = unit.MaxHealth.ToString();
        _defenseText.text = unit.Defense.ToString();
        _attackText.text = unit.Attack.ToString();
        _speedText.text = unit.Speed.ToString();
    }

    private void SetStructureInfo(StructureSO structure)
    {
        _healthParent.SetActive(true);
        _defenseParent.SetActive(true);
        _attackParent.SetActive(false);
        _speedParent.SetActive(false);

        _healthText.text = structure.MaxHealth.ToString();
        _defenseText.text = structure.Defense.ToString();
    }
}