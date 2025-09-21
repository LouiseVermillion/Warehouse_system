import json
import random
import os
import math

# --- Configuration ---
width = 30
height = 30
max_stack_height = 10
fill_ratio = 0.5
seed = 38

# Output paths (set to your project structure)
inventory_path = r"C:\Users\61meh\Warehouse_system\Assets\StreamingAssets\inventory.json"
stored_path = r"C:\Users\61meh\Warehouse_system\Assets\StreamingAssets\stored.json"
# ----------------------

random.seed(seed)

total_slots = width * height * max_stack_height
num_boxes = math.floor(total_slots * fill_ratio)

stored_boxes = []
id_counter = 0

while len(stored_boxes) < num_boxes:
    gx = random.randint(0, width - 1)
    gy = random.randint(0, height - 1)
    current_stack = sum(1 for b in stored_boxes if b["grid"]["x"] == gx and b["grid"]["y"] == gy)
    if current_stack >= max_stack_height:
        continue
    altitude = current_stack
    stored_boxes.append({
        "id": f"BOX_{id_counter:04d}",
        "grid": { "x": gx, "y": gy },
        "altitude": altitude
    })
    id_counter += 1

# Write inventory.json (boxes in warehouse)
inventory = {"stored": stored_boxes}
os.makedirs(os.path.dirname(inventory_path), exist_ok=True)
with open(inventory_path, "w") as f:
    json.dump(inventory, f, indent=2)

# Write stored.json (delivered boxes) -- initially empty
# THIS IS THE CORRECTED PART
delivered = {"taken": []} 
with open(stored_path, "w") as f:
    json.dump(delivered, f, indent=2)

print(f"✅ Inventory generated with {len(stored_boxes)} boxes.")
print(f"📦 Saved to: {inventory_path}")
print(f"✅ Stored (delivered) list created at: {stored_path} with the correct format.")