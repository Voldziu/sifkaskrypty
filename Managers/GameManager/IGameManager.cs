using UnityEngine;

public enum GameState
{
    Initializing,
    Running,
    Paused,
    GameOver
}

public interface IGameManager
{
    GameState CurrentGameState { get; }
    int CurrentTurn { get; }

    // Core Managers
    IMapManager MapManager { get; }
    ICivsManager CivsManager { get; }

    // Game Flow
    void StartGame();
    void PauseGame();
    void ResumeGame();
    void EndGame();

    // Turn Management
    void NextTurn();
    void ProcessTurn();

    // Game Events
    event System.Action<GameState> OnGameStateChanged;
    event System.Action<int> OnTurnChanged;
}