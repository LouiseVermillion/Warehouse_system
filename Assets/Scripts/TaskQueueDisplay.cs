using UnityEngine;
using UnityEngine.UI;

public class TaskQueueDisplay : MonoBehaviour
{
    public EnvironmentManager environmentManager;
    public Text queueText;

    void Update()
    {
        if (environmentManager == null || queueText == null)
            return;

        var queue = environmentManager.GetTaskQueue();

        if (queue.Count == 0)
        {
            queueText.text = "📭 Task Queue: Empty";
        }
        else
        {
            queueText.text = "📦 Task Queue:\n";
            foreach (var task in queue)
            {
                queueText.text += $"→ {task}\n";
            }
        }
    }
}
