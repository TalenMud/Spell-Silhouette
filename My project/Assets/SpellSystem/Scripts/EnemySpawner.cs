using UnityEngine;

// Spawns a black box enemy in the scene at startup.
// Attach this to any active GameObject (e.g. XR Origin or a Manager object).
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] Vector3 spawnPosition = new Vector3(0f, 0.5f, 3f);
    [SerializeField] Vector3 enemyScale = new Vector3(0.6f, 1.8f, 0.3f);

    void Start() => SpawnEnemy(spawnPosition);

    public GameObject SpawnEnemy(Vector3 position)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Enemy";
        go.transform.position = position;
        go.transform.localScale = enemyScale;

        // Black material
        var renderer = go.GetComponent<Renderer>();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = Color.black;
        renderer.material = mat;

        // Physics
        var rb = go.AddComponent<Rigidbody>();
        rb.mass = 10f;

        // Damage script
        go.AddComponent<Enemy>();

        return go;
    }
}
