using UnityEngine;

public class GameManager : MonoBehaviour, IGameManager
{
    [Header("Manager References")]
    public MapManager mapManager;
    public CivsManager civsManager;

    [Header("Game Settings")]
    public int maxTurns = 500;
    public bool autoStartGame = true;

    private GameState currentGameState = GameState.Initializing;
    private int currentTurn = 0;

    // Properties
    public GameState CurrentGameState => currentGameState;
    public int CurrentTurn => currentTurn;
    public IMapManager MapManager => mapManager;
    public ICivsManager CivsManager => civsManager;

    // Events
    public event System.Action<GameState> OnGameStateChanged;
    public event System.Action<int> OnTurnChanged;

    void Start()
    {
        InitializeGame();

        if (autoStartGame)
        {
            StartGame();
        }
    }

    void InitializeGame()
    {
        Debug.Log("=== INITIALIZING GAME ===");

        // Validate dependencies
        if (!ValidateDependencies())
        {
            Debug.LogError("GameManager: Missing dependencies! Cannot initialize game.");
            return;
        }

        // Initialize managers with proper timing
        StartCoroutine(InitializeManagersSequentially());
    }

    System.Collections.IEnumerator InitializeManagersSequentially()
    {
        // Initialize MapManager first
        Debug.Log("Initializing MapManager...");
        mapManager.Initialize();

        // Wait for hex map to be generated
        Debug.Log("Waiting for hex map generation...");
        float timeout = 10f; // 10 second timeout
        float elapsed = 0f;

        while (mapManager.HexMap.hexes.Count == 0 && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;

            if (elapsed % 1f < 0.1f) // Log every second
            {
                Debug.Log($"Waiting for hexes... Elapsed: {elapsed:F1}s, Hexes: {mapManager.HexMap.hexes.Count}");
            }
        }

        if (mapManager.HexMap.hexes.Count == 0)
        {
            Debug.LogError("Hex map generation failed or timed out!");
            yield break;
        }

        Debug.Log($"Hex map ready with {mapManager.HexMap.hexes.Count} hexes");

        // Now initialize CivsManager with the ready map
        Debug.Log("Initializing CivsManager...");
        civsManager.Initialize(mapManager);

        Debug.Log("All managers initialized successfully");
    }

    bool ValidateDependencies()
    {
        if (mapManager == null)
        {
            Debug.LogError("GameManager: MapManager is not assigned!");
            return false;
        }

        if (civsManager == null)
        {
            Debug.LogError("GameManager: CivsManager is not assigned!");
            return false;
        }

        return true;
    }

    void InitializeManagers()
    {
        // Initialize MapManager first (everything depends on the map)
        mapManager.Initialize();

        // Initialize CivsManager (depends on map being ready)
        civsManager.Initialize(mapManager);

        Debug.Log("All managers initialized");
    }

    public void StartGame()
    {
        if (currentGameState != GameState.Initializing && currentGameState != GameState.Paused)
        {
            Debug.LogWarning("Game can only be started from Initializing or Paused state");
            return;
        }

        // Double-check that we have civilizations and hexes
        if (civsManager.CivCount == 0)
        {
            Debug.LogError("Cannot start game: No civilizations created!");
            return;
        }

        if (mapManager.HexMap.hexes.Count == 0)
        {
            Debug.LogError("Cannot start game: No hexes available!");
            return;
        }

        SetGameState(GameState.Running);
        currentTurn = 1;
        OnTurnChanged?.Invoke(currentTurn);

        Debug.Log("=== GAME STARTED ===");
        Debug.Log($"Turn {currentTurn} - {civsManager.CivCount} civilizations, {mapManager.HexMap.hexes.Count} hexes");
    }

    public void PauseGame()
    {
        if (currentGameState == GameState.Running)
        {
            SetGameState(GameState.Paused);
            Debug.Log("Game paused");
        }
    }

    public void ResumeGame()
    {
        if (currentGameState == GameState.Paused)
        {
            SetGameState(GameState.Running);
            Debug.Log("Game resumed");
        }
    }

    public void EndGame()
    {
        SetGameState(GameState.GameOver);
        Debug.Log("=== GAME ENDED ===");

        // Display final results
        var stats = civsManager.GetGameStats();
        foreach (var stat in stats)
        {
            Debug.Log($"{stat.Key}: {stat.Value}");
        }
    }

    public void NextTurn()
    {
        if (currentGameState != GameState.Running)
        {
            Debug.LogWarning("Cannot advance turn - game is not running");
            return;
        }

        ProcessTurn();

        currentTurn++;
        OnTurnChanged?.Invoke(currentTurn);

        Debug.Log($"=== TURN {currentTurn} ===");

        // Check win conditions
        CheckGameEndConditions();
    }

    public void ProcessTurn()
    {
        Debug.Log($"Processing turn {currentTurn}...");

        // Process all civilizations
        civsManager.ProcessTurn();

        // Other turn processing can go here
        // (random events, barbarians, etc.)

        Debug.Log($"Turn {currentTurn} processing complete");
    }

    void CheckGameEndConditions()
    {
        // Check max turns
        if (currentTurn >= maxTurns)
        {
            Debug.Log("Maximum turns reached - ending game");
            EndGame();
            return;
        }

        // Check victory conditions
        var winner = civsManager.CheckVictoryConditions();
        if (winner != null)
        {
            Debug.Log($"Victory! {winner.CivName} has won the game!");
            EndGame();
            return;
        }

        // Check if only one civ remains
        if (civsManager.GetAliveCivCount() <= 1)
        {
            Debug.Log("Only one civilization remains - ending game");
            EndGame();
            return;
        }
    }

    void SetGameState(GameState newState)
    {
        if (currentGameState != newState)
        {
            var oldState = currentGameState;
            currentGameState = newState;
            OnGameStateChanged?.Invoke(newState);
            Debug.Log($"Game state changed: {oldState} ? {newState}");
        }
    }

    void Update()
    {
        // Handle debug input
        if (Input.GetKeyDown(KeyCode.N) && currentGameState == GameState.Running)
        {
            NextTurn();
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            if (currentGameState == GameState.Running)
                PauseGame();
            else if (currentGameState == GameState.Paused)
                ResumeGame();
        }
    }

    
}