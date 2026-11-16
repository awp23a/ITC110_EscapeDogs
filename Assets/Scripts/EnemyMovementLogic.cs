using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum PlannedAction
{
    Move,
    Attack
}

public class EnemyMovementLogic : MonoBehaviour
{
    public PlannedAction plannedAction = PlannedAction.Move;
    public EnemyController enemy;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        enemy = GetComponent<EnemyController>();
    }

    // ATTACK BEHAVIOR
    public void Attack(Vector3Int targetTile)
    {
        Tile attackedTile = enemy.gameManager.gridManager.grid[targetTile];
        bool containsPlayer = attackedTile.thingOnTile is Things.Player;

        // if there is a player on the tile, attack
        // we can use the same random in the initial condition if we add dodge
        if( containsPlayer )
        {
            // here, we get the player's script
            PlayerController player = GameObject.Find("Player (Placeholder)").GetComponent<PlayerController>();

            // check to see if the attack crits
            float critCheck = UnityEngine.Random.Range(0f, 1f);
            bool crit = critCheck < enemy.critChance;
            int damage = crit ? (int)(enemy.critMult * enemy.atk) : enemy.atk;

            // deal damage to the enemy
            player.hp = Mathf.Max(0, player.hp - damage);
        }
    }

    // MOVEMENT PLANNING
    public Vector3Int BasicEnemyMove(Vector3Int pos)
    {
        Vector3Int move = Vector3Int.zero;
        Things[] acceptableThings = {
            Things.Nothing,
            Things.Player,
            Things.Enemy
        };

        // find the optimal tile
        // if sitting next to the player, don't move
        Vector3Int[] Directions = { Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down };
        int distance = enemy.gameManager.gridManager.grid[pos].distanceFromPlayer;

        //Debug.Log($"Enemy distance: {distance}");
        foreach (Vector3Int direction in Directions)
        {
            if (enemy.gameManager.gridManager.grid.GetValueOrDefault(pos + direction, null) == null || !acceptableThings.Contains(enemy.gameManager.gridManager.grid[pos + direction].thingOnTile))
            {
                // if the tile doesn't exist or has something on it, it isn't considered
                // if the tile has a playerFollower or Enemy on it, it will (stupidly) sit and wait for it to move
                continue;
            }
            int tDistance = enemy.gameManager.gridManager.grid[pos + direction].distanceFromPlayer;

            // Either: 
            //  - Tile is closer than the current best move
            //  - Tile is the same distance, but there is currently nothing on it
            // And the tile is not non-existent
            //Debug.Log(
            //    $"Enemy at {convertedPos}, " +
            //    $"direction {direction}, " +
            //    $"tile type {gm.gm.grid[convertedPos + direction].thingOnTile}, " +
            //    $"current distance {distance}, " +
            //    $"neighbor distance {tDistance}"
            //);
            bool betterDistance = tDistance < distance;
            bool sameDistanceUnoccupied = tDistance == distance &&
                move != Vector3Int.zero &&
                enemy.gameManager.gridManager.grid[pos + direction].thingOnTile is Things.Nothing &&
                enemy.gameManager.gridManager.grid[pos + move].thingOnTile is Things.Enemy;

            if (tDistance >= 0 && (betterDistance || sameDistanceUnoccupied))
            {
                distance = tDistance;
                move = direction;
            }
        }

        //Debug.Log(
        //    $"Enemy selected move {move}, " +
        //    $"planned action {plannedAction}"
        //);

        // if we planned a move into the tile the player is in, then we planned an attack
        if (enemy.gameManager.gridManager.grid[pos + move].thingOnTile is Things.Player)
        {
            plannedAction = PlannedAction.Attack;
            // Debug.Log($"Enemy plans to attack in direction {move}");
        }

        // when we exit, we have the best possible move for this logic stored in "move"
        return move;
    }

    public Vector3Int SmartEnemyMove(Vector3Int pos)
    {
        Vector3Int move = Vector3Int.zero;
        Things[] acceptableThings = {
            Things.Nothing,
            Things.Player,
            Things.Enemy
        };

        // Djikstra's algorithm
        // Dictionary to store tiles, like the grid manager
        Dictionary<Vector3Int, (int distance, Vector3Int cameFrom)> pathData = new();

        List<Vector3Int> openSet = new();

        // start similarly to the BFS
        pathData[pos] = (0, pos);
        openSet.Add(pos);

        Vector3Int[] directions =
        {
            Vector3Int.right,
            Vector3Int.left,
            Vector3Int.up,
            Vector3Int.down
        };

        // difference starts here
        // instead of just adding 1, we add a variable amount based on what's on the tile
        // if a faster path is found to a tile, take it
        while (openSet.Count > 0)
        {
            // starting place
            Vector3Int current = openSet[0];

            // start from the first tile that has the lowest distance
            foreach (Vector3Int position in openSet)
            {
                if (pathData[position].distance < pathData[current].distance)
                {
                    current = position;
                }
            }

            openSet.Remove(current);

            if (enemy.gameManager.gridManager.grid[current].thingOnTile is Things.Player)
            {
                break;
            }

            foreach (Vector3Int direction in directions)
            {
                if (!enemy.gameManager.gridManager.grid.ContainsKey(current + direction) || !acceptableThings.Contains(enemy.gameManager.gridManager.grid[current + direction].thingOnTile))
                {
                    // grid doesn't have the tile, skip it
                    continue;
                }

                // determine the tile's cost
                int cost;
                switch (enemy.gameManager.gridManager.grid[current + direction].thingOnTile)
                {
                    case Things.Enemy:
                        cost = (int)Math.Sqrt(enemy.gameManager.gridManager.grid.Count);
                        break;
                    default:
                        cost = 1;
                        break;
                }

                // assign the cost to the tile, and the direction it came from
                int newDistance = pathData[current].distance + cost;

                if (!pathData.ContainsKey(current + direction) || newDistance < pathData[current + direction].distance)
                {
                    pathData[current + direction] = (newDistance, current);
                    if (!openSet.Contains(current + direction))
                    {
                        openSet.Add(current + direction);
                    }
                }
            }
        }

        Vector3Int step = Vector3Int.FloorToInt(enemy.gameManager.gridManager.playerMovePoint.transform.position);

        while (pathData[step].cameFrom != pos)
        {
            step = pathData[step].cameFrom;
        }

        move = step - pos;

        if (enemy.gameManager.gridManager.grid[pos + move].thingOnTile is Things.Player)
        {
            plannedAction = PlannedAction.Attack;
            // Debug.Log($"Enemy plans to attack in direction {move}");
        }

        return move;
    }

    public Vector3Int GoalCamp(Vector3Int pos)
    {
        Vector3Int move = Vector3Int.zero;
        Things[] acceptableThings = {
            Things.Nothing,
            Things.Player,
            Things.Enemy
        };

        // find the optimal tile
        // if sitting next to the closest goal, don't move
        Vector3Int[] Directions = { Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down };
        int distance = enemy.gameManager.gridManager.grid[pos].distanceFromPOI;

        // Two Cases:
        // First: next to player - attack towards player
        // Second : not next to player - move to be adjacent to goal
        if (enemy.gameManager.gridManager.grid[pos].distanceFromPlayer == 1)
        {
            // Debug.Log("Player Adjacent: ATTACK");
            foreach (Vector3Int direction in Directions)
            {
                if (enemy.gameManager.gridManager.grid.ContainsKey(pos + direction))
                    // if we're at 1, a surrounding tile is 0
                    if (enemy.gameManager.gridManager.grid[pos + direction].distanceFromPlayer == 0)
                    {
                        move = direction;
                    }
            }
            plannedAction = PlannedAction.Attack;
        }
        else
        {
            // prioritize tiles closer to the player
            foreach (Vector3Int direction in Directions)
            {
                if (enemy.gameManager.gridManager.grid.GetValueOrDefault(pos + direction, null) == null || !acceptableThings.Contains(enemy.gameManager.gridManager.grid[pos + direction].thingOnTile))
                {
                    // if the tile doesn't exist or has something on it, it isn't considered
                    // if the tile has a playerFollower or Enemy on it, it will (stupidly) sit and wait for it to move
                    continue;
                }
                int tDistance = enemy.gameManager.gridManager.grid[pos + direction].distanceFromPOI;
                int tPlayerDistance = enemy.gameManager.gridManager.grid[pos + direction].distanceFromPlayer;

                // Either: 
                //  - Tile is closer than the current best move
                //  - Tile is the same distance, but there is currently nothing on it
                // And the tile is not non-existent
                //Debug.Log(
                //    $"Enemy at {convertedPos}, " +
                //    $"direction {direction}, " +
                //    $"tile type {gm.gm.grid[convertedPos + direction].thingOnTile}, " +
                //    $"current distance {distance}, " +
                //    $"neighbor distance {tDistance}"
                //);
                bool betterDistance = tDistance < distance;
                bool sameDistance = tDistance == distance &&
                    move != Vector3Int.zero;
                bool unoccupied = enemy.gameManager.gridManager.grid[pos + direction].thingOnTile is Things.Nothing &&
                    enemy.gameManager.gridManager.grid[pos + move].thingOnTile is Things.Enemy;
                bool closerToPlayer = tPlayerDistance < enemy.gameManager.gridManager.grid[pos].distanceFromPlayer;

                //Debug.Log($"Direction Considered: {direction}\nDistance to Goal: {tDistance}\nDistance to Player: {tPlayerDistance}");

                if (tDistance > 0 && (betterDistance || (sameDistance && (unoccupied || closerToPlayer))))
                {
                    distance = tDistance;
                    move = direction;
                    //Debug.Log("Direction Accepted");
                }
            }

            // if we planned a move into the tile the player is in, then we planned an attack
            if (enemy.gameManager.gridManager.grid[pos + move].thingOnTile is Things.Player)
            {
                plannedAction = PlannedAction.Attack;
                Debug.Log($"Enemy plans to attack in direction {move}");
            }
        }

        //Debug.Log($"Planned Move: {move}");
        // when we exit, we have the best possible move for this logic stored in "move"
        return move;
    }

    public Vector3Int GoalProtect(Vector3Int pos)
    {
        Vector3Int move = Vector3Int.zero;
        Things[] acceptableThings = {
            Things.Nothing,
            Things.Player,
            Things.Enemy
        };
        int guardDistance = 4;
        int provokeDistance = 3;
        int chaseDistance = 8;
        bool provoked = false;

        // three modes:
        // seek - go towards the nearest object, just use GoalCamp
        // protect - do not consider tiles further from the goal, prioritize tiles closer to the player
        // chase - if the player enters provoke distance from the enemy, charge them until they are outside of the chase distance
        if (enemy.gameManager.gridManager.grid[pos].distanceFromPOI > guardDistance && !provoked)
        {
            // seek
            move = GoalCamp(pos);
        }
        else
        {
            // either protect or chase
            provoked = (!provoked && enemy.gameManager.gridManager.grid[pos].distanceFromPlayer <= provokeDistance)
                || (enemy.gameManager.gridManager.grid[pos].distanceFromPOI <= chaseDistance && provoked);

            // we'll use directions regardless
            Vector3Int[] Directions = { Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down };
            int distance = enemy.gameManager.gridManager.grid[pos].distanceFromPlayer;

            foreach (Vector3Int direction in Directions)
            {
                Vector3Int consideredTile = pos + direction;
                if (!enemy.gameManager.gridManager.grid.ContainsKey(consideredTile))
                {
                    // tile DNE, don't consider it
                    continue;
                }

                // if tile exists, store its distance to the player
                // prefer that, but do not exceed guard / chase distance
                int tPlayerDistance = enemy.gameManager.gridManager.grid[consideredTile].distanceFromPlayer;
                int tGoalDistance = enemy.gameManager.gridManager.grid[consideredTile].distanceFromPOI;

                bool betterDistance = tPlayerDistance < distance;
                bool sameDistanceUnoccupied = tPlayerDistance == distance &&
                    move != Vector3Int.zero &&
                    enemy.gameManager.gridManager.grid[pos + direction].thingOnTile is Things.Nothing &&
                    enemy.gameManager.gridManager.grid[pos + move].thingOnTile is Things.Enemy;

                if ((betterDistance || sameDistanceUnoccupied) && ((!provoked && tGoalDistance <= guardDistance) || (provoked && tGoalDistance <= chaseDistance)))
                {
                    distance = tPlayerDistance;
                    move = direction;
                }
            }
        }

        // see if we planned an attack
        if (enemy.gameManager.gridManager.grid[pos + move].thingOnTile is Things.Player)
        {
            plannedAction = PlannedAction.Attack;
            // Debug.Log($"Enemy plans to attack in direction {move}");
        }

        return move;
    }

    public Vector3Int PredictiveEnemyMove(Vector3Int pos)
    {
        int smartDistance = 5;
        Vector3Int move = Vector3Int.zero;
        Things[] acceptableThings = {
            Things.Nothing,
            Things.Player,
            Things.Enemy
        };

        // Djikstra's algorithm with an influence map
        // Dictionary to store tiles, like the grid manager
        Dictionary<Vector3Int, (int distance, Vector3Int cameFrom)> pathData = new();

        List<Vector3Int> openSet = new();

        // start similarly to the BFS
        pathData[pos] = (0, pos);
        openSet.Add(pos);

        Vector3Int[] directions =
        {
            Vector3Int.right,
            Vector3Int.left,
            Vector3Int.up,
            Vector3Int.down
        };

        Dictionary<Vector3Int, int> influence = new();
        Vector3Int target = Vector3Int.zero;

        // PREDICTION TIME
        if (enemy.gameManager.gridManager.grid[pos].distanceFromPlayer <= smartDistance)
        {
            // if we're close enough, charge!!!
            target = Vector3Int.FloorToInt(enemy.gameManager.gridManager.playerMovePoint.transform.position);
        }
        else
        {
            // otherwise, predict
            influence.Add(Vector3Int.FloorToInt(enemy.gameManager.gridManager.playerMovePoint.transform.position), 1);

            int steps = 3;

            for (int i = 0; i < steps; i++)
            {
                Queue<Vector3Int> tempTiles = new();
                // how many steps to look ahead
                foreach (Vector3Int possibleTile in influence.Keys)
                {

                    // look through all currently examined tiles
                    foreach (Vector3Int direction in directions)
                    {
                        // add 1 to the surrounding tiles
                        if (enemy.gameManager.gridManager.grid.ContainsKey(possibleTile + direction))
                        {
                            tempTiles.Enqueue(possibleTile + direction);
                        }
                    }

                    tempTiles.Enqueue(possibleTile);
                }

                // add all the extra tiles to the influence
                while (tempTiles.Count > 0)
                {
                    Vector3Int possibleTile = tempTiles.Dequeue();
                    if (!influence.ContainsKey(possibleTile))
                    {
                        influence.Add(possibleTile, 1);
                    }
                    else
                    {
                        influence[possibleTile] += 1;
                    }
                }
            }

            // count all the "heat" the player gave off
            int sum = 0;
            foreach (Vector3Int tile in influence.Keys)
            {
                sum += influence[tile];
            }

            // generate a random number between 0 and sum
            // add the heat values of tiles in the dictionary until rSum >= random, then keep that one
            int rSum = 0;
            int targetValue = UnityEngine.Random.Range(0, sum + 1);
            foreach (Vector3Int tile in influence.Keys)
            {
                target = tile;
                rSum += influence[tile];
                if (rSum >= targetValue)
                {
                    break;
                }
            }
        }


        // difference starts here
        // instead of just adding 1, we add a variable amount based on what's on the tile
        // if a faster path is found to a tile, take it
        while (openSet.Count > 0)
        {
            // starting place
            Vector3Int current = openSet[0];

            // start from the first tile that has the lowest distance
            foreach (Vector3Int position in openSet)
            {
                if (pathData[position].distance < pathData[current].distance)
                {
                    current = position;
                }
            }

            openSet.Remove(current);

            if (current == target)
            {
                break;
            }

            foreach (Vector3Int direction in directions)
            {
                if (!enemy.gameManager.gridManager.grid.ContainsKey(current + direction) || !acceptableThings.Contains(enemy.gameManager.gridManager.grid[current + direction].thingOnTile))
                {
                    // grid doesn't have the tile, skip it
                    continue;
                }

                // determine the tile's cost
                int cost;
                switch (enemy.gameManager.gridManager.grid[current + direction].thingOnTile)
                {
                    case Things.Enemy:
                        cost = (int)Math.Sqrt(enemy.gameManager.gridManager.grid.Count);
                        break;
                    case Things.Player or Things.Nothing or Things.Enemy:
                        cost = 1;
                        break;
                    default:
                        continue;
                }

                // assign the cost to the tile, and the direction it came from
                int newDistance = pathData[current].distance + cost;

                if (!pathData.ContainsKey(current + direction) || newDistance < pathData[current + direction].distance)
                {
                    pathData[current + direction] = (newDistance, current);
                    if (!openSet.Contains(current + direction))
                    {
                        openSet.Add(current + direction);
                    }
                }
            }
        }

        Vector3Int step = target;

        while (pathData[step].cameFrom != pos)
        {
            step = pathData[step].cameFrom;
        }

        move = step - pos;

        if (enemy.gameManager.gridManager.grid[pos + move].thingOnTile is Things.Player)
        {
            plannedAction = PlannedAction.Attack;
            // Debug.Log($"Enemy plans to attack in direction {move}");
        }

        return move;
    }
}
