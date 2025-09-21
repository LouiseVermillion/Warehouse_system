import time
import os
import json
import random

Q_TABLE = {}
ACTIONS = [0, 1, 2, 3]  # Up, Down, Left, Right

ALPHA = 0.1
GAMMA = 0.9
EPSILON = 0.1

STATE_FILE = "StreamingAssets/state_input.txt"
ACTION_FILE = "StreamingAssets/action_output.txt"
QTABLE_FILE = "StreamingAssets/qtable.json"

def get_state():
    if not os.path.exists(STATE_FILE):
        return None

    with open(STATE_FILE, "r") as f:
        line = f.read().strip()

    try:
        agent_id, x, y = map(int, line.split(","))
        return f"{agent_id}_{x}_{y}"
    except:
        return None

def choose_action(state):
    if random.random() < EPSILON or state not in Q_TABLE:
        return random.choice(ACTIONS)

    q_values = Q_TABLE[state]
    return max(range(len(q_values)), key=lambda i: q_values[i])

def update_q_table(state, action, reward, next_state):
    if state not in Q_TABLE:
        Q_TABLE[state] = [0.0] * len(ACTIONS)
    if next_state not in Q_TABLE:
        Q_TABLE[next_state] = [0.0] * len(ACTIONS)

    old_q = Q_TABLE[state][action]
    max_next_q = max(Q_TABLE[next_state])
    new_q = old_q + ALPHA * (reward + GAMMA * max_next_q - old_q)
    Q_TABLE[state][action] = new_q

def write_action(action):
    with open(ACTION_FILE, "w") as f:
        f.write(str(action))

def main():
    print("Trainer started.")
    while True:
        state = get_state()
        if state is None:
            time.sleep(0.1)
            continue

        action = choose_action(state)

        # Dummy reward logic (for now)
        next_state = state  # could be simulated
        reward = -0.1 if state == next_state else 1.0

        update_q_table(state, action, reward, next_state)
        write_action(action)

        os.remove(STATE_FILE)

if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print("Training stopped.")
        with open(QTABLE_FILE, "w") as f:
            json.dump(Q_TABLE, f, indent=2)
