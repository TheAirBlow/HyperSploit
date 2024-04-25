using AdvancedSharpAdbClient.Receivers;

namespace HyperSploit;

/// <summary>
/// Event-based output receiver
/// </summary>
public class EventOutputReceiver : IShellOutputReceiver {
    /// <summary>
    /// Output event handler delegate
    /// </summary>
    public delegate void OutputEventHandler(EventOutputReceiver sender, string line);
    
    /// <summary>
    /// On output line event
    /// </summary>
    public event OutputEventHandler? OnOutput;

    /// <summary>
    /// Should output be terminated
    /// </summary>
    private bool _terminate;
    
    /// <summary>
    /// Add output line
    /// </summary>
    /// <param name="line">Line</param>
    /// <returns>True on success</returns>
    public bool AddOutput(string line) {
        if (_terminate) return false;
        OnOutput?.Invoke(this, line);
        return !_terminate;
    }

    /// <summary>
    /// Flush output
    /// </summary>
    public void Flush() {
        // Do nothing
    }

    /// <summary>
    /// Should output handling be terminated
    /// </summary>
    public void Terminate()
        => _terminate = true;

    /// <summary>
    /// Add output line
    /// </summary>
    /// <param name="line">Line</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True on success</returns>
    public Task<bool> AddOutputAsync(string line, CancellationToken cancellationToken)
        => Task.FromResult(AddOutput(line));

    /// <summary>
    /// Flush output
    /// </summary>
    public Task FlushAsync(CancellationToken cancellationToken) {
        Flush(); return Task.CompletedTask;
    }
}