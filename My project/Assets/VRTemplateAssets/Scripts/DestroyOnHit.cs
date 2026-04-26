using UnityEngine;

// Attach to any target. Plays a sound at the hit point then destroys the GameObject.
public class DestroyOnHit : MonoBehaviour
{
    [SerializeField] AudioClip hitSound;
    [SerializeField] float volume = 1f;

    void OnCollisionEnter(Collision col)
    {
        if (hitSound != null)
            AudioSource.PlayClipAtPoint(hitSound, transform.position, volume);

        Destroy(gameObject);
    }
}
