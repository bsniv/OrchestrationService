using System.Collections.Concurrent;

namespace OrchestrationService.Logger;

public static class OverlayNetworkLoggerProvider
{
    public static void Init(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        _existingLoggers = new ConcurrentDictionary<string, ILogger>();
    }

    public static ILogger GetLogger(string componentName)
    {
        if (!_existingLoggers.ContainsKey(componentName))
        {
            // in case of race condition, 2 loggers will be created but eventually only 1 will be used.
            _existingLoggers.TryAdd(componentName, _loggerFactory.CreateLogger(componentName));
        }

        _existingLoggers.TryGetValue(componentName, out var logger);

        return logger;
    }

    private static ConcurrentDictionary<string, ILogger> _existingLoggers;
    private static ILoggerFactory _loggerFactory;
}
