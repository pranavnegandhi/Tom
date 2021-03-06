using Microsoft.Extensions.Hosting;
using Serilog;

namespace Notadesigner.Tom.Core
{
    public class PomoEngine : IHostedService
    {
        public event EventHandler<ProgressEventArgs>? Progress;

        public event EventHandler<StateChangeEventArgs>? StateChange;

        public event EventHandler<EventArgs>? StartSession;

        private readonly ILogger _log = Log.ForContext<PomoEngine>();

        private readonly Func<PomoEngineSettings> _settingsFactory;

        private readonly NotificationsQueue _queue;

        private IEngineState _engineState;

        private CancellationTokenSource? _cts;

        private Task? _execute;

        public PomoEngine(Func<PomoEngineSettings> settingsFactory, NotificationsQueue queue)
        {
            _settingsFactory = settingsFactory;
            _queue = queue;
            _engineState = new AppReadyState(0, _settingsFactory, _queue);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _log.Verbose("Starting {serviceName}", nameof(PomoEngine));
            await Task.CompletedTask;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _execute = ExecuteAsync(_cts.Token);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _cts?.Cancel();
            _ = _execute ?? throw new InvalidOperationException();

            await _execute;
        }

        public void MoveNext()
        {
            if (_engineState.State == EngineState.AppReady)
            {
                StartSession?.Invoke(this, EventArgs.Empty);
            }

            _engineState.MoveNext();
        }

        public EngineState State => _engineState.State;

        private async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _log.Verbose("Executing {serviceName}", nameof(PomoEngine));
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await _engineState.EnterAsync(cancellationToken);
                    var notification = await _queue.DequeueAsync(cancellationToken);
                    _engineState.Progress -= EngineStateProgressHandler; /// Stop listening for Progress events from the current state instance
                    _engineState = notification?.State ?? throw new InvalidOperationException();
                    _engineState.Progress += EngineStateProgressHandler; /// Start listening for Progress events from the new state instance

                    StateChange?.Invoke(this, new StateChangeEventArgs(_engineState.State, _engineState.RoundCounter));
                }
                catch (TaskCanceledException)
                {
                    /// Nothing to do here
                }
                catch (Exception exception)
                {
                    _log.Fatal(exception, "A fatal exception occured in the {serviceName}", nameof(PomoEngine));
                }
            }
        }

        private void EngineStateProgressHandler(object? sender, ProgressEventArgs e)
        {
            Progress?.Invoke(this, e);
        }
    }
}