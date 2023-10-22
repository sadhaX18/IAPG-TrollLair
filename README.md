# IAPG-TrollLair

# **Troll Lair Simulation**
1. Created By : **Sadhana Krish (EC22185)**
2. Unity Version Used : **2021.3.20f1**  
3. Project build : https://sadhax18.itch.io/iapg-troll-lair  

<br>

## **`Level Generator`**
***
#### Process of Level Generation
    The map is generated using Cellular automata and uses a little initial map modification to control it.
    1. Generate a int[,] map with randomly filled 1's and 0's. 
    2. Randomly select a few splits and fill the split axis with 1's. This is done to form more room like areas in the map
    3. Perform a set amount of cellular automata iterations
    4. Check for connectivity in the map and force connectivity
    4. Use the tilemap visualizer to convert the integer array into a tilemap based level
    4. Spawn entities

``` 
NOTE: The level generation developed from tutorials by Sebastian Lague and Sunny Valley Studios
```

<br>
<br>
<br>


## **`Interactive Agents`**
>First, I would like to establish the setting : A thief enters the trolls layer and tries to steal their precious gem and escape, The trolls are lazy beings who act active when near their chief and the chief who has been studying teleportation magic is now wandering about their Lair
   
### ```Troll```
---
#### 
    The troll is a utility based agent with 3 states.
    1. Idle : The troll lazes about doing nothing if the Thief and TrollChief are not nearby.
    2. Wander : When the TrollChief is nearby, the trolls act like they are scouting the area and wander about.
    3. Attack : When the Thief is nearby, the trolls start chasing the thief and try to catch them.
    
### ```TrollChief```
---
#### 
    The troll Chief is also a utility based agent with 2 states and 1 special ability which can only be used once.
    1. Wander : The troll chief wanders about their layer while searching for any intruders.
    2. Ability : When the Gem is stolen, the troll chief tries to find the troll closest to the thief and swap locations with them.
    3. Attack : When the Thief is nearby, the troll chief starts chasing the thief and try to catch them.

### ```Thief```
---
#### 
    The thief is a completely behaviour tree based agent, with the main goal of collecting the gem and running to the goal. On the way if it notices a torch, it will go to get the torch to increase its perception range or if it notices a troll or a troll chief it will run away from them.