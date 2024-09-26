using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;
using System.Net;
using Newtonsoft.Json;
using BotBot.Data;
using System.Text;
using BotBot.Data;
using System.Text.RegularExpressions;
using Telegram.Bot.Extensions;
using System.Xml;
using HtmlAgilityPack;
using System.Linq;

string token = "7190154174:AAHWCx_6w4VNdE_5sp0p3KYE4qVPwJ4sR-A";
var bot = new TelegramBotClient(token);

await bot.DropPendingUpdatesAsync();

//добавляем клавиатуру
var keyboard = new ReplyKeyboardMarkup();
//добавляем кнопку в 1 строку
keyboard.AddNewRow(); //вторая строка
                      //добавляем кнопку во вторую строку
keyboard.AddButtons(new KeyboardButton("Крикнуть гойда"));

//Другая логика отправки
var markup = new InlineKeyboardMarkup(
     new List<InlineKeyboardButton>()
     {
                     new InlineKeyboardButton("Донат по хохлам") {CallbackData = "1"},
                     new InlineKeyboardButton("Погода") {CallbackData = "2"},
                     new InlineKeyboardButton("Расписание на сегодня") {CallbackData = "3"}
     });

bot.OnMessage += async (message, type) =>
{
    //получаем ID чата
    var chatId = message.Chat.Id;
    //текст сообщения пользователя
    var messageText = message.Text;

    switch (messageText)
    {
        case "/start":
            
            await bot.SendTextMessageAsync(chatId, "Привет",replyMarkup: markup);
            Console.WriteLine(message.Chat.FirstName + " || Включил бота");
            break;
        case "Донат по хохлам":
            await bot.SendTextMessageAsync(chatId,"Стреляем по хохлам");
            Console.WriteLine(message.Chat.FirstName +" || ");
            SendDron(chatId);
            break;
        default:
            await bot.SendTextMessageAsync(chatId,"Команды нафиг", replyMarkup:markup);
            Console.WriteLine(message.Chat.FirstName+" || " + message.Text);
            break;
    }
};


//обработка данных, не отправленных в сообщениях
bot.OnUpdate += async (update) =>
{
    //проверка что тип поступивших данных - запрос обратного вызова
    if (update.Type == UpdateType.CallbackQuery)
    {
        //получаем идентификатор чата
        var chatId = update.CallbackQuery.Message.Chat.Id;
        //считываем полученный запрос
        var callbackData = update.CallbackQuery.Data;

        //проверяем значение запроса
        switch (callbackData)
        {
            case "1":
                await bot.SendTextMessageAsync(chatId, "Стреляем по хохлам");
                Console.WriteLine(update.CallbackQuery.Message.Chat.FirstName + " || Донатит по хохлам");
                SendDron(chatId);
                break;
            case "2":
                Console.WriteLine(update.CallbackQuery.Message.Chat.FirstName + " || Узнаёт погоду");
                var end = await ServerRequest();
                await bot.SendTextMessageAsync(chatId, end, replyMarkup: markup);
                break;
            case "3":
                Console.WriteLine(update.CallbackQuery.Message.Chat.FirstName + " || Узнаёт расписание");
                var Tables = await Shedule("https://api.vgltu.ru/s/schedule?date_start=2024-09-24&group_name=ИЛ2-241-ОБ");
                string output="";
                var query = Tables.SelectMany(table =>
                table.Time.Select((time, index) => new { Time = time, Lesson = table.LessonName[index] }))
                .Select(timeAndLesson => new
                {
                    time = timeAndLesson.Time,
                    lesson = timeAndLesson.Lesson
                });

                foreach (var Table in Tables)
                {
                    
                    //output += "День: " + Table.Day + "\n" + "Время:\n" + string.Join("\n",Table.Time.Select(x=>x.Trim())) + "\n" + string.Join("\n", Table.LessonName.Select(x =>x)) + "\n";
                }
                output = Regex.Replace(output, "<.*?>", "");
                //output = Regex.Replace(output, @"\\s", " ");
                output = Regex.Replace(output, "(\\s|\\t)+", " ");
                //Console.WriteLine(output);
                await bot.SendTextMessageAsync(chatId, output, replyMarkup: markup);
                break;
        }
    }
};


async void SendDron(long IdOfChat)
{
    FileStream fileStream = System.IO.File.OpenRead("B:\\VS\\работы\\BotBot\\Data\\Dron.mp4");
    await bot.SendVideoAsync(IdOfChat, fileStream);
};

//создаём запрос на сервер

async Task <string> ServerRequest()
{
    WebRequest webRequest = WebRequest.Create("https://api.openweathermap.org/data/2.5/weather?lat=51.66&lon=39.20&appid=b8d7338c7ff1412cb65b6a0751b250dd");
    webRequest.Method = "POST";
    webRequest.ContentType = "application/x-www-urlencoded";

    WebResponse response = await webRequest.GetResponseAsync();

    string Data;

    using (Stream s = response.GetResponseStream())
    {
        using (StreamReader sr = new StreamReader(s))
        {
            Data = await sr.ReadToEndAsync();
        }
    };
    response.Close();

    Root weather = JsonConvert.DeserializeObject<Root>(Data);
    Console.WriteLine(string.Join("\n", weather.weather.Select(x => x.description)));
    string _end = $"Ваш город = {weather.name}, Температура = { weather.main.temp - 273}";
    Console.WriteLine(_end);
    return _end;
}

async Task<List<Schedule>> Shedule(string url)
{
    string _shedule = "";
    List<Schedule> schedules = new List<Schedule>();

    using (WebClient webClient = new WebClient())
    {
        webClient.Encoding = Encoding.UTF8;
        _shedule = await webClient.DownloadStringTaskAsync(url);
        var htmlDoc = new HtmlAgilityPack.HtmlDocument();
        htmlDoc.LoadHtml(_shedule);
        var tableNodes = htmlDoc.DocumentNode.SelectNodes("//table");

        Schedule schedule = new Schedule();

        foreach (var table in tableNodes)
        {
            var dates = table.SelectSingleNode(".//td");
            var times = table.SelectNodes(".//td");
            var HtmlLessons = table.SelectNodes(".//td");

            List<string> hours = new List<string>();
            List<string> Lessons = new List<string>();

            schedule.Day = dates.InnerHtml; //dates.GetAttributeValue("rowspan",string.Empty);

            //Исключаем день из массива уроков Complete
            for (int i = 0; i < HtmlLessons.Count; i++)
            {
                if (HtmlLessons[i].OuterHtml.Contains(schedule.Day))
                {
                    HtmlLessons.Remove(HtmlLessons[i]);
                }
            }

            //Исключаем день из массива времени Complete
            for (int i = 0; i < times.Count; i++)
            {
                if (times[i].OuterHtml.Contains(schedule.Day))
                {
                    times.Remove(times[i]);
                }
            }

            //Получаем отдельный массив именно времени Complete
            for (int i = 0; i < times.Count; i++)
            {
                if (times[i].OuterHtml.Contains("rowspan"))
                {
                    hours.Add(times[i].InnerHtml.Trim());
                }
            }
            schedule.Time = hours.ToArray();

            // Проверяем, содержит ли текущий элемент HtmlLessons хотя бы одно время из schedule.Time Complete
            for (int i = 0; i < HtmlLessons.Count; i++)
            {
                foreach (var time in schedule.Time)
                {
                    if (HtmlLessons[i].OuterHtml.Contains(time))
                    {
                        HtmlLessons.Remove(HtmlLessons[i]);
                    }
                }
            }

            foreach (var lesson in HtmlLessons)
            {
                Console.WriteLine(lesson.OuterHtml);
                Console.WriteLine("\n");
            }
        }
    }
    return schedules;
}
Console.ReadKey();