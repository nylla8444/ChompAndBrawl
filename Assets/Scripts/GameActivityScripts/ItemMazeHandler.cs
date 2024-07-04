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
    [SerializeField] private GameObject powerPelletPrefab;
    [SerializeField] private GameObject fruitPrefab;
    [SerializeField] private GameObject effectItemPrefab;

    [Header("Scores")]
    [SerializeField] private int pacdotScore;
    [SerializeField] private int powerPelletScore;
    [SerializeField] private int powerPelletFailScore;
    [SerializeField] private int effectItemScore;
    
    [Header("Item Info")]
    [SerializeField] private float powerPelletDuration;
    [SerializeField] private float fruitSpawnInterval;
    [SerializeField] private float effectItemSpawnInterval;

    [Header("Item List")]
    [SerializeField] private FruitList fruitList;
    [SerializeField] private EffectItemList effectItemList;

    private List<GameObject> pacdotsOnPath = new List<GameObject>();
    private List<GameObject> powerPelletsOnPath = new List<GameObject>();
    private List<GameObject> fruitsOnPath = new List<GameObject>();
    private List<GameObject> effectItemsOnPath = new List<GameObject>();
    private Dictionary<GameObject, Fruit> fruitDictionary = new Dictionary<GameObject, Fruit>();
    private Dictionary<GameObject, EffectItem> effectItemDictionary = new Dictionary<GameObject, EffectItem>();
    
    private Vector2 lastPacmanPosition;
    private Coroutine fruitSpawnCoroutine;
    private Coroutine effectItemSpawnCoroutine;

    private const float TILE_SIZE = 0.16f;
    private const float TILE_OFFSET = 0.08f;
    private const int MAX_COUNT_POWER_PELLETS = 3;
    private const int MAX_COUNT_FRUITS = 4;
    private const int MAX_COUNT_EFFECT_ITEMS = 4;
    private const float MIN_DISTANCE_POWER_PELLETS_FROM_ORIGIN = 12f;
    private const float MIN_DISTANCE_BETWEEN_POWER_PELLETS = 16f;
    private const float MIN_DISTANCE_FRUITS_FROM_ORIGIN = 8f;
    private const float MIN_DISTANCE_EFFECT_ITEMS_FROM_ORIGIN = 8f;

    private void Start()
    {
        InitializeItems();
    }

    private void Update()
    {
        CheckPacdotCollection();
        CheckPowerPelletCollection();
        CheckFruitCollection();
        CheckEffectItemCollection();
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

        SpawnAllPowerPellets();
        StartSpawnFruits();
        StartSpawnEffectItems();
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
        Debug.Log(System.String.Join(", ", gameData.item_data.pacdot_positions));
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

    private void RemovePacdot(Vector2 position)
    {
        foreach (GameObject pacdot in new List<GameObject>(pacdotsOnPath))
        {
            if (position == (Vector2)pacdot.transform.position)
            {
                GameData gameData = GameDataManager.LoadData();
                gameData.item_data.pacdot_positions.Remove(position);
                
                pacdotsOnPath.Remove(pacdot);
                Destroy(pacdot);
                
                GameDataManager.SaveData(gameData);
            }
        }
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

    /*********************************************************************/
    //                          PowerPellets
    /*********************************************************************/

    private void SpawnAllPowerPellets()
    {
        foreach (GameObject powerPellet in powerPelletsOnPath)
        {
            Destroy(powerPellet);
        }
        powerPelletsOnPath.Clear();

        BoundsInt bounds = pathTilemap.cellBounds;
        List<Vector3> possiblePositions = new List<Vector3>();

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int localTile = new Vector3Int(x, y, (int)pathTilemap.transform.position.z);
                if (pathTilemap.HasTile(localTile))
                {
                    Vector3 worldPosition = pathTilemap.CellToWorld(localTile) + new Vector3(TILE_OFFSET, TILE_OFFSET, 0);
                    if (Vector3.Distance(Vector3.zero, worldPosition) > MIN_DISTANCE_POWER_PELLETS_FROM_ORIGIN * TILE_SIZE + TILE_OFFSET)
                    {
                        possiblePositions.Add(worldPosition);
                    }
                }
            }
        }

        while (powerPelletsOnPath.Count < MAX_COUNT_POWER_PELLETS && possiblePositions.Count > 0)
        {
            Vector3 position = possiblePositions[Random.Range(0, possiblePositions.Count)];
            possiblePositions.Remove(position);

            bool validPosition = true;
            foreach (GameObject powerPellet in powerPelletsOnPath)
            {
                if (Vector3.Distance(powerPellet.transform.position, position) < MIN_DISTANCE_BETWEEN_POWER_PELLETS * TILE_SIZE + TILE_OFFSET)
                {
                    validPosition = false;
                    break;
                }
            }

            if (validPosition)
            {
                GameObject powerPellet = Instantiate(powerPelletPrefab, position, Quaternion.identity);
                powerPelletsOnPath.Add(powerPellet);
                Debug.Log($"Power Pellet at ({powerPellet.transform.position.x}, {powerPellet.transform.position.y})");
            }
        }
    }

    private void CheckPowerPelletCollection()
    {
        GameData gameData = GameDataManager.LoadData();
        Vector2 pacmanPosition = gameData.pacman_data.coordinate;

        if (!gameData.pacman_data.has_power_pellet)
        {
            foreach (GameObject powerPellet in new List<GameObject>(powerPelletsOnPath))
            {
                if (pacmanPosition == (Vector2)powerPellet.transform.position)
                {
                    PowerPelletCollected(powerPellet.transform.position);
                    powerPelletsOnPath.Remove(powerPellet);
                    Destroy(powerPellet);
                    
                    StartCoroutine(PowerPelletEffect());
                    break;
                }
            }
        }

        if (!System.String.IsNullOrEmpty(gameData.ghost_data.current_fighting_ghost))
        {
            StopCoroutine(PowerPelletEffect());
        }
    }

    private void PowerPelletCollected(Vector2 position)
    {
        GameData gameData = GameDataManager.LoadData();
        gameData.pacman_data.has_power_pellet = true;
        gameData.pacman_data.score += powerPelletScore;
        GameDataManager.SaveData(gameData);
    }

    private IEnumerator PowerPelletEffect()
    {
        yield return new WaitForSeconds(powerPelletDuration);
        
        GameData gameData = GameDataManager.LoadData();
        gameData.pacman_data.has_power_pellet = false;
        gameData.pacman_data.score -= powerPelletFailScore;
        GameDataManager.SaveData(gameData);

        SpawnAllPowerPellets();
    }

    /*********************************************************************/
    //                          Fruits
    /*********************************************************************/

    private void StartSpawnFruits()
    {
        fruitSpawnCoroutine = StartCoroutine(SpawnFruitsCoroutine());
    }

    private IEnumerator SpawnFruitsCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(fruitSpawnInterval);
            if (fruitsOnPath.Count < MAX_COUNT_FRUITS)
            {
                SpawnFruit();
            }
        }
    }

    private void SpawnFruit()
    {
        GameData gameData = GameDataManager.LoadData();
        List<Fruit> possibleFruits = new List<Fruit>();
        int aliveGhostCount = gameData.ghost_data.list_alive_ghosts.Count;

        foreach (Fruit fruit in fruitList.fruits)
        {
            if (fruit.fruitGhostRequirement <= aliveGhostCount - 4)
            {
                possibleFruits.Add(fruit);
            }
        }

        if (possibleFruits.Count <= 0) return;

        Fruit selectedFruit = SelectFruitBasedOnChance(possibleFruits);
        if (selectedFruit == null) return;

        BoundsInt bounds = pathTilemap.cellBounds;
        List<Vector3> possiblePositions = new List<Vector3>();

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int localTile = new Vector3Int(x, y, (int)pathTilemap.transform.position.z);
                if (pathTilemap.HasTile(localTile))
                {
                    Vector3 worldPosition = pathTilemap.CellToWorld(localTile) + new Vector3(TILE_OFFSET, TILE_OFFSET, 0);
                    if (Vector3.Distance(Vector3.zero, worldPosition) > MIN_DISTANCE_FRUITS_FROM_ORIGIN * TILE_SIZE + TILE_OFFSET)
                    {
                        possiblePositions.Add(worldPosition);
                    }
                }
            }
        }

        if (possiblePositions.Count <= 0) return;
        
        Vector3 position = possiblePositions[Random.Range(0, possiblePositions.Count)];
        GameObject fruitObject = Instantiate(fruitPrefab, position, Quaternion.identity);
        
        RemovePacdot(position);
        fruitObject.GetComponent<SpriteRenderer>().sprite = selectedFruit.fruitSprite;
        fruitObject.name = selectedFruit.fruitName;
        fruitsOnPath.Add(fruitObject);
        fruitDictionary[fruitObject] = selectedFruit;
        
        Debug.Log($"Fruit spawned: {selectedFruit.fruitName} at ({position.x}, {position.y})");
    }

    private Fruit SelectFruitBasedOnChance(List<Fruit> possibleFruits)
    {
        int randomChance = Random.Range(1, 101);
        List<Fruit> possibleItems = new List<Fruit>();

        foreach (Fruit fruit in possibleFruits)
        {
            if (randomChance <= fruit.fruitSpawnChance)
            {
                possibleItems.Add(fruit);
            }
        }

        if (possibleItems.Count > 0)
        {
            Fruit fruit = possibleItems[Random.Range(0, possibleItems.Count)];
            return fruit;
        }

        return null;
    }

    private void CheckFruitCollection()
    {
        Vector2 pacmanPosition = GameDataManager.LoadData().pacman_data.coordinate;
        foreach (GameObject fruit in new List<GameObject>(fruitsOnPath))
        {
            if (pacmanPosition == (Vector2)fruit.transform.position)
            {
                FruitCollected(fruit);
                fruitsOnPath.Remove(fruit);
                Destroy(fruit);
            }
        }
    }

    private void FruitCollected(GameObject fruitObject)
    {
        GameData gameData = GameDataManager.LoadData();
        if (fruitDictionary.TryGetValue(fruitObject, out Fruit fruitData))
        {
            gameData.pacman_data.score += fruitData.fruitScore;
            fruitDictionary.Remove(fruitObject);
            GameDataManager.SaveData(gameData);
        }
    }

    /*********************************************************************/
    //                       Effect Items
    /*********************************************************************/

    private void StartSpawnEffectItems()
    {
        effectItemSpawnCoroutine = StartCoroutine(SpawnEffectItemsCoroutine());
    }

    private IEnumerator SpawnEffectItemsCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(effectItemSpawnInterval);
            if (effectItemsOnPath.Count < MAX_COUNT_EFFECT_ITEMS)
            {
                SpawnEffectItem();
            }
        }
    }

    private void SpawnEffectItem()
    {
        GameData gameData = GameDataManager.LoadData();
        List<EffectItem> possibleEffectItems = effectItemList.effectItems;

        EffectItem selectedEffectItem = SelectEffectItemBasedOnChance(possibleEffectItems);
        if (selectedEffectItem == null) return;

        BoundsInt bounds = pathTilemap.cellBounds;
        List<Vector3> possiblePositions = new List<Vector3>();

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int localTile = new Vector3Int(x, y, (int)pathTilemap.transform.position.z);
                if (pathTilemap.HasTile(localTile))
                {
                    Vector3 worldPosition = pathTilemap.CellToWorld(localTile) + new Vector3(TILE_OFFSET, TILE_OFFSET, 0);
                    if (Vector3.Distance(Vector3.zero, worldPosition) > MIN_DISTANCE_EFFECT_ITEMS_FROM_ORIGIN * TILE_SIZE + TILE_OFFSET)
                    {
                        possiblePositions.Add(worldPosition);
                    }
                }
            }
        }

        if (possiblePositions.Count <= 0) return;
        
        Vector3 position = possiblePositions[Random.Range(0, possiblePositions.Count)];
        GameObject effectItemObject = Instantiate(effectItemPrefab, position, Quaternion.identity);
        
        RemovePacdot(position);
        effectItemObject.transform.GetChild(0).GetComponent<SpriteRenderer>().sprite = selectedEffectItem.effectItemSprite;
        effectItemObject.name = selectedEffectItem.effectItemName;
        effectItemsOnPath.Add(effectItemObject);
        effectItemDictionary[effectItemObject] = selectedEffectItem;
        
        Debug.Log($"Effect Item spawned: {selectedEffectItem.effectItemName} at ({position.x}, {position.y})");
    }

    private EffectItem SelectEffectItemBasedOnChance(List<EffectItem> possibleEffectItems)
    {
        int randomChance = Random.Range(1, 101);
        List<EffectItem> possibleItems = new List<EffectItem>();

        foreach (EffectItem effectItem in possibleEffectItems)
        {
            if (randomChance <= effectItem.effectItemSpawnChance)
            {
                possibleItems.Add(effectItem);
            }
        }

        if (possibleItems.Count > 0)
        {
            EffectItem effectItem = possibleItems[Random.Range(0, possibleItems.Count)];
            return effectItem;
        }

        return null;
    }

    private void CheckEffectItemCollection()
    {
        GameData gameData = GameDataManager.LoadData();
        Vector2 pacmanPosition = gameData.pacman_data.coordinate;

        if (!System.String.IsNullOrEmpty(gameData.pacman_data.current_effect_item)) return;

        foreach (GameObject effectItem in new List<GameObject>(effectItemsOnPath))
        {
            if (pacmanPosition == (Vector2)effectItem.transform.position)
            {
                EffectItemCollected(effectItem);
                effectItemsOnPath.Remove(effectItem);
                Destroy(effectItem);
            }
        }
    }

    private void EffectItemCollected(GameObject effectItemObject)
    {
        GameData gameData = GameDataManager.LoadData();
        if (effectItemDictionary.TryGetValue(effectItemObject, out EffectItem effectItemData))
        {
            gameData.pacman_data.score += effectItemScore;
            gameData.pacman_data.current_effect_item = effectItemData.effectItemName;
            effectItemDictionary.Remove(effectItemObject);
            GameDataManager.SaveData(gameData);
        }
    }
}