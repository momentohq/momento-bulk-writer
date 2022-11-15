using Microsoft.Extensions.Logging;

namespace Momento.Etl.Utils;

public static class LoggerUtils
{
    public static ILoggerFactory CreateConsoleLoggerFactory()
    {
        return LoggerFactory.Create(builder =>
            {
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.SingleLine = true;
                    options.TimestampFormat = "hh:mm:ss ";
                });
                builder.SetMinimumLevel(LogLevel.Information);
            });
    }
}
