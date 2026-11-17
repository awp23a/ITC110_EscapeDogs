using System.Collections;
using UnityEngine;

public enum EnemyLogic
{
    basic,      // uses "dumbMove" logic to swarm
    fast,       // uses "smartMove" logic to hunt
    tank,       // uses "campGoal" logic to interfere
    brute,      // uses "guardGoal" logic
    blitz,      // uses "predictive" logic
}

[DefaultExecutionOrder(10)]
public class EnemyController : MonoBehaviour, IEnemyTurn
{
    // SCRIPT REFERENCES
    public GameManager gameManager;
    public EnemyMovementLogic movementLogic;
    public CreatureAnimationManager animManager;

    // VARIABLES
    public int DefaultTurnNumber{ get; set; }
    public int TurnDelay{ get; set; }
    public int CurrentDelay{ get; set; }
    public float moveSpeed = 5f;
    public Transform logicPoint;
    Vector3Int plannedMove = Vector3Int.zero;
    public Vector3Int GridPosition{ get; set; }
    public EnemyLogic logic;
    
    // ATTACK / DEFENSE DATA
    // INT values are actual values
    // FLOAT values are percentages or multipliers
    public int maxhp;
    public int hp;
    public float def;
    public int atk;
    public float critChance;
    public float critMult;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        logicPoint.parent = null;   // unassign the parent so movement is not infinite
        gameManager.registry.RegisterEnemy(this);   // register this enemy so it will take turns

        GridPosition = Vector3Int.FloorToInt(transform.position);
        gameManager.gridManager.grid[GridPosition].thingOnTile = Things.Enemy;
        // Debug.Log($"Thing on tile {GridPosition} assigned: {gameManager.gridManager.grid[GridPosition].thingOnTile}");

        movementLogic = GetComponent<EnemyMovementLogic>();

        GridPosition = Vector3Int.FloorToInt(logicPoint.position);

        DefaultTurnNumber = 1;
        TurnDelay = 1;
        CurrentDelay = 0;
        // CHANGE IENEMYTURN VALUES BASED ON TYPE
        switch (logic)
        {
            case EnemyLogic.basic:
                maxhp = 350;
                hp = maxhp;
                def = 0.05f;
                atk = 25;
                critChance = 0.05f;
                critMult = 1.5f;
                break;
            case EnemyLogic.fast:
                maxhp = 275;
                hp = maxhp;
                def = 0.00f;
                atk = 35;
                critChance = 0.05f;
                critMult = 1.75f;
                TurnDelay = 0;
                break;
            case EnemyLogic.tank:
                maxhp = 500;
                hp = maxhp;
                def = 0.10f;
                atk = 20;
                critChance = 0.01f;
                critMult = 1.5f;
                break;
            case EnemyLogic.brute:
                maxhp = 1000;
                hp = maxhp;
                def = 0.25f;
                atk = 40;
                critChance = 0.15f;
                critMult = 1.25f;
                TurnDelay = 2;
                break;
            case EnemyLogic.blitz:
                maxhp = 200;
                hp = maxhp;
                def = 0.00f;
                atk = 50;
                critChance = 0.15f;
                critMult = 2.0f;
                TurnDelay = 0;
                DefaultTurnNumber = 2;
                break;
            default:
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, logicPoint.position, moveSpeed * Time.deltaTime);
    }

    private void OnDestroy()
    {
        if (gameManager != null && gameManager.gridManager != null)
        {
            gameManager.registry.UnRegisterEnemy(this);
        }
        if (logicPoint != null && gameManager.gridManager.grid != null)
        {
            gameManager.gridManager.grid[GridPosition].thingOnTile = Things.Nothing;
            // Debug.Log($"Thing on tile {GridPosition} assigned: {gameManager.gridManager.grid[GridPosition].thingOnTile}");
        }
        if (logicPoint != null)
        {
            Destroy(logicPoint.gameObject);
        }
    }

    public IEnumerator TakeTurn(int turns)
    {

        if (CurrentDelay != 0)
        {
            // the enemy is on "Cooldown"
            CurrentDelay -= 1;

            // Debug.Log($"Stored Move: {plannedMove}; Executing!");
            // the enemy will still attempt to execute its stored move, then clear it
            if (movementLogic.plannedAction is PlannedAction.Attack)
            {
                movementLogic.Attack(GridPosition + plannedMove);
                movementLogic.plannedAction = PlannedAction.Move;
            }
            else
            {
                yield return ExecuteMove(plannedMove);
            }

            // Debug.Log("This turn was a skipped turn, reset move to 0 so it doesn't keep moving");
            plannedMove = Vector3Int.zero;
        }
        else
        {
            // reset the delay/cooldown
            CurrentDelay = TurnDelay;

            // we will take a number of turns equal to the passed in number
            for( int i = 0; i < turns; i++ )
            {
                // execute the stored move
                if (movementLogic.plannedAction is PlannedAction.Attack)
                {
                    movementLogic.Attack(GridPosition + plannedMove);
                    movementLogic.plannedAction = PlannedAction.Move;
                }
                else
                {
                    yield return ExecuteMove(plannedMove);
                }

                // depending on the state, plan a different move
                switch(logic)
                {
                    case EnemyLogic.basic:
                        plannedMove = movementLogic.BasicEnemyMove(GridPosition);
                        break;

                    case EnemyLogic.fast:
                        plannedMove = movementLogic.SmartEnemyMove(GridPosition);
                        break;

                    case EnemyLogic.tank:
                        plannedMove = movementLogic.GoalCamp(GridPosition);
                        break;

                    case EnemyLogic.brute:
                        plannedMove = movementLogic.GoalProtect(GridPosition);
                        break;

                    case EnemyLogic.blitz:
                        plannedMove = movementLogic.PredictiveEnemyMove(GridPosition);
                        break;

                    default:
                        yield return null;
                        break;
                }
            }
        }
    }

    private IEnumerator ExecuteMove(Vector3Int move)
    {
        Tile plannedTile = gameManager.gridManager.grid[GridPosition + move];

        // check to see if the move is still valid
        // an enemy can plan to move to where another enemy is, in case that enemy moves out of the way and acts earlier
        if( !gameManager.gridManager.grid.ContainsKey(GridPosition + move) || move == Vector3Int.zero || plannedTile.thingOnTile is not Things.Nothing )
        {
            // Debug.Log($"Something on planned tile ({plannedTile.gridPos.x},{plannedTile.gridPos.y}): {plannedTile.thingOnTile}");
            yield break;
        }

        gameManager.gridManager.grid[GridPosition].thingOnTile = Things.Nothing;
        // Debug.Log($"Thing on tile {GridPosition} assigned: {gameManager.gridManager.grid[GridPosition].thingOnTile}");
        // the enemy will never plan to move out of bounds
        // at this point, we know the move is valid, so just execute it
        logicPoint.position += move;
        GridPosition = Vector3Int.FloorToInt(logicPoint.position);
        gameManager.gridManager.grid[GridPosition].thingOnTile = Things.Enemy;
        // Debug.Log($"Thing on tile {GridPosition} assigned: {gameManager.gridManager.grid[GridPosition].thingOnTile}");

        // IEnumerator return
        yield return null;
    }
}
