using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Net.Http;
using System.IO;
using System.Configuration;

using VkNet;
using VkNet.Model;
using VkNet.Model.RequestParams;
using VkNet.Enums.Filters;
using VkNet.Exception;

namespace ChatBot

{
    static class Program
    {
        const string _firstGoodMessage = "День добрый! Вам пора выбрать время для встречи!";
        const string _refirstMessage   = "День добрый! Вы по-прежнему не встретились в этом месяце. :(\nВыберите время для встречи!";
        const string _firstBadMessage  = "День добрый! Вы так и не погуляли в прошлом месяце! :'(\nВам пора выбрать время для встречи!";

        static string[] _restMessagesSingular =
            {
                "{0}, ты по-прежнему не участвуешь в коммуникации!",
                "{0}, уже два часа прошло, а ты всё не обсуждаешь вашу будущую прогулку!",
                "{0}, может, ты обратишь внимание на этот чат?..",
                "{0}, ты заставляешь друзей ждать!..",
                "{0}, подай признаки жизнедеятельности!",
                "{0}, ты что, не хочешь гулять?",
                "{0}, я сдаюсь... Я так и не смог привлечь твое внимание... Напишу в этот чат на следующей неделе."
            };

        static string[] _restMessagesPlural =
        {
                "{0}, вы по-прежнему не участвуете в коммуникации!",
                "{0}, уже два часа прошло, а вы всё не обсуждаете вашу будущую прогулку!",
                "{0}, может, вы обратите внимание на этот чат?..",
                "{0}, вы заставляете друга ждать!..",
                "{0}, подайте признаки жизнедеятельности!",
                "{0}, вы что, не хотите гулять?",
                "{0}, я сдаюсь... Я так и не смог привлечь ваше внимание... Напишу в этот чат на следующей неделе."
            };

        const string _goodMessage  = "БОТ:\nВы начали коммуницировать.\nМоя задача выполнена!";

        const string _solutionFile = "PeterSolution.txt";

        const bool isTest = false;

        static int _maxRepeats = _restMessagesSingular.Length;

        const int fiveMinutes = isTest ? 1000 : 1000 * 60 * 5;
        const int oneHour = isTest ? 1000 : fiveMinutes * 12;

        static long _botID;

        const long _myChatID = isTest ? 80 : 62;
        const long _botChatID = isTest ? 1 : 2;
        static string _myAccessToken;
        static string _botAccessToken;
        static VkApi _myApi;
        static VkApi _botApi;

        static List<User> _users;

        static Logger _logger;

        static long MyChatID
        {
            get
            {
                return _myChatID + 2000000000;
            }
        }

        static void Main(string[] args)
        {
            try
            {
                if (args.Length > 0) Directory.SetCurrentDirectory(args[0]);
                _logger = new Logger();
                BotMain(isTest);
            }
            catch (Exception e)
            {
                _logger.WriteInLog(string.Format("Исключение во время исполнения:\n{0}", e));
            }
        }

        static void BotMain(bool isTest)
        {
            _logger.WriteInLog(string.Format("Начало работы для чата #{0} (для бота - #{1}).", _myChatID, _botChatID));

            var isFirstWeek = DateTime.Now.Day <= 7;

            var wasWalkBeforeReCreate = Reader.ReadFrom(_solutionFile);

            if (!isTest && isFirstWeek)
            {
                _logger.WriteInLog("В прошлом месяце прогулка " + (wasWalkBeforeReCreate ? "" : "не") + "состоялась.");
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
            if (!readSettings()) return;

            _myApi = new VkApi();
            _myApi.Authorize(new ApiAuthParams { AccessToken = _myAccessToken });

            _botApi = new VkApi();
            _botApi.Authorize(new ApiAuthParams { AccessToken = _botAccessToken });

            initUsers();

            var firstText = isFirstWeek ? (wasWalkBeforeReCreate ? _firstGoodMessage : _firstBadMessage) : _refirstMessage;
            sendMessage(firstText);
            var fstBotMsgID = findLastBotMsgID();

            for (int i = 0; i < _maxRepeats; ++i)
            {
                Thread.Sleep(oneHour);

                var authors = getUsersAfterBot(fstBotMsgID);

                if (authors.Count() < _users.Count)
                {
                    _logger.WriteInLog("Не все пользователи написали в чате.");
                    var msgTemplate = _users.Count - authors.Count() == 1 ? _restMessagesSingular[i] : _restMessagesPlural[i];
                    var appeal = createAppeal(authors);
                    var id = sendMessage(string.Format(msgTemplate, appeal));
                }
                else
                {
                    _logger.WriteInLog("Каждый пользователь написал в чате.");
                    sendMessage(_goodMessage);
                    return;
                }
            }
        }

        static bool readSettings()
        {
            var mat = "my_access_token";
            var bat = "bot_access_token";
            var bid = "bot_id";

            _myAccessToken = ConfigurationManager.AppSettings[mat];
            if (string.IsNullOrEmpty(_myAccessToken))
            {
                var msg = "Access Token пользователя (\"{0}\") не обнаружен в конфигурационном файле. Дальнейцая работа невозможна.";
                _logger.WriteInLog(string.Format(msg, mat));
                return false;
            }

            _botAccessToken = ConfigurationManager.AppSettings[bat];
            if (string.IsNullOrEmpty(_botAccessToken))
            {
                var msg = "Access Token сообщества (\"{0}\") не обнаружен в конфигурационном файле. Дальнейцая работа невозможна.";
                _logger.WriteInLog(string.Format(msg, bat));
                return false;
            }

            var botIdStr = ConfigurationManager.AppSettings[bid];
            if (string.IsNullOrEmpty(botIdStr))
            {
                var msg = "Id сообщества (\"{0}\") не обнаружено в конфигурационном файле. Дальнейцая работа невозможна.";
                _logger.WriteInLog(string.Format(msg, bid));
                return false;
            }

            if (!long.TryParse(botIdStr, out _botID))
            {
                var msg = "Id сообщества (\"{0}\") должно быть числом. Дальнейцая работа невозможна.";
                _logger.WriteInLog(string.Format(msg, bid));
                return false;
            }

            return true;
        }

        static void initUsers()
        {
            Func<IEnumerable<User>> get = () =>
              _myApi.Messages.GetChatUsers(new long[] { _myChatID }, UsersFields.Domain, null);

            _logger.WriteInLog(string.Format("Получение информации о чате #{0}.", _myChatID));
            _users = perform(get, "Неудача при получении информации о чате.").ToList();
            var botIndex = _users.FindIndex(u => u.Id == _botID);
            if (botIndex >= 0) _users.RemoveAt(botIndex);
            _logger.WriteInLog(string.Format("В чате #{0} учавствуют: {1}.", _myChatID, createAppeal(new List<long>())));
        }

        static long findLastBotMsgID()
        {
            _logger.WriteInLog("Поиск последнего сообщения бота.");
            var id = perform(() => findLastBotMsgID_loc(), "Неудача при поиске последнего сообщения бота.");
            _logger.WriteInLog("Последнее сообщение бота найдено.");
            return id;
        }

        static long findLastBotMsgID_loc()
        {
            long index = -1;
            long? lastMessageID = 0;
            IEnumerable<Message> msgs = null;

            while (index == -1)
            {

                var arg = new MessagesGetHistoryParams
                {
                    PeerId = MyChatID,
                    Count = 20,
                    StartMessageId = lastMessageID == 0 ? null : lastMessageID,
                    Offset = lastMessageID == 0 ? 0 : 1
                };

                msgs = _myApi.Messages.GetHistory(arg).Messages;
                var botMsgs = msgs.Where(m => m.FromId == -_botID);
                if (botMsgs.Any()) index = botMsgs.First().Id.Value;
                lastMessageID = msgs.Last().Id;
            }

            return index;
        }

        static IEnumerable<long> getUsersAfterBot(long botMessageID)
        {
            _logger.WriteInLog("Получение идентификаторов активных пользователей.");
            var ids = perform(() => getUsersAfterBot_loc(botMessageID), "Неудача при получении идентификаторов активных пользователей.");
            _logger.WriteInLog(string.Format("Получены идентификаторы активных пользователей: [{0}].", string.Join(", ", ids)));
            return ids;
        }

        static IEnumerable<long> getUsersAfterBot_loc(long fstBotID)
        {
            Func<IEnumerable<Message>, IEnumerable<long>> ms2AIDs =
                ms => ms.Where(m => m.FromId != -_botID)
                        .Select(m => m.FromId)
                        .Where(i => i.HasValue)
                        .Select(i => i.Value);

            IEnumerable<long> authorIDs = new List<long>();
            long? lastMessageID = 0;
            var index = -1;

            IEnumerable<Message> msgs = null;

            while (index == -1 && authorIDs.Count() < _users.Count)
            {
                var arg = new MessagesGetHistoryParams
                {
                    PeerId = MyChatID,
                    Count = 20,
                    StartMessageId = lastMessageID == 0 ? null : lastMessageID,
                    Offset = lastMessageID == 0 ? 0 : 1
                };

                msgs = _myApi.Messages.GetHistory(arg).Messages;
                index = msgs.Select(m => m.Id).ToList().IndexOf(fstBotID);

                if (index == -1)
                    authorIDs = authorIDs.Concat(ms2AIDs(msgs)).Distinct();

                lastMessageID = msgs.Last().Id;
            }

            if (authorIDs.Count() < _users.Count)
                authorIDs = authorIDs.Concat(ms2AIDs(msgs.Take(index))).Distinct();

            return authorIDs;
        }

        static long sendMessage(string msg)
        {
            Func<long> f = () => _botApi.Messages.Send(new MessagesSendParams { ChatId = _botChatID, Message = msg });

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
            catch (Exception e) when (e is HttpRequestException || e is CaptchaNeededException)
            {
                _logger.WriteInLog(errorMessage + " Exception type: " + e.GetType().ToString() + ".");
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