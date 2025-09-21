using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class TrainerConnector : MonoBehaviour
{
    public bool isTrainingMode = false;
    public string stateFile = "state_input.txt";
    public string actionFile = "action_output.txt";

    private string statePath;
    private string actionPath;

    void Awake()
    {
        statePath = Path.Combine(Application.streamingAssetsPath, stateFile);
        actionPath = Path.Combine(Application.streamingAssetsPath, actionFile);
    }


    /// Sends current state and waits for action to appear
    public int RequestActionFromTrainer(Vector2Int state, int agentId)
    {
        if (!isTrainingMode)
        {
            Debug.LogWarning("Trainer mode not active. Use Q-table.");
            return -1;
        }

        // Write state to file
        string stateData = $"{agentId},{state.x},{state.y}";
        File.WriteAllText(statePath, stateData);

        // Wait for trainer to respond (this is blocking; you can improve this with async/file watcher)
        float timeout = 2f;
        float timer = 0f;

        while (!File.Exists(actionPath))
        {
            timer += Time.deltaTime;
            if (timer > timeout)
            {
                Debug.LogWarning("Trainer response timed out.");
                return -1;
            }
        }

        string result = File.ReadAllText(actionPath);
        File.Delete(actionPath);

        if (int.TryParse(result.Trim(), out int action))
        {
            return action;
        }
        else
        {
            Debug.LogError("Invalid action received from trainer.");
            return -1;
        }
    }
}
