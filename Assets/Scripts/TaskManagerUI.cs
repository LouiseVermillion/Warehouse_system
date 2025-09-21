using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TaskManagerUI : MonoBehaviour
{
    public TMP_InputField boxIdInput;
    public Button assignButton;
    public EnvironmentManager environment;
    [SerializeField] private BoxManager boxManager;
    [SerializeField] private RobotArmAgent robotArmAgent;
    [SerializeField] private EnvironmentManager environmentManager;

    void Start()
    {
        assignButton.onClick.AddListener(AssignTask);
    }


    void AssignUnstackingTasks(string targetBoxId)
    {
        var boxManager = environment.GetComponent<BoxManager>();
        var tasks = boxManager.GetUnstackingTasks(targetBoxId);

        foreach (var task in tasks)
        {
            RobotArmAgent best = FindNearestIdleAgent(task.from);
            if (best == null)
            {
                Debug.LogWarning("❌ No available robot for task.");
                return;
            }

            if (!task.isFinalTarget)
            {
                best.AssignMoveBoxTask(task.boxId, task.from, task.to);
            }
            else
            {
                best.AssignTakeBoxTask(task.boxId, task.from);
            }
        }
    }

    public RobotArmAgent FindNearestIdleAgent(Vector2Int target)
    {
        RobotArmAgent best = null;
        float bestDist = float.MaxValue;
        foreach (var agent in environment.agents)
        {
            if (!agent.IsBusy())
            {
                float dist = Vector2Int.Distance(agent.gridPosition, target);
                if (dist < bestDist)
                {
                    best = agent;
                    bestDist = dist;
                }
            }
        }
        return best;
    }

    public Queue<(string boxId, Vector2Int from, Vector2Int to, bool isFinalTarget)> pendingTasks = new();

    public void AssignTask()
    {
        string boxId = boxIdInput.text.Trim();
        if (string.IsNullOrEmpty(boxId)) return;

        var boxManager = this.boxManager != null ? this.boxManager : FindAnyObjectByType<BoxManager>();
        if (boxManager == null)
        {
            Debug.LogWarning("❌ BoxManager not assigned.");
            return;
        }

        var tasks = boxManager.GetUnstackingTasks(boxId);
        if (tasks == null || tasks.Count == 0)
        {
            Debug.LogWarning($"❌ Box {boxId} not found or no tasks generated.");
            return;
        }

        pendingTasks = new Queue<(string, Vector2Int, Vector2Int, bool)>(tasks);
        AssignNextPendingTask();
    }

    public void AssignNextPendingTask()
    {
        if (pendingTasks.Count == 0) return;

        var task = pendingTasks.Dequeue();
        RobotArmAgent best = FindNearestIdleAgent(task.from);
        if (best == null)
        {
            Debug.LogWarning("❌ No available robot for task.");
            return;
        }

        if (!task.isFinalTarget)
        {
            best.AssignMoveBoxTask(task.boxId, task.from, task.to);
        }
        else
        {
            best.AssignTakeBoxTask(task.boxId, task.from);
        }
    }



}
