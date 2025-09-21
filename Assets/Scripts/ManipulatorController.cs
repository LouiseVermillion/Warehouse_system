using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ManipulatorController : MonoBehaviour
{
    public float liftSpeed = 1.5f;
    public float placeDelay = 0.5f;

    private Transform manipulatorVisual;
    private bool isBusy = false;
    private BoxManager boxManager;

    void Start()
    {
        manipulatorVisual = transform;
        boxManager = FindFirstObjectByType<BoxManager>();
    }

    public bool IsBusy() => isBusy;

    public void StartBoxPickup(Vector2Int target2D, string desiredBoxId)
    {
        if (isBusy || boxManager == null) return;
        StartCoroutine(PickupSequence(target2D, desiredBoxId));
    }

    private IEnumerator PickupSequence(Vector2Int target2D, string targetId)
    {
        isBusy = true;

        Stack<BoxManager.BoxData> buffer = new Stack<BoxManager.BoxData>();

        while (true)
        {
            var topBox = boxManager.PeekTopBox(target2D);
            if (topBox == null) break;

            if (topBox.id == targetId)
            {
                Debug.Log($"🤖 Manipulator grabbed target box {topBox.id}!");
                boxManager.PopTopBox(target2D);
                break;
            }
            else
            {
                buffer.Push(boxManager.PopTopBox(target2D));
                Debug.Log($"🔁 Moved box {buffer.Peek().id} off stack...");
            }

            yield return new WaitForSeconds(placeDelay);
        }

        // Put back boxes on nearest stacks
        while (buffer.Count > 0)
        {
            Vector3Int? spot = boxManager.FindNearestFreeSpot(target2D);
            if (spot.HasValue)
            {
                var temp = buffer.Pop();
                boxManager.AddBox(temp.id, spot.Value);
                Debug.Log($"📦 Re-stacked box {temp.id} at {spot.Value}");
            }
            yield return new WaitForSeconds(placeDelay);
        }

        isBusy = false;
    }
}
