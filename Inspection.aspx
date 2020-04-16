<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" CodeFile="Inspection.aspx.cs" Inherits="Inspection" Title="Инспекционные решетки - прослеживание труб" EnableEventValidation="false" %>

<%@ Register Assembly="System.Web.Extensions, Version=1.0.61025.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35"
    Namespace="System.Web.UI" TagPrefix="asp" %>

<%@ Register Src="SelectionTable.ascx" TagName="SelectionTable" TagPrefix="uc1" %>

<%@ MasterType VirtualPath="~/MasterPage.master" %>

<%@ Register Src="PopupWindow.ascx" TagName="PopupWindow" TagPrefix="uc2" %>

<%@ Register Src="CalendarControl.ascx" TagName="CalendarControl" TagPrefix="uc3" %>

<asp:Content ID="Content1" ContentPlaceHolderID="TitlePlaceHolder" runat="Server">
    Ввод данных по трубам на инспекционных решетках&nbsp;

    <script type="text/javascript" language="javascript">

        var prefix = "ctl00_MainPlaceHolder_";

        //подсветка активной строки таблицы выбора записи для редактирования по приемке труб
        selectedRow = null;
        selectedPipeNumber = null;
        function HighlightEditTableRow(row, pipenumber) {
            if (selectedRow != null)
                selectedRow.style.backgroundColor = '';
            selectedRow = row;
            selectedPipeNumber = pipenumber;
            row.style.backgroundColor = '#cdcde1';
            document.getElementById("<%=btnEditRecord.ClientID%>").setAttribute('disabled', '');
            document.getElementById("<%=btnDeleteRecord.ClientID%>").setAttribute('disabled', '');
        }

        //подсветка активной строки таблицы выбора записи по транспортировочным номерам для редактирования
        function HighlightTransportNumberTableRow(row, pipenumber) {
            if (selectedRow != null)
                selectedRow.style.backgroundColor = '';
            selectedRow = row;
            document.getElementById("<%=fldSelectedTransportNumber.ClientID%>").value = pipenumber;
            row.style.backgroundColor = '#cdcde1';
            document.getElementById("<%=btnTransportNumberPrintlabel.ClientID%>").setAttribute('disabled', '');
        document.getElementById("<%=btnTransportNumberDelete.ClientID%>").setAttribute('disabled', '');
        document.getElementById("<%=btnTransportNumberEdit.ClientID%>").setAttribute('disabled', '');
        }

        //Обработчик нажатия на чекбокс cbAutoCampaign
        function check_AutoCampaign(event) {
            if (event.target.checked) {
                alert('Внимание! Выбран автоматический режим назначения кампании');
            }
            else {
                alert('Внимание! Автоматический режим назначения кампании отключен');
            }
            return false;
        }

        //обработчик нажатия кнопки btnEditRecord
        function btnEditClick() {
            if (selectedRow == null) {
                alert('Не выбрана запись для редактирования.');
            }
            else {
                __doPostBack('btnEditRecord', selectedRow.getAttribute('rowid'));
            }
            return false;
        }

        //обработчик нажатия кнопки btnDeleteRecord
        function btnDeleteClick() {
            if (selectedRow == null) {
                alert('Не выбрана запись для удаления.');
            }
            else
            if (confirm('Нажмите ОК для подтверждения удаления выбранной записи')) {
                __doPostBack('btnDeleteRecord', selectedRow.getAttribute('rowid'));
            }
            return false;
        }

        function btnGetGeomInsp() {
            __doPostBack('btnGetGeomInsp', '');
            return false;
        }

        function btnGetRuscRes() {
            __doPostBack('btnGetRuscRes', '');
            return false;
        }
        //отображение окна истории трубы
        function bthShowHistory2Click() {
            if (selectedPipeNumber == null) {
                alert('Не выбрана запись для просмотра истории трубы.');
            }
            else
                url = 'Reports/PipeHistoryReport.aspx?PIPE_NUMBER=' + selectedPipeNumber;
            window.open(url, 'GeometryWindow', 'toolbar=0,menubar=1,location=0,resizable=1,scrollbars=1');
            return false;
        }

        //отображение окна истории трубы
        function bthShowHistory3Click() {
            var pipeyear = document.getElementById("<%=lblYear.ClientID%>").innerText;
            var pipenumber = document.getElementById("<%=lblPipeNo.ClientID%>").innerText;
            url = 'Reports/PipeHistoryReport.aspx?PIPE_NUMBER=' + pipeyear + pipenumber;
            window.open(url, 'GeometryWindow', 'toolbar=0,menubar=1,location=0,resizable=1,scrollbars=1');
            return false;
        }


        //отображение календаря для выбора даты
        function ShowCalendar(ident) {
            name = 'cldStartDate';
            if (ident == 2) name = 'cldEndDate';
            cld = document.getElementById(prefix + name);

            if (cld.style.display == 'inline')
                cld.style.display = 'none';
            else cld.style.display = 'inline';
            return false;
        }

        //установка SelectedItem для DropDownList по значению Value
        function SelectDropDownItemByValue(ddlBox, txtBox) {
            ddlBox.selectedIndex = -1;
            if (Trim(txtBox.value) == "") return;
            for (var i = 0; i < ddlBox.options.length; i++) {
                if (ddlBox.options[i].value == Trim(txtBox.value))
                    ddlBox.selectedIndex = i;
            }
            if (ddlBox.selectedIndex == -1) {
                alert('Указан код несуществующего дефекта');
                txtBox.value = "";
                txtBox.focus();
            }
        }

        //установка кода дефекта в textBox при выборе из DropdownList
        function GetDefectCodeFromDropDownList(ddlBox, txtBox) {
            txtBox.value = Trim(ddlBox.options.value);
            if (txtBox.value == "") ddlBox.selectedIndex = -1;
        }

        //установка itemIndex=0 для DropdownList, если выбрано пустое значение
        function ChangeDropdownList(ddlBox) {
            if ((ddlBox.value == " ") | (ddlBox.value == "")) {
                ddlBox.selectedIndex = 0;
            }
            return true;
        }

        //функция проверки правильности ввода всех данных формы поиска
        //параметр allowEmpty=true - разрешать пустые поля
        function CheckFindInputs(allowEmpty) {
            //удаление лишних пробелов в значениях 
            Year = Trim(document.getElementById(prefix + "txbYear").value);
            PipeNumber = Trim(document.getElementById(prefix + "txbPipeNumber").value);
            Check = Trim(document.getElementById(prefix + "txbCheck").value);

            var workplace_id = document.getElementById(prefix + "ddlWorkPlace").value;

            //проверка корректности ввода данных
            msg = "";
            if (!IsNumber(Year, false))
                msg = msg + "Неверно указано значение в поле \"Год\"\n";
            if (!IsNumber(PipeNumber, false))
                msg = msg + "Неверно указано значение в поле \"№ Трубы\"\n";
            if (workplace_id != "0") {
                if (!IsNumber(Check, false))
                    msg = msg + "Неверно указано значение в поле \"Контрольная цифра\"\n";
            }

            if (msg != "") {
                alert(msg + "\nДля продолжения необходимо правильно указать перечисленные значения.");
                return false;
            }

            //проверка наличия ввода всех данных
            msg = "";
            if (!allowEmpty) {
                if (Year == "")
                    msg = msg + "Не указано значение в поле \"Год\"\n";
                if (PipeNumber == "")
                    msg = msg + "Не указано значение в поле \"№ Трубы\"\n";
                if (workplace_id != "0") {
                    if (Check == "")
                        msg = msg + "Не указано значение в поле \"Контрольная цифра\"\n";
                }
                if (msg != "") {
                    alert(msg + "\nДля продолжения необходимо правильно указать перечисленные значения.");
                    return false;
                }
            }
            return true;
        }


        function CheckFindInputs2(allowEmpty) {
            //удаление лишних пробелов в значениях 
            Amount = '0';

            var workplace_id = document.getElementById(prefix + "ddlWorkPlace").value;

            //проверка корректности ввода данных
            msg = "";

            try {
                Amount = Trim(document.getElementById(prefix + "txbAmounts").value);
            }
            catch (e) { };
            if (workplace_id == "85") {
                if (!IsNumber(Amount, false))
                    msg = msg + "Неверно указано значение в поле \"Количество зачисток\"\n";
            }

            if (msg != "") {
                alert(msg + "\nДля продолжения необходимо правильно указать перечисленные значения.");
                return false;
            }


            //проверка наличия ввода всех данных
            msg = "";
            if (!allowEmpty) {
                if (msg != "") {
                    alert(msg + "\nДля продолжения необходимо правильно указать перечисленные значения.");
                    return false;
                }
            }
            return true;
        }


        //открытие окна ввода геометрии
        function GeometryInput(btn) {
            //получение номера трубы из полей ввода
            Year = Trim(document.getElementById(prefix + "txbYear").value);
            PipeNumber = Trim(document.getElementById(prefix + "txbPipeNumber").value);
            Check = Trim(document.getElementById(prefix + "txbCheck").value);
            url = "Inspection.aspx?ToGeometry=1&PIPE_YEAR=" + Year + "&PIPE_NUMBER=" + PipeNumber + "&CHECK_CHAR=" + Check;
            window.open(url, 'GeometryWindow', 'toolbar=0,menubar=1,location=0,resizable=1,scrollbars=1');
            return false;
        }

        //обработчик нажатия кнопки btnDeleteRecord
        function btnGetWeight() {
            if (document.getElementById(prefix + "txbWeight").value != "") {
                if (confirm('Поле массы уже заполнено. Нажмите ОК для замены значения массой по теории.')) {
                    __doPostBack('btnGetWeight', '');
                }
            }
            else __doPostBack('btnGetWeight', '');
        }
    </script>

</asp:Content>


<asp:Content ID="Content2" ContentPlaceHolderID="MainPlaceHolder" runat="Server">
    <asp:Panel ID="Panel1" runat="server"
        Style="padding-bottom: 8px; border-bottom: gray 1px solid; padding-top: 12px; padding-right: 8px; padding-left: 8px;"
        BackColor="#E0E0E0" Width="1200px">
        Номер инспекционной решетки&nbsp;<asp:DropDownList
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
            <asp:ListItem Value="85">Установка по зачистке внутренней поверхности</asp:ListItem>
        </asp:DropDownList>
        &nbsp;Смена&nbsp;<asp:Label ID="lblShift" runat="server" BackColor="White"
            BorderColor="Gray" BorderStyle="Solid" BorderWidth="1px" Height="20px" Style="padding-right: 4px; padding-left: 4px; text-align: center"
            Width="49px"></asp:Label>
    </asp:Panel>
    <asp:Label ID="lblEnterDataMsg" runat="server" Font-Size="10pt"
        Text="<br/>&nbsp;&nbsp;Для начала работы необходимо указать номер инспекции"></asp:Label>
    <asp:Panel ID="pnlTabs" runat="server" Style="border-top-width: 3px; border-left-width: 3px; border-left-color: aqua; border-top-color: aqua; padding-top: 1px; border-right-width: 3px; border-right-color: aqua; margin-top: 16px; border-bottom-width: 3px; border-bottom-color: silver;"
        Width="1200px" BackColor="#E0E0E0">
        <table cellspacing="0" height="24" style="display: inline; width: 100%;">
            <tr>
                <td runat="server" style="width: 4px; border-bottom: gray 1px solid; text-align: center; padding-bottom: 4px; padding-top: 4px;">&nbsp;</td>
                <td id="tdInputDataTab" runat="server" style="padding-right: 8px; padding-left: 8px; width: 100px; text-align: center; padding-bottom: 4px; padding-top: 4px; border-right: gray 1px solid; border-top: gray 1px solid; border-bottom-width: 1px; border-bottom-color: gray; border-left: gray 1px solid;"
                    bgcolor="#ffffff">
                    <asp:LinkButton ID="btnInputDataTab" runat="server" Font-Size="11pt" ForeColor="Black"
                        OnClick="btnSetInputDataTab_Click" Style="text-decoration: none">Ввод данных</asp:LinkButton></td>
                <td id="tdEditDataTab" runat="server" style="padding-right: 8px; padding-left: 8px; width: 100px; text-align: center; padding-bottom: 4px; padding-top: 4px; border-bottom: gray 1px solid;" bgcolor="#e0e0e0">
                    <asp:LinkButton ID="btnEditDataTab" runat="server" Font-Size="11pt" Font-Underline="False"
                        ForeColor="Gray" OnClick="btnEditDataTab_Click" Style="text-decoration: none">Исправление</asp:LinkButton></td>
                <td runat="server" style="padding-right: 8px; padding-left: 8px; border-bottom: gray 1px solid; padding-bottom: 4px; padding-top: 4px;">&nbsp;</td>
            </tr>
        </table>
    </asp:Panel>
    <asp:Panel ID="pnlMainContentPanel" runat="server" Style="padding-right: 8px; padding-left: 8px; padding-bottom: 8px; padding-top: 8px; border-left-width: 1px; border-left-color: gray; border-bottom-width: 1px; border-bottom-color: gray; border-right-width: 1px; border-right-color: gray;" Width="1200px">
        <asp:MultiView ID="MainMultiView" runat="server" ActiveViewIndex="0">
            <asp:View ID="FindPipesView" runat="server">
                <br />
                <table id="pnlFindPipe" runat="server">
                    <tr>
                        <td style="vertical-align: middle; text-align: left">Год</td>
                        <td style="vertical-align: middle; width: 7px; text-align: left"></td>
                        <td style="vertical-align: middle; text-align: left">№&nbsp;Трубы</td>
                        <td style="vertical-align: middle; width: 15px; text-align: left"></td>
                        <td style="text-align: center">
                            <asp:LinkButton ID="lblHelpCheck" runat="server" OnClick="lblHelpCheck_Click">(?)</asp:LinkButton></td>
                        <td align="center" style="width: 15px"></td>
                        <td align="center"></td>
                        <td align="center"></td>
                        <td align="center"></td>
                        <td align="center"></td>
                        <td align="center">&nbsp;</td>
                    </tr>
                    <tr>
                        <td style="text-align: left">
                            <asp:TextBox ID="txbYear" runat="server" MaxLength="2" Width="25px"
                                Font-Bold="True" TabIndex="10" Font-Size="12pt"></asp:TextBox></td>
                        <td style="width: 7px; text-align: left"></td>
                        <td>
                            <asp:TextBox ID="txbPipeNumber" runat="server" MaxLength="6" TabIndex="11"
                                Width="90px" Font-Bold="True" Font-Size="12pt"></asp:TextBox></td>
                        <td style="width: 15px; text-align: center; font-size: 13pt; font-weight: bold;">-</td>
                        <td>
                            <asp:TextBox ID="txbCheck" runat="server" MaxLength="1" TabIndex="12"
                                Width="25px" Font-Bold="True" Font-Size="12pt"></asp:TextBox></td>
                        <td style="width: 15px"></td>
                        <td>
                            <asp:Button ID="btnOk" runat="server" Height="26px" OnClick="btnOk_Click" TabIndex="13"
                                Text="Ввести данные" Width="140px" OnClientClick="return CheckFindInputs(false)" />
                        </td>
                        <td style="padding-left: 8px">
                            <asp:Button ID="btnPrintLabel2" runat="server" Height="26px" OnClick="btnPrintLabel2_Click" TabIndex="14"
                                Text="Напечатать бирку" Width="140px" OnClientClick="return CheckFindInputs(false)" /></td>
                        <td style="padding-left: 8px">
                            <asp:Button ID="btnGeometryInput" runat="server" Height="26px" TabIndex="13"
                                Text="Ввести геометрические замеры" Width="213px" OnClientClick="return GeometryInput(this);" /></td>
                        <td style="padding-left: 32px">
                            <asp:Button ID="btnShowHistory" runat="server" Height="26px" OnClick="btnShowHistory_Click" TabIndex="14"
                                Text="История трубы" Width="140px" OnClientClick="return CheckFindInputs(false)" /></td>
                        <td style="padding-left: 32px">
                            <asp:Button ID="btnTransportNumber" runat="server" Height="26px" OnClick="btnTransportNumber_Click" OnClientClick="return PostBackByButton(this);" TabIndex="14" Text="Труба без номера" Width="140px" />
                        </td>
                    </tr>
                    <tr>
                        <td style="vertical-align: middle; text-align: center">
                            <asp:CustomValidator ID="cvdYear" runat="server" ClientValidationFunction="ValidateNumberInt"
                                ControlToValidate="txbYear" ErrorMessage="! ! !" Font-Size="Smaller"></asp:CustomValidator></td>
                        <td style="vertical-align: middle; width: 7px; text-align: left"></td>
                        <td style="vertical-align: middle; text-align: center">
                            <asp:CustomValidator ID="cvdPipeNumber" runat="server" ClientValidationFunction="ValidateNumberInt"
                                ControlToValidate="txbPipeNumber" ErrorMessage="Ошибка" Font-Size="Smaller"></asp:CustomValidator></td>
                        <td style="vertical-align: middle; width: 15px; text-align: center"></td>
                        <td style="vertical-align: middle; text-align: center">
                            <asp:CustomValidator ID="cvdCheck" runat="server" ClientValidationFunction="ValidateNumberInt"
                                ControlToValidate="txbCheck" ErrorMessage="! ! !" Font-Size="Smaller"></asp:CustomValidator></td>
                        <td style="width: 15px"></td>
                        <td colspan="2"></td>
                        <td></td>
                        <td></td>
                        <td>&nbsp;</td>
                    </tr>
                </table>
                    <asp:CheckBox ID="cbAutoCampaign" OnCheckedChanged="cbAutoCampaign_OnCheckedChanged"  onchange="check_AutoCampaign(event);" runat="server" Text="Выбирать кампанию по последней осмотренной трубе" 
                                  style="font-size: 10pt; margin-left: 5px" AutoPostBack="True" Checked="False"/>
                <table>
                    <asp:Timer ID="Timer1" runat="server" OnTick="Timer_Tick1" Enabled="true" Interval="3000"/>
                    <tr>
                        <td>
                            <asp:Button ID="btnPipeNum" runat="server" Height="26px" OnClick="btnPipeNum_Click" Style="margin-left: 5px" TabIndex="13" Text="Получить номер трубы со сканера" Width="250px" />
                        </td>
                        <td>
                            <asp:UpdatePanel ID="UpdatePanel4" runat="server" RenderMode="Inline" UpdateMode="Conditional">
                                
                                <ContentTemplate>
                                    &nbsp;<asp:Label ID="lblLastScanned" Style="margin-left: 15px; font-size: 13px" runat="server"/>
                                </ContentTemplate>
                                <triggers><asp:AsyncPostBackTrigger ControlID="Timer1" /></triggers>
                            </asp:UpdatePanel>
                        </td>
                    </tr>
                </table>
                <asp:Panel runat="server" ID="pnlPipesQueue" Style="padding: 8px">

                    <asp:Table ID="tblPipesQueue" runat="server" CellPadding="0" CellSpacing="0" EnableViewState="False" Font-Size="10pt" GridLines="Both" OnPreRender="tblPipesQueue_PreRender" Width="300px" BorderColor="Black" BorderStyle="Solid" BorderWidth="1px">
                        <asp:TableRow runat="server" BackColor="#E0E0E0" HorizontalAlign="Center" Font-Bold="True" Height="32px">
                            <asp:TableCell runat="server" Width="32px">#</asp:TableCell>
                            <asp:TableCell runat="server">Быстрый выбор номеров труб</asp:TableCell>
                        </asp:TableRow>
                    </asp:Table>

                    <br />
                    <asp:Button ID="btnClearPipesQueue" runat="server" OnClientClick="return confirm('Удалить отмеченные номера труб из очереди?');" Text="Удалить отмеченные" OnClick="btnClearPipesQueue_Click" />

                </asp:Panel>
            </asp:View>
            <asp:View runat="server" ID="vControlDigit">


                <table>
                    <tr>
                        <td>Чтобы раcсчитать контрольную цифру для №<asp:Label ID="lblNumtrub" runat="server"></asp:Label>
                            ввдедите учетную запись и пароль:<br />
                            <br />
                            <table>
                                <tr>
                                    <td style="width: 135px">Учетная запись </td>
                                    <td style="width: 158px">
                                        <asp:TextBox ID="txbLogin" runat="server" Font-Bold="True" TabIndex="20" Width="184px"></asp:TextBox>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="width: 135px">Пароль </td>
                                    <td style="width: 158px">
                                        <asp:TextBox ID="txbPassword" runat="server" Font-Bold="True" TabIndex="21" TextMode="Password" Width="184px"></asp:TextBox>
                                    </td>
                                </tr>
                                <tr>
                                    <td colspan="2"></td>
                                </tr>
                                <tr>
                                    <td colspan="2" style="text-align: right">
                                        <asp:Button ID="btnLogin" runat="server" Height="26px" OnClick="btnLogin_Click" TabIndex="22" Text="ОК" Width="70px" />
                                        &nbsp;
                                        <asp:Button ID="btnCansel" runat="server" Height="26px" OnClick="btnCansel_Click" TabIndex="23" Text="Отмена" Width="70px" />
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>

            </asp:View>
            <asp:View ID="FindForEditView" runat="server">
                <table style="font-size: 10pt" cellpadding="2" cellspacing="0">
                    <tr>
                        <td style="height: 38px">№ партии</td>
                        <td style="padding-left: 4px; height: 38px">Год и № трубы</td>
                        <td style="padding-left: 24px; height: 38px">ФИО оператора</td>
                        <td style="padding-right: 24px; height: 38px">Рабочее место</td>
                        <td bgcolor="#e0e0e0" style="padding-left: 4px; height: 38px"></td>
                        <td bgcolor="#e0e0e0" style="padding-left: 4px; height: 38px" colspan="3">Дата ввода данных</td>
                        <td bgcolor="#e0e0e0" style="padding-left: 4px; width: 30px; height: 38px">Смена</td>
                        <td style="padding-left: 4px; width: 30px; height: 38px"></td>
                    </tr>
                    <tr>
                        <td>
                            <asp:TextBox ID="txbPartNo2" runat="server" Width="72px" Style="font-weight: bold" MaxLength="5"></asp:TextBox></td>
                        <td style="padding-left: 4px">
                            <asp:TextBox ID="txbYear2" runat="server" Width="34px" Style="font-weight: bold" MaxLength="2"></asp:TextBox>
                            <asp:TextBox ID="txbPipeNumber2" runat="server" Width="80px" Style="font-weight: bold" MaxLength="6"></asp:TextBox></td>
                        <td style="padding-left: 24px">
                            <asp:DropDownList ID="ddlOperatorFIO2" runat="server" Width="239px" Font-Bold="True" TabIndex="1">
                                <asp:ListItem></asp:ListItem>
                                <asp:ListItem>Оператор 1</asp:ListItem>
                                <asp:ListItem>Оператор 2</asp:ListItem>
                            </asp:DropDownList></td>
                        <td style="padding-right: 24px; text-align: left">
                            <asp:DropDownList
                                ID="ddlWorkPlace2" runat="server" Font-Bold="True" TabIndex="3">
                                <asp:ListItem Value="-1">(все)</asp:ListItem>
                                <asp:ListItem Value="0">ПДО</asp:ListItem>
                                <asp:ListItem Value="1">Инсп. 1</asp:ListItem>
                                <asp:ListItem Value="2">Инсп. 2</asp:ListItem>
                                <asp:ListItem Value="3">Инсп. 3</asp:ListItem>
                                <asp:ListItem Value="4">Инсп. 4</asp:ListItem>
                                <asp:ListItem Value="5">Инсп. 5</asp:ListItem>
                                <asp:ListItem Value="6">Инсп. 6</asp:ListItem>
                                <asp:ListItem Value="80">Инсп. 7</asp:ListItem>
                                <asp:ListItem Value="81">Инсп. 8</asp:ListItem>
                                <asp:ListItem Value="82">Инсп. 9</asp:ListItem>
                                <asp:ListItem Value="83">Инсп. 10</asp:ListItem>
                                <asp:ListItem Value="84">Инсп. 11</asp:ListItem>
                                <asp:ListItem Value="63">Mair</asp:ListItem>
                                <asp:ListItem Value="7">Ремонт</asp:ListItem>
                                <asp:ListItem Value="85">Зачистка</asp:ListItem>
                            </asp:DropDownList></td>
                        <td bgcolor="#e0e0e0" style="padding-left: 4px; text-align: left">От</td>
                        <td bgcolor="#e0e0e0">
                            <uc3:CalendarControl ID="cldStartDate" runat="server" DateOnly="True" />
                        </td>
                        <td bgcolor="#e0e0e0" style="padding-left: 8px">До</td>
                        <td bgcolor="#e0e0e0">
                            <uc3:CalendarControl ID="cldEndDate" runat="server" DateOnly="True" />
                        </td>
                        <td bgcolor="#e0e0e0" style="padding-right: 4px; padding-left: 8px; width: 30px">
                            <asp:DropDownList ID="ddlShift2" runat="server" Width="67px" Font-Bold="True" TabIndex="2">
                                <asp:ListItem></asp:ListItem>
                                <asp:ListItem>А</asp:ListItem>
                                <asp:ListItem>Б</asp:ListItem>
                                <asp:ListItem>В</asp:ListItem>
                                <asp:ListItem>Г</asp:ListItem>
                            </asp:DropDownList></td>
                        <td style="padding-right: 4px; padding-left: 12px; width: 30px">
                            <asp:Button ID="btnFindForEdit" runat="server" Height="26px" OnClick="btnFindForEdit_Click"
                                Text="Найти" Width="68px" /></td>
                    </tr>
                </table>
                <br />
                <asp:Panel ID="pnlFindForEditRecords" runat="server" BorderStyle="Solid" BorderWidth="1px"
                    Height="82px" ScrollBars="Both" Width="1178px" BorderColor="Silver">
                    <asp:Label ID="lblNoDataForEdit" runat="server" Text="<br/>Нет записей для редактирования" ForeColor="Gray"></asp:Label>
                    <asp:Table
                        ID="tblEditRecordsList" runat="server" BorderColor="Gray" BorderStyle="Solid"
                        BorderWidth="1px" CellPadding="2" Font-Size="8pt" GridLines="Both" OnPreRender="tblEditRecordsList_PreRender"
                        Style="border-collapse: collapse" Visible="False" Width="1200px"
                        EnableViewState="False">
                        <asp:TableRow runat="server" BackColor="#E0E0E0" Font-Bold="True" HorizontalAlign="Center">
                            <asp:TableCell runat="server">Дата</asp:TableCell>
                            <asp:TableCell runat="server">Инспекция</asp:TableCell>
                            <asp:TableCell runat="server">Смена</asp:TableCell>
                            <asp:TableCell runat="server">№ партии</asp:TableCell>
                            <asp:TableCell runat="server">Год и<br/>№ трубы</asp:TableCell>
                            <asp:TableCell runat="server">Сортамент<br/><img src="Images/CX.png" /></asp:TableCell>
                            <asp:TableCell runat="server">Марка стали<br/><img src="Images/CX.png" /></asp:TableCell>
                            <asp:TableCell runat="server">НД</asp:TableCell>
                            <asp:TableCell runat="server">Длина, м</asp:TableCell>
                            <asp:TableCell runat="server">Масса, кг</asp:TableCell>
                            <asp:TableCell runat="server">Направление</asp:TableCell>
                            <asp:TableCell runat="server">№ заказа</asp:TableCell>
                            <asp:TableCell runat="server">Строка<br/>заказа</asp:TableCell>
                            <asp:TableCell runat="server">Номенклатурный<br/>номер</asp:TableCell>
                            <asp:TableCell runat="server">ФИО<br/>оператора</asp:TableCell>
                        </asp:TableRow>
                    </asp:Table>
                </asp:Panel>
                <br />
                <div style="width: 1177px; text-align: right;">
                    <asp:Button ID="btnShowHistory2" runat="server" Height="26px" Text="История трубы" OnClientClick="return bthShowHistory2Click();" />&nbsp;&nbsp;
                &nbsp;&nbsp; &nbsp;<asp:Button ID="btnEditRecord" runat="server" Height="26px" Text="Изменить" OnClientClick="return btnEditClick()" />&nbsp;
                <asp:Button ID="btnDeleteRecord" runat="server" Height="26px" Text="Удалить" OnClientClick="return btnDeleteClick();" />
                </div>
                <br />
            </asp:View>
            <asp:View ID="PipeHistoryView" runat="server">
                <strong>
                    <br />
                    История трубы<br />
                    <br />
                    <asp:Table ID="tblPipeHistory" runat="server" Style="border-collapse: collapse; font-size: 10pt;" BorderColor="Gray" BorderStyle="Solid" BorderWidth="1px" CellPadding="4" Font-Size="11pt" GridLines="Both">
                        <asp:TableRow runat="server" BackColor="#E0E0E0" HorizontalAlign="Center" TableSection="TableHeader">
                            <asp:TableCell runat="server">Участок</asp:TableCell>
                            <asp:TableCell runat="server">Дата</asp:TableCell>
                            <asp:TableCell runat="server">Направление</asp:TableCell>
                            <asp:TableCell runat="server">Примечание</asp:TableCell>
                            <asp:TableCell runat="server">Оператор</asp:TableCell>
                        </asp:TableRow>
                    </asp:Table>
                    <br />
                    <br />
                    <asp:Button ID="btnHistoryOk" runat="server" Height="26px" OnClick="btnOk_Click"
                        Text="Ввести данные" TabIndex="25" />&nbsp;
                <asp:Button ID="btnHistoryCancel" runat="server" Height="26px" OnClick="btnNoPipeBack_Click"
                    Text="Отмена" TabIndex="25" /><br />
                    <br />
                </strong>
            </asp:View>
            <asp:View ID="InputDataView" runat="server">
                <div>
                    <asp:BulletedList ID="lstWarningsTop" runat="server" Font-Size="11pt" ForeColor="#FF3300">
                        <asp:ListItem>Сообщение 1</asp:ListItem>
                        <asp:ListItem>Сообщение 2</asp:ListItem>
                        <asp:ListItem>Сообщение 3</asp:ListItem>
                    </asp:BulletedList>
                </div>
                <asp:Panel ID="Panel6" runat="server" Style="padding-left: 8px">
                    <table style="font-size: 10pt">
                        <tr>
                            <td>
                                <strong>Данные трубы для учета</strong><br />
                            </td>
                            <td>&nbsp;</td>
                            <td>
                                <asp:UpdatePanel ID="UpdatePanel6" runat="server">
                                    <ContentTemplate>
                                        <asp:Panel ID="pnlRecordDate" runat="server" Width="503px">
                                            <table style="font-size: 10pt">
                                                <tr>
                                                    <td style="width: 91px">Учётная дата</td>
                                                    <td style="padding-left: 12px;" id="tdDateInput" runat="server">
                                                        <uc3:CalendarControl ID="cldDate" runat="server" DateOnly="False" />
                                                    </td>
                                                </tr>
                                            </table>
                                        </asp:Panel>
                                    </ContentTemplate>
                                </asp:UpdatePanel>
                            </td>
                        </tr>
                    </table>
                    <table style="font-size: 10pt">
                        <tr>
                            <td style="width: 169px; text-align: right;">Год и номер трубы:</td>
                            <td style="width: 90px">
                                <asp:Label ID="lblYear" runat="server" Font-Size="10pt" Style="border-right: gray 1px solid; border-top: gray 1px solid; border-left: gray 1px solid; border-bottom: gray 1px solid; height: 20px; padding-right: 2px; padding-left: 2px; display: inline;">00</asp:Label>
                                <asp:Label ID="lblPipeNo" runat="server" Font-Size="10pt" Style="border-right: gray 1px solid; padding-right: 4px; border-top: gray 1px solid; display: inline; padding-left: 4px; border-left: gray 1px solid; border-bottom: gray 1px solid; height: 20px">000000</asp:Label>
                            </td>
                            <td style="padding-left: 12px; padding-right: 4px; width: 166px; text-align: right;">Масса, кг:</td>
                            <td style="width: 93px">
                                <asp:TextBox ID="txbWeight" runat="server" AutoPostBack="True" OnSelectedIndexChanged="ddlCampaign_SelectedIndexChanged" Font-Bold="True" MaxLength="7" Style="font-weight: bold" TabIndex="34" Width="83px"></asp:TextBox>
                            </td>
                            <td style="vertical-align: top; width: 90px">
                                <asp:Button ID="btnGetWeightT" runat="server" Height="22px" TabIndex="36" Text="По теории" Width="83px" OnClientClick="return btnGetWeight();" OnClick="btnGetWeightT_Click" />
                            </td>
                            <td>
                                <asp:Label ID="lblWeight" runat="server" Font-Size="11pt"></asp:Label>
                            </td>
                            <td style="vertical-align: top; width: 90px">
                                <asp:Button ID="btnRefreshWeight" runat="server" Height="22px" OnClick="btnRefreshWeight_Click" TabIndex="36" Text="Обновить" Width="83px" />
                            </td>

                        </tr>
                        <tr>
                            <td style="width: 169px; text-align: right;">Номер плавки:</td>
                            <td style="width: 90px">
                                <asp:TextBox ID="txbSmelting" runat="server" Font-Bold="True" MaxLength="15" Width="85px" TabIndex="31"></asp:TextBox>
                            </td>
                            <td style="padding-left: 12px; width: 166px; text-align: right;">Длина, мм:</td>
                            <td style="width: 93px">
                                <asp:TextBox ID="txbLength" runat="server" AutoPostBack="True" Font-Bold="True" MaxLength="5" OnTextChanged="ddlNTD_SelectedIndexChanged" Style="font-weight: bold" TabIndex="35" Width="83px"></asp:TextBox>
                            </td>
                            <td>&nbsp;</td>
                            <td runat="server" style="vertical-align: top;">
                                <asp:Label ID="lblWeightTemp" runat="server" Visible="False"></asp:Label></td>
                            <td runat="server" style="vertical-align: top;">&nbsp;</td>
                        </tr>
                        <tr runat="server">
                            <td style="width: 169px; font-size: 10pt; text-align: right; height: 26px;" />
                            <td style="width: 90px; font-size: 10pt; height: 26px;" />
                            <td style="padding-left: 12px; width: 166px; text-align: right; height: 26px;">Длина с МРТ1420:</td>
                            <td style="width: 93px; height: 26px;">
                                <asp:Label ID="lblMrt1420Length" runat="server" Font-Size="11pt">00000</asp:Label>
                            </td>
                            <td style="height: 26px" />
                            <td style="vertical-align: top; height: 26px;" />
                            <td style="vertical-align: top; height: 26px;" />
                        </tr>
                        <tr runat="server">
                            <td style="width: 169px; font-size: 10pt; text-align: right; height: 26px;">Год и номер партии:</td>
                            <td style="width: 90px; font-size: 10pt; height: 26px;">
                                <asp:TextBox ID="txbPartYear" runat="server" Font-Bold="True" MaxLength="5" Width="28px" Style="font-weight: bold" TabIndex="32"></asp:TextBox>
                                <asp:TextBox ID="txbPartNo" runat="server" Font-Bold="True" MaxLength="5" Style="font-weight: bold" TabIndex="33" Width="54px"></asp:TextBox>
                            </td>
                            <td style="padding-left: 12px; width: 166px; text-align: right; height: 26px;">Длина по УЗК шва:</td>
                            <td style="width: 93px; height: 26px;">
                                <asp:Label ID="lblUscLength" runat="server" Font-Size="11pt">00000</asp:Label>
                            </td>
                            <td style="height: 26px">
                                <asp:Label ID="lblObrlenght" runat="server" Visible="false"></asp:Label></td>
                            <td style="vertical-align: top; height: 26px;"></td>
                            <td style="vertical-align: top; height: 26px;"></td>
                        </tr>
                        <tr runat="server">
                            <td style="width: 169px; font-size: 10pt; text-align: right;">Партия на участке сварки:</td>
                            <td style="width: 90px; font-size: 10pt;">
                                <asp:Label ID="lblPartStan" runat="server" Font-Size="10pt" Style="border-right: gray 1px solid; border-top: gray 1px solid; border-left: gray 1px solid; border-bottom: gray 1px solid; height: 20px; padding-right: 2px; padding-left: 2px; display: inline;"
                                    Width="85px">0000</asp:Label>
                            </td>
                            <td style="padding-left: 12px; width: 166px; text-align: right;">Длина по измерителю:</td>
                            <td style="width: 93px">
                                <asp:Label ID="lblIzmLength" runat="server" Font-Bold="False" Font-Size="11pt">00000</asp:Label>
                            </td>
                            <td>
                                <asp:Label ID="lblCount_Return" runat="server" Visible="false"></asp:Label></td>
                            <td style="vertical-align: top;">&nbsp;</td>
                            <td style="vertical-align: top;">&nbsp;</td>
                        </tr>
                        <tr runat="server">
                            <td style="width: 169px; font-size: 10pt; text-align: right;">Партия термообработки:</td>
                            <td style="width: 90px; font-size: 10pt;">
                                <asp:Label ID="lblPartOto" runat="server" Font-Size="10pt" Style="border-right: gray 1px solid; border-top: gray 1px solid; border-left: gray 1px solid; border-bottom: gray 1px solid; height: 20px; padding-right: 2px; padding-left: 2px; display: inline;"
                                    Width="85px">0000</asp:Label>
                            </td>
                            <td style="padding-left: 12px; width: 166px; text-align: right;">Длина измеренная по ТОС:</td>
                            <td style="vertical-align: top; width: 93px">
                                <asp:Label ID="lblTosLength" runat="server" Font-Bold="False" Font-Size="11pt">00000</asp:Label>
                            </td>
                            <td>&nbsp;</td>
                            <td style="vertical-align: top;">&nbsp;</td>
                            <td style="vertical-align: top;">&nbsp;</td>
                        </tr>
                        <tr runat="server">
                            <td style="width: 169px; font-size: 10pt; text-align: right;"></td>
                            <td style="width: 90px; font-size: 10pt;"></td>
                            <td style="padding-left: 12px; width: 166px; text-align: right;">Данные трубы для учета:</td>
                            <td style="vertical-align: top; width: 93px">
                                <asp:Label ID="lblDivLenght" runat="server" Font-Bold="False" Font-Size="11pt">00000</asp:Label>
                            </td>
                            <td>
                                <asp:Label ID="lblhydLenght" runat="server" Visible="false"></asp:Label></td>
                            <td style="vertical-align: top;">&nbsp;</td>
                            <td style="vertical-align: top;">&nbsp;</td>
                        </tr>
                        <tr runat="server">
                            <td colspan="6" style="font-size: 10pt; text-align: left;">
                                <br />
                                <asp:Label ID="lblNomenclatureByCampaign" runat="server" Style="text-align: left">Данные из задания на кампанию: нет</asp:Label>
                            </td>
                        </tr>
                    </table>

                    <br />

                    <strong>Данные заказа</strong>&nbsp;&nbsp;<asp:CheckBox ID="cbShowCampaigns"
                        runat="server" AutoPostBack="True" Font-Size="10pt"
                        Text="Получать из строки задания на кампанию"
                        OnCheckedChanged="cbShowCampaigns_CheckedChanged" Checked="True" />
				
                    <asp:UpdatePanel ID="UpdatePanel2" runat="server" RenderMode="Inline"
                        UpdateMode="Conditional" OnUnload="UpdatePanel1_Unload">
                        <ContentTemplate>

							

                            <table style="margin-top: 8px; margin-bottom: 8px;" id="tblCampaign"
                                runat="server">
                                <tr>
                                    <td  bgcolor="#e0e0e0">Строка задания на кампанию</td>
                                    <td><asp:Label ID="lbCaptionResidual" runat="server" Font-Bold="False" Font-Size="11pt">Осталось до выполнения, шт(тн)</asp:Label></td>
                                </tr>
                                <tr>
                                    <td>
                                        <asp:DropDownList ID="ddlCampaign" runat="server" AutoPostBack="True"
                                            Font-Size="9pt" OnSelectedIndexChanged="ddlCampaign_SelectedIndexChanged"
                                            Style="font-family: Courier New; margin-right: 0px;" Width="925px">
                                            <asp:ListItem></asp:ListItem>
                                        </asp:DropDownList>
                                    </td>
                                    <td><asp:Label ID="lbResidual" runat="server" Font-Bold="true" Font-Size="11pt">0 (0)</asp:Label></td>
                                </tr>
                            </table>

							
							
							<table id="tblOrderInfo" runat="server" style="font-size: 11pt; margin-top: 8px;">
								<tr>
									<td bgcolor="#e0e0e0" style="padding-left: 4px; width: 79px">№ заказа</td>
									<td bgcolor="#e0e0e0" style="padding-left: 4px; width: 103px">Строка заказа</td>
									<td bgcolor="#e0e0e0" style="padding-left: 4px">Номенклатурная позиция</td>
								</tr>
								<tr>
									<td style="width: 79px">
										<asp:TextBox ID="txbZakazNo" runat="server" MaxLength="15" Style="font-weight: bold" TabIndex="40" Width="86px"></asp:TextBox>
									</td>
									<td style="width: 103px">
										<asp:TextBox ID="txbZakazLine" runat="server" MaxLength="5" Style="font-weight: bold" TabIndex="41" Width="106px"></asp:TextBox>
									</td>
									<td>
										<div style="width: 359px">
											<asp:TextBox ID="txbInventoryNumber" runat="server" __designer:wfdid="w31" AutoPostBack="True" OnTextChanged="txbInventoryNumber_TextChanged" Style="font-weight: bold" TabIndex="42" Visible="False" Width="237px"></asp:TextBox>
											<asp:DropDownList ID="ddlInventoryNumber" runat="server" __designer:wfdid="w32" AutoPostBack="True" OnSelectedIndexChanged="ddlInventoryNumber_SelectedIndexChanged" Style="font-weight: bold" TabIndex="43" Width="237px">
											</asp:DropDownList>
											<asp:CheckBox ID="cbOracleList" runat="server" __designer:wfdid="w33" AutoPostBack="True" Checked="True" OnCheckedChanged="cbOracleList_CheckedChanged" Style="font-size: 10pt" Text="Список" />
											<asp:HiddenField ID="hfInventoryNumberKP" runat="server" />
										</div>
									</td>
								</tr>
							</table>


							
                        </ContentTemplate>
                        <Triggers><asp:AsyncPostBackTrigger ControlID="ddlDiam"
                                EventName="SelectedIndexChanged" /><asp:AsyncPostBackTrigger ControlID="ddlThickness"
                                EventName="SelectedIndexChanged" /><asp:AsyncPostBackTrigger ControlID="ddlSteelmark"
                                EventName="SelectedIndexChanged" /><asp:AsyncPostBackTrigger ControlID="ddlNTD" EventName="SelectedIndexChanged" /><asp:AsyncPostBackTrigger ControlID="ddlInventoryNumber"
                                EventName="SelectedIndexChanged" /><asp:AsyncPostBackTrigger ControlID="txbInventoryNumber"
                                EventName="TextChanged" /><asp:AsyncPostBackTrigger ControlID="btnClearSortamentFlds" EventName="Click" /></Triggers>
                    </asp:UpdatePanel>
						
                    <asp:UpdatePanel ID="UpdatePanel1" runat="server" UpdateMode="Conditional" OnUnload="UpdatePanel1_Unload">
                        <ContentTemplate>
                            <table runat="server" id="tblSelectSortament" style="font-size: 10pt;">
                                <tr>
                                    <td bgcolor="#e0e0e0">
                                        <img src="Images/CX.png" style="width: 29px; height: 29px" /></td>
                                    <td bgcolor="#e0e0e0">Диаметр</td>
                                    <td bgcolor="#e0e0e0">
                                        <img src="Images/CX.png" style="width: 29px; height: 29px" /></td>
                                    <td bgcolor="#e0e0e0">Типоразмер профиля</td>
                                    <td bgcolor="#e0e0e0">
                                        <img src="Images/CX.png" style="width: 29px; height: 29px" /></td>
                                    <td bgcolor="#e0e0e0">Стенка</td>
                                    <td bgcolor="#e0e0e0">
                                        <img src="Images/CX.png" style="width: 29px; height: 29px" /></td>
                                    <td bgcolor="#e0e0e0" style="width: 92px">Марка стали</td>
                                    <td></td>
                                    <td bgcolor="#e0e0e0">Наименование НД:</td>
                                    <td bgcolor="#e0e0e0">Код НД:</td>
                                    <td style="font-weight: bold; vertical-align: bottom">&nbsp;</td>
                                </tr>
                                <tr>
                                    <td colspan="2" style="vertical-align: top">
                                        <asp:DropDownList ID="ddlDiam" runat="server" AutoPostBack="True" Font-Bold="True" OnSelectedIndexChanged="ddlNTD_SelectedIndexChanged" TabIndex="44" Width="90px"></asp:DropDownList></td>
                                    <td colspan="2" style="vertical-align: top">
                                        <asp:DropDownList ID="ddlProfileSize" runat="server" AutoPostBack="True" Font-Bold="True" OnSelectedIndexChanged="ddlNTD_SelectedIndexChanged" TabIndex="45" Width="160px"></asp:DropDownList></td>
                                    <td colspan="2" style="vertical-align: top">
                                        <asp:DropDownList ID="ddlThickness" runat="server" AutoPostBack="True" Font-Bold="True" OnSelectedIndexChanged="ddlNTD_SelectedIndexChanged" TabIndex="46" Width="95px"></asp:DropDownList></td>
                                    <td colspan="2" style="vertical-align: top">
                                        <asp:DropDownList ID="ddlSteelmark" runat="server" AutoPostBack="True" Font-Bold="True" OnSelectedIndexChanged="ddlNTD_SelectedIndexChanged" TabIndex="47" Width="131px"></asp:DropDownList></td>
                                    <td></td>
                                    <td style="font-weight: bold; vertical-align: bottom">
                                        <asp:DropDownList ID="ddlNTD" runat="server" AutoPostBack="True" Font-Bold="True" OnSelectedIndexChanged="ddlNTD_SelectedIndexChanged" TabIndex="48" Width="230px"></asp:DropDownList></td>
                                    <td style="vertical-align: bottom">
                                        <asp:TextBox ID="txbNTD" runat="server" AutoPostBack="True" MaxLength="3" Style="font-weight: bold" TabIndex="49" Width="57px"></asp:TextBox></td>
                                    <td style="font-weight: bold; vertical-align: bottom">
                                        <asp:ImageButton ID="btnClearSortamentFlds" runat="server" BorderColor="Silver" BorderStyle="Outset" BorderWidth="1px" ImageUrl="~/Images/Delete16x16.png" OnClick="btnClearSortamentFlds_Click" Style="padding-right: 2px; padding-left: 2px; padding-bottom: 2px; margin: 2px; padding-top: 2px" /></td>
                                </tr>
                            </table>
							
                            <table runat="server" id="tblSelectRoute" style="font-size: 10pt">
                                <tr style="background-color: #e0e0e0">
                                    <td><strong>Изменение маршрута:</strong></td>
                                </tr>
                                <tr>
                                    <td>
                                        <asp:DropDownList ID="ddlPipeRouteMap" runat="server" Width="640px" Font-Bold="True">
                                        </asp:DropDownList>
                                    </td>
                                </tr>
                            </table>

                            <table style="font-size: 10pt;">
                                <tr>
                                    <td style="padding-left: 4px; vertical-align: top">
                                        <asp:Panel ID="pnlInventoryNumberNotFound" runat="server" __designer:wfdid="w26" Style="border: 1px solid navy; padding-left: 2px; padding-bottom: 2px; padding-top: 2px;" Wrap="False">
                                            <table>
                                                <tr>
                                                    <td style="vertical-align: top">
                                                        <img src="Images/warning32x32.gif" />
                                                    </td>
                                                    <td style="padding-left: 12px; padding-right: 12px; font-size: 10pt">
                                                        <asp:Label ID="lblInventoryNumberNotFound" runat="server" __designer:wfdid="w27"></asp:Label>
                                                    </td>
                                                </tr>
                                            </table>
                                        </asp:Panel>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding-left: 4px; vertical-align: top">
                                        <asp:Panel ID="pnlTestResult" runat="server" __designer:wfdid="w26" Style="border: 1px solid navy; padding-left: 2px; padding-bottom: 2px; padding-top: 2px;" Wrap="False">
                                            <table>
                                                <tr>
                                                    <td style="vertical-align: top">
                                                        <img src="Images/GoodResult.png" />
                                                    </td>
                                                    <td style="padding-left: 12px; padding-right: 12px; font-size: 10pt">
                                                        <asp:Label ID="lblTestResult" runat="server" __designer:wfdid="w27"></asp:Label>
                                                    </td>
                                                </tr>
                                            </table>
                                        </asp:Panel>
                                    </td>
                                </tr>
                            </table>
							<table id="tblTemplate" runat="server" style="font-size: 11pt; margin-top: 8px; width: 852px;">
								<tr>
									<td bgcolor="#e0e0e0" style="width: 310px; font-size: 17px;">Шаблонирование</td>
									<td bgcolor="#e0e0e0" style="width: 331px"></td>
								</tr>
								<tr>
									<td bgcolor="#e0e0e0" style="width: 310px">Результат шаблонирования</td>
									<td bgcolor="#e0e0e0" style="width: 331px">Дефект</td>
								</tr>
								<tr>
									<td style="width: 310px">
										<asp:DropDownList ID="ddlResultTemplate" runat="server" AutoPostBack="True" Font-Size="9pt" Style="font-family: Courier New; margin-right: 0px;" Width="300">
											<asp:ListItem></asp:ListItem>
											<asp:ListItem>Пройдено</asp:ListItem>
											<asp:ListItem>Не пройдено</asp:ListItem>
											<asp:ListItem>Не требуется</asp:ListItem>
										</asp:DropDownList>
									</td>
									<td style="width: 331px">
										<asp:DropDownList ID="ddlDefectTemplate" runat="server" AutoPostBack="True" Font-Size="9pt" Style="font-family: Courier New; margin-right: 0px;" Width="300">
											<asp:ListItem></asp:ListItem>
										</asp:DropDownList>
									</td>
									<td>
										
									</td>
								</tr>
							</table>
							<br /><asp:Button ID="ShowInfoAboutTemplating" runat="server" Height="22px" OnClick="ShowInfoAboutTemplating_Click" TabIndex="36" Text="Данные шаблонирования" AutoPostBack="True" Width="246px" />

							<uc2:PopupWindow ID="PopupWindow2" runat="server" Visible ="false"
							ContentPanelId="pnlLotWarning" Title="Ввод дополнительной информации"
							TitleBackColor="Red" />

							<asp:Panel ID="pnlShowInfoTemplating" runat="server" Height="180px" Style="padding: 8px" Visible="false" Width="550px">
		<table style="font-size: 10pt">
			<tr>
				<td style="border: 1px solid black; width: 520px; height: 155px;">
					<asp:TextBox ID="txbInformationTemplate" runat="server" BackColor="#E0E0E0" Height="150px" MaxLength="255" ReadOnly="True" TextMode="MultiLine" Width="518px"></asp:TextBox>
				</td>
			</tr>
			<tr>
				<td style="text-align: right; width: 520px; ">
					<asp:Button ID="btnInfoTemplatingCancel" runat="server" OnClick="btnRouteRejectCancel_Click" OnClientClick="return PostBackByButton(this);" Text="Закрыть" />
				</td>
			</tr>
		</table>
	</asp:Panel>

                        </ContentTemplate>
                        <Triggers><asp:AsyncPostBackTrigger ControlID="ddlDiam" EventName="SelectedIndexChanged"></asp:AsyncPostBackTrigger><asp:AsyncPostBackTrigger ControlID="ddlThickness" EventName="SelectedIndexChanged"></asp:AsyncPostBackTrigger><asp:AsyncPostBackTrigger ControlID="ddlSteelmark" EventName="SelectedIndexChanged"></asp:AsyncPostBackTrigger><asp:AsyncPostBackTrigger ControlID="ddlNTD" EventName="SelectedIndexChanged"></asp:AsyncPostBackTrigger><asp:AsyncPostBackTrigger ControlID="ddlInventoryNumber" EventName="SelectedIndexChanged"></asp:AsyncPostBackTrigger><asp:AsyncPostBackTrigger ControlID="txbInventoryNumber" EventName="TextChanged"></asp:AsyncPostBackTrigger><asp:AsyncPostBackTrigger ControlID="cbOracleList" EventName="CheckedChanged"></asp:AsyncPostBackTrigger><asp:AsyncPostBackTrigger ControlID="txbNTD" EventName="TextChanged"></asp:AsyncPostBackTrigger><asp:AsyncPostBackTrigger ControlID="btnClearSortamentFlds" EventName="Click"></asp:AsyncPostBackTrigger><asp:AsyncPostBackTrigger ControlID="ddlCampaign"
                                EventName="SelectedIndexChanged" /></Triggers>
                    </asp:UpdatePanel>
					
                </asp:Panel>


                <br />
                <hr color="gray" size="1" />
                <table style="font-size: 10pt">
                    <tr>
                        <td>
                            <strong style="padding-right: 20px">Результаты испытаний:</strong></td>
                        <td style="font-weight: bold" id="TD1" runat="server" visible="false">Дефекты трубы на стане:</td>
                    </tr>
                    <tr>
                        <td style="padding-right: 20px; vertical-align: top">
                            <asp:UpdatePanel ID="updResultTest" runat="server" UpdateMode="Conditional">
                                <ContentTemplate>
                                    <asp:Table ID="tblTestResults" runat="server" Style="font-size: 9pt">
                                    </asp:Table>
                                </ContentTemplate>
                                <Triggers><asp:AsyncPostBackTrigger ControlID="ddlCampaign" EventName="SelectedIndexChanged" /><asp:AsyncPostBackTrigger ControlID="ddlDiam" EventName="SelectedIndexChanged" /><asp:AsyncPostBackTrigger ControlID="ddlNTD" EventName="SelectedIndexChanged" /></Triggers>
                            </asp:UpdatePanel>
                        </td>
                        <td style="vertical-align: top">
                            <asp:Table ID="tblDefectsList" runat="server" Style="font-size: 9pt; vertical-align: top;" CellPadding="0" CellSpacing="0" Visible="False">
                            </asp:Table>
                        </td>
                    </tr>
                </table>
                <hr color="gray" size="1" />
            <asp:Panel runat="server" ID="pnlGeomInsp" Visible="False" Width="915px" >
                <table>
                    <tr>
                        <td>
                            <iframe id="ifrGeomInsp" runat="server" src="GeometryInsp.aspx" Style="width: 900px; height: 600px"></iframe>
                        </td>
                    </tr>
                    <tr>
                        <td>
                            <asp:Button ID="btnCanselGeom" runat="server" Width="900px" Text="Закрыть окно" OnClick="btnCanselGeom_Click"/>
                        </td>
                    </tr>
                </table>
            </asp:Panel>
            <asp:Panel ID ="pnlInputPipe" runat="server" Height="635px" Width="415px" Visible="False">
                <iframe id="ifrInputPipe" runat="server" src="modal_InputPipeRUsc.aspx" Style="width: 415px; height: 635px"></iframe>
                <asp:Button ID="btnCloseInputPipe" Text="Закрыть" OnClick="btnCloseInputPipe_Click" Style="width: 415px" runat="server"></asp:Button>
            </asp:Panel>
                <asp:UpdatePanel ID="UpdatePanel3" runat="server"
                    OnPreRender="UpdatePanel3_PreRender">
                    <ContentTemplate>
                        <table style="background-color: #e0e0e0; font-size: 10pt">
                            <tr>
                                <td colspan="5">
                                    <asp:CheckBox ID="cbRepair" runat="server" Text="Требуется ремонт зачисткой внутренней поверхности" style="font-size: 10pt" AutoPostBack="True" OnCheckedChanged="cbRepair_CheckedChanged" />
                                </td>
                            </tr>
                            <tr>
                                <td colspan="5">
                                    <asp:Panel ID="pnlDefect" runat="server">
                                        <table style="background-color: #e0e0e0; font-size: 10pt">
                                            <tr>
                                                <td>
                                                    <asp:Label ID="lblDefectName" runat="server" Font-Bold="true">Дефект:</asp:Label>&nbsp;&nbsp;
                                                    <asp:HyperLink ID="HyperLink1" runat="server" NavigateUrl="~/Doc/defects_msd_classify.pdf" 
                                                        Target="_blank" ForeColor="Blue">Открыть классификатор дефектов</asp:HyperLink>&nbsp;|
                                                    <asp:HyperLink ID="btnDefectImage" runat="server" NavigateUrl="#">Фото</asp:HyperLink>
                                                </td>
                                                <td>&nbsp;</td>
                                                <td><asp:Label ID="lblNote" runat="server" Text="Примечание"></asp:Label></td>
                                                <td><asp:Label ID="lblSize" runat="server" Text="Расположение"></asp:Label></td>
                                                <td><asp:Label ID="lblDist" runat="server" Text="Расстояние, мм"></asp:Label></td>
                                            </tr>
                                            <tr>
                                                <td>
                                                    <asp:DropDownList ID="ddlDefect" runat="server" Font-Bold="True" TabIndex="50"
                                                        Width="350px" AutoPostBack="True"
                                                        OnSelectedIndexChanged="ddlDefect_SelectedIndexChanged">
                                                    </asp:DropDownList></td>
                                                <td>
                                                    <asp:DropDownList ID="ddlDeltaSize" runat="server" Font-Bold="True"
                                                        Width="121px" TabIndex="51">
                                                        <asp:ListItem>(величина, мм)</asp:ListItem>
                                                        <asp:ListItem>0,1 мм</asp:ListItem>
                                                        <asp:ListItem>0,2 мм</asp:ListItem>
                                                        <asp:ListItem>0,3 мм</asp:ListItem>
                                                        <asp:ListItem>0,4 мм</asp:ListItem>
                                                        <asp:ListItem>0,5 мм</asp:ListItem>
                                                        <asp:ListItem>0,6 мм</asp:ListItem>
                                                        <asp:ListItem>0,7 мм</asp:ListItem>
                                                        <asp:ListItem>0,8 мм</asp:ListItem>
                                                        <asp:ListItem>1,0 мм</asp:ListItem>
                                                        <asp:ListItem>более 1 мм</asp:ListItem>
                                                    </asp:DropDownList>
                                                </td>
                                                <td>
                                                    <asp:TextBox ID="txbDefectDescription" runat="server" MaxLength="50"
                                                        Width="122px" Font-Bold="True" TabIndex="52"></asp:TextBox>
                                                </td>
                                                <td>
                                                    <asp:DropDownList ID="ddlDefectLocation" runat="server" Font-Bold="True"
                                                        Width="121px" TabIndex="53">
                                                        <asp:ListItem></asp:ListItem>
                                                        <asp:ListItem Value="С">Север</asp:ListItem>
                                                        <asp:ListItem Value="Ю">Юг</asp:ListItem>
                                                    </asp:DropDownList>
                                                </td>
                                                <td>
                                                    <asp:TextBox ID="txbDefectDistance" runat="server" Font-Bold="True"
                                                        MaxLength="5" TabIndex="54"></asp:TextBox>
                                                </td>
                                            </tr>
                                            <tr>
                                                <td colspan="5">
                                                    <asp:Panel ID="pnlDefectDescription" runat="server">
                                                        <table style="font-size: 10pt">
                                                            <tr>
                                                                <td style="width: 122px">Описание дефекта:</td>
                                                                <td>&nbsp;</td>
                                                                <td>Возможные причины дефекта:</td>
                                                            </tr>
                                                            <tr>
                                                                <td style="border: 1px solid gray; background-color: white; width: 122px;">
                                                                    <asp:Label ID="lblDefectDescription" runat="server"></asp:Label>
                                                                </td>
                                                                <td>&nbsp;</td>
                                                                <td style="border: 1px solid gray; background-color: white">
                                                                    <asp:Label ID="lblDefectReason" runat="server"></asp:Label>
                                                                </td>
                                                            </tr>
                                                        </table>
                                                    </asp:Panel>
                                                </td>
                                            </tr>
                                        </table>
                                    </asp:Panel>
                                </td>
                            </tr>
                            <tr>
                                <td colspan="5">
                                    <asp:Panel ID="pnlDefect2" runat="server">
                                        <table style="background-color: #e0e0e0; font-size: 10pt">
                                            <tr>
                                                <td>
                                                    <asp:Label ID="lblDefectName2" runat="server" Font-Bold="true">Дефект №2:</asp:Label>&nbsp;&nbsp;
                                                    <asp:HyperLink ID="HyperLink2" runat="server" NavigateUrl="~/Doc/defects_msd_classify.pdf" 
                                                        Target="_blank" ForeColor="Blue">Открыть классификатор дефектов</asp:HyperLink>&nbsp;|
                                                    <asp:HyperLink ID="btnDefectImage2" runat="server" NavigateUrl="#">Фото</asp:HyperLink>
                                                </td>
                                                <td>&nbsp;</td>
                                                <td><asp:Label ID="lblNote2" runat="server" Text="Примечание"></asp:Label></td>
                                                <td><asp:Label ID="lblSize2" runat="server" Text="Расположение"></asp:Label></td>
                                                <td><asp:Label ID="lblDist2" runat="server" Text="Расстояние, мм"></asp:Label></td>
                                            </tr>
                                            <tr>
                                                <td>
                                                    <asp:DropDownList ID="ddlDefect2" runat="server" Font-Bold="True" TabIndex="50"
                                                        Width="350px" AutoPostBack="True"
                                                        OnSelectedIndexChanged="ddlDefect_SelectedIndexChanged">
                                                    </asp:DropDownList></td>
                                                <td>
                                                    <asp:DropDownList ID="ddlDeltaSize2" runat="server" Font-Bold="True"
                                                        Width="121px" TabIndex="51">
                                                        <asp:ListItem>(величина, мм)</asp:ListItem>
                                                        <asp:ListItem>0,1 мм</asp:ListItem>
                                                        <asp:ListItem>0,2 мм</asp:ListItem>
                                                        <asp:ListItem>0,3 мм</asp:ListItem>
                                                        <asp:ListItem>0,4 мм</asp:ListItem>
                                                        <asp:ListItem>0,5 мм</asp:ListItem>
                                                        <asp:ListItem>0,6 мм</asp:ListItem>
                                                        <asp:ListItem>0,7 мм</asp:ListItem>
                                                        <asp:ListItem>0,8 мм</asp:ListItem>
                                                        <asp:ListItem>1,0 мм</asp:ListItem>
                                                        <asp:ListItem>более 1 мм</asp:ListItem>
                                                    </asp:DropDownList>
                                                </td>
                                                <td>
                                                    <asp:TextBox ID="txbDefectDescription2" runat="server" MaxLength="50"
                                                        Width="122px" Font-Bold="True" TabIndex="52"></asp:TextBox>
                                                </td>
                                                <td>
                                                    <asp:DropDownList ID="ddlDefectLocation2" runat="server" Font-Bold="True"
                                                        Width="121px" TabIndex="53">
                                                        <asp:ListItem></asp:ListItem>
                                                        <asp:ListItem Value="С">Север</asp:ListItem>
                                                        <asp:ListItem Value="Ю">Юг</asp:ListItem>
                                                    </asp:DropDownList>
                                                </td>
                                                <td>
                                                    <asp:TextBox ID="txbDefectDistance2" runat="server" Font-Bold="True"
                                                        MaxLength="5" TabIndex="54"></asp:TextBox>
                                                </td>
                                            </tr>
                                            <tr>
                                                <td colspan="5">
                                                    <asp:Panel ID="pnlDefectDescription2" runat="server">
                                                        <table style="font-size: 10pt">
                                                            <tr>
                                                                <td style="width: 122px">Описание дефекта:</td>
                                                                <td>&nbsp;</td>
                                                                <td>Возможные причины дефекта:</td>
                                                            </tr>
                                                            <tr>
                                                                <td style="border: 1px solid gray; background-color: white; width: 122px;">
                                                                    <asp:Label ID="lblDefectDescription2" runat="server"></asp:Label>
                                                                </td>
                                                                <td>&nbsp;</td>
                                                                <td style="border: 1px solid gray; background-color: white">
                                                                    <asp:Label ID="lblDefectReason2" runat="server"></asp:Label>
                                                                </td>
                                                            </tr>
                                                        </table>
                                                    </asp:Panel>
                                                </td>
                                            </tr>
                                        </table>
                                    </asp:Panel>
                                </td>
                            </tr>
                            <tr>
                                <td colspan="5">
                                    <asp:Panel ID="pnlDefect3" runat="server">
                                        <table style="background-color: #e0e0e0; font-size: 10pt">
                                            <tr>
                                                <td>
                                                    <asp:Label ID="lblDefectName3" runat="server" Font-Bold="true">Дефект №3:</asp:Label>&nbsp;&nbsp;
                                                    <asp:HyperLink ID="HyperLink4" runat="server" NavigateUrl="~/Doc/defects_msd_classify.pdf" 
                                                        Target="_blank" ForeColor="Blue">Открыть классификатор дефектов</asp:HyperLink>&nbsp;|
                                                    <asp:HyperLink ID="btnDefectImage3" runat="server" NavigateUrl="#">Фото</asp:HyperLink>
                                                </td>
                                                <td>&nbsp;</td>
                                                <td><asp:Label ID="lblNote3" runat="server" Text="Примечание"></asp:Label></td>
                                                <td><asp:Label ID="lblSize3" runat="server" Text="Расположение"></asp:Label></td>
                                                <td><asp:Label ID="lblDist3" runat="server" Text="Расстояние, мм"></asp:Label></td>
                                            </tr>
                                            <tr>
                                                <td>
                                                    <asp:DropDownList ID="ddlDefect3" runat="server" Font-Bold="True" TabIndex="50"
                                                        Width="350px" AutoPostBack="True"
                                                        OnSelectedIndexChanged="ddlDefect_SelectedIndexChanged">
                                                    </asp:DropDownList></td>
                                                <td>
                                                    <asp:DropDownList ID="ddlDeltaSize3" runat="server" Font-Bold="True"
                                                        Width="121px" TabIndex="51">
                                                        <asp:ListItem>(величина, мм)</asp:ListItem>
                                                        <asp:ListItem>0,1 мм</asp:ListItem>
                                                        <asp:ListItem>0,2 мм</asp:ListItem>
                                                        <asp:ListItem>0,3 мм</asp:ListItem>
                                                        <asp:ListItem>0,4 мм</asp:ListItem>
                                                        <asp:ListItem>0,5 мм</asp:ListItem>
                                                        <asp:ListItem>0,6 мм</asp:ListItem>
                                                        <asp:ListItem>0,7 мм</asp:ListItem>
                                                        <asp:ListItem>0,8 мм</asp:ListItem>
                                                        <asp:ListItem>1,0 мм</asp:ListItem>
                                                        <asp:ListItem>более 1 мм</asp:ListItem>
                                                    </asp:DropDownList>
                                                </td>
                                                <td>
                                                    <asp:TextBox ID="txbDefectDescription3" runat="server" MaxLength="50"
                                                        Width="122px" Font-Bold="True" TabIndex="52"></asp:TextBox>
                                                </td>
                                                <td>
                                                    <asp:DropDownList ID="ddlDefectLocation3" runat="server" Font-Bold="True"
                                                        Width="121px" TabIndex="53">
                                                        <asp:ListItem></asp:ListItem>
                                                        <asp:ListItem Value="С">Север</asp:ListItem>
                                                        <asp:ListItem Value="Ю">Юг</asp:ListItem>
                                                    </asp:DropDownList>
                                                </td>
                                                <td>
                                                    <asp:TextBox ID="txbDefectDistance3" runat="server" Font-Bold="True"
                                                        MaxLength="5" TabIndex="54"></asp:TextBox>
                                                </td>
                                            </tr>
                                            <tr>
                                                <td colspan="5">
                                                    <asp:Panel ID="pnlDefectDescription3" runat="server">
                                                        <table style="font-size: 10pt">
                                                            <tr>
                                                                <td style="width: 122px">Описание дефекта:</td>
                                                                <td>&nbsp;</td>
                                                                <td>Возможные причины дефекта:</td>
                                                            </tr>
                                                            <tr>
                                                                <td style="border: 1px solid gray; background-color: white; width: 122px;">
                                                                    <asp:Label ID="lblDefectDescription3" runat="server"></asp:Label>
                                                                </td>
                                                                <td>&nbsp;</td>
                                                                <td style="border: 1px solid gray; background-color: white">
                                                                    <asp:Label ID="lblDefectReason3" runat="server"></asp:Label>
                                                                </td>
                                                            </tr>
                                                        </table>
                                                    </asp:Panel>
                                                </td>
                                            </tr>
                                        </table>
                                    </asp:Panel>
                                </td>
                            </tr>                          
                            <tr>
                                <td colspan="5">
                                    <asp:Panel ID="pnlPerevod" runat="server">
                                        <table style="font-size: 10pt">
                                            <tr>
                                                <td>
                                                    <asp:Label ID="lblDefectAdditional" runat="server" Text="Дефект, выводящий длину за пределы требований НД:"></asp:Label></td>
                                                <td>&nbsp;</td>
                                                <td>&nbsp;</td>
                                                <td>&nbsp;</td>
                                                <td>&nbsp;</td>
                                            </tr>
                                            <tr>
                                                <td>
                                                    <asp:DropDownList ID="ddlDefectAdditional" runat="server" Font-Bold="True" TabIndex="50" Width="350px">
                                                    </asp:DropDownList>
                                                </td>
                                                <td>&nbsp;</td>
                                                <td colspan="3" rowspan="5" style="vertical-align: top">
                                                    <asp:Label ID="lblCutDataInformation" runat="server"></asp:Label>
                                                </td>
                                            </tr>
                                            <tr>
                                                <td><strong>
                                                    <br />
                                                    <asp:Label ID="lblInstructionType" runat="server" Text="Основание перевода (вид и № документа):"></asp:Label></strong></td>
                                                <td>&nbsp;</td>
                                            </tr>
                                            <tr>
                                                <td>
                                                    <asp:DropDownList ID="ddlInstructionType" runat="server" Font-Bold="True" Height="53px" OnSelectedIndexChanged="ddlDefect_SelectedIndexChanged" TabIndex="50" Width="196px">
                                                        <asp:ListItem></asp:ListItem>
                                                        <asp:ListItem Value="-1">Письмо</asp:ListItem>
                                                        <asp:ListItem Value="-2">Распоряжение</asp:ListItem>
                                                        <asp:ListItem Value="-3">Заказ</asp:ListItem>
                                                        <asp:ListItem Value="-4">Протокол</asp:ListItem>
                                                        <asp:ListItem Value="-5">Акт</asp:ListItem>
                                                    </asp:DropDownList>
                                                    <asp:TextBox ID="txbInstructionNumber" runat="server" Font-Bold="True" MaxLength="50" TabIndex="52" Width="151px"></asp:TextBox>
                                                </td>
                                                <td>&nbsp;</td>
                                            </tr>
                                            <tr>
                                                <td><strong>
                                                    <asp:Label ID="lblPerevodReason" runat="server" Text="Причина перевода:"></asp:Label></strong></td>
                                                <td>&nbsp;</td>
                                            </tr>
                                            <tr>
                                                <td>
                                                    <asp:DropDownList ID="ddlPerevodReason" runat="server" Width="350px" Font-Bold="True">
                                                    </asp:DropDownList>
                                                </td>
                                                <td>&nbsp;</td>
                                            </tr>
                                         </table>
                                    </asp:Panel>
                                </td>
                            </tr>

                            <tr>
                                <td colspan="5">
                                    <asp:Panel ID="pnlScraping" runat="server">
                                        <table style="font-size: 10pt">
                                            <tr>
                                                <td>
                                                    <asp:CheckBox ID="cbScraping" runat="server" Text="По всей длине трубы" AutoPostBack="True" /></td>
                                            </tr>
                                            <tr>
                                                <td>
                                                    <asp:Label ID="lblAmounts" runat="server" Text="Количество зачисток:"></asp:Label></td>
                                                <td>
                                                    <asp:TextBox ID="txbAmounts" runat="server" Font-Bold="True" MaxLength="5" TabIndex="54"></asp:TextBox>
                                                     <%--<asp:CustomValidator ID="CustomValidator1" runat="server" ClientValidationFunction="ValidateNumber"
                                                         ControlToValidate="txbAmounts" ErrorMessage="*" Font-Size="Smaller"></asp:CustomValidator>--%>
                                                </td>
                                            </tr>
                                            <tr>
                                                <td>
                                                    <asp:Label ID="lblDefLocation" runat="server" Text="Расположение дефектов"></asp:Label></td>
                                                <td>
                                                    <asp:DropDownList ID="ddlLocationScraping" runat="server" Font-Bold="True" TabIndex="50" AutoPostBack="True" OnSelectedIndexChanged="ddlLocationScraping_SelectedIndexChanged" ></asp:DropDownList></td>
                                            </tr>
                                            <tr>
                                                <td>
                                                    <asp:Label ID="lblMinWallWeld" runat="server" Text="Минимальная толщина стенки на шве в местах зачистки, мм:"></asp:Label></td>
                                                <td>
                                                    <asp:TextBox ID="txbMinWallWeld" runat="server" Font-Bold="True" MaxLength="5" TabIndex="54" Width="200px"></asp:TextBox></td>
                                            </tr>
                                            <tr>
                                                <td>
                                                    <asp:Label ID="lblMinWallPipe" runat="server" Text="Минимальная толщина стенки в основном металле в местах зачистки, мм:"></asp:Label></td>
                                                <td>
                                                    <asp:TextBox ID="txbMinWallPipe" runat="server" Font-Bold="True" MaxLength="5" TabIndex="54" Width="200px"></asp:TextBox></td>
                                            </tr>
                                            <tr>
                                                <td>
                                                    <asp:Label ID="lblResultScraping" runat="server" Text="Заключение после зачистки"></asp:Label></td>
                                                <td>
                                                    <asp:DropDownList ID="ddlResultScraping" runat="server" Font-Bold="True" TabIndex="50" AutoPostBack="True" ></asp:DropDownList></td>
                                            </tr>
                                            <tr>
                                                <td>
                                                    <asp:Label ID="lblPrestar" runat="server" Text="Заключение от системы Prestar"></asp:Label></td>
                                                <td>
                                                    <asp:DropDownList ID="ddlResultPrestar" runat="server" Font-Bold="True" TabIndex="50" AutoPostBack="false" ></asp:DropDownList></td>
                                            </tr>
                                        </table>
                                    </asp:Panel>
                                </td>
                            </tr>
                        </table>

                        &nbsp;<asp:Panel ID="pnlRepairScraping" runat="server">
                            <table style="background-color: #e0e0e0; font-size: 10pt">
                                <tr>
                                    <td style="width: 144px">
                                        <strong>
                                            <br />
                                            Ремонт зачисткой:</strong></td>
                                    <td style="width: 374px">
                                        <br />
                                        Дефект отремонтированный зачисткой:</td>
                                    <td style="width: 92px">Длина
                                    <br />
                                        зачистки, мм</td>
                                    <td>Толщина стенки<br />
                                        до зачистки, мм</td>
                                    <td>Толщина стенки
                                    <br />
                                        после зачистки, мм</td>
                                </tr>
                                <tr>
                                    <td style="width: 144px">
                                        <asp:DropDownList ID="ddlZachistkaEnabled" runat="server" AutoPostBack="True"
                                            Font-Bold="True" Width="135px" TabIndex="60">
                                            <asp:ListItem></asp:ListItem>
                                            <asp:ListItem>Производился</asp:ListItem>
                                            <asp:ListItem>Не производился</asp:ListItem>
                                        </asp:DropDownList>
                                    </td>
                                    <td style="width: 374px">
                                        <asp:DropDownList ID="ddlDefectZachistka" runat="server" Font-Bold="True"
                                            TabIndex="61" Width="364px">
                                        </asp:DropDownList>
                                    </td>
                                    <td style="width: 92px">
                                        <asp:TextBox ID="txbZachistkaLength" runat="server" Font-Bold="True"
                                            MaxLength="5" Width="82px" TabIndex="62"></asp:TextBox>
                                    </td>
                                    <td>
                                        <asp:TextBox ID="txbZachistkaThickness1" runat="server" Font-Bold="True"
                                            MaxLength="6" Width="115px" TabIndex="63"></asp:TextBox>
                                    </td>
                                    <td>
                                        <asp:TextBox ID="txbZachistkaThickness2" runat="server" Font-Bold="True"
                                            MaxLength="6" Width="123px" TabIndex="64"></asp:TextBox>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="width: 144px">&nbsp;</td>
                                    <td style="width: 374px">&nbsp;</td>
                                    <td style="width: 92px">&nbsp;</td>
                                    <td>&nbsp;</td>
                                    <td>&nbsp;</td>
                                </tr>
                            </table>
                        </asp:Panel>

                    </ContentTemplate>
                </asp:UpdatePanel>
                <hr color="gray" size="1" />

                <asp:UpdatePanel ID="pnlLabelType" runat="server">
                    <ContentTemplate>

                        <table style="font-size: 10pt">
                            <tr>
                                <td style="width: 171px">Тип бирки:</td>
                                <td>
                                    <asp:DropDownList ID="ddlLabelType" runat="server" Width="250px">
                                    </asp:DropDownList>
                                </td>
                                <td>
                                    <asp:CheckBox ID="cbWeight" runat="server" Text="С весом трубы" />
                                </td>
                                <td>
                                    <asp:CheckBox ID="cbInch" runat="server" Text="ММ в дюймы *" />
                                </td>
                                <td></td>
                                <td>
                                    <asp:CheckBox ID="cbKGtoFunt" runat="server" Text="КГ в фунты" />
                                </td>
                            </tr>
                            <tr>
                                <td style="width: 171px">Печать бирки на принтере:</td>
                                <td colspan="5">
                                    <asp:DropDownList ID="ddlPrinter" runat="server">
                                    </asp:DropDownList>
                                </td>
                            </tr>
                            <tr>
                                <td style="width: 171px"> <asp:Label runat="server" ID="lblKMK">Тип клейма для КМК:</asp:Label> </td>
                                <td colspan="5">
                                    <asp:DropDownList ID="ddlLabelTypeKmk" runat="server" Width="360px">
                                    </asp:DropDownList>
                                </td>
                            </tr>
                            <tr>
                                <%--<td style="width: 171px">Маршрут СГП:</td>
                                <td colspan="3">
                                    <asp:DropDownList ID="ddlSgpRoute" runat="server" Width="360px">
                                    </asp:DropDownList>
                                </td>--%>
                            </tr>
                        </table>
                    </ContentTemplate>
                </asp:UpdatePanel>

                <asp:Panel ID="pnlExistKP" runat="server">
                    <table style="font-size: 10pt">
                        <tr>
                            <td style="width: 171px">Качество консервационного покрытия:</td>
                            <td>
                                <asp:DropDownList ID="ddlQualityKP" runat="server" Width="250px">
                                    <asp:ListItem Text="" Value=""></asp:ListItem>
                                    <asp:ListItem Text="соответствует" Value="1"></asp:ListItem>
                                    <asp:ListItem Text="не соответствует" Value="0"></asp:ListItem>
                                </asp:DropDownList>
                            </td>
                        </tr>
                    </table>
                </asp:Panel>

                &nbsp;
            <table style="width: 100%" bgcolor="#e0e0e0">
                <tr>
                    <td style="padding-right: 12px; text-align: left;">
                        <table style="font-size: 10pt" cellspacing="0" cellpadding="0">
                            <tr>
                                <td>
                                    <asp:Button ID="btnMasterConfirm" runat="server" Text="Разрешение мастера..." Width="177px" OnClick="btnMasterConfirm_Click" />
                                </td>
                                <td style="padding-left: 8px">
                                    <asp:Label ID="lblMasterConfirmFio" runat="server"></asp:Label>
                                    <asp:HiddenField ID="fldMasterConfirmLogin" runat="server" />
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
                <tr>
                    <td style="padding-right: 12px; text-align: left;">
                        <asp:Button ID="btnToSklad" runat="server" Font-Bold="False" Font-Size="11pt" Height="32px" OnClick="btnToSklad_Click" OnClientClick="return PostBackByButton(this);" TabIndex="103" Text="На склад" Width="130px" />
                        &nbsp;
                                    <asp:Button ID="btnToRepair" runat="server" Font-Bold="False" Font-Size="11pt" Height="32px" OnClick="btnToBrakOrRepair_Click" OnClientClick="return PostBackByButton(this);" TabIndex="103" Text="На ремонт" Width="130px" />
                        &nbsp;
                                    <asp:Button ID="btnToBrak" runat="server" Font-Bold="False" Font-Size="11pt" Height="32px" OnClick="btnToBrakOrRepair_Click" OnClientClick="return PostBackByButton(this);" Style="margin-right: 50px" TabIndex="103" Text="Брак / Лом негабаритный" Width="201px" />
                        <asp:Button ID="btnFromSklad" runat="server" Font-Bold="False" Font-Size="11pt" Height="32px" OnClick="btnToSklad_Click" OnClientClick="return PostBackByButton(this);" TabIndex="103" Text="Возврат со склада" Width="162px" />
                        &nbsp;
                                    <asp:Button ID="btnNewPipe" runat="server" Font-Bold="False" Font-Size="11pt" Height="32px" OnClick="btnNewPipe_Click" OnClientClick="return PostBackByButton(this);" TabIndex="104" Text="Новая труба" />
                        &nbsp; &nbsp; &nbsp; 
                        <asp:Button ID="btnShowHistory1" runat="server" Font-Size="11pt" Height="32px" OnClientClick="return bthShowHistory3Click();" TabIndex="14"
                                    Text="История трубы" Width="140px" />
                    </td>
                </tr>
                <tr>
                    <td bgcolor="white" style="padding-right: 12px; padding-top: 8px; text-align: left; font-size: 9pt">* - При выборе опции "ММ в дюймы" на бирку будут переданы значения диаметра/размера профиля, толщины стенки  в дюймах, а длинна трубы в футах </td>
                </tr>
            </table>
            </asp:View>

            <asp:View runat="server" ID="vDuplicatePrintLabel">
                <table style="font-size: 10pt">
                    <tr>
                        <td>
                            <b>Внимание!</b><br />
                            <br />
                            Бирка на данную трубу выпускается повторно и работник несет личную 
                            ответственность за выпуск бирки-дубликата.
                            <br />
                            Для подтверждегия выпуска бирки-дубликата укажите причину из списка и нажмите 
                            кнопку &quot;Напечатать бирку&quot;. Для выхода без печати бирки нажмите &quot;Отмена&quot;.
                        </td>
                    </tr>
                </table>
                <table style="font-size: 10pt">
                    <tr>
                        <td style="width: 310px">
                            <br />
                            Причина выпуска бирки-дубликата</td>
                        <td style="width: 119px">&nbsp;</td>
                        <td>&nbsp;</td>
                    </tr>
                    <tr>
                        <td style="width: 310px">
                            <asp:DropDownList ID="ddlDuplicateReason" runat="server" Width="296px" Font-Bold="true">
                            </asp:DropDownList>
                        </td>
                        <td style="width: 119px">
                            <asp:Button ID="btnConfirmPrintLabel" runat="server"
                                Height="26px" OnClick="btnPrintLabel2_Click" Text="Напечатать бирку"
                                Width="133px" />
                        </td>
                        <td>
                            <asp:Button ID="btnCancelPrintLabel" runat="server" Height="26px"
                                OnClick="btnNewPipe_Click" Text="Отмена" />
                        </td>
                    </tr>
                </table>
                <br />
            </asp:View>

            <asp:View ID="vWarnings" runat="server">
                <table style="width: 100%;">
                    <tr>
                        <td>
                            <p style="font-weight: bold">Внимание !</p>
                            <asp:BulletedList ID="lstWarnings" runat="server" BulletStyle="Numbered" Font-Size="11pt">
                                <asp:ListItem>Сообщение 1</asp:ListItem>
                                <asp:ListItem>Сообщение 2</asp:ListItem>
                                <asp:ListItem>Сообщение 3</asp:ListItem>
                            </asp:BulletedList>
                        </td>
                    </tr>
                    <tr>
                        <td>
                            <hr />
                        </td>
                    </tr>
                    <tr>
                        <td>
                            <asp:Button ID="btnWarningOk" runat="server" OnClick="btnWeightWarningOk_Click" Text="Принять трубу" />
                            &nbsp;<asp:Button ID="btnWarningCancel" runat="server" OnClick="btnWeightWarningCancel_Click" Text="Отмена" />
                            <asp:HiddenField ID="fldWarningReturnButtonId" runat="server" />
                        </td>
                    </tr>
                </table>
                <br />
            </asp:View>

        </asp:MultiView>
    </asp:Panel>
    <br />
    &nbsp;<uc2:PopupWindow ID="PopupWindow1" runat="server"
        ContentPanelId="pnlLotWarning" Title="Ввод дополнительной информации"
        TitleBackColor="Red" />

    <br />


    <asp:Panel ID="pnlNotZakazReason" runat="server" Height="325px" Width="470px"
        Style="padding: 8px" Visible="False">
        <table style="font-size: 10pt;">
            <tr>
                <td>
                    <asp:TextBox ID="txbNotZakazReasonDescription" runat="server"
                        BackColor="#E0E0E0" Height="51px" MaxLength="255" ReadOnly="True"
                        TextMode="MultiLine" Width="450px"></asp:TextBox>
                </td>
            </tr>
            <tr>
                <td>&nbsp;</td>
            </tr>
            <tr>
                <td>Наименование прчины перевода:</td>
            </tr>
            <tr>
                <td>
                    <asp:DropDownList ID="ddlNotZakazReason" runat="server" Width="450px"
                        AutoPostBack="True"
                        OnSelectedIndexChanged="ddlNotZakazReason_SelectedIndexChanged">
                    </asp:DropDownList>
                </td>
            </tr>
        </table>
        <table style="font-size: 10pt;">
            <tr>
                <td style="width: 174px">
                    <br />
                    Фактическое значение:</td>
                <td style="width: 55px">
                    <br />
                    <asp:TextBox ID="txbNotZakazReasonValue" runat="server" MaxLength="6"
                        Width="50px"></asp:TextBox>
                </td>
                <td style="width: 26px">
                    <br />
                </td>
            </tr>
            <tr>
                <td style="width: 174px">Расстояние от торца трубы:</td>
                <td style="width: 55px">
                    <asp:TextBox ID="txbNotZakazReasonDistance" runat="server" MaxLength="6"
                        Width="50px"></asp:TextBox>
                </td>
                <td style="width: 26px">мм</td>
            </tr>
        </table>
        <asp:HiddenField ID="fldNotZakazCallbackButton" runat="server" />
        <asp:HiddenField ID="fldNotZakazCheckOk" runat="server" />
        <br />
        <table style="width: 100%;">
            <tr>
                <td style="text-align: right">
                    <asp:Button ID="btnNotZakazSave" runat="server" Height="26px"
                        OnClick="btnNotZakazSave_Click" OnClientClick="return PostBackByButton(this);"
                        Text="Сохранить и напечатать бирку" Width="209px" />
                    &nbsp;<asp:Button ID="btnNotZakazCancel" runat="server" Height="26px"
                        OnClick="btnNotZakazCancel_Click"
                        OnClientClick="return PostBackByButton(this);" Text="Отмена" Width="70px" />
                </td>
            </tr>
        </table>
    </asp:Panel>
	
	
    <asp:Panel runat="server" ID="pnlMasterConfirm" Height="116px" Width="344px" Style="padding: 8px" Visible="false">
        <table style="font-size: 10pt;">
            <tr>
                <td style="width: 134px">Учетная запись или табельный номер:</td>
                <td style="width: 158px">
                    <asp:TextBox ID="txbMasterConfirmLogin" runat="server" Font-Bold="True" TabIndex="20"
                        Width="184px"></asp:TextBox>
                </td>
            </tr>
            <tr>
                <td style="width: 134px">Пароль:</td>
                <td style="width: 158px">
                    <asp:TextBox ID="txbMasterConfirmPassword" runat="server" Font-Bold="True" TabIndex="21"
                        TextMode="Password" Width="184px"></asp:TextBox>
                </td>
            </tr>
            <tr>
                <td colspan="2">
                    <br />
                </td>
            </tr>
            <tr>
                <td colspan="2" style="text-align: right">
                    <asp:Button ID="btnConfirmMasterOk" runat="server" Height="26px" TabIndex="22" Text="Подтвердить разрешение"
                        Width="192px" OnClick="btnConfirmMasterOk_Click"
                        OnClientClick="return PostBackByButton(this);" />
                    &nbsp;
                    <asp:Button ID="btnConfirmMasterCancel" runat="server" Height="26px"
                        OnClick="btnConfirmMasterCancel_Click" TabIndex="23" Text="Отмена" Width="70px" />
                </td>
            </tr>
        </table>
    </asp:Panel>

    <asp:Panel runat="server" ID="pnlRouteRejection" Height="228px" Width="445px" Style="padding: 8px" Visible="false">
        <table style="font-size: 10pt">
            <tr>
                <td style="background-color: #fffec5; border: 1px solid black">
                    <asp:Label ID="lblRouteRejectMessage" runat="server"></asp:Label>
                </td>
            </tr>
            <tr>
                <td>
                    <br />
                    Проверьте правильность ввода номера трубы, и если номер введен правильно, то укажите причину отклонения от маршрута:</td>
            </tr>
            <tr>
                <td>
                    <asp:TextBox ID="txbRouteRejectReason" runat="server" Height="81px" MaxLength="255" TextMode="MultiLine" Width="423px"></asp:TextBox>
                </td>
            </tr>
            <tr>
                <td style="text-align: right">
                    <br />
                    <asp:Button ID="btnRouteRejectConfirm" runat="server" Text="Зарегистрировать отклонение от маршрута" Width="308px" OnClick="btnRouteRejectConfirm_Click" OnClientClick="return PostBackByButton(this);" />
                    <asp:Button ID="btnRouteRejectCancel" runat="server" Text="Отмена" OnClick="btnRouteRejectCancel_Click" OnClientClick="return PostBackByButton(this);" />
                </td>
            </tr>
        </table>
    </asp:Panel>

    <asp:Panel runat="server" ID="pnlTransportNumber" Style="padding: 8px" Width="588px" Height="429px" Visible="false">
        <asp:Panel ID="pnlTabsTransportNumber" runat="server" Style="border-top-width: 3px; border-left-width: 3px; border-left-color: aqua; border-top-color: aqua; padding-top: 1px; border-right-width: 3px; border-right-color: aqua; border-bottom-width: 3px; border-bottom-color: silver;"
            BackColor="#E0E0E0">
            <table cellspacing="0" height="24" style="display: inline; width: 100%;">
                <tr>
                    <td runat="server" style="width: 4px; border-bottom: gray 1px solid; text-align: center; padding-bottom: 4px; padding-top: 4px;">&nbsp;</td>
                    <td id="tdTransportNumberTab" runat="server" style="padding-right: 8px; padding-left: 8px; width: 260px; text-align: center; padding-bottom: 4px; padding-top: 4px; border-right: gray 1px solid; border-top: gray 1px solid; border-bottom-width: 1px; border-bottom-color: gray; border-left: gray 1px solid;"
                        bgcolor="#ffffff">
                        <asp:LinkButton ID="btnTransportNumberTab" runat="server" Font-Size="11pt" ForeColor="Black"
                            Style="text-decoration: none" OnClick="btnTransportNumberTab_Click">Получение транспортного номера</asp:LinkButton>
                    </td>
                    <td id="tdTransportNumberEditTab" runat="server" style="padding-right: 8px; padding-left: 8px; width: 100px; text-align: center; padding-bottom: 4px; padding-top: 4px; border-bottom: gray 1px solid;" bgcolor="#e0e0e0">
                        <asp:LinkButton ID="btnTransportNumberEditTab" runat="server" Font-Size="11pt" Font-Underline="False"
                            ForeColor="Gray" Style="text-decoration: none" OnClick="btnTransportNumberEditTab_Click">Исправление</asp:LinkButton>
                    </td>
                    <td runat="server" style="padding-right: 8px; padding-left: 8px; border-bottom: gray 1px solid; padding-bottom: 4px; padding-top: 4px;">&nbsp;</td>
                </tr>
            </table>
        </asp:Panel>
        <asp:Panel runat="server" Style="border-left: 1px solid silver; border-right: 1px solid silver; border-bottom: 1px solid silver; background-color: white; padding: 8px" Height="391px" Width="588px">

            <asp:MultiView ID="mvTransportNumber" runat="server" ActiveViewIndex="0">
                <asp:View runat="server" ID="vTransportNumberEdit">

                    <table style="font-size: 10pt">
                        <tr>
                            <td style="width: 213px">Год и номер трубы:</td>
                            <td>
                                <asp:Label ID="lblTransportPipeNumber" runat="server" Font-Bold="True"></asp:Label>
                            </td>
                        </tr>
                        <tr>
                            <td style="width: 213px">&nbsp;</td>
                            <td>&nbsp;</td>
                        </tr>
                        <tr>
                            <td style="width: 213px">Рабочее место потери номера:</td>
                            <td>
                                <asp:DropDownList ID="ddlTransportNumberWorkplace" runat="server" Width="350px">
                                </asp:DropDownList>
                            </td>
                        </tr>
                        <tr>
                            <td style="width: 213px">Номинальный диаметр:</td>
                            <td>
                                <asp:DropDownList ID="ddlTransportNumberDiameter" runat="server" Width="350px">
                                </asp:DropDownList>
                            </td>
                        </tr>
                        <tr>
                            <td style="width: 213px">Номинальный типоразмер профиля:</td>
                            <td>
                                <asp:DropDownList ID="ddlTransportNumberProfileSize" runat="server" Width="350px">
                                </asp:DropDownList>
                            </td>
                        </tr>
                        <tr>
                            <td style="width: 213px">Номинальная толщина стенки:</td>
                            <td>
                                <asp:DropDownList ID="ddlTransportNumberThickness" runat="server" Width="350px">
                                </asp:DropDownList>
                            </td>
                        </tr>
                        <tr>
                            <td style="width: 213px">Марка стали:</td>
                            <td>
                                <asp:DropDownList ID="ddlTransportNumberSteelmark" runat="server" Width="350px">
                                </asp:DropDownList>
                            </td>
                        </tr>
                        <tr>
                            <td style="width: 213px">Фактическая длина, м:</td>
                            <td>
                                <asp:TextBox ID="txbTransportNumberLength" runat="server"></asp:TextBox>
                            </td>
                        </tr>
                        <tr>
                            <td style="width: 213px">&nbsp;</td>
                            <td>&nbsp;</td>
                        </tr>
                        <tr>
                            <td colspan="2">Причина получения транспортировочного номера:</td>
                        </tr>
                        <tr>
                            <td colspan="2">
                                <asp:TextBox ID="txbTransportNumberReason" runat="server" MaxLength="255" TextMode="MultiLine" Width="561px" Height="45px"></asp:TextBox>
                            </td>
                        </tr>
                        <tr>
                            <td style="width: 213px">Примечание:</td>
                            <td>&nbsp;</td>
                        </tr>
                        <tr>
                            <td colspan="2">
                                <asp:TextBox ID="txbTransportNumberNote" runat="server" MaxLength="255" TextMode="MultiLine" Width="561px" Height="47px"></asp:TextBox>
                            </td>
                        </tr>
                        <tr>
                            <td style="width: 213px">&nbsp;</td>
                            <td style="text-align: right">
                                <br />
                                <asp:Button ID="btnTransportNumberRepair" runat="server" OnClientClick="return PostBackByButton(this);" Text="На ремонт" OnClick="btnTransportNumberRepair_Click" />
                                <asp:Button ID="btnTransportNumberCancel" runat="server" OnClientClick="return PostBackByButton(this);" Text="Отмена" OnClick="btnTransportNumberCancel_Click" />
                            </td>
                        </tr>
                    </table>

                </asp:View>

                <asp:View runat="server" ID="vTransportNumberFind" OnPreRender="vTransportNumberFind_PreRender">
                    <table style="font-size: 10pt">
                        <tr>
                            <td>Начало периода:</td>
                            <td>
                                <uc3:CalendarControl ID="cldTransportNumberPeriodStart" runat="server" DateOnly="True" />
                            </td>
                            <td>&nbsp;</td>
                            <td>
                                <asp:HiddenField ID="fldSelectedTransportNumber" runat="server" />
                            </td>
                        </tr>
                        <tr>
                            <td>Конец периода:</td>
                            <td>
                                <uc3:CalendarControl ID="cldTransportNumberPeriodEnd" runat="server" DateOnly="True" />
                            </td>
                            <td>&nbsp;</td>
                            <td>
                                <asp:Button ID="btnTransportNumberFind" runat="server" OnClick="btnTransportNumberFind_Click" Text="Показать данные" />
                            </td>
                        </tr>
                    </table>
                    <br />
                    <asp:Panel ID="Panel7" runat="server" BorderColor="Black" BorderStyle="Solid" BorderWidth="1px" Height="298px" ScrollBars="Auto">
                        <asp:Table ID="tblTransportNumbersHistory" runat="server" CellPadding="1" CellSpacing="0" EnableViewState="False" Font-Size="10pt" GridLines="Both">
                            <asp:TableRow runat="server" BackColor="#E0E0E0" HorizontalAlign="Center" VerticalAlign="Middle">
                                <asp:TableCell runat="server">Дата</asp:TableCell>
                                <asp:TableCell runat="server">Номер трубы</asp:TableCell>
                                <asp:TableCell runat="server">Диаметр</asp:TableCell>
                                <asp:TableCell runat="server">Толщина стенки</asp:TableCell>
                                <asp:TableCell runat="server">Марка стали</asp:TableCell>
                                <asp:TableCell runat="server">Рабочее место</asp:TableCell>
                                <asp:TableCell runat="server">ФИО оператора</asp:TableCell>
                            </asp:TableRow>
                        </asp:Table>
                    </asp:Panel>
                    &nbsp;<table style="width: 100%;">
                        <tr>
                            <td style="text-align: right">
                                <asp:Button ID="btnTransportNumberPrintlabel" runat="server" OnClick="btnTransportNumberPrintlabel_Click" Text="Напечатать бирку" Enabled="False" />
                                &nbsp;
                                <asp:Button ID="btnTransportNumberEdit" runat="server" OnClick="btnTransportNumberEdit_Click" Text="Редактировать" Enabled="False" />
                                &nbsp;<asp:Button ID="btnTransportNumberDelete" runat="server" OnClick="btnTransportNumberDelete_Click" Text="Удалить" Enabled="False" OnClientClick="if(confirm('Удалить выбранную запись?')) return PostBackByButton(this);" />
                                &nbsp;
                                <asp:Button ID="btnTransportNumberCancel2" runat="server" OnClick="btnTransportNumberCancel_Click" Text="Отмена" />
                            </td>
                        </tr>
                    </table>
                </asp:View>

            </asp:MultiView>
        </asp:Panel>
    </asp:Panel>
</asp:Content>

