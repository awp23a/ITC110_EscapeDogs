using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// An enumerator to track the current game state
public enum TurnState
{
    Player,
    Enemy,
    Waiting
}

[DefaultExecutionOrder(-200)]
public class GameManager : MonoBehaviour
{
    // SCRIPT REFERENCES - OTHER SCRIPTS USE THESE
    public GridManager gridManager;
    public Registry registry;

    TurnState currentTurn = TurnState.Enemy; // start with the turn being the enemy's turn, so they get a turn to plan their movements

    public bool IsPlayerTurn()
    {
        return currentTurn is TurnState.Player;
    }
    public void StartPlayerTurn()
    {
        currentTurn = TurnState.Player;
    }
    public void StartEnemyTurn()
    {
        currentTurn = TurnState.Enemy;
        StartCoroutine(registry.EnemyTurnSequence());
    }
    public void WaitForAction()
    {
        currentTurn = TurnState.Waiting;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gridManager = GetComponent<GridManager>();
        registry = GetComponent<Registry>();
    }
}
