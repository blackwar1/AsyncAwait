namespace AsyncMonitor;
public sealed record CheckResult(string Name, bool Success, TimeSpan Duration, string Message);