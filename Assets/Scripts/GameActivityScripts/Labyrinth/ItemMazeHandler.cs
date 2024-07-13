using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ItemMazeHandler : MonoBehaviour
{
    [SerializeField] private bool isMazeStarted = false;
    
    [Header("Miscellaneous")]
    [SerializeField] private Tilemap pathTilemap;
    [SerializeField] private LayerMask pathLayer;
    [SerializeField] private GameObject environmentAmbient;

    [Header("Item Prefabs")]
    [SerializeField] private GameObject pacdotPrefab;
    [SerializeField] private GameObject powerPelletPrefab;
    [SerializeField] private GameObject fruitPrefab;
    [SerializeField] private GameObject effectItemPrefab;

    [Header("Scores")]
    [SerializeField] private int pacdotPoints;
    [SerializeField] private int powerPelletPoints;
    [SerializeField] private int powerPelletFailPoints;
    [SerializeField] private int effectItemPoints;
    
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
    private const int MAX_COUNT_POWER_PELLETS = 2;
    private const int MAX_COUNT_FRUITS = 4;
    private const int MAX_COUNT_EFFECT_ITEMS = 4;
    private const float MIN_DISTANCE_POWER_PELLETS_FROM_ORIGIN = 12f;
    private const float MIN_DISTANCE_BETWEEN_POWER_PELLETS = 16f;
    private const float MIN_DISTANCE_FRUITS_FROM_ORIGIN = 4f;
    private const float MIN_DISTANCE_EFFECT_ITEMS_FROM_ORIGIN = 8f;

    private void Start()
    {
        isMazeStarted = true;   // remove after test
        InitializeItems();
    }

    private void Update()
    {
        if (isMazeStarted && !IngameDataManager.LoadSpecificData<bool>("pacman_data.is_immune_to_ghost"))
        {
            Vector2 _pacman_coordinate = IngameDataManager.LoadSpecificData<Vector2>("pacman_data.coordinate");
            if (_pacman_coordinate == lastPacmanPosition) return;
            
            CheckPacdotCollection(_pacman_coordinate);
            CheckPowerPelletCollection(_pacman_coordinate);
            CheckFruitCollection(_pacman_coordinate);
            CheckEffectItemCollection(_pacman_coordinate);
            lastPacmanPosition = _pacman_coordinate;
        }
    }

    public void StartItemController(bool triggerValue)
    {
        isMazeStarted = triggerValue;
    }

    private void InitializeItems()
    {
        List<Vector2> _item_pacdotPositions = IngameDataManager.LoadSpecificData<List<Vector2>>("item_data.pacdot_positions");
        bool _pacman_hasWonAtFight = IngameDataManager.LoadSpecificData<bool>("pacman_data.has_won_at_fight");

        if (_item_pacdotPositions == null || _item_pacdotPositions.Count == 0)
        {
            SpawnAllPacdots();
        }
        else if (_pacman_hasWonAtFight)
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
        List<Vector2> _item_pacdotPositions = IngameDataManager.LoadSpecificData<List<Vector2>>("item_data.pacdot_positions");
        _item_pacdotPositions.Clear();

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

                    _item_pacdotPositions.Add(worldPosition);   
                }
            }
        }
        
        IngameDataManager.SaveSpecificData("item_data.pacdot_positions", _item_pacdotPositions);
    }

    private void SpawnSavedPacdots()
    {
        List<Vector2> _item_pacdotPositions = IngameDataManager.LoadSpecificData<List<Vector2>>("item_data.pacdot_positions");
        foreach (Vector2 pos in _item_pacdotPositions)
        {
            GameObject pacdot = Instantiate(pacdotPrefab, pos, Quaternion.identity);
            pacdotsOnPath.Add(pacdot);
        }
    }

    public void RespawnPacdots()
    {
        List<Vector2> _item_pacdotPositions = IngameDataManager.LoadSpecificData<List<Vector2>>("item_data.pacdot_positions");
        foreach (Vector2 pos in _item_pacdotPositions)
        {
            if (!Physics2D.OverlapCircle(pos, 0.05f, pathLayer))
            {
                GameObject pacdot = Instantiate(pacdotPrefab, pos, Quaternion.identity);
                pacdotsOnPath.Add(pacdot);
                _item_pacdotPositions.Add(pos);
            }
        }

        IngameDataManager.SaveSpecificData("item_data.pacdot_positions", _item_pacdotPositions);
    }

    private void RemovePacdot(Vector2 position)
    {
        foreach (GameObject pacdot in new List<GameObject>(pacdotsOnPath))
        {
            if (position == (Vector2)pacdot.transform.position)
            {
                List<Vector2> _item_pacdotPositions = IngameDataManager.LoadSpecificData<List<Vector2>>("item_data.pacdot_positions");
                _item_pacdotPositions.Remove(position);
                
                pacdotsOnPath.Remove(pacdot);
                Destroy(pacdot);
                
                IngameDataManager.SaveSpecificData("item_data.pacdot_positions", _item_pacdotPositions);
            }
        }
    }

    private void CheckPacdotCollection(Vector2 position)
    {
        foreach (GameObject pacdot in new List<GameObject>(pacdotsOnPath))
        {
            if (position == (Vector2)pacdot.transform.position)
            {
                PacdotCollected(pacdot.transform.position);
                pacdotsOnPath.Remove(pacdot);
                Destroy(pacdot);
            }
        }
    }

    private void PacdotCollected(Vector2 position)
    {
        int _pacman_points = IngameDataManager.LoadSpecificData<int>("pacman_data.points");
        List<Vector2> _item_pacdotPositions = IngameDataManager.LoadSpecificData<List<Vector2>>("item_data.pacdot_positions");

        _pacman_points += pacdotPoints;
        _item_pacdotPositions.Remove(position);
        
        IngameDataManager.SaveSpecificData("pacman_data.points", _pacman_points);
        IngameDataManager.SaveSpecificData("item_data.pacdot_positions", _item_pacdotPositions);
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

    private void CheckPowerPelletCollection(Vector2 position)
    {
        if (!IngameDataManager.LoadSpecificData<bool>("pacman_data.has_power_pellet"))
        {
            foreach (GameObject powerPellet in new List<GameObject>(powerPelletsOnPath))
            {
                if (position == (Vector2)powerPellet.transform.position)
                {
                    PowerPelletCollected(powerPellet.transform.position);
                    powerPelletsOnPath.Remove(powerPellet);
                    Destroy(powerPellet);
                    
                    StartCoroutine(PowerPelletEffect());
                    break;
                }
            }
        }

        if (!System.String.IsNullOrEmpty(IngameDataManager.LoadSpecificData<string>("ghost_data.current_fighting")))
        {
            StopCoroutine(PowerPelletEffect());
        }
    }

    private void PowerPelletCollected(Vector2 position)
    {
        int _pacman_points = IngameDataManager.LoadSpecificData<int>("pacman_data.points");
        bool _pacman_hasPowerPellet = IngameDataManager.LoadSpecificData<bool>("pacman_data.has_power_pellet");

        _pacman_points += pacdotPoints;
        _pacman_hasPowerPellet = true;
        
        IngameDataManager.SaveSpecificData("pacman_data.points", _pacman_points);
        IngameDataManager.SaveSpecificData("pacman_data.has_power_pellet", _pacman_hasPowerPellet);
    }

    private IEnumerator PowerPelletEffect()
    {
        const float AMBIENT_TRANSITION_SPEED = 2.5f;
        List<Color> AMBIENT_COLORS = new List<Color>
        {
            new Color(0.1461f, 0.04784f, 0.2358f, 0.6275f),
            new Color(1.0f, 1.0f, 1.0f, 0.0f)
        };

        StartCoroutine(UpdateObjectColor(environmentAmbient, AMBIENT_COLORS[0], AMBIENT_TRANSITION_SPEED));
        float elapsedTime = 0f;

        while (elapsedTime < powerPelletDuration)
        {
            if (!IngameDataManager.LoadSpecificData<bool>("pacman_data.has_power_pellet"))
            {
                break;
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        int _pacman_points = IngameDataManager.LoadSpecificData<int>("pacman_data.points");
        bool _pacman_hasPowerPellet = IngameDataManager.LoadSpecificData<bool>("pacman_data.has_power_pellet");

        _pacman_points -= powerPelletFailPoints;
        _pacman_hasPowerPellet = false;
        
        IngameDataManager.SaveSpecificData("pacman_data.points", _pacman_points);
        IngameDataManager.SaveSpecificData("pacman_data.has_power_pellet", _pacman_hasPowerPellet);

        SpawnAllPowerPellets();
        StartCoroutine(UpdateObjectColor(environmentAmbient, AMBIENT_COLORS[1], AMBIENT_TRANSITION_SPEED));
    }

    private IEnumerator UpdateObjectColor(GameObject _object, Color _color, float _transitionSpeed)
    {
        while (Vector4.Distance(_object.GetComponent<SpriteRenderer>().color, _color) > 0.01f)
        {
            _object.GetComponent<SpriteRenderer>().color = Color.Lerp(_object.GetComponent<SpriteRenderer>().color, _color, _transitionSpeed * Time.deltaTime);
            yield return null;
        }
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
            if (fruitsOnPath.Count < MAX_COUNT_FRUITS && isMazeStarted)
            {
                SpawnFruit();
            }
        }
    }

    private void SpawnFruit()
    {
        List<Fruit> possibleFruits = new List<Fruit>();
        int _ghost_listAlive_Count = IngameDataManager.LoadSpecificData<List<string>>("ghost_data.list_alive").Count;

        foreach (Fruit fruit in fruitList.fruits)
        {
            if (fruit.ghostRequirement <= _ghost_listAlive_Count - 4)
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
        fruitObject.GetComponent<SpriteRenderer>().sprite = selectedFruit.asItemSprite;
        fruitObject.name = selectedFruit.name;
        fruitsOnPath.Add(fruitObject);
        fruitDictionary[fruitObject] = selectedFruit;
        
        Debug.Log($"Fruit spawned: {selectedFruit.name} at ({position.x}, {position.y})");
    }

    private Fruit SelectFruitBasedOnChance(List<Fruit> possibleFruits)
    {
        int randomChance = Random.Range(1, 101);
        List<Fruit> possibleItems = new List<Fruit>();

        foreach (Fruit fruit in possibleFruits)
        {
            if (randomChance <= fruit.spawnChance)
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

    private void CheckFruitCollection(Vector2 position)
    {
        foreach (GameObject fruit in new List<GameObject>(fruitsOnPath))
        {
            if (position == (Vector2)fruit.transform.position)
            {
                FruitCollected(fruit);
                fruitsOnPath.Remove(fruit);
                Destroy(fruit);
            }
        }
    }

    private void FruitCollected(GameObject fruitObject)
    {
        int _pacman_points = IngameDataManager.LoadSpecificData<int>("pacman_data.points");
        if (fruitDictionary.TryGetValue(fruitObject, out Fruit fruitData))
        {
            _pacman_points += fruitData.points;
            fruitDictionary.Remove(fruitObject);
            IngameDataManager.SaveSpecificData("pacman_data.points", _pacman_points);
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
            if (effectItemsOnPath.Count < MAX_COUNT_EFFECT_ITEMS && isMazeStarted)
            {
                SpawnEffectItem();
            }
        }
    }

    private void SpawnEffectItem()
    {
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
        effectItemObject.transform.GetChild(0).GetComponent<SpriteRenderer>().sprite = selectedEffectItem.asItemSprite;
        effectItemObject.name = selectedEffectItem.name;
        effectItemsOnPath.Add(effectItemObject);
        effectItemDictionary[effectItemObject] = selectedEffectItem;
        
        Debug.Log($"Effect Item spawned: {selectedEffectItem.name} at ({position.x}, {position.y})");
    }

    private EffectItem SelectEffectItemBasedOnChance(List<EffectItem> possibleEffectItems)
    {
        int randomChance = Random.Range(1, 101);
        List<EffectItem> possibleItems = new List<EffectItem>();

        foreach (EffectItem effectItem in possibleEffectItems)
        {
            if (randomChance <= effectItem.spawnChance)
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

    private void CheckEffectItemCollection(Vector2 position)
    {
        if (!System.String.IsNullOrEmpty(IngameDataManager.LoadSpecificData<string>("pacman_data.current_effect_item"))) return;

        foreach (GameObject effectItem in new List<GameObject>(effectItemsOnPath))
        {
            if (position == (Vector2)effectItem.transform.position)
            {
                EffectItemCollected(effectItem);
                effectItemsOnPath.Remove(effectItem);
                Destroy(effectItem);
            }
        }
    }

    private void EffectItemCollected(GameObject effectItemObject)
    {
        int _pacman_points = IngameDataManager.LoadSpecificData<int>("pacman_data.points");
        string _pacman_currentEffectItem = IngameDataManager.LoadSpecificData<string>("pacman_data.current_effect_item");

        if (effectItemDictionary.TryGetValue(effectItemObject, out EffectItem effectItemData))
        {
            _pacman_points += effectItemPoints;
            _pacman_currentEffectItem = effectItemData.name;
            effectItemDictionary.Remove(effectItemObject);
            
            IngameDataManager.SaveSpecificData("pacman_data.points", _pacman_points);
            IngameDataManager.SaveSpecificData("pacman_data.current_effect_item", _pacman_currentEffectItem);
        }
    }
}