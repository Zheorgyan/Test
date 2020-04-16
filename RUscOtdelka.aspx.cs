using System;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Data.OleDb;
using System.Collections.Generic;
using System.Globalization;
using System.Drawing;


public partial class RUscOtdelka : Page
{
    //отключение отслеживания маршрутов
    const bool DEBUG_DISABLE_ROUTE_CONTROL = false;

    int pnl { get { object o = ViewState["_pnlVisibleInput"]; if (o == null) return 0; return (int)ViewState["_pnlVisibleInput"]; } set { ViewState["_pnlVisibleInput"] = value; } }
    //Действия осуществляемые при загрузке страницы
    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {
            Culture = "Ru-Ru";

            //проверка возможности доступа к странице РУЗК
            Authentification.CheckAnyAccess("R_UZK");

            //очистка старых сообщений об ошибках
            Master.ClearErrorMessages();

            //включение сохранения позиции полосок прокрутки
            Master.EnableSaveScrollPositions(this);

            if (!IsPostBack)
            {
                //заполнение выпадающих списков
                FillDropdownLists();
                RebuildSopList();

                //свойства элементов управления
                pnlConditions.Style["border-left"] = "1px solid gray";
                pnlConditions.Style["border-right"] = "1px solid gray";
                pnlReport.Style["border-left"] = "1px solid gray";
                pnlReport.Style["border-right"] = "1px solid gray";
                pnlReport.Style["border-bottom"] = "1px solid gray";
                pnlEditControls.Style["display"] = "none";
                txbYear.Text = DateTime.Now.ToString("yy");

                //выбор рабочего места из строки запроса
                object workplace = Request.Params["Workplace"];
                if (workplace != null)
                {
                    SelectDDLItemByValue(ddlWorkPlace, workplace.ToString());
                    ddlWorkPlace_SelectedIndexChanged(sender, e);
                }
            }

            //скрытие элементов управления на странице, если не выбрано рабочее место
            if (ddlWorkPlace.SelectedIndex == 0) mvMain.SetActiveView(vSelectWorkplace);

            if (IsPostBack & !Master.IsRefresh)
            {
                //обработка событий редактирования записей журнала калибровки
                String argument = Request["__EVENTARGUMENT"].ToString();
                String target = Request["__EVENTTARGET"].ToString();

                //аргументы: <дата_калибровки dd.mm.yyyy hh24:mi:ss>|<код_установки>            
                char[] sep = { '|' };
                String[] args = argument.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                String CalibrDate = "";
                String workplaceID = "";
                if (args.Length > 0) CalibrDate = args[0];
                if (args.Length > 1) workplaceID = args[1];

                if (target == "EDIT_RECORD") EditRecord(workplaceID, CalibrDate);          //редактирование записи по калибровке
                if (target == "DELETE_CALIBRATION_RECORD") DeleteCalibrationRecord(workplaceID, CalibrDate);  //удаление записи по калибровке
                if (target == "CONFIRM_EDIT") ConfirmEditRecord(workplaceID, CalibrDate);  //подтверждение изменений
                if (target == "CANCEL_EDIT") CancelEditRecord(workplaceID, CalibrDate);    //отмена редактирования записи
                if (target == "INPUT_PIPES") BeginInputPipes(workplaceID, CalibrDate);     //переход ко вводу труб
                if (target == "DEL_RECORD") DeleteRecordByPipe(argument);                        //удаление записи по испытанию трубы
                if (target == "EDIT_PIPE") BeginEditPipe(argument);         //редактирование записи по испытанию трубы
                if (target == "txbPipeNumber") GetPipeNumber(argument); // загрузка данных по сортаменту введенной трубы
            }

        }
        finally
        {
            //
        }
    }


    //удаление записи по испытанию трубы
    private void DeleteRecordByPipe(string row_id)
    {
        try
        {
            try
            {
                //проверка возможности удаления записи
                if (!CheckCanEditRecByPipe(row_id)) return;

                //удаление записи в отслеживании маршрутов
                DeleteLastPositionOfPipe(row_id);
                int CountPOnCalibration = CountPipeOnCalibration(row_id);
                //подключение к БД
                OleDbConnection conn = Master.Connect.ORACLE_TESC3();
                OleDbCommand cmd = conn.CreateCommand();
                cmd.Transaction = conn.BeginTransaction();

                try
                {
                    //пометка существующей записи как удаленной
                    cmd.CommandText = "update USC_OTDELKA set EDIT_STATE=2 where ROW_ID=? and EDIT_STATE=0";
                    cmd.Parameters.AddWithValue("ROW_ID", row_id);
                    cmd.ExecuteNonQuery();

                    //вставка записи-подтверждения удаления
                    cmd.CommandText = "insert into USC_OTDELKA (ROW_ID, PIPE_NUMBER, ORIGINAL_ROW_ID, OPERATOR_ID, EDIT_STATE) "
                        + "select to_char(SYSDATE, 'dd.MM.yyyy hh24:mi:ss')||'_'||to_char(PIPE_NUMBER), PIPE_NUMBER, ROW_ID, ?, 3 "
                        + "from USC_OTDELKA "
                        + "where EDIT_STATE=2 and ROW_ID=?";
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("OPERATOR_ID", Authentification.User.UserName);
                    cmd.Parameters.AddWithValue("ORIGINAL_ROW_ID", row_id);
                    cmd.ExecuteNonQuery();

                    if (CountPOnCalibration == 1)
                    {
                        String strDat = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                        //вставка записи если удалена последняя труба в калибровке
                        cmd.CommandText = @"insert into USC_OTDELKA (CALIBRATION_DATE, NTD_NAME, SOP_NAME, TECHCHART_NUMBER, CHANNEL1, CHANNEL2, CHANNEL3, CHANNEL4, CHANNEL5, CHANNEL6, CHANNEL7, CHANNEL8, 
                                            WORKPLACE_ID, OPERATOR_ID, ROW_ID, DIAMETER, THICKNESS) 
                        select CALIBRATION_DATE, NTD_NAME, SOP_NAME, TECHCHART_NUMBER, CHANNEL1, CHANNEL2, CHANNEL3, CHANNEL4, CHANNEL5, CHANNEL6, CHANNEL7, CHANNEL8, 
                                            WORKPLACE_ID, ?, ?||'_', DIAMETER, THICKNESS from USC_OTDELKA 
                        where EDIT_STATE=2 and ROW_ID=?";
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("OPERATOR_ID", Authentification.User.UserName);
                        cmd.Parameters.AddWithValue("ORIGINAL_ROW_ID", strDat);
                        cmd.Parameters.AddWithValue("ROW_ID", row_id);
                        cmd.ExecuteNonQuery();
                    }

                    //подтверждение транзакции
                    cmd.Transaction.Commit();
                }
                catch (Exception ex)
                {
                    //откат транзакции в случае ошибки
                    cmd.Transaction.Rollback();
                    throw ex;
                }

                cmd.Dispose();
            }
            catch (Exception ex)
            {
                Master.AddErrorMessage("Ошибка удаления записи", ex);
            }
        }
        finally
        {
            //
        }
    }

    // подсчитывает количество труб оствшихся в калибровке
    private int CountPipeOnCalibration(string rowId)
    {
        int result = 0;

        using (OleDbCommand cmd = Master.Connect.ORACLE_TESC3().CreateCommand())
        {
            cmd.CommandText = "  SELECT Count(*) as PIPES_COUNT FROM USC_OTDELKA WHERE  CALIBRATION_DATE=(select  CALIBRATION_DATE  FROM USC_OTDELKA where  ROW_ID=?) and edit_state=0";
            cmd.Parameters.AddWithValue("ROW_ID", rowId);
            using (OleDbDataReader rdr = cmd.ExecuteReader())
            {
                if (rdr.Read())
                {
                    int.TryParse(rdr["PIPES_COUNT"].ToString(), out result);

                }
            }
        }
        return result;
    }
    //Заполнение выпадающих списков
    private void FillDropdownLists()
    {
        try
        {
            //список рабочих мест
            try
            {
                if (ddlWorkPlace.Items.Count == 0)
                {
                    ddlWorkPlace.Items.Add("");
                    ddlWorkPlace.Items.Add(new ListItem("РУЗК инспекционной решетки 1", "25"));
                    ddlWorkPlace.Items.Add(new ListItem("РУЗК инспекционной решетки 2", "26"));
                    ddlWorkPlace.Items.Add(new ListItem("РУЗК инспекционной решетки 3", "27"));
                    ddlWorkPlace.Items.Add(new ListItem("РУЗК инспекционной решетки 4", "28"));
                    ddlWorkPlace.Items.Add(new ListItem("РУЗК инспекционной решетки 5", "29"));
                    ddlWorkPlace.Items.Add(new ListItem("РУЗК инспекционной решетки 6", "30"));
                    ddlWorkPlace.Items.Add(new ListItem("РУЗК участка ремонта 1", "89"));
                    ddlWorkPlace.Items.Add(new ListItem("РУЗК участка ремонта 2", "90"));
                }
            }
            catch (Exception ex)
            {
                Master.AddErrorMessage("Ошибка получения списка рабочих мест (FillDropDownLists)", ex);
            }

            //заполнение выпадающего списка диаметра трубы
            try
            {
                if (ddlDiam.Items.Count == 0)
                {
                    ddlDiam.Items.Add("");
                    OleDbConnection conn = Master.Connect.ORACLE_ORACLE();
                    OleDbCommand cmd = conn.CreateCommand();
                    cmd.CommandText = "select distinct diameter from v_t3_pipe_items where (org_id=127)and(SHOV is NULL) and diameter is not null order by diameter";
                    OleDbDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        ListItem item = new ListItem(Convert.ToDouble(reader["diameter"]).ToString());
                        ddlDiam.Items.Add(item);
                    }
                    reader.Close();
                    reader.Dispose();
                    cmd.Dispose();
                }
            }
            catch (Exception ex)
            {
                Master.AddErrorMessage("Ошибка получения списка диаметров труб (FillDropDownLists)", ex);
            }

            //заполнение выпадающего списка толщины стенки
            try
            {
                if (ddlThickness.Items.Count == 0)
                {
                    ddlThickness.Items.Add("");
                    OleDbConnection conn = Master.Connect.ORACLE_ORACLE();
                    OleDbCommand cmd = conn.CreateCommand();
                    cmd.CommandText = "select distinct thickness from v_t3_pipe_items where (org_id=127)and(SHOV is NULL) order by thickness";
                    OleDbDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        ddlThickness.Items.Add(Convert.ToDouble(reader["thickness"]).ToString());
                    }
                    reader.Close();
                    reader.Dispose();
                    cmd.Dispose();
                }
            }
            catch (Exception ex)
            {
                Master.AddErrorMessage("Ошибка получения списка толщины стенки (FillDropDownLists)", ex);
            }
        }
        finally
        {
            //
        }
    }



    //переход ко вводу номеров труб
    private void BeginInputPipes(String WorkplaceID, String CalibrDate)
    {
        try
        {
            try
            {
                //подключение к БД
                OleDbConnection conn = Master.Connect.ORACLE_TESC3();
                OleDbCommand cmd = conn.CreateCommand();

                //получение параметров калибровки и переход к экрану ввода труб
                cmd.CommandText = "select CALIBRATION_DATE, NTD_NAME, SOP_NAME, TECHCHART_NUMBER," +
                                  "CHANNEL1, CHANNEL2, CHANNEL3, CHANNEL4, CHANNEL5, CHANNEL6, CHANNEL7, CHANNEL8," +
                                  "DIAMETER, THICKNESS  from USC_OTDELKA where (CALIBRATION_DATE = to_date(?, 'dd.mm.yyyy HH24:mi:ss'))and(WORKPLACE_ID=?)and(EDIT_STATE=0)";
                cmd.Parameters.AddWithValue("CALIBRATION_DATE", CalibrDate);
                cmd.Parameters.AddWithValue("WORKPLACE_ID", WorkplaceID);
                OleDbDataReader rdr = cmd.ExecuteReader();
                if (rdr.Read())
                {
                    //сохранение параметров калибровки
                    this.CalibrationDate = Convert.ToDateTime(rdr["CALIBRATION_DATE"]).ToString("dd.MM.yyyy HH:mm:ss");
                    this.NTDName = rdr["NTD_NAME"].ToString();
                    this.SopName = rdr["SOP_NAME"].ToString();
                    this.TechChart = rdr["TECHCHART_NUMBER"].ToString();
                    String[] channels = this.ChannelSettings;
                    channels[0] = rdr["CHANNEL1"].ToString().Replace('.', ',');
                    channels[1] = rdr["CHANNEL2"].ToString().Replace('.', ',');
                    channels[2] = rdr["CHANNEL3"].ToString().Replace('.', ',');
                    channels[3] = rdr["CHANNEL4"].ToString().Replace('.', ',');
                    channels[4] = rdr["CHANNEL5"].ToString().Replace('.', ',');
                    channels[5] = rdr["CHANNEL6"].ToString().Replace('.', ',');
                    channels[6] = rdr["CHANNEL7"].ToString().Replace('.', ',');
                    channels[7] = rdr["CHANNEL8"].ToString().Replace('.', ',');
                    this.ChannelSettings = channels;
                    this.Diameter = rdr["DIAMETER"].ToString().Replace('.', ',');
                    this.Thickness = rdr["THICKNESS"].ToString().Replace('.', ',');

                    //переключение вида
                    txbPipeNumber.Text = "";
                    txbCheck.Text = "";
                    mvMain.SetActiveView(vPipes);
                    Master.FocusControl = txbPipeNumber.ID;
                    tblInfo.Visible = (tblInfo.Rows.Count > 1);

                }
                rdr.Close();
                rdr.Dispose();
                cmd.Dispose();
            }
            catch (Exception ex)
            {
                Master.AddErrorMessage("Ошибка  получения параметров калибровки", ex);
            }
        }
        finally
        {
            //
        }
    }


    //удаление записи в журнале калибровки
    private void DeleteCalibrationRecord(String WorkplaceID, String CalibrDate)
    {
        try
        {
            //подключение к БД
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();

            //пометка существующего набора записей как неактуальных
            cmd.CommandText = "update USC_OTDELKA set EDIT_STATE=2 where (CALIBRATION_DATE = to_date(?, 'dd.mm.yyyy HH24:mi:ss'))and(WORKPLACE_ID=?)and(EDIT_STATE=0)";
            cmd.Parameters.AddWithValue("CALIBRATION_DATE", CalibrDate);
            cmd.Parameters.AddWithValue("WORKPLACE_ID", WorkplaceID);
            cmd.ExecuteNonQuery();
            cmd.Dispose();

            //добавление записей, подтверждащих удаление
            OleDbCommand rdrcmd = conn.CreateCommand();
            rdrcmd.CommandText = "select * from USC_OTDELKA where (CALIBRATION_DATE = to_date(?, 'dd.mm.yyyy HH24:mi:ss'))and(WORKPLACE_ID=?)and(EDIT_STATE=2)";
            rdrcmd.Parameters.AddWithValue("CALIBRATION_DATE", CalibrDate);
            rdrcmd.Parameters.AddWithValue("WORKPLACE_ID", WorkplaceID);
            OleDbDataReader rdr = rdrcmd.ExecuteReader();
            while (rdr.Read())
            {
                cmd = conn.CreateCommand();
                cmd.CommandText = "insert into USC_OTDELKA (CALIBRATION_DATE, REC_DATE, PIPE_NUMBER, ORIGINAL_ROW_ID, OPERATOR_ID, EDIT_STATE)"
                                + "                 values (?,                SYSDATE,  ?,           ?,               ?,           3)";
                cmd.Parameters.AddWithValue("CALIBRATION_DATE", rdr["CALIBRATION_DATE"]);
                cmd.Parameters.AddWithValue("PIPE_NUMBER", rdr["PIPE_NUMBER"]);
                cmd.Parameters.AddWithValue("ORIGINAL_ROW_ID", rdr["ROW_ID"]);
                cmd.Parameters.AddWithValue("OPERATOR_ID", Authentification.User.UserName);
                cmd.ExecuteNonQuery();
                cmd.Dispose();
            }
            rdr.Close();
            rdr.Dispose();
            cmd.Dispose();
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage(ex.Message, ex);
        }
    }



    //отмена изменений в записи журнала калибровки
    private void CancelEditRecord(String WorkplaceID, String CalibrDate)
    {
        EditingLineID = "";

        //если запись вновь создана - удаление из таблицы
        if (IsNewRecord)
        {
            try
            {
                OleDbConnection conn = Master.Connect.ORACLE_TESC3();
                OleDbCommand cmd = conn.CreateCommand();
                cmd.CommandText = "delete from USC_OTDELKA where (CALIBRATION_DATE = to_date(?, 'dd.mm.yyyy HH24:mi:ss'))and(WORKPLACE_ID=?)and(EDIT_STATE=0)";
                cmd.Parameters.AddWithValue("CALIBRATION_DATE", CalibrDate);
                cmd.Parameters.AddWithValue("WORKPLACE_ID", WorkplaceID);
                cmd.ExecuteNonQuery();
                cmd.Dispose();
            }
            catch (Exception ex)
            {
                Master.AddErrorMessage("Ошибка удаления записи", ex);
            }
            IsNewRecord = false;
        }
    }


    //Подтверждение редактирования записи в журнале калибровки
    private void ConfirmEditRecord(String WorkplaceID, String CalibrDate)
    {
        try
        {
            //подключение к БД
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();

            //Исправление существующей записи
            if (IsNewRecord == false)
            {
                OleDbCommand cmd = conn.CreateCommand();
                cmd.CommandText = @"update USC_OTDELKA set 
                    NTD_NAME=?, SOP_NAME=?, TECHCHART_NUMBER=?,
                    CHANNEL1=?, CHANNEL2=?, CHANNEL3=?, CHANNEL4=?, CHANNEL5=?, CHANNEL6=?, CHANNEL7=?, CHANNEL8=?
                    where (CALIBRATION_DATE = to_date(?, 'dd.mm.yyyy HH24:mi:ss'))and(WORKPLACE_ID=?)and(EDIT_STATE=0)";
                cmd.Parameters.AddWithValue("NTD_NAME", ddlNtdName.SelectedItem.Text);
                cmd.Parameters.AddWithValue("SOP_NAME", GetSopNumber());
                cmd.Parameters.AddWithValue("TECHCHART_NUMBER", ddlTechChart.SelectedItem.Text);
                cmd.Parameters.AddWithValue("CHANNEL1", Checking.GetDbType(txbCh1.Text));
                cmd.Parameters.AddWithValue("CHANNEL2", Checking.GetDbType(txbCh2.Text));
                cmd.Parameters.AddWithValue("CHANNEL3", Checking.GetDbType(txbCh3.Text));
                cmd.Parameters.AddWithValue("CHANNEL4", Checking.GetDbType(txbCh4.Text));
                cmd.Parameters.AddWithValue("CHANNEL5", Checking.GetDbType(txbCh5.Text));
                cmd.Parameters.AddWithValue("CHANNEL6", Checking.GetDbType(txbCh6.Text));
                cmd.Parameters.AddWithValue("CHANNEL7", Checking.GetDbType(txbCh7.Text));
                cmd.Parameters.AddWithValue("CHANNEL8", Checking.GetDbType(txbCh8.Text));
                cmd.Parameters.AddWithValue("CALIBRATION_DATE", CalibrDate);
                cmd.Parameters.AddWithValue("WORKPLACE_ID", WorkplaceID);
                cmd.ExecuteNonQuery();
                cmd.Dispose();

                //обнуление идентификатора текущей редактируемой строки
                EditingLineID = "";
                IsNewRecord = false;
            }
            //Cоздание новой записи (не_редактирование существующей)
            else
            {
                //получение rowid строки
                OleDbCommand cmd = conn.CreateCommand();
                cmd.CommandText = "select rowid from USC_OTDELKA where (CALIBRATION_DATE = to_date(?, 'dd.mm.yyyy HH24:mi:ss'))and(WORKPLACE_ID=?)and(EDIT_STATE=0)";
                cmd.Parameters.AddWithValue("CALIBRATION_DATE", CalibrDate);
                cmd.Parameters.AddWithValue("WORKPLACE_ID", WorkplaceID);
                String rowid = "";
                OleDbDataReader rdr = cmd.ExecuteReader();
                if (rdr.Read()) rowid = rdr["rowid"].ToString();
                rdr.Close();
                rdr.Dispose();
                cmd.Dispose();

                //обновление полей строки
                cmd = conn.CreateCommand();
                cmd.Parameters.Clear();
                cmd.CommandText = "UPDATE USC_OTDELKA SET CALIBRATION_DATE=?, NTD_NAME=?, SOP_NAME=?, TECHCHART_NUMBER=?, "
                                + "CHANNEL1=?, CHANNEL2=?, CHANNEL3=?, CHANNEL4=?, CHANNEL5=?, CHANNEL6=?, CHANNEL7=?, CHANNEL8=?, "
                                + "REC_DATE=SYSDATE, WORKPLACE_ID=?, OPERATOR_ID=?, "
                                + "ROW_ID=to_char(SYSDATE, 'dd.mm.yyyy hh24:mi:ss')||'_', EDIT_STATE=0, DIAMETER=?, THICKNESS=? "
                                + "WHERE rowid=?";

                DateTimeFormatInfo dtfi = new DateTimeFormatInfo();
                dtfi.LongDatePattern = "dd.MM.yyyy HH:mm:ss";
                DateTime calibrDate = DateTime.MinValue;
                if (!DateTime.TryParseExact(txbCalibrDate.Text.Trim(), "dd.MM.yyyy HH:mm:ss", dtfi, DateTimeStyles.None, out calibrDate))
                {
                    throw new Exception("Неверный формат даты в поле \"Дата калибровки\"");
                }
                cmd.Parameters.AddWithValue("CALIBRATION_DATE", calibrDate);
                cmd.Parameters.AddWithValue("NTD_NAME", ddlNtdName.SelectedItem.Text);
                cmd.Parameters.AddWithValue("SOP_NAME", GetSopNumber());
                cmd.Parameters.AddWithValue("TECHCHART_NUMBER", ddlTechChart.SelectedItem.Text);
                cmd.Parameters.AddWithValue("CHANNEL1", Checking.GetDbType(txbCh1.Text));
                cmd.Parameters.AddWithValue("CHANNEL2", Checking.GetDbType(txbCh2.Text));
                cmd.Parameters.AddWithValue("CHANNEL3", Checking.GetDbType(txbCh3.Text));
                cmd.Parameters.AddWithValue("CHANNEL4", Checking.GetDbType(txbCh4.Text));
                cmd.Parameters.AddWithValue("CHANNEL5", Checking.GetDbType(txbCh5.Text));
                cmd.Parameters.AddWithValue("CHANNEL6", Checking.GetDbType(txbCh6.Text));
                cmd.Parameters.AddWithValue("CHANNEL7", Checking.GetDbType(txbCh7.Text));
                cmd.Parameters.AddWithValue("CHANNEL8", Checking.GetDbType(txbCh8.Text));
                cmd.Parameters.AddWithValue("WORKPLACE_ID", WorkplaceID);
                cmd.Parameters.AddWithValue("OPERATOR_ID", Authentification.User.UserName);
                cmd.Parameters.AddWithValue("DIAMETER", Checking.GetDbType(ddlDiam.SelectedItem.Text));
                cmd.Parameters.AddWithValue("THICKNESS", Checking.GetDbType(ddlThickness.SelectedItem.Text));
                cmd.Parameters.AddWithValue("rowid", rowid);

                cmd.ExecuteNonQuery();
                cmd.Dispose();

                //обнуление идентификатора текущей редактируемой строки
                EditingLineID = "";
                IsNewRecord = false;

                //переход к экрану ввода номеров труб
                BeginInputPipes(WorkplaceID, calibrDate.ToString("dd.MM.yyyy HH:mm:ss"));
            }
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage(ex.Message, ex);
        }
    }


    //Начало редактирования записи в журнале калибровки
    private void EditRecord(String WorkplaceID, String CalibrDate)
    {
        //сохранение ID записи калибровки
        EditingLineID = CalibrDate + "|" + WorkplaceID;

        //очистка старых значений в полях
        txbCalibrDate.Text = "";
        SetSopNumber("");
        txbCh1.Text = "";
        txbCh2.Text = "";
        txbCh3.Text = "";
        txbCh4.Text = "";
        txbCh5.Text = "";
        txbCh6.Text = "";
        txbCh7.Text = "";
        txbCh8.Text = "";
        ddlDiam.SelectedIndex = 0;
        ddlThickness.SelectedIndex = 0;

        //получение полей из БД
        try
        {
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "select * from USC_OTDELKA where (CALIBRATION_DATE = to_date(?, 'dd.mm.yyyy HH24:mi:ss'))and(WORKPLACE_ID=?)and(EDIT_STATE=0)";
            cmd.Parameters.AddWithValue("CALIBR_DATE", CalibrDate);
            cmd.Parameters.AddWithValue("WORKPLACE_ID", WorkplaceID);
            OleDbDataReader rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                txbCalibrDate.Text = Convert.ToDateTime(rdr["CALIBRATION_DATE"]).ToString("dd.MM.yyyy HH:mm:ss");

                SelectDDLItemByText(ddlNtdName, rdr["NTD_NAME"].ToString());
                if (ddlNtdName.Items.Count == 2)
                    ddlNtdName.SelectedIndex = 1;

                SetSopNumber(rdr["SOP_NAME"].ToString());

                SelectDDLItemByText(ddlTechChart, rdr["TECHCHART_NUMBER"].ToString());
                if (ddlTechChart.Items.Count == 2)
                    ddlTechChart.SelectedIndex = 1;

                txbCh1.Text = rdr["CHANNEL1"].ToString().Replace('.', ',');
                txbCh2.Text = rdr["CHANNEL2"].ToString().Replace('.', ',');
                txbCh3.Text = rdr["CHANNEL3"].ToString().Replace('.', ',');
                txbCh4.Text = rdr["CHANNEL4"].ToString().Replace('.', ',');
                txbCh5.Text = rdr["CHANNEL5"].ToString().Replace('.', ',');
                txbCh6.Text = rdr["CHANNEL6"].ToString().Replace('.', ',');
                txbCh7.Text = rdr["CHANNEL7"].ToString().Replace('.', ',');
                txbCh8.Text = rdr["CHANNEL8"].ToString().Replace('.', ',');
                SelectDDLItemByText(ddlDiam, rdr["DIAMETER"].ToString().Replace('.', ','));
                SelectDDLItemByText(ddlThickness, rdr["THICKNESS"].ToString().Replace('.', ','));
            }
            rdr.Close();
            rdr.Dispose();
            cmd.Dispose();
            RebuildSopList();
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка получения записи из БД", ex);
        }
    }


    //создание новой записи по параметрам калибровки
    protected void btnNewCalibrParams_Click(object sender, EventArgs e)
    {
        try
        {
            //подтверждение редактирования предыдущей строки
            if (EditingLineID != "")
            {
                char[] sep = { '|' };
                String[] args = EditingLineID.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                String CalibrDate = "";
                String Workplace_ID = "";
                if (args.Length > 0) CalibrDate = args[0];
                if (args.Length > 0) Workplace_ID = args[1];
                ConfirmEditRecord(Workplace_ID, CalibrDate);
            }

            //добавление строки в БД (без номера трубы)
            String strDat = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "insert into USC_OTDELKA (CALIBRATION_DATE, WORKPLACE_ID, ROW_ID, OPERATOR_ID, EDIT_STATE) "
                            + "values (to_date(?, 'dd.mm.yyyy hh24:mi:ss'), ?, ?||'_', ?, 0)";
            cmd.Parameters.AddWithValue("CALIBRATION_DATE", strDat);
            cmd.Parameters.AddWithValue("WORKPLACE_ID", WorkplaceID);
            cmd.Parameters.AddWithValue("ROW_ID_DATE", strDat);
            cmd.Parameters.AddWithValue("OPERATOR_ID", Authentification.User.UserName);
            cmd.ExecuteNonQuery();
            cmd.Dispose();

            //переход к редактированию строки
            IsNewRecord = true;
            EditRecord(WorkplaceID.ToString(), strDat);
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка создания записи", ex);
        }
    }


    //обработчик нажатия клавиши "За смену"
    protected void btnZaSmeny_Click(object sender, EventArgs e)
    {
        mvConditions.SetActiveView(vZaSmeny);
        DeactivateTab(tdZaPeriod, btnZaPeriod);
        ActivateTab(tdZaSmeny, btnZaSmeny);
    }

    //обработчик нажатия клавиши "За период"
    protected void btnZaPeriod_Click(object sender, EventArgs e)
    {
        mvConditions.SetActiveView(vZaPeriod);
        DeactivateTab(tdZaSmeny, btnZaSmeny);
        ActivateTab(tdZaPeriod, btnZaPeriod);
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

    //Обработчик нажатия клавиши "Отобразить данные"
    protected void ApplyCondition_Click(object sender, EventArgs e)
    {
        //
    }

    //Обработчик нажатия клавиши "?"
    protected void lbtHelpCheck_Click(object sender, EventArgs e)
    {
        int PipeNumber = 0;
        int Year = 0;
        if (Int32.TryParse(txbPipeNumber.Text, out PipeNumber) & Int32.TryParse(txbYear.Text, out Year))
        {
            mvPipes.SetActiveView(vGetChekingNum);
            txbLogin.Text = "";
            txbPassword.Text = "";
            lblNumtrub.Text = txbYear.Text + txbPipeNumber.Text.PadLeft(6, '0');
        }
        else Master.AddErrorMessage(Messages.GetByCode(302) + ": Год или № Трубы");
    }

    //проверка именя пользователя/пароля для осуществления вычисления контрольной цифры
    protected void btnLogin_Click(object sender, EventArgs e)
    {
        String pass = txbPassword.Text;
        if (((pass == "aspenadmin") | (pass == "12345678")) & txbLogin.Text != "")
        {
            txbPipeNumber.Text = txbPipeNumber.Text.Trim().PadLeft(6, '0');
            txbCheck.Text = Checking.Check_Class(txbYear.Text + txbPipeNumber.Text).ToString();
            BadMarkingPipeNumber = Convert.ToInt32(txbYear.Text + txbPipeNumber.Text);
            mvPipes.SetActiveView(vContExtTable);
            Master.FocusControl = txbCheck.ID;
            GetPipeNumber(txbYear.Text + txbPipeNumber.Text);
        }
        else Master.AddErrorMessage(Messages.GetByCode(401));
    }

    //возврат к списку труб из панели ввода логина/пароля для запроса контрольной цифры
    protected void btnCansel_Click(object sender, EventArgs e)
    {
        Master.FocusControl = txbCheck.ID;
        mvPipes.SetActiveView(vContExtTable);
    }

    //обработчик нажатия кнопки для возврата к просмотру списка партий
    protected void btnBackToPartsList_Click(object sender, EventArgs e)
    {
        mvMain.SetActiveView(vParts);
    }


    //обновление таблицы журнала калибровки
    protected void tblSettingsList_PreRender(object sender, EventArgs e)
    {
        RebuildCalibrationList();
    }



    //обновление таблицы журнала калибровки
    private void RebuildCalibrationList()
    {
        //удаление старых строк таблицы
        while (tblSettingsList.Rows.Count > 2)
            tblSettingsList.Rows.RemoveAt(2);


        //интервал времени
        DateTime dat1 = new DateTime();
        DateTime dat2 = new DateTime();
        GetTimeInterval(out dat1, out dat2);

        //выборка данных из БД (с группировкой только по параметрам калибровки)
        OleDbConnection conn = Master.Connect.ORACLE_TESC3();
        OleDbCommand cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT distinct CALIBRATION_DATE, NTD_NAME, SOP_NAME, TECHCHART_NUMBER, DIAMETER, THICKNESS, CHANNEL1, CHANNEL2, CHANNEL3, "
                        + "CHANNEL4, CHANNEL5, CHANNEL6, CHANNEL7, CHANNEL8, WORKPLACE_ID FROM USC_OTDELKA "
                        + "WHERE (WORKPLACE_ID=?)and(CALIBRATION_DATE>=?)and(CALIBRATION_DATE<?)and(EDIT_STATE=0) "
                        + "ORDER BY CALIBRATION_DATE";
        cmd.Parameters.Add("WORKPLACE_ID", OleDbType.BigInt).Value = WorkplaceID;
        cmd.Parameters.Add("DATE1", OleDbType.DBTimeStamp).Value = dat1;
        cmd.Parameters.Add("DATE2", OleDbType.DBTimeStamp).Value = dat2;
        OleDbDataReader rdr = cmd.ExecuteReader();
        DateTime maxDateCalibration = GetMaxDateCalibration(WorkplaceID);
        TableRow row;
        TableCell cell;
        int LineIndex = 0;
        while (rdr.Read())
        {
            row = new TableRow();

            //дата и смена калибровки
            DateTime dat = Convert.ToDateTime(rdr["CALIBRATION_DATE"]);
            cell = new TableCell(); cell.Text = Convert.ToDateTime(dat.ToString("dd.MM.yyyy HH:mm")) + " (" + Authentification.Shift + ")";
            cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);
            //наименование НТД
            cell = new TableCell(); cell.Text = rdr["NTD_NAME"].ToString();
            cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);
            //номер эталонного образца
            cell = new TableCell(); cell.Text = rdr["SOP_NAME"].ToString();
            cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);
            //номер технологической карты
            cell = new TableCell(); cell.Text = rdr["TECHCHART_NUMBER"].ToString();
            cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);
            //параеметры калибровки (по каналам)
            cell = new TableCell(); cell.Text = rdr["CHANNEL1"].ToString().Replace('.', ','); cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);
            cell = new TableCell(); cell.Text = rdr["CHANNEL2"].ToString().Replace('.', ','); cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);
            cell = new TableCell(); cell.Text = rdr["CHANNEL3"].ToString().Replace('.', ','); cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);
            cell = new TableCell(); cell.Text = rdr["CHANNEL4"].ToString().Replace('.', ','); cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);
            cell = new TableCell(); cell.Text = rdr["CHANNEL5"].ToString().Replace('.', ','); cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);
            cell = new TableCell(); cell.Text = rdr["CHANNEL6"].ToString().Replace('.', ','); cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);
            cell = new TableCell(); cell.Text = rdr["CHANNEL7"].ToString().Replace('.', ','); cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);
            cell = new TableCell(); cell.Text = rdr["CHANNEL8"].ToString().Replace('.', ','); cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);
            //диаметр и толщина стенки
            cell = new TableCell(); cell.Text = rdr["DIAMETER"].ToString().Replace('.', ',') + " x " + rdr["THICKNESS"].ToString().Replace('.', ',');
            cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);
            //кол-во труб
            int GoodPipesCount = 0;
            int BadPipesCount = 0;
            GetPipesCount(rdr["CALIBRATION_DATE"], WorkplaceID, out GoodPipesCount, out BadPipesCount);
            int pipes_count = GoodPipesCount + BadPipesCount;
            cell = new TableCell(); cell.Text = pipes_count.ToString(); cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);

            //идентификация строки
            String id = dat.ToString("dd.MM.yyyy HH:mm:ss") + "|" + rdr["WORKPLACE_ID"].ToString();
            bool bLineEdit = (EditingLineID == id);

            //ссылки на редактирование
            if (!bLineEdit & maxDateCalibration == dat & GoodPipesCount == 0 & BadPipesCount == 0)
            {
                //ссылка "редактировать строку"
                String EditLnk = "<a title='Редактировать параметры' href=\"javascript: __doPostBack('EDIT_RECORD', '" + id + "')\"><img src='Images/Edit16x16.png' style=\"color: Black; text-decoration: none; border-style: none;\" /></a>";
                //ссылка "удалить cтроку"
                String DelLnk = "<a title='Удалить запись' href=\"javascript: if(confirm('Нажмите ОК для подтверждения удаления строки. Будут удалены ВСЕ записи по трубам с данными параметрами калибровки.')) __doPostBack('DELETE_CALIBRATION_RECORD', '" + id + "')\"><img src='Images/Delete16x16.png' style=\"color: Black; text-decoration: none; border-style: none;\"/></a>";
                cell = new TableCell(); cell.Text = EditLnk + "&nbsp;" + DelLnk; cell.Width = 40; cell.HorizontalAlign = HorizontalAlign.Center;
                row.Cells.Add(cell);
            }
            else
            {
                cell = new TableCell(); cell.Text = ""; cell.Width = 40; cell.HorizontalAlign = HorizontalAlign.Center;
                row.Cells.Add(cell);
            }

            //ссылки на подтверждение или отмену редактирования
            if (bLineEdit)
            {
                //ссылка "подтвердить изменения"
                String ConfirmLink = "<a title='Подтвердить изменения' href=\"javascript: if(CheckLineInputs()) __doPostBack('CONFIRM_EDIT', '" + id + "')\"><img src='Images/Confirm16x16.png' style=\"color: Black; text-decoration: none; border-style: none;\" /></a>";
                //ссылка "отменить"
                String CancelLnk = "<a title='Отменить изменения' href=\"javascript: __doPostBack('CANCEL_EDIT', '" + id + "')\"><img src='Images/Cancel16x16.png' style=\"color: Black; text-decoration: none; border-style: none;\"/></a>";
                cell = new TableCell(); cell.Text = ConfirmLink + "&nbsp;" + CancelLnk; cell.Width = 40; cell.HorizontalAlign = HorizontalAlign.Center;
                row.Cells.Add(cell);
            }

            //выделение активной строки цветом и жирной границей
            if (bLineEdit)
            {
                for (int c = 0; c < row.Cells.Count; c++)
                {
                    cell = row.Cells[c];
                    cell.Style["border-top"] = "1px Solid Black";
                    cell.Style["border-bottom"] = "1px Solid Black";
                    if (c == 0) cell.Style["border-left"] = "1px Solid Black";
                    if (c == (row.Cells.Count - 1)) cell.Style["border-right"] = "1px Solid Black";
                }
                row.Style[HtmlTextWriterStyle.BackgroundColor] = "#E6E6F0";
            }
            //вставка элементов редактирования строки на страницу
            if (bLineEdit)
            {
                //удаление значений из ячеек
                for (int c = 0; c < row.Cells.Count - 1; c++) row.Cells[c].Text = "";

                //добавление скрипта для размещения элементов управления в ячейках
                String script = "<script type=\"text/javascript\" language=\"javascript\"> "
                             + "function OnDocumentLoad(e) { InsertActEditControls(@R); RestoreScrollPositions(); }; "
                             + "window.onload=OnDocumentLoad;"
                             + "</script>";
                script = script.Replace("@R", (LineIndex + 2).ToString());
                RegisterStartupScript("edit_controls_script", script);
            }

            //скрипт для перехода ко вводу труб по клику на строке
            if (!bLineEdit & maxDateCalibration == dat)
            {
                String onclick = "__doPostBack('INPUT_PIPES', '" + id + "')";
                String onmouseleave = "this.style.backgroundColor=''";
                String onmouseover = "this.style.backgroundColor='#E6E6F0'";
                row.Attributes["onclick"] = onclick;
                row.Attributes["onmouseover"] = onmouseover;
                row.Attributes["onmouseleave"] = onmouseleave;
            }

            //добавление строки
            LineIndex++;
            tblSettingsList.Rows.Add(row);
        }
        rdr.Close();
        rdr.Dispose();
        cmd.Dispose();
    }

    // возвращает максимальную даку калибровки для выбранного рабочего места
    private DateTime GetMaxDateCalibration(int workplaceId)
    {
        DateTime result = DateTime.Now;
        DateTime dat1 = new DateTime();
        DateTime dat2 = new DateTime();
        GetTimeInterval(out dat1, out dat2);

        using (OleDbCommand cmd = Master.Connect.ORACLE_TESC3().CreateCommand())
        {
            cmd.CommandText = "SELECT nvl(Max(CALIBRATION_DATE), sysdate) as CALIBRATION_DATE FROM USC_OTDELKA WHERE (WORKPLACE_ID=?) and (EDIT_STATE=0) and (CALIBRATION_DATE>=?)and(CALIBRATION_DATE<?) ";
            cmd.Parameters.Add("WORKPLACE_ID", OleDbType.BigInt).Value = WorkplaceID;
            cmd.Parameters.Add("DATE1", OleDbType.DBTimeStamp).Value = dat1;
            cmd.Parameters.Add("DATE2", OleDbType.DBTimeStamp).Value = dat2;
            using (OleDbDataReader rdr = cmd.ExecuteReader())
            {
                if (rdr.Read())
                {
                    try
                    {
                        result = Convert.ToDateTime(rdr["CALIBRATION_DATE"].ToString());
                    }
                    catch
                    { }
                }
            }
        }
        return result;
    }

    //подсчет кол-ва труб с заданными параметрами калибровки
    private void GetPipesCount(object CalibrationDate, int WorkplaceID, out int GoodPipesCount, out int BadPipesCount)
    {
        //подключение к БД
        OleDbConnection conn = Master.Connect.ORACLE_TESC3();
        OleDbCommand cmd = conn.CreateCommand();

        //кол-во труб, отбракованных автоматически/вручную или с неопределенным статусом
        cmd.CommandText = "select count(*) from USC_OTDELKA where (workplace_id=?)and(edit_state=0)and(PIPE_NUMBER is not NULL)and(CALIBRATION_DATE=?)"
                        + "and( ((TEST_BRAK_AUTO=1)and(TEST_BRAK_MANUAL is NULL)) or (TEST_BRAK_MANUAL=1) or ((TEST_BRAK_AUTO is NULL)and(TEST_BRAK_MANUAL is NULL)) )";
        cmd.Parameters.AddWithValue("workplace_id", WorkplaceID);
        cmd.Parameters.AddWithValue("calibration_date", CalibrationDate);
        BadPipesCount = Convert.ToInt32(cmd.ExecuteScalar());

        //кол-во труб, проверенных автоматически или с определенным вручную статусом
        cmd.CommandText = "select count(*) from USC_OTDELKA where (workplace_id=?)and(edit_state=0)and(PIPE_NUMBER is not NULL)and(CALIBRATION_DATE=?)"
                        + "and( ((TEST_BRAK_AUTO=0)and(TEST_BRAK_MANUAL is NULL)) or (TEST_BRAK_MANUAL=0) )";
        cmd.Parameters.Clear();
        cmd.Parameters.AddWithValue("workplace_id", WorkplaceID);
        cmd.Parameters.AddWithValue("calibration_date", CalibrationDate);
        GoodPipesCount = Convert.ToInt32(cmd.ExecuteScalar());

        cmd.Dispose();
        return;
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


    //получение текущего интервала времени для журнала
    private void GetTimeInterval(out DateTime dat1, out DateTime dat2)
    {
        //за смену
        if (mvConditions.GetActiveView() == vZaSmeny)
        {
            dat1 = navByShift.StartDate;
            dat2 = navByShift.EndDate;
        }
        //за период
        else
        {
            dat1 = navByPeriod.StartDate;
            dat2 = navByPeriod.EndDate;
        }
    }


    #region Свойства, сохраняемые в ViewState

    //дата калибровки
    String CalibrationDate
    {
        get
        {
            return tblCurrentSettings.Rows[2].Cells[0].Text;
        }
        set
        {
            tblCurrentSettings.Rows[2].Cells[0].Text = value;
        }
    }

    //наименование НТД
    String NTDName
    {
        get
        {
            return tblCurrentSettings.Rows[2].Cells[1].Text;
        }
        set
        {
            tblCurrentSettings.Rows[2].Cells[1].Text = value;
        }
    }


    //номер эталонного образца
    String SopName
    {
        get
        {
            return tblCurrentSettings.Rows[2].Cells[2].Text;
        }
        set
        {
            tblCurrentSettings.Rows[2].Cells[2].Text = value;
        }
    }

    //номер технологической карты
    String TechChart
    {
        get
        {
            return tblCurrentSettings.Rows[2].Cells[3].Text;
        }
        set
        {
            tblCurrentSettings.Rows[2].Cells[3].Text = value;
        }
    }

    //параметры калибровки по каналам 1-8
    String[] ChannelSettings
    {
        get
        {
            String[] res = new String[8];
            for (int i = 0; i < 8; i++)
                res[i] = tblCurrentSettings.Rows[2].Cells[4 + i].Text;
            return res;
        }
        set
        {
            for (int i = 0; i < 8; i++)
                tblCurrentSettings.Rows[2].Cells[4 + i].Text = value[i];
        }
    }

    //диаметр
    String Diameter
    {
        get
        {
            char[] sep = { 'x' };
            String[] sort = tblCurrentSettings.Rows[2].Cells[12].Text.Split(sep, StringSplitOptions.RemoveEmptyEntries);
            String Diam = "";
            String Thick = "";
            if (sort.Length > 0) Diam = sort[0].Trim();
            if (sort.Length > 1) Thick = sort[1].Trim();
            return Diam;
        }
        set
        {
            tblCurrentSettings.Rows[2].Cells[12].Text = value + " x " + Thickness;
        }
    }

    //толщина стенки
    String Thickness
    {
        get
        {
            char[] sep = { 'x' };
            String[] sort = tblCurrentSettings.Rows[2].Cells[12].Text.Split(sep, StringSplitOptions.RemoveEmptyEntries);
            String Diam = "";
            String Thick = "";
            if (sort.Length > 0) Diam = sort[0].Trim();
            if (sort.Length > 1) Thick = sort[1].Trim();
            return Thick;
        }
        set
        {
            tblCurrentSettings.Rows[2].Cells[12].Text = Diameter + " x " + value;
        }
    }

    //код рабочего места
    int WorkplaceID
    {
        get
        {
            try
            {
                int res = 0;
                if (Int32.TryParse(ddlWorkPlace.SelectedItem.Value, out res)) return res;
                else return -1;
            }
            catch { return -1; };
        }
        set
        {
            SelectDDLItemByValue(ddlWorkPlace, value.ToString());
        }
    }


    //идентификация редактируемой строки
    String EditingLineID
    {
        get
        {
            object o = ViewState["EditingLineID"];
            if (o == null) return ""; else return o.ToString();
        }
        set
        {
            ViewState["EditingLineID"] = value;
        }
    }

    //rowid редактируемой записи по трубе
    String SelectedRowId
    {
        get
        {
            object o = ViewState["SelectedRowId"];
            if (o == null) return ""; else return o.ToString();
        }
        set
        {
            ViewState["SelectedRowId"] = value;
        }
    }


    //признак создания новой записи журнала калибровки
    //(если false-редактируется ранее существовавшая запись)
    bool IsNewRecord
    {
        get
        {
            object o = ViewState["IsNewRecord"];
            if (o == null) return false; else return Convert.ToBoolean(o);
        }
        set
        {
            ViewState["IsNewRecord"] = value;
        }
    }


    //номер последней трубы с нечитаемой маркировкой
    int BadMarkingPipeNumber
    {
        get
        {
            object o = ViewState["BadMarkingPipeNumber"];
            if (o == null) return -1; else return Convert.ToInt32(o);
        }
        set
        {
            ViewState["BadMarkingPipeNumber"] = value;
        }
    }

    #endregion


    //обновление страницы при изменении рабочего места
    protected void ddlWorkPlace_SelectedIndexChanged(object sender, EventArgs e)
    {
        //переключение вида
        mvMain.SetActiveView(vParts);
        EditingLineID = "";
        if (sender == ddlWorkPlace) Page_Load(sender, e);

        //построение списка НТД
        try
        {
            RebuildNtdList();
        }
        catch { }

        //построение списка технкарты
        try
        {
            RebuildTechChart();
        }
        catch { }
    }



    /// <summary>
    /// Заполнение списка НТД
    /// </summary>
    protected void RebuildNtdList()
    {
        ddlNtdName.Items.Clear();
        ddlNtdName.Items.Add("");
        using (OleDbCommand cmd = Master.Connect.ORACLE_TESC3().CreateCommand())
        {
            cmd.CommandText = "SELECT ntd_name FROM spr_snk_ntd WHERE workplace_id = ? AND is_active = 1 ORDER BY ntd_name";
            cmd.Parameters.AddWithValue("workplace_id", WorkplaceID);
            using (OleDbDataReader rdr = cmd.ExecuteReader())
            {
                if (rdr.HasRows)
                {
                    while (rdr.Read())
                    {
                        ddlNtdName.Items.Add(rdr["ntd_name"].ToString());
                    }
                    rdr.Close();
                }
            }
        }
    }

    /// <summary>
    /// Заполнение списка технологической карты
    /// </summary>
    protected void RebuildTechChart()
    {
        ddlTechChart.Items.Clear();
        ddlTechChart.Items.Add("");
        using (OleDbCommand cmd = Master.Connect.ORACLE_TESC3().CreateCommand())
        {
            cmd.CommandText = "SELECT TECHCHART_NUMBER FROM spr_tech_chart_snk WHERE workplace_id = ? AND is_active = 1 ORDER BY TECHCHART_NUMBER";
            cmd.Parameters.AddWithValue("workplace_id", WorkplaceID);
            using (OleDbDataReader rdr = cmd.ExecuteReader())
            {
                if (rdr.HasRows)
                {
                    while (rdr.Read())
                    {
                        ddlTechChart.Items.Add(rdr["techchart_number"].ToString());
                    }
                    rdr.Close();
                }
            }
        }
    }


    //перестроение списка испытательных образцов
    protected void RebuildSopList()
    {
        try
        {
            String prevSop = "";
            if (ddlSop.SelectedIndex >= 0) prevSop = ddlSop.SelectedItem.Text;

            ddlSop.Items.Clear();
            ddlSop.Items.Add("");
            try
            {
                //параметры выборки
                //диаметр (округлен до меньшего целого)            
                double diam = -1;
                if (ddlDiam.SelectedItem.Text != "")
                    diam = Math.Truncate(Convert.ToDouble(ddlDiam.SelectedItem.Text));
                //толщина стенки
                double thickness = -1;
                if (ddlThickness.SelectedItem.Text != "")
                    thickness = Convert.ToDouble(ddlThickness.SelectedItem.Text);
                //номер рабочего места
                int workplace_id = -1;
                if (WorkplaceID == 13) workplace_id = 13;
                if (WorkplaceID == 14) workplace_id = 13;
                if ((WorkplaceID == 17) | (WorkplaceID == 17)) workplace_id = 16;

                //запрос на выборку из справочника
                String SQL = "";
                if (diam != -1) SQL = "(DIAM=?)";
                if (thickness != -1) SQL += "(THICKNESS=?)";
                if (workplace_id != -1) SQL += "(AGREGAT=?)";
                SQL = SQL.Replace(")(", ")AND(");
                if (SQL != "") SQL = " AND " + SQL;
                OleDbConnection conn = Master.Connect.ORACLE_TESC3();
                using (OleDbCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT sop_number FROM spr_usc_sop WHERE is_active = 1 " + SQL + " ORDER BY sop_number";
                    //параметры запроса
                    if (diam != -1) cmd.Parameters.AddWithValue("DIAM", diam);
                    if (thickness != -1) cmd.Parameters.AddWithValue("THICKNESS", thickness);
                    if (workplace_id != -1) cmd.Parameters.AddWithValue("AGREGAT", workplace_id);

                    //заполнение выпадающего списка
                    using (OleDbDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.HasRows)
                        {
                            while (rdr.Read())
                            {
                                ddlSop.Items.Add(rdr["sop_number"].ToString());
                            }
                            rdr.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Master.AddErrorMessage("Ошибка получения списка испытательных образцов", ex);
            }
            ddlSop.Items.Add("Другой");

            //восстановление выбора элемента из списка
            if (prevSop != "") SelectDDLItemByText(ddlSop, prevSop);
            if ((ddlSop.Items.Count > 2) & (ddlSop.SelectedItem.Text != "Другой"))
            {
                txbSop.Text = "";
                txbSop.Visible = false;
            }
        }
        finally
        {
            //
        }
    }


    //перестроение списка испытательных образцов при изменении диаметра и стенки
    protected void ddlDiam_SelectedIndexChanged(object sender, EventArgs e)
    {
        RebuildSopList();
    }
    protected void ddlThickness_SelectedIndexChanged(object sender, EventArgs e)
    {
        RebuildSopList();
    }


    //установка номера испытательного образца 
    //при редактировании параметров калибровки
    protected void SetSopNumber(String SopNumber)
    {
        if (ddlSop.Items.FindByText(SopNumber) != null)
        {
            SelectDDLItemByText(ddlSop, SopNumber);
            txbSop.Text = "";
        }
        else
        {
            SelectDDLItemByText(ddlSop, "Другой");
            txbSop.Text = SopNumber;
        }
        txbSop.Visible = (txbSop.Text != "");
    }

    //получение текущего номера испытательного образца
    //при редактировании параметров калибровки
    protected String GetSopNumber()
    {
        if (txbSop.Text != "") return txbSop.Text;
        if (ddlSop.SelectedIndex >= 0) return ddlSop.SelectedItem.Text;
        return "";
    }


    //выбор номера испытательного образца из выпадающего списка
    protected void ddlSop_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (ddlSop.SelectedItem.Text == "Другой")
        {
            txbSop.Visible = true;
        }
        else
        {
            txbSop.Text = "";
            txbSop.Visible = false;
        }

    }

    protected void btnCloseInputPipe_Click(object sender, EventArgs e)
    {
        pnlInputPipe.Visible = false;
        txbYear.Text = DateTime.Now.ToString("yy");
        txbPipeNumber.Text = "";
        txbCheck.Text = "";
    }

    //Задача трубы на испытание по нажатию "ввести данные"
    protected void btnOk_Click(object sender, EventArgs e)
    {
        try
        {
            //проверка возможности ввода данных согласно маршрутной карте
            if (!CheckValidCurrentPositionOfPipe()) return;

            PopupWindow1.MoveToCenter();
            string target = "INPUT_PIPE";
            Session.Add("WorkplaceIDForRusc", WorkplaceID);
            Session.Add("PipeYearForRusc", txbYear.Text);
            Session.Add("PipeNumberForRusc", txbPipeNumber.Text);
            Session.Add("SelectedROWForRusc", SelectedRowId);
            Session.Add("BadMarkingPipeNumberForRusc", BadMarkingPipeNumber);
            Session.Add("CheckCalibrForRusc", Convert.ToByte(cblCheck.Checked));
            Session.Add("TargetForRusc", target);
            PopupWindow1.ContentPanelId = pnlInputPipe.ID;
            PopupWindow1.Title = "Ввод данных по испытанию трубы";
            pnlInputPipe.Visible = true;
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка ввода данных по трубе", ex);
        }
    }


    //формирование таблицы журнала испытаний
    protected void tblHistory_PreRender(object sender, EventArgs e)
    {
        try
        {
            //очистка старых значений
            while (tblHistory.Rows.Count > 1)
                tblHistory.Rows.RemoveAt(1);

            //выборка данных и добавление строк
            try
            {
                OleDbConnection conn = Master.Connect.ORACLE_TESC3();
                OleDbCommand cmd = conn.CreateCommand();
                cmd.CommandText = @"select uo.REC_DATE,  uo.PIPE_NUMBER,  PPI.D_GRADE, PPI. D_NTDQM, NVL(to_Char(PPI.S_DIAM), to_Char(PPI.S_SIZE1)||'x'||to_Char(PPI.S_SIZE2))||'x'||to_Char(PPI.S_THICKNESS_PIPE) as Razmer,
                    uo.USC_STAN_BRAK, uo.TEST_BRAK_AFTER_STAN, uo.USC_KROM_BRAK, uo.TEST_BRAK_AFTER_KROM, uo.PREV_AUSC_BRAK, uo.TEST_BRAK_MANUAL, uo.PREV_AUSC_BODY_BRAK, uo.MANUAL_AUSC_BODY_BRAK, uo.PREV_AUSC_END_LEFT_BRAK
                    , uo.MANUAL_AUSC_END_LEFT_BRAK, uo.PREV_AUSC_END_RIGHT_BRAK, uo.MANUAL_AUSC_END_RIGHT_BRAK, uo.NEXT_DIRECTION, uo.FIRST_REC_DATE, uo.ID, uo.ROW_ID
                                        from USC_OTDELKA  uo
                                        left join optimal_pipes op on uo.pipe_number = op.pipe_number
                                left join geometry_coils_sklad gc on OP.COIL_PIPEPART_YEAR=gc.coil_pipepart_year and op.coil_pipepart_no=gc.coil_pipepart_no and OP.COIL_INTERNALNO=GC.COIL_RUN_NO
                                left join campaigns cmp on gc.campaign_line_id=CMP.CAMPAIGN_LINE_ID
                                left join ORACLE.Z_SPR_MATERIALS ppi on CMP.INVENTORY_CODE = PPI.MATNR
                                        where (uo.PIPE_NUMBER is not null)and(uo.EDIT_STATE=0)and(uo.WORKPLACE_ID=?)  and(gc.EDIT_STATE=0) and uo.FIRST_REC_DATE>SYSDATE-4
                    union all        
                    select uo.REC_DATE,  uo.PIPE_NUMBER,  rn.STEELMARK, rn.NTD,NVL(to_Char(rn.DIAMETER), to_Char(rn.PROFILE_SIZE_A)||'x'||to_Char(rn.PROFILE_SIZE_B))||'x'||to_Char(rn.THICKNESS) as Razmer,
                    uo.USC_STAN_BRAK, uo.TEST_BRAK_AFTER_STAN, uo.USC_KROM_BRAK, uo.TEST_BRAK_AFTER_KROM, uo.PREV_AUSC_BRAK, uo.TEST_BRAK_MANUAL, uo.PREV_AUSC_BODY_BRAK, uo.MANUAL_AUSC_BODY_BRAK, uo.PREV_AUSC_END_LEFT_BRAK
                    , uo.MANUAL_AUSC_END_LEFT_BRAK, uo.PREV_AUSC_END_RIGHT_BRAK, uo.MANUAL_AUSC_END_RIGHT_BRAK, uo.NEXT_DIRECTION, uo.FIRST_REC_DATE, uo.ID, uo.ROW_ID
                                        from USC_OTDELKA uo
                                        left join RESERVE_NUMBERS rn on uo.pipe_number = rn.pipe_number
                                       where (uo.PIPE_NUMBER is not null)and(uo.EDIT_STATE=0)and(uo.WORKPLACE_ID=?) and(rn.EDIT_STATE=0) and uo.REC_DATE>SYSDATE-4
                                        order by FIRST_REC_DATE desc, ID desc";
                cmd.Parameters.AddWithValue("WORKPLACE_ID", WorkplaceID);
                cmd.Parameters.AddWithValue("WORKPLACE_ID", WorkplaceID);
                OleDbDataReader rdr = cmd.ExecuteReader();
                int rowCount = 0;
                while (rdr.Read() && rowCount < 20)
                {
                    TableRow row = new TableRow();
                    TableCell cell;

                    //дата испытания                    
                    cell = new TableCell(); cell.Text = Convert.ToDateTime(rdr["REC_DATE"]).ToString("dd MMM HH:mm"); cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);

                    //номер трубы
                    cell = new TableCell(); cell.Text = rdr["PIPE_NUMBER"].ToString(); cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);

                    //Размер
                    cell = new TableCell(); cell.Text = rdr["RAZMER"].ToString(); cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);

                    //Марка стали
                    cell = new TableCell(); cell.Text = rdr["D_GRADE"].ToString(); cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);

                    //НД
                    cell = new TableCell(); cell.Text = rdr["D_NTDQM"].ToString(); cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);


                    //метка АУЗК сварки
                    cell = new TableCell(); cell.Text = TestResultToString2(rdr["USC_STAN_BRAK"]);
                    cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);

                    //результат перепроверки АУЗК сварки
                    String res = TestResultToString2(rdr["TEST_BRAK_AFTER_STAN"]);
                    cell = new TableCell(); cell.Text = res;
                    cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);

                    //метка АУЗК кромок
                    cell = new TableCell(); cell.Text = TestResultToString2(rdr["USC_KROM_BRAK"]);
                    cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);

                    //результат перепроверки АУЗК кромок
                    res = TestResultToString2(rdr["TEST_BRAK_AFTER_KROM"]);
                    cell = new TableCell(); cell.Text = res;
                    cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);

                    //метка АУЗК шва
                    cell = new TableCell(); cell.Text = TestResultToString2(rdr["PREV_AUSC_BRAK"]);
                    cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);

                    //результат перепроверки АУЗК шва
                    res = TestResultToString2(rdr["TEST_BRAK_MANUAL"]);
                    cell = new TableCell(); cell.Text = res;
                    cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);


                    //метка АУЗК тела
                    cell = new TableCell(); cell.Text = TestResultToString2(rdr["PREV_AUSC_BODY_BRAK"]);
                    cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);

                    //результат перепроверки АУЗК тела
                    res = TestResultToString2(rdr["MANUAL_AUSC_BODY_BRAK"]);
                    cell = new TableCell(); cell.Text = res;
                    cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);

                    //метка АУЗК торца левая
                    cell = new TableCell(); cell.Text = TestResultToString2(rdr["PREV_AUSC_END_LEFT_BRAK"]);
                    cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);

                    //результат перепроверки АУЗК торца левая
                    res = TestResultToString2(rdr["MANUAL_AUSC_END_LEFT_BRAK"]);
                    cell = new TableCell(); cell.Text = res;
                    cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);


                    //метка АУЗК торца правая
                    cell = new TableCell(); cell.Text = TestResultToString2(rdr["PREV_AUSC_END_RIGHT_BRAK"]);
                    cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);

                    //результат перепроверки АУЗК торца правая
                    res = TestResultToString2(rdr["MANUAL_AUSC_END_RIGHT_BRAK"]);
                    cell = new TableCell(); cell.Text = res;
                    cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);


                    //направление
                    cell = new TableCell(); cell.Text = rdr["NEXT_DIRECTION"].ToString();
                    cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);

                    //кнопка команды удаления записи
                    cell = new TableCell();
                    String argument = rdr["ROW_ID"].ToString();
                    String script = "if(confirm('Удалить запись по испытанию трубы " + rdr["PIPE_NUMBER"].ToString() + " ?')) __doPostBack('DEL_RECORD', '" + argument + "')";
                    cell.Text = "<input type='button' style='width:24px; height: 24px' value='X' onclick=\"" + script + "\" />";
                    cell.HorizontalAlign = HorizontalAlign.Center; row.Cells.Add(cell);
                    row.Cells.Add(cell);

                    //скрипт для перехода к редактированию строки
                    for (int i = 0; i < row.Cells.Count - 1; i++)
                    {
                        row.Cells[i].Attributes["onclick"] = "__doPostBack('EDIT_PIPE', '" + argument + "')";
                        row.Cells[i].Style["cursor"] = "pointer";
                    }
                    String onmouseleave = "this.style.backgroundColor=''";
                    String onmouseover = "this.style.backgroundColor='#E6E6F0'";
                    row.Attributes["onmouseover"] = onmouseover;
                    row.Attributes["onmouseleave"] = onmouseleave;

                    //добавление строки и подсветка
                    rowCount++;
                    tblHistory.Rows.Add(row);
                }
                rdr.Close();
                rdr.Dispose();
                cmd.Dispose();
                Master.Connect.CloseConnections();
            }
            catch (Exception ex)
            {
                Master.AddErrorMessage("Ошибка формирования истории испытаний", ex);
            }
        }
        finally
        {
            //
        }
    }


    //Событие по нажатию ссылки  "?"
    protected void lblHelpCheck_Click(object sender, EventArgs e)
    {
        int PipeNumber = 0;
        int Year = 0;
        if (Int32.TryParse(txbPipeNumber.Text, out PipeNumber) & Int32.TryParse(txbYear.Text, out Year))
        {
            mvPipes.SetActiveView(vGetChekingNum);
            Master.FocusControl = txbLogin.ID;
            txbLogin.Text = "";
            txbPassword.Text = "";
            lblNumtrub.Text = txbYear.Text + txbPipeNumber.Text.PadLeft(6, '0');
        }
        else
            Master.AddErrorMessage(Messages.GetByCode(302) + ": Год или № Трубы");
    }


    //переход к форме получения резервного номера
    protected void btnReserveNumber_Click(object sender, EventArgs e)
    {
        //
    }


    //закрытие подключений при выгрузке UpdatePanel
    protected void UpdatePanel3_Unload(object sender, EventArgs e)
    {
        try
        {
            Master.Connect.CloseConnections();
        }
        finally
        {
            //
        }
    }




    //обновление состояния кнопки "Новая строка"
    protected void btnNewCalibrParams_PreRender(object sender, EventArgs e)
    {
        try
        {
            btnNewCalibrParams.Enabled = (EditingLineID == "");
        }
        finally
        {
            //
        }
    }



    //проверка возможности ввода данных по трубе на рабочем месте
    //согласно маршрутной карте
    //возвращает true, если приемка возможна. Иначе false и messageBox на клиенте.
    protected bool CheckValidCurrentPositionOfPipe()
    {
        try
        {
            if (DEBUG_DISABLE_ROUTE_CONTROL) return true;

            //номер трубы, код рабочего места, буфер сообщений
            int PipeNumber = Convert.ToInt32(txbYear.Text.Trim() + txbPipeNumber.Text.Trim().PadLeft(6, '0'));
            List<String> msgs = new List<string>();

            //проврерка
            if (Checking.CheckValidCurrentPositionOfPipe(PipeNumber, WorkplaceID, ref msgs))
                return true;
            else
            {
                //если ввод данных невозможен - отображение сообщения
                String msg = "ВНИМАНИЕ!\nВвод данных по трубе " + PipeNumber + " невозможен";
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
        finally
        {
            //
        }
    }


    //удаление последней позиции трубы для отслеживания маршрутов
    //rowid - ID оригинальной записи
    private void DeleteLastPositionOfPipe(String rowid)
    {
        if (DEBUG_DISABLE_ROUTE_CONTROL) return;

        try
        {
            //получение номера трубы по rowid
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "select PIPE_NUMBER from USC_OTDELKA where ROW_ID=?";
            cmd.Parameters.AddWithValue("ROW_ID", rowid);
            OleDbDataReader rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                int PIPE_NUMBER = Convert.ToInt32(rdr["PIPE_NUMBER"]);

                //удаление записи в отслеживании маршрутов
                List<String> msg = new List<string>();
                Checking.DeleteLastPositionOfPipe(PIPE_NUMBER, WorkplaceID, ref msg);
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


    //проверка возможности редактирования данных по трубе на рабочем месте
    //согласно маршрутной карте
    //возвращает true, если правка возможна. Иначе false и messageBox на клиенте
    //rowid - ID оригинальной записи
    protected bool CheckCanEditRecByPipe(String rowid)
    {
        try
        {
            if (DEBUG_DISABLE_ROUTE_CONTROL) return true;

            try
            {
                //получение номера трубы по rowid
                OleDbConnection conn = Master.Connect.ORACLE_TESC3();
                OleDbCommand cmd = conn.CreateCommand();
                cmd.CommandText = "select PIPE_NUMBER from USC_OTDELKA where ROW_ID=?";
                cmd.Parameters.AddWithValue("ROW_ID", rowid);
                OleDbDataReader rdr = cmd.ExecuteReader();

                if (rdr.Read())
                {
                    int PIPE_NUMBER = Convert.ToInt32(rdr["PIPE_NUMBER"]);

                    //код рабочего места, буфер сообщений        
                    List<String> msgs = new List<string>();

                    //проврерка
                    if (Checking.CanEditRecByPipe(PIPE_NUMBER, WorkplaceID, ref msgs))
                        return true;
                    else
                    {
                        //если ввод данных невозможен - отображение сообщения
                        String msg = "ВНИМАНИЕ!\nПравка или удаление данных по трубе " + PIPE_NUMBER.ToString() + " невозможно";
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
                rdr.Close();
                rdr.Dispose();
                cmd.Dispose();
                return true;
            }
            catch (Exception ex)
            {
                Master.AddErrorMessage("Ошибка проверки возможности правки записи при отслеживании маршрута", ex);
                return false;
            }
        }
        finally
        {
            //
        }
    }


    //Сохранение позиции трубы для отслеживания маршрута
    private void SetNewPositionOfPipe()
    {
        try
        {
            if (DEBUG_DISABLE_ROUTE_CONTROL) return;

            try
            {
                //номер трубы, код рабочего места, буфер сообщений
                int PipeNumber = Convert.ToInt32(txbYear.Text.Trim() + txbPipeNumber.Text.Trim().PadLeft(6, '0'));

                //сохранение
                List<String> msg = new List<string>();
                if (!Checking.SetNewPositionOfPipe(PipeNumber, WorkplaceID, ref msg))
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


    //преобразование результата испытаний (наличия метки дефекта) в текстовую форму
    protected String TestResultToString(object test_result)
    {
        if (test_result.ToString() == "1") return "Да"; //метка дефекта есть
        if (test_result.ToString() == "0") return "Нет"; //метки дефекта нет
        return "";
    }

    //преобразование результата испытаний (наличия метки дефекта) в текстовую форму
    protected String TestResultToString2(object test_result)
    {
        if (test_result.ToString() == "0") return "Пройдено";
        if (test_result.ToString() == "1") return "Не пройдено";
        return "";
    }

    //преобразование результата испытаний (наличия метки дефекта) из текстовой формы
    protected String TestResultFromText(String test_result_text)
    {
        if (test_result_text == "Нет") return "0";
        if (test_result_text == "Да") return "1";
        return "";
    }


    //редактирование записи по испытанию трубы
    protected void BeginEditPipe(String row_id)
    {
        try
        {
            //отображение окна редактирования записи
            SelectedRowId = row_id;
            int checkCalibr = Convert.ToByte(cblCheck.Checked);
            Session.Add("SelectedROWForRusc", SelectedRowId);
            Session.Add("WorkplaceIDForRusc", WorkplaceID);
            Session.Add("BadMarkingPipeNumberForRusc", BadMarkingPipeNumber);
            Session.Add("CheckCalibrForRusc", checkCalibr);
            string target = "EDIT_PIPE";
            Session.Add("TargetForRusc", target);
            PopupWindow1.Visible = true;
            PopupWindow1.ContentPanelId = pnlInputPipe.ID;
            PopupWindow1.Title = "Ввод данных по испытанию трубы";
            pnlInputPipe.Visible = true;
            PopupWindow1.MoveToCenter();
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка получения данных по трубе", ex);
        }
    }

    private void GetPipeNumber(string PipeNumber)
    {
        try
        {
            //очистка старых значений
            while (tblInfo.Rows.Count > 1)
                tblInfo.Rows.RemoveAt(1);

            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = @"select op.pipe_number, PPI.D_GRADE, PPI.D_NTDQM, 
            NVL(to_Char(PPI.S_DIAM), to_Char(PPI.S_SIZE1)||'x'||to_Char(PPI.S_SIZE2))||'x'||to_Char(PPI.S_THICKNESS_PIPE) as Razmer, to_Char(PPI.S_DIAM) diam, to_Char(PPI.S_THICKNESS_PIPE) thick
            from  optimal_pipes op
            left join geometry_coils_sklad gc on OP.COIL_PIPEPART_YEAR=gc.coil_pipepart_year and op.coil_pipepart_no=gc.coil_pipepart_no and OP.COIL_INTERNALNO=GC.COIL_RUN_NO
            left join campaigns cmp on gc.campaign_line_id=CMP.CAMPAIGN_LINE_ID
            left join ORACLE.Z_SPR_MATERIALS ppi on CMP.INVENTORY_CODE = PPI.MATNR
                    where (op.pipe_number=?)  and (gc.EDIT_STATE=0)";
            cmd.Parameters.AddWithValue("PIPE_NUMBER", PipeNumber);
            OleDbDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                TableRow row = new TableRow();
                TableCell cell;

                CultureInfo culture = new CultureInfo("ru-RU");
                //номер трубы
                cell = new TableCell();
                cell.Text = rdr["PIPE_NUMBER"].ToString();
                cell.HorizontalAlign = HorizontalAlign.Center;
                row.Cells.Add(cell);

                //Размер
                cell = new TableCell();
                cell.Text = rdr["RAZMER"].ToString();
                cell.HorizontalAlign = HorizontalAlign.Center;
                row.Cells.Add(cell);

                //Марка стали
                cell = new TableCell();
                cell.Text = rdr["D_GRADE"].ToString();
                cell.HorizontalAlign = HorizontalAlign.Center;
                row.Cells.Add(cell);

                //НД
                cell = new TableCell();
                cell.Text = rdr["D_NTDQM"].ToString();
                cell.HorizontalAlign = HorizontalAlign.Center;
                row.Cells.Add(cell);
                row.BackColor = Color.FromArgb(0xE9E9F0);
                tblInfo.Rows.Add(row);
            }
            tblInfo.Visible = (tblInfo.Rows.Count > 1);
            rdr.Close();
            rdr.Dispose();
            cmd.Dispose();
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка при получении характеристик трубы", ex);
        }
    }
}