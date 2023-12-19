using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Data;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.ReplyMarkups;
using Update = Telegram.Bot.Types.Update;
using Telegram.Bot.Types.InlineQueryResults;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using static System.Net.Mime.MediaTypeNames;
using static IronPython.Modules._ast;

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
                    UpdateType.CallbackQuery// Коллбеки
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
            connectionString = System.IO.File.ReadAllText(@"C:\_учеба\tg_bot_student_helper\\cnt_string.txt");
            sql = new NpgsqlConnection(connectionString);
            await sql.OpenAsync();

            Console.WriteLine($"Установлено соединение с БД { sql.Database }");

            await sql.CloseAsync();
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


        private static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            // Обязательно ставим блок try-catch, чтобы наш бот не "падал" в случае каких-либо ошибок
            try
            {
                // Сразу же ставим конструкцию switch, чтобы обрабатывать приходящие Update
                switch (update.Type)
                {
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
                            switch (message.Type)
                            {
                                // Тут понятно, текстовый тип
                                case MessageType.Text:
                                    {
                                        // тут обрабатываем команду /start, остальные аналогичн
                                        if (message.Text == "/start" || message.Text == "start" || message.Text == "Старт" || message.Text == "старт")
                                        {
                                            SetNewUser(user.Id, chat.Id);
                                            SetFlag(user.Id, 1);

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
                                                "Добро пожаловать, " + $"{message.From.FirstName} " + $"{message.From.LastName}" + "!\n\n" +
                                                "Я — бот-помощник, чтобы использовать мой функционал выбирете, кем вы являетесь:",
                                                replyMarkup: startKeyboard);

                                            break;
                                        }

                                        if (message.Text == "Назад")
                                        {
                                            if (GetStateInsertQuest(user.Id) == 1)
                                            {
                                                SetStateInsertQuest(user.Id, 0);
                                                break;
                                            }
                                            int flag = GetFlag(user.Id);
                                            switch (flag)
                                            {
                                                case (1):
                                                    {
                                                        Console.WriteLine($"Возврат пользователя назад с флага {flag}");
                                                        SetFlag(user.Id, 0);
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
                                                        "До новых встреч!\n\nДля начала работы вы всегда можете нажать «Старт».",
                                                        replyMarkup: replyKeyboard);

                                                        break;
                                                    }
                                                case (2):
                                                    {
                                                        Console.WriteLine($"Возврат пользователя назад с флага {flag}");
                                                        SetFlag(user.Id, 1);

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
                                                                new KeyboardButton("Назад"),
                                                            },
                                                         })
                                                        { ResizeKeyboard = true, };
                                                        startKeyboard.OneTimeKeyboard = true;

                                                        await botClient.SendTextMessageAsync(
                                                            chat.Id,
                                                            "Чтобы использовать мой функционал выбирете, кем вы являетесь:",
                                                            replyMarkup: startKeyboard);

                                                        break;
                                                    }
                                            }
                                            break;
                                        }

                                        if (message.Text == "Добавить свой вопрос" && !GetIsAdmin(user.Id))
                                        {
                                            SetStateInsertQuest(user.Id, 1);
                                            await botClient.SendTextMessageAsync(
                                                    chat.Id,
                                                    $"Введите свой вопрос и чуть позже сотруник обязательно на него ответит.\n\nКак только будет дан ответ на ваш вопрос, Вы получите уведомление.");

                                            break;
                                        }

                                        if (GetStateInsertQuest(user.Id) == 1 && !GetIsAdmin(user.Id))
                                        {
                                            SetStateInsertQuest(user.Id, 0);

                                            AddStudentQuestion(message.Text, user.Id);

                                            await botClient.SendTextMessageAsync(
                                                    chat.Id,
                                                    $"Ваш вопрос успешно отправлен!");
                                            Console.WriteLine($"Пользователь {user.Id} задал вопрос");
                                            break;
                                        }

                                        if (message.Text == "До свидания")
                                        {
                                            SetFlag(user.Id, 0);
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

                                        if (message.Text == "Студент ЗФО")
                                        {
                                            var studentKeyboard = new InlineKeyboardMarkup(
                                                new List<InlineKeyboardButton[]>()
                                                {
                                                    new InlineKeyboardButton[]
                                                    {
                                                        InlineKeyboardButton.WithCallbackData("Бакалавриат/Специалитет", "Bachelor"),
                                                        InlineKeyboardButton.WithCallbackData("Магистратура", "Magistr")
                                                    },
                                                });

                                            await BotClient.SendTextMessageAsync(
                                                message.Chat.Id,
                                                text: "Выбирете ступень образования:",
                                                allowSendingWithoutReply: true,
                                                replyMarkup: studentKeyboard);
                                        }

                                        if (message.Text == "Расписание экзаменов")
                                        {
                                            SetFlag(user.Id, 3);

                                            var schedualKeyboard = new InlineKeyboardMarkup(
                                               new List<InlineKeyboardButton[]>()
                                               {
                                                   new InlineKeyboardButton[]
                                                   {
                                                       InlineKeyboardButton.WithUrl("Расписание экзаменов", "https://cs.istu.ru/index.php?project=kaf&page=_process_schedule_extramural_")
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
                                            Console.WriteLine($"Пользователь ({user.Id}) запросил список преподавателей");

                                            List<string> faq = SelectAllTeachers();
                                            string messege_text = "Вот список преподавателей:\n\n";
                                            for (int i = 0; i < faq.Count - 1; i++)
                                                messege_text += (i + 1) + ") " + faq[i] + "\n";
                                            messege_text += faq.Count + ") " + faq[faq.Count - 1];

                                            var schedualKeyboard = new InlineKeyboardMarkup(
                                               new List<InlineKeyboardButton[]>()
                                               {
                                                   new InlineKeyboardButton[]
                                                   {
                                                       InlineKeyboardButton.WithCallbackData("1", " t1"),
                                                       InlineKeyboardButton.WithCallbackData("2", " t2"),
                                                       InlineKeyboardButton.WithCallbackData("3", " t3"),
                                                       InlineKeyboardButton.WithCallbackData("4", " t4")
                                                   },

                                                   new InlineKeyboardButton[]
                                                   {
                                                       InlineKeyboardButton.WithCallbackData("5", " t5"),
                                                       InlineKeyboardButton.WithCallbackData("6", " t6"),
                                                       InlineKeyboardButton.WithCallbackData("7", " t7"),
                                                       InlineKeyboardButton.WithCallbackData("8", " t8")
                                                   },

                                                   new InlineKeyboardButton[]
                                                   {
                                                       InlineKeyboardButton.WithCallbackData("9", " t9"),
                                                       InlineKeyboardButton.WithCallbackData("10", " t10"),
                                                       InlineKeyboardButton.WithCallbackData("11", " t11"),
                                                       InlineKeyboardButton.WithCallbackData("12", " t12")
                                                   },

                                                   new InlineKeyboardButton[]
                                                   {
                                                       InlineKeyboardButton.WithCallbackData("13", " t13"),
                                                       InlineKeyboardButton.WithCallbackData("14", " t14"),
                                                       InlineKeyboardButton.WithCallbackData("15", " t15"),
                                                       InlineKeyboardButton.WithCallbackData("16", " t16")
                                                   },

                                                   new InlineKeyboardButton[]
                                                   {
                                                       InlineKeyboardButton.WithCallbackData("17", "t17"),
                                                       InlineKeyboardButton.WithCallbackData("18", "t18"),
                                                       InlineKeyboardButton.WithCallbackData("19", "t19"),
                                                       InlineKeyboardButton.WithCallbackData("20", "t20")
                                                   },

                                                   new InlineKeyboardButton[]
                                                   {
                                                       InlineKeyboardButton.WithCallbackData("21", " t21"),
                                                       InlineKeyboardButton.WithCallbackData("22", " t22"),
                                                       InlineKeyboardButton.WithCallbackData("23", " t23"),
                                                       InlineKeyboardButton.WithCallbackData("24", " t24")
                                                   },

                                                   new InlineKeyboardButton[]
                                                   {
                                                       InlineKeyboardButton.WithCallbackData("25", " t25"),
                                                       InlineKeyboardButton.WithCallbackData("26", " t26")
                                                   },
                                               });
                                            
                                            await botClient.SendTextMessageAsync(
                                                chat.Id,    
                                                messege_text,
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

                                        if (message.Text == "Расписание экзаменов")
                                        {
                                            var siteKeyboard = new InlineKeyboardMarkup(
                                                new List<InlineKeyboardButton[]>()
                                                {
                                                    new InlineKeyboardButton[]
                                                    {
                                                        InlineKeyboardButton.WithUrl("Страница кафедры","https://cs.istu.ru/index.php?project=kaf&page=_process_schedule_extramural_"),
                                                    },
                                                });

                                            await _botClient.SendTextMessageAsync(
                                            chat.Id,
                                            "Нажмите на кнопку для просмотра расписания",
                                            replyMarkup: siteKeyboard);

                                            return;
                                        }

                                        if (message.Text == "Часто задаваемые вопросы")
                                        {
                                            Console.WriteLine($"Пользователь ({user.Id}) запросил список ЧЗВ");

                                            List<string> faq = SelectFAQ();
                                            string messege_text = "Вот список вопросов:\n\n";
                                            for (int i = 0; i < faq.Count - 1; i++)
                                                messege_text += (i+1) + ") " + faq[i] + ";\n";
                                            messege_text += faq.Count + ") " + faq[faq.Count - 1] + ".";

                                            var questionsKeyboard = new InlineKeyboardMarkup(
                                               new List<InlineKeyboardButton[]>()
                                               {
                                                   new InlineKeyboardButton[]
                                                   {
                                                       InlineKeyboardButton.WithCallbackData("1", " q1"),
                                                       InlineKeyboardButton.WithCallbackData("2", " q2"),
                                                       InlineKeyboardButton.WithCallbackData("3", " q3")
                                                   },

                                                   new InlineKeyboardButton[]
                                                   {
                                                       InlineKeyboardButton.WithCallbackData("4", " q4"),
                                                       InlineKeyboardButton.WithCallbackData("5", " q5"),
                                                       InlineKeyboardButton.WithCallbackData("6", " q6")
                                                   },

                                                   new InlineKeyboardButton[]
                                                   {
                                                       InlineKeyboardButton.WithCallbackData("7", " q7"),
                                                       InlineKeyboardButton.WithCallbackData("8", " q8"),
                                                       InlineKeyboardButton.WithCallbackData("9", " q9")
                                                   },

                                               });

                                            await botClient.SendTextMessageAsync(
                                                chat.Id,
                                                messege_text,
                                                replyToMessageId: message.MessageId,
                                                replyMarkup:questionsKeyboard);

                                            break;
                                        }

                                        if (message.Text == "Сотрудник УВП" && !GetIsAdmin(user.Id))
                                        {
                                            await botClient.SendTextMessageAsync(
                                                chat.Id,
                                                "Введите кодовое слово:"
                                            );

                                            break;
                                        }

                                        if (message.Text == "key-word" || (message.Text == "Сотрудник УВП" && GetIsAdmin(user.Id)))
                                        {
                                            SetIsAdmin(user.Id, true);
                                            SetFlag(user.Id, 2);
                                            Console.WriteLine($"{user.FirstName}" + $"{user.LastName}" + ", успешно вошёл как сотрудник УВП");
                                            var replyKeyboard = new ReplyKeyboardMarkup(
                                                new List<KeyboardButton[]>()
                                                {
                                                    new KeyboardButton[]
                                                    {
                                                        new KeyboardButton("Вопросы от студентов"),
                                                        new KeyboardButton("Страница кафедры"),
                                                    },
                                                    new KeyboardButton[]
                                                    {
                                                        new KeyboardButton("Назад")
                                                    }
                                                }
                                            )
                                            { ResizeKeyboard = true, };

                                            await botClient.SendTextMessageAsync(
                                                chat.Id,
                                                "Вы успешно вошли как сотрудник УВП!",
                                                replyMarkup: replyKeyboard
                                            );
                                        }

                                        if (GetStateInsertQuest(user.Id) == 1 && GetIsAdmin(user.Id))
                                        {
                                            SetNewQuest1(user.Id, message.Text);
                                            await botClient.SendTextMessageAsync(
                                                chat.Id,
                                                "Введите ответ на вопрос",
                                                replyToMessageId: message.MessageId);
                                            SetStateInsertQuest(user.Id, 2);
                                            break;
                                        }

                                        if (GetStateInsertQuest(user.Id) == 2 && GetIsAdmin(user.Id))
                                        {
                                            SetNewQuest2(user.Id, message.Text);
                                            SetStateInsertQuest(user.Id, 0);

                                            int q_id = int.Parse(GetNewQuest1(user.Id));
                                            string answer = GetNewQuest2(user.Id);

                                            sql.Open();
                                            NpgsqlCommand command = new NpgsqlCommand(
                                                    $"UPDATE questions SET answer = '{answer}' WHERE q_id = {q_id}",
                                                    sql);
                                            await command.ExecuteNonQueryAsync();
                                            command = new NpgsqlCommand(
                                                    $"UPDATE questions SET status = 'Решен' WHERE q_id = {q_id}",
                                                    sql);
                                            await command.ExecuteNonQueryAsync();
                                            sql.Close();

                                            await botClient.SendTextMessageAsync(
                                                chat.Id,
                                                "Ответ успешно добавлен",
                                                replyToMessageId: message.MessageId);

                                            long author = 0;
                                            if (sql.State == ConnectionState.Closed)
                                            {
                                                sql.Open();
                                                NpgsqlCommand select = new NpgsqlCommand($"SELECT author FROM questions WHERE q_id = {q_id}", sql);
                                                NpgsqlDataReader reader = select.ExecuteReader();//Если запрос возвращает таблицу

                                                reader.Read();
                                                author = Convert.ToInt64(reader[0]);

                                                reader.Close();
                                                sql.Close();
                                            }

                                            string q_text = "";
                                            if (sql.State == ConnectionState.Closed)
                                            {
                                                sql.Open();
                                                NpgsqlCommand select = new NpgsqlCommand($"SELECT text FROM questions WHERE q_id = {q_id}", sql);
                                                NpgsqlDataReader reader = select.ExecuteReader();//Если запрос возвращает таблицу

                                                reader.Read();
                                                q_text = reader.GetString(0);

                                                reader.Close();
                                                sql.Close();
                                            }

                                            await botClient.SendTextMessageAsync(
                                                author,
                                                $"На ваш вопрос ответили!\n\nВопрос: {q_text}\n\nОтвет: {answer}"
                                                );

                                            break;
                                        }

                                        if (message.Text == "Вопросы от студентов" && GetIsAdmin(user.Id))
                                        {
                                            sql.Open();
                                            NpgsqlCommand command = new NpgsqlCommand(
                                                    $"SELECT q_id, text FROM questions WHERE status = 'Не решен'",
                                                    sql);
                                            NpgsqlDataReader dr = await command.ExecuteReaderAsync();

                                            string studQuest = "";
                                            while (dr.Read())
                                                studQuest += $"ID = {dr["q_id"]}: {dr["text"]}\n";
                                            studQuest = studQuest.TrimEnd('\n');

                                            var adminKeyboard = new InlineKeyboardMarkup(
                                                new List<InlineKeyboardButton[]>()
                                                {
                                                    new InlineKeyboardButton[]
                                                    {
                                                        InlineKeyboardButton.WithCallbackData("Ответить на вопрос", "Answer")
                                                    },
                                                });

                                            await botClient.SendTextMessageAsync(
                                                chat.Id,
                                                text: studQuest,
                                                replyMarkup: adminKeyboard,
                                                replyToMessageId: message.MessageId
                                                );
                                            sql.Close();

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
                    case UpdateType.CallbackQuery:
                        {
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

                            int q_id = 0;
                            if (" q1 q2 q3 q4 q5 q6 q7 q8 q9".Contains(callbackQuery.Data))
                            {
                                q_id = Convert.ToInt16(callbackQuery.Data.TrimStart(' ', 'q'));
                                callbackQuery.Data = " q1 q2 q3 q4 q5 q6 q7 q8 q9";
                            }
                            
                            int t_id = 0;
                            if (" t1 t2 t3 t4 t5 t6 t7 t8 t9 t10 t11 t12 t13 t14 t15 t16 t17 t19 t20 t21 t22 t23 t24 t25 t26".Contains(callbackQuery.Data))
                            {
                                t_id = Convert.ToInt16(callbackQuery.Data.TrimStart(' ', 't'));
                                callbackQuery.Data = " t1 t2 t3 t4 t5 t6 t7 t8 t9 t10 t11 t12 t13 t14 t15 t16 t17 t19 t20 t21 t22 t23 t24 t25 t26";
                            }

                            // Добавляем блок switch для проверки кнопок
                            switch (callbackQuery.Data)
                            {
                                // Data - это придуманный нами id кнопки, мы его указывали в параметре
                                // callbackData при создании кнопок. У меня это button1, button2 и button3


                                case ("Bachelor"):
                                    SetFlag(user.Id, 2);

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
                                                    new KeyboardButton("Добавить свой вопрос"),
                                                },
                                                new KeyboardButton[]
                                                {
                                                    new KeyboardButton("Страница кафедры"),
                                                },
                                                new KeyboardButton[]
                                                {
                                                    new KeyboardButton("Назад"),
                                                },
                                        }
                                    )

                                    { ResizeKeyboard = true };

                                    await
                                        botClient.SendTextMessageAsync(
                                        chat.Id,
                                        $"Вы выбрали Бакалавриат/Специалитет",
                                        replyMarkup: bachelorKeyboard,
                                        allowSendingWithoutReply: true);

                                    break;

                                case ("Magistr"):
                                    SetFlag(user.Id, 2);

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
                                                    new KeyboardButton("Добавить свой вопрос"),
                                                },
                                                new KeyboardButton[]
                                                {
                                                    new KeyboardButton("Страница кафедры"),
                                                },
                                                new KeyboardButton[]
                                                {
                                                    new KeyboardButton("Назад"),
                                                },
                                        })

                                    { ResizeKeyboard = true };

                                    await
                                        botClient.SendTextMessageAsync(
                                        chat.Id,
                                        text: $"Вы выбрали Магистратуру",
                                        replyMarkup: magKeyboard,
                                        allowSendingWithoutReply: true);
                                    break;

                                case (" q1 q2 q3 q4 q5 q6 q7 q8 q9"):
                                    {
                                        string Text1 = SelectQuestion(q_id);
                                        await botClient.SendTextMessageAsync(
                                            chat.Id,
                                            text: "Ответ: " + $"{Text1.TrimEnd('\n')}");
                                    }
                                    break;

                                case (" t1 t2 t3 t4 t5 t6 t7 t8 t9 t10 t11 t12 t13 t14 t15 t16 t17 t19 t20 t21 t22 t23 t24 t25 t26"):
                                    {
                                        sql.Open();
                                        string commandText = $"SELECT * FROM teachers WHERE id = {t_id}";
                                        NpgsqlCommand command = new NpgsqlCommand(commandText, sql);
                                        NpgsqlDataReader dr = command.ExecuteReader();

                                        string Text1 = "";
                                        string Text2 = "";
                                        string Text3 = "";
                                        while (dr.Read())
                                        {
                                            Text1 = dr["fullname"].ToString();
                                            Text2 = dr["contacts"].ToString();
                                            Text3 = dr["consultations"].ToString();
                                        }

                                        if (Text2.Length == 0)
                                            Text2 = "не имеет доступной контактной информации.";

                                        sql.Close();

                                        string messege = $"{Text1}";
                                        if (Text2.Length == 0)
                                        {
                                            messege += "\n\nУ данного преподавателя нет контактной информации.";
                                        } else
                                        {
                                            messege += $"\n\nКонтактная информация:\n{Text2.TrimEnd('\n')}.";
                                        }
                                        if (Text3.Length == 0)
                                        {
                                            messege += "\n\nУ данного преподавателя нет консультаций.";
                                        } else
                                        {
                                            messege += $"\n\nРасписание консультаций:\n{Text3.TrimEnd('\n')}.";
                                        }
                                        await botClient.SendTextMessageAsync(
                                            chat.Id,
                                            text: messege);
                                    }
                                    break;

                                case ("Answer"):
                                    {
                                        SetStateInsertQuest(user.Id, 1);

                                        await
                                        botClient.SendTextMessageAsync(
                                        chat.Id,
                                        text: $"Введите ID вопроса",
                                        allowSendingWithoutReply: true);
                                    }
                                    break;
                            }
                        #endregion
                        break;
                        }
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
        
        #region psql

        private static async Task AddStudentQuestion(string text, long chat_id)
        {
            if (sql.State != ConnectionState.Open)
            {
                sql.Open();
                NpgsqlCommand command = new NpgsqlCommand(
                    $"INSERT INTO questions (text, author) VALUES ('{text}', '{chat_id}')",
                    sql);
                await command.ExecuteNonQueryAsync();
                sql.Close();
            }
        }

        private static List<string> SelectFAQ()
        {
            List<string> resultAsStringList = new List<string>();
        
            if (sql.State != ConnectionState.Open)
            {
                sql.Open();
                NpgsqlCommand select = new NpgsqlCommand($"SELECT text FROM questions WHERE status = 'FAQ'", sql);
                NpgsqlDataReader reader = select.ExecuteReader();
        
                if (reader.HasRows)//Если пришли результаты
                {
                    int i = 0;
                    while (reader.Read())//Пока есть записи
                    {
                            resultAsStringList.Insert(i++, reader.GetString(0));
                    }
                }
                sql.Close();
            }
        
            return resultAsStringList;
        }
        
        private static string SelectQuestion(int id)
        {
            string resultAsString = "";
        
            if (sql.State != ConnectionState.Open)
            {
                sql.Open();
                NpgsqlCommand select = new NpgsqlCommand($"SELECT answer FROM questions WHERE id = { id }", sql);
                //int rows_changed = await select.ExecuteNonQueryAsync();//Если запрос не возвращает таблицу
                NpgsqlDataReader reader = select.ExecuteReader();//Если запрос возвращает таблицу
        
                reader.Read();
                resultAsString = reader.GetInt32(0).ToString();
        
                reader.Close();
                sql.Close();
            }
        
            return resultAsString;
        }

        private static List<string> SelectAllTeachers()
        {
            //NpgsqlCommand select = new NpgsqlCommand($"SELECT fullname FROM teachers WHERE id = {teacher_id.GetHashCode()}", sql);
            List<string> resultAsStringList = new List<string>();

            if (sql.State != ConnectionState.Open)
            {
                sql.Open();
                NpgsqlCommand select = new NpgsqlCommand($"SELECT fullname FROM teachers", sql);
                NpgsqlDataReader reader = select.ExecuteReader();

                if (reader.HasRows)//Если пришли результаты
                {
                    int i = 0;
                    while (reader.Read())//Пока есть записи
                    {
                        resultAsStringList.Insert(i++, reader.GetString(0));
                    }
                }
                sql.Close();
            }

            return resultAsStringList;
        }

        private static async Task SelectFromTeachersById(int teacher_id)
        {
            if (sql.State != ConnectionState.Open)
            {
                sql.Open();
                NpgsqlCommand select = new NpgsqlCommand($"SELECT fullname FROM teachers WHERE id = {teacher_id.GetHashCode()}", sql);
                //int rows_changed = await select.ExecuteNonQueryAsync();//Если запрос не возвращает таблицу
                NpgsqlDataReader reader = select.ExecuteReader();//Если запрос возвращает таблицу

                await reader.ReadAsync();
                var teacher_fullname = reader.GetString(0);

                reader.Close();
                sql.Close();
            }
        }



        /// <summary>
        /// Set or alter new user
        /// </summary>
        /// <param name="user_id"></param>
        public static void SetNewUser(long user_id, double chat_id)
        {
            if (sql.State == ConnectionState.Closed)
            {
                sql.Open();
                NpgsqlCommand command = new NpgsqlCommand(
                    $"DELETE FROM users WHERE id = { user_id.GetHashCode() }",
                    sql);
                command.ExecuteNonQuery();

                command = new NpgsqlCommand(
                    $"INSERT INTO users (id, chat_id) VALUES ({ user_id.GetHashCode() }, {chat_id})",
                    sql);
                command.ExecuteNonQuery();
                sql.Close();
            }
        }



        /// <summary>
        /// Alter admin privelegues
        /// </summary>
        /// <param name="user_id"></param>
        /// <param name="is_admin"></param>
        public static void SetIsAdmin(long user_id, bool is_admin)
        {
            if (sql.State == ConnectionState.Closed)
            {
                sql.Open();
                NpgsqlCommand command = new NpgsqlCommand(
                    $"UPDATE users SET is_admin = { is_admin } WHERE id = { user_id.GetHashCode() }",
                    sql);
                command.ExecuteNonQuery();
                sql.Close();
            }
        }
        /// <summary>
        /// Check if user us admin
        /// </summary>
        /// <param name="user_id"></param>
        /// <returns></returns>
        public static bool GetIsAdmin(long user_id)
        {
            bool is_admin = false;
            if (sql.State == ConnectionState.Closed)
            {
                sql.Open();
                NpgsqlCommand select = new NpgsqlCommand($"SELECT is_admin FROM users WHERE id = { user_id.GetHashCode() }", sql);
                NpgsqlDataReader reader = select.ExecuteReader();//Если запрос возвращает таблицу

                reader.Read();
                is_admin = Convert.ToBoolean(reader[0]);

                reader.Close();
                sql.Close();
            }
            return is_admin;
        }



        /// <summary>
        /// Sets check param
        /// </summary>
        /// <param name="user_id"></param>
        /// <param name="check"></param>
        public static void SetCheck(long user_id, int check)
        {
            if (sql.State == ConnectionState.Closed)
            {
                sql.Open();
                NpgsqlCommand command = new NpgsqlCommand(
                    $"UPDATE users SET do_check = {check} WHERE id = {user_id.GetHashCode()}",
                    sql);
                command.ExecuteNonQuery();
                sql.Close();
            }
        }
        /// <summary>
        /// Gets check param
        /// </summary>
        /// <param name="user_id"></param>
        /// <returns></returns>
        public static int GetCheck(long user_id)
        {
            int check = 0;
            if (sql.State == ConnectionState.Closed)
            {
                sql.Open();
                NpgsqlCommand select = new NpgsqlCommand($"SELECT do_check FROM users WHERE id = {user_id.GetHashCode()}", sql);
                NpgsqlDataReader reader = select.ExecuteReader();//Если запрос возвращает таблицу

                reader.Read();
                check = Convert.ToInt16(reader[0]);

                reader.Close();
                sql.Close();
            }
            return check;
        }



        /// <summary>
        /// Sets flag param
        /// </summary>
        /// <param name="user_id"></param>
        /// <param name="check"></param>
        public static void SetFlag(long user_id, int flag)
        {
            if (sql.State == ConnectionState.Closed)
            {
                sql.Open();
                NpgsqlCommand command = new NpgsqlCommand(
                    $"UPDATE users SET do_flag = {flag} WHERE id = {user_id.GetHashCode()}",
                    sql);
                command.ExecuteNonQuery();
                sql.Close();
            }
        }
        /// <summary>
        /// Gets flag param
        /// </summary>
        /// <param name="user_id"></param>
        /// <returns></returns>
        public static int GetFlag(long user_id)
        {
            int flag = 0;
            if (sql.State == ConnectionState.Closed)
            {
                sql.Open();
                NpgsqlCommand select = new NpgsqlCommand($"SELECT do_flag FROM users WHERE id = {user_id.GetHashCode()}", sql);
                NpgsqlDataReader reader = select.ExecuteReader();//Если запрос возвращает таблицу

                reader.Read();
                flag = Convert.ToInt16(reader[0]);

                reader.Close();
                sql.Close();
            }
            return flag;
        }



        /// <summary>
        /// Sets stateDeleteQuest param
        /// </summary>
        /// <param name="user_id"></param>
        /// <param name="check"></param>
        public static void SetStateDeleteQuest(long user_id, bool stateDeleteQuest)
        {
            if (sql.State == ConnectionState.Closed)
            {
                sql.Open();
                NpgsqlCommand command = new NpgsqlCommand(
                    $"UPDATE users SET state_delete_quest = {stateDeleteQuest} WHERE id = {user_id.GetHashCode()}",
                    sql);
                command.ExecuteNonQuery();
                sql.Close();
            }
        }
        /// <summary>
        /// Gets stateDeleteQuest param
        /// </summary>
        /// <param name="user_id"></param>
        /// <returns></returns>
        public static bool GetStateDeleteQuest(long user_id)
        {
            bool stateDeleteQuest = false;
            if (sql.State == ConnectionState.Closed)
            {
                sql.Open();
                NpgsqlCommand select = new NpgsqlCommand($"SELECT state_delete_quest FROM users WHERE id = {user_id.GetHashCode()}", sql);
                NpgsqlDataReader reader = select.ExecuteReader();//Если запрос возвращает таблицу

                reader.Read();
                stateDeleteQuest = Convert.ToBoolean(reader[0]);

                reader.Close();
                sql.Close();
            }
            return stateDeleteQuest;
        }



        /// <summary>
        /// Sets stateInsertQuest param
        /// </summary>
        /// <param name="user_id"></param>
        /// <param name="check"></param>
        public static void SetStateInsertQuest(long user_id, int stateInsertQuest)
        {
            if (sql.State == ConnectionState.Closed)
            {
                sql.Open();
                NpgsqlCommand command = new NpgsqlCommand(
                    $"UPDATE users SET state_insert_quest = {stateInsertQuest} WHERE id = {user_id.GetHashCode()}",
                    sql);
                command.ExecuteNonQuery();
                sql.Close();
            }
        }
        /// <summary>
        /// Gets stateInsertQuest param
        /// </summary>
        /// <param name="user_id"></param>
        /// <returns></returns>
        public static int GetStateInsertQuest(long user_id)
        {
            int stateInsertQues = 0;
            if (sql.State == ConnectionState.Closed)
            {
                sql.Open();
                NpgsqlCommand select = new NpgsqlCommand($"SELECT state_insert_quest FROM users WHERE id = {user_id.GetHashCode()}", sql);
                NpgsqlDataReader reader = select.ExecuteReader();//Если запрос возвращает таблицу

                reader.Read();
                stateInsertQues = Convert.ToInt16(reader[0]);

                reader.Close();
                sql.Close();
            }
            return stateInsertQues;
        }



        /// <summary>
        /// Sets newQuest1 param
        /// </summary>
        /// <param name="user_id"></param>
        /// <param name="check"></param>
        public static void SetNewQuest1(long user_id, string newQuest1)
        {
            if (sql.State == ConnectionState.Closed)
            {
                sql.Open();
                NpgsqlCommand command = new NpgsqlCommand(
                    $"UPDATE users SET new_quest_f = '{newQuest1}' WHERE id = {user_id.GetHashCode()}",
                    sql);
                command.ExecuteNonQuery();
                sql.Close();
            }
        }
        /// <summary>
        /// Gets newQuest1 param
        /// </summary>
        /// <param name="user_id"></param>
        /// <returns></returns>
        public static string GetNewQuest1(long user_id)
        {
            string newQuest1 = "";
            if (sql.State == ConnectionState.Closed)
            {
                sql.Open();
                NpgsqlCommand select = new NpgsqlCommand($"SELECT new_quest_f FROM users WHERE id = {user_id.GetHashCode()}", sql);
                NpgsqlDataReader reader = select.ExecuteReader();//Если запрос возвращает таблицу

                reader.Read();
                newQuest1 = reader[0].ToString();

                reader.Close();
                sql.Close();
            }
            return newQuest1;
        }



        /// <summary>
        /// Sets newQuest2 param
        /// </summary>
        /// <param name="user_id"></param>
        /// <param name="check"></param>
        public static void SetNewQuest2(long user_id, string newQuest2)
        {
            if (sql.State == ConnectionState.Closed)
            {
                sql.Open();
                NpgsqlCommand command = new NpgsqlCommand(
                    $"UPDATE users SET new_quest_s = '{newQuest2}' WHERE id = {user_id.GetHashCode()}",
                    sql);
                command.ExecuteNonQuery();
                sql.Close();
            }
        }
        /// <summary>
        /// Gets newQuest2 param
        /// </summary>
        /// <param name="user_id"></param>
        /// <returns></returns>
        public static string GetNewQuest2(long user_id)
        {
            string newQuest2 = "";
            if (sql.State == ConnectionState.Closed)
            {
                sql.Open();
                NpgsqlCommand select = new NpgsqlCommand($"SELECT new_quest_s FROM users WHERE id = {user_id.GetHashCode()}", sql);
                NpgsqlDataReader reader = select.ExecuteReader();//Если запрос возвращает таблицу

                reader.Read();
                newQuest2 = reader[0].ToString();

                reader.Close();
                sql.Close();
            }
            return newQuest2;
        }

        #endregion
    }
}
