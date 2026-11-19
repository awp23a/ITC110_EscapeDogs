using UnityEngine;

public interface IPOI
{
    public GameManager gameManager{ get; }
    public GameObject self{ get; }

    public Vector2Int gridPos{ get; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameManager.registry.RegisterPOI(self);
    }

    // collection method
    private void OnCollect() { }

    public void DestroySelf() { }

    void OnDestroy()
    {
        if( gameManager != null && gameManager.gridManager != null && gameManager.registry != null && gameManager.gridManager.grid != null && self != null )
        {
            gameManager.gridManager.grid[Vector3Int.FloorToInt(self.transform.position)].thingOnTile = Things.Nothing;
            gameManager.registry.UnregisterPOI(self);
        }
    }
}
