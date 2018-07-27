using System;
using System.Linq;
using System.Threading;

using VkNet;
using VkNet.Model;
using VkNet.Model.RequestParams;

namespace ChatBot

{
    static class Program
    {
        // Put your accss token here
        const string _accessToken = "Put your accss token here";

        const string _firstGoodMessage = "БОТ:\nДень добрый! Вам пора выбрать время для встречи!";
        const string _refirstMessage   = "БОТ:\nДень добрый! Вы по-прежнему не встретились в этом месяце. :(\nВыберите время для встречи!";
        const string _firstBadMessage  = "БОТ:\nДень добрый! Вы так и не погуляли в прошлом месяце! :'(\nВам пора выбрать время для встречи!";

        static string[] _restMessages  = 
            {
                "БОТ:\nНу не молчите... Начните, пожалуйста, обсуждение!",
                "БОТ:\nУже два часа прошло, а вы все не начнете обсуждать вашу будущую прогулку!",
                "БОТ:\n@id182929580 (Саша), может ты первым начнешь этот разовор?..",
                "БОТ:\nА может ты, @l_macabre (Лада)?..",
                "БОТ:\n@n0t_kill (Петя), ну хоть ты начни ваш диалог!",
                "БОТ:\nВы что, не хотите гулять?",
                "БОТ:\nЭх вы... Больше не буду вам писать на этой неделе..."
            };

        const string _goodMessage  = "БОТ:\nМоя задача выполнена!\nВы начали коммуницировать.";

        const string _solutionFile = "PeterSolution.txt";

        const bool isTest = false;

        const long _chatID = isTest ? 80 : 62;
        static int _maxRepeats = _restMessages.Length;

        const int fiveMinutes = isTest ? 1000 : 1000 * 60 * 5;
        const int oneHour = isTest ? 1000 : fiveMinutes * 12;

        static VkApi _api;
        static string _logPath;
            
        static void Main(string[] args)
        {
            _logPath = Logger.CreateLog();
            Logger.WriteInLog(_logPath, string.Format("Начало работы для чата #{0}.", _chatID));

            var isFirstWeek = DateTime.Now.Day <= 7;

            var wasWalkBeforeReCreate = Reader.ReadFrom(_solutionFile);

            if (!isTest && isFirstWeek)
            {
                Reader.ReCreate(_solutionFile);
                Logger.WriteInLog(_logPath, "Обновление информации о прогулке в начале месяца.");
            }

            var wasWalk = Reader.ReadFrom(_solutionFile);

            if (!isTest && wasWalk)
            {
                Logger.WriteInLog(_logPath, "Прогулка уже состоялась. Завершение работы бота.");
                return;
            }

            Logger.WriteInLog(_logPath, "Оповещение необходимо.");

            _api = new VkApi();
            _api.Authorize(new ApiAuthParams { AccessToken = _accessToken });

            var myMessageID = sendMessage(isFirstWeek ? (wasWalkBeforeReCreate ? _firstGoodMessage : _firstBadMessage) : _refirstMessage);

            for (int i = 0; i < _maxRepeats; ++i)
            {
                Thread.Sleep(oneHour);

                var lastMessageID = getLastMessageID();

                if (lastMessageID != myMessageID)
                {
                    Logger.WriteInLog(_logPath, "Кто-то писал в чате.");
                    sendMessage(_goodMessage);
                    return;
                }

                myMessageID = sendMessage(_restMessages[i]);
            }
        }

        static long? getLastMessageID()
        {
            Logger.WriteInLog(_logPath, "Получение последнего сообщения.");
            var id = getLastMessageID_withoutLog();
            Logger.WriteInLog(_logPath, string.Format("Получено ID последнего сообщения: {0}.", id));
            return id;
        }

        static long? getLastMessageID_withoutLog()
        {
            try
            {
                return _api.Messages.GetHistory(new MessagesGetHistoryParams { PeerId = 2000000000 + _chatID, Count = 1 }).Messages.Last().Id;
            }
            catch
            {
                Logger.WriteInLog(_logPath, "Неудача при получении последнего сообщения.");
                Thread.Sleep(fiveMinutes);
                return getLastMessageID_withoutLog();
            }
        }

        static long sendMessage(string msg)
        {
            Logger.WriteInLog(_logPath, string.Format("Отправка сообщения: \"{0}\".", msg));
            var id = sendMessageWithoutLog(msg);
            Logger.WriteInLog(_logPath, string.Format("Сообщение отправлено; его ID: {0}.", id));
            return id;
        }

        static long sendMessageWithoutLog(string msg)
        {
            try
            {
                return _api.Messages.Send(new MessagesSendParams { ChatId = _chatID, Message = msg });
            }
            catch
            {
                Logger.WriteInLog(_logPath, "Неудача при отправке сообщения.");
                Thread.Sleep(fiveMinutes);
                return sendMessageWithoutLog(msg);
            }
        }

    }
}