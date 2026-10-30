using System.Diagnostics;

namespace AsyncMonitor;

public sealed class CheckService
{
    // Практика 1: одна проверка на Task.Delay, не блокирует вызывающий поток.
    public async Task<CheckResult> CheckServiceAsync(
        string name,
        TimeSpan delay,
        CancellationToken cancellationToken = default)
    {
        // TODO 1: Отклонить пустое имя проверки.
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Имя проверки не может быть пустым.", nameof(name));

        // TODO 2: Отклонить отрицательную длительность.
        if (delay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(
                nameof(delay), delay, "Длительность не может быть отрицательной.");

        var sw = Stopwatch.StartNew();
        try
        {
            // TODO 3: await Task.Delay, а не Thread.Sleep. Поток свободен.
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            sw.Stop();
            // Пробрасываем отмену — её обработает вызывающий код (catch OperationCanceledException).
            throw;
        }
        sw.Stop();

        // TODO 4: Корректный CheckResult.
        return new CheckResult(
            Name: name,
            Success: true,
            Duration: sw.Elapsed,
            Message: $"Проверка «{name}» завершена за {sw.ElapsedMilliseconds} мс.");
    }

    // TODO 5: Последовательное выполнение — каждая проверка ожидается через await.
    public async Task<IReadOnlyList<CheckResult>> RunSequentialAsync(
        IReadOnlyList<(string Name, TimeSpan Delay)> checks,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(checks);

        var results = new List<CheckResult>(checks.Count);
        foreach (var check in checks)
        {
            // Ждём каждую проверку по очереди — общее время = сумма задержек.
            var result = await CheckServiceAsync(check.Name, check.Delay, token)
                .ConfigureAwait(false);
            results.Add(result);
        }
        return results;
    }

    // TODO 6 + 7: Параллельное выполнение через Task.WhenAll.
    public async Task<IReadOnlyList<CheckResult>> RunParallelAsync(
        IReadOnlyList<(string Name, TimeSpan Delay)> checks,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(checks);

        // TODO 6: Сначала создаём ВСЕ задачи, ничего не ожидая.
        var tasks = new Task<CheckResult>[checks.Count];
        for (int i = 0; i < checks.Count; i++)
        {
            tasks[i] = CheckServiceAsync(checks[i].Name, checks[i].Delay, token);
        }

        // TODO 7: Совместное ожидание всех задач.
        // WhenAll пробрасывает первое исключение; при отмене — OperationCanceledException.
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        return results;
    }
}