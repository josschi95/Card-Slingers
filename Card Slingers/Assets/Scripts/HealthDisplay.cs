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
        permanent = GetComponentInParent<Summon>().Card as Card_Unit;
        nameText.text = permanent.CardInfo.name;

        if (permanent.isPlayerCard) nameText.color = Color.green;
        else nameText.color = Color.red;

        permanent.onValueChanged += UpdateHealth;
        UpdateHealth();

        cam = Camera.main.transform;
    }

    private void UpdateHealth()
    {
        float f = permanent.CurrentHealth;
        fill.fillAmount = f / permanent.MaxHealth;

        if (permanent.CurrentHealth <= 0)
        {
            Destroy(gameObject, 0.1f);
        }
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
