using System;

namespace MathRunner.Core.Run
{
    public enum GameState
    {
        Menu,
        Countdown,
        Running,
        Paused,
        GameOver,
    }

    /// <summary>Allowed transitions between game states.</summary>
    public sealed class GameStateMachine
    {
        public GameState State { get; private set; } = GameState.Menu;

        public event Action<GameState, GameState> Changed;

        public bool CanMoveTo(GameState next)
        {
            switch (State)
            {
                case GameState.Menu: return next == GameState.Countdown;
                case GameState.Countdown: return next == GameState.Running || next == GameState.Menu;
                case GameState.Running:
                    return next == GameState.Paused || next == GameState.GameOver;
                case GameState.Paused:
                    return next == GameState.Running || next == GameState.Menu;
                case GameState.GameOver:
                    return next == GameState.Countdown || next == GameState.Menu;
                default: return false;
            }
        }

        public bool TryMoveTo(GameState next)
        {
            if (!CanMoveTo(next)) return false;
            var previous = State;
            State = next;
            Changed?.Invoke(previous, next);
            return true;
        }
    }
}
