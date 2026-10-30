using System.Diagnostics;
using AsyncMonitor;

var service = new CheckService();

var checks = new (string Name, TimeSpan Delay)[]
{
    ("Сеть",         TimeSpan.FromMilliseconds(700)),
    ("База данных",  TimeSpan.FromMilliseconds(900)),
    ("Файлы",        TimeSpan.FromMilliseconds(500))
};

while (true)
{
    Console.WriteLine("\n1 — одна проверка; 2 — последовательно; 3 — параллельно; 0 — выход");
    var cmd = Console.ReadLine();
    if (cmd == "0")
        break;

    // TODO 10: на каждую команду — свой CancellationTokenSource.
    // Меню не зависает: пока идёт await Task.Delay, поток свободен,
    // но для наглядности добавим отмену по таймауту.
    using var cts = new CancellationTokenSource();

    try
    {
        if (cmd == "1")
        {
            // TODO 8: await без .Wait() / .Result.
            // Ждём недолго, чтобы продемонстрировать неблокирующее ожидание:
            // пока идёт Task.Delay, можно было бы принимать ввод — поток свободен.
            var r = await service.CheckServiceAsync(
                "Сеть", TimeSpan.FromMilliseconds(700), cts.Token);
            Print(new[] { r });
        }
        else if (cmd == "2")
        {
            var sw = Stopwatch.StartNew();
            var r = await service.RunSequentialAsync(checks, cts.Token);
            sw.Stop();
            Console.WriteLine($"Последовательно: {sw.ElapsedMilliseconds} мс");
            Print(r);
        }
        else if (cmd == "3")
        {
            var sw = Stopwatch.StartNew();
            var r = await service.RunParallelAsync(checks, cts.Token);
            sw.Stop();
            Console.WriteLine($"Параллельно: {sw.ElapsedMilliseconds} мс");
            Print(r);
        }
        else
        {
            Console.WriteLine("Неизвестная команда.");
        }
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("Проверка отменена.");
    }
    catch (Exception ex)
    {
        // TODO 9: не скрывать исключение — тип + причина + сообщение.
        Console.WriteLine($"[ОШИБКА] {ex.GetType().Name}: {ex.Message}");
        if (ex.InnerException is not null)
            Console.WriteLine($"  внутренняя: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
    }
}

static void Print(IEnumerable<CheckResult> xs)
{
    foreach (var x in xs)
    {
        var status = x.Success ? "OK " : "ERR";
        Console.WriteLine($"  [{status}] {x.Name,-12} {x.Duration.TotalMilliseconds,6:F0} мс — {x.Message}");
    }
}