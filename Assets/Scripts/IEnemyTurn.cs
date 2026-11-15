using System.Collections;
using UnityEngine;

public interface IEnemyTurn
{
    public int DefaultTurnNumber { get; set; }
    public int TurnDelay { get; set; }
    public int CurrentDelay { get; set; }
    IEnumerator TakeTurn(int turns);
    Vector3Int GridPosition { get; set; }
}