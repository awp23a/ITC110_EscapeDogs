using System.Collections;
using UnityEngine;

public interface IEnemyTurn
{
    int DefaultTurnNumber { get; }
    int TurnDelay { get; }
    int CurrentDelay { get; set;  }
    IEnumerator TakeTurn(int turns);
    Vector3Int GridPosition { get; }
}