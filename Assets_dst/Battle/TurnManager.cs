using UnityEngine;
using System;

public class TurnManager : MonoBehaviour
{
    public static TurnManager instance;

    public enum TurnState
    {
        PlayerTurn,
        EnemyTurn
    }

    [Header("State")]
    public TurnState currentTurn = TurnState.PlayerTurn;

    public event Action<TurnState> onTurnChanged;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        Debug.Log($"[TurnManager] Start -> {currentTurn}");
        onTurnChanged?.Invoke(currentTurn);
    }

    public void NextTurn()
    {
        if (currentTurn == TurnState.PlayerTurn)
            SetTurn(TurnState.EnemyTurn);
        else
            SetTurn(TurnState.PlayerTurn);
    }

    public void SetTurn(TurnState nextTurn)
    {
        if (currentTurn == nextTurn) return;

        currentTurn = nextTurn;
        Debug.Log($"[TurnManager] Turn Changed -> {currentTurn}");
        onTurnChanged?.Invoke(currentTurn);
    }

    public bool IsPlayerTurn()
    {
        return currentTurn == TurnState.PlayerTurn;
    }

    public bool IsEnemyTurn()
    {
        return currentTurn == TurnState.EnemyTurn;
    }

    // Optional: quick test in Play Mode (Space = next turn)
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            NextTurn();
        }
    }
}
