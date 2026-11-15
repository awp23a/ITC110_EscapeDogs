using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(0)]
public class PlayerController : MonoBehaviour
{
    // SCRIPT REFERENCE
    public GameManager gameManager;  // GameManager - the script that links to everything else
    public CreatureAnimationManager animManager;    // AnimationManager - send commands to the animation manager to control sprite animation

    // INPUT SCHEMES
    public InputActionReference playerMove;
    public InputActionReference playerWait;

    // OTHER VARIABLES
    public float moveSpeed = 5f;    // adjust this to change how fast the sprite moves to its logic point
    public Transform logicPoint;

    // ATTACK / DEFENSE DATA
    // INT values are actual values
    // FLOAT values are percentages or multipliers
    public int maxhp = 1000;
    public int hp = 1000;
    public float def = 0f;
    public int atk = 50;
    public float critChance = 0.01f;
    public float critMult = 1.5f;
    public float healPercent = 1.00f;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        logicPoint.parent = null;   // unassign the parent so movement is not infinite
    }

    // Update is called once per frame
    void Update()
    {
        if( hp <= 0 )
        {
            // call the lose function here
        }

        // repeated condition we've checked: is the player sprite close to the player's position
        bool inPosition = Vector3.Distance(transform.position, logicPoint.position) <= 0.5f;

        // check for wait first
        if( gameManager.IsPlayerTurn()  && playerWait.action.WasPressedThisFrame() && inPosition )
        {
            // player's turn and they want to wait
            gameManager.StartEnemyTurn();
        }

        // if we didn't wait, then check to see if we moved
        Vector2Int moveDirection = Vector2Int.FloorToInt(playerMove.action.ReadValue<Vector2>());
        if( gameManager.IsPlayerTurn() && moveDirection != Vector2Int.zero && inPosition )
        {
            gameManager.WaitForAction();
            Move(moveDirection);
        }

        // regardless of if the logic point moved, the sprite moves towards the logic point
        transform.position = Vector3.MoveTowards(transform.position, logicPoint.position, moveSpeed * Time.deltaTime);
    }

    // Move Function - controls the position of the logicPoint, checking it against the walkable grid
    void Move(Vector2Int move)
    {
        // normalize movement to one orthogonal direction
        if( move.x != 0 )
        {
            move.y = 0;
        }

        // see if the tile exists and is empty
        Vector3Int targetTile = new Vector3Int(move.x + (int)logicPoint.position.x, move.y + (int)logicPoint.position.y, 0);
        if( gameManager.gridManager.grid.ContainsKey(targetTile) )
        {
            switch(gameManager.gridManager.grid[targetTile].thingOnTile )
            {
                case Things.Nothing:
                    // update the things on the tiles we're moving between
                    gameManager.gridManager.grid[Vector3Int.FloorToInt(logicPoint.position)].thingOnTile = Things.Nothing;
                    gameManager.gridManager.grid[targetTile].thingOnTile = Things.Player;

                    // move the player
                    logicPoint.position += new Vector3(move.x, move.y, 0);

                    // recalculate the distances from the player
                    gameManager.gridManager.CalculatePlayerDistances();

                    // IF WE INSERT A SKIP TURN CHANCE
                    // insert if-else block here with random

                    // start the enemies' turns
                    gameManager.StartEnemyTurn();
                    break;
                case Things.Enemy:
                    // insert attack function here
                    gameManager.StartEnemyTurn();
                    break;
                case Things.Interactable:
                    // insert pickup function here
                    gameManager.StartEnemyTurn();
                    break;
                default:
                    // something on tile that we can't do anything with, restart the turn to be forgiving
                    gameManager.StartPlayerTurn();
                    break;
            }
        }
    }

    // Attack Function
    private void Attack(Vector3Int targetTile)
    {
        // we only call this if the tile exists, and there is an enemy on it
        IEnemyTurn enemyScript = gameManager.registry.GetEnemyAtPosition(targetTile);

        if( enemyScript == null )
        {
            return;
        }

        GameObject enemy = ((MonoBehaviour)enemyScript).gameObject;
        EnemyController target =  enemy.GetComponent<EnemyController>();

        // check to see if the attack crits
        float critCheck = Random.Range(0f,1f);
        bool crit = critCheck < critChance;
        int damage = crit ? (int)(critMult * atk) : atk;

        // deal damage to the enemy
        target.hp = Mathf.Max(0, target.hp - damage);
    }
}
