using UnityEngine;

public class FireballProjectile : MonoBehaviour
{
    public float speed = 8f;

    void Start() => Destroy(gameObject, 2.5f);

    void Update() => transform.Translate(Vector3.forward * speed * Time.deltaTime, Space.Self);
}
