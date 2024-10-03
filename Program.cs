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
using static System.Runtime.InteropServices.JavaScript.JSType;
using Telegram.Bot.Types;
using Microsoft.EntityFrameworkCore;

string token = "7190154174:AAHWCx_6w4VNdE_5sp0p3KYE4qVPwJ4sR-A";
var bot = new TelegramBotClient(token);

await bot.DropPendingUpdatesAsync();

//добавляем клавиатуру
var keyboard = new ReplyKeyboardMarkup();
//добавляем кнопку в 1 строку
keyboard.AddButtons(new KeyboardButton("/start"));
keyboard.AddNewRow(); //вторая строка
keyboard.AddButtons(new KeyboardButton("Донат по хохлам"), new KeyboardButton("Узнать расписание"));
keyboard.AddNewRow();
keyboard.AddButtons(new KeyboardButton("Изменить группу"));

//Другая логика отправки
var markup = new InlineKeyboardMarkup(
     new List<InlineKeyboardButton>()
     {
                     new InlineKeyboardButton("Донат по хохлам") {CallbackData = "1"},
                     new InlineKeyboardButton("Расписание на сегодня") {CallbackData = "2"},
                     new InlineKeyboardButton("Изменить группу") {CallbackData = "3"}
     });

bot.OnMessage += async (message, type) =>
{
    //получаем ID чата
    var chatId = message.Chat.Id;
    //текст сообщения пользователя
    var messageText = message.Text;

    using DataContext dataContext = new DataContext();
    var user = dataContext.Users.FirstOrDefault(i => i.Id == message.Chat.Id);
    if (user !=null && user.WaitToChangeGroup)
    {
        user.GroupName = message.Text;
        await bot.SendTextMessageAsync(chatId, "Вы успешно сменили название группы!", replyMarkup: markup);
    }

    switch (messageText)
    {
        case "/start":
            ChekingDatabase(message);
            var messageId = message.MessageId;
            await bot.SendTextMessageAsync(chatId, "Привет",replyMarkup: keyboard);
            await bot.DeleteMessageAsync(chatId, messageId+1);
            await bot.SendTextMessageAsync(chatId, "Текущий список команд:", replyMarkup: markup);
            Console.WriteLine(message.Chat.FirstName + " || Включил бота");
            break;
        case "Донат по хохлам":
            await bot.SendTextMessageAsync(chatId,"Стреляем по хохлам");
            Console.WriteLine(message.Chat.FirstName +" || Стреляет по хохлам");
            SendDron(chatId);
            break;
        case "Узнать расписание":
            Console.WriteLine(message.Chat.FirstName + " || Узнаёт рассписание");
            var Tables = await Shedule("https://api.vgltu.ru/s/schedule?date_start=2024-09-24&group_name=ИЛ2-241-ОБ");
            string output = "";

            for (int i = 0; i < 6; i++)
            {
                output += Tables[i].Day + "\n";
                for (int j = 0; j < Tables[i].Time.Length; j++)
                {
                    output += "🕓" + Tables[i].Time[j] + "\n";
                    output += Tables[i].LessonName[j] + "\n";
                }
            }
            output = Regex.Replace(output, "<.*?>", string.Empty);
            await bot.SendTextMessageAsync(chatId, output, replyMarkup: markup);
            break;
        case "Изменить группу":
                var userChange = dataContext.Users.FirstOrDefault(i => i.Id == message.Chat.Id);
                await bot.SendTextMessageAsync(chatId, "Вы хотите сменить группу\nВаша текущая группа: " + userChange.GroupName + "\nВведите точное название новой группы:");
                userChange.WaitToChangeGroup = true;
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
                Console.WriteLine(update.CallbackQuery.Message.Chat.FirstName + " || Узнаёт расписание");
                var Tables = await Shedule("https://api.vgltu.ru/s/schedule?date_start=2024-09-24&group_name=ИЛ2-241-ОБ");
                string output="";
                
                for (int i=0;i<6;i++)
                {
                    output += Tables[i].Day + "\n";
                    for (int j = 0; j < Tables[i].Time.Length; j++)
                    {
                        output += "🕓" + Tables[i].Time[j] + "\n";
                        output +=Tables[i].LessonName[j] + "\n";
                    }
                }
                output = Regex.Replace(output, "<.*?>", string.Empty);
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

        

        foreach (var table in tableNodes)
        {
            var dates = table.SelectSingleNode(".//td");
            var times = table.SelectNodes(".//td");
            var HtmlLessons = table.SelectNodes(".//td");

            List<string> hours = new List<string>();
            List<string> Lessons = new List<string>();
            Schedule schedule = new Schedule();

            schedule.Day = dates.InnerHtml.Trim();

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
                        if (HtmlLessons[i].GetAttributeValue("rowspan", string.Empty) == "2")
                        {
                            string subgroups = "📚" + HtmlLessons[i + 1].InnerHtml + "\n" + "📚" + HtmlLessons[i + 2].InnerHtml;
                            Lessons.Add(subgroups);
                        }
                        else
                        {
                            Lessons.Add("📚" + HtmlLessons[i+1].InnerHtml);
                        }
                        HtmlLessons.Remove(HtmlLessons[i]);
                    }
                }
            }

            List<string> normal = new List<string>();
            for (int i = 0; i < Lessons.Count; i++)
            {
                normal = Lessons[i].Split("<span>").ToList();
                Lessons[i] = string.Empty;
                for (int j = 0; j < normal.Count; j++)
                {
                    normal[j] = normal[j].Trim();
                    Lessons[i] += normal[j] + "\n";
                }
            }

            schedule.Day = "                                " + "📅" + schedule.Day;
            schedule.LessonName = Lessons.ToArray();
            
            schedules.Add(schedule);
        }
    }
    return schedules;
}


//Проверка, есть ли текущий пользователь в базе данных
async Task ChekingDatabase(Telegram.Bot.Types.Message message)
{
    using DataContext dataContext = new DataContext();
    var user = await dataContext.Users.FirstOrDefaultAsync(i => i.Id == message.Chat.Id);
    if (user == null)
    {
        BotBot.Data.User User = new BotBot.Data.User()
        {
            Id = message.Chat.Id,
            UserName = message.Chat.Username
        };
        try
        {
            await dataContext.Users.AddAsync(User);
            await dataContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        
        Console.WriteLine(message.Chat.Username + " || Был добавлен в базу данных");
    }
}

Console.ReadKey();