using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.ReplyMarkups;
using Update = Telegram.Bot.Types.Update;
using Telegram.Bot.Types.InlineQueryResults;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Bot_Telegram
{
    class Program
    {
        static string connectionString;
        private static NpgsqlConnection sql;

        static string telegramBotToken;
        private static ITelegramBotClient _botClient;// Это клиент для работы с Telegram Bot API, который позволяет отправлять сообщения, управлять ботом, подписываться на обновления и многое другое.
        private static ReceiverOptions _receiverOptions; // Это объект с настройками работы бота. Здесь мы будем указывать, какие типы Update мы будем получать, Timeout бота и так далее.
        public static ITelegramBotClient BotClient { get => _botClient; set => _botClient = value; }
        public static ReceiverOptions ReceiverOptions { get => _receiverOptions; set => _receiverOptions = value; }
        public static NpgsqlConnection Sql { get => sql; set => sql = value; }
        
        public static async Task TelegramBotInit()
        {
            telegramBotToken = System.IO.File.ReadAllText(@"C:\\_учеба\\_tg_bot\\tg_bot\\tg_token.txt");
            
            BotClient = new TelegramBotClient(telegramBotToken); // Присваиваем нашей переменной значение, в параметре передаем Token, полученный от BotFather

            ReceiverOptions = new ReceiverOptions // Также присваем значение настройкам бота
            {
                AllowedUpdates = new[] // Тут указываем типы получаемых Update`ов, о них подробнее расказано тут https://core.telegram.org/bots/api#update
                {
                    UpdateType.Message,// Сообщения (текст, фото/видео, голосовые/видео сообщения и т.д.)
                    UpdateType.CallbackQuery// ЁБАННЫЕ КОЛЛБЕКИ
                },

                // Параметр, отвечающий за обработку сообщений, пришедших за то время, когда ваш бот был оффлайн
                // True - не обрабатывать, False (стоит по умолчанию) - обрабаывать
                ThrowPendingUpdates = true,
            };
            using var cts = new CancellationTokenSource();

            // UpdateHander - обработчик приходящих Update`ов
            // ErrorHandler - обработчик ошибок, связанных с Bot API
            BotClient.StartReceiving(UpdateHandler, ErrorHandler, ReceiverOptions, cts.Token); // Запускаем бота

            var me = await BotClient.GetMeAsync(); // Создаем переменную, в которую помещаем информацию о нашем боте.

            Console.WriteLine($"{me.FirstName} запущен!");
        }

        public static async Task NpgsqlInit()
        {
            connectionString = System.IO.File.ReadAllText(@"C:\\_учеба\\_tg_bot\\tg_bot\\cnt_string.txt");
            sql = new NpgsqlConnection(connectionString);
        }

        static async Task Main()
        {
            try
            {
                await TelegramBotInit();
                await NpgsqlInit();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            await Task.Delay(-1); // Устанавливаем бесконечную задержку, чтобы наш бот работал постоянно
        }

        private static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken){
            // Обязательно ставим блок try-catch, чтобы наш бот не "падал" в случае каких-либо ошибок
            try{
                // Сразу же ставим конструкцию switch, чтобы обрабатывать приходящие Update
                switch (update.Type) {
                    #region message_updates
                    case UpdateType.Message:
                        {
                            // Эта переменная будет содержать в себе все связанное с сообщениями
                            var message = update.Message;

                            // From - это от кого пришло сообщение (или любой другой Update)
                            var user = message.From;

                            // Выводим на экран то, что пишут нашему боту, а также небольшую информацию об отправителе
                            Console.WriteLine($"{user.FirstName} ({user.Id}) написал сообщение: {message.Text}");

                            // Chat - содержит всю информацию о чате
                            var chat = message.Chat;

                            // Добавляем проверку на тип Message
                            switch (message.Type){     
                                // Тут понятно, текстовый тип
                                case MessageType.Text:{     
                                        // тут обрабатываем команду /start, остальные аналогично
                                        if (message.Text == "/start" || message.Text == "Начать" || message.Text == "Старт" || message.Text == "\n")
                                        {
                                            await InsertIntoLogs(chat.Id, 1);

                                            var startKeyboard = new ReplyKeyboardMarkup(
                                            new List<KeyboardButton[]>()
                                            {
                                                new KeyboardButton[]
                                                {
                                                    new KeyboardButton("Студент ЗФО"),
                                                    new KeyboardButton("Сотрудник УВП"),
                                                },

                                                new KeyboardButton[]
                                                {
                                                    new KeyboardButton("До свидания"),
                                                },
                                            })
                                            { ResizeKeyboard = true, };
                                            startKeyboard.OneTimeKeyboard = true;

                                            await botClient.SendTextMessageAsync(
                                                chat.Id,
                                                "Добро пожаловать, " + $"{message.From.FirstName}" + "!\n" +
                                                "Я  - бот-помощник, чтобы использовать мой функционал выбирете, кем вы являетесь:",
                                                replyMarkup: startKeyboard);


                                            break;
                                        }

                                        if (message.Text == "До свидания")
                                        {
                                            var replyKeyboard = new ReplyKeyboardMarkup(
                                            new List<KeyboardButton[]>()
                                            {
                                                new KeyboardButton[]
                                                {
                                                    new KeyboardButton("Старт"),
                                                }
                                            })
                                            { ResizeKeyboard = true, };

                                            await botClient.SendTextMessageAsync(
                                            chat.Id,
                                            "До свидания, " + $"{message.From.FirstName}" + $" {message.From.LastName}!",
                                            replyMarkup: replyKeyboard);
                                            break;
                                        }

                                        if (message.Text == "Назад")
                                        {
                                            var backKeyboard = new ReplyKeyboardMarkup(
                                            new List<KeyboardButton[]>()
                                            {
                                                    new KeyboardButton[]
                                                    {
                                                        new KeyboardButton("Студент ЗФО"),
                                                        new KeyboardButton("Сотрудник УВП"),
                                                    },

                                                    new KeyboardButton[]
                                                    {
                                                        new KeyboardButton("До свидания"),
                                                    },
                                            })
                                            { ResizeKeyboard = true, };
                                            await botClient.SendTextMessageAsync(
                                                chat.Id,
                                                "",
                                                replyMarkup: backKeyboard
                                            );

                                            break;
                                        }

                                        if (message.Text == "Студент ЗФО")
                                        {
                                            var studentKeyboard = new InlineKeyboardMarkup(
                                            new InlineKeyboardButton[2]
                                                {
                                                InlineKeyboardButton.WithCallbackData("Бакалавриат/Специалитет", "Bachelor"),
                                                InlineKeyboardButton.WithCallbackData("Магистратура", "Magistr")
                                                }
                                            )
                                            { };

                                            await botClient.SendTextMessageAsync(
                                                chat.Id,
                                                text: "Выбирете ступень образования:",
                                                allowSendingWithoutReply: true,
                                                replyMarkup: studentKeyboard
                                            );
                                            //var send = new SendMessage(update.Message.Chat.Id, "Студент ЗФО")
                                            //{
                                            //    ReplyMarkup = new ReplyKeyboardRemove() { RemoveKeyboard = true }
                                            //};
                                            //await botClient.MakeRequestAsync(send);

                                            break;
                                    }




                                    if (message.Text == "Страница кафедры")
                                    {
                                        var siteKeyboard = new InlineKeyboardMarkup(
                                            new List<InlineKeyboardButton[]>()
                                            {
                                                new InlineKeyboardButton[]
                                                {
                                                    InlineKeyboardButton.WithUrl("Страница кафедры","https://istu.ru/department/kafedra-programmnoe-obespechenie"),
                                                },
                                            }
                                        );

                                        await botClient.SendTextMessageAsync(
                                        chat.Id,
                                        "Нажмите на кнопку для перехода на страницу кафедры",
                                        replyMarkup: siteKeyboard);

                                        break;
                                    }


                                    if (message.Text == "Сотрудник УВП")
                                    {
                                        var replyKeyboard = new ReplyKeyboardMarkup(
                                            new List<KeyboardButton[]>()
                                            {
                                                new KeyboardButton[]
                                                {
                                                    new KeyboardButton("Редaктировать контакты преподавателей"),
                                                    new KeyboardButton("Редактировать часто задаваемые вопросы"),
                                                },
                                                new KeyboardButton[]
                                                {
                                                    new KeyboardButton("Добавить расписание экзаменов"),
                                                    new KeyboardButton("Страница кафедры"),
                                                },
                                                new KeyboardButton[]
                                                {
                                                    new KeyboardButton("До свидания")
                                                }
                                            }
                                        )
                                        { ResizeKeyboard = true, };

                                        await botClient.SendTextMessageAsync(
                                            chat.Id,
                                            "Вы вошли как сотрудник УВП.",
                                            replyMarkup: replyKeyboard
                                        );

                                        break;
                                    }

                                    if (message.Text == "Редактировать часто задаваемые вопросы:")
                                    {
                                        await botClient.SendTextMessageAsync(
                                            chat.Id,
                                            "7 или 8? 5 или 6?",
                                            replyToMessageId: message.MessageId
                                        );

                                        break;
                                    }

                                    break;
                                }

                                default:
                                {
                                    await botClient.SendTextMessageAsync(
                                        chat.Id,
                                        "Используй только текст!"
                                    );
                                    break;
                                }
                            }
                    }
                    break;
                    #endregion

                    #region callback_updates
                    case UpdateType.CallbackQuery: {

                            // Переменная, которая будет содержать в себе всю информацию о кнопке, которую нажали
                            CallbackQuery callbackQuery = update.CallbackQuery;

                            // Аналогично и с Message мы можем получить информацию о чате, о пользователе и т.д.
                            var user = callbackQuery.From;

                            // Выводим на экран нажатие кнопки
                            Console.WriteLine($"{user.FirstName} ({user.Id}) нажал на кнопку: {callbackQuery.Id}");

                            // Вот тут нужно уже быть немножко внимательным и не путаться!
                            // Мы пишем не callbackQuery.Chat, а callbackQuery.Message.Chat, так как
                            // кнопка привязана к сообщению, то мы берем информацию от сообщения.
                            var chat = callbackQuery.Message.Chat;

                            // Добавляем блок switch для проверки кнопок
                            switch (callbackQuery.Data) {
                                // Data - это придуманный нами id кнопки, мы его указывали в параметре
                                // callbackData при создании кнопок. У меня это button1, button2 и button3

                                case ("Bachelor"): {
                                        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                                        var magKeyboard = new ReplyKeyboardMarkup
                                        (
                                            new List<KeyboardButton[]>()
                                            {
                                                new KeyboardButton[]
                                                {
                                                    new KeyboardButton("Расписание экзаменов"),
                                                    new KeyboardButton("Контакты преподавателей"),
                                                },
                                                new KeyboardButton[]
                                                {
                                                    new KeyboardButton("Часто задаваемые вопросы"),
                                                    new KeyboardButton("Страница кафедры"),
                                                    new KeyboardButton("Назад"),
                                                },
                                            }
                                        )
                                        { ResizeKeyboard = true };

                                        await
                                            botClient.SendTextMessageAsync(
                                            chat.Id,
                                            $"Вы выбрали {callbackQuery.Data}",
                                            replyMarkup: magKeyboard,
                                            allowSendingWithoutReply: true
                                        );
                                    }
                                    break;

                                case ("Magistr"):
                                    {
                                        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                                            var magKeyboard = new ReplyKeyboardMarkup
                                            (
                                                new List<KeyboardButton[]>()
                                                {
                                                new KeyboardButton[]
                                                {
                                                    new KeyboardButton("Расписание экзаменов"),
                                                    new KeyboardButton("Контакты преподавателей"),
                                                },
                                                new KeyboardButton[]
                                                {
                                                    new KeyboardButton("Часто задаваемые вопросы"),
                                                    new KeyboardButton("Страница кафедры"),
                                                    new KeyboardButton("Назад"),
                                                },
                                                }
                                            )
                                            { ResizeKeyboard = true };

                                        await 
                                            botClient.SendTextMessageAsync(
                                            chat.Id,
                                            text: $"Вы выбрали {callbackQuery.Data}",
                                            replyMarkup: magKeyboard,
                                            allowSendingWithoutReply: true
                                        );
                                    }
                                    break;
                            }
                            break;
                    }
                    #endregion
                }
            }
            // huy
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
        {
            // Тут создадим переменную, в которую поместим код ошибки и её сообщение 
            var ErrorMessage = error switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => error.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

        private static async Task InsertIntoLogs(long chat_id, int val)
        {
            if (sql.State != ConnectionState.Open)
            {
                sql.Open();
                NpgsqlCommand command = new NpgsqlCommand($"INSERT INTO logs (chat_id, val) VALUES ('{chat_id}', {val})");
                await command.ExecuteNonQueryAsync();
                sql.Close();
            }
        }

        private static string SelectFromLogs(long chat_id)
        {
            if (sql.State != ConnectionState.Open)
            {
                NpgsqlCommand select = new NpgsqlCommand($"SELECT val FROM logs WHERE chat_id = {chat_id}", sql);
                int rows_changed = select.ExecuteNonQuery();//Если запрос не возвращает таблицу
                NpgsqlDataReader reader = select.ExecuteReader();//Если запрос возвращает таблицу

                if (reader.HasRows)//Если пришли результаты
                {
                    while (reader.Read())//Пока есть записи
                    {
                        for (int i = 0; i < rows_changed; i++)
                        {
                            if (reader.GetFieldType(i).ToString() == "System.Int32")//Проверяем тип следующей записи
                            {
                                return reader.GetInt32(i).ToString();//Получаем запись и переводим её в строку
                            }
                        }
                    }
                }
            }

            return "";
            //hjn tr,ffffffk
        }
    }
}