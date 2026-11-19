using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridMergeScript : MonoBehaviour
{
    Tilemap sourceFloor;
    Tilemap sourceWalls;
    Tilemap targetFloor;
    Tilemap targetWalls;
    GridManager gridManager;

    void Awake()
    {
        sourceFloor = transform.Find("Grid/RoomFloor").GetComponent<Tilemap>();
        sourceWalls = transform.Find("Grid/RoomWalls").GetComponent<Tilemap>();
        targetFloor = GameObject.Find("StartingRoom/Grid/Floor").GetComponent<Tilemap>();
        targetWalls = GameObject.Find("StartingRoom/Grid/Walls").GetComponent<Tilemap>();
        gridManager = GameObject.Find("Canvas").GetComponent<GridManager>();
    }

    // everything here happens in "Start"
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // We already have the references. Now, just combine them
        // find the boundaries of the new tilemap
        sourceFloor.CompressBounds();
        sourceWalls.CompressBounds();
        BoundsInt floorBounds = sourceFloor.cellBounds;
        BoundsInt wallBounds = sourceWalls.cellBounds;

        // here are the grids of tiles that we will need to place
        TileBase[] transferFloor = sourceFloor.GetTilesBlock(floorBounds);
        TileBase[] transferWall = sourceWalls.GetTilesBlock(wallBounds);

        // we need arrays of Vector3Int positions for the tiles
        List<TileBase> transferFloorValid = new();
        List<Vector3Int> transferFloorPositions = new();
        List<TileBase> transferWallValid = new();
        List<Vector3Int> transferWallPositions = new();

        int index = 0;
        for( int z = floorBounds.zMin; z < floorBounds.zMax; z++)
        {
            for( int y = floorBounds.yMin; y < floorBounds.yMax; y++)
            {
                for( int x = floorBounds.xMin; x < floorBounds.xMax; x++)
                {
                    if( transferFloor[index] != null)
                    {
                        // convert out of these local coords and into the target's local coords
                        Vector3Int tilePos = targetFloor.WorldToCell(sourceFloor.GetCellCenterWorld(new Vector3Int(x, y, z)));
                        transferFloorPositions.Add(tilePos);
                        transferFloorValid.Add(transferFloor[index]);
                    }
                    index++;
                }
            }
        }
        index = 0;
        for (int z = wallBounds.zMin; z < wallBounds.zMax; z++)
        {
            for (int y = wallBounds.yMin; y < wallBounds.yMax; y++)
            {
                for (int x = wallBounds.xMin; x < wallBounds.xMax; x++)
                {
                    if (transferWall[index] != null)
                    {
                        // convert out of these local coords and into the target's local coords
                        Vector3Int tilePos = targetWalls.WorldToCell(sourceWalls.GetCellCenterWorld(new Vector3Int(x, y, z)));
                        transferWallPositions.Add(tilePos);
                        transferWallValid.Add(transferWall[index]);
                    }
                    index++;
                }
            }
        }

        // Copy the tiles over
        targetFloor.SetTiles(transferFloorPositions.ToArray(), transferFloorValid.ToArray());
        targetWalls.SetTiles(transferWallPositions.ToArray(), transferWallValid.ToArray());

        gridManager.UpdateGrid(transferFloorPositions.ToArray());

        // now, we add the room to the registry

        Destroy(gameObject);
    }
}
