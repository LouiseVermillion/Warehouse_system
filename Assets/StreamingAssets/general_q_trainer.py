import random
import json

GRID_WIDTH = 10
GRID_HEIGHT = 10
EPISODES_PER_PAIR = 200
MAX_STEPS = 100
ALPHA = 0.1
GAMMA = 0.95
EPSILON = 0.1
ACTIONS = [0, 1, 2, 3]  # up, down, left, right

q_table = {}

def get_state_key(x, y, agent_id=0):
    return f"{agent_id}_{x}_{y}"

def get_next_position(x, y, action):
    if action == 0: y += 1  # up
    elif action == 1: y -= 1  # down
    elif action == 2: x -= 1  # left
    elif action == 3: x += 1  # right

    x = max(0, min(GRID_WIDTH - 1, x))
    y = max(0, min(GRID_HEIGHT - 1, y))
    return x, y

# Iterate over every possible (start, goal) pair
for start_x in range(GRID_WIDTH):
    for start_y in range(GRID_HEIGHT):
        for goal_x in range(GRID_WIDTH):
            for goal_y in range(GRID_HEIGHT):
                if (start_x, start_y) == (goal_x, goal_y):
                    continue  # skip same cell

                for episode in range(EPISODES_PER_PAIR):
                    x, y = start_x, start_y

                    for step in range(MAX_STEPS):
                        state = get_state_key(x, y)

                        if state not in q_table:
                            q_table[state] = [0.0] * len(ACTIONS)

                        # ε-greedy
                        if random.random() < EPSILON:
                            action = random.choice(ACTIONS)
                        else:
                            action = max(range(4), key=lambda a: q_table[state][a])

                        next_x, next_y = get_next_position(x, y, action)
                        next_state = get_state_key(next_x, next_y)

                        if next_state not in q_table:
                            q_table[next_state] = [0.0] * len(ACTIONS)

                        reward = 1.0 if (next_x, next_y) == (goal_x, goal_y) else -0.01

                        old_q = q_table[state][action]
                        max_next_q = max(q_table[next_state])
                        new_q = old_q + ALPHA * (reward + GAMMA * max_next_q - old_q)
                        q_table[state][action] = new_q

                        x, y = next_x, next_y

                        if (x, y) == (goal_x, goal_y):
                            break  # reached target

print("Running...")

with open("qtable.json", "w") as f:
    json.dump(q_table, f, indent=2)

print("Training complete. Q-table saved to qtable.json")
