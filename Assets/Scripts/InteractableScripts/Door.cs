using UnityEngine;

public enum Direction
{
    North,
    East,
    South,
    West
}
public class Door : MonoBehaviour, IPOI
{
    public GameManager gameManager => GameObject.Find("Canvas").GetComponent<GameManager>();
    public GameObject self => gameObject;
    public bool[] exits;
    public Vector2Int gridPos => Vector2Int.FloorToInt(new Vector2(transform.position.x, transform.position.y));
    public Vector2Int roomPos;
    public Direction direction;

    // when we interact with the door, figure out which room to place
    public void PlaceRoom(bool forceDoor)
    {
        exits = new bool[4];
        Vector2Int doorExit = Vector2Int.zero;
        switch(direction)
        {
            case Direction.North:
                doorExit = Vector2Int.up;
                break;
            case Direction.East:
                doorExit = Vector2Int.right;
                break;
            case Direction.South:
                doorExit = Vector2Int.down;
                break;
            case Direction.West:
                doorExit = Vector2Int.left;
                break;
        }

        // this is the room coordinates of the room we're placing (large grid)
        Vector2Int placeRoomPos = roomPos + doorExit;

        /*
         * N - exits[0]
         * E - exits[1]
         * S - exits[2]
         * W - exits[3]
        */
        // 1. check each surrounding to make sure we match
        // 2. for the ones that don't have a surrounding, randomize
        // 3. if we have to place a door, make sure we do


        // randomize through the presets that have the required doors
        
        // place it
        
        // place a preset over where the door was
        
        // clear the door
    
    }
}
