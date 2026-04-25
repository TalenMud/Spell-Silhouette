using UnityEngine;

/// <summary>
/// AI Creature that wanders randomly until a player enters its detection radius,
/// then chases the player and deals damage on contact.
/// Requires: Rigidbody2D, Collider2D on this GameObject.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class AICreature : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float wanderSpeed = 2f;
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float wanderDirectionChangeInterval = 2f;

    [Header("Detection")]
    [SerializeField] private float detectionRadius = 6f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    [Header("Combat")]
    [SerializeField] private float damagePerSecond = 10f;

    // Internal state
    private Rigidbody2D rb;
    private Transform playerTransform;
    private CreatureState currentState = CreatureState.Wandering;

    private float currentHealth;
    private Vector2 wanderDirection;
    private float wanderTimer;
    private bool isTouchingPlayer = false;
    private float damageTickTimer = 0f;

    private enum CreatureState
    {
        Wandering,
        Chasing
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
    }

    private void Start()
    {
        PickNewWanderDirection();
    }

    private void Update()
    {
        DetectPlayer();
        HandleDamageTick();

        switch (currentState)
        {
            case CreatureState.Wandering:
                UpdateWander();
                break;
            case CreatureState.Chasing:
                UpdateChase();
                break;
        }
    }

    // -------------------------------------------------------------------------
    // Detection
    // -------------------------------------------------------------------------

    private void DetectPlayer()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, detectionRadius, playerLayer);

        if (hit != null)
        {
            playerTransform = hit.transform;
            currentState = CreatureState.Chasing;
        }
        else
        {
            playerTransform = null;
            currentState = CreatureState.Wandering;
        }
    }

    // -------------------------------------------------------------------------
    // Movement
    // -------------------------------------------------------------------------

    private void UpdateWander()
    {
        wanderTimer -= Time.deltaTime;

        if (wanderTimer <= 0f)
        {
            PickNewWanderDirection();
        }

        rb.linearVelocity = wanderDirection * wanderSpeed;
    }

    private void UpdateChase()
    {
        if (playerTransform == null)
        {
            return;
        }

        Vector2 directionToPlayer = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        rb.linearVelocity = directionToPlayer * chaseSpeed;
    }

    private void PickNewWanderDirection()
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        wanderDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        wanderTimer = wanderDirectionChangeInterval;
    }

    // -------------------------------------------------------------------------
    // Damage (dealt to player while touching)
    // -------------------------------------------------------------------------

    private void HandleDamageTick()
    {
        if (!isTouchingPlayer)
        {
            return;
        }

        damageTickTimer -= Time.deltaTime;

        if (damageTickTimer <= 0f)
        {
            damageTickTimer = 1f; // reset to 1-second interval

            if (playerTransform != null)
            {
                PlayerHealth playerHealth = playerTransform.GetComponent<PlayerHealth>();

                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damagePerSecond);
                }
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isTouchingPlayer = true;
            damageTickTimer = 0f; // deal first tick of damage immediately
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isTouchingPlayer = false;
        }
    }

    // -------------------------------------------------------------------------
    // Health
    // -------------------------------------------------------------------------

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        // Extend this with death animation, drops, etc.
        Destroy(gameObject);
    }

    // -------------------------------------------------------------------------
    // Getters
    // -------------------------------------------------------------------------

    public float GetHealthPercent() => currentHealth / maxHealth;

    // -------------------------------------------------------------------------
    // Debug
    // -------------------------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
