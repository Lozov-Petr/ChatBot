using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Net.Http;

using VkNet;
using VkNet.Model;
using VkNet.Model.RequestParams;
using VkNet.Enums.Filters;

namespace ChatBot

{
    static class Program
    {
        const string _firstGoodMessage = "БОТ:\nДень добрый! Вам пора выбрать время для встречи!";
        const string _refirstMessage   = "БОТ:\nДень добрый! Вы по-прежнему не встретились в этом месяце. :(\nВыберите время для встречи!";
        const string _firstBadMessage  = "БОТ:\nДень добрый! Вы так и не погуляли в прошлом месяце! :'(\nВам пора выбрать время для встречи!";

        static string[] _restMessagesSingular =
            {
                "БОТ:\n{0}, ты по-прежнему не участвуешь в коммуникации!",
                "БОТ:\n{0}, уже два часа прошло, а ты всё не обсуждаешь вашу будущую прогулку!",
                "БОТ:\n{0}, может, ты обратишь внимание на этот чат?..",
                "БОТ:\n{0}, ты заставляешь друзей ждать!..",
                "БОТ:\n{0}, подай признаки жизнедеятельности!",
                "БОТ:\n{0}, ты что, не хочешь гулять?",
                "БОТ:\n{0}, я сдаюсь... Я так и не смог привлечь твое внимание... Напишу в этот чат на следующей неделе."
            };

        static string[] _restMessagesPlural =
        {
                "БОТ:\n{0}, вы по-прежнему не участвуете в коммуникации!",
                "БОТ:\n{0}, уже два часа прошло, а вы всё не обсуждаете вашу будущую прогулку!",
                "БОТ:\n{0}, может, вы обратите внимание на этот чат?..",
                "БОТ:\n{0}, вы заставляете друга ждать!..",
                "БОТ:\n{0}, подайте признаки жизнедеятельности!",
                "БОТ:\n{0}, вы что, не хотите гулять?",
                "БОТ:\n{0}, я сдаюсь... Я так и не смог привлечь ваше внимание... Напишу в этот чат на следующей неделе."
            };

        const string _goodMessage  = "БОТ:\nВы начали коммуницировать.\nМоя задача выполнена!";

        const string _solutionFile = "PeterSolution.txt";

        const bool isTest = false;

        const long _chatID = isTest ? 80 : 62;
        static int _maxRepeats = _restMessagesSingular.Length;

        const int fiveMinutes = isTest ? 1000 : 1000 * 60 * 5;
        const int oneHour = isTest ? 1000 : fiveMinutes * 12;

        static string _accessToken;
        static VkApi _api;
        static List<User> _users;

        static Logger _logger;

        static long ChatID
        {
            get
            {
                return _chatID + 2000000000;
            }
        }

        static void Main(string[] args)
        {
            _logger = new Logger();

            try
            {
                BotMain(isTest);
            }
            catch (Exception e)
            {
                _logger.WriteInLog(string.Format("Исключение во время исполнения:\n{0}", e));
            }
        }

        static void BotMain(bool isTest)
        {
            var botMessages = new List<long>();

            _logger.WriteInLog(string.Format("Начало работы для чата #{0}.", _chatID));

            var isFirstWeek = DateTime.Now.Day <= 7;

            var wasWalkBeforeReCreate = Reader.ReadFrom(_solutionFile);

            if (!isTest && isFirstWeek)
            {
                Reader.ReCreate(_solutionFile);
                _logger.WriteInLog("Обновление информации о прогулке в начале месяца.");
            }

            var wasWalk = Reader.ReadFrom(_solutionFile);

            if (!isTest && wasWalk)
            {
                _logger.WriteInLog("Прогулка уже состоялась. Завершение работы бота.");
                return;
            }

            _logger.WriteInLog("Оповещение необходимо.");

            _accessToken = System.Configuration.ConfigurationManager.AppSettings["access_token"];

            if (string.IsNullOrEmpty(_accessToken))
            {
                _logger.WriteInLog("Access Token не обнаружен в конфигурационном файле. Дальнейцая работа невозможна.");
                return;
            }

            _api = new VkApi();
            _api.Authorize(new ApiAuthParams { AccessToken = _accessToken });
            initUsers();

            var firstText = isFirstWeek ? (wasWalkBeforeReCreate ? _firstGoodMessage : _firstBadMessage) : _refirstMessage;
            var botMessageID = sendMessage(firstText);
            botMessages.Add(botMessageID);

            for (int i = 0; i < _maxRepeats; ++i)
            {
                Thread.Sleep(oneHour);

                var authors = getUsersAfterBot(botMessages);

                if (authors.Count() < _users.Count)
                {
                    _logger.WriteInLog("Не все пользователи написали в чате.");
                    var msgTemplate = _users.Count - authors.Count() == 1 ? _restMessagesSingular[i] : _restMessagesPlural[i];
                    var appeal = createAppeal(authors);
                    var id = sendMessage(string.Format(msgTemplate, appeal));
                    botMessages.Add(id);
                }
                else
                {
                    _logger.WriteInLog("Каждый пользователь написал в чате.");
                    sendMessage(_goodMessage);
                    return;
                }
            }
        }

        static void initUsers()
        {
            Func<IEnumerable<User>> get = () =>
              _api.Messages.GetChatUsers(new long[] { _chatID }, UsersFields.Domain, null);

            _logger.WriteInLog(string.Format("Получение информации о чате #{0}.", _chatID));
            _users = perform(get, "Неудача при получении информации о чате.").ToList();
            _logger.WriteInLog(string.Format("В чате #{0} учавствуют: {1}.", _chatID, createAppeal(new List<long>())));
        }

        static IEnumerable<long> getUsersAfterBot(List<long> botMessageIDs)
        {
            _logger.WriteInLog("Получение идентификаторов активных пользователей.");
            var ids = perform(() => getUsersAfterBot_loc(botMessageIDs), "Неудача при получении идентификаторов активных пользователей.");
            _logger.WriteInLog(string.Format("Получены идентификаторы активных пользователей: [{0}].", string.Join(", ", ids)));
            return ids;
        }

        static IEnumerable<long> getUsersAfterBot_loc(List<long> botMessageIDs)
        {
            Func<IEnumerable<Message>, IEnumerable<long>> ms2AIDs =
                ms => ms.Where(m => botMessageIDs.All(bid => bid != m.Id))
                        .Select(m => m.FromId)
                        .Where(i => i.HasValue)
                        .Select(i => i.Value);

            var fstBotID = botMessageIDs[0];

            IEnumerable<long> authorIDs = new List<long>();
            long? lastMessageID = 0;
            var index = -1;

            IEnumerable<Message> msgs = null;

            while (index == -1 && authorIDs.Count() < _users.Count)
            {
                var arg = new MessagesGetHistoryParams
                {
                    PeerId = ChatID,
                    Count = 20,
                    StartMessageId = lastMessageID == 0 ? null : lastMessageID,
                    Offset = lastMessageID == 0 ? 0 : 1
                };

                msgs = _api.Messages.GetHistory(arg).Messages;
                index = msgs.Select(m => m.Id).ToList().IndexOf(fstBotID);

                if (index == -1)
                    authorIDs = authorIDs.Concat(ms2AIDs(msgs)).Distinct();

                lastMessageID = msgs.Last().Id;
            }

            if (authorIDs.Count() < _users.Count)
                authorIDs = authorIDs.Concat(ms2AIDs(msgs.Take(index - 1))).Distinct();

            return authorIDs;
        }

        static long sendMessage(string msg)
        {
            Func<long> f = () => _api.Messages.Send(new MessagesSendParams { ChatId = _chatID, Message = msg });

            _logger.WriteInLog(string.Format("Отправка сообщения: \"{0}\".", msg));
            var id = perform(f, "Неудача при отправке сообщения.");
            _logger.WriteInLog(string.Format("Сообщение отправлено; его ID: {0}.", id));
            return id;
        }

        static Typ perform<Typ>(Func<Typ> f, string errorMessage)
        {
            try
            {
                return f();
            }
            catch (HttpRequestException)
            {
                _logger.WriteInLog(errorMessage);
                Thread.Sleep(fiveMinutes);
                return perform(f, errorMessage);
            }
        }

        static string createAppeal(IEnumerable<long> ignoredUsers)
        {
            var users = _users.Where(u => ignoredUsers.All(i => i != u.Id));
            var texts = users.Select(u => string.Format("@{0} ({1})", u.Domain, u.FirstName));

            if (!texts.Any()) return string.Empty;
            if (texts.Count() == 1) return texts.First();

            var last = texts.Last();
            var withoutLast = texts.Take(texts.Count() - 1);

            return string.Format("{0} и {1}", string.Join(", ", withoutLast), last);

        }
    }
}