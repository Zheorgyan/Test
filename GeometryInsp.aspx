<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" CodeFile="GeometryInsp.aspx.cs" Inherits="GeometryInsp"
    Title="Ввод геометрических параметров на инспекционных решетках " %>

<%@ Register Src="SelectionTable.ascx" TagName="SelectionTable" TagPrefix="uc1" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>


<asp:Content ID="Content1" ContentPlaceHolderID="TitlePlaceHolder" runat="Server">
    Ввод геометрических параметров на инспекционных решетках

    <script language="javascript" type="text/javascript">

        prefix = "ctl00_MainPlaceHolder_";

        //удаление лишних пробелов в строке
        function Trim(text) {
            try {
                while (text.slice(0, 1) == " ")
                    text = text.slice(1, text.length);
                while (text.slice(text.length - 1, text.length) == " ")
                    text = text.slice(0, text.length - 1);
                while (text.indexOf("  ") != -1)
                    text = text.replace("  ", " ");
            }
            catch (error) { };
            return (text);
        }

        //проверка, является ли строка числом
        //параметр allowEmpty=true - разрешение ввода пустых строк
        function IsNumber(text, allowEmpty) {
            text = Trim(text);
            if ((text == "") & (!allowEmpty)) return true;
            if (text == "") return false;
            return !(isNaN(Number(text)));
        }

        //функция валидации ввода номера
        function fncClientValidation(source, arguments) {
            arguments.Value = arguments.Value.replace(",", ".");
            arguments.IsValid = IsNumber(arguments.Value, false);
        }

        //функция валидации ввода диаметров трубы
        //с расчетом овальности по переднему концу
        function ValidateNumberOvalPK(source, arguments) {
            arguments.Value = arguments.Value.replace(",", ".");
            arguments.IsValid = IsNumber(arguments.Value, false);

            //пролучение значений
            DiamPKpSH = GetEditValueFloat(document.getElementById(prefix + "txtDiamPKpSH"));
            DiamPKpT = GetEditValueFloat(document.getElementById(prefix + "txtDiamPKpT"));
        }

        //с расчетом овальности по заднему концу
        function ValidateNumberOvalZK(source, arguments) {
            arguments.Value = arguments.Value.replace(",", ".");
            arguments.IsValid = IsNumber(arguments.Value, false);

            //пролучение значений
            DiamZKpSH = GetEditValueFloat(document.getElementById(prefix + "txtDiamZKpSH"));
            DiamZKpT = GetEditValueFloat(document.getElementById(prefix + "txtDiamZKpT"));
        }

        //с расчетом овальности по телу
        function ValidateNumberOvalT(source, arguments) {
            arguments.Value = arguments.Value.replace(",", ".");
            arguments.IsValid = IsNumber(arguments.Value, false);

            //пролучение значений
            DiamTpSH = GetEditValueFloat(document.getElementById(prefix + "txtDiamTpSH"));
            DiamTpT = GetEditValueFloat(document.getElementById(prefix + "txtDiamTpT"));
        }

        //Объедененная функция для проверки ввода данных
        function fncSave(btn) {
            if (CheckInputsGeometry() == true) PostBackByButton(btn);
            return false;
        }

        //функция проверки правильности ввода всех данных формы поиска
        //параметр allowEmpty=true - разрешать пустые поля
        function CheckFindInputs(allowEmpty) {
            //удаление лишних пробелов в значениях 
            Year = Trim(document.getElementById(prefix + "txbYear").value);
            PipeNumber = Trim(document.getElementById(prefix + "txbPipeNumber").value);
            Check = Trim(document.getElementById(prefix + "txbCheck").value);

            //проверка критериев поиска - должны быть все параметры
            if ((Year == "") | (PipeNumber == "") | (Check == "")) {
                alert("Для поиска трубы необходимо указать её номер и контрольную цифру");
                return false;
            }

            //проверка корректности ввода данных
            msg = "";
            if (!IsNumber(Year, false))
                msg = msg + "Неверно указано значение в поле \"Год\"\n";
            if (!IsNumber(PipeNumber, false))
                msg = msg + "Неверно указано значение в поле \"№ Трубы\"\n";
            if (!IsNumber(Check, false))
                msg = msg + "Неверно указано значение в поле \"Контрольная цифра\"\n";
            if (msg != "") {
                alert(msg + "\nДля продолжения необходимо правильно указать перечисленные значения.");
                return false;
            }

            //проверка наличия ввода всех данных
            msg = "";
            if (!allowEmpty) {
                if (Year == "")
                    msg = msg + "Не указано значение в поле \"Годe\"\n";
                if (PipeNumber == "")
                    msg = msg + "Не указано значение в поле \"№ Трубы\"\n";
                if (Check == "")
                    msg = msg + "Не указано значение в поле \"Контрольная цифра\"\n";
                if (msg != "") {
                    alert(msg + "\nДля продолжения необходимо правильно указать перечисленные значения.");
                    return false;
                }
            }
            return true;
        }

        //функция проверки правильности ввода всех данных по геометрии
        function CheckInputsGeometry() {
            //пролучение значений

            PartNum = GetEditValueInt(document.getElementById(prefix + "txtPartNumber"));
            DiamPKpSH = GetEditValueFloat(document.getElementById(prefix + "txtDiamPKpSH"));
            DiamPKpT = GetEditValueFloat(document.getElementById(prefix + "txtDiamPKpT"));
            DiamTpSH = GetEditValueFloat(document.getElementById(prefix + "txtDiamTpSH"));
            DiamTpT = GetEditValueFloat(document.getElementById(prefix + "txtDiamTpT"));
            DiamZKpSH = GetEditValueFloat(document.getElementById(prefix + "txtDiamZKpSH"));
            DiamZKpT = GetEditValueFloat(document.getElementById(prefix + "txtDiamZKpT"));
            Dlina = GetEditValueFloat(document.getElementById(prefix + "txtDlina"));
            KosinaPTor = GetEditValueFloat(document.getElementById(prefix + "txtKosinaPTor"));
            KosinaZTor = GetEditValueFloat(document.getElementById(prefix + "txtKosinaZTor"));
            Krivizna1mT = GetEditValueFloat(document.getElementById(prefix + "txtKrivizna1mT"));
            KriviznaVciaT = GetEditValueFloat(document.getElementById(prefix + "txtKriviznaVciaT"));
            OstatokVnutGrata = GetEditValueFloat(document.getElementById(prefix + "txtOstatokVnutGrata"));
            OstatokNarujGrata = GetEditValueFloat(document.getElementById(prefix + "txtOstatokNarujGrata"));
            SmeschKrom = GetEditValueFloat(document.getElementById(prefix + "txtSmeschKrom"));
            TolSten = GetEditValueFloat(document.getElementById(prefix + "txtTolSten"));
            TolSten2 = GetEditValueFloat(document.getElementById(prefix + "txtTolSten2"));

            ShirinaTorKolPTorMin = GetEditValueFloat(document.getElementById(prefix + "txtShirinaTorKolPTorMin"));
            ShirinaTorKolPTorMax = GetEditValueFloat(document.getElementById(prefix + "txtShirinaTorKolPTorMax"));
            ShirinaTorKolZTorMin = GetEditValueFloat(document.getElementById(prefix + "txtShirinaTorKolZTorMin"));
            ShirinaTorKolZTorMax = GetEditValueFloat(document.getElementById(prefix + "txtShirinaTorKolZTorMax"));

            DiametrOnFrontEnd = GetEditValueFloat(document.getElementById(prefix + "txtDiametrOnFrontEnd"));
            DiametrOnBackEnd = GetEditValueFloat(document.getElementById(prefix + "txtDiametrOnBackEnd"));
            DiametrOnBody = GetEditValueFloat(document.getElementById(prefix + "txtDiametrOnBody"));

            OuterDefect = Trim(document.getElementById(prefix + "ddlOuterDefect").value);
            InnerDefect = Trim(document.getElementById(prefix + "ddlInnerDefect").value);
            WorkPlace = Trim(document.getElementById(prefix + "ddlWorkPlace").value);
            EndDefect = Trim(document.getElementById(prefix + "ddlEndDefect").value);
            //Проверка наличия данных   
            if ((PartNum == "") & (DiamPKpSH == "") & (DiamPKpT == "") & (DiamTpSH == "") & (DiamTpT == "") & (DiamZKpSH == "") & (DiamZKpT == "")
                & (Dlina == "") & (KosinaPTor == "") & (KosinaZTor == "") & (Krivizna1mT == "") & (KriviznaVciaT == "")
                & (OstatokVnutGrata == "") & (OstatokNarujGrata == "") & (SmeschKrom == "") & (TolSten == "") & (TolSten2 == "") & (ShirinaTorKolPTorMin == "")
                & (ShirinaTorKolPTorMax == "") & (ShirinaTorKolZTorMin == "") & (ShirinaTorKolZTorMax == "")) {
                alert("Не указано ни одного значения.\nДля продолжения необходимо их указать.");
                return false;
            }

            //проверка заполнения всех данных
            msg = "";

            if (PartNum == "")
                msg = msg + "Необходимо указать значение в поле \"Номер партии\"\n";
            if (Dlina == "")
                msg = msg + "Необходимо указать значение в поле \"Длина трубы\"\n";
            if (KosinaPTor == "")
                msg = msg + "Необходимо указать значение в поле \"Косина реза (передний торец)\"\n";
            if (KosinaZTor == "")
                msg = msg + "Необходимо указать значение в поле \"Косина реза (задний торец)\"\n";
            if (Krivizna1mT == "")
                msg = msg + "Необходимо указать значение в поле \"Кривизна на 1 м трубы\"\n";
            if (KriviznaVciaT == "")
                msg = msg + "Необходимо указать значение в поле \"Кривизна трубы по всей длине\"\n";
            if (OstatokVnutGrata == "")
                msg = msg + "Необходимо указать значение в поле \"Остаток внутреннего грата\"\n";
            if (OstatokNarujGrata == "")
                msg = msg + "Необходимо указать значение в поле \"Остаток наружного грата\"\n";
            if (SmeschKrom == "")
                msg = msg + "Необходимо указать значение в поле \"Смещение кромок трубы\"\n";
            if (TolSten == "")
                msg = msg + "Необходимо указать значение в поле \"Толщина стенки трубы (замер 1)\"\n";
            if (TolSten2 == "")
                msg = msg + "Необходимо указать значение в поле \"Толщина стенки трубы (замер 2)\"\n";

            if (DiametrOnFrontEnd == "")
                msg = msg + "Необходимо указать значение в поле \"Диаметр по переднему концу\"\n";
            if (DiametrOnBackEnd == "")
                msg = msg + "Необходимо указать значение в поле \"Диаметр по заднему концу\"\n";
            if (DiametrOnBody == "")
                msg = msg + "Необходимо указать значение в поле \"Диаметр по телу\"\n";

            if (ShirinaTorKolPTorMin == "")
                msg = msg + "Необходимо указать значение в поле \"Ширина торцевого кольца передний торец - минимальный\"\n";

            if (ShirinaTorKolPTorMax == "")
                msg = msg + "Необходимо указать значение в поле \"Ширина торцевого кольца передний торец - максимальный\"\n";

            if (ShirinaTorKolZTorMin == "")
                msg = msg + "Необходимо указать значение в поле \"Ширина торцевого кольца задний торец - минимальный\"\n";

            if (ShirinaTorKolZTorMax == "")
                msg = msg + "Необходимо указать значение в поле \"Ширина торцевого кольца задний торец - максимальный\"\n";

            if (OuterDefect == "")
                msg = msg + "Необходимо заполнить поле \"Состояние наружной поверхности\". Если дефект отсутствует, то необходимо выбрать \"Уд\"\n";

            if (InnerDefect == "")
                msg = msg + "Необходимо заполнить поле \"Состояние внутренней поверхности\". Если дефект отсутствует, то необходимо выбрать \"Уд\"\n";

            if (WorkPlace == "-1")
                msg = msg + "Необходимо заполнить поле \"Номер инспекционной решетки\"\n";

            if (EndDefect == "")
                msg = msg + "Необходимо заполнить поле \"Заусенцы на торцах\". Если дефект отсутствует, то необходимо выбрать \"Уд\"\n";


            if (msg != "") {
                alert(msg + "\nДля продолжения необходимо указать перечисленные значения.");
                return false;
            }

            //проверка правильности ввода всех данных
            msg = "";

            if (!IsNumber(PartNum, false))
                msg = msg + "Неверно указано значение в поле \"Номер партии\"\n";
            if (!IsNumber(DiamPKpSH, false))
                msg = msg + "Неверно указано значение в поле \"Диаметр локальный по переднему концу трубы максимальный, мм\"\n";
            if (!IsNumber(DiamPKpT, false))
                msg = msg + "Неверно указано значение в поле \"Диаметр локальный по переднему концу трубы минимальный\"\n";
            if (!IsNumber(DiamTpSH, false))
                msg = msg + "Неверно указано значение в поле \"Диаметр локальный по телу трубы максимальный\"\n";
            if (!IsNumber(DiamTpT, false))
                msg = msg + "Неверно указано значение в поле \"Диаметр локальный по телу трубы минимальный\"\n";
            if (!IsNumber(DiamZKpSH, false))
                msg = msg + "Неверно указано значение в поле \"Диаметр локальный по заднему концу трубы максимальный\"\n";
            if (!IsNumber(DiamZKpT, false))
                msg = msg + "Неверно указано значение в поле \"Диаметр локальный по заднему концу трубы минимальный\"\n";
            if (!IsNumber(Dlina, false))
                msg = msg + "Неверно указано значение в поле \"Длина трубы\"\n";
            if (!IsNumber(KosinaPTor, false))
                msg = msg + "Неверно указано значение в поле \"Косина реза (передний торец)\"\n";
            if (!IsNumber(KosinaZTor, false))
                msg = msg + "Неверно указано значение в поле \"Косина реза (задний торец)\"\n";
            if (!IsNumber(Krivizna1mT, false))
                msg = msg + "Неверно указано значение в поле \"Кривизна на 1 м трубы\"\n";
            if (!IsNumber(KriviznaVciaT, false))
                msg = msg + "Неверно указано значение в поле \"Кривизна трубы по всей длине\"\n";
            if (!IsNumber(OstatokVnutGrata, false))
                msg = msg + "Неверно указано значение в поле \"Остаток внутреннего грата\"\n";
            if (!IsNumber(OstatokNarujGrata, false))
                msg = msg + "Неверно указано значение в поле \"Остаток наружного грата\"\n";
            if (!IsNumber(SmeschKrom, false))
                msg = msg + "Неверно указано значение в поле \"Смещение кромок трубы\"\n";
            if (!IsNumber(TolSten, false))
                msg = msg + "Неверно указано значение в поле \"Толщина стенки трубы (замер 1)\"\n";
            if (!IsNumber(TolSten2, false))
                msg = msg + "Неверно указано значение в поле \"Толщина стенки трубы (замер 2)\"\n";

            if (!IsNumber(DiametrOnFrontEnd, false))
                msg = msg + "Неверно указано значение в поле \"Диаметр по переднему концу\"\n";
            if (!IsNumber(DiametrOnBackEnd, false))
                msg = msg + "Неверно указано значение в поле \"Диаметр по заднему концу\"\n";
            if (!IsNumber(DiametrOnBody, false))
                msg = msg + "Неверно указано значение в поле \"Диаметр по телу\"\n";
            if (msg != "") {
                alert(msg + "\nДля продолжения необходимо правильно указать перечисленные значения.");
                return false;
            }
            return true;
        }

    </script>

</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainPlaceHolder" runat="Server">
    &nbsp;&nbsp;
    <asp:MultiView ID="mvViews" runat="server" ActiveViewIndex="0">
        <asp:View ID="vInputCondition" runat="server">
            <table>
                <tr>
                    <td style="text-align: left;">
                        <table>
                            <tr>
                                <td style="vertical-align: middle; text-align: left; font-size: 10pt;">Год</td>
                                <td style="vertical-align: middle; text-align: left;"></td>
                                <td style="vertical-align: middle; text-align: left; font-size: 10pt;">№ Трубы</td>
                                <td style="vertical-align: middle; text-align: center"></td>
                                <td style="vertical-align: middle; text-align: center;">
                                    <asp:LinkButton ID="lbtHelpCheck" runat="server" OnClick="lbtHelpCheck_Click">(?)</asp:LinkButton>
                                </td>
                                <td align="center" style="width: 15px"></td>
                                <td align="center">&nbsp;</td>
                            </tr>
                            <tr>
                                <td style="vertical-align: middle; text-align: left">
                                    <asp:TextBox ID="txbYear" runat="server" MaxLength="2" TabIndex="1"
                                        Width="25px"></asp:TextBox>
                                </td>
                                <td style="vertical-align: middle; width: 15px; text-align: left"></td>
                                <td style="vertical-align: middle; text-align: left;">
                                    <asp:TextBox ID="txbPipeNumber" runat="server" MaxLength="6" TabIndex="2"
                                        Width="90px"></asp:TextBox>
                                </td>
                                <td style="vertical-align: middle; width: 15px; text-align: center">-</td>
                                <td style="vertical-align: middle; text-align: center;">
                                    <asp:TextBox ID="txbCheck" runat="server" MaxLength="1" TabIndex="3"
                                        Width="25px"></asp:TextBox>
                                </td>
                                <td style="vertical-align: middle; width: 15px; text-align: center"></td>
                                <td style="text-align: left;">
                                    <asp:Button ID="btnInputGeometry" runat="server" Height="23px"
                                        OnClick="btnInputGeometry_Click" OnClientClick="return CheckFindInputs(false)"
                                        Style="display: inline" TabIndex="4" Text="Ввести данные" Width="107px" />
                                </td>
                            </tr>
                            <tr>
                                <td style="vertical-align: middle; width: 26px; text-align: center; height: 21px;">
                                    <asp:CustomValidator ID="CustomValidator1" runat="server"
                                        ClientValidationFunction="fncClientValidation" ControlToValidate="txbYear"
                                        ErrorMessage="! ! !" Font-Size="Smaller"
                                        Style="vertical-align: middle; text-align: left"></asp:CustomValidator>
                                </td>
                                <td style="vertical-align: middle; width: 15px; height: 21px; text-align: center"></td>
                                <td style="vertical-align: middle; text-align: center;">
                                    <asp:CustomValidator ID="CustomValidator2" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="txbPipeNumber" ErrorMessage="Ошибка" Font-Size="Smaller"></asp:CustomValidator>
                                </td>
                                <td style="vertical-align: middle; width: 15px; height: 21px; text-align: center"></td>
                                <td style="vertical-align: middle; text-align: center;">
                                    <asp:CustomValidator ID="CustomValidator3" runat="server"
                                        ClientValidationFunction="fncClientValidation" ControlToValidate="txbCheck"
                                        ErrorMessage="! ! !" Font-Size="Smaller"></asp:CustomValidator>
                                </td>
                                <td style="vertical-align: middle; width: 15px; height: 21px; text-align: center"></td>
                                <td></td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>
            <asp:Panel ID="pnlExistingRecsInGeometry" runat="server" Height="38px"
                Visible="False">
                <table>
                    <tr>
                        <td style="width: 371px; height: 26px; font-size: 10pt;">Список существующих записей по геометрическим замерам</td>
                    </tr>
                    <tr>
                        <td style="width: 371px;">
                            <asp:Table ID="tblExistsRecordInGeometry" runat="server">
                            </asp:Table>
                        </td>
                    </tr>
                    <tr>
                        <td style="width: 371px;">
                            <asp:Button ID="btnAddNewRecord_ifRecExists" runat="server"
                                Text="Добавить новую запись" Width="163px" Font-Size="10pt"
                                Height="23px" />
                        </td>
                    </tr>
                </table>
            </asp:Panel>
        </asp:View>
        <asp:View ID="vGetChekingNum" runat="server">
            <table>
                <tr>
                    <td style="height: 100px; width: 667px;">
                        <font size="2">Чтобы раccчитать контрольную цифру для №<asp:Label 
                            ID="lblNumtrub" runat="server"></asp:Label>
                        ввдедите свой логин и пароль</font>
                        <br style="font-size: 10pt" />
                        <table>
                            <tr>
                                <td>
                                    <font size="2">Логин</font>
                                </td>
                                <td style="width: 158px">
                                    <asp:TextBox ID="txbLogin" runat="server" TabIndex="1" Width="184px"></asp:TextBox>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <font size="2">Пароль</font>
                                </td>
                                <td style="width: 158px">
                                    <asp:TextBox ID="txbPassword" runat="server" TabIndex="2" TextMode="Password"
                                        Width="184px"></asp:TextBox>
                                </td>
                            </tr>
                            <tr>
                                <td colspan="2" style="font-size: 4px"></td>
                            </tr>
                            <tr>
                                <td colspan="2" style="text-align: right">
                                    <asp:Button ID="btnLogin" runat="server" Height="23px" OnClick="btnLogin_Click"
                                        TabIndex="3" Text="Ok" Width="69px" Font-Size="10pt" />
                                    &nbsp;
                                    <asp:Button ID="btnCansel" runat="server" Height="23px"
                                        OnClick="btnCansel_Click" TabIndex="4" Text="Отмена" UseSubmitBehavior="False"
                                        Width="69px" Font-Size="10pt" />
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>
        </asp:View>
        <asp:View ID="vInputGeometry" runat="server">
            <table style="font-size: 10pt">
                <tr>
                    <td style="width: 14px;"></td>
                    <td style="width: 777px; vertical-align: middle; text-align: right;">
                        <table style="font-size: 10pt">
                            <tr>
                                <td style="text-align: left; width: 466px;">
                                    <b>Труба №
                                    <asp:Label ID="txtPipeNumberDubl" runat="server" Width="24%"></asp:Label>
                                    </b>
                                </td>
                                <td style="text-align: left; width: 900px;">
                                    <strong>Номер партии</strong>&nbsp;
                                    <asp:TextBox ID="txtPartNumber" runat="server" MaxLength="5" TabIndex="1"
                                        Width="57px"></asp:TextBox>
                                    &nbsp; &nbsp;<asp:CustomValidator ID="CustomValidator4" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="txtPartNumber" ErrorMessage="Ошибка" Font-Size="Smaller"
                                        Width="19px"></asp:CustomValidator>
                                </td>
                                <td style="width: 900px; text-align: left;"></td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>
            <table style="margin-top: 8px; margin-bottom: 8px;" id="tblCampaign"
                runat="server">
                <tr>
                    <td bgcolor="#e0e0e0">Строка задания на кампанию</td>
                </tr>
                <tr>
                    <td>
                        <asp:DropDownList ID="ddlCampaign" runat="server" AutoPostBack="True"
                            Font-Size="9pt" OnSelectedIndexChanged="ddlCampaign_SelectedIndexChanged"
                            Style="font-family: Courier New; margin-right: 0px;" Width="925px">
                            <asp:ListItem></asp:ListItem>
                        </asp:DropDownList>
                    </td>
                </tr>
                <tr>
                    <td>
                        <asp:Label
                            ID="lblErrorFillField" runat="server" Style="text-align: left; width: 900px; font-size: 8pt; color: #ff0000;"></asp:Label>
                    </td>
                </tr>
            </table>
            <table style="margin-top: 8px; margin-bottom: 8px;" id="Table1"
                runat="server">
                <tr>
                    <td>Номер инспекционной решетки</td>
                    <td>
                        <asp:DropDownList
                            ID="ddlWorkPlace" runat="server" AutoPostBack="True" Font-Bold="True" TabIndex="3" OnSelectedIndexChanged="ddlWorkPlace_SelectedIndexChanged">
                            <asp:ListItem Value="-1">(выберите)</asp:ListItem>
                            <asp:ListItem Value="0">ПДО</asp:ListItem>
                            <asp:ListItem>1</asp:ListItem>
                            <asp:ListItem>2</asp:ListItem>
                            <asp:ListItem>3</asp:ListItem>
                            <asp:ListItem>4</asp:ListItem>
                            <asp:ListItem>5</asp:ListItem>
                            <asp:ListItem>6</asp:ListItem>
                            <asp:ListItem Value="80">7</asp:ListItem>
                            <asp:ListItem Value="81">8</asp:ListItem>
                            <asp:ListItem Value="82">9</asp:ListItem>
                            <asp:ListItem Value="83">10</asp:ListItem>
                            <asp:ListItem Value="84">11</asp:ListItem>
                            <asp:ListItem Value="63">Mair</asp:ListItem>
                            <asp:ListItem Value="7">Участок ремонта</asp:ListItem>
                        </asp:DropDownList>
                    </td>
                </tr>
                <tr>
                    <td>
                        <asp:Button ID="btnLoad" runat="server" Height="23px"
                            OnClientClick="" TabIndex="3" Text="Заполнить значения"
                            UseSubmitBehavior="False" Font-Size="10pt" OnClick="btnLoad_Click" />
                    </td>
                </tr>
            </table>
            <table style="font-size: 10pt">
                <tr>
                    <td style="width: 14px; height: 159px"></td>
                    <td style="width: 777px; height: 159px; vertical-align: middle; text-align: right;">
                        <table style="font-size: 10pt">
                            <tr>
                                <td colspan="4" style="font-size: 4px"></td>
                            </tr>
                            <tr>
                                <td colspan="4" style="border-top: gray thin solid; font-size: 4px">&nbsp;</td>
                            </tr>
                            <tr>
                                <td style="font-size: 10pt; text-align: left; height: 36px;" colspan="2">
                                    <img src="Images/CX.png" />
                                    Диаметр локальный по переднему концу трубы, мм</td>
                                <td style="width: 900px; text-align: left" rowspan="2">Овальность по переднему
                                    <br />
                                    концу трубы, мм
                                </td>
                                <td style="width: 900px; text-align: left" rowspan="2"></td>
                            </tr>
                            <tr>
                                <td style="font-size: 10pt; text-align: left; width: 900px;">минимальный</td>
                                <td style="text-align: left; width: 900px;">максимальный</td>
                            </tr>
                            <tr>
                                <td style="text-align: left; width: 900px; font-size: 12pt; color: #000000;">
                                    <asp:TextBox ID="txtDiamPKpT" runat="server" MaxLength="6" TabIndex="3"
                                        Width="180px" AutoPostBack="True" OnTextChanged="txtOvalPK_SelectedIndexChanged"></asp:TextBox>
                                    &nbsp;
                                    <asp:CustomValidator ID="CusVal_txtDiamPKpT" runat="server"
                                        ClientValidationFunction="ValidateNumberOvalPK" ControlToValidate="txtDiamPKpT"
                                        ErrorMessage="Ошибка" Font-Size="Smaller" ValidateEmptyText="True"></asp:CustomValidator>
                                </td>
                                <td style="text-align: left; width: 900px; font-size: 12pt;">
                                    <asp:TextBox ID="txtDiamPKpSH" runat="server" MaxLength="6" TabIndex="4"
                                        Width="180px" AutoPostBack="True" OnTextChanged="txtOvalPK_SelectedIndexChanged"></asp:TextBox>
                                    &nbsp;
                                    <asp:CustomValidator ID="CusVal_txtDiamPKpSH" runat="server"
                                        ClientValidationFunction="ValidateNumberOvalPK" ControlToValidate="txtDiamPKpSH"
                                        ErrorMessage="Ошибка" Font-Size="Smaller" ValidateEmptyText="True"></asp:CustomValidator>
                                </td>
                                <td style="width: 900px; text-align: left; font-size: 12pt; color: #000000;">
                                    <asp:TextBox ID="txtOvalPK" runat="server" MaxLength="4" ReadOnly="true"
                                        Style="border-right: silver 1px solid; border-top: silver 1px solid; border-left: silver 1px solid; border-bottom: silver 1px solid"
                                        Width="180px"></asp:TextBox>
                                    &nbsp;&nbsp;
                                </td>
                                <td style="width: 900px; text-align: left"></td>
                            </tr>
                            <tr style="font-size: 12pt; color: #000000">
                                <td style="text-align: left; width: 900px; font-size: 10px; height: 3px;"></td>
                                <td style="text-align: left; width: 900px; font-size: 10px; height: 3px;"></td>
                                <td style="width: 900px; text-align: left; font-size: 10px; height: 3px;"></td>
                                <td style="width: 900px; text-align: left; font-size: 10px; height: 3px;"></td>
                            </tr>
                            <tr style="font-size: 10pt; color: #000000">
                                <td style="text-align: left" colspan="2">
                                    <img src="Images/CX.png" />
                                    Диаметр локальный по заднему концу трубы, мм</td>
                                <td style="width: 900px; text-align: left" rowspan="2">Овальность по заднему
                                    <br />
                                    концу трубы, мм
                                </td>
                                <td style="width: 900px; text-align: left" rowspan="2"></td>
                            </tr>
                            <tr style="font-size: 10pt; color: #000000">
                                <td style="text-align: left; width: 900px;">минимальный</td>
                                <td style="text-align: left; width: 900px;">максимальный</td>
                            </tr>
                            <tr style="font-size: 12pt">
                                <td style="text-align: left; width: 900px; font-size: 12pt; color: #000000; height: 29px;">
                                    <asp:TextBox ID="txtDiamZKpT" runat="server" MaxLength="6" TabIndex="5"
                                        Width="180px" AutoPostBack="True" OnTextChanged="txtOvalZK_SelectedIndexChanged"></asp:TextBox>
                                    &nbsp;
                                    <asp:CustomValidator ID="CusVal_txtDiamZKpT" runat="server"
                                        ClientValidationFunction="ValidateNumberOvalZK" ControlToValidate="txtDiamZKpT"
                                        ErrorMessage="Ошибка" Font-Size="Smaller" ValidateEmptyText="True"></asp:CustomValidator>
                                </td>
                                <td style="text-align: left; width: 900px; height: 29px;">
                                    <asp:TextBox ID="txtDiamZKpSH" runat="server" MaxLength="6" TabIndex="6"
                                        Width="180px" AutoPostBack="True" OnTextChanged="txtOvalZK_SelectedIndexChanged"></asp:TextBox>
                                    &nbsp;
                                    <asp:CustomValidator ID="CusVal_txtDiamZKpSH" runat="server"
                                        ClientValidationFunction="ValidateNumberOvalZK" ControlToValidate="txtDiamZKpSH"
                                        ErrorMessage="Ошибка" Font-Size="Smaller" ValidateEmptyText="True"></asp:CustomValidator>
                                </td>
                                <td style="width: 900px; text-align: left; font-size: 12pt; color: #000000; vertical-align: middle; height: 29px;">
                                    <asp:TextBox ID="txtOvalZK" runat="server" MaxLength="4" ReadOnly="True"
                                        Style="border-right: silver 1px solid; border-top: silver 1px solid; border-left: silver 1px solid; border-bottom: silver 1px solid"
                                        Width="180px"></asp:TextBox>
                                    &nbsp;&nbsp;
                                </td>
                                <td style="width: 900px; text-align: left; font-size: 10px; height: 3px;"></td>
                            </tr>
                            <tr style="font-size: 12pt; color: #000000">
                                <td style="text-align: left; width: 900px; font-size: 10px; height: 3px;"></td>
                                <td style="text-align: left; width: 900px; font-size: 10px; height: 3px;"></td>
                                <td style="width: 900px; text-align: left; font-size: 10px; height: 3px;"></td>
                                <td style="width: 900px; text-align: left; font-size: 10px; height: 3px;"></td>
                            </tr>
                            <tr style="color: #000000">
                                <td style="font-size: 10pt; text-align: left" colspan="2">
                                    <img src="Images/CX.png" />
                                    Диаметр локальный по телу трубы, мм</td>
                                <td style="width: 900px; text-align: left;" rowspan="2">Овальность по телу
                                    <br />
                                    трубы, мм
                                </td>
                                <td style="width: 900px; text-align: left;" rowspan="2"></td>
                            </tr>
                            <tr style="color: #000000">
                                <td style="font-size: 10pt; text-align: left; width: 900px;">минимальный</td>
                                <td style="text-align: left; width: 900px;">максимальный</td>
                            </tr>
                            <tr>
                                <td style="text-align: left; font-size: 12pt; width: 900px; color: #000000;">
                                    <asp:TextBox ID="txtDiamTpT" runat="server" MaxLength="6" TabIndex="7"
                                        Width="180px" AutoPostBack="True" OnTextChanged="txtOvalT_SelectedIndexChanged"></asp:TextBox>
                                    &nbsp;
                                    <asp:CustomValidator ID="CusVal_txtDiamTpT" runat="server"
                                        ClientValidationFunction="ValidateNumberOvalT" ControlToValidate="txtDiamTpT"
                                        ErrorMessage="Ошибка" Font-Size="Smaller" ValidateEmptyText="True"></asp:CustomValidator>
                                </td>
                                <td style="text-align: left; width: 900px;">
                                    <asp:TextBox ID="txtDiamTpSH" runat="server" MaxLength="6" TabIndex="8"
                                        Width="180px" AutoPostBack="True" OnTextChanged="txtOvalT_SelectedIndexChanged"></asp:TextBox>
                                    &nbsp; &nbsp;<asp:CustomValidator ID="CusVal_txtDiamTpSH" runat="server"
                                        ClientValidationFunction="ValidateNumberOvalT" ControlToValidate="txtDiamTpSH"
                                        ErrorMessage="Ошибка" Font-Size="Smaller" ValidateEmptyText="True"></asp:CustomValidator>
                                </td>
                                <td style="text-align: left; font-size: 12pt; width: 900px; color: #000000;">
                                    <asp:TextBox ID="txtOvalT" runat="server" MaxLength="4" ReadOnly="True"
                                        Style="border-right: silver 1px solid; border-top: silver 1px solid; border-left: silver 1px solid; border-bottom: silver 1px solid"
                                        Width="180px"></asp:TextBox>
                                    &nbsp; &nbsp;&nbsp;</td>
                                <td style="text-align: left; font-size: 10px; width: 900px;"></td>
                            </tr>
                            <tr>
                                <td style="text-align: left; font-size: 10px; width: 900px;"></td>
                                <td style="text-align: left; font-size: 10px; width: 900px;"></td>
                                <td style="text-align: left; font-size: 10px; width: 900px;"></td>
                                <td style="text-align: left; font-size: 10px; width: 900px;"></td>
                            </tr>
                            <tr>
                                <td style="font-size: 10pt; text-align: left; width: 900px;">Диаметр по переднему<br />
                                    концу, мм</td>
                                <td style="text-align: left; width: 900px;">Диаметр по заднему<br />
                                    концу, мм</td>
                                <td style="text-align: left; width: 900px;">Диаметр по телу, мм</td>
                                <td style="text-align: left; width: 900px;"></td>
                            </tr>
                            <tr>
                                <td style="text-align: left; width: 900px;">
                                    <asp:TextBox ID="txtDiametrOnFrontEnd" runat="server" MaxLength="8"
                                        TabIndex="9" Width="180px"></asp:TextBox>
                                    &nbsp; &nbsp;<asp:CustomValidator ID="CusVal_txtDiametrOnFrontEnd" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="txtDiametrOnFrontEnd" ErrorMessage="Ошибка"
                                        Font-Size="Smaller"></asp:CustomValidator>
                                    &nbsp;</td>
                                <td style="text-align: left; width: 900px;">
                                    <asp:TextBox ID="txtDiametrOnBackEnd" runat="server" MaxLength="8"
                                        TabIndex="10" Width="180px"></asp:TextBox>
                                    &nbsp;
                                    <asp:CustomValidator ID="CusVal_txtDiametrOnBackEnd" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="txtDiametrOnBackEnd" ErrorMessage="Ошибка"
                                        Font-Size="Smaller"></asp:CustomValidator></td>
                                <td style="text-align: left; width: 900px;">
                                    <asp:TextBox ID="txtDiametrOnBody" runat="server" MaxLength="8"
                                        TabIndex="11" Width="180px"></asp:TextBox>
                                    &nbsp; &nbsp;<asp:CustomValidator ID="CusVal_txtDiametrOnBodyEnd" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="txtDiametrOnBody" ErrorMessage="Ошибка"
                                        Font-Size="Smaller"></asp:CustomValidator></td>
                                <td style="text-align: left; width: 900px;"></td>
                            </tr>
                            <tr style="border-top: gray 2px solid; font-size: 12pt; color: #000000;">
                                <td colspan="4" rowspan="1" style="font-size: 4px">&nbsp;</td>
                            </tr>
                            <tr style="border-top: solid 2px gray">
                                <td colspan="4" rowspan="" style="border-top: gray thin solid; font-size: 4px">&nbsp;<!--  --></td>
                            </tr>
                            <tr>
                                <td style="font-size: 10pt; text-align: left; width: 900px;">Длина трубы, м</td>
                                <td style="text-align: left; width: 900px;">
                                    <img src="Images/CX.png" />
                                    Толщина стенки, мм<br />
                                    (замер 1 / замер 2)</td>
                                <td style="text-align: left; width: 900px;">Смещение кромок трубы, мм</td>
                                <td style="text-align: left; width: 900px;"></td>
                            </tr>
                            <tr>
                                <td style="text-align: left; width: 900px;">
                                    <asp:TextBox ID="txtDlina" runat="server" MaxLength="5" TabIndex="12"
                                        Width="180px"></asp:TextBox>
                                    &nbsp; &nbsp;<asp:CustomValidator ID="CusVal_txtDlina" runat="server"
                                        ClientValidationFunction="fncClientValidation" ControlToValidate="txtDlina"
                                        ErrorMessage="Ошибка" Font-Size="Smaller"></asp:CustomValidator>
                                </td>
                                <td style="text-align: left; font-size: 12pt; width: 900px; color: #000000;">
                                    <table style="font-size: 10pt; border-collapse: collapse">
                                        <tr>
                                            <td>
                                                <asp:TextBox ID="txtTolSten" runat="server" MaxLength="5" TabIndex="13"
                                                    Width="71px"></asp:TextBox>
                                            </td>
                                            <td>/</td>
                                            <td>
                                                <asp:TextBox ID="txtTolSten2" runat="server" MaxLength="5" TabIndex="14"
                                                    Width="71px"></asp:TextBox>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td>
                                                <asp:CustomValidator ID="CusVal_txtTolSten" runat="server"
                                                    ClientValidationFunction="fncClientValidation" ControlToValidate="txtTolSten"
                                                    ErrorMessage="Ошибка" Font-Size="Smaller"></asp:CustomValidator>
                                            </td>
                                            <td>&nbsp;</td>
                                            <td>&nbsp;</td>
                                        </tr>
                                    </table>
                                </td>
                                <td style="text-align: left; font-size: 12pt; width: 900px; color: #000000;">
                                    <asp:TextBox ID="txtSmeschKrom" runat="server" MaxLength="3" TabIndex="15"
                                        Width="180px"></asp:TextBox>
                                    &nbsp;
                                    <asp:CustomValidator ID="CusVal_txtSmeschKrom" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="txtSmeschKrom" ErrorMessage="Ошибка" Font-Size="Smaller"></asp:CustomValidator>
                                </td>
                                <td style="text-align: left; font-size: 12pt; width: 900px; color: #000000;"></td>
                            </tr>
                            <tr style="font-size: 12pt; color: #000000">
                                <td style="text-align: left; font-size: 10px; width: 900px; height: 3px;"></td>
                                <td style="text-align: left; font-size: 10px; width: 900px; height: 3px;"></td>
                                <td style="text-align: left; font-size: 10px; width: 900px; height: 3px;"></td>
                                <td style="text-align: left; font-size: 10px; width: 900px; height: 3px;"></td>
                            </tr>
                            <tr style="font-size: 10pt; color: #000000">
                                <td style="text-align: left; width: 900px;">Косина реза
                                    <br />
                                    (передний торец), мм</td>
                                <td style="text-align: left; width: 900px;">Косина реза
                                    <br />
                                    (задний торец), мм</td>
                                <td style="text-align: left; width: 900px;"></td>
                                <td style="text-align: left; width: 900px;"></td>
                            </tr>
                            <tr style="font-size: 12pt">
                                <td style="text-align: left; width: 900px;">
                                    <asp:TextBox ID="txtKosinaPTor" runat="server" MaxLength="3" TabIndex="16"
                                        Width="180px"></asp:TextBox>
                                    &nbsp;
                                    <asp:CustomValidator ID="CusVal_txtKosinaPTor" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="txtKosinaPTor" ErrorMessage="Ошибка" Font-Size="Smaller"></asp:CustomValidator>
                                </td>
                                <td style="text-align: left; font-size: 12pt; width: 900px; color: #000000;">
                                    <asp:TextBox ID="txtKosinaZTor" runat="server" MaxLength="3" TabIndex="17"
                                        Width="180px"></asp:TextBox>
                                    &nbsp;
                                    <asp:CustomValidator ID="CusVal_txtKosinaZTor" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="txtKosinaZTor" ErrorMessage="Ошибка" Font-Size="Smaller"></asp:CustomValidator>
                                </td>
                                <td style="text-align: left; font-size: 12pt; width: 900px; color: #000000;"></td>
                                <td style="text-align: left; font-size: 12pt; width: 900px; color: #000000;"></td>
                            </tr>
                            <tr style="font-size: 12pt; color: #000000">
                                <td style="text-align: left; font-size: 10px; width: 900px; height: 3px;"></td>
                                <td style="text-align: left; font-size: 10px; width: 900px; height: 3px;"></td>
                                <td style="text-align: left; font-size: 10px; width: 900px; height: 3px;"></td>
                                <td style="text-align: left; font-size: 10px; width: 900px; height: 3px;"></td>
                            </tr>
                            <tr style="font-size: 10pt; color: #000000">
                                <td style="text-align: left; width: 900px;">Кривизна на 1 м
                                    <br />
                                    трубы, мм</td>
                                <td style="text-align: left; width: 900px;">Кривизна трубы по всей
                                    <br />
                                    длине, мм</td>
                                <td style="text-align: left; width: 900px;"></td>
                                <td style="text-align: left; width: 900px;"></td>
                            </tr>
                            <tr style="font-size: 12pt">
                                <td style="text-align: left; width: 900px;">
                                    <asp:TextBox ID="txtKrivizna1mT" runat="server" MaxLength="3" TabIndex="18"
                                        Width="180px"></asp:TextBox>
                                    &nbsp;
                                    <asp:CustomValidator ID="CusVal_txtKrivizna1mT" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="txtKrivizna1mT" ErrorMessage="Ошибка" Font-Size="Smaller"></asp:CustomValidator>
                                </td>
                                <td style="text-align: left; font-size: 12pt; width: 900px; color: #000000;">
                                    <asp:TextBox ID="txtKriviznaVciaT" runat="server" MaxLength="4" TabIndex="19"
                                        Width="180px"></asp:TextBox>
                                    &nbsp;
                                    <asp:CustomValidator ID="CusVal_txtKriviznaVciaT" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="txtKriviznaVciaT" ErrorMessage="Ошибка" Font-Size="Smaller"></asp:CustomValidator>
                                </td>
                                <td style="text-align: left; font-size: 12pt; width: 900px; color: #000000;"></td>
                                <td style="text-align: left; font-size: 12pt; width: 900px; color: #000000;"></td>
                            </tr>
                            <tr style="font-size: 12pt; color: #000000">
                                <td style="text-align: left; height: 3px; font-size: 10px; width: 900px;"></td>
                                <td style="text-align: left; height: 3px; font-size: 10px; width: 900px;"></td>
                                <td style="text-align: left; height: 3px; font-size: 10px; width: 900px;"></td>
                                <td style="text-align: left; height: 3px; font-size: 10px; width: 900px;"></td>
                            </tr>
                            <tr style="font-size: 10pt; color: #000000">
                                <td style="text-align: left; width: 900px;">Остаток внутреннего
                                    <br />
                                    грата, мм</td>
                                <td style="text-align: left; width: 900px;">Остаток наружного
                                    <br />
                                    грата, мм</td>
                                <td style="text-align: left; width: 900px;"></td>
                                <td style="text-align: left; width: 900px;"></td>
                            </tr>
                            <tr style="font-size: 12pt">
                                <td style="text-align: left; width: 900px;">
                                    <asp:TextBox ID="txtOstatokVnutGrata" runat="server" MaxLength="3"
                                        TabIndex="20" Width="180px"></asp:TextBox>
                                    &nbsp;
                                    <asp:CustomValidator ID="CusVal_txtOstatokVnutGrata" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="txtOstatokVnutGrata" ErrorMessage="Ошибка"
                                        Font-Size="Smaller"></asp:CustomValidator>
                                </td>
                                <td style="text-align: left; width: 900px;">
                                    <asp:TextBox ID="txtOstatokNarujGrata" runat="server" MaxLength="3"
                                        TabIndex="21" Width="180px"></asp:TextBox>
                                    &nbsp;
                                    <asp:CustomValidator ID="CusVal_txtOstatokNarujGrata" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="txtOstatokNarujGrata" ErrorMessage="Ошибка"
                                        Font-Size="Smaller"></asp:CustomValidator>
                                </td>
                                <td style="text-align: left; width: 900px;"></td>
                                <td style="text-align: left; width: 900px;"></td>
                            </tr>
                            <%-- Добавлены новые поля 05.10.17 Романов С.А.--%>
                            <tr>
                                <td style="text-align: left; font-size: 10px; width: 900px;"></td>
                                <td style="text-align: left; font-size: 10px; width: 900px;"></td>
                                <td style="text-align: left; font-size: 10px; width: 900px;"></td>
                                <td style="text-align: left; font-size: 10px; width: 900px;"></td>
                            </tr>
                            <tr>
                                <td style="font-size: 10pt; text-align: left" colspan="2">Угол скоса фаски передний торец, град</td>
                                <td style="text-align: left" colspan="2">Угол скоса фаски задний торец, град</td>
                            </tr>
                            <tr>
                                <td style="font-size: 10pt; text-align: left; width: 900px;">минимальный</td>
                                <td style="text-align: left; width: 900px;">максимальный</td>
                                <td style="font-size: 10pt; text-align: left; width: 900px;">минимальный</td>
                                <td style="text-align: left; width: 900px;">максимальный</td>
                            </tr>
                            <tr>
                                <td style="text-align: left; width: 900px;">
                                    <asp:TextBox ID="txtYgolSkosaFaskiPTorMin" runat="server" MaxLength="8"
                                        TabIndex="22" Width="180px"></asp:TextBox>
                                    &nbsp; &nbsp;<asp:CustomValidator ID="CusVal_txtYgolSkosaFaskiPTorMin" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="txtYgolSkosaFaskiPTorMin" ErrorMessage="Ошибка"
                                        Font-Size="Smaller"></asp:CustomValidator>
                                    &nbsp;
                                </td>
                                <td style="text-align: left; width: 900px;">
                                    <asp:TextBox ID="txtYgolSkosaFaskiPTorMax" runat="server" MaxLength="8"
                                        TabIndex="23" Width="180px"></asp:TextBox>
                                    &nbsp;
                                    <asp:CustomValidator ID="CusVal_txtYgolSkosaFaskiPTorMax" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="txtYgolSkosaFaskiPTorMax" ErrorMessage="Ошибка"
                                        Font-Size="Smaller"></asp:CustomValidator>
                                </td>
                                <td style="text-align: left; width: 900px;">
                                    <asp:TextBox ID="txtYgolSkosaFaskiZTorMin" runat="server" MaxLength="8"
                                        TabIndex="24" Width="180px"></asp:TextBox>
                                    &nbsp;
                                    <asp:CustomValidator ID="CusVal_txtYgolSkosaFaskiZTorMin" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="txtYgolSkosaFaskiZTorMin" ErrorMessage="Ошибка"
                                        Font-Size="Smaller"></asp:CustomValidator>
                                </td>
                                <td style="text-align: left; width: 900px;">
                                    <asp:TextBox ID="txtYgolSkosaFaskiZTorMax" runat="server" MaxLength="8"
                                        TabIndex="25" Width="180px"></asp:TextBox>
                                    &nbsp;
                                    <asp:CustomValidator ID="CusVal_txtYgolSkosaFaskiZTorMax" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="txtYgolSkosaFaskiZTorMax" ErrorMessage="Ошибка"
                                        Font-Size="Smaller"></asp:CustomValidator>
                                    &nbsp;
                                </td>
                            </tr>
                            <tr>
                                <td style="text-align: left; font-size: 10px; width: 900px;"></td>
                                <td style="text-align: left; font-size: 10px; width: 900px;"></td>
                                <td style="text-align: left; font-size: 10px; width: 900px;"></td>
                                <td style="text-align: left; font-size: 10px; width: 900px;"></td>
                            </tr>
                            <tr>
                                <td style="font-size: 10pt; text-align: left; height: 20px;" colspan="2">Ширина торцевого кольца передний торец, мм</td>
                                <td style="text-align: left; height: 20px;" colspan="2">Ширина торцевого кольца задний торец, мм</td>
                            </tr>
                            <tr>
                                <td style="font-size: 10pt; text-align: left; width: 900px;">минимальный</td>
                                <td style="text-align: left; width: 900px;">максимальный</td>
                                <td style="font-size: 10pt; text-align: left; width: 900px;">минимальный</td>
                                <td style="text-align: left; width: 900px;">максимальный</td>
                            </tr>
                            <tr>
                                <td style="text-align: left; width: 900px;">
                                    <asp:TextBox ID="txtShirinaTorKolPTorMin" runat="server" MaxLength="8"
                                        TabIndex="26" Width="180px"></asp:TextBox>
                                    &nbsp; &nbsp;<asp:CustomValidator ID="CusVal_txtShirinaTorKolPTorMin" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="txtShirinaTorKolPTorMin" ErrorMessage="Ошибка"
                                        Font-Size="Smaller"></asp:CustomValidator>

                                </td>
                                <td style="text-align: left; width: 900px;">
                                    <asp:TextBox ID="txtShirinaTorKolPTorMax" runat="server" MaxLength="8"
                                        TabIndex="27" Width="180px"></asp:TextBox>
                                    &nbsp;
                                    <asp:CustomValidator ID="CusVal_txtShirinaTorKolPTorMax" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="txtShirinaTorKolPTorMax" ErrorMessage="Ошибка"
                                        Font-Size="Smaller"></asp:CustomValidator>
                                </td>
                                <td style="text-align: left; width: 900px;">
                                    <asp:TextBox ID="txtShirinaTorKolZTorMin" runat="server" MaxLength="8"
                                        TabIndex="28" Width="180px"></asp:TextBox>
                                    &nbsp;
                                    <asp:CustomValidator ID="CusVal_txtShirinaTorKolZTorMin" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="txtShirinaTorKolZTorMin" ErrorMessage="Ошибка"
                                        Font-Size="Smaller"></asp:CustomValidator>
                                </td>
                                <td style="text-align: left; width: 900px;">
                                    <asp:TextBox ID="txtShirinaTorKolZTorMax" runat="server" MaxLength="8"
                                        TabIndex="29" Width="180px"></asp:TextBox>
                                    &nbsp; &nbsp;<asp:CustomValidator ID="CusVal_txtShirinaTorKolZTorMax" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="txtShirinaTorKolZTorMax" ErrorMessage="Ошибка"
                                        Font-Size="Smaller"></asp:CustomValidator>
                                    &nbsp;
                                </td>
                            </tr>
                            <tr>
                                <td style="text-align: left; font-size: 10px; width: 900px;"></td>
                                <td style="text-align: left; font-size: 10px; width: 900px;"></td>
                                <td style="text-align: left; font-size: 10px; width: 900px;"></td>
                                <td style="text-align: left; font-size: 10px; width: 900px;"></td>
                            </tr>
                            <tr>
                                <td style="font-size: 10pt; text-align: left" colspan="2">Кривизна концевая передний конец трубы, мм</td>
                                <td style="font-size: 10pt; text-align: left" colspan="2">Кривизна концевая задний конец трубы, мм</td>
                            </tr>
                            <tr>
                                <td style="font-size: 10pt; text-align: left; width: 900px;">на 1 метре</td>
                                <td style="text-align: left; width: 900px;">на 1,5 метрах</td>
                                <td style="font-size: 10pt; text-align: left; width: 900px;">на 1 метре</td>
                                <td style="text-align: left; width: 900px;">на 1,5 метрах</td>
                            </tr>
                            <tr>
                                <td style="text-align: left; width: 900px;">
                                    <asp:TextBox ID="txtCURVATURE_FRONT_END_1000MM" runat="server" MaxLength="8"
                                        TabIndex="30" Width="180px"></asp:TextBox>
                                    &nbsp; &nbsp;<asp:CustomValidator ID="CusVal_txtCURVATURE_FRONT_END_1000MM" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="txtCURVATURE_FRONT_END_1000MM" ErrorMessage="Ошибка"
                                        Font-Size="Smaller"></asp:CustomValidator>
                                    &nbsp;
                                </td>
                                <td style="text-align: left; width: 900px;">
                                    <asp:TextBox ID="txtCURVATURE_FRONT_END_1500MM" runat="server" MaxLength="8"
                                        TabIndex="31" Width="180px"></asp:TextBox>
                                    &nbsp; &nbsp;<asp:CustomValidator ID="CusVal_txtCURVATURE_FRONT_END_1500MM" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="txtCURVATURE_FRONT_END_1500MM" ErrorMessage="Ошибка"
                                        Font-Size="Smaller"></asp:CustomValidator>
                                    &nbsp;
                                </td>
                                <td style="text-align: left; width: 900px;">
                                    <asp:TextBox ID="CURVATURE_BACK_END_1000MM" runat="server" MaxLength="8"
                                        TabIndex="32" Width="180px"></asp:TextBox>
                                    &nbsp; &nbsp;<asp:CustomValidator ID="CusVal_CURVATURE_BACK_END_1000MM" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="CURVATURE_BACK_END_1000MM" ErrorMessage="Ошибка"
                                        Font-Size="Smaller"></asp:CustomValidator>
                                    &nbsp;
                                </td>
                                <td style="text-align: left; width: 900px;">
                                    <asp:TextBox ID="CURVATURE_BACK_END_1500MM" runat="server" MaxLength="8"
                                        TabIndex="33" Width="180px"></asp:TextBox>
                                    &nbsp; &nbsp;<asp:CustomValidator ID="CusVal_CURVATURE_BACK_END_1500MM" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="CURVATURE_BACK_END_1500MM" ErrorMessage="Ошибка"
                                        Font-Size="Smaller"></asp:CustomValidator>
                                    &nbsp;
                                </td>
                            </tr>
                            <tr>
                                <td style="text-align: left; font-size: 10px; width: 900px;"></td>
                                <td style="text-align: left; font-size: 10px; width: 900px;"></td>
                                <td style="text-align: left; font-size: 10px; width: 900px;"></td>
                                <td style="text-align: left; font-size: 10px; width: 900px;"></td>
                            </tr>
                            <tr>
                                <td style="text-align: left; width: 900px;">Состояние наружной поверхности:</td>
                                <td colspan="3" style="text-align: left;">
                                    <asp:DropDownList ID="ddlOuterDefect" runat="server" TabIndex="34">
                                    </asp:DropDownList>
                                </td>
                            </tr>
                            <tr>
                                <td style="text-align: left; width: 900px;">Состояние внутренней поверхности:</td>
                                <td colspan="3" style="text-align: left;">
                                    <asp:DropDownList ID="ddlInnerDefect" runat="server" TabIndex="35">
                                    </asp:DropDownList>
                                </td>
                            </tr>
                            <tr>
                                <td style="text-align: left; width: 900px;">Заусенцы на торцах:</td>
                                <td colspan="3" style="text-align: left;">
                                    <asp:DropDownList ID="ddlEndDefect" runat="server" TabIndex="36">
                                    </asp:DropDownList>
                                </td>
                            </tr>
                            <tr>
                                <td colspan="4" style="font-size: 4px">&nbsp;</td>
                            </tr>
                            <tr>
                                <td colspan="4" style="border-top: gray thin solid; font-size: 4px">&nbsp;</td>
                            </tr>
                        </table>
                        <asp:Button ID="btnSave" runat="server" Height="23px" OnClick="btnSave_Click"
                            OnClientClick="return fncSave(this);" TabIndex="37" Text="Сохранить"
                            UseSubmitBehavior="False" Width="109px" Font-Size="10pt" />
                        <asp:Button ID="btnBack" runat="server" Height="23px" OnClick="btnBack_Click"
                            TabIndex="38" Text="Отмена" UseSubmitBehavior="False" Width="110px"
                            Font-Size="10pt" />
                    </td>
                </tr>
            </table>
        </asp:View>
        <asp:View ID="vContenueAfterSave" runat="server">
            <table>
                <tr>
                    <td style="width: 667px; height: 28px; vertical-align: middle; text-align: left;">
                        <font size="2">Данные успешно сохранены</font>&nbsp; &nbsp;<asp:Button
                            ID="btnContinue" runat="server" Height="23px" OnClick="btnContinue_Click"
                            TabIndex="1" Text="Продолжить" Font-Size="10pt" />
                    </td>
                </tr>
            </table>
        </asp:View>
    </asp:MultiView>
    &nbsp; 
</asp:Content>
