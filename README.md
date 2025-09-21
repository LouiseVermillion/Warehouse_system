Hi,

Here is a fully automated warehouse simulation that allows users to only insert a box number on a panel that gives tasks to nearest robot to take assigned box and deliver it automatically.

Robot find shortest path using Q-values, that is pretrained in a clamped area around random robot. This helps modularity with when the given task number is outside of the trained area and not in Q-table, it clamped and can be used.

Multiple robots can work in warehouse simultaneously. A FSM (Finite State Machine) used to control task system.

There is a FallBack mechanism used for when robots stuck or their movement fell into repetition. A* algorithm used for FallBack. When "stuckness" is solved, robots used Q-values for movement again.
