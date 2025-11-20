using UnityEngine;

public class DustGridGenerator : MonoBehaviour
{
    public GameObject dustTilePrefab;
    public Material dustMaterial;
    public int gridWidth = 10;
    public int gridHeight = 10;
    public float tileSize = 1f;

    void Start()
    {
        GenerateDustGrid();
    }

    void GenerateDustGrid()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                Vector3 localPosition = new Vector3(
                    x * tileSize - (gridWidth * tileSize / 2),
                    0.02f,
                    z * tileSize - (gridHeight * tileSize / 2)
                );

                Vector3 worldPosition = transform.position + localPosition;

                GameObject tile = Instantiate(dustTilePrefab, worldPosition, Quaternion.identity, transform);

                if (dustMaterial != null)
                {
                    Renderer renderer = tile.GetComponent(typeof(Renderer)) as Renderer;
                    if (renderer != null)
                    {
                        renderer.material = dustMaterial;
                    }
                }
            }
        }
    }
}