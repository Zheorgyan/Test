using System;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Data.OleDb;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AuthenticationService;



public partial class StoreHousesOfPipesByPipesNums : System.Web.UI.Page
{
    public String PostID
    {
        get
        {
            object o = (object)Session["_PostID"];
            if (o == null) return Convert.ToString(null);
            return Convert.ToString(o);
        }
        set
        {
            Session["_PostID"] = value;
        }
    }
    public String Error
    {
        get
        {
            object o = (object)Session["_Error"];
            if (o == null) return Convert.ToString(null);
            return Convert.ToString(o);
        }
        set
        {
            Session["_Error"] = value;
        }
    }

    #region *** Переменные ViewState **************************************************************************
    /// <summary>Номер трубы</summary>
    string VPipeNumber { get { object o = ViewState["_VPipeNumber"]; if (o == null)return ""; return (string)ViewState["_VPipeNumber"]; } set { ViewState["_VPipeNumber"] = value; } }

    /// <summary>Отмеченные флажки</summary>
    string[] VCheckChecked { get { object o = ViewState["_VCheckChecked"]; if (o == null)return (string[])ViewState["_VCheckChecked"]; return (string[])ViewState["_VCheckChecked"]; } set { ViewState["_VCheckChecked"] = value; } }

    /// <summary>Дефект</summary>
    string VDefects { get { object o = ViewState["_VDefects"]; if (o == null)return ""; return (string)ViewState["_VDefects"]; } set { ViewState["_VDefects"] = value; } }

    /// <summary>Примечание</summary>
    string VNotes { get { object o = ViewState["_VNotes"]; if (o == null)return ""; return (string)ViewState["_VNotes"]; } set { ViewState["_VNotes"] = value; } }

    /// <summary>НЗП</summary>
    int VNZP { get { object o = ViewState["_VNZP"]; if (o == null) return 0; return (int)ViewState["_VNZP"]; } set { ViewState["_VNZP"] = value; } }

    /// <summary>Для предъявления</summary>
    int VPrezentation { get { object o = ViewState["_VPrezentation"]; if (o == null) return 0; return (int)ViewState["_VPrezentation"]; } set { ViewState["_VPrezentation"] = value; } }

    /// <summary>Тип операции</summary>
    string VOperation { get { object o = ViewState["_VOperation"]; if (o == null)return ""; return (string)ViewState["_VOperation"]; } set { ViewState["_VOperation"] = value; } }

    /// <summary>Тип операции</summary>
    string VRowID { get { object o = ViewState["_VRowID"]; if (o == null)return ""; return (string)ViewState["_VRowID"]; } set { ViewState["_VRowID"] = value; } }

    /// <summary>Начальный индекс</summary>
    int VStartIndex { get { object o = ViewState["_VStartIndex"]; if (o == null) return 0; return (int)ViewState["_VStartIndex"]; } set { ViewState["_VStartIndex"] = value; } }

    /// <summary>Конечный индекс</summary>
    int VEndIndex { get { object o = ViewState["_VEndIndex"]; if (o == null) return 0; return (int)ViewState["_VEndIndex"]; } set { ViewState["_VEndIndex"] = value; } }

    #endregion *** Переменные ViewState **************************************************************************


    //Действия осуществляемые при загрузке страницы
    protected void Page_Load(object sender, EventArgs e)
    {
        //Authentification.CanAnyAccess(WORKPLACE_ID);
        //очистка старых сообщений об ошибках
        Master.ClearErrorMessages();
        Culture = "Ru-Ru";
        //установка курсора в текстбокс для считывания со штрихкода
        Page.SetFocus(txbBarcode);
        //включение сохранения позиции полосок прокрутки
        Master.EnableSaveScrollPositions(this);
        OleDbConnection ConnectToOracle = Master.Connect.ORACLE_TESC3();

        if (!IsPostBack)
        {
            PostID = null;
            FillZonesDDL();
            //Разбор параметров ссылки на страницу
            try
            {
                //ddlZone.SelectedIndex = Convert.ToInt32(Request["ZONE_INDEX"].ToString().Trim());
                ddlZone_SelectedIndexChanged(ddlZone, null);
            }
            catch
            { }
            MainMultiView.SetActiveView(vLogined);
            mvViews.SetActiveView(vFilter);
        }
        else
        {
            SelectedZoneName = ddlZone.SelectedItem.Value;
        }

        if (ddlZone.SelectedIndex > 0)
        {
            if (ddlZone.SelectedItem.Text != "(Все)")
            {
                GetSkladRollNameByZoneName(ref RollNameForCurrentZone);
                if (!Authentification.CanAddData(RollNameForCurrentZone))
                    tdPrihod.Style["display"] = "none";
                else
                    tdPrihod.Style["display"] = "block";

                if (!Authentification.CanEditData(RollNameForCurrentZone))
                    tdRelocation.Style["display"] = "none";
                else
                    tdRelocation.Style["display"] = "block";
            }
            else
            {
                tdPrihod.Style["display"] = "block";
                tdRelocation.Style["display"] = "block";
            }
        }
        else
        {
            bool canAccess = false;
            try
            {
                OleDbConnection conn = Master.Connect.ORACLE_TESC3();
                OleDbCommand cmd = conn.CreateCommand();
                cmd.CommandText = "select distinct zone_rol_name from SPR_ALL_STACKS WHERE USE_FOR_BYPIPESNUMS = 1 and IS_ACTIVE = 1 ";
                OleDbDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    canAccess = canAccess || Authentification.CanAnyAccess(reader["zone_rol_name"].ToString());
                }
                reader.Close();
                cmd.Dispose();
            }
            catch (Exception ex)
            {
                Master.AddErrorMessage("Ошибка получения имен полей ролей", ex); return;
            }
            if (!canAccess) System.Web.HttpContext.Current.Response.Redirect("Default.aspx?AccessDeniedMsg=1");
        }

        pnlSkladControls.Style["display"] = "none";
        //запрет доступа, если не выбран складской объект и штабель
        if (ddlZone.SelectedItem.Text.Trim() == "" || ddlStack_name.SelectedItem.Text.Trim() == "")
        {
            //очистка контейнера для хранения названия складского объекта
            SelectedZoneName = "";
            RollNameForCurrentZone = "";
            lblEnterDataMsg.Text = "<br/>Для начала работы необходимо указать название складского объекта";
            MainMultiView.SetActiveView(vNotLoginedView);
        }
        else
        {
            GetSkladRollNameByZoneName(ref RollNameForCurrentZone);
            MainMultiView.SetActiveView(vLogined);
        }

        //заполнение поля смена при загрузке страницы
        lblShift.Text = Checking.GetShiftChar(DateTime.Now).ToString();
       
        String arg = Request.Params["__EVENTARGUMENT"];
        String target = Request.Params["__EVENTTARGET"];

        #region ///ПостБек по таблице приход
        if (target == "PRIHOD")
        {
            String[] Argument = arg.Split('#');
            if (PostID != arg)
            {
                switch (Argument[0])
                {
                    case "INSERT_EDIT":
                        {
                            PrihodTable(true, Argument[2], true);
                            break;
                        }
                    case "EDIT":
                        {
                            EditPrihod(Argument[2], true);
                            break;
                        }
                    case "ADD":
                        {
                            Prihod(true);
                            break;
                        }
                    case "DELETE":
                        {
                            DeleteSklad(Argument[2]);
                            PrihodTable(false, "", false);
                            break;
                        }
                    case "CANCEL":
                        {
                            PrihodTable(false, "", false);
                            break;
                        }
                    /*case "AutoFill":
                        {
                            PrihodTable(true, Argument[2], false);
                            AutoFillLines(chbNZP.Checked, tbPrihodPipeNumber.Text, tblPrihod.Rows[Convert.ToInt32(numRowEdit)]);
                            break;
                        }*/
                }
                PostID = arg;
                //Master.AddErrorMessage(Error);
            }
            else
            {
                PrihodTable(false, "", false);
            }
        }
        #endregion
        #region ///ПостБек по таблице склад
        else if (target == "SKLAD")
        {
            String[] Argument = arg.Split('#');
            switch (Argument[0])
            {
                case "INSERT_EDIT":
                    {
                        SkladTable(true, Argument[1], Convert.ToInt32(Argument[2]), Convert.ToInt32(Argument[3]), true);
                        hfAutoFillData.Value = "";
                        break;
                    }
                case "EDIT":
                    {
                        VStartIndex = Convert.ToInt32(Argument[2]);
                        VEndIndex = Convert.ToInt32(Argument[3]);

                        EditSklad(Argument[1], true);
                        //SkladTable(false, "", Convert.ToInt32(Argument[2]), Convert.ToInt32(Argument[3]), false);
                        //hfAutoFillData.Value = "";
                        break;
                    }
                case "DELETE":
                    {
                        DeleteSklad(Argument[1]);
                        SkladTable(false, "", Convert.ToInt32(Argument[2]), Convert.ToInt32(Argument[3]), false);
                        break;
                    }
                case "CANCEL":
                    {
                        SkladTable(false, "", Convert.ToInt32(Argument[1]), Convert.ToInt32(Argument[2]), false);
                        break;
                    }
                case "AutoFill":
                    {
                        SkladTable(true, Argument[1], Convert.ToInt32(Argument[2]), Convert.ToInt32(Argument[3]), false);
                        AutoFillLines(chbNZP0.Checked, tbPipeNumber.Text, tblSklad.Rows[Convert.ToInt32(numRowEdit)]);
                        break;
                    }

            }
        }
        #endregion
        //подтверждение изменений строки на мобильном устройстве
        if (target == "CONFIRM_LINE_MOBILE")
        {
            //парсинг со штрихкода
            txbBarcode_TextChanged(sender, e);
        }
        btnDelAll.Enabled = (ddlZone.SelectedIndex > 0 && ddlStack_name.SelectedIndex > 0 /*&& !cbxStack.Checked */ && Authentification.CanDeleteData(RollNameForCurrentZone));
    }

    protected void Page_PreRender(object sender, EventArgs e)
    {
        /*if (ddlStack.SelectedItem == null) return;
        tbRelocationPipeNumber.Attributes["onkeypress"] = "return AddPrihod('btnRelocationAdd')";
        ddlRelocationZone.Attributes["onkeypress"] = "return newKeyPress('ddlRelocationStack')";
        ddlRelocationStack.Attributes["onkeypress"] = "return newKeyPress('ddlRelocationPocket')";*/

        if (vRelocation.Visible)
        {
            BuildTblFrom();
            BuildTblTo();
            RelocationTable();
        }
    }

    //функция получения названия поля роли для текущей зоны; возвращает 0 в случае удачного её выполнения, 
    //при этом в strFildName будет помещено название поля
    //иначе функция возвращает 1
    protected int GetSkladRollNameByZoneName(ref String strFildName)
    {
        int Result = 1;
        if (ddlZone.SelectedItem.Value.Trim() == "") return 1;
        try
        {
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "select distinct zone_rol_name from SPR_ALL_STACKS where ZONE_NAME=? and USE_FOR_BYPIPESNUMS = 1 and IS_ACTIVE = 1 ";
            cmd.Parameters.AddWithValue("ZONE_NAME", System.Convert.ToString(ddlZone.SelectedItem.Value));
            OleDbDataReader reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                strFildName = reader["zone_rol_name"].ToString();
                if (strFildName != "") Result = 0;
            }
            reader.Close();
            cmd.Dispose();

        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка получения названия поля роли", ex);
        }
        return Result;
    }
    protected int GetRollNameByZoneNameForEdit(ref String strFildName, String Zone)
    {
        int Result = 1;
        if (ddlZone.SelectedItem.Value.Trim() == "") return 1;
        try
        {
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "select distinct zone_rol_name from SPR_ALL_STACKS where ZONE_NAME=? and USE_FOR_BYPIPESNUMS = 1 and IS_ACTIVE = 1 ";
            cmd.Parameters.AddWithValue("ZONE_NAME", Zone);
            OleDbDataReader reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                strFildName = reader["zone_rol_name"].ToString();
                if (strFildName != "") Result = 0;
            }
            reader.Close();
            cmd.Dispose();

        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка получения названия поля роли", ex);
        }
        return Result;
    }

    //Имя роли для текущего складского объекта
    protected String RollNameForCurrentZone;
    //свойство: SelectedZoneName название текущего склада
    protected String SelectedZoneName
    {
        get
        {
            object o = ViewState["StoreHouse_Zone_name"];
            if (o == null) return ""; else return o.ToString();
        }
        set
        {
            ViewState["StoreHouse_Zone_name"] = value;
        }
    }

    //заполнение выпадающих списков из справочников
    protected void FillZonesDDL()
    {
        //заполнение выпадающего списка сотрудников
        try
        {
            ddlZone.Items.Clear();
            ddlRelocationZone.Items.Clear();
            ddlZone.Items.Add("");
            ddlRelocationZone.Items.Add("");
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();
            cmd = conn.CreateCommand();
            cmd.CommandText = "select distinct ZONE_NAME from SPR_ALL_STACKS where USE_FOR_BYPIPESNUMS = 1 and IS_ACTIVE = 1 and ZONE_NAME is not null order by ZONE_NAME";
            OleDbDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                ddlZone.Items.Add(reader["ZONE_NAME"].ToString());
                ddlRelocationZone.Items.Add(reader["ZONE_NAME"].ToString());
            }
            reader.Close();
            reader.Dispose();
            cmd.Dispose();
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка заполнения всплывающих списков", ex);
        }

    }

    //заполнение выпадающих списков из справочников
    protected void FillFiltersDDL()
    {
        //----------------
        int SelInd_ddlDiametrs = ddlDiametrs.SelectedIndex;

        int SelInd_ddlThickneses = ddlThickneses.SelectedIndex;

        int SelInd_ddlSteelMarks = ddlSteelMarks.SelectedIndex;

        int SelInd_ddlDefects = ddlDefects.SelectedIndex;

        int SelInd_ddlGost = ddlGost.SelectedIndex;
        int SelInd_ddlGroup = ddlGroup.SelectedIndex;

        int SelInd_ddlStack = ddlStack_name.SelectedIndex;

        int SelInd_ddlPocket = ddlPocket_name.SelectedIndex;
        int SelInd_ddlOperators = ddlOperators.SelectedIndex;

        int SelInd_ddlDestination = ddlDestination.SelectedIndex;

        int SelInd_ddlInspection = ddlInspection.SelectedIndex;

        //----------------


        //Заполнение списков фильтров данными из справочников, хранящихся в Oracle
        try
        {
            //Подключение к БД
            OleDbConnection conn = Master.Connect.ORACLE_ORACLE();  //new OleDbConnection(ConfigurationManager.AppSettings.Get("OraConnection_MS1_oracle"));
            //conn.Open();
            OleDbCommand cmd = conn.CreateCommand();

            //Формирование запроса на получение списка возможных диаметров труб
            if (ddlZone.SelectedItem.Value == "СК-ТЭСЦ-3 пролет 1 (термоотдел)")
                cmd.CommandText = "select distinct to_char(DIAMETER) DIAMETER from ORACLE.V_T3_PIPE_ITEMS v where ((ORG_ID = 127)or(ORG_ID = 129))and(not(DIAMETER is null)) and diameter>0 order by v.DIAMETER";
            else
                cmd.CommandText = "select distinct to_char(DIAMETER) DIAMETER from ORACLE.V_T3_PIPE_ITEMS v where (ORG_ID = 127)and(not(DIAMETER is null)) and diameter>0 order by v.DIAMETER";

            OleDbDataReader readerDIAMETER = cmd.ExecuteReader();
            ddlDiametrs.Items.Clear();
            ddlDiametrs.Items.Add("(Все)");
            //Запись списка диаметров в фильтр по диаметру
            while (readerDIAMETER.Read())
            {
                ListItem lstItem1 = new ListItem(readerDIAMETER["DIAMETER"].ToString(), readerDIAMETER["DIAMETER"].ToString());
                ddlDiametrs.Items.Add(lstItem1);
            }

            readerDIAMETER.Close();
            /*
            //<---Список диаметров для труб NORD STREAM---            
            ddlDiametrs.Items.Add(new ListItem("1020", "1020"));
            ddlDiametrs.Items.Add(new ListItem("1067", "1067"));
            ddlDiametrs.Items.Add(new ListItem("1220", "1220"));
            //--->
            */
            if (SelInd_ddlDiametrs < (ddlDiametrs.Items.Count)) ddlDiametrs.SelectedIndex = SelInd_ddlDiametrs;
            else ddlDiametrs.SelectedIndex = 0;

            //Формирование запроса на получение списка возможных толщин стенок труб
            if (ddlZone.SelectedItem.Value == "СК-ТЭСЦ-3 пролет 1 (термоотдел)")
                cmd.CommandText = "select distinct to_char(THICKNESS) THICKNESS from ORACLE.V_T3_PIPE_ITEMS v where ((ORG_ID = 127)or(ORG_ID = 129))and(not(THICKNESS is null)) and THICKNESS>0 order by v.THICKNESS";
            else
                cmd.CommandText = "select distinct to_char(THICKNESS) THICKNESS from ORACLE.V_T3_PIPE_ITEMS v where (ORG_ID = 127)and(not(THICKNESS is null)) and THICKNESS>0 order by v.THICKNESS";
            OleDbDataReader readerTHICKNESS = cmd.ExecuteReader();
            ddlThickneses.Items.Clear();
            ddlThickneses.Items.Add("(Все)");
            //Запись списка толщин стенок в фильтр по толщине стеноки
            while (readerTHICKNESS.Read())
            {
                ListItem lstItem1 = new ListItem(readerTHICKNESS["THICKNESS"].ToString(), readerTHICKNESS["THICKNESS"].ToString());
                ddlThickneses.Items.Add(lstItem1);
            }
            readerTHICKNESS.Close();
            /*
            //<---Список толщин стенок для труб NORD STREAM---
            ddlThickneses.Items.Add(new ListItem("15", "15"));
            ddlThickneses.Items.Add(new ListItem("20", "20"));
            ddlThickneses.Items.Add(new ListItem("30,9", "30,9"));
            ddlThickneses.Items.Add(new ListItem("34,6", "34,6"));
            //--->
            */
            if (SelInd_ddlThickneses < (ddlThickneses.Items.Count)) ddlThickneses.SelectedIndex = SelInd_ddlThickneses;
            else ddlThickneses.SelectedIndex = 0;

            //Формирование запроса на получение списка возможных марок сталей труб
            if (ddlZone.SelectedItem.Value == "СК-ТЭСЦ-3 пролет 1 (термоотдел)")
                cmd.CommandText = "select distinct STAL from ORACLE.V_T3_PIPE_ITEMS where ((ORG_ID = 127)or(ORG_ID = 129))and(not(STAL is null)) and STAL is not null order by STAL";
            else
                cmd.CommandText = "select distinct STAL from ORACLE.V_T3_PIPE_ITEMS where (ORG_ID = 127)and(not(STAL is null)) and STAL is not null order by STAL";
            OleDbDataReader readerSTAL = cmd.ExecuteReader();
            ddlSteelMarks.Items.Clear();
            ddlSteelMarks.Items.Add("(Все)");
            //Запись списка марок сталей в фильтр по марке стали
            while (readerSTAL.Read())
            {
                ListItem lstItem1 = new ListItem(readerSTAL["STAL"].ToString(), readerSTAL["STAL"].ToString());
                ddlSteelMarks.Items.Add(lstItem1);
            }
            readerSTAL.Close();
            cmd.Dispose();
            if (SelInd_ddlSteelMarks < (ddlSteelMarks.Items.Count)) ddlSteelMarks.SelectedIndex = SelInd_ddlSteelMarks;
            else ddlSteelMarks.SelectedIndex = 0;

            //Закрываем подключение к БД
            //conn.Close();
            //--------------------------------------------------------------------------------------------//
            //Подключение к другой БД
            conn = Master.Connect.ORACLE_TESC3(); //new OleDbConnection(ConfigurationManager.AppSettings.Get("OraConnection_MS1_tesc3"));
            //conn.Open();
            cmd = conn.CreateCommand();
            //Формирование запроса на получение списка возможных дефектов (групп труб)

            cmd.CommandText = "select DEFECT_NAME from SPR_DEFECTS_IN_STACK where DEFECT_NAME is not null order by DEFECT_NAME";
            OleDbDataReader readerDefects = cmd.ExecuteReader();
            ddlDefects.Items.Clear();
            ddlPrihodDefect.Items.Clear();
            ddlDefect.Items.Clear();
            ddlDefects.Items.Add("(Все)");
            ddlPrihodDefect.Items.Add("");
            ddlDefect.Items.Add("");
            //Запись списка дефектов в фильтр по дефектам
            while (readerDefects.Read())
            {
                ListItem lstItem1 = new ListItem(readerDefects["DEFECT_NAME"].ToString(), readerDefects["DEFECT_NAME"].ToString());
                ddlDefects.Items.Add(lstItem1);
                ddlPrihodDefect.Items.Add(readerDefects["DEFECT_NAME"].ToString());
                ddlDefect.Items.Add(readerDefects["DEFECT_NAME"].ToString());
            }
            readerDefects.Close();

            if (SelInd_ddlDefects < (ddlDefects.Items.Count)) ddlDefects.SelectedIndex = SelInd_ddlDefects;
            else ddlDefects.SelectedIndex = 0;

            // гост 
            if (ddlZone.SelectedItem.Value == "СК-ТЭСЦ-3 пролет 1 (термоотдел)")
                cmd.CommandText = "select distinct GOST from ORACLE.V_T3_PIPE_ITEMS where (ORG_ID = 127 or ORG_ID = 129) and GOST IS NOT NULL order by GOST";
            else
                cmd.CommandText = "select distinct NTD_NAME GOST from SPR_NTD where NTD_NAME is not null order by NTD_NAME ";
            OleDbDataReader readerND = cmd.ExecuteReader();
            ddlGost.Items.Clear();
            ddlGost.Items.Add("(Все)");
            //Запись списка НД
            while (readerND.Read())
            {
                ddlGost.Items.Add(new ListItem(readerND["GOST"].ToString(), readerND["GOST"].ToString()));
            }
            readerND.Close();
            if (SelInd_ddlGost < (ddlGost.Items.Count)) ddlGost.SelectedIndex = SelInd_ddlGost;
            else ddlGost.SelectedIndex = 0;

            // группа
            if (ddlZone.SelectedItem.Value == "СК-ТЭСЦ-3 пролет 1 (термоотдел)")
                cmd.CommandText = "select distinct GRUP from ORACLE.V_T3_PIPE_ITEMS where (ORG_ID = 127 or ORG_ID = 129) and GRUP is not null order by GRUP ";
            else
                cmd.CommandText = "select distinct NTD_GROUP GRUP from SPR_NTD where SPR_NTD.NTD_GROUP is not null order by NTD_GROUP ";
            readerND = cmd.ExecuteReader();
            ddlGroup.Items.Clear();
            ddlGroup.Items.Add("(Все)");
            //Запись списка НД
            while (readerND.Read())
            {
                ddlGroup.Items.Add(new ListItem(readerND["GRUP"].ToString(), readerND["GRUP"].ToString()));
            }
            readerND.Close();
            if (SelInd_ddlGroup < (ddlGroup.Items.Count)) ddlGroup.SelectedIndex = SelInd_ddlGroup;
            else ddlGroup.SelectedIndex = 0;


            //Формирование запроса на получение списка Назначений
            cmd.CommandText = "select * from SPR_DESTINATIONS_IN_STACK order by DESTINATION ";
            OleDbDataReader readerDestinations = cmd.ExecuteReader();

            ddlDestination.Items.Clear();
            ddlDestination.Items.Add("(Все)");
            //Запись списка Назначий
            while (readerDestinations.Read())
            {
                ddlDestination.Items.Add(new ListItem(readerDestinations["DESTINATION"].ToString(), readerDestinations["DESTINATION"].ToString()));
            }
            readerDestinations.Close();
            if (SelInd_ddlDestination < (ddlDestination.Items.Count)) ddlDestination.SelectedIndex = SelInd_ddlDestination;
            else ddlDestination.SelectedIndex = 0;



            //Формирование запроса на получение списка штабелей на складе            
            if (ddlZone.SelectedItem.Text == "(Все)")
                cmd.CommandText = "select distinct stack_name from SPR_ALL_STACKS where ZONE_NAME is not null and USE_FOR_BYPIPESNUMS = 1 and IS_ACTIVE = 1 and stack_name is not null order by stack_name ";
            else
                cmd.CommandText = "select distinct stack_name from SPR_ALL_STACKS where ZONE_NAME = ? and USE_FOR_BYPIPESNUMS = 1 and IS_ACTIVE = 1 order by stack_name ";
            cmd.Parameters.AddWithValue("ZONE_NAME", System.Convert.ToString(ddlZone.SelectedItem.Value));
            OleDbDataReader readerStacks = cmd.ExecuteReader();
            ddlStack_name.Items.Clear();
            ddlStack_name.Items.Add("");

            //Запись списка штабелей в лист по штабям
            while (readerStacks.Read())
            {
                /*
                if (prevZone_Name != readerStacks["zone_name"].ToString())
                {
                    ddlStack.Items.Add(new ListItem("-" + readerStacks["zone_name"].ToString() + "-", "-"));
                    prevZone_Name = readerStacks["zone_name"].ToString();
                }
                */
                ddlStack_name.Items.Add(new ListItem(readerStacks["stack_name"].ToString(), readerStacks["stack_name"].ToString()));
            }
            readerStacks.Close();
            if (SelInd_ddlStack < (ddlStack_name.Items.Count)) ddlStack_name.SelectedIndex = SelInd_ddlStack;
            else ddlStack_name.SelectedIndex = 0;

            //Формирование запроса на получение списка внешних инспекций
            cmd.CommandText = @"select * from admintesc5.spr_out_insp where shop = 'ТЭСЦ-3' and is_active = 1 order by INSP_NAME ";
            using (OleDbDataReader readerInspection = cmd.ExecuteReader())
            {
                ddlInspection.Items.Clear();
                ddlInspection.Items.Add("(Все)");
                //Запись списка внешних инспекций
                while (readerInspection.Read())
                {
                    ddlInspection.Items.Add(new ListItem(readerInspection["INSP_NAME"].ToString(), readerInspection["INSP_NAME"].ToString()));
                }

                readerInspection.Close();
            }

            if (SelInd_ddlInspection < (ddlInspection.Items.Count)) ddlInspection.SelectedIndex = SelInd_ddlInspection;
            else ddlInspection.SelectedIndex = 0;

            //Формирование запроса на получение списка возможных операторов
            ddlOperators.Items.Clear();
            ddlOperators.Items.Add("(Все)");
            UserInfo[] users = Authentification.GetUsersList(new String[] { "SCLAD" }, false);
            foreach (UserInfo user_info in users)
            {
                if (!String.IsNullOrEmpty(user_info.FIO))
                {
                    ListItem item = new ListItem(user_info.FIO, user_info.TabNumber.ToString());
                    ddlOperators.Items.Add(item);
                }
            }
            if (SelInd_ddlOperators < (ddlOperators.Items.Count)) ddlOperators.SelectedIndex = SelInd_ddlOperators;
            else ddlOperators.SelectedIndex = 0;

            //Формирование запроса на получение списка возможных операторов
            cmd.CommandText = "select distinct NOTES from STOREHOUSES_OF_PIPES_BY_NUM where edit_state = 0 and notes is not null order by NOTES";
            OleDbDataReader readerNotes = cmd.ExecuteReader();
            ddlNotes.Items.Clear();
            ddlNotes.Items.Add("");
            //Запись списка операторов в фильтр по операторам
            while (readerNotes.Read())
            {
                ListItem lstItem = new ListItem(readerNotes["NOTES"].ToString());
                ddlNotes.Items.Add(lstItem);
            }
            readerNotes.Close();

            // номер ведомости
            ddlSheet.Items.Clear();
            ddlSheet.Items.Add("(Все)");
            cmd.CommandText = @"select distinct sheet_year||'-'||sheet_number as sheet from otgruzka_sheet otgr
                left join STOREHOUSES_OF_PIPES_BY_NUM sp on SP.PIPENUMBER =  otgr.pipe_number and sp.edit_state=0
                where otgr.edit_state = 0 and sheet_year is not null and sheet_number is not null and sp.row_id is not null";
            if (ddlZone.SelectedIndex > 1) cmd.CommandText += " and SP.ZONE_NAME = '" + ddlZone.SelectedItem.Text + "'";
            cmd.CommandText += " order by sheet_year||'-'||sheet_number";
            OleDbDataReader rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                ddlSheet.Items.Add(rdr["sheet"].ToString());
            }
            rdr.Close();
            rdr.Dispose();

            cmd.Dispose();
        }
        catch (Exception ex)
        {
            //Вывод сообщения об ошибке
            Master.AddErrorMessage("Ошибка заполнения всплывающих списков", ex);
        }

        if (SelInd_ddlPocket < (ddlPocket_name.Items.Count)) ddlPocket_name.SelectedIndex = SelInd_ddlPocket;
        else ddlPocket_name.SelectedIndex = 0;




    }

    //получение времени начала интервала по дате и индексу смены
    //strDate - дата в формате "dd.mm.yyyy", iShift - индекс смены (0-2)
    protected DateTime GetTimestampByDateAndShift(String strDate, int iShift, int ThisIsEnd)
    {
        DateTime Date = new DateTime();
        if (!DateTime.TryParseExact(strDate.Trim(), "dd.MM.yyyy", new DateTimeFormatInfo(), DateTimeStyles.None, out Date))
        {
            Date = DateTime.Today;
        }
        Date = Date.AddHours(-1 + (iShift + ThisIsEnd) * 8);
        return Date;
    }

    //обработчик выбора нового названия складского объекта
    protected void ddlZone_SelectedIndexChanged(object sender, EventArgs e)
    {
        FillDDLStack(ddlZone.SelectedItem.Text);
        SelectedZoneName = ddlZone.SelectedItem.Value;

        if (SelectedZoneName != "")
        {
            FillFiltersDDL();
        }

        ActivateTab(tdFilter);
        mvMainViews.Visible = true;
        mvViews.SetActiveView(vFilter);
    }

    //Метод устанавливает активный стиль указанной вкладки, а все остальные делает неактивными
    protected void ActivateTab(HtmlTableCell active_tab)
    {
        tdManualInput.Attributes["class"] = "DeactivatedTabStyle";
        tdOperationsHistory.Attributes["class"] = "DeactivatedTabStyle";
        tdFilter.Attributes["class"] = "DeactivatedTabStyle";
        tdRelocation.Attributes["class"] = "DeactivatedTabStyle";
        tdPrihod.Attributes["class"] = "DeactivatedTabStyle";
        tdOtgruzka.Attributes["class"] = "DeactivatedTabStyle";


        active_tab.Attributes["class"] = "ActivatedTabStyle";
    }

    //Активация вкладки "Ввод данных"
    protected void btnManualInput_Click(object sender, EventArgs e)
    {
        mvMainViews.Visible = false;
        ActivateTab(tdManualInput);
        mvViews.SetActiveView(vSklad);
        SkladTable(false, "", 0, 100, false);
    }

    //Активация вкладки "История операций"
    protected void btnOperationsHistory_Click(object sender, EventArgs e)
    {
        ActivateTab(tdOperationsHistory);
        mvMainViews.Visible = false;
        mvViews.SetActiveView(vOperationsHistory);
    }
    
    //Активация вкладки "Автоматизированный ввод данных"
    protected void btnFilter_Click(object sender, EventArgs e)
    {
        ActivateTab(tdFilter);
        mvMainViews.Visible = true;
        mvViews.SetActiveView(vFilter);
    }

    protected void vOperationsHistory_Activate(object sender, EventArgs e)
    {
        CreateOperationsHistory();
    }

    protected void CreateOperationsHistory()
    {
        if (tblOperationsHistory.Rows.Count>1) tblOperationsHistory.Rows.RemoveAt(1);

        OleDbConnection T3Connect;
        try
        {
            T3Connect = Master.Connect.ORACLE_TESC3();
        }
        catch (Exception ex)
        {
            tblOperationsHistory.Visible = false;
            Master.AddErrorMessage(ex.Message, ex);
            return;
        }

        OleDbCommand cmd;
        try
        {
            cmd = T3Connect.CreateCommand();
            try
            {
                cmd.CommandText =
                    "select rownum as rownumber, operhistory.* " +
                    "from ( select to_char(storehouses_of_pipes_by_num.trx_date, 'DD.MM.YYYY HH24:MI:SS') as trxdate, " +
                      "spr_kadry.fio as emplfio, notes, case storehouses_of_pipes_by_num.original_rowid || '*' " +
                        "when '*' then case storehouses_of_pipes_by_num.edit_state " +
                            "when '0' then 'добавление' when '1' then 'добавление' when '2' then 'добавление' else 'нераспознана' end " +
                        "else " +
                          "case storehouses_of_pipes_by_num.edit_state when '0' then 'исправление' when '1' then 'исправление' when '2' then 'исправление' when '3' then 'удаление' " +
                            "when '7' then 'удаление дубликата' else 'нераспознана' end end as operationtype, " +
                      "case storehouses_of_pipes_by_num.original_rowid || '*' when '*' then 'номер трубы: ' || to_char(storehouses_of_pipes_by_num.pipenumber) || " +
                          "';<br/> штабель: ' || storehouses_of_pipes_by_num.stack_name || '; карман: ' || storehouses_of_pipes_by_num.pocket_num " +
                        "else case storehouses_of_pipes_by_num.edit_state when '3' then 'Номер трубы: ' || to_char(storehouses_old_rec.pipenumber) || '; штабель: ' || storehouses_old_rec.stack_name || " +
                                    "'; карман: ' || storehouses_old_rec.pocket_num when '7' then 'Номер трубы: ' || to_char(storehouses_old_rec.pipenumber) || " +
                                    "'; штабель: ' || storehouses_old_rec.stack_name || '; карман: ' || storehouses_old_rec.pocket_num " +
                                "else 'Номер трубы исходный: ' || to_char(storehouses_old_rec.pipenumber) || " +
                                    "', новый: ' || to_char(storehouses_of_pipes_by_num.pipenumber) || '; штабель исходный: ' || storehouses_old_rec.stack_name || " +
                                    "', новый: ' || storehouses_of_pipes_by_num.stack_name || '; карман исходный: ' || storehouses_old_rec.pocket_num || " +
                                    "', новый: ' || storehouses_of_pipes_by_num.pocket_num end end as changeslist from storehouses_of_pipes_by_num " +
                      "left join spr_kadry on storehouses_of_pipes_by_num.employer_number = spr_kadry.ntab " +
                      "left join (select row_id, pipenumber, stack_name, pocket_num from storehouses_of_pipes_by_num) storehouses_old_rec " +
                      "on storehouses_of_pipes_by_num.original_rowid = storehouses_old_rec.row_id " +
                      "where (storehouses_of_pipes_by_num.trx_date >  current_date - ?/24) " +
                      (ddlZone.SelectedIndex > 1 ? "and (storehouses_of_pipes_by_num.zone_name = ?) " : "") +
                      (ddlStack_name.SelectedIndex > 0 ? "and (storehouses_of_pipes_by_num.stack_name " + (cbxStack.Checked?"<>":"=") + " ?) " : "") +
                      (ddlPocket_name.SelectedIndex > 0 ? "and (storehouses_of_pipes_by_num.pocket_num " + (cbxPocket.Checked ? "<>" : "=") + " ?) " : "") +
                      (tbFilterPipeNumber.Text != "" ? "and (storehouses_of_pipes_by_num.pipenumber = ?) " : "") +
                      "order by storehouses_of_pipes_by_num.trx_date, emplfio, operationtype, changeslist " +
                    ") operhistory " +
                    "order by rownumber desc ";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("COUNT_OF_LAST_HOURS", System.Convert.ToInt32(ddlCountOfLastHours.SelectedValue));
                if (ddlZone.SelectedIndex > 1) cmd.Parameters.AddWithValue("ZONE", ddlZone.SelectedValue);
                if (ddlStack_name.SelectedIndex > 0) cmd.Parameters.AddWithValue("STACK", ddlStack_name.SelectedValue);
                if (ddlPocket_name.SelectedIndex > 0) cmd.Parameters.AddWithValue("POCKET", ddlPocket_name.SelectedValue);
                if (tbFilterPipeNumber.Text != "") cmd.Parameters.AddWithValue("pipenumber", tbFilterPipeNumber.Text);
                OleDbDataReader rdr = null;
                try
                {
                    rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        TableRow row = new TableRow(); row.HorizontalAlign = HorizontalAlign.Center;
                        row.Attributes["onmouseover"] = "this.style.textDecoration='underline'; this.style.backgroundColor='#E6E6F0'";
                        row.Attributes["onmouseout"] = "this.style.textDecoration='none'; this.style.backgroundColor=''";
                        TableCell cell;
                        //№ п/п
                        cell = new TableCell(); cell.Text = rdr["rownumber"].ToString(); cell.Width = new Unit("50px"); cell.BorderColor = System.Drawing.Color.Black; cell.BorderStyle = BorderStyle.Solid; cell.BorderWidth = new Unit("1px"); row.Cells.Add(cell);
                        //Дата
                        cell = new TableCell(); cell.Text = rdr["trxdate"].ToString(); cell.Width = new Unit("130px"); cell.BorderColor = System.Drawing.Color.Black; cell.BorderStyle = BorderStyle.Solid; cell.BorderWidth = new Unit("1px"); row.Cells.Add(cell);
                        //Оператор
                        cell = new TableCell(); cell.Text = rdr["emplfio"].ToString(); cell.Width = new Unit("200px"); cell.BorderColor = System.Drawing.Color.Black; cell.BorderStyle = BorderStyle.Solid; cell.BorderWidth = new Unit("1px"); row.Cells.Add(cell);
                        //Тип операции
                        cell = new TableCell(); cell.Text = rdr["operationtype"].ToString(); cell.Width = new Unit("100px"); cell.BorderColor = System.Drawing.Color.Black; cell.BorderStyle = BorderStyle.Solid; cell.BorderWidth = new Unit("1px"); row.Cells.Add(cell);
                        //Дополнительная информация
                        cell = new TableCell(); cell.Text = rdr["changeslist"].ToString(); cell.Width = new Unit("450px"); cell.BorderColor = System.Drawing.Color.Black; cell.BorderStyle = BorderStyle.Solid; cell.BorderWidth = new Unit("1px"); row.Cells.Add(cell);
                        //Примечание
                        cell = new TableCell(); cell.Text = rdr["notes"].ToString(); cell.Width = new Unit("300px"); cell.BorderColor = System.Drawing.Color.Black; cell.BorderStyle = BorderStyle.Solid; cell.BorderWidth = new Unit("1px"); row.Cells.Add(cell);
                        tblOperationsHistory.Rows.Add(row);
                    }
                }
                catch (Exception ex)
                {
                    Master.AddErrorMessage(ex.Message, ex);
                }
                finally
                {
                    if (rdr != null)
                    {
                        rdr.Close();
                        rdr.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                Master.AddErrorMessage(ex.Message, ex);
            }
            finally
            {
                cmd.Dispose();
            }
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage(ex.Message, ex);
        }
        
        tblOperationsHistory.Visible = (tblOperationsHistory.Rows.Count > 1);
        if (!tblOperationsHistory.Visible) Master.AddErrorMessage("Отсутствуют данные о проведениии каких-либо операций за последние " + ddlCountOfLastHours.SelectedValue + " часов");
    
    }

    protected void vOperationsHistory_PreRender(object sender, EventArgs e)
    {
        CreateOperationsHistory();
    }

    #region *** ПРИХОД ********************************************************************************

    protected void btnPrihod_Click(object sender, EventArgs e)
    {
        //ddlPrihodZone.SelectedItem.Text = ddlZone.SelectedItem.Text;
        //ddlPrihodStack.SelectedItem.Text = ddlStack_name.SelectedItem.Text;
        //ddlPrihodPocket.SelectedItem.Text = ddlPocket_name.SelectedItem.Text;
        mvMainViews.Visible = false;
        ActivateTab(tdPrihod);
        mvViews.SetActiveView(vPrihod);
        PrihodTable(false, "", false);
    }

    protected void ddlStack_Change(object sender, EventArgs e)
    {
        ddlPocket_name.SelectedIndex = 0;
        FillDDLStack(ddlZone.SelectedItem.Text, ddlStack_name.SelectedItem.Text);
        
    }

    protected void FillDDLStack(String Zone)
    {
        try
        {
            //Подключение к БД
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();

            cmd.CommandText = "select stack_name from SPR_ALL_STACKS where ZONE_NAME = ? and USE_FOR_BYPIPESNUMS = 1 and IS_ACTIVE = 1 order by stack_name ";
            cmd.Parameters.AddWithValue("ZONE_NAME", Zone);
            OleDbDataReader readerStacks = cmd.ExecuteReader();
            ddlStack_name.Items.Clear();
            ddlStack_name.Items.Add("");

            //Запись списка штабелей в лист по штабям
            while (readerStacks.Read())
            {
                ddlStack_name.Items.Add(new ListItem(readerStacks["stack_name"].ToString(), readerStacks["stack_name"].ToString()));
            }
            readerStacks.Dispose();
            readerStacks.Close();
            conn.Close();
            //PrihodTable(false, "", false);
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка", ex);
        }
    }

    protected void FillDDLStack(String Zone, String Stack)
    {
        try
        {
            //Подключение к БД
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();

            //Формирование запроса на получение количества карманов в текущем штабеле
            cmd.CommandText = "select Count_Of_Pockets from SPR_ALL_STACKS where (Stack_Name =?) and (ZONE_NAME= ?) and IS_ACTIVE = 1 order by Stack_Name, Count_Of_Pockets ";
            cmd.Parameters.AddWithValue("Stack_Name", Stack);
            cmd.Parameters.AddWithValue("ZONE_NAME", Zone);
            OleDbDataReader readerStacks = cmd.ExecuteReader();
            ddlPocket_name.Items.Clear();
            ddlPocket_name.Items.Add("");
            //Запись списка штабелей в лист по штабям
            if (readerStacks.Read())
            {
                int maxindex = System.Convert.ToInt32(readerStacks["Count_Of_Pockets"]);
                for (int index = 1; index <= maxindex; index++)
                {
                    ListItem lstItem = new ListItem(index.ToString(), index.ToString());
                    //tmpddlPocket.Items.Add(lstItem);
                    ddlPocket_name.Items.Add(lstItem);
                }
            }
            readerStacks.Close();
            cmd.Dispose();
            conn.Close();
            //PrihodTable(false, "", false);
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка", ex);
        }
    }
    /// <summary>
    /// Функция возвращает значения наполненности штабеля/кармана
    /// </summary>
    /// <param name="zone">Складской объект</param>
    /// <param name="stack">Штабель</param>
    /// <param name="pocket">Карман</param>
    /// <param name="Label">Лэйбл для подкраски</param>
    protected void ColorPipes(String zone, String stack, String pocket, Label Label)
    {
        OleDbConnection conn = Master.Connect.ORACLE_TESC3();
        OleDbCommand cmd = conn.CreateCommand();
        try
        {
            int CountPIPE = 0;
            int count_diameter = 0;
            double countPercent = 0;
            int MinPercent = 0;
            int MaxPercent = 0;
            string EmptyKarmanColor = "";
            string MaxKarmanColor = "";
            string NormalKarmanColor = "";

            String CountSQL = @"select count(*) from STOREHOUSES_OF_PIPES_BY_NUM where edit_state=0 and ZONE_NAME=? and STACK_NAME=?";
            if (pocket != "") CountSQL += " and pocket_num=?";
            cmd.CommandText = CountSQL;
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("zone_name", zone);
            cmd.Parameters.AddWithValue("stack_name", stack);
            if(pocket != "") cmd.Parameters.AddWithValue("pocket_name", pocket);

            CountPIPE = Convert.ToInt32(cmd.ExecuteScalar());
            if (pocket == "")
            {
                #region запрос на покраску последнего пакета
                String ColorPocket = @"
            SELECT ZONE_NAME, STACK_NAME, POCKET_NUM, DIAMETER, COUNT_PIPE_PLAN, COUNT_DIAMETER, COUNT_PIPE_FACT, COUNT_PERCENT
            FROM ( SELECT SOPBN.NZP,
          SAS.ID AS ID_STACK,
          SAS.ZONE_NAME,
          SAS.STACK_NAME,
          SOPBN.POCKET_NUM,
          SOPBN.PIPENUMBER,
          SFS.TR_DIAMETER,
          CASE WHEN SOPBN.NZP = 0 THEN CASE WHEN INV.S_SIZE1 IS NOT NULL AND INV.S_SIZE2 IS NOT NULL THEN  TO_CHAR (INV.S_SIZE1) || 'x' || TO_CHAR (INV.S_SIZE2)
                ELSE NVL (TO_CHAR (INV.DIAMETER),TO_CHAR (SOPBN.PIPE_DIAMETER)) END
             ELSE CASE WHEN     PROFILE_SIZE_A IS NOT NULL AND PROFILE_SIZE_B IS NOT NULL THEN TO_CHAR (PROFILE_SIZE_A) || 'x' || TO_CHAR (PROFILE_SIZE_B) 
                ELSE NVL (TO_CHAR (INV_NZP.D_NZP), TO_CHAR (SOPBN.PIPE_DIAMETER)) END
            END AS DIAMETER,
          SAS.COUNT_OF_POCKETS,
          NVL (SFS.TR_POCKET_QT, -1) AS COUNT_PIPE_PLAN,
          COUNT (DISTINCT NVL (INV.DIAMETER,NVL (INV_NZP.D_NZP, SOPBN.PIPE_DIAMETER)))
          OVER (PARTITION BY SOPBN.ZONE_NAME, SAS.STACK_NAME, SOPBN.POCKET_NUM) AS COUNT_DIAMETER,
          COUNT (SOPBN.PIPENUMBER)
          OVER (PARTITION BY SOPBN.ZONE_NAME,SAS.STACK_NAME,SOPBN.POCKET_NUM,
          NVL (INV.DIAMETER,NVL (INV_NZP.D_NZP, SOPBN.PIPE_DIAMETER)))AS COUNT_PIPE_FACT,
          CASE WHEN SFS.TR_POCKET_QT IS NOT NULL
                  AND COUNT (DISTINCT NVL (INV.DIAMETER,NVL (INV_NZP.D_NZP, SOPBN.PIPE_DIAMETER)))
                      OVER (PARTITION BY SOPBN.ZONE_NAME, SAS.STACK_NAME, SOPBN.POCKET_NUM) = 1
             THEN ROUND (100/ (  SFS.TR_POCKET_QT/ COUNT (SOPBN.PIPENUMBER)
                        OVER (PARTITION BY SOPBN.ZONE_NAME,SAS.STACK_NAME,SOPBN.POCKET_NUM,
                                        NVL (INV.DIAMETER,NVL (INV_NZP.D_NZP,SOPBN.PIPE_DIAMETER)))),1)                                
             ELSE 0 END AS COUNT_PERCENT
            FROM TESC3.SPR_ALL_STACKS SAS
          LEFT JOIN STOREHOUSES_OF_PIPES_BY_NUM SOPBN
             ON     SAS.ZONE_NAME = SOPBN.ZONE_NAME
                AND SAS.STACK_NAME = SOPBN.STACK_NAME
                AND SOPBN.EDIT_STATE = 0
          LEFT JOIN TESC3.SPR_FILL_SGP SFS
             ON     SAS.ID = SFS.ID_STACK
                AND SOPBN.POCKET_NUM = SFS.NUMBER_POCKET
                AND TO_CHAR (SOPBN.PIPE_DIAMETER) = SFS.TR_DIAMETER
                AND SFS.IS_ACTIVE = 1
          LEFT JOIN
          (SELECT MOD (PS.BATCH_YEAR, 100)|| TRIM (TO_CHAR (SUBSTR (PS.BATCH, INSTR (PS.BATCH, '-') + 1),'000000'))PIPE_NUMBER,
                  PI.DIAMETER,
                  PI.S_SIZE1,
                  PI.S_SIZE2
             FROM ORACLE.Z_PIPE_STORAGE PS
                  LEFT JOIN ORACLE.V_T3_PIPE_ITEMS PI
                     ON TO_NUMBER (PS.MATERIAL) = PI.INVENTORY_ITEM_ID
            WHERE PS.ORG_ID = 127 AND PS.ENTRY_QNT_T > 0) INV
             ON TO_CHAR (SOPBN.PIPENUMBER) = INV.PIPE_NUMBER
          LEFT JOIN (SELECT DISTINCT DIAMETER D_NZP,
                           PIPE_NUMBER P_NZP,
                           PROFILE_SIZE_A,
                           PROFILE_SIZE_B
             FROM INSPECTION_PIPES
            WHERE EDIT_STATE = 0 AND (NEXT_DIRECTION = 'SKLAD')) INV_NZP
             ON TO_CHAR (SOPBN.PIPENUMBER) = INV_NZP.P_NZP
            WHERE SAS.IS_ACTIVE = 1 AND SOPBN.ZONE_NAME = ? AND SOPBN.STACK_NAME = ? AND SOPBN.POCKET_NUM is not null
            and SOPBN.pocket_num = (select max(pocket_num) from TESC3.STOREHOUSES_OF_PIPES_BY_NUM where edit_state = 0 and ZONE_NAME = ? and STACK_NAME = ?))
            GROUP BY ZONE_NAME, STACK_NAME, POCKET_NUM, DIAMETER, COUNT_PIPE_PLAN, COUNT_DIAMETER, COUNT_PIPE_FACT, COUNT_PERCENT
            ORDER BY ZONE_NAME ASC";
                cmd.Parameters.Clear();
                cmd.CommandText = ColorPocket;
                cmd.Parameters.AddWithValue("ZONE_NAME", zone);
                cmd.Parameters.AddWithValue("STACK_NAME", stack);
                cmd.Parameters.AddWithValue("ZONE_NAME1", zone);
                cmd.Parameters.AddWithValue("STACK_NAME1", stack);
                OleDbDataReader reader1 = cmd.ExecuteReader();

                if (reader1.HasRows)
                {
                    if (reader1.Read())
                    {
                        Double.TryParse(reader1["COUNT_PERCENT"].ToString(), out countPercent);
                    }
                }
                reader1.Close();
                reader1.Dispose();

                
                #endregion
            }
            else
            {
                #region запрос на покраску

                if (CountPIPE != 0)
                {
                    String ColorPocket = @"
            SELECT ZONE_NAME, STACK_NAME, POCKET_NUM, DIAMETER, COUNT_PIPE_PLAN, COUNT_DIAMETER, COUNT_PIPE_FACT, COUNT_PERCENT
            FROM ( SELECT SOPBN.NZP,
          SAS.ID AS ID_STACK,
          SAS.ZONE_NAME,
          SAS.STACK_NAME,
          SOPBN.POCKET_NUM,
          SOPBN.PIPENUMBER,
          SFS.TR_DIAMETER,
          CASE WHEN SOPBN.NZP = 0 THEN CASE WHEN INV.S_SIZE1 IS NOT NULL AND INV.S_SIZE2 IS NOT NULL THEN  TO_CHAR (INV.S_SIZE1) || 'x' || TO_CHAR (INV.S_SIZE2)
                ELSE NVL (TO_CHAR (INV.DIAMETER),TO_CHAR (SOPBN.PIPE_DIAMETER)) END
             ELSE CASE WHEN     PROFILE_SIZE_A IS NOT NULL AND PROFILE_SIZE_B IS NOT NULL THEN TO_CHAR (PROFILE_SIZE_A) || 'x' || TO_CHAR (PROFILE_SIZE_B)
                ELSE NVL (TO_CHAR (INV_NZP.D_NZP), TO_CHAR (SOPBN.PIPE_DIAMETER)) END
            END AS DIAMETER,
          SAS.COUNT_OF_POCKETS,
          NVL (SFS.TR_POCKET_QT, -1) AS COUNT_PIPE_PLAN,
          COUNT (DISTINCT NVL (INV.DIAMETER,NVL (INV_NZP.D_NZP, SOPBN.PIPE_DIAMETER)))
          OVER (PARTITION BY SOPBN.ZONE_NAME, SAS.STACK_NAME, SOPBN.POCKET_NUM) AS COUNT_DIAMETER,
          COUNT (SOPBN.PIPENUMBER)
          OVER (PARTITION BY SOPBN.ZONE_NAME,SAS.STACK_NAME,SOPBN.POCKET_NUM,
          NVL (INV.DIAMETER,NVL (INV_NZP.D_NZP, SOPBN.PIPE_DIAMETER)))AS COUNT_PIPE_FACT,
          CASE WHEN SFS.TR_POCKET_QT IS NOT NULL
                  AND COUNT (DISTINCT NVL (INV.DIAMETER,NVL (INV_NZP.D_NZP, SOPBN.PIPE_DIAMETER)))
                      OVER (PARTITION BY SOPBN.ZONE_NAME, SAS.STACK_NAME, SOPBN.POCKET_NUM) = 1
             THEN ROUND (100/ (  SFS.TR_POCKET_QT/ COUNT (SOPBN.PIPENUMBER)
                        OVER (PARTITION BY SOPBN.ZONE_NAME,SAS.STACK_NAME,SOPBN.POCKET_NUM,
                                        NVL (INV.DIAMETER,NVL (INV_NZP.D_NZP,SOPBN.PIPE_DIAMETER)))),1)                                
             ELSE 0 END AS COUNT_PERCENT
            FROM TESC3.SPR_ALL_STACKS SAS
          LEFT JOIN STOREHOUSES_OF_PIPES_BY_NUM SOPBN
             ON     SAS.ZONE_NAME = SOPBN.ZONE_NAME
                AND SAS.STACK_NAME = SOPBN.STACK_NAME
                AND SOPBN.EDIT_STATE = 0
          LEFT JOIN TESC3.SPR_FILL_SGP SFS
             ON     SAS.ID = SFS.ID_STACK
                AND SOPBN.POCKET_NUM = SFS.NUMBER_POCKET
                AND TO_CHAR (SOPBN.PIPE_DIAMETER) = SFS.TR_DIAMETER
                AND SFS.IS_ACTIVE = 1
          LEFT JOIN
          (SELECT MOD (PS.BATCH_YEAR, 100)|| TRIM (TO_CHAR (SUBSTR (PS.BATCH, INSTR (PS.BATCH, '-') + 1),'000000'))PIPE_NUMBER,
                  PI.DIAMETER,
                  PI.S_SIZE1,
                  PI.S_SIZE2
             FROM ORACLE.Z_PIPE_STORAGE PS
                  LEFT JOIN ORACLE.V_T3_PIPE_ITEMS PI
                     ON TO_NUMBER (PS.MATERIAL) = PI.INVENTORY_ITEM_ID
            WHERE PS.ORG_ID = 127 AND PS.ENTRY_QNT_T > 0) INV
             ON TO_CHAR (SOPBN.PIPENUMBER) = INV.PIPE_NUMBER
          LEFT JOIN (SELECT DISTINCT DIAMETER D_NZP,
                           PIPE_NUMBER P_NZP,
                           PROFILE_SIZE_A,
                           PROFILE_SIZE_B
             FROM INSPECTION_PIPES
            WHERE EDIT_STATE = 0 AND (NEXT_DIRECTION = 'SKLAD')) INV_NZP
             ON TO_CHAR (SOPBN.PIPENUMBER) = INV_NZP.P_NZP
            WHERE SAS.IS_ACTIVE = 1 AND SOPBN.ZONE_NAME = ? AND SOPBN.STACK_NAME = ? AND SOPBN.POCKET_NUM = ?)
            GROUP BY ZONE_NAME, STACK_NAME, POCKET_NUM, DIAMETER, COUNT_PIPE_PLAN, COUNT_DIAMETER, COUNT_PIPE_FACT, COUNT_PERCENT
            ORDER BY ZONE_NAME ASC";
                    cmd.Parameters.Clear();
                    cmd.CommandText = ColorPocket;
                    cmd.Parameters.AddWithValue("ZONE_NAME", zone);
                    cmd.Parameters.AddWithValue("STACK_NAME", stack);
                    cmd.Parameters.AddWithValue("POCKET_NUM", pocket);
                    OleDbDataReader reader1 = cmd.ExecuteReader();

                    if (reader1.HasRows)
                    {
                        if (reader1.Read())
                        {
                            Double.TryParse(reader1["COUNT_PERCENT"].ToString(), out countPercent);
                        }
                    }
                    reader1.Close();
                    reader1.Dispose();

 
                }

                #endregion
            }

            cmd.CommandText = @"SELECT (SELECT INTEGER_VALUE FROM AUX_CONSTS WHERE CONST_ID='EMPTY_KARMAN_VALUE') AS EMPTY_KARMAN_VALUE,--пустой карман меньше
                                       (SELECT INTEGER_VALUE FROM AUX_CONSTS WHERE CONST_ID='MAX_KARMAN_VALUE') AS MAX_KARMAN_VALUE     --максимально заполненный карман больше
                                FROM DUAL";
            cmd.Parameters.Clear();
            OleDbDataReader reader2 = cmd.ExecuteReader();

            if (reader2.Read())
            {
                MinPercent = Convert.ToInt32(reader2["EMPTY_KARMAN_VALUE"]);
                MaxPercent = Convert.ToInt32(reader2["MAX_KARMAN_VALUE"]);
            }

            reader2.Close();
            reader2.Dispose();
            cmd.Dispose();

            if (CountPIPE != 0)
            {
                if (vPrihod.Visible)
                {
                    if (countPercent == 0) { Label.BackColor = Color.WhiteSmoke; }
                    else if (countPercent < MinPercent) { Label.BackColor = Color.LimeGreen; }
                    else if (countPercent >= MinPercent && countPercent <= MaxPercent) { Label.BackColor = Color.Yellow; }
                    else if (countPercent > MaxPercent) { Label.BackColor = Color.Red; }
                }
                else if (vRelocation.Visible)
                {
                    if (countPercent == 0) { Label.BackColor = Color.WhiteSmoke; }
                    else if (countPercent < MinPercent) { Label.BackColor = Color.LimeGreen; }
                    else if (countPercent >= MinPercent && countPercent <= MaxPercent) { Label.BackColor = Color.Yellow; }
                    else if (countPercent > MaxPercent) { Label.BackColor = Color.Red; }
                }
                else if (vSklad.Visible)
                {
                    if (countPercent == 0) { Label.BackColor = Color.WhiteSmoke; }
                    else if (countPercent < MinPercent) { Label.BackColor = Color.LimeGreen; }
                    else if (countPercent >= MinPercent && countPercent <= MaxPercent) { Label.BackColor = Color.Yellow; }
                    else if (countPercent > MaxPercent) { Label.BackColor = Color.Red; }
                }
            }
            
            if (CountPIPE == 0)
            {
                if (vPrihod.Visible) { Label.BackColor = Color.LimeGreen; }
                else if (vRelocation.Visible) { Label.BackColor = Color.LimeGreen; }
                else if (vSklad.Visible) { Label.BackColor = Color.LimeGreen; }
            }
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка", ex);
            Error = "Ошибка: " + ex.Message;
        }
    }

    //Отображение таблицы приход
    protected void PrihodTable(bool Edit, String Row_Id, bool bFill)
    {
        numRowEdit = 0;
        ddlPrihodDefect.SelectedItem.Text = "";
        tbPrihodPipeNumber.Text = "";
        tbPrihodNotes.Text = "";
        try
        {
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();

            //покраска значений количества труб в штабеле/пакете как в мнемосхеме
            ColorPipes(ddlZone.SelectedItem.Text, ddlStack_name.SelectedItem.Text, ddlPocket_name.SelectedItem.Text, LabelPrihod);

            //подсчет количества труб для штабеля/кармана
            CountPipesColor(LabelPrihod, ddlZone.SelectedItem.Text, ddlStack_name.SelectedItem.Text, ddlPocket_name.SelectedItem.Text);

            //Фильтрация
            String StackTemp = ddlStack_name.SelectedItem.Text;
            String PocketTemp = ddlPocket_name.SelectedItem.Text;
            String PipeNumberTemp = tbFilterPipeNumber.Text;
            if (StackTemp != "(Все)")
                StackTemp = " = '" + StackTemp + "'";
            else
                StackTemp = " is not null ";
            if (PocketTemp != "(Все)")
                PocketTemp = " = '" + PocketTemp + "'";
            else
                PocketTemp = " is not null ";
            if (PipeNumberTemp != "")
                PipeNumberTemp = " = '" + PipeNumberTemp + "'";
            else
                PipeNumberTemp = " is not null ";

            //Условия выборки записей
            String Condition = "where edit_state = 0 and ORIGINAL_ROWID is null ";

            //Запрос на выборку
            String SQL = "select * from " +
                                "(select ta.*, rownum rn from " +
                         "(select ZONE_NAME,STACK_NAME,POCKET_NUM,PIPENUMBER,DEFECT,NOTES,FIO,TRX_DATE,ROW_ID, NZP from STOREHOUSES_OF_PIPES_BY_NUM " +
                            "left join spr_kadry on SPR_KADRY.USERNAME = STOREHOUSES_OF_PIPES_BY_NUM.OPERATOR_ID " + Condition +
                            "order by TRX_DATE desc) ta) " +
                                        "where rn > 0 and rn <= 10 order by TRX_DATE";
            cmd.CommandText = SQL;
            OleDbDataReader reader = cmd.ExecuteReader();
            TableRow Row;
            TableCell Cell;
            int countRow = 0;

            //Шапка таблицы
            Row = new TableRow();
            Cell = new TableCell(); Cell.Text = "№ п/п"; Cell.Width = 20; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
            Cell = new TableCell(); Cell.Text = "Куда"; Cell.CssClass = "css_td"; Cell.Width = 300; Cell.Font.Bold = true; Row.Cells.Add(Cell);
            Cell = new TableCell(); Cell.Text = "Номер"; Cell.Width = 60; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
            Cell = new TableCell(); Cell.Text = "НЗП"; /*Cell.Width = 60*/; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
            Cell = new TableCell(); Cell.Text = "Дефект"; Cell.Width = 200; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
            Cell = new TableCell(); Cell.Text = "Примечание"; Cell.Width = 300; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
            Cell = new TableCell(); Cell.Text = "ФИО"; Cell.Width = 200; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
            Cell = new TableCell(); Cell.Text = "Дата"; Cell.Width = 130; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
            Cell = new TableCell(); Cell.Width = 20; Cell.CssClass = "css_td"; Row.Cells.Add(Cell);
            Cell = new TableCell(); Cell.Width = 20; Cell.CssClass = "css_td"; Row.Cells.Add(Cell);
            //Cell = new TableCell(); Cell.Width = 20; Cell.CssClass = "css_td"; Row.Cells.Add(Cell);
            Row.Style[HtmlTextWriterStyle.BackgroundColor] = "D3D3D3";
            tblPrihod.Rows.Add(Row);
            while (reader.Read())
            {
                //Тело таблицы
                countRow = countRow + 1;
                Row = new TableRow();
                Cell = new TableCell(); Cell.Text = countRow.ToString(); Cell.Width = 20; Cell.CssClass = "css_td"; Row.Cells.Add(Cell);
                Cell = new TableCell(); Cell.Text = "Складской объект " + reader["ZONE_NAME"].ToString() + ", штабель " + reader["STACK_NAME"].ToString() + ", карман " + reader["POCKET_NUM"].ToString(); Cell.Width = 300; Cell.CssClass = "css_td"; Row.Cells.Add(Cell);
                String num = GetPipeHistoryURL(reader["PIPENUMBER"].ToString());
                Cell = new TableCell(); Cell.Text = num; Cell.Width = 60; Cell.CssClass = "css_td"; Row.Cells.Add(Cell);
                String nzp = ""; if (reader["NZP"].ToString() == "1") nzp = "*";
                Cell = new TableCell(); Cell.Text = nzp; /*Cell.Width = 60*/; Cell.CssClass = "css_td"; Row.Cells.Add(Cell);
                Cell = new TableCell(); Cell.Text = reader["DEFECT"].ToString(); Cell.Width = 200; Cell.CssClass = "css_td"; Row.Cells.Add(Cell);
                Cell = new TableCell(); Cell.Text = reader["NOTES"].ToString(); Cell.Width = 300; Cell.CssClass = "css_td"; Row.Cells.Add(Cell);
                Cell = new TableCell(); Cell.Text = reader["FIO"].ToString(); Cell.Width = 200; Cell.CssClass = "css_td"; Row.Cells.Add(Cell);
                Cell = new TableCell(); Cell.Text = reader["TRX_DATE"].ToString(); Cell.Width = 130; Cell.CssClass = "css_td"; Row.Cells.Add(Cell);

                #region ///Если редактирование, то вставляем элементы "Сохранить" и "Отмена"
                if ((Edit == true) && (reader["ROW_ID"].ToString() == Row_Id))
                {
                    numRowEdit = countRow;
                    Cell = new TableCell();
                    Cell.Width = 20; Cell.CssClass = "css_td";
                    GetRollNameByZoneNameForEdit(ref RollNameForCurrentZone, reader["ZONE_NAME"].ToString());
                    Cell.Text = "<a title='Сохранить'><img src='Images/Confirm16x16.png' style=\"color: Black; text-decoration: none; border-style: none;\"/></a>";
                    Cell.Attributes["onclick"] = "__doPostBack('PRIHOD','EDIT#" + DateTime.Now.ToString() + "#" + reader["ROW_ID"].ToString() + "');";
                    Cell.Attributes["onmouseover"] = "this.style.cursor='hand'";
                    Row.Cells.Add(Cell);

                    Cell = new TableCell();
                    Cell.Width = 20; Cell.CssClass = "css_td";
                    GetRollNameByZoneNameForEdit(ref RollNameForCurrentZone, reader["ZONE_NAME"].ToString());
                    Cell.Text = "<a title='Отменить'><img src='Images/Cancel16x16.png' style=\"color: Black; text-decoration: none; border-style: none;\"/></a>";
                    Cell.Attributes["onclick"] = "__doPostBack('PRIHOD','CANCEL#" + DateTime.Now.ToString() + "');";
                    Cell.Attributes["onmouseover"] = "this.style.cursor='hand'";
                    Row.Cells.Add(Cell);

                    // автозаполнение
                    /* Cell = new TableCell();
                     Cell.Text = "<a title='Получить данные по номеру трубы'><img src='Images/bd16x16.gif' style=\"color: Black; text-decoration: none; border-style: none;\"/></a>";
                     Cell.Width = 20; Cell.CssClass = "css_td";
                     Cell.Attributes["onclick"] = "__doPostBack('PRIHOD','AutoFill#" + DateTime.Now.ToString() + "#" + reader["ROW_ID"].ToString() + "');";
                     Cell.Attributes["onmouseover"] = "this.style.cursor='hand'";
                     Row.Cells.Add(Cell);*/

                    tblPrihod.Rows.Add(Row);

                    SelectDDLItem(ddlPrihodDefect, tblPrihod.Rows[countRow].Cells[4]);
                    //добавление скрипта для размещения контролов в ячейках
                    String script = "";
                    script = "<script type=\"text/javascript\" language=\"javascript\"> "
                      + "function OnDocumentLoad(e) { InsertActAddControls(@R); AlertMessage();}; "
                      + "window.onload=OnDocumentLoad;"
                      + "</script>";
                    script = script.Replace("@R", countRow.ToString());
                    RegisterStartupScript("edit_controls_script", script);

                    tbPrihodPipeNumber.Attributes["onkeypress"] = "if (event.keyCode == 13) __doPostBack('PRIHOD','EDIT#" + DateTime.Now.ToString() + "#" + reader["ROW_ID"].ToString() + "');";
                    tbPrihodNotes.Attributes["onkeypress"] = "if (event.keyCode == 13) __doPostBack('PRIHOD','EDIT#" + DateTime.Now.ToString() + "#" + reader["ROW_ID"].ToString() + "');";
                    chbNZP.Checked = (nzp == "*");
                }
                #endregion
                #region ///Иначе, элементы "Редактировать" и "Удалить"
                else
                {
                    Cell = new TableCell();
                    Cell.Width = 20; Cell.CssClass = "css_td";
                    GetRollNameByZoneNameForEdit(ref RollNameForCurrentZone, reader["ZONE_NAME"].ToString());
                    if ((Authentification.CanEditData(RollNameForCurrentZone)))
                    {
                        Cell.Text = "<a title='Редактироовать'><img src='Images/Edit16x16.png' style=\"color: Black; text-decoration: none; border-style: none;\"/></a>";
                        Cell.Attributes["onclick"] = "__doPostBack('PRIHOD','INSERT_EDIT#" + DateTime.Now.ToString() + "#" + reader["ROW_ID"].ToString() + "')";
                        Cell.Attributes["onmouseover"] = "this.style.cursor='hand'";
                    }
                    Row.Cells.Add(Cell);

                    Cell = new TableCell();
                    Cell.Width = 20; Cell.CssClass = "css_td";
                    GetRollNameByZoneNameForEdit(ref RollNameForCurrentZone, reader["ZONE_NAME"].ToString());
                    if ((Authentification.CanEditData(RollNameForCurrentZone)))
                    {
                        Cell.Text = "<a title='Удалить'><img src='Images/Delete16x16.png' style=\"color: Black; text-decoration: none; border-style: none;\"/></a>";
                        Cell.Attributes["onclick"] = "if(confirm('Нажмите ОК для подтверждения удаления записи.')) __doPostBack('PRIHOD','DELETE#" + DateTime.Now.ToString() + "#" + reader["ROW_ID"].ToString() + "')";
                        Cell.Attributes["onmouseover"] = "this.style.cursor='hand'";
                    }
                    Row.Cells.Add(Cell);
                    tblPrihod.Rows.Add(Row);
                }
                #endregion

            }
            #region ///Если не редактирование, то вставляем строку добавления записи
            if (Edit == false)
            {
                Row = new TableRow();
                Cell = new TableCell(); Cell.Text = (countRow + 1).ToString(); Cell.Width = 20; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
                Cell = new TableCell(); Cell.Text = ""; Cell.Width = 180; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
                Cell = new TableCell(); Cell.Text = ""; Cell.Width = 60; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
                Cell = new TableCell(); Cell.Text = ""; Cell.Width = 250; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
                Cell = new TableCell(); Cell.Text = ""; Cell.Width = 250; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
                Cell = new TableCell(); Cell.Text = ""; Cell.Width = 200; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
                Cell = new TableCell(); Cell.Text = ""; Cell.Width = 130; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
                Cell = new TableCell(); Cell.Width = 20; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
                Cell = new TableCell(); Cell.Width = 20; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
                tblPrihod.Rows.Add(Row);

                //добавление скрипта для размещения контролов в ячейках
                String script = "";
                script = "<script type=\"text/javascript\" language=\"javascript\"> "
                  + "function OnDocumentLoad(e) { InsertActAddControls(@R); AlertMessage();}; "
                  + "window.onload=OnDocumentLoad;"
                  + "</script>";
                script = script.Replace("@R", (countRow + 1).ToString());
                RegisterStartupScript("edit_controls_script", script);

                //tbPrihodPipeNumber.Attributes["onkeypress"] = "if (window.event.keyCode == 13) {var evt = evt || window.event;   evt.cancelBubble = true; __doPostBack('ADD_PRIHOD');}";
                tbPrihodPipeNumber.Attributes["onkeypress"] = "if (event.keyCode == 13) __doPostBack('PRIHOD','ADD#" + DateTime.Now + "');";
                tbPrihodNotes.Attributes["onkeypress"] = "if (event.keyCode == 13) __doPostBack('PRIHOD','ADD#" + DateTime.Now + "');";
            }
            #endregion

            reader.Dispose();
            reader.Close();
            conn.Close();
            Error = "";
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка", ex);
            Error = "Ошибка: " + ex.Message;
        }
    }

    //Обработчик события приход труб
    protected void Prihod(bool bCheckDuplicates)
    {
        try
        {
            int pipenumber = 0;
            //Проверяем введенный номер
            String Zone = ddlZone.SelectedItem.Text;
            String Stack = ddlStack_name.SelectedItem.Text;
            String Pocket = ddlPocket_name.SelectedItem.Text;
            String PipeNumber = bCheckDuplicates ? tbPrihodPipeNumber.Text : VPipeNumber;
            String Defect = bCheckDuplicates ? ddlPrihodDefect.SelectedItem.Text : VDefects;
            String Notes = bCheckDuplicates ? tbPrihodNotes.Text : VNotes;
            Int32 NZP = bCheckDuplicates ? Convert.ToByte(chbNZP.Checked) : VNZP;
            GetRollNameByZoneNameForEdit(ref RollNameForCurrentZone, Zone);
            if (Authentification.CanAddData(RollNameForCurrentZone))
            {
                if ((System.Int32.TryParse(PipeNumber, out pipenumber)) && (Zone != "") && (Stack != "") && (Pocket != ""))
                {
                    using (OleDbCommand cmd = Master.Connect.ORACLE_TESC3().CreateCommand())
                    {
                        // проверка на дубликат и вывод информационного сообщения при нахождении с указанием всех мест
                        string msg = CheckPlaces(pipenumber, true, "");

                        //сохранение значений во ViewState
                        VOperation = "AddPrihod";
                        VPipeNumber = PipeNumber;
                        VDefects = ddlPrihodDefect.SelectedItem.Text;
                        VNotes = tbPrihodNotes.Text;

                        VNZP = Convert.ToByte(chbNZP.Checked);
                        if (msg != "")
                        {
                            if (bCheckDuplicates)
                            {
                                lblAddDupl.Text = msg + "\r\n Удалить ранние дубликаты трубы ?";
                                PopupWindow1.ContentPanelId = pnlConfirmAdd.ID;
                                PopupWindow1.Title = "Добавление трубы на склад";
                                pnlConfirmAdd.Visible = true;

                                PrihodTable(false, "", false);
                                return;
                            }
                            else
                            {
                                //удаление дубликатов
                                DeleteDuplicates(pipenumber, "");
                            }
                        }

                        //Проверка сортамента в выбранном штабеле по диаметру и госту
                        if (bCheckDuplicates)
                        {
                            String SQL2 = @"select distinct count(*) from STOREHOUSES_OF_PIPES_BY_NUM
                                            where edit_state = 0 and ZONE_NAME=? and STACK_NAME=? and POCKET_NUM=?";
                            cmd.Parameters.Clear();
                            cmd.CommandText = SQL2;
                            cmd.Parameters.AddWithValue("ZONE_NAME", ddlZone.SelectedItem.Text);
                            cmd.Parameters.AddWithValue("STACK_NAME", ddlStack_name.SelectedItem.Text);
                            cmd.Parameters.AddWithValue("POCKET_NUM", ddlPocket_name.SelectedItem.Text);
                            int CountP = 0;
                            CountP = Convert.ToInt32(cmd.ExecuteScalar());

                            if (CountP != 0)
                            {
                                String SQL1 = @"select diameter, pipe_number, case when GOST_GROUP is null then gost else gost || '-' || GOST_GROUP end gost
                            from inspection_pipes IP
                            where IP.pipe_number = ? and edit_state = 0
                            and trx_date = (select max(trx_date) from inspection_pipes where edit_state = 0 and pipe_number = ?)
                            and next_direction = 'SKLAD' or next_direction = 'remont'";
                                cmd.Parameters.Clear();
                                cmd.CommandText = SQL1;
                                cmd.Parameters.AddWithValue("PIPE_NUMBER", bCheckDuplicates ? tbPrihodPipeNumber.Text : VPipeNumber);
                                cmd.Parameters.AddWithValue("PIPE_NUMBER1", bCheckDuplicates ? tbPrihodPipeNumber.Text : VPipeNumber);
                                OleDbDataReader rdr = cmd.ExecuteReader();
                                string diam = "";
                                string gost = "";
                                string pn = "";
                                if (rdr.Read())
                                {
                                    diam = rdr["diameter"].ToString();
                                    gost = rdr["gost"].ToString();
                                    pn = rdr["PIPE_NUMBER"].ToString();
                                }

                                rdr.Close();
                                rdr.Dispose();

                                if (diam != "" || gost != "")
                                {
                                    cmd.CommandText = @"select count(*)
                                                        from (select pipenumber, diameter, gost
                                                              from (select DISTINCT SOPBN.ZONE_NAME, SOPBN.STACK_NAME, SOPBN.POCKET_NUM, PIPENUMBER, inv.diameter, INV.GOST, SOPBN.ROW_ID
                                                                    from STOREHOUSES_OF_PIPES_BY_NUM SOPBN
                                                        left join  (select MOD (PS.BATCH_YEAR, 100)||TRIM(TO_CHAR(SUBSTR (PS.BATCH, INSTR (PS.BATCH, '-') + 1), '000000'))  PIPE_NUMBER,
                                                                    PI.DIAMETER, case when PI.GRUP is null then PI.GOST
                                                                    else PI.GOST || '-' ||PI.GRUP end GOST, PS.ENTRY_QNT_T
                                                                    from ORACLE.Z_PIPE_STORAGE PS
                                                                    left join ORACLE.V_T3_PIPE_ITEMS PI on TO_NUMBER (PS.MATERIAL) = PI.INVENTORY_ITEM_ID
                                                                    where PS.ORG_ID = 127 and PS.entry_qnt_t>0
                                                                    ) INV  on to_char(SOPBN.PIPENUMBER) = INV.PIPE_NUMBER
                                                        left join inspection_pipes IP on to_char(SOPBN.PIPENUMBER) = IP.PIPE_NUMBER and IP.edit_state = 0
                                                        where sopbn.edit_state = 0
                                                        and SOPBN.zone_name = ? and SOPBN.stack_name = ? and SOPBN.pocket_num = ?
                                                        and inv.diameter = '" + diam + "' and INV.gost = '" + gost + "' ))";
                                    cmd.Parameters.Clear();
                                    cmd.Parameters.AddWithValue("ZONE_NAME", ddlZone.SelectedItem.Text);
                                    cmd.Parameters.AddWithValue("STACK_NAME", ddlStack_name.SelectedItem.Text);
                                    cmd.Parameters.AddWithValue("POCKET_NUM", ddlPocket_name.SelectedItem.Text);
                                    int countS = 0;

                                    countS = Convert.ToInt32(cmd.ExecuteScalar());

                                    if (countS == 0)
                                    {
                                        lblPrihodSort.Text = "Внимаение! В выбранном штабеле лежат трубы отличные от трубы:\r\n" + pn +" по диаметру: " + diam + " и НТД: " + gost +"\r\nДействителньо переместить выбранные трубы?";
                                        PopupWindow1.ContentPanelId = pnlPrihodSort.ID;
                                        PopupWindow1.Title = "Перемещение труб";
                                        pnlPrihodSort.Visible = true;
                                        return;
                                    }
                                }
                            }
                        }

                        //вставка новой записи
                        cmd.CommandText = "insert into STOREHOUSES_OF_PIPES_BY_NUM " +
                                            "(TRX_DATE, STACK_NAME, POCKET_NUM, " +
                                            "DEFECT, EMPLOYER_NUMBER, SHIFT_INDEX, " +
                                            "OPERATOR_ID, EDIT_STATE, ZONE_NAME, ROW_ID, PIPENUMBER, NOTES, NZP) " +
                                         "values " +
                                            "(sysdate, '" + Stack + "','" + Pocket + "', " +
                                            "'" + Defect + "',(select ntab from spr_kadry where username = '" +
                                            Authentification.User.UserName + "'),'" + Authentification.Shift + "', " + "'" +
                                            Authentification.User.UserName + "',0,'" + Zone + "',to_char(sysdate,'DD.MM.YYYY HH24:MI:SS')||'_" +
                                            Zone + "_" + Stack + "_" + Pocket + "_" + PipeNumber + "'," + PipeNumber + ",'" + Notes +
                                            " ', '" + NZP.ToString() + "')";
                        cmd.Parameters.Clear();
                        cmd.ExecuteNonQuery();

                        //очистка переменных ViewState
                        ClearStoreHousesViewState();
                    }
                }
                else
                {
                    Master.AddErrorMessage("Не указан складской объект.");
                    Error = "Не указан складской объект.";
                }
            }
            else
            {
                Master.AddErrorMessage("Недостаточно прав для добавления трубы на складской объект.");
                Error = "Недостаточно прав для добавления трубы на складской объект.";
            }

            PrihodTable(false, "", false);
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка добавленмя данных ", ex);
            Error = "Ошибка добавленмя данных. " + ex.Message;
        }
    }

    //Обработчик события редактирование прихода труб
    protected void EditPrihod(String RowId, bool bCheckDuplicates)
    {
        #region *** проверка на ошибки ввода *********************
        int PipeNumber = 0;
        int.TryParse(bCheckDuplicates ? tbPrihodPipeNumber.Text.Trim() : VPipeNumber, out PipeNumber);
        if (PipeNumber < 1000000 || PipeNumber >= 100000000)
        {
            Master.AlertMessage = "Номер трубы имеет неверный формат.\r\nНомер должен состоять из года (одна или 2 цифры) и самого номера (6 цифр) без пробелов и разделителей";
            return;
        }
        #endregion *** проверка на ошибки ввода *********************

        try
        {
            // проверка на дубликат и вывод информационного сообщения при нахождении с указанием всех мест
            string msg = CheckPlaces(PipeNumber, false, RowId);
            if (msg != "")
            {
                if (bCheckDuplicates)
                {
                    VOperation = "EditPrihod";
                    VPipeNumber = PipeNumber.ToString();
                    VDefects = ddlPrihodDefect.SelectedItem.Text;
                    VNotes = tbPrihodNotes.Text;
                    VNZP = Convert.ToByte(chbNZP.Checked);
                    VRowID = RowId;

                    lblAddDupl.Text = msg + "\r\n Удалить ранние дубликаты трубы?";
                    PopupWindow1.ContentPanelId = pnlConfirmAdd.ID;
                    PopupWindow1.Title = "Редактирование трубы";
                    pnlConfirmAdd.Visible = true;

                    PrihodTable(false, "", false);
                    return;
                }
                else
                {
                    DeleteDuplicates(PipeNumber, RowId);
                }
            }

            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();
            cmd.Transaction = conn.BeginTransaction();

            //Вставляем новую запись
            String SQL = "insert into STOREHOUSES_OF_PIPES_BY_NUM " +
                                "(TRX_DATE,DEFECT,EMPLOYER_NUMBER,SHIFT_INDEX,OPERATOR_ID, " +
                                "EDIT_STATE,ORIGINAL_ROWID,ROW_ID,NOTES,PIPENUMBER," +
                                "INVENTORY_CODE,PIPE_DIAMETER,PIPE_THICKNESS,PIPE_STEELMARK,PRESENTATION," +
                                "ZONE_NAME,STACK_NAME,POCKET_NUM,DESTINATION,SHOP,NZP) " +
                            "(select " +
                                "sysdate,?,?,?,?," +
                                "0,?,to_char(sysdate,'DD.MM.YYYY HH24:MI:SS')||'_'||ZONE_NAME||'_'||STACK_NAME||'_'||POCKET_NUM||'_'||PIPENUMBER,?,?," +
                                "INVENTORY_CODE,PIPE_DIAMETER,PIPE_THICKNESS,PIPE_STEELMARK,?," +
                                "ZONE_NAME,STACK_NAME,POCKET_NUM,DESTINATION,SHOP,? " +
                            "from STOREHOUSES_OF_PIPES_BY_NUM where ROW_ID = ? and EDIT_STATE = 0)";


            cmd.Parameters.Clear();
            cmd.CommandText = SQL;
            cmd.Parameters.AddWithValue("DEFECT", bCheckDuplicates ? ddlPrihodDefect.SelectedItem.Text : VDefects);
            cmd.Parameters.AddWithValue("EMPLOYER_NUMBER", Authentification.User.TabNumber);
            cmd.Parameters.AddWithValue("SHIFT_INDEX", Authentification.Shift);
            cmd.Parameters.AddWithValue("OPERATOR_ID", Authentification.User.UserName);
            cmd.Parameters.AddWithValue("ORIGINAL_ROWID", "");
            cmd.Parameters.AddWithValue("NOTES", bCheckDuplicates ? tbPrihodNotes.Text : VNotes);
            cmd.Parameters.AddWithValue("PIPENUMBER", Checking.GetDbType(bCheckDuplicates ? tbPrihodPipeNumber.Text : VPipeNumber));
            cmd.Parameters.AddWithValue("PRESENTATION", Checking.GetDbType(0.ToString()));
            cmd.Parameters.AddWithValue("NZP", bCheckDuplicates ? Convert.ToByte(chbNZP.Checked) : VNZP);
            cmd.Parameters.AddWithValue("ROW_ID", RowId);

            //Откатываем транзакцию если возникли ошибки
            try { cmd.ExecuteNonQuery(); }
            catch { cmd.Transaction.Rollback(); }

            //Обновляем старую запись
            SQL = "update STOREHOUSES_OF_PIPES_BY_NUM set EDIT_STATE = 1 where ROW_ID = ? and EDIT_STATE = 0";
            cmd.Parameters.Clear();
            cmd.CommandText = SQL;
            cmd.Parameters.AddWithValue("ROW_ID", RowId);

            //Откатываем транзакцию если возникли ошибки
            try { cmd.ExecuteNonQuery(); }
            catch { cmd.Transaction.Rollback(); }

            cmd.Transaction.Commit();
            Error = "";

            ClearStoreHousesViewState();
            PrihodTable(false, "", false);
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка", ex);
            Error = "Ошибка: " + ex.Message;
        }
    }

    #endregion *** ПРИХОД ********************************************************************************

    #region *** ПЕРЕМЕЩЕНИЕ ********************************************************************************

    protected void btnRelocation_Click(object sender, EventArgs e)
    {
        mvMainViews.Visible = false;
        ddlRelocationZone.SelectedIndex = 0;
        ddlRelocationStack.SelectedIndex = 0;
        ddlRelocationPocket.SelectedIndex = 0;
        ActivateTab(tdRelocation);
        mvViews.SetActiveView(vRelocation);
        if (ddlRelocationZone.SelectedIndex == 0 || ddlRelocationStack.SelectedIndex == 0 || ddlRelocationPocket.SelectedIndex == 0)
            LabelToCount.Text = "0";
    }

    protected void btnRelocationAdd_Click(object sender, EventArgs e)
    {
        /*Relocation(tbRelocationPipeNumber.Text, ddlZone.SelectedItem.Text, ddlStack.SelectedItem.Text, ddlPocket.SelectedItem.Text, ddlRelocationZone.SelectedItem.Text, ddlRelocationStack.SelectedItem.Text, ddlRelocationPocket.SelectedItem.Text);
        tbRelocationPipeNumber.Text = "";
        RelocationTable();*/
    }

    //Обработчик события при выборе складского объекта при перемещении
    protected void ddlRelocationZone_Change(object sender, EventArgs e)
    {
        FillDDLRelocationStack(ddlRelocationZone.SelectedItem.Text, ddlRelocationStack);
    }

    //Обработчик события при выборе штабеля объекта при перемещении
    protected void ddlRelocationStack_Change(object sender, EventArgs e)
    {
        FillDDLRelocationPocket(ddlRelocationZone.SelectedItem.Text, ddlRelocationStack.SelectedItem.Text, ddlRelocationPocket);
    }

    //Заполнение выпадающего списка по штабелям при перемещении
    protected void FillDDLRelocationStack(String Zone, DropDownList ddl)
    {
        try
        {
            //Подключение к БД
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();

            cmd.CommandText = "select stack_name from SPR_ALL_STACKS where ZONE_NAME = ? and USE_FOR_BYPIPESNUMS = 1 and IS_ACTIVE = 1 order by stack_name ";
            cmd.Parameters.AddWithValue("ZONE_NAME", Zone);
            OleDbDataReader readerStacks = cmd.ExecuteReader();
            ddl.Items.Clear();
            ddl.Items.Add("");

            //Запись списка штабелей в лист по штабям
            while (readerStacks.Read())
            {
                ddl.Items.Add(new ListItem(readerStacks["stack_name"].ToString(), readerStacks["stack_name"].ToString()));
            }
            readerStacks.Dispose();
            readerStacks.Close();
            conn.Close();
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка ", ex);
        }
    }
    //Заполнение выпадающего списка по карманам при перемещении
    protected void FillDDLRelocationPocket(String Zone, String Stack, DropDownList ddl)
    {
        try
        {
            //Подключение к БД
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();
            //Формирование запроса на получение количества карманов в текущем штабеле
            cmd.CommandText = "select Count_Of_Pockets from SPR_ALL_STACKS where (Stack_Name =?) and (ZONE_NAME= ?) and IS_ACTIVE = 1 order by Stack_Name, Count_Of_Pockets ";
            cmd.Parameters.AddWithValue("Stack_Name", Stack);
            cmd.Parameters.AddWithValue("ZONE_NAME", Zone);           
            OleDbDataReader readerStacks = cmd.ExecuteReader();
            ddl.Items.Clear();
            ddl.Items.Add("");
            //Запись списка штабелей в лист по штабям
            if (readerStacks.Read())
            {
                int maxindex = System.Convert.ToInt32(readerStacks["Count_Of_Pockets"]);
                for (int index = 1; index <= maxindex; index++)
                {
                    ListItem lstItem = new ListItem(index.ToString(), index.ToString());
                    //tmpddlPocket.Items.Add(lstItem);
                    ddl.Items.Add(lstItem);
                }
            }
            readerStacks.Close();
            cmd.Dispose();
            conn.Close();
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка", ex);
        }
    }

    protected void CountPipesColor(Label Label, string Zone, string Stack, string Pocket)
    {
        try
        {
            //подключение к БД

            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            using (OleDbCommand cmd = conn.CreateCommand())
            {
                // Считаем количество труб
                int CountPipes = 0;
                String CountPipesSQL = @"select count(*)
                                            from STOREHOUSES_OF_PIPES_BY_NUM
                                            where edit_state = 0 and zone_name = ? and stack_name = ? and pocket_num = ?";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("zone", Zone);
                cmd.Parameters.AddWithValue("stack", Stack);
                cmd.Parameters.AddWithValue("pocket", Pocket);

                cmd.CommandText = CountPipesSQL;
                OleDbDataReader rdr = cmd.ExecuteReader();
                if (rdr.Read())
                {
                    CountPipes = Convert.ToInt32(rdr["count(*)"]);
                }
                rdr.Close();
                rdr.Dispose();
                Label.Text = CountPipes.ToString();
            }
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка: " + ex);
        }

    }
    void BuildTblFrom()
    {
        if (ddlZone.SelectedIndex < 1 || ddlStack_name.SelectedIndex < 1 || ddlPocket_name.SelectedIndex < 1)
        {
            lblFrom.Visible = true;
            return;
        }

        lblFrom.Visible = false;

        int[] sz = new int[3] { 30, 120, 90 };
        Comm.TblBuilder tblBld = new Comm.TblBuilder(sz, tblFrom);
        TableRow row = tblBld.GetRowHead(new string[3] { "", "Дата", "Номер трубы" });
        CheckBox chbxAll = new CheckBox(); chbxAll.ID = "chbx_ALL"; chbxAll.Attributes["onclick"] = "SelectAll()";
        row.Cells[0].Controls.Add(chbxAll);
        tblBld.Align = new HorizontalAlign[3] { HorizontalAlign.Center, HorizontalAlign.Left, HorizontalAlign.Center };
        tblFrom.Rows.Add(row);

        try
        {
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();

            //покраска значений количества труб в штабеле/пакете как в мнемосхеме
            ColorPipes(ddlZone.SelectedItem.Text, ddlStack_name.SelectedItem.Text, ddlPocket_name.SelectedItem.Text, LabelFromCount);

            //Подсчет количества труб для штабеля/кармана
            CountPipesColor(LabelFromCount, ddlZone.SelectedItem.Text, ddlStack_name.SelectedItem.Text, ddlPocket_name.SelectedItem.Text);

            String SQL = "select row_id, PIPENUMBER, POCKET_NUM, trx_date from STOREHOUSES_OF_PIPES_BY_NUM where edit_state=0 and ZONE_NAME=? and STACK_NAME=?";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("zone", ddlZone.SelectedItem.Text);
            cmd.Parameters.AddWithValue("stack", ddlStack_name.SelectedItem.Text);
            if (ddlPocket_name.SelectedIndex > 0)
            {
                SQL += " and POCKET_NUM=?";
                cmd.Parameters.AddWithValue("pocket", ddlPocket_name.SelectedItem.Text);
            }
            SQL += " order by PIPENUMBER";

            cmd.CommandText = SQL;
            OleDbDataReader rdr = cmd.ExecuteReader();
            string hfldIDStr = "";
            while (rdr.Read())
            {
                CheckBox chbx = new CheckBox();
                chbx.ID = "chbx_" + rdr["row_id"].ToString();
                chbx.Attributes["onclick"] = "RemindChecked()";

                row = tblBld.GetRowBody(new string[3] { "", Convert.ToDateTime(rdr["trx_date"]).ToString("dd.MM.yyyy HH.mm").Replace(" ", "&nbsp;"), rdr["PIPENUMBER"].ToString() }, false);
                row.Cells[0].Controls.Add(chbx);

                tblFrom.Rows.Add(row);

                if (hfldIDStr != "") hfldIDStr += "~";
                hfldIDStr += chbx.ID;
            }

            rdr.Dispose();
            rdr.Close();
            cmd.Dispose();

            hfldSelect.Value = "";
            hfldID.Value = hfldIDStr;

        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка формирования таблицы перемещения 'Откуда' ", ex);
        }


    }

    void BuildTblTo()
    {
        if (ddlRelocationZone.SelectedIndex<1 || ddlRelocationStack.SelectedIndex < 1 || ddlRelocationPocket.SelectedIndex<1)
        {
            lblTo.Visible = true;
            return;
        }

        lblTo.Visible = false;

        int[] sz = new int[3] { 120, 90, 450 };
        Comm.TblBuilder tblBld = new Comm.TblBuilder(sz, tblTo);
        TableRow row = tblBld.GetRowHead(new string[3] {"Дата", "Номер трубы",  "примечания" });
        tblBld.Align = new HorizontalAlign[3] { HorizontalAlign.Left, HorizontalAlign.Center, HorizontalAlign.Left };
        tblTo.Rows.Add(row);

        try
        {
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();

            //покраска значений количества труб в штабеле/пакете как в мнемосхеме
            ColorPipes(ddlRelocationZone.SelectedItem.Text, ddlRelocationStack.SelectedItem.Text, ddlRelocationPocket.SelectedItem.Text, LabelToCount);

            //Подсчет количества труб для штабеля/кармана
            CountPipesColor(LabelToCount, ddlRelocationZone.SelectedItem.Text, ddlRelocationStack.SelectedItem.Text, ddlRelocationPocket.SelectedItem.Text);
            String SQL = "select row_id, PIPENUMBER, POCKET_NUM, notes, trx_date from STOREHOUSES_OF_PIPES_BY_NUM where edit_state=0 and ZONE_NAME=? and STACK_NAME=?";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("zone", ddlRelocationZone.SelectedItem.Text);
            cmd.Parameters.AddWithValue("stack", ddlRelocationStack.SelectedItem.Text);
            if (ddlRelocationPocket.SelectedIndex > 0)
            {
                SQL += " and POCKET_NUM=?";
                cmd.Parameters.AddWithValue("pocket", ddlRelocationPocket.SelectedItem.Text);
            }
            SQL += " order by PIPENUMBER";

            cmd.CommandText = SQL;
            OleDbDataReader rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                row = tblBld.GetRowBody(new string[3] { Convert.ToDateTime(rdr["trx_date"]).ToString("dd.MM.yyyy HH.mm").Replace(" ", "&nbsp;"), rdr["PIPENUMBER"].ToString(), rdr["notes"].ToString() }, false);
                // если перемещена из текущего места
                string notes = rdr["notes"].ToString();
              
                /*if (notes.IndexOf("Перемещена") != -1 && notes.IndexOf("Перемещена") != -1 && notes.IndexOf("объекта " + ddlZone.SelectedItem.Text) != -1 && notes.IndexOf("штабель " + ddlRelocationStack0.SelectedItem.Text) != -1 && (ddlRelocationPocket0.SelectedIndex < 1 || notes.IndexOf("карман " + ddlRelocationPocket0.SelectedItem.Text) != -1))
                {
                    row.Style[HtmlTextWriterStyle.BackgroundColor] = "#DDE3FF";
                }*/

                tblTo.Rows.Add(row);
            }

            rdr.Dispose();
            rdr.Close();
            cmd.Dispose();

        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка формирования таблицы перемещения 'Куда' ", ex);
        }
    }

    //Создание таблицы перемещений
    protected void RelocationTable()
    {
        try
        {
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();

            String ZoneTemp = ddlZone.SelectedItem.Text;
            String StackTemp = ddlStack_name.SelectedItem.Text;
            String PocketTemp = ddlPocket_name.SelectedItem.Text;
            String PipeNumberTemp = tbFilterPipeNumber.Text;
            if (ZoneTemp != "(Все)")
                ZoneTemp = " = '" + ZoneTemp + "'";
            else
                ZoneTemp = " is not null ";
            if (StackTemp != "(Все)")
                StackTemp = " = '" + StackTemp + "'";
            else
                StackTemp = " is not null ";
            if (PocketTemp != "(Все)")
                PocketTemp = " = '" + PocketTemp + "'";
            else
                PocketTemp = " is not null ";
            if (PipeNumberTemp != "")
                PipeNumberTemp = " = '" + PipeNumberTemp + "'";
            else
                PipeNumberTemp = " is not null ";

            String Condition = "where edit_state = 0 and notes like '%Перемещена%' and (STACK_NAME " + StackTemp + " or notes like '%штабель " + ddlStack_name.SelectedItem.Text + "%') and (ZONE_NAME " + ZoneTemp + " or notes like '%объекта " + ddlZone.SelectedItem.Text + "%')" +
                                    "and PIPENUMBER " + PipeNumberTemp + " and (POCKET_NUM " + PocketTemp + " or notes like '%карман " + ddlPocket_name.SelectedItem.Text + "%')";

            String SQL = "select * from " +
                                "(select ta.*, rownum rn from " +
                         "(select TRX_DATE,PIPENUMBER,ZONE_NAME,STACK_NAME,POCKET_NUM,NOTES,FIO from STOREHOUSES_OF_PIPES_BY_NUM " +
                            "left join spr_kadry on SPR_KADRY.USERNAME = STOREHOUSES_OF_PIPES_BY_NUM.OPERATOR_ID " + Condition +
                            "order by TRX_DATE desc) ta) " +
                                        "where rn > 0 and rn <= 10";
            cmd.CommandText = SQL;
            OleDbDataReader reader = cmd.ExecuteReader();
            TableRow Row;
            TableCell Cell;
            int countRow = 0;
            Row = new TableRow();
            Cell = new TableCell(); Cell.Text = "№ п/п"; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
            Cell = new TableCell(); Cell.Text = "Номер"; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
            Cell = new TableCell(); Cell.Text = "Откуда"; Cell.CssClass = "css_td"; Cell.Width = 300; Cell.Font.Bold = true; Row.Cells.Add(Cell);
            Cell = new TableCell(); Cell.Text = "Куда"; Cell.CssClass = "css_td"; Cell.Width = 300; Cell.Font.Bold = true; Row.Cells.Add(Cell);
            Cell = new TableCell(); Cell.Text = "ФИО оператора"; Cell.CssClass = "css_td"; Cell.Width = 100; Cell.Font.Bold = true; Row.Cells.Add(Cell);
            Cell = new TableCell(); Cell.Text = "Дата"; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
            Row.Style[HtmlTextWriterStyle.BackgroundColor] = "D3D3D3";
            tblRelocation.Rows.Add(Row);
            while (reader.Read())
            {
                countRow = countRow + 1;
                Row = new TableRow();
                Cell = new TableCell(); Cell.Text = countRow.ToString(); Cell.CssClass = "css_td"; Row.Cells.Add(Cell);
                String num = GetPipeHistoryURL(reader["PIPENUMBER"].ToString());
                Cell = new TableCell(); Cell.Text = num; Cell.CssClass = "css_td"; Row.Cells.Add(Cell);
                Cell = new TableCell(); Cell.Text = reader["NOTES"].ToString(); Cell.Width = 300; Cell.CssClass = "css_td"; Row.Cells.Add(Cell);
                Cell = new TableCell(); Cell.Text = "Перемещена на складской объект " + reader["ZONE_NAME"] + ", штабель " + reader["STACK_NAME"] + ", карман " + reader["POCKET_NUM"]; Cell.Width = 300; Cell.CssClass = "css_td"; Row.Cells.Add(Cell);
                Cell = new TableCell(); Cell.Text = reader["FIO"].ToString(); Cell.Width = 100; Cell.CssClass = "css_td"; Row.Cells.Add(Cell);
                Cell = new TableCell(); Cell.Text = reader["TRX_DATE"].ToString(); Cell.CssClass = "css_td"; Row.Cells.Add(Cell);
                tblRelocation.Rows.Add(Row);
            }
            Row = new TableRow();
            Cell = new TableCell(); Cell.Text = (countRow + 1).ToString(); Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
            Cell = new TableCell(); Cell.Text = ""; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
            Cell = new TableCell(); Cell.Text = ""; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
            Cell = new TableCell(); Cell.Text = ""; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
            Cell = new TableCell(); Cell.Text = ""; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
            Cell = new TableCell(); Cell.Text = ""; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
            tblRelocation.Rows.Add(Row);
            reader.Dispose();
            reader.Close();
            conn.Close();
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка", ex);
        }
    }

    //Обработчик события перемещенния труб
    void Relocation(String rowId, OleDbCommand cmd, int cnt)
    {
        //Меняем edit_state на 1
        cmd.CommandText = "update STOREHOUSES_OF_PIPES_BY_NUM set Edit_state = 1 where row_id=?";
        cmd.Parameters.Clear();
        cmd.Parameters.AddWithValue("row_id", rowId);
        cmd.ExecuteNonQuery();

        //добавляем нувую запись
        String Notes = "Перемещена со складского объекта " + ddlZone.SelectedItem.Text + ", штабель " + ddlStack_name.SelectedItem.Text + ", карман " + ddlPocket_name.SelectedItem.Text;
        cmd.CommandText = @"insert into STOREHOUSES_OF_PIPES_BY_NUM 
                                (TRX_DATE, OPERATOR_ID, EDIT_STATE, ORIGINAL_ROWID, SHIFT_INDEX, ROW_ID,
                                 ZONE_NAME, STACK_NAME, POCKET_NUM, NOTES,
                                 DEFECT, EMPLOYER_NUMBER, PRESENTATION, 
                                 DESTINATION, SHOP,  
                                 PIPENUMBER, INVENTORY_CODE, PIPE_DIAMETER, PIPE_THICKNESS, PIPE_STEELMARK) 
                             select sysdate, ?, '0', row_id, ?, to_char(SYSDATE, 'dd.mm.yyyy hh24:mi:ss')||'_'||?||'_'||PIPENUMBER||'_'||'" + cnt.ToString() + @"',
                                 ?, ?, ?, ?,
                                 DEFECT, EMPLOYER_NUMBER, PRESENTATION, 
                                 DESTINATION, SHOP,  
                                 PIPENUMBER, INVENTORY_CODE, PIPE_DIAMETER, PIPE_THICKNESS, PIPE_STEELMARK 
                                 from STOREHOUSES_OF_PIPES_BY_NUM where row_id=?";
        cmd.Parameters.Clear();
        cmd.Parameters.AddWithValue("OPERATOR_ID", Authentification.User.UserName);
        cmd.Parameters.AddWithValue("SHIFT_INDEX", Authentification.Shift);
        cmd.Parameters.AddWithValue("ROW_ID_OPERATOR", Authentification.User.UserName);

        cmd.Parameters.AddWithValue("ZONE_NAME", ddlRelocationZone.SelectedItem.Text);
        cmd.Parameters.AddWithValue("STACK_NAME", ddlRelocationStack.SelectedItem.Text);
        cmd.Parameters.AddWithValue("POCKET_NUM", ddlRelocationPocket.SelectedItem.Text);
        cmd.Parameters.AddWithValue("NOTES", Notes);

        cmd.Parameters.AddWithValue("SRS_ROW_ID", rowId);

        cmd.ExecuteNonQuery();
        cmd.Parameters.Clear();
    }
    /// <summary>
    /// Функция возвращает значения для проверки сортамента
    /// </summary>
    /// <returns>Перечисление в виде</returns>
    String GetComparisonSheet(bool bCheckDuplicates)
    {
        string result = "";
        List<string> rowIdList = new List<string>();
        string[] strL = bCheckDuplicates ? hfldSelect.Value.Split('~'): VCheckChecked;
        if (bCheckDuplicates) { VCheckChecked = strL; }
        string[] strIdL = hfldID.Value.Split('~');
        string strRowId = "";
        for (int i = 0; i < strIdL.Length; i++)
        {
            if (strL[i] == "1")
            {
                string actRowId = strIdL[i].Substring(5, strIdL[i].Length - 5);
                rowIdList.Add(actRowId);
                if (strRowId != "") strRowId += ", ";
                strRowId += "'" + actRowId + "'";
            }
        }

        try
        {
            //подключение к БД
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();


            //запрос на выборку записей
            OleDbCommand cmd = conn.CreateCommand();

            String SQL3 = @"select count(*) from storehouses_of_pipes_by_num sopbn
                                where zone_name = ? and stack_name = ? and pocket_num = ? and edit_state = 0";
            cmd.Parameters.Clear();
            cmd.CommandText = SQL3;
            cmd.Parameters.AddWithValue("ZONE_NAME", ddlRelocationZone.SelectedItem.Text);
            cmd.Parameters.AddWithValue("STACK_NAME", ddlRelocationStack.SelectedItem.Text);
            cmd.Parameters.AddWithValue("POCKET_NUM", ddlRelocationPocket.SelectedItem.Text);
            int countP = 0;

            countP = Convert.ToInt32(cmd.ExecuteScalar());

            if (countP != 0)
            {   
                //Проверка соответсвий записей в одном и другом штабеле по диаметру и госту
                String SQL = @"select count(*) from (select DIAMETER, GOST, PIPENUMBER from ( select DISTINCT ZONE_NAME, STACK_NAME, POCKET_NUM, PIPENUMBER, INV.DIAMETER, INV.GOST, SOPBN.ROW_ID 
                                from STOREHOUSES_OF_PIPES_BY_NUM SOPBN  
                                left join  (select MOD (PS.BATCH_YEAR, 100)||TRIM(TO_CHAR(SUBSTR (PS.BATCH, INSTR (PS.BATCH, '-') + 1), '000000'))  PIPE_NUMBER,
                            PI.DIAMETER, case when PI.GRUP is null then PI.gost else PI.gost || '-' || PI.GRUP end gost, PS.ENTRY_QNT_T
                            from ORACLE.Z_PIPE_STORAGE PS
                            left join ORACLE.V_T3_PIPE_ITEMS PI on TO_NUMBER (PS.MATERIAL) = PI.INVENTORY_ITEM_ID
                where PS.ORG_ID = 127 and PS.entry_qnt_t>0) INV  on to_char(SOPBN.PIPENUMBER) = INV.PIPE_NUMBER
                where sopbn.edit_state = 0)
                where row_id in (" + strRowId + ") " +
                  " and diameter in (select diameter from STOREHOUSES_OF_PIPES_BY_NUM SOPBN" +
                  " left join  (select MOD (PS.BATCH_YEAR, 100)||TRIM(TO_CHAR(SUBSTR (PS.BATCH, INSTR (PS.BATCH, '-') + 1), '000000'))  PIPE_NUMBER," +
                  " PI.DIAMETER, case when PI.GRUP is null then PI.gost else PI.gost || '-' || PI.GRUP end gost, PS.ENTRY_QNT_T" +
                  " from ORACLE.Z_PIPE_STORAGE PS" +
                  " left join ORACLE.V_T3_PIPE_ITEMS PI on TO_NUMBER (PS.MATERIAL) = PI.INVENTORY_ITEM_ID" +
                  " where PS.ORG_ID = 127 and PS.entry_qnt_t>0) INV  on to_char(SOPBN.PIPENUMBER) = INV.PIPE_NUMBER" +
                  " where sopbn.edit_state = 0" +
                  " and zone_name=? and stack_name = ? and pocket_num = ?)" +
                  " and gost in (select gost from STOREHOUSES_OF_PIPES_BY_NUM SOPBN" +
                  " left join  (select MOD (PS.BATCH_YEAR, 100)||TRIM(TO_CHAR(SUBSTR (PS.BATCH, INSTR (PS.BATCH, '-') + 1), '000000'))  PIPE_NUMBER," +
                  " PI.DIAMETER, case when PI.GRUP is null then PI.gost else PI.gost || '-' || PI.GRUP end gost, PS.ENTRY_QNT_T" +
                  " from ORACLE.Z_PIPE_STORAGE PS" +
                  " left join ORACLE.V_T3_PIPE_ITEMS PI on TO_NUMBER (PS.MATERIAL) = PI.INVENTORY_ITEM_ID" +
                  " where PS.ORG_ID = 127 and PS.entry_qnt_t>0) INV  on to_char(SOPBN.PIPENUMBER) = INV.PIPE_NUMBER" +
                  " where sopbn.edit_state = 0" +
                  " and zone_name=? and stack_name = ? and pocket_num = ?))";
                cmd.Parameters.Clear();
                cmd.CommandText = SQL;
                cmd.Parameters.AddWithValue("ZONE_NAME", ddlRelocationZone.SelectedItem.Text);
                cmd.Parameters.AddWithValue("STACK_NAME", ddlRelocationStack.SelectedItem.Text);
                cmd.Parameters.AddWithValue("POCKET_NUM", ddlRelocationPocket.SelectedItem.Text);
                cmd.Parameters.AddWithValue("ZONE_NAME1", ddlRelocationZone.SelectedItem.Text);
                cmd.Parameters.AddWithValue("STACK_NAME1", ddlRelocationStack.SelectedItem.Text);
                cmd.Parameters.AddWithValue("POCKET_NUM1", ddlRelocationPocket.SelectedItem.Text);

                int CountPipes = 0;
                CountPipes = Convert.ToInt32(cmd.ExecuteScalar());

                //Если в штабеле соответствий нет по диаметру и ГОСТу
                if (CountPipes == 0)
                {
                    String SQL2 = @"select * from (select DIAMETER, GOST, pipenumber from ( select DISTINCT ZONE_NAME, STACK_NAME, POCKET_NUM, PIPENUMBER, INV.DIAMETER, INV.GOST, SOPBN.ROW_ID 
                                                        from STOREHOUSES_OF_PIPES_BY_NUM SOPBN  
                                                        left join  (select MOD (PS.BATCH_YEAR, 100)||TRIM(TO_CHAR(SUBSTR (PS.BATCH, INSTR (PS.BATCH, '-') + 1), '000000'))  PIPE_NUMBER,
                                                                    PI.DIAMETER, case when PI.GRUP is null then  PI.GOST else PI.GOST || '-' || PI.GRUP end GOST, PS.ENTRY_QNT_T
                                                                    from ORACLE.Z_PIPE_STORAGE PS
                                                                    left join ORACLE.V_T3_PIPE_ITEMS PI on TO_NUMBER (PS.MATERIAL) = PI.INVENTORY_ITEM_ID
                                                                    where PS.ORG_ID = 127 and PS.entry_qnt_t>0) INV  on to_char(SOPBN.PIPENUMBER) = INV.PIPE_NUMBER
                                                        where sopbn.edit_state = 0)
                                    where row_id in (" + strRowId + "))";
                    cmd.Parameters.Clear();
                    cmd.CommandText = SQL2;

                    OleDbDataReader rdr1 = cmd.ExecuteReader();

                    string pn = "";
                    string ND = "";
                    string Diam = "";
                    while (rdr1.Read())
                    {
                        if (pn != "") pn += ", ";
                        pn += rdr1["pipenumber"].ToString();
                        if (ND != "") ND += ", ";
                        ND += rdr1["GOST"].ToString();
                        if (Diam != "") Diam += ", ";
                        Diam += rdr1["DIAMETER"].ToString();
                    }

                    rdr1.Close();
                    rdr1.Dispose();

                    result = "Внимаение! В выбранном штабеле лежат трубы отличные от труб:\r\n" + pn + " по диаметру: " + Diam + " и НТД: " + ND + "\r\nДействителньо переместить выбранные трубы?";
                }
                cmd.Dispose();
            }
        }

        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка при сравнении сортамента в штабеле для перемещения:", ex);
        }
        return result;
    }
    // кнопка "переместить"
    protected void btnMove_Click(object sender, EventArgs e)
    {
        MovePipes(true);
    }

    void MovePipes(bool bCheckDuplicates)
    {
        string res = GetComparisonSheet(bCheckDuplicates);
        GetRollNameByZoneNameForEdit(ref RollNameForCurrentZone, ddlRelocationZone.SelectedItem.Text);
        if (!Authentification.CanAddData(RollNameForCurrentZone))
        {
            Master.AddErrorMessage("Недостаточно прав для перемещения трубы на складской объект: " + ddlRelocationZone.SelectedItem.Text);
            return;
        }

        if (ddlRelocationZone.SelectedIndex < 1 || ddlRelocationStack.SelectedIndex < 1 || ddlRelocationPocket.SelectedIndex < 1)
        {
            Master.AlertMessage = "Перемещение невозможно, так как не выбрано место, куда следует перемещать";
            return;
        }

        if (ddlRelocationZone.SelectedItem.Text == ddlZone.SelectedItem.Text && ddlRelocationStack.SelectedItem.Text == ddlStack_name.SelectedItem.Text && ddlRelocationPocket.SelectedItem.Text == ddlPocket_name.SelectedItem.Text)
        {
            Master.AlertMessage = "Невозможно переместить трубы в тоже место, откуда перемещается";
            return;
        }


        List<string> rowIdList = new List<string>();
        string[] strL = bCheckDuplicates ? hfldSelect.Value.Split('~') : VCheckChecked;
        string[] strIdL = hfldID.Value.Split('~');
        VCheckChecked = strL;
        string strRowId = "";
        for (int i = 0; i < strIdL.Length; i++)
        {
            if (strL[i] == "1")
            {
                string actRowId = strIdL[i].Substring(5, strIdL[i].Length - 5);
                rowIdList.Add(actRowId);
                if (strRowId != "") strRowId += ", ";
                strRowId += "'" + actRowId + "'";
            }
        }

        OleDbConnection conn = Master.Connect.ORACLE_TESC3();
        OleDbCommand cmd = conn.CreateCommand();
        cmd.Transaction = conn.BeginTransaction();

        try
        {
            // проверяем наличие row_id
            cmd.CommandText = "select nvl(count(*),0) from STOREHOUSES_OF_PIPES_BY_NUM where row_id in (" + strRowId + ")";
            int CountRowIdInDb = Convert.ToInt32(cmd.ExecuteScalar());
            if (CountRowIdInDb != rowIdList.Count)
            {
                Master.AlertMessage = "Перемещение невозможно, так как некоторых выбранных записей уже не сущестует.\r\nОбновите таблицу и попробуйте снова.";
                cmd.Transaction.Rollback();
                cmd.Dispose();
                return;
            }
            if (bCheckDuplicates)
            {
                // проверяем на дубликаты
                cmd.CommandText = @"select pipenumber from STOREHOUSES_OF_PIPES_BY_NUM
                where row_id in (" + strRowId + ") and pipenumber in (select pipenumber from STOREHOUSES_OF_PIPES_BY_NUM where edit_state=0 and ZONE_NAME=? and STACK_NAME=? and POCKET_NUM=?)";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("zone", ddlRelocationZone.SelectedItem.Text);
                cmd.Parameters.AddWithValue("stack", ddlRelocationStack.SelectedItem.Text);
                cmd.Parameters.AddWithValue("pocket", ddlRelocationPocket.SelectedItem.Text);
                OleDbDataReader rdr = cmd.ExecuteReader();
                string pp = "";
                int CountDupl = 0;
                while (rdr.Read())
                {
                    if (pp != "") pp += ", ";
                    pp += rdr["pipenumber"].ToString();
                    CountDupl++;
                }

                rdr.Close();
                rdr.Dispose();

                if (CountRowIdInDb != rowIdList.Count)
                {
                    lblMoveDupl.Text ="Внимаение! В выбранном месте для перемещения уже сущестуют записи по следующим номера труб:\r\n" + pp + "\r\nДействителньо переместить выбранные трубы?";
                    PopupWindow1.ContentPanelId = pnlConfirmMove.ID;
                    PopupWindow1.Title = "Перемещение труб";
                    pnlConfirmMove.Visible = true;
                    cmd.Transaction.Rollback();
                    cmd.Dispose();
                    return;
                }

                if (res != "")
                {
                    lblSortMove.Text = res;
                    PopupWindow1.ContentPanelId = pnlSortMove.ID;
                    PopupWindow1.Title = "Перемещение труб";
                    pnlSortMove.Visible = true;
                    cmd.Transaction.Rollback();
                    return;
                }
            }
            
            // перемещаем
            for (int i = 0; i < rowIdList.Count; i++)
            {
                Relocation(rowIdList[i], cmd, i);
            }

            cmd.Transaction.Commit();

            ClearStoreHousesViewState();
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка перемещения труб", ex);
            cmd.Transaction.Rollback();
        }

        cmd.Dispose();
    }

    #endregion *** ПЕРЕМЕЩЕНИЕ ********************************************************************************

    #region *** СКЛАД **************************************************************************************

    protected void btnOtgruzka_Click(object sender, EventArgs e)
    {
        mvMainViews.Visible = false;
        ActivateTab(tdOtgruzka);
        mvViews.SetActiveView(vOtgruzka);

    }

    // строка запроса, начинающаяся с from
    String BuildkladQueryPart2(OleDbCommand cmd)
    {
        String Zone = ddlZone.SelectedItem.Text;
        String Stack = ddlStack_name.SelectedItem.Text;
        String Pocket = ddlPocket_name.SelectedItem.Text;
        String PipeNumber = tbFilterPipeNumber.Text;

        StringBuilder Condition = new StringBuilder();
        #region *** Фильтрация *********************************************************

        Condition.Append(" where SOPBN.EDIT_STATE = 0 \r\n");

        if (Zone != "(Все)")
        {
            Condition.Append(" and ZONE_NAME = '" + Zone + "' ");
        }
        else
        {
            Condition.Append(" and ZONE_NAME is not null ");
        }

        String[] temp = PipeNumber.Split(','); ;
        if (PipeNumber != "")
        {
            Condition.Append(" and (to_char(PIPENUMBER) like '%" + temp[0] + "%'");
            for (int i = 1; i < temp.Length; i++)
                Condition.Append(" or to_char(PIPENUMBER) like '%" + temp[i] + "%'");
            Condition.Append(") ");
        }

        if (Pocket != "") Condition.Append(" and POCKET_NUM = '" + Pocket + "' ");
        else Condition.Append(" and POCKET_NUM is not null ");

        if (Stack != "(Все)") Condition.Append(" and STACK_NAME = '" + Stack + "' ");
        else Condition.Append(" and STACK_NAME is not null \r\n");

        if (ddlDiametrs.SelectedItem.Text != "(Все)")
        {
            string diam = ddlDiametrs.SelectedItem.Text.Trim();
            Condition.Append(" and (to_char(PIPE_DIAMETER) = '" + diam + "' or to_char(INV.DIAMETER) = '" + diam + "') ");
        }

        if (ddlThickneses.SelectedItem.Text != "(Все)")
        {
            string th = ddlThickneses.SelectedItem.Text.Trim();
            Condition.Append(" and (to_char(PIPE_THICKNESS) = '" + th + "' or to_char(INV.THICKNESS) = '" + th + "') ");
        }

        if (ddlSteelMarks.SelectedItem.Text != "(Все)")
        {
            string st = ddlSteelMarks.SelectedItem.Text.Trim();
            Condition.Append(" and (PIPE_STEELMARK = '" + st + "' or INV.STAL = '" + st + "') ");
        }

        if (ddlDefects.SelectedItem.Text != "(Все)")
            Condition.Append(" and  DEFECT = '" + ddlDefects.SelectedItem.Text + "' ");

        if (ddlGost.SelectedItem.Text != "(Все)")
        {
            Condition.Append(" and (GOST = '" + ddlGost.SelectedItem.Text + "' or INV.GOST = '" + ddlGost.SelectedItem.Text + "') ");
        }

        if (ddlGroup.SelectedItem.Text != "(Все)")
        {
            Condition.Append(" and (GRUP = '" + ddlGroup.SelectedItem.Text + "' or INV.GRUP = '" + ddlGroup.SelectedItem.Text + "') ");
        }

        if (ddlDestination.SelectedItem.Text != "(Все)")
            Condition.Append(" and  DESTINATION = '" + ddlDestination.SelectedItem.Text + "' ");

        if (ddlSheet.SelectedItem.Text != "(Все)")
        {
            if(cbxSheet.Checked) Condition.Append(" and otgr.sheet_year||'-'||otgr.sheet_number != '" + ddlSheet.SelectedItem.Text + "' ");
            else Condition.Append(" and otgr.sheet_year||'-'||otgr.sheet_number = '" + ddlSheet.SelectedItem.Text + "' ");
        }

        if (ddlOperators.SelectedItem.Text != "(Все)")
            Condition.Append(" and  FIO = '" + ddlOperators.SelectedItem.Text + "' ");

        if (ddlPresentation.SelectedItem.Text == "Для предъявления")
            Condition.Append(" and  PRESENTATION = 0 ");

        if (ddlPresentation.SelectedItem.Text == "Не для предъявления")
            Condition.Append(" and  PRESENTATION = 1 ");

        if (tbFilterNotes.Text != "")
            Condition.Append(" and  NOTES like '%" + tbFilterNotes.Text + "%' ");

        if (tbItemNumber.Text != "" && tbItemNumber.Text.Length == 12)
            Condition.Append(" and (NOMER = '000000" + tbItemNumber.Text + "' ) ");
        if (tbItemNumber.Text != "" && tbItemNumber.Text.Length == 18)
            Condition.Append(" and (NOMER = '" + tbItemNumber.Text + "' ) ");

        if (tbPipeOrderNumber.Text != "")
            Condition.Append(" and ORDER_HEADER = '" + tbPipeOrderNumber.Text + "' ");

        if (tbGrat.Text != "")
            Condition.Append(" and GRAT_DOPUSK >= '" + tbGrat.Text + "' ");

        if (tbGratTo.Text != "")
            Condition.Append(" and GRAT_DOPUSK <= '" + tbGratTo.Text + "' ");

        if (tbPipeLenght.Text != "")
            Condition.Append(" and LENGTH >= '" + tbPipeLenght.Text + "' ");

        if (tbPipeLenghtTo.Text != "")
            Condition.Append(" and LENGTH <= '" + tbPipeLenghtTo.Text + "' ");

        if (ddlInspection.SelectedItem.Text != "(Все)")
            Condition.Append(" and INSPECTION LIKE '%" + ddlInspection.SelectedItem.Text + "%' ");

        if (tbDP.Text != "")
            Condition.Append(" and (ADDITIONAL_TEXT LIKE '%" + tbDP.Text + "%' or ADDITIONAL_TEXT2 LIKE '%" + tbDP.Text + "%' )");

        #endregion *** Фильтрация *********************************************************

        // запрос на выборку данных по геометрии из V_T3_PIPE_STORAGE и V_T3_PIPE_ITEMS
        // PIPE_NUMBER
        // DIAMETER
        // THICKNESS
        // STAL
        // GOST
        String SQL_INV = @" (select MOD (PS.BATCH_YEAR, 100)||TRIM(TO_CHAR(SUBSTR (PS.BATCH, INSTR (PS.BATCH, '-') + 1), '000000'))  PIPE_NUMBER,
                PI.DIAMETER, PI.THICKNESS, PI.STAL, PI.NOMER, PI.GOST, PI.GRUP, PS.ENTRY_QNT, PS.ENTRY_QNT_T,
                RS.B_MELT smelting_number, RS.B_MELT_YEAR smelting_year, SPRCL.CLIENT_TEXT supplier, B_STEAM_PART part, PS.MATERIAL
                from ORACLE.Z_PIPE_STORAGE PS
                left join ORACLE.V_T3_PIPE_ITEMS PI on TO_NUMBER (PS.MATERIAL) = PI.INVENTORY_ITEM_ID
                left join ORACLE.Z_ROLL_STORAGE RS on PS.B_SHEET_MATERIAL = RS.MATERIAL and PS.B_SHEET = RS.BATCH
                left join ORACLE.Z_SPR_CLIENTS SPRCl on RS.CLIENT1 = SPRCL.CLIENT
                where PS.ORG_ID = 127 and PS.entry_qnt_t>0) INV ";

        // часть основного запроса, начинающаяся с from
        return "from STOREHOUSES_OF_PIPES_BY_NUM SOPBN " +
                        " left join " + SQL_INV + " on to_char(SOPBN.PIPENUMBER) = INV.PIPE_NUMBER " +
                        " left join SPR_KADRY SK on SOPBN.OPERATOR_ID = SK.USERNAME " +
                        " left join OTGRUZKA_SHEET otgr on otgr.pipe_number = SOPBN.pipenumber and otgr.edit_state=0" +
                        " left join (select IP.length, ip.pipe_number, cmp.grat_dopusk, cmp.order_header, cmp.Inspection, ip.trx_date, cmp.additional_text, cmp.additional_text2 from inspection_pipes IP left join campaigns CMP " +
                        " on IP.Campaign_line_id = cmp.campaign_line_id where ip.edit_state = 0 and cmp.edit_state = 0) INS on ins.pipe_number = SOPBN.PIPENUMBER" +
                        Condition.ToString() +
                        "\r\n  " +
                        " order by pocket_num asc, pipenumber asc ";
    }


    /// <summary>
    /// Функция возвращает список суммарных весов с группировкой для каждого кармана
    /// </summary>
    /// <returns>Перечисление в виде</returns>
    private Dictionary<string, double> GetSumWeight()
    {
        Dictionary<string, double> result = new Dictionary<string, double>();

        try
        {
            //подключение к БД
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();

            //запрос на выборку записей
            using (OleDbCommand cmd = conn.CreateCommand())
            {
                if (ddlPocket_name.SelectedItem.Text == "")
                {
                    String SQL = @"select POCKET_NUM, SUM(ENTRY_QNT_T) from STOREHOUSES_OF_PIPES_BY_NUM SOPBN  left join  (select MOD (PS.BATCH_YEAR, 100)||TRIM(TO_CHAR(SUBSTR (PS.BATCH, INSTR (PS.BATCH, '-') + 1), '000000'))  PIPE_NUMBER,
                PI.DIAMETER, PI.THICKNESS, PI.STAL, PI.NOMER, PI.GOST, PI.GRUP, PS.ENTRY_QNT, PS.ENTRY_QNT_T,
                RS.B_MELT smelting_number, RS.B_MELT_YEAR smelting_year, SPRCL.CLIENT_TEXT supplier, B_STEAM_PART part, PS.MATERIAL
                from ORACLE.Z_PIPE_STORAGE PS
                left join ORACLE.V_T3_PIPE_ITEMS PI on TO_NUMBER (PS.MATERIAL) = PI.INVENTORY_ITEM_ID
                left join ORACLE.Z_ROLL_STORAGE RS on PS.B_SHEET_MATERIAL = RS.MATERIAL and PS.B_SHEET = RS.BATCH
                left join ORACLE.Z_SPR_CLIENTS SPRCl on RS.CLIENT1 = SPRCL.CLIENT
                where PS.ORG_ID = 127 and PS.entry_qnt_t>0) INV  on to_char(SOPBN.PIPENUMBER) = INV.PIPE_NUMBER  
                left join SPR_KADRY SK on SOPBN.OPERATOR_ID = SK.USERNAME  left join OTGRUZKA_SHEET otgr on otgr.pipe_number = SOPBN.pipenumber 
                and otgr.edit_state=0 
                left join (select IP.length, ip.pipe_number, cmp.grat_dopusk, cmp.order_header, cmp.Inspection, ip.trx_date from inspection_pipes IP left join campaigns CMP  on IP.Campaign_line_id = cmp.campaign_line_id where ip.edit_state = 0 and cmp.edit_state = 0) INS on ins.pipe_number = SOPBN.PIPENUMBER 
                where SOPBN.EDIT_STATE = 0  and ZONE_NAME = ?  and POCKET_NUM is not null and STACK_NAME = ?  
                and INS.TRX_DATE = (select max(trx_date) from inspection_pipes IP left join campaigns CMP on IP.Campaign_line_id = cmp.campaign_line_id where pipe_number = SOPBN.PIPENUMBER and ip.edit_state = 0 and cmp.edit_state = 0)
                group by pocket_num
                order by pocket_num asc";
                    cmd.CommandText = SQL;
                    cmd.Parameters.AddWithValue("ZONE_NAME", ddlZone.SelectedItem.Text);
                    cmd.Parameters.AddWithValue("STACK_NAME", ddlStack_name.SelectedItem.Text);
                }
                else
                {
                    String SQL = @"select POCKET_NUM, sum(ENTRY_QNT_T) from STOREHOUSES_OF_PIPES_BY_NUM SOPBN  left join  (select MOD (PS.BATCH_YEAR, 100)||TRIM(TO_CHAR(SUBSTR (PS.BATCH, INSTR (PS.BATCH, '-') + 1), '000000'))  PIPE_NUMBER,
                PI.DIAMETER, PI.THICKNESS, PI.STAL, PI.NOMER, PI.GOST, PI.GRUP, PS.ENTRY_QNT, PS.ENTRY_QNT_T,
                RS.B_MELT smelting_number, RS.B_MELT_YEAR smelting_year, SPRCL.CLIENT_TEXT supplier, B_STEAM_PART part, PS.MATERIAL
                from ORACLE.Z_PIPE_STORAGE PS
                left join ORACLE.V_T3_PIPE_ITEMS PI on TO_NUMBER (PS.MATERIAL) = PI.INVENTORY_ITEM_ID
                left join ORACLE.Z_ROLL_STORAGE RS on PS.B_SHEET_MATERIAL = RS.MATERIAL and PS.B_SHEET = RS.BATCH
                left join ORACLE.Z_SPR_CLIENTS SPRCl on RS.CLIENT1 = SPRCL.CLIENT
                where PS.ORG_ID = 127 and PS.entry_qnt_t>0) INV  on to_char(SOPBN.PIPENUMBER) = INV.PIPE_NUMBER  
                left join SPR_KADRY SK on SOPBN.OPERATOR_ID = SK.USERNAME  left join OTGRUZKA_SHEET otgr on otgr.pipe_number = SOPBN.pipenumber 
                and otgr.edit_state=0 
                left join (select IP.length, ip.pipe_number, cmp.grat_dopusk, cmp.order_header, cmp.Inspection, ip.trx_date from inspection_pipes IP left join campaigns CMP  on IP.Campaign_line_id = cmp.campaign_line_id where ip.edit_state = 0 and cmp.edit_state = 0) INS on ins.pipe_number = SOPBN.PIPENUMBER 
                where SOPBN.EDIT_STATE = 0  and ZONE_NAME = ?  and POCKET_NUM = ? and STACK_NAME = ?  
                and INS.TRX_DATE = (select max(trx_date) from inspection_pipes IP left join campaigns CMP on IP.Campaign_line_id = cmp.campaign_line_id where pipe_number = SOPBN.PIPENUMBER and ip.edit_state = 0 and cmp.edit_state = 0)
                group by pocket_num
                order by pocket_num asc";
                    cmd.CommandText = SQL;
                    cmd.Parameters.AddWithValue("ZONE_NAME", ddlZone.SelectedItem.Text);
                    cmd.Parameters.AddWithValue("POCKET_NUM", ddlPocket_name.SelectedItem.Text);
                    cmd.Parameters.AddWithValue("STACK_NAME", ddlStack_name.SelectedItem.Text);
                }


                //выборка данных и заполнение списка суммы для каждого пакета
                OleDbDataReader rdr = cmd.ExecuteReader();
                
                double sumweight = 0;
                if (rdr.HasRows)
                {
                    while (rdr.Read())
                    {
                        Double.TryParse(rdr["sum(ENTRY_QNT_T)"].ToString(), out sumweight);
                        result.Add(rdr["POCKET_NUM"].ToString(), sumweight);
                    }
                }

                rdr.Close();
                rdr.Dispose();
            }
        }
        catch (Exception)
        {
            throw new Exception("Ошибка при расчете веса");
        }
        return result;
    }

    /// <summary>
    /// Функция возвращает список количеста труб для каждого пакета
    /// </summary>
    /// <returns>Перечисление</returns>
    private Dictionary<string, int> GetCountPipe()
    {
        Dictionary<string, int> result = new Dictionary<string, int>();

        try
        {

            //подключение к БД
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();

            //запрос на выборку записей
            using (OleDbCommand cmd = conn.CreateCommand())
            {
                if (ddlPocket_name.SelectedItem.Text == "")
                {
                    String SQL = @"select POCKET_NUM, COUNT(POCKET_NUM) 
                                    from STOREHOUSES_OF_PIPES_BY_NUM SOPBN  
                                    left join (select MOD (PS.BATCH_YEAR, 100)||TRIM(TO_CHAR(SUBSTR (PS.BATCH, INSTR (PS.BATCH, '-') + 1), '000000'))  PIPE_NUMBER, PS.ENTRY_QNT_T                
                                                from ORACLE.Z_PIPE_STORAGE PS
                                                where PS.ORG_ID = 127 and PS.entry_qnt_t>0) INV  on to_char(SOPBN.PIPENUMBER) = INV.PIPE_NUMBER  
                                    where SOPBN.EDIT_STATE = 0  and ZONE_NAME = ?  and POCKET_NUM is not null and STACK_NAME = ?
                                    group by pocket_num
                                    order by pocket_num asc";
                    cmd.CommandText = SQL;
                    cmd.Parameters.AddWithValue("ZONE_NAME", ddlZone.SelectedItem.Text);
                    cmd.Parameters.AddWithValue("STACK_NAME", ddlStack_name.SelectedItem.Text);
                }
                else
                {
                    String SQL = @"select POCKET_NUM, COUNT(POCKET_NUM) 
                                    from STOREHOUSES_OF_PIPES_BY_NUM SOPBN  
                                    left join (select MOD (PS.BATCH_YEAR, 100)||TRIM(TO_CHAR(SUBSTR (PS.BATCH, INSTR (PS.BATCH, '-') + 1), '000000'))  PIPE_NUMBER, PS.ENTRY_QNT_T                
                                                from ORACLE.Z_PIPE_STORAGE PS
                                                where PS.ORG_ID = 127 and PS.entry_qnt_t>0) INV  on to_char(SOPBN.PIPENUMBER) = INV.PIPE_NUMBER  
                                    where SOPBN.EDIT_STATE = 0  and ZONE_NAME = ?  and POCKET_NUM = ? and STACK_NAME = ?
                                    group by pocket_num
                                    order by pocket_num asc";
                    cmd.CommandText = SQL;
                    cmd.Parameters.AddWithValue("ZONE_NAME", ddlZone.SelectedItem.Text);
                    cmd.Parameters.AddWithValue("POCKET_NUM", ddlPocket_name.SelectedItem.Text);
                    cmd.Parameters.AddWithValue("STACK_NAME", ddlStack_name.SelectedItem.Text);
                }


                //выборка данных и заполнение списка количества труб
                OleDbDataReader rdr = cmd.ExecuteReader();
                
                if (rdr.HasRows)
                {
                    while (rdr.Read())
                    {
                        result.Add(rdr["POCKET_NUM"].ToString(), Convert.ToInt32(rdr["COUNT(POCKET_NUM)"].ToString()));
                    }
                }

                rdr.Close();
                rdr.Dispose();
            }
        }
        catch (Exception)
        {
            throw new Exception("Ошибка при расчете количества труб");
        }

        return result;
    }

    /// <summary>
    /// Функция возвращает список количеста труб для каждого пакета
    /// </summary>
    String LastCount()
    {
        string LastPocket = "";
        try
        {

            //подключение к БД
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();

            //запрос на выборку записей
            using (OleDbCommand cmd = conn.CreateCommand())
            {
                if (ddlPocket_name.SelectedItem.Text == "")
                {
                    String SQL = @"select (POCKET_NUM), count(POCKET_NUM), sum(ENTRY_QNT_T)
            from STOREHOUSES_OF_PIPES_BY_NUM SOPBN
                left join (select MOD(PS.BATCH_YEAR, 100) || TRIM(TO_CHAR(SUBSTR(PS.BATCH, INSTR(PS.BATCH, '-') + 1), '000000'))  PIPE_NUMBER,PS.ENTRY_QNT_T
                from ORACLE.Z_PIPE_STORAGE PS
            where PS.entry_qnt_t > 0) INV on to_char(SOPBN.PIPENUMBER) = INV.PIPE_NUMBER
            where SOPBN.EDIT_STATE = 0  and ZONE_NAME = ?  and POCKET_NUM is not null and STACK_NAME = ?
            and pocket_num = (select max(pocket_num) from storehouses_of_pipes_by_num SOPBN  where SOPBN.EDIT_STATE = 0  and ZONE_NAME = ? and POCKET_NUM is not null and STACK_NAME = ?)
            group by pocket_num";
                    cmd.CommandText = SQL;
                    cmd.Parameters.AddWithValue("ZONE_NAME", ddlZone.SelectedItem.Text);
                    cmd.Parameters.AddWithValue("STACK_NAME", ddlStack_name.SelectedItem.Text);
                    cmd.Parameters.AddWithValue("ZONE_NAME1", ddlZone.SelectedItem.Text);
                    cmd.Parameters.AddWithValue("STACK_NAME1", ddlStack_name.SelectedItem.Text);
                }

                int CountLast = 0;
                double SumLast = 0;
                
                //выборка данных и заполнение списка кампаний
                OleDbDataReader rdr = cmd.ExecuteReader();
                
                if (rdr.HasRows)
                {
                    while (rdr.Read())
                    {
                        CountLast = Convert.ToInt32(rdr["COUNT(POCKET_NUM)"]);
                        Double.TryParse(rdr["SUM(ENTRY_QNT_T)"].ToString(), out SumLast);
                        LastPocket = "Количество труб последнего пакета " + CountLast + " Вес " + SumLast.ToString("F3");
                    }
                }
                rdr.Close();
                rdr.Dispose();
            }
        }
        catch (Exception)
        {
            throw new Exception("Ошибка при расчете количества труб");
        }

        return LastPocket;
    }

    //Таблица склад
    int numRowEdit = 0;
    protected void SkladTable(bool Edit, String RowId, int StartIndex, int EndIndex, bool bFillNZP)
    {
        String Zone = ddlZone.SelectedItem.Text;
        String Stack = ddlStack_name.SelectedItem.Text;
        String Pocket = ddlPocket_name.SelectedItem.Text;
        String PipeNumber = tbFilterPipeNumber.Text;

        tblSklad.Rows.Clear();
        numRowEdit = 0;

        try
        {
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();

            // основной запрос
            // поля, которые выбираются (для наглядности)
            List<String> fld = new List<string>();
            fld.Add("ZONE_NAME");
            fld.Add("STACK_NAME");
            fld.Add("POCKET_NUM");
            fld.Add("ENTRY_QNT_T");
            fld.Add("PIPENUMBER");
            fld.Add("INV.DIAMETER");
            fld.Add("INV.THICKNESS");
            fld.Add("INV.STAL");
            fld.Add("INV.NOMER");
            fld.Add("INV.GOST");
            fld.Add("INV.GRUP");
            //fld[9] = "INV.d";
            //fld[10] = "INV.th";
            //fld[11] = "INV.ms";
            fld.Add("PRESENTATION");
            fld.Add("SOPBN.DEFECT");
            fld.Add("FIO");
            fld.Add("SOPBN.TRX_DATE");
            fld.Add("NOTES");
            fld.Add("SOPBN.ROW_ID");
            fld.Add("DESTINATION");
            fld.Add("NZP");
            fld.Add("OTGR.SHEET_YEAR");
            fld.Add("OTGR.SHEET_NUMBER");

            // составляем все поля
            StringBuilder MainQuery = new StringBuilder();
            MainQuery.Append("select DISTINCT ");
            for (int i = 0; i < fld.Count; i++)
            {
                MainQuery.Append(fld[i]);
                if (i != (fld.Count - 1)) MainQuery.Append(", ");
                else MainQuery.Append(" ");
            }

            string MainQueryPart2 = BuildkladQueryPart2(cmd);

            // составляем вторую часть запроса
            MainQuery.Append(MainQueryPart2);

            //вес труб в кармане
            Double Qnt_T = 0;
            String QNT_TSQL = "select SUM(ENTRY_QNT_T) " + BuildkladQueryPart2(cmd);
            cmd.CommandText = QNT_TSQL;
            cmd.Parameters.Clear();
            OleDbDataReader reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                Double.TryParse(reader["SUM(ENTRY_QNT_T)"].ToString(), out Qnt_T);
                LabelQNT_T.Text = " Вес " + Convert.ToString(Qnt_T) + " тонн";
            }

            reader.Dispose();
            reader.Close();

            // Считаем количество записей для постраничного вывода
            int CountPipes = 0;
            String CountPipesSQL = @"select count(*) from (" + MainQuery + ")";

                cmd.CommandText = CountPipesSQL;
                cmd.Parameters.Clear();
                CountPipes = Convert.ToInt32(cmd.ExecuteScalar());
                SkladCountPipes.Text = CountPipes.ToString();
                CountOfPipes.Text = "Количество труб " + SkladCountPipes.Text + " ";




            #region *** формирование для кнопки группового удаления предупреждающего сообщения ***********
            StringBuilder sb = new StringBuilder();
            sb.Append("if(confirm('");
            sb.Append("Будут удалены все записи из складского объекта " + Zone + ", штабеля " + Stack + " с учетом выбранных фильтров");
            sb.Append("\\r\\nКоличество удаляемых записей: " + CountPipes.ToString());
            sb.Append("\\r\\nВы уверены, что хотите удалить все записи?");
            sb.Append("')) return true; return false; ");
            btnDelAll.OnClientClick = sb.ToString();
            #endregion *** формирование для кнопки группового удаления предупреждающего сообщения ********

            //Записи 
            String SQL = "select * from " +
                                "(select ta.*, rownum rn from " +
                         "( " + MainQuery + " ) ta) " +
                                        "where rn > ? and rn <= ?";
            cmd.CommandText = SQL;
            cmd.Parameters.AddWithValue("StartIndex", StartIndex);
            cmd.Parameters.AddWithValue("EndIndex", EndIndex);
            OleDbDataReader Reader = cmd.ExecuteReader();
            TableRow Row;
            TableCell Cell;
            int countRow = 0;
            double sum_weight = 0;
            string last_pocket = "";
            string new_pocket = "";
            List<String> PipeNums = new List<String>(); // параллельный массив для запоминания номеров труб
            Dictionary<string, double> SumWeightPocket = new Dictionary<string, double>(); //массив для запоминания массы по карману
            Dictionary<string, int> CountPipePocket = new Dictionary<string, int>(); //массив для запоминания количества труб в кармане

            SumWeightPocket = GetSumWeight();
            CountPipePocket = GetCountPipe();
            while (Reader.Read())
            {
                countRow = countRow + 1;
                //Шапка таблицы
                if (countRow == 1)
                {
                    if (last_pocket == "")
                    {
                        last_pocket = Reader["POCKET_NUM"].ToString();

                        //покраска значений количества труб в штабеле/пакете как в мнемосхеме
                        ColorPipes(Reader["ZONE_NAME"].ToString(), Reader["STACK_NAME"].ToString(), Reader["POCKET_NUM"].ToString(), LabelPodItogRow);
                    }
                    new_pocket = Reader["POCKET_NUM"].ToString();

                    Row = new TableRow();
                    Cell = new TableCell(); Cell.Text = "№ п.п."; Cell.Width = 40; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
                    if (Zone == "(Все)")
                    {
                        Cell = new TableCell(); Cell.Text = "Складской объект"; Cell.Width = 70; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
                    }
                    if (Stack == "(Все)")
                    {
                        Cell = new TableCell(); Cell.Text = "Штабель"; Cell.Width = 50; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
                    }
                    if (Pocket == "")
                    {
                        Cell = new TableCell(); Cell.Text = "Карман"; Cell.Width = 50; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
                    }
                    Cell = new TableCell(); Cell.Text = "Номер трубы"; Cell.Width = 60; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
                    //Cell = new TableCell(); Cell.Text = "Номер партии"; Cell.Width = 60; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
                    Cell = new TableCell(); Cell.Text = "Диаметр, мм"; Cell.Width = 80; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
                    Cell = new TableCell(); Cell.Text = "Толщина стенки, мм"; Cell.Width = 50; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
                    Cell = new TableCell(); Cell.Text = "Марка стали"; Cell.Width = 150; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
                    Cell = new TableCell(); Cell.Text = "Номенклатурный номер"; Cell.Width = 100; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
                    Cell = new TableCell(); Cell.Text = "НД"; Cell.Width = 150; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
                    if (Zone == "СК-ТЭСЦ-3 пролет 2 (ремонтный участок)")
                    {
                        Cell = new TableCell(); Cell.Text = "Предъявление"; Cell.Width = 80; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
                    }
                    Cell = new TableCell(); Cell.Text = "НЗП"; /*Cell.Width = 200;*/ Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
                    Cell = new TableCell(); Cell.Text = "Дефект"; Cell.Width = 200; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
                    Cell = new TableCell(); Cell.Text = "Год-номер ведомости"; Cell.Width = 100; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
                    Cell = new TableCell(); Cell.Text = "ФИО"; Cell.Width = 150; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
                    Cell = new TableCell(); Cell.Text = "Дата"; Cell.Width = 120; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
                    Cell = new TableCell(); Cell.Text = "Примечание"; Cell.Width = 300; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
                    Cell = new TableCell(); Cell.Width = 20; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
                    Cell = new TableCell(); Cell.Width = 20; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
                    //Cell = new TableCell(); Cell.Width = 20; Cell.CssClass = "css_td"; Cell.Font.Bold = true; Row.Cells.Add(Cell);
                    Row.Style[HtmlTextWriterStyle.BackgroundColor] = "D3D3D3";
                    tblSklad.Rows.Add(Row);
                }

                bool bNZP = (Reader["NZP"].ToString() == "1");
                //bNZP = false;

                //Тело таблицы
                Row = new TableRow();
                Cell = new TableCell(); Cell.Text = Reader["rn"].ToString(); /*Cell.Width = 40;*/ Cell.CssClass = "css_td"; Row.Cells.Add(Cell);
                if (Zone == "(Все)")
                {
                    Cell = new TableCell(); Cell.Text = Reader["ZONE_NAME"].ToString();/* Cell.Width = 70; */Cell.CssClass = "css_td"; Row.Cells.Add(Cell);
                }
                if (Stack == "(Все)")
                {
                    Cell = new TableCell(); Cell.Text = Reader["STACK_NAME"].ToString(); /*Cell.Width = 50;*/ Cell.CssClass = "css_td"; Row.Cells.Add(Cell);
                }
                if (Pocket == "")
                {
                    Cell = new TableCell(); Cell.Text = Reader["POCKET_NUM"].ToString(); /*Cell.Width = 50;*/ Cell.CssClass = "css_td"; Row.Cells.Add(Cell);
                }
                String pn = Reader["PIPENUMBER"].ToString();
                PipeNums.Add(pn);
                Cell = new TableCell(); Cell.Text = GetPipeHistoryURL(pn);/* Cell.Width = 60;*/ Cell.CssClass = "css_td"; Row.Cells.Add(Cell);
                //Cell = new TableCell(); Cell.Text = Reader["LOT_NUMBER"].ToString(); Cell.Width = 60; Cell.CssClass = "css_td"; Row.Cells.Add(Cell);
                Cell = new TableCell(); Cell.Text = Reader["DIAMETER"].ToString(); /*Cell.Width = 80;*/ Cell.CssClass = "css_td"; Row.Cells.Add(Cell);
                Cell = new TableCell(); Cell.Text = Reader["THICKNESS"].ToString(); /*Cell.Width = 50;*/ Cell.CssClass = "css_td"; Row.Cells.Add(Cell);
                Cell = new TableCell(); Cell.Text = Reader["STAL"].ToString();/* Cell.Width = 150;*/ Cell.CssClass = "css_td"; Row.Cells.Add(Cell);
                Cell = new TableCell(); Cell.Text = Reader["NOMER"].ToString(); /*Cell.Width = 100;*/ Cell.CssClass = "css_td"; Row.Cells.Add(Cell);
                Cell = new TableCell(); Cell.Text = Reader["GOST"].ToString() + (Reader["GRUP"].ToString() != "" ? " гр." + Reader["GRUP"].ToString() : ""); /*Cell.Width = 150;*/ Cell.CssClass = "css_td"; Row.Cells.Add(Cell);
                if (Zone == "СК-ТЭСЦ-3 пролет 2 (ремонтный участок)")
                {
                    String tmp = "";
                    if (Reader["PRESENTATION"].ToString() == "0") tmp = "Нет"; else tmp = "Да";
                    Cell = new TableCell(); Cell.Text = tmp; /*Cell.Width = 80;*/ Cell.CssClass = "css_td"; Row.Cells.Add(Cell);
                }
                String nzp = ""; if (bNZP) nzp = "*";
                Cell = new TableCell(); Cell.Text = nzp; /*Cell.Width = 200;*/ Cell.CssClass = "css_td"; Row.Cells.Add(Cell);
                Cell = new TableCell(); Cell.Text = Reader["DEFECT"].ToString(); /*Cell.Width = 200;*/ Cell.CssClass = "css_td"; Row.Cells.Add(Cell);
                Cell = new TableCell(); Cell.Text = Reader["sheet_year"].ToString() != "" ? (Reader["sheet_year"].ToString() + "-" + Reader["sheet_number"].ToString()) : ""; /*Cell.Width = 200;*/ Cell.CssClass = "css_td"; Row.Cells.Add(Cell);
                Cell = new TableCell(); Cell.Text = Comm.CorrectFioString(Reader["FIO"].ToString(), false); /*Cell.Width = 150;*/ Cell.CssClass = "css_td"; Row.Cells.Add(Cell);
                Cell = new TableCell(); Cell.Text = Reader["TRX_DATE"].ToString();/* Cell.Width = 80;*/ Cell.CssClass = "css_td"; Row.Cells.Add(Cell);
                Cell = new TableCell(); Cell.Text = Reader["NOTES"].ToString(); /*Cell.Width = 500;*/ Cell.CssClass = "css_td"; Row.Cells.Add(Cell);

                //суммирование массы
                if (Reader["ENTRY_QNT_T"].ToString() != "") sum_weight += Convert.ToDouble(Reader["ENTRY_QNT_T"]);

                //если меняется карман, то вставляем строку с суммарным количеством для кармана
                if (last_pocket != Reader["POCKET_NUM"].ToString())
                {
                    
                    LabelPodItogRow.Text = CountPipePocket[last_pocket].ToString();

                    TableRow rowItog = new TableRow();
                    Cell = new TableCell(); Cell.ColumnSpan = 13; Cell.Text = ""; rowItog.Cells.Add(Cell);
                    Cell = new TableCell();
                    Cell.Text = "Количество труб в кармане " + LabelPodItogRow.Text + " Вес " + SumWeightPocket[last_pocket].ToString("F3");
                    if (LabelPodItogRow.BackColor == Color.Yellow)
                    {
                        Cell.BackColor = Color.Yellow;
                    }
                    else if (LabelPodItogRow.BackColor == Color.Red)
                    {
                        Cell.BackColor = Color.Red;
                    }
                    else if (LabelPodItogRow.BackColor == Color.LimeGreen)
                    {
                        Cell.BackColor = Color.LimeGreen;
                    }
                    Cell.HorizontalAlign = HorizontalAlign.Center; rowItog.Cells.Add(Cell);
                    Cell = new TableCell(); Cell.ColumnSpan = 5; rowItog.Cells.Add(Cell);
                    rowItog.Font.Bold = true;
                    tblSklad.Rows.Add(rowItog);
                    last_pocket = Reader["POCKET_NUM"].ToString();
                    //покраска значений количества труб в штабеле/пакете как в мнемосхеме
                    ColorPipes(Reader["ZONE_NAME"].ToString(), Reader["STACK_NAME"].ToString(), Reader["POCKET_NUM"].ToString(), LabelPodItogRow);

                }
                //Если редактирование, то вставляем кнопки для редактирования
                if ((Edit) && (Reader["ROW_ID"].ToString() == RowId))
                {
                    numRowEdit = countRow;
                    // сохранить
                    Cell = new TableCell();
                    Cell.Text = "<a title='Сохранить'><img src='Images/Confirm16x16.png' style=\"color: Black; text-decoration: none; border-style: none;\"/></a>";
                    /*Cell.Width = 20;*/
                    Cell.CssClass = "css_td";
                    Cell.Attributes["onclick"] = "__doPostBack('SKLAD','EDIT#" + Reader["ROW_ID"].ToString() + "#" + StartIndex.ToString() + "#" + EndIndex.ToString() + "')";
                    Cell.Attributes["onmouseover"] = "this.style.cursor='hand'";
                    Row.Cells.Add(Cell);
                    // отменить
                    Cell = new TableCell();
                    Cell.Text = "<a title='Отменить'><img src='Images/Cancel16x16.png' style=\"color: Black; text-decoration: none; border-style: none;\"/></a>";
                    /*Cell.Width = 20;*/
                    Cell.CssClass = "css_td";
                    Cell.Attributes["onclick"] = "__doPostBack('SKLAD','CANCEL#" + StartIndex.ToString() + "#" + EndIndex.ToString() + "')";
                    Cell.Attributes["onmouseover"] = "this.style.cursor='hand'";
                    Row.Cells.Add(Cell);

                    // автозаполнение
                    /*Cell = new TableCell();
                    Cell.Text = "<a title='Получить данные по номеру трубы'><img src='Images/bd16x16.gif' style=\"color: Black; text-decoration: none; border-style: none;\"/></a>";
                    Cell.Width = 20; Cell.CssClass = "css_td";
                    Cell.Attributes["onclick"] = "__doPostBack('SKLAD','AutoFill#" + Reader["ROW_ID"].ToString() + "#" + StartIndex.ToString() + "#" + EndIndex.ToString() + "')";
                    Cell.Attributes["onmouseover"] = "this.style.cursor='hand'";
                    Row.Cells.Add(Cell);*/

                    tblSklad.Rows.Add(Row);

                    int c = 0, p = 0;
                    if (Stack == "(Все)") c++; if (Pocket == "(Все)") c++; if (ddlZone.SelectedItem.Text == "(Все)") c++; if (Zone == "СК-ТЭСЦ-3 пролет 2 (ремонтный участок)") p++;
                    //Выполняем скрипт для вставки контролов в ячейки таблицы
                    String script = "<script type=\"text/javascript\" language=\"javascript\"> "
                        + "function OnDocumentLoad(e) { InsertActControls(@R,@C,@P); RestoreScrollPositions(); AlertMessage();}; "
                        + "window.onload=OnDocumentLoad;"
                        + "</script>";
                    script = script.Replace("@R", (tblSklad.Rows.Count - 1).ToString());
                    script = script.Replace("@C", c.ToString());
                    script = script.Replace("@P", p.ToString());
                    RegisterStartupScript("edit_controls_script", script);
                    //Выделяем елементы в выпадающих списках
                    SelectDDLItem(ddlDefect, tblSklad.Rows[tblSklad.Rows.Count - 1].Cells[8 + c + p]);
                    if (SelectDDLItem(ddlNotes, tblSklad.Rows[tblSklad.Rows.Count - 1].Cells[12 + c + p]))
                        tbNotes.Text = "";
                    else tbNotes.Text = tblSklad.Rows[tblSklad.Rows.Count - 1].Cells[12 + c + p].Text;
                    if (Reader["PRESENTATION"].ToString() == "0") cbPresentation.Checked = false; else cbPresentation.Checked = true;

                    // заполнение поля nzp в случае, если нажато редактирование или добавление
                    if (bFillNZP) chbNZP0.Checked = (nzp == "*");
                }
                else
                {
                    Cell = new TableCell();
                    /*Cell.Width = 20;*/
                    Cell.CssClass = "css_td";
                    string q = Reader["ZONE_NAME"].ToString();
                    GetRollNameByZoneNameForEdit(ref RollNameForCurrentZone, Reader["ZONE_NAME"].ToString());
                    if ((Authentification.CanEditData(RollNameForCurrentZone)))
                    {
                        Cell.Text = "<a title='Редактироовать'><img src='Images/Edit16x16.png' style=\"color: Black; text-decoration: none; border-style: none;\"/></a>";
                        Cell.Attributes["onclick"] = "__doPostBack('SKLAD','INSERT_EDIT#" + Reader["ROW_ID"].ToString() + "#" + StartIndex.ToString() + "#" + EndIndex.ToString() + "')";
                        Cell.Attributes["onmouseover"] = "this.style.cursor='hand'";
                    }
                    Row.Cells.Add(Cell);

                    Cell = new TableCell();
                    /*Cell.Width = 20;*/
                    Cell.CssClass = "css_td";
                    if ((Authentification.CanDeleteData(RollNameForCurrentZone)))
                    {
                        Cell.Text = "<a title='Удалить'><img src='Images/Delete16x16.png' style=\"color: Black; text-decoration: none; border-style: none;\"/></a>";
                        Cell.Attributes["onclick"] = "if(confirm('Нажмите ОК для подтверждения удаления записи.')) __doPostBack('SKLAD','DELETE#" + Reader["ROW_ID"].ToString() + "#" + StartIndex.ToString() + "#" + EndIndex.ToString() + "')";
                        Cell.Attributes["onmouseover"] = "this.style.cursor='hand'";
                    }
                    Row.Cells.Add(Cell);
                    tblSklad.Rows.Add(Row);
                }
            }
            Reader.Dispose();
            Reader.Close();

            tblSklad.Width = 1700;

            // пробегаемся по всем ячейкам и заполняем nzp
            /*int c1 = 0, p1 = 0;
                    if (Stack == "(Все)") c1++; if (Pocket == "(Все)") c1++; if (ddlZone.SelectedItem.Text == "(Все)") c1++; if (Zone == "СК-ТЭСЦ-3 пролет 2 (ремонтный участок)") p1++;
            for (int i = 1; i < tblSklad.Rows.Count; i++)
            {
                bool bNzp = (tblSklad.Rows[i].Cells[7 + c1 + p1].Text != "");
                if (bNzp)
                    AutoFillLines(bNzp, PipeNums[i - 1], tblSklad.Rows[i]);
            }*/

            //Ячейка с сылкам на страницы
            if (CountPipes > 100)
            {
                Row = new TableRow();
                Cell = new TableCell();
                for (int i = 1; i < (CountPipes / 100) + 2; i++)
                {
                    if (EndIndex / 100 == i)
                    {
                        Cell.Text = Cell.Text + "<b>" + i.ToString() + "</b>";
                    }
                    else
                    {
                        Cell.Text = Cell.Text + " <a title='Показать записи " + ((i * 100) - 100) + "-" + (i * 100) + "' href=\"javascript: __doPostBack('SKLAD','CANCEL#" + ((i * 100) - 100) + "#" + (i * 100) + "' )\">" + i + "</a> ";
                    }

                }
                Cell.ColumnSpan = tblSklad.Rows[tblSklad.Rows.Count - 1].Cells.Count; Cell.CssClass = "css_td"; Row.Cells.Add(Cell);
                tblSklad.Rows.Add(Row);
            }

            if(ddlPocket_name.SelectedItem.Text == "")
            {
                LabelLastPocket.Text = LastCount();
                //покраска значений количества труб в штабеле/пакете как в мнемосхеме
                ColorPipes(ddlZone.SelectedItem.Text, ddlStack_name.SelectedItem.Text, ddlPocket_name.SelectedItem.Text, LabelLastPocket);

                //подитоги - для последнего пакета
                Row = new TableRow();
                Cell = new TableCell(); Cell.ColumnSpan = 13; Cell.Text = ""; Row.Cells.Add(Cell);
                Cell = new TableCell(); Cell.Text = LabelLastPocket.Text;
                if (LabelLastPocket.BackColor == Color.Yellow)
                {
                    Cell.BackColor = Color.Yellow;
                }
                else if (LabelLastPocket.BackColor == Color.Red)
                {
                    Cell.BackColor = Color.Red;
                }
                else if (LabelLastPocket.BackColor == Color.LimeGreen)
                {
                    Cell.BackColor = Color.LimeGreen;
                }
                Cell.HorizontalAlign = HorizontalAlign.Center; Row.Cells.Add(Cell);
                Cell = new TableCell(); Cell.ColumnSpan = 5; Row.Cells.Add(Cell);
                Row.Font.Bold = true;
                tblSklad.Rows.Add(Row);

                //итоговые данные
                Row = new TableRow();
                Cell = new TableCell(); Cell.ColumnSpan = 13; Cell.Text = "&nbsp;Итого:"; Row.Cells.Add(Cell);
                Cell = new TableCell(); Cell.Text = CountOfPipes.Text + LabelQNT_T.Text; Cell.HorizontalAlign = HorizontalAlign.Center; Row.Cells.Add(Cell);
                Cell = new TableCell(); Cell.ColumnSpan = 5; Row.Cells.Add(Cell);
                Row.Font.Bold = true;
                tblSklad.Rows.Add(Row);
            }
            
            if (ddlPocket_name.SelectedItem.Text != "")
            {

                //итоговые данные
                Row = new TableRow();
                Cell = new TableCell(); Cell.ColumnSpan = 12; Cell.Text = "&nbsp;Итого:"; Row.Cells.Add(Cell);
                Cell = new TableCell(); Cell.Text = CountOfPipes.Text + LabelQNT_T.Text; Cell.HorizontalAlign = HorizontalAlign.Center; Row.Cells.Add(Cell);
                //покраска значений количества труб в штабеле/пакете как в мнемосхеме
                ColorPipes(ddlZone.SelectedItem.Text, ddlStack_name.SelectedItem.Text, ddlPocket_name.SelectedItem.Text, SkladCountPipes);
                if (SkladCountPipes.BackColor == Color.Yellow)
                {
                    Cell.BackColor = Color.Yellow;
                }
                else if (SkladCountPipes.BackColor == Color.Red)
                {
                    Cell.BackColor = Color.Red;
                }
                else if (SkladCountPipes.BackColor == Color.LimeGreen)
                {
                    Cell.BackColor = Color.LimeGreen;
                }
                Cell = new TableCell(); Cell.ColumnSpan = 5; Row.Cells.Add(Cell);
                Row.Font.Bold = true;
                tblSklad.Rows.Add(Row);
            }
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка", ex);
        }
    }

    //Редактироние записей в таблице склад
    protected void EditSklad(String RowID, bool bCheckDuplicates)
    {
        #region *** проверка на ошибки ввода *********************
        int PipeNumber = 0;
        int.TryParse(bCheckDuplicates ? tbPipeNumber.Text.Trim() : VPipeNumber, out PipeNumber);
        if (PipeNumber < 1000000 || PipeNumber >= 100000000)
        {
            Master.AlertMessage = "Номер трубы имеет неверный формат.\r\nНомер должен состоять из года (одна или 2 цифры) и самого номера (6 цифр) без пробелов и разделителей";
            return;
        }
        #endregion *** проверка на ошибки ввода *********************

        try
        { 
            // проверка на дубликат и вывод информационного сообщения при нахождении с указанием всех мест
            string msg = CheckPlaces(PipeNumber, false, RowID);
            if (msg != "")
            {
                if (bCheckDuplicates)
                {
                    VOperation = "EditSclad";
                    VPipeNumber = PipeNumber.ToString();
                    VDefects = ddlDefect.SelectedItem.Text;
                    VNotes = tbNotes.Text != "" ? tbNotes.Text : ddlNotes.SelectedItem.Text;
                    VNZP = Convert.ToInt32(chbNZP0.Checked);
                    VPrezentation = Convert.ToInt32(cbPresentation.Checked);
                    VRowID = RowID;

                    lblAddDupl.Text = msg + "\r\n Удалить ранние дубликаты трубы?";
                    PopupWindow1.ContentPanelId = pnlConfirmAdd.ID;
                    PopupWindow1.Title = "Редактирование трубы";
                    pnlConfirmAdd.Visible = true;

                    SkladTable(false, "", VStartIndex, VEndIndex, false);
                    hfAutoFillData.Value = "";
                    return;
                }
                else
                {
                    DeleteDuplicates(PipeNumber, RowID);
                }
            }

            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();
            cmd.Transaction = conn.BeginTransaction();
            //Вставляем новую запись
            String SQL = "insert into STOREHOUSES_OF_PIPES_BY_NUM " +
                                "(TRX_DATE,DEFECT,EMPLOYER_NUMBER,SHIFT_INDEX,OPERATOR_ID, " +
                                "EDIT_STATE,ORIGINAL_ROWID,ROW_ID,NOTES,PIPENUMBER," +
                                "INVENTORY_CODE,PIPE_DIAMETER,PIPE_THICKNESS,PIPE_STEELMARK,PRESENTATION," +
                                "ZONE_NAME,STACK_NAME,POCKET_NUM,DESTINATION,SHOP,NZP) " +
                            "(select " +
                                "sysdate,?,?,?,?," +
                                "0,?,to_char(sysdate,'DD.MM.YYYY HH24:MI:SS')||'_'||ZONE_NAME||'_'||STACK_NAME||'_'||POCKET_NUM||'_'||PIPENUMBER,?,?," +
                                "?,?,?,?,?," +
                                "ZONE_NAME,STACK_NAME,POCKET_NUM,DESTINATION,SHOP,? " +
                            "from STOREHOUSES_OF_PIPES_BY_NUM where ROW_ID = ? and EDIT_STATE = 0)";


            cmd.Parameters.Clear();
            cmd.CommandText = SQL;
            cmd.Parameters.AddWithValue("DEFECT", bCheckDuplicates ? ddlDefect.SelectedItem.Text : VDefects);
            cmd.Parameters.AddWithValue("EMPLOYER_NUMBER", Authentification.User.TabNumber);
            cmd.Parameters.AddWithValue("SHIFT_INDEX", Authentification.Shift);
            cmd.Parameters.AddWithValue("OPERATOR_ID", Authentification.User.UserName);
            cmd.Parameters.AddWithValue("ORIGINAL_ROWID", RowID);
            cmd.Parameters.AddWithValue("NOTES", bCheckDuplicates ? (tbNotes.Text != "" ? tbNotes.Text : ddlNotes.SelectedItem.Text) : VNotes);
            cmd.Parameters.AddWithValue("PIPENUMBER", Checking.GetDbType(bCheckDuplicates ? tbPipeNumber.Text : VPipeNumber));
            String[] AFData = hfAutoFillData.Value.Split('|');
            if (AFData.Length != 4)
            {
                cmd.Parameters.AddWithValue("INVENTORY_CODE", "");
                cmd.Parameters.AddWithValue("PIPE_DIAMETER", "");
                cmd.Parameters.AddWithValue("PIPE_THICKNESS", "");
                cmd.Parameters.AddWithValue("PIPE_STEELMARK", "");
            }
            else
            {
                cmd.Parameters.AddWithValue("INVENTORY_CODE", AFData[3]);
                cmd.Parameters.AddWithValue("PIPE_DIAMETER", Checking.GetDbType(AFData[0]));
                cmd.Parameters.AddWithValue("PIPE_THICKNESS", Checking.GetDbType(AFData[1]));
                cmd.Parameters.AddWithValue("PIPE_STEELMARK", AFData[2]);
            }
            int Presentation = 0;
            if (cbPresentation.Checked) Presentation = 1;
            cmd.Parameters.AddWithValue("PRESENTATION", bCheckDuplicates ? Presentation : VPrezentation);
            cmd.Parameters.AddWithValue("NZP", bCheckDuplicates ? Convert.ToInt32(chbNZP0.Checked) : VNZP);
            cmd.Parameters.AddWithValue("ROW_ID", RowID);

            //Откатываем транзакцию если возникли ошибки
            try { cmd.ExecuteNonQuery(); }
            catch { cmd.Transaction.Rollback(); }

            //Обновляем старую запись
            SQL = "update STOREHOUSES_OF_PIPES_BY_NUM set EDIT_STATE = 1 where ROW_ID = ? and EDIT_STATE = 0";
            cmd.Parameters.Clear();
            cmd.CommandText = SQL;
            cmd.Parameters.AddWithValue("ROW_ID", RowID);

            //Откатываем транзакцию если возникли ошибки
            try { cmd.ExecuteNonQuery(); }
            catch { cmd.Transaction.Rollback(); }

            cmd.Transaction.Commit();

            ClearStoreHousesViewState();

            SkladTable(false, "", VStartIndex, VEndIndex, false);
            hfAutoFillData.Value = "";
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка", ex);
        }
    }
    //Удаление записей в таблице склад
    protected void DeleteSklad(String RowID)
    {
        OleDbConnection conn = Master.Connect.ORACLE_TESC3();
        OleDbCommand cmd = conn.CreateCommand();
        cmd.Transaction = conn.BeginTransaction();
        try
        {
            DeleteSklad(RowID, cmd, 0);
        }
        catch (Exception ex)
        {
            cmd.Transaction.Rollback();
            Master.AddErrorMessage("Ошибка", ex);
            cmd.Dispose();
            return;
        }
        cmd.Transaction.Commit();
        cmd.Dispose();
    }

    protected void DeleteSklad(String RowID, OleDbCommand cmd, int cnt)
    {
        //Вставляем новую запись
        cmd.CommandText = "insert into STOREHOUSES_OF_PIPES_BY_NUM " +
                            "(TRX_DATE,DEFECT,EMPLOYER_NUMBER,SHIFT_INDEX,OPERATOR_ID, " +
                            "EDIT_STATE,ORIGINAL_ROWID,ROW_ID,NOTES,PIPENUMBER," +
                            "INVENTORY_CODE,PIPE_DIAMETER,PIPE_THICKNESS,PIPE_STEELMARK,PRESENTATION," +
                            "ZONE_NAME,STACK_NAME,POCKET_NUM,DESTINATION,SHOP,NZP) " +
                        "(select " +
                            "sysdate,DEFECT,?,?,?," +
                            "3,?,to_char(sysdate,'DD.MM.YYYY HH24:MI:SS')||'_'||PIPENUMBER||'_'||?||'_'||?, NOTES, PIPENUMBER," +
                            "INVENTORY_CODE,PIPE_DIAMETER,PIPE_THICKNESS,PIPE_STEELMARK,PRESENTATION," +
                            "ZONE_NAME,STACK_NAME,POCKET_NUM,DESTINATION,SHOP,NZP " +
                        "from STOREHOUSES_OF_PIPES_BY_NUM where ROW_ID = ? and EDIT_STATE = 0)";

        cmd.Parameters.Clear();
        cmd.Parameters.AddWithValue("EMPLOYER_NUMBER", Authentification.User.TabNumber);
        cmd.Parameters.AddWithValue("SHIFT_INDEX", Authentification.Shift);
        cmd.Parameters.AddWithValue("OPERATOR_ID", Authentification.User.UserName);
        cmd.Parameters.AddWithValue("ORIGINAL_ROWID", RowID);
        cmd.Parameters.AddWithValue("OPERATOR_ID_RowID", Authentification.User.UserName);
        cmd.Parameters.AddWithValue("CNT_RowID", cnt.ToString());
        cmd.Parameters.AddWithValue("ROW_ID", RowID);
        cmd.ExecuteNonQuery();

        //Обновляем старую запись
        cmd.CommandText = "update STOREHOUSES_OF_PIPES_BY_NUM set EDIT_STATE = 2 where ROW_ID = ? and EDIT_STATE = 0";
        cmd.Parameters.Clear();
        cmd.Parameters.AddWithValue("ROW_ID", RowID);
        cmd.ExecuteNonQuery();
    }

    protected void btnDelAll_Click(object sender, EventArgs e)
    {
        OleDbConnection conn = Master.Connect.ORACLE_TESC3();
        OleDbCommand cmd = conn.CreateCommand();
        try
        {
            cmd.Transaction = conn.BeginTransaction();

            // находим все row_id
            string query2 = BuildkladQueryPart2(cmd);
            cmd.CommandText = "select SOPBN.row_id " + query2;
            OleDbDataReader rdr = cmd.ExecuteReader();
            List<string> rl = new List<string>();
            while (rdr.Read()) rl.Add(rdr["row_id"].ToString());
            rdr.Close();
            rdr.Dispose();

            // удаляем все записи с найденными по запросу row_id
            for (int i = 0; i < rl.Count; i++) DeleteSklad(rl[i], cmd, i);
        }
        catch (Exception ex) { cmd.Transaction.Rollback(); cmd.Dispose(); Master.AddErrorMessage("Ошибка группового удаления", ex); return; }

        cmd.Transaction.Commit();
        cmd.Dispose();
    }

    #endregion *** СКЛАД **************************************************************************************

    #region *** Прочии Фии **********************************************************************************

    //получение тэга <A> для URL на историю трубы
    protected String GetPipeHistoryURL(String PipeNumber)
    {
        if (PipeNumber == "") return "";
        PipeNumber = PipeNumber.PadLeft(8, '0');
        String PipeYearPart = PipeNumber.Substring(0, 2);
        String PipeNumberPart = PipeNumber.Remove(0, 2);
        return "<a href=\"Reports\\FullPipeHistoryReport.aspx?HideAllPanels=1&PIPE_NUMBER=" + PipeNumberPart + "&PIPE_YEAR=" + PipeYearPart + "&dohtml=1" + "\" target=\"_blank\">" + PipeNumber + "</a>";
    }

    // Выбор элемента с текстом в ячейке из выпадающего списка (для редактирования)
    protected bool SelectDDLItem(DropDownList DDL, TableCell Cell)
    {
        //Берем значение из ячейки таблицы
        ListItem item1 = DDL.Items.FindByText(Cell.Text);
        //Если значене есть, то выделяем элемент
        if (item1 != null)
        {
            DDL.SelectedIndex = DDL.Items.IndexOf(item1);
            return true;
        }
        else
        {
            DDL.SelectedIndex = 0;
            return false;
        }
    }

    /// <summary>проверка на дубликат. Возвращает сообщение при нахождении дубликата с указанием всех мест или пустую строку при отсутствии</summary>
    /// <param name="PipeNumber">номер трубы</param>
    /// <param name="bAdd">проверка идет перед добавлением новой записи. Влияет на текст сообщения и условия вывода</param>
    /// <returns></returns>
    String CheckPlaces(int PipeNumber, bool bAdd, string rowID)
    {
        OleDbConnection conn = Master.Connect.ORACLE_TESC3();
        OleDbCommand cmd = conn.CreateCommand();
        cmd.CommandText = "select ZONE_NAME, STACK_NAME, POCKET_NUM, NZP from STOREHOUSES_OF_PIPES_BY_NUM where edit_state=0 and pipenumber = ? and (row_id != ? or ? is null)";
        cmd.Parameters.AddWithValue("pipe_number", PipeNumber);
        cmd.Parameters.AddWithValue("row_id1", rowID);
        cmd.Parameters.AddWithValue("row_id2", rowID);
        OleDbDataReader rdr = cmd.ExecuteReader();
        StringBuilder ret = new StringBuilder();
        int cnt = 0;
        while (rdr.Read())
        {
            ret.AppendLine(" - Складской объект: " + rdr["ZONE_NAME"].ToString().Trim() + ", штабель: " + rdr["STACK_NAME"].ToString().Trim() + ", карман: " + rdr["POCKET_NUM"].ToString().Trim() + (rdr["NZP"].ToString().Trim() == "1" ? " (НЗП)" : ""));
            cnt++;
        }

        if (!bAdd && cnt > 0)
            return "Внимание! Труба с номером " + PipeNumber + " находится более чем в одном месте\r\n" + ret.ToString();

        if (bAdd && cnt > 0)
            return "Внимание! Труба с номером " + PipeNumber + " уже находится на складе в следующ" + (cnt == 1 ? "ем месте:" : "их местах:") + "\r\n" + ret.ToString();

        return "";
    }

    #endregion *** Прочии Фии **********************************************************************************
    
    #region авто заполнение полей данными из бд
    //**********************************************************************************************************
    class AutoFillData
    { 
        public String NumPart;
        public String Diam;
        public String Thickness;
        public String Steelmark;
        public String NTD;
        public String PipeLength;
        public String InvNum;
        public String Note;
    }
    void AutoFillLines(bool nzp, String numPipe, TableRow row)
    {
        // проверка длины номера и его целостности
        int tmpNum;
        bool equal = false;
        if (int.TryParse(numPipe, out tmpNum))
            if (tmpNum > 0)
                equal = true;
        if (!equal)
        {
            Master.AlertMessage = "номер трубы должен быть целым положительным числом";
            return;
        }
        if (tmpNum < 1000000)
        {
            Master.AlertMessage = "номер трубы слишком короткий";
            return;
        }

        //очищаем поля склада
        for (int i = 5; i < 10; i++)
            row.Cells[i].Text = "";

        AutoFillData dt;
        if (nzp)
        {
            // ищем трубу (трубы когда либо принятые на инспекции или ремонте) и в резервных номерах
            dt = AutoWriteInInspectionPipes(numPipe);
            // если трубы на инспекциях или ремонте нет, ищем трубу в optimal_pipes
            if (dt == null) dt = AutoWriteInOptimalPipes(numPipe);
        }
        else
        {
            // ищем трубу на складе
            dt = AutoWriteInPipesShops(numPipe);
        }

        // заполняем поля
        if (dt != null)
        {
            row.Cells[5].Text = dt.Diam;
            row.Cells[6].Text = dt.Thickness;
            row.Cells[7].Text = dt.Steelmark;
            row.Cells[8].Text = dt.InvNum;
            row.Cells[9].Text = dt.NTD;
            hfAutoFillData.Value = dt.Diam + "|" + dt.Thickness + "|" + dt.Steelmark + "|" + dt.InvNum;
        }
    }

    // Получение данных о трубе с инспекций
    AutoFillData AutoWriteInInspectionPipes(String numPipe)
    {
        try
        {
            int num = 0;
            bool NotPipeOnInspection = true;
            if (Int32.TryParse(numPipe, out num) == false) return null;

            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();
            cmd.Parameters.Clear();
            cmd.CommandText = "select LOT_NUMBER, DIAMETER, THICKNESS, STEELMARK, GOST, LENGTH, NEXT_DIRECTION, TRX_DATE from INSPECTION_PIPES where EDIT_STATE=? and PIPE_NUMBER=? and (next_direction='SKLAD') ORDER BY TRX_DATE DESC";
            //cmd.CommandText = "select LOT_NUMBER, DIAMETER, THICKNESS, STEELMARK, GOST, LENGTH, NEXT_DIRECTION, TRX_DATE from INSPECTION_PIPES where EDIT_STATE=? and PIPE_NUMBER=? ORDER BY TRX_DATE DESC";

            cmd.Parameters.AddWithValue("EDIT_STATE", "0");
            cmd.Parameters.AddWithValue("PIPE_NUMBER", Convert.ToInt32(numPipe));
            cmd.ExecuteNonQuery();
            OleDbDataReader reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                if (reader["NEXT_DIRECTION"].ToString() == "SKLAD")
                {
                    AutoFillData dt = new AutoFillData();
                    NotPipeOnInspection = false;
                    dt.NumPart = reader["LOT_NUMBER"].ToString();
                    dt.Diam = reader["DIAMETER"].ToString();
                    dt.Thickness = reader["THICKNESS"].ToString();
                    dt.Steelmark = reader["STEELMARK"].ToString();
                    dt.NTD = reader["GOST"].ToString();
                    dt.PipeLength = "";
                    return dt;
                }
            }

            // ищем трубу в резервных номерах
            OleDbConnection conn1 = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd1 = conn1.CreateCommand();
            cmd1.Parameters.Clear();
            cmd1.CommandText = "select LOT_NUMBER, DIAMETER, THICKNESS, STEELMARK, NTD, PIPE_LENGTH, REC_DATE from RESERVE_NUMBERS where EDIT_STATE=? and PIPE_NUMBER=? ORDER BY REC_DATE DESC";
            cmd1.Parameters.AddWithValue("EDIT_STATE", "0");
            cmd1.Parameters.AddWithValue("PIPE_NUMBER", Checking.GetDbType(numPipe));
            OleDbDataReader reader1 = cmd1.ExecuteReader();
            if (reader1.Read())
            {
                bool readRecerv = false;
                if (NotPipeOnInspection)
                    readRecerv = true;
                else if (Convert.ToDateTime(reader["TRX_DATE"]) < Convert.ToDateTime(reader1["REC_DATE"]))
                    readRecerv = true;

                if (readRecerv)
                {
                    AutoFillData dt = new AutoFillData();
                    dt.NumPart = reader1["LOT_NUMBER"].ToString();
                    dt.Diam = reader1["DIAMETER"].ToString();
                    dt.Thickness = reader1["THICKNESS"].ToString();
                    dt.Steelmark = reader1["STEELMARK"].ToString();
                    dt.NTD = reader1["NTD"].ToString();
                    dt.PipeLength = reader1["PIPE_LENGTH"].ToString();
                    dt.PipeLength = "";
                    return dt;
                }
            }
            reader.Close();
            reader.Dispose();
            cmd.Dispose();
            reader1.Close();
            reader1.Dispose();
            cmd1.Dispose();

        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка поиска номера", ex);

        }
        return null;
    }

    // Получение данных о трубе из оракла
    AutoFillData AutoWriteInPipesShops(String numPipe)
    {
        try
        {
            int num = 0;
            if (Int32.TryParse(numPipe, out num) == false) return null;

            OleDbConnection conn = Master.Connect.ORACLE_ORACLE();
            OleDbCommand cmd = conn.CreateCommand();
            cmd.Parameters.Clear();
            cmd.CommandText = "select *" +
                " from V_T3_PIPE_STORAGE left join V_T3_PIPE_ITEMS on V_T3_PIPE_STORAGE.INVENTORY_ITEM_ID=V_T3_PIPE_ITEMS.INVENTORY_ITEM_ID" +
                " where (SUBLOT_NUMBER=?) and (PRODUCER_YEAR=?) and (PRODUCER_ORG_ID='127')";
            cmd.Parameters.AddWithValue("SUBLOT_NUMBER", Convert.ToInt32(numPipe) % 1000000);
            cmd.Parameters.AddWithValue("PRODUCER_YEAR", Convert.ToInt32(numPipe) / 1000000);
            OleDbDataReader reader = cmd.ExecuteReader();
            // номенклатура и статус удачного заполнения (нужно для последующего перезаполнения списка номенклатур)
            if (reader.Read())
            {
                AutoFillData dt = new AutoFillData();

                dt.NumPart = reader["LOT"].ToString();
                dt.Diam = reader["DIAMETER"].ToString();
                dt.Thickness = reader["WALL"].ToString();
                dt.Steelmark = reader["STEEL"].ToString();
                String gost = reader["GOST"].ToString();
                String group = reader["GRUP"].ToString();
                String NTD = gost;
                if (group != "") NTD += " гр." + group;
                dt.NTD = NTD;
                dt.InvNum = reader["NOMER"].ToString();
                dt.PipeLength = "";
                dt.Note = reader["SUBINVENTORY_CODE"].ToString();
                dt.PipeLength = reader["PIPE_LENGTH"].ToString();
                String Delivery = reader["DELIVERY_ID"].ToString();
                if (Delivery != "")
                {
                    if (dt.Note != "") dt.Note += "; ";
                    dt.Note = "Доставка " + Delivery;
                }
                if (reader["TUBE_TYPE"].ToString() == "Y")
                {
                    if (dt.Note != "") dt.Note += "; ";
                    dt.Note += "Изолированная";
                }
                return dt;
            }
            reader.Close();
            reader.Dispose();
            cmd.Dispose();
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка поиска номера", ex);
        }

        return null;
    }

    // Получение данных о трубе из optimal_pipes
    AutoFillData AutoWriteInOptimalPipes(String numPipe)
    {
        try
        {
            int num = 0;
            if (Int32.TryParse(numPipe, out num) == false) return null;

            OleDbConnection conn = Master.Connect.ORACLE_ORACLE();
            OleDbCommand cmd = conn.CreateCommand();
            cmd.Parameters.Clear();
            cmd.CommandText = @"select op.pipe_number, pipelength, op.part_no, SPR.DIAMETER, SPR.THICKNESS, SPR.STAL, SPR.GOST, SPR.GRUP, NOMER from Optimal_Pipes op 
left join geometry_coils_sklad gc on (OP.COIL_INTERNALNO = GC.COIL_RUN_NO and OP.COIL_PIPEPART_NO = GC.COIL_PIPEPART_NO and OP.COIL_PIPEPART_YEAR=GC.COIL_PIPEPART_YEAR)
left join Campaigns cmp on GC.CAMPAIGN_LINE_ID = CMP.CAMPAIGN_LINE_ID
left join ORACLE.V_T3_PIPE_ITEMS spr on CMP.INVENTORY_CODE = SPR.NOMER
where op.pipe_number = ? and GC.EDIT_STATE = 0 and CMP.EDIT_STATE = 0 and SPR.ORG_ID = 127";

            cmd.Parameters.AddWithValue("pipe_number", numPipe);
            OleDbDataReader reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                AutoFillData dt = new AutoFillData();

                dt.NumPart = reader["part"].ToString();
                dt.Diam = reader["DIAMETER"].ToString();
                dt.Thickness = reader["THICKNESS"].ToString();
                dt.Steelmark = reader["STAL"].ToString();
                String gost = reader["GOST"].ToString();
                String group = reader["GRUP"].ToString();
                String NTD = gost;
                if (group != "") NTD += " гр." + group;
                dt.NTD = NTD;
                dt.InvNum = reader["NOMER"].ToString();
                dt.PipeLength = "";
                dt.PipeLength = reader["PIPELENGTH"].ToString();
                return dt;
            }
            reader.Close();
            reader.Dispose();
            cmd.Dispose();
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка поиска номера в OptimalPipes", ex);
        }

        return null;
    }

    //**********************************************************************************************************
    #endregion


    //парсинг штрихкода по складскому объекту.штабелю.карману
    protected void txbBarcode_TextChanged(object sender, EventArgs e)
    {
        try
        {
            //разбор на 3 части
            if (txbBarcode.Text != "")
            {
                OleDbConnection conn = Master.Connect.ORACLE_TESC3();
                OleDbCommand cmd = conn.CreateCommand();
                cmd = conn.CreateCommand();

                var str = txbBarcode.Text;
                var splitted = str.Split('.');
                string a = "";
                int c = 0;


                //проверка на корректность штрихкода
                var s = txbBarcode.Text;
                var pattern = @"([^.]*)\.([^.]*)\.([^.*])";
                var pattern1 = @"([^.]*)\.([^.]*)\.([^.*])\.([^.]*)";
                var regex1 = new Regex(pattern1);
                var regex = new Regex(pattern);

                if (!regex.IsMatch(s) || regex1.IsMatch(s))
                {
                    Master.AlertMessage = "Ошибка: Считанный штрих-код не является штрих-кодом штабеля ТЭСЦ-3";
                    txbBarcode.Text = "";
                    return;
                }
                //проверка существует ли такой объкет/штабель/карман
                cmd.CommandText = @"SELECT zone_name, stack_name, count_of_pockets
                                FROM SPR_ALL_STACKS
                                WHERE (ZONE_NAME= ?)
                                   and(stack_name = ?) AND USE_FOR_BYPIPESNUMS = 1 AND IS_ACTIVE = 1
                                ORDER BY COUNT_OF_POCKETS";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("ZONE_NAME", splitted[0]);
                cmd.Parameters.AddWithValue("STACK_NAME", splitted[1]);
                OleDbDataReader rdr = cmd.ExecuteReader();
                if (rdr.Read())
                {
                    a = rdr["ZONE_NAME"] + " " + rdr["STACK_NAME"];
                    c = Convert.ToInt32(rdr["COUNT_OF_POCKETS"]);
                }
                rdr.Close();
                rdr.Dispose();

                int pocket_index = Convert.ToInt32(splitted[2]);

                //если есть, то подставляем значения в выпадающие списки с последующим рендером остальной формы
                if (a != "" && c >= pocket_index)
                {
                    ddlZone.SelectedValue = splitted[0];
                    ddlZone_SelectedIndexChanged(sender, e);
                    ddlStack_name.SelectedValue = splitted[1];
                    ddlPocket_name.SelectedValue = splitted[2];

                    GetSkladRollNameByZoneName(ref RollNameForCurrentZone);
                    MainMultiView.SetActiveView(vLogined);
                }
                else
                {
                    Master.AlertMessage = "Ошибка: Такого объекта/штабеля/кармана не существует";
                    txbBarcode.Text = "";
                }
            }
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка: " + ex.Message);
        }
    }

    //Обработчик смены текущего элемента списка штабелей
    protected void ddlStack_SelectedIndexChanged(object sender, EventArgs e)
    {
        //FeelDdlsPockets(sender);
        FeelDdlsPockets(sender);
    }
    //Заполнение списка карманов штабеля в соответствии с названием штабеля
    protected void FeelDdlsPockets(object sender)
    {
        DropDownList tmpddlPocket;
        //if (((DropDownList)sender).ID == "ddlStack")
        //{
        //    tmpddlPocket = ddlPocket;
        //}
        //else if (((DropDownList)sender).ID == "ddlStackEdt")
        //{
        //    tmpddlPocket = ddlPocketEdt;
        //}

        //tmpddlPocket.Items.Clear();
        ddlPocket_name.Items.Clear();
        if (/*(((DropDownList)sender).SelectedIndex == 0) && */(((DropDownList)sender).ID == "ddlStack"))
        {
            //tmpddlPocket.Items.Add("(Все)");
            ddlPocket_name.Items.Add("(Все)");
            //return;
        }
        OleDbConnection conn;
        try
        {
            //Подключение к БД
            conn = Master.Connect.ORACLE_TESC3();//new OleDbConnection(ConfigurationManager.AppSettings.Get("OraConnection_MS1_tesc3"));
            //conn.Open();
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Сбой подключения к базе данных: " + ex.Message);
            return;
        }
        try
        {
            OleDbCommand cmd = conn.CreateCommand();
            //Формирование запроса на получение количества карманов в текущем штабеле
            cmd.CommandText = "select Count_Of_Pockets from SPR_ALL_STACKS where (Stack_Name =?) and (ZONE_NAME= ?) and IS_ACTIVE = 1 order by Stack_Name, Count_Of_Pockets ";
            cmd.Parameters.AddWithValue("Stack_Name", System.Convert.ToString(((DropDownList)sender).SelectedItem.Value));
            cmd.Parameters.AddWithValue("ZONE_NAME", System.Convert.ToString(SelectedZoneName));
            OleDbDataReader readerStacks = cmd.ExecuteReader();
            //Запись списка штабелей в лист по штабям
            if (readerStacks.Read())
            {
                int maxindex = System.Convert.ToInt32(readerStacks["Count_Of_Pockets"]);
                for (int index = 1; index <= maxindex; index++)
                {
                    ListItem lstItem = new ListItem(index.ToString(), index.ToString());
                    //tmpddlPocket.Items.Add(lstItem);
                    ddlPocket_name.Items.Add(lstItem);
                }
            }
            readerStacks.Close();
            cmd.Dispose();
            //conn.Close();
        }
        catch (Exception ex)
        {
            //conn.Close();
            Master.AddErrorMessage("Ошибка: " + ex.Message);
        }

    }
    protected void btnMoveOk_Click(object sender, EventArgs e)
    {
        pnlConfirmMove.Visible = false;
        MovePipes(false);
    }

    protected void btnMoveCancel_Click(object sender, EventArgs e)
    {
        pnlConfirmMove.Visible = false;
    }

    protected void btnSortOk_Click(object sender, EventArgs e)
    {
        pnlSortMove.Visible = false;
        MovePipes(false);
    }

    protected void btnSortCancel_Click(object sender, EventArgs e)
    {
        pnlSortMove.Visible = false;
    }
    protected void btnMoveUpdate_Click(object sender, EventArgs e)
    {

    }

    protected void btnReadBarcode_Click(object sender, EventArgs e)
    {
        txbBarcode.Text = "";
        txbBarcode.Focus();
        Page.SetFocus(txbBarcode);
    }
    protected void btnAddOk_Click(object sender, EventArgs e)
    {
        pnlConfirmAdd.Visible = false;
        switch (VOperation)
        {
            case "AddPrihod":
                Prihod(false);
                break;
            case "EditPrihod":
                EditPrihod(VRowID, false);
                break;
            case "EditSclad":
                EditSklad(VRowID, false);
                break;
        }
    }
    protected void btnAddCancel_Click(object sender, EventArgs e)
    {
        pnlConfirmAdd.Visible = false;
        switch (VOperation)
        {
            case "AddPrihod":
                PrihodTable(false, "", false);
                break;
            case "EditPrihod":
                PrihodTable(false, "", false);
                break;
            case "EditSclad":
                SkladTable(false, "", VStartIndex, VEndIndex, false);
                hfAutoFillData.Value = "";
                break;
        }

        ClearChildViewState();
    }

    protected void btnPrihodOk_Click(object sender, EventArgs e)
    {
        pnlPrihodSort.Visible = false;
        switch (VOperation)
        {
            case "AddPrihod":
                Prihod(false);
                break;
            case "EditPrihod":
                EditPrihod(VRowID, false);
                break;
            case "EditSclad":
                EditSklad(VRowID, false);
                break;
        }
    }
    protected void btnPrihodCancel_Click(object sender, EventArgs e)
    {
        pnlPrihodSort.Visible = false;
        switch (VOperation)
        {
            case "AddPrihod":
                PrihodTable(false, "", false);
                break;
            case "EditPrihod":
                PrihodTable(false, "", false);
                break;
            case "EditSclad":
                SkladTable(false, "", VStartIndex, VEndIndex, false);
                hfAutoFillData.Value = "";
                break;
        }

        ClearChildViewState();
    }

    /// <summary>Очистка ViewState</summary>
    public void ClearStoreHousesViewState()
    {
        VPipeNumber = "";
        VDefects = "";
        VNotes = "";
        VNZP = 0;
        VPrezentation = 0;
        VOperation = "";
    }

    #region ********************Удаление дубликатов трубы********************
    /// <summary>Удаление дубликатов трубы</summary>
    /// <param name="PipeNumber">Номер трубы</param>
    /// <param name="NoDelRowID">Идентификатор записи, который не надо удалять (при редактировании)</param>
    protected void DeleteDuplicates(Int32 PipeNumber, String NoDelRowID)
    {
        OleDbCommand cmd = null;
        try
        {
            OleDbTransaction trans = Master.Connect.ORACLE_TESC3().BeginTransaction();
            cmd = Master.Connect.ORACLE_TESC3().CreateCommand();

            cmd.Transaction = trans;
            //вставка строки-подтверждения удаления дубликатов
            cmd.CommandText = @"INSERT INTO storehouses_of_pipes_by_num (trx_date,
                                                                            defect,
                                                                            employer_number,
                                                                            shift_index,
                                                                            operator_id,
                                                                            edit_state,
                                                                            original_rowid,
                                                                            row_id,
                                                                            notes,
                                                                            pipenumber,
                                                                            inventory_code,
                                                                            pipe_diameter,
                                                                            pipe_thickness,
                                                                            pipe_steelmark,
                                                                            presentation,
                                                                            zone_name,
                                                                            stack_name,
                                                                            pocket_num,
                                                                            destination,
                                                                            shop,
                                                                            nzp)
                                    (SELECT SYSDATE,
                                            defect,
                                            ?,
                                            ?,
                                            ?,
                                            7,
                                            row_id,
                                                TO_CHAR (SYSDATE, 'DD.MM.YYYY HH24:MI:SS')
                                            || '_'
                                            || pipenumber
                                            || '_'
                                            || ?
                                            || '_'
                                            || rownum,
                                            notes,
                                            pipenumber,
                                            inventory_code,
                                            pipe_diameter,
                                            pipe_thickness,
                                            pipe_steelmark,
                                            presentation,
                                            zone_name,
                                            stack_name,
                                            pocket_num,
                                            destination,
                                            shop,
                                            nzp
                                        FROM storehouses_of_pipes_by_num
                                        WHERE pipenumber = ? AND edit_state = 0 AND (row_id != ? or ? is null))";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("employer_number", Authentification.User.TabNumber);
            cmd.Parameters.AddWithValue("shift_index", Authentification.Shift);
            cmd.Parameters.AddWithValue("operator_id", Authentification.User.UserName);
            cmd.Parameters.AddWithValue("operator_id_RowID", Authentification.User.UserName);
            cmd.Parameters.AddWithValue("pipenumber", PipeNumber);
            cmd.Parameters.AddWithValue("row_id1", NoDelRowID);
            cmd.Parameters.AddWithValue("row_id2", NoDelRowID);
            cmd.ExecuteNonQuery();

            //Обновляем старую запись
            cmd.CommandText = "update STOREHOUSES_OF_PIPES_BY_NUM set EDIT_STATE = 2 where pipenumber = ? and EDIT_STATE = 0 AND (row_id != ? or ? is null)";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("pipenumber", PipeNumber);
            cmd.Parameters.AddWithValue("row_id1", NoDelRowID);
            cmd.Parameters.AddWithValue("row_id2", NoDelRowID);
            cmd.ExecuteNonQuery();

            cmd.Transaction.Commit();
            cmd.Dispose();
        }
        catch (Exception ex)
        {
            cmd.Transaction.Rollback();
            cmd.Dispose();
            Master.AddErrorMessage("Ошибка удаления дубликатов.", ex);
        }
    }
    #endregion ********************Удаление дубликатов трубы********************
}