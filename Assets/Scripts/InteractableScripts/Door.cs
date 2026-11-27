using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine;
using System.Runtime.CompilerServices;
using UnityEngine.ResourceManagement.AsyncOperations;

public enum Direction
{
    North,
    East,
    South,
    West
}

[DefaultExecutionOrder(-150)]
public class Door : MonoBehaviour, IPOI
{
    public GameManager gameManager => GameObject.Find("Canvas").GetComponent<GameManager>();
    public GameObject self => gameObject;
    public Vector2Int gridPos => Vector2Int.FloorToInt(new Vector2(transform.position.x, transform.position.y));
    public Vector2Int roomPos;
    public Direction direction;
    public IList<GameObject> rooms;
    public GameObject doorUpdate;
    private AsyncOperationHandle<IList<GameObject>> _loadHandle;

    bool activating = false;

    private void Start()
    {
        // Debug.Log($"GameManager Found: {gameManager != null}; GameManager Registry Found: {gameManager.registry != null}");
        gameManager.registry.RegisterPOI(gameObject);
        gameManager.gridManager.grid[new Vector3Int(gridPos.x, gridPos.y, 0)].thingOnTile = Things.Interactable;
        transform.parent = null;

        // load all of the rooms
        _loadHandle = Addressables.LoadAssetsAsync<GameObject>(
            "Rooms",
            loadedAsset =>
            {
                Debug.Log($"Loaded Asset: {loadedAsset.name}");
            }
        );
        _loadHandle.Completed += OnAllAssetsLoaded;
    }

    // when we interact with the door, figure out which room to place
    public void Interact()
    {
        if(activating)
        {
            return;
        }

        activating = true;

        // Debug.Log("Interacting with Door!");
        // bool[] exits = new bool[4]; // we use this to store which exits are forced
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

        // if the room already exists, then we skip the generation
        if ( !gameManager.registry.rooms.ContainsKey(placeRoomPos) )
        {
            int randomRoomNum = UnityEngine.Random.Range(0, rooms.Count);
            GameObject newRoom = Instantiate(rooms[randomRoomNum], 16 * (new Vector3(roomPos.x,roomPos.y,0) + new Vector3(doorExit.x,doorExit.y,0)), transform.rotation);
            GridMergeScript retrieve = newRoom.GetComponent<GridMergeScript>();

            switch(direction)
            {
                case Direction.North:
                    retrieve.entry = EntryDirection.South;
                    break;
                case Direction.East:
                    retrieve.entry = EntryDirection.West;
                    break;
                case Direction.South:
                    retrieve.entry = EntryDirection.North;
                    break;
                case Direction.West:
                    retrieve.entry = EntryDirection.East;
                    break;
            }

            retrieve.placeRoomPos = placeRoomPos;

            gameManager.registry.rooms.Add(placeRoomPos, retrieve.exits);            
        }

        // place the doorUpdate to update the surrounding walls
        Instantiate(doorUpdate, transform.position, transform.rotation);

        // Test Code - Always place a four-way room

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
        Destroy(gameObject);
    }

    void OnAllAssetsLoaded( AsyncOperationHandle<IList<GameObject>> handle )
    {
        if( handle.Status == AsyncOperationStatus.Succeeded )
        {
            rooms = handle.Result;
            Debug.Log($"Loaded {rooms.Count} assets.");
        }
        else
        {
            Debug.Log("Failed to load assets.");
        }
    }

    void OnDestroy()
    {
        if (gameManager != null && gameManager.gridManager != null && gameManager.registry != null && gameManager.gridManager.grid != null && self != null)
        {
            gameManager.gridManager.grid[Vector3Int.FloorToInt(self.transform.position)].thingOnTile = Things.Nothing;
            gameManager.registry.UnregisterPOI(self);
        }
    }
}
