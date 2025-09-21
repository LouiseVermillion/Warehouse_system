using UnityEngine;

public class AgentController : MonoBehaviour
{
    public GridManager gridManager;
    public Vector2Int currentPos = new Vector2Int(0, 0); // Başlangıç grid konumu

    void Start()
    {
        MoveToGrid(currentPos);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W)) TryMove(Vector2Int.up);
        if (Input.GetKeyDown(KeyCode.S)) TryMove(Vector2Int.down);
        if (Input.GetKeyDown(KeyCode.A)) TryMove(Vector2Int.left);
        if (Input.GetKeyDown(KeyCode.D)) TryMove(Vector2Int.right);
    }

    void TryMove(Vector2Int direction)
    {
        Vector2Int newPos = currentPos + direction;
        if (gridManager.IsInsideGrid(newPos))
        {
            currentPos = newPos;
            MoveToGrid(currentPos);
        }
    }

    void MoveToGrid(Vector2Int gridPos)
    {
        transform.position = gridManager.GridToWorld(gridPos.x, gridPos.y);
    }
}
