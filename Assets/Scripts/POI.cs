using UnityEngine;

public class POI : MonoBehaviour
{
    public GameManager gameManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameManager.registry.RegisterPOI(gameObject);
    }

    // collection method
    private void OnCollect()
    {
        // Insert script to add dog to the player's inventory

        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if( gameManager != null && gameManager.gridManager != null && gameManager.registry != null && gameManager.gridManager.grid != null && gameObject != null )
        {
            gameManager.gridManager.grid[Vector3Int.FloorToInt(transform.position)].thingOnTile = Things.Nothing;
            gameManager.registry.UnregisterPOI(gameObject);
        }
    }
}
