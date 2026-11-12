using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Registry : MonoBehaviour
{
    public GameManager gameManager;
    List<IEnemyTurn> enemies = new();   // the list containing all enemies currently in the game
    public List<GameObject> pois = new();

    
    // REGISTRATION OF ENEMIES AND POIS
    public void RegisterEnemy(IEnemyTurn enemy)
    {
        if( !enemies.Contains(enemy) )
        {
            enemies.Add(enemy);
        }
    }
    public void UnRegisterEnemy(IEnemyTurn enemy)
    {
        if( enemies.Contains(enemy))
        {
            enemies.Remove(enemy);
        }
    }
    public void RegisterPOI(GameObject poi)
    {
        if( !pois.Contains(poi))
        {
            pois.Add(poi);
        }
    }
    public void UnregisterPOI(GameObject poi)
    {
        if (pois.Contains(poi))
        {
            pois.Remove(poi);
        }
    }

    // Allow the enemy/enemies to take their turn(s)
    public IEnumerator EnemyTurnSequence(int turns = -1)
    {
        yield return new WaitForSeconds(0.25f); // inital wait, can be adjusted

        // If the enemy exists, have it take its turn
        foreach (IEnemyTurn enemy in enemies.ToArray())
        {
            if (enemy != null)
            {
                // if turns is -1, then have the enemies all take the number of turns based on their type
                if( turns < 0 )
                {
                    yield return enemy.TakeTurn(enemy.DefaultTurnNumber);
                }
                else
                {
                    yield return enemy.TakeTurn(turns);
                }
                    
            }
        }

        gameManager.StartPlayerTurn(); // hand control back to the player
    }

    // Since the list is private, we need a way to hand what enemy to damage to the player
    public IEnemyTurn GetEnemyAtPosition(Vector3Int pos)
    {
        foreach( IEnemyTurn enemy in enemies)
        {
            if ( enemy.GridPosition == pos )
            {
                return enemy;
            }
        }

        return null;
    }

    // This is the enemies' first turn
    // None of them will move, and will simply plan their movements
    private IEnumerator InitialEnemyPhase()
    {
        yield return null;
        yield return EnemyTurnSequence(1);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameManager = GetComponent<GameManager>();
        StartCoroutine(InitialEnemyPhase());
    }
}
