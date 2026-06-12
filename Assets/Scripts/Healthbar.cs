using UnityEngine;
using UnityEngine.UI;
 
public class HealthBar : MonoBehaviour
{
    [Header("Entity to Track")]
    public Entity trackedEntity;

    [Header("UI References")]
    public Slider healthSlider;
    public Image fillImage;
 
    [Header("Colors")]
    public Color fullHealthColor  = Color.green;
    public Color lowHealthColor   = Color.red;
 
    private void Start()
    {
        if (trackedEntity == null)
        {
            Debug.LogWarning("HealthBar: No entity assigned!");
            return;
        }
 
        trackedEntity.OnHealthChanged += UpdateHealthBar;
 
        if (healthSlider != null)
        {
            healthSlider.maxValue = trackedEntity.maxHealth;
            healthSlider.value    = trackedEntity.health;
        }
 
        UpdateColor(trackedEntity.health, trackedEntity.maxHealth);
    }
 
    private void OnDestroy()
    {
        if (trackedEntity != null)
        {
            trackedEntity.OnHealthChanged -= UpdateHealthBar;
        }
    }
 
    private void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }
 
        UpdateColor(currentHealth, maxHealth);
    }
 
    // Smoothly transitions fill color from green to red as health drops
    private void UpdateColor(float currentHealth, float maxHealth)
    {
        if (fillImage != null)
        {
            float healthPercent = currentHealth / maxHealth;
            fillImage.color = Color.Lerp(lowHealthColor, fullHealthColor, healthPercent);
        }
    }
}