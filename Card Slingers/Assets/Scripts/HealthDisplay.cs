using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image fill;

    private Transform cam;
    private Card_Unit permanent;

    private void Start()
    {
        permanent = GetComponentInParent<Card_Unit>();
        nameText.text = permanent.CardInfo.name;

        if (permanent.Commander is PlayerCommander) nameText.color = Color.green;
        else nameText.color = Color.red;

        permanent.onValueChanged += UpdateHealth;
        UpdateHealth();

        cam = Camera.main.transform;
    }

    private void UpdateHealth()
    {
        float f = permanent.CurrentHealth;
        fill.fillAmount = f / permanent.MaxHealth;
    }

    private void OnDestroy()
    {
        if (permanent != null)
            permanent.onValueChanged -= UpdateHealth;
    }

    void LateUpdate()
    {
        transform.forward = cam.forward;
    }
}
