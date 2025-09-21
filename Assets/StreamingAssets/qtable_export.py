import json

def save_q_table(q_table, filename="StreamingAssets/qtable.json"):
    with open(filename, "w") as f:
        json.dump(q_table, f, indent=2)

# Example call (if running after training)
# save_q_table(Q_TABLE)
