using System;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.OleDb;
using System.Globalization;

public partial class GeometryInsp : Page
{
    //Имя поля, содержащего привелегии операторов для рабочего места
    const String WORKPLACE_ID = "GEOM_INSP";

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

    public void SelectDDLItemByValue(DropDownList ddl, String val)
    {
        ListItem item = ddl.Items.FindByValue(val);
        ddl.SelectedIndex = ddl.Items.IndexOf(item);
    }

    //свойство: basePipeNumber базовый номер трубы
    protected String basePipeNumber
    {
        get
        {
            object o = ViewState["basePipeNumber"];
            if (o == null) return ""; else return o.ToString();
        }
        set
        {
            ViewState["basePipeNumber"] = value;
        }
    }

    //свойство: baseDiameter базовый диаметр трубы
    protected String baseDiameter
    {
        get
        {
            object o = ViewState["baseDiameter"];
            if (o == null) return ""; else return o.ToString();
        }
        set
        {
            ViewState["baseDiameter"] = value;
        }
    }

    //свойство: baseThickness базовая толщина стенки трубы
    protected String baseThickness
    {
        get
        {
            object o = ViewState["baseThickness"];
            if (o == null) return ""; else return o.ToString();
        }
        set
        {
            ViewState["baseThickness"] = value;
        }
    }

    //свойство: baseSteelMark базовая марка стали трубы
    protected String baseSteelMark
    {
        get
        {
            object o = ViewState["baseSteelMark"];
            if (o == null) return ""; else return o.ToString();
        }
        set
        {
            ViewState["baseSteelMark"] = value;
        }
    }

    //свойство: baseLengthPipe базовая длина трубы
    protected String baseLengthPipe
    {
        get
        {
            object o = ViewState["baseLengthPipe"];
            if (o == null) return ""; else return o.ToString();
        }
        set
        {
            ViewState["baseLengthPipe"] = value;
        }
    }

    //свойство: basePipePartNumber базовый номер партии
    protected String basePipePartNumber
    {
        get
        {
            object o = ViewState["basePipePartNumber"];
            if (o == null) return ""; else return o.ToString();
        }
        set
        {
            ViewState["basePipePartNumber"] = value;
        }
    }

    //свойство: baseCoilNumber базовый номер рулона
    protected String baseCoilPipePartYear
    {
        get
        {
            object o = ViewState["baseCoilPipePartYear"];
            if (o == null) return ""; else return o.ToString();
        }
        set
        {
            ViewState["baseCoilPipePartYear"] = value;
        }
    }

    //свойство: baseCoilNumber базовый номер рулона
    protected String baseCoilPipePartNo
    {
        get
        {
            object o = ViewState["baseCoilPipePartNo"];
            if (o == null) return ""; else return o.ToString();
        }
        set
        {
            ViewState["baseCoilPipePartNo"] = value;
        }
    }

    //свойство: baseCoilInternalNo базовый внутренний номер рулона
    protected String baseCoilInternalNo
    {
        get
        {
            object o = ViewState["baseCoilInternalNo"];
            if (o == null) return ""; else return o.ToString();
        }
        set
        {
            ViewState["baseCoilInternalNo"] = value;
        }
    }
    //год трубы
    protected Int32 PipeYearGeom
    {
        get
        {
            object o = ViewState["PipeYearGeom"];
            if (o == null) return 0; else return Convert.ToInt32(o);
        }
        set
        {
            ViewState["PipeYearGeom"] = value;
        }
    }
    //номер трубы
    protected Int32 PipeNumberGeom
    {
        get
        {
            object o = ViewState["PipeNumberGeom"];
            if (o == null) return 0; else return Convert.ToInt32(o);
        }
        set
        {
            ViewState["PipeNumberGeom"] = value;
        }
    }
    //Номер контрольной цифры
    protected int PipeCheckGeom
    {
        get
        {
            object o = ViewState["PipeCheckGeom"];
            if (o == null) return -1; else return Convert.ToInt32(o);
        }
        set
        {
            ViewState["PipeCheckGeom"] = value;
        }
    }
    //таргет операции
    protected String Target
    {
        get
        {
            object o = ViewState["Target"];
            if (o == null) return ""; else return o.ToString();
        }
        set
        {
            ViewState["Target"] = value;
        }
    }
    //таргет операции
    protected String Campaign
    {
        get
        {
            object o = ViewState["Campaign"];
            if (o == null) return ""; else return o.ToString();
        }
        set
        {
            ViewState["Campaign"] = value;
        }
    }

    private int _baseNtdId;
    public int baseNtdId
    {
        get
        {
            return _baseNtdId;
        }
        set
        {
            _baseNtdId = value;
        }
    }


    //******************************************************************
    //**  Функция возвращает true если в строке txt содержится число  **
    //*   с количеством цифр в целой части <= whole и с количеством    *
    //*   цифр в дробной части <= fractional; целая и дробная части    *
    //**           числа разделяются точкой или запятой               **
    //******************************************************************
    private bool XValidation(string txt, int whole, int fractional)
    {
        txt = txt.Trim();
        if (txt == "") return true;
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
            txt = txt.Trim().Replace('.', ',');
            NumberFormatInfo numformat = new NumberFormatInfo();
            numformat.NumberDecimalSeparator = ",";
            numformat.NumberGroupSeparator = "";
            if (!Double.TryParse(txt, NumberStyles.Float, numformat, out tmpxd)) return false;
            int xwhole, xfractional, xtmp;
            xwhole = txt.IndexOf(",");
            xtmp = txt.Length;
            if (xwhole <= 0) xfractional = 0;
            else xfractional = xtmp - xwhole - 1;
            return ((xwhole == -1 && xtmp > 0 && xtmp <= whole) || (xwhole <= whole && xwhole > 0 && xfractional <= fractional && xfractional > 0));
        }
    }

    //******************************************************************************************************************
    //* *   Проверка правильности заполнения полей ввода диаметров трубы в двух плоскостях и вычисление овальности:   **
    //*   возвращает 0 при удачном вычислении овальности, а также возвращает её значение в txtOval, в текстовой форме  *
    //*   возвращает 1 если ошибочно введён диаметр трубы в плоскости тела                                             *
    //*   возвращает 2 если ошибочно введён диаметр трубы в плоскости шва                                              * 
    //*   возвращает 3 если ни один из диаметров не введён                                                             *
    //* * возвращает 4 в случаи ошибочного ввода обоих диаметров                                                      **
    //******************************************************************************************************************
    protected int OneRun(string txtPSH, string txtPT, out string txtOval)
    {
        if (XValidation(txtPSH, 3, 2))
        {
            if (XValidation(txtPT, 3, 2))
            {
                //вычисление овальности
                NumberFormatInfo numformat = new NumberFormatInfo();
                numformat.NumberDecimalSeparator = ",";
                numformat.NumberGroupSeparator = "";

                Double xPSH, xPT, xO;
                txtPSH = txtPSH.Trim().Replace('.', ',');
                Double.TryParse(txtPSH, NumberStyles.Float, numformat, out xPSH);
                txtPT = txtPT.Trim().Replace('.', ',');
                Double.TryParse(txtPT, NumberStyles.Float, numformat, out xPT);
                xO = Math.Abs(xPSH - xPT);
                txtOval = xO.ToString("F2").Replace('.', ',');
                return 0;
            }
            else
            {
                //ошибка ввода PT
                txtOval = "";
                return 1;

            }
        }
        else
        {
            if (XValidation(txtPT, 3, 2))
            {
                //ошибка ввода SH
                txtOval = "";
                return 2;
            }
            else
            {
                txtOval = "";
                //ничего не введено
                if (txtPSH == "" && txtPT == "") return 3;
                //Ошибка ввода SH и PT
                return 4;
            }
        }
    }

    //Функция производит проверку введённых данных и получает значение овальностей 
    protected bool InputedDataPreparation(out string strOvalPK, out string strOvalT, out string strOvalZK)
    {
        String Msg = "";

        //Проверка правильности ввода номера партии
        if (!XValidation(txtPartNumber.Text, 5, 0)) Msg += " Номер партии;";

        int res;

        //3//4//5
        switch (res = OneRun(txtDiamPKpSH.Text, txtDiamPKpT.Text, out strOvalPK))
        {
            case 1:
                Msg += " Диаметр по переднему концу трубы в плоскости тела;";
                break;
            case 2:
                Msg += " Диаметр по переднему концу трубы в плоскости шва;";
                break;
            case 3:
                //Не заданы диаметры по переднему концу трубы в двух плоскостях
                break;
            case 4:
                //ошибочно заданы диаметры трубы в обеих плоскостях
                Msg += " Диаметр по переднему концу трубы в плоскости тела;"
                    + " Диаметр по переднему концу трубы в плоскости шва;";
                break;
            default:
                //Овальность по переднему концу трубы вычислена
                break;
        }
        txtOvalPK.Text = strOvalPK;

        //6//7//8//
        switch (res = OneRun(txtDiamZKpSH.Text, txtDiamZKpT.Text, out strOvalZK))
        {
            case 1:
                Msg += " Диаметр по заднему концу трубы в плоскости тела;";
                break;
            case 2:
                Msg += " Диаметр по заднему концу трубы в плоскости шва;";
                break;
            case 3:
                //Не заданы диаметры по заднему концу трубы в двух плоскостях                                      
                break;
            case 4:
                //ошибочно заданы диаметры трубы в обеих плоскостях                                     
                Msg += " Диаметр по заднему концу трубы в плоскости тела;"
                    + " Диаметр по заднему концу трубы в плоскости шва;";
                break;
            default:
                //Овальность по заднему концу трубы вычислена
                break;
        }
        txtOvalZK.Text = strOvalZK;

        //9//10//11//
        switch (res = OneRun(txtDiamTpSH.Text, txtDiamTpT.Text, out strOvalT))
        {
            case 1:
                Msg += " Диаметр по телу трубы в плоскости тела;";
                break;
            case 2:
                Msg += " Диаметр по телу трубы в плоскости шва;";
                break;
            case 3:
                //Не заданы диаметры по телу трубы в двух плоскостях
                break;
            case 4:
                //ошибочно заданы диаметры трубы в обеих плоскостях
                Msg += " Диаметр по телу трубы в плоскости тела;"
                    + " Диаметр по телу трубы в плоскости шва;";
                break;
            default:
                //Овальность по телу трубы вычислена
                break;
        }
        txtOvalT.Text = strOvalT;

        if (txtTolSten.Text.Trim() == "" || txtTolSten2.Text.Trim() == "")
            Msg += "Толщина стенки (замер 1/замер 2);";

        //Проверка правильности ввода и заполнения полей
        if (Msg != "") { Master.AddErrorMessage("Неверно указано значение поля: " + Msg); return false; }

        bool bFieldsNotWrite =
            (txtDiamPKpSH.Text.Trim() == "") && (txtDiamPKpT.Text.Trim() == "") &&
            (txtDiamTpSH.Text.Trim() == "") && (txtDiamTpT.Text.Trim() == "") &&
            (txtDiamZKpSH.Text.Trim() == "") && (txtDiamZKpT.Text.Trim() == "") &&
            (txtDlina.Text.Trim() == "") &&
            (txtKosinaPTor.Text.Trim() == "") &&
            (txtKosinaZTor.Text.Trim() == "") &&
            (txtKrivizna1mT.Text.Trim() == "") &&
            (txtKriviznaVciaT.Text.Trim() == "") &&
            (txtOstatokVnutGrata.Text.Trim() == "") &&
            (txtOstatokNarujGrata.Text.Trim() == "") &&
            (txtSmeschKrom.Text.Trim() == "") &&
            (txtTolSten.Text.Trim() == "") &&
            (txtTolSten2.Text.Trim() == "");

        if (bFieldsNotWrite) { Master.AddErrorMessage("Не указано значение одного или нескольких полей"); return false; }
        return true;
    }


    //Получение списка уже существующих записей по геометрическим замерам для трубы с указанным номером
    protected int GetRecordInGeometryPipes_ORA(int pipeNumber)
    {
        int res = 0;
        tblExistsRecordInGeometry.Rows.Clear();
        TableRow row;
        TableCell cell;

        //Формирование заголовка таблицы
        row = new TableRow();
        cell = new TableCell();

        return res;
    }

    //Получение информации по трубе с номером pipeNumber из таблицы принятых труб
    protected int GetPipesBaseInfo_ORA(int pipeNumber, out String Errors)
    {
        Errors = "";
        int res = -1;
        try
        {
            //подключение к БД Oracle
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            using (OleDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT CASE
                                          WHEN term.trx_date > op.cutdate THEN term.topartnumber
                                          ELSE op.part_no
                                       END
                                          lot_number,
                                       gcs.pipe_diameter AS diameter,
                                       gcs.thickness AS thickness,
                                       gcs.steelmark AS steelmark,
                                       usc.LENGTH,
                                       op.coil_pipepart_year,
                                       op.coil_pipepart_no,
                                       op.coil_internalno
                                  FROM tesc3.optimal_pipes op
                                       LEFT JOIN tesc3.geometry_coils_sklad gcs
                                          ON     gcs.edit_state = 0
                                             AND gcs.coil_pipepart_year = op.coil_pipepart_year
                                             AND gcs.coil_pipepart_no = op.coil_pipepart_no
                                             AND gcs.coil_run_no = op.coil_internalno
                                       LEFT JOIN (SELECT uo.pipe_number, uo.LENGTH
                                                    FROM tesc3.usc_otdelka uo
                                                   WHERE uo.edit_state = 0 AND uo.pipe_number = ?
                                                         AND uo.rec_date =
                                                                (SELECT MAX (uo_.rec_date)
                                                                   FROM tesc3.usc_otdelka uo_
                                                                  WHERE uo_.edit_state = 0
                                                                        AND uo_.pipe_number = ? and UO_.WORKPLACE_ID in (13, 14))) usc
                                          ON usc.pipe_number = op.pipe_number
                                       LEFT JOIN (SELECT topt.pipenumber, topt.topartnumber, topt.trx_date
                                                    FROM tesc3.termo_otdel_pipes_tesc3 topt
                                                   WHERE topt.edit_state = 0
                                                         AND topt.pipenumber = ?
                                                         AND topt.trx_date =
                                                                (SELECT MAX (topt_.trx_date)
                                                                   FROM tesc3.termo_otdel_pipes_tesc3 topt_
                                                                  WHERE topt_.edit_state = 0
                                                                        AND topt_.pipenumber = ?)) term
                                          ON term.pipenumber = op.pipe_number
                                 WHERE op.pipe_number = ?";
                cmd.Parameters.AddWithValue("pipe_number_1", pipeNumber);
                cmd.Parameters.AddWithValue("pipe_number_2", pipeNumber);
                cmd.Parameters.AddWithValue("pipe_number_3", pipeNumber);
                cmd.Parameters.AddWithValue("pipe_number_4", pipeNumber);
                cmd.Parameters.AddWithValue("pipe_number_5", pipeNumber);
                using (OleDbDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        if (reader.Read())
                        {
                            basePipePartNumber = reader["lot_number"].ToString();
                            baseDiameter = reader["diameter"].ToString();
                            baseThickness = reader["thickness"].ToString();
                            baseSteelMark = reader["steelmark"].ToString();
                            baseLengthPipe = reader["length"].ToString();
                            baseCoilPipePartYear = reader["coil_pipepart_year"].ToString();
                            baseCoilPipePartNo = reader["coil_pipepart_no"].ToString();
                            baseCoilInternalNo = reader["coil_internalno"].ToString();
                            res = 0;
                        }
                        else
                        {
                            basePipePartNumber = "";
                            baseDiameter = "";
                            baseThickness = "";
                            baseSteelMark = "";
                            baseLengthPipe = "";
                            baseCoilPipePartYear = "";
                            baseCoilPipePartNo = "";
                            baseCoilInternalNo = "";
                            res = 1;
                        }
                        reader.Close();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Errors = ex.Message;
            res = -1;
        }
        return res;
    }


    //Добавление новой записи о геометрических замерах в Geometry_pipes_Insp (Oracle)
    protected bool AddNewRecordToGeometry_ORA()
    {
        string strOvalPK = "";
        string strOvalT = "";
        string strOvalZK = "";

        strOvalT = txtOvalT.Text;
        strOvalPK = txtOvalPK.Text;
        strOvalZK = txtOvalZK.Text;

        //Проверка заполнения обязательных полей  выбора перенесены на сторону клиента              
        OleDbConnection conn = Master.Connect.ORACLE_TESC3();
        using (OleDbCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText =
                @"INSERT INTO GEOMETRY_PIPES_INSP (            
              PIPE_NUMBER,
              LOT_NUMBER,
              COIL_PIPEPART_YEAR, 
              COIL_PIPEPART_NO,
              COIL_INTERNALNO,
              DIAMETER,
              THICKNESS, 
              STEELMARK, 
              LENGTH,
              DIAMETER_FORWARD_ON_WELD, 
              DIAMETER_FORWARD_ON_BARRE,
              DIAMETER_BARREL_ON_WELD,
              DIAMETER_BARREL_ON_BARREL,
              DIAMETER_BACK_ON_WELD,
              DIAMETER_BACK_ON_BARREL,
              LENGTH_OF_PIPE,
              KOSINA_REZA_FORWARD_TOR,
              KOSINA_REZA_BACK_TOR,
              CURVATURE_ON_1500MM,
              CURVATURE_ON_ALL_PIPE,
              OVALITY_ON_BACK,
              OVALITY_ON_FORWARD,
              OVALITY_ON_BARREL,
              REST_OF_INNER_FLASH,
              REST_OF_OUTER_FLASH,
              DISPLACEMENT_EDGES_PIPE,
              PIPE_WALL_THICKNESS,
              PIPE_WALL_THICKNESS_2,
              OUTER_DEFECT_ID,
              INNER_DEFECT_ID,
              END_DEFECT_ID,
              EMPLOYEE_NUMBER,
              WORKPLACE_ID,
              OPERATOR_ID,
              SHIFT_INDEX,
              ORIGINAL_ROWID,
              ROW_ID,
              EDIT_STATE,
              REC_DATE,
              DIAMETR_ON_FRONT_END,
              DIAMETR_ON_BACK_END,
              DIAMETR_ON_BODY,
              SLANT_EDGE_BCK_MIN, SLANT_EDGE_BCK_MAX, SLANT_EDGE_FWR_MIN, SLANT_EDGE_FWR_MAX, 
              WIDTH_TOR_RING_BCK_MIN, WIDTH_TOR_RING_BCK_MAX, WIDTH_TOR_RING_FWR_MIN, WIDTH_TOR_RING_FWR_MAX, CURVATURE_FRONT_END_1000MM,
                CURVATURE_FRONT_END_1500MM, CURVATURE_BACK_END_1500MM, CURVATURE_BACK_END_1000MM, CAMPAIGN_ID
            ) 
            VALUES
            (
              ?, ?, ?,          -- PIPE_NUMBER, LOT_NUMBER, COIL_PIPEPART_YEAR
              ?, ?, ?, ?, ?,    -- COIL_PIPEPART_NO, COIL_INTERNALNO, DIAMETER, THICKNESS, STEELMARK
              ?, ?, ?, ?, ?,    -- LENGTH, DIAMETER_FORWARD_ON_WELD, DIAMETER_FORWARD_ON_BARRE, DIAMETER_BARREL_ON_WELD, DIAMETER_BARREL_ON_BARREL
              ?, ?, ?, ?, ?,    -- DIAMETER_BACK_ON_WELD, DIAMETER_BACK_ON_BARREL, LENGTH_OF_PIPE, KOSINA_REZA_FORWARD_TOR, KOSINA_REZA_BACK_TOR
              ?, ?, ?, ?, ?,    -- CURVATURE_ON_1500MM, CURVATURE_ON_ALL_PIPE, OVALITY_ON_BACK, OVALITY_ON_FORWARD, OVALITY_ON_BARREL
              ?, ?, ?, ?, ?,    -- REST_OF_INNER_FLASH, REST_OF_OUTER_FLASH, DISPLACEMENT_EDGES_PIPE, PIPE_WALL_THICKNESS, PIPE_WALL_THICKNESS_2
              ?, ?, ?,          -- OUTER_DEFECT_ID, INNER_DEFECT_ID, END_DEFECT_ID
              ?, ?, ?, ?, ?,    -- EMPLOYEE_NUMBER, WORKPLACE_ID, OPERATOR_ID, SHIFT_INDEX, ORIGINAL_ROWID,
              (to_char(Sysdate, 'dd.MM.yyyy hh24:mi:ss_') ||(?)),  -- ROW_ID
              0,                -- EDIT_STATE
              SYSDATE,          -- REC_DATE
              ?, ?, ?,           -- DIAMETR_ON_FRONT_END, DIAMETR_ON_BACK_END, DIAMETR_ON_BODY
              ?, ?, ?, ?, ?, ?, ?, ?, -- min&max SlantEdge&WidthRing
              ?, ?, ?, ?, ?             -- min&max CURVATURE and campaign_id
            ) ";


            //базовые характеристики
            cmd.Parameters.AddWithValue("PIPE_NUMBER", Checking.GetDbType(basePipeNumber));
            if (txtPartNumber.Text.Trim() != "") cmd.Parameters.AddWithValue("LOT_NUMBER", Checking.GetDbType(txtPartNumber.Text));
            else cmd.Parameters.AddWithValue("LOT_NUMBER", Checking.GetDbType(basePipePartNumber));

            cmd.Parameters.AddWithValue("COIL_PIPEPART_YEAR", Checking.GetDbType(baseCoilPipePartYear));
            cmd.Parameters.AddWithValue("COIL_PIPEPART_NO", Checking.GetDbType(baseCoilPipePartNo));
            cmd.Parameters.AddWithValue("COIL_INTERNALNO", Checking.GetDbType(baseCoilInternalNo));

            cmd.Parameters.AddWithValue("DIAMETER", Checking.GetDbType(baseDiameter));
            cmd.Parameters.AddWithValue("THICKNESS", Checking.GetDbType(baseThickness));
            cmd.Parameters.AddWithValue("STEELMARK", Convert.ToString(baseSteelMark));
            cmd.Parameters.AddWithValue("LENGTH", Checking.GetDbType(baseLengthPipe));

            //геометрические параметры
            //Диаметр по переднему концу трубы в плоскости шва
            cmd.Parameters.AddWithValue("DIAMETER_FORWARD_ON_WELD", Checking.GetDbType(txtDiamPKpSH.Text));
            //Диаметр по переднему концу трубы в плоскости тела
            cmd.Parameters.AddWithValue("DIAMETER_FORWARD_ON_BARRE", Checking.GetDbType(txtDiamPKpT.Text));
            //Диаметр по телу трубы в плоскости шва
            cmd.Parameters.AddWithValue("DIAMETER_BARREL_ON_WELD", Checking.GetDbType(txtDiamTpSH.Text));
            //Диаметр по телу трубы в плоскости тела
            cmd.Parameters.AddWithValue("DIAMETER_BARREL_ON_BARREL", Checking.GetDbType(txtDiamTpT.Text));
            //Диаметр по заднему концу трубы в плоскости шва
            cmd.Parameters.AddWithValue("DIAMETER_BACK_ON_WELD", Checking.GetDbType(txtDiamZKpSH.Text));
            //Диаметр по заднему концу трубы в плоскости тела
            cmd.Parameters.AddWithValue("DIAMETER_BACK_ON_BARREL", Checking.GetDbType(txtDiamZKpT.Text));

            //Длина трубы
            cmd.Parameters.AddWithValue("LENGTH_OF_PIPE", Checking.GetDbType(txtDlina.Text));

            //Косина реза передний торец 
            cmd.Parameters.AddWithValue("KOSINA_REZA_FORWARD_TOR", Checking.GetDbType(txtKosinaPTor.Text));
            //Косина реза задний торец 
            cmd.Parameters.AddWithValue("KOSINA_REZA_BACK_TOR", Checking.GetDbType(txtKosinaZTor.Text));

            //Кривизна на 1500 мм трубы 
            cmd.Parameters.AddWithValue("CURVATURE_ON_1500MM", Checking.GetDbType(txtKrivizna1mT.Text));
            //Кривизна трубы по всей длинне 
            cmd.Parameters.AddWithValue("CURVATURE_ON_ALL_PIPE", Checking.GetDbType(txtKriviznaVciaT.Text));

            //Овальность по заднему концу трубы
            cmd.Parameters.AddWithValue("OVALITY_ON_BACK", Checking.GetDbType(strOvalZK));
            //Овальность по переднему концу трубы
            cmd.Parameters.AddWithValue("OVALITY_ON_FORWARD", Checking.GetDbType(strOvalPK));
            //Овальность по телу трубы
            cmd.Parameters.AddWithValue("OVALITY_ON_BARREL", Checking.GetDbType(strOvalT));

            //Остаток внутреннего грата 
            cmd.Parameters.AddWithValue("REST_OF_INNER_FLASH", Checking.GetDbType(txtOstatokVnutGrata.Text));
            //Остаток наружного грата 
            cmd.Parameters.AddWithValue("REST_OF_OUTER_FLASH", Checking.GetDbType(txtOstatokNarujGrata.Text));

            //Смещение кромок трубы 
            cmd.Parameters.AddWithValue("DISPLACEMENT_EDGES_PIPE", Checking.GetDbType(txtSmeschKrom.Text));

            //Толщина стенки трубы 
            cmd.Parameters.AddWithValue("PIPE_WALL_THICKNESS", Checking.GetDbType(txtTolSten.Text));
            cmd.Parameters.AddWithValue("PIPE_WALL_THICKNESS_2", Checking.GetDbType(txtTolSten2.Text));

            //состояние внутренней/наружной поверхности, заусенцы на торцах
            cmd.Parameters.AddWithValue("OUTER_DEFECT_ID", Checking.GetDbType((ddlOuterDefect.SelectedItem.Value != "Уд") ? ddlOuterDefect.SelectedItem.Value : ""));
            cmd.Parameters.AddWithValue("INNER_DEFECT_ID", Checking.GetDbType((ddlInnerDefect.SelectedItem.Value != "Уд") ? ddlInnerDefect.SelectedItem.Value : ""));
            cmd.Parameters.AddWithValue("END_DEFECT_ID", (ddlEndDefect.SelectedItem.Value != "Уд") ? ddlEndDefect.SelectedItem.Value : "");

            //служебные данные
            cmd.Parameters.AddWithValue("EMPLOYEE_NUMBER", Authentification.User.TabNumber);
            cmd.Parameters.AddWithValue("WORKPLACE_ID", Checking.GetDbType(""));
            cmd.Parameters.AddWithValue("OPERATOR_ID", Convert.ToString(Authentification.User.UserName));
            cmd.Parameters.AddWithValue("SHIFT_INDEX", Authentification.Shift);
            cmd.Parameters.AddWithValue("ORIGINAL_ROWID", Convert.ToString(""));
            cmd.Parameters.AddWithValue("ROW_ID", Convert.ToString(basePipeNumber));

            //диаметр по переднему концу
            cmd.Parameters.AddWithValue("DIAMETR_ON_FRONT_END", Checking.GetDbType(txtDiametrOnFrontEnd.Text));
            //диаметр по заднему концу
            cmd.Parameters.AddWithValue("DIAMETR_ON_BACK_END", Checking.GetDbType(txtDiametrOnBackEnd.Text));
            //диаметр по телу
            cmd.Parameters.AddWithValue("DIAMETR_ON_BODY", Checking.GetDbType(txtDiametrOnBody.Text));

            // PAV 2018-04-03
            cmd.Parameters.AddWithValue("SLANT_EDGE_BCK_MIN", Checking.GetDbType(txtYgolSkosaFaskiZTorMin.Text));
            cmd.Parameters.AddWithValue("SLANT_EDGE_BCK_MAX", Checking.GetDbType(txtYgolSkosaFaskiZTorMax.Text));
            cmd.Parameters.AddWithValue("SLANT_EDGE_FWR_MIN", Checking.GetDbType(txtYgolSkosaFaskiPTorMin.Text));
            cmd.Parameters.AddWithValue("SLANT_EDGE_FWR_MAX", Checking.GetDbType(txtYgolSkosaFaskiPTorMax.Text));
            cmd.Parameters.AddWithValue("WIDTH_TOR_RING_BCK_MIN", Checking.GetDbType(txtShirinaTorKolZTorMin.Text));
            cmd.Parameters.AddWithValue("WIDTH_TOR_RING_BCK_MAX", Checking.GetDbType(txtShirinaTorKolZTorMax.Text));
            cmd.Parameters.AddWithValue("WIDTH_TOR_RING_FWR_MIN", Checking.GetDbType(txtShirinaTorKolPTorMin.Text));
            cmd.Parameters.AddWithValue("WIDTH_TOR_RING_FWR_MAX", Checking.GetDbType(txtShirinaTorKolPTorMax.Text));

            // RSA 2018-04-20
            cmd.Parameters.AddWithValue("CURVATURE_FRONT_END_1000MM", Checking.GetDbType(txtCURVATURE_FRONT_END_1000MM.Text));
            cmd.Parameters.AddWithValue("CURVATURE_FRONT_END_1500MM", Checking.GetDbType(txtCURVATURE_FRONT_END_1500MM.Text));
            cmd.Parameters.AddWithValue("CURVATURE_BACK_END_1500MM", Checking.GetDbType(CURVATURE_BACK_END_1000MM.Text));
            cmd.Parameters.AddWithValue("CURVATURE_BACK_END_1000MM", Checking.GetDbType(CURVATURE_BACK_END_1500MM.Text));
            cmd.Parameters.AddWithValue("CAMPAIGN_ID", ddlCampaign.SelectedItem.Value);

            cmd.ExecuteNonQuery();
        }

        return true;
    }


    protected void txtOvalPK_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (txtDiamPKpT.Text != "" || txtDiamPKpSH.Text != "")
        {
            double v1 = 0;
            double v2 = 0;
            double.TryParse(txtDiamPKpT.Text, out v1);
            double.TryParse(txtDiamPKpSH.Text, out v2);
            txtOvalPK.Text = Math.Round(Math.Abs(v1 - v2), 2).ToString().Replace('.', ',');
        }
        else
        {
            txtOvalPK.Text = "";
        }
    }


    protected void txtOvalZK_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (txtDiamZKpT.Text != "" || txtDiamZKpSH.Text != "")
        {
            double v1 = 0;
            double v2 = 0;
            double.TryParse(txtDiamZKpT.Text, out v1);
            double.TryParse(txtDiamZKpSH.Text, out v2);
            txtOvalZK.Text = Math.Round(Math.Abs(v1 - v2), 2).ToString().Replace('.', ',');
        }
        else
        {
            txtOvalZK.Text = "";
        }
    }


    protected void txtOvalT_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (txtDiamTpT.Text != "" || txtDiamTpSH.Text != "")
        {
            double v1 = 0;
            double v2 = 0;
            double.TryParse(txtDiamTpT.Text, out v1);
            double.TryParse(txtDiamTpSH.Text, out v2);
            txtOvalT.Text = Math.Round(Math.Abs(v1 - v2), 2).ToString().Replace('.', ',');
        }
        else
        {
            txtOvalT.Text = "";
        }
    }


    //Инициализация при загрузке страницы
    protected void Page_Load(object sender, EventArgs e)
    {
        Culture = "Ru-RU";

        Master.EnableSaveScrollPositions(this);
        Master.ClearErrorMessages();

        Authentification.CanAnyAccess(WORKPLACE_ID);

        if (!IsPostBack)
        {
            FillDropDownLists();
        }

        if (!IsPostBack)
        {

            //получение номера трубы из строки запроса
            try
            {
                if (Session["TargetForGeom"] != null)
                    Target = Session["TargetForGeom"].ToString();
                if (Session["PipeYearForGeom"] != null)
                    PipeYearGeom = Convert.ToInt32(Session["PipeYearForGeom"]);
                if (Session["PipeNumberForGeom"] != null)
                    PipeNumberGeom = Convert.ToInt32(Session["PipeNumberForGeom"]);
                if (Session["PipeCheckForGeom"] != null)
                    PipeCheckGeom = Convert.ToInt32(Session["PipeCheckForGeom"]);
                if (Session["WorkPlaceForGeom"] != null)
                    WorkplaceId = Convert.ToInt32(Session["WorkPlaceForGeom"]);
                if (Session["CampaignLineForGeom"] != null)
                {
                    Campaign = Session["CampaignLineForGeom"].ToString();
                    RebuildCampaignList();
                }

                if (Target == "GeomInsp") btnInputGeometry_Click(sender, e);
                if (Target == "")
                {
                    String PipeYear = Request["PIPE_YEAR"].Trim();
                    String PipeNumber = Request["PIPE_NUMBER"].Trim();
                    String CheckChar = Request["CHECK_CHAR"].Trim();
                    if ((PipeNumber != "") | (PipeYear != "") | (CheckChar != ""))
                    {
                        txbPipeNumber.Text = PipeNumber;
                        txbYear.Text = PipeYear;
                        txbCheck.Text = CheckChar;
                    }
                }
            }
            catch { }

        }

        //Получение текущего года и запись его в поле ввода
        if (txbYear.Text == "") txbYear.Text = DateTime.Now.Year.ToString().Substring(2, 2);
    }


    /// <summary>
    /// Заполнение выпадающих списков значений
    /// </summary>
    private void FillDropDownLists()
    {
        OleDbConnection conn = Master.Connect.ORACLE_TESC3();

        //заполнение списка дефектов наружной поверхности
        ddlOuterDefect.Items.Clear();
        ddlOuterDefect.Items.Add("");
        ddlOuterDefect.Items.Add("Уд");
        using (OleDbCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = "select ID, DEFECT_NAME from SPR_DEFECT where DEFECT_AREA in ('На стане', 'По металлу') order by DEFECT_NAME";
            using (OleDbDataReader readerDefects = cmd.ExecuteReader())
            {
                if (readerDefects.HasRows)
                {
                    while (readerDefects.Read())
                        ddlOuterDefect.Items.Add(new ListItem(readerDefects["DEFECT_NAME"].ToString(), readerDefects["ID"].ToString()));
                    readerDefects.Close();
                }
            }
        }

        //заполнение списка дефектов внутренней поверхности
        ddlInnerDefect.Items.Clear();
        ddlInnerDefect.Items.Add("");
        ddlInnerDefect.Items.Add("Уд");
        using (OleDbCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = "select ID, DEFECT_NAME from SPR_DEFECT where DEFECT_AREA in ('На стане', 'По металлу') order by DEFECT_NAME";
            using (OleDbDataReader readerDefects = cmd.ExecuteReader())
            {
                if (readerDefects.HasRows)
                {
                    while (readerDefects.Read())
                        ddlInnerDefect.Items.Add(new ListItem(readerDefects["DEFECT_NAME"].ToString(), readerDefects["ID"].ToString()));
                    readerDefects.Close();
                }
            }
        }

        //заполнение списка дефектов "заусенцы на торцах"
        ddlEndDefect.Items.Clear();
        ddlEndDefect.Items.Add("");
        ddlEndDefect.Items.Add("Уд");
        using (OleDbCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = "select ID, DEFECT_NAME from SPR_DEFECT where DEFECT_AREA in ('На стане') order by DEFECT_NAME";
            using (OleDbDataReader readerDefects = cmd.ExecuteReader())
            {
                if (readerDefects.HasRows)
                {
                    while (readerDefects.Read())
                        ddlEndDefect.Items.Add(new ListItem(readerDefects["DEFECT_NAME"].ToString(), readerDefects["ID"].ToString()));
                    readerDefects.Close();
                }
            }
        }
        RebuildCampaignList();
    }


    //Обработчик нажатия кнопки подткерждения выбора трубы
    protected void btnOk_Click(object sender, EventArgs e)
    {
        txtPipeNumberDubl.Text = basePipeNumber;
        txtPartNumber.Text = basePipePartNumber;
        mvViews.SetActiveView(vInputGeometry);
    }


    //Обработчик нажатия кнопки Назад
    protected void btnBack_Click(object sender, EventArgs e)
    {
        mvViews.SetActiveView(vInputCondition);
        foreach (Control ctrl in vInputGeometry.Controls)
        {
            if (ctrl is TextBox) (ctrl as TextBox).Text = "";
        }
    }


    //Обработчик нажатия кнопки Сохранить
    protected void btnSave_Click(object sender, EventArgs e)
    {
        bool OperationsComplit = false;
        try
        {
            OperationsComplit = AddNewRecordToGeometry_ORA();
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Данные измерения геометрических параметров не сохранены", ex);
        }
        finally
        {
            if (OperationsComplit)
            {
                foreach (Control ctrl in vInputGeometry.Controls)
                {
                    if (ctrl is TextBox) (ctrl as TextBox).Text = "";
                }
                txbYear.Text = DateTime.Now.Year.ToString().Substring(2, 2);
                mvViews.SetActiveView(vContenueAfterSave);
            }
        }
    }


    //обработчик запроса подсказки контрольной цифры
    protected void lbtHelpCheck_Click(object sender, EventArgs e)
    {
        int PipeNumber;
        string NumSMS, errmsg;
        try
        {
            if (Checking.GetSMSNum(txbYear.Text, txbPipeNumber.Text, out NumSMS, out errmsg, out PipeNumber))
            {
                basePipeNumber = NumSMS;
                lblNumtrub.Text = " " + NumSMS;
                mvViews.SetActiveView(vGetChekingNum);
                txbLogin.Text = "";
                txbPassword.Text = "";
            }
            else Master.AddErrorMessage("Неверно указано значение поля: " + errmsg);
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage(ex.Message);
        }
    }


    //проверка именя пользователя/пароля
    protected void btnLogin_Click(object sender, EventArgs e)
    {
        if (txbLogin.Text != "" && txbPassword.Text == "12345678")
        {
            txbCheck.Text = Checking.Check_Class(basePipeNumber).ToString();
            txbLogin.Text = "";
            txbPassword.Text = "";
            mvViews.SetActiveView(vInputCondition);
        }
        else Master.AddErrorMessage("Неверное имя пользователя/пароль");
    }


    //возврат к панели поиска трубы из панели ввола логина/пароля для запроса подсказки
    protected void btnCansel_Click(object sender, EventArgs e)
    {
        mvViews.SetActiveView(vInputCondition);
    }


    //Вывод сообщения об успешном добавлении данных в Batch
    protected void btnContinue_Click(object sender, EventArgs e)
    {
        txbPipeNumber.Text = "";
        txbCheck.Text = "";
        mvViews.SetActiveView(vInputCondition);
    }


    //Переход не страницу для добавления трубы
    protected void btnGoToAddNewPipe_Click(object sender, EventArgs e)
    {
        String url = "AddPipe.aspx?";

        String param = "&Yaer=" + txbYear.Text
                + "&PaperNum=" + txbPipeNumber.Text
                + "&ChekNum=" + txbCheck.Text
                + "&BackUrl=GeometryInsp.aspx";
        HttpContext.Current.Response.Redirect(url + param);
    }


    //Обработчик нажатия кнопки "Ввести данные"
    protected void btnInputGeometry_Click(object sender, EventArgs e)
    {
        pnlExistingRecsInGeometry.Visible = false;
        //проверка полей ввода и определение NumSMS
        int PipeNumber;
        int Check;
        string NumSMS, errmsg;
        bool AnyErr = true;

        if (PipeYearGeom != 0) txbYear.Text = PipeYearGeom.ToString();
        if (PipeNumberGeom != 0) txbPipeNumber.Text = PipeNumberGeom.ToString();
        if (PipeCheckGeom != -1) txbCheck.Text = PipeCheckGeom.ToString();
        if (WorkplaceId != 0) ddlWorkPlace.SelectedItem.Value = WorkplaceId.ToString();
        if (Campaign != "")
        {
            ddlCampaign.SelectedItem.Value = Campaign;
            ddlCampaign_SelectedIndexChanged(sender, e);
        }

        if (Checking.GetSMSNum(txbYear.Text, txbPipeNumber.Text, out NumSMS, out errmsg, out PipeNumber))
        {
            basePipeNumber = NumSMS;
            AnyErr = false;
        }

        if (!Int32.TryParse(txbCheck.Text, out Check)) { errmsg += " контрольноя цифра; \n"; AnyErr = true; }
        if (AnyErr)
        {
            Master.AddErrorMessage("Неверно указано значение поля: " + errmsg);
            return;
        }
        //----------Проверка соответствия контрольной цифры и номера трубы-----------
        if (PipeNumber == 0 || (Checking.Check_Class(NumSMS)) != Check)
        {
            Master.AddErrorMessage("Неверно указан номер или контрольная цифра");
            return;
        }

        String strErrors;
        int res = GetPipesBaseInfo_ORA(Convert.ToInt32(basePipeNumber), out strErrors);
        switch (res)
        {
            case -1:
                {
                    Master.AddErrorMessage("Ошибка: " + strErrors);
                    return;
                }
        }
        mvViews.SetActiveView(vInputGeometry);
        txtPipeNumberDubl.Text = basePipeNumber;
        txtPartNumber.Text = basePipePartNumber;

    }


    //заполнение сортамента и параметров заказа при выборе кампании
    protected void ddlCampaign_SelectedIndexChanged(object sender, EventArgs e)
    {
        try
        {
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            using (OleDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"select sm.s_diam, sm.s_thickness_pipe, sn.id, sn.ntd_name, sn.ntd_group
                                  from campaigns cp
                                       join oracle.z_spr_materials sm
                                          on inventory_code = sm.matnr
                                       left join spr_ntd sn
                                          on (sm.d_ntdqm = sn.ntd_name and nvl(sm.d_group_pipes, '#') = nvl(sn.ntd_group, '#'))
                                 where cp.campaign_line_id = ? and cp.edit_state = 0";

                if (Campaign != "") cmd.Parameters.AddWithValue("CAMPAIGN_LINE_ID", Campaign);
                else cmd.Parameters.AddWithValue("CAMPAIGN_LINE_ID", (sender as DropDownList).SelectedItem.Value);
                using (OleDbDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        if (reader.Read())
                        {
                            baseNtdId = Convert.ToInt16(reader["id"].ToString());
                            baseDiameter = reader["s_diam"].ToString();
                            baseThickness = reader["s_thickness_pipe"].ToString();
                        }

                        reader.Close();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка при получении данных из списка кампаний: " + ex.Message, ex);
        }
    }


    //построение списка кампаний
    private void RebuildCampaignList()
    {
        try
        {
            //запоминание выбранного элемента списка
            String OldCampaign = Campaign != "" ? Campaign : ddlCampaign.SelectedItem.Value;

            //очистка старых значений
            ddlCampaign.Items.Clear();
            ddlCampaign.Items.Add(new ListItem(""));

            //подключение к БД            
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            using (OleDbCommand cmd = conn.CreateCommand())
            {
                //выборка информации по кампаниям
                cmd.CommandText = @"SELECT DISTINCT campaign_line_id,
                                                      rec_date,
                                                      v_t3.diameter,
                                                      v_t3.s_size1,
                                                      v_t3.s_size2,
                                                      v_t3.thickness,
                                                      gost,
                                                      grup,
                                                      stal,
                                                      campaign_date,
                                                      order_line,
                                                      order_header,
                                                      inventory_code,
                                                      additional_text,
                                                      inspection,
                                                      sm.d_ur_isp
                                        FROM campaigns
                                             LEFT JOIN oracle.v_t3_pipe_items v_t3 ON (inventory_code = v_t3.nomer)
                                             JOIN oracle.z_spr_materials sm ON inventory_code = sm.matnr
                                       WHERE (end_date IS NULL) AND (visible = 1) AND edit_state = 0
                                    ORDER BY rec_date";
                using (OleDbDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            String gost = reader["GOST"].ToString();
                            String group = reader["GRUP"].ToString();
                            if (group != "") gost += " гр. " + group;
                            String date = Convert.ToDateTime(reader["CAMPAIGN_DATE"]).ToString("dd.MM.yy");

                            String sort = "";
                            if (reader["DIAMETER"].ToString() != "") sort = reader["DIAMETER"] + "x" + reader["THICKNESS"]; //типоразмер обычной трубы
                            else sort = reader["S_SIZE1"] + "x" + reader["S_SIZE2"] + "x" + reader["THICKNESS"];            //типоразмер профильной трубы 

                            String steel = reader["STAL"].ToString();
                            String nom = reader["INVENTORY_CODE"].ToString().TrimStart(new char[] { '0' });
                            String zakaz = reader["ORDER_HEADER"] + "/" + reader["ORDER_LINE"];
                            String additional = reader["ADDITIONAL_TEXT"].ToString();
                            String inspection = reader["INSPECTION"].ToString();

                            String ur_isp = reader["D_UR_ISP"].ToString();

                            String txt = date.PadRight(10, '_')
                                       + sort.PadRight(13, '_')
                                       + steel.PadRight(17, '_')
                                       + nom + "__"
                                       + zakaz + "__"
                                       + gost + "__"
                                       + ur_isp + "__"
                                       + additional + "__"
                                       + inspection.ToUpper();

                            char[] trimChars = { '_' };
                            String lineID = reader["CAMPAIGN_LINE_ID"].ToString();
                            ddlCampaign.Items.Add(new ListItem(txt.Trim(trimChars), lineID));
                            ddlCampaign.Items.Add("");
                        }
                        reader.Close();
                    }
                }
            }

            //восстановление выбранного элемента списка кампаний
            ListItem item = ddlCampaign.Items.FindByValue(OldCampaign);
            if (item != null) ddlCampaign.SelectedIndex = ddlCampaign.Items.IndexOf(item);
        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка получения списка кампаний", ex);
        }
    }

    protected void ddlWorkPlace_SelectedIndexChanged(object sender, EventArgs e)
    {
        try
        {

        }
        catch (Exception ex)
        {
            Master.AddErrorMessage("Ошибка заполнения списка принтеров", ex);
        }
    }

    protected void btnLoad_Click(object sender, EventArgs e)
    {
        int pipeNumber = 0;
        int.TryParse(txtPipeNumberDubl.Text, out pipeNumber);
        int Campaing = 0;
        lblErrorFillField.Text = "";
        if (!int.TryParse(ddlCampaign.SelectedItem.Value, out Campaing))
        {
            lblErrorFillField.Text = "Выберите кампанию";
            return;
        }


        DateTime LastInLineTime = Checking.GetHistoryLastInOrOutPosition(pipeNumber, true);
        DateTime NawPositionTime;
        CheckPipe pipe = new CheckPipe(pipeNumber, Campaing);
        DateTime.TryParse(pipe.GetMrtValue("MEASURE_TIME").ToString(), out NawPositionTime);
        if (NawPositionTime < LastInLineTime)
        {
            lblErrorFillField.Text = "Отсутствуют актуальные данные по последнему входу на линию";
            return;
        }

        #region Диаметр по заднему концу, мм
        txtDiametrOnBackEnd.Text = "";
        if (pipe.TestMrtGeom("OUTDIAMCASING100_S") && pipe.TestMrtGeom("OUTDIAMCASING200_S"))
            txtDiametrOnBackEnd.Text = pipe.GetMrtDValue("OUTDIAMCASING100_S");
        if (pipe.TestMrtGeom("OUTDIAM100_S") && pipe.TestMrtGeom("OUTDIAM200_S"))
            txtDiametrOnBackEnd.Text = pipe.GetMrtDValue("OUTDIAM100_S");
        #endregion Диаметр по заднему концу, мм

        #region Диаметр по переднему концу, мм
        txtDiametrOnFrontEnd.Text = "";
        if (pipe.TestMrtGeom("OUTDIAMCASING100_U") && pipe.TestMrtGeom("OUTDIAMCASING200_U"))
            txtDiametrOnFrontEnd.Text = pipe.GetMrtDValue("OUTDIAMCASING100_U");
        if (pipe.TestMrtGeom("OUTDIAM100_U") && pipe.TestMrtGeom("OUTDIAM200_U"))
            txtDiametrOnFrontEnd.Text = pipe.GetMrtDValue("OUTDIAM100_U");
        #endregion Диаметр по переднему концу, мм

        #region Диаметр по телу, мм
        txtDiametrOnBody.Text = "";
        if (pipe.TestMrtGeom("OUTDIAM_T"))
            txtDiametrOnBody.Text = pipe.GetMrtDValue("OUTDIAM_T");
        if (pipe.TestMrtGeom("OUTDIAMCASING_T"))
            txtDiametrOnBody.Text = pipe.GetMrtDValue("OUTDIAMCASING_T");
        #endregion Диаметр по телу, мм

        #region Овальность по заднему концу, мм
        txtOvalZK.Text = "";
        if (pipe.TestMrtGeom("OUTVALCASING100_S") && pipe.TestMrtGeom("OUTVALCASING200_S"))
            txtOvalZK.Text = pipe.GetMrtDValue("OUTVALCASING100_S");
        if (pipe.TestMrtGeom("OUTVAL100_S") && pipe.TestMrtGeom("OUTVAL200_S"))
            txtOvalZK.Text = pipe.GetMrtDValue("OUTVAL100_S");
        #endregion Овальность по заднему концу, мм

        #region Овальность по переднему концу, мм
        txtOvalPK.Text = "";
        if (pipe.TestMrtGeom("OUTVALCASING100_U") && pipe.TestMrtGeom("OUTVALCASING200_U"))
            txtOvalPK.Text = pipe.GetMrtDValue("OUTVALCASING100_U");
        if (pipe.TestMrtGeom("OUTVAL100_U") && pipe.TestMrtGeom("OUTVAL200_U"))
            txtOvalPK.Text = pipe.GetMrtDValue("OUTVAL100_U");
        #endregion Овальность по переднему концу, мм

        #region Овальность по телу трубы, мм
        txtOvalT.Text = "";
        if (pipe.TestMrtGeom("OVALCASING"))
            txtOvalT.Text = pipe.GetMrtDValue("OVALCASING");
        if (pipe.TestMrtGeom("SECOVAL"))
            txtOvalT.Text = pipe.GetMrtDValue("SECOVAL");
        #endregion Овальность по телу трубы, мм

        #region Косина реза
        txtKosinaPTor.Text = pipe.TestMrtGeom("SLANTLEAD") ? pipe.GetMrtDValue("SLANTLEAD") : "";
        txtKosinaZTor.Text = pipe.TestMrtGeom("SLANTBACK") ? pipe.GetMrtDValue("SLANTBACK") : "";
        #endregion Косина реза

        #region Кривизна
        txtKriviznaVciaT.Text = pipe.TestMrtGeom("COMMCURVMAX") ? pipe.GetMrtDValue("COMMCURVMAX") : ""; //кривизна общая
        txtKrivizna1mT.Text = pipe.TestMrtGeom("LOCALCURVMAX") ? pipe.GetMrtDValue("LOCALCURVMAX") : ""; //кривизна на одном метре
        #endregion Кривизна

        #region Толщина стенки
        txtTolSten.Text = pipe.TestMrtGeom("THICKLEAD") ? pipe.GetMrtDValue("THICKLEAD") : ""; //толщина начала
        txtTolSten2.Text = pipe.TestMrtGeom("THICKBACK") ? pipe.GetMrtDValue("THICKBACK") : ""; //толщина конца
        #endregion Толщина стенки

        #region Угол фаски
        txtYgolSkosaFaskiZTorMin.Text = pipe.TestMrtGeom("MINBEVANG1BACK") ? pipe.GetMrtDValue("MINBEVANG1BACK") : ""; //угол фаски миним зад
        txtYgolSkosaFaskiZTorMax.Text = pipe.TestMrtGeom("MAXBEVANG1BACK") ? pipe.GetMrtDValue("MAXBEVANG1BACK") : ""; //угол фаски максимальный зад
        txtYgolSkosaFaskiPTorMin.Text = pipe.TestMrtGeom("MINBEVANG1LEAD") ? pipe.GetMrtDValue("MINBEVANG1LEAD") : ""; //угол фаски миним перед
        txtYgolSkosaFaskiPTorMax.Text = pipe.TestMrtGeom("MAXBEVANG1LEAD") ? pipe.GetMrtDValue("MAXBEVANG1LEAD") : ""; //угол фаски максимальный перед
        #endregion Угол фаски

        #region Ширина торцевого кольца
        txtShirinaTorKolZTorMin.Text = pipe.TestMrtGeom("MINTRUNCBACK") ? pipe.GetMrtDValue("MINTRUNCBACK") : ""; //ширина торцевого кольца задний торец минимальный
        txtShirinaTorKolZTorMax.Text = pipe.TestMrtGeom("MAXTRUNCBACK") ? pipe.GetMrtDValue("MAXTRUNCBACK") : ""; //ширина торцевого кольца задний торец максимальный, мм
        txtShirinaTorKolPTorMin.Text = pipe.TestMrtGeom("MINTRUNCLEAD") ? pipe.GetMrtDValue("MINTRUNCLEAD") : ""; //ширина торцевого кольца передний торец минимальный, мм 
        txtShirinaTorKolPTorMax.Text = pipe.TestMrtGeom("MAXTRUNCLEAD") ? pipe.GetMrtDValue("MAXTRUNCLEAD") : ""; //ширина торцевого кольца передний торец максимальный, мм 
        #endregion Ширина торцевого кольца

        #region Кривизна концевая
        txtCURVATURE_FRONT_END_1000MM.Text = pipe.TestMrtGeom("ENDCURVLEAD2") ? pipe.GetMrtDValue("ENDCURVLEAD2") : ""; //кривизна концевая переднего конца трубы на 1 м, в 
        txtCURVATURE_FRONT_END_1500MM.Text = pipe.TestMrtGeom("ENDCURVLEAD1") ? pipe.GetMrtDValue("ENDCURVLEAD1") : ""; //кривизна концевая переднего конца трубы на 1,5 м, в мм 
        CURVATURE_BACK_END_1000MM.Text = pipe.TestMrtGeom("ENDCURVBACK2") ? pipe.GetMrtDValue("ENDCURVBACK2") : ""; //кривизна концевая заднего конца трубы на 1 м, в мм 
        CURVATURE_BACK_END_1500MM.Text = pipe.TestMrtGeom("ENDCURVBACK1") ? pipe.GetMrtDValue("ENDCURVBACK1") : ""; //кривизна концевая заднего конца трубы на 1,5 м, в мм 
        #endregion Кривизна концевая

        //Получаем длинну трубы
        try
        {
            //получение рабочего места, где труба была последней
            OleDbConnection conn = Master.Connect.ORACLE_TESC3();
            OleDbCommand cmd = conn.CreateCommand();

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
            cmd.Parameters.AddWithValue("pipe_number", pipeNumber);
            using (OleDbDataReader rdr = cmd.ExecuteReader())
            {
                if (rdr.HasRows)
                {
                    if (rdr.Read())
                    {
                        //заполнение диаметра, марки стали, стенки и других параметров со стана для участка ремонта
                        if (WorkplaceId == 7)
                            txtDlina.Text = rdr["PIPELENGTH"].ToString();
                    }
                    rdr.Close();
                }
            }
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
            cmd.Parameters.AddWithValue("PIPE_NUMBER", pipeNumber);
            using (OleDbDataReader rdr = cmd.ExecuteReader())
            {
                if (rdr.HasRows)
                {
                    while (rdr.Read())
                    {
                        //отображение последней длины трубы после обрези на участке ремонта
                        if (WorkplaceId == 7)
                            txtDlina.Text = rdr["LENGTH"].ToString();
                    }
                    rdr.Close();
                }
            }
            cmd.Dispose();

            if (WorkplaceId != -1)
            {
                if (WorkplaceId >= 1 && WorkplaceId <= 6)
                {
                    //длина трубы с УЗК шва линии отделки 1 и 2 
                    //(для линии 1 (workplace_id: 1, 2, 3) берется usc_otdelka.workplace_id=13, для линии 2 (workplace_id: 4, 5, 6) берется usc_otdelka.workplace_id=14)
                    cmd = Master.Connect.ORACLE_TESC3().CreateCommand();
                    cmd.CommandText = @"select length
                                        from usc_otdelka
                                        where edit_state=0 and workplace_id=" + (WorkplaceId < 4 ? "13" : "14") + @"
                                        and pipe_number=?
                                        order by rec_date desc";
                    cmd.Parameters.AddWithValue("PIPE_NUMBER", pipeNumber);
                    using (OleDbDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.HasRows)
                        {
                            if (rdr.Read())
                                txtDlina.Text = rdr["LENGTH"].ToString();
                            rdr.Close();
                        }
                    }
                    cmd.Dispose();
                }
                // для инспекционной решетке №7 и Mair в поле ввода «Длина, мм» 
                //подтягивать значение длины трубы, учитывая прохождение ее по участку ремонта и отбора проб
                else if ((WorkplaceId == 80 || WorkplaceId == 63))
                {
                    cmd = conn.CreateCommand();
                    cmd.CommandText = @"SELECT LENGTH FROM
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
                    cmd.Parameters.AddWithValue("PIPE_NUMBERip", pipeNumber);
                    cmd.Parameters.AddWithValue("PIPE_NUMBERop", pipeNumber);
                    using (OleDbDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.HasRows)
                        {
                            if (rdr.Read())
                                txtDlina.Text = rdr["LENGTH"].ToString();
                            rdr.Close();
                        }
                    }
                    cmd.Dispose();
                }
                //заполнение длины трубы с ТОС, если рабочее место установка пакетирования или инспекции 8-11
                else if ((WorkplaceId > 80 && WorkplaceId <= 84))
                {
                    cmd = conn.CreateCommand();
                    cmd.CommandText = @"SELECT            op.pipelength as LENGTH
                                          FROM    optimal_pipes op
                                          WHERE op.pipe_number =? ";
                    cmd.Parameters.AddWithValue("PIPE_NUMBER", pipeNumber);
                    using (OleDbDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.HasRows)
                        {
                            if (rdr.Read())
                                txtDlina.Text = rdr["LENGTH"].ToString();
                            rdr.Close();
                        }
                    }
                    cmd.Dispose();
                }
                else
                {
                    //длина трубы с измерителя линии отделки 1
                    cmd = Master.Connect.ORACLE_TESC3().CreateCommand();
                    cmd.CommandText = "select LENGTH from IZMLENGTH_OTDELKA where (PIPE_NUMBER=?)and(EDIT_STATE=0) order by REC_DATE desc";
                    cmd.Parameters.AddWithValue("PIPE_NUMBER", pipeNumber);
                    using (OleDbDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.HasRows)
                        {
                            if (rdr.Read())
                                txtDlina.Text = rdr["LENGTH"].ToString();
                            rdr.Close();
                        }
                    }
                    cmd.Dispose();
                }
            }
            double temp = 0;
            if (double.TryParse(txtDlina.Text, out temp))
            {
                temp = Math.Round(temp / 1000, 2);
                txtDlina.Text = temp.ToString();
            }
        }
        catch { }
    }
}