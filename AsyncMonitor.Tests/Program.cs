using AsyncMonitor;

var passed = 0;
var failed = 0;

void Check(bool ok, string name)
{
    if (ok)
    {
        passed++;
        Console.WriteLine("PASS " + name);
    }
    else
    {
        failed++;
        Console.WriteLine("FAIL " + name);
    }
}

var s = new CheckService();

var one = await s.CheckServiceAsync("Тест", TimeSpan.FromMilliseconds(25));
Check(
    one.Name == "Тест" && one.Success && one.Duration >= TimeSpan.Zero,
    "одиночная проверка возвращает результат"
);

var seq = await s.RunSequentialAsync(new[]
{
    ("A", TimeSpan.FromMilliseconds(60)),
    ("B", TimeSpan.FromMilliseconds(60)),
    ("C", TimeSpan.FromMilliseconds(60))
});
Check(
    seq.Count == 3 && seq.Select(x => x.Name).SequenceEqual(new[] { "A", "B", "C" }),
    "последовательный запуск"
);

var all = await s.RunParallelAsync(new[]
{
    ("A", TimeSpan.FromMilliseconds(60)),
    ("B", TimeSpan.FromMilliseconds(60)),
    ("C", TimeSpan.FromMilliseconds(60))
});
Check(all.Count == 3 && all.All(x => x.Success), "параллельный запуск WhenAll");

// --- Новые тесты (TODO) ---

// 1. Пустое имя → ArgumentException.
try
{
    await s.CheckServiceAsync("   ", TimeSpan.FromMilliseconds(10));
    Check(false, "пустое имя → ArgumentException");
}
catch (ArgumentException)
{
    Check(true, "пустое имя → ArgumentException");
}

// 2. Отрицательная задержка → ArgumentOutOfRangeException.
try
{
    await s.CheckServiceAsync("X", TimeSpan.FromMilliseconds(-1));
    Check(false, "отрицательная задержка → ArgumentOutOfRangeException");
}
catch (ArgumentOutOfRangeException)
{
    Check(true, "отрицательная задержка → ArgumentOutOfRangeException");
}

// 3. Отмена через CancellationToken.
using (var cts = new CancellationTokenSource())
{
    cts.CancelAfter(TimeSpan.FromMilliseconds(30));
    try
    {
        await s.CheckServiceAsync("Отменяемая", TimeSpan.FromMilliseconds(500), cts.Token);
        Check(false, "отмена → OperationCanceledException");
    }
    catch (OperationCanceledException)
    {
        Check(true, "отмена → OperationCanceledException");
    }
}

// 4. Отмена в параллельном режиме.
using (var cts = new CancellationTokenSource())
{
    cts.CancelAfter(TimeSpan.FromMilliseconds(30));
    try
    {
        await s.RunParallelAsync(new[]
        {
            ("A", TimeSpan.FromMilliseconds(500)),
            ("B", TimeSpan.FromMilliseconds(500))
        }, cts.Token);
        Check(false, "параллельная отмена → OperationCanceledException");
    }
    catch (OperationCanceledException)
    {
        Check(true, "параллельная отмена → OperationCanceledException");
    }
}

// 5. Параллельно быстрее, чем последовательно (грубая проверка).
var sw = System.Diagnostics.Stopwatch.StartNew();
await s.RunParallelAsync(new[]
{
    ("A", TimeSpan.FromMilliseconds(200)),
    ("B", TimeSpan.FromMilliseconds(200)),
    ("C", TimeSpan.FromMilliseconds(200))
});
sw.Stop();
Check(sw.ElapsedMilliseconds < 450, $"параллельно быстрее последовательного ({sw.ElapsedMilliseconds} мс)");

Console.WriteLine($"Итого: {passed} пройдено, {failed} ошибок.");
return failed == 0 ? 0 : 1;