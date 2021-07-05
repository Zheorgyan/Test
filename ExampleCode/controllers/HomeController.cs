using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Configuration;


namespace Mimic_Panel_Sklad3.WebUI.Controllers
{
    public class HomeController : ControllerInitializer
    {
        public HomeController(IServiceLayer serviceLayer) : base(serviceLayer) { }

        /// <summary>
        /// Метод загружается при старте страницы
        /// </summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            var systems = GlobalCache.GetMnemoObjModel.Mnemos;
            ViewBag.SystemList = new SelectList(systems, "MNEMO_NAME", "MNEMO_NAME");

            return View();
        }

        /// <summary>
        /// Метод формирует отчет по выбранному штабелю
        /// Отчет формируется из системы АСПТП -> Складские объекты ТЭСЦ-3 (пономерной учет)
        /// </summary>
        /// <param name="id">Идентфикатор штабеля, поле ID таблицы TESC3.SPR_ALL_STACKS</param>
        /// <returns></returns>
        public ActionResult GetReport(int id)
        {
            // ссылка на отчет в системе АСПТП берем из web.config
            string pathValue = ConfigurationManager.AppSettings["ReportByShtabel"].ToString();
            return Redirect(pathValue + "?Shtab=" + id);
        }


        public ActionResult GetKarmanDetails(string _mnemoName, string _stackName, int? _pocketNum)
        {
            try
            {
                new Mapper().LoadDataKarman(_mnemoName, _stackName, Convert.ToInt32(_pocketNum));


                jsonClass.ResultHtml = this.RenderPartialViewToString("GetKarmanDetails", GlobalCache.GetKarmanDetails);

                jsonClass.Success = true;
            }
            catch (Exception ex) { jsonClass.InstallErrorMessage(ex); }

            return jsonClass.ToJson();
        }

        public ActionResult GetPipeNumbers(string _mnemoName,
                                           string _stackName,
                                           int? _pocketNum,
                                           double _diameter,
                                           double _thickness)
        {
            try
            {
                new Mapper().LoadDataPipes(_mnemoName, _stackName, Convert.ToInt32(_pocketNum), _diameter, _thickness);

                jsonClass.ResultHtml = this.RenderPartialViewToString("GetPipeNumbers", GlobalCache.GetPipeNumbers);

                jsonClass.Success = true;
            }
            catch (Exception ex)
            {
                jsonClass.InstallErrorMessage(ex);
            }

            return jsonClass.ToJson();
        }

        /// <summary>
        /// Метод отображает на экране выбранную мнемосхему
        /// </summary>
        /// <param name="id">Идентификатор мнемосхемы</param>
        /// <returns></returns>
        public ActionResult MnemoLoad(string SystemList)
        {
            (new Mapper()).LoadData(SystemList);
            return PartialView(GlobalCache.GetSkladObjModel);
        }

    }
}