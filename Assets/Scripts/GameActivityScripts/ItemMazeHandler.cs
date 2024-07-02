using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ItemMazeHandler : MonoBehaviour
{
    [Header("Miscellaneous")]
    [SerializeField] private Tilemap pathTilemap;
    [SerializeField] private LayerMask pathLayer;

    [Header("Item Prefabs")]
    [SerializeField] private GameObject pacdotPrefab;

    [Header("Scores")]
    [SerializeField] private int pacdotScore;

    private List<GameObject> pacdotsOnPath = new List<GameObject>();
    private Vector2 lastPacmanPosition;

    private const float TILE_OFFSET = 0.08f; 

    private void Start()
    {
        InitializeItems();
    }

    private void Update()
    {
        CheckPacdotCollection();
    }

    private void InitializeItems()
    {
        GameData gameData = GameDataManager.LoadData();
        if (gameData.item_data.pacdot_positions == null || gameData.item_data.pacdot_positions.Count == 0)
        {
            SpawnAllPacdots();
        }
        else if (gameData.pacman_data.has_won_at_fight)
        {
            SpawnAllPacdots();
        }
        else
        {
            SpawnSavedPacdots();
        }
    }

    /*********************************************************************/
    //                             Pacdots
    /*********************************************************************/

    private void SpawnAllPacdots()
    {
        GameData gameData = GameDataManager.LoadData();
        gameData.item_data.pacdot_positions.Clear();

        BoundsInt bounds = pathTilemap.cellBounds;
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int localTile = new Vector3Int(x, y, (int)pathTilemap.transform.position.z);
                if (pathTilemap.HasTile(localTile))
                {
                    Vector3 worldPosition = pathTilemap.CellToWorld(localTile) + new Vector3(TILE_OFFSET, TILE_OFFSET, 0);
                    GameObject pacdot = Instantiate(pacdotPrefab, worldPosition, Quaternion.identity);
                    pacdotsOnPath.Add(pacdot);

                    gameData.item_data.pacdot_positions.Add(worldPosition);   
                }
            }
        }

        GameDataManager.SaveData(gameData);
    }

    private void SpawnSavedPacdots()
    {
        GameData gameData = GameDataManager.LoadData();
        foreach (Vector2 pos in gameData.item_data.pacdot_positions)
        {
            GameObject pacdot = Instantiate(pacdotPrefab, pos, Quaternion.identity);
            pacdotsOnPath.Add(pacdot);
        }
    }

    public void RespawnPacdots()
    {
        GameData gameData = GameDataManager.LoadData();
        foreach (Vector2 pos in gameData.item_data.pacdot_positions)
        {
            if (!Physics2D.OverlapCircle(pos, 0.05f, pathLayer))
            {
                GameObject pacdot = Instantiate(pacdotPrefab, pos, Quaternion.identity);
                pacdotsOnPath.Add(pacdot);
                gameData.item_data.pacdot_positions.Add(pos);
            }
        }

        GameDataManager.SaveData(gameData);
    }

    private void CheckPacdotCollection()
    {
        Vector2 pacmanPosition = GameDataManager.LoadData().pacman_data.coordinate;
        foreach (GameObject pacdot in new List<GameObject>(pacdotsOnPath))
        {
            if (pacmanPosition == (Vector2)pacdot.transform.position)
            {
                PacdotCollected(pacdot.transform.position);
                pacdotsOnPath.Remove(pacdot);
                Destroy(pacdot);
            }
        }
    }

    private void PacdotCollected(Vector2 position)
    {
        GameData gameData = GameDataManager.LoadData();
        gameData.pacman_data.score += pacdotScore;
        gameData.item_data.pacdot_positions.Remove(position);
        GameDataManager.SaveData(gameData);
    }
}