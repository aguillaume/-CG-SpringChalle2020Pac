# -CG-SpringChalle2020Pac

Pac Challenge
- If blocked and is enemy, change type to eat the enemy. 
- if blocked and is me then one blocked pac wait a turn
- path by A* instead of Distance because distance cause 
- don't go on tile that has enemy pac of opposite type 


Closest reWorked

for each pac
    find closet super pellet
if more than one pac has the same target, compare distance to this target then find new target for the pacs that arent the closest (remove this target for possibilities) repeat until all pacs have target.

FORWARD Rule is not right. Need to revise. Needs to be where you are going, not where you were coming from.
- seed=2946908039706620400
- https://www.codingame.com/share-replay/460836705