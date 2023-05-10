using UnityEngine;

public class TestLSystem : MonoBehaviour
{
    [SerializeField] private int resolution;
    [SerializeField] private int chunkResolution;
    [SerializeField] private LSystem.Settings settings;

    private LSystem lSystem;

    private void Start()
    {
        lSystem = new LSystem(settings);
    }

    private void Update()
    {
        for(int i = 0; i < 10; i++)
        {
            lSystem.Step(out Vector3 position, out bool isFinished);
            if (!isFinished)
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.position = position;
            }
        }
    }
}
