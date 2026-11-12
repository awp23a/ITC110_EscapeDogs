using System;
using System.Collections.Generic;
using UnityEditor.Build.Content;
using UnityEngine;
using UnityEngine.Tilemaps;

// Self-Explanatory Struct
// Used by tiles to track their coordinates
public struct coordinates
{
    public int x;
    public int y;
}

// An enumerator used by tiles to track what is on them
// Add to this as needed, it will not break anything that is currently made,
//      but you might need to change certain functions to account for the new values
// We use this in most of the enemy movement functions
public enum Things
{
    Nothing,
    Player,
    Enemy,
    Interactable
}

// This is the backbone of the grid
public class Tile
{
    // ATTRIBUTES
    public coordinates gridPos;
    public Things thingOnTile;

    // the following two values are used by dumber enemies to reduce strain
    public int distanceFromPlayer;
    public int distanceFromPOI;

    // CONSTRUCTOR
    public Tile( int x, int y)
    {
        gridPos.x = x;
        gridPos.y = y;
        thingOnTile = Things.Nothing;
        distanceFromPlayer = -1;
        distanceFromPOI = -1;
    }
}

// Specifying the execution order since the grid has to generate before anything else
[DefaultExecutionOrder(-100)]
public class GridManager : MonoBehaviour
{
    public GameManager gameManager;
    public Tilemap walkableFloor;   // the grid layer that counts as a "floor"
    public Dictionary<Vector3Int, Tile> grid = new();   // the collection of all tiles, stored by coordinates
    public GameObject playerMovePoint;  // the logical location of the player
    Vector3Int playerGridPos;   // the player's world position, floored to a readable key

    void Start()
    {
        gameManager = GetComponent<GameManager>();
        InitializeGrid();   // load all the tiles initially on screen
        playerGridPos = Vector3Int.FloorToInt(playerMovePoint.transform.position);  // set the player's starting position
        grid[playerGridPos].thingOnTile = Things.Player;    // make sure the tile knows it has a player on it
        CalculatePlayerDistances();
        // We only call CalculatePOIDistances when a POI is created/destroyed
    }

    // Only call this ONCE. It completely rewrites the grid
    void InitializeGrid()
    {
        // Create a grid, where each data point can potentially have a tile
        walkableFloor.CompressBounds();
        BoundsInt bounds = walkableFloor.cellBounds;
        
        // For each position within bounds, see if it has a tile
        foreach( var pos in bounds.allPositionsWithin )
        {
            // If it has a tile, add it to grid (the dictionary), with its key as its floored world position
            if( walkableFloor.HasTile(pos) )
            {
                Vector3Int worldPos = Vector3Int.FloorToInt(walkableFloor.GetCellCenterWorld(pos));
                Tile tile = new(worldPos.x, worldPos.y);
                grid.Add(worldPos, tile);
            }
        }
    }

    // Placeholder function to update the grid
    // Call this INSTEAD OF InitializeGrid when adding something to the world
    void UpdateGrid()
    {

    }

    // Calculate the distance from each tile to the player
    public void CalculatePlayerDistances()
    {
        // Make sure the player's key is updated
        playerGridPos = Vector3Int.FloorToInt(playerMovePoint.transform.position);

        // SAFETY PRECAUTION: Reset all tiles to have -1 distance
        foreach( Tile tile in grid.Values )
        {
            tile.distanceFromPlayer = -1;
        }
        if( grid.TryGetValue(playerGridPos, out Tile playerTile) )
        {
            playerTile.distanceFromPlayer = 0;
        }
        else
        {
            throw new ArgumentNullException(nameof(Tile), "Zero Key was not found during Calculate Distances.\nCheck the PlayerMovePoint's location");
        }

        // HERE'S THE ACTUAL CALCULATION
        // We use a breadth-first search to calculate the distances
        Queue<Vector3Int> expansionTiles = new();
        expansionTiles.Enqueue(playerGridPos);
        Vector3Int[] directions = { Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down };    // array of standard moves to make calculations easier

        // As long as there is a tile whose neighbors we haven't looked at, keep looking
        while( expansionTiles.Count > 0 )
        {
            Vector3Int currentKey = expansionTiles.Dequeue();

            // Look in each direction to see if that neighbor exists
            // If it does, add it to the queue to be calculated
            foreach( Vector3Int direction in directions )
            {
                Vector3Int nextKey = currentKey + direction;
                if( !grid.ContainsKey(nextKey) )
                {
                    // The tile doesn't exists; move on to the next direction (or end of loop)
                    continue;
                }

                Tile nextTile = grid[nextKey];
                if( nextTile.distanceFromPlayer != -1 )
                {
                    // The tile has a faster route; move on to the next direction (or end of loop)
                    continue;
                }

                // If we made it here, we need to calculate the distance
                // Simple: It's 1 further than the current tile
                // Complex: Enemies will handle their own if they use different movement
                nextTile.distanceFromPlayer = grid[currentKey].distanceFromPlayer + 1;

                expansionTiles.Enqueue(nextKey);
            }
        }
    }

    // Calculate the distance from each tile to the nearest POI
    public void CalculatePOIDistance()
    {
        // COPY/PASTED AND EDITED CalculatePlayerDistance

        List<Vector3Int> poisGridPos = new();
        foreach( GameObject poi in gameManager.registry.pois)
        {
            poisGridPos.Add(Vector3Int.FloorToInt(poi.transform.position));
        }

        // SAFETY PRECAUTION: Reset all tiles to have -1 distance
        foreach (Tile tile in grid.Values)
        {
            tile.distanceFromPOI = -1;
        }
        foreach( Vector3Int poiGridPos in poisGridPos )
        {
            if (grid.TryGetValue(playerGridPos, out Tile playerTile))
            {
                playerTile.distanceFromPOI = 0;
            }
            else
            {
                throw new ArgumentNullException(nameof(Tile), "Zero Key was not found during Calculate Distances.\nCheck the pois' locations");
            }
        }

        // HERE'S THE ACTUAL CALCULATION
        // We use a breadth-first search to calculate the distances
        Queue<Vector3Int> expansionTiles = new();
        
        foreach(Vector3Int poiGridPos in poisGridPos)
        {
            expansionTiles.Enqueue(poiGridPos);
        }
        
        Vector3Int[] directions = { Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down };    // array of standard moves to make calculations easier

        // As long as there is a tile whose neighbors we haven't looked at, keep looking
        while (expansionTiles.Count > 0)
        {
            Vector3Int currentKey = expansionTiles.Dequeue();

            // Look in each direction to see if that neighbor exists
            // If it does, add it to the queue to be calculated
            foreach (Vector3Int direction in directions)
            {
                Vector3Int nextKey = currentKey + direction;
                if (!grid.ContainsKey(nextKey))
                {
                    // The tile doesn't exists; move on to the next direction (or end of loop)
                    continue;
                }

                Tile nextTile = grid[nextKey];
                if (nextTile.distanceFromPOI != -1)
                {
                    // The tile has a faster route; move on to the next direction (or end of loop)
                    continue;
                }

                // If we made it here, we need to calculate the distance
                // Simple: It's 1 further than the current tile
                // Complex: Enemies will handle their own if they use different movement
                nextTile.distanceFromPOI = grid[currentKey].distanceFromPOI + 1;

                expansionTiles.Enqueue(nextKey);
            }
        }
    }
}