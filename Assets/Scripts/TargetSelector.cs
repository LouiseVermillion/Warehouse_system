using UnityEngine;
using System.Collections.Generic;

public class TargetSelector : MonoBehaviour
{
    public Camera mainCamera;
    public GridManager gridManager;
    public EnvironmentManager environmentManager;
    public GameObject highlightPrefab;

    // Stores all active highlights per grid cell
    private Dictionary<Vector2Int, GameObject> highlights = new();

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Vector3 clickPosition = hit.point;
                Vector2Int targetCell = gridManager.WorldToGrid(clickPosition);

                if (gridManager.IsInsideGrid(targetCell))
                {
                    // Add task to the environment's queue
                    environmentManager.EnqueueTask(targetCell);

                    // Show visual highlight
                    CreateHighlight(targetCell);
                }
            }
        }
    }

    void CreateHighlight(Vector2Int cell)
    {
        if (highlights.ContainsKey(cell))
            return; // already highlighted

        Vector3 worldPos = gridManager.GridToWorld(cell.x, cell.y);
        GameObject highlight = Instantiate(highlightPrefab, worldPos + Vector3.up * 0.01f, Quaternion.Euler(90, 0, 0));
        highlights[cell] = highlight;
    }

    public void RemoveHighlight(Vector2Int cell)
    {
        if (highlights.TryGetValue(cell, out GameObject highlight))
        {
            Destroy(highlight);
            highlights.Remove(cell);
        }
    }

    public void ClearAllHighlights()
    {
        foreach (var h in highlights.Values)
            Destroy(h);

        highlights.Clear();
    }
}
