using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Enemy : MonoBehaviour
{
    [SerializeField] float contactDamage = 10f;

    void OnCollisionEnter(Collision collision)
    {
        var health = collision.gameObject.GetComponentInParent<PlayerHealth>();
        if (health != null)
            health.TakeDamage(contactDamage);
    }
}
