using UnityEngine;

public class GameManager : MonoBehaviour, IGameManager
{
    [Header("Manager References")]
    public MapManager mapManager;
    public CivsManager civsManager;

    [Header("Game Settings")]
    public int maxTurns = 500;
    public bool autoStartGame = true;

    [Header("Hot-seat Turn System")]
    public float turnTransitionDelay = 1f; // Delay between civ turns

    private GameState currentGameState = GameState.Initializing;
    private int currentTurn = 0;
    private int currentCivIndex = 0; // Which civ's turn it is
    private bool waitingForNextCiv = false;

    // Properties
    public GameState CurrentGameState => currentGameState;
    public int CurrentTurn => currentTurn;
    public IMapManager MapManager => mapManager;
    public ICivsManager CivsManager => civsManager;

    // Hot-seat properties
    public ICivilization CurrentCivilization
    {
        get
        {
            var aliveCivs = civsManager?.GetAliveCivilizations();
            if (aliveCivs != null && aliveCivs.Count > 0 && currentCivIndex < aliveCivs.Count)
                return aliveCivs[currentCivIndex];
            return null;
        }
    }

    public bool IsCurrentCivTurn(ICivilization civ)
    {
        return CurrentCivilization?.CivId == civ?.CivId;
    }

    // Events
    public event System.Action<GameState> OnGameStateChanged;
    public event System.Action<int> OnTurnChanged;
    // Add new events for UI communication
    public event System.Action<ICivilization> OnCivTurnStarted;
    public event System.Action<ICivilization> OnCivTurnEnded;

    void Start()
    {
        InitializeGame();
    }

    void InitializeGame()
    {
        Debug.Log("=== INITIALIZING GAME ===");

        if (!ValidateDependencies())
        {
            Debug.LogError("GameManager: Missing dependencies! Cannot initialize game.");
            return;
        }

        // Use coroutine for proper initialization timing
        StartCoroutine(InitializeManagersSequentially());
    }

    System.Collections.IEnumerator InitializeManagersSequentially()
    {
        Debug.Log("Starting sequential manager initialization...");

        // Step 1: Initialize MapManager first
        Debug.Log("Initializing MapManager...");
        mapManager.Initialize();

        // Step 2: Wait for hex map to be generated
        Debug.Log("Waiting for hex map generation...");
        float timeout = 10f;
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

        Debug.Log($"✓ Hex map ready with {mapManager.HexMap.hexes.Count} hexes");

        // Step 3: Initialize CivsManager with the ready map
        Debug.Log("Initializing CivsManager...");
        civsManager.Initialize(mapManager);

        // Step 4: Wait for civilizations to be created
        Debug.Log("Waiting for civilizations to be created...");
        timeout = 5f;
        elapsed = 0f;

        while (civsManager.CivCount == 0 && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;

            if (elapsed % 1f < 0.1f) // Log every second
            {
                Debug.Log($"Waiting for civs... Elapsed: {elapsed:F1}s, Civs: {civsManager.CivCount}");
            }
        }

        if (civsManager.CivCount == 0)
        {
            Debug.LogError("Civilization creation failed or timed out!");
            yield break;
        }

        Debug.Log($"✓ Civilizations ready: {civsManager.CivCount} created");

        // Step 5: Everything is ready, start the game
        yield return new WaitForSeconds(0.1f); // Small delay to ensure everything is settled

        if (autoStartGame)
        {
            Debug.Log("All managers initialized successfully - starting game...");
            StartGame();
        }
        else
        {
            Debug.Log("All managers initialized successfully - ready to start game manually");
        }
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
        // Initialize MapManager first
        mapManager.Initialize();

        // Initialize CivsManager
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

        // Validate everything is ready
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

        // Start with first civilization
        currentTurn = 1;
        currentCivIndex = 0;

        SetGameState(GameState.Running);

        // Start the first civilization's turn
        StartCurrentCivTurn();

        Debug.Log("=== GAME STARTED ===");
        Debug.Log($"Turn {currentTurn} - {civsManager.CivCount} civilizations, {mapManager.HexMap.hexes.Count} hexes");
    }

    void StartCurrentCivTurn()
    {
        var currentCiv = CurrentCivilization;
        if (currentCiv == null)
        {
            Debug.LogError("No current civilization to start turn for!");
            return;
        }

        // Reset movement for current civ's units
        var unitsManager = currentCiv.CivManager?.UnitsManager;
        if (unitsManager != null)
        {
            unitsManager.ResetMovement();
        }

        waitingForNextCiv = false;

        OnCivTurnStarted?.Invoke(currentCiv);
        Debug.Log($"=== {currentCiv.CivName}'s Turn Started (Turn {currentTurn}) ===");
    }

    public void EndCurrentCivTurn()
    {
        if (currentGameState != GameState.Running || waitingForNextCiv)
        {
            Debug.LogWarning("Cannot end turn - game is not running or already waiting");
            return;
        }

        var currentCiv = CurrentCivilization;
        if (currentCiv == null)
        {
            Debug.LogWarning("No current civilization to end turn for");
            return;
        }

        waitingForNextCiv = true;

        // Process only the current civilization
        Debug.Log($"Processing turn for {currentCiv.CivName}...");

        var civManager = currentCiv.CivManager;
        civManager?.ProcessTurn();

        OnCivTurnEnded?.Invoke(currentCiv);
        Debug.Log($"=== {currentCiv.CivName}'s Turn Ended ===");

        // Move to next civilization
        Invoke(nameof(AdvanceToNextCiv), turnTransitionDelay);
    }

    void AdvanceToNextCiv()
    {
        var aliveCivs = civsManager.GetAliveCivilizations();

        if (aliveCivs.Count == 0)
        {
            Debug.LogError("No alive civilizations remaining!");
            EndGame();
            return;
        }

        // Move to next civ
        currentCivIndex++;

        // Check if we've completed a full round of all civs
        if (currentCivIndex >= aliveCivs.Count)
        {
            // All civs have had their turn, advance to next turn
            currentCivIndex = 0;
            currentTurn++;
            OnTurnChanged?.Invoke(currentTurn);
            Debug.Log($"=== TURN {currentTurn} STARTED ===");

            // Check for game end conditions
            CheckGameEndConditions();
            if (currentGameState != GameState.Running)
                return;
        }

        // Start next civ's turn
        StartCurrentCivTurn();
    }

    // This method is called by the UI "End Turn" button
    public void NextTurn()
    {
        EndCurrentCivTurn();
    }

    // Legacy method - now processes only current civ
    public void ProcessTurn()
    {
        // This now only processes the current civilization
        var currentCiv = CurrentCivilization;
        if (currentCiv?.CivManager != null)
        {
            currentCiv.CivManager.ProcessTurn();
        }
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

    void SetGameState(GameState newState)
    {
        if (currentGameState != newState)
        {
            var oldState = currentGameState;
            currentGameState = newState;
            OnGameStateChanged?.Invoke(newState);
            Debug.Log($"Game state changed: {oldState} → {newState}");
        }
    }

    void Update()
    {
        // Debug hotkeys
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

        // Debug key to force start game
        if (Input.GetKeyDown(KeyCode.G) && currentGameState == GameState.Initializing)
        {
            StartGame();
        }
    }

    // Utility methods for UI
    public string GetCurrentCivName()
    {
        return CurrentCivilization?.CivName ?? "None";
    }

    public int GetCivTurnOrder()
    {
        var aliveCivs = civsManager?.GetAliveCivilizations();
        if (aliveCivs != null && aliveCivs.Count > 0)
        {
            return currentCivIndex + 1;
        }
        return 0;
    }

    public int GetTotalAliveCivs()
    {
        return civsManager?.GetAliveCivCount() ?? 0;
    }
}