using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Plane & Grid Settings")]
    public Transform planeTransform;
    public Vector2Int gridSize = new Vector2Int(10, 10);
    public float cellSize = 1f;

    private Vector3 origin;

    void Awake()
    {
        if (planeTransform == null)
        {
            Debug.LogError("Plane not assigned!");
            return;
        }

        // 1) Compute world dimensions from gridSize & cellSize
        float worldW = gridSize.x * cellSize;
        float worldH = gridSize.y * cellSize;

        // 2) Re‐position origin at bottom-left of the plane
        Vector3 center = planeTransform.position;
        origin = new Vector3(
            center.x - worldW / 2f,
            center.y,
            center.z - worldH / 2f
        );

        // 3) (Optional) Auto-scale the plane so it actually covers that area
        //    Unity’s default Plane mesh is 10×10 units per scale=1
        planeTransform.localScale = new Vector3(worldW / 10f,
                                                1f,
                                                worldH / 10f);
    }

    // If you tweak gridSize in the inspector at edit-time,
    // this keeps the plane in sync immediately:
    void OnValidate()
    {
        if (planeTransform == null) return;
        float worldW = gridSize.x * cellSize;
        float worldH = gridSize.y * cellSize;
        planeTransform.localScale = new Vector3(worldW / 10f, 1f, worldH / 10f);
    }

    // grid→world mapping now uses the correct origin & cellSize
    public Vector3 GridToWorld(int x, int y)
        => origin + new Vector3(x * cellSize + cellSize / 2f,
                                0f,
                                y * cellSize + cellSize / 2f);

    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        Vector3 d = worldPos - origin;
        int gx = Mathf.FloorToInt(d.x / cellSize);
        int gy = Mathf.FloorToInt(d.z / cellSize);
        return new Vector2Int(gx, gy);
    }

    public bool IsInsideGrid(Vector2Int pos)
        => pos.x >= 0 && pos.x < gridSize.x
        && pos.y >= 0 && pos.y < gridSize.y;
}
