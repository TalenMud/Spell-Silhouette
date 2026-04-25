using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] float maxHealth = 100f;
    float currentHealth;

    public float MaxHealth => maxHealth;
    public float Current => currentHealth;
    public float Normalized => currentHealth / maxHealth;

    public event System.Action<float> OnHealthChanged;
    public event System.Action OnDied;

    void Awake() => currentHealth = maxHealth;

    public void TakeDamage(float amount)
    {
        if (currentHealth <= 0f) return;
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        OnHealthChanged?.Invoke(Normalized);
        if (currentHealth <= 0f) OnDied?.Invoke();
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(Normalized);
    }
}
