import json
import random
import os
import math

# ─── Configuration ──────────────────────────────────────────────────────────
width = 15              # grid width
height = 25             # grid height
max_stack_height = 10   # max boxes per cell
fill_ratio = 0.5        # 0.0–1.0 fill percentage
seed = 42               # RNG seed
output_path = r"C:\Users\61meh\Warehouse_system\Assets\StreamingAssets\inventory.json"
# ────────────────────────────────────────────────────────────────────────────

random.seed(seed)

total_slots = width * height * max_stack_height
num_boxes = math.floor(total_slots * fill_ratio)

stored = []

id_counter = 0
while len(stored) < num_boxes:
    gx = random.randint(0, width - 1)
    gy = random.randint(0, height - 1)
    # count current stack at this cell
    current_stack = sum(1 for b in stored if b["grid"]["x"] == gx and b["grid"]["y"] == gy)
    if current_stack >= max_stack_height:
        continue
    altitude = current_stack
    stored.append({
        "id": f"BOX_{id_counter:04d}",
        "grid": { "x": gx, "y": gy },
        "altitude": altitude
    })
    id_counter += 1

inventory = {"stored": stored}

# Ensure directory exists
os.makedirs(os.path.dirname(output_path), exist_ok=True)
# Write JSON
with open(output_path, "w") as f:
    json.dump(inventory, f, indent=2)

print(f"✅ Inventory generated with {len(stored)} boxes.")
print(f"📦 Saved to: {output_path}")
