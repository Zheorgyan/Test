using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.Data.OleDb;
using System.Collections.Generic;
using System.Drawing;
using AuthenticationService;
using System.Data;
using System.Data.OracleClient;
using System.Web;
using System.Text;
using Button = System.Web.UI.WebControls.Button;
using Control = System.Web.UI.Control;
using TextBox = System.Web.UI.WebControls.TextBox;
using TESC3;

public partial class Inspection : Page
{
    //предельное расхождение между длиной трубы с измерителя и длиной с ТОС
    const int MAX_DELTALENGTH_WARNING = 30;

    //максимальное отклонение фактической массы от теоретической, кг
    const int MAX_DELTAWEIGHT_WARNING = 40;

    //предельное расхождение между длиной трубы с измерителя и длиной с гидропресса
    const int MAX_DELTALENGTH_WARNING_HYD = 100;
    //const int MAX_DELTALENGTH_WARNING_HYD = 100;

    //игнорировать ошибку "не получен ответ от ПЛК"
    const bool IGNORE_UDP_ERRORS = true;

    //отключение контроля маршрутов для отладки
    const bool DISABLE_ROUTE_CONTROL = false;

    //разрешение приемки некондиции
    const bool EnableNoCondidion = false;

    // (ВРЕМЕННО ЗАКОММЕНТИРОВАНО на рабочих серверах по просьбе заказчика. После окончательного тестирования раскомментировать)
    /// <summary>Блокировка возможности сохранения НЕ на строку кампании</summary>
    const bool BlockNotCampainAccept = false;

    //списки элементов управления дефектов на обрезь
    List<DropDownList> ddlsLeftDefects = new List<DropDownList>();
    List<TextBox> txbsLeftDefects = new List<TextBox>();
    List<DropDownList> ddlsRightDefects = new List<DropDownList>();
    List<TextBox> txbsRightDefects = new List<TextBox>();

    //номенклатурный номер брака и лома негабаритного
    const String BrakInventoryCode = "000000000260000095";
    const String ObrInventoryCode = "000000000260000096";

    int Check { get { object o = ViewState["_Check"]; if (o == null) return 0; return (int)ViewState["_Check"]; } set { ViewState["_Check"] = value; } }

    private int _Code_Scraping;

    public int Code_Scraping
    {
        get { return _Code_Scraping; }
        set { _Code_Scraping = value; }
    }

    private string _Hydropress;

    public string Hydropress
    {
        get { return _Hydropress; }
        set { _Hydropress = value; }
    }

    //инициализация при загрузке страницы
    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {
            Master.ClearErrorMessages();
            Master.EnableSaveScrollPositions(this);
            Culture = "Ru-RU";

            //проверка прав доступа к странице
            if (!Authentification.CanAddData("INSP") && !Authentification.CanAddData("PDO") &&
                !Authentification.CanAddData("REPAIR_STAN"))
                Authentification.CheckAnyAccess("INSP");

            //отмена выбора рабочего места, если выбранное место не соответствует правам доступа
            if (IsPdoForm)
            {
                if (!Authentification.CanAddData("PDO"))
                    WorkplaceId = -1;
            }
            else
            {
                if (WorkplaceId != -1)
                {
                    if (!Authentification.CanAddData("INSP") && !Authentification.CanAddData("REPAIR_STAN"))
                        WorkplaceId = -1;
                }
            }

            //если в адресной строке передан ID фото дефекта, то отображение фото дефекта
            if (!String.IsNullOrEmpty(Request.Params["show_defect_image_id"]))
            {
                ShowDefectImage(Request.Params["show_defect_image_id"]);
                return;
            }

            //заполнение выпадающих списков
            if (!IsPostBack)
            {
                FillDropDownLists();
            }

            //заполнение поля №инсп. при загрузке страницы
            lblShift.Text = Authentification.Shift;
            if (!IsPostBack)
            {
                if (Request["Inspection_WorkplaceID"] != null)
                    Session["Inspection_WorkplaceID"] = Request["Inspection_WorkplaceID"];
                try
                {
                    object o = Session["Inspection_WorkplaceID"];
                    if (o != null) WorkplaceId = Convert.ToInt32(WorkplaceId);
                }
                catch
                {
                }

                ;
            }
            else
            {
                Session["Inspection_WorkplaceID"] = WorkplaceId;
            }

            //подстановка текущего года в поле "год"
            if (txbYear.Text.Trim() == "") txbYear.Text = (DateTime.Today.Year % 100).ToString("D2");

            //видимость строки кампании
            UpdateCampaignLineVisiblity(cbShowCampaigns.Checked);

            //видимость кнопок в зависимости от кода рабочего места "обновить вес"            
            btnRefreshWeight.Visible = (WorkplaceId >= 4 & WorkplaceId <= 6);

            if (!((WorkplaceId >= 1 && WorkplaceId <= 6) || WorkplaceId == 85))
            {
                SetVisiblePlateTemplating(false);
            }

            btnToRepair.Visible = (WorkplaceId != 7);
            btnGeometryInput.Visible = (WorkplaceId != 7);
            pnlPipesQueue.Visible = (WorkplaceId == 80); //быстрый выбор труб только для инспекционной решетки 7


            //если инспекция 1-6 то авто заполнение шаблонирования
            if ((WorkplaceId >= 1 && WorkplaceId <= 6) || WorkplaceId == 85)
            {
                SetTemplateValue();
            }

            //скрытие поля "учетная дата" и контрольной цифры при выборе ПДО

            if (IsPdoForm)
            {
                //ПДО
                txbCheck.Enabled = (WorkplaceId != 0);
                btnPrintLabel2.Enabled = false;
                btnGeometryInput.Enabled = false;
                pnlRecordDate.Visible = true;
            }
            else
            {
                //не ПДО
                txbCheck.Enabled = true;
                btnPrintLabel2.Enabled = true;
                btnGeometryInput.Enabled = true;
                pnlRecordDate.Visible = false;
            }

            //выбор активного вида
            if (WorkplaceId == -1)
            {
                MainMultiView.ActiveViewIndex = -1;
                lblEnterDataMsg.Visible = true;
            }
            else
            {
                lblEnterDataMsg.Visible = false;
                if (MainMultiView.ActiveViewIndex == -1) MainMultiView.SetActiveView(FindPipesView);
            }

            TabsVisible = (MainMultiView.ActiveViewIndex != -1) & !InputDataView.Visible;

            //получение списка дефектов текущей трубы со стана
            FillDefectsList();

            //получение списка результатов испытаний трубы        
            GetTestResults();

            //построение выпадающих списков дефектов на обрезь
            FillPipeDefectsList();

            //инициализация элементов управления на вкладке исправлений            
            pnlFindForEditRecords.Height = 400;

            if (!IsPostBack)
            {
                cldDate.SelectedDate = DateTime.Now;
                cldTransportNumberPeriodStart.SelectedDate = DateTime.Today.AddDays(-1);
                cldTransportNumberPeriodEnd.SelectedDate = DateTime.Today;
                cldStartDate.SelectedDate = DateTime.Today.AddDays(-1);
                cldEndDate.SelectedDate = DateTime.Today;
            }

            //обработка запроса от кнопки "ввод геометрии"
            if (Request["ToGeometry"] != null)
            {
                try
                {
                    String pipeYear = Request["PIPE_YEAR"].ToString().Trim();
                    String pipeNumber = Request["PIPE_NUMBER"].ToString().Trim();
                    String checkChar = Request["CHECK_CHAR"].ToString().Trim();
                    if ((pipeNumber == "") | (pipeYear == ""))
                    {
                        Checking.GetLastPipeNumber(out pipeYear, out pipeNumber, out checkChar);
                    }

                    String url = "GeometryInsp.aspx?HideAllPanels=1&PIPE_YEAR=" + pipeYear + "&PIPE_NUMBER=" +
                                 pipeNumber.PadLeft(6, '0') + "&CHECK_CHAR=" + checkChar;
                    Response.Redirect(url);
                }
                catch
                {
                }
            }


            //переход ко вводу данных по трубе, если в строке запроса передан номер трубы
            if (!IsPostBack && Request["ToGeometry"] == null && Request["PIPE_YEAR"] != null)
            {
                txbPartYear.Text = Request["PIPE_YEAR"].ToString().Trim();
                txbPipeNumber.Text = Request["PIPE_NUMBER"].ToString().Trim();
                txbCheck.Text = Request["CHECK_CHAR"].ToString().Trim();
                btnOk_Click(btnOk, e);
            }

            //обработка событий из клиентского javascript       
            if (IsPostBack & !Master.IsRefresh)
            {
                String arg = Request.Params["__EVENTARGUMENT"];
                String target = Request.Params["__EVENTTARGET"];
                //удаление записи по кнопке "удалить"
                //в arg - rowid записи
                if (target == "btnDeleteRecord")
                    DeleteRecord(arg);
                //редактирование записи
                //в arg - rowid записи
                if (target == "btnEditRecord")
                    BeginEditRecord(arg);
                //установка/снятие отметки с номера трубы в списке быстрого выбора
                if (target == "CHECK_PIPE_IN_QUEUE")
                    CheckPipeInQueue(arg);
                //выбор номера трубы из списка быстрого выбора
                if (target == "SELECT_PIPE_IN_QUEUE")
                    SelectPipeInQueue(arg);
                if (target == "btnGetWeight")
                    GetWeight();
                if (target == "btnGetGeomInsp")
                    GetGeomInsp();
                if (target == "btnGetRuscRes")
                    GetRuscRes();
            }

            RebuildEditListTable();
            SetColorAndResidual(ddlCampaign.SelectedItem.Value);
        }
        finally
        {

        }
    }

    /// <summary>
    /// Установка видимости элементов блока шаблонирования
    /// </summary>
    /// <param name="visible"></param>
    void SetVisiblePlateTemplating(bool visible)
    {
        tblTemplate.Visible = visible;
        ShowInfoAboutTemplating.Visible = visible;
    }

    /// <summary>
    /// Установить значения полей шаблонирования
    /// </summary>
    void SetTemplateValue()
    {
        if (ddlResultTemplate.SelectedItem.Text == "" &&
            ((ddlCampaign.SelectedItem.Text != "" && cbShowCampaigns.Checked) ||
             (ddlNTD.SelectedItem.Text != "" && !cbShowCampaigns.Checked))
            && InputDataView.Visible)
        {
            decimal? isTemplate = GetValueIsTemplate(ddlCampaign.SelectedIndex, ddlCampaign.SelectedItem.Value);
            if (isTemplate == 0)
            {
                SetVisiblePlateTemplating(false);
                ddlResultTemplate.SelectedIndex = 3;
            }

            if (isTemplate == null)
            {
                SetVisiblePlateTemplating(true);
                Master.AddErrorMessage(
                    "Отсутствие данных о необходимости проведения ключевых контрольных операций в НД");
                return;
            }

            if (isTemplate == 1)
            {
                SetVisiblePlateTemplating(true);
                if (IsTemplate(PipeNumber))
                    ddlResultTemplate.SelectedIndex = 1;
                else
                    ddlResultTemplate.SelectedIndex = 2;

            }
        }
    }

    /// <summary>
    /// Проверка прохождения шаблонирования
    /// </summary>
    /// <param name="pipe">номер трубы</param>
    /// <returns>true - пройдено</returns>
    private bool IsTemplate(int pipe)
    {
        List<SqlParameter> parametrs = new List<SqlParameter>();
        parametrs.Add(new SqlParameter { name = "PipeNumber", Value = pipe });
        HYDROPRESS_DRIFTER_PIPE hdp =
            Repository.SelectOne<HYDROPRESS_DRIFTER_PIPE>("WHERE PIPE_NUMBER = :PipeNumber ORDER BY REC_DATE DESC",
                parametrs);
        if (hdp != null)
        {
            if (hdp.DRIFTER_WAY > hdp.PIPE_LENGTH + ((hdp.DRIFTER_LENGTH / 100) * 2 / 3))
            {
                return true;
            }
        }

        return false;
    }


    /// <summary>
    /// Получить значение шаблонирования
    /// </summary>
    /// <param name="selectedIndexCampany"></param>
    /// <param name="valueCampany"> </param>
    /// <returns></returns>
    decimal? GetValueIsTemplate(int selectedIndexCampany, string valueCampany)
    {
        SPR_CONTROL_OPERATION SCO;
        CAMPAIGNS campain;
        if (cbShowCampaigns.Checked)
        {
            campain = Repository.SelectOne<CAMPAIGNS>(
                "WHERE CAMPAIGN_LINE_ID = " + valueCampany + " and EDIT_STATE = 0");
            if (campain != null && campain.IS_TEMPLATING != null)
            {
                return campain.IS_TEMPLATING;
            }
            //если шаблонирования нет
            else
            {
                SCO = SprControlOperationService.GetByInventoryCode(campain.INVENTORY_CODE);
            }
        }
        else
        {
            SCO = SprControlOperationService.GetByNTDId(GetNDCode(ddlNTD));
        }

        if (SCO == null)
        {
            return null;
        }

        return SCO.IS_TEMPLATING;

    }



    /// <summary>
    /// Заполнение списка принтеров при выборе рабочего места
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void ddlWorkPlace_SelectedIndexChanged(object sender, EventArgs e)
    {
        try
        {
            //чекбокс автоматического назначения кампании по последней записи
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            using (OleDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"select distinct auto_campaign_flag from inspection_pipes
                                where trx_date = (select max(trx_date) from inspection_pipes where edit_state = 0 and workplace_id = ?)
                                and edit_state = 0
                                and workplace_id = ?";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("WORKPLACE_ID", WorkplaceId);
                cmd.Parameters.AddWithValue("WORKPLACE_ID1", WorkplaceId);
                int cbAuto = Convert.ToInt32(cmd.ExecuteScalar());

                if (cbAuto == 1)
                {
                    cbAutoCampaign.Checked = true;
                    Master.AlertMessage = "Внимание! Выбран автоматический режим назначения кампании";
                }
                else
                {
                    cbAutoCampaign.Checked = false;
                    Master.AlertMessage = "Внимание! Автоматический режим назначения кампании отключен";
                }
            }

            ddlPrinter.Items.Clear();

            Dictionary<int, String> printers = PrintersSettings.GetPrinters(WorkplaceId);
            foreach (KeyValuePair<int, String> printer in printers)
            {
                ddlPrinter.Items.Add(new ListItem(printer.Value, printer.Key.ToString()));
            }
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка заполнения списка принтеров", ex);
        }
    }


    /// <summary>
    /// Код текущего рабочего места
    /// </summary>
    protected int WorkplaceId
    {
        get
        {
            if (ddlWorkPlace.SelectedIndex > 0)
                return Convert.ToInt32(ddlWorkPlace.SelectedItem.Value);
            else
                return -1;
        }
        set
        {
            SelectDDLItemByValue(ddlWorkPlace, value.ToString());
            ddlWorkPlace_SelectedIndexChanged(ddlWorkPlace, EventArgs.Empty);
        }
    }


    /// <summary>
    /// Номер текущей трубы по которой вводятся данные
    /// </summary>
    protected int PipeNumber
    {
        get
        {
            if (txbPipeNumber.Text.Trim() != "" && txbYear.Text.Trim() != "")
            {
                string strPipeNumber = txbYear.Text.Trim() + txbPipeNumber.Text.Trim().PadLeft(6, '0');
                return Convert.ToInt32(strPipeNumber);
            }
            else
            {
                if (lblPipeNo.Text.Trim() != "" && lblYear.Text.Trim() != "")
                {
                    string strPipeNumber = lblYear.Text.Trim() + lblPipeNo.Text.Trim().PadLeft(6, '0');
                    return Convert.ToInt32(strPipeNumber);
                }

                return 0;
            }
        }
        set
        {
            txbYear.Text = ((int)(value / 1E6)).ToString("D2");
            txbPipeNumber.Text = ((int)(value % 1E6)).ToString("D6");
        }
    }


    /// <summary>
    /// Название текущего рабочего меcта
    /// </summary>
    protected String WorkplaceName
    {
        get
        {
            if (WorkplaceId != -1)
            {
                OleDbConnection conn = Master.Connect.ORACLE_TESC3();
                using (OleDbCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "select max(workplace_name) from spr_workplaces where ID=?";
                    cmd.Parameters.AddWithValue("ID", WorkplaceId);
                    return cmd.ExecuteScalar().ToString();
                }
            }
            else
            {
                return "";
            }
        }
    }


    //поиск элемента управления по ID
    protected Control RecursiveFindControl(String id, Control ParentControl)
    {
        for (int i = 0; i < ParentControl.Controls.Count; i++)
        {
            if (ParentControl.Controls[i].ID == id)
                return ParentControl.Controls[i];
            else
            {
                Control ctrl = RecursiveFindControl(id, ParentControl.Controls[i]);
                if (ctrl != null) return ctrl;
            }
        }

        return null;
    }


    #region Работа со списками предупреждений

    /// <summary>
    /// Очистка всех предупреждающих сообщений
    /// <param name="atTopPage">Очистить сообщения в верхней области страницы редактирования данных по трубе</param>
    /// <param name="atWarningWindow">Очистить сообщения в отдельной странице сообщений (отображается перед приемкой трубы)</param>
    /// </summary>
    protected void ClearWarnings(bool atTopPage = true, bool atWarningWindow = true)
    {
        lstWarningsTop.Items.Clear();
        lstWarnings.Items.Clear();
    }


    /// <summary>
    /// Добавление сообщения предупреждения на страницу
    /// </summary>
    /// <param name="message">Текст сообщения</param>
    /// <param name="atTopPage">Добавить сообщение в верхнюю область страницы редактирования данных по трубе</param>
    /// <param name="atWarningWindow">Добавить сообщение в отдельную страницу сообщений (отображается перед приемкой трубы)</param>
    protected void AddWarning(String message, bool atTopPage, bool atWarningWindow)
    {
        if (atTopPage)
            lstWarningsTop.Items.Add(message);
        if (atWarningWindow)
            lstWarnings.Items.Add(message);
    }

    #endregion


    //получение информации о трубе
    protected void GetPipeInfo(String Number)
    {
        //очистка старых значений
        txbPartNo.Text = "";
        txbPartYear.Text = "";
        txbSmelting.Text = "";
        txbLength.Text = "";
        lblPartStan.Text = "нет";
        lblPartOto.Text = "нет";
        lblCutDataInformation.Text = "";
        lblNomenclatureByCampaign.Text = "Данные из задания на кампанию: нет.";
        pnlInventoryNumberNotFound.Visible = false;
        int iNumber = 0;
        bool isProfile = false; //профильная труба
        if (!Int32.TryParse(Number, out iNumber)) return;

        //по умолчанию - год партии трубы из номера
        int LotYear = (int)(iNumber / 1E6);
        txbPartYear.Text = LotYear.ToString();

        //если ремонтный участок, то отображение полей сортамента вместо поля выбора кампании        
        if (WorkplaceId == 7)
        {
            cbShowCampaigns.Checked = false;
            cbShowCampaigns_CheckedChanged(cbShowCampaigns, EventArgs.Empty);
            UpdateCampaignLineVisiblity(false);
            SelectDDLItemByText(ddlDiam, "");
            SelectDDLItemByText(ddlProfileSize, "");
            SelectDDLItemByText(ddlThickness, "");
            SelectDDLItemByText(ddlSteelmark, "");
            SelectDDLItemByText(ddlNTD, "");
            txbNTD.Text = "";
            txbInventoryNumber.Text = "";
            SelectDDLItemByText(ddlInventoryNumber, "");
            hfInventoryNumberKP.Value = "";
        }

        try
        {
            //получение рабочего места, где труба была последней
            PipeFromInspection = false;
            PipeFromRepair = false;
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "select WORKPLACE_ID from INSPECTION_PIPES where (PIPE_NUMBER=?)and(EDIT_STATE=0) order by TRX_DATE desc";
            cmd.Parameters.AddWithValue("PIPE_NUMBER", Number);
            OleDbDataReader rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                int w = Convert.ToInt32(rdr["WORKPLACE_ID"]);
                PipeFromInspection = ((w >= 1) & (w <= 6));
                PipeFromRepair = (((w >= 7) & (w <= 9)) | (w == 85));
            }

            rdr.Close();
            rdr.Dispose();
            cmd.Dispose();

            //получение данных о трубе со стана                       
            cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT op.cutdate,
                                       gc.steelmark,
                                       gc.pipe_diameter,
                                       gc.profile_size_a,
                                       gc.profile_size_b,
                                       gc.thickness,
                                       op.part_no,
                                       op.pipelength,
                                       op.defects,
                                       op.coil_pipepart_year,
                                       op.coil_pipepart_no,
                                       op.coil_internalno
                                  FROM    optimal_pipes op
                                       LEFT JOIN
                                          geometry_coils_sklad gc
                                       ON (op.coil_pipepart_no = gc.coil_pipepart_no)
                                          AND (op.coil_pipepart_year = gc.coil_pipepart_year)
                                 WHERE op.pipe_number = ? AND (gc.edit_state = 0 OR gc.edit_state IS NULL)";
            cmd.Parameters.AddWithValue("pipe_number", iNumber);
            rdr = cmd.ExecuteReader();

            ushort defs = 0;
            if (rdr.Read())
            {
                lblCutDataInformation.Text =
                    "Отрезана на ТОС: " + Convert.ToDateTime(rdr["CUTDATE"]).ToString("dd.MM.yyyy HH:mm") + ", "
                    + rdr["PIPELENGTH"].ToString() + " мм";
                lblCutDataInformation.Text += "<br/>";

                if (rdr["profile_size_a"].ToString() != "")
                {
                    isProfile = true;
                }


                //заполнение диаметра, марки стали, стенки и других параметров со стана для участка ремонта
                if (WorkplaceId == 7)
                {
                    SelectDDLItemByText(ddlDiam, rdr["PIPE_DIAMETER"].ToString());
                    SelectDDLItemByText(ddlProfileSize,
                        rdr["profile_size_a"].ToString() != ""
                            ? rdr["profile_size_a"].ToString() + "x" + rdr["profile_size_b"].ToString()
                            : "");
                    SelectDDLItemByText(ddlThickness, rdr["THICKNESS"].ToString());
                    SelectDDLItemByText(ddlSteelmark, rdr["STEELMARK"].ToString());
                    SelectDDLItemByText(ddlNTD, "");
                    txbNTD.Text = "";
                    txbInventoryNumber.Text = "";
                    SelectDDLItemByText(ddlInventoryNumber, "");
                    txbPartNo.Text = rdr["COIL_PIPEPART_NO"].ToString();
                    txbPartYear.Text = rdr["COIL_PIPEPART_YEAR"].ToString();
                    txbLength.Text = rdr["PIPELENGTH"].ToString();
                    hfInventoryNumberKP.Value = "";
                }

                //заполнение длины трубы со стана, если рабочее место установка пакетирования или инспекции 8-11
                //if ((WorkplaceId > 80 && WorkplaceId <= 84))
                //    txbLength.Text = rdr["PIPELENGTH"].ToString();

                lblPartStan.Text = rdr["COIL_PIPEPART_YEAR"].ToString() + "-" + rdr["COIL_PIPEPART_NO"].ToString();
                ushort.TryParse(Convert.ToString(rdr["DEFECTS"]), out defs);
            }

            rdr.Close();
            rdr.Dispose();
            cmd.Dispose();

            //получение данных по имеющейся обрези от трубы            
            cmd = conn.CreateCommand();
            cmd.CommandText = @"select ip.trx_date, sd.defect_name, ip.length, ip.cut_left_length
                from inspection_pipes ip
                left join spr_defect sd on ip.cut_left_defects=to_char(sd.id)
                where edit_state=0 and next_direction is not null
                and cut_left_length>0
                and pipe_number=?
                order by ip.trx_date";
            cmd.Parameters.AddWithValue("PIPE_NUMBER", iNumber);
            rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                lblCutDataInformation.Text += Convert.ToDateTime(rdr["TRX_DATE"]).ToString("dd.MM.yyyy HH:mm")
                                              + " обрезь " + rdr["CUT_LEFT_LENGTH"].ToString() + " мм (" +
                                              rdr["DEFECT_NAME"].ToString() + ")" + "<br/>";

                //отображение последней длины трубы после обрези на участке ремонта
                if (WorkplaceId == 7)
                {
                    txbLength.Text = rdr["LENGTH"].ToString();
                }
            }

            rdr.Close();
            rdr.Dispose();
            cmd.Dispose();

            //получение партии трубы с участка ОТО
            conn = Master.Connect.ORACLE_TESC3();
            cmd = conn.CreateCommand();
            cmd.CommandText = @"select trx_date, topartnumber part 
                from termo_otdel_pipes_tesc3
                where (pipenumber=?)and(EDIT_STATE=0)                
                order by TRX_DATE desc";
            cmd.Parameters.AddWithValue("PIPE_NUMBER1", iNumber);
            rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                lblPartOto.Text = rdr["part"].ToString();
            }

            rdr.Close();
            rdr.Dispose();
            cmd.Dispose();

            //перестроение списка номенклатурных номеров
            RebuildInventoryNumberList();

            //получение партии труб со стана или с ОТО (по последнему)            
            cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT *
                                    FROM (SELECT trx_date, topartnumber part, MOD (topartyear, 100) part_year
                                            FROM termo_otdel_pipes_tesc3
                                           WHERE (pipenumber = ?) AND (edit_state = 0)
                                          UNION
                                          SELECT cutdate, part_no part, coil_pipepart_year part_year
                                            FROM optimal_pipes
                                           WHERE pipe_number = ?)
                                ORDER BY trx_date DESC";
            cmd.Parameters.AddWithValue("PIPE_NUMBER1", iNumber);
            cmd.Parameters.AddWithValue("PIPE_NUMBER2", iNumber);
            rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                txbPartNo.Text = rdr["part"].ToString();
                txbPartYear.Text = rdr["part_year"].ToString();
            }

            rdr.Close();
            rdr.Dispose();
            cmd.Dispose();

            //при неопределенном номере партии, получение её из резервных номеров            
            if (txbPartNo.Text == "")
            {
                int iLotNumber = 0;
                int iLotYear = 0;
                if (GetReservePipepart(iNumber, out iLotNumber, out iLotYear))
                {
                    txbPartNo.Text = iLotNumber.ToString();
                    txbPartYear.Text = iLotYear.ToString();
                }
            }

            //получение номера плавки со стана (по номеру партии трубы), при условии отсутствия неоднозначности
            if (txbPartNo.Text.Trim() != "")
            {
                cmd = conn.CreateCommand();
                cmd.CommandText = @"select  gc.SMELTING 
                from optimal_pipes op 
                join geometry_coils_sklad gc on (op.coil_pipepart_year=gc.coil_pipepart_year)and(op.coil_pipepart_no=gc.coil_pipepart_no)and(op.coil_internalno=gc.coil_run_no) 
                where (PIPE_NUMBER=?) and gc.EDIT_STATE=0 ";
                cmd.Parameters.AddWithValue("PIPE_NUMBER", iNumber);
                rdr = cmd.ExecuteReader();
                if (rdr.Read())
                {
                    //получение номера плавки и очистка значения, если есть неоднозначность
                    String smelting = rdr["SMELTING"].ToString();
                    if (rdr.Read()) smelting = "";

                    txbSmelting.Text = smelting;
                }

                rdr.Close();
                rdr.Dispose();
                cmd.Dispose();
            }

            //получение номера партии штрипса, из которого отрезана труба
            ShtripsLotNumber = "";
            cmd = conn.CreateCommand();
            cmd.CommandText = "select SHTRIPS_LOT_NUMBER from OPTIMAL_PIPES p "
                              + "left join GEOMETRY_COILS_SKLAD c "
                              + "on P.COIL_INTERNALNO=C.COIL_RUN_NO and P.COIL_PIPEPART_NO=C.COIL_PIPEPART_NO and P.COIL_PIPEPART_YEAR=C.COIL_PIPEPART_YEAR "
                              + "where c.EDIT_STATE=0 and p.PIPE_NUMBER=? "
                              + "order by CUTDATE desc";
            cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);
            rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                ShtripsLotNumber = rdr["SHTRIPS_LOT_NUMBER"].ToString();
            }

            rdr.Close();
            rdr.Dispose();
            cmd.Dispose();

            //данные по заданию на кампанию
            OleDbCommand TMPcmd = conn.CreateCommand();
            TMPcmd.CommandText =
                @"select gc.ROW_ID, ORIGINAL_ROWID, gc.REC_DATE, gc.COIL_PIPEPART_YEAR, gc.COIL_PIPEPART_NO, gc.COIL_RUN_NO, gc.PIPE_DIAMETER DIAM, gc.THICKNESS, gc.STEELMARK, 
                SUPPLIER, SMELTING, gc.PART_NO, COIL_NO, gc.CERT_NUM CERT_NO, CERT_DATE, WEIGHT, REC_DATE, gc.PIPE_PART_NO_THERMO PIPEPART_OTO, EDIT_STATE, FIO 
                from optimal_pipes op 
                join geometry_coils_sklad gc on (op.coil_pipepart_year=gc.coil_pipepart_year)and(op.coil_pipepart_no=gc.coil_pipepart_no)and(op.coil_internalno=gc.coil_run_no) 
                left join SPR_KADRY on (gc.OPERATOR_ID=SPR_KADRY.USERNAME)
                where (PIPE_NUMBER=?) order by cutdate desc";
            TMPcmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);
            OleDbDataReader TMPrdr = TMPcmd.ExecuteReader();

            while (TMPrdr.Read())
            {
                cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT c.*, pi.* FROM GEOMETRY_COILS_SKLAD gk
                    join CAMPAIGNS c on gk.CAMPAIGN_LINE_ID=c.CAMPAIGN_LINE_ID
                    left join oracle.V_T3_PIPE_ITEMS pi on (c.inventory_code=pi.nomer)
                    WHERE (COIL_PIPEPART_YEAR=?)and(COIL_PIPEPART_NO=?)and(COIL_RUN_NO=?)
                    and gk.EDIT_STATE=0 and c.EDIT_STATE=0                    
                    order by gk.REC_DATE";
                cmd.Parameters.AddWithValue("COIL_PIPEPART_YEAR", TMPrdr["COIL_PIPEPART_YEAR"]);
                cmd.Parameters.AddWithValue("COIL_PIPEPART_NO", TMPrdr["COIL_PIPEPART_NO"]);
                cmd.Parameters.AddWithValue("COIL_RUN_NO", TMPrdr["COIL_RUN_NO"]);
                rdr = cmd.ExecuteReader();
                if (rdr.Read())
                {
                    //маршрут СГП из строки задания на кампанию
                    //SelectDDLItemByValue(ddlSgpRoute, rdr["INSPECTION_ROUTE_ID"].ToString());

                    lblNomenclatureByCampaign.Text = "Данные из задания на кампанию: ";

                    lblNomenclatureByCampaign.Text += Convert.ToDateTime(rdr["CAMPAIGN_DATE"]).ToString("dd.MM.yyyy") + " "
                                                                         + rdr["DIAMETER"].ToString() + "x" + rdr["THICKNESS"].ToString() + " " 
                                                                         + rdr["STAL"].ToString() + " " + rdr["INVENTORY_CODE"].ToString() + " " 
                                                                         + rdr["ORDER_HEADER"].ToString() + "/" + rdr["ORDER_LINE"].ToString().PadLeft(2, '_') + " "
                                                                         + (rdr["GRUP"].ToString() != "" ? rdr["GOST"].ToString() + " гр." 
                                                                         + rdr["GRUP"].ToString() : rdr["GOST"].ToString()) + " "
                                                                         + rdr["ADDITIONAL_TEXT"].ToString() + " " + rdr["INSPECTION"].ToString();

                    //hfInventoryNumberKP.Value = rdr["inventory_code_kp"].ToString();
                }

                rdr.Close();
                rdr.Dispose();
                cmd.Dispose();
            }

            TMPrdr.Close();
            TMPrdr.Dispose();
            TMPcmd.Dispose();

            //длина с измерителя линии 1 или узк шва линии 2
            if (WorkplaceId != -1)
            {
                if ((WorkplaceId >= 1 && WorkplaceId <= 6) || WorkplaceId == 85)
                {
                    //длина трубы с УЗК шва линии отделки 1 и 2 
                    //(для линии 1 (workplace_id: 1, 2, 3) берется usc_otdelka.workplace_id=13,
                    //для линии 2 (workplace_id: 4, 5, 6) берется usc_otdelka.workplace_id=14,
                    //для зачистки (workplace_id: 85) берется последняя запись из usc_otdelka.workplace_id in (13,14))
                    cmd = Master.Connect.ORACLE_TESC3().CreateCommand();
                    cmd.CommandText = @"SELECT LENGTH
                                            FROM usc_otdelka
                                           WHERE edit_state = 0 AND pipe_number = ?" +
                                      (WorkplaceId == 85
                                          ? " AND workplace_id in (13, 14)"
                                          : " AND workplace_id = " + (WorkplaceId < 4 ? "13" : "14")) +
                                      " ORDER BY rec_date DESC";
                    cmd.Parameters.AddWithValue("pipe_number", iNumber);
                    rdr = cmd.ExecuteReader();
                    if (rdr.Read())
                    {
                        txbLength.Text = rdr["LENGTH"].ToString();
                    }

                    rdr.Close();
                    rdr.Dispose();
                    cmd.Dispose();
                }
                // для инспекционной решетке №7 и Mair в поле ввода «Длина, мм» 
                //подтягивать значение длины трубы, учитывая прохождение ее по участку ремонта и отбора проб
                else if ((WorkplaceId == 80 || WorkplaceId == 63))
                {
                    cmd = conn.CreateCommand();
                    cmd.CommandText = @" SELECT LENGTH FROM
                        (  
                           select  ip.length as LENGTH, 
                            ROW_NUMBER() OVER (PARTITION BY PIPE_NUMBER ORDER BY TRX_DATE DESC) RNUM
                            FROM TESC3.INSPECTION_PIPES ip
                            WHERE  ip.edit_state=0 
                and ip.WORKPLACE_ID in (8,7,9) and ip.NEXT_DIRECTION is not null
                and ip.pipe_number= ?                           
                        ) 
                        WHERE RNUM=1
                union all
                SELECT            op.pipelength as LENGTH
                                  FROM    optimal_pipes op
                                  WHERE op.pipe_number =? ";
                    cmd.Parameters.AddWithValue("PIPE_NUMBERip", iNumber);
                    cmd.Parameters.AddWithValue("PIPE_NUMBERop", iNumber);
                    rdr = cmd.ExecuteReader();
                    if (rdr.Read())
                    {
                        txbLength.Text = rdr["LENGTH"].ToString();
                    }

                    rdr.Close();
                    rdr.Dispose();
                    cmd.Dispose();
                }
                //заполнение длины трубы с ТОС, если рабочее место установка пакетирования или инспекции 8-11
                else if ((WorkplaceId > 80 && WorkplaceId <= 84))
                {
                    cmd = conn.CreateCommand();
                    cmd.CommandText = @"SELECT op.pipelength as LENGTH
                                        FROM optimal_pipes op
                                        WHERE op.pipe_number =? ";
                    cmd.Parameters.AddWithValue("PIPE_NUMBER", iNumber);
                    rdr = cmd.ExecuteReader();
                    if (rdr.Read())
                    {
                        txbLength.Text = rdr["LENGTH"].ToString();
                    }

                    rdr.Close();
                    rdr.Dispose();
                    cmd.Dispose();
                }
                else
                {
                    //длина трубы с измерителя линии отделки 1
                    cmd = Master.Connect.ORACLE_TESC3().CreateCommand();
                    cmd.CommandText =
                        "select LENGTH from IZMLENGTH_OTDELKA where (PIPE_NUMBER=?)and(EDIT_STATE=0) order by REC_DATE desc";
                    cmd.Parameters.AddWithValue("PIPE_NUMBER", iNumber);
                    rdr = cmd.ExecuteReader();
                    if (rdr.Read())
                    {
                        txbLength.Text = rdr["LENGTH"].ToString();
                    }

                    rdr.Close();
                    rdr.Dispose();
                    cmd.Dispose();
                }
            }

            //фактическая масса по измерителю (если не участок ремонта)
            if (WorkplaceId != 7)
            {
                lblWeight.Text = "";
                txbWeight.Text = "";
                lblWeightTemp.Text = "";
                cmd = Master.Connect.ORACLE_TESC3().CreateCommand();
                cmd.CommandText = "select WEIGHT from IZMWEIGHT_OTDELKA where (PIPE_NUMBER=?)and(EDIT_STATE=0) order by REC_DATE desc";
                cmd.Parameters.AddWithValue("PIPE_NUMBER", iNumber);
                rdr = cmd.ExecuteReader();
                if (rdr.Read())
                {
                    txbWeight.Text = rdr["WEIGHT"].ToString();
                    txbWeight.BackColor = Color.White;
                }

                rdr.Close();
                rdr.Dispose();
                cmd.Dispose();
            }

            //расчет теоретической массы, если профильная труба
            if ((txbWeight.Text == "" || txbWeight.Text == "0") && isProfile)
                GetWeight();

            //текстовые надписи длины на УЗК и предупреждение при несоответствии длины
            UpdateLengthLabels(Number);

            //получение назначенной маршрутной карты
            String pipeRouteMap = GetLastPipeRouteMap(iNumber);
            SelectDDLItemByValue(ddlPipeRouteMap, pipeRouteMap);

            //получение и отображение требуемого уровня исполнения в области предупреждений
            CheckIsProductionLevel(iNumber);
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка получения данных о трубе (GetPipeInfo)", ex);
        }
    }


    /// <summary>
    /// Получение маршрутной карты из строки задания на кампанию
    /// </summary>
    protected String GetCampaignPipeRouteMap(int campaignLineId)
    {
        String routeMap = "";

        using (OleDbCommand cmd = Master.Connect.ORACLE_TESC3().CreateCommand())
        {
            cmd.CommandText = @"select max(map_name)
                                from campaigns_routes r
                                    join campaigns c on r.campaign_line_id=c.campaign_line_id
                                    join spr_pipes_route_maps m on R.ROUTE_MAP_ID=M.ID
                                where r.edit_state=0 and c.edit_state=0 and M.EDIT_STATE=0
                                    and main_area='УОТ'
                                    and r.campaign_line_id=?";
            cmd.Parameters.AddWithValue("CAMPAIGN_LINE_ID", campaignLineId);
            routeMap = cmd.ExecuteScalar().ToString();
        }

        return routeMap;
    }


    /// <summary>
    /// Получение наименования последней заданной маршрутной карты для трубы
    /// </summary>
    /// <returns></returns>
    protected String GetLastPipeRouteMap(int pipeNumber)
    {
        //получение ID строки задания на кампанию для трубы с участка сварки
        int campaignLineId = -1;
        using (OleDbCommand cmd = Master.Connect.ORACLE_TESC3().CreateCommand())
        {
            cmd.CommandText = @"select nvl(max(cmp.campaign_line_id),-1)
                                from optimal_pipes op
                                    join geometry_coils_sklad gc on OP.COIL_INTERNALNO=GC.COIL_RUN_NO
                                        and OP.COIL_PIPEPART_NO=GC.COIL_PIPEPART_NO
                                        and OP.COIL_PIPEPART_YEAR=GC.COIL_PIPEPART_YEAR
                                    join campaigns cmp on GC.CAMPAIGN_LINE_ID=CMP.CAMPAIGN_LINE_ID
                                where cmp.edit_state=0 and gc.edit_state=0
                                    and op.pipe_number=?";
            cmd.Parameters.AddWithValue("PIPE_NUMBER", pipeNumber);
            campaignLineId = Convert.ToInt32(cmd.ExecuteScalar());
        }

        //получение маршрутной карты для трубы из исходной кампании на которую был задан металл
        String routeMap = GetCampaignPipeRouteMap(campaignLineId);

        //получение последней маршрутной карты при изменении маршрута на окончательной приемке или ремонте
        using (OleDbCommand cmd = Master.Connect.ORACLE_TESC3().CreateCommand())
        {
            cmd.CommandText = @"select map_name
                                from inspection_pipes
                                where edit_state=0
                                    and pipe_number=?
                                    and map_name is not null
                                order by trx_date desc";
            cmd.Parameters.AddWithValue("PIPE_NUMBER", pipeNumber);
            using (OleDbDataReader rdr = cmd.ExecuteReader())
            {
                if (rdr.Read())
                {
                    routeMap = rdr["MAP_NAME"].ToString();
                }
            }
        }

        return routeMap;
    }


    //получение предыдущей точки маршрута трубы для поля PREV_DIRECTION
    protected String GetPrevDirection(int PipeNumber)
    {
        //подключение к БД и запрос на выборку значений из Inspection_Pipes
        OleDbConnection conn = Master.Connect.ORACLE_TESC3();
        OleDbCommand cmd = conn.CreateCommand();
        String SQL = @"select * 
                        from inspection_pipes 
                        where trx_date = (select max(trx_date) 
                                            from inspection_pipes 
                                            where (PIPE_NUMBER=?)and(EDIT_STATE=0)
                                                and(((INVENTORY_CODE<>'034.000037')and(INVENTORY_CODE<>'034.000086'))or(INVENTORY_CODE is NULL))";
        if (UpdateRowID != "") SQL += "and(TRX_DATE<(select trx_date from inspection_pipes where row_id=?)) ";
        SQL += ")" + "and((INVENTORY_CODE<>'034.000037')or(INVENTORY_CODE is NULL))";

        cmd.CommandText = SQL;
        cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);
        if (UpdateRowID != "") cmd.Parameters.AddWithValue("UPDATE_ROW_ID", UpdateRowID);

        //попытка выборки найденных данных
        String prevDirection = "";
        OleDbDataReader rdr = cmd.ExecuteReader();
        if (rdr.Read())
        {
            String nextdir = rdr["NEXT_DIRECTION"].ToString();
            if (nextdir != "SKLAD")
            {
                int workplace = Convert.ToInt32(rdr["WORKPLACE_ID"]);
                if ((workplace > 0) & (workplace <= 6)) prevDirection = "INSPECTION_" + workplace.ToString();
                if (((workplace >= 7) & (workplace <= 9)) || (workplace == 85))
                    prevDirection = "REMONT_" + workplace.ToString();
                if (workplace == 10) prevDirection = "RFA_STF";
            }
            else
            {
                prevDirection = "SKLAD";
            }
        }

        rdr.Close();
        rdr.Dispose();
        cmd.Dispose();
        if (prevDirection != "") return prevDirection;

        return prevDirection;
    }

    /// <summary>Получение списка результатов</summary>
    /// <returns></returns>
    protected DataTable GetTestResultsTable()
    {
        try
        {
            //данные по гидроиспытанию, УЗК и измерению длины из новой системы
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();

            //результаты испытаний
            DataTable dtTestResult = new DataTable("TestResult");
            dtTestResult.Columns.Add("rec_date", typeof(DateTime));
            dtTestResult.Columns.Add("workplace_id", typeof(int));
            dtTestResult.Columns.Add("result", typeof(string));
            dtTestResult.Columns.Add("pipe_defect", typeof(string));
            dtTestResult.Columns.Add("name_test", typeof(string));

            #region Рабочие центры

            //список объектов Линии 1
            Dictionary<int, string> dListTestLine1 = new Dictionary<int, string>
            {
                {12, "Гидроиспытание"},
                {13, "УЗК шва линии 1"},
                {15, "УЗК тела линии 1"},
                {16, "УЗК торцов линии 1, левая (первая) установка"},
                {17, "УЗК торцов линии 1, правая (вторая) установка"},
                {66, "МПК линии 1, левый конец (первая установка)"},
                {67, "МПК линии 1, правый конец (вторая установка)"}
            };
            //список объектов Линии 2
            Dictionary<int, string> dListTestLine2 = new Dictionary<int, string>
            {
                {11, "Гидроиспытание"},
                {14, "УЗК шва линии 2"},
                {64, "УЗК тела линии 2"},
                {18, "УЗК торцов линии 2, левая (первая) установка"},
                {19, "УЗК торцов линии 2, правая (вторая) установка"},
                {68, "МПК линии 2, левый конец (первая установка)"},
                {69, "МПК линии 2, правый конец (вторая установка)"}
            };
            //итоговый список
            Dictionary<int, string> dListTestLine = new Dictionary<int, string>();
            //список пройденных испытаний
            List<int> lExecuteTest = new List<int>();

            #endregion Рабочие центры

            using (OleDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT tbl.rec_date, tbl.workplace_id, tbl.result, tbl.pipe_defect, sw.area workpace_line, sw.entrance_to_line
                                   FROM    (SELECT h.rec_date, h.workplace_id, NVL (h.test_brak_manual, h.new_test_brak_auto) result, NULL pipe_defect
                                                    FROM tesc3.hydropress h
                                                    WHERE h.edit_state = 0
                                                        AND h.pipe_number = ?
                                                        AND h.workplace_id IN (11, 12)
                                                    UNION ALL
                                                    SELECT uo.first_rec_date rec_date, uo.workplace_id, NVL (uo.test_brak_manual, uo.test_brak_auto) result, uo.pipe_defect
                                                    FROM tesc3.usc_otdelka uo
                                                    WHERE     uo.edit_state = 0
                                                        AND uo.pipe_number = ?
                                                        AND uo.workplace_id IN (13, 15, 16, 17, 14, 64, 18, 19)
                                                    UNION ALL
                                                    SELECT md.rec_date, md.workplace_id, md.result_code, NULL pipe_defect
                                                    FROM tesc3.mpk_data md
                                                    WHERE md.pipe_number = ?
                                                        AND md.workplace_id IN (66, 67, 68, 69)) tbl
                                                LEFT JOIN tesc3.spr_workplaces sw ON sw.is_active = 1 AND sw.id = tbl.workplace_id
                                    ORDER BY tbl.rec_date DESC";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("pipe_number_1", PipeNumber);
                cmd.Parameters.AddWithValue("pipe_number_2", PipeNumber);
                cmd.Parameters.AddWithValue("pipe_number_3", PipeNumber);
                using (OleDbDataReader rdr = cmd.ExecuteReader())
                {
                    if (rdr.HasRows)
                    {
                        while (rdr.Read())
                        {
                            int temp_workplace_id = 0;
                            int.TryParse(rdr["workplace_id"].ToString(), out temp_workplace_id);

                            DataRow dr = dtTestResult.NewRow();
                            dr[0] = Convert.ToDateTime(rdr["rec_date"]);
                            dr[1] = temp_workplace_id;
                            dr[2] = rdr["result"].ToString();
                            dr[3] = rdr["pipe_defect"].ToString();
                            dtTestResult.Rows.Add(dr);

                            if (!lExecuteTest.Contains(temp_workplace_id))
                                lExecuteTest.Add(temp_workplace_id);

                            if (rdr["entrance_to_line"].ToString() == "1")
                            {
                                dListTestLine = rdr["workpace_line"].ToString().Contains("Линия 1")
                                    ? dListTestLine1
                                    : dListTestLine2;
                                break;
                            }
                        }

                        rdr.Close();
                    }
                }
            }

            //объединение список объектов в один, если не указан вход на линию
            if (dListTestLine.Count == 0)
            {
                dListTestLine = dListTestLine1;
                foreach (KeyValuePair<int, string> kTest in dListTestLine2)
                    dListTestLine.Add(kTest.Key, kTest.Value);
            }

            dtTestResult.Columns.Add("sort", typeof(int));
            //установка заголовков
            foreach (DataRow dr in dtTestResult.Rows)
            {
                int temp_workplace_id = 0;
                int.TryParse(dr["workplace_id"].ToString(), out temp_workplace_id);
                if (dListTestLine.ContainsKey(temp_workplace_id))
                    dr[4] = dListTestLine[temp_workplace_id];
            }

            //добавление недостающих рабочих центров
            foreach (KeyValuePair<int, string> kTest in dListTestLine)
            {
                if (!lExecuteTest.Contains(kTest.Key))
                {
                    DataRow dr = dtTestResult.NewRow();
                    dr[1] = kTest.Key;
                    dr[4] = kTest.Value;
                    dr[5] = 1;
                    dtTestResult.Rows.Add(dr);
                }
            }

            dListTestLine = null;
            dListTestLine1 = null;
            dListTestLine2 = null;
            lExecuteTest = null;

            dtTestResult = DataWorks.SortTable(dtTestResult, "sort ASC, rec_date ASC");

            return dtTestResult;
        }
        catch (Exception ex)
        {
            throw new Exception("Ошибка получения результатов испытаний (" + ex.Message + ")");
        }
    }

    //заполнение списка результатов испытаний на НЛО или КЛО
    protected bool GetTestResults()
    {
        bool res = false;
        tblTestResults.Rows.Clear();

        int test_brak_manual = -1; //перепроверка аузк шва
        int manual_ausc_body_brak = -1; //перепроверка аузк тела
        int manual_ausc_end_left_brak = -1; //перепроверка аузк концов (левая)
        int manual_ausc_end_right_brak = -1; //перепроверка аузк концов (правая)
        string rec_date = "";

        try
        {
            //шапка таблицы         
            TableRow row = new TableRow();
            row.Cells.Add(new TableCell()); row.Cells[0].Text = "Испытание"; row.Cells[0].Width = new Unit(200);
            row.Cells.Add(new TableCell()); row.Cells[1].Text = "Результат"; row.Cells[1].Width = new Unit(90);
            row.Cells.Add(new TableCell()); row.Cells[2].Text = "Дата"; row.Cells[2].Width = new Unit(120);
            row.Cells.Add(new TableCell()); row.Cells[3].Text = "Отчет"; row.Cells[3].Width = new Unit(240);
            row.BackColor = Color.FromArgb(0xE0E0E0); row.Font.Bold = true;
            tblTestResults.Rows.Add(row);

            if (PipeNumber != 0)
            {
                int count_results = 0;
                //данные по гидроиспытанию, УЗК и измерению длины из новой системы
                OleDbConnection conn = Master.Connect.ORACLE_TESC3();

                //УЗК шва.Стан
                int Def = 0;
                string def_date = "";
                using (OleDbCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "select DEFECTS, CUTDATE from OPTIMAL_PIPES where PIPE_NUMBER = ?";
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);
                    using (OleDbDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.HasRows)
                        {
                            if (rdr.Read())
                            {
                                int.TryParse(Convert.ToString(rdr["DEFECTS"]), out Def);
                                def_date = rdr["CUTDATE"].ToString();

                                using (OleDbCommand cmd_def = conn.CreateCommand())
                                {
                                    cmd_def.CommandText = "select nvl(GET_DEFECT_LIST(?), 0) as DEF_NAME from dual";
                                    cmd_def.Parameters.Clear();
                                    cmd_def.Parameters.AddWithValue("DEFECT_NUMBER", Def);
                                    using (OleDbDataReader rdr_def = cmd_def.ExecuteReader())
                                    {
                                        if (rdr_def.HasRows)
                                        {
                                            if (rdr_def.Read())
                                            {
                                                // Дефект УЗК шва.Стан
                                                row = new TableRow();
                                                row.Cells.Add(new TableCell());
                                                row.Cells.Add(new TableCell());
                                                row.Cells.Add(new TableCell());
                                                row.Cells[0].Text = "УЗК шва.Стан";
                                                if (rdr_def["DEF_NAME"].ToString().Contains("Дефект УЗК шва"))
                                                {
                                                    row.Cells[1].Text = "Дефект";
                                                    count_results++;
                                                }
                                                else if (rdr["DEFECTS"] == DBNull.Value)
                                                    row.Cells[1].Text = "";
                                                else
                                                    row.Cells[1].Text = "Пройдено";
                                                row.Cells[2].Text = def_date;
                                                tblTestResults.Rows.Add(row);

                                                // Дефект УЗК кромок
                                                row = new TableRow();
                                                row.Cells.Add(new TableCell());
                                                row.Cells.Add(new TableCell());
                                                row.Cells.Add(new TableCell());
                                                row.Cells[0].Text = "УЗК кромок";
                                                if (rdr_def["DEF_NAME"].ToString().Contains("Дефект левой кромки") ||
                                                    rdr_def["DEF_NAME"].ToString().Contains("Дефект правой кромки") ||
                                                    rdr_def["DEF_NAME"].ToString().Contains("Отсутствие АК левой кромки") ||
                                                    rdr_def["DEF_NAME"].ToString().Contains("Отсутствие АК правой кромки"))
                                                {
                                                    row.Cells[1].Text = "Дефект";
                                                    count_results++;
                                                }

                                                else if (rdr["DEFECTS"] == DBNull.Value)
                                                    row.Cells[1].Text = "";
                                                else
                                                    row.Cells[1].Text = "Пройдено";
                                                row.Cells[2].Text = def_date;
                                                tblTestResults.Rows.Add(row);
                                            }

                                            rdr_def.Close();
                                        }

                                    }
                                }
                            }

                            rdr.Close();
                        }
                    }
                }

                //результаты перепроверок, если были исправления по результатам испытаний
                using (OleDbCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"select count(*)
                                        from tesc3.usc_otdelka
                                        where pipe_number = ?
                                        and edit_state = 0";
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);
                    int countrec = Convert.ToInt32(cmd.ExecuteScalar());
                    if (countrec > 0)
                    {
                        cmd.CommandText = @"select TEST_BRAK_MANUAL, MANUAL_AUSC_BODY_BRAK, MANUAL_AUSC_END_LEFT_BRAK, MANUAL_AUSC_END_RIGHT_BRAK, REC_DATE
                                        from TESC3.USC_OTDELKA
                                        where pipe_number = ?
                                            and edit_state = 0
                                            and rec_date = (select max(rec_date)
                                                            from tesc3.usc_otdelka
                                                            where pipe_number = ?
                                                            and edit_state = 0)";
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);
                        cmd.Parameters.AddWithValue("PIPE_NUMBER1", PipeNumber);
                        using (OleDbDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.HasRows)
                                if (rdr.Read())
                                {
                                    rec_date = rdr["REC_DATE"].ToString();
                                    Int32.TryParse(rdr["TEST_BRAK_MANUAL"].ToString(), out test_brak_manual);
                                    Int32.TryParse(rdr["MANUAL_AUSC_BODY_BRAK"].ToString(), out manual_ausc_body_brak);
                                    Int32.TryParse(rdr["MANUAL_AUSC_END_LEFT_BRAK"].ToString(), out manual_ausc_end_left_brak);
                                    Int32.TryParse(rdr["MANUAL_AUSC_END_RIGHT_BRAK"].ToString(), out manual_ausc_end_right_brak);
                                }
                        }
                    }
                }


                //результаты испытаний
                DataTable dtTestResult = GetTestResultsTable();

                #region Рабочие центры

                //список рабочих центров Гидропресс
                List<int> listHydro = new List<int>() { 11, 12 };
                //список рабочих центров УЗК шва
                List<int> listUSC_S = new List<int>() { 13, 14 };
                //список рабочих центров УЗК тела
                List<int> listUSC_T = new List<int>() { 15, 64 };
                //список рабочих центров УЗК концов
                List<int> listUSC_K = new List<int>() { 16, 17, 18, 19 };
                //список рабочих центров МПК концов
                List<int> listMPK_K = new List<int>() { 66, 67, 68, 69 };

                #endregion Рабочие центры

                //получение значений контрольных операций
                ControlOperations control_operation = new ControlOperations();
                if (cbShowCampaigns.Checked)
                {
                    int campaign_line_id = 0;
                    int.TryParse(ddlCampaign.SelectedItem.Value, out campaign_line_id);

                    if (campaign_line_id > 0)
                        control_operation.GetControlOperationByCampaign(campaign_line_id, true);
                    else control_operation.GetControlOperationByPipe(PipeNumber, true);
                }
                else
                {
                    int ntd_id = 0;
                    double diameter = 0;
                    if (int.TryParse(GetNDCode(ddlNTD), out ntd_id) && ntd_id > 0 &&
                        double.TryParse(ddlDiam.SelectedItem.Value, out diameter) && diameter > 0)
                        control_operation.GetControlOperationByND(ntd_id, diameter);
                    else control_operation.GetControlOperationByPipe(PipeNumber, true);
                }

                //отображение данных
                foreach (DataRow dr in dtTestResult.Rows)
                {
                    row = new TableRow();
                    row.Cells.Add(new TableCell());
                    row.Cells.Add(new TableCell());
                    row.Cells.Add(new TableCell());
                    row.Cells.Add(new TableCell());
                    row.Cells[0].Text = dr["name_test"].ToString();

                    int test_workplace_id = Convert.ToInt32(dr["workplace_id"]);

                    string result_test = "";
                    if ((control_operation.isHydro && listHydro.Contains(test_workplace_id)) ||
                        (control_operation.isUSC_S && listUSC_S.Contains(test_workplace_id)) ||
                        (control_operation.isUSC_T && listUSC_T.Contains(test_workplace_id)) ||
                        (control_operation.isUSC_K && listUSC_K.Contains(test_workplace_id)) ||
                        (control_operation.isMPK_K && listMPK_K.Contains(test_workplace_id)))
                    {
                        result_test = dr["result"].ToString() == "0" ? "Пройдено" : "Не пройдено";
                        if (dr["pipe_defect"].ToString() != "" && dr["pipe_defect"].ToString() != "0")
                        {
                            result_test = "Не пройдено";
                            uint def = Convert.ToUInt32(dr["pipe_defect"]);
                            if ((def & 0x10) != 0) result_test = "Не пройдено/Потеря АК";
                        }
                    }
                    else result_test = "Не требуется";

                    row.Cells[1].Text = result_test;
                    row.Cells[2].Text = dr["rec_date"].ToString() != ""
                        ? Convert.ToDateTime(dr["rec_date"]).ToString("dd.MM.yyyy HH:mm:ss")
                        : "";
                    if (result_test.Contains("Не пройдено"))
                    {

                        row.Cells[1].BackColor = Color.Red;
                        //УЗК шва, УЗК тела, УЗК концов
                        if (listUSC_S.Contains(test_workplace_id) || listUSC_T.Contains(test_workplace_id) ||
                            listUSC_K.Contains(test_workplace_id))
                        {
                            Button btnRUSC = new Button() { Height = 22, Width = 120, Text = "РУЗК" };
                            btnRUSC.OnClientClick = "return btnGetRuscRes(); return false;";
                            row.Cells[3].Controls.Add(btnRUSC);

                            if ((test_brak_manual == 0 && row.Cells[0].Text.Contains("УЗК шва линии"))
                                || (manual_ausc_body_brak == 0 && row.Cells[0].Text.Contains("УЗК тела линии"))
                                || ((manual_ausc_end_left_brak == 0 || manual_ausc_end_right_brak == 0)
                                    && row.Cells[0].Text.Contains("УЗК торцов линии")))
                            {
                                btnRUSC.Visible = false;
                                row.Cells[1].Text = "Пройдено";
                                row.Cells[1].BackColor = Color.White;
                                row.Cells[2].Text = rec_date;
                            }
                        }

                        //УЗК шва, УЗК тела
                        if (listUSC_S.Contains(test_workplace_id) || listUSC_T.Contains(test_workplace_id))
                        {
                            Button btnCoordinates = new Button() { Height = 22, Width = 120, Text = "Координаты" };
                            btnCoordinates.OnClientClick = "window.open('Reports/UscDefectLocation.aspx?pipe_number= " + PipeNumber.ToString() + "&workplace_id=" +
                                                           test_workplace_id.ToString() + "&usc_date=" + dr["rec_date"].ToString() + "'); return false;";
                            btnCoordinates.Enabled = dr["rec_date"].ToString() != "";
                            row.Cells[3].Controls.Add(btnCoordinates);
                        }
                    }

                    if (result_test != "Не требуется") tblTestResults.Rows.Add(row);
                    if (row.Cells[1].Text == "Не пройдено") count_results++;
                }


                #region Очистка объектов

                dtTestResult = null;
                listHydro = null;
                listUSC_S = null;
                listUSC_T = null;
                listUSC_K = null;
                listMPK_K = null;
                control_operation.Dispose();

                #endregion Очистка объектов

                //перепроверка геометрических замеров
                string rec_date_geom = "";
                using (OleDbCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"select rec_date
                                        from GEOMETRY_PIPES_INSP
                                        where pipe_number = ?
                                        and edit_state = 0";
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);
                    using (OleDbDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                            rec_date_geom = rdr["REC_DATE"].ToString();
                    }
                }

                // УИ геометрии труб
                row = new TableRow();
                row.Cells.Add(new TableCell());
                row.Cells.Add(new TableCell());
                row.Cells.Add(new TableCell());
                row.Cells.Add(new TableCell());
                row.Cells[0].Text = "УИ геометрии труб (авт.)";
                row.Cells[1].Text = "Не пройдено";
                row.Cells[1].BackColor = Color.White;
                row.Cells[2].Text = rec_date_geom != "" ? rec_date_geom : DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");

                int campaignId = 0;
                int.TryParse(ddlCampaign.SelectedItem.Value, out campaignId);
                CheckPipe cp = new CheckPipe(PipeNumber);
                cp.Reload(PipeNumber, campaignId);
                if (cp.GetGeomValues().Count == 0) row.Cells[1].Text = "Отсут.норм.";
                else
                {
                    //получение информации по последнему входу на линию
                    PIPES_MOTION_HISTORY inPosition = PIPES_MOTION_HISTORY_Repository.GetHistoryLastInOrOutPosition(PipeNumber, true);
                    DateTime NawPositionTime;
                    if (DateTime.TryParse(cp.GetMrtValue("MEASURE_TIME").ToString(), out NawPositionTime) && NawPositionTime < inPosition.REC_DATE)
                    {
                        row.Cells[1].Text = "Отсут.замеры.";
                    }
                    else
                    {
                        if (cp.GetMrtValue("MEASURE_TIME") != DBNull.Value)
                            row.Cells[2].Text = Convert.ToDateTime(cp.GetMrtValue("MEASURE_TIME")).ToString("dd.MM.yyyy HH:mm:ss");
                        if (cp.TestMrtGeom())
                        {
                            row.Cells[1].Text = "Пройдено";
                            row.Cells[1].BackColor = Color.White;
                        }
                        else
                        {
                            row.Cells[1].Text = rec_date_geom != "" ? "Пройдено" : "Не пройдено";
                            row.Cells[1].BackColor = rec_date_geom != "" ? Color.White : Color.Red;
                        }
                    }
                }

                Button btnMrt = new Button() { Height = 22, Width = 120, Text = "Отчет МРТ" };
                btnMrt.OnClientClick = "window.open('Reports/MRTReport.aspx?pipe=" + PipeNumber.ToString() + "&date=" + row.Cells[2].Text + "'); return false;";
                row.Cells[3].Controls.Add(btnMrt);
                if (row.Cells[1].Text.Contains("Отсут.замеры.") || row.Cells[1].Text.Contains("Не пройдено"))
                {
                    if (rec_date_geom == "")
                    {
                        Button btnGeomZ = new Button() { Height = 22, Width = 120, Text = "Добавить" };
                        btnGeomZ.OnClientClick = " return btnGetGeomInsp(); return false;";
                        row.Cells[3].Controls.Add(btnGeomZ);
                    }
                }

                tblTestResults.Rows.Add(row);

                //подсчет результатов испытаний(счетчик срабатывающий на те строки, в которых результат был неудовлетворителен)
                if (count_results == 0)
                {
                    lblTestResult.Text = "По трубе не обнаружено отклонений"; pnlTestResult.Visible = true;
                }
                else pnlTestResult.Visible = false;
                count_results = 0;
            }
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка получения данных о трубе (fl_pipe)", ex);
        }

        return res;
    }

    //iframe для GeometryInsp
    protected void GetGeomInsp()
    {
        string target = "GeomInsp";
        //Запоминаем в сессионую переменную номер трубы
        Session.Add("PipeYearForGeom", lblYear.Text);
        Session.Add("PipeNumberForGeom", lblPipeNo.Text);
        Session.Add("PipeCheckForGeom", Check);
        Session.Add("WorkPlaceForGeom", WorkplaceId);
        Session.Add("TargetForGeom", target);
        Session.Add("CampaignLineForGeom", ddlCampaign.SelectedItem.Value);
        PopupWindow1.ContentPanelId = pnlGeomInsp.ID;
        PopupWindow1.Title = "Добавить геометрические замеры";
        pnlGeomInsp.Visible = true;
    }

    //iframe для RUscOtdelka.aspx
    protected void GetRuscRes()
    {
        string target = "INPUT_PIPE_INSP";
        int checkCalibr = 1;
        int WorkplaceR = 0;
        UpdateRowID = "";
        if (WorkplaceId == 1) WorkplaceR = 25;
        else if (WorkplaceId == 2) WorkplaceR = 26;
        else if (WorkplaceId == 3) WorkplaceR = 27;
        else if (WorkplaceId == 4) WorkplaceR = 28;
        else if (WorkplaceId == 5) WorkplaceR = 29;
        else if (WorkplaceId == 6) WorkplaceR = 30;

        if (WorkplaceR == 0)
        {
            Master.AlertMessage = "Номер инспекционной решетки допускается в значении с 1 по 6";
            return;
        }

        Session.Add("WorkplaceIDForRusc", WorkplaceR);
        Session.Add("PipeYearForRusc", lblYear.Text);
        Session.Add("PipeNumberForRusc", lblPipeNo.Text);
        Session.Add("SelectedROWForRusc", UpdateRowID);
        Session.Add("BadMarkingPipeNumberForRusc", BadMarkingPipeNumber);
        Session.Add("CheckCalibrForRusc", checkCalibr);
        Session.Add("TargetForRusc", target);
        PopupWindow1.ContentPanelId = pnlInputPipe.ID;
        PopupWindow1.Title = "Ввод данных по испытанию трубы";
        pnlInputPipe.Visible = true;
    }

    protected void btnCanselGeom_Click(object sender, EventArgs e)
    {
        pnlGeomInsp.Visible = false;
    }

    protected void btnCloseInputPipe_Click(object sender, EventArgs e)
    {
        pnlInputPipe.Visible = false;
    }

    /// <summary>Получение кода маршрута для отправки в ПЛК</summary>
    /// <param name="gost">ГОСТ</param>
    /// <param name="wp_id">Номер рабочего места</param>
    /// <param name="diameter">Диаметр</param>
    /// <param name="thickness">Толщина стенки</param>
    /// <param name="length_pipe">Длина</param>
    /// <returns></returns>
    private int GetRouteID(string gost, int wp_id, double diameter, double thickness, int length_pipe, string ntd_group)
    {
        int route_id = 0;
        string s = null;
        try
        {
            if (gost != "")
            {
                using (OleDbCommand cmd = Master.Connect.ORACLE_TESC3().CreateCommand())
                {
                    cmd.CommandText =
                        @"select nvl (min (sir.value), 5) route_id
                            from tesc3.spr_inspection_route sir
                           where sir.is_active = 1 and sir.ntd = ?
                             and case when ? between 1 and 3 then 'Линия 1' when ? between 4 and 6 then 'Линия 2' else ''
                                 end = sir.processing_line
                                 and SIR.DIAMETER=?
                                 and SIR.THICKNESS=?
                                 and ? between SIR.LENGTH1 and SIR.LENGTH2
                                 and SIR.NTD_GROUP ";

                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("gost", gost);
                    cmd.Parameters.AddWithValue("wp_id1", wp_id);
                    cmd.Parameters.AddWithValue("wp_id2", wp_id);
                    cmd.Parameters.AddWithValue("DIAMETER", diameter);
                    cmd.Parameters.AddWithValue("THICKNESS", thickness);
                    cmd.Parameters.AddWithValue("LENGTH_PIPE", length_pipe);

                    if (string.IsNullOrEmpty(ntd_group))
                    {
                        cmd.CommandText = cmd.CommandText + " is null";

                    }
                    else
                    {
                        cmd.CommandText = cmd.CommandText + " =?";
                        cmd.Parameters.AddWithValue("NTD_GROUP", ntd_group);
                    }

                    using (OleDbDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.HasRows)
                        {
                            if (rdr.Read())
                            {
                                if (int.TryParse(rdr["route_id"].ToString(), out route_id))
                                    return route_id;
                            }
                            rdr.Close();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка получения номера для отправки в ПЛК", ex);
        }
        return 5;
    }



    //выбор элемента списка по значению
    public void SelectDDLItemByValue(DropDownList ddl, String val)
    {
        ListItem item = ddl.Items.FindByValue(val);
        ddl.SelectedIndex = ddl.Items.IndexOf(item);
    }

    //выбор элемента списка по тексту
    public void SelectDDLItemByText(DropDownList ddl, String txt)
    {
        ListItem item = ddl.Items.FindByText(txt);
        if (item != null)
            ddl.SelectedIndex = ddl.Items.IndexOf(item);
        else ddl.SelectedIndex = -1;
    }


    //заполнение выпадающих списков из справочников
    protected void FillDropDownLists()
    {
        try
        {
            //заполнение выпадающего списка сотрудников         
            ddlOperatorFIO2.Items.Clear();
            ddlOperatorFIO2.Items.Add("");
            UserInfo[] users = Authentification.GetUsersList(new String[] { "INSP", "PDO" }, false);
            foreach (UserInfo user_info in users)
            {
                if (!String.IsNullOrEmpty(user_info.FIO))
                {
                    ListItem item = new ListItem(user_info.OtkCode.ToString() + " " + user_info.FIO, user_info.UserName);
                    ddlOperatorFIO2.Items.Add(item);
                }
            }

            //заполнение выпадающего списка причин дубликатов бирок
            ddlDuplicateReason.Items.Clear();
            ddlDuplicateReason.Items.Add(new ListItem("", "-1"));
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            using (OleDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "select * from SPR_LABEL_DUPLICATE_REASON where IS_ACTIVE=1 order by REASON_TEXT";
                using (OleDbDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        ddlDuplicateReason.Items.Add(new ListItem(rdr["REASON_TEXT"].ToString(), rdr["ID"].ToString()));
                    }
                }
            }


            //заполнение выпадающего списка дефектов          
            ddlDefectTemplate.Items.Clear();
            ddlDefectTemplate.Items.Add("");
            conn = Master.Connect.ORACLE_TESC3();
            using (OleDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT SD.DEFECT_NAME, SD.ID
						FROM SPR_DEFECT SD
						WHERE SD.DEFECT_AREA in ('На стане','На отделке','После печей')";
                using (OleDbDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        ddlDefectTemplate.Items.Add(new ListItem(rdr["DEFECT_NAME"].ToString(), rdr["ID"].ToString()));
                    }
                }
            }



            //заполнение выпадающего списка НТД          
            ddlNTD.Items.Clear();
            ddlNTD.Items.Add("");
            conn = Master.Connect.ORACLE_ORACLE();
            using (OleDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"select distinct GOST, GRUP
                    from V_T3_PIPE_ITEMS
                    where (ORG_ID=127)and(GOST is NOT NULL)
                    and class not in ('T3_100', 'T3_200') and class is not null
                    order by GOST, GRUP";
                using (OleDbDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        String gost = rdr["GOST"].ToString();
                        String group = rdr["GRUP"].ToString();
                        String txt = gost;
                        if (group != "") txt += " гр. " + group;
                        ddlNTD.Items.Add(txt);
                    }
                }
            }

            //заполнение выпадающего списка марок сталей
            ddlSteelmark.Items.Clear();
            ddlSteelmark.Items.Add("");
            ddlTransportNumberSteelmark.Items.Clear();
            ddlTransportNumberSteelmark.Items.Add("");
            conn = Master.Connect.ORACLE_ORACLE();
            using (OleDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"select distinct stal
                    from V_T3_PIPE_ITEMS
                    where (org_id=127)and(STAL is NOT NULL)
                    and class not in ('T3_100', 'T3_200') and class is not null
                    order by stal";
                using (OleDbDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        ddlSteelmark.Items.Add(new ListItem(rdr["stal"].ToString(), rdr["stal"].ToString()));
                        ddlTransportNumberSteelmark.Items.Add(new ListItem(rdr["stal"].ToString(), rdr["stal"].ToString()));
                    }
                }
            }

            //заполнение выпадающего списка диаметра трубы
            ddlDiam.Items.Clear();
            ddlDiam.Items.Add("");
            ddlTransportNumberDiameter.Items.Clear();
            ddlTransportNumberDiameter.Items.Add("");
            conn = Master.Connect.ORACLE_ORACLE();
            using (OleDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"select distinct diameter
                    from V_T3_PIPE_ITEMS
                    where (org_id=127)
                    and class not in ('T3_100', 'T3_200') and class is not null
                    and diameter>0
                    order by diameter";
                using (OleDbDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        ddlDiam.Items.Add(new ListItem(Convert.ToDouble(rdr["diameter"]).ToString()));
                        ddlTransportNumberDiameter.Items.Add(new ListItem(Convert.ToDouble(rdr["diameter"]).ToString()));
                    }
                }
            }

            //заполнение выпадающего списка типоразмеров профиля трубы
            ddlProfileSize.Items.Clear();
            ddlProfileSize.Items.Add("");
            ddlTransportNumberProfileSize.Items.Clear();
            ddlTransportNumberProfileSize.Items.Add("");
            conn = Master.Connect.ORACLE_ORACLE();
            using (OleDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT DISTINCT s_size1 || 'x' || s_size2 AS profile_size
                                        FROM v_t3_pipe_items
                                       WHERE (org_id = 127) AND class NOT IN ('T3_100', 'T3_200') AND class IS NOT NULL AND s_size1 > 0
                                    ORDER BY s_size1 || 'x' || s_size2";
                using (OleDbDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        ddlProfileSize.Items.Add(new ListItem(rdr["profile_size"].ToString()));
                        ddlTransportNumberProfileSize.Items.Add(new ListItem(rdr["profile_size"].ToString()));
                    }
                }
            }

            //заполнение выпадающего списка толщины стенки
            ddlThickness.Items.Clear();
            ddlThickness.Items.Add("");
            ddlTransportNumberThickness.Items.Clear();
            ddlTransportNumberThickness.Items.Add("");
            conn = Master.Connect.ORACLE_ORACLE();
            using (OleDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"select distinct thickness
                    from V_T3_PIPE_ITEMS
                    where (org_id=127)and(THICKNESS is NOT NULL)
                    and class not in ('T3_100', 'T3_200') and class is not null
                    and thickness>0
                    order by thickness";
                using (OleDbDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        ddlThickness.Items.Add(Convert.ToDouble(rdr["thickness"]).ToString());
                        ddlTransportNumberThickness.Items.Add(Convert.ToDouble(rdr["thickness"]).ToString());
                    }
                }
            }

            //заполнение выпадающего списка номенклатурных номеров
            RebuildInventoryNumberList();

            //заполнение выпадающего списка причин перевода
            ddlNotZakazReason.Items.Clear();
            ddlNotZakazReason.Items.Add("");
            ddlPerevodReason.Items.Clear();
            ddlPerevodReason.Items.Add("");
            conn = Master.Connect.ORACLE_TESC3();
            using (OleDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"select ID, REASON_NAME 
                    from SPR_NOT_ZAKAZ_REASON 
                    order by REASON_NAME";
                using (OleDbDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        ddlNotZakazReason.Items.Add(new ListItem(rdr["REASON_NAME"].ToString(), rdr["ID"].ToString()));
                        ddlPerevodReason.Items.Add(new ListItem(rdr["REASON_NAME"].ToString(), rdr["ID"].ToString()));
                    }
                }
            }

            //заполнение выпадающего списка шаблонов бирки
            ddlLabelType.Items.Clear();
            ddlLabelType.Items.Add("");
            using (OleDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "select id, label_name from spr_label_type where is_active=1 order by id";
                using (OleDbDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        ddlLabelType.Items.Add(new ListItem(rdr["LABEL_NAME"].ToString(), rdr["ID"].ToString()));
                    }
                }
            }

            //заполнение выпадающего списка шаблонов клейма для КМК      
            ddlLabelTypeKmk.Items.Clear();
            ddlLabelTypeKmk.Items.Add("");
            using (OleDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "select template_id, template_name from spr_kmk_marking where is_active=1 order by template_id";
                using (OleDbDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        ddlLabelTypeKmk.Items.Add(new ListItem(rdr["TEMPLATE_NAME"].ToString(), rdr["TEMPLATE_ID"].ToString()));
                    }
                }
            }

            //заполнение выпадающего списка наименования маршрутных карт
            ddlPipeRouteMap.Items.Clear();
            ddlPipeRouteMap.Items.Add("");
            using (OleDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"select distinct map_name from spr_pipes_route_maps where edit_state=0 order by map_name";
                using (OleDbDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        ddlPipeRouteMap.Items.Add(rdr["MAP_NAME"].ToString());
                    }
                }
            }

            //заполнение выпадающего списка "Маршрут с СГП"
            /*
            ddlSgpRoute.Items.Clear();
            ddlSgpRoute.Items.Add("");
            using (OleDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"select distinct ID, NAME from SPR_INSPECTION_ROUTE where IS_ACTIVE=1 order by NAME";
                using (OleDbDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        ddlSgpRoute.Items.Add(new ListItem(rdr["NAME"].ToString(), rdr["ID"].ToString()));
                    }
                }
            }
            */
            //заполнение выпадающего списка рабочих мест потери номера
            ddlTransportNumberWorkplace.Items.Clear();
            ddlTransportNumberWorkplace.Items.Add("");
            using (OleDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "select ID, WORKPLACE_NAME from SPR_WORKPLACES where IS_ACTIVE=1 order by WORKPLACE_NAME";
                using (OleDbDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        ddlTransportNumberWorkplace.Items.Add(new ListItem(rdr["WORKPLACE_NAME"].ToString(), rdr["ID"].ToString()));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка заполнения списков", ex);
        }
    }


    //заполнение списков дефектов на обрезь
    protected void FillPipeDefectsList()
    {
        OleDbConnection conn = Master.Connect.ORACLE_TESC3();

        GetPipeDefectList(conn, new List<DropDownList>() { ddlDefect, ddlDefect2, ddlDefect3 }, false, (cbRepair.Checked || WorkplaceId == 85)); //получение списка дефектов №1
        GetPipeDefectList(conn, new List<DropDownList>() { ddlDefectZachistka }); //получение списка дефектов зачистки
        GetPipeDefectList(conn, new List<DropDownList>() { ddlDefectAdditional }, true); //получение списка дефектов и кодов дефектов не являющихся выводящими длину за пределы НД

        //очистка списков элементов
        ddlsLeftDefects = new List<DropDownList>();
        ddlsRightDefects = new List<DropDownList>();
        txbsLeftDefects = new List<TextBox>();
        txbsRightDefects = new List<TextBox>();

        if (ddlLocationScraping.Items.Count == 0)
        {
            //получение списка справочника «Расположение дефектов»
            ddlLocationScraping.Items.Add(new ListItem("", ""));
            using (OleDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "select location, id from spr_defect_location where is_active = 1 order by 1";
                using (OleDbDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            ddlLocationScraping.Items.Add(new ListItem(reader["LOCATION"].ToString(), reader["ID"].ToString()));
                        }
                        reader.Close();
                    }
                }
            }
        }

        if (ddlResultScraping.Items.Count == 0)
        {
            //получение списка справочника «Заключение после зачистки»
            ddlResultScraping.Items.Add(new ListItem("", ""));
            using (OleDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "select result, id from spr_result_scraping where is_active = 1 order by 1";
                using (OleDbDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            ddlResultScraping.Items.Add(new ListItem(reader["RESULT"].ToString(), reader["ID"].ToString()));
                        }
                        reader.Close();
                    }
                }
            }
        }

        if (WorkplaceId == 85)
        {
            if (ddlResultPrestar.Items.Count == 0)
            {
                //получение списка Prestar
                ddlResultPrestar.Items.Add(new ListItem("", ""));
                using (OleDbCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText =
                        @"SELECT ID, PIPE_ID_ASUTP ||' / ' || TO_CHAR(REC_DATE, 'DD.MM.YYYY hh24:mi:ss') || ' / ' || PIPE_NUMBER AS RESULT FROM (
                    SELECT ID, PIPE_ID_ASUTP, PIPE_ID, ENTER_DATE, PIPE_NUMBER, REC_DATE FROM TESC3.STORE_PRESTAR ORDER BY REC_DATE DESC)
                    WHERE ROWNUM <= 15   ";

                    using (OleDbDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                ddlResultPrestar.Items.Add(
                                    new ListItem(reader["RESULT"].ToString(), reader["ID"].ToString()));
                                selectPrestar = reader["ID"].ToString();
                            }

                            reader.Close();
                        }
                    }
                }
            }


        }
    }


    /// <summary>Заполнение выпадающего списка дефектов</summary>
    /// <param name="conn">Подключение к БД</param>
    /// <param name="ddl">Выпадающие списки</param>
    /// <param name="is_reduce_length">Признак дефектов не являющихся выводящими длину за пределы НД</param>
    /// <param name="is_zachistka_enabled">Признак ремонта зачисткой</param>
    protected void GetPipeDefectList(OleDbConnection conn, List<DropDownList> ddl_list, bool is_reduce_length = false, bool is_zachistka_enabled = false)
    {
        //получение списка дефектов и кодов        
        List<String> defIDs = new List<String>();
        List<String> defNames = new List<String>();

        using (OleDbCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"SELECT sd.defect_area, sd.defect_name, sd.id FROM tesc3.spr_defect sd WHERE sd.is_active = 1" +
                                    (is_reduce_length ? " AND sd.is_reduce_length = 0" : "") +
                                    (is_zachistka_enabled ? " AND sd.zachistka_enabled = 1" : "") +
                                " ORDER BY sd.defect_area, sd.id, sd.defect_name";
            using (OleDbDataReader reader = cmd.ExecuteReader())
            {
                if (reader.HasRows)
                {
                    String prevArea = "";
                    while (reader.Read())
                    {
                        String defect_area = reader["DEFECT_AREA"].ToString();
                        String defect_name = reader["DEFECT_NAME"].ToString();
                        String id = reader["ID"].ToString();
                        if (defect_area != prevArea)
                        {
                            defIDs.Add(" ");
                            defNames.Add(" ");
                            defIDs.Add(" ");
                            defNames.Add(defect_area);
                        }
                        defIDs.Add(id);
                        defNames.Add("   - " + defect_name);
                        prevArea = defect_area;
                    }
                    reader.Close();
                }
            }
        }

        //построение выпадающего списка дефектов
        foreach (DropDownList ddl in ddl_list)
        {
            string old_value = ddl.SelectedIndex > 0 ? ddl.SelectedItem.Value : "";
            ddl.Items.Clear();
            for (int c = 0; c < defIDs.Count; c++)
            {
                ddl.Items.Add(new ListItem(defNames[c], defIDs[c]));
            }
            SelectDDLItemByValue(ddl, old_value);
        }
    }


    protected void cbRepair_CheckedChanged(object sender, EventArgs e)
    {
        try
        {
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            GetPipeDefectList(conn, new List<DropDownList>() { ddlDefect, ddlDefect2, ddlDefect3 }, false, cbRepair.Checked); //получение списка дефектов
            pnlDefect2.Visible = pnlDefect3.Visible = cbRepair.Checked;
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка при отметке требуется ремонт зачисткой", ex);
        }
    }


    //заполнение списка дефектов текущей трубы
    protected void FillDefectsList()
    {
        //список дефектов
        String[] def ={"Остановка стана",
                       "Наружный дефект УЗК",
                       "Температура сварки ниже нормы",
                       "Температура сварки выше нормы",
                       "Внутренний дефект УЗК",
                       "Потеря акустического контакта УЗК",
                       "Скорость снижена",
                       "Скорость увеличина",
                       "Внутренний грат",
                       "Наружный грат",
                       "Дефект УЗК шва",
                       "Температура зоны 1 ЛТО ниже допуска",
                       "Температура зоны 1 ЛТО выше допуска",
                       "Температура зоны 2 ЛТО ниже допуска",
                       "Температура зоны 2 ЛТО выше допуска",
                       "Стык поперечный",
                       "Мощность увеличена",
                       "Мощность снижена",
                       "Индекс мощности снижен",
                       "Дефект левой кромки",
                       "Дефект правой кромки",
                       "Отсутствие АК левой кромки",
                       "Отсутствие АК правой кромки",
                       "Изменение ЛК",
                       "Изменение ПК"
                      };
        tblDefectsList.Rows.Clear();

        //получение битовой маски дефектов
        int defmap = 0;
        try
        {
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "select defects from optimal_pipes where pipe_number=?";
            cmd.Parameters.AddWithValue("pipe_number", PipeNumber);
            OleDbDataReader reader = cmd.ExecuteReader();
            if (reader.Read())
                int.TryParse(Convert.ToString(reader["DEFECTS"]), out defmap);
            reader.Close();
            reader.Dispose();
            cmd.Dispose();
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка получения списка дефектов текущей трубы (FillDefectsList)", ex);
        }

        //предварительный подсчет кол-ва дефектов
        int defcount = 0;
        for (int i = 0; i < def.Length; i++)
        {
            if (((defmap >> i) & 0x01) != 0) defcount++;
        }

        //сообщение при отсутствии дефектов
        if (defcount == 0)
        {
            TableRow row = new TableRow();
            row.Cells.Add(new TableCell());
            row.Cells[0].Text = "Дефекты отсутствуют";
            tblDefectsList.Rows.Add(row);
        }

        //заполнение таблицы дефектами в два столбца
        try
        {
            int r = 0;
            for (int i = 0; i < def.Length; i++)
            {
                if (((defmap >> i) & 0x01) != 0)
                {
                    TableCell cell = new TableCell();
                    int d1 = defcount / 2;
                    if (r <= d1)
                    {
                        cell.Style[HtmlTextWriterStyle.PaddingRight] = "10px";
                        TableRow row = new TableRow();
                        row.Cells.Add(cell);
                        tblDefectsList.Rows.Add(row);
                    }
                    else
                    {
                        cell = new TableCell();
                        int c = r - d1;
                        tblDefectsList.Rows[c].Cells.Add(cell);
                    }
                    cell.Text = "- " + def[i];
                    cell.Style[HtmlTextWriterStyle.PaddingRight] = "16px";
                    r++;
                }
            }
        }
        catch
        { };
    }


    /// <summary>
    /// Получение контрольной цифры по нажатию кнопки "?"
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void lblHelpCheck_Click(object sender, EventArgs e)
    {
        if (PipeNumber != 0)
        {
            MainMultiView.SetActiveView(vControlDigit);
            txbLogin.Text = "";
            txbPassword.Text = "";
            lblNumtrub.Text = PipeNumber.ToString();
        }
        else Master.AlertMessage = "Необходимо заполнить поля год или № трубы";
    }


    //проверка соответствия номера и контрольной цифры
    protected bool CheckPipeNumber()
    {
        double dYear, dPipeNumber, dCheck;
        txbPipeNumber.Text = txbPipeNumber.Text.Trim().PadLeft(6, '0');
        Check = Convert.ToInt32(txbCheck.Text);

        //if (IsPdoForm) return true;

        bool boolYear = Checking.Validation_Class(txbYear.Text, 2, 0, out dYear);
        bool boolPipeNumber = Checking.Validation_Class(txbPipeNumber.Text, 6, 0, out dPipeNumber);
        bool boolCheck = Checking.Validation_Class(txbCheck.Text, 1, 0, out dCheck);
        int Check1 = (int)dCheck;

        if (boolYear && boolPipeNumber && boolCheck)
        {
            int ValidCheck = Checking.Check_Class(PipeNumber.ToString());
            return (ValidCheck == Check1);
        }
        else return false;
    }


    //События по нажатию на кнопку 'Ввести параметры'
    protected void btnOk_Click(object sender, EventArgs e)
    {
        UpdateRowID = "";
        if (sender == btnHistoryOk)
            MainMultiView.SetActiveView(FindPipesView);
        //очистка списка предупреждений
        ClearWarnings();

        //очистка списков
        ddlDefectTemplate.SelectedIndex = 0;
        ddlResultTemplate.SelectedIndex = 0;


        //проверка номера и контрольной цифры
        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 2) && CheckPipeNumber() == false)
        {
            Master.AlertMessage = "Неверно указан номер или контрольная цифра";
            txbPipeNumber.Text = "";
            txbCheck.Text = "";
            return;
        }

        //проверка условия, что номер не является транспортировочным
        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 3) && Checking.CheckIsTransportNumber(PipeNumber))
        {
            Master.AlertMessage = "Транспортировочный номер трубы " + PipeNumber.ToString() + " запрещен для ввода в данную форму. Необходимо присвоить резервный или настоящий номер трубы.";
            return;
        }

        //проверка возможности ввода по трубе согласно маршруту
        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 4) && !CheckValidCurrentPositionOfPipe(false))
        {
            //получение текста сообщения о несоответствии маршрута            
            List<String> msgs = new List<string>();
            if (!Checking.CheckValidCurrentPositionOfPipe(PipeNumber, WorkplaceId, ref msgs))
            {
                String messageText = "";
                foreach (String msg in msgs)
                    messageText += msg + ". ";

                //отображение окна ввода причины отклонения от маршрута
                lblRouteRejectMessage.Text = messageText;
                txbRouteRejectReason.Text = "";
                PopupWindow1.ContentPanelId = pnlMasterConfirm.ID;
                PopupWindow1.Title = "Отклонение от маршрута";
                PopupWindow1.MoveToCenter();
                pnlMasterConfirm.Visible = true;

                return;
            }
        }

        //Проверка сортаментов предыдущей и текущей труб
        if (cbAutoCampaign.Checked) if(!CheckSortLastPipe()) cbAutoCampaign.Checked = false;

        //обновление списка кампаний
        RebuildCampaignList();

        //включение выбора из строки кампании
        if (WorkplaceId != 7)
        {
            cbShowCampaigns.Checked = true;
            cbShowCampaigns_CheckedChanged(cbShowCampaigns, e);
        }

        //получение данных о трубе и переключение вида
        GetPipeInfo(PipeNumber.ToString());

        //отображение истории трубы, если она после ремонта
        if ((PipeFromRepair | PipeFromInspection) & (sender != btnHistoryOk))
        {
            btnShowHistory_Click(sender, e);
            return;
        }

        //запоминание номера трубы как текущего
        Checking.SaveLastPipeNumber(txbYear.Text, txbPipeNumber.Text, txbCheck.Text);

        //получение массы трубы, если рабочее место = инспекции КЛО        
        if (WorkplaceId >= 4 & WorkplaceId <= 6)
        {
            SetWeightText(GetWeightFromIp21());
        }
        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 5))
        {
            CheckPipeSampling();  // проверка на отбор проб для трубы, назначенной на испытания
        }
        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 6))
        {
            CheckSetSamplingForPart(); // проверка назначения на отбор проб для партии трубы               
        }

        //переключение вида на ввод данных трубы
        TabsVisible = false;
        ClearInputFields();

        // установка по умолчанию дефекта "проба механическая", если труба назначена на пробы
        bool IsSamplingPipe = false;
        CheckPipeSampling(out IsSamplingPipe);
        if (IsSamplingPipe)
        {
            ListItem item = ddlDefect.Items.FindByValue("365");
            if (item != null) ddlDefect.SelectedIndex = ddlDefect.Items.IndexOf(item);
        }

        MainMultiView.SetActiveView(InputDataView);
        lblPipeNo.Text = txbPipeNumber.Text;
        lblYear.Text = txbYear.Text;
        ddlNTD_SelectedIndexChanged(sender, e);
        GetTestResults();

        if (WorkplaceId >= 80 && WorkplaceId <= 84)
            SelectDDLItemByValue(ddlZachistkaEnabled, "Не производился");

        if (WorkplaceId == 85)
        {
            SelectDDLItemByValue(ddlZachistkaEnabled, "Не производился");
            if (!string.IsNullOrEmpty(selectPrestar)) SelectDDLItemByValue(ddlResultPrestar, selectPrestar);
        }

        //автоматический выбор подходящей по сортаменту строки кампании
        if (WorkplaceId == 80)
        {
            SelectNearestCampaign();
            ddlCampaign_SelectedIndexChanged(ddlCampaign, EventArgs.Empty);
        }

        //выбор принтера на инспекионных решетках 2 пролета если участок ремонта
        if (WorkplaceId == 7)
        {
            if (ddlPrinter.Items.Count > 1)
                ddlPrinter.SelectedIndex = 0;
        }

        //автоматический выбор дефекта для резервных номеров труб
        SelectReserveNumberDefect();

        //сброс флага "данные сохранены"
        DataSaved = false;

        //управление активностью поля качество КП
        ddlQualityKP.Enabled = CheckTransitKP();

        if (WorkplaceId == 85)
        {
            //получение дефектов при отправке на зачистку
            int[] defect_by_zachistka = GetDefectByZachistka();
            if (defect_by_zachistka[0] > 0)
                SelectDDLItemByValue(ddlDefect, defect_by_zachistka[0].ToString());
            if (defect_by_zachistka[1] > 0)
                SelectDDLItemByValue(ddlDefect2, defect_by_zachistka[1].ToString());
            if (defect_by_zachistka[2] > 0)
                SelectDDLItemByValue(ddlDefect3, defect_by_zachistka[2].ToString());
        }

        //если не нужные нам участки то принудительно прячем
        if (!((WorkplaceId >= 1 && WorkplaceId <= 6) || WorkplaceId == 85))
        {
            SetVisiblePlateTemplating(false);
        }
        ShowInfoAboutTemplating.Visible = WorkplaceId >= 1 && WorkplaceId <= 6;


    }


    /// <summary>Получение списка дефектов при отправке на зачистку</summary>
    /// <returns></returns>
    protected int[] GetDefectByZachistka()
    {
        int[] defect_ids = new int[3];
        for (int i = 0; i < 3; i++)
            defect_ids[i] = -1;
        try
        {
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            using (OleDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT NVL (ip.cut_left_defects, -1) defect_id_1,
                                           NVL (defect_id_2, -1) defect_id_2,
                                           NVL (defect_id_3, -1) defect_id_3
                                      FROM tesc3.inspection_pipes ip
                                     WHERE     ip.edit_state = 0
                                           AND ip.pipe_number = ?
                                           AND ip.next_direction = 'REMONT'
                                           AND ip.zachistka_checkbox = 'Y'
                                           AND ip.trx_date =
                                                  (SELECT MAX (ip_.trx_date)
                                                     FROM tesc3.inspection_pipes ip_
                                                    WHERE ip_.edit_state = 0 AND ip_.pipe_number = ip.pipe_number)";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("pipe_number", PipeNumber);
                using (OleDbDataReader rdr = cmd.ExecuteReader())
                {
                    if (rdr.HasRows)
                    {
                        if (rdr.Read())
                        {
                            int def_id_1 = -1; int.TryParse(rdr["defect_id_1"].ToString(), out def_id_1); defect_ids[0] = def_id_1;
                            int def_id_2 = -1; int.TryParse(rdr["defect_id_2"].ToString(), out def_id_2); defect_ids[1] = def_id_2;
                            int def_id_3 = -1; int.TryParse(rdr["defect_id_3"].ToString(), out def_id_3); defect_ids[2] = def_id_3;
                        }
                        rdr.Close();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка получения дефектов", ex);
        }
        return defect_ids;
    }


    /// <summary>
    /// Получение причины и заполнение поля "дефект" для труб с резервным номером
    /// </summary>
    private void SelectReserveNumberDefect()
    {
        OleDbConnection conn = Master.Connect.ORACLE_TESC3();

        using (OleDbCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"select max(ID_DEFECT) from RESERVE_NUMBERS where EDIT_STATE=0 and PIPE_NUMBER=?";
            cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);
            object o = cmd.ExecuteScalar();

            if (o != DBNull.Value)
                SelectDDLItemByValue(ddlDefect, o.ToString());
        }
    }


    /// <summary>
    /// Выбор наиболее подходящей строки кампании по совпадению Диаметра, марки стали, НТД, Толщины и уровня исполнения
    /// </summary>
    private void SelectNearestCampaign()
    {
        ddlCampaign.SelectedIndex = 0;

        //сортамент трубы с участка стана
        OleDbConnection conn = Master.Connect.ORACLE_TESC3();
        using (OleDbCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"select gc.campaign_line_id, pi.diameter, pi.thickness, pi.stal, pi.gost||' '||pi.grup gost, PI.LEVEL_QLT
                from optimal_pipes op
                join geometry_coils_sklad gc
                    on op.coil_pipepart_year=gc.coil_pipepart_year
                    and op.coil_pipepart_no=gc.coil_pipepart_no
                    and op.coil_internalno=gc.coil_run_no
                    and gc.edit_state=0
                join campaigns cmp
                    on gc.campaign_line_id=cmp.campaign_line_id
                    and cmp.edit_state=0
                join oracle.v_t3_pipe_items pi
                    on cmp.inventory_code=pi.nomer       
                where op.pipe_number=?";

            cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);

            using (OleDbDataReader rdr = cmd.ExecuteReader())
            {
                if (rdr.Read())
                {
                    //попытка поиска по строке кампании
                    String campaignLineId = rdr["CAMPAIGN_LINE_ID"].ToString();
                    SelectDDLItemByValue(ddlCampaign, campaignLineId);
                    if (ddlCampaign.SelectedIndex > 0)
                        return;

                    String diameter = rdr["DIAMETER"].ToString();
                    String thickness = rdr["THICKNESS"].ToString();
                    String steelmark = rdr["STAL"].ToString();
                    String gost = rdr["GOST"].ToString();
                    String level = rdr["LEVEL_QLT"].ToString();

                    //если по строке кампании не найдено точного совпадения, то поиск подходящего элемента по совпадению сортамента
                    foreach (ListItem item in ddlCampaign.Items)
                    {
                        if (item.Value != "")
                        {
                            //получение сортамента из строки кампании
                            using (OleDbCommand cmdCampaign = conn.CreateCommand())
                            {
                                cmdCampaign.CommandText = @"select pi.diameter, pi.thickness, pi.stal, pi.gost||' '||pi.grup gost, PI.LEVEL_QLT
                                    from campaigns cmp
                                    join oracle.v_t3_pipe_items pi
                                        on cmp.inventory_code=pi.nomer
                                    where cmp.edit_state=0
                                        and cmp.campaign_line_id=?";

                                cmdCampaign.Parameters.AddWithValue("CAMPAIGN_LINE_ID", item.Value);
                                using (OleDbDataReader rdrCampaign = cmdCampaign.ExecuteReader())
                                {
                                    if (rdrCampaign.Read())
                                    {
                                        String cmpDiameter = rdrCampaign["DIAMETER"].ToString();
                                        String cmpThickness = rdrCampaign["THICKNESS"].ToString();
                                        String cmpSteelmark = rdrCampaign["STAL"].ToString();
                                        String cmpGost = rdrCampaign["GOST"].ToString();
                                        String cmpLevel = rdrCampaign["LEVEL_QLT"].ToString();

                                        //выбор элемента при полном совпадении сортамента и возврат
                                        if (cmpDiameter == diameter && cmpThickness == thickness && cmpSteelmark == steelmark && cmpGost == gost && cmpLevel == level)
                                        {
                                            SelectDDLItemByValue(ddlCampaign, item.Value);
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }


    //построение списка кампаний
    private void RebuildCampaignList()
    {
        try
        {

            //подключение к БД            
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();

            string pipeNum = "", campaign="";

            //выбирать кампанию по последней осмотренной трубе
            if (cbAutoCampaign.Checked)
            {
                //выборка строки задания на компанию только тех объектов, которые имеют одинаковую марку стали , диаметр и стенку с выбранной трубой/ (по последней записи)
                String Sort = @"select distinct pipe_number, campaign_line_id
                            from inspection_pipes
                            where edit_state = 0
                            and auto_campaign_flag = 1
                            and workplace_id = ?
                            and trx_date = (select max(trx_date)
                                            from inspection_pipes
                                            where edit_state = 0
                                            and workplace_id = ?
                                            and auto_campaign_flag = 1)";
                cmd.CommandText = Sort;
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("WORKPLACE_ID", WorkplaceId);
                cmd.Parameters.AddWithValue("WORKPLACE_ID1", WorkplaceId);
                using (OleDbDataReader rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        pipeNum = rdr["PIPE_NUMBER"].ToString();
                        campaign = rdr["CAMPAIGN_LINE_ID"].ToString();
                    }
                }
            }
            //выбирать кампанию с рулона, если не выбран автоматический режим по последней трубе
            else
            {
                cmd.CommandText = @"SELECT distinct campaign_line_id
                                    FROM optimal_pipes op
                                        LEFT JOIN geometry_coils_sklad gc ON (op.coil_pipepart_no = gc.coil_pipepart_no)
                                            AND (op.coil_pipepart_year = gc.coil_pipepart_year)
                                    WHERE op.pipe_number = ?
                                        AND (gc.edit_state = 0 OR gc.edit_state IS NULL)";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);
                campaign = cmd.ExecuteScalar().ToString();
            }

            //запоминание выбранного элемента списка
            String OldCampaign = (campaign != "") ? campaign : ddlCampaign.SelectedItem.Value;

            //очистка старых значений
            ddlCampaign.Items.Clear();
            ddlCampaign.Items.Add(new ListItem(""));


            //выборка информации по кампаниям
            String SQL = @"SELECT DISTINCT  cm.campaign_line_id, cm.rec_date, v_t3.diameter, v_t3.s_size1, v_t3.s_size2, v_t3.thickness, v_t3.gost,
                            v_t3.grup, v_t3.stal, cm.campaign_date, cm.order_line, cm.order_header, cm.inventory_code,
                            cm.additional_text, cm.inspection, v_t3.d_ur_isp, pipe_number
                            FROM campaigns cm
                                JOIN (select v_3.*, pipe_number from oracle.v_t3_pipe_items v_3
                                RIGHT JOIN (select sm.d_grade grade, sm.s_thickness_pipe thickness, sm.s_diam diam, op.pipe_number
                                    from optimal_pipes op
                                    join geometry_coils_sklad gc
                                    on op.coil_pipepart_year=gc.coil_pipepart_year
                                        and op.coil_pipepart_no=gc.coil_pipepart_no
                                        and op.coil_internalno=gc.coil_run_no
                                        and gc.edit_state=0
                                    join campaigns cmp
                                    on gc.campaign_line_id=cmp.campaign_line_id
                                        and cmp.edit_state=0
                                    join oracle.z_spr_materials sm
                                    on cmp.inventory_code = sm.matnr
                                    where op.pipe_number = ?) sm
                                on ((v_3.thickness <= sm.thickness + 0.5) and (v_3.thickness >= sm.thickness - 0.5))
                                and v_3.stal = sm.grade
                                and ((v_3.diameter <= diameter + 0.5) and (v_3.diameter >= diameter - 0.5)))  v_t3
                                ON cm.inventory_code = v_t3.nomer
                            WHERE (cm.end_date IS NULL) AND (cm.visible = 1) AND cm.edit_state = 0";

            if (WorkplaceId == 63)
                SQL += " and DIAMETER is NULL";
            SQL += " order by REC_DATE";
            cmd.CommandText = SQL;
            cmd.Parameters.Clear();
            if (cbAutoCampaign.Checked) cmd.Parameters.AddWithValue("PIPE_NUMBER", pipeNum);
            else cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);

            OleDbDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                String gost = reader["GOST"].ToString();
                String group = reader["GRUP"].ToString();
                if (group != "") gost += " гр. " + group;
                String date = Convert.ToDateTime(reader["CAMPAIGN_DATE"]).ToString("dd.MM.yy");

                String sort = "";
                //типоразмер обычной трубы
                if (reader["DIAMETER"].ToString() != "") sort = reader["DIAMETER"] + "x" + reader["THICKNESS"];
                //типоразмер профильной трубы
                else sort = reader["S_SIZE1"] + "x" + reader["S_SIZE2"] + "x" + reader["THICKNESS"];

                String steel = reader["STAL"].ToString();
                String nom = reader["INVENTORY_CODE"].ToString().TrimStart(new char[] { '0' });
                String zakaz = reader["ORDER_HEADER"] + "/" + reader["ORDER_LINE"];
                String additional = reader["ADDITIONAL_TEXT"].ToString();
                String inspection = reader["INSPECTION"].ToString();

                String ur_isp = reader["D_UR_ISP"].ToString();

                String txt = date.PadRight(10, '_') + sort.PadRight(13, '_') + steel.PadRight(17, '_') + nom + "__" + zakaz
                             + "__" + gost + "__" + ur_isp + "__" + additional + "__" + inspection.ToUpper();

                char[] trimChars = { '_' };
                String lineID = reader["CAMPAIGN_LINE_ID"].ToString();
                ddlCampaign.Items.Add(new ListItem(txt.Trim(trimChars), lineID));
                ddlCampaign.Items.Add("");
            }
            reader.Close();
            reader.Dispose();
            cmd.Dispose();

            //восстановление выбранного элемента списка кампаний
            ListItem item = ddlCampaign.Items.FindByValue(OldCampaign);
            campaign = "";
            if (item != null)
            {
                ddlCampaign.SelectedIndex = ddlCampaign.Items.IndexOf(item);
                hfInventoryNumberKP.Value = GetCampaignValue(ddlCampaign.SelectedItem.Value, "INVENTORY_CODE_KP");
            }
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка получения списка кампаний", ex);
        }
    }


    //Событие по нажатию кнопки  "Ок" - принять логин
    protected void btnLogin_Click(object sender, EventArgs e)
    {
        String pass = txbPassword.Text;
        if (((pass == "aspenadmin") | (pass == "12345678")) & txbLogin.Text != "")
        {
            txbPipeNumber.Text = txbPipeNumber.Text.Trim().PadLeft(6, '0');
            txbCheck.Text = Checking.Check_Class(PipeNumber.ToString()).ToString();
            BadMarkingPipeNumber = PipeNumber;
            MainMultiView.SetActiveView(FindPipesView);
        }
        else Master.AddErrorMessage("Неверное имя пользователя или пароль");
    }

    //Событие по нажатию кнопки  "Отмена" - передумал логиниться :)
    protected void btnCansel_Click(object sender, EventArgs e)
    {
        MainMultiView.SetActiveView(FindPipesView);
    }


    //проверка, является ли номенклатура некондицией
    //возвращает true, если номенклатура является некондицией
    protected bool CheckNoCondition(String InventoryCode)
    {
        OleDbConnection conn = Master.Connect.ORACLE_ORACLE();
        OleDbCommand cmd = conn.CreateCommand();
        cmd.CommandText = "select INSTR(description, 'НЕКОНДИЦИЯ') POS from V_T3_PIPE_ITEMS where (NOMER=?)and(ORG_ID=127)";
        cmd.Parameters.AddWithValue("INV_CODE", InventoryCode);
        OleDbDataReader rdr = cmd.ExecuteReader();

        bool res = false;
        if (rdr.Read())
            res = (Convert.ToInt32(rdr["POS"]) != 0);

        rdr.Close();
        rdr.Dispose();
        cmd.Dispose();
        return res;
    }



    //отмена ввода данных и переход ко вводу новой трубы
    protected void btnNewPipe_Click(object sender, EventArgs e)
    {
        //очистка полей и переключение вида
        ClearInputFields();
        txbWeight.Text = "";
        lblWeight.Text = "";
        lblWeightTemp.Text = "";
        txbWeight.BackColor = Color.White;
        txbPipeNumber.Text = "";
        txbCheck.Text = "";
        MainMultiView.ActiveViewIndex = 0;
        TabsVisible = true;
        btnSetInputDataTab_Click(sender, e);
        txbPipeNumber.Focus();
        SelectDDLItemByText(ddlDuplicateReason, "");
        fldWarningReturnButtonId.Value = "";
        fldMasterConfirmLogin.Value = "";
        lblMasterConfirmFio.Text = "";
        lblObrlenght.Text = "";
        lblCount_Return.Text = "";
        //сброс флага "данные сохранены"
        DataSaved = false;
    }


    //очистка всех полей ввода на форме
    protected void ClearInputFields()
    {
        ddlDefect.SelectedIndex = -1;
        ddlDefect2.SelectedIndex = -1;
        ddlDefect3.SelectedIndex = -1;
        ddlDefectAdditional.SelectedIndex = -1;
        ddlDefectZachistka.SelectedIndex = -1;
        txbZachistkaLength.Text = "";
        txbZachistkaThickness1.Text = "";
        txbZachistkaThickness2.Text = "";
        ddlZachistkaEnabled.SelectedIndex = 0;
        ddlDeltaSize.SelectedIndex = 0;
        ddlDeltaSize2.SelectedIndex = 0;
        ddlDeltaSize3.SelectedIndex = 0;
        ddlNotZakazReason.SelectedIndex = 0;
        txbNotZakazReasonDescription.Text = "";
        txbNotZakazReasonDistance.Text = "";
        txbNotZakazReasonValue.Text = "";
        fldNotZakazCheckOk.Value = "";
        fldWarningReturnButtonId.Value = "";
        //ddlSgpRoute.SelectedIndex = 0;
        ddlLocationScraping.SelectedIndex = -1;
        ddlResultScraping.SelectedIndex = -1;
        ddlResultPrestar.SelectedIndex = -1;
        cbRepair.Checked = false;
        cbScraping.Checked = false;
        txbAmounts.Text = "";
        txbMinWallWeld.Text = "";
        txbMinWallPipe.Text = "";
        txbMinWallWeld.Enabled = true;
        txbMinWallPipe.Enabled = true;
        txbDefectDescription.Text = "";
        txbDefectDescription2.Text = "";
        txbDefectDescription3.Text = "";
        ddlDefectLocation.SelectedIndex = -1;
        ddlDefectLocation2.SelectedIndex = -1;
        ddlDefectLocation3.SelectedIndex = -1;
        txbDefectDistance.Text = "";
        txbDefectDistance2.Text = "";
        txbDefectDistance3.Text = "";
    }


    //очистка элементов выбора дефектов на обрезь
    protected void ClearCuttingOptions(Table tbl)
    {
        foreach (Control ctrl in tbl.Controls)
        {
            if (ctrl is TableRow)
            {
                TableRow row = (ctrl as TableRow);
                TableCell cell = row.Cells[1];
                TextBox txb = null;
                DropDownList ddlBox = null;
                foreach (Control ctrl1 in cell.Controls)
                {
                    if (ctrl1 is TextBox) txb = (ctrl1 as TextBox);
                }
                cell = row.Cells[2];
                foreach (Control ctrl1 in cell.Controls)
                {
                    if (ctrl1 is DropDownList) ddlBox = (ctrl1 as DropDownList);
                }
                if ((txb != null) & (ddlBox != null))
                {
                    txb.Text = "";
                    ddlBox.SelectedIndex = -1;
                }
            }
        }
    }


    //печать бирки по нажатию кнопки с экрана ввода номера
    protected void btnPrintLabel2_Click(object sender, EventArgs e)
    {
        //txbPipeNumber.Text = txbPipeNumber.Text.Trim().PadLeft(6, '0');
        //PrintLabel(PipeNumber, false, true, true);

        try
        {
            txbPipeNumber.Text = txbPipeNumber.Text.Trim().PadLeft(6, '0');

            if (PrintLabel(PipeNumber, cbWeight.Checked, cbInch.Checked, cbKGtoFunt.Checked))
                btnNewPipe_Click(sender, e);
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка печати бирки", ex);
        }
    }


    //сохранение трубы на склад и печать бирки после ввода параметров
    protected void btnToSklad_Click(object sender, EventArgs e)
    {

        //если инспекция 1-6 то проверяем результат шаблонирования
        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 76) && ((WorkplaceId >= 1 && WorkplaceId <= 6) || WorkplaceId == 85))
        {
            if (ddlResultTemplate.SelectedIndex == 1)
            {
                if (!IsTemplate(PipeNumber))
                {
                    ddlResultTemplate.SelectedIndex = 2;
                }
            }
            if (ddlResultTemplate.SelectedIndex == 0)
            {
                Master.AlertMessage = ("Необходимо выбрать результат шаблонирования");
                return;
            }
            if (ddlResultTemplate.SelectedIndex == 2)
            {
                Master.AlertMessage = ("Шаблонирование не пройдено, запись не возможна");
                return;
            }
        }

        ddlNTD_SelectedIndexChanged(sender, e);

        //при приемке трубы на строку кампании - маршрутная карта задается из кампании
        if (cbShowCampaigns.Checked && ddlCampaign.SelectedItem.Value != "")
        {
            int campaignLineId = Convert.ToInt32(ddlCampaign.SelectedItem.Value);
            String routeMap = GetCampaignPipeRouteMap(campaignLineId);
            SelectDDLItemByValue(ddlPipeRouteMap, routeMap);
        }
        if (cbShowCampaigns.Checked && ddlCampaign.SelectedItem.Value == "")
        {
            SelectDDLItemByValue(ddlPipeRouteMap, "");
        }


        //НД и группа
        String GOST = "";
        String GROUP = "";
        GetNDAndGroup(out GOST, out GROUP, ddlNTD);


        //значение отправляемое в ПЛК
        int route_sgp_id = 0, length_pipe = 0;
        double diameter = 0, thickness = 0;
        double.TryParse(ddlDiam.Text, out diameter);
        double.TryParse(ddlThickness.Text, out thickness);
        int.TryParse(txbLength.Text, out length_pipe);

        route_sgp_id = GetRouteID(GOST, WorkplaceId, diameter, thickness, length_pipe, GROUP);

        // 69
        string s_insp_roll = "";
        string s_insp_pipe = "";
        bool b69 = false;
        bool b70 = false;
        bool b77 = false;
        string list_def_b77 = "";

        #region *** Общие параметры для проверок на заперт и предупреждение *******
        bool is_kp_need = hfInventoryNumberKP.Value != ""; //необходимость консервационного покрытия
        bool is_kp_fact = CheckTransitKP(); //прохождение консервационного покрытия
        #endregion *** Общие параметры для проверок на заперт и предупреждение *******

        #region *** Проверки на запрет ******


        // Зачистка внутренней поверхности
        if (WorkplaceId == 85 && CheckScrapingFields())
            return;

        //проверка толщин после зачистки 
        if (WorkplaceId == 85 && !((GOST == "ТС 153-21-2007") || (GOST == "ГОСТ 10705-80" && GROUP == "Д")))
        {
            if (CheckScrapingThickness())
                return;
        }

        //проверка "Запрет приема на склад труб с названием внешней инспекции отличающейся от внешней инспекции заданной на рулон"
        if (((WorkplaceId >= 1 && WorkplaceId <= 6) || WorkplaceId == 80) && !(GOST == "ТС 153-21-2007" || (GOST == "ГОСТ 10705-80" && GROUP == "Д")))
            if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 69))
            {
                b69 = CheckPipeInspections(out s_insp_roll, out s_insp_pipe);
                if ((!b69) && !IsPdoForm)
                    return;
            }

        //проверка дефектов "На отгрузке"
        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 65) && CheckDefectShipment())
        {
            return;
        }

        //проверка дефектов ОТО
        if (!CheckDefectOTO(GOST, GROUP))
        {
            return;
        }

        //проверка интервала редактирования записи с момента последнего сохранения
        if (UpdateRowID != "")
        {
            if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 7) && !CheckCanEditRecByTime(UpdateRowID))
                return;
        }

        // Проверка наличия задания на компанию
        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 8) && BlockNotCampainAccept)
        {
            if (cbShowCampaigns.Checked == false)
            {
                Master.AlertMessage = "Необходимо в Данных заказа указать: Получать из строки задания на кампанию";
                return;
            }
            if (cbShowCampaigns.Checked == true && ddlCampaign.SelectedItem.Text.Length == 0)
            {
                Master.AlertMessage = "Необходимо выбрать Строку задания на кампанию";
                return;
            }
        }

        //проверка типа бирки
        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 9) && ddlLabelType.SelectedItem.Value == "")
        {
            Master.AlertMessage = "Необходимо указать тип бирки";
            return;
        }
        if (WorkplaceId >= 1 && WorkplaceId <= 7 && ddlLabelTypeKmk.SelectedItem.Value == "")
        {
            Master.AlertMessage = "Необходимо указать тип клейма для КМК";
            return;
        }

        //проверка номенклатуры
        String InvCode = Checking.CorrectInventoryNumber(txbInventoryNumber.Text);
        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 10) && InvCode == "")
        {
            Master.AlertMessage = "Не указан номер номенклатурной позиции. Необходимо выбрать строку задания на кампанию или ввести номенклатурный номер вручную.";
            return;
        }


        //проверка на уровень исполнения и НД
        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 67) && CheckUrIspGroup(InvCode))
        {
            Master.AlertMessage = "Отсутствуют данные по уровню исполнения и/или группе труб для печати этикетки и приемки на СГП.";
            return;
        }

        //проверка на уровень исполнения, категорию и НД "ТУ 1380-036-05757848-2015" 
        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 72) && Check72(InvCode))
        {
            Master.AlertMessage = "По \"ТУ 1380-036-05757848-2015\" отсутствуют данные по уровню исполнения и категории качества для печати этикетки и приемки на СГП";
            return;
        }


        //проверка обязательных полей
        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 11) && (txbPartNo.Text.Trim() == "" || txbPartYear.Text.Trim() == ""))
        {
            Master.AlertMessage = "Необходимо ввести год и номер партии";
            return;
        }
        int pipe_length = 0;
        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 12) && !Int32.TryParse(txbLength.Text.Trim(), out pipe_length))
        {
            Master.AlertMessage = "Необходимо ввести длину трубы в мм.";
            return;
        }
        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 13) && (pipe_length < 4000 || pipe_length > 15000))
        {
            Master.AlertMessage = "Длина трубы должна быть в пределах от 4000 до 15000 мм";
            return;
        }
        double pipe_weight = 0;
        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 14) && !double.TryParse(txbWeight.Text.Trim(), out pipe_weight))
        {
            Master.AlertMessage = "Необходимо указать массу в кг.";
            return;
        }
        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 15) && (ddlDiam.SelectedItem.Text == "" && ddlProfileSize.SelectedItem.Text == "") && WorkplaceId != 63)
        {
            Master.AlertMessage = "Необходимо указать диаметр или типоразмер профиля трубы";
            return;
        }
        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 16) && ddlThickness.SelectedItem.Text == "")
        {
            Master.AlertMessage = "Необходимо указать толщину стенки трубы";
            return;
        }
        if (txbNTD.Text.Trim() != "50")
        {
            if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 17) && (txbZakazNo.Text.Trim() == "" || txbZakazLine.Text.Trim() == ""))
            {
                Master.AlertMessage = "Необходимо указать номер и строку заказа";
                return;
            }
        }
        if ((ddlInstructionType.SelectedItem.Value != "" && txbInstructionNumber.Text.Trim() == "") && Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 18))
        {
            Master.AlertMessage = "Необходимо указать номер документа-основания перевода";
            return;
        }

        //проверка указания дефекта для труб принимаемых в брак
        if (txbNTD.Text.Trim() == "50")
        {
            if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 19) && ddlDefect.SelectedItem.Value.Trim() == "")
            {
                Master.AlertMessage = "Необходимо указать дефект";
                return;
            }
        }

        //проверка указания дефекта если он необходим для заданного НД
        String defectName = ddlDefect.SelectedItem.Text.Replace("-", "").ToUpper().Trim();
        bool isDefectRequired = IsDefectRequired(GOST, GROUP);
        if (isDefectRequired)
        {
            if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 20) && defectName == "" && ddlInstructionType.SelectedItem.Value == "")
            {
                Master.AlertMessage = "Для приёмки труб в " + GOST + " " + GROUP + " необходимо указать дефект или документ-основание перевода.";
                return;
            }
        }

        //блокировка приемки труб с дефектом на СГП по всем НД, кроме тех НД в которых он необходим
        // блокировка не работает для рабочего места 85 (Зачистка внутренней поверхности
        if (!isDefectRequired & WorkplaceId != 85)
        {
            if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 21) && ((ddlDefect.SelectedIndex > 0 && Convert.ToInt32(ddlDefect.SelectedItem.Value) > 0) || ddlDefectAdditional.SelectedIndex > 0))
            {
                Master.AlertMessage = "Для выбранного НД нельзя принять трубу на СГП с дефектом";
                return;
            }
        }

        //проверка указания дополнительного дефекта если он необходим
        if (IsReduceLengthDefect())
        {
            if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 22) && ddlDefectAdditional.SelectedItem.Value.Trim() == "")
            {
                Master.AlertMessage = "Необходимо указать дефект, выводящий длину за пределы требований НД";
                return;
            }
        }

        //проверка указания причины перевода
        if (IsPerevodDefect())
        {
            if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 23) && ddlPerevodReason.SelectedItem.Value == "")
            {
                Master.AlertMessage = "Необходимо указать причину перевода";
                return;
            }
        }

        //проверка некондиции        
        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 24) && CheckNoCondition(InvCode) & !EnableNoCondidion)
        {
            Master.AlertMessage = "Приемка труб в некондицию в данный период невозможна.";
            return;
        }

        //проверка допустимого диапазона длины по заданию на кампанию
        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 25) && !CheckCampaignMinMaxLength()) return;

        //проверка указания длины зачистки
        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 26) && !CheckDeltaSize()) return;

        //проверка трубы по актам на предъявление (если есть в акте - должна быть предъявлена)
        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 27) && !CheckPresentationActs()) return;

        //проверка обязательного предъявления трубы если дефект требует обязательного предъявления
        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 28) && !CheckPipePresentation()) return;

        //проверка на заполнение значений контрольных операций
        ControlOperations control_operation = new ControlOperations();
        if (WorkplaceId >= 1 && WorkplaceId <= 7)
        {
            bool isControlOperation = false;
            if (cbShowCampaigns.Checked)
            {
                int campaign_line_id = 0;
                int.TryParse(ddlCampaign.SelectedItem.Value, out campaign_line_id);
                isControlOperation = campaign_line_id > 0 ? control_operation.GetControlOperationByCampaign(campaign_line_id, true) :
                                                                 control_operation.GetControlOperationByPipe(PipeNumber, true);
            }
            else
            {
                int ntd_id = 0;
                double diam_for_control_operation = 0;
                if (int.TryParse(GetNDCode(ddlNTD), out ntd_id) && ntd_id > 0 && double.TryParse(ddlDiam.SelectedItem.Value, out diam_for_control_operation) && diam_for_control_operation > 0)
                    isControlOperation = control_operation.GetControlOperationByND(ntd_id, diam_for_control_operation);
                else isControlOperation = control_operation.GetControlOperationByPipe(PipeNumber, true);
            }

            //запрет приёмки, если отсутствуют значения контрольных операций
            if (!isControlOperation)
            {
                Master.AlertMessage = "Внимание!\n\nТруба не может быть принята, т.к. отсутствуют значения контрольных операций";
                return;
            }
        }

        //проверка данных по испытаниям трубы на гидропрессе
        bool isHydropress = true;
        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 29) && (WorkplaceId >= 1 && WorkplaceId <= 7))
        {
            isHydropress = CheckHydroTest(control_operation);
            if (!isHydropress) return;
        }

        //проверка ГОСТ после зачистки c не удаленным дефектом
        if (WorkplaceId == 85 && ddlResultScraping.SelectedItem.Value == "3")
        {
            if (!((GOST == "ТС 153-21-2007") || (GOST == "ГОСТ 10705-80" && GROUP == "Д" && isHydropress)))
            {
                Master.AlertMessage = "Разрешена приемка на склад только по ТС 153-21-2007 или ГОСТ 10705-80 (гр Д) с пройденым гидроиспытанием!";
                return;
            }
        }

        //проверка указания параметров ремонта зачисткой
        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 30) && !CheckZachistka()) return;


        //проверка отбора проб от трубы/партии
        if ((WorkplaceId >= 0 && WorkplaceId <= 7)
            || (WorkplaceId >= 80 && WorkplaceId <= 84))
            if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 31) && !CheckPipeForProbes()) return;


        //проверка контроля УЗК
        if (WorkplaceId >= 0 && WorkplaceId <= 7)
            if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 32) && !((GOST == "ГОСТ 10705-80" && GROUP == "Д") || (GOST == "ТС 153-21-2007")) && !CheckUsc(control_operation)) return;

        //письмо/распоряжение не может быть указано, если труба принимается не в группу Д и не в ТС
        if (ddlInstructionType.SelectedItem.Value != "")
        {
            if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 33) && GROUP != "Д" && GOST != "ТС 153-21-2007" && GOST != "ТС 153-11-2002-05-15")
            {
                Master.AlertMessage = "Приемка труб по письму, распоряжению или заказу невозможна в " + GOST;
                return;
            }
        }

        //блокировка приёмки труб с неуказанной плавкой
        if (GOST.Contains("EN") || GOST.Contains("API") || GOST == "ТУ 1380-060-05757848-2011")
        {
            if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 34) && txbSmelting.Text.Trim() == "")
            {
                Master.AlertMessage = "Для приёмки трубы по требованиям " + GOST + " необходимо указывать плавку.";
                Master.FocusControl = txbSmelting.ID;
                return;
            }
        }

        //проверка указания причины перевода в другое назначение
        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 35) && !CheckNotZakaz(sender, false)) return;

        //проверка соответствия сортамента по приёмке и по задаче рулонов
        //только для круглой трубы
        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 36) && WorkplaceId != 63 && !CheckSortament()) return;

        // блокировка сохранения с указанием дефекта "проба механическая" (письмо 23-37-03/6734. IS-548)
        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 37) && ddlDefect.SelectedItem.Value == "365")
        {
            Master.AlertMessage = "Дефект «Проба механическая» может быть внесен только на рабочем месте «Участок вырезки механических проб»";
            return;
        }

        //проверка указания маршрута СГП
        /*
        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 38) && ddlSgpRoute.SelectedItem.Value == "")
        {
            Master.AlertMessage = "Необходимо указать маршрут СГП.";
            return;
        }
        */

        // проверка на повторную термообработку
        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 39) && !CheckReHeatTreatment(GOST, GROUP))
        {
            return;
        }

        //проверка на ручные замеры геометрических параметров
        string message_gp = "";

        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 1) && ((WorkplaceId >= 1 && WorkplaceId <= 6) || WorkplaceId == 80 || WorkplaceId == 85) && !CheckManualGeometryParam(out message_gp))
        {
            Master.AlertMessage = message_gp;
            return;
        }


        //проверка "Дефект АУЗК шва на участке сварки без положительной перепроверки с помощью РУЗК на инспекционных решетках"
        //if ((WorkplaceId >= 1 && WorkplaceId <= 6) || GOST != "ТС 153-21-2007" || !(GOST == "ГОСТ 10705-80" && GROUP == "Д"))
        if ((WorkplaceId >= 1 && WorkplaceId <= 6) && !(GOST == "ТС 153-21-2007" || (GOST == "ГОСТ 10705-80" && GROUP == "Д")))
            if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 70))
            {
                b70 = CheckRestrictions70();
                if ((!b70) && !IsPdoForm)
                    return;
            }

        //проверка "Дефект АУЗК кромок на участке сварки без положительной перепроверки с помощью РУЗК на инспекционных решетках"
        if ((WorkplaceId >= 1 && WorkplaceId <= 6) && !(GOST == "ТС 153-21-2007" || (GOST == "ГОСТ 10705-80" && GROUP == "Д")))
            if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 77))
            {
                b77 = CheckRestrictions77(out list_def_b77);
                if ((!b77) && !IsPdoForm)
                    return;
            }

        //проверка на обязательное заполнение поля "Качество КП" при прохождении консервационного покрытия
        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 73) && is_kp_need && is_kp_fact && ddlQualityKP.SelectedItem.Value == "")
        {
            Master.AlertMessage = "Поле «Качество консервационного покрытия» обязательно для заполнения при прохождении трубы через консервационное покрытие!";
            return;
        }

        //проверка на возможность приемки после зачистки
        if (WorkplaceId == 85 && !(GOST == "ТС 153-21-2007" || (GOST == "ГОСТ 10705-80" && GROUP == "Д")) && !CheckAcceptAfterScrapingDefect())
        {
            Master.AlertMessage = "Приемка на склад запрещена, так как отправка на зачистку была с дефектами, у которых отсутвует признак " +
                                  "'Возможность приемки на склад после зачистки на внутренней поверхности' в справочнике дефектов черной трубы!";
            return;
        }

        if (WorkplaceId == 85 && ddlResultScraping.SelectedItem.Text == "дефект удален, толщина выходит за минусовой допуск" && !(GOST == "ТС 153-21-2007" || (GOST == "ГОСТ 10705-80" && GROUP == "Д")))
        {
            Master.AlertMessage = "Приемка на склад запрещена, так как трубы можно принимать с заключением 'дефект удален, толщина выходит за минусовой допуск' " +
                                  "только на ГОСТ 10705-80 (гр. Д) или ТС 153-21-2007";
            return;
        }

        #endregion *** Проверки на запрет ******

        #region *** Проверки для предупреждения ******

        //выполнение проверок, результатами которых будет набор предупреждений;
        //выполняется только если предупреждения ещё не были отображены, о чем говорит заполнение поля "функции обратного вызова из окна предупреждения"        
        if (fldWarningReturnButtonId.Value == "")
        {
            //очистка предупреждений
            ClearWarnings(false, true);

            if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 69) && IsPdoForm && !b69)
                Check69(s_insp_roll, s_insp_pipe);

            if (WorkplaceId >= 0 && WorkplaceId <= 7)
            {
                if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 60))
                {
                    CheckDeltaWeight();
                }
                if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 61))
                {
                    CheckDeltaLength();
                }
                if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 62))
                {
                    CheckCzlSmelting();
                }
            }

            if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 59))
            {
                CheckLot();
            }

            // предупреждение для трубы, принимаемой в другое назначение, если от нее не отобраны пробы
            if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 63) && (WorkplaceId >= 1 && WorkplaceId <= 7) && ddlNotZakazReason.SelectedItem.Value != "")
            {
                CheckPipeSampling();
            }

            // предупреждение о не назначеной партии при  приемки с участка ремонта
            if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 64) && (WorkplaceId == 7 || (WorkplaceId >= 80 && WorkplaceId <= 84)))
            {
                CheckSetSamplingForPart();
            }

            if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 70) && IsPdoForm && !b70)
                Check70();

            // Информационное сообщение АУЗК кромок
            if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 77) && IsPdoForm && !b77)
                Check77(list_def_b77);

            //предупреждение о необходимости прохождения консервационных покрытий
            if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 73) && is_kp_need)
                if (!is_kp_fact || (is_kp_fact && ddlQualityKP.SelectedItem.Value == "0"))
                    AddWarning("Требуется нанесение консервационного покрытия", true, true);

            //проверка на автоматические замеры геометрических параметров
            message_gp = "";
            if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 1) && (WorkplaceId >= 1 && WorkplaceId <= 6) && !CheckAutoGeometryParam(out message_gp))
                AddWarning(message_gp, true, true);

            //отображение окна с предупреждением, если имеются предупреждения
            if (lstWarnings.Items.Count > 0)
            {
                fldWarningReturnButtonId.Value = (sender as Button).ID;
                MainMultiView.SetActiveView(vWarnings);
                return;
            }
        }

        #endregion *** Проверки для предупреждения ******

        //печать бирки и сохранение данных
        try
        {
            //поле для пометки труб, возвращенных со склада
            String Reason = "";
            if (sender == btnFromSklad) Reason = "ремонт";

            //проверка наличия трубы на СГП или в актах возврата на ремонт
            if (Reason != "ремонт" & !IsPdoForm)
                if (CheckPipeOnSklad()) Reason = "ремонт";

            //признак передачи в SAP данных по трубе (в случае исправления записи)
            bool SapProcessed = false;

            //сохранение в БД        
            String createdRowId = "";
            if (!DataSaved)
            {
                //проверка возможности ввода по трубе согласно маршруту
                if (UpdateRowID == "")
                    if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 40) && !CheckValidCurrentPositionOfPipe())
                    {
                        return;
                    }

                //сохранение данных
                SapProcessed = !SaveDataToDB(false, false, Reason, out createdRowId);
            }
            //установка флага "данные сохранены"
            DataSaved = true;

            //передача номера трубы в транспорт НЛО, если инспекция 1...6 и не исправление записи            
            if (WorkplaceId <= 6 && WorkplaceId > 0 && UpdateRowID == "")
                SendCommandToTransportPlc(route_sgp_id);

            //передача данных по трубе в SAP
            if (WorkplaceId >= 80 && WorkplaceId <= 84 && UpdateRowID == "" && createdRowId != "")
                SendDataToSapIntegration(createdRowId, (Reason == "ремонт"));

            //установка флага обработки трубы из очереди на MAIR
            if (WorkplaceId == 80 || WorkplaceId == 81)
                SetMairQueueProcessingStatus(PipeNumber);

            //печать бирки, если исправляемые данные по трубе не были переданы в SAP           
            if (!SapProcessed)
            {
                if (!PrintLabel(PipeNumber, cbWeight.Checked, cbInch.Checked, cbKGtoFunt.Checked))
                    return;
            }
            else
            {
                //предупреждение о невозможности правки если данные ранее переданы в SAP
                Master.AlertMessage = "Внимание! Данные по трубе №" + PipeNumber.ToString()
                    + " ранее были переданы в SAP. Изменение информации невозможно. Все необходимые изменения по данной трубе оформить на бумажном носителе и передать старшему сменному мастеру.";
            }

            //переход к экрану ввода номера трубы
            btnNewPipe_Click(sender, e);
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка при сохранении данных и печати бирки", ex);
        }

        //сброс отметок о предупреждениях
        fldNotZakazCheckOk.Value = "";
        fldWarningReturnButtonId.Value = "";
    }

    /// <summary>Проверка на возможность приемке на склад после зачистки</summary>
    /// <returns></returns>
    private bool CheckAcceptAfterScrapingDefect()
    {
        try
        {
            List<int> TestRepitDefect = new List<int>();
            int[] defects = GetDefectByZachistka();
            string str_defect = "";
            int count_defect = 0;
            for (int i = 0; i < 3; i++)
            {
                if (defects[i] > 0)
                {
                    if (!TestRepitDefect.Contains(defects[i]))
                    {
                        str_defect += (i > 0 ? "," : "") + defects[i].ToString();
                        TestRepitDefect.Add(defects[i]);
                        count_defect++;
                    }
                }
            }
            if (str_defect != "")
            {
                OleDbConnection conn = Master.Connect.ORACLE_TESC3();
                using (OleDbCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT COUNT (sd.id) FROM tesc3.spr_defect sd WHERE sd.is_active = 1 AND sd.is_accept_after_scraping = 1 AND sd.id IN (" + str_defect + ")";
                    return Convert.ToInt32(cmd.ExecuteScalar()) == count_defect;
                }
            }
            else return true;
        }
        catch (Exception ex)
        {
            throw new Exception("Ошибка проверки возможности приемки на склад после зачистки (" + ex.Message + ")");
        }
    }
    /// <summary> проверка сортаментов предыдущей и текущей труб </summary>
    private bool CheckSortLastPipe()
    {
        try
        {
            double diam = 0, thick = 0, diam1 = 0, thick1 = 0;
            string steelmark = "", steelmark1 = "";
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            using (OleDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"select pipe_number, diameter, thickness, steelmark from inspection_pipes
                                    where trx_date = (select max(trx_date) 
                                                    from inspection_pipes 
                                                    where edit_state = 0 
                                                        and workplace_id = ?)
                                        and edit_state = 0
                                        and workplace_id = ?";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("WORKPLACE_ID", WorkplaceId);
                cmd.Parameters.AddWithValue("WORKPLACE_ID1", WorkplaceId);
                using (OleDbDataReader rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        Double.TryParse(rdr["DIAMETER"].ToString(), out diam);
                        Double.TryParse(rdr["THICKNESS"].ToString(), out thick);
                        steelmark = rdr["STEELMARK"].ToString();
                    }
                }

                cmd.CommandText = @"SELECT distinct gc.steelmark, gc.pipe_diameter, gc.thickness
                                FROM optimal_pipes op
                                LEFT JOIN geometry_coils_sklad gc ON (op.coil_pipepart_no = gc.coil_pipepart_no)
                                    AND (op.coil_pipepart_year = gc.coil_pipepart_year)
                                WHERE op.pipe_number = ?
                                    AND (gc.edit_state = 0 OR gc.edit_state IS NULL)";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);
                using (OleDbDataReader rdr1 = cmd.ExecuteReader())
                {
                    if (rdr1.Read())
                    {
                        steelmark1 = rdr1["STEELMARK"].ToString();
                        Double.TryParse(rdr1["PIPE_DIAMETER"].ToString(), out diam1);
                        Double.TryParse(rdr1["THICKNESS"].ToString(), out thick1);
                    }
                }

                if (diam == diam1 && thick == thick1 && steelmark == steelmark1) return true; else return false;
            }
        }
        catch(Exception ex)
        {
            Master.AddErrorMessage("Ошибка проверки сортамента предыдущей и текущей труб: ", ex);
            return false;
        }
        

    }

    /// <summary>
    /// проверка на уровень исполнения и группу труб
    /// </summary>
    /// <param name="inventoryID"></param>
    private bool CheckUrIspGroup(String inventoryID)
    {
        bool result = false;
        OleDbConnection conn = Master.Connect.ORACLE_TESC3();
        using (OleDbCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"select OZSM.D_UR_ISP, PI.GRUP
                                FROM CAMPAIGNS cp
                                    JOIN ORACLE.Z_SPR_MATERIALS ozsm ON (cp.INVENTORY_CODE = ozsm.MATNR)
                                    join oracle.v_t3_pipe_items pi on cp.inventory_code=pi.nomer
                                where  CP.EDIT_STATE=0 and ozsm.MATNR=? and CP.CAMPAIGN_LINE_ID=?
                                    and ozsm.D_NTDQM = 'ТУ 1380-036-05757848-2015' ";
            cmd.Parameters.AddWithValue("MATNR", inventoryID);
            cmd.Parameters.AddWithValue("CAMPAIGN_LINE_ID", ddlCampaign.SelectedItem.Value);
            OleDbDataReader rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                if (string.IsNullOrEmpty(rdr["D_UR_ISP"].ToString()) || string.IsNullOrEmpty(rdr["GRUP"].ToString()))
                {
                    result = true;
                }
            }
        }
        return result;
    }


    /// <summary>
    /// проверка на уровень исполнения и категорию труб
    /// </summary>
    /// <param name="inventoryID"></param>
    private bool Check72(String inventoryID)
    {
        bool result = false;
        OleDbConnection conn = Master.Connect.ORACLE_TESC3();
        using (OleDbCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText =
                @"  select ozsm.d_ur_isp, ozsm.d_kategor
                    from campaigns cp
                           join oracle.z_spr_materials ozsm on (cp.inventory_code = ozsm.matnr)
                           join oracle.v_t3_pipe_items pi on cp.inventory_code = pi.nomer
                     where cp.edit_state = 0
                       and ozsm.matnr = ?
                       and cp.campaign_line_id = ?
                       and ozsm.d_ntdqm = 'ТУ 1380-036-05757848-2015'";

            cmd.Parameters.AddWithValue("MATNR", inventoryID);
            cmd.Parameters.AddWithValue("CAMPAIGN_LINE_ID", ddlCampaign.SelectedItem.Value);
            OleDbDataReader rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                if (string.IsNullOrEmpty(rdr["D_UR_ISP"].ToString()) || string.IsNullOrEmpty(rdr["D_KATEGOR"].ToString()))
                {
                    result = true;
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Отправка данных по приемке трубы в интерфейсную таблицу интеграции с SAP
    /// </summary>
    /// <param name="rowID"></param>
    private void SendDataToSapIntegration(String rowID, bool isRepair)
    {
        int msgId = (!isRepair) ? 2 : 4; //приемка или ремонт

        OleDbConnection conn = Master.Connect.ORACLE_TESC3();
        int messageCounter = SapIntegration.CreateZTesc3Event(conn, DateTime.Now, WorkplaceId, msgId);
        SapIntegration.PipesToSapInterface(conn, rowID, messageCounter);
    }


    /// <summary>
    /// Проверка соответствия диаметра, толщины стенки и марки стали по задаче рулонов и по приёмке
    /// </summary>
    /// <returns>true если сортамент совпадает, false + сообщение на странице если сортамент различается и нет прав доступа на приёмку труб с различающимся сортаментом от рулонов</returns>
    private bool CheckSortament()
    {
        try
        {
            //не производить проверку, если имеется разрешение мастера на приемку в другой сортамент
            //или пользователь является работником ПДО            
            if (IsPdoForm
                || fldMasterConfirmLogin.Value != "")
                return true;

            //получение сортамента по задаче рулонов
            double stan_diameter = 0;
            double stan_thickness = 0;
            String stan_steelmark = "";
            String stan_profile_size = "";
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = @"select pi.diameter, pi.thickness, pi.stal, pi.s_size1, pi.s_size2
                                from optimal_pipes op
                                    join geometry_coils_sklad gc on op.coil_pipepart_year=gc.coil_pipepart_year
                                        and op.coil_pipepart_no=gc.coil_pipepart_no and OP.COIL_INTERNALNO=gc.coil_run_no
                                    join campaigns cmp on gc.campaign_line_id=cmp.campaign_line_id
                                    join ORACLE.V_T3_PIPE_ITEMS pi on cmp.inventory_code=pi.nomer
                                where gc.edit_state=0 and cmp.edit_state=0 and op.pipe_number=?
                                order by op.cutdate desc";
            cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);
            OleDbDataReader rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                if (rdr["DIAMETER"].ToString() != "") stan_diameter = Math.Round(Convert.ToDouble(rdr["DIAMETER"]), 4);
                if (rdr["THICKNESS"].ToString() != "") stan_thickness = Math.Round(Convert.ToDouble(rdr["THICKNESS"]), 4);
                if (rdr["s_size1"].ToString() != "") stan_profile_size = rdr["s_size1"].ToString() + "x" + rdr["s_size2"].ToString();
                stan_steelmark = rdr["STAL"].ToString();
            }
            cmd.Dispose();

            //сравнение сортамента с указанным в окончательной приемке
            bool b_sortament_equals = true;
            double thickness = Convert.ToDouble(ddlThickness.SelectedItem.Text);
            double diameter = 0;
            double.TryParse(ddlDiam.SelectedItem.Text, out diameter);
            String steelmark = ddlSteelmark.SelectedItem.Text;
            String profile_size = ddlProfileSize.SelectedItem.Text;

            if (diameter != stan_diameter)
            {
                if ((stan_diameter == 219.1 && diameter == 219) || (stan_diameter == 219 && diameter == 219.1)
                    || (stan_diameter == 273.1 && diameter == 273) || (stan_diameter == 273 && diameter == 273.1)
                    || (stan_diameter == 323.9 && diameter == 325) || (stan_diameter == 325 && diameter == 323.9))
                    b_sortament_equals = true;
                else
                    b_sortament_equals = false;
            }

            if (stan_thickness != thickness || stan_steelmark != steelmark || stan_profile_size != profile_size)
                b_sortament_equals = false;

            //сообщение при несоответствии сортамента и при отсутствии разрешения мастера на приёмку в другой сортамент
            if (!b_sortament_equals)
            {
                Master.AlertMessage = String.Format("Внимание! Обнаружено несоответствие сортамента труб.\nПо задаче рулонов: {0}\nПо окончательной приемке: {1}."
                     + "\n\nТекущий пользователь не имеет права на приемку труб при несоответствии сортамента с задачей рулонов. Обратитесь к сменному мастеру.",
                    stan_diameter + "x" + stan_thickness + " " + stan_steelmark,
                    diameter + "x" + thickness + " " + steelmark);

                return false;
            }
            else
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка проверки толщины стенки", ex);
            return false;
        }
    }




    //печать бирки для заданного номера трубы
    protected bool PrintLabel(int PipeNumber, bool bWeight, bool bInch, bool bFunt)
    {
        try
        {
            // не печатать бирку с ПК разработчиков
            if (Authentification.User.UserName == "DEV_LOGIN") return true;

            // выход, если форма открыта в режиме ПДО
            if (IsPdoForm) return true;

            //имя рабочего места
            String workPlaceName = this.WorkplaceName;

            //подключение к БД                
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();

            //подготовка и выполнение запроса данных для бирки
            OleDbCommand cmd = conn.CreateCommand();
            cmd.CommandText =
               @"  SELECT IP.*, sm.*, pi.*, sk.*, sd.DEFECT_NAME, lt.*, CP.THEORY_WEIGHT_LABEL, SPR_NTD.CE_MARKER, SPR_NTD.LABEL_MAIR, SOI.BIRKA_DESCRIPTION
                    FROM INSPECTION_PIPES IP
                         LEFT JOIN SPR_NTD ON (SPR_NTD.ID = IP.NTD_ID)
                         LEFT JOIN ORACLE.V_T3_PIPE_ITEMS pi ON INVENTORY_CODE = pi.NOMER
                         LEFT JOIN SPR_KADRY sk ON OPERATOR_ID = sk.USERNAME
                         LEFT JOIN SPR_DEFECT sd ON IP.CUT_LEFT_DEFECTS = TO_CHAR (sd.ID)
                         LEFT JOIN SPR_LABEL_TYPE lt ON IP.LABEL_TYPE_ID = lt.ID
                         LEFT JOIN ORACLE.Z_SPR_MATERIALS sm ON SM.MATNR = IP.INVENTORY_CODE
                         LEFT JOIN CAMPAIGNS cp ON CP.CAMPAIGN_LINE_ID = IP.CAMPAIGN_LINE_ID 
                            and CP.EDIT_STATE=0
                         LEFT JOIN ADMINTESC5.SPR_OUT_INSP SOI ON CP.INSPECTION=SOI.INSP_NAME
                            and CP.EDIT_STATE=0 and SOI.SHOP='ТЭСЦ-3'
                   WHERE     (PIPE_NUMBER = ?)
                         AND (NEXT_DIRECTION IS NOT NULL)
                         AND (ip.EDIT_STATE = 0)
                         AND (pi.ORG_ID = 127 OR pi.ORG_ID IS NULL)
                         AND ( ( (ip.INVENTORY_CODE <>'" + ObrInventoryCode
                        + "')and(ip.INVENTORY_CODE<>'"
                        + BrakInventoryCode + @"'))or(ip.INVENTORY_CODE is NULL))
                               order by TRX_DATE desc";
            cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);

            OleDbDataReader rdr = cmd.ExecuteReader();

            bool bPrintOk = false;
            if (rdr.Read())
            {
                //получение данных для бирки
                String RecDate = Convert.ToDateTime(rdr["TRX_DATE"]).ToString("dd.MM.yy");
                String Diameter = rdr["DIAMETER"].ToString();
                String ProfileSizeA = rdr["S_SIZE1"].ToString();
                String ProfileSizeB = rdr["S_SIZE2"].ToString();
                String Thickness = rdr["THICKNESS"].ToString();
                String SteelMark = rdr["STEELMARK"].ToString();
                String PipeLength = rdr["LENGTH"].ToString();
                String PipeWeight = rdr["WEIGHT"].ToString();

                //если необходимо перевести мм в дюймы
                if (bInch)
                {
                    Diameter = CustomConvert.ConvertToInch(Diameter, "mm.", 3).Replace(',', '.');
                    Thickness = CustomConvert.ConvertToInch(Thickness, "mm.", 3).Replace(',', '.');
                    ProfileSizeA = CustomConvert.ConvertToInch(ProfileSizeA, "mm.", 3).Replace(',', '.');
                    ProfileSizeB = CustomConvert.ConvertToInch(ProfileSizeB, "mm.", 3).Replace(',', '.');
                    PipeLength = CustomConvert.ConvertToFootSpr(PipeLength, "mm.", 2).Replace(',', '.');
                }
                else
                {
                    if (PipeLength != "")
                        PipeLength = (Convert.ToDouble(PipeLength) / 1000.0).ToString("F2");
                }

                //если необходимо перевести килограммы в фунты
                if (bFunt)
                {
                    PipeWeight = CustomConvert.ConvertToFuntSpr(PipeWeight, "kg.", 2).Replace(',', '.');
                }

                // String PipeLengthFt = (Convert.ToDouble(rdr["LENGTH"]) / 1000.0 / 0.3048).ToString("F1");
                String GostText = rdr["GOST"].ToString();
                String GostGroup = rdr["GOST_GROUP"].ToString();
                if (rdr["GOST_GROUP"].ToString() != "")
                    GostText += " гр." + rdr["GOST_GROUP"].ToString();
                String NtdId = rdr["NTD_ID"].ToString();
                String LotNumber = rdr["LOT_NUMBER"].ToString();
                String ClassStal = rdr["CLASS_STAL"].ToString();
                String OtkNumber = rdr["NCONTR"].ToString();
                String CoilSupplier = GetSteelSupplierName(PipeNumber);
                String Smelting = rdr["COIL_SMELTING"].ToString();
                String NextDirection = rdr["NEXT_DIRECTION"].ToString();
                String DeltaSize = rdr["DELTA_SIZE_MM"].ToString();
                String Defect = rdr["DEFECT_NAME"].ToString();
                if (DeltaSize != "") Defect += " " + DeltaSize;
                if (!bWeight) PipeWeight = "";
                String inventory_code = rdr["INVENTORY_CODE"].ToString();
                String campaign_line_id = rdr["CAMPAIGN_LINE_ID"].ToString();
                String ur_isp = rdr["D_UR_ISP"].ToString();
                String D_KATEGOR = rdr["D_KATEGOR"].ToString();
                String F_NOTE_USE = rdr["F_NOTE_USE"].ToString();
                String Out_Inspection = rdr["BIRKA_DESCRIPTION"].ToString();
                String tm = "";
                //if (rdr["THEORY_WEIGHT_LABEL"].ToString() == "0")
                //{
                //    tm = "(ТМ)";
                //}
                String GostTextD = "";
                switch (GostText.Trim())
                {
                    case "ГОСТ 10705-80 гр.Д":
                        GostTextD = "Д";
                        break;
                    case "ТС 153-21-2007":
                        GostTextD = "ТС 21";
                        break;
                    case "ТС 153-11-2002-05-15":
                        GostTextD = "ТС 11";
                        break;
                    case "ТС 153-20-2007":
                        GostTextD = "ТС 20";
                        break;
                    case "ТС 153-31-2008":
                        GostTextD = "ТС 31";
                        break;
                    default:
                        GostTextD = "";
                        break;
                }
                String TermoProc = "";
                // вид термообработки
                TermoProc = Convert.ToInt32(rdr["LOT_NUMBER"].ToString()) % 2 == 0 ? "ЛТО" : "ОТО";
                String Zachistka_Checkbox = rdr["ZACHISTKA_CHECKBOX"].ToString();
                String Zachistka_Final = rdr["ZACHISTKA_FINAL_ID_ZVS"].ToString();
                //String ce_marker = rdr["CE_MARKER"].ToString();
                //получение допуска по грату из строки задания на кампанию
                String GratDopusk = "";
                String GratDirection = "";
                String OrderNumber = "";
                if (campaign_line_id != "")
                {
                    OleDbCommand cmd_campaign = conn.CreateCommand();
                    cmd_campaign.CommandText = @"select max(GRAT_DOPUSK) GRAT_DOPUSK, GRAT_DIRECTION, ORDER_HEADER
                                                    from CAMPAIGNS
                                                    where CAMPAIGN_LINE_ID=?
                                                        and EDIT_STATE=0
                                                    group by GRAT_DOPUSK, GRAT_DIRECTION, ORDER_HEADER";
                    cmd_campaign.Parameters.AddWithValue("CAMPAIGN_LINE_ID", campaign_line_id);
                    OleDbDataReader reader_campaign = cmd_campaign.ExecuteReader();
                    while (reader_campaign.Read())
                    {
                        GratDopusk = reader_campaign["GRAT_DOPUSK"].ToString();
                        GratDirection = reader_campaign["GRAT_DIRECTION"].ToString();
                        OrderNumber = reader_campaign["ORDER_HEADER"].ToString();
                    }
                    reader_campaign.Close();
                    reader_campaign.Dispose();

                    //если необходимо перевести мм в дюймы
                    if (bInch)
                        GratDopusk = CustomConvert.ConvertToInch(GratDopusk, "mm.", 2);

                    if (GratDopusk != "") GratDopusk = "Грат " + GratDopusk;
                    cmd_campaign.Dispose();
                }

                //для экспериментальных труб (по заданным номенклатурам) - вместо допуска грата спец.обозначения группы
                if (inventory_code == "000000503030002220")
                    GratDopusk = "Дс";
                if (inventory_code == "000000503030002221")
                    GratDopusk = "Ес";

                //вывод информационного сообщения при условии отсутствия массы пакета на бирке
                if (string.IsNullOrEmpty(rdr["WEIGHT"].ToString()))
                {

                    Master.AlertMessage = "Внимание!\n\nВ бирке отсутствует масса пакета";
                    return false;
                }

                //вывод информационного сообщения при условии несовпадения данных на трубе/пакете и в строке компании
                if (rdr["THEORY_WEIGHT_LABEL"].ToString() == rdr["TYPE_WEIGHT"].ToString())
                {

                    Master.AlertMessage = "Внимание!\n\nТип веса не соответствует требованиям строки компании. Продолжить печать бирки?";
                }


                //код типа бирки, по умолчанию если не указан то 1
                int label_type_id = (rdr["LABEL_TYPE_ID"].ToString() != "") ? Convert.ToInt32(rdr["LABEL_TYPE_ID"]) : 1;

                // если выбран шаблон Инспекции_60x60 или Инспекции_100x100 или По ТУ 1380-036-05757848-2015 то печатаем уровень исполнения в формате ur_isp/D_KATEGOR/F_NOTE_USE

                if (!string.IsNullOrEmpty(D_KATEGOR))
                {
                    ur_isp += "/" + D_KATEGOR;
                }

                if (!string.IsNullOrEmpty(F_NOTE_USE))
                {
                    ur_isp += "/" + F_NOTE_USE;
                }

                //признак бирки на профильную трубу
                bool is_profile_pipe = (rdr["IS_PROFILE_PIPE"].ToString() == "1");

                //печать бирки
                int DuplicateReason = -1;
                if (ddlDuplicateReason.SelectedItem.Value != "")
                    DuplicateReason = Convert.ToInt32(ddlDuplicateReason.SelectedItem.Value);

                if (string.IsNullOrEmpty(ddlPrinter.SelectedValue))
                    throw new Exception("Необходимо выбрать принтер для печати бирки");


                //ID принтера
                int printerId = Convert.ToInt32(ddlPrinter.SelectedItem.Value);

                int LabelMAIR_CE = 0;
                int.TryParse(rdr["LABEL_MAIR"].ToString(), out LabelMAIR_CE);

                if (LabelMAIR_CE == 1)
                {
                    // печать бирки на склад
                    if (NextDirection == "SKLAD")
                    {
                        if (!is_profile_pipe)
                        {
                            //бирка на обычную трубу
                            bPrintOk = Printing.PrintLabel_FinalInspection(250, DuplicateReason, PipeNumber, printerId,
                               workPlaceName, OtkNumber,
                               Diameter, Thickness, SteelMark, ClassStal,
                                GostText, NtdId, Defect, LotNumber, PipeLength, PipeWeight, CoilSupplier,
                               Smelting, GratDopusk, NextDirection, ur_isp, GratDirection, OrderNumber, Out_Inspection, "");
                        }
                        else
                        {
                            //бирка на профильную трубу
                            bPrintOk = Printing.PrintLabel_FinalInspectionProfile(label_type_id, DuplicateReason,
                               PipeNumber, printerId, workPlaceName, OtkNumber,
                               ProfileSizeA, ProfileSizeB, Thickness, SteelMark, ClassStal,
                                GostText, NtdId, Defect, LotNumber, PipeLength, PipeWeight, CoilSupplier,
                               Smelting, NextDirection, OrderNumber, Out_Inspection);
                        }
                    }

                    //печать бирки на ремонт
                    if (NextDirection == "REMONT")
                    {
                        bPrintOk = Printing.PrintLabel_FinalInspection(250, DuplicateReason, PipeNumber, printerId,
                            workPlaceName, OtkNumber,
                            Diameter, Thickness, SteelMark, ClassStal,
                               GostText, NtdId, Defect, LotNumber, PipeLength, PipeWeight, CoilSupplier,
                               Smelting, GratDopusk, NextDirection, ur_isp, GratDirection, OrderNumber, Out_Inspection, Zachistka_Checkbox);
                    }

                    //печать бирки для приемки в брак/лом негабаритный
                    if (NextDirection == "BRAK" || NextDirection == "OTDELKA" || NextDirection == "OTO")
                    {
                        bPrintOk = Printing.PrintLabel_FinalInspection(250, DuplicateReason, PipeNumber, printerId,
                            workPlaceName, OtkNumber,
                               Diameter, Thickness, SteelMark, ClassStal,
                            GostText, NtdId, Defect, LotNumber, PipeLength, PipeWeight, CoilSupplier,
                               Smelting, GratDopusk, NextDirection, ur_isp, GratDirection, OrderNumber, Out_Inspection, "");
                    }
                }
                else
                {
                    // печать бирки на склад
                    if (NextDirection == "SKLAD")
                    {
                        if (!is_profile_pipe)
                        {
                            if (GostText == "ГОСТ 10705-80 гр.Д" || GostText == "ТС 153-21-2007")
                            {
                                //бирка на обычную трубу по ГОСТ 10705-80 грД или ТС 153-21-2007
                                bPrintOk = Printing.PrintLabel_FinalInspectionD(label_type_id, DuplicateReason, PipeNumber,
                                    printerId, workPlaceName, OtkNumber,
                                    Diameter, Thickness, SteelMark, ClassStal,
                                    GostTextD, NtdId, Defect, LotNumber, PipeLength, PipeWeight, CoilSupplier,
                                    NextDirection, Out_Inspection, Zachistka_Final);
                            }
                            else
                            {
                                if (label_type_id == 550)
                                {
                                    //бирка на обычную трубу По ТУ 1380-036-05757848-2015
                                    bPrintOk = Printing.PrintLabel_FinalInspectionT036(label_type_id, DuplicateReason, PipeNumber,
                                        printerId, workPlaceName, OtkNumber,
                                        Diameter, Thickness, SteelMark, ClassStal,
                                        TermoProc, GostGroup, ur_isp, LotNumber, PipeLength, GratDopusk, OrderNumber, Out_Inspection);
                                }
                                else
                                {
                                    //бирка на обычную трубу
                                    bPrintOk = Printing.PrintLabel_FinalInspection(label_type_id, DuplicateReason, PipeNumber,
                                        printerId, workPlaceName, OtkNumber,
                                        Diameter, Thickness, SteelMark, ClassStal,
                                        GostText, NtdId, Defect, LotNumber, PipeLength, PipeWeight, CoilSupplier,
                                        Smelting, GratDopusk, NextDirection, ur_isp, GratDirection, OrderNumber, Out_Inspection, "");
                                }

                            }
                        }
                        else
                        {

                            //бирка на профильную трубу
                            bPrintOk = Printing.PrintLabel_FinalInspectionProfile(label_type_id, DuplicateReason,
                                PipeNumber, printerId, workPlaceName, OtkNumber,
                                ProfileSizeA, ProfileSizeB, Thickness, SteelMark, ClassStal,
                                GostText, NtdId, Defect, LotNumber, PipeLength, PipeWeight, CoilSupplier,
                                Smelting, NextDirection, OrderNumber, Out_Inspection);


                        }
                    }

                    //печать бирки на ремонт
                    if (NextDirection == "REMONT")
                    {
                        if (GostText == "ГОСТ 10705-80 гр.Д" || GostText == "ТС 153-21-2007")
                        {
                            //бирка на обычную трубу по ГОСТ 10705-80 грД или ТС 153-21-2007
                            bPrintOk = Printing.PrintLabel_FinalInspectionD(label_type_id, DuplicateReason, PipeNumber,
                                printerId, workPlaceName, OtkNumber,
                                Diameter, Thickness, SteelMark, ClassStal,
                                GostTextD, NtdId, Defect, LotNumber, PipeLength, PipeWeight, CoilSupplier,
                                NextDirection, Zachistka_Final, Out_Inspection, Zachistka_Checkbox);
                        }
                        else
                        {
                            bPrintOk = Printing.PrintLabel_FinalInspection(1, DuplicateReason, PipeNumber, printerId,
                                workPlaceName, OtkNumber,
                                Diameter, Thickness, SteelMark, ClassStal,
                                GostText, NtdId, Defect, LotNumber, PipeLength, PipeWeight, CoilSupplier,
                                Smelting, GratDopusk, NextDirection, ur_isp, GratDirection, OrderNumber, Out_Inspection, Zachistka_Checkbox);
                        }
                    }

                    //печать бирки для приемки в брак/лом негабаритный
                    if (NextDirection == "BRAK" || NextDirection == "OTDELKA" || NextDirection == "OTO")
                    {
                        if (GostText == "ГОСТ 10705-80 гр.Д" || GostText == "ТС 153-21-2007")
                        {
                            //бирка на обычную трубу по ГОСТ 10705-80 грД или ТС 153-21-2007
                            bPrintOk = Printing.PrintLabel_FinalInspectionD(label_type_id, DuplicateReason, PipeNumber,
                                printerId, workPlaceName, OtkNumber,
                                Diameter, Thickness, SteelMark, ClassStal,
                                GostTextD, NtdId, Defect, LotNumber, PipeLength, PipeWeight, CoilSupplier,
                                NextDirection, Out_Inspection, Zachistka_Final);
                        }
                        else
                        {
                            bPrintOk = Printing.PrintLabel_FinalInspection(1, DuplicateReason, PipeNumber, printerId,
                                workPlaceName, OtkNumber,
                                Diameter, Thickness, SteelMark, ClassStal,
                                GostText, NtdId, Defect, LotNumber, PipeLength, PipeWeight, CoilSupplier,
                                Smelting, GratDopusk, NextDirection, ur_isp, GratDirection, OrderNumber, Out_Inspection, "");
                        }
                    }
                }

                //закрытие подключения к БД
                rdr.Close();
                rdr.Dispose();
                cmd.Dispose();
            }
            else
            {
                rdr.Close();
                rdr.Dispose();
                cmd.Dispose();
                throw new Exception("Отсутствует информация по приемке трубы " + PipeNumber.ToString());
            }

            //переход к указанию причины дубликата
            if (!bPrintOk)
            {
                MainMultiView.SetActiveView(vDuplicatePrintLabel);
                SelectDDLItemByText(ddlDuplicateReason, "");
            }
            return bPrintOk;
        }
        finally
        {
            //
        }
    }

    // Получение INVENTORY_CODE_TARGET из CAMPAIGN по CAMPAIGN_LINE_ID
    protected String GetIctFromCompaigns(String CampaignLineID)
    {
        String ict = "";
        try
        {
            //подключение к БД        
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();
            OleDbDataReader reader = null;
            cmd.CommandText = "select INVENTORY_CODE_TARGET from CAMPAIGNS where EDIT_STATE=0 and CAMPAIGN_LINE_ID = " + CampaignLineID;
            reader = cmd.ExecuteReader();
            while (reader.Read())
                ict = reader["INVENTORY_CODE_TARGET"].ToString().Trim();
            reader.Close();
            reader.Dispose();
            cmd.Dispose();
        }
        catch
        {
        }
        return ict;
    }


    protected void ShowInfoAboutTemplating_Click(object sender, EventArgs e)
    {


        var templateInfo = Repository.SelectOne<HYDROPRESS_DRIFTER_PIPE>("WHERE PIPE_NUMBER = " + PipeNumber + " ORDER BY REC_DATE DESC");
        if (templateInfo == null)
        {
            txbInformationTemplate.Text = "Данные отсутствуют";
        }
        else
        {
            txbInformationTemplate.Text = @"-Диаметр дрифтера, мм: " + templateInfo.DRIFTER_DIAMETER +
                                        "\n-Длина оправки дрифтера, мм: " + (templateInfo.DRIFTER_LENGTH / 100) +
                                        "\n-Путь пройденный дрифтером при испытании трубы,  мм: " + templateInfo.DRIFTER_WAY +
                                        "\n-Длина трубы измеренная на ГП, мм: " + templateInfo.PIPE_LENGTH;
        }
        PopupWindow2.ContentPanelId = pnlShowInfoTemplating.ID;
        PopupWindow2.Title = "Данные о шаблонировании трубы №" + PipeNumber;
        PopupWindow2.MoveToCenter();
        pnlShowInfoTemplating.Visible = true;
        return;
    }


    //сохранение текущих данных в БД
    //возвращает FALSE, если труба ранее была передана в SAP (в случае исправлений)
    //в остальных случаях возвращает TRUE
    protected bool SaveDataToDB(bool bRepair, bool bBrak, String Reason, out String CreatedRowId)
    {
        try
        {
            //подключение к БД        
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();

            //признак передачи данных по старой записи в SAP и статус актуальности записи
            bool bSapProcessed = false;
            int EDIT_STATE = 0; //по умолчанию - запись актуальна

            //ROW_ID создаваемой записи
            CreatedRowId = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + "_" + PipeNumber.ToString();

            //получение номенклатурного номера по текущим данным
            String InvCode = Checking.CorrectInventoryNumber(txbInventoryNumber.Text);

            //подготовка и выполнение запроса
            if (UpdateRowID == "")
            {
                //добавление новой записи
                cmd.CommandText = @"insert into INSPECTION_PIPES (TRX_DATE, EDIT_STATE, AUTO_CAMPAIGN_FLAG, SHIFT_INDEX, PIPE_NUMBER, SHTRIPS_LOT_NUMBER, DIAMETER, THICKNESS, STEELMARK, 
                 EMPLOYEE_NUMBER, INVENTORY_CODE, INVENTORY_CODE_TARGET, LENGTH, WEIGHT, LOT_NUMBER, LOT_YEAR, COIL_SMELTING, ORDER_HEADER, ORDER_LINE, NTD_ID, GOST, GOST_GROUP, CUT_LEFT_DEFECTS, 
                 ZACHISTKA_DEFECT_ID, ZACHISTKA_LENGTH_MM, ZACHISTKA_THICKNESS_1, ZACHISTKA_THICKNESS_2, DELTA_SIZE_MM, CUT_RIGHT_DEFECTS, INSTRUCTION_NUMBER, INSTRUCTION_TYPE, CUT_LEFT_LENGTH,
                 CUT_RIGHT_LENGTH, WORKPLACE_ID, OPERATOR_ID, ORIGINAL_ROWID, NEXT_DIRECTION, REASON, CAMPAIGN_LINE_ID, BAD_MARKING, PREV_DIRECTION, CUT_DATE, ROW_ID, DEFECT_DESCRIPTION,
                 DEFECT_LOCATION, DEFECT_DISTANCE, NOT_ZAKAZ_REASON_ID, NOT_ZAKAZ_REASON_VALUE, NOT_ZAKAZ_REASON_DISTANCE, NOT_ZAKAZ_REASON_DESCRIPTION, PEREVOD_REASON_ID, ADDITIONAL_DEFECT_ID,
                 KMK_MARKING_TEMPLATE_ID, LABEL_TYPE_ID, MASTER_CONFIRM_USERNAME, MAP_NAME, SGP_ROUTE_ID, PROFILE_SIZE_A, PROFILE_SIZE_B, CHECKMARK, TYPE_WEIGHT, QUALITY_KP, ZACHISTKA_AMOUNT_ZVS, 
                 ZACHISTKA_MINTHICKSHOV_ZVS, ZACHISTKA_MINTHICKTELO_ZVS, ZACHISTKA_FINAL_ID_ZVS, DEFECT_LOCATION_ID_ZVS, ZACHISTKA_WHOLE_LENGTH_ZVS, ZACHISTKA_CHECKBOX, DEFECT_ID_2, 
                 DELTA_SIZE_MM_2, DEFECT_DESCRIPTION_2, DEFECT_LOCATION_2, DEFECT_DISTANCE_2, DEFECT_ID_3, DELTA_SIZE_MM_3, DEFECT_DESCRIPTION_3, DEFECT_LOCATION_3, DEFECT_DISTANCE_3, 
                 ZACHISTKA_GOST, TEMPLATING_RESULT, ID_DEFECT_FOR_TEMPLATING, ZACHISTKA_ID) 
				values (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, 
				(select max(CUTDATE) from OPTIMAL_PIPES where OPTIMAL_PIPES.PIPE_NUMBER=?), 
				?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
            }
            else
            {
                //измение старой записи
                //признак передачи данных по старой записи в SAP
                bSapProcessed = CheckSapProcessed(UpdateRowID);

                //установка значения поля EDIT_STATE=0x01 если по старой записи данные не были переданы в SAP
                if (!bSapProcessed)
                {
                    cmd.CommandText = "update INSPECTION_PIPES set EDIT_STATE=1 where (row_id=?)";
                    cmd.Parameters.AddWithValue("row_id", UpdateRowID);
                    cmd.ExecuteNonQuery();
                }

                //если данные ранее переданы в SAP - статус актуальности добавляемой записи "неподтвержденное исправление"
                if (bSapProcessed)
                {
                    EDIT_STATE = 4;
                }

                //запрос на добавление новой записи в таблицу
                cmd.CommandText = @"insert into INSPECTION_PIPES (TRX_DATE, EDIT_STATE, AUTO_CAMPAIGN_FLAG, SHIFT_INDEX, PIPE_NUMBER, SHTRIPS_LOT_NUMBER, DIAMETER, THICKNESS, STEELMARK, 
                   EMPLOYEE_NUMBER, INVENTORY_CODE, INVENTORY_CODE_TARGET, LENGTH, WEIGHT, LOT_NUMBER, LOT_YEAR, COIL_SMELTING, ORDER_HEADER, ORDER_LINE, NTD_ID, GOST, GOST_GROUP, 
                   CUT_LEFT_DEFECTS, ZACHISTKA_DEFECT_ID, ZACHISTKA_LENGTH_MM, ZACHISTKA_THICKNESS_1, ZACHISTKA_THICKNESS_2, DELTA_SIZE_MM, CUT_RIGHT_DEFECTS, INSTRUCTION_NUMBER, 
                   INSTRUCTION_TYPE, CUT_LEFT_LENGTH, CUT_RIGHT_LENGTH, WORKPLACE_ID, OPERATOR_ID, ORIGINAL_ROWID, NEXT_DIRECTION, REASON, CAMPAIGN_LINE_ID, ORACLE_PROCESSED, SAP_PROCESSED, 
                   BAD_MARKING, PREV_DIRECTION, CUT_DATE, ROW_ID, DEFECT_DESCRIPTION, DEFECT_LOCATION, DEFECT_DISTANCE, NOT_ZAKAZ_REASON_ID, NOT_ZAKAZ_REASON_VALUE, NOT_ZAKAZ_REASON_DISTANCE, 
                   NOT_ZAKAZ_REASON_DESCRIPTION, PEREVOD_REASON_ID, ADDITIONAL_DEFECT_ID, KMK_MARKING_TEMPLATE_ID, LABEL_TYPE_ID, MASTER_CONFIRM_USERNAME, MAP_NAME, SGP_ROUTE_ID, PROFILE_SIZE_A, 
                   PROFILE_SIZE_B, CHECKMARK, TYPE_WEIGHT, QUALITY_KP, ZACHISTKA_AMOUNT_ZVS, ZACHISTKA_MINTHICKSHOV_ZVS, ZACHISTKA_MINTHICKTELO_ZVS, ZACHISTKA_FINAL_ID_ZVS, DEFECT_LOCATION_ID_ZVS, 
                   ZACHISTKA_WHOLE_LENGTH_ZVS, ZACHISTKA_CHECKBOX, DEFECT_ID_2, DELTA_SIZE_MM_2, DEFECT_DESCRIPTION_2, DEFECT_LOCATION_2, DEFECT_DISTANCE_2, DEFECT_ID_3, DELTA_SIZE_MM_3, 
                   DEFECT_DESCRIPTION_3, DEFECT_LOCATION_3, DEFECT_DISTANCE_3, ZACHISTKA_GOST, TEMPLATING_RESULT, ID_DEFECT_FOR_TEMPLATING, ZACHISTKA_ID)
                   values (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?,
                   (select oracle_processed from inspection_pipes where (row_id='" + UpdateRowID + "')and((INVENTORY_CODE<>'" + ObrInventoryCode + @"')or(INVENTORY_CODE is NULL))),
                   (select sap_processed from inspection_pipes where (row_id='" + UpdateRowID + "')and((INVENTORY_CODE<>'" + ObrInventoryCode + @"')or(INVENTORY_CODE is NULL))),
                   (select bad_marking from inspection_pipes where (row_id='" + UpdateRowID + "')and((INVENTORY_CODE<>'" + ObrInventoryCode + @"')or(INVENTORY_CODE is NULL))),
                   (select prev_direction from inspection_pipes where (row_id='" + UpdateRowID + "')and((INVENTORY_CODE<>'" + ObrInventoryCode + @"')or(INVENTORY_CODE is NULL))),
                   (select max(CUTDATE) from OPTIMAL_PIPES where OPTIMAL_PIPES.PIPE_NUMBER=?),
                   ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?,
                   (SELECT ip.zachistka_gost FROM tesc3.inspection_pipes ip WHERE ip.row_id = '" + UpdateRowID + "'), ?, ?, ?)";
            }

            //НТД
            String GOST = "";
            String GROUP = "";
            GetNDAndGroup(out GOST, out GROUP, ddlNTD);
            String NTD = txbNTD.Text.Trim();

            //№ и сторка заказа
            String OrderNo = txbZakazNo.Text;
            String OrderLine = txbZakazLine.Text;

            String ZachistkaGost = "";

            //направление трубы и код НД
            String NextDir = "SKLAD";
            if (bRepair)
            {
                NextDir = "REMONT";
                ZachistkaGost = GOST + (GROUP != "" ? " гр." + GROUP : "");
                NTD = "";
                InvCode = "";
                GOST = "";
                GROUP = "";
                OrderNo = "";
                OrderLine = "";
            }
            if (bBrak)
            {
                NextDir = "BRAK";
                NTD = "50";
                InvCode = "";
                GOST = "";
                GROUP = "";
                OrderNo = "";
                OrderLine = "";
            }

            //наименование и код дефекта
            String defect_name = ddlDefect.SelectedItem.Text.Replace("-", "").Trim().ToUpper();
            String defect_id = ddlDefect.SelectedItem.Value.Trim();

            //Признак некондиции
            bool NoCondition = false; // CheckNoCondition(InvCode);
            if (NoCondition)
            {
                OrderNo = "1";
                OrderLine = "1";
            }

            //длина трубы
            int Length = 0;
            Int32.TryParse(txbLength.Text.Trim(), out Length);
            Length = (int)(Length / 10.0 + 0.5) * 10;

            //идентификатор строки кампании
            String CampaignLineID = ddlCampaign.SelectedItem.Value;
            if (NextDir != "SKLAD") CampaignLineID = "";


            //значение отправляемое в ПЛК
            int route_sgp_id = 0, length_pipe = 0;
            double diameter = 0, thickness = 0;
            double.TryParse(ddlDiam.Text, out diameter);
            double.TryParse(ddlThickness.Text, out thickness);
            int.TryParse(txbLength.Text, out length_pipe);

            route_sgp_id = GetRouteID(GOST, WorkplaceId, diameter, thickness, length_pipe, GROUP);

            //передача параметров в запрос
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("TRX_DATE", GetActualDate());
            cmd.Parameters.AddWithValue("EDIT_STATE", EDIT_STATE);
            cmd.Parameters.AddWithValue("AUTO_CAMPAIGN_FLAG", Convert.ToByte(cbAutoCampaign.Checked));
            cmd.Parameters.AddWithValue("SHIFT_INDEX", Authentification.Shift);
            cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);
            cmd.Parameters.AddWithValue("SHTRIPS_LOT_NUMBER", ShtripsLotNumber);
            cmd.Parameters.AddWithValue("DIAMETER", Checking.GetDbType(ddlDiam.Text));
            cmd.Parameters.AddWithValue("THICKNESS", Checking.GetDbType(ddlThickness.Text));
            cmd.Parameters.AddWithValue("STEELMARK", ddlSteelmark.Text);
            cmd.Parameters.AddWithValue("EMPLOYEE_NUMBER", Authentification.User.TabNumber);
            cmd.Parameters.AddWithValue("INVENTORY_CODE", InvCode);
            //
            String InvCodeTarget = GetIctFromCompaigns(CampaignLineID);
            cmd.Parameters.AddWithValue("INVENTORY_CODE_TARGET", InvCodeTarget);
            //
            cmd.Parameters.AddWithValue("LENGTH", Length);
            cmd.Parameters.AddWithValue("WEIGHT", Checking.GetDbType(txbWeight.Text));
            cmd.Parameters.AddWithValue("LOT_NUMBER", txbPartNo.Text);
            cmd.Parameters.AddWithValue("LOT_YEAR", Checking.GetDbType(txbPartYear.Text));
            cmd.Parameters.AddWithValue("COIL_SMELTING", txbSmelting.Text);
            cmd.Parameters.AddWithValue("ORDER_HEADER", OrderNo.Trim());
            cmd.Parameters.AddWithValue("ORDER_LINE", OrderLine.Trim());
            cmd.Parameters.AddWithValue("NTD_ID", Checking.GetDbType(NTD));
            cmd.Parameters.AddWithValue("GOST", GOST);
            cmd.Parameters.AddWithValue("GROUP", GROUP);
            //-1,-2,-3,-4, -5 = письмо/распоряжение/заказ/протокол/акт
            cmd.Parameters.AddWithValue("CUT_LEFT_DEFECTS", (defect_id == "-1" | defect_id == "-2" | defect_id == "-3" | defect_id == "-4" | defect_id == "-5") ? "" : defect_id);
            cmd.Parameters.AddWithValue("ZACHISTKA_DEFECT_ID", ddlDefectZachistka.SelectedItem.Value.Trim());
            cmd.Parameters.AddWithValue("ZACHISTKA_LENGTH_MM", txbZachistkaLength.Text);
            cmd.Parameters.AddWithValue("ZACHISTKA_THICKNESS_1", Checking.GetDbType(txbZachistkaThickness1.Text));
            cmd.Parameters.AddWithValue("ZACHISTKA_THICKNESS_2", Checking.GetDbType(txbZachistkaThickness2.Text));
            cmd.Parameters.AddWithValue("DELTA_SIZE_MM", (ddlDeltaSize.SelectedIndex != 0) ? ddlDeltaSize.SelectedItem.Value : "");
            cmd.Parameters.AddWithValue("CUT_RIGHT_DEFECTS", "");
            cmd.Parameters.AddWithValue("INSTRUCTION_NUMBER", txbInstructionNumber.Text.Trim());
            cmd.Parameters.AddWithValue("INSTRUCTION_TYPE", ddlInstructionType.SelectedItem.Text);
            cmd.Parameters.AddWithValue("CUT_LEFT_LENGTH", "");
            cmd.Parameters.AddWithValue("CUT_RIGHT_LENGTH", "");
            cmd.Parameters.AddWithValue("WORKPLACE_ID", WorkplaceId);
            cmd.Parameters.AddWithValue("OPERATOR_ID", Authentification.User.UserName);
            cmd.Parameters.AddWithValue("ORIGINAL_ROWID", UpdateRowID);
            cmd.Parameters.AddWithValue("NEXT_DIRECTION", NextDir);
            cmd.Parameters.AddWithValue("REASON", Reason);
            cmd.Parameters.AddWithValue("CAMPAIGN_LINE_ID", Checking.GetDbType(CampaignLineID));
            if (UpdateRowID == "")
            {
                String par = "0";
                if (PipeNumber == BadMarkingPipeNumber) par = "1";
                cmd.Parameters.AddWithValue("BAD_MARKING", par);
                cmd.Parameters.AddWithValue("PREV_DIRECTION", GetPrevDirection(PipeNumber));
            }
            cmd.Parameters.AddWithValue("PIPE_NUMBER2", PipeNumber);
            cmd.Parameters.AddWithValue("NEW_ROW_ID", CreatedRowId);

            cmd.Parameters.AddWithValue("DEFECT_DESCRIPTION", txbDefectDescription.Text.Trim());
            cmd.Parameters.AddWithValue("DEFECT_LOCATION", ddlDefectLocation.SelectedItem.Value);
            cmd.Parameters.AddWithValue("DEFECT_DISTANCE", Checking.GetDbType(txbDefectDistance.Text));
            cmd.Parameters.AddWithValue("NOT_ZAKAZ_REASON_ID", ddlNotZakazReason.SelectedItem.Value);
            cmd.Parameters.AddWithValue("NOT_ZAKAZ_REASON_VALUE", Checking.GetDbType(txbNotZakazReasonValue.Text));
            cmd.Parameters.AddWithValue("NOT_ZAKAZ_REASON_DISTANCE", Checking.GetDbType(txbNotZakazReasonDistance.Text));
            cmd.Parameters.AddWithValue("NOT_ZAKAZ_REASON_DESCRIPTION", txbNotZakazReasonDescription.Text.Trim());
            cmd.Parameters.AddWithValue("PEREVOD_REASON_ID", ddlPerevodReason.SelectedItem.Value);
            cmd.Parameters.AddWithValue("ADDITIONAL_DEFECT_ID", ddlDefectAdditional.SelectedItem.Value.Trim());
            cmd.Parameters.AddWithValue("KMK_MARKING_TEMPLATE_ID", ddlLabelTypeKmk.SelectedItem.Value);
            cmd.Parameters.AddWithValue("LABEL_TYPE_ID", ddlLabelType.SelectedItem.Value);
            cmd.Parameters.AddWithValue("MASTER_CONFIRM_USERNAME", fldMasterConfirmLogin.Value);
            cmd.Parameters.AddWithValue("MAP_NAME", ddlPipeRouteMap.SelectedItem.Value);
            cmd.Parameters.AddWithValue("SGP_ROUTE_ID", route_sgp_id);

            cmd.Parameters.AddWithValue("PROFILE_SIZE_A", ddlProfileSize.SelectedItem.Text != "" ? ddlProfileSize.SelectedItem.Text.Split('x')[0] : "");
            cmd.Parameters.AddWithValue("PROFILE_SIZE_B", ddlProfileSize.SelectedItem.Text != "" ? ddlProfileSize.SelectedItem.Text.Split('x')[1] : "");

            cmd.Parameters.AddWithValue("CHECKMARK", CheckMark);

            if (cmd.Parameters["INSTRUCTION_TYPE"].ToString() == "")
                cmd.Parameters["INSTRUCTION_NUMBER"].Value = "";
            int TypeWeight = txbWeight.Text == lblWeightTemp.Text ? 0 : 1;
            cmd.Parameters.AddWithValue("TYPE_WEIGHT", TypeWeight);
            cmd.Parameters.AddWithValue("QYALITY_KP", Checking.GetDbType(ddlQualityKP.SelectedItem.Value));
            cmd.Parameters.AddWithValue("ZACHISTKA_AMOUNT_ZVS", cbScraping.Checked ? "" : Checking.GetDbType(txbAmounts.Text));
            cmd.Parameters.AddWithValue("ZACHISTKA_MINTHICKSHOV_ZVS", txbMinWallWeld.Enabled ? Checking.GetDbType(txbMinWallWeld.Text) : DBNull.Value);
            cmd.Parameters.AddWithValue("ZACHISTKA_MINTHICKTELO_ZVS", txbMinWallPipe.Enabled ? Checking.GetDbType(txbMinWallPipe.Text) : DBNull.Value);
            cmd.Parameters.AddWithValue("ZACHISTKA_FINAL_ID_ZVS", ddlResultScraping.SelectedItem.Value);
            cmd.Parameters.AddWithValue("DEFECT_LOCATION_ID_ZVS", ddlLocationScraping.SelectedItem.Value);
            cmd.Parameters.AddWithValue("ZACHISTKA_WHOLE_LENGTH_ZVS", cbScraping.Checked ? "Y" : "");
            cmd.Parameters.AddWithValue("ZACHISTKA_CHECKBOX", cbRepair.Checked ? "Y" : "");

            //дополнительные дефекты
            cmd.Parameters.AddWithValue("DEFECT_ID_2", (ddlDefect2.SelectedIndex > 0) ? ddlDefect2.SelectedItem.Value : "");
            cmd.Parameters.AddWithValue("DELTA_SIZE_MM_2", (ddlDeltaSize2.SelectedIndex != 0) ? ddlDeltaSize2.SelectedItem.Value : "");
            cmd.Parameters.AddWithValue("DEFECT_DESCRIPTION_2", txbDefectDescription2.Text.Trim());
            cmd.Parameters.AddWithValue("DEFECT_LOCATION_2", ddlDefectLocation2.SelectedItem.Value);
            cmd.Parameters.AddWithValue("DEFECT_DISTANCE_2", Checking.GetDbType(txbDefectDistance2.Text));
            cmd.Parameters.AddWithValue("DEFECT_ID_3", (ddlDefect3.SelectedIndex > 0) ? ddlDefect3.SelectedItem.Value : "");
            cmd.Parameters.AddWithValue("DELTA_SIZE_MM_3", (ddlDeltaSize3.SelectedIndex != 0) ? ddlDeltaSize3.SelectedItem.Value : "");
            cmd.Parameters.AddWithValue("DEFECT_DESCRIPTION_3", txbDefectDescription3.Text.Trim());
            cmd.Parameters.AddWithValue("DEFECT_LOCATION_3", ddlDefectLocation3.SelectedItem.Value);
            cmd.Parameters.AddWithValue("DEFECT_DISTANCE_3", Checking.GetDbType(txbDefectDistance3.Text));
            if (UpdateRowID == "")
                cmd.Parameters.AddWithValue("ZACHISTKA_GOST", ZachistkaGost);
            cmd.Parameters.AddWithValue("TEMPLATING_RESULT", ddlResultTemplate.SelectedItem.Text);
            cmd.Parameters.AddWithValue("ID_DEFECT_FOR_TEMPLATING", ddlDefectTemplate.SelectedValue);
            cmd.Parameters.AddWithValue("ZACHISTKA_ID", (ddlResultPrestar.SelectedIndex > 0) ? ddlResultPrestar.SelectedItem.Value : "");

            cmd.ExecuteNonQuery();

            //Обновление записи в таблице STORE_PRESTAR
            if (ddlResultPrestar.SelectedIndex > 0)
            {
                cmd.CommandText = "UPDATE STORE_PRESTAR SET ENTER_DATE=SYSDATE,   PIPE_NUMBER=? WHERE ID=?";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);
                cmd.Parameters.AddWithValue("id", ddlResultPrestar.SelectedItem.Value);
                cmd.ExecuteNonQuery();
            }

            //сохранение последней позиции трубы для отслеживания маршрута
            SetNewPositionOfPipe();

            //снятие отметки приемки на склад для последней трубы, если исправление
            if (UpdateRowID != "")
                ResetInspectionDateInSkladReturnActs(UpdateRowID);

            //отметка даты приемки трубы в актах возврата со склада
            if (NextDir == "SKLAD")
                SetInspectionDateInSkladReturnActs();

            //сброс флага "нечитаемая маркировка"
            BadMarkingPipeNumber = -1;

            //возврат признака не_передачи исправленной записи в SAP
            return !bSapProcessed;
        }
        finally
        {
            //
        }
    }



    //проверка, зафиксированы ли испытания трубы на гидропрессе
    //возвращает true если приемка возможна
    private bool CheckHydroTest(ControlOperations control_operation)
    {
        //выход, если форма открыта в режиме ПДО или отключено отслеживание маршрута
        if (IsPdoForm || DISABLE_ROUTE_CONTROL) return true;

        //при приемке профильных труб и на инспекциях 7-11 не проверять испытание на гидропрессе
        if (WorkplaceId == 63 || (WorkplaceId >= 80 && WorkplaceId <= 84))
            return true;

        if (!control_operation.isHydro) return true;

        try
        {
            //получение даты последнего гидроиспытания трубы            
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "select max(REC_DATE) from HYDROPRESS where PIPE_NUMBER=? and EDIT_STATE=0 GROUP BY PIPE_NUMBER " +
                                " HAVING  max(REC_DATE)>=Nvl((select Max(TRX_DATE)  from INSPECTION_PIPES where PIPE_NUMBER=? and EDIT_STATE=0 and WORKPLACE_ID=7),  max(REC_DATE))";
            cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);
            cmd.Parameters.AddWithValue("PIPE_NUMBER_INSP", PipeNumber);
            object o = cmd.ExecuteScalar();
            cmd.Dispose();

            //если труба не испытывалась на гидропрессе - приёмка невозможна
            if (o.ToString() == "")
            {
                Master.AlertMessage = "Внимание!\n\nНет информации по гидроиспытанию трубы. Необходим возврат трубы на повторную опрессовку.";
                return false;
            }

            //дата гидроиспытания трубы
            DateTime HydroTestDate = Convert.ToDateTime(o);

            //проверка установки флага "брак/годная" автоматически или вручную,
            //проверка наличия времени начала гидроиспытания для построения диаграммы
            bool bOk = true;
            cmd = conn.CreateCommand();
            cmd.CommandText = "select * from HYDROPRESS where EDIT_STATE=0 and REC_DATE=? and PIPE_NUMBER=?";
            cmd.Parameters.AddWithValue("REC_DATE", HydroTestDate);
            cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);
            OleDbDataReader rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                //проверка наличия флага "годная" автоматически или вручную
                bOk = (rdr["TEST_BRAK_MANUAL"].ToString() == "0");
                if (!bOk) bOk = (rdr["NEW_TEST_BRAK_AUTO"].ToString() == "0");
                if (!bOk)
                {
                    String autoStatus = "нет";
                    if (rdr["NEW_TEST_BRAK_AUTO"].ToString() == "0") autoStatus = "Пройдено";
                    if (rdr["NEW_TEST_BRAK_AUTO"].ToString() == "1") autoStatus = "Не пройдено";
                    String manualStatus = "нет";
                    if (rdr["TEST_BRAK_MANUAL"].ToString() == "0") manualStatus = "Пройдено";
                    if (rdr["TEST_BRAK_MANUAL"].ToString() == "1") manualStatus = "Не пройдено";
                    Master.AlertMessage = "Внимание!\n\nДля данной трубы не получен результат гидроиспытания. Необходим возврат трубы на повторную опрессовку.\n\n"
                        + "Дата ввода трубы на гидропрессе: " + Convert.ToDateTime(rdr["FIRST_REC_DATE"]).ToString("dd.MM.yyyy HH:mm") + "\n"
                        + "Результат (автоматический): " + autoStatus + "\n"
                        + "Результат (ручной): " + manualStatus;
                }

                //проверка наличия даты начала гидроиспытания для построения диаграммы
                if (bOk)
                {
                    bOk = (rdr["NEW_TEST_DATE"].ToString() != "");
                    if (!bOk)
                    {
                        Master.AlertMessage = "Внимание!\n\nДля данной трубы нет данных по времени начала гидроиспытания. Необходим возврат трубы на повторную опрессовку.\n\n"
                            + "Дата ввода трубы на гидропрессе: " + Convert.ToDateTime(rdr["FIRST_REC_DATE"]).ToString("dd.MM.yyyy HH:mm") + "\n"
                            + "Дата фактического испытания: нет данных";
                    }
                }
            }
            rdr.Close();
            rdr.Dispose();
            cmd.Dispose();

            return bOk;
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка получения состояния трубы на гидропрессе", ex);
            return false;
        }
    }


    //преобразование результата испытаний в текстовую форму
    protected String TestResultToString(object test_result)
    {
        if (test_result.ToString() == "1") return "Не пройдено";
        if (test_result.ToString() == "0") return "Пройдено";
        return "";
    }


    //проверка отсутствия дефектов УЗК, РУЗК
    //возвращает 1, если приёмка возможна
    //иначе возвращает 0 и отображает сообщение
    //приёмка невозможна, если имеются дефекты УЗК сварки и УЗК шва, без положительного результата перепроверки на РУЗК
    private bool CheckUsc(ControlOperations control_operation)
    {
        try
        {
            //выход, если форма открыта в режиме ПДО или отключено отслеживание маршрута
            if (IsPdoForm || DISABLE_ROUTE_CONTROL) return true;

            //не производить проверку для профильных труб и для инспекций 7-11
            if (WorkplaceId == 63 || (WorkplaceId >= 80 && WorkplaceId <= 84))
                return true;

            #region Проверка дефектов УЗК сварки и РУЗК
            //получение метки дефекта с УЗК сварки
            String usc_weld_defect = "";
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();

            using (OleDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "select cutdate, nvl(defects, 0) defects from optimal_pipes where pipe_number=? order by cutdate desc";
                cmd.Parameters.AddWithValue("pipe_number", PipeNumber);
                using (OleDbDataReader rdr = cmd.ExecuteReader())
                {
                    if (rdr.HasRows)
                    {
                        if (rdr.Read())
                        {
                            /*1 "Наружный дефект", */
                            /*4 "Внутренний дефект", */
                            /*5 "Потеря акустического контакта", */
                            /*10 "Дефект УЗК шва", */

                            int def = Convert.ToInt32(rdr["DEFECTS"]);
                            def = ((def >> 1) & 0x01 + (def >> 4) & 0x01 + (def >> 5) & 0x01 + (def >> 10) & 0x01);
                            if (def != 0) def = 1;
                            usc_weld_defect = TestResultToString(def);
                        }
                        rdr.Close();
                    }
                }
            }

            //получение последних результатов перепроверки РУЗК
            String ruzk_weld_defect = "Нет данных";
            String ruzk_otdelka_defect = "Нет данных";
            using (OleDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"select * 
                from USC_OTDELKA 
                where PIPE_NUMBER=? and EDIT_STATE=0 and workplace_id in (25, 26, 27, 28, 29, 30)
                   and first_rec_date>=Nvl((select Max(TRX_DATE)  from INSPECTION_PIPES where PIPE_NUMBER=? and EDIT_STATE=0 and WORKPLACE_ID=7),  first_rec_date)
                order by FIRST_REC_DATE desc";
                cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);
                cmd.Parameters.AddWithValue("PIPE_NUMBER_INSP", PipeNumber);
                using (OleDbDataReader rdr = cmd.ExecuteReader())
                {
                    if (rdr.HasRows)
                    {
                        if (rdr.Read())
                        {
                            ruzk_weld_defect = TestResultToString(rdr["TEST_BRAK_AFTER_STAN"]);
                            ruzk_otdelka_defect = TestResultToString(rdr["TEST_BRAK_MANUAL"]);
                        }
                        rdr.Close();
                    }
                }
            }

            //запрет приёмки, если имеется дефект РУЗК
            if (ruzk_otdelka_defect == "Не пройдено" || ruzk_weld_defect == "Не пройдено")
            {
                Master.AlertMessage = "Внимание!\n\nТруба не может быть принята, т.к. имееются дефекты РУЗК:\n"
                    + " - перепроверка УЗК сварки: " + ruzk_weld_defect + "\n"
                    + " - перепроверка УЗК шва отделки: " + ruzk_otdelka_defect + "\n";
                return false;
            }

            //запрет приёмки, если имеется дефект УЗК сварки и нет положительного результата перепроверки
            if (usc_weld_defect == "Не пройдено" && ruzk_weld_defect != "Пройдено")
            {
                Master.AlertMessage = "Внимание!\n\nТруба не может быть принята, т.к. имееются дефекты УЗК сварки без положительного результата перепроверки:\n"
                    + " - УЗК сварки: " + usc_weld_defect + "\n"
                    + " - результат перепроверки на РУЗК: " + ruzk_weld_defect + "\n";
                return false;
            }
            #endregion Проверка дефектов УЗК сварки и РУЗК

            #region Проверка УЗК шва, УЗК тела, УЗК концов
            //результаты испытаний
            DataTable dtTestResult = GetTestResultsTable();

            #region Рабочие центры
            //список рабочих центров Гидропресс
            List<int> listNotUSC = new List<int>() { 11, 12, 66, 67, 68, 69 };
            //список рабочих центров УЗК шва
            List<int> listUSC_S = new List<int>() { 13, 14 };
            //список рабочих центров УЗК тела
            List<int> listUSC_T = new List<int>() { 15, 64 };
            //список рабочих центров УЗК концов (левая установка)
            List<int> listUSC_K_L = new List<int>() { 16, 18 };
            //список рабочих центров УЗК концов (правая установка)
            List<int> listUSC_K_R = new List<int>() { 17, 19 };
            #endregion Рабочие центры

            foreach (DataRow dr in dtTestResult.Rows)
            {
                int temp_wp = Convert.ToInt32(dr["workplace_id"].ToString());

                //не проверяется гидропресс и МПК
                if (listNotUSC.Contains(temp_wp)) continue;

                //УЗК шва
                if (listUSC_S.Contains(temp_wp))
                {
                    if (!control_operation.isUSC_S) continue;
                    else
                    {
                        if (dr["result"].ToString() != "0" || (dr["pipe_defect"].ToString() != "" && dr["pipe_defect"].ToString() != "0"))
                        {
                            DateTime temp_date = DateTime.MinValue;
                            DateTime.TryParse(dr["rec_date"].ToString(), out temp_date);
                            if (!CheckRUSC("test_brak_manual", temp_date))
                            {
                                Master.AlertMessage = "Внимание!\n\nТруба не может быть принята, т.к. имееются дефекты УЗК шва линии отделки без положительного результата перепроверки.";
                                return false;
                            }
                        }
                    }
                }

                //УЗК тела
                if (listUSC_T.Contains(temp_wp))
                {
                    if (!control_operation.isUSC_T) continue;
                    else
                    {
                        if (dr["result"].ToString() != "0" || (dr["pipe_defect"].ToString() != "" && dr["pipe_defect"].ToString() != "0"))
                        {
                            DateTime temp_date = DateTime.MinValue;
                            DateTime.TryParse(dr["rec_date"].ToString(), out temp_date);
                            if (!CheckRUSC("manual_ausc_body_brak", temp_date))
                            {
                                Master.AlertMessage = "Внимание!\n\nТруба не может быть принята, т.к. имееются дефекты УЗК тела линии отделки без положительного результата перепроверки.";
                                return false;
                            }
                        }
                    }
                }

                //УЗК концов (левая)
                if (listUSC_K_L.Contains(temp_wp))
                {
                    if (!control_operation.isUSC_K) continue;
                    else
                    {
                        if (dr["result"].ToString() != "0" || (dr["pipe_defect"].ToString() != "" && dr["pipe_defect"].ToString() != "0"))
                        {
                            DateTime temp_date = DateTime.MinValue;
                            DateTime.TryParse(dr["rec_date"].ToString(), out temp_date);
                            if (!CheckRUSC("manual_ausc_end_left_brak", temp_date, true))
                            {
                                Master.AlertMessage = "Внимание!\n\nТруба не может быть принята, т.к. имееются дефекты УЗК концов (левая) линии отделки без положительного результата перепроверки.";
                                return false;
                            }
                        }
                    }
                }

                //УЗК концов (правая)
                if (listUSC_K_R.Contains(temp_wp))
                {
                    if (!control_operation.isUSC_K) continue;
                    else
                    {
                        if (dr["result"].ToString() != "0" || (dr["pipe_defect"].ToString() != "" && dr["pipe_defect"].ToString() != "0"))
                        {
                            DateTime temp_date = DateTime.MinValue;
                            DateTime.TryParse(dr["rec_date"].ToString(), out temp_date);
                            if (!CheckRUSC("manual_ausc_end_right_brak", temp_date, true))
                            {
                                Master.AlertMessage = "Внимание!\n\nТруба не может быть принята, т.к. имееются дефекты УЗК концов (правая) линии отделки без положительного результата перепроверки.";
                                return false;
                            }
                        }
                    }
                }
            }
            #endregion Проверка УЗК шва, УЗК тела, УЗК концов

            return true;

        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка получения данных по УЗК", ex);
            return false;
        }
    }


    /// <summary>Функция перепроверки РУЗК после УЗК</summary>
    /// <param name="parameter_rusc">Имя переменной РУЗК</param>
    /// <param name="date_usc">Дата проверки АУЗК</param>
    /// <param name="is_expanded">Учет дополнительных рабочих мест</param>
    /// <returns></returns>
    private bool CheckRUSC(string parameter_rusc, DateTime date_usc, bool is_expanded = false)
    {
        try
        {
            bool check_result = false;
            string workplace_ids = "25, 26, 27, 28, 29, 30, 89, 90"; //рабочие места с формы RUscOtdelka.aspx
            string expanded_workplace_ids = "51, 52, 53, 54, 55, 56"; //рабочие места с формы UscOtdelka.aspx (для УЗК концов)

            OleDbConnection conn = Master.Connect.ORACLE_TESC3();

            string SQL_USC = @"SELECT tbl.brak_manual
                                    FROM (SELECT CASE
                                                    WHEN uo.workplace_id IN (" + workplace_ids + @")
                                                    THEN
                                                       uo.{brak_manual}
                                                    ELSE
                                                       uo.test_brak_manual
                                                 END
                                                    brak_manual,
                                                 uo.rec_date
                                            FROM tesc3.usc_otdelka uo
                                           WHERE uo.edit_state = 0
                                                 AND uo.workplace_id IN
                                                        (" + workplace_ids + (is_expanded ? ", " + expanded_workplace_ids : "") + @")
                                                 AND uo.pipe_number = ?
                                                 AND uo.rec_date > ?) tbl
                                   WHERE tbl.brak_manual IS NOT NULL
                                ORDER BY tbl.rec_date DESC";

            using (OleDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = SQL_USC.Replace("{brak_manual}", parameter_rusc);
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("pipe_number", PipeNumber);
                cmd.Parameters.AddWithValue("rec_date", date_usc);
                using (OleDbDataReader rdr = cmd.ExecuteReader())
                {
                    if (rdr.HasRows)
                    {
                        if (rdr.Read())
                        {
                            check_result = rdr["brak_manual"].ToString() == "0";
                        }
                        rdr.Close();
                    }
                }
            }
            return check_result;
        }
        catch (Exception ex)
        {
            throw new Exception("Ошибка получения результата РУЗК (" + ex.Message + ")");
        }
    }


    //проверка наличия трубы в актах на предъявление
    //возвращает true если приемка возможна
    private bool CheckPresentationActs()
    {
        return true; // проверка отключена Красновым А.В. 19.10.2011   
        //выход, если форма открыта в режиме ПДО
        if (IsPdoForm) return true;

        try
        {
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "select * from PRESENTATION_ACTS where PIPE_NUMBER=? and EDIT_STATE=0 and IS_PROCESSED=0";
            cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);
            OleDbDataReader rdr = cmd.ExecuteReader();
            bool ok = true;
            if (rdr.Read())
            {
                ok = false;
                Master.AlertMessage = "Данная труба не может быть принята, т.к. она внесена в акт по забраковке труб, но не была предъявлена.\n"
                    + "(Акт о забраковке труб по вине поставщика металла от " + Convert.ToDateTime(rdr["ACT_DATE"]).ToString("dd.MM.yyyy") + " "
                    + rdr["ACT_NUMBER_EXT"].ToString() + ")\n"
                    + "Статус трубы в акте: " + rdr["STATUS"].ToString();
            }
            rdr.Close();
            rdr.Dispose();
            cmd.Dispose();
            return ok;
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка получения состояния трубы из актов по забраковке", ex);
            return false;
        }
    }


    //проверка факта предъявления трубы с дефектами "плена" и др.
    //возвращает true если труба была предъявлена и приемка возможна
    private bool CheckPipePresentation()
    {
        return true; // проверка отключена Красновым А.В. 19.10.2011       
        //выход, если форма открыта в режиме ПДО
        if (IsPdoForm) return true;

        try
        {
            //коды дефектов, по которым обязательно предъявление
            String def = ddlDefect.SelectedItem.Value;
            if (def != "374" & def != "369") return true;

            //наличие не предъявленных труб
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "select count(*) from PRESENTATION_ACTS where PIPE_NUMBER=? and EDIT_STATE=0 and IS_PROCESSED=0";
            cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);
            int not_processed_pipes = Convert.ToInt32(cmd.ExecuteScalar());

            //наличие предъявленных труб
            cmd.CommandText = "select count(*) from PRESENTATION_ACTS where PIPE_NUMBER=? and EDIT_STATE=0 and IS_PROCESSED<>0";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);
            int processed_pipes = Convert.ToInt32(cmd.ExecuteScalar());
            cmd.Dispose();

            //труба может быть принята, если она присутствует в актах и была предъявлена
            if (processed_pipes > 0 & not_processed_pipes == 0)
                return true;
            else
            {
                Master.AlertMessage = "Данная труба не может быть принята, т.к. она не была предъявлена.";
                return false;
            }
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка проверки предъявления трубы", ex);
            return false;
        }
    }


    //проверка наличия трубы на СГП или в актах возврата на ремонт
    //возвращает true если труба есть на СГП или в актах
    protected bool CheckPipeOnSklad()
    {
        OleDbConnection conn = Master.Connect.ORACLE_TESC3();
        OleDbCommand cmd = conn.CreateCommand();
        cmd.CommandText = "select to_char(PIPE_NUMBER) from SKLAD_RETURN where EDIT_STATE=0 and PIPE_NUMBER=? "
            + "union select to_char(SUBLOT_NUMBER) from ORACLE.V_T3_PIPE_STORAGE where (SUBLOT_NUMBER+PRODUCER_YEAR*1E6)=? and PRODUCER_ORG_ID=127";
        cmd.Parameters.AddWithValue("PIPE_NUMBER1", PipeNumber);
        cmd.Parameters.AddWithValue("PIPE_NUMBER2", PipeNumber);
        OleDbDataReader rdr = cmd.ExecuteReader();

        bool b = rdr.Read();
        rdr.Close();
        rdr.Dispose();
        cmd.Dispose();
        return b;
    }


    //проверка корректности указания параметров ремонта зачисткой
    protected bool CheckZachistka()
    {
        if (ddlZachistkaEnabled.SelectedIndex < 1)
        {
            Master.AlertMessage = "Необходимо указать, производился ли ремонт трубы зачисткой.";
            return false;
        }
        else
        {
            if (ddlZachistkaEnabled.SelectedIndex == 1 &&
                (ddlDefectZachistka.SelectedItem.Text == "" || txbZachistkaLength.Text.Trim() == "" || txbZachistkaThickness1.Text.Trim() == "" || txbZachistkaThickness2.Text.Trim() == ""))
            {
                Master.AlertMessage = "Необходимо указать дефект, длину зачистки и толщину стенки до и после зачистки";
                return false;
            }

            if (ddlZachistkaEnabled.SelectedIndex == 1)
            {
                //дефект зачистки
                String def = ddlDefectZachistka.SelectedItem.Value;

                //проверка допустимых диапазонов параметров зачистки
                int PipeLength = 0;
                int ZachistkaLength = 0;
                Decimal Thickness1 = 0;
                Decimal Thickness2 = 0;
                Int32.TryParse(txbLength.Text.Trim(), out PipeLength);
                Int32.TryParse(txbZachistkaLength.Text.Trim(), out ZachistkaLength);
                Decimal.TryParse(txbZachistkaThickness1.Text.Replace('.', ',').Trim(), out Thickness1);
                Decimal.TryParse(txbZachistkaThickness2.Text.Replace('.', ',').Trim(), out Thickness2);
                if (ZachistkaLength <= 0 || ZachistkaLength > PipeLength)
                {
                    Master.AlertMessage = "Длина зачистки должна быть больше нуля и меньше длины трубы";
                    return false;
                }
                if (Thickness1 == 0 || Thickness2 == 0)
                {
                    if (def != "310" && def != "351")
                    {
                        Master.AlertMessage = "Неверно указана толщина стенки до и после зачистки.";
                        return false;
                    }
                }
                if (Thickness2 > Thickness1)
                {
                    Master.AlertMessage = "Неверно указана толщина стенки до и после зачистки. Толщина стенки после зачистки должна быть больше толщины стенки до зачистки.";
                    return false;
                }
                if (Thickness1 < 4 || Thickness1 >= 13 || Thickness2 < 4 || Thickness2 >= 13)
                {
                    Master.AlertMessage = "Толщина стенки до и после зачистки должна быть в пределах от 4,00 до 12,99 мм";
                    return false;
                }
                if (Thickness1 * 100 % 1 != 0 || Thickness2 * 100 % 1 != 0)
                {
                    Master.AlertMessage = "Толщина стенки до и после зачистки должна быть указана с точностью до десятых или до сотых";
                    return false;
                }

                //проверка допустимых дефектов зачистки
                OleDbCommand cmd = Master.Connect.ORACLE_TESC3().CreateCommand();
                cmd.CommandText = "select count(*) from SPR_DEFECT where ID=? and ZACHISTKA_ENABLED=1";
                cmd.Parameters.AddWithValue("ID", def.Trim());
                int c = Convert.ToInt32(cmd.ExecuteScalar());
                cmd.Dispose();
                if (c == 0)
                {
                    Master.AlertMessage = "Указан недопустимый дефект для ремонта зачисткой";
                    return false;
                }
            }
        }
        return true;
    }


    /// <summary>
    /// Проверка отбора проб от труб, назначенные для первичных сдаточных испытаний
    /// </summary>
    /// <returns>Возвращает True, если нет замечаний по отбору проб, иначе False и показывает сообщение</returns>
    protected bool CheckPipeForProbes()
    {
        //выход, если форма открыта в режиме ПДО
        if (IsPdoForm || DISABLE_ROUTE_CONTROL) return true;

        //получение даты производства трубы        
        OleDbConnection conn = Master.Connect.ORACLE_TESC3();
        OleDbCommand cmd = conn.CreateCommand();

        //для труб произведенных ранее чем 01.07.2014 проверки не производятся
        if (CheckOldPipe()) return true;

        //для профильных труб проверки не производятся
        if (CheckProfilePipe()) return true;

        //отключение проверки для труб с резервными номерами
        if (Checking.CheckReserveNumber(PipeNumber)) return true;

        //признак назначения трубы на отбор проб
        bool isPipeSetOnSamples = Sampling.PipeIsSetOnSampling(cmd, PipeNumber);

        //признак фактического отбора проб от трубы
        bool isPipeCutSampling = Sampling.PipeCutSampling(cmd, PipeNumber);

        //если труба была назначенной на отбор проб и от неё были отобраны пробы - не производить дальнейшие проверки
        if (isPipeSetOnSamples && isPipeCutSampling) return true;

        //Если на трубы, назначенные на отбор проб, отсутствует запись в АСПТП по величине обрези с дефектом «проба механическая»
        if (isPipeSetOnSamples && !isPipeCutSampling)
        {
            Master.AlertMessage = "От данной трубы необходимо произвести отбор проб для сдаточных испытаний.";
            return false;
        }

        //признак назначения партии на отбор проб
        int lotNumber = Convert.ToInt32(txbPartNo.Text.Trim());
        int lotYear = Convert.ToInt32(txbPartYear.Text.Trim());
        bool isLotSetOnSamples = false;
        String samplingErrorMessage = "";
        try
        {
            isLotSetOnSamples = Sampling.PartIsSetOnSampling(cmd, lotYear.ToString(), lotNumber.ToString());
        }
        catch (Exception ex)
        {
            samplingErrorMessage = ex.Message;
        }

        //если партия не назначена на отбор проб, то запрет на приемку труб в эту партию
        if (!isLotSetOnSamples)
        {
            if (samplingErrorMessage == "")
                Master.AlertMessage = "Трубу данной партии принять не возможно, так как на данную партию не назначены трубы на отбор проб. " +
                                      "Сообщите об этом по телефону 66-48 (УСТ) и бригадиру УОТТ. После назначения труб на данную партию повторите попытку приемки данной трубы.";
            else
                Master.AlertMessage = samplingErrorMessage;
            return false;
        }

        return true;
    }


    //проверка указания величины смещения
    protected bool CheckDeltaSize()
    {
        String defId = ddlDefect.SelectedItem.Value;
        if (defId == "107" || defId == "345") // "смещение" и "настр.смещение"
        {
            if (ddlDeltaSize.SelectedIndex <= 0)
            {
                Master.AlertMessage = "Не указана величина смещения.";
                return false;
            }
        }
        return true;
    }


    /// <summary>
    /// Проверка возможности приемки трубы, принятой на кампанию, по заданному допустимому ограничению длины.
    /// Возвращает true, если приемка возможна.
    /// Возвращает false и отображает сообщение, если приемка невозможна
    /// </summary>
    /// <returns></returns>
    private bool CheckCampaignMinMaxLength()
    {
        //если труба принимается не на кампанию, то разрешение приемки без проверки длины
        if (!cbShowCampaigns.Checked || ddlCampaign.SelectedItem.Value == "") return true;

        //длина трубы из поля ввода
        int pipeLength = 0;

        int.TryParse(txbLength.Text.Trim(), out pipeLength);

        //получение объема производства, предельно допустимых длин и порогового процента труб ограниченной длины из задания на кампанию
        int campaignLineId = Convert.ToInt32(ddlCampaign.SelectedItem.Value);
        int lengthMin1 = 0;
        int lengthMin2 = 0;
        int lengthMax = 0;
        double lengthMin1Percent = 0;
        double lengthMin2Percent = 0;
        double steel_out = 0;
        using (OleDbCommand cmd = Master.Connect.ORACLE_TESC3().CreateCommand())
        {
            cmd.CommandText = @"SELECT cmp.length_min_1,
                                       cmp.length_min_2,
                                       cmp.length_max,
                                       cmp.length_min_1_percent,
                                       cmp.length_min_2_percent,
                                       cmp.steel_out
                                  FROM campaigns cmp
                                 WHERE cmp.edit_state = 0 AND cmp.campaign_line_id = ?";

            cmd.Parameters.AddWithValue("CAMPAIGN_LINE_ID", campaignLineId);
            using (OleDbDataReader rdr = cmd.ExecuteReader())
            {
                if (rdr.Read())
                {
                    int.TryParse(rdr["length_min_1"].ToString(), out lengthMin1);
                    int.TryParse(rdr["length_min_2"].ToString(), out lengthMin2);
                    int.TryParse(rdr["length_max"].ToString(), out lengthMax);
                    double.TryParse(rdr["length_min_1_percent"].ToString(), out lengthMin1Percent);
                    double.TryParse(rdr["length_min_2_percent"].ToString(), out lengthMin2Percent);
                    double.TryParse(rdr["steel_out"].ToString(), out steel_out);
                }
            }
        }

        //упорядочивание предельных длин: lengthMin1 должно быть меньше чем lengthMin2
        if (lengthMin1 > lengthMin2)
        {
            int t = lengthMin1;
            lengthMin1 = lengthMin2;
            lengthMin2 = t;
            double p = lengthMin1Percent;
            lengthMin1Percent = lengthMin2Percent;
            lengthMin2Percent = p;
        }

        //проверка ограничения максимальной длины
        if (lengthMax > 0 && pipeLength > lengthMax)
        {
            Master.AlertMessage = "Длина трубы не соответствует требованиям, указанным в задании на кампанию. Превышение максимальной длины.";
            return false;
        }

        //если длина трубы в пределах номинальной
        if (pipeLength >= lengthMin1 && pipeLength >= lengthMin2) return true;

        if (steel_out > 0)
        {
            //получение теоретического веса трубы
            double theor_weight = 0;
            String inventory_code = Checking.CorrectInventoryNumber(txbInventoryNumber.Text);
            using (OleDbCommand cmd = Master.Connect.ORACLE_TESC3().CreateCommand())
            {
                cmd.CommandText = "SELECT NVL (conversion_rate, 0) conversion_rate FROM oracle.v_t3_pipe_items WHERE org_id = 127 AND nomer = ?";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("nomer", inventory_code);
                using (OleDbDataReader rdr = cmd.ExecuteReader())
                {
                    if (rdr.HasRows)
                    {
                        if (rdr.Read())
                        {
                            double conversion_rate = 0;
                            double.TryParse(rdr["conversion_rate"].ToString(), out conversion_rate);
                            theor_weight = Math.Round(conversion_rate * pipeLength / 1000, 3);
                        }
                        rdr.Close();
                    }
                }
            }

            //получение суммы веса труб по диапазонам длины, принятых в кампанию на окончательной приемке
            double weightMin1 = 0;
            double weightMin2 = 0;
            using (OleDbCommand cmd = Master.Connect.ORACLE_TESC3().CreateCommand())
            {
                cmd.CommandText = @"SELECT NVL (SUM (NVL (tesc3.pipes.weight (ip.inventory_code,
                                                                              NULL,
                                                                              NULL,
                                                                              ip.LENGTH),
                                                          0)),
                                                0)
                                      FROM inspection_pipes ip
                                     WHERE     ip.edit_state = 0
                                           AND ip.next_direction = 'SKLAD'
                                           AND ip.campaign_line_id = ?
                                           AND ip.LENGTH < ?
                                           AND ip.pipe_number != ?
                                           AND ip.trx_date =
                                                  (SELECT MAX (ip_.trx_date)
                                                     FROM tesc3.inspection_pipes ip_
                                                    WHERE ip_.edit_state = 0 AND ip_.pipe_number = ip.pipe_number)";

                cmd.Parameters.AddWithValue("campaign_line_id", campaignLineId);
                cmd.Parameters.AddWithValue("LENGTH", lengthMin1);
                cmd.Parameters.AddWithValue("pipe_number", PipeNumber);
                weightMin1 = Convert.ToDouble(cmd.ExecuteScalar());

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("campaign_line_id", campaignLineId);
                cmd.Parameters.AddWithValue("LENGTH", lengthMin2);
                cmd.Parameters.AddWithValue("pipe_number", PipeNumber);
                weightMin2 = Convert.ToDouble(cmd.ExecuteScalar());
            }

            //проверка более "жесткого" предела по минимальной длине        
            if (lengthMin1 > 0 && pipeLength < lengthMin1)
            {
                if ((weightMin1 + theor_weight) / steel_out * 100.0 > lengthMin1Percent)
                {
                    Master.AlertMessage = "Превышение по весу принятых труб с ограничением длиной не менее " + (lengthMin1 / 1000.0).ToString() + " м в размере до "
                                          + lengthMin1Percent.ToString() + "% от объема производства строки заказа.";
                    return false;
                }
            }

            //проверка более "мягкого" предела по минимальной длине        
            if (lengthMin2 > 0 && pipeLength < lengthMin2)
            {
                if ((weightMin2 + theor_weight) / steel_out * 100.0 > lengthMin2Percent)
                {
                    Master.AlertMessage = "Превышение по весу принятых труб с ограничением длиной не менее " + (lengthMin2 / 1000.0).ToString() + " м в размере до "
                                          + lengthMin2Percent.ToString() + "% от объема производства строки заказа.";
                    return false;
                }
            }
        }

        return true;
    }


    //возврат к изменению данных для сохранения и печати бирки
    protected void btnSaveMsgBack_Click(object sender, EventArgs e)
    {
        MainMultiView.SetActiveView(InputDataView);
    }


    //обновление инф. о трубе при выборе НТД
    protected void ddlNTD_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (sender != null) RebuildInventoryNumberList();
        try
        {


            txbNTD.Text = GetNDCode(ddlNTD);
        }
        catch
        { }
        String InvCode = Checking.CorrectInventoryNumber(txbInventoryNumber.Text);

        //отображение предупреждения об отсутствии номенклатурного номера
        pnlInventoryNumberNotFound.Visible = (InvCode == "") | (ddlInventoryNumber.SelectedItem.Text == "")
                                           | (ddlCampaign.SelectedIndex < 1) & (cbShowCampaigns.Checked);
        String msg = "";
        if (!cbShowCampaigns.Checked)
        {
            if ((WorkplaceId >= 1 && WorkplaceId <= 6) || WorkplaceId == 85)
            {
                SetTemplateValue();
            }
            if ((ddlDiam.SelectedItem.Text == "" && ddlProfileSize.SelectedItem.Text == "") && WorkplaceId != 63) msg += "Не указан диаметр или размер профиля<br/>";
            if (ddlThickness.SelectedItem.Text == "") msg += "Не указана толщина стенки<br/>";
            if (ddlSteelmark.SelectedItem.Text == "") msg += "Не указана марка стали<br/>";
            if (WorkplaceId != 7)
            {
                if (ddlNTD.SelectedItem.Text == "") msg += "Не выбран НД<br/>";
                if ((msg == "") & (ddlInventoryNumber.Items.Count < 2)) msg = "Указано несуществующее сочетание диаметра, толщины стенки, марки стали и НД<br/>";
                if ((msg == "") & (ddlInventoryNumber.SelectedItem.Text == "")) msg = "Номер номенклатурной позиции не указан или указан неверно<br/>";
            }
        }
        else
        {
            if (ddlCampaign.SelectedIndex < 1) msg = "Не выбрана строка кампании";
        }

        lblInventoryNumberNotFound.Text = msg;
        if (msg == "")
            pnlInventoryNumberNotFound.Visible = false;
    }


    //отображение панели ввода данных обрези
    /*protected void btnCutting_Click(object sender, EventArgs e)
    {
        CuttingMultiView.SetActiveView(CuttingOnView);
    }*/


    //возврат к вводу другого номера трубы если она не найдена в БД
    protected void btnNoPipeBack_Click(object sender, EventArgs e)
    {
        MainMultiView.SetActiveView(FindPipesView);
    }


    //ввод данных по трубе с резервным номером
    protected void btnNoPipeCreateNew_Click(object sender, EventArgs e)
    {
        GetPipeInfo(PipeNumber.ToString());
        ClearInputFields();
        TabsVisible = false;
        MainMultiView.SetActiveView(InputDataView);
        lblYear.Text = txbYear.Text;
        lblPipeNo.Text = txbPipeNumber.Text;
        ddlNTD_SelectedIndexChanged(sender, e);

        //запоминание номера трубы как текущего
        Checking.SaveLastPipeNumber(lblYear.Text, lblPipeNo.Text, txbCheck.Text);

        //получение массы трубы, если рабочее место = инспекции КЛО        
        if (WorkplaceId >= 4 & WorkplaceId <= 6)
        {
            SetWeightText(GetWeightFromIp21());
        }

        //обновление списка кампаний
        RebuildCampaignList();

        //сброс флага "данные сохранены"
        DataSaved = false;
    }



    //признак того, что труба последний раз была на ремонте (данные из INSPECTION_PIPES)
    private bool PipeFromRepair
    {
        get
        {
            object ovalue = ViewState["PipeFromRepair"];
            if (ovalue == null) return false; else return Convert.ToBoolean(ovalue);
        }
        set { ViewState["PipeFromRepair"] = value; }
    }

    //признак того, что труба последний раз была на инспекциях (данные из INSPECTION_PIPES)
    private bool PipeFromInspection
    {
        get
        {
            object ovalue = ViewState["PipeFromInspection"];
            if (ovalue == null) return false; else return Convert.ToBoolean(ovalue);
        }
        set { ViewState["PipeFromInspection"] = value; }
    }



    //выделение активной закладки
    protected void ActivateTab(HtmlTableCell td, LinkButton label)
    {
        td.BgColor = "white";
        td.Style["border-top"] = "gray 1px solid";
        td.Style["border-left"] = "gray 1px solid";
        td.Style["border-right"] = "gray 1px solid";
        td.Style["border-bottom"] = "";
        label.ForeColor = Color.Black;
    }

    //выделение неактивной закладки
    protected void DeactivateTab(HtmlTableCell td, LinkButton label)
    {
        td.BgColor = "";
        td.Style["border-top"] = "";
        td.Style["border-left"] = "";
        td.Style["border-right"] = "";
        td.Style["border-bottom"] = "gray 1px solid";
        label.ForeColor = Color.Gray;
    }

    //скрытие/отображение панели закладок
    protected bool TabsVisible
    {
        get
        {
            return TabsVisible;
        }
        set
        {
            if (value == true)
            {
                pnlTabs.Visible = true;
                pnlMainContentPanel.Style["border-left"] = "1px solid gray";
                pnlMainContentPanel.Style["border-right"] = "1px solid gray";
                pnlMainContentPanel.Style["border-bottom"] = "1px solid gray";
            }
            else
            {
                pnlTabs.Visible = false;
                pnlMainContentPanel.Style["border-left"] = "";
                pnlMainContentPanel.Style["border-right"] = "";
                pnlMainContentPanel.Style["border-bottom"] = "";
            }
        }
    }

    //переключение на вкладку "Ввод данных"
    protected void btnSetInputDataTab_Click(object sender, EventArgs e)
    {
        ActivateTab(tdInputDataTab, btnInputDataTab);
        DeactivateTab(tdEditDataTab, btnEditDataTab);
        MainMultiView.SetActiveView(FindPipesView);
    }

    //переключение на вкладку "изменение данных"
    protected void btnEditDataTab_Click(object sender, EventArgs e)
    {
        DeactivateTab(tdInputDataTab, btnInputDataTab);
        ActivateTab(tdEditDataTab, btnEditDataTab);

        //заполнение полей формы поиска
        if (!IsPdoForm)
        {
            SelectDDLItemByValue(ddlOperatorFIO2, Authentification.User.UserName);
            SelectDDLItemByValue(ddlWorkPlace2, WorkplaceId.ToString());
        }
        txbPipeNumber2.Text = txbPipeNumber.Text.Trim();
        txbYear2.Text = txbYear.Text;
        if (txbPipeNumber2.Text != "")
        {
            cldStartDate.SelectedDate = new DateTime(Convert.ToInt32(txbYear2.Text) + 2000, 1, 1);
        }

        //переключение вида
        MainMultiView.SetActiveView(FindForEditView);
        btnFindForEdit_Click(sender, e);

    }


    //удаление записи
    protected void DeleteRecord(String rowid)
    {
        try
        {
            //проверка интервала редактирования записи с момента последнего сохранения
            if (!CheckCanEditRecByTime(rowid))
            {
                btnFindForEdit_Click(btnFindForEdit, EventArgs.Empty);
                return;
            }

            //подключение к БД            
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();

            //получение номера удаляемой трубы
            cmd.CommandText = "select * from INSPECTION_PIPES WHERE row_id=?";
            cmd.Parameters.AddWithValue("row_id", rowid);
            using (OleDbDataReader reader = cmd.ExecuteReader())
            {
                reader.Read();

                //проверка возможности правки/удаления данных согласно маршрутной карте
                if (!CheckCanEditRecByPipe(Convert.ToInt32(reader["PIPE_NUMBER"])))
                {
                    btnFindForEdit_Click(btnFindForEdit, EventArgs.Empty);
                    return;
                }
            }

            //флаг передачи данных по трубе в SAP
            bool bSapProcessed = CheckSapProcessed(rowid);
            int EDIT_STATE = 3; //по умолчанию вид записи - "подтверждение удаления"            

            //пометка существующей записи как удаленной, если запись ранее не была передана в SAP
            if (!bSapProcessed)
            {
                cmd.Parameters.Clear();
                cmd.CommandText = "update inspection_pipes set EDIT_STATE=2 where (row_id=?)";
                cmd.Parameters.AddWithValue("row_id", rowid);
                cmd.ExecuteNonQuery();
            }
            else
            {
                //если удаление не разрешено - вид записи "отклоненная попытка удаления"
                EDIT_STATE = 5;

                //сообщение о невозможности удаления записи, переданной в SAP
                Master.AlertMessage = "Данные по указанной трубе были переданы в SAP. Удаление записи по приемке трубы невозможно. " +
                                      "Все необходимые изменения по данной трубе оформить на бумажном носителе и передать старшему сменному мастеру.";
            }

            //добавление записи о удалении
            cmd.Parameters.Clear();
            cmd.CommandText = "insert into inspection_pipes "
                            + "(       TRX_DATE, PIPE_NUMBER, EMPLOYEE_NUMBER, SHIFT_INDEX, WORKPLACE_ID, OPERATOR_ID, EDIT_STATE, ORACLE_PROCESSED, SAP_PROCESSED, BAD_MARKING, ORIGINAL_ROWID) "
                            + "(select ?,        PIPE_NUMBER, ?,               ?,           ?,            ?,           ?,          ORACLE_PROCESSED, SAP_PROCESSED, BAD_MARKING, ? "
                            + "from inspection_pipes where (row_id=?))";
            cmd.Parameters.AddWithValue("TRX_DATE", GetActualDate());
            cmd.Parameters.AddWithValue("EMPLOYEE_NUMBER", Authentification.User.TabNumber);
            cmd.Parameters.AddWithValue("SHIFT_INDEX", Authentification.Shift);
            cmd.Parameters.AddWithValue("WORKPLACE_ID", WorkplaceId);
            cmd.Parameters.AddWithValue("OPERATOR_ID", Authentification.User.UserName);
            cmd.Parameters.AddWithValue("EDIT_STATE", EDIT_STATE);
            cmd.Parameters.AddWithValue("ORIGINAL_ROWID1", rowid);
            cmd.Parameters.AddWithValue("ORIGINAL_ROWID1", rowid);
            cmd.ExecuteNonQuery();

            //удаление последней позиции трубы для отслеживания маршрутов, 
            //если произведено фактическое удаление записи
            if (!bSapProcessed)
            {
                DeleteLastPositionOfPipe(rowid);
            }

            //снятие отметки о приемке в актах возврата
            if (!bSapProcessed)
            {
                ResetInspectionDateInSkladReturnActs(rowid);
            }

            //обновление данных
            btnFindForEdit_Click(null, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка удаления записи", ex);
        }
    }


    //переключение вида и чтение данных для редактирования записи
    protected void BeginEditRecord(String rowid)
    {
        try
        {
            //очистка предупреждений
            ClearWarnings();

            //обновление списка кампаний
            RebuildCampaignList();

            //получение данных существующей записи из БД            
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();
            cmd.CommandText =
                @"SELECT IP.*
                      FROM    INSPECTION_PIPES IP                        
                      WHERE row_id=?";

            cmd.Parameters.AddWithValue("row_id", rowid);
            OleDbDataReader reader = cmd.ExecuteReader();
            reader.Read();

            //проверка возможности правки данных согласно маршрутной карте
            if (!CheckCanEditRecByPipe(Convert.ToInt32(reader["PIPE_NUMBER"])))
            {
                btnFindForEdit_Click(btnFindForEdit, EventArgs.Empty);
                return;
            }

            //проверка интервала редактирования записи с момента последнего сохранения
            if (!CheckCanEditRecByTime(rowid))
            {
                btnFindForEdit_Click(btnFindForEdit, EventArgs.Empty);
                return;
            }

            //запоминание rowid редактируемой записи
            UpdateRowID = rowid;

            //получение данных в поля ввода
            ClearInputFields();
            PipeNumber = Convert.ToInt32(reader["PIPE_NUMBER"]); //заполнение полей год и номер трубы
            cldDate.SelectedDate = Convert.ToDateTime(reader["TRX_DATE"]);
            lblPipeNo.Text = txbPipeNumber.Text;
            lblYear.Text = txbYear.Text;
            ddlNotZakazReason.SelectedIndex = 0;
            txbNotZakazReasonDescription.Text = "";
            txbNotZakazReasonDistance.Text = "";
            txbNotZakazReasonValue.Text = "";
            fldNotZakazCheckOk.Value = "";

            //получение данных о трубе со стана и др.
            GetPipeInfo(PipeNumber.ToString());

            txbPartYear.Text = reader["LOT_YEAR"].ToString();
            txbZakazNo.Text = reader["ORDER_HEADER"].ToString();
            txbZakazLine.Text = reader["ORDER_LINE"].ToString();
            txbWeight.Text = reader["WEIGHT"].ToString();
            lblWeightTemp.Text = reader["WEIGHT"].ToString();
            txbWeight.BackColor = Color.White;
            txbLength.Text = reader["LENGTH"].ToString();
            txbSmelting.Text = reader["COIL_SMELTING"].ToString();

            txbPartNo.Text = reader["LOT_NUMBER"].ToString();
            ddlDiam.Text = reader["DIAMETER"].ToString().Replace('.', ',');
            SelectDDLItemByValue(ddlProfileSize, reader["PROFILE_SIZE_A"].ToString() != "" ? reader["PROFILE_SIZE_A"].ToString() + "x" + reader["PROFILE_SIZE_B"].ToString() : "");
            ddlThickness.Text = reader["THICKNESS"].ToString().Replace('.', ',');
            SelectDDLItemByValue(ddlSteelmark, reader["STEELMARK"].ToString());

            //кампания
            string campaign_line_id = reader["CAMPAIGN_LINE_ID"].ToString();
            SelectDDLItemByValue(ddlCampaign, campaign_line_id);
            hfInventoryNumberKP.Value = GetCampaignValue(campaign_line_id, "INVENTORY_CODE_KP");

            SetNDAndGroup(reader["GOST"].ToString(), reader["GOST_GROUP"].ToString(), ddlNTD);
            RebuildInventoryNumberList();
            SelectDDLItemByText(ddlInventoryNumber, reader["INVENTORY_CODE"].ToString());
            txbInventoryNumber.Text = ddlInventoryNumber.SelectedItem.Text;

            SelectDDLItemByValue(ddlDefect, reader["CUT_LEFT_DEFECTS"].ToString().Trim());
            SelectDDLItemByValue(ddlDefectAdditional, reader["ADDITIONAL_DEFECT_ID"].ToString().Trim());
            txbDefectDescription.Text = reader["DEFECT_DESCRIPTION"].ToString();
            txbDefectDistance.Text = reader["DEFECT_DISTANCE"].ToString();
            SelectDDLItemByValue(ddlDefectLocation, reader["DEFECT_LOCATION"].ToString());
            if (reader["ZACHISTKA_DEFECT_ID"].ToString() == "")
                ddlZachistkaEnabled.SelectedIndex = 2;
            else
                ddlZachistkaEnabled.SelectedIndex = 1;
            SelectDDLItemByValue(ddlDefectZachistka, reader["ZACHISTKA_DEFECT_ID"].ToString());
            txbZachistkaLength.Text = reader["ZACHISTKA_LENGTH_MM"].ToString();
            txbZachistkaThickness1.Text = reader["ZACHISTKA_THICKNESS_1"].ToString();
            txbZachistkaThickness2.Text = reader["ZACHISTKA_THICKNESS_2"].ToString();

            txbInstructionNumber.Text = reader["INSTRUCTION_NUMBER"].ToString();
            txbDefectDescription.Text = reader["DEFECT_DESCRIPTION"].ToString();
            SelectDDLItemByValue(ddlPerevodReason, reader["PEREVOD_REASON_ID"].ToString());

            foreach (ListItem it in ddlInstructionType.Items)
            {
                if (it.Text.ToUpper() == reader["INSTRUCTION_TYPE"].ToString().ToUpper())
                {
                    SelectDDLItemByText(ddlInstructionType, it.Text);
                    break;
                }
            }

            SelectDDLItemByValue(ddlLabelType, reader["LABEL_TYPE_ID"].ToString());
            SelectDDLItemByValue(ddlLabelTypeKmk, reader["KMK_MARKING_TEMPLATE_ID"].ToString());
            SelectDDLItemByValue(ddlDeltaSize, reader["DELTA_SIZE_MM"].ToString());
            SelectDDLItemByValue(ddlPipeRouteMap, reader["MAP_NAME"].ToString());
            //SelectDDLItemByValue(ddlSgpRoute, reader["SGP_ROUTE_ID"].ToString());
            SelectDDLItemByValue(ddlQualityKP, reader["QUALITY_KP"].ToString());

            cbRepair.Checked = reader["ZACHISTKA_CHECKBOX"].ToString() == "Y";
            cbScraping.Checked = reader["ZACHISTKA_WHOLE_LENGTH_ZVS"].ToString() == "Y";
            txbAmounts.Text = reader["ZACHISTKA_AMOUNT_ZVS"].ToString();
            txbMinWallWeld.Text = reader["ZACHISTKA_MINTHICKSHOV_ZVS"].ToString();
            txbMinWallPipe.Text = reader["ZACHISTKA_MINTHICKTELO_ZVS"].ToString();
            SelectDDLItemByValue(ddlResultScraping, reader["ZACHISTKA_FINAL_ID_ZVS"].ToString().Trim());
            SelectDDLItemByValue(ddlLocationScraping, reader["DEFECT_LOCATION_ID_ZVS"].ToString());
            SelectDDLItemByValue(ddlResultPrestar, reader["ZACHISTKA_ID"].ToString());

            //заполнение информации по дефектам
            SelectDDLItemByValue(ddlDefect2, reader["DEFECT_ID_2"].ToString().Trim());
            SelectDDLItemByValue(ddlDeltaSize2, reader["DELTA_SIZE_MM_2"].ToString());
            txbDefectDescription2.Text = reader["DEFECT_DESCRIPTION_2"].ToString();
            txbDefectDistance2.Text = reader["DEFECT_DISTANCE_2"].ToString();
            SelectDDLItemByValue(ddlDefectLocation2, reader["DEFECT_LOCATION_2"].ToString());
            SelectDDLItemByValue(ddlDefect3, reader["DEFECT_ID_3"].ToString().Trim());
            SelectDDLItemByValue(ddlDeltaSize3, reader["DELTA_SIZE_MM_3"].ToString());
            txbDefectDescription3.Text = reader["DEFECT_DESCRIPTION_3"].ToString();
            txbDefectDistance3.Text = reader["DEFECT_DISTANCE_3"].ToString();
            SelectDDLItemByValue(ddlDefectLocation3, reader["DEFECT_LOCATION_3"].ToString());

            //получение данных о испытаниях и переключение вида            
            TabsVisible = false;
            MainMultiView.SetActiveView(InputDataView);
            ddlNTD_SelectedIndexChanged(null, EventArgs.Empty);
            GetTestResults();

            reader.Close();
            reader.Dispose();
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка получения данных для редактирования", ex);
        }
    }


    //поиск данных для исправлений по кнопке "найти"
    protected void btnFindForEdit_Click(object sender, EventArgs e)
    {
        if (cldStartDate.SelectedDate == null || cldEndDate.SelectedDate == null)
        {
            Master.AlertMessage = "Необходимо указать дату начала и окончания периода.";
            return;
        }

        //интервал дат
        DateTime startDate = cldStartDate.SelectedDate.Value;
        DateTime endDate = cldEndDate.SelectedDate.Value.AddHours(23.9999);

        try
        {
            //запрос данных истории ввода
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = @"select ip.row_id, ip.DIAMETER, pi.S_SIZE1, pi.S_SIZE2,
                    ip.CUT_LEFT_DEFECTS, ip.LENGTH, ip.LOT_NUMBER, ip.LOT_YEAR, nd.NTD_NAME, nd.NTD_GROUP, sk.FIO, 
                    ip.ORDER_HEADER, ip.ORDER_LINE, ip.INVENTORY_CODE, ip.PIPE_NUMBER, 
                    ip.SHIFT_INDEX, ip.STEELMARK, ip.THICKNESS, ip.TRX_DATE, ip.WEIGHT, ip.NEXT_DIRECTION, ip.GOST, ip.GOST_GROUP, ip.WORKPLACE_ID 
                    from INSPECTION_PIPES ip
                    left join spr_ntd nd on ip.ntd_id=nd.id 
                    left join spr_kadry sk on ip.OPERATOR_ID=sk.USERNAME
                    left join oracle.v_t3_pipe_items pi on ip.inventory_code=pi.nomer
                    where ip.TRX_DATE>=? and ip.TRX_DATE<=? 
                    and ip.EDIT_STATE=0
                    and ip.LOT_NUMBER LIKE ? 
                    and to_char(ip.PIPE_NUMBER, '00000000') LIKE ? 
                    and ip.OPERATOR_ID=NVL(?, ip.OPERATOR_ID)
                    and ip.WORKPLACE_ID LIKE ? 
                    and (ip.WORKPLACE_ID<=6 or (ip.WORKPLACE_ID=7 and ip.NEXT_DIRECTION in ('SKLAD','BRAK')) or ip.WORKPLACE_ID in (63,80,81,82,83,84,85)) 
                    and ip.SHIFT_INDEX LIKE ? 
                    and (ip.INVENTORY_CODE<>? or ip.inventory_code is null or ip.NTD_ID=50)
                    order by ip.TRX_DATE desc";
            cmd.Parameters.AddWithValue("TRX_DATE1", startDate);
            cmd.Parameters.AddWithValue("TRX_DATE2", endDate);
            cmd.Parameters.AddWithValue("LOT_NUMBER", "%" + txbPartNo2.Text.Trim() + "%");
            String pipeNumber = "";
            if (txbPipeNumber2.Text.Trim() != "")
            {
                pipeNumber = txbYear2.Text.Trim();
                pipeNumber += txbPipeNumber2.Text.Trim().PadLeft(6, '0');
            }
            cmd.Parameters.AddWithValue("PIPE_NUMBER", "%" + pipeNumber + "%");
            cmd.Parameters.AddWithValue("OPERATOR_ID", ddlOperatorFIO2.SelectedItem.Value);
            if (ddlWorkPlace2.SelectedIndex <= 0)
                cmd.Parameters.AddWithValue("WORKPLACE_ID", "%%");
            else
                cmd.Parameters.AddWithValue("WORKPLACE_ID", "%" + ddlWorkPlace2.SelectedItem.Value + "%");
            cmd.Parameters.AddWithValue("SHIFT_INDEX", "%" + ddlShift2.Text + "%");
            cmd.Parameters.AddWithValue("INV_CODE1", ObrInventoryCode);

            //получение данных
            OleDbDataReader reader = cmd.ExecuteReader();
            int c = 0;
            EditTableRecords.Clear();
            while (reader.Read() & (c < 500))
            {
                RecordData data = new RecordData();
                data.ROWID = reader["row_id"];

                if (reader["DIAMETER"].ToString() != "")
                {
                    data.DIAMETER = reader["DIAMETER"].ToString();
                }
                else
                {
                    data.DIAMETER = reader["S_SIZE1"].ToString() + "x" + reader["S_SIZE2"].ToString();
                }

                data.NEXT_DIRECTION = reader["NEXT_DIRECTION"].ToString();
                data.LENGTH = reader["LENGTH"].ToString();
                data.LOT_NUMBER = reader["LOT_NUMBER"].ToString();
                data.OPERATOR_FIO = reader["FIO"].ToString();
                data.ORDER_HEADER = reader["ORDER_HEADER"].ToString();
                data.ORDER_LINE = reader["ORDER_LINE"].ToString();
                data.INVENTORY_CODE = reader["INVENTORY_CODE"].ToString();
                data.PIPE_NUMBER = reader["PIPE_NUMBER"].ToString();
                data.SHIFT_INDEX = reader["SHIFT_INDEX"].ToString();
                data.STEELMARK = reader["STEELMARK"].ToString();
                data.THICKNESS = reader["THICKNESS"].ToString();
                data.TRX_DATE = Convert.ToDateTime(reader["TRX_DATE"]).ToString("dd.MM.yyyy HH:mm");
                data.WEIGHT = reader["WEIGHT"].ToString();
                data.WORKPLACE_ID = reader["WORKPLACE_ID"].ToString();
                data.NTD_GOST = reader["GOST"].ToString();
                data.NTD_Group = reader["GOST_GROUP"].ToString();

                EditTableRecords.Add(data);
                c++;
            }

            //отображение предупреждения об отсутствии данных
            lblNoDataForEdit.Visible = (c == 0);
            tblEditRecordsList.Visible = !lblNoDataForEdit.Visible;

            //закрытие подключения к БД
            reader.Close();
            reader.Dispose();
            cmd.Dispose();
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка получения данных", ex);
        }
    }



    //функция перестроения таблицы записей на редактирование
    protected void RebuildEditListTable()
    {
        //удаление старых строк
        while (tblEditRecordsList.Rows.Count > 1)
            tblEditRecordsList.Rows.RemoveAt(tblEditRecordsList.Rows.Count - 1);

        //добавление строк
        foreach (RecordData rec in EditTableRecords)
        {
            TableRow row = new TableRow();
            TableCell cell;
            cell = new TableCell(); cell.Text = rec.TRX_DATE; cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);
            cell = new TableCell(); cell.Text = rec.WORKPLACE_ID; cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);
            cell = new TableCell(); cell.Text = rec.SHIFT_INDEX; cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);

            cell = new TableCell(); cell.Text = rec.LOT_NUMBER; cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);
            cell = new TableCell(); cell.Text = rec.PIPE_NUMBER; cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);
            cell = new TableCell(); cell.Text = rec.DIAMETER + " x " + rec.THICKNESS; cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);
            cell = new TableCell(); cell.Text = rec.STEELMARK; cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);

            String gost = rec.NTD_GOST;
            if (rec.NTD_Group != "") gost += " гр. " + rec.NTD_Group;
            //if (rec.INVENTORY_CODE == BrakInventoryCode) gost = "Лом негабаритный";
            cell = new TableCell(); cell.Text = gost; row.Cells.Add(cell);

            String sLength = rec.LENGTH;
            if (sLength != "") sLength = (Convert.ToInt32(sLength) / 1000.0).ToString();
            cell = new TableCell(); cell.Text = sLength; cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);
            cell = new TableCell(); cell.Text = rec.WEIGHT; cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);

            String direction = "";
            if (rec.NEXT_DIRECTION == "REMONT") direction = "Ремонт";
            if (rec.NEXT_DIRECTION == "SKLAD") direction = "Склад";
            if (rec.NEXT_DIRECTION == "BRAK") direction = "Брак";
            cell = new TableCell(); cell.Text = direction; cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);

            cell = new TableCell(); cell.Text = rec.ORDER_HEADER; cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);
            cell = new TableCell(); cell.Text = rec.ORDER_LINE; cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);
            cell = new TableCell(); cell.Text = rec.INVENTORY_CODE; cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);
            cell = new TableCell(); cell.Text = rec.OPERATOR_FIO; row.Cells.Add(cell);

            foreach (TableCell rowcell in row.Cells)
                cell.Text = cell.Text.Replace(" ", "&nbsp;");
            row.Attributes["rowid"] = rec.ROWID.ToString();
            row.Attributes["onclick"] = "HighlightEditTableRow(this, " + rec.PIPE_NUMBER + ")";
            tblEditRecordsList.Rows.Add(row);
        }
    }

    ////перестроение таблицы записей на редактирование перед отправкой клиенту
    protected void tblEditRecordsList_PreRender(object sender, EventArgs e)
    {
        RebuildEditListTable();
    }




    //структура записи в таблице исправлений
    [Serializable]
    protected struct RecordData
    {
        public String TRX_DATE;
        public String PIPE_NUMBER;
        public String DIAMETER;
        public String THICKNESS;
        public String STEELMARK;
        public String OPERATOR_FIO;
        public String LENGTH;
        public String WEIGHT;
        public String LOT_NUMBER;
        public String ORDER_HEADER;
        public String ORDER_LINE;
        public String INVENTORY_CODE;
        public String NTD_ID;
        public String NTD_GOST;
        public String NTD_Group;
        public String CUT_LEFT_DEFECTS;
        public String SHIFT_INDEX;
        public String WORKPLACE_ID;
        public String NEXT_DIRECTION;
        public object ROWID;
    }


    //список записей для таблицы
    protected List<RecordData> EditTableRecords
    {
        get
        {
            try
            {
                if (ViewState["EDIT_TABLE_RECORDS"] == null)
                {
                    List<RecordData> recs = new List<RecordData>();
                    ViewState["EDIT_TABLE_RECORDS"] = recs;
                }
                return ((List<RecordData>)ViewState["EDIT_TABLE_RECORDS"]);
            }
            catch
            {
                return new List<RecordData>();
            }
        }
        set { ViewState["EDIT_TABLE_RECORDS"] = value; }
    }

    //rowid редактируемой записи
    protected String UpdateRowID
    {
        get
        {
            object oRowID = ViewState["UpdateRowID"];
            if (oRowID == null) oRowID = "";
            return oRowID.ToString();
        }
        set
        {
            ViewState["UpdateRowID"] = value;
        }
    }


    //номер трубы, для которой получена контрольная цифра
    protected int BadMarkingPipeNumber
    {
        get
        {
            object o = ViewState["BadMarkingPipeNumber"];
            if (o == null) o = -1;
            return Convert.ToInt32(o);
        }
        set
        {
            ViewState["BadMarkingPipeNumber"] = value;
        }
    }

    //Идентификатор выбранной записи в таблице TESC3.STORE_PRESTAR
    protected String selectPrestar
    {
        get
        {
            object oPrestar = ViewState["selectPrestar"];
            if (oPrestar == null) oPrestar = "";
            return oPrestar.ToString();
        }
        set
        {
            ViewState["selectPrestar"] = value;
        }
    }


    //отображение истории трубы по кнопке
    protected void btnShowHistory_Click(object sender, EventArgs e)
    {
        txbPipeNumber.Text = txbPipeNumber.Text.Trim().PadLeft(6, '0');
        MainMultiView.SetActiveView(PipeHistoryView);
        GetPipeHistory();
    }


    //получение истории трубы
    public void GetPipeHistory()
    {
        if (PipeHistoryView.Visible == false) return;

        //списки для хранения истории
        List<String> ListArea = new List<String>();
        List<DateTime> ListDate = new List<DateTime>();
        List<String> ListDirection = new List<string>();
        List<String> ListNote = new List<string>();
        List<String> ListOperator = new List<string>();

        try
        {
            //получение записей со стана
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "select PIPE_NUMBER, CUTDATE from optimal_pipes where (pipe_number=?) order by cutdate";
            cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);
            OleDbDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                ListArea.Add("Стан");
                ListDate.Add(Convert.ToDateTime(reader["CUTDATE"]));
                ListDirection.Add("");
                ListNote.Add("");
                ListOperator.Add("");
            }
            reader.Close();
            reader.Dispose();
            cmd.Dispose();

            //получение записей с ремонта и инспекций
            conn = Master.Connect.ORACLE_TESC3();
            cmd = conn.CreateCommand();
            String SQL = "select * from inspection_pipes "
                + "left join spr_kadry on inspection_pipes.OPERATOR_ID=USERNAME "
                + "where ((inventory_code is null)or(inventory_code<>'034.000037'))and(edit_state=0)and(PIPE_NUMBER=?) order by TRX_DATE";
            cmd.CommandText = SQL;
            cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);
            reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                String txt = "";
                int workplace_id = Convert.ToInt32(reader["WORKPLACE_ID"]);
                if (workplace_id == 1) txt = "Инспекция 1 (НЛО)";
                if (workplace_id == 2) txt = "Инспекция 2 (НЛО)";
                if (workplace_id == 3) txt = "Инспекция 3 (НЛО)";
                if (workplace_id == 4) txt = "Инспекция 4 (КЛО)";
                if (workplace_id == 5) txt = "Инспекция 5 (КЛО)";
                if (workplace_id == 6) txt = "Инспекция 6 (КЛО)";
                if (workplace_id == 7) txt = "Участок ремонта";
                if (workplace_id == 8) txt = "Участок вырезки дефектов в линии стана";
                if (workplace_id == 9) txt = "Участок вырезки механических проб";
                if (workplace_id == 10) txt = "Торцеподрезные станки";
                if (workplace_id == 85) txt = "Установка по зачистке внутренней поверхности";
                ListArea.Add(txt);

                txt = reader["NEXT_DIRECTION"].ToString();
                if (txt == "SKLAD") txt = "Склад";
                if (txt == "BRAK") txt = "Лом негабаритный";
                if (txt == "OTO") txt = "Участок ОТО";
                if (txt == "OTDELKA") txt = "Отделка";
                if (txt == "REMONT") txt = "Ремонт";
                if (workplace_id == 85) txt = "Зачистка внутренней поверхности";
                ListDirection.Add(txt);

                txt = reader["CUT_LEFT_LENGTH"].ToString();
                if (txt != "") txt = (Convert.ToInt32(txt) / 1000.0).ToString();
                String obrRight = reader["CUT_RIGHT_LENGTH"].ToString();
                if (obrRight != "") obrRight = (Convert.ToInt32(obrRight) / 1000.0).ToString();
                if ((obrRight != "") & (txt != "")) txt = txt + " + ";
                txt = txt + obrRight;
                if (txt != "") txt = "Обрезь " + txt + " м";
                //признак нечитаемости номера
                String BadMarking = reader["BAD_MARKING"].ToString();
                if (BadMarking == "1")
                {
                    if (txt != "") txt += "<br/>";
                    txt += "Нечитаемая маркировка";
                }
                ListNote.Add(txt);

                ListDate.Add(Convert.ToDateTime(reader["TRX_DATE"]));
                ListOperator.Add(reader["FIO"].ToString());
            }
            reader.Close();
            reader.Dispose();
            cmd.Dispose();

            //заполнение таблицы истории
            DateTime minDate = new DateTime(0);
            for (int r = 0; r < ListDate.Count; r++)
            {
                DateTime fndDate = new DateTime(2099, 1, 1);
                int fndIndex = -1;
                for (int i = 0; i < ListDate.Count; i++)
                {
                    if ((ListDate[i] < fndDate) & (ListDate[i] > minDate))
                    {
                        fndDate = ListDate[i];
                        fndIndex = i;
                    }
                }
                //заполнение ряда таблицы
                if (fndIndex != -1)
                {
                    minDate = fndDate;
                    TableRow row = new TableRow();
                    TableCell cell;
                    cell = new TableCell(); cell.Text = ListArea[fndIndex]; row.Cells.Add(cell);
                    cell = new TableCell(); cell.Text = ListDate[fndIndex].ToString("dd.MM.yyyy HH:mm:ss"); cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);
                    cell = new TableCell(); cell.Text = ListDirection[fndIndex]; row.Cells.Add(cell);
                    cell = new TableCell(); cell.Text = ListNote[fndIndex]; row.Cells.Add(cell);
                    cell = new TableCell(); cell.Text = ListOperator[fndIndex]; row.Cells.Add(cell);
                    tblPipeHistory.Rows.Add(row);
                }
            }
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage(ex.Message, ex);
        }
    }

    //Отправка трубы на ремонт или в брак
    protected void btnToBrakOrRepair_Click(object sender, EventArgs e)
    {

        ddlNTD_SelectedIndexChanged(sender, e);

        //при приемке трубы на строку кампании - маршрутная карта задается из кампании
        if (cbShowCampaigns.Checked && ddlCampaign.SelectedItem.Value != "")
        {
            int campaignLineId = Convert.ToInt32(ddlCampaign.SelectedItem.Value);
            String routeMap = GetCampaignPipeRouteMap(campaignLineId);
            SelectDDLItemByValue(ddlPipeRouteMap, routeMap);
        }
        if (cbShowCampaigns.Checked && ddlCampaign.SelectedItem.Value == "")
        {
            SelectDDLItemByValue(ddlPipeRouteMap, "");
        }

        //проверка интервала редактирования записи с момента последнего сохранения
        if (UpdateRowID != "")
        {
            if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 41) && !CheckCanEditRecByTime(UpdateRowID))
            {
                return;
            }
        }

        //Зачистка внутренней поверхности
        #region 
        if (sender == btnToRepair)
        {
            if (ddlDefect.SelectedItem != null && ddlDefect.SelectedIndex > 0 && cbRepair.Checked)
            {
                OleDbCommand cmd = Master.Connect.ORACLE_TESC3().CreateCommand();
                cmd.CommandText = "select nvl(scraping_inner_surface,0) from spr_defect where IS_ACTIVE=1 and id = ? ";
                cmd.Parameters.AddWithValue("ID", ddlDefect.SelectedItem.Value);
                Code_Scraping = Convert.ToInt16(cmd.ExecuteScalar());
                cmd.Dispose();

                if (Code_Scraping == 0)
                {
                    Master.AlertMessage = "Данный дефект ремонту зачисткой не допустим";
                    return;
                }
                else
                {
                    //go to "Труба проследует на зачистку внутренней поверхности";
                }
            }
        }
        #endregion


        // Зачистка внутренней поверхности
        if (WorkplaceId == 85 && CheckScrapingFields())
            return;

        //Красавин Д.Н. убрал проверку в соответствии с TFS:37731
        //проверка толщин после зачистки 
        //if (WorkplaceId == 85 && CheckScrapingThickness())
        //{
        //    return;
        //}

        if (sender == btnToBrak)
        {
            if (WorkplaceId == 85 && ddlResultScraping.SelectedItem.Value != "3")
            {
                Master.AlertMessage = "Труба отремонтирована. Нельзя списать в брак!";
                return;
            }

            //выполнение проверок, результатами которых будет набор предупреждений;
            //выполняется только если предупреждения ещё не были отображены, о чем говорит заполнение поля "функции обратного вызова из окна предупреждения"
            if (fldWarningReturnButtonId.Value == "")
            {
                //очистка предупреждений
                ClearWarnings(false, true);

                // проверка на ввод настроечных труб
                if (CheckSettingPipe(PipeNumber))
                {
                    AddWarning("Внимание! Для настроечной трубы указан дефект определяющий брак", true, true);
                }

                //отображение окна с предупреждением, если имеются предупреждения
                if (lstWarnings.Items.Count > 0)
                {
                    fldWarningReturnButtonId.Value = (sender as Button).ID;
                    MainMultiView.SetActiveView(vWarnings);
                    return;
                }
            }
        }

        // Проверка АУЗК-РУЗК
        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 42) && (WorkplaceId >= 1 && WorkplaceId <= 7))
        {
            try
            {
                OleDbConnection conn = Master.Connect.ORACLE_TESC3();
                OleDbCommand cmd = conn.CreateCommand();
                OleDbDataReader reader = null;
                // результаты АУЗК
                cmd.CommandText = @"select REC_DATE, PIPE_NUMBER, TEST_BRAK_AUTO, TEST_BRAK_MANUAL, WORKPLACE_ID from USC_OTDELKA where WORKPLACE_ID in (13, 14, 15, 16, 17, 18, 19, 64) 
                                and EDIT_STATE = 0 and PIPE_NUMBER = " + PipeNumber.ToString() + " order by REC_DATE";
                reader = cmd.ExecuteReader();
                Dictionary<String, Boolean> ausc = new Dictionary<String, Boolean>();
                ausc.Clear();
                String brak_date = "";
                while (reader.Read())
                {
                    Boolean brak = false;
                    String auto = reader["TEST_BRAK_AUTO"].ToString();
                    String manual = reader["TEST_BRAK_MANUAL"].ToString();
                    String result = "";
                    if (manual != "")
                        result = manual;
                    else result = auto;
                    if (result != "0")
                        brak = true;
                    if (ausc.ContainsKey(reader["WORKPLACE_ID"].ToString()))
                    {
                        ausc.Remove(reader["WORKPLACE_ID"].ToString());
                        ausc.Add(reader["WORKPLACE_ID"].ToString(), brak);
                    }
                    else
                        ausc.Add(reader["WORKPLACE_ID"].ToString(), brak);
                    if (brak)
                        brak_date = " and REC_DATE > to_date('" + reader["REC_DATE"].ToString() + "', 'dd.mm.yyyy hh24:mi:ss')";
                }
                reader.Close();

                // результаты РУЗК
                cmd.CommandText = @"select REC_DATE, PIPE_NUMBER, TEST_BRAK_AUTO, TEST_BRAK_MANUAL, WORKPLACE_ID 
                                    from USC_OTDELKA where WORKPLACE_ID in (25, 26, 27, 28, 29, 30, 51, 52, 53, 54, 55, 56) 
                                        and EDIT_STATE = 0 and PIPE_NUMBER = " + PipeNumber.ToString() + brak_date + " order by REC_DATE";
                reader = cmd.ExecuteReader();
                Dictionary<String, Boolean> rusc = new Dictionary<String, Boolean>();
                rusc.Clear();
                while (reader.Read())
                {
                    Boolean brak = false;
                    String auto = reader["TEST_BRAK_AUTO"].ToString();
                    String manual = reader["TEST_BRAK_MANUAL"].ToString();
                    String result = "";
                    if (manual != "")
                        result = manual;
                    else result = auto;
                    if (result != "0")
                        brak = true;
                    if (rusc.ContainsKey(reader["WORKPLACE_ID"].ToString()))
                    {
                        rusc.Remove(reader["WORKPLACE_ID"].ToString());
                        rusc.Add(reader["WORKPLACE_ID"].ToString(), brak);
                    }
                    else
                        rusc.Add(reader["WORKPLACE_ID"].ToString(), brak);
                }
                reader.Close();
                reader.Dispose();
                cmd.Dispose();

                if (ausc.ContainsValue(true) && rusc.Count == 0)
                {
                    Master.AlertMessage = "Необходима перепроверка РУЗК";
                    return;
                }
            }
            catch
            { }
        }

        //проверка обязательных полей

        if (sender == btnToRepair)
        {
            if (ddlDefect.SelectedItem != null)
            {
                if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 68) && ddlDefect.SelectedItem.Value == "4")
                {
                    Master.AlertMessage = "Запрет на отправку трубы на участок ремонта с площадок окончательной приемки и склада готовой продукции с дефектом «Без дефектов/ с УСТ»";
                    return;
                }
            }
        }


        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 11) && (txbPartNo.Text.Trim() == "" || txbPartYear.Text.Trim() == ""))
        {
            Master.AlertMessage = "Необходимо ввести год и номер партии";
            return;
        }
        int pipe_length = 0;
        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 12) && !Int32.TryParse(txbLength.Text.Trim(), out pipe_length))
        {
            Master.AlertMessage = "Необходимо ввести длину трубы в мм.";
            return;
        }
        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 13) && (pipe_length < 4000 || pipe_length > 15000))
        {
            Master.AlertMessage = "Длина трубы должна быть в пределах от 4000 до 15000 мм";
            return;
        }
        double pipe_weight = 0;
        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 14) && !double.TryParse(txbWeight.Text.Trim(), out pipe_weight))
        {
            Master.AlertMessage = "Необходимо указать массу в кг.";
            return;
        }
        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 15) && ddlDiam.SelectedItem.Text == "" && ddlProfileSize.SelectedItem.Text == "" && WorkplaceId != 63)
        {
            Master.AlertMessage = "Необходимо указать диаметр или типоразмер профиля трубы";
            return;
        }
        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 16) && ddlThickness.SelectedItem.Text == "")
        {
            Master.AlertMessage = "Необходимо указать толщину стенки трубы";
            return;
        }
        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 43) && ddlDefect.SelectedItem.Value.Trim() == "")
        {
            Master.AlertMessage = "Необходимо указать дефект";
            return;
        }

        //проверка: в качестве дефекта не может быть указан письмо/распоряжение/заказ
        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 44) && ddlInstructionType.SelectedItem.Value != "")
        {
            Master.AlertMessage = "По письму, распоряжению или заказу труба может быть принята только на СГП в группу Д.";
            return;
        }

        //проверка указания дополнительного дефекта если он необходим
        if (IsReduceLengthDefect())
        {
            if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 20) && ddlDefectAdditional.SelectedItem.Value.Trim() == "")
            {
                Master.AlertMessage = "Необходимо указать дефект, выводящий длину за пределы требований НД";
                return;
            }
        }

        #region *** блокировка сохранения с указанием дефекта "проба механическая" ***
        // (письмо 23-37-03/6734. IS-548)
        // блокировка сохранения НЕ действует для труб, направленых на ремонт И назначеных на пробы И еще не отбирались пробы (мероприятия по отборам проб IS-480)
        bool IsBlock365 = false;
        if (sender == btnToBrak) IsBlock365 = true; // если приемка в брак/лом
        else
        {
            bool IsSamplingPipe = false;
            bool PipeCutSampling = CheckPipeSampling(out IsSamplingPipe);
            if (!IsSamplingPipe) IsBlock365 = true; // если трубы не назначалась на отбор проб
            else if (PipeCutSampling) IsBlock365 = true; // если от трубы уже отобраны пробы
        }
        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 45) && ddlDefect.SelectedItem.Value == "365" && IsBlock365)
        {
            Master.AlertMessage = "Дефект «Проба механическая» может быть внесен только на рабочем месте «Участок вырезки механических проб»";
            return;
        }
        #endregion *** блокировка сохранения с указанием дефекта "проба механическая" ***

        //проверка отбора проб от трубы/партии, не выполняется для случая когда труба направляется на участок ремонта
        if ((WorkplaceId >= 0 && WorkplaceId <= 6) || WorkplaceId == 7)
        {
            if (sender != btnToRepair)
                if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 46) && !CheckPipeForProbes())
                {
                    return;
                }
        }


        if (sender == btnToRepair)
        {
            //проверка указания длины зачистки
            if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 47) && !CheckDeltaSize())
            {
                return;
            }

            //проверка указания параметров ремонта зачисткой
            if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 48) && !CheckZachistka())
            {
                return;
            }

            if (cbRepair.Checked)
            {
                //проверка на ручные замеры геометрических параметров
                string message_gp = "";
                if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 1) &&
                    ((WorkplaceId >= 1 && WorkplaceId <= 6) || WorkplaceId == 80) && !CheckManualGeometryParam(out message_gp, true))
                {
                    Master.AlertMessage = message_gp;
                    return;
                }
            }

            //выполнение проверок, результатами которых будет набор предупреждений;
            //выполняется только если предупреждения ещё не были отображены, о чем говорит заполнение поля "функции обратного вызова из окна предупреждения"
            if (fldWarningReturnButtonId.Value == "")
            {
                //очистка предупреждений
                ClearWarnings(false, true);


                if (Code_Scraping == 1)
                    AddWarning("Труба проследует на зачистку внутренней поверхности", true, true);

                if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 59))
                {
                    CheckLot();
                }


                if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 61) && WorkplaceId >= 1 && WorkplaceId <= 7)
                {
                    CheckDeltaLength();
                }

                // Проверка отправки на ремонт труб со значениями сортамента и марки стали отличными от заданных на рулоне
                if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 71) && ((WorkplaceId >= 1 && WorkplaceId <= 6) || WorkplaceId == 80))
                {
                    String warning = "";
                    String camp_line = Session["CampaignLineDesc"].ToString();
                    OleDbConnection conn = Master.Connect.ORACLE_TESC3();
                    OleDbCommand cmd = conn.CreateCommand();
                    cmd.CommandText =
                        @"  select ca.inventory_code
                         , t3.diameter
                         , t3.thickness
                         , t3.stal
                         , t3.s_size1 || 'x' || t3.s_size2 profile_pipe
                      from geometry_coils_sklad gcs, campaigns ca, oracle.v_t3_pipe_items t3
                     where gcs.edit_state = 0
                       and (gcs.coil_pipepart_no, gcs.coil_pipepart_year, gcs.coil_run_no) in
                              (select op.coil_pipepart_no, op.coil_pipepart_year, op.coil_internalno
                                 from optimal_pipes op
                                where op.pipe_number = ? )
                       and gcs.campaign_line_id = ca.campaign_line_id
                       and ca.edit_state = 0
                       and ca.inventory_code_stan = t3.nomer(+) ";

                    cmd.Parameters.AddWithValue("pipe_number", PipeNumber);
                    OleDbDataReader rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        if (ddlInventoryNumber.SelectedItem.Text != "")
                        {
                            if (ddlInventoryNumber.SelectedItem.Text != rdr["inventory_code"].ToString())
                            {
                                if (camp_line == "")
                                    warning = "Внимание! На рулон, из которого произведена труба, были назначены другие значения величины: Диаметр =" +
                                       ddlDiam.SelectedItem.Text + " Стенка = " + ddlThickness.SelectedItem.Text + " Марка = " + ddlSteelmark.SelectedItem.Text;
                                else
                                    warning = "Внимание! На рулон, из которого произведена труба, были назначены другие значения величины: " + camp_line;
                                AddWarning(warning, true, true);
                            }
                        }
                        else
                        {
                            if (ddlDiam.SelectedItem.Text != "")
                            {
                                if ((ddlDiam.SelectedItem.Text != rdr["diameter"].ToString()) || (ddlThickness.SelectedItem.Text != rdr["thickness"].ToString()) ||
                                    (ddlSteelmark.SelectedItem.Text != rdr["stal"].ToString()))
                                {
                                    if (camp_line == "")
                                        warning = "Внимание! На рулон, из которого произведена труба, были назначены другие значения величины: Диаметр =" +
                                           ddlDiam.SelectedItem.Text + " Стенка = " + ddlThickness.SelectedItem.Text + " Марка = " + ddlSteelmark.SelectedItem.Text;
                                    else
                                        warning = "Внимание! На рулон, из которого произведена труба, были назначены другие значения величины: " + camp_line;
                                    AddWarning(warning, true, true);
                                }
                            }
                            else

                            {
                                if ((ddlProfileSize.SelectedItem.Text != rdr["profile_pipe"].ToString()) || (ddlThickness.SelectedItem.Text != rdr["thickness"].ToString()) ||
                                    (ddlSteelmark.SelectedItem.Text != rdr["stal"].ToString()))
                                {
                                    if (camp_line == "")
                                        warning = "Внимание! На рулон, из которого произведена труба, были назначены другие значения величины: Профиль =" +
                                           ddlProfileSize.SelectedItem.Text + " Стенка = " + ddlThickness.SelectedItem.Text + " Марка = " + ddlSteelmark.SelectedItem.Text;
                                    else
                                        warning = "Внимание! На рулон, из которого произведена труба, были назначены другие значения величины: " + camp_line;
                                    AddWarning(warning, true, true);
                                }
                            }

                        }
                    }
                    rdr.Close();
                    cmd.Dispose();
                }



                //отображение окна с предупреждением, если имеются предупреждения
                if (lstWarnings.Items.Count > 0)
                {
                    fldWarningReturnButtonId.Value = (sender as Button).ID;
                    MainMultiView.SetActiveView(vWarnings);
                    return;
                }
            }
        }

        //признак передачи в SAP данных по трубе (в случае исправления записи)
        bool SapProcessed = false;

        //печать бирки и сохранение данных
        try
        {
            String createdRowId = "";
            if (!DataSaved)
            {
                //проверка возможности ввода по трубе согласно маршруту
                if (UpdateRowID == "")
                    if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 40) && !CheckValidCurrentPositionOfPipe())
                    {
                        return;
                    }

                //сохранение в БД
                bool bRepair = (sender == btnToRepair);
                bool bBrak = (sender == btnToBrak);
                if (!bRepair & !bBrak) bBrak = true;
                SapProcessed = !SaveDataToDB(bRepair, bBrak, "", out createdRowId);

                //снятие отметки экспериментальной трубы, если труба направлена в брак/лом
                if (bBrak)
                {
                    ExperimentalPipes.DeletePipeExperimentalFlag(PipeNumber);
                }
            }

            //установка флага "данные сохранены в БД"
            DataSaved = true;

            //передача номера трубы в транспорт НЛО, если инспекция 1...6 и не исправление записи
            if (WorkplaceId <= 6 && WorkplaceId > 0 && UpdateRowID == "")
                if (cbRepair.Checked)
                    SendCommandToTransportPlc(4);
                else
                    SendCommandToTransportPlc(2);


            //установка флага обработки трубы из очереди на MAIR
            if (WorkplaceId == 80 || WorkplaceId == 81)
                SetMairQueueProcessingStatus(PipeNumber);

            //печать бирки, если исправляемые данные по трубе не были переданы в SAP
            if (!SapProcessed)
            {
                if (!PrintLabel(PipeNumber, cbWeight.Checked, cbInch.Checked, cbKGtoFunt.Checked))
                    return;
            }
            else
            {
                //предупреждение о невозможности правки если данные ранее переданы в SAP
                Master.AlertMessage = "Внимание! Данные по трубе №" + PipeNumber + " ранее были переданы в SAP. Изменение информации невозможно. " +
                                      "Все необходимые изменения по данной трубе необходимо оформить на бумажном носителе и передать старшему сменному мастеру.";
            }

            //переход к экрану ввода номера
            btnNewPipe_Click(sender, e);
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка при сохранении данных и печати бирки", ex);
        }

        //сброс отметок о контроле партии и переводе        
        fldNotZakazCheckOk.Value = "";
        fldWarningReturnButtonId.Value = "";
    }


    //изменение способа ввода номенклатурного номера - список/ручной ввод
    protected void cbOracleList_CheckedChanged(object sender, EventArgs e)
    {
        txbInventoryNumber.Visible = !cbOracleList.Checked;
        ddlInventoryNumber.Visible = cbOracleList.Checked;
        if (ddlInventoryNumber.Visible)
        {
            ListItem item = ddlInventoryNumber.Items.FindByText(txbInventoryNumber.Text.Trim());
            if (item != null)
                ddlInventoryNumber.SelectedIndex = ddlInventoryNumber.Items.IndexOf(item);
        }
        if (txbInventoryNumber.Visible)
            txbInventoryNumber.Text = ddlInventoryNumber.Text;
    }


    //изменение номенклатурного номера через текстовое поле
    protected void txbInventoryNumber_TextChanged(object sender, EventArgs e)
    {
        //очистка старых значений параметров сортамента
        ddlDiam.Text = "";
        ddlProfileSize.Text = "";
        ddlThickness.Text = "";
        ddlSteelmark.Text = "";
        ddlNTD.Text = "";

        //заполнение выпадающего списка значением из текстового поля
        String invNum = Checking.CorrectInventoryNumber(txbInventoryNumber.Text);
        txbInventoryNumber.Text = invNum;
        ddlInventoryNumber.Items.Clear();
        ddlInventoryNumber.Items.Add("");
        ddlInventoryNumber.Items.Add(invNum);
        ddlInventoryNumber.SelectedIndex = ddlInventoryNumber.Items.Count - 1;

        //обновление информации с новым номенклат. номером        
        ddlInventoryNumber_SelectedIndexChanged(sender, e);
        RebuildInventoryNumberList();
        txbInventoryNumber.Text = invNum;
    }


    //выбор номенклатурного номера из выпадающего списка или текстового поля
    protected void ddlInventoryNumber_SelectedIndexChanged(object sender, EventArgs e)
    {
        //заполнение текстового поля значением из выпадающего списка
        if (sender == ddlInventoryNumber)
        {
            txbInventoryNumber.Text = ddlInventoryNumber.SelectedItem.Text;
        }

        //получение характеристик трубы из номенклатурного справочника
        try
        {
            //подключение к БД и запрос данных по сортаменту            
            OleDbConnection conn = Master.Connect.ORACLE_ORACLE();
            OleDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = @"select * 
                from V_T3_PIPE_ITEMS 
                where NOMER=? and class not in ('T3_100', 'T3_200') and class is not null ";
            cmd.Parameters.AddWithValue("NOMER", txbInventoryNumber.Text);
            OleDbDataReader reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                //данные из номенклатурного справочника
                String Diam = reader["DIAMETER"].ToString();
                String SizeA = reader["S_SIZE1"].ToString();
                String SizeB = reader["S_SIZE2"].ToString();
                String Thickness = reader["THICKNESS"].ToString();
                String Steelmark = reader["STAL"].ToString();
                String GOST = reader["GOST"].ToString();
                String Group = reader["GRUP"].ToString();

                //выбор параметров сортамента из выпадающих списков
                SetNDAndGroup(GOST, Group, ddlNTD);
                if (Diam != "") SelectDDLItemByText(ddlDiam, Diam);
                if (SizeA != "") SelectDDLItemByText(ddlProfileSize, SizeA + "x" + SizeB);
                if (Thickness != "") SelectDDLItemByText(ddlThickness, Thickness);
                if (Steelmark != "") SelectDDLItemByText(ddlSteelmark, Steelmark);
            }

            //закрытие подключения к БД
            reader.Close();
            reader.Dispose();
            cmd.Dispose();
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка получения данных из номенклатурного справочника", ex);
        }

        //обновление сообщения о ненайденном номенклатурном номере
        ddlNTD_SelectedIndexChanged(null, EventArgs.Empty);
    }


    //очистка полей ввода сортамента по кнопке
    protected void btnClearSortamentFlds_Click(object sender, EventArgs e)
    {
        ddlSteelmark.SelectedIndex = 0;
        ddlThickness.SelectedIndex = 0;
        ddlDiam.SelectedIndex = 0;
        ddlProfileSize.SelectedIndex = 0;
        ddlNTD.SelectedIndex = 0;
        txbNTD.Text = "";
        ddlInventoryNumber.SelectedIndex = 0;
        ddlNTD_SelectedIndexChanged(ddlNTD, e);
    }


    //получение списка возможных номенклатурных номеров по выбранным характеристикам сортамента трубы
    protected void RebuildInventoryNumberList()
    {
        String prevInvNo = ddlInventoryNumber.Text;
        ddlInventoryNumber.Items.Clear();
        ddlInventoryNumber.Items.Add("");

        try
        {
            //подключение к БД            
            OleDbConnection conn = Master.Connect.ORACLE_ORACLE();
            OleDbCommand cmd = conn.CreateCommand();

            String where = " and (ORG_ID=127)and(TUBE_TYPE='N')";
            if (ddlDiam.SelectedItem.Text != "") where += "and(DIAMETER=? or DIAMETER IS NULL)";
            if (ddlProfileSize.SelectedItem.Text != "") where += "and(S_SIZE1||'x'||S_SIZE2=? or S_SIZE1||'x'||S_SIZE2 IS NULL)";
            if (ddlSteelmark.SelectedItem.Text != "") where += "and(STAL=? or STAL is NULL)";
            if (ddlThickness.SelectedItem.Text != "") where += "and(THICKNESS=? or THICKNESS is NULL)";
            if (ddlNTD.SelectedItem.Text != "") where += "and(GOST=?)";
            cmd.CommandText = "select * from V_T3_PIPE_ITEMS where class not in ('T3_100', 'T3_200') and class is not null " + where;

            if (ddlDiam.SelectedItem.Text != "") cmd.Parameters.AddWithValue("DIAM", Checking.GetDbType(ddlDiam.SelectedItem.Text));
            if (ddlProfileSize.SelectedItem.Text != "") cmd.Parameters.AddWithValue("PROFILE_SIZE", ddlProfileSize.SelectedItem.Text);
            if (ddlSteelmark.SelectedItem.Text != "") cmd.Parameters.AddWithValue("STAL", ddlSteelmark.SelectedItem.Text);
            if (ddlThickness.SelectedItem.Text != "") cmd.Parameters.AddWithValue("THICKNESS", Checking.GetDbType(ddlThickness.SelectedItem.Text));
            String ND = "";
            String Group = "";
            GetNDAndGroup(out ND, out Group, ddlNTD);
            if (ddlNTD.SelectedItem.Text != "")
            {
                cmd.Parameters.AddWithValue("GOST", ND);
                if (Group != "")
                {
                    cmd.CommandText = cmd.CommandText + "and(GRUP=?)";
                    cmd.Parameters.AddWithValue("GRUP", Group);
                }
            }

            //выборка найденных записей
            OleDbDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                ListItem newitem = new ListItem(reader["NOMER"].ToString());
                ddlInventoryNumber.Items.Add(newitem);
            }

            //закрытие подключения
            reader.Close();
            reader.Dispose();
            cmd.Dispose();
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка получения списка номенклатурных позиций", ex);
        }

        //если НД=Лом негабаритный, добавление единственного номенклат.номера
        if (ddlNTD.SelectedItem.Text == "Лом негабаритный")
            ddlInventoryNumber.Items.Add(ObrInventoryCode);

        //восстановление выбора предыдущей записи
        ListItem item = ddlInventoryNumber.Items.FindByText(prevInvNo);
        if (item != null)
        {
            ddlInventoryNumber.SelectedIndex = ddlInventoryNumber.Items.IndexOf(item);
        }
        //если найдена единственная номенклатура - установка значения                
        if (ddlInventoryNumber.Items.Count == 2)
        {
            ddlInventoryNumber.SelectedIndex = 1;
            ddlInventoryNumber_SelectedIndexChanged(ddlInventoryNumber, EventArgs.Empty);
        }
        //если НД не Лом негабаритный и не заполнены все поля сортамента - выбор пустого значения из списка
        if (ddlInventoryNumber.SelectedItem.Text != "Лом негабаритный")
        {
            if ((ddlDiam.SelectedIndex < 1 && ddlProfileSize.SelectedIndex < 1 && WorkplaceId != 63)
                | (ddlThickness.SelectedIndex < 1) | /*(ddlSteelmark.SelectedIndex < 1) |*/ (ddlNTD.SelectedIndex < 1))
                ddlInventoryNumber.SelectedIndex = 0;
        }

        txbInventoryNumber.Text = ddlInventoryNumber.SelectedItem.Text;
    }


    //установка текущего НД и группы
    protected void SetNDAndGroup(String ND, String Group, DropDownList ddl)
    {
        String txt = ND;
        if (Group != "") txt += " гр. " + Group;
        SelectDDLItemByText(ddl, txt);
    }

    //получение текущего НД и группы    
    protected void GetNDAndGroup(out String ND, out String Group, DropDownList ddl)
    {
        String txt = ddl.SelectedItem.Text;
        int i = txt.IndexOf(" гр. ");
        if (i != -1)
        {
            ND = txt.Substring(0, i);
            Group = txt.Substring(i + 5, txt.Length - i - 5);
        }
        else
        {
            ND = txt;
            Group = "";
        }
    }


    //установка текущего НД по коду
    protected void SetNDByCode(String Code)
    {
        String txt = "";
        if (Code == "50")
        {
            txt = "Лом негабаритный";
        }
        else
        {
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM SPR_NTD where (ID=?)";
            cmd.Parameters.AddWithValue("ID", Code);
            OleDbDataReader reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                txt = reader["NTD_NAME"].ToString();
                String Group = reader["NTD_GROUP"].ToString();
                if (Group != "") txt += " гр. " + Group;
            }
            reader.Close();
            reader.Dispose();
            cmd.Dispose();
        }

        //поиск элемента в списке НД
        SelectDDLItemByText(ddlNTD, txt);
        if (ddlNTD.SelectedIndex < 1)
        {
            for (int i = 0; i < ddlNTD.Items.Count - 1; i++)
            {
                if (ddlNTD.Items[i].Text.IndexOf(txt) == 0)
                {
                    ddlNTD.SelectedIndex = i;
                    break;
                }
            }
        }
    }


    //получение кода НТД
    protected String GetNDCode(DropDownList ddl)
    {
        //получение текста текущего НД и группы
        if (ddl.SelectedIndex == -1) return "";
        String ND = "";
        String Group = "";
        GetNDAndGroup(out ND, out Group, ddl);

        //возврат кода "Лом негабаритный"
        if (ND == "Лом негабаритный") return "50";

        //подключение к БД и выполнение запроса        
        OleDbConnection conn = Master.Connect.ORACLE_TESC3();
        OleDbCommand cmd = conn.CreateCommand();

        //НД с группой
        if (Group != "")
        {
            cmd.CommandText = "SELECT id FROM spr_ntd WHERE is_active = 1 AND ntd_name = ? AND ntd_group = ?";
            cmd.Parameters.AddWithValue("NTD_NAME", ND);
            cmd.Parameters.AddWithValue("NTD_GROUP", Group);
        }
        //НД без группы
        else
        {
            cmd.CommandText = "SELECT id FROM spr_ntd WHERE is_active = 1 AND ntd_name = ? AND ntd_group IS NULL";
            cmd.Parameters.AddWithValue("NTD_NAME", ND);
        }
        //получение данных
        String code = "";
        OleDbDataReader reader = cmd.ExecuteReader();
        if (reader.Read()) code = reader["ID"].ToString();

        //закрытие подключения к БД
        reader.Close();
        reader.Dispose();
        cmd.Dispose();

        return code;
    }

    /// <summary>
    /// Вычисляет остаток до выполнения и устанавливает соответствующий цвет, так же проверяет совпадение выбранной компании той что задана на задаче металла
    /// </summary>
    /// <param name="id_line_campaing">ид. компании</param>
    protected void SetColorAndResidual(string id_line_campaing)
    {
        double volume_production = 0, summ_mass = 0;
        int quantity_pipes = 0;
        double residualCol = 0, residualT = 0;

        if (id_line_campaing == "")
        {
            lbResidual.Text = "0(0)";
            lbResidual.ForeColor = Color.Red;
            return;
        }

        try
        {
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();

            //находим объем производства
            cmd.CommandText = @"select cp.*
                                from campaigns cp 
                                 where cp.CAMPAIGN_LINE_ID=?
                                   and Cp.EDIT_STATE = 0 ";

            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("CAMPAIGN_LINE_ID", id_line_campaing);
            OleDbDataReader reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                double.TryParse(reader["steel_out"].ToString(), out volume_production);
            }
            reader.Close();

            // получаем сумму веса труб на компанию и их количество
            cmd.CommandText = @"select
            count(CC.PIPE_NUMBER) koll, sum(CC.WEIGHT) summa
            from
            (
            select IP.PIPE_NUMBER, IP.WEIGHT, ROW_NUMBER() OVER (PARTITION BY IP.TRX_DATE ORDER BY TRX_DATE DESC) rnum
            from   TESC3.CAMPAIGNS C
            inner join TESC3.INSPECTION_PIPES IP
            on C.CAMPAIGN_LINE_ID = IP.CAMPAIGN_LINE_ID
            where C.EDIT_STATE = 0 and IP.EDIT_STATE = 0
            and IP.NEXT_DIRECTION = 'SKLAD'
            and C.CAMPAIGN_LINE_ID = ?
            ) cc
            where rnum =1";

            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("CAMPAIGN_LINE_ID", id_line_campaing);
            reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                double.TryParse(reader["summa"].ToString(), out summ_mass);
                int.TryParse(reader["koll"].ToString(), out quantity_pipes);
            }
            reader.Close();

            //записываем вычисления
            if (quantity_pipes == 0)
            {
                double.TryParse(txbWeight.Text, out summ_mass);
                residualCol = Math.Ceiling(volume_production / (summ_mass / 1000));
                residualT = volume_production - (summ_mass / 1000);
            }
            else
            {
                summ_mass = summ_mass / quantity_pipes;
                residualCol = Math.Ceiling(volume_production / (summ_mass / 1000)) - quantity_pipes;
                residualT = volume_production - (summ_mass / 1000);
            }



            if (residualT <= 0 || residualCol <= 0)
            {
                lbResidual.ForeColor = Color.Green;
            }
            else
            {
                lbResidual.ForeColor = Color.Red;
            }

            if (summ_mass == 0)
            {
                lbResidual.ForeColor = Color.Red;
                lbResidual.Text = "Отсутствует значение массы (" + Math.Round(residualT, 3) + ")";
            }
            else
            {
                lbResidual.Text = residualCol + "(" + Math.Round(residualT, 3) + ")";
            }


            // Проверяем на совпадение компаний
            cmd.CommandText = @"select
                    ROW_ID
                    from
                    (
                    select C.CAMPAIGN_LINE_ID ROW_ID, 
                    ROW_NUMBER() OVER (PARTITION BY IP.TRX_DATE ORDER BY TRX_DATE DESC) rnum
                    from   TESC3.CAMPAIGNS C
                    inner join TESC3.INSPECTION_PIPES IP
                    on C.CAMPAIGN_LINE_ID = IP.CAMPAIGN_LINE_ID
                    where C.EDIT_STATE = 0 and IP.EDIT_STATE = 0
                    and IP.NEXT_DIRECTION = 'SKLAD'
                    and IP.PIPE_NUMBER = ?
                    ) cc
                    where rnum =1";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);
            reader = cmd.ExecuteReader();


            if (reader.Read())
            {
                string temppipe = reader["ROW_ID"].ToString();
                if (temppipe != id_line_campaing)
                {
                    ddlCampaign.SelectedItem.Attributes["style"] = "background-color:red;color:white";
                }
                else
                {
                    ddlCampaign.SelectedItem.Attributes["style"] = "background-color:white;color:black";
                }
            }
            else
            {
                ddlCampaign.SelectedItem.Attributes["style"] = "background-color:red;color:white";
            }
            reader.Close();


            reader.Dispose();
            cmd.Dispose();
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка при получении данных из списка кампаний: " + ex.Message, ex);
        }
    }

    //заполнение сортамента и параметров заказа при выборе кампании
    protected void ddlCampaign_SelectedIndexChanged(object sender, EventArgs e)
    {
        cbInch.Checked = false;

        if ((WorkplaceId >= 1 && WorkplaceId <= 6) || WorkplaceId == 85)
        {
            ddlResultTemplate.SelectedIndex = -1;
            SetTemplateValue();
        }


        try
        {
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();

            cmd.CommandText = @"select cp.*, z.MM_IN_DUIM
                                from campaigns cp 
                                join (select MATNR, MM_IN_DUIM 
                                      from ORACLE.Z_SPR_MATERIALS zsm  
                                      join TESC3.SPR_NTD sn on  zsm.D_NTDQM =sn.NTD_NAME and SN.IS_ACTIVE=1
                                      group by MATNR, MM_IN_DUIM) z on cp.INVENTORY_CODE=z.MATNR 
                                 where cp.CAMPAIGN_LINE_ID=?
                                   and Cp.EDIT_STATE = 0 ";

            cmd.Parameters.AddWithValue("CAMPAIGN_LINE_ID", (sender as DropDownList).SelectedItem.Value);
            OleDbDataReader reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                if (sender == ddlCampaign)
                {
                    txbZakazLine.Text = reader["ORDER_LINE"].ToString();
                    txbZakazNo.Text = reader["ORDER_HEADER"].ToString();
                    txbInventoryNumber.Text = reader["INVENTORY_CODE"].ToString();
                    hfInventoryNumberKP.Value = reader["INVENTORY_CODE_KP"].ToString();
                    txbInventoryNumber_TextChanged(sender, e);
                    SelectDDLItemByValue(ddlLabelType, reader["LABEL_TYPE_ID"].ToString());
                    SelectDDLItemByValue(ddlLabelTypeKmk, reader["KMK_MARKING_TEMPLATE_ID"].ToString());

                    //маршрутная карта из строки задания на кампанию
                    int campaignLineId = Convert.ToInt32((sender as DropDownList).SelectedItem.Value);
                    String routeMap = GetCampaignPipeRouteMap(campaignLineId);
                    SelectDDLItemByValue(ddlPipeRouteMap, routeMap);

                    //маршрут СГП из строки задания на кампанию
                    String sgpRouteId = reader["INSPECTION_ROUTE_ID"].ToString();
                    //SelectDDLItemByValue(ddlSgpRoute, sgpRouteId);
                    if (!String.IsNullOrEmpty(reader["MM_IN_DUIM"].ToString()))
                    {
                        if (reader["MM_IN_DUIM"].ToString().CompareTo("1") == 0)
                        {
                            cbInch.Checked = true;
                        }
                    }
                }
            }
            reader.Close();

            reader.Dispose();
            cmd.Dispose();
            SetColorAndResidual((sender as DropDownList).SelectedItem.Value);
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка при получении данных из списка кампаний: " + ex.Message, ex);
        }
    }


    //отображение календаря при смене месяца
    protected void cldStartDate_VisibleMonthChanged(object sender, MonthChangedEventArgs e)
    {
        (sender as System.Web.UI.WebControls.Calendar).Style[HtmlTextWriterStyle.Display] = "";
        (sender as System.Web.UI.WebControls.Calendar).Visible = true;
    }

    //закрытие подключений при выгрузке UpdatePanel
    protected void UpdatePanel1_Unload(object sender, EventArgs e)
    {
        try
        {
            try
            {
                Master.Connect.CloseConnections();
            }
            catch
            { }
        }
        finally
        {
            //
        }
    }


    protected void Page_Unload(object sender, EventArgs e)
    {
        try
        {
            try
            {
                Master.Connect.CloseConnections();
            }
            catch
            { }
        }
        finally
        {
            //
        }
    }


    /// <summary>
    /// Отправка данных в ПЛК транспорта
    /// </summary>    
    /// <param name="pipeDirectionCode">Код направления трубы, передаваемый в ПЛК транспорта. Если NULL, то определяется на основе значения выбранного из списка "Маршрут СГП"</param>
    protected void SendCommandToTransportPlc(int? pipeDirectionCode = null)
    {
        //не отправлять данные при входе с ПК разработчиков
        if (Authentification.User.UserName == "DEV_LOGIN") return;



        try
        {
            //код команды для ПЛК = номер рабочего места
            short cmdCode = (short)WorkplaceId;

            //отправка в ПЛК транспорта линии 1
            if (cmdCode > 0 && cmdCode <= 3)
                PipeTrackingPLC.SendCommand(PipeTrackingPLC.PLC.NloTransport, cmdCode, PipeNumber, pipeDirectionCode.Value, true);

            //отправка в ПЛК транспорта линии 2
            if (cmdCode > 3 && cmdCode <= 6)
                PipeTrackingPLC.SendCommand(PipeTrackingPLC.PLC.Line2Transport, cmdCode, PipeNumber, pipeDirectionCode.Value, true);
        }
        catch (Exception ex)
        {
            if (!IGNORE_UDP_ERRORS)
            {
                Master.AddErrorMessage("Ошибка " + ex.Message, ex);
            }
        }
    }


    //обновление видимости строки задания на кампанию
    protected void UpdateCampaignLineVisiblity(bool Visible)
    {
        if (Visible)
        {
            tblCampaign.Style["display"] = "";
            tblOrderInfo.Style["display"] = "none";
            tblSelectSortament.Style["display"] = "none";
            tblSelectRoute.Style["display"] = "none";
        }
        else
        {
            tblCampaign.Style["display"] = "none";
            tblOrderInfo.Style["display"] = "";
            tblSelectSortament.Style["display"] = "";
            tblSelectRoute.Style["display"] = "";
            ddlCampaign.SelectedIndex = 0;
        }
    }

    //обновление строки кампании при изменении флажка "выбор из списка"
    protected void cbShowCampaigns_CheckedChanged(object sender, EventArgs e)
    {
        ddlNTD_SelectedIndexChanged(sender, e);
    }

    //обновление состояния надписи "Длина на УЗК шва"
    protected void UpdateLengthLabels(String PipeNumber)
    {
        lblMrt1420Length.Text = "нет";
        lblUscLength.Text = "нет";
        lblIzmLength.Text = "нет";
        lblTosLength.Text = "нет";
        lblDivLenght.Text = "нет";
        lblMrt1420Length.BackColor = Color.White;
        lblUscLength.BackColor = Color.White;
        lblIzmLength.BackColor = Color.White;
        lblTosLength.BackColor = Color.White;
        lblDivLenght.BackColor = Color.White;
        try
        {
            //номер рабочего места УЗК (для чтения длины)
            int usc_workplace = 0;
            if (WorkplaceId <= 3) usc_workplace = 13;
            if (WorkplaceId >= 4) usc_workplace = 14;

            //чтение длины трубы с измерителя
            int PipeLength = 0;
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "select LENGTH from IZMLENGTH_OTDELKA where (PIPE_NUMBER=?)and(EDIT_STATE=0) order by REC_DATE desc";
            cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);
            OleDbDataReader rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                if (rdr["LENGTH"] != DBNull.Value)
                {
                    PipeLength = Convert.ToInt32(rdr["LENGTH"]);
                    lblIzmLength.Text = PipeLength.ToString();
                }
            }
            rdr.Close();
            rdr.Dispose();

            //чтение длины трубы с МРТ
            int MrtLength = 0;
            cmd.CommandText = @"select round(LENGTH) LENGTH from (
	            select STARTTIME TT, 1 TBL, LENGTH from GEOMETRY_PIPES_DIAM where NUMBERF = ?
	            UNION ALL
	            select TIME TT, 2 TBL, LENGTH from GEOMETRY_PIPES_FASKA where PIPE = ?
                ) order by TBL desc, TT desc";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("NUMBERF", PipeNumber);
            cmd.Parameters.AddWithValue("PIPE", PipeNumber);
            rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                MrtLength = Convert.ToInt32(rdr["LENGTH"]);
                lblMrt1420Length.Text = MrtLength.ToString();
            }
            rdr.Close();
            rdr.Dispose();

            //чтение длины трубы с ТОС
            int TosLength = 0;
            cmd.CommandText = "select PIPELENGTH from OPTIMAL_PIPES where PIPE_NUMBER=? order by CUTDATE desc";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);
            rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                TosLength = Convert.ToInt32(rdr["PIPELENGTH"]);
                lblTosLength.Text = TosLength.ToString();
            }
            rdr.Close();
            rdr.Dispose();

            //получение длины трубы с УЗК            
            int UscLength = 0;
            cmd.CommandText = "select * from USC_OTDELKA where PIPE_NUMBER=? and EDIT_STATE=0 and WORKPLACE_ID=? order by rec_date desc";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);
            cmd.Parameters.AddWithValue("WORKPLACE_ID", usc_workplace);
            rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                if (rdr["LENGTH"] != DBNull.Value)
                {
                    UscLength = Convert.ToInt32(rdr["LENGTH"].ToString());
                    lblUscLength.Text = UscLength.ToString();
                }
            }
            rdr.Close();
            rdr.Dispose();

            //получение обрези на ремонте            
            cmd.CommandText = @"select ip.trx_date, sd.defect_name, ip.length, Sum(Nvl(ip.cut_left_length, 0)) over(partition by pipe_number)  as CUT_LEFT_LENGTH
                from inspection_pipes ip
                left join spr_defect sd on ip.cut_left_defects=to_char(sd.id)
                where edit_state=0 and next_direction is not null
                and cut_left_length>0
                and pipe_number=?
                order by ip.trx_date";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);
            rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                if (rdr["CUT_LEFT_LENGTH"] != DBNull.Value)
                {
                    lblObrlenght.Text = rdr["CUT_LEFT_LENGTH"].ToString();
                }
            }
            rdr.Close();
            rdr.Dispose();

            //получение количества возвратов            
            cmd.CommandText = "select Count(PIPE_NUMBER )+1 as COUNT_RETURN  " +
                              "from INSPECTION_PIPES " +
                              "where PIPE_NUMBER=? and EDIT_STATE=0 and WORKPLACE_ID in (1, 2, 3, 4, 5, 6, 63, 80, 81, 82, 83, 84) ";

            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);
            rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                if (rdr["COUNT_RETURN"] != DBNull.Value)
                {
                    lblCount_Return.Text = rdr["COUNT_RETURN"].ToString();
                }
            }
            rdr.Close();
            rdr.Dispose();

            //получение длины трубы с гидропресса 
            cmd.CommandText = "select HDP.PIPE_LENGTH  from HYDROPRESS_DRIFTER_PIPE  hdp " +
                                "join HYDROPRESS hd  " +
                                "on HD.PIPE_NUMBER = HDP.PIPE_NUMBER " +
                                " where HD.EDIT_STATE =0 and HDP.PIPE_NUMBER=? " +
                                " order by HDP.REC_DATE ";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);
            rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                if (rdr["PIPE_LENGTH"] != DBNull.Value)
                {
                    lblhydLenght.Text = rdr["PIPE_LENGTH"].ToString();
                }
            }
            rdr.Close();
            rdr.Dispose();

            //на линии 2 длина с измерителя = длина УЗК, т.к. нет измерителя
            if (WorkplaceId == 4 || WorkplaceId == 5 || WorkplaceId == 6)
                PipeLength = UscLength;

            //изменение аналогично линии 2 по ЗНИ 151557
            //В форме ввода «Ввод данных по трубам на инспекционных решетках» для инспекций №1, №2 и №3 считать длиной трубы на измерителе длины - длину с установки УЗК шва 1-ой линии отделки,
            //по аналогии со 2-ой линией отделки
            if (WorkplaceId == 1 || WorkplaceId == 2 || WorkplaceId == 3)
                PipeLength = UscLength;

            //подсветка желтым цветом при большом отличии длины
            if (Math.Abs(PipeLength - UscLength) > MAX_DELTALENGTH_WARNING)
            {
                lblUscLength.BackColor = Color.Yellow;
                lblIzmLength.BackColor = Color.Yellow;
                Master.FocusControl = txbLength.ID;
            }


            if (Math.Abs(PipeLength - TosLength) > MAX_DELTALENGTH_WARNING)
            {
                lblTosLength.BackColor = Color.Yellow;
                lblIzmLength.BackColor = Color.Yellow;
                Master.FocusControl = txbLength.ID;
            }


            //подсветка при отсутствии данных по длине трубы
            if (TosLength == 0)
                lblTosLength.BackColor = Color.Yellow;
            if (PipeLength == 0)
                lblIzmLength.BackColor = Color.Yellow;

            int Obrlenght = 0, Count_Return = 0;
            int.TryParse(lblObrlenght.Text, out Obrlenght);
            int.TryParse(lblCount_Return.Text, out Count_Return);
            lblDivLenght.Text = (Math.Abs(PipeLength - (TosLength - Obrlenght - (20 * Count_Return)))).ToString();

            //подсветка красным длины на МРТ
            int USC_MRT_DIFFERENCE_MAX = -1;
            cmd.CommandText = @"
                SELECT op.pipe_number, pi.diameter, pi.thickness, pi.gost, pi.grup, ntd.ntd_name, ntd.ntd_group, sgp.length_difference_max
                FROM tesc3.optimal_pipes op
                LEFT JOIN tesc3.geometry_coils_sklad gcs ON gcs.edit_state = 0 AND gcs.coil_run_no = op.coil_internalno AND gcs.coil_pipepart_no = op.coil_pipepart_no AND gcs.coil_pipepart_year = op.coil_pipepart_year
                LEFT JOIN tesc3.campaigns cmp ON cmp.edit_state = 0 AND cmp.campaign_line_id = gcs.campaign_line_id
                LEFT JOIN oracle.v_t3_pipe_items pi ON pi.nomer = cmp.inventory_code
                LEFT JOIN tesc3.spr_ntd ntd ON ntd.ntd_name = pi.gost AND nvl(ntd.ntd_group, 'NULL') = nvl(pi.grup, 'NULL')
                LEFT JOIN spr_settings_geom_params sgp ON sgp.ntd_id = ntd.ID AND sgp.diam = pi.diameter 
                            AND (pi.thickness BETWEEN sgp.thickness_from AND sgp.thickness_to)
                WHERE op.pipe_number = ?";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);
            rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                USC_MRT_DIFFERENCE_MAX = (int.TryParse(rdr["LENGTH_DIFFERENCE_MAX"].ToString(), out USC_MRT_DIFFERENCE_MAX)) ? USC_MRT_DIFFERENCE_MAX : -1;
            }
            rdr.Close();
            rdr.Dispose();
            if (UscLength == 0 || (USC_MRT_DIFFERENCE_MAX > 0 && Math.Abs(MrtLength - UscLength) > USC_MRT_DIFFERENCE_MAX))
            {
                lblMrt1420Length.BackColor = Color.Red;
            }
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка получения длины трубы с УЗК шва", ex);
        }
    }


    //получение года партии и номер партии трубы из резервных номеров
    protected bool GetReservePipepart(int PipeNumber, out int LotNumber, out int LotYear)
    {
        LotNumber = 0;
        LotYear = 0;

        OleDbConnection conn = Master.Connect.ORACLE_TESC3();
        OleDbCommand cmd = conn.CreateCommand();
        cmd.CommandText = "select LOT_NUMBER, LOT_YEAR from RESERVE_NUMBERS WHERE (PIPE_NUMBER=?)and(EDIT_STATE=0) order by REC_DATE desc";
        cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);
        OleDbDataReader rdr = cmd.ExecuteReader();
        if (rdr.Read())
        {
            if (rdr["LOT_YEAR"].ToString() != "") LotYear = Convert.ToInt32(rdr["LOT_YEAR"]);
            if (rdr["LOT_NUMBER"].ToString() != "") LotNumber = Convert.ToInt32(rdr["LOT_NUMBER"]);
        }
        rdr.Close();
        rdr.Dispose();
        cmd.Dispose();

        return (LotYear != 0);
    }


    //проверка возможности ввода данных по трубе на рабочем месте
    //согласно маршрутной карте
    //возвращает true, если приемка возможна. Иначе false и messageBox на клиенте.
    protected bool CheckValidCurrentPositionOfPipe(bool showMessageBox = true)
    {
        if (DISABLE_ROUTE_CONTROL) return true;

        //выход, если форма открыта в режиме ПДО
        if (IsPdoForm) return true;

        //буфер сообщений        
        List<String> msgs = new List<string>();

        //проврерка
        if (Checking.CheckValidCurrentPositionOfPipe(PipeNumber, WorkplaceId, ref msgs))
            return true;
        else
        {
            //если ввод данных невозможен - отображение сообщения
            if (showMessageBox)
            {
                String msg = "ВНИМАНИЕ!\nВвод данных по трубе " + PipeNumber + " невозможен";
                if (msgs.Count > 0)
                {
                    msg += " по следующим причинам:\n";
                    foreach (String txt in msgs)
                        msg += "\n- " + txt;
                }
                Master.AlertMessage = msg;
            }
            return false;
        }
    }


    /// <summary>
    /// Проверка возможности изменения/удаления записи по времени (ограничение времени редактирования старых записей).
    /// Возвращает true если запись можно редактироать, иначе false и отображение сообщения на клиенте
    /// </summary>
    /// <param name="rowId"></param>
    /// <returns></returns>
    protected bool CheckCanEditRecByTime(String rowId)
    {
        //интервал времени с момента сохранения, после которого запрет редактирования записей
        const int MAX_TIME_INTERVAL_MINUTES = 3;

        if (DISABLE_ROUTE_CONTROL) return true;

        //выход, если форма открыта в режиме ПДО
        if (IsPdoForm) return true;

        using (OleDbCommand cmd = Master.Connect.ORACLE_TESC3().CreateCommand())
        {
            //получение интервала времени в минутах с момента предыдущего ввода записи
            cmd.CommandText = "select (SYSDATE-TRX_DATE)*24*60 MINUTES from INSPECTION_PIPES where ROW_ID=?";
            cmd.Parameters.AddWithValue("ROW_ID", rowId);

            using (OleDbDataReader rdr = cmd.ExecuteReader())
            {
                if (rdr.Read())
                {
                    double minutes = Convert.ToDouble(rdr["MINUTES"]);

                    if (minutes > MAX_TIME_INTERVAL_MINUTES)
                    {
                        Master.AlertMessage = "Запись по трубе не может быть отредактирована, т.к. она была сохранена более " + MAX_TIME_INTERVAL_MINUTES.ToString() + " минут назад.";
                        return false;
                    }
                }
            }
        }

        return true;
    }


    //проверка возможности редактирования данных по трубе на рабочем месте
    //согласно маршрутной карте
    //возвращает true, если правка возможна. Иначе false и messageBox на клиенте
    protected bool CheckCanEditRecByPipe(int PipeNumber)
    {
        if (DISABLE_ROUTE_CONTROL) return true;

        //выход, если форма открыта в режиме ПДО
        if (IsPdoForm) return true;

        //код рабочего места, буфер сообщений
        List<String> msgs = new List<string>();

        //проврерка
        if (Checking.CanEditRecByPipe(PipeNumber, WorkplaceId, ref msgs))
            return true;
        else
        {
            //если ввод данных невозможен - отображение сообщения
            String msg = "ВНИМАНИЕ!\nПравка данных по трубе " + PipeNumber + " невозможна";
            if (msgs.Count > 0)
            {
                msg += " по следующим причинам:\n";
                foreach (String txt in msgs)
                    msg += "\n- " + txt;
            }
            Master.AlertMessage = msg;
            return false;
        }
    }


    //Сохранение позиции трубы для отслеживания маршрута
    private void SetNewPositionOfPipe()
    {
        try
        {
            if (DISABLE_ROUTE_CONTROL) return;

            try
            {
                //если обновление записи - выход
                if (UpdateRowID != "") return;

                //сохранение
                List<String> msg = new List<string>();
                if (!Checking.SetNewPositionOfPipe(PipeNumber, WorkplaceId, ref msg))
                    throw new Exception();
            }
            catch (Exception ex)
            {
                Master.AddErrorMessage("Ошибка сохранения данных по позиции трубы для отслеживания маршрута", ex);
            }
        }
        finally
        {
            //
        }
    }


    //удаление последней позиции трубы для отслеживания маршрутов
    //rowid - ID оригинальной записи
    private void DeleteLastPositionOfPipe(String rowid)
    {
        try
        {
            //получение номера трубы по rowid
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "select PIPE_NUMBER from INSPECTION_PIPES where ROW_ID=?";
            cmd.Parameters.AddWithValue("ROW_ID", rowid);
            OleDbDataReader rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                int PIPE_NUMBER = Convert.ToInt32(rdr["PIPE_NUMBER"]);

                //удаление записи в отслеживании маршрутов
                List<String> msg = new List<string>();
                Checking.DeleteLastPositionOfPipe(PIPE_NUMBER, WorkplaceId, ref msg);
                if (msg.Count != 0)
                {
                    String ex = "";
                    foreach (String txt in msg)
                        ex += "; " + txt;
                    throw new Exception(ex);
                }
            }
            rdr.Close();
            rdr.Dispose();
            cmd.Dispose();
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка удаления записи при отслеживании маршрута", ex);
        }
    }



    //свойство - масса последней трубы, кг
    protected int LastPipeWeight
    {
        get
        {
            object o = ViewState["LastPipeWeight"];
            if (o == null)
                return (int)(DateTime.Now.Year % 100);
            else return Convert.ToInt32(o);
        }
        set
        {
            ViewState["LastPipeWeight"] = value;
        }
    }



    //свойство - номер партии штрипса при партионном учете
    protected String ShtripsLotNumber
    {
        get
        {
            object o = ViewState["SHTRIPS_LOT_NUMBER"];
            if (o == null)
                return "";
            else return o.ToString();
        }
        set
        {
            ViewState["SHTRIPS_LOT_NUMBER"] = value;
        }
    }


    //свойство - признак того, что данные сохранены до печати бирки
    protected bool DataSaved
    {
        get
        {
            object o = ViewState["DataSaved"];
            if (o == null)
                return false;
            else return Convert.ToBoolean(o);
        }
        set
        {
            ViewState["DataSaved"] = value;
        }
    }


    //обновление массы трубы из IP21 в текстовом поле
    protected void btnRefreshWeight_Click(object sender, EventArgs e)
    {
        SetWeightText(GetWeightFromIp21());
    }


    //получение массы трубы из IP21
    private double GetWeightFromIp21()
    {
        try
        {
            lblWeight.Text = "";

            //выход, если форма открыта в режиме ПДО
            if (IsPdoForm) return double.NaN;

            //выход если осуществляется правка записи
            if (UpdateRowID != "") return double.NaN;

            //если инспекция не КЛО - выход            
            if (WorkplaceId < 4 || WorkplaceId > 6) return double.NaN;

            //имя поля, из которого брать данные
            String inspection_field = "INSP" + WorkplaceId.ToString();

            //подключение к БД и получение значения из IP21
            int wght = 0;
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = @"select " + inspection_field + @"
                from izmweight_otdelka_data_bridge
                where " + inspection_field + @" is not null
                and rec_date>sysdate-1/24
                order by rec_date desc";
            OleDbDataReader rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                wght = Convert.ToInt32(rdr[inspection_field]);
                lblWeight.Text = "(" + rdr[inspection_field].ToString() + ")";
            }
            rdr.Close();
            rdr.Dispose();
            cmd.Dispose();

            //возврат полученного значения
            return wght;
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка получения массы трубы с измерителя линии 2", ex);
            return 0;
        }
    }



    /// <summary>
    /// Признак выбора дефекта, который является основанием для перевода
    /// </summary>
    protected bool IsPerevodDefect()
    {
        OleDbConnection conn = Master.Connect.ORACLE_TESC3();
        using (OleDbCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = "select max(IS_PEREVOD) from SPR_DEFECT where ID=?";
            cmd.Parameters.AddWithValue("ID", ddlDefect.SelectedItem.Value.Trim());
            return (cmd.ExecuteScalar().ToString() == "1");
        }
    }


    /// <summary>
    /// Признак выбора дефекта, который выводит длину труб за допуски требований НД
    /// </summary>
    /// <returns></returns>
    protected bool IsReduceLengthDefect()
    {
        OleDbConnection conn = Master.Connect.ORACLE_TESC3();
        using (OleDbCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = "select max(IS_REDUCE_LENGTH) from SPR_DEFECT where ID=?";
            cmd.Parameters.AddWithValue("ID", ddlDefect.SelectedItem.Value.Trim());
            return (cmd.ExecuteScalar().ToString() == "1");
        }
    }


    /// <summary>
    /// Признак обязательности указания дефекта для указанного НД
    /// </summary>
    /// <param name="gost"></param>
    /// <param name="gostGroup"></param>
    /// <returns></returns>
    protected bool IsDefectRequired(String gost, String gostGroup)
    {
        OleDbConnection conn = Master.Connect.ORACLE_TESC3();
        using (OleDbCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = "select max(DEFECT_REQUIRED) from SPR_NTD where NTD_NAME=? and NVL(NTD_GROUP, '-')=nvl(?, '-')";
            cmd.Parameters.AddWithValue("GOST", gost);
            cmd.Parameters.AddWithValue("GOST_GROUP", gostGroup);
            return (cmd.ExecuteScalar().ToString() == "1");
        }
    }


    //обновление панели указания дефекта/№ распоряжения
    protected void UpdatePanel3_PreRender(object sender, EventArgs e)
    {
        //объекты с дефектами
        List<Panel> pnl_defects = new List<Panel>() { pnlDefect, pnlDefect2, pnlDefect3 }; //панели
        List<DropDownList> ddl_defects = new List<DropDownList>() { ddlDefect, ddlDefect2, ddlDefect3 }; //выпадающие списки дефектов
        List<DropDownList> ddl_delta_size = new List<DropDownList>() { ddlDeltaSize, ddlDeltaSize2, ddlDeltaSize3 }; //выпадающие списки величина смещения
        List<TextBox> txb_defect_description = new List<TextBox>() { txbDefectDescription, txbDefectDescription2, txbDefectDescription3 };
        List<TextBox> txb_defect_distance = new List<TextBox>() { txbDefectDistance, txbDefectDistance2, txbDefectDistance3 };
        List<DropDownList> ddl_defect_location = new List<DropDownList>() { ddlDefectLocation, ddlDefectLocation2, ddlDefectLocation3 };

        //очистка полей расположения дефекта при невыбранном дефекте
        for (int i = 0; i < 3; i++)
        {
            if (!pnl_defects[i].Visible || ddl_defects[i].SelectedItem.Value.Trim() == "")
            {
                ddl_defects[i].SelectedIndex = 0;
                txb_defect_description[i].Text = "";
                txb_defect_distance[i].Text = "";
                ddl_defect_location[i].SelectedIndex = 0;
            }
        }

        //активация/деактивация списка указания дополнительного дефекта
        ddlDefectAdditional.Enabled = IsReduceLengthDefect();
        if (!ddlDefectAdditional.Enabled)
            SelectDDLItemByValue(ddlDefectAdditional, "");

        //ремонт зачисткой
        if (ddlZachistkaEnabled.SelectedIndex == 1)
        {
            ddlDefectZachistka.Enabled = true;
            txbZachistkaLength.Enabled = true;
            txbZachistkaThickness1.Enabled = true;
            txbZachistkaThickness2.Enabled = true;
        }
        else
        {
            ddlDefectZachistka.Enabled = false;
            txbZachistkaLength.Enabled = false;
            txbZachistkaThickness1.Enabled = false;
            txbZachistkaThickness2.Enabled = false;
            ddlDefectZachistka.SelectedIndex = 0;
            txbZachistkaLength.Text = "";
            txbZachistkaThickness1.Text = "";
            txbZachistkaThickness2.Text = "";
        }

        //отображение дополнительной информации по дефекту
        DisplayDefectInfo();

        // Нсатройка рабочего центра «Установка по зачистке внутренней поверхности» 
        if (WorkplaceId == 85)
        {
            pnlRepairScraping.Visible = false;
            SelectDDLItemByValue(ddlZachistkaEnabled, "Не производился");

            for (int i = 0; i < 3; i++)
            {
                ddl_delta_size[i].Visible = false;
                txb_defect_description[i].Visible = false;
                ddl_defect_location[i].Visible = false;
                txb_defect_distance[i].Visible = false;
            }

            lblNote.Visible = false;
            lblSize.Visible = false;
            lblDist.Visible = false;
            lblNote2.Visible = false;
            lblSize2.Visible = false;
            lblDist2.Visible = false;
            lblNote3.Visible = false;
            lblSize3.Visible = false;
            lblDist3.Visible = false;
            btnFromSklad.Visible = false;
            pnlPerevod.Visible = false;
            pnlScraping.Visible = true;

            if (cbScraping.Checked)
            {
                txbAmounts.Text = "";
                txbAmounts.Enabled = false;
            }
            else
                txbAmounts.Enabled = true;

            lblKMK.Visible = false;
            ddlLabelTypeKmk.Visible = false;
            cbRepair.Visible = false;
            pnlDefect2.Visible = pnlDefect3.Visible = true;
            lblDefectName.Text = "Дефект №1";
        }
        else
        {
            pnlRepairScraping.Visible = true;
            //ddlZachistkaEnabled.SelectedIndex = -1;

            //отображение поля "величина смещения"
            for (int i = 0; i < 3; i++)
            {
                txb_defect_description[i].Visible = true;
                ddl_defect_location[i].Visible = true;
                txb_defect_distance[i].Visible = true;

                if (pnl_defects[i].Visible)
                {
                    String defect_id = ddl_defects[i].SelectedItem.Value;
                    if (defect_id == "107" || defect_id == "345") // "смещение" и "настр.смещение"
                    {
                        ddl_delta_size[i].Visible = true;
                    }
                    else
                    {
                        ddl_delta_size[i].Visible = false;
                        ddl_delta_size[i].SelectedIndex = 0;
                        ddlDeltaSize.Visible = false;
                    }
                }
            }

            lblNote.Visible = true;
            lblSize.Visible = true;
            lblDist.Visible = true;
            lblNote2.Visible = true;
            lblSize2.Visible = true;
            lblDist2.Visible = true;
            lblNote3.Visible = true;
            lblSize3.Visible = true;
            lblDist3.Visible = true;
            btnFromSklad.Visible = true;
            pnlPerevod.Visible = true;
            pnlScraping.Visible = false;
            ddlLabelTypeKmk.Visible = true;
            cbRepair.Visible = true;
            lblKMK.Visible = true;
            pnlDefect2.Visible = pnlDefect3.Visible = cbRepair.Checked;
            lblDefectName.Text = cbRepair.Checked ? "Дефект №1" : "Дефект";
        }

        //очистка объектов
        pnl_defects = null;
        ddl_defects = null;
        ddl_delta_size = null;
        ddl_defect_location = null;
        txb_defect_description = null;
        txb_defect_distance = null;
    }



    //отмена перевода в пониженную стенку
    protected void btnPerevodCancel_Click(object sender, EventArgs e)
    {
        //переключение вида
        MainMultiView.SetActiveView(InputDataView);
    }



    //проверка передачи данных в SAP по исправляемой записи
    //возвращает true, если запись передавалась в интерфейс SAP/SAP
    protected bool CheckSapProcessed(String ROW_ID)
    {
        //выход, если форма в режиме ПДО
        if (IsPdoForm) return false;

        //проверка передачи записи в SAP
        OleDbConnection conn = Master.Connect.ORACLE_TESC3();
        OleDbCommand cmd = conn.CreateCommand();
        cmd.CommandText = @"select max(NVL(SAP_PROCESSED,0)) SAP_PROCESSED 
            from INSPECTION_PIPES where EDIT_STATE=0 and ROW_ID=?";
        cmd.Parameters.AddWithValue("ROW_ID", ROW_ID);
        int p = Convert.ToInt32(cmd.ExecuteScalar());
        cmd.Dispose();
        return (p != 0);
    }


    //установка пометки приемки трубы в актах возврата со склада
    protected void SetInspectionDateInSkladReturnActs()
    {
        OleDbConnection conn = Master.Connect.ORACLE_TESC3();
        OleDbCommand cmd = conn.CreateCommand();
        cmd.CommandText = "update SKLAD_RETURN set INSPECTION_DATE=SYSDATE where PIPE_NUMBER=? and EDIT_STATE=0 and INSPECTION_DATE is NULL";
        cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);
        cmd.ExecuteNonQuery();
        cmd.Dispose();
    }


    //снятие пометки приемки трубы с последней записи в актах возврата со склада
    protected void ResetInspectionDateInSkladReturnActs(String InspectionPipesRowId)
    {
        if (InspectionPipesRowId == "") return;

        //получение id последней записи по трубе в актах возврата
        String SkladReturnRowId = "";
        OleDbConnection conn = Master.Connect.ORACLE_TESC3();
        OleDbCommand cmd = conn.CreateCommand();
        cmd.CommandText = "select ROW_ID from SKLAD_RETURN "
            + "where PIPE_NUMBER=(select PIPE_NUMBER from INSPECTION_PIPES where ROW_ID=? and ROWNUM=1) "
            + "and EDIT_STATE=0 and INSPECTION_DATE is NOT NULL "
            + "order by REC_DATE desc";
        cmd.Parameters.AddWithValue("INSPECTION_ROW_ID", InspectionPipesRowId);
        OleDbDataReader rdr = cmd.ExecuteReader();
        if (rdr.Read())
        {
            SkladReturnRowId = rdr["ROW_ID"].ToString();
        }
        rdr.Close();
        rdr.Dispose();
        cmd.Dispose();

        //снятие пометки приемки трубы
        cmd = conn.CreateCommand();
        cmd.CommandText = "update SKLAD_RETURN set INSPECTION_DATE=NULL where ROW_ID=?";
        cmd.Parameters.AddWithValue("ROW_ID", SkladReturnRowId);
        cmd.ExecuteNonQuery();
    }


    //получение имени поставщика рулонной стали для печати бирки
    protected String GetSteelSupplierName(object PipeNumber)
    {
        //получение полного имени поставщика металла из данных по геометрии рулонной стали
        String SupplierName = "";

        OleDbConnection conn = Master.Connect.ORACLE_TESC3();
        OleDbCommand cmd = conn.CreateCommand();
        cmd.CommandText = "select gc.SUPPLIER from OPTIMAL_PIPES op "
            + "join GEOMETRY_COILS_SKLAD gc on OP.COIL_PIPEPART_YEAR=gc.COIL_PIPEPART_YEAR and op.COIL_PIPEPART_NO=gc.COIL_PIPEPART_NO and OP.COIL_INTERNALNO=GC.COIL_RUN_NO "
            + "where gc.EDIT_STATE=0 and op.PIPE_NUMBER=?";
        cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);
        OleDbDataReader rdr = cmd.ExecuteReader();
        if (rdr.Read())
        {
            SupplierName = rdr["SUPPLIER"].ToString();
        }
        rdr.Close();
        rdr.Dispose();
        cmd.Dispose();

        //приведение поставщика к сокращенной форме
        if (SupplierName.ToUpper() == "СТАН 2000 СЕВЕРСТАЛЬ") return "CC";
        if (SupplierName.ToUpper() == "СТАН 2000 ООО ОМК-СТАЛЬ ФИЛИАЛ Г.ВЫКСА" || SupplierName.ToUpper() == "ФИЛИАЛ ОАО ОМК-СТАЛЬ Г.ВЫКСА НИЖЕГОРОДСКАЯ ОБЛАСТЬ") return "ЛПК";
        if (SupplierName.ToUpper() == "СТАН 2000 ММК") return "ММК";
        return SupplierName;
    }


    /// <summary>
    /// Получение системной даты по часам сервера БД
    /// </summary>
    /// <returns></returns>
    protected DateTime GetDbTime()
    {
        using (OleDbCommand cmd = Master.Connect.ORACLE_TESC3().CreateCommand())
        {
            cmd.CommandText = "select SYSDATE DAT from DUAL";
            using (OleDbDataReader rdr = cmd.ExecuteReader())
            {
                rdr.Read();
                return Convert.ToDateTime(rdr["DAT"]);
            }
        }
    }


    //получение учетной даты из поля ввода (если форма в режиме ПДО)    
    //возвращает DateTime.Now если невозможно получить дату из строки ввода    
    protected DateTime GetActualDate()
    {
        if (IsPdoForm)
        {
            //возвращение даты/времени из календаря выбора даты
            if (cldDate.SelectedDate != null)
                return cldDate.SelectedDate.Value;
            else
                return GetDbTime();
        }

        //возврат данных по часам сервера БД
        return GetDbTime();
    }


    //свойство - признак того, что форма открыта в режиме ПДО
    protected bool IsPdoForm
    {
        get
        {
            return (WorkplaceId == 0 || Authentification.CanEditData("PDO"));
        }
    }


    //установка значения в поле массы (кг)
    protected void SetWeightText(double value)
    {
        txbWeight.BackColor = Color.White;
        txbWeight.Text = "";
        if (!double.IsNaN(value))
        {
            txbWeight.Text = value.ToString();
        }
        else
        {
            return;
        }

        if (!IsPdoForm)
        {
            //подсветка, если не изменилось значение массы
            if (LastPipeWeight == value)
                txbWeight.BackColor = Color.Red;

            //подсветка, если значение в недопустимом диапазоне
            if (value < 150 | value > 2000)
            {
                txbWeight.BackColor = Color.Red;
                txbWeight.Text = "";
            }

            //запоминание текущей массы
            LastPipeWeight = (int)value;
        }
    }


    //сравнение теоретической и фактической массы трубы,
    //добавляет предупрежденгие на форму при расхождении
    //возвращает true, если расхождение в пределах нормы    
    protected bool CheckDeltaWeight()
    {
        //не проверять для профильных труб и для инспекций 7-11
        if (WorkplaceId == 63 || (WorkplaceId >= 80 && WorkplaceId <= 84))
            return true;

        //получение фактической массы и длины из полей ввода
        if (txbWeight.Text.Trim() == "") return true;
        if (txbLength.Text.Trim() == "") return true;
        int FactWeight = Convert.ToInt32(txbWeight.Text.Trim());
        int Length = Convert.ToInt32(txbLength.Text.Trim());

        //получение теоретической массы трубы
        double conversion_rate = 0;
        String InvCode = Checking.CorrectInventoryNumber(txbInventoryNumber.Text);
        OleDbConnection conn = Master.Connect.ORACLE_ORACLE();
        OleDbCommand cmd = conn.CreateCommand();
        cmd.CommandText = "select CONVERSION_RATE from V_T3_PIPE_ITEMS where ORG_ID=127 and NOMER=?";
        cmd.Parameters.AddWithValue("NOMER", InvCode);
        OleDbDataReader rdr = cmd.ExecuteReader();
        if (rdr.Read())
        {
            if (rdr["CONVERSION_RATE"].ToString() != "")
                conversion_rate = Convert.ToDouble(rdr["CONVERSION_RATE"]);
        }
        rdr.Close();
        rdr.Dispose();
        cmd.Dispose();
        if (conversion_rate == 0) return true;
        double TheorWeight = conversion_rate * Length;

        //сравнение теоретической и фактической массы и добавление предупреждения при расхождении
        if (Math.Abs(TheorWeight - FactWeight) > MAX_DELTAWEIGHT_WARNING)
        {
            AddWarning("Фактическая масса трубы отличается от расчётной теоретической массы.", true, true);
            return false;
        }
        else
        {
            return true;
        }
    }


    //сравнение длины трубы измеренной на ТОС и измерителем длины
    //добавляет предупреждение на форму при расхождении
    //возвращает true, если расхождение в пределах нормы    
    protected bool CheckDeltaLength()
    {
        //проверять для профильных труб и для инспекций 1-6
        if (WorkplaceId > 6 || WorkplaceId < 1)
            return true;

        if (lblTosLength.Text == "нет")
        {
            AddWarning("Нет данных по измерению длины трубы на трубоотрезном станке", true, true);
            return false;
        }

        int pipe_length = 0; int.TryParse(txbLength.Text.Trim(), out pipe_length);
        int tos_lengh = 0; int.TryParse(lblTosLength.Text, out tos_lengh);
        int obr_lenght = 0; int.TryParse(lblObrlenght.Text, out obr_lenght);
        int count_return = 0; int.TryParse(lblCount_Return.Text, out count_return);
        // if (Math.Abs(pipe_length - tos_lengh) > MAX_DELTALENGTH_WARNING)
        if (Math.Abs(pipe_length - (tos_lengh - obr_lenght - (20 * count_return))) > MAX_DELTALENGTH_WARNING)
        {
            AddWarning("Длина трубы с трубоотрезного станка не соответствует длине после измерителя линии отделки более чем на "
                + MAX_DELTALENGTH_WARNING.ToString() + " мм. Необходимо произвести измерение длины трубы вручную.", true, true);
            return false;
        }

        int hydLenght = 0;
        int.TryParse(lblhydLenght.Text, out hydLenght);
        if (Math.Abs(pipe_length - hydLenght) > MAX_DELTALENGTH_WARNING_HYD)
        {
            AddWarning("Длина трубы с измерителя УЗК не соответствует длине после измерителя на гидропрессе "
                + MAX_DELTALENGTH_WARNING_HYD.ToString() + " мм. Необходимо произвести измерение длины трубы вручную.", true, true);
        }
        return true;
    }



    //продолжение сохранения при подтверждении предупреждений (если нажата кнопка "принять трубу")
    protected void btnWeightWarningOk_Click(object sender, EventArgs e)
    {
        if (fldWarningReturnButtonId.Value == btnToSklad.ID)
        {
            btnToSklad_Click(btnToSklad, e);
            return;
        }

        if (fldWarningReturnButtonId.Value == btnFromSklad.ID)
        {
            btnToSklad_Click(btnFromSklad, e);
            return;
        }

        if (fldWarningReturnButtonId.Value == btnToRepair.ID)
        {
            btnToBrakOrRepair_Click(btnToRepair, e);
            return;
        }

        if (fldWarningReturnButtonId.Value == btnToBrak.ID)
        {
            btnToBrakOrRepair_Click(btnToBrak, e);
            return;
        }

        throw new Exception("Не определен код функции обратного вызова при выходе из окна предупреждения.");
    }

    //возврат ко вводу данных из окна предупреждения (если нажата кнопка "отмена")
    protected void btnWeightWarningCancel_Click(object sender, EventArgs e)
    {
        MainMultiView.SetActiveView(InputDataView);
        fldWarningReturnButtonId.Value = "";
    }


    //выбор дефекта из списка дефектов
    protected void ddlDefect_SelectedIndexChanged(object sender, EventArgs e)
    {
        //
    }



    //проверка корректности указания партии трубы
    //возвращает true если партия существует
    //или false, если партии нет на ОТО, на стане и в списке перевода
    //При возврате false добавляется текстовое сообщение в список предупреждений
    protected bool CheckLot()
    {
        try
        {
            //год и номер партии, номер трубы
            String LotYear = Convert.ToInt32(lblYear.Text).ToString();
            String LotNumber = Convert.ToInt32(txbPartNo.Text.Trim()).ToString();

            //проверка наличия партии на участке стана
            String StanLotYear = "";
            String StanLotNumber = "";
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "select COIL_PIPEPART_NO, COIL_PIPEPART_YEAR from OPTIMAL_PIPES where PIPE_NUMBER=?";
            cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);
            OleDbDataReader rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                StanLotYear = rdr["COIL_PIPEPART_YEAR"].ToString();
                StanLotNumber = rdr["COIL_PIPEPART_NO"].ToString();
            }
            rdr.Close();
            rdr.Dispose();
            cmd.Dispose();
            if (StanLotYear == LotYear && StanLotNumber == LotNumber) return true;

            //проверка наличия партии трубы на ОТО        
            String OtoLotNumber = "";
            cmd = conn.CreateCommand();
            cmd.CommandText = "select TOPARTNUMBER, PIPENUMBER from TERMO_OTDEL_PIPES_TESC3 where PIPENUMBER=?";
            cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);
            rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                OtoLotNumber = rdr["TOPARTNUMBER"].ToString();
            }
            rdr.Close();
            rdr.Dispose();
            cmd.Dispose();
            if (OtoLotNumber == LotNumber) return true;

            //проверка наличия партии в журнале перевода на утонение        
            String LotsFrom = "";
            cmd = conn.CreateCommand();
            cmd.CommandText = "select LOTS_FROM from PIPE_ALT_LOTS where LOT_NUMBER=? and LOT_YEAR=?";
            cmd.Parameters.AddWithValue("LOT_NUMBER", LotNumber);
            cmd.Parameters.AddWithValue("LOT_YEAR", LotYear);
            rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                LotsFrom = rdr["LOTS_FROM"].ToString();
            }
            rdr.Close();
            rdr.Dispose();
            cmd.Dispose();

            //проверка допустимости перевода в пониженную стенку из имеющихся партий трубы
            if (LotsFrom != "")
            {
                String[] lots = LotsFrom.Split(new char[] { ',' }, StringSplitOptions.None);
                foreach (String lot in lots)
                {
                    String[] lot_split = lot.Split(new char[] { '-' }, StringSplitOptions.None);
                    if (lot_split.Length == 2)
                    {
                        String FromLotYear = Convert.ToInt32(lot_split[0]).ToString();
                        String FromLotNumber = Convert.ToInt32(lot_split[1]).ToString();
                        if (FromLotYear == StanLotYear && FromLotNumber == StanLotNumber) return true;
                        if (FromLotYear == LotYear && FromLotNumber == OtoLotNumber) return true;
                    }
                }
            }

            //если партия не соответствует трубе, то добавление предупреждения
            String warning = "Партия " + LotNumber + " не соответствует ни одной партии, закрепленной за данной трубой.\n"
                + "Партия трубы на участке сварки: " + ((StanLotYear != "") ? (StanLotYear + "-" + StanLotNumber.PadLeft(4, '0')) : "нет данных") + "\n"
                + "Партия трубы на участке ОТО: " + ((OtoLotNumber != "") ? (LotYear + "-" + OtoLotNumber.PadLeft(4, '0')) : "нет данных");
            AddWarning(warning, true, true);
            return false;
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка проверки номера партии", ex);
            AddWarning("Ошибка проверки номера партии", true, true);
            return false;
        }
    }


    /// <summary>
    /// Проверка наличия данных по Pcm и Cэ. Возвращает true если данные есть, иначе false
    /// При возврате false добавляется текстовое сообщение в список предупреждений    
    /// </summary>
    private bool CheckCzlSmelting()
    {
        try
        {
            //год и номер партии, номер трубы            
            int LotYear = Convert.ToInt32(lblYear.Text);
            int LotNumber = Convert.ToInt32(txbPartNo.Text.Trim());

            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            double Pcm, Ceq;
            bool ok = OTK.GetPcmAndCeqByPipe(conn, PipeNumber, LotYear, LotNumber, out Pcm, out Ceq);
            if (!ok)
            {
                AddWarning("Данные по Сэ и/или Pcm отсутствуют по причине отсутствия плавки в документообороте ЦЗЛ.", true, true);
                return false;
            }
            else
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка проверки Pcm и Cэ", ex);
            AddWarning("Ошибка проверки Pcm и Cэ", true, true);
            return false;
        }
    }

    /// <summary>Проверка отбора проб у трубы, назначеной на пробы
    /// если труба назначена на пробы, но пробы от нее не отрезаны, возвращает false и добавляет в список предупреждений строку
    /// </summary>
    bool CheckPipeSampling()
    {
        bool IsSamplingPipe;
        return CheckPipeSampling(out IsSamplingPipe);
    }

    /// <summary>Проверка отбора проб у трубы, назначеной на пробы
    /// если труба назначена на пробы, но пробы от нее не отрезаны, возвращает false и добавляет в список предупреждений строку
    /// </summary>
    /// <param name="IsSamplingPipe">труба назначена на пробы</param>
    bool CheckPipeSampling(out bool IsSamplingPipe)
    {
        OleDbConnection conn = Master.Connect.ORACLE_TESC3();
        OleDbCommand cmd = conn.CreateCommand();

        //для труб произведенных ранее чем 01.07.2014 проверки не производятся
        if (CheckOldPipe()) IsSamplingPipe = true;

        IsSamplingPipe = Sampling.PipeIsSetOnSampling(cmd, PipeNumber);

        //отключение проверки для труб с резервными номерами
        if (Checking.CheckReserveNumber(PipeNumber)) return true;

        bool ret = true;
        if (IsSamplingPipe) ret = Sampling.PipeCutSampling(cmd, PipeNumber);

        cmd.Dispose();
        // если труба назначена на пробы, но пробы от нее не отрезаны, выводить сообщение и блокировать
        if (!ret)
        {
            AddWarning("От данной трубы необходимо произвести отбор проб для сдаточных испытаний", true, true);
            return false;
        }
        return true;
    }

    /// <summary>проверка назначения партии на сдаточные испытания
    /// если партия этой трубы не назначена на сдаточные испытания
    /// партия считается назначеной на сдаточные испытания, если, в формах «Назначение труб для проведения первичных сдаточных испытаний» или 
    /// «Назначение труб для первичных сдаточных испытаний после ОТО» для нее назначено достаточное количество труб
    /// При возврате false добавляется текстовое сообщение в список предупреждений    
    /// </summary>
    bool CheckSetSamplingForPart()
    {
        OleDbConnection conn = Master.Connect.ORACLE_TESC3();

        //для труб произведенных ранее чем 01.07.2014 проверки не производятся
        if (CheckOldPipe()) return true;

        //для профильных труб проверки не производятся
        if (CheckProfilePipe()) return true;

        //отключение проверки для труб с резервными номерами
        if (Checking.CheckReserveNumber(PipeNumber)) return true;

        bool ret = false;
        String samplingErrorMessage = "";
        using (OleDbCommand cmd = conn.CreateCommand())
        {
            try
            {
                ret = Sampling.PartIsSetOnSampling(cmd, txbPartYear.Text, txbPartNo.Text);
            }
            catch (Exception ex)
            {
                samplingErrorMessage = ex.Message;
            }
        }

        if (!ret)
        {
            if (samplingErrorMessage == "")
                AddWarning("На данную партию не назначены трубы для проведения первичных сдаточных испытаний", true, true);
            else
                AddWarning(samplingErrorMessage, true, true);

            return false;
        }

        return true;
    }


    /// <summary>
    /// Проверка, производена ли труба под заданный уровень исполнения. Если произведена, то добавляется сообщение в список предупреждений на странице
    /// </summary>
    /// <param name="pipeNumber"></param>
    /// <returns></returns>
    bool CheckIsProductionLevel(int pipeNumber)
    {
        OleDbConnection conn = Master.Connect.ORACLE_TESC3();
        OleDbCommand cmd = conn.CreateCommand();
        {
            cmd.CommandText = @"select mt.d_ntdqm GOST,
                    mt.d_ur_isp PRODUCTION_LEVEL
                from optimal_pipes op
                join geometry_coils_sklad gc
                    on op.coil_pipepart_year=gc.coil_pipepart_year
                    and op.coil_pipepart_no=gc.coil_pipepart_no
                    and op.coil_internalno=gc.coil_run_no
                    and gc.edit_state=0
                join campaigns cmp
                    on gc.campaign_line_id=cmp.campaign_line_id
                    and cmp.edit_state=0
                join oracle.z_spr_materials mt
                    on cmp.inventory_code=mt.matnr        
                where op.pipe_number=?";

            cmd.Parameters.AddWithValue("PIPE_NUMBER", pipeNumber);

            using (OleDbDataReader rdr = cmd.ExecuteReader())
            {
                if (rdr.Read())
                {
                    if (rdr["PRODUCTION_LEVEL"] != DBNull.Value)
                    {
                        AddWarning("Труба №" + pipeNumber.ToString() + " была изготовлена для выполнения заказа с уровнем качества/исполнения "
                                   + rdr["PRODUCTION_LEVEL"].ToString() + " и " + rdr["GOST"].ToString(), true, false);
                        return true;
                    }
                }
            }
        }

        return false;
    }


    //скрытие окна редактирования причины перевода в другое назначение
    protected void btnNotZakazCancel_Click(object sender, EventArgs e)
    {
        if (sender == btnNotZakazCancel) fldNotZakazCheckOk.Value = "";
        fldNotZakazCallbackButton.Value = "";
        ddlNotZakazReason.SelectedIndex = 0;
        txbNotZakazReasonDescription.Text = "";
        txbNotZakazReasonDistance.Text = "";
        txbNotZakazReasonValue.Text = "";
        fldNotZakazCheckOk.Value = "";
        pnlNotZakazReason.Visible = false;
    }


    //подтверждение причины перевода в другое назначение
    protected void btnNotZakazSave_Click(object sender, EventArgs e)
    {
        //проверка обязательных полей
        if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 50) && ddlNotZakazReason.SelectedItem.Value == "")
        {
            Master.AlertMessage = "Необходимо указать причину перевода в другое назначение.";
            Master.FocusControl = ddlNotZakazReason.ID;
            return;
        }
        if (txbNotZakazReasonValue.Enabled)
        {
            double val = 0;
            txbNotZakazReasonValue.Text = txbNotZakazReasonValue.Text.Replace('.', ',');
            if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 51) && (txbNotZakazReasonValue.Text.Trim() == "" ||
                                                                                        !Double.TryParse(txbNotZakazReasonValue.Text.Trim(), out val)))
            {
                Master.AlertMessage = "Необходимо ввести фактическое значение.";
                Master.FocusControl = txbNotZakazReasonValue.ID;
                return;
            }
        }
        if (txbNotZakazReasonDistance.Enabled)
        {
            double dist = 0;
            txbNotZakazReasonDistance.Text = txbNotZakazReasonDistance.Text.Replace('.', ',');
            if ((txbNotZakazReasonDistance.Text.Trim() == "" || !Double.TryParse(txbNotZakazReasonDistance.Text.Trim(), out dist)) &&
                Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 51))
            {
                Master.AlertMessage = "Необходимо ввести фактическое значение.";
                Master.FocusControl = txbNotZakazReasonDistance.ID;
                return;
            }

            double pipe_length = 0;
            Double.TryParse(txbLength.Text.Trim(), out pipe_length);
            if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 52) && dist > pipe_length)
            {
                Master.AlertMessage = "Расстояние от торца трубы должно быть не больше чем длина трубы.";
                Master.FocusControl = txbNotZakazReasonDistance.ID;
                return;
            }
        }

        //вызов обработчика нажатия кнопки сохранения данных
        fldNotZakazCheckOk.Value = "OK";
        if (fldNotZakazCallbackButton.Value == btnToSklad.ID) btnToSklad_Click(btnToSklad, e);
        if (fldNotZakazCallbackButton.Value == btnToRepair.ID) btnToBrakOrRepair_Click(btnToRepair, e);
        if (fldNotZakazCallbackButton.Value == btnToBrak.ID) btnToBrakOrRepair_Click(btnToBrak, e);
        if (fldNotZakazCallbackButton.Value == btnFromSklad.ID) btnToSklad_Click(btnFromSklad, e);

        //скрытие диалогового окна
        if (!Master.ErrorMessageVisible)
        {
            btnNotZakazCancel_Click(sender, e);
        }
    }


    //проверка необходимости указания причины перевода в другое назначение
    //возвращает true если указывать причину не нужно
    //или возвращает false и отображает диалог ввода причины    
    //Sender - кнопка-источник события
    //bPerevod - признак перевода в пониженную стенку
    protected bool CheckNotZakaz(Object Sender, bool bPerevod)
    {
        try
        {
            try
            {
                //если причина уже выбрана - не выполнять проверку           
                if (fldNotZakazCheckOk.Value != "")
                {
                    return true;
                }

                bool b_not_zakaz = false;

                //номенклатурный номер, номер заказа и строка заказа в которую принимается труба
                String pipe_number = lblYear.Text + lblPipeNo.Text.PadLeft(6, '0');
                String inv_number = Checking.CorrectInventoryNumber(txbInventoryNumber.Text);
                String order_line = txbZakazLine.Text.Trim();
                String order_header = txbZakazNo.Text.Trim();
                String campaign_line_id = "";
                if (cbShowCampaigns.Checked) campaign_line_id = ddlCampaign.SelectedItem.Value;

                //случай 1: в строке кампании явно указано, что приёмка вне заказа
                if (campaign_line_id != "")
                {
                    OleDbConnection conn = Master.Connect.ORACLE_TESC3();
                    OleDbCommand cmd = conn.CreateCommand();
                    cmd.CommandText = "select LINE_NOT_IN_ORDER from CAMPAIGNS where EDIT_STATE=0 and CAMPAIGN_LINE_ID=?";
                    cmd.Parameters.AddWithValue("CAMPAIGN_LINE_ID", campaign_line_id);
                    OleDbDataReader rdr = cmd.ExecuteReader();
                    if (rdr.Read())
                    {
                        if (rdr["LINE_NOT_IN_ORDER"].ToString() == "1")
                        {
                            b_not_zakaz = true;
                            txbNotZakazReasonDescription.Text = "Приёмка на строку кампании которая помечена как \"Вне заказа\"";
                        }
                    }
                    rdr.Close();
                    rdr.Dispose();
                    cmd.Dispose();
                }


                //случай 2: приёмка на строку кампании, фактический выпуск по которой превышает плановый на 5%            
                if (!b_not_zakaz && campaign_line_id != "")
                {
                    //получение планового выпуска по строке кампании
                    double plan_weight = 0;
                    OleDbConnection conn = Master.Connect.ORACLE_TESC3();
                    OleDbCommand cmd = conn.CreateCommand();
                    cmd.CommandText = "select NVL(STEEL_OUT,0) STEEL_OUT from CAMPAIGNS where EDIT_STATE=0 and CAMPAIGN_LINE_ID=?";
                    cmd.Parameters.AddWithValue("CAMPAIGN_LINE_ID", campaign_line_id);
                    OleDbDataReader rdr = cmd.ExecuteReader();
                    if (rdr.Read())
                        plan_weight = Convert.ToDouble(rdr["STEEL_OUT"].ToString());
                    rdr.Close();
                    rdr.Dispose();
                    cmd.Dispose();

                    //получение фактического выпуска по строке кампании
                    double fact_weight = 0;
                    cmd = conn.CreateCommand();
                    cmd.CommandText =
                    // PAV 2016-03-24
                    // @"select NVL(sum(round(round(ip.LENGTH/1000,2)*pi.CONVERSION_RATE,3)),0) FACT_WGHT 
                    @"select NVL( sum( tesc3.pipes.weight(pi.CONVERSION_RATE, null, null, ip.LENGTH) ), 0) FACT_WGHT 
                    from inspection_pipes ip
                    join oracle.V_T3_PIPE_ITEMS pi on IP.INVENTORY_CODE=PI.NOMER and pi.org_id=127 
                    where edit_state=0 and campaign_line_id=?";
                    cmd.Parameters.AddWithValue("CAMPAIGN_LINE_ID", campaign_line_id);
                    rdr = cmd.ExecuteReader();
                    if (rdr.Read())
                        fact_weight = Convert.ToDouble(rdr["FACT_WGHT"].ToString());
                    rdr.Close();
                    rdr.Dispose();
                    cmd.Dispose();

                    //приёмка вне заказа, если фактически принято более 5% сверх плана
                    /*if (plan_weight != 0 && fact_weight / plan_weight > 1.05)
                    {
                        b_not_zakaz = true;
                        txbNotZakazReasonDescription.Text = "Фактический выпуск (" + fact_weight.ToString() + "т) превышает план (" + plan_weight.ToString() + "т) по строке кампании "
                            + order_header + "/" + order_line + " " + inv_number + " (" + campaign_line_id + ")";
                    }*/
                }

                //случай 3: приёмка не на строку кампании, 
                //в таком случае сравнивается фактический и плановый выпуск по всем текущим кампаниям, у которых заказ-номенклатура совпадают с указанными.
                //Если факт. выпуск превышает плановый на 5%, или схожих компаний нет - то считается что приёмка вне заказа
                if (!b_not_zakaz && campaign_line_id == "")
                {
                    //получение планового выпуска по подходящим текущим компаниям
                    double plan_weight = 0;
                    OleDbConnection conn = Master.Connect.ORACLE_TESC3();
                    OleDbCommand cmd = conn.CreateCommand();
                    cmd.CommandText = @"select NVL(sum(STEEL_OUT),0) STEEL_OUT from campaigns
                    where edit_state=0 and inventory_code=? and order_header=? and order_line=?
                    and (sysdate between campaign_date-5/24 and plan_end_date+19/24)";
                    cmd.Parameters.AddWithValue("INVENTORY_CODE", inv_number);
                    cmd.Parameters.AddWithValue("ORDER_HEADER", order_header);
                    cmd.Parameters.AddWithValue("ORDER_LINE", order_line);
                    OleDbDataReader rdr = cmd.ExecuteReader();
                    if (rdr.Read())
                        plan_weight = Convert.ToDouble(rdr["STEEL_OUT"].ToString());
                    rdr.Close();
                    rdr.Dispose();
                    cmd.Dispose();

                    //если подходящих кампаний нет - приёмка вне заказа
                    if (plan_weight == 0)
                    {
                        b_not_zakaz = true;
                        txbNotZakazReasonDescription.Text = "Отсутствуют кампании подходящие под заказ " + order_header + "/" + order_line + " " + inv_number;
                    }
                    else
                    {
                        //получение фактического выпуска
                        double fact_weight = 0;
                        cmd = conn.CreateCommand();
                        cmd.CommandText =
                        // PAV 2016-03-24
                        // @"select NVL(sum(round(round(ip.LENGTH/1000,2)*pi.CONVERSION_RATE,3)),0) FACT_WGHT 
                        @"select NVL( sum( tesc3.pipes.weight(pi.CONVERSION_RATE, null, null, ip.LENGTH) ), 0) FACT_WGHT 
                        from inspection_pipes ip
                        join oracle.V_T3_PIPE_ITEMS pi on IP.INVENTORY_CODE=PI.NOMER and pi.org_id=127 
                        where edit_state=0 and inventory_code=? and order_header=? and order_line=?";
                        cmd.Parameters.AddWithValue("INVENTORY_CODE", inv_number);
                        cmd.Parameters.AddWithValue("ORDER_HEADER", order_header);
                        cmd.Parameters.AddWithValue("ORDER_LINE", order_line);
                        rdr = cmd.ExecuteReader();
                        if (rdr.Read())
                            fact_weight = Convert.ToDouble(rdr["FACT_WGHT"].ToString());
                        rdr.Close();
                        rdr.Dispose();
                        cmd.Dispose();

                        //приёмка вне заказа, если фактически принято более 5% сверх плана
                        /*if (fact_weight / plan_weight > 1.05)
                        {
                            b_not_zakaz = true;
                            txbNotZakazReasonDescription.Text = "Фактический выпуск (" + fact_weight.ToString() + "т) превышает план (" + plan_weight.ToString() + "т) по заказу "
                               + order_header + "/" + order_line + " " + inv_number;
                        }*/
                    }
                }

                if (!b_not_zakaz)
                {
                    return true;
                }

                //отображение диалогового окна
                fldNotZakazCallbackButton.Value = (Sender as Control).ID;
                ddlNotZakazReason_SelectedIndexChanged(null, EventArgs.Empty);
                PopupWindow1.Title = "Перевод трубы в другое назначение";
                PopupWindow1.ContentPanelId = pnlNotZakazReason.ID;
                PopupWindow1.MoveToCenter();
                pnlNotZakazReason.Visible = true;
                return false;
            }
            catch (Exception ex)
            {
                Master.AddErrorMessage("Ошибка проверки приёмки трубы вне заказа", ex);
                return false;
            }
        }
        finally
        {
            //
        }
    }


    //выбор причины перевода в пониженную сортность из выпадающего списка
    protected void ddlNotZakazReason_SelectedIndexChanged(object sender, EventArgs e)
    {
        try
        {
            txbNotZakazReasonDistance.Enabled = false;
            txbNotZakazReasonValue.Enabled = false;

            if (ddlNotZakazReason.SelectedItem.Value != "")
            {
                OleDbConnection conn = Master.Connect.ORACLE_TESC3();
                OleDbCommand cmd = conn.CreateCommand();
                cmd.CommandText = "select * from SPR_NOT_ZAKAZ_REASON where ID=?";
                cmd.Parameters.AddWithValue("ID", ddlNotZakazReason.SelectedItem.Value);
                OleDbDataReader rdr = cmd.ExecuteReader();
                if (rdr.Read())
                {
                    txbNotZakazReasonValue.Enabled = (rdr["FACT_VALUE_INPUT"].ToString() == "1");
                    txbNotZakazReasonDistance.Enabled = (rdr["DISTANCE_INPUT"].ToString() == "1");
                }
                rdr.Close();
                rdr.Dispose();
                cmd.Dispose();
            }

            if (!txbNotZakazReasonDistance.Enabled) txbNotZakazReasonDistance.Text = "";
            if (!txbNotZakazReasonValue.Enabled) txbNotZakazReasonValue.Text = "";
            txbNotZakazReasonDistance.BackColor = (txbNotZakazReasonDistance.Enabled) ? Color.White : Color.Silver;
            txbNotZakazReasonValue.BackColor = (txbNotZakazReasonValue.Enabled) ? Color.White : Color.Silver;
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка получения данных по причине перевода в другое назначение", ex);
        }
    }


    /// <summary>
    /// Отображение окна разрешения мастера
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void btnMasterConfirm_Click(object sender, EventArgs e)
    {
        PopupWindow1.ContentPanelId = pnlMasterConfirm.ID;
        PopupWindow1.Title = "Разрешение мастера";
        PopupWindow1.MoveToCenter();
        pnlMasterConfirm.Visible = true;

        txbMasterConfirmLogin.Text = "";
        txbMasterConfirmPassword.Text = "";
        Master.FocusControl = txbMasterConfirmLogin.ID;
    }


    /// <summary>
    /// Подтверждение разрешения мастера на приемку трубы
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void btnConfirmMasterOk_Click(object sender, EventArgs e)
    {
        try
        {
            if (txbMasterConfirmLogin.Text.Trim() == "" || txbMasterConfirmPassword.Text.Trim() == "")
            {
                Master.AlertMessage = "Необходимо ввести логин и пароль";
                return;
            }

            //получение логина из строки ввода
            //если введён табельный номер, то преобразование его в учетную запись
            String login = txbMasterConfirmLogin.Text.Trim();
            int tab_number = 0;
            if (Int32.TryParse(login, out tab_number))
                login = Authentification.GetLoginByTabNumber(tab_number);

            try
            {
                //проверка авторизации пользователя
                //если неверное имя пользователя или пароль, то сгенерируется исключение
                Authentification.CheckAuthentication(login, txbMasterConfirmPassword.Text);
            }
            catch (Exception ex)
            {
                Master.AlertMessage = "Неверное имя пользователя или пароль";
                Master.FocusControl = txbMasterConfirmLogin.ID;
                txbMasterConfirmLogin.Text = "";
                txbMasterConfirmPassword.Text = "";
            }

            //проверка наличия разрешения приемки труб в другой сортамент
            bool ignore_sortament_rights = false;
            UserInfo user_info = Authentification.GetUserInfoByLogin(login);
            foreach (AccessFlags access_flags in user_info.AccessRights)
            {
                if (access_flags.ShopSystemName == "ASPTPTESC3" && access_flags.RoleName == "INSP" && access_flags.Operation == "IGNORE_SORTAMENT")
                {
                    ignore_sortament_rights = true;
                    break;
                }
            }

            if (!ignore_sortament_rights)
            {
                Master.AlertMessage = "Пользователь " + login + " не имеет права выдачи разрешения на приемку труб в другой сортамент.";
                return;
            }

            //сохранение учетной записи мастера, выдавшего разрешение
            lblMasterConfirmFio.Text = "Разрешение мастера получено " + DateTime.Now.ToString("dd.MM.yyyy HH:mm") + " (" + Authentification.GetFIObyLogin(login) + ")";
            fldMasterConfirmLogin.Value = login;

            //закрытие окна
            pnlMasterConfirm.Visible = false;
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка проверки доступа", ex);
            Master.AlertMessage = ex.Message;
        }
    }


    /// <summary>
    /// Отмена разрешения мастера
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void btnConfirmMasterCancel_Click(object sender, EventArgs e)
    {
        pnlMasterConfirm.Visible = false;
    }


    /// <summary>
    /// Закрытие окна регистрации отклонения от маршрута (кнопка "Отмена")
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void btnRouteRejectCancel_Click(object sender, EventArgs e)
    {
        pnlShowInfoTemplating.Visible = false;
        pnlRouteRejection.Visible = false;
        txbPipeNumber.Text = "";
        txbCheck.Text = "";
    }


    /// <summary>
    /// Сохранение отклонения от маршрута
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void btnRouteRejectConfirm_Click(object sender, EventArgs e)
    {
        try
        {
            //проверка заполнения обязательных полей
            if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 53) && txbRouteRejectReason.Text.Trim() == "")
            {
                Master.AlertMessage = "Необходимо указать причину отклонения трубы от маршрута.";
                return;
            }

            //сохранение записи по отклонению от маршрута
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            using (OleDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT INTO TESC3.ROUTE_REJECTION (
                   REC_DATE, PIPE_NUMBER, WORKPLACE_ID, 
                   OPERATOR_ID, REJECTION_DESCRIPTION) 
                VALUES ( SYSDATE,
                 ? /* PIPE_NUMBER */,
                 ? /* WORKPLACE_ID */,
                 ? /* OPERATOR_ID */,
                 ? /* REJECTION_DESCRIPTION */ )";
                cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);
                cmd.Parameters.AddWithValue("WORKPLACE_ID", WorkplaceId);
                cmd.Parameters.AddWithValue("OPERATOR_ID", Authentification.User.UserName);
                cmd.Parameters.AddWithValue("REJECTION_DESCRIPTION", txbRouteRejectReason.Text.Trim());
                cmd.ExecuteNonQuery();
            }

            //печать бирки с дефектом "отклонение от маршрута"
            PrintRouteRejectionLabel(PipeNumber);

            //отправка телеграммы в ПЛК транспорта
            SendCommandToTransportPlc(2);

            //закрытие окна отклонения от маршрута
            btnRouteRejectCancel_Click(sender, e);
        }
        catch (Exception ex)
        {
            Master.AlertMessage = ex.Message;
            Master.AddErrorMessage("Ошибка сохранения данных по отклонению от маршрута", ex);
        }
    }


    /// <summary>
    /// Печать бирки на трубу с отклонением от маршрута
    /// </summary>
    private void PrintRouteRejectionLabel(int pipeNumber)
    {
        //не печатать бирку с ПК разработчиков
        if (Authentification.User.UserName == "DEV_LOGIN") return;

        //имя рабочего места
        String workPlaceName = this.WorkplaceName;

        //получение данных для печати бирки с дефектом "отклонение от маршрута"                        
        String coilSupplier = GetSteelSupplierName(pipeNumber);
        String diameter = "";
        String thickness = "";
        String steelMark = "";
        String classStal = "";
        String lotNumber = "";
        String pipeLength = "";
        String smelting = "";
        String gratdirection = "";
        String gratdopusk = "";
        String ordernumber = "";
        String Out_Inspection = "";
        OleDbConnection conn = Master.Connect.ORACLE_TESC3();
        using (OleDbCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"SELECT pi.diameter,
                                       pi.thickness,
                                       pi.stal,
                                       pi.class_stal,
                                       op.coil_pipepart_no,
                                       op.pipelength,
                                       gc.supplier,
                                       gc.smelting,
                                       (SELECT length_next
                                          FROM (SELECT uo.LENGTH AS length_next, uo.rec_date
                                                  FROM tesc3.usc_otdelka uo
                                                 WHERE uo.edit_state = 0 AND uo.LENGTH IS NOT NULL AND uo.pipe_number = ?
                                                UNION ALL
                                                SELECT ip.LENGTH, ip.trx_date
                                                  FROM tesc3.inspection_pipes ip
                                                 WHERE     ip.edit_state = 0
                                                       AND ip.next_direction IS NOT NULL
                                                       AND ip.pipe_number = ?
                                                ORDER BY rec_date DESC)
                                         WHERE ROWNUM = 1)
                                          length_next,
                                          CMP.GRAT_DIRECTION,
                                          CMP.GRAT_DOPUSK,
                                          CMP.ORDER_HEADER,
                                          SOI.BIRKA_DESCRIPTION
                                  FROM optimal_pipes op
                                       LEFT JOIN geometry_coils_sklad gc
                                          ON     op.coil_pipepart_year = gc.coil_pipepart_year
                                             AND op.coil_pipepart_no = gc.coil_pipepart_no
                                             AND op.coil_internalno = gc.coil_run_no
                                             AND gc.edit_state = 0
                                       LEFT JOIN campaigns cmp
                                          ON gc.campaign_line_id = cmp.campaign_line_id AND cmp.edit_state = 0
                                       LEFT JOIN ADMINTESC5.SPR_OUT_INSP SOI
                                          ON CP.INSPECTION=SOI.INSP_NAME  and CP.EDIT_STATE=0 and SOI.SHOP='ТЭСЦ-3' 
                                       LEFT JOIN oracle.v_t3_pipe_items pi
                                          ON cmp.inventory_code = pi.nomer
                                 WHERE op.pipe_number = ?";
            cmd.Parameters.AddWithValue("PIPE_NUMBER_1", pipeNumber);
            cmd.Parameters.AddWithValue("PIPE_NUMBER_2", pipeNumber);
            cmd.Parameters.AddWithValue("PIPE_NUMBER_3", pipeNumber);
            using (OleDbDataReader rdr = cmd.ExecuteReader())
            {
                if (rdr.Read())
                {
                    diameter = rdr["DIAMETER"].ToString();
                    thickness = rdr["THICKNESS"].ToString();
                    steelMark = rdr["STAL"].ToString();
                    classStal = rdr["CLASS_STAL"].ToString();
                    lotNumber = rdr["COIL_PIPEPART_NO"].ToString();
                    pipeLength = rdr["length_next"].ToString() != "" ? rdr["length_next"].ToString() : rdr["PIPELENGTH"].ToString();
                    if (pipeLength != "")
                        pipeLength = (Convert.ToDouble(pipeLength) / 1000.0).ToString("F2");
                    smelting = rdr["SMELTING"].ToString();
                    gratdirection = rdr["GRAT_DIRECTION"].ToString();
                    gratdopusk = rdr["GRAT_DOPUSK"].ToString();
                    ordernumber = rdr["ORDER_HEADER"].ToString();
                    Out_Inspection = rdr["BIRKA_DESCRIPTION"].ToString();
                }
            }
        }

        //id принтера
        int printerId = Convert.ToInt32(ddlPrinter.SelectedItem.Value);

        //печать бирки возврата на ремонт
        int duplicateReason = -1;
        bool bPrintOk = Printing.PrintLabel_FinalInspection(1, duplicateReason, pipeNumber, printerId, workPlaceName, Authentification.User.OtkCode.ToString(),
            diameter, thickness, steelMark, classStal,
            "", "", "Отклонение от маршрута", lotNumber, pipeLength, "", coilSupplier, smelting, gratdopusk, "REMONT", "", gratdirection, ordernumber, Out_Inspection, "");

        if (!bPrintOk)
        {
            duplicateReason = 1;
            bPrintOk = Printing.PrintLabel_FinalInspection(1, duplicateReason, pipeNumber, printerId, workPlaceName, Authentification.User.OtkCode.ToString(),
                diameter, thickness, steelMark, classStal,
                "", "", "Отклонение от маршрута", lotNumber, pipeLength, "", coilSupplier, smelting, gratdopusk, "REMONT", "", gratdirection, ordernumber, Out_Inspection, "");

            if (!bPrintOk)
                throw new Exception("Ошибка печати бирки для трубы с отклонением от маршрута " + pipeNumber.ToString());
        }
    }


    #region Работа со списком труб быстрого выбора


    /// <summary>
    /// Коллекция отмеченных флажками номеров труб в списке быстрого выбора
    /// </summary>
    private List<int> CheckedPipeNumbers
    {
        get
        {
            if (ViewState["CheckedPipeNumbers"] == null)
                ViewState["CheckedPipeNumbers"] = new List<int>();
            return (ViewState["CheckedPipeNumbers"] as List<int>);
        }
    }


    /// <summary>
    /// Формирование таблицы списка труб для быстрого выбора
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void tblPipesQueue_PreRender(object sender, EventArgs e)
    {
        try
        {
            //выборка последних необработанных труб за 7 дней
            DataTable tbl;
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            using (OleDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"select PIPE_NUMBER, REC_DATE from
                    (
                        select PIPE_NUMBER, max(REC_DATE) REC_DATE 
                        from PLC_L3_PIPE_NUMBER_MAIR
                        where PROCESSING_STATUS=1
                            and REC_DATE>SYSDATE-7
                            and PIPE_NUMBER BETWEEN 10000000 and (EXTRACT(YEAR FROM sysdate) - 2000 || 999999)
                        group by PIPE_NUMBER
                        order by 2 desc
                    ) where ROWNUM<=15
                    order by REC_DATE DESC";
                using (OleDbDataAdapter ad = new OleDbDataAdapter(cmd))
                {
                    tbl = new DataTable();
                    ad.Fill(tbl);
                }
            }

            foreach (DataRow dataRow in tbl.Rows)
            {
                TableRow tableRow = new TableRow();
                tblPipesQueue.Rows.Add(tableRow);
                tblPipesQueue.Rows.Add(tableRow);
                tableRow.Cells.Add(new TableCell());
                tableRow.Cells.Add(new TableCell());

                int pipeNumber = 0;
                Int32.TryParse(dataRow["PIPE_NUMBER"].ToString(), out pipeNumber);

                if (CheckedPipeNumbers.Contains(pipeNumber))
                    tableRow.Cells[0].Text = "<img src='Images/Checkbox16x16_on.png'/>";
                else
                    tableRow.Cells[0].Text = "<img src='Images/Checkbox16x16_off.png'/>";

                tableRow.Cells[0].HorizontalAlign = HorizontalAlign.Center;

                tableRow.Cells[1].Text = pipeNumber.ToString();
                tableRow.Cells[1].Font.Size = 16;
                tableRow.Cells[1].Style["cursor"] = "pointer";

                //скрипт для генерации события отметки трубы в списке
                tableRow.Cells[0].Attributes["onclick"] = "__doPostBack('CHECK_PIPE_IN_QUEUE', " + pipeNumber.ToString() + ")";

                //скрипт для генерации события выбора трубы из списка
                tableRow.Cells[1].Attributes["onclick"] = "__doPostBack('SELECT_PIPE_IN_QUEUE', " + pipeNumber.ToString() + ")";

                //скрипт подсветки строки при наведении мыши
                tableRow.Cells[1].Attributes["onmouseover"] = "this.style.backgroundColor='#E6E6F0'";
                tableRow.Cells[1].Attributes["onmouseout"] = "this.style.backgroundColor=''";
            }
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка построения списка номеров труб быстрого выбора", ex);
        }
    }


    /// <summary>
    /// Событие отметки номера трубы в списке быстрого выбора
    /// </summary>
    /// <param name="strPipeNumber"></param>
    protected void CheckPipeInQueue(String strPipeNumber)
    {
        int pipeNumber = Convert.ToInt32(strPipeNumber);

        if (CheckedPipeNumbers.Contains(pipeNumber))
            CheckedPipeNumbers.Remove(pipeNumber);
        else
            CheckedPipeNumbers.Add(pipeNumber);
    }


    /// <summary>
    /// Событие выбора трубы из списка быстрого выбора
    /// </summary>
    /// <param name="strPipeNumber"></param>
    protected void SelectPipeInQueue(String strPipeNumber)
    {
        PipeNumber = Convert.ToInt32(strPipeNumber);

        //расчет контрольной цифры
        txbCheck.Text = Checking.Check_Class(PipeNumber.ToString()).ToString();

        //событие нажатия кнопки "ввести данные"
        btnOk_Click(btnOk, EventArgs.Empty);
    }


    /// <summary>
    /// Отметка отмеченных номеров труб в списке как обработанных
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void btnClearPipesQueue_Click(object sender, EventArgs e)
    {
        try
        {
            foreach (int pipeNumber in CheckedPipeNumbers)
            {
                SetMairQueueProcessingStatus(pipeNumber);
            }

            CheckedPipeNumbers.Clear();
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка очистки списка номеров труб", ex);
        }
    }


    /// <summary>
    /// Установка статуса обработки трубы из очереди с установки MAIR на АРМ инспекционных решеток
    /// </summary>
    /// <param name="pipeNumber"></param>
    private void SetMairQueueProcessingStatus(int pipeNumber)
    {
        OleDbConnection conn = Master.Connect.ORACLE_TESC3();
        using (OleDbCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = "update PLC_L3_PIPE_NUMBER_MAIR set PROCESSING_STATUS=2 where PIPE_NUMBER=? and PROCESSING_STATUS<>2";
            cmd.Parameters.AddWithValue("PIPE_NUMBER", pipeNumber);
            cmd.ExecuteNonQuery();
        }
    }

    #endregion

    #region Реализация отображения информации о дефекте и фото дефекта

    /// <summary>
    /// Отображение описания и причин возникновения выбранного дефекта
    /// </summary>    
    protected void DisplayDefectInfo()
    {
        try
        {
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            //объекты с дефектами
            List<Panel> pnl_defects = new List<Panel>() { pnlDefect, pnlDefect2, pnlDefect3 }; //панели
            List<Panel> pnl_defect_description = new List<Panel>() { pnlDefectDescription, pnlDefectDescription2, pnlDefectDescription3 };
            List<Label> lbl_defect_description = new List<Label>() { lblDefectDescription, lblDefectDescription2, lblDefectDescription3 };
            List<Label> lbl_defect_reason = new List<Label>() { lblDefectReason, lblDefectReason2, lblDefectReason3 };
            List<HyperLink> btn_defect_image = new List<HyperLink>() { btnDefectImage, btnDefectImage2, btnDefectImage3 };
            List<DropDownList> ddl_defects = new List<DropDownList>() { ddlDefect, ddlDefect2, ddlDefect3 }; //выпадающие списки дефектов

            for (int i = 0; i < 3; i++)
            {
                if (pnl_defects[i].Visible)
                {

                    pnl_defect_description[i].Visible = false;
                    lbl_defect_description[i].Text = "";
                    lbl_defect_reason[i].Text = "";
                    btn_defect_image[i].Visible = false;

                    String defectId = ddl_defects[i].SelectedItem.Value.Trim();

                    if (defectId != "")
                    {
                        using (OleDbCommand cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = @"select 
                            DEFECT_DESCRIPTION, 
                            DEFECT_REASON,
                            (case when DEFECT_IMAGE is not null then 1 else 0 end) IMAGE_EXISTS
                            from SPR_DEFECT 
                            where ID=?";
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("ID", defectId);
                            using (OleDbDataReader rdr = cmd.ExecuteReader())
                            {
                                if (rdr.HasRows)
                                {
                                    if (rdr.Read())
                                    {
                                        lbl_defect_description[i].Text = rdr["DEFECT_DESCRIPTION"].ToString();
                                        lbl_defect_reason[i].Text = rdr["DEFECT_REASON"].ToString();
                                        pnl_defect_description[i].Visible = (lbl_defect_description[i].Text != "" || lbl_defect_reason[i].Text != "");

                                        if (rdr["IMAGE_EXISTS"].ToString() == "1")
                                        {
                                            btn_defect_image[i].Visible = true;
                                            btn_defect_image[i].NavigateUrl = "Inspection.aspx?show_defect_image_id=" + defectId;
                                        }
                                    }
                                    rdr.Close();
                                }
                            }
                        }
                    }
                }
            }

            //очистка объектов
            pnl_defects = null;
            pnl_defect_description = null;
            lbl_defect_description = null;
            lbl_defect_reason = null;
            btn_defect_image = null;
            ddl_defects = null;
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка получения информации по дефекту", ex);
        }
    }


    /// <summary>
    /// Получение изображения дефекта из прикрепленного документа
    /// </summary>
    /// <param name="defectId"></param>
    protected void ShowDefectImage(String defectId)
    {
        //подключение к БД через провайдер Oracle
        OleDbConnection conn = Master.Connect.ORACLE_TESC3();
        String connstr = conn.ConnectionString.Replace("Provider=MSDAORA.1;", "");
        OracleConnection oraconn = new OracleConnection(connstr);
        oraconn.Open();

        try
        {
            using (OracleCommand cmd = oraconn.CreateCommand())
            {
                cmd.CommandText = "select DEFECT_IMAGE from SPR_DEFECT where ID=:ID";
                cmd.Parameters.AddWithValue("ID", defectId);

                using (OracleDataReader rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        if (rdr["DEFECT_IMAGE"] != DBNull.Value)
                        {
                            Byte[] blob_data = (Byte[])rdr["DEFECT_IMAGE"];
                            if (blob_data != null && blob_data.Length > 0)
                            {
                                ViewBlobData(blob_data);
                            }
                            else
                                throw new Exception("Для дефекта ID=" + defectId + " отсутствует фото.");
                        }
                        else
                            throw new Exception("Для дефекта ID=" + defectId + " отсутствует фото.");
                    }
                    else
                        throw new Exception("Для дефекта ID=" + defectId + " отсутствует фото.");
                }
            }
        }
        finally
        {
            oraconn.Close();
            oraconn.Dispose();
        }
    }


    /// <summary>
    /// Получение документа из бинарных данных BLOB-поля справочника и отправка его в http-ответ
    /// Данные blob_data должны содержать имя документа и content-type в первых 256 байтах, и полезную информацию документа в остальном объеме
    /// </summary>
    /// <param name="blob_data">Данные документа прочитанные из БД</param>
    private void ViewBlobData(byte[] blob_data)
    {
        try
        {
            if (blob_data == null || blob_data.Length == 0) return;

            //получение данных из blob-поля
            String document_name = "";
            String content_type = "";
            byte[] data;
            if (blob_data.Length > 256)
            {
                String[] blob_header = Encoding.GetEncoding(1251).GetString(blob_data, 0, 256).Split(new char[] { '\t' });
                document_name = blob_header[0].Trim();
                content_type = blob_header[1].Trim();
                data = new Byte[blob_data.Length - 256];
                Array.Copy(blob_data, 256, data, 0, data.Length);
            }
            else
                throw new Exception("Неверная структура данных в BLOB-поле документа");

            //транслитерация имени файла            
            const String find = "abcdefghijklmnopqrstuvwxyz 12345678 ;.-/! абвгдежзийклмнопрстуфхцчшщьъыэюя";
            const String repl = "abcdefghijklmnopqrstuvwxyz 12345678 ;.-/! abvgdejziyklmnoprstufhcchh  yeuj";
            document_name = document_name.ToLower();
            for (int i = 0; i < document_name.Length; i++)
            {
                int p = find.IndexOf(document_name[i]);
                if (p == -1)
                {
                    document_name = document_name.Replace(document_name[i], ' ');
                    continue;
                }
                document_name = document_name.Replace(find[p], repl[p]);
            }
            document_name = document_name.Replace(' ', '_').Trim().ToLower();

            //отправка данных в HTTP-поток
            HttpContext.Current.Response.Clear();
            HttpContext.Current.Response.ContentType = content_type;
            HttpContext.Current.Response.AddHeader("content-length", data.Length.ToString());
            HttpContext.Current.Response.AddHeader("content-disposition", "attachment; filename=" + document_name);
            HttpContext.Current.Response.BinaryWrite(data);
            HttpContext.Current.Response.Flush();
            HttpContext.Current.Response.End();
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка получения документа", ex);
        }
    }

    #endregion

    #region Реализация функционала получения транспортировочных номеров

    /// <summary>
    /// Получение нового транспортного номера трубы
    /// </summary>
    protected int GetNextTransportNumber()
    {
        //диапазон транспортных номеров
        const int TRANSPORT_NUMBER_START = 800001;
        const int TRANSPORT_NUMBER_END = 999999;

        int year = (int)(DateTime.Now.Year % 100);

        OleDbConnection conn = Master.Connect.ORACLE_TESC3();
        using (OleDbCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"select max(PIPE_NUMBER) from TRANSPORT_NUMBERS where EDIT_STATE=0 and PIPE_NUMBER between ? and ?";
            cmd.Parameters.AddWithValue("START_NUMBER", (int)(year * 1E6) + TRANSPORT_NUMBER_START);
            cmd.Parameters.AddWithValue("END_NUMBER", (int)(year * 1E6) + TRANSPORT_NUMBER_END);
            object o = cmd.ExecuteScalar();

            if (o != DBNull.Value)
            {
                int pipeNumber = Convert.ToInt32(o);
                if (pipeNumber == (int)(year * 1E6) + TRANSPORT_NUMBER_END)
                    throw new Exception("Новый транспортный номер трубы не может быть получен, т.к. все номера из допустимого диапазона уже были получены.");

                return pipeNumber + 1;
            }
            else
            {
                return (int)(year * 1E6) + TRANSPORT_NUMBER_START;
            }
        }
    }


    /// <summary>
    /// Отображение формы ввода данных по трубе без номера
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void btnTransportNumber_Click(object sender, EventArgs e)
    {
        try
        {
            OpenTransportNumberForm(-1);
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка получения/открытия на редактирование транспортировочного номера трубы", ex);
        }
    }


    /// <summary>
    /// Открытие на редактирование или создание транспортного номера трубы
    /// Если transportPipeNumber=-1, то создается новый транспортный номер, если не -1 - существующая запись открывается на редактирование
    /// </summary>
    /// <param name="transportNumberRowId"></param>
    protected void OpenTransportNumberForm(int transportPipeNumber)
    {
        if (transportPipeNumber == -1)
        {
            //получение нового транспортного номера
            int pipeNumber = GetNextTransportNumber();
            int year = (int)(pipeNumber / 1E6);
            int num = (int)(pipeNumber % 1E6);
            lblTransportPipeNumber.Text = year.ToString("D2") + "-" + num.ToString("D6");

            txbTransportNumberLength.Text = "";
            txbTransportNumberNote.Text = "";
            txbTransportNumberReason.Text = "";
            ddlTransportNumberDiameter.SelectedIndex = 0;
            ddlTransportNumberProfileSize.SelectedIndex = 0;
            ddlTransportNumberSteelmark.SelectedIndex = 0;
            ddlTransportNumberThickness.SelectedIndex = 0;
            ddlTransportNumberWorkplace.SelectedIndex = 0;

            SelectedTransportPipeNumber = -1;
        }
        else
        {
            //открытие существующего транспортного номера на редактирование

            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            using (OleDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "select * from TRANSPORT_NUMBERS where EDIT_STATE=0 and PIPE_NUMBER=?";
                cmd.Parameters.AddWithValue("ROW_ID", transportPipeNumber);

                using (OleDbDataReader rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        int year = (int)(transportPipeNumber / 1E6);
                        int num = (int)(transportPipeNumber % 1E6);
                        lblTransportPipeNumber.Text = year.ToString("D2") + "-" + num.ToString("D6");

                        txbTransportNumberLength.Text = rdr["PIPE_LENGTH"].ToString();
                        txbTransportNumberNote.Text = rdr["ADDITIONAL_NOTE"].ToString();
                        txbTransportNumberReason.Text = rdr["TRANSPORT_NUMBER_REASON"].ToString();
                        SelectDDLItemByValue(ddlTransportNumberDiameter, rdr["DIAMETER"].ToString());
                        SelectDDLItemByValue(ddlTransportNumberProfileSize, rdr["PROFILE_SIZE_A"].ToString() != "" ? rdr["PROFILE_SIZE_A"].ToString() + "x"
                                                                                                                                                      + rdr["PROFILE_SIZE_B"].ToString() : "");
                        SelectDDLItemByValue(ddlTransportNumberSteelmark, rdr["STEELMARK"].ToString());
                        SelectDDLItemByValue(ddlTransportNumberThickness, rdr["THICKNESS"].ToString());
                        SelectDDLItemByValue(ddlTransportNumberWorkplace, rdr["LOSS_NUMBER_WORKPLACE_ID"].ToString());

                        SelectedTransportPipeNumber = transportPipeNumber;
                    }
                    else
                    {
                        throw new Exception("Невозможно открыть запись на редактирование, т.к. данная запись была изменена или удалена другим пользователем.");
                    }
                }
            }
        }

        //отображение диалогового окна
        PopupWindow1.Title = "Получение транспортировочного номера трубы";
        PopupWindow1.ContentPanelId = pnlTransportNumber.ID;
        PopupWindow1.MoveToCenter();
        pnlTransportNumber.Visible = true;
        btnTransportNumberTab_Click(null, EventArgs.Empty); //переход на вкладку получения номера
    }


    /// <summary>
    /// Текущий редактируемый транспортный номер трубы
    /// Если запись по новому транспортному номеру не редактируется или создается новая запись, то -1
    /// </summary>
    protected int SelectedTransportPipeNumber
    {
        get
        {
            if (ViewState["SelectedTransportPipeNumber"] == null)
                return -1;
            else
                return Convert.ToInt32(ViewState["SelectedTransportPipeNumber"]);
        }
        set
        {
            ViewState["SelectedTransportPipeNumber"] = value;
        }
    }


    /// <summary>
    /// Переключение на вкладку "Получение транспортного номера"
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void btnTransportNumberTab_Click(object sender, EventArgs e)
    {
        ActivateTab(tdTransportNumberTab, btnTransportNumberTab);
        DeactivateTab(tdTransportNumberEditTab, btnTransportNumberEditTab);
        mvTransportNumber.SetActiveView(vTransportNumberEdit);
    }


    /// <summary>
    /// Переключение на вкладку исправления данных по транспортному номеру
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void btnTransportNumberEditTab_Click(object sender, EventArgs e)
    {
        DeactivateTab(tdTransportNumberTab, btnTransportNumberTab);
        ActivateTab(tdTransportNumberEditTab, btnTransportNumberEditTab);
        mvTransportNumber.SetActiveView(vTransportNumberFind);
    }


    /// <summary>
    /// Закрытие окна получения транспортного номера без сохранения изменений
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void btnTransportNumberCancel_Click(object sender, EventArgs e)
    {
        pnlTransportNumber.Visible = false;
    }


    /// <summary>
    /// Сохранение данных по транспортному номеру и печать бирки
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void btnTransportNumberRepair_Click(object sender, EventArgs e)
    {
        try
        {
            //проверка обязательных полей
            String msg = "";
            if (!Validation.ValidateNumber(txbTransportNumberLength.Text, "Фактическая длина, м", out msg, true, 2, 7.00, 17.00))
            {
                Master.AlertMessage = msg;
                return;
            }
            if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 54) && ddlTransportNumberWorkplace.SelectedItem.Value == "")
            {
                Master.AlertMessage = "Необходимо указать рабочее место потери номера";
                return;
            }
            if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 55) && ddlTransportNumberDiameter.SelectedItem.Value == ""
                                                                                    && ddlTransportNumberProfileSize.SelectedItem.Value == "")
            {
                Master.AlertMessage = "Необходимо указать номинальный диаметр или номинальный типоразмер профиля трубы";
                return;
            }
            if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 56) && ddlTransportNumberThickness.SelectedItem.Value == "")
            {
                Master.AlertMessage = "Необходимо указать номинальную толщину стенки";
                return;
            }
            if (Restrictions.IsRestrictionActive(Master.Connect.ORACLE_TESC3(), 57) && txbTransportNumberReason.Text.Trim() == "")
            {
                Master.AlertMessage = "Необходимо ввести причину получения транспортировочного номера";
                return;
            }

            //получение текущего редактируемого транспортного номера
            //если запись по транспортному номеру не редактируется а создается новая, то получение нового уникального номера
            int pipeNumber = SelectedTransportPipeNumber;
            if (pipeNumber == -1)
                pipeNumber = GetNextTransportNumber();

            //сохранение данных по получению транспортного номера
            SaveTransportNumber(pipeNumber);
            SelectedTransportPipeNumber = pipeNumber;

            //печать бирки на трубу с транспортным номером
            if (WorkplaceId != 0)
                PrintTransportNumberLabel(pipeNumber);

            //отправка номера трубы в ПЛК транспорта линии отделки
            if (WorkplaceId > 0 && WorkplaceId <= 6 && SelectedTransportPipeNumber == -1)
                SendTransportNumberToPlc(pipeNumber);

            //закрытие формы редактирования данных по получению транспортного номера
            btnTransportNumberCancel_Click(sender, e);
        }
        catch (Exception ex)
        {
            Master.AlertMessage = "Ошибка сохранения данных: " + ex.Message;
            Master.AddErrorMessage("Ошибка сохранения данных по получению транспортировочного номера", ex);
        }
    }


    /// <summary>
    /// Печать бирки на трубу с транспортным номером
    /// </summary>
    /// <param name="transportPipeNumber">Транспортировочный номер трубы</param>
    private void PrintTransportNumberLabel(int transportPipeNumber)
    {
        //не печатать бирку с ПК разработчиков
        if (Authentification.User.UserName == "DEV_LOGIN") return;

        OleDbConnection conn = Master.Connect.ORACLE_TESC3();
        using (OleDbCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT tp.*, sk.NCONTR, cp.GRAT_DIRECTION,  cp.GRAT_DOPUSK, cp.ORDER_HEADER, SOI.BIRKA_DESCRIPTION " +
        "                    FROM TRANSPORT_NUMBERS tp      " +
        "                    left join INSPECTION_PIPES ip on IP.PIPE_NUMBER=tp.PIPE_NUMBER and IP.EDIT_STATE=0 " +
        "                    left join TESC3.CAMPAIGNS cp on ip.CAMPAIGN_LINE_ID=CP.CAMPAIGN_LINE_ID and CP.EDIT_STATE=0 " +
        "                    LEFT JOIN ADMINTESC5.SPR_OUT_INSP SOI " +
        "                      ON CP.INSPECTION = SOI.INSP_NAME  and CP.EDIT_STATE = 0 and SOI.SHOP='ТЭСЦ-3'  " +
        "                    LEFT JOIN SPR_KADRY sk " +
        "                       ON tp.OPERATOR_ID = sk.USERNAME " +
        "                    WHERE  tp.PIPE_NUMBER =? " +
        "                         AND tp.EDIT_STATE = 0 " +
        "                    ORDER BY tp.REC_DATE desc ";

            cmd.Parameters.AddWithValue("PIPE_NUMBER", transportPipeNumber);

            using (OleDbDataReader rdr = cmd.ExecuteReader())
            {
                if (rdr.Read())
                {
                    //получение данных для бирки  
                    String recDate = Convert.ToDateTime(rdr["REC_DATE"]).ToString("dd.MM.yy");
                    String diameter = rdr["DIAMETER"].ToString();
                    String thickness = rdr["THICKNESS"].ToString();
                    String steelMark = rdr["STEELMARK"].ToString();
                    String pipeLength = rdr["PIPE_LENGTH"].ToString();
                    if (pipeLength != "")
                        pipeLength = Convert.ToDouble(pipeLength).ToString("F2");
                    String otkNumber = rdr["NCONTR"].ToString();
                    String reason = rdr["TRANSPORT_NUMBER_REASON"].ToString();
                    String gratdirection = rdr["GRAT_DIRECTION"].ToString();
                    String gratdopusk = rdr["GRAT_DOPUSK"].ToString();
                    String ordernumber = rdr["ORDER_HEADER"].ToString();
                    String Out_Inspection = rdr["BIRKA_DESCRIPTION"].ToString();

                    //ID принтера
                    int printerId = Convert.ToInt32(ddlPrinter.SelectedItem.Value);

                    //печать бирки
                    bool bOk = Printing.PrintLabel_FinalInspection(1, 0, transportPipeNumber, printerId, this.WorkplaceName, otkNumber,
                        diameter, thickness, steelMark, "",
                        "", "", reason, "", pipeLength, "", "", "", gratdopusk, "REMONT", "", gratdirection, ordernumber, Out_Inspection, "");

                    //если печать бирки невозможна из-за дубликата, то печать с кодом причины дубликата "1"
                    if (!bOk)
                        Printing.PrintLabel_FinalInspection(1, 1, transportPipeNumber, printerId, this.WorkplaceName, otkNumber,
                        diameter, thickness, steelMark, "",
                        "", "", reason, "", pipeLength, "", "", "", gratdopusk, "REMONT", "", gratdirection, ordernumber, Out_Inspection, "");
                }
                else
                {
                    throw new Exception("Отсутствуют данные по транспортировочному номеру трубы " + transportPipeNumber.ToString() + " для печати бирки.");
                }
            }
        }
    }


    /// <summary>
    /// Сохранение данных по получению транспортировочного номера
    /// </summary>
    /// <param name="transportPipeNumber">Транспортировочный номер трубы</param>
    private void SaveTransportNumber(int transportPipeNumber)
    {
        //получение row_id существующей записи с указанным транспортным номером        
        String oldRowId = "";
        OleDbConnection conn = Master.Connect.ORACLE_TESC3();
        using (OleDbCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"select max(ROW_ID) from TRANSPORT_NUMBERS where EDIT_STATE=0 and PIPE_NUMBER=?";
            cmd.Parameters.AddWithValue("PIPE_NUMBER", transportPipeNumber);
            oldRowId = cmd.ExecuteScalar().ToString();
        }

        //row_id новой создаваемой записи
        DateTime dat = DateTime.Now;
        String newRowId = transportPipeNumber.ToString() + "_" + dat.ToString("dd.MM.yyyy HH:mm") + "." + dat.Millisecond.ToString();

        OleDbTransaction trans = conn.BeginTransaction();

        try
        {
            using (OleDbCommand cmd = conn.CreateCommand())
            {
                cmd.Transaction = trans;

                //пометка исправленной записи как неактуальной
                if (oldRowId != "")
                {
                    cmd.CommandText = "update TRANSPORT_NUMBERS set EDIT_STATE=1 where ROW_ID=?";
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("ROW_ID", oldRowId);
                    cmd.ExecuteNonQuery();
                }

                //сохранение измененной записи
                cmd.CommandText = @"INSERT INTO TRANSPORT_NUMBERS (
                    REC_DATE, OPERATOR_ID, EDIT_STATE, 
                    ROW_ID, ORIGINAL_ROW_ID, PIPE_NUMBER, 
                    DIAMETER, THICKNESS, STEELMARK, 
                    PIPE_LENGTH, TRANSPORT_NUMBER_REASON, ADDITIONAL_NOTE, 
                    WORKPLACE_ID, LOSS_NUMBER_WORKPLACE_ID, PROFILE_SIZE_A, PROFILE_SIZE_B) 
                VALUES ( SYSDATE /* REC_DATE */,
                    ? /* OPERATOR_ID */,
                    0 /* EDIT_STATE */,
                    ? /* ROW_ID */,
                    ? /* ORIGINAL_ROW_ID */,
                    ? /* PIPE_NUMBER */,
                    ? /* DIAMETER */,
                    ? /* THICKNESS */,
                    ? /* STEELMARK */,
                    ? /* PIPE_LENGTH */,
                    ? /* TRANSPORT_NUMBER_REASON */,
                    ? /* ADDITIONAL_NOTE */,
                    ? /* WORKPLACE_ID */,
                    ? /* LOSS_NUMBER_WORKPLACE_ID */,
                    ? /* PROFILE_SIZE_A */,
                    ? /* PROFILE_SIZE_B */)";

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("OPERATOR_ID", Authentification.User.UserName);
                cmd.Parameters.AddWithValue("ROW_ID", newRowId);
                cmd.Parameters.AddWithValue("ORIGINAL_ROW_ID", oldRowId);
                cmd.Parameters.AddWithValue("PIPE_NUMBER", transportPipeNumber);
                cmd.Parameters.AddWithValue("DIAMETER", Checking.GetDbType(ddlTransportNumberDiameter.SelectedItem.Value));
                cmd.Parameters.AddWithValue("THICKNESS", Checking.GetDbType(ddlTransportNumberThickness.SelectedItem.Value));
                cmd.Parameters.AddWithValue("STEELMARK", ddlTransportNumberSteelmark.SelectedItem.Value);
                cmd.Parameters.AddWithValue("PIPE_LENGTH", Checking.GetDbType(txbTransportNumberLength.Text));
                cmd.Parameters.AddWithValue("TRANSPORT_NUMBER_REASON", txbTransportNumberReason.Text.Trim());
                cmd.Parameters.AddWithValue("ADDITIONAL_NOTE", txbTransportNumberNote.Text.Trim());
                cmd.Parameters.AddWithValue("WORKPLACE_ID", WorkplaceId);
                cmd.Parameters.AddWithValue("LOSS_NUMBER_WORKPLACE_ID", ddlTransportNumberWorkplace.SelectedItem.Value);
                cmd.Parameters.AddWithValue("PROFILE_SIZE_A", ddlTransportNumberProfileSize.SelectedItem.Value != "" ? ddlTransportNumberProfileSize.SelectedItem.Value.Split('x')[0] : "");
                cmd.Parameters.AddWithValue("PROFILE_SIZE_B", ddlTransportNumberProfileSize.SelectedItem.Value != "" ? ddlTransportNumberProfileSize.SelectedItem.Value.Split('x')[1] : "");

                cmd.ExecuteNonQuery();

                trans.Commit();
            }
        }
        catch
        {
            trans.Rollback();
            throw;
        }
    }


    /// <summary>
    /// Отправка данных по полученному транспортному номеру в ПЛК транспорта
    /// </summary>    
    /// <param name="transportPipeNumber">Транспортировочный номер трубы</param>
    protected void SendTransportNumberToPlc(int transportPipeNumber)
    {
        //не отправлять данные при входе с ПК разработчиков
        if (Authentification.User.UserName == "DEV_LOGIN") return;

        //направление трубы (по выбранному маршруту СГП)
        int pipeDirectionCode = 2;

        try
        {
            //код команды для ПЛК = номер рабочего места
            short cmdCode = (short)WorkplaceId;

            //отправка в ПЛК транспорта линии 1
            if (cmdCode > 0 && cmdCode <= 3)
                PipeTrackingPLC.SendCommand(PipeTrackingPLC.PLC.NloTransport, cmdCode, transportPipeNumber, pipeDirectionCode, true);

            //отправка в ПЛК транспорта линии 1
            if (cmdCode > 3 && cmdCode <= 6)
                PipeTrackingPLC.SendCommand(PipeTrackingPLC.PLC.Line2Transport, cmdCode, transportPipeNumber, pipeDirectionCode, true);
        }
        catch
        {
            if (!IGNORE_UDP_ERRORS)
            {
                throw;
            }
        }
    }


    /// <summary>
    /// Обновление данных в списке журнала транспортных номеров
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void btnTransportNumberFind_Click(object sender, EventArgs e)
    {
        //
    }


    /// <summary>
    /// Повторная печать бирки на транспортировочный номер
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void btnTransportNumberPrintlabel_Click(object sender, EventArgs e)
    {
        if (fldSelectedTransportNumber.Value == "")
        {
            Master.AlertMessage = "Необходимо выбрать номер трубы для печати бирки.";
            return;
        }

        try
        {
            int pipeNumber = Convert.ToInt32(fldSelectedTransportNumber.Value);
            PrintTransportNumberLabel(pipeNumber);
        }
        catch (Exception ex)
        {
            Master.AlertMessage = "Ошибка печати бирки: " + ex.Message;
            Master.AddErrorMessage("Ошибка печати бирки", ex);
        }
    }


    /// <summary>
    /// Открытие транспортного номера на редактирование
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void btnTransportNumberEdit_Click(object sender, EventArgs e)
    {
        if (fldSelectedTransportNumber.Value == "")
        {
            Master.AlertMessage = "Необходимо выбрать номер трубы для печати бирки.";
            return;
        }

        try
        {
            int pipeNumber = Convert.ToInt32(fldSelectedTransportNumber.Value);
            OpenTransportNumberForm(pipeNumber);
        }
        catch (Exception ex)
        {
            Master.AlertMessage = "Ошибка открытия записи на редактирование: " + ex.Message;
            Master.AddErrorMessage("Ошибка открытия записи на редактирование", ex);
        }
    }


    /// <summary>
    /// Удаление записи по получению транспортировочного номера
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void btnTransportNumberDelete_Click(object sender, EventArgs e)
    {
        if (fldSelectedTransportNumber.Value == "")
        {
            Master.AlertMessage = "Необходимо выбрать номер трубы для печати бирки.";
            return;
        }

        try
        {
            int transportPipeNumber = Convert.ToInt32(fldSelectedTransportNumber.Value);

            //получение row_id существующей записи с указанным транспортным номером        
            String oldRowId = "";
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            using (OleDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"select max(ROW_ID) from TRANSPORT_NUMBERS where EDIT_STATE=0 and PIPE_NUMBER=?";
                cmd.Parameters.AddWithValue("PIPE_NUMBER", transportPipeNumber);
                oldRowId = cmd.ExecuteScalar().ToString();
            }

            //row_id новой создаваемой записи
            DateTime dat = DateTime.Now;
            String newRowId = transportPipeNumber.ToString() + "_" + dat.ToString("dd.MM.yyyy HH:mm") + "." + dat.Millisecond.ToString();

            OleDbTransaction trans = conn.BeginTransaction();

            try
            {
                using (OleDbCommand cmd = conn.CreateCommand())
                {
                    cmd.Transaction = trans;

                    //пометка исправленной записи как удаленной
                    if (oldRowId != "")
                    {
                        cmd.CommandText = "update TRANSPORT_NUMBERS set EDIT_STATE=2 where ROW_ID=?";
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("ROW_ID", oldRowId);
                        cmd.ExecuteNonQuery();
                    }

                    //сохранение записи-подтверждения удаления
                    cmd.CommandText = @"INSERT INTO TRANSPORT_NUMBERS (
                        REC_DATE, OPERATOR_ID, EDIT_STATE, 
                        ROW_ID, ORIGINAL_ROW_ID, PIPE_NUMBER) 
                    VALUES ( SYSDATE /* REC_DATE */,
                        ? /* OPERATOR_ID */,
                        3 /* EDIT_STATE */,
                        ? /* ROW_ID */,
                        ? /* ORIGINAL_ROW_ID */,
                        ? /* PIPE_NUMBER */ )";

                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("OPERATOR_ID", Authentification.User.UserName);
                    cmd.Parameters.AddWithValue("ROW_ID", newRowId);
                    cmd.Parameters.AddWithValue("ORIGINAL_ROW_ID", oldRowId);
                    cmd.Parameters.AddWithValue("PIPE_NUMBER", transportPipeNumber);

                    cmd.ExecuteNonQuery();

                    trans.Commit();
                }
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }
        catch (Exception ex)
        {
            Master.AlertMessage = "Ошибка удаления транспортировочного номера: " + ex.Message;
            Master.AddErrorMessage("Ошибка удаления транспортировочного номера", ex);
        }
    }


    /// <summary>
    /// Динамическое построение элементов вкладки журнала записей по получению транспортировочных номеров
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void vTransportNumberFind_PreRender(object sender, EventArgs e)
    {
        try
        {
            //стирание выбранного номера на странице журнала записей
            fldSelectedTransportNumber.Value = "";

            //дата начала и окончания периода
            DateTime startDate = (cldTransportNumberPeriodStart.SelectedDate != null) ? cldTransportNumberPeriodStart.SelectedDate.Value : DateTime.Today.AddDays(-1);
            DateTime endDate = (cldTransportNumberPeriodEnd.SelectedDate != null) ? cldTransportNumberPeriodEnd.SelectedDate.Value.AddDays(1) : DateTime.Today;

            //формирование списка записей за период
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            using (OleDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT tp.*, 
                        nvl(sk.FIO, tp.OPERATOR_ID) FIO,
                        wp.WORKPLACE_NAME
                    FROM TRANSPORT_NUMBERS tp     
                    LEFT JOIN SPR_KADRY sk
                       ON OPERATOR_ID = sk.USERNAME
                    LEFT JOIN SPR_WORKPLACES wp
                        ON tp.WORKPLACE_ID = wp.ID
                    WHERE EDIT_STATE = 0
                        AND REC_DATE>=?
                        AND REC_DATE<?
                    ORDER BY REC_DATE";

                cmd.Parameters.AddWithValue("START_DATE", startDate);
                cmd.Parameters.AddWithValue("END_DATE", endDate);

                using (OleDbDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        TableRow row = new TableRow();
                        for (int i = 0; i < tblTransportNumbersHistory.Rows[0].Cells.Count; i++)
                        {
                            row.Cells.Add(new TableCell());
                        }

                        row.Cells[0].Text = Convert.ToDateTime(rdr["REC_DATE"]).ToString("dd.MM.yyyy HH:mm");
                        row.Cells[1].Text = rdr["PIPE_NUMBER"].ToString();
                        row.Cells[2].Text = rdr["DIAMETER"].ToString() != "" ? rdr["DIAMETER"].ToString() : rdr["PROFILE_SIZE_A"].ToString() + "x" + rdr["PROFILE_SIZE_B"].ToString();
                        row.Cells[3].Text = rdr["THICKNESS"].ToString();
                        row.Cells[4].Text = rdr["STEELMARK"].ToString();
                        row.Cells[5].Text = rdr["WORKPLACE_NAME"].ToString();

                        //скрипт выделения строки по клику
                        row.Attributes["rowid"] = rdr["ROW_ID"].ToString();
                        row.Attributes["onclick"] = "HighlightTransportNumberTableRow(this, " + rdr["PIPE_NUMBER"] + ")";

                        tblTransportNumbersHistory.Rows.Add(row);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка формирования журнала записей по получению транспортировочных номеров", ex);
        }
    }

    #endregion


    /// <summary>
    /// Расчет массы по теории
    /// </summary>
    private void GetWeight()
    {
        try
        {
            if (txbLength.Text != "")
            {
                double pipe_length = 0;
                double conversion_rate = 0;
                double weight = 0;
                if (double.TryParse(txbLength.Text, out pipe_length))
                {
                    using (OleDbCommand cmd = Master.Connect.ORACLE_TESC3().CreateCommand())
                    {
                        cmd.CommandText = @"SELECT pi.conversion_rate
                                              FROM tesc3.optimal_pipes op
                                                   LEFT JOIN tesc3.geometry_coils_sklad gcs
                                                      ON     gcs.edit_state = 0
                                                         AND gcs.coil_run_no = op.coil_internalno
                                                         AND gcs.coil_pipepart_no = op.coil_pipepart_no
                                                         AND gcs.coil_pipepart_year = op.coil_pipepart_year
                                                   LEFT JOIN tesc3.campaigns c
                                                      ON c.edit_state = 0 AND c.campaign_line_id = gcs.campaign_line_id
                                                   LEFT JOIN oracle.v_t3_pipe_items pi
                                                      ON pi.nomer = c.inventory_code
                                             WHERE op.pipe_number = ?";
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("pipe_number", PipeNumber);
                        using (OleDbDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.HasRows)
                            {
                                rdr.Read();
                                double.TryParse(rdr["conversion_rate"].ToString(), out conversion_rate);
                                rdr.Close();
                            }
                            else
                            {
                                Master.AlertMessage = "Отсутствует коэффициент пересчета для выбранной трубы.";
                                return;
                            }
                        }
                    }

                    weight = pipe_length * conversion_rate;
                    txbWeight.Text = Math.Round(weight).ToString();
                    lblWeightTemp.Text = txbWeight.Text;
                }
                else
                {
                    Master.AlertMessage = "Введено некорректное значение длины трубы.";
                    return;
                }
            }
            else
            {
                Master.AlertMessage = "Необходимо ввести длину трубы.";
                return;
            }
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка расчета веса по теории", ex);
        }
    }

    /// <summary>
    /// Проверка трубы на дату ее создания до 01.07.2014
    /// </summary>
    /// <returns></returns>
    private bool CheckOldPipe()
    {
        try
        {
            using (OleDbCommand cmd = Master.Connect.ORACLE_TESC3().CreateCommand())
            {
                cmd.CommandText = "select nvl(max(CUTDATE),SYSDATE) from OPTIMAL_PIPES where PIPE_NUMBER=?";
                cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);
                DateTime pipeCutDate = Convert.ToDateTime(cmd.ExecuteScalar());

                //для труб произведенных ранее чем 01.07.2014 проверки не производятся
                if (pipeCutDate < new DateTime(2014, 07, 01)) return true;
            }
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка проверки трубы на ввод до 01.07,2014", ex);
        }
        return false;
    }

    /// <summary>
    /// Проверка профильная ли труба?
    /// </summary>
    /// <returns></returns>
    private bool CheckProfilePipe()
    {
        try
        {
            using (OleDbCommand cmd = Master.Connect.ORACLE_TESC3().CreateCommand())
            {
                cmd.CommandText = @"SELECT CASE WHEN nvl(gc.profile_size_a, 0) = 0 THEN 
                                                CASE WHEN nvl(gc.profile_size_b, 0) = 0 THEN 0
                                                ELSE gc.profile_size_b END
                                           ELSE gc.profile_size_a END PROFILE_SIZE
                                    FROM    optimal_pipes op
                                       LEFT JOIN
                                          geometry_coils_sklad gc
                                       ON (op.coil_pipepart_no = gc.coil_pipepart_no)
                                          AND (op.coil_pipepart_year = gc.coil_pipepart_year)
                                          AND (op.coil_internalno = gc.coil_run_no)
                                    WHERE op.pipe_number = ? AND (gc.edit_state = 0 OR gc.edit_state IS NULL)";
                cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);
                Int16 pSize = Convert.ToInt16(cmd.ExecuteScalar());

                //для профильных труб проверки не производятся
                if (pSize > 0) return true;
            }
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка проверки профильная ли труба?", ex);
        }
        return false;
    }


    // проверка на повторную термообработку
    bool CheckReHeatTreatment(string GOST, string GROUP)
    {
        try
        {
            //получение текущего номера трубы
            int pipe_number = 0;
            if (!Int32.TryParse(txbYear.Text.Trim() + txbPipeNumber.Text.Trim().PadLeft(6, '0'), out pipe_number)) return false;

            //получение партии трубы по задаче
            Int64 _pipenumber = 0;
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();

            cmd.CommandText = @"select sr.* 
                                from SKLAD_RETURN sr
                                join (select SKLAD_RETURN.PIPE_NUMBER,
                                       max(SKLAD_RETURN.REC_DATE) REC_DATE 
                                from SKLAD_RETURN
                                where PIPE_NUMBER= ? and EDIT_STATE=0
                                group by PIPE_NUMBER) dat on dat.REC_DATE = SR.REC_DATE and SR.PIPE_NUMBER = dat.pipe_number
                                left join SPR_DEFECT on DEFECT_CODE=SPR_DEFECT.ID
                                where defect_code = 423
                                order by sr.REC_DATE desc ";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("PIPE_NUMBER", pipe_number);
            OleDbDataReader rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                rdr.Close();

                // Проверка НД, которые отмечены в "справочнике НД на черные трубы" флагом «Принимать при назначении повторной термообработки»
                cmd.CommandText = @"select SN.ACCEPT_REHEAT_TREATMENT
                                    from spr_NTD sn
                                    where sn.NTD_NAME = ?";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("NTD_NAME", GOST);
                if (!String.IsNullOrEmpty(GROUP))
                {
                    cmd.CommandText += " and sn.NTD_GROUP = ? ";
                    cmd.Parameters.AddWithValue("NTD_GROUP", GROUP);
                }

                rdr = cmd.ExecuteReader();
                if (rdr.Read())
                {
                    if (Convert.ToInt64(rdr["ACCEPT_REHEAT_TREATMENT"]) == 1)
                    {
                        return true;
                    }

                }
                else
                {
                    Master.AlertMessage = "Внимание!\n\nДанного НД нет в справочнике на черные трубы!";
                }
                rdr.Close();

                cmd.CommandText = @"select pipenumber, trx_date, rec_date
                                    from (select pipenumber
                                         , max(trx_date) trx_date
                                    from TERMO_OTDEL_PIPES_TESC3 topt
                                    where topt.edit_state = 0 and pipenumber = ?
                                    group by  pipenumber) a

                                    join (select skl.PIPE_NUMBER
                                               , skl.REC_DATE
                                          from sklad_return skl
                                          join 
                                          (select PIPE_NUMBER
                                                , max(SR.REC_DATE) rec_date
                                          from SKLAD_RETURN sr 
                                          where SR.EDIT_STATE = 0 and pipe_number = ? and defect_code = 423
                                          group by PIPE_NUMBER) tmp on tmp.pipe_number = SKL.PIPE_NUMBER and SKL.REC_DATE = tmp.rec_date) b 
                                    on a.PIPENUMBER = b.PIPE_NUMBER and a.TRX_DATE > b.REC_DATE
                                    order by trx_date desc";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("pipenumber", pipe_number);
                cmd.Parameters.AddWithValue("pipe_number", pipe_number);
                rdr = cmd.ExecuteReader();
                if (rdr.Read())
                {
                    _pipenumber = Convert.ToInt64(rdr["PIPENUMBER"]);
                }
                rdr.Close();

                //выход и предупреждение, если труба не была зафиксирована на термообработке 
                if (_pipenumber == 0)
                {
                    Master.AlertMessage = "Внимание!\n\nДанная труба не прошла повторную термообработку!";
                    return false;
                }

                cmd.CommandText = @"select topt.trx_date
                                         , topt.PIPENUMBER
                                    from TERMO_OTDEL_PIPES_TESC3 topt
                                    join (select  max(trx_date) trx_date
                                                , PIPENUMBER
                                          from TERMO_OTDEL_PIPES_TESC3 
                                          where pipenumber = ? and (edit_state = 1 or edit_state = 0)
                                          group by PIPENUMBER) dat on dat.PIPENUMBER = TOPT.PIPENUMBER and dat.trx_date = TOPT.TRX_DATE 
                                    join optimal_pipes op on ToPt.PIPENUMBER=OP.PIPE_NUMBER
                                    JOIN  geometry_coils_sklad gcs  ON (op.coil_pipepart_no = gcs.coil_pipepart_no)
                                                                   AND (op.coil_pipepart_year = gcs.coil_pipepart_year) 
                                                                   And (op.coil_internalno = gcs.coil_run_no)
                                                                   and GCS.PIPE_PART_NO_THERMO = TOPT.TOPARTNUMBER  
                                    where GCS.EDIT_STATE = 0 ";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("PIPENUMBER", _pipenumber);
                rdr = cmd.ExecuteReader();
                //выход и предупреждение, если не изменена партия на другую, отличную от той, что была до термообработки

                if (rdr.Read())
                {
                    Master.AlertMessage = "Внимание!\n\nУ данной трубы при прохождении последней термообработки не изменена партия ТО!";
                    return false;
                }
                rdr.Close();
            }
            rdr.Dispose();
            cmd.Dispose();

        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка проверки прохождения повторной термообработки", ex);
            return false;
        }

        return true;
    }

    //проверка дефекта на термообработке
    bool CheckDefectOTO(string GOST, string GROUP)
    {
        try
        {
            //получение текущего номера трубы
            int pipe_number = 0;
            if (!Int32.TryParse(txbYear.Text.Trim() + txbPipeNumber.Text.Trim().PadLeft(6, '0'), out pipe_number)) return false;

            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();

            cmd.CommandText = @"select topt.trx_date
                                     , topt.PIPENUMBER
                                     , TOPT.DEFECT_ID_PIPE
                                     , SD.DEFECT_NAME
                                from TERMO_OTDEL_PIPES_TESC3 topt
                                join (select  max(trx_date) trx_date
                                            , PIPENUMBER
                                      from TERMO_OTDEL_PIPES_TESC3 
                                      where pipenumber = ?
                                      group by PIPENUMBER) dat on dat.PIPENUMBER = TOPT.PIPENUMBER and dat.trx_date = TOPT.TRX_DATE 
                                left join spr_defect sd on SD.ID = TOPT.DEFECT_ID_PIPE      
                                where   SD.DEFECT_NAME = 'Перегрев' or SD.DEFECT_NAME = 'Перегрев/буферная' ";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("PIPENUMBER", pipe_number);
            OleDbDataReader rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                if (GOST != "ТС 153-21-2007" && !(GOST == "ГОСТ 10705-80" && GROUP == "Д"))
                {
                    rdr.Dispose();
                    cmd.Dispose();
                    Master.AlertMessage = "Имеется дефект на термообработке. Разрешено принимать только в брак или под ТС 153-21-2007 и ГОСТ 10705-80 гр.Д.";
                    return false;
                }
            }
            rdr.Dispose();
            cmd.Dispose();

        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка проверки дефектов на термообработке", ex);
            return false;
        }

        return true;
    }


    //проверка дефекта 'На отгрузке'
    bool CheckDefectShipment()
    {
        try
        {
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();

            cmd.CommandText = @"select sd.id
                                from spr_defect sd 
                                where SD.DEFECT_AREA = 'На отгрузке' and SD.IS_ACTIVE = 1 and sd.id = ? ";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("ID", (String.IsNullOrEmpty(ddlDefect.SelectedValue.Trim(' '))) ? "-1" : ddlDefect.SelectedValue);
            OleDbDataReader rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                rdr.Dispose();
                cmd.Dispose();
                Master.AlertMessage = "Запрещено принимать трубы на склад с дефектом 'На отгрузке'";
                return true;
            }
            rdr.Dispose();
            cmd.Dispose();

        }
        catch (Exception ex)
        {
            Master.AlertMessage = "Ошибка проверки дефектов 'На отгрузке' - " + ex.Message.ToString();
            return true;
        }

        return false;
    }


    #region ********************Проверка на геометрические замеры********************
    /// <summary>Отметка трубы о проведении геометрических замеров</summary>
    int CheckMark { get { object o = ViewState["_CheckMark"]; if (o == null) return 0; return (int)ViewState["_CheckMark"]; } set { ViewState["_CheckMark"] = value; } }


    /// <summary>Проверка на необходимость проверки геометрических замеров</summary>
    /// <returns>0 - замеры не требуются, 1 - по указанным сортаментным данным отсутствуют замеры за месяц, 2 - подошло время на переодичность замеров</returns>
    private int CheckPeriodicityGeom()
    {
        try
        {
            //необходимые переменные
            string gost = "";
            string group = "";
            string diameter = "";
            string size_a = "";
            string size_b = "";
            string thickness = "";
            string steelmark = "";
            int periodicity_check_geom = 0;
            int accept_without_geometric = 0;
            DateTime last_date = DateTime.MinValue;

            //получение значений сортамента
            GetNDAndGroup(out gost, out group, ddlNTD);
            diameter = ddlDiam.SelectedItem.Text;
            if (ddlProfileSize.SelectedItem.Text != "")
            {
                string[] profile = ddlProfileSize.SelectedItem.Text.Split('x');
                if (profile.Length > 1)
                {
                    size_a = profile[0];
                    size_b = profile[1];
                }
            }
            thickness = ddlThickness.SelectedItem.Text;
            steelmark = ddlSteelmark.SelectedItem.Text;

            int campaignLineId = 0;
            int.TryParse(ddlCampaign.SelectedItem.Value, out campaignLineId);


            using (OracleCommand cmd = Master.Connect.ORACLE_TESC3_ORA().CreateCommand())
            {
                //получение периодичности замеров на геометрические параметры из справочника по НД
                cmd.CommandText = @"SELECT NVL (sn.periodicity_check_geom, 0) periodicity_check_geom,
                                           sn.accept_without_geometric
                                      FROM tesc3.spr_ntd sn
                                     WHERE     sn.is_active = 1
                                           AND sn.ntd_name = :ntd_name
                                           AND (sn.ntd_group = :ntd_group OR :ntd_group IS NULL)";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("ntd_name", gost);
                cmd.Parameters.AddWithValue("ntd_group", group);
                using (OracleDataReader rdr = cmd.ExecuteReader())
                {
                    if (rdr.HasRows)
                    {
                        if (rdr.Read())
                        {
                            int.TryParse(rdr["accept_without_geometric"].ToString(), out accept_without_geometric);
                            int.TryParse(rdr["periodicity_check_geom"].ToString(), out periodicity_check_geom);
                        }
                        rdr.Close();
                    }
                }

                //выход, если проверка на замеры не требуется
                if (accept_without_geometric == 1) return 0;

                //получение периодичности замеров на геометрические параметры из справочника дополнительных геометрических параметров
                // значение из справочника дополнительных геометрических параметров являеся более приоритетным по сравнению со значением из справочника по НД
                cmd.CommandText = @"select NVL(SAGP.PERIOD,0) as periodicity_check_geom from TESC3.SPR_ADDITIONAL_GEOM_PARAMS sagp
                                    join TESC3.CAMPAIGNS cmp
                                    on cmp.REQUIREMENT_ID=SAGP.SETTINGS_ID
                                    where CMP.CAMPAIGN_LINE_ID=:campaignLineId and CMP.EDIT_STATE=0
                                    AND SAGP.IS_ACTIVE=1 AND SAGP.EDIT_STATE=0     ";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("campaignLineId", campaignLineId);

                using (OracleDataReader rdr = cmd.ExecuteReader())
                {
                    if (rdr.HasRows)
                    {
                        if (rdr.Read())
                        {
                            int.TryParse(rdr["periodicity_check_geom"].ToString(), out periodicity_check_geom);
                        }
                        rdr.Close();
                    }
                }


                //выход, если периодичность замеров не указана
                if (periodicity_check_geom < 1) return 0;

                //фильтр по рабочему центру (для площадки №7 проверка идет только по ней, для остальных с учетом линии)
                string workplace_filter = WorkplaceId != 80 ?
                    " AND ip.workplace_id IN (SELECT sw.id FROM tesc3.spr_workplaces sw WHERE sw.area = (SELECT sw_.area FROM tesc3.spr_workplaces sw_ WHERE sw_.id = :workplace_id))" :
                    " AND ip.workplace_id = :workplace_id";

                //получение даты последнего замера по сортаментным данным
                cmd.CommandText = @"SELECT MAX (ip.trx_date)
                                      FROM tesc3.inspection_pipes ip
                                     WHERE ip.edit_state = 0 AND ((ip.next_direction = 'SKLAD'
                                              AND ip.gost = :gost
                                              AND (ip.gost_group = :gost_group OR :gost_group IS NULL))
                                            OR (    ip.next_direction = 'REMONT'
                                                AND ip.zachistka_checkbox = 'Y'
                                                AND ip.zachistka_gost = :zachistka_gost))" + workplace_filter +
                                           @" AND ip.trx_date >= ADD_MONTHS (SYSDATE, -1)
                                           AND (ip.diameter = :diameter
                                                OR (ip.profile_size_a = :profile_size_a
                                                    AND ip.profile_size_b = :profile_size_b))
                                           AND ip.thickness = :thickness
                                           AND ip.steelmark = :steelmark
                                           AND ip.checkmark = 1";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("gost", gost);
                cmd.Parameters.AddWithValue("gost_group", group);
                cmd.Parameters.AddWithValue("zachistka_gost", gost + (group != "" ? " гр." + group : ""));
                cmd.Parameters.AddWithValue("workplace_id", WorkplaceId);
                cmd.Parameters.AddWithValue("diameter", Checking.GetDbType(diameter));
                cmd.Parameters.AddWithValue("profile_size_a", Checking.GetDbType(size_a));
                cmd.Parameters.AddWithValue("profile_size_b", Checking.GetDbType(size_b));
                cmd.Parameters.AddWithValue("thickness", Checking.GetDbType(thickness));
                cmd.Parameters.AddWithValue("steelmark", steelmark);
                if (!DateTime.TryParse(cmd.ExecuteScalar().ToString(), out last_date))
                    return 1; //отсутствуют замеры в текущем месяце

                //получение количества труб после последнего замера
                cmd.CommandText = @"SELECT COUNT (*)
                                      FROM tesc3.inspection_pipes ip
                                     WHERE ip.edit_state = 0 AND ((ip.next_direction = 'SKLAD'
                                              AND ip.gost = :gost
                                              AND (ip.gost_group = :gost_group OR :gost_group IS NULL))
                                            OR (    ip.next_direction = 'REMONT'
                                                AND ip.zachistka_checkbox = 'Y'
                                                AND ip.zachistka_gost = :zachistka_gost))" + workplace_filter +
                                           @" AND ip.trx_date > :last_date
                                           AND (ip.diameter = :diameter
                                                OR (ip.profile_size_a = :profile_size_a
                                                    AND ip.profile_size_b = :profile_size_b))
                                           AND ip.thickness = :thickness
                                           AND ip.steelmark = :steelmark
                                           AND ip.checkmark = 0";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("gost", gost);
                cmd.Parameters.AddWithValue("gost_group", group);
                cmd.Parameters.AddWithValue("zachistka_gost", gost + group != "" ? " гр." + group : "");
                cmd.Parameters.AddWithValue("workplace_id", WorkplaceId);
                cmd.Parameters.AddWithValue("last_date", last_date);
                cmd.Parameters.AddWithValue("diameter", Checking.GetDbType(diameter));
                cmd.Parameters.AddWithValue("profile_size_a", Checking.GetDbType(size_a));
                cmd.Parameters.AddWithValue("profile_size_b", Checking.GetDbType(size_b));
                cmd.Parameters.AddWithValue("thickness", Checking.GetDbType(thickness));
                cmd.Parameters.AddWithValue("steelmark", steelmark);
                if (Convert.ToInt32(cmd.ExecuteScalar()) + 1 >= periodicity_check_geom)
                    return 2; //подошло время на переодичность замеров
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Ошибка проверки на периодичность (" + ex.Message + ")");
        }
        return 0;
    }


    /// <summary>Проверка на автоматические замеры</summary>
    /// <param name="message">Сообщение ошибки</param>
    /// <returns>true - пройдено или не требуется, false - не требуется</returns>
    private bool CheckAutoGeometryParam(out string message)
    {
        message = "";
        try
        {
            //если ПДО
            if (IsPdoForm) return true;

            //проверка на периодичность замеров
            int iCheckPeriodicityGeom = CheckPeriodicityGeom();
            if (iCheckPeriodicityGeom == 0) return true;

            //получение информации по последнему входу на линию
            PIPES_MOTION_HISTORY inPosition = PIPES_MOTION_HISTORY_Repository.GetHistoryLastInOrOutPosition(PipeNumber, true);

            //получение информации по геометрическим замерам из класса
            int campaignId = 0;
            int.TryParse(ddlCampaign.SelectedItem.Value, out campaignId);
            CheckPipe cp = new CheckPipe(PipeNumber, campaignId);
            DateTime NawPositionTime;
            if (DateTime.TryParse(cp.GetMrtValue("MEASURE_TIME").ToString(), out NawPositionTime) && NawPositionTime < inPosition.REC_DATE)
            {
                message = "Отсутствуют актуальные данные по последнему входу на линию";
                return false;
            }

            bool isExistAuto = cp.GetMrtValues().Count != 0;
            bool isExistSpr = cp.GetGeomValues().Count != 0;

            if (isExistAuto)
            {
                List<string> notConditionalList = new List<string>();
                bool checkAuto = cp.TestMrtGeom(out notConditionalList);
                if (isExistSpr && !checkAuto)
                {
                    message = "\nВнимание.\nАвтоматические геометрические замеры не соответствуют нормативам по следующим параметрам:";
                    foreach (string notCond in notConditionalList)
                        message += "\n" + notCond + ".";
                    return false;
                }
                return true;
            }
        }
        catch (Exception ex)
        {
            message = "Ошибка проверки трубы на автоматические замеры геометрических параметров - " + ex.Message.ToString();
            return false;
        }
        return true;
    }


    /// <summary>Проверка на ручные замеры</summary>
    /// <param name="message">Сообщение ошибки</param>
    /// <returns>true - пройдено или не требуется, false - не требуется</returns>
    private bool CheckManualGeometryParam(out string message, bool is_remont = false)
    {
        message = "";
        try
        {
            //если ПДО
            if (IsPdoForm) return true;

            int current_workplace_id = WorkplaceId;

            //проверка на периодичность замеров
            int iCheckPeriodicityGeom = 0;
            if (current_workplace_id != 85)
            {
                iCheckPeriodicityGeom = CheckPeriodicityGeom();
                if (iCheckPeriodicityGeom == 0) return true;
            }
            else
            {
                using (OracleCommand cmd = Master.Connect.ORACLE_TESC3_ORA().CreateCommand())
                {
                    cmd.CommandText = @"SELECT ip.workplace_id
                                          FROM tesc3.inspection_pipes ip
                                         WHERE     ip.edit_state = 0
                                               AND ip.pipe_number = :pipe_number
                                               AND ip.zachistka_checkbox = 'Y'
                                               AND ip.checkmark = 1
                                               AND ip.trx_date =
                                                      (SELECT MAX (ip_.trx_date)
                                                         FROM tesc3.inspection_pipes ip_
                                                        WHERE ip_.edit_state = 0 AND ip_.pipe_number = ip.pipe_number)";
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("pipe_number", PipeNumber);
                    using (OracleDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.HasRows)
                        {
                            if (rdr.Read())
                            {
                                int.TryParse(rdr["workplace_id"].ToString(), out current_workplace_id);
                            }
                            rdr.Close();
                            iCheckPeriodicityGeom = 2;
                        }
                        else return true;
                    }
                }
            }

            if (current_workplace_id != 80) //Инспекционные площадки для круглой трубы
            {
                //получение информации по геометрическим замерам из класса
                int campaignId = 0;
                int.TryParse(ddlCampaign.SelectedItem.Value, out campaignId);
                CheckPipe cp = new CheckPipe(PipeNumber, campaignId);

                bool isExistSpr = cp.GetGeomValues().Count != 0;

                //получение информации по последнему входу на линию
                PIPES_MOTION_HISTORY inPosition =
                    PIPES_MOTION_HISTORY_Repository.GetHistoryLastInOrOutPosition(PipeNumber, true);

                using (OracleCommand cmd = Master.Connect.ORACLE_TESC3_ORA().CreateCommand())
                {
                    //проверка текущей трубы на замеры геометрических параметров
                    cmd.CommandText = @"SELECT COUNT (*)
                                      FROM tesc3.geometry_pipes_insp gpi
                                     WHERE     gpi.edit_state = 0
                                           AND gpi.pipe_number = :pipe_number";
                    if (WorkplaceId != 85)
                        cmd.CommandText += " AND gpi.rec_date >= :rec_date";
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("pipe_number", PipeNumber);
                    if (WorkplaceId != 85)
                        cmd.Parameters.AddWithValue("rec_date",
                            inPosition.REC_DATE > DateTime.MinValue
                                ? inPosition.REC_DATE
                                : ShiftTime.ShiftStartTime12(DateTime.Now));
                    if (Convert.ToInt32(cmd.ExecuteScalar()) > 0)
                    {
                        if (!is_remont)
                        {
                            List<string> notConditionalList = new List<string>();
                            bool checkManual = cp.TestInspGeom(out notConditionalList);
                            if (isExistSpr && !checkManual)
                            {
                                message =
                                    "\nНевозможно принять на СГП.\nРучные геометрические замеры не соответствуют нормативам по следующим параметрам:";
                                foreach (string notCond in notConditionalList)
                                    message += "\n" + notCond;
                                return false;
                            }
                        }

                        CheckMark = WorkplaceId != 85 ? 1 : 0;
                        return true;
                    }
                }
            }
            else //профильная труба
            {
                //необходимые переменные
                string gost = "";
                string group = "";
                string size_a = "";
                string size_b = "";
                string thickness = "";

                //получение значений сортамента
                GetNDAndGroup(out gost, out group, ddlNTD);
                if (ddlProfileSize.SelectedItem.Text != "")
                {
                    string[] profile = ddlProfileSize.SelectedItem.Text.Split('x');
                    if (profile.Length > 1)
                    {
                        size_a = profile[0];
                        size_b = profile[1];
                    }
                }
                thickness = ddlThickness.SelectedItem.Text;

                using (OracleCommand cmd = Master.Connect.ORACLE_TESC3_ORA().CreateCommand())
                {
                    //проверка текущей трубы на наличие записей в журнале "Журнал результатов контроля профильных труб , Ф 7 УСТ"
                    cmd.CommandText = @"SELECT COUNT (*)
                                          FROM (SELECT (SELECT jfd.string_value
                                                          FROM tesc3.journal_field_data jfd
                                                         WHERE jfd.journal_record_id = jr.id
                                                               AND jfd.field_name = 'ND')
                                                          nd,
                                                       (SELECT jfd.number_value
                                                          FROM tesc3.journal_field_data jfd
                                                         WHERE jfd.journal_record_id = jr.id
                                                               AND jfd.field_name = 'PROFILE_SIZE_A')
                                                          profile_size_a,
                                                       (SELECT jfd.number_value
                                                          FROM tesc3.journal_field_data jfd
                                                         WHERE jfd.journal_record_id = jr.id
                                                               AND jfd.field_name = 'PROFILE_SIZE_B')
                                                          profile_size_b,
                                                       (SELECT jfd.number_value
                                                          FROM tesc3.journal_field_data jfd
                                                         WHERE jfd.journal_record_id = jr.id
                                                               AND jfd.field_name = 'THICKNESS')
                                                          thickness
                                                  FROM tesc3.journal_records jr
                                                 WHERE     jr.journal_id = 204
                                                       AND jr.journal_table_id = 1
                                                       AND jr.edit_state = 0)
                                         WHERE     nd = :gost
                                               AND profile_size_a = :profile_size_a
                                               AND profile_size_b = :profile_size_b
                                               AND thickness = :thickness";
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("gost", gost + (group != "" ? "гр. " + group : ""));
                    cmd.Parameters.AddWithValue("profile_size_a", Checking.GetDbType(size_a));
                    cmd.Parameters.AddWithValue("profile_size_b", Checking.GetDbType(size_b));
                    cmd.Parameters.AddWithValue("thickness", Checking.GetDbType(thickness));
                    if (Convert.ToInt32(cmd.ExecuteScalar()) > 0)
                    {
                        CheckMark = WorkplaceId != 85 ? 1 : 0;
                        return true;
                    }
                }
            }

            switch (iCheckPeriodicityGeom)
            {
                case 1: message = "За последний месяц по указанному сортаменту замеры геометрических параметров не проводились! Необходимо перед отправкой трубы на склад произвести замеры."; break;
                case 2: message = "\nНевозможно принять на СГП.\nГеометрические замеры на инспекциях отсутствуют.\nЗаполните форму ввода геометрических замеров на инспекционных решетках."; break;
            }
            return false;
        }
        catch (Exception ex)
        {
            message = "Ошибка проверки трубы на ручные замеры геометрических параметров - " + ex.Message.ToString();
            return false;
        }
    }
    #endregion ********************Проверка на геометрические замеры********************


    protected void btnGetWeightT_Click(object sender, EventArgs e)
    {

    }

    //проверка "Запрет приема на склад труб с названием внешней инспекции отличающейся от внешней инспекции заданной на рулон"
    bool CheckPipeInspections(out string s_insp_roll, out string s_insp_pipe)
    {
        try
        {
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();

            s_insp_roll = "NOT FOUND";
            s_insp_pipe = "NOT FOUND";


            cmd.CommandText =
                @"select gcs.campaign_line_id, nvl(ca.inspection, 'NOT FOUND') inspection
                    from geometry_coils_sklad gcs, campaigns ca
                   where gcs.edit_state = 0
                    and(gcs.coil_pipepart_no, gcs.coil_pipepart_year, gcs.coil_run_no) in (select op.coil_pipepart_no, op.coil_pipepart_year, op.coil_internalno
                                                                            from optimal_pipes op
                                                                            where op.pipe_number = ? )
                    and gcs.campaign_line_id = ca.campaign_line_id
                    and ca.edit_state = 0 ";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("pipe_number", PipeNumber);

            OleDbDataReader rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                s_insp_roll = rdr["inspection"].ToString();
            }
            rdr.Close();

            if (s_insp_roll == "NOT FOUND") return true;

            int campaignLineId = 0;
            int.TryParse(ddlCampaign.SelectedItem.Value, out campaignLineId);

            cmd.CommandText =
                @"select nvl (ca.inspection, 'NOT FOUND') inspection
                    from campaigns ca
                   where ca.edit_state = 0 and ca.campaign_line_id = ? ";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("campaign_line_id", campaignLineId);

            rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                s_insp_pipe = rdr["inspection"].ToString();
            }
            rdr.Close();
            if ((s_insp_pipe != "NOT FOUND") && (s_insp_roll == s_insp_pipe)) return true;

            if (!IsPdoForm)
                Master.AlertMessage = "Внимание! Вы не можете произвести приемку данной трубы по этому заказу, так как труба должна быть принята на заказ с внешней инспекцией " + s_insp_roll;

            rdr.Dispose();
            cmd.Dispose();
        }
        catch (Exception ex)
        {
            s_insp_roll = "";
            s_insp_pipe = "";
            Master.AlertMessage = "Ошибка при проверке инспекций (код 69): " + ex.Message.ToString();
            return false;
        }

        return false;
    }

    protected void Check69(string s_insp_roll, string s_insp_pipe)
    {
        try
        {
            String warning = "Внимание! На задаче рулона в производство ему была указана внешняя инспекция [ " + s_insp_roll +
                " ]. Вы уверены, что трубу нужно принять именно на эту строку кампании?";
            AddWarning(warning, true, true);
            return;
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка проверки (код 69) ", ex);
            AddWarning("Ошибка проверки (код 69) ", true, true);
            return;
        }
    }


    bool CheckRestrictions70()
    {
        try
        {
            //получение метки дефекта с УЗК сварки
            String usc_weld_defect = "";
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "select nvl(defects, 0) defects from optimal_pipes where pipe_number = ? ";
            cmd.Parameters.AddWithValue("pipe_number", PipeNumber);
            OleDbDataReader rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                //10 "Дефект УЗК шва"

                int def = Convert.ToInt32(rdr["DEFECTS"]);
                def = ((def >> 10) & 0x01);
                if (def != 0) def = 1;
                usc_weld_defect = TestResultToString(def);
            }
            rdr.Close();
            rdr.Dispose();
            cmd.Dispose();

            if (usc_weld_defect == "Пройдено") return true;

            //получение метки дефекта с АУЗК линий отделки
            String usc_otdelka_defect = "";
            cmd = conn.CreateCommand();
            cmd.CommandText =
                @"select first_rec_date, row_id, nvl (test_brak_manual, test_brak_auto) is_brak
                    from usc_otdelka
                   where edit_state = 0
                     and pipe_number = ?
                     and workplace_id in (13, 14)
                     and first_rec_date >= nvl ( (select max (trx_date)
                                                    from inspection_pipes
                                                   where pipe_number = ? and edit_state = 0 and workplace_id = 7)
                                              , first_rec_date)
                order by first_rec_date desc ";
            cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);
            cmd.Parameters.AddWithValue("PIPE_NUMBER_INSP", PipeNumber);
            rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                usc_otdelka_defect = TestResultToString(rdr["IS_BRAK"].ToString());
            }
            rdr.Close();
            rdr.Dispose();
            cmd.Dispose();

            //получение последних результатов перепроверки РУЗК
            String ruzk_weld_defect = "Нет данных";
            String ruzk_otdelka_defect = "Нет данных";
            cmd = conn.CreateCommand();
            cmd.CommandText =
                @"select test_brak_after_stan, test_brak_manual
                    from usc_otdelka
                   where pipe_number = ?
                     and edit_state = 0
                     and workplace_id in (25, 26, 27, 28, 29, 30)
                     and first_rec_date >= nvl((select max(trx_date)
                                                    from inspection_pipes
                                                   where pipe_number = ? and edit_state = 0 and workplace_id = 7)
                                              , first_rec_date)
                order by first_rec_date desc ";

            cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);
            cmd.Parameters.AddWithValue("PIPE_NUMBER_INSP", PipeNumber);
            rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                ruzk_weld_defect = TestResultToString(rdr["TEST_BRAK_AFTER_STAN"]);
                ruzk_otdelka_defect = TestResultToString(rdr["TEST_BRAK_MANUAL"]);
            }
            rdr.Close();
            rdr.Dispose();
            cmd.Dispose();


            //запрет приёмки, если имеется дефект РУЗК
            if (ruzk_otdelka_defect == "Не пройдено" || ruzk_weld_defect == "Не пройдено")
            {
                if (!IsPdoForm)
                    Master.AlertMessage = "Внимание!\n\nТруба не может быть принята, т.к. имееются дефекты РУЗК:\n"
                    + " - перепроверка УЗК сварки: " + ruzk_weld_defect + "\n"
                    + " - перепроверка УЗК шва отделки: " + ruzk_otdelka_defect + "\n";
                return false;
            }

            //запрет приёмки, если имеется дефект УЗК сварки и нет положительного результата перепроверки
            if (usc_weld_defect == "Не пройдено" && ruzk_weld_defect != "Пройдено")
            {
                if (!IsPdoForm)
                    Master.AlertMessage = "Внимание!\n\nТруба не может быть принята, т.к. имееются дефекты УЗК сварки без положительного результата перепроверки:\n"
                    + " - УЗК сварки: " + usc_weld_defect + "\n"
                    + " - результат перепроверки на РУЗК: " + ruzk_weld_defect + "\n";
                return false;
            }

            //запрет приёмки, если имеется дефект УЗК шва и нет положительного результата перепроверки
            if (usc_otdelka_defect == "Не пройдено" && ruzk_otdelka_defect != "Пройдено")
            {
                if (!IsPdoForm)
                    Master.AlertMessage = "Внимание!\n\nТруба не может быть принята, т.к. имееются дефекты УЗК шва линии отделки без положительного результата перепроверки:\n"
                    + " - УЗК шва линии отделки: " + usc_otdelka_defect + "\n"
                    + " - результат перепроверки на РУЗК: " + ruzk_otdelka_defect + "\n";
                return false;
            }

            return true;

        }
        catch (Exception ex)
        {
            Master.AlertMessage = "Ошибка при проверке АУЗК (код 70): " + ex.Message.ToString();
            return false;
        }

    }
    protected void Check70()
    {
        try
        {
            String warning = "Внимание! При прохождении АУЗК стана у трубы был выявлен дефект шва. Он не был перепроверен с помощью средств РУЗК. Вы уверены, что трубу нужно принять на склад?";
            AddWarning(warning, true, true);
            return;
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка проверки (код 70) ", ex);
            AddWarning("Ошибка проверки (код 70) ", true, true);
            return;
        }
    }

    protected void Check77(string list_def)
    {
        try
        {
            String warning = "Внимание! При прохождении АУЗК кромок у трубы был выявлен дефект [" + list_def.Trim() +
                             "]. Он не был перепроверен с помощью средств РУЗК. Вы уверены, что трубу нужно принять на склад?";
            AddWarning(warning, true, true);
            return;
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка проверки (код 77) ", ex);
            AddWarning("Ошибка проверки (код 77) ", true, true);
            return;
        }
    }


    // Проверка на дефекты АУЗК промок
    bool CheckRestrictions77(out string list_def)
    {
        list_def = "";
        try
        {
            //получение метки дефекта с УЗК кромок
            String usc_edge_defect = "";

            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "select nvl(defects, 0) defects from optimal_pipes where pipe_number = ? ";
            cmd.Parameters.AddWithValue("pipe_number", PipeNumber);
            OleDbDataReader rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                int def = Convert.ToInt32(rdr["DEFECTS"]);
                //19 "Дефект левой кромки"
                //20 "Дефект правой кромки"
                //21 "Отсутствие АК левой кромки"
                //22 "Отсутствие АК правой кромки"
                int def19, def20, def21, def22;

                def19 = ((def >> 19) & 0x01);
                if (def19 != 0) list_def += "Дефект левой кромки; ";
                def20 = ((def >> 20) & 0x01);
                if (def20 != 0) list_def += "Дефект правой кромки; ";
                def21 = ((def >> 21) & 0x01);
                if (def21 != 0) list_def += "Отсутствие АК левой кромки; ";
                def22 = ((def >> 22) & 0x01);
                if (def22 != 0) list_def += "Отсутствие АК правой кромки; ";

                if ((def19 != 0) || (def20 != 0) || (def21 != 0) || (def22 != 0)) def = 1;
                usc_edge_defect = TestResultToString(def);
            }
            rdr.Close();
            rdr.Dispose();
            cmd.Dispose();

            if (usc_edge_defect == "Пройдено") return true;


            //получение последних результатов перепроверки РУЗК
            String ruzk_edge_defect = "Нет данных";

            cmd = conn.CreateCommand();
            cmd.CommandText =
                @"select TEST_BRAK_AFTER_KROM
                    from usc_otdelka
                   where pipe_number = ?
                     and edit_state = 0
                     and workplace_id in (25, 26, 27, 28, 29, 30)
                     and first_rec_date >= nvl((select max(trx_date)
                                                    from inspection_pipes
                                                   where pipe_number = ? and edit_state = 0 and workplace_id = 7)
                                              , first_rec_date)
                order by first_rec_date desc ";

            cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);
            cmd.Parameters.AddWithValue("PIPE_NUMBER_INSP", PipeNumber);
            rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                ruzk_edge_defect = TestResultToString(rdr["TEST_BRAK_AFTER_KROM"]);
            }
            rdr.Close();
            rdr.Dispose();
            cmd.Dispose();


            //запрет приёмки, если имеется дефект РУЗК
            if (ruzk_edge_defect == "Не пройдено")
            {
                if (!IsPdoForm)
                    Master.AlertMessage = "Внимание!\n\nТруба не может быть принята, т.к. имееются дефекты РУЗК:\n"
                    + " - перепроверка УЗК кромок: " + ruzk_edge_defect + "\n";
                return false;
            }

            //запрет приёмки, если имеется дефект УЗК сварки и нет положительного результата перепроверки
            if (usc_edge_defect == "Не пройдено" && ruzk_edge_defect != "Пройдено")
            {
                if (!IsPdoForm)
                    Master.AlertMessage = "Внимание!\n\nТруба не может быть принята, т.к. имееются дефекты УЗК кромок без положительного результата перепроверки:\n"
                    + " - УЗК кромок: " + usc_edge_defect + "\n"
                    + " - результат перепроверки на РУЗК: " + ruzk_edge_defect + "\n";
                return false;
            }


            return true;

        }
        catch (Exception ex)
        {
            Master.AlertMessage = "Ошибка при проверке АУЗК кромок (код 77): " + ex.Message.ToString();
            return false;
        }

    }

    /// <summary>Проверка прохождения консервационного покрытия</summary>
    /// <returns></returns>
    protected bool CheckTransitKP()
    {
        try
        {
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            using (OleDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT kp.kp_fact
                                        FROM tesc3.kp_asutp_pipes kp
                                       WHERE kp.pipe_number = ?
                                             AND (SELECT COUNT (*)
                                                    FROM tesc3.termo_otdel_pipes_tesc3 tm
                                                   WHERE     tm.edit_state = 0
                                                         AND tm.pipenumber = ?
                                                         AND tm.trx_date > kp.rec_date) = 0
                                    ORDER BY kp.rec_date DESC";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("pipe_number_1", PipeNumber);
                cmd.Parameters.AddWithValue("pipe_number_2", PipeNumber);
                using (OleDbDataReader rdr = cmd.ExecuteReader())
                {
                    if (rdr.HasRows)
                    {
                        if (rdr.Read())
                            return rdr["kp_fact"].ToString() == "1";

                        rdr.Close();
                    }
                }

                return false;
            }
        }
        catch (Exception ex)
        {
            Master.AlertMessage = "Ошибка при проверке прохождения консервационного покрытия: " + ex.Message.ToString();
            return false;
        }
    }

    /// <summary>Получение значения кампании</summary>
    /// <param name="campaign_line_id">Идентификатор кампании</param>
    /// <param name="parameter_name">Имя параметра из БД</param>
    /// <returns></returns>
    private string GetCampaignValue(string campaign_line_id, string parameter_name)
    {
        try
        {
            if (campaign_line_id != "")
            {
                using (OleDbCommand cmd = Master.Connect.ORACLE_TESC3().CreateCommand())
                {
                    cmd.CommandText = @"SELECT " + parameter_name + " FROM tesc3.campaigns c WHERE c.edit_state = 0 AND c.campaign_line_id = ?";
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("campaign_line_id", campaign_line_id);
                    return cmd.ExecuteScalar().ToString();
                }
            }
        }
        catch (Exception ex)
        {
            Master.AlertMessage = "Ошибка получения параметра кампании: " + ex.Message.ToString();
        }
        return "";
    }


    /// <summary>Проверка полей рабочего центра «Установка по зачистке внутренней поверхности»</summary>
    /// <returns></returns>
    bool CheckScrapingFields()
    {
        try
        {
            double i;
            if (!cbScraping.Checked && txbAmounts.Text.ToString() == "")
            {
                Master.AlertMessage = " Поле 'Количество зачисток' обязательно для заполнения";
                return true;
            }
            if (ddlLocationScraping.SelectedItem.Value == "")
            {
                Master.AlertMessage = " Поле 'Расположение дефектов' обязательно для заполнения";
                return true;
            }
            if (txbMinWallWeld.Enabled)
            {
                if (txbMinWallWeld.Text.ToString() == "")
                {
                    Master.AlertMessage = " Поле 'Минимальная толщина стенки на шве в местах зачистки' обязательно для заполнения";
                    return true;
                }
                if (!Validation_Class(txbMinWallWeld.Text.ToString(), 2, 2))
                {
                    Master.AlertMessage = " Поле 'Минимальная толщина стенки на шве в местах зачистки' д.б. в формате 99.99";
                    return true;
                }
                i = Convert.ToDouble(txbMinWallWeld.Text.ToString().Trim().Replace('.', ','));
                if (i < 4 || i > 13)
                {
                    Master.AlertMessage = " Поле 'Минимальная толщина стенки на шве в местах зачистки' не в диапазоне 4-13";
                    return true;
                }
            }
            if (txbMinWallPipe.Enabled)
            {
                if (txbMinWallPipe.Text.ToString() == "")
                {
                    Master.AlertMessage = " Поле 'Минимальная толщина стенки в основном металле в местах зачистки' обязательно для заполнения";
                    return true;
                }
                if (!Validation_Class(txbMinWallPipe.Text.ToString(), 2, 2))
                {
                    Master.AlertMessage = " Поле Минимальная толщина стенки в основном металле в местах зачистки' д.б. в формате 99.99";
                    return true;
                }
                i = Convert.ToDouble(txbMinWallPipe.Text.ToString().Trim().Replace('.', ','));
                if (i < 4 || i > 13)
                {
                    Master.AlertMessage = " Поле 'Минимальная толщина стенки в основном металле в местах зачистки' не в диапазоне 4-13";
                    return true;
                }
            }
            if (ddlResultScraping.SelectedItem.Value == "")
            {
                Master.AlertMessage = " Поле 'Заключение после зачистки' обязательно для заполнения";
                return true;
            }
        }
        catch (Exception ex)
        {
            Master.AlertMessage = "Ошибка проверки зачистки внутренней поверхности " + ex.Message.ToString();
            return true;
        }
        return false;
    }


    //проверка толщин после зачистки
    bool CheckScrapingThickness()
    {
        try
        {
            string text_error = "";
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();

            //---проверка толщины стенки через справочник Дополнительные требования геометрических параметров
            using (OleDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText =
                    @"SELECT NVL (PI.THICKNESS , 0) + NVL (ss.limit_for_max, 0) factthickness_max,
                           NVL (PI.THICKNESS , 0) + NVL (ss.limit_for_min, 0)
                              factthickness_min
                      FROM TESC3.CAMPAIGNS cmp 
                      join TESC3.SPR_ADDITIONAL_GEOM_PARAMS ss
                      on SS.SETTINGS_ID=cmp.REQUIREMENT_ID
                      join oracle.V_T3_PIPE_ITEMS pi on (cmp.inventory_code=pi.nomer)
                     WHERE  CMP.EDIT_STATE=0 and SS.IS_ACTIVE=1 and SS.EDIT_STATE=0 and
                       CMP.CAMPAIGN_LINE_ID=? ";

                cmd.Parameters.AddWithValue("CAMPAIGN_LINE_ID", Checking.GetDbType(ddlCampaign.SelectedValue));
                using (OleDbDataReader rdr = cmd.ExecuteReader())
                {
                    if (rdr.HasRows)
                    {
                        if (rdr.Read())
                        {
                            //int precision = 0;
                            //int.TryParse(rdr["FACTTHICKNESS_ROUND"].ToString(), out precision);
                            decimal d_thickness_max = Convert.ToDecimal(rdr["FACTTHICKNESS_MAX"]);
                            decimal d_thickness_min = Convert.ToDecimal(rdr["FACTTHICKNESS_MIN"]);

                            if (txbMinWallWeld.Enabled)
                            {
                                decimal d_weld = Convert.ToDecimal(txbMinWallWeld.Text.ToString().Trim().Replace('.', ','));
                                if ((d_weld < d_thickness_min) || (d_weld > d_thickness_max))
                                    text_error = "Минимальная толщина стенки на шве в местах зачистки вне пределов : " + d_thickness_min.ToString() + "-" + d_thickness_max.ToString();
                            }
                            if (txbMinWallPipe.Enabled)
                            {
                                decimal d_pipe = Convert.ToDecimal(txbMinWallPipe.Text.ToString().Trim().Replace('.', ','));
                                if ((d_pipe < d_thickness_min) || (d_pipe > d_thickness_max))
                                    text_error = "Минимальная толщина стенки в основном металле в местах зачистки вне пределов : " + d_thickness_min.ToString() + "-" + d_thickness_max.ToString();
                            }

                        }
                        else
                        {
                            text_error = " Нет данных в Справочнике Дополнительные требования геометрических параметров!";
                        }
                        rdr.Close();

                        if (text_error != "")
                        {
                            Master.AlertMessage = text_error;
                            return true;
                        }

                        return false;
                    }

                }
            }
            //----------------------------------------------------------

            //----проверка толщины стенки через справочник уставок геометрических параметров 
            using (OleDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText =
                    @"SELECT NVL (ss.thickness_to, 0) + NVL (ss.limit_for_max, 0) factthickness_max,
                           NVL (ss.thickness_from, 0) + NVL (ss.limit_for_min, 0)
                              factthickness_min,
                           ss.factthickness_round
                      FROM spr_settings_geom_params ss
                     WHERE     ss.diam = ?
                           AND (? BETWEEN ss.thickness_from AND ss.thickness_to)
                           AND ss.ntd_id = ? ";
                cmd.Parameters.AddWithValue("diam", Checking.GetDbType(ddlDiam.Text));
                cmd.Parameters.AddWithValue("thickness", Checking.GetDbType(ddlThickness.Text));
                cmd.Parameters.AddWithValue("ntd_id", txbNTD.Text.ToString());
                using (OleDbDataReader rdr = cmd.ExecuteReader())
                {
                    if (rdr.HasRows)
                    {
                        if (rdr.Read())
                        {
                            int precision = 0;
                            int.TryParse(rdr["FACTTHICKNESS_ROUND"].ToString(), out precision);
                            decimal d_thickness_max = Convert.ToDecimal(rdr["FACTTHICKNESS_MAX"]);
                            decimal d_thickness_min = Convert.ToDecimal(rdr["FACTTHICKNESS_MIN"]);

                            if (txbMinWallWeld.Enabled)
                            {
                                decimal d_weld = Math.Round(Convert.ToDecimal(txbMinWallWeld.Text.ToString().Trim().Replace('.', ',')), precision);
                                if ((d_weld < d_thickness_min) || (d_weld > d_thickness_max))
                                    text_error = "Минимальная толщина стенки на шве в местах зачистки вне пределов : " + d_thickness_min.ToString() + "-" + d_thickness_max.ToString();
                            }
                            if (txbMinWallPipe.Enabled)
                            {
                                decimal d_pipe = Math.Round(Convert.ToDecimal(txbMinWallPipe.Text.ToString().Trim().Replace('.', ',')), precision);
                                if ((d_pipe < d_thickness_min) || (d_pipe > d_thickness_max))
                                    text_error = "Минимальная толщина стенки в основном металле в местах зачистки вне пределов : " + d_thickness_min.ToString() + "-" + d_thickness_max.ToString();
                            }
                        }
                        else
                        {
                            text_error = " Нет данных в Справочнике уставок геометрических параметров!";
                        }
                        rdr.Close();
                    }
                    else
                    {
                        text_error = " Нет данных в Справочнике уставок геометрических параметров!";
                    }
                }
            }

            if (text_error != "")
            {
                Master.AlertMessage = text_error;
                return true;
            }
        }
        catch (Exception ex)
        {
            Master.AlertMessage = "Ошибка проверки толщин зачистки внутренней поверхности " + ex.Message.ToString();
            return true;
        }
        return false;
    }

    //******************************************************************
    //**  Функция возвращает true если в строке txt содержится число  **
    //*   с количеством цифр в целой части <= whole и с количеством    *
    //*   цифр в дробной части <= fractional; целая и дробная части    *
    //**           числа разделяются точкой или запятой               **
    //******************************************************************
    // InterShop.Checking.cs
    public static bool Validation_Class(string txt, int whole, int fractional)
    {
        if ((txt.IndexOf("-") != -1) || (txt.IndexOf("+") != -1)) return false;
        if (fractional == 0)
        {   //***целое число***
            int tmpxi;
            txt = txt.Trim();
            if (!Int32.TryParse(txt, out tmpxi)) return false;
            tmpxi = txt.Length;
            return (tmpxi <= whole && tmpxi > 0);
        }
        else
        {  //***число с дробной частью***
            double tmpxd;
            txt = txt.Trim();
            txt = txt.Replace('.', ',');
            if (!Double.TryParse(txt, out tmpxd)) return false;
            int xwhole, xfractional, xtmp;
            xwhole = txt.IndexOf(",");
            xtmp = txt.Length;
            if (xwhole <= 0) xfractional = 0;
            else xfractional = xtmp - xwhole - 1;
            return ((xwhole == -1 && xtmp > 0 && xtmp <= whole) || (xwhole <= whole && xwhole > 0 && xfractional <= fractional && xfractional > 0));
        }
    }

    protected void ddlLocationScraping_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (ddlLocationScraping.SelectedIndex < 1 || ddlLocationScraping.SelectedItem.Text.ToUpper().Contains("ПО ТЕЛУ И ШВУ"))
        {
            txbMinWallWeld.Text = "";
            txbMinWallWeld.Enabled = true;
            txbMinWallPipe.Text = "";
            txbMinWallPipe.Enabled = true;
        }
        else
        {
            if (ddlLocationScraping.SelectedItem.Text.ToUpper().Contains("ПО ТЕЛУ"))
            {
                txbMinWallWeld.Text = "зачистка не производилась";
                txbMinWallWeld.Enabled = false;
                txbMinWallPipe.Text = "";
                txbMinWallPipe.Enabled = true;
            }

            if (ddlLocationScraping.SelectedItem.Text.ToUpper().Contains("ПО ШВУ"))
            {
                txbMinWallWeld.Text = "";
                txbMinWallWeld.Enabled = true;
                txbMinWallPipe.Text = "зачистка не производилась";
                txbMinWallPipe.Enabled = false;
            }
        }
    }


    /// <summary>
    /// проверка на настроечные трубы
    /// </summary>
    /// <param name="pipenumber">номер трубы</param>
    /// <returns>Возвращает true если труба имеет статус "настроечная" и указанный дефект имеет отметку «Является браком» </returns>
    private bool CheckSettingPipe(int pipenumber)
    {
        bool result = false, isSetting = false;
        if (WorkplaceId != 85 && ddlDefect.SelectedItem.Value.Trim() == "")
            return false;

        try
        {

            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            //выполняется проверка статуса трубы «настроечная» 
            using (OleDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"select IS_SETTING from TESC3.OPTIMAL_PIPES where PIPE_NUMBER=? ";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("PIPE_NUMBER", pipenumber);

                using (OleDbDataReader rdr = cmd.ExecuteReader())
                {
                    if (rdr.HasRows)
                    {
                        if (rdr.Read())
                        {
                            isSetting = rdr["IS_SETTING"].ToString() == "1";
                        }
                    }
                }
            }

            // если труба является настроечной, от выполняем проверку указанного дефекта
            //Если указанный дефект согласно классификатора дефектов  (справочник дефектов черной трубы)
            // имеет отметку «Является браком» на форме выводить диалоговое окно с предупреждающим сообщением 
            if (isSetting)
            {
                string ListDefects = ddlDefect.SelectedItem.Value.Trim();

                // если труба принимается на участке "Установка зачистки внутренней поверхности"
                if (WorkplaceId == 85)
                {
                    // если Дефект 2 присутствует, то добавляем его ID в список дефектов
                    if (ddlDefect2.SelectedItem.Value.Trim() != "")
                    {
                        if (string.IsNullOrEmpty(ListDefects))
                        {
                            ListDefects = ddlDefect2.SelectedItem.Value.Trim();
                        }
                        else
                        {
                            ListDefects += ", " + ddlDefect2.SelectedItem.Value.Trim();
                        }
                    }

                    // если Дефект 3 присутствует, то добавляем его ID в список дефектов
                    if (ddlDefect3.SelectedItem.Value.Trim() != "")
                    {
                        if (string.IsNullOrEmpty(ListDefects))
                        {
                            ListDefects = ddlDefect3.SelectedItem.Value.Trim();
                        }
                        else
                        {
                            ListDefects += ", " + ddlDefect3.SelectedItem.Value.Trim();
                        }
                    }
                }

                using (OleDbCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT IS_BRAK FROM TESC3.SPR_DEFECT WHERE IS_ACTIVE=1 AND IS_BRAK=1 and  ID in (" + ListDefects + ") ";

                    using (OleDbDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.HasRows)
                        {
                            result = true;
                        }
                    }
                }
            }
        }

        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка проверки статуса настроечной трубы", ex);
            AddWarning("Ошибка проверки статуса настроечной трубы", true, true);
            return false;
        }

        return result;
    }

    protected void btnPipeNum_Click(object sender, EventArgs e)
    {
        OleDbConnection conn = Master.Connect.ORACLE_TESC3();
        using (OleDbCommand cmd = conn.CreateCommand())
        {

            cmd.CommandText = @"select pipe_year, pipe_number, control_number from insp_scan_current where insp_number = ? ";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("INSP_NUMBER", WorkplaceId);

            using (OleDbDataReader rdr = cmd.ExecuteReader())
            {
                if (rdr.HasRows)
                {
                    if (rdr.Read())
                    {
                        txbYear.Text = rdr["PIPE_YEAR"].ToString();
                        txbPipeNumber.Text = rdr["PIPE_NUMBER"].ToString();
                        txbCheck.Text = rdr["CONTROL_NUMBER"].ToString();
                    }
                }
                else
                {
                    txbYear.Text = (DateTime.Today.Year % 100).ToString("D2");
                    txbPipeNumber.Text = "";
                    txbCheck.Text = "";
                }
            }
        }
    }
    /// <summary>
    /// Таймер на срабатывание функции раз в N - количество секунд
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void Timer_Tick1(object sender, EventArgs e)
    {
        lblLastScanned.Text = UpdateLabel();
    }

    /// <summary>
    /// Функция возвращает значение по инспекционной решетке о номере и дате последней отсканированной трубы
    /// </summary>
    /// <returns></returns>
    private string UpdateLabel()
    {
        string pipe_number = "";
        string result = "";
        OleDbConnection conn = Master.Connect.ORACLE_TESC3();
        using (OleDbCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"select pipe_year||pipe_number pipe_number, control_number, insert_date
                                from insp_scan_history
                                where insp_number = ?
                                and insert_date = (select max(insert_date) from insp_scan_history where insp_number = ?)";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("INSP_NUMBER", WorkplaceId);
            cmd.Parameters.AddWithValue("INSP_NUMBER1", WorkplaceId);
            using (OleDbDataReader rdr = cmd.ExecuteReader())
            {
                if (rdr.HasRows)
                    if (rdr.Read())
                    {
                        pipe_number = rdr["PIPE_NUMBER"].ToString();
                        result = "Последняя отсканированная труба: " + rdr["PIPE_NUMBER"] + " - " + rdr["CONTROL_NUMBER"] + " : " + rdr["INSERT_DATE"];
                    }
                    else
                        result = "";
            }

            if (WorkplaceId >= 1 && WorkplaceId <= 6)
            {
                /*Для ЗНИ RFC-192771 предусматриваются индексы от 1 до 6 по рабочим инспекционным площадкам*/
                cmd.CommandText = @"select trx_date
                                from tesc3.inspection_pipes
                                where edit_state = 0
                                    and workplace_id = ?
                                    and pipe_number = ?
                                    and trx_date = (select max(trx_date)
                                                    from tesc3.inspection_pipes
                                                    where edit_state = 0
                                                        and workplace_id = ?
                                                        and pipe_number = ?)";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("WORKPLACE_ID", WorkplaceId);
                cmd.Parameters.AddWithValue("PIPE_NUMBER", pipe_number);
                cmd.Parameters.AddWithValue("WORKPLACE_ID1", WorkplaceId);
                cmd.Parameters.AddWithValue("PIPE_NUMBER1", pipe_number);
                using (OleDbDataReader rdr1 = cmd.ExecuteReader())
                {
                    if (rdr1.HasRows)
                        if (rdr1.Read())
                        {
                            result += ". По данной трубей уже было совершено действие - " + rdr1["TRX_DATE"];
                        }
                }
            }
            return result;
        }
    }

    protected void cbAutoCampaign_OnCheckedChanged(object sender, EventArgs e)
    {
        if (cbAutoCampaign.Checked) Master.AlertMessage = "Внимание! Выбран автоматический режим назначения кампании";
        else Master.AlertMessage = "Внимание! Автоматический режим назначения кампании отключен";
    }
}