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

        //private static async Auntification()

        private static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken){
            var flag = 0;
            var check = 0;
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
                                        // тут обрабатываем команду /start, остальные аналогичн
                                        if (message.Text == "/start")
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
                                                "Добро пожаловать, " + $"{message.From.FirstName}" + $"{message.From.LastName}"+"!\n" +
                                                "Я  - бот-помощник, чтобы использовать мой функционал выбирете, кем вы являетесь:",
                                                replyMarkup: startKeyboard);

                                            break;

                                        }
                                        if (message.Text == "Войти")
                                        {
                                            if (check == 1)
                                            {
                                                flag = 1;

                                                //Сюда нужно добавить проверку данных, ввелённых пользователем с данными из БД
                                                //...
                                                await botClient.SendTextMessageAsync(
                                                    chat.Id,
                                                    $"{user.FirstName}"+$"{user.LastName}"+", вы успешно вошли как студент ЗФО!");
                                                break;
                                            }

                                            else if(check == 2)
                                            {
                                                //Сюда нужно добавить проверку данных, введённых пользователем с данными в БД
                                                //...
                                                flag = 1;
                                                await botClient.SendTextMessageAsync(
                                                    chat.Id,
                                                    $"{user.FirstName}"+$"{user.LastName}"+", вы успешно вошли как сотрудник УВП!");
                                                break;
                                            }
                                        }

                                        if (message.Text == "До свидания")
                                        {
                                            flag = 0;
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

                                            if (flag == 1)
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

                                            flag -= 1;

                                        }

                                        if (message.Text == ($"{user.FirstName}" + $"{user.LastName}" + ", вы успешно вошли как студент ЗФО!"))
                                        {
                                            check = 1;
                                            flag = 1;
                                            var studentKeyboard = new InlineKeyboardMarkup(
                                                new List<InlineKeyboardButton[]>()
                                                {
                                                    new InlineKeyboardButton[]
                                                    {
                                                        InlineKeyboardButton.WithCallbackData("Бакалавриат/Специалитет", "Bachelor"),
                                                        InlineKeyboardButton.WithCallbackData("Магистратура", "Magistr")
                                                    },
                                                });

                                            await botClient.SendTextMessageAsync(
                                                chat.Id,
                                                text: "Выбирете ступень образования:",
                                                allowSendingWithoutReply: true,
                                                replyMarkup: studentKeyboard);

                                            break;
                                        }

                                        if (message.Text == "Расписание экзаменов")
                                        {
                                            flag = 3;
                                            var schedualKeyboard = new InlineKeyboardMarkup(
                                               new List<InlineKeyboardButton[]>()
                                               {
                                                   new InlineKeyboardButton[]
                                                   {
                                                       InlineKeyboardButton.WithCallbackData("Расписание экзаменов", " ekz")
                                                   },
                                               });

                                            await botClient.SendTextMessageAsync(
                                                chat.Id,
                                                "Для просмотра расписания нажмите кнопку ниже",
                                                replyMarkup: schedualKeyboard);

                                            break;
                                        }

                                        if (message.Text == "Контакты преподавателей")
                                        {
                                            flag = 3;
                                            var schedualKeyboard = new InlineKeyboardMarkup(
                                               new List<InlineKeyboardButton[]>()
                                               {
                                                   new InlineKeyboardButton[]
                                                   {
                                                       InlineKeyboardButton.WithCallbackData("1", " 1"),
                                                       InlineKeyboardButton.WithCallbackData("2", " 2"),
                                                       InlineKeyboardButton.WithCallbackData("3", " 3")
                                                   },

                                                   new InlineKeyboardButton[]
                                                   {
                                                       InlineKeyboardButton.WithCallbackData("4", " 4"),
                                                       InlineKeyboardButton.WithCallbackData("5", " 5"),
                                                       InlineKeyboardButton.WithCallbackData("6", " 6")
                                                   },

                                                   new InlineKeyboardButton[]
                                                   {
                                                       InlineKeyboardButton.WithCallbackData("7", " 7"),
                                                       InlineKeyboardButton.WithCallbackData("8", " 8"),
                                                       InlineKeyboardButton.WithCallbackData("9", " 9")
                                                   },

                                                   new InlineKeyboardButton[]
                                                   {
                                                       InlineKeyboardButton.WithCallbackData("10", " 10"),
                                                       InlineKeyboardButton.WithCallbackData("11", " 11"),
                                                       InlineKeyboardButton.WithCallbackData("12", " 12")
                                                   },

                                                   new InlineKeyboardButton[]
                                                   {
                                                       InlineKeyboardButton.WithCallbackData("13", " 13"),
                                                       InlineKeyboardButton.WithCallbackData("14", " 14"),
                                                       InlineKeyboardButton.WithCallbackData("15", " 15")
                                                   },

                                                     new InlineKeyboardButton[]
                                                   {
                                                       InlineKeyboardButton.WithCallbackData("16", " 16"),
                                                       InlineKeyboardButton.WithCallbackData("17", " 17"),
                                                       InlineKeyboardButton.WithCallbackData("18", " 18")
                                                   },

                                                       new InlineKeyboardButton[]
                                                   {
                                                       InlineKeyboardButton.WithCallbackData("19", " 19")
                                                   },
                                               });

                                            await botClient.SendTextMessageAsync(
                                                chat.Id,
                                                "Вот список преподавателей:"+
                                                "1)Леонов Михаил Витальевич"+
                                                "2)Аль аккад Мхд айман" +
                                                "3)Архипов Игорь Олегович" +
                                                "4)Брычкина Мария Сергеевна" +
                                                "5)Власов Вадим Геннадьевич" +
                                                "6)Еланцев Михаил Олегович" +
                                                "7)Зылева Елена Анатольевна" +
                                                "8)Коробейников Александр Васильевич" +
                                                "9)Левицкая Людмила Николаевна" +
                                                "10)Лугачев Павел Петрович" +
                                                "11)Макарова Ольга Леонидовна" +
                                                "12)Постникова Елена Николаевна" +
                                                "13)Русских Анатолий Геннадьевич" +
                                                "14)Соболева Валентина Павловна" +
                                                "15)Старыгин Артем Викторович" +
                                                "16)Тарасов Владимир Георгиевич" +
                                                "17)Чернышев Константин Сергеевич" +
                                                "18)Шаталова Ольга Михайловна" +
                                                "19)Шишлина Наталья Васильевна",
                                                replyMarkup: schedualKeyboard);

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
                                                });

                                            await _botClient.SendTextMessageAsync(
                                            chat.Id,
                                            "Нажмите на кнопку для перехода на страницу кафедры",
                                            replyMarkup: siteKeyboard);

                                            return;
                                        }

                                        if (message.Text == "Вот список вопросов:")
                                        {
                                            flag = 3;
                                            await botClient.SendTextMessageAsync(
                                                chat.Id,
                                                "Вот список вопросов:" +
                                                "1)Можно ли дистанционно закрывать сессии ? " +
                                                "2)Где получить справку об обучении в вузе ? " +
                                                "3)Что нужно сделать, чтобы обновили информацию в личном кабинете?" +
                                                "4)Где можно посмотреть даты сессий и расписание занятий?" +
                                                "5)Каким образом можно оформить пропуск / допуск для прохода в корпусы ИжГТУ ? " +
                                                "6)Где и как получить студенческий билет?" +
                                                "7)Какие сроки по сдаче нормоконтроля, реферата, учётной карточки, проверки на заимствование?" +
                                                "8)Как выбрать научного руководителя для написания ВКР ? " +
                                                "9)Как и где можно заказать справку-вызов ? ",
                                                replyToMessageId: message.MessageId);

                                            break;
                                        }



                                        if (message.Text == ($"{user.FirstName}" + $"{user.LastName}" + ", вы успешно вошли как сотрудник УВП!"))
                                        {
                                            check = 2;
                                            flag = 3;
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

                                    if (message.Text == "Вот список вопросов:")
                                    {
                                        flag = 4;
                                        await botClient.SendTextMessageAsync(
                                            chat.Id,
                                            "Вот список вопросов:"+
                                            "1)Можно ли дистанционно закрывать сессии ? " +
                                            "2)Где получить справку об обучении в вузе ? " +
                                            "3)Что нужно сделать, чтобы обновили информацию в личном кабинете?" +
                                            "4)Где можно посмотреть даты сессий и расписание занятий?" +
                                            "5)Каким образом можно оформить пропуск / допуск для прохода в корпусы ИжГТУ ? " +
                                            "6)Где и как получить студенческий билет?" +
                                            "7)Какие сроки по сдаче нормоконтроля, реферата, учётной карточки, проверки на заимствование?" +
                                            "8)Как выбрать научного руководителя для написания ВКР ? " +
                                            "9)Как и где можно заказать справку-вызов ? ",
                                            replyToMessageId: message.MessageId);

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
                            switch (callbackQuery.Data)
                            {
                                // Data - это придуманный нами id кнопки, мы его указывали в параметре
                                // callbackData при создании кнопок. У меня это button1, button2 и button3


                                case ("Bachelor"): 

                                        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                                        var bachelorKeyboard = new ReplyKeyboardMarkup
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
                                            replyMarkup: bachelorKeyboard,
                                            allowSendingWithoutReply: true);
                                   
                                    break;

                                case ("Magistr"):
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
                                                })

                                            { ResizeKeyboard = true };

                                        await 
                                            botClient.SendTextMessageAsync(
                                            chat.Id,
                                            text: $"Вы выбрали {callbackQuery.Data}",
                                            replyMarkup: magKeyboard,
                                            allowSendingWithoutReply: true); 
                                    break;

                                case ("1"):
                                    {

                                    }
                                    break;
                            }
                    }
                        break;
                    #endregion
                }
            }
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
            
        }
    }
}