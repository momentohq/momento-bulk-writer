public static class ExitUtils
{
    /// <summary>
    /// Exits the program after a delay. A delay is necessary to allow the logger to flush.
    /// </summary>
    /// <param name="exitCode"></param>
    /// <param name="delay"></param>
    /// <returns></returns>
    public static async Task DelayedExit(int exitCode = 1, int delay = 1)
    {
        await Task.Delay(delay);
        Environment.Exit(1);
    }
}
