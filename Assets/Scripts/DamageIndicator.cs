using UnityEngine;
using UnityEngine.UI;
using System.Collections;
 
public class DamageIndicator : MonoBehaviour
{
    [Header("UI Reference")]
    public Image damageOverlay; 
 
    [Header("Settings")]
    public float flashAlpha    = 0.4f; 
    public float fadeDuration  = 0.5f; 
 
    private Player player;
    private Coroutine flashCoroutine;
 
    private void Start()
    {
        player = GetComponent<Player>();
 
        if (player == null)
        {
            Debug.LogWarning("DamageIndicator: No Player component found!");
            return;
        }
 
        player.OnHealthChanged += OnDamageTaken;
 
        if (damageOverlay != null)
        {
            SetOverlayAlpha(0f);
        }
    }
 
    private void OnDestroy()
    {
        if (player != null)
        {
            player.OnHealthChanged -= OnDamageTaken;
        }
    }
 
    private float lastHealth = 100f;
 
    private void OnDamageTaken(float currentHealth, float maxHealth)
    {
        if (currentHealth < lastHealth)
        {
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashRed());
        }
 
        lastHealth = currentHealth;
    }
 
    private IEnumerator FlashRed()
    {
        SetOverlayAlpha(flashAlpha);
 
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(flashAlpha, 0f, elapsed / fadeDuration);
            SetOverlayAlpha(alpha);
            yield return null;
        }
 
        SetOverlayAlpha(0f);
    }
 
    private void SetOverlayAlpha(float alpha)
    {
        if (damageOverlay != null)
        {
            Color c = damageOverlay.color;
            c.a = alpha;
            damageOverlay.color = c;
        }
    }
}