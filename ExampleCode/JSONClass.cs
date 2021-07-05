using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Mimic_Panel_Sklad3.Com
{
    public class JSONClass : Controller
    {
        public class Messages
        {
            public String Success { get; set; }
            public String Info { get; set; }
            public String Error { get; set; }
        }
        /// <summary>
        /// Флаг пришли ли данные или нет.
        /// </summary>
        public bool Success { get; set; }
        /// <summary>
        /// Сообщение пользователю
        /// </summary>
        //public String Message { get; set; }
        public Messages Message = new Messages();
        /// <summary>
        /// Вывод сообщения об ошибках(На стороне клиента используется для консольки)
        /// </summary>
        public String ConsoleMessage { get; set; }
        /// <summary>
        /// Возвращаемые данные(Обычно это HTML строка, но в разных методах используется по разному)
        /// </summary>
        public String ResultHtml { get; set; }

        public Object Object_j { get; set; }
        /// <summary>
        /// Устанавливаем сообщение ошибкию Используется когда срабатывает Exception
        /// </summary>
        /// <param name="ex"></param>
        public void InstallErrorMessage(Exception ex, string message = null)
        {
            if (message != null)
                Message.Error = message;
            else
                Message.Error = String.Format("Возникла непредвиденная ошибка. По техническим вопросам, связанным с работой сайта, обращаться по телефону: 1111. Направьте свой вопрос в ОМК-ИТ, описав последовательность Ваших действий.");

            ConsoleMessage = String.Format("Message - {0}\r\n\r\n" +
                                                     "InnerException - {1}\r\n\r\n" +
                                                     "StackTrace - {2}", ex.Message, ex.InnerException, ex.StackTrace);
            Success = false;
        }
        /// <summary>
        /// Метод преобразования данных класса в формат JSON
        /// </summary>
        /// <returns>Строка в формате JSON</returns>
        public JsonResult ToJson()
        {
            return Json(
                new
                {
                    success = Success,
                    message = Message,
                    consoleMessage = ConsoleMessage,
                    resultHtml = ResultHtml,
                    object_j = Object_j
                });
        }
    }
}
