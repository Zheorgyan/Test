<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" ValidateRequest="false"
    CodeFile="StoreHousesOfPipesByPipesNums.aspx.cs" Inherits="StoreHousesOfPipesByPipesNums" Title="Складские объекты ТЭСЦ-3" %>

<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Src="wucExtendedTableTemprory.ascx" TagName="wucExtendedTableTemprory" TagPrefix="uc1" %>
<%@ Register Assembly="System.Web.Extensions, Version=1.0.61025.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35"
    Namespace="System.Web.UI" TagPrefix="asp" %>
<%@ Register Src="PopupWindow.ascx" TagName="PopupWindow" TagPrefix="uc2" %>
<asp:Content ID="Content1" ContentPlaceHolderID="TitlePlaceHolder" runat="Server">
    Складские объекты ТЭСЦ-3 (пономерной учёт)


    <script type="text/javascript" language="javascript">
        document.body.onkeypress = null;

        // вставка в таблицу склада
        function InsertActControls(Row, Cell, Pr) {

            //document.getElementById(prefix + "hfActRow").value = Row;
            table = document.getElementById(prefix + "tblSklad");

            // номер трубы
            temp = table.rows[Row].cells[1 + Cell].innerText;
            table.rows[Row].cells[1 + Cell].innerText = '';
            table.rows[Row].cells[1 + Cell].insertBefore(document.getElementById(prefix + "tbPipeNumber"));
            document.getElementById(prefix + "tbPipeNumber").value = temp;
            document.getElementById(prefix + "tbPipeNumber").focus();

            if (Pr > 0) {
                table.rows[Row].cells[7 + Cell].innerText = '';
                table.rows[Row].cells[7 + Cell].insertBefore(document.getElementById(prefix + "cbPresentation"));
            }

            // нзп
            temp = table.rows[Row].cells[7 + Cell + Pr].innerText;
            table.rows[Row].cells[7 + Cell + Pr].innerText = '';
            table.rows[Row].cells[7 + Cell + Pr].insertBefore(document.getElementById(prefix + "chbNZP0"));
            if (temp != '') document.getElementById(prefix + "chbNZP0").chacked = true;
            else document.getElementById(prefix + "chbNZP0").chacked = false;

            // дефект
            table.rows[Row].cells[8 + Cell + Pr].innerText = '';
            table.rows[Row].cells[8 + Cell + Pr].insertBefore(document.getElementById(prefix + "ddlDefect"));

            // примечание
            temp = table.rows[Row].cells[12 + Cell + Pr].innerText;
            table.rows[Row].cells[12 + Cell + Pr].innerText = '';
            table.rows[Row].cells[12 + Cell + Pr].insertBefore(document.getElementById(prefix + "tbNotes"));
            table.rows[Row].cells[12 + Cell + Pr].insertBefore(document.getElementById(prefix + "ddlNotes"));
            //document.getElementById(prefix + "tbNotes").value = temp;
        }

        //автоматический переход на следующий элемент управления
        //по нажатию Enter
        function newKeyPress(nextCtrlID) {
            input_code = window.event.keyCode;
            if (input_code != 13) return true;
            ctrl = document.getElementById(prefix + nextCtrlID);
            if (ctrl != null) ctrl.focus();
            return false;
        }

        //автоматический вызов команды "Подтвердить ввод"
        //по нажатию Enter
        function newSubmitInput() {
            //       input_code=window.event.keyCode;
            if (input_code != 13) return true;
            _btnCONFIRM_LINE = document.getElementById(prefix + "ucExtTablePipesInStack_btnCONFIRM_LINE");
            if (_btnCONFIRM_LINE != null)
                _btnCONFIRM_LINE.click();

            return false;
        }
        
        // функция обрабатывает добавление трубы на промышленном планшете
        function KeyFromMobile() {
            
            var myParam = location.search.split('op_mode=')[1] ? location.search.split('op_mode=')[1] : 'web';
            if (myParam == 'mobile') {
                var pipeNumber = document.getElementById('<%=tbPrihodPipeNumber.ClientID%>').value;
                if (pipeNumber.length > 7) {
                    var now = new Date().toLocaleString();
                    __doPostBack('PRIHOD', 'ADD#' + now);
                    return false;
                }
            }
            return true;
        }

        //Проверка на введеный символ для текстовых полей
        function EnsureNumeric(s, e) {
            var key = window.event.keyCode;
            //Цифры
            if (key < 48 || key > 57)
                window.event.returnValue = false;
            //Точка
            if (key == 46)
                window.event.returnValue = true;
            //esc
            if (key == 27) {
                __doPostBack('CANCEL', '0#100');
                window.event.returnValue = true;
            }
        }

        // Запрет ввода не числовой информаци
        function OnKeyDown() {
            input_code = window.event.keyCode;
            if ((input_code > 47 && input_code < 58) || (input_code > 95 && input_code < 106) || input_code == 8 || input_code == 46 || input_code == 37 || input_code == 39 || input_code == 9) return true;
            return false;
        }

        //Сброс параметров фильтрации в исходное состояние
        function ClearFilterl(mainObj) {
            ddlStack = document.getElementById(prefix + 'ddlStack');
            if (ddlStack != null) ddlStack.setAttribute("selectedIndex", "0");
            ddlPocket = document.getElementById(prefix + 'ddlPocket');
            if (ddlPocket != null) ddlStack.setAttribute("selectedIndex", "0");

            document.getElementById(prefix + 'ddlDefects').setAttribute("selectedIndex", "0");
            document.getElementById(prefix + 'ddlND').setAttribute("selectedIndex", "0");
            document.getElementById(prefix + 'ddlDestination').setAttribute("selectedIndex", "0");
            document.getElementById(prefix + 'ddlDiametrs').setAttribute("selectedIndex", "0");
            document.getElementById(prefix + 'ddlThickneses').setAttribute("selectedIndex", "0");
            document.getElementById(prefix + 'ddlSteelMarks').setAttribute("selectedIndex", "0");
            document.getElementById(prefix + 'ddlOperators').setAttribute("selectedIndex", "0");
            document.getElementById(prefix + 'ddlPresentation').setAttribute("selectedIndex", "0");

            document.getElementById(prefix + 'cbxStack').setAttribute("checked", "");
            document.getElementById(prefix + 'cbxPocket').setAttribute("checked", "");
            document.getElementById(prefix + 'cbxDiameter').setAttribute("checked", "");
            //        document.getElementById(prefix + 'cbxThickness').setAttribute("checked", ""); 
            document.getElementById(prefix + 'cbxSteelMark').setAttribute("checked", "");
            document.getElementById(prefix + 'cbxDefect').setAttribute("checked", "");
            document.getElementById(prefix + 'cbxND').setAttribute("checked", "");
            document.getElementById(prefix + 'cbxDestination').setAttribute("checked", "");
            document.getElementById(prefix + 'cbxOperator').setAttribute("checked", "");
            document.getElementById(prefix + 'cbxPresentation').setAttribute("checked", "");
            document.getElementById(prefix + 'tbFilterPipeNumber').value = '';
            document.getElementById(prefix + 'tbFilterNotes').value = '';


            return false;
        }

        function GetDataFromClipboard() {
            //window.clipboardData.setData("text1","It's copy result");
            hfClipboardInput = document.getElementById(prefix + "hfClipboardInput");
            if (hfClipboardInput != null) {
                hfClipboardInput.setAttribute("value", window.clipboardData.getData("text"));
                return true;
            }
            return true;
        }

        //Скрипт для вставки контролов в ячейки таблицы приход
        function InsertActAddControls(RowIndex) {

            //вставка элементов управления в ячейки
            table = document.getElementById(prefix + "tblPrihod");

            temp = table.rows[RowIndex].cells[2].innerText;
            table.rows[RowIndex].cells[2].innerText = '';
            table.rows[RowIndex].cells[2].insertBefore(document.getElementById(prefix + "tbPrihodPipeNumber"));
            document.getElementById(prefix + "tbPrihodPipeNumber").value = temp;
            document.getElementById(prefix + "tbPrihodPipeNumber").focus();

            temp = table.rows[RowIndex].cells[3].innerText;
            table.rows[RowIndex].cells[3].innerText = '';
            table.rows[RowIndex].cells[3].insertBefore(document.getElementById(prefix + "chbNZP"));
            if (temp != '') document.getElementById(prefix + "chbNZP").chacked = true;
            else document.getElementById(prefix + "chbNZP").chacked = false;

            table.rows[RowIndex].cells[4].innerText = '';
            table.rows[RowIndex].cells[4].insertBefore(document.getElementById(prefix + "ddlPrihodDefect"));

            temp = table.rows[RowIndex].cells[5].innerText;
            table.rows[RowIndex].cells[5].innerText = '';
            table.rows[RowIndex].cells[5].insertBefore(document.getElementById(prefix + "tbPrihodNotes"));
            document.getElementById(prefix + "tbPrihodNotes").value = temp;

        }


        //Подсветка активной строки
        function RowClick(row_id, table_id) {
            table_ = document.getElementById(table_id);
            if (table_ == null) return;
            oldselrow = document.getElementById(table_.getAttribute("selectedrow"));
            newselrow = document.getElementById(row_id);
            if (oldselrow != null) {
                oldselrow.removeAttribute("bgColor");
                oldselrow.removeAttribute("textDecoration");
            }
            table_.setAttribute("selectedrow", row_id);
            if (newselrow != null) {
                newselrow.setAttribute("bgColor", "#E6E6F0");
                newselrow.setAttribute('textDecoration', 'underline');
            }

        }

        function TableKeyDown(e, table_id) {
            table_ = document.getElementById(table_id);
            if (table_ == null) return;
            currselrow_ = document.getElementById(table_.getAttribute("selectedrow"));
            if (currselrow_ == null) return;
            if (e.keyCode == 0x26) rowx = currselrow_.previousSibling;
            if (e.keyCode == 0x28) rowx = currselrow_.nextSibling;
            if (rowx == null) return;
            RowClick(rowx.getAttribute("id"), table_id);
            //rowx.focus(); 
        }

        function RemindChecked() {
            var hFld = document.getElementById(prefix + "hfldSelect");
            var hFldID = document.getElementById(prefix + "hfldID");
            var str = "";
            var indList = hFldID.value.split('~');
            var chbxCount = indList.length;
            var cntSel = 0;
            for (i = 0; i < chbxCount; i++) {
                var chbx = document.getElementById(prefix + indList[i]);
                if (chbx.checked) { str += "1"; cntSel++; }
                else str += "0";
                str += "~";
            }

            // ставим/снимаем галочку общего выделения
            document.getElementById(prefix + "chbx_ALL").checked = cntSel == chbxCount;

            document.getElementById(prefix + "btnMove").disabled = cntSel == 0;

            hFld.value = str;
        }

        // функция обрабатывает добавление трубы на промышленном планшете
        function KeyFromMobileBRC() {

            var myParam = location.search.split('op_mode=')[1] ? location.search.split('op_mode=')[1] : 'web';
            if (myParam == 'mobile') {
                var Barcode = document.getElementById('<%=txbBarcode.ClientID%>').value;
                if (Barcode.Text != "") {
                    __doPostBack('CONFIRM_LINE_MOBILE', document.getElementById(prefix + "hfROW_IDmobile").value);
                    return false;
                }
            }
            return true;
        }

        function SelectAll() {
            var hFld = document.getElementById(prefix + "hfldSelect");
            var hFldID = document.getElementById(prefix + "hfldID");

            if (hFldID.value == "") return;

            var select = document.getElementById(prefix + "chbx_ALL").checked;
            var str = "";
            var indList = hFldID.value.split('~');
            var chbxCount = indList.length;
            for (i = 0; i < chbxCount; i++) {
                var chbx = document.getElementById(prefix + indList[i]);
                if (select) str += "1";
                else str += "0";
                str += "~";
                chbx.checked = select;
            }
            document.getElementById(prefix + "btnMove").disabled = !select;

            hFld.value = str;
        }

        

    </script>
    <style type="text/css">
        .css_td {
            padding: 2px;
            border: 1px Solid #778899;
            border-collapse: collapse;
            text-align: center;
            border-bottom: none;
        }

        .ActivatedTabStyle {
            background-color: #ffffff;
            padding-top: 4px;
            padding-bottom: 4px;
            padding-left: 8px;
            padding-right: 8px;
            border-top: gray 1px solid;
            /*border-bottom: gray 1px solid;*/
            /*border-left: gray 1px solid;*/
            border-right: gray 1px solid;
            text-align: center;
            color: Black;
            /*font-family:verdana;*/
            font-size: 11pt;
            font-weight: bold;
        }

            .ActivatedTabStyle a {
                color: Black;
                text-decoration: none;
            }

        .DeactivatedTabStyle {
            background-color: #E0E0E0;
            padding-top: 4px;
            padding-bottom: 4px;
            padding-left: 8px;
            padding-right: 8px;
            border-top: gray 1px solid;
            border-bottom: gray 1px solid;
            /*border-left: gray 1px solid;*/
            border-right: gray 1px solid;
            text-align: center;
            color: Gray;
            /*font-family:verdana;*/
            font-size: 11pt;
            font-weight: bold;
        }

            .DeactivatedTabStyle a {
                color: Gray;
                text-decoration: none;
            }

        .FirstTabStyle {
            border-bottom: gray 1px solid;
            border-right: gray 1px solid;
            /*background-color: #E7E7FF;*/
            height: 28px;
            width: 1px;
        }

        .LastTabStyle {
            border-bottom: gray 1px solid;
            /*background-color: #E7E7FF;*/
            height: 28px;
        }

        .style2 {
            width: 326px;
            height: 20px;
        }

        .style3 {
            width: 40px;
            height: 20px;
        }

        .style4 {
            height: 20px;
        }

        .style5 {
            width: 81px;
        }

        .style6 {
            width: 148px;
        }

        .style7 {
            height: 23px;
        }

        .style8 {
            height: 23px;
            width: 190px;
        }

        .style10 {
            width: 48px;
        }

        .style11 {
            height: 47px;
            width: 48px;
        }

        .style12 {
            width: 190px;
        }

        .style15 {
            height: 23px;
            width: 189px;
        }

        .style16 {
            width: 189px;
        }

        .style17 {
            height: 23px;
            width: 170px;
        }

        .style18 {
            width: 170px;
        }

        .style19 {
            width: 450px;
        }
        .auto-style1 {
            height: 28px;
            width: 1184px;
        }
        .auto-style7 {
            height: 15px;
            width: 472px;
        }
        
        .auto-style8 {
            width: 243px;
        }
        .auto-style9 {
            height: 15px;
            width: 243px;
        }
        .auto-style10 {
            width: 240px;
            height: 15px;
        }
        
        .auto-style11 {
            height: 15px;
            width: 242px;
        }
        
    </style>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainPlaceHolder" runat="Server">
    <asp:Panel ID="Panel1" runat="server" BackColor="#E0E0E0" Style="padding-right: 8px; padding-left: 8px; padding-bottom: 8px; padding-top: 12px; border-bottom-width: 1px; border-bottom-color: gray;"
        Width="1205px" Height="30px">
        <table width="auto">
            <tr>
                <td style="vertical-align: bottom;" class="auto-style1">
                    <span style="font-size: 11pt">&nbsp;Складской объект
                        <asp:DropDownList ID="ddlZone" runat="server" AutoPostBack="True"
                            Font-Bold="True" Height="17px" TabIndex="1"
                            Width="305px" OnSelectedIndexChanged="ddlZone_SelectedIndexChanged">
                            <asp:ListItem></asp:ListItem>
                        </asp:DropDownList>&nbsp;Штабель
                        <asp:DropDownList ID="ddlStack_name" runat="server" AutoPostBack="True"
                                          Font-Bold="True" Height="17px" TabIndex="1"
                                          Width="100px" OnSelectedIndexChanged="ddlStack_Change">
                            <asp:ListItem></asp:ListItem>
                        </asp:DropDownList>&nbsp;Карман
                        <asp:DropDownList ID="ddlPocket_name" runat="server" AutoPostBack="True"
                                          Font-Bold="True" Height="17px" TabIndex="1"
                                          Width="100px">
                            <asp:ListItem></asp:ListItem>
                            <asp:ListItem>1</asp:ListItem>
                            <asp:ListItem>2</asp:ListItem>
                            <asp:ListItem>3</asp:ListItem>
                            <asp:ListItem>4</asp:ListItem>
                            <asp:ListItem>5</asp:ListItem>
                            <asp:ListItem>6</asp:ListItem>
                        </asp:DropDownList>
                        &nbsp;<span style="font-size: 11pt">&nbsp; Смена
                    <asp:Label ID="lblShift" runat="server" BackColor="White" BorderColor="Gray"
                        BorderStyle="Solid" BorderWidth="1px" Height="20px" Style="padding-right: 4px; padding-left: 4px; text-align: center"
                        Width="50px"></asp:Label>
                        </span>
                    </span>
                    <asp:TextBox ID="txbBarcode" runat="server" AutoPostBack="True" Font-Bold="False"  OnKeyUp="KeyFromMobileBRC()"
                                 Font-Size="8pt" OnTextChanged="txbBarcode_TextChanged" TabIndex="7"
                                 Width="199px"></asp:TextBox>
                    <asp:Button ID="btnReadBarcode" runat="server" Height="26px" OnClick="btnReadBarcode_Click"
                                Text="Штрихкод" Width="76px" />
                </td>
            </tr>
        </table>
    </asp:Panel>
    <asp:MultiView ID="MainMultiView" runat="server" ActiveViewIndex="0">
        <asp:View ID="vNotLoginedView" runat="server">
            <asp:Label ID="lblEnterDataMsg" runat="server" Font-Size="10pt" Text="<br/>Для начала работы необходимо указать название складского объекта" Height="24px"></asp:Label>
        </asp:View>
        <asp:View ID="vLogined" runat="server">
            <asp:Panel
                ID="pnlTabs" runat="server" Style="margin-top: 16px;" Height="28px">
                <table cellspacing="0" height="24" style="display: inline; width: 100%">

                    <tr>
                        <td class="FirstTabStyle">&nbsp;</td>
                        <td id="tdPrihod" runat="server" class="DeactivatedTabStyle" style="width: 170px; text-decoration: none;">
                            <asp:LinkButton ID="btnPrihod" runat="server"
                                OnClick="btnPrihod_Click">Приход</asp:LinkButton>
                        </td>
                        <td id="tdRelocation" runat="server" class="DeactivatedTabStyle" style="width: 170px;">
                            <asp:LinkButton ID="btnRelocation" runat="server" OnClick="btnRelocation_Click">Перемещение</asp:LinkButton>
                        </td>
                        <td id="tdManualInput" runat="server" class="DeactivatedTabStyle" style="width: 170px; text-decoration: none;">
                            <asp:LinkButton ID="btnManualInput" runat="server"
                                OnClick="btnManualInput_Click">Склад</asp:LinkButton>
                        </td>
                        <!--<td id="tdOtgruzka" runat="server" class="DeactivatedTabStyle" style="width: 170px; text-decoration:none;">
                        <asp:LinkButton ID="btnOtgruzka" runat="server" 
                            onclick="btnOtgruzka_Click">Отгрузка</asp:LinkButton>
                    </td>-->
                        <td id="tdOperationsHistory" runat="server" class="DeactivatedTabStyle" style="width: 170px;">
                            <asp:LinkButton ID="btnOperationsHistory" runat="server"
                                OnClick="btnOperationsHistory_Click">История операций</asp:LinkButton>
                        </td>
                        <td id="tdFilter" runat="server" class="ActivatedTabStyle" style="width: 170px;">
                            <asp:LinkButton ID="btnFilter" runat="server" OnClick="btnFilter_Click">Фильтрация</asp:LinkButton>
                        </td>
                        <td class="LastTabStyle">&nbsp;</td>
                    </tr>

                </table>
            </asp:Panel>
            <asp:Panel ID="pnlViews" runat="server" Style="border-bottom: gray 1px solid; border-left: gray 1px solid; border-right: gray 1px solid; padding-top: 20px; padding-bottom: 20px; padding-left: 20px; padding-right: 20px;">
                <asp:MultiView ID="mvMainViews" runat="server" ActiveViewIndex="0">
                    <asp:View ID="vMainFilter" runat="server">
                        <table style="font-size: 10pt">

                            <tr>
                                <td style="padding-left: 10px; vertical-align: bottom; width: 182px; height: 15px; text-align: left"></td>
                                <td style="padding-left: 10px; vertical-align: bottom; width: 165px; height: 15px; text-align: left">&nbsp;</td>
                            </tr>
                            <tr>
                                <td style="padding-left: 10px; vertical-align: bottom; width: 182px; height: 15px; text-align: left"><%--Штабель--%> <asp:CheckBox ID="cbxStack" runat="server" __designer:wfdid="w32"
                                    Text="<%--инв--%>" TextAlign="Left" Width="41px" Visible="False"/>
                                    </td>
                                <td style="padding-left: 10px; vertical-align: bottom; width: 165px; height: 15px; text-align: left">
                                    <asp:DropDownList ID="ddlStack" runat="server" __designer:wfdid="w33" Height="16px" Width="160px" AutoPostBack="True" OnSelectedIndexChanged="ddlStack_SelectedIndexChanged" Visible="False">
                                    </asp:DropDownList>
                                </td>
                            </tr>
                            <tr style="color: #000000">
                                <td style="padding-left: 10px; vertical-align: bottom; width: 182px; height: 15px; text-align: left"><%--Карман--%> <asp:CheckBox ID="cbxPocket" runat="server" __designer:wfdid="w34"
                                    Text="<%--инв--%>" TextAlign="Left" Width="41px" Visible="False"/>
                                    </td>
                                <td style="padding-left: 10px; vertical-align: bottom; width: 165px; height: 15px; text-align: left">
                                    <asp:DropDownList ID="ddlPocket" runat="server" __designer:wfdid="w35" TabIndex="8" Width="160px" AutoPostBack="True" Visible="False">
                                        <asp:ListItem>(Все)</asp:ListItem>
                                        <asp:ListItem>1</asp:ListItem>
                                        <asp:ListItem>2</asp:ListItem>
                                        <asp:ListItem>3</asp:ListItem>
                                        <asp:ListItem>4</asp:ListItem>
                                        <asp:ListItem>5</asp:ListItem>
                                        <asp:ListItem>6</asp:ListItem>
                                    </asp:DropDownList>
                                </td>
                            </tr>
                            <tr>
                                <td style="padding-left: 10px; vertical-align: bottom; width: 182px; height: 15px; text-align: left"></td>
                                <td style="padding-left: 10px; vertical-align: bottom; width: 165px; height: 15px; text-align: left"></td>
                            </tr>

                        </table>
                    </asp:View>
                </asp:MultiView>
                <asp:MultiView ID="mvViews" runat="server">
                    <asp:View ID="vOperationsHistory" runat="server"
                        OnPreRender="vOperationsHistory_PreRender">
                        <table>
                            <tr style="vertical-align: middle">
                                <td class="style2">
                                    <asp:Label ID="lblOperHistTitleBegin" runat="server" Font-Bold="True"
                                        Font-Size="10pt" Text="Список операций, произведённых за последние"></asp:Label>
                                </td>
                                <td class="style3">

                                    <asp:DropDownList ID="ddlCountOfLastHours" runat="server" Font-Bold="True"
                                        Font-Size="8pt" AutoPostBack="True">
                                        <asp:ListItem>1</asp:ListItem>
                                        <asp:ListItem>2</asp:ListItem>
                                        <asp:ListItem>4</asp:ListItem>
                                        <asp:ListItem>6</asp:ListItem>
                                        <asp:ListItem>8</asp:ListItem>
                                        <asp:ListItem Selected="True">12</asp:ListItem>
                                        <asp:ListItem>24</asp:ListItem>
                                    </asp:DropDownList>

                                </td>
                                <td class="style4">
                                    <asp:Label ID="lblOperHistTitleEnd" runat="server" Font-Bold="True"
                                        Font-Size="10pt" Text="часов"></asp:Label>
                                </td>
                            </tr>
                        </table>
                        <asp:Table ID="tblOperationsHistory" runat="server" CellPadding="3"
                            CellSpacing="0" Font-Size="9pt">
                            <asp:TableRow runat="server" BackColor="LightGray" Font-Bold="True"
                                HorizontalAlign="Center" VerticalAlign="Middle" Height="30px">
                                <asp:TableCell runat="server" BorderColor="Black" BorderStyle="Solid" BorderWidth="1px" Width="50px">№ п/п</asp:TableCell>
                                <asp:TableCell runat="server" BorderColor="Black" BorderStyle="Solid" BorderWidth="1px" Width="130px">Дата</asp:TableCell>
                                <asp:TableCell runat="server" BorderColor="Black" BorderStyle="Solid" BorderWidth="1px" Width="200px">Оператор</asp:TableCell>
                                <asp:TableCell runat="server" BorderColor="Black" BorderStyle="Solid" BorderWidth="1px" Width="100px">Тип операции</asp:TableCell>
                                <asp:TableCell runat="server" BorderColor="Black" BorderStyle="Solid" BorderWidth="1px" Width="550px">Дополнительная 
                        информация</asp:TableCell>
                                <asp:TableCell runat="server" BorderColor="Black" BorderStyle="Solid"
                                    BorderWidth="1px">Примечание</asp:TableCell>
                            </asp:TableRow>
                            <asp:TableRow runat="server" HorizontalAlign="Center" VerticalAlign="Middle">
                                <asp:TableCell runat="server" BorderColor="Black" BorderStyle="Solid"
                                    BorderWidth="1px" Width="50px">1</asp:TableCell>
                                <asp:TableCell runat="server" BorderColor="Black" BorderStyle="Solid"
                                    BorderWidth="1px" Width="130px">01.06.2009 11:17:07</asp:TableCell>
                                <asp:TableCell runat="server" BorderColor="Black" BorderStyle="Solid"
                                    BorderWidth="1px" Width="200px">Иванов Иван Иванович</asp:TableCell>
                                <asp:TableCell runat="server" BorderColor="Black" BorderStyle="Solid"
                                    BorderWidth="1px" Width="100px">Исправление</asp:TableCell>
                                <asp:TableCell runat="server" BorderColor="Black" BorderStyle="Solid"
                                    BorderWidth="1px" Width="550px">Номер трубы исходный: 8888888, новый: 9999999</asp:TableCell>
                                <asp:TableCell runat="server" BorderColor="Black" BorderStyle="Solid"
                                    BorderWidth="1px">Примечание</asp:TableCell>
                            </asp:TableRow>
                        </asp:Table>
                    </asp:View>
                    <asp:View ID="vOtgruzka" runat="server">
                    </asp:View>
                    <asp:View ID="vSklad" runat="server">
                        <table style="width: 100%;">
                            <tr>
                                <td><asp:label id="CountOfPipes" runat="server" Visible ="False"></asp:label><asp:label id="SkladCountPipes" runat="server" Visible ="False"></asp:label><asp:label id="LabelQNT_T" runat="server" Visible ="False"></asp:label><asp:label id="LabelPodItogRow" runat="server" Visible ="False"></asp:label><asp:label id="LabelLastPocket" runat="server" Visible ="False"></asp:label></td>
                            </tr>
                            <tr>
                                <td>
                                    <span style="font-size: 11pt">
                                        <asp:Table ID="tblSklad" runat="server" BorderColor="Black" BorderStyle="Solid"
                                            BorderWidth="1px" CellPadding="1" CellSpacing="0" EnableViewState="False"
                                            Font-Size="8pt">
                                        </asp:Table>
                                    </span>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Button ID="btnDelAll" runat="server" Text="Удалить все"
                                        OnClick="btnDelAll_Click"
                                        OnClientClick="if(confirm('вопрос')) return true; return false;" />
                                </td>
                            </tr>
                        </table>
                    </asp:View>
                    <asp:View ID="vRelocation" runat="server">
                        <table style="border-collapse: collapse">
                            <tr>
                                <td class="style19">
                                    <strong>Откуда</strong></td>
                                <td class="style10">&nbsp;</td>
                                <td>
                                    <table style="width: 100%;">
                                        <tr>
                                            <td>
                                                <strong>Куда</strong></td>
                                            <td>&nbsp;</td>
                                            <td style="text-align: right">
                                                <asp:Button ID="btnMoveUpdate" runat="server" OnClick="btnMoveUpdate_Click"
                                                    Text="Обновить" />
                                            </td>
                                        </tr>
                                    </table>
                                </td>
                            </tr>
                            <tr>
                                <td style="border-width: 1px; border-color: #808080; border-right-style: solid; border-left-style: solid; border-top-style: solid;"
                                    class="style19">Сейчас в кармане находится: <asp:label id="LabelFromCount" runat="server"> </asp:label> труб
                                </td>
                                <td class="style10"></td>
                                <td style="border-width: 1px; border-color: #808080; border-right-style: solid; border-left-style: solid; border-top-style: solid;">
                                    Сейчас в кармане находится: <asp:label id="LabelToCount" runat="server"></asp:label> труб
                                    <table>
                                        <tr>
                                            <td class="style8">Складской объект</td>
                                            <td class="style17">Штабель</td>
                                            <td class="style7">Карман</td>
                                        </tr>
                                        <tr>
                                            <td class="style12">
                                                <span style="font-size: 11pt">
                                                    <asp:DropDownList ID="ddlRelocationZone" runat="server" AutoPostBack="True"
                                                        Font-Bold="False" Height="17px"
                                                        OnSelectedIndexChanged="ddlRelocationZone_Change" TabIndex="1"
                                                        Width="180px">
                                                        <asp:ListItem></asp:ListItem>
                                                    </asp:DropDownList>
                                                </span>
                                            </td>
                                            <td class="style18">
                                                <asp:DropDownList ID="ddlRelocationStack" runat="server" __designer:wfdid="w33"
                                                    Height="16px" Width="160px" AutoPostBack="True"
                                                    OnSelectedIndexChanged="ddlRelocationStack_Change">
                                                    <asp:ListItem></asp:ListItem>
                                                </asp:DropDownList>
                                            </td>
                                            <td>
                                                <asp:DropDownList ID="ddlRelocationPocket" runat="server"
                                                    __designer:wfdid="w35" TabIndex="8" Width="80px" AutoPostBack="True">
                                                    <asp:ListItem></asp:ListItem>
                                                    <asp:ListItem>1</asp:ListItem>
                                                    <asp:ListItem>2</asp:ListItem>
                                                    <asp:ListItem>3</asp:ListItem>
                                                    <asp:ListItem>4</asp:ListItem>
                                                    <asp:ListItem>5</asp:ListItem>
                                                    <asp:ListItem>6</asp:ListItem>
                                                </asp:DropDownList>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td class="style12">&nbsp;</td>
                                            <td class="style18">&nbsp;</td>
                                            <td>&nbsp;</td>
                                        </tr>
                                    </table>
                                </td>
                            </tr>
                            <tr>
                                <td style="padding: 4px; border-width: 1px; border-color: #808080; vertical-align: top; border-right-style: solid; border-bottom-style: solid; border-left-style: solid;"
                                    class="style19">
                                    <asp:Table ID="tblFrom" runat="server">
                                    </asp:Table>
                                    <asp:Label ID="lblFrom" runat="server" Font-Bold="True" Font-Size="10pt"
                                        Text="Выберете складской объект штабель и карман, откуда перемещать"></asp:Label>
                                </td>
                                <td style="vertical-align: top; text-align: center; padding-right: 10px; padding-left: 10px;"
                                    class="style11">
                                    <asp:Button ID="btnMove" runat="server" Text="&gt;&gt;" Enabled="False"
                                        OnClick="btnMove_Click" OnClientClick="return PostBackByButton(this);"
                                        ToolTip="Переместить" Height="50px" />
                                </td>
                                <td style="padding: 4px; border-width: 1px; border-color: #808080; vertical-align: top; border-right-style: solid; border-bottom-style: solid; border-left-style: solid;">
                                    <asp:Table ID="tblTo" runat="server">
                                    </asp:Table>
                                    <asp:Label ID="lblTo" runat="server" Font-Bold="True" Font-Size="10pt"
                                        Text="Выберете складской объект штабель и карман, куда перемещать"></asp:Label>
                                </td>
                            </tr>
                        </table>
                        <table style="font-size: 10pt">
                            <tr>
                                <td style="padding-left: 10px; vertical-align: bottom; text-align: center">&nbsp;</td>
                            </tr>
                            <tr>
                                <td style="padding-left: 10px;">&nbsp;</td>
                            </tr>
                            <tr>
                                <td style="padding-left: 10px; vertical-align: bottom; text-align: left">
                                    <strong>История перемещения труб</strong>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Table ID="tblRelocation" runat="server" CellPadding="1" CellSpacing="0"
                                        Font-Size="8pt" BorderColor="Black" BorderStyle="Solid" BorderWidth="1px">
                                    </asp:Table>
                                </td>
                            </tr>
                            <tr>
                                <td style="text-align: right">
                                    <asp:HiddenField ID="hfldID" runat="server" />
                                    <asp:HiddenField ID="hfldSelect" runat="server" />
                                </td>
                            </tr>
                        </table>
                    </asp:View>

                    <asp:View ID="vPrihod" runat="server">
                        <table>
                            <tr>
                                <td>Сейчас в кармане находится: <asp:label id="LabelPrihod" runat="server"></asp:label> труб<td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Table ID="tblPrihod" runat="server" BorderColor="Black"
                                        BorderStyle="Solid" BorderWidth="1px" CellPadding="1" CellSpacing="0"
                                        Font-Size="8pt">
                                    </asp:Table>
                                </td>
                            </tr>
                        </table>
                        <table>
                            <tr>
                                <td>&nbsp;</td>
                                <td>&nbsp;</td>
                                <td>
                                    <asp:TextBox ID="tbPrihodPipeNumber" runat="server" Font-Size="8pt" Height="20px" Width="60px" MaxLength="8" OnKeyUp="KeyFromMobile()" ></asp:TextBox>
                                </td>
                                <td class="style5">
                                    <asp:DropDownList ID="ddlPrihodDefect" runat="server" Font-Size="8pt"
                                        Width="250px">
                                    </asp:DropDownList>
                                </td>
                                <td class="style6">
                                    <asp:TextBox ID="tbPrihodNotes" runat="server" Font-Size="8pt" Height="20px" Width="250px"></asp:TextBox>
                                </td>
                                <td class="style6">
                                    <asp:CheckBox ID="chbNZP" runat="server" Font-Bold="False" />
                                </td>
                            </tr>
                        </table>
                    </asp:View>
                    <asp:View ID="vFilter" runat="server">
                        <asp:Panel ID="pnlFilterBy" runat="server" BorderColor="Silver"
                            BorderWidth="1px" Height="50px" Width="720px">
                            <table style="font-size: 10pt">
                                <tr>
                                    <td class="auto-style8"></td>
                                </tr>
                                <tr>
                                    <td style="padding-left: 10px; vertical-align: bottom; text-align: left" class="auto-style9">Номер трубы</td>
                                    <td style="padding-left: 10px; vertical-align: bottom; width: 473px; height: 15px; text-align: left">
                                        <asp:TextBox ID="tbFilterPipeNumber" runat="server" Font-Size="10pt"
                                            Height="20px" Width="124px"></asp:TextBox>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding-left: 10px; vertical-align: bottom; text-align: left" class="auto-style9">Диаметр, мм (<asp:CheckBox ID="cbxDiameter" runat="server" Text="инв"
                                        TextAlign="Left" Width="41px" />
                                        )</td>
                                    <td style="padding-left: 10px; vertical-align: bottom; width: 473px; height: 15px; text-align: left">
                                        <asp:DropDownList ID="ddlDiametrs" runat="server" TabIndex="8" Width="149px">
                                        </asp:DropDownList>
                                    </td>
                                </tr>
                                <tr style="color: #000000">
                                    <td style="padding-left: 10px; vertical-align: bottom; text-align: left" class="auto-style9">Толщина стенки, мм (<asp:CheckBox ID="cbxThickness" runat="server" Text="инв"
                                        TextAlign="Left" Width="41px" />
                                        )</td>
                                    <td style="padding-left: 10px; vertical-align: bottom; width: 473px; height: 15px; text-align: left">
                                        <asp:DropDownList ID="ddlThickneses" runat="server" TabIndex="9" Width="176px">
                                        </asp:DropDownList>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding-left: 10px; vertical-align: bottom; text-align: left" class="auto-style9">Марка стали (<asp:CheckBox ID="cbxSteelMark" runat="server" Text="инв"
                                        TextAlign="Left" Width="41px" />
                                        )</td>
                                    <td style="padding-left: 10px; vertical-align: bottom; width: 473px; height: 15px; text-align: left">
                                        <asp:DropDownList ID="ddlSteelMarks" runat="server" TabIndex="10" Width="233px">
                                        </asp:DropDownList>
                                    </td>
                                </tr>
                            </table>
                            <table style="font-size: 10pt; width: auto;">
                                <tr>
                                    <td style="padding-left: 10px; vertical-align: bottom; text-align: left" class="auto-style11">Дефект (<asp:CheckBox ID="cbxDefect" runat="server" Text="инв" TextAlign="Left"
                                        Width="41px" />
                                        )
                                    </td>
                                    <td style="padding-left: 10px; vertical-align: bottom; width: 473px; height: 15px; text-align: left">
                                        <asp:DropDownList ID="ddlDefects" runat="server" Width="100%">
                                        </asp:DropDownList>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding-left: 10px; vertical-align: bottom; text-align: left" class="auto-style11">Гост (<asp:CheckBox ID="cbxGost" runat="server" Text="инв" TextAlign="Left"
                                        Width="41px" />
                                        )
                                    </td>
                                    <td style="padding-left: 10px; vertical-align: bottom; width: 473px; height: 15px; text-align: left">
                                        <asp:DropDownList ID="ddlGost" runat="server" Height="16px" Width="258px">
                                        </asp:DropDownList>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding-left: 10px; vertical-align: bottom; text-align: left" class="auto-style11">Группа (<asp:CheckBox ID="cbxGroup" runat="server" Text="инв" TextAlign="Left"
                                        Width="41px" />
                                        )</td>
                                    <td style="padding-left: 10px; vertical-align: bottom; width: 473px; height: 15px; text-align: left">
                                        <asp:DropDownList ID="ddlGroup" runat="server" Height="16px" Width="258px">
                                        </asp:DropDownList>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding-left: 10px; vertical-align: bottom; text-align: left" class="auto-style11">Назначение (<asp:CheckBox ID="cbxDestination" runat="server" Text="инв"
                                        TextAlign="Left" Width="41px" />
                                        )</td>
                                    <td style="padding-left: 10px; vertical-align: bottom; width: 473px; height: 15px; text-align: left">
                                        <asp:DropDownList ID="ddlDestination" runat="server" Font-Bold="False"
                                            Width="228px">
                                        </asp:DropDownList>
                                    </td>
                                </tr>
                            </table>
                            <table style="font-size: 10pt">
                                <tr>
                                    <td style="padding-left: 10px; vertical-align: bottom; text-align: left" class="auto-style10">Оператор (<asp:CheckBox ID="cbxOperator" runat="server" Text="инв"
                                        TextAlign="Left" Width="41px" />
                                        )</td>
                                    <td style="padding-left: 10px; vertical-align: bottom; text-align: left" class="auto-style7">
                                        <asp:DropDownList ID="ddlOperators" runat="server" Width="265px">
                                        </asp:DropDownList>
                                        &nbsp;
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding-left: 10px; vertical-align: bottom; text-align: left" class="auto-style10">Предъявление (<asp:CheckBox ID="cbxPresentation" runat="server" Text="инв"
                                        TextAlign="Left" Width="41px" />
                                        )</td>
                                    <td style="padding-left: 10px; vertical-align: bottom; text-align: left" class="auto-style7">
                                        <asp:DropDownList ID="ddlPresentation" runat="server" TabIndex="8"
                                            Width="160px">
                                            <asp:ListItem>(Все)</asp:ListItem>
                                            <asp:ListItem Value="1">Для предъявления</asp:ListItem>
                                            <asp:ListItem Value="0">Не для предъявления</asp:ListItem>
                                        </asp:DropDownList>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding-left: 10px; vertical-align: bottom; text-align: left" class="auto-style10">Год-номер ведомости (<asp:CheckBox ID="cbxSheet" runat="server" Text="инв"
                                        TextAlign="Left" Width="41px" />
                                        )</td>
                                    <td style="padding-left: 10px; vertical-align: bottom; text-align: left" class="auto-style7">
                                        <asp:DropDownList ID="ddlSheet" runat="server" TabIndex="8" Width="160px">
                                            <asp:ListItem>(Все)</asp:ListItem>
                                            <asp:ListItem Value="1">Для предъявления</asp:ListItem>
                                            <asp:ListItem Value="0">Не для предъявления</asp:ListItem>
                                        </asp:DropDownList>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding-left: 10px; vertical-align: bottom; text-align: left" class="auto-style10">Номер номенклатуры трубы</td>
                                    <td style="padding-left: 10px; vertical-align: bottom; text-align: left" class="auto-style7">
                                        <asp:TextBox ID="tbItemNumber" runat="server" Height="20px" Width="100%"></asp:TextBox>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding-left: 10px; vertical-align: bottom; text-align: left" class="auto-style10">Номер производственного заказа</td>
                                    <td style="padding-left: 10px; vertical-align: bottom; text-align: left" class="auto-style7">
                                        <asp:TextBox ID="tbPipeOrderNumber" runat="server" Height="20px" Width="100%"></asp:TextBox>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding-left: 10px; vertical-align: bottom; text-align: left" class="auto-style10">Величина неснятого грата от и до, мм</td>
                                    <td style="padding-left: 10px; vertical-align: bottom; text-align: left" class="auto-style7">ОТ &nbsp;
                                        <asp:TextBox ID="tbGrat" runat="server" Height="20px" Width="37%"></asp:TextBox>
                                        &nbsp; ДО &nbsp;<asp:TextBox ID="tbGratTo" runat="server" Height="20px" Width="33%"></asp:TextBox>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding-left: 10px; vertical-align: bottom; text-align: left" class="auto-style10">Длина трубы от и до, мм</td>
                                    <td style="padding-left: 10px; vertical-align: bottom; text-align: left" class="auto-style7">ОТ &nbsp;
                                        <asp:TextBox ID="tbPipeLenght" runat="server" Height="20px" Width="37%"></asp:TextBox>
                                        &nbsp; ДО &nbsp;<asp:TextBox ID="tbPipeLenghtTo" runat="server" Height="20px" Width="33%"></asp:TextBox>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding-left: 10px; vertical-align: bottom; text-align: left" class="auto-style10">Название внешней инспекции</td>
                                    <td style="padding-left: 10px; vertical-align: bottom; text-align: left" class="auto-style7">
                                        <asp:DropDownList ID="ddlInspection" runat="server" Height="16px" Width="258px">
                                        </asp:DropDownList>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding-left: 10px; vertical-align: bottom; text-align: left" class="auto-style10">Доп. параметр для поиска</td>
                                    <td style="padding-left: 10px; vertical-align: bottom; text-align: left" class="auto-style7">
                                        <asp:TextBox ID="tbDP" runat="server" Height="20px" Width="100%"></asp:TextBox>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding-left: 10px; vertical-align: bottom; text-align: left" class="auto-style10">Примечание</td>
                                    <td style="padding-left: 10px; vertical-align: bottom; text-align: left" class="auto-style7">
                                        <asp:TextBox ID="tbFilterNotes" runat="server" Height="20px" Width="100%"></asp:TextBox>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding-left: 10px; vertical-align: bottom; text-align: left" class="auto-style10"></td>
                                    <td style="padding-left: 10px; vertical-align: bottom; text-align: left" class="auto-style7"></td>
                                </tr>
                                <tr>
                                    <td style="padding-left: 10px; vertical-align: bottom; text-align: left" class="auto-style10"></td>
                                    <td style="padding-left: 10px; vertical-align: bottom; text-align: left" class="auto-style7"></td>
                                </tr>
                                <tr>
                                    <td style="padding-left: 10px; vertical-align: bottom; text-align: left" class="auto-style10">&nbsp;</td>
                                    <td style="padding-left: 10px; vertical-align: bottom; text-align: right" class="auto-style7">
                                        <asp:Button ID="btnClearFilter0" runat="server" Height="23px"
                                            OnClientClick="return ClearFilterl(document.getElementById(prefix + 'pnlFilterBy'));"
                                            Text="Сбросить" />
                                        &nbsp;&nbsp;&nbsp;
                                    </td>
                                </tr>
                            </table>
                            <table id="ctl00_MainPlaceHolder_tblLegend0"
                                style="font-size: 8pt; width: 100%; color: #808080">
                                <tr>
                                    <td
                                        style="border-top-width: 1px; padding-left: 10px; border-left-width: 1px; border-left-color: #c0c0c0; border-top-color: #c0c0c0; padding-top: 10px; border-bottom: #c0c0c0 1px solid; border-right-width: 1px; border-right-color: #c0c0c0">Примечания</td>
                                </tr>
                                <tr>
                                    <td style="padding-left: 10px">
                                        <span style="font-size: 8pt">* При активации функции &quot;исключить&quot;, отчёт будет 
                                сформирован без данных с указанным в фильтре значением.</span></td>
                                </tr>
                                <tr>
                                    <td style="padding-left: 10px">
                                        <span style="font-size: 8pt">* При фильтрации по номерам труб, номера указываются через запятую.</span></td>
                                </tr>
                                <tr>
                                    <td style="padding-left: 10px">&nbsp;</td>
                                </tr>
                            </table>
                        </asp:Panel>
                    </asp:View>
                    <br />
                </asp:MultiView>
            </asp:Panel>
            <asp:Panel runat="server" ID="pnlSkladControls">
                <table>
                    <tr>
                        <td style="width: 25px">
                            <asp:DropDownList ID="ddlDefect" runat="server" Width="150px" Font-Size="8pt">
                                <asp:ListItem></asp:ListItem>
                            </asp:DropDownList>
                        </td>
                        <td style="width: 25px">
                            <asp:TextBox ID="tbNotes" runat="server" Width="200px" Font-Size="8pt"></asp:TextBox>
                            <asp:DropDownList ID="ddlNotes" runat="server" Width="200px" Font-Size="8pt">
                                <asp:ListItem></asp:ListItem>
                            </asp:DropDownList>
                        </td>
                        <td style="width: 25px">
                            <asp:TextBox ID="tbPipeNumber" runat="server"  Width="100%"  Font-Size="8pt" onkeypress="EnsureNumeric();" ></asp:TextBox>
                        </td>
                        <td style="width: 25px">
                            <asp:CheckBox ID="cbPresentation" runat="server" />
                        </td>
                        <td style="width: 25px">
                            <asp:CheckBox ID="chbNZP0" runat="server" Font-Bold="False" />
                        </td>
                        <td>
                            <asp:HiddenField ID="hfAutoFillData" runat="server" />

                        </td>
                    </tr>
                </table>

            </asp:Panel>
        </asp:View>
    </asp:MultiView>

<uc2:PopupWindow ID="PopupWindow1" runat="server"
        ContentPanelId="pnlConfirmMove" Title="Перемещение труб" />
    <asp:Panel ID="pnlConfirmMove" runat="server" Height="80px" Width="500px"
        Visible="False">
        <table style="width: 100%;">
            <tr>
                <td>
                    <asp:Label ID="lblMoveDupl" runat="server" Text="Label"></asp:Label>
                </td>
            </tr>
            <tr>
                <td>&nbsp;</td>
            </tr>
            <tr>
                <td style="text-align: right">
                    <asp:Button ID="btnMoveOk" runat="server" Text="OK" OnClick="btnMoveOk_Click"
                        Width="80px" />&nbsp;
                            <asp:Button ID="btnMoveCancel" runat="server" Text="Отмена"
                                OnClick="btnMoveCancel_Click" Width="80px" />
                </td>
            </tr>
        </table>
    </asp:Panel>
<asp:Panel ID="pnlSortMove" runat="server" Height="80px" Width="500px" Visible="false">
    <table style="width: 100%;">
        <tr>
            <td>
                <asp:Label ID="lblSortMove" runat="server" Text="Label"></asp:Label>
            </td>
        </tr>
        <tr>
            <td class="style7"></td>
        </tr>
        <tr>
            <td style="text-align: right">
                <asp:Button ID="btnSortOk" runat="server" Text="OK" Width="80px" OnClick="btnSortOk_Click" />&nbsp;
                <asp:Button ID="btnSortCancel" runat="server" Text="Отмена" Width="80px" OnClick="btnSortCancel_Click" />
            </td>
        </tr>
    </table>
</asp:Panel>
    <asp:Panel ID="pnlConfirmAdd" runat="server" Height="80px" Width="500px" Visible="false">
        <table style="width: 100%;">
            <tr>
                <td>
                    <asp:Label ID="lblAddDupl" runat="server" Text="Label"></asp:Label>
                </td>
            </tr>
            <tr>
                <td class="style7"></td>
            </tr>
            <tr>
                <td style="text-align: right">
                    <asp:Button ID="btnAddOk" runat="server" Text="OK" Width="80px" OnClick="btnAddOk_Click" />&nbsp;
                            <asp:Button ID="btnAddCancel" runat="server" Text="Отмена" Width="80px" OnClick="btnAddCancel_Click" />
                </td>
            </tr>
        </table>
    </asp:Panel>
<asp:Panel ID="pnlPrihodSort" runat="server" Height="80px" Width="500px" Visible="false">
    <table style="width: 100%;">
        <tr>
            <td>
                <asp:Label ID="lblPrihodSort" runat="server" Text="Label"></asp:Label>
            </td>
        </tr>
        <tr>
            <td class="style7"></td>
        </tr>
        <tr>
            <td style="text-align: right">
                <asp:Button ID="btnPrihodOk" runat="server" Text="OK" Width="80px" OnClick="btnPrihodOk_Click" />&nbsp;
                <asp:Button ID="tbnPrihodCancel" runat="server" Text="Отмена" Width="80px" OnClick="btnPrihodCancel_Click" />
            </td>
        </tr>
    </table>
</asp:Panel>
</asp:Content>
