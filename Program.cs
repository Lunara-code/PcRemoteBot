using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
Console.OutputEncoding = System.Text.Encoding.UTF8;

var token = Environment.GetEnvironmentVariable("BOT_TOKEN") // создаем переменную для хранения токена бота   (номер телефона на бумажке)
    ?? throw new InvalidOperationException("Переменная окружения 'BOT_TOKEN' не установлена"); // проверяем, что токен установлен
var allowedIdText = Environment.GetEnvironmentVariable("ALLOWED_USER_ID")
    ?? throw new InvalidOperationException("Переменная окружения ALLOWED_USER_ID не задана.");
if (!long.TryParse(allowedIdText, out var allowedUserId))
{
    throw new InvalidOperationException("ALLOWED_USER_ID должен быть числом.");
}
using var cts = new CancellationTokenSource();
var bot = new TelegramBotClient(token, cancellationToken: cts.Token); // создаем экземпляр клиента бота с использованием токена и токена отмены
var me = await bot.GetMe(); // получаем информацию о боте (Звоним и спрашиваем кто я такой)
Console.WriteLine($"Бот @{me.Username} запущен."); // выводим имя бота в консоль
var notifiedStrangers = new HashSet<long>(); // создаем множество для хранения ID пользователей, которые не являются владельцем ПК
bot.OnMessage += OnMessage;
bot.OnError += OnError;

Console.WriteLine("Напишите боту в Telegram. Enter - остановить.");
Console.ReadLine(); // ждем ввода пользователя для остановки бота
cts.Cancel(); // отменяем процесс получения обновлений

async Task OnMessage(Message msg, UpdateType type)
{
    Log($"{msg.From?.FirstName} ({msg.From?.Id}) ➡️{msg.Text}");
    if (msg.From?.Id != allowedUserId)
    {
        //Console.WriteLine($"До await: поток {Environment.CurrentManagedThreadId}");
        Log($"Отклонено сообщение от {msg.From?.Id}");
        if (msg.From != null) //защищает от сообщений без отправителя (каналы)
        {
            bool firstTime; //объявляем заранее, потому что значение будет присвоено внутри блока lock, а использовано снаружи
            lock (notifiedStrangers)
            {
                firstTime = notifiedStrangers.Add(msg.From.Id); //возвращает true, если ID добавлен впервые. Если человек уже был в списке, вернёт false, и ответа не будет.
            }
            if(firstTime)
            { 
                await bot.SendMessage(msg.Chat, "⚠️ Пожалуйста, не пишите мне больше. Вы не являетесь владельцем данного ПК.");
                Log($"Предупреждение отправлено: {msg.From.Id}");
            }
        }
        return;
    }
    if (msg.Text is null)
    {
        await bot.SendMessage(msg.Chat, "Я понимаю только текстовые сообщения.");
        Log($"Попытка написать не текстовое сообщение у пользователя {msg.From?.Id}");
        return;
    }
    if (msg.Text == "/lock")
    {
        bool locked = NativeMethods.LockWorkStation();
        await bot.SendMessage(msg.Chat, locked ? "Экран был заблокирован" : "Не удалось заблокировать ваш экран ПК");
        Log($"Попытка заблокировать экран ПК: {locked}");
        return;
    }
    if (msg.Text == "/steam")
    {
        try
        {
            ProgramLauncher.Start("steam://open/main");
            await bot.SendMessage(msg.Chat, "Steam был успешно открыт.");
            Log($"Попытка открыть Steam: успешно");
        }
        catch (Exception ex)
        {
            await bot.SendMessage(msg.Chat, $"К сожалению Steam не смог открыться по причине: {ex.Message}");
            Log($"Попытка открыть Steam: неудачно. Причина: {ex.Message}");
        }
        return;
    }
   if (msg.Text == "/chrome")
    {
        try
        {
            ProgramLauncher.Start("chrome");
            await bot.SendMessage(msg.Chat, "Chrome был успешно открыт.");
            Log($"Попытка открыть Chrome: успешно");
        }
        catch (Exception ex)
        {
            await bot.SendMessage(msg.Chat, $"К сожалению браузер Chrome не смог открыться по причине: {ex.Message}");
            Log($"Попытка открыть Chrome: неудачно. Причина: {ex.Message}");
        }
        return;

        
    }

    await bot.SendMessage(msg.Chat, $"Вы написали: {msg.Text}");

    //Console.WriteLine($"После await: поток {Environment.CurrentManagedThreadId}");
}
Task OnError (Exception exception, HandleErrorSource source)
{
    Log($"Ошибка ({source}): {exception.Message}");
    return Task.CompletedTask;
}
void Log(string text)
{
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {text}");
}
