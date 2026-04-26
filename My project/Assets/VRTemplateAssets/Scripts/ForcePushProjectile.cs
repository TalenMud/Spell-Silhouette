using UnityEngine;

// Invisible force wave that physically knocks Rigidbodies on contact.
// Spawned by SpellCaster when the Force Push gesture is recognized.
public class ForcePushProjectile : MonoBehaviour
{
    public float speed = 10f;
    public float knockForce = 12f;

    void Start() => Destroy(gameObject, 2f);

    void Update() => transform.Translate(Vector3.forward * speed * Time.deltaTime, Space.Self);

    void OnCollisionEnter(Collision col)
    {
        Rigidbody rb = col.rigidbody;
        if (rb == null) return;

        Vector3 dir = (col.transform.position - transform.position).normalized;
        rb.AddForce(dir * knockForce, ForceMode.Impulse);
    }
}
