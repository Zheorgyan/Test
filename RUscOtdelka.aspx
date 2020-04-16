<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true"
    CodeFile="RUscOtdelka.aspx.cs" Inherits="RUscOtdelka" Title="РУЗК линий отделки" %>

<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Assembly="System.Web.Extensions, Version=1.0.61025.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35"
    Namespace="System.Web.UI" TagPrefix="asp" %>
    
    
<%@ Register src="PopupWindow.ascx" tagname="PopupWindow" tagprefix="uc1" %>
    
    
<%@ Register src="DateNavigation.ascx" tagname="DateNavigation" tagprefix="uc2" %>
    
    
<asp:Content ID="Content1" ContentPlaceHolderID="TitlePlaceHolder" runat="Server">
    
    <table style="display: inline; font-size: 14pt; font-weight: bold;" cellspacing="0" cellpadding="0">
    <tr>
        <td><img src="Images/OOP.png" style="width: 29px; height: 29px" /></td>
        <td style="padding-left: 8px">РУЗК линий отделки</td>
    </tr>
    </table>

    <script type="text/javascript" language="javascript">
    
    var prefix="ctl00_MainPlaceHolder_";
    
    
    //скрытие окна и его показ через 5 секунд
    function HideWindow()
    {
       //window.moveTo(0,screen.availHeight-30);                
       //setTimeout('ShowWindow()', 5000);       
    }
    
    //отображение окна
    function ShowWindow()
    {
      window.moveTo(0,0); 
      window.resizeTo(screen.availWidth, screen.availHeight); 
      window.focus();
      document.getElementById(prefix+'txbPipeNumber').focus();
    }

    //установка позиции курсора в textBox
    function setCaretPosition(ctrlID, posStart, posEnd)
    {
        var ctrl=document.getElementById(prefix+ctrlID);
    	
    	try
    	{
	        if(ctrl.setSelectionRange)
	        {
		        ctrl.focus();
		        ctrl.setSelectionRange(posStart, posEnd);
	        }
	        else if (ctrl.createTextRange) {
		        var range = ctrl.createTextRange();
		        range.collapse(true);
		        range.moveEnd('character', posEnd);
		        range.moveStart('character', posStart);
		        range.select();
	        }
	    }
	    catch(e)
	    {
	      //
	    }
    }    
    
    
    //отображение календаря для выбора даты
    function ShowCalendar(ident)
    {   
        name = 'cldrDate';
        if(ident==2)
        {   
            document.getElementById(prefix + 'cldrEndDate').style.display='none';
            name = 'cldrBeginDate';            
        }
        if(ident==3)
        {
            document.getElementById(prefix + 'cldrBeginDate').style.display='none';
            name = 'cldrEndDate';
        }
        
        cld=document.getElementById(prefix + name);
     
        if(cld.style.display=='inline')
            cld.style.display='none';
        else cld.style.display='inline';
        return false;
    }
    
    
     //вставка элементов управления в строку таблицы
     function InsertActEditControls(RowIndex)
     {      
       //получение элементов управления
       txbCalibrDate=document.getElementById(prefix+"txbCalibrDate");
       ddlNtdName = document.getElementById(prefix + "ddlNtdName");
       pnlSop=document.getElementById(prefix+"pnlSop");
       ddlTechChart = document.getElementById(prefix + "ddlTechChart");
       txbCh1=document.getElementById(prefix+"txbCh1");
       txbCh2=document.getElementById(prefix+"txbCh2");
       txbCh3=document.getElementById(prefix+"txbCh3");
       txbCh4=document.getElementById(prefix+"txbCh4");
       txbCh5=document.getElementById(prefix+"txbCh5");
       txbCh6=document.getElementById(prefix+"txbCh6");
       txbCh7=document.getElementById(prefix+"txbCh7");
       txbCh8=document.getElementById(prefix+"txbCh8");
       pnlSort=document.getElementById(prefix+"pnlSort");       
       
       //вставка элементов управления в ячейки
       table=document.getElementById(prefix+"tblSettingsList");
       table.rows[RowIndex].cells[0].insertBefore(txbCalibrDate);
       table.rows[RowIndex].cells[1].insertBefore(ddlNtdName);
       table.rows[RowIndex].cells[2].insertBefore(pnlSop);   
       table.rows[RowIndex].cells[3].insertBefore(ddlTechChart);   
       table.rows[RowIndex].cells[4].insertBefore(txbCh1);   
       table.rows[RowIndex].cells[5].insertBefore(txbCh2);
       table.rows[RowIndex].cells[6].insertBefore(txbCh3);
       table.rows[RowIndex].cells[7].insertBefore(txbCh4);
       table.rows[RowIndex].cells[8].insertBefore(txbCh5);
       table.rows[RowIndex].cells[9].insertBefore(txbCh6);
       table.rows[RowIndex].cells[10].insertBefore(txbCh7);
       table.rows[RowIndex].cells[11].insertBefore(txbCh8);
       table.rows[RowIndex].cells[12].insertBefore(pnlSort);       
     }
     
    
    //проверка корректности значений при редактировании строки
     function CheckLineInputs()
     {
       //получение текущих значений из полей ввода
       Ch1=GetEditValueFloat(document.getElementById(prefix+"txbCh1"));
       Ch2=GetEditValueFloat(document.getElementById(prefix+"txbCh2"));
       Ch3=GetEditValueFloat(document.getElementById(prefix+"txbCh3"));
       Ch4=GetEditValueFloat(document.getElementById(prefix+"txbCh4"));
       Ch5=GetEditValueFloat(document.getElementById(prefix+"txbCh5"));
       Ch6=GetEditValueFloat(document.getElementById(prefix+"txbCh6"));
       Ch7=GetEditValueFloat(document.getElementById(prefix+"txbCh7"));
       Ch8=GetEditValueFloat(document.getElementById(prefix+"txbCh8"));             
       CalibrDate=document.getElementById(prefix+'txbCalibrDate').value;
       Diameter=document.getElementById(prefix+'ddlDiam').value;
       Thickness = document.getElementById(prefix + 'ddlThickness').value;
       NtdName = document.getElementById(prefix + 'ddlNtdName').value;
       TechChart = document.getElementById(prefix + 'ddlTechChart').value;
       SopN = document.getElementById(prefix + 'ddlSop').value;

       //проверка обязательных значений

       msg="";
       if(CalibrDate=="")
          msg=msg+"Не указано значение в поле \"Дата калибровки\"\n";
       if(Diameter=="")
           msg=msg+"Не указан диаметр труб\n";
       if(Thickness=="")
           msg = msg + "Не указана толщина стенки труб\n";
       if(NtdName=="")
           msg = msg + "Не указано значение в поле \"Номер НТД\"\n";
       if(SopN=="")
           msg = msg + "Не указано значение в поле \"Номер эталонного образца\"\n";
       if(TechChart=="")
           msg=msg+"Не указано значение в поле \"Номер технологической карты\"\n";
       if(msg!="")
       {
         alert(msg+"\nДля сохранения записи необходимо правильно указать перечисленные значения.");
         return false;
       }
       
       //проверка соответствия формата
       msg="";
       if(!IsNumber(Ch1, false))   
          msg=msg+"Неверно указано значение в поле \"Параметры калибровки (канал 1)\"\n";
       if(!IsNumber(Ch2, false)) 
          msg=msg+"Неверно указано значение в поле \"Параметры калибровки (канал 2)\"\n";
       if(!IsNumber(Ch3, false))   
          msg=msg+"Неверно указано значение в поле \"Параметры калибровки (канал 3)\"\n";
       if(!IsNumber(Ch4, false))   
          msg=msg+"Неверно указано значение в поле \"Параметры калибровки (канал 4)\"\n";
       if(!IsNumber(Ch5, false))   
          msg=msg+"Неверно указано значение в поле \"Параметры калибровки (канал 5)\"\n";
       if(!IsNumber(Ch6, false))   
          msg=msg+"Неверно указано значение в поле \"Параметры калибровки (канал 6)\"\n";
       if(!IsNumber(Ch7, false))   
          msg=msg+"Неверно указано значение в поле \"Параметры калибровки (канал 7)\"\n";
       if(!IsNumber(Ch8, false))
          msg=msg+"Неверно указано значение в поле \"Параметры калибровки (канал 8)\"\n";          

       var regexp=/^(\d{2}.\d{2}.\d{4} \d{2}:\d{2}:\d{2})$/;
       if(!regexp.test(CalibrDate)&(CalibrDate!=''))
          msg=msg+"Неверно указано значение в поле \"Дата калибровки\". Дату следует указывать с формате ДД.ММ.ГГГГ ЧЧ:ММ:СС, например: 08.10.2008 12:05:00\n";
       if(msg!="")
       {
         alert(msg+"\nДля сохранения записи необходимо правильно указать перечисленные значения.");
         return false;
       }
       
       return true;
     }
     
     //переход к полю "контрольная цифра" по завершению ввода номера
     function OnPipeNumberChange() {
         document.getElementById(prefix + 'txbCheck').value = "";
         setTimeout("document.getElementById(prefix+'txbCheck').focus();", 100);
         __doPostBack('txbPipeNumber', document.getElementById(prefix + 'txbYear').value + document.getElementById(prefix + 'txbPipeNumber').value);
         return true;
     }

     //нажатие кнопки "Ок" по вводу контрольной цифры
     function OnControlNumberChange() {
         //setTimeout("document.getElementById(prefix+'btnOk').click();", 100);
         __doPostBack('txbPipeNumber', document.getElementById(prefix + 'txbYear').value + document.getElementById(prefix + 'txbPipeNumber').value);
         return true;
     }

    //проверка ввода данных, необходимых для испытания
    function OnOkBtnClick(btn)
    {
     //получение данных из полей ввода
     Year=Trim(document.getElementById(prefix+"txbYear").value); 
     PipeNumber=Trim(document.getElementById(prefix+"txbPipeNumber").value);
     Check=Trim(document.getElementById(prefix+"txbCheck").value);
      
     //проверка корректности ввода данных
     msg="";
     if(!IsNumber(Year, false))
        msg=msg+"Неверно указано значение в поле \"Год\"\n";   
     if(!IsNumber(PipeNumber, false))
        msg=msg+"Неверно указано значение в поле \"№ Трубы\"\n"; 
     if(!IsNumber(Check, false))
            msg = msg + "Неверно указано значение в поле \"Контрольная цифра\"\n";

     if (msg != "")
        {
        document.getElementById(prefix + "txbPipeNumber").focus();
        alert(msg+"\nДля продолжения необходимо правильно указать перечисленные значения.");
        return false;
        }
     
     //проверка наличия ввода всех данных
     msg="";
      if(Year=="")
         msg=msg+"Не указано значение в поле \"Год\"\n";
      if(PipeNumber=="")   
         msg=msg+"Не указано значение в поле \"№ Трубы\"\n";     
      if(Check=="")    
         msg=msg+"Не указано значение в поле \"Контрольная цифра\"\n";
      if(msg!="")
        {
         document.getElementById(prefix+"txbPipeNumber").focus();
         alert(msg+"\nДля продолжения необходимо правильно указать перечисленные значения.");
         return false;
        }
        
      PostBackByButton(document.getElementById(prefix+'btnOk'));
      return false;
    }
      
    </script>

</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainPlaceHolder" runat="Server">

    <asp:Panel style="PADDING-RIGHT: 8px; PADDING-LEFT: 8px; PADDING-BOTTOM: 8px; PADDING-TOP: 12px; BORDER-BOTTOM: gray 1px solid" 
        id="Panel2" runat="server" Width="959px" BackColor="#E0E0E0" Height="25px">
        <table style="width: 621px">
             
        <tr><td style="width: 615px">&#160;Рабочее место
            <asp:DropDownList ID="ddlWorkPlace" runat="server" AutoPostBack="True" 
        Font-Bold="True" OnSelectedIndexChanged="ddlWorkPlace_SelectedIndexChanged" 
        tabIndex="1" Width="473px">
                    </asp:DropDownList>
            <asp:CheckBox ID="cblCheck" runat="server" Font-Size="10pt" 
                          style="width: 336px; position: fixed;
    margin-left: 600px; margin-top: -25px; top: 194px; left: -5px;" Text="Дополнительная проверка параметров калибровки" Checked="True"></asp:CheckBox>
            </td></tr>
             
        </table>
    </asp:Panel>
    
    <asp:MultiView ID="mvMain" runat="server" ActiveViewIndex="0">
    
    <asp:View ID="vSelectWorkplace" runat="server">
    <div style="margin: 16px">Для начала работы необходимо указать рабочее место</div>
    </asp:View>
    
        <asp:View ID="vParts" runat="server">
            <asp:Panel ID="pnlTabs" runat="server" BackColor="#E0E0E0" Style="border-top-width: 3px;
                margin-top: 16px; border-left-width: 3px; border-left-color: aqua;  border-top-color: aqua; padding-top: 1px; border-right-width: 3px;
                border-right-color: aqua" Width="960px">
                <table cellspacing="0" height="24" style="display: inline; width: 100%">
                     
                        <tr>
                            <td id="Td4" runat="server" style="padding-bottom: 4px; width: 16px; padding-top: 4px;
                                border-bottom: gray 1px solid; height: 28px; text-align: center">
                                &nbsp;
                            </td>
                            <td id="tdZaSmeny" runat="server" bgcolor="#ffffff" 
                                
                                style="padding: 4px 8px; border-right: 1px solid gray;
                                border-top: 1px solid gray; border-bottom-width: 1px; border-bottom-color: gray;
                                border-left: gray 1px solid; width: 128px; height: 28px; text-align: center">
                                <asp:LinkButton ID="btnZaSmeny" runat="server" Font-Size="11pt" ForeColor="Black"
                                    OnClick="btnZaSmeny_Click" Style="text-decoration: none">Журнал за смену</asp:LinkButton>
                            </td>
                            <td id="tdZaPeriod" runat="server" bgcolor="#e0e0e0" style="padding: 4px 8px; width: 140px;
                                border-bottom: gray 1px solid; height: 28px; text-align: center">
                                <asp:LinkButton ID="btnZaPeriod" runat="server" Font-Size="11pt" Font-Underline="False"
                                    ForeColor="Gray" OnClick="btnZaPeriod_Click" Style="text-decoration: none">Журнал 
                                за период</asp:LinkButton>
                            </td>
                            <td id="TdLast" runat="server" style="padding-right: 8px; padding-left: 8px; padding-bottom: 4px;
                                padding-top: 4px; border-bottom: gray 1px solid; height: 28px">
                                &nbsp;
                            </td>
                        </tr>
                     
                </table>
            </asp:Panel>
            <asp:Panel ID="pnlConditions" runat="server" Width="960px" style="padding: 10px">
                <asp:MultiView ID="mvConditions" runat="server" ActiveViewIndex="0">
                    <asp:View ID="vZaSmeny" runat="server">
                        <table style="font-size: 10pt; width: auto; text-align: center;">
                            <tr>
                                <td>
                                    &nbsp;</td>
                                <td>
                                    &nbsp;</td>
                            </tr>
                            <tr>
                                <td>
                                    <uc2:DateNavigation ID="navByShift" runat="server" DateIntervalType="Shift" />
                                </td>
                                <td>
                                    <asp:Button ID="ApplyCondition" runat="server" Height="26px" OnClick="ApplyCondition_Click" onclientclick="return PostBackByButton(this);" Text="Отобразить данные" UseSubmitBehavior="False" Width="160px" />
                                </td>
                            </tr>
                        </table>
                    </asp:View>
                    <asp:View ID="vZaPeriod" runat="server">
                        <table style="font-size: 10pt; width: auto; text-align: center;">
                            <tr>
                                <td>                                    
                                    &nbsp;</td>
                                <td>
                                    &nbsp;</td>
                            </tr>
                            <tr>
                                <td>
                                    <uc2:DateNavigation ID="navByPeriod" runat="server" DateIntervalType="Period" />
                                </td>
                                <td>
                                    <asp:Button ID="ApplyCondition0" runat="server" Height="26px" OnClick="ApplyCondition_Click" onclientclick="return PostBackByButton(this);" Text="Отобразить данные" UseSubmitBehavior="False" Width="160px" />
                                </td>
                            </tr>
                        </table>
                    </asp:View>
                </asp:MultiView></asp:Panel>
            
            <asp:Panel ID="pnlReport" runat="server" Visible="true" Width="960px" style="padding: 8px">
                
                <asp:Table ID="tblSettingsList" runat="server" BorderColor="Black" 
                    BorderStyle="Solid" BorderWidth="1px" CellPadding="1" CellSpacing="0" 
                    Font-Size="10pt" GridLines="Both" onprerender="tblSettingsList_PreRender">
                    <asp:TableRow runat="server" BackColor="#E0E0E0" Font-Bold="True" Height="24px" 
                        HorizontalAlign="Center">
                        <asp:TableCell runat="server" RowSpan="2" Width="140px">Дата<br />калибровки</asp:TableCell>
                        <asp:TableCell runat="server" RowSpan="2" Width="120px">Номер НТД</asp:TableCell>
                        <asp:TableCell runat="server" RowSpan="2" Width="155px">Номер<br />эталонного образца</asp:TableCell>
                        <asp:TableCell runat="server" RowSpan="2" Width="120px">Номер технологической карты</asp:TableCell>
                        <asp:TableCell runat="server" ColumnSpan="8">Параметры калибровки (по каналам)</asp:TableCell>
                        <asp:TableCell runat="server" RowSpan="2" Width="130px">Диаметр,<br /> толщина стенки 
                            <img src="Images/CX.png" /></asp:TableCell>
                        <asp:TableCell runat="server" RowSpan="2">Кол-во<br />труб</asp:TableCell>
                        <asp:TableCell runat="server" RowSpan="2"></asp:TableCell>
                    </asp:TableRow>
                    <asp:TableRow runat="server" BackColor="#E0E0E0" Font-Bold="True" 
                        HorizontalAlign="Center">
                        <asp:TableCell runat="server" Width="36px">1</asp:TableCell>
                        <asp:TableCell runat="server" Width="36px">2</asp:TableCell>
                        <asp:TableCell runat="server" Width="36px">3</asp:TableCell>
                        <asp:TableCell runat="server" Width="36px">4</asp:TableCell>
                        <asp:TableCell runat="server" Width="36px">5</asp:TableCell>
                        <asp:TableCell runat="server" Width="36px">6</asp:TableCell>
                        <asp:TableCell runat="server" Width="36px">7</asp:TableCell>
                        <asp:TableCell runat="server" Width="36px">8</asp:TableCell>
                    </asp:TableRow>
                </asp:Table>
                <br />
                <asp:Button ID="btnNewCalibrParams" runat="server" Height="26px" onclick="btnNewCalibrParams_Click" onclientclick="return PostBackByButton(this);" onprerender="btnNewCalibrParams_PreRender" Text="Новые параметры калибровки" Width="205px" />
                <br />
                <br />
            </asp:Panel>            
            <br />
            <br />
        </asp:View>
        <asp:View ID="vPipes" runat="server">
            
            <asp:Panel ID="pnlConditions0" runat="server" Height="50px" style="padding: 8px" >
                <table style="width: 880px;">
                    <tr>
                        <td>
                            <b>Параметры калибровки</b></td>
                        <td style="text-align: right">
                            <asp:Button ID="btnBackToPartsList" runat="server" Height="26px" 
                                OnClick="btnBackToPartsList_Click" Text="Вернуться к журналу калибровки" 
                                Width="240px" UseSubmitBehavior="False" 
                                onclientclick="return PostBackByButton(this);" />
                        </td>
                    </tr>
                </table>
                <br />
                <asp:Table ID="tblCurrentSettings" runat="server" BorderColor="Silver" 
                    BorderStyle="Solid" BorderWidth="1px" CellPadding="2" CellSpacing="0" 
                    Font-Size="10pt" GridLines="Both" Width="887px">
                    <asp:TableRow runat="server" BackColor="#E0E0E0" Font-Bold="True" Height="24px" 
                        HorizontalAlign="Center">
                        <asp:TableCell runat="server" RowSpan="2" Width="140px">Дата<br />калибровки</asp:TableCell>
                        <asp:TableCell runat="server" RowSpan="2" Width="120px">Номер НТД</asp:TableCell>
                        <asp:TableCell runat="server" RowSpan="2" Width="155px">Номер<br />эталонного образца</asp:TableCell>
                        <asp:TableCell runat="server" RowSpan="2" Width="120px">Номер технологической карты</asp:TableCell>
                        <asp:TableCell runat="server" ColumnSpan="8">Параметры калибровки (по каналам)</asp:TableCell>
                        <asp:TableCell runat="server" RowSpan="2" Width="130px">Диаметр,<br />толщина стенки<br />
                            <img src="Images/CX.png" /></asp:TableCell>
                    </asp:TableRow>
                    <asp:TableRow runat="server" BackColor="#E0E0E0" Font-Bold="True" 
                        HorizontalAlign="Center">
                        <asp:TableCell runat="server" Width="36px">1</asp:TableCell>
                        <asp:TableCell runat="server" Width="36px">2</asp:TableCell>
                        <asp:TableCell runat="server" Width="36px">3</asp:TableCell>
                        <asp:TableCell runat="server" Width="36px">4</asp:TableCell>
                        <asp:TableCell runat="server" Width="36px">5</asp:TableCell>
                        <asp:TableCell runat="server" Width="36px">6</asp:TableCell>
                        <asp:TableCell runat="server" Width="36px">7</asp:TableCell>
                        <asp:TableCell runat="server" Width="36px">8</asp:TableCell>
                    </asp:TableRow>
                    <asp:TableRow runat="server" HorizontalAlign="Center">
                        <asp:TableCell runat="server"></asp:TableCell>
                        <asp:TableCell runat="server"></asp:TableCell>
                        <asp:TableCell runat="server"></asp:TableCell>
                        <asp:TableCell runat="server"></asp:TableCell>
                        <asp:TableCell runat="server"></asp:TableCell>
                        <asp:TableCell runat="server"></asp:TableCell>
                        <asp:TableCell runat="server"></asp:TableCell>
                        <asp:TableCell runat="server"></asp:TableCell>
                        <asp:TableCell runat="server"></asp:TableCell>
                        <asp:TableCell runat="server"></asp:TableCell>
                        <asp:TableCell runat="server"></asp:TableCell>
                        <asp:TableCell runat="server"></asp:TableCell>
                        <asp:TableCell runat="server"></asp:TableCell>
                    </asp:TableRow>
                </asp:Table>
                <br />
            </asp:Panel>
            <asp:Panel ID="pnlReport0" runat="server" Width="1400px">
                <asp:MultiView ID="mvPipes" runat="server" ActiveViewIndex="0">
                    <asp:View ID="vContExtTable" runat="server">
                        <table style="font-size: 11pt" visible="true">
                            <tr>
                                <td style="vertical-align: top; padding-left: 25px; width: 1357px;">
                                    
                                    <table>
          <tr>
            <td style="vertical-align: top; padding-right: 24px;">
            <b>&nbsp;Ввод данных по трубам</b>
                <br />
            <br/>
                
                    <table style="font-size: 10pt">
                         
                            <tr>
                                <td style="VERTICAL-ALIGN: middle; TEXT-ALIGN: left;">
                                    Год</td>
                                <td style="VERTICAL-ALIGN: middle; TEXT-ALIGN: left; padding-left: 4px;">
                                    №&nbsp;Трубы</td>
                                <td style="VERTICAL-ALIGN: middle; TEXT-ALIGN: left; width: 15px;">
                                </td>
                                <td style="text-align: center;">
                                    <asp:LinkButton ID="lblHelpCheck" runat="server" onclick="lblHelpCheck_Click">(?)</asp:LinkButton>
                                </td>
                                <td align="center">
                                </td>
                                <td align="center">
                                    &nbsp;</td>
                            </tr>
                            <tr>
                                <td style="TEXT-ALIGN: left">
                                    <asp:TextBox ID="txbYear" runat="server" Font-Bold="True" Font-Size="16pt" 
                                        MaxLength="2" tabIndex="10" Width="32px"></asp:TextBox>
                                </td>
                                <td style="padding-left: 4px">
                                    <asp:TextBox ID="txbPipeNumber" runat="server" Font-Bold="True" 
                                        Font-Size="16pt" MaxLength="6" onchange="OnPipeNumberChange();"
                                        tabIndex="11" Width="90px"></asp:TextBox>
                                </td>
                                <td style="TEXT-ALIGN: center; font-size: 24px; font-weight: bold; width: 15px;">
                                    -</td>
                                <td>
                                    <asp:TextBox ID="txbCheck" runat="server" Font-Bold="True" Font-Size="16pt" 
                                        MaxLength="1"  onchange="OnControlNumberChange();"
                                        tabIndex="12" Width="25px"></asp:TextBox>
                                </td>
                                <td style="padding-left: 18px">
                                    <asp:Button ID="btnOk" runat="server" Font-Bold="True" Height="30px" 
                                        onclick="btnOk_Click" OnClientClick="return OnOkBtnClick(this);" tabIndex="13" 
                                        Text="Ввести данные" Width="123px" />
                                </td>
                                <td style="padding-left: 8px">
                                    <img src="Images/OOP.png" style="width: 29px; height: 29px" /></td>
                            </tr>
                            <tr>
                                <td style="VERTICAL-ALIGN: middle; TEXT-ALIGN: center; height: 20px;">
                                    <asp:CustomValidator ID="cvdYear" runat="server" 
                                        ClientValidationFunction="ValidateNumberInt" ControlToValidate="txbYear" 
                                        Display="Dynamic" ErrorMessage="! ! !" Font-Size="Smaller"></asp:CustomValidator>
                                </td>
                                <td style="VERTICAL-ALIGN: middle; TEXT-ALIGN: center; padding-left: 8px; height: 20px;">
                                    <asp:CustomValidator ID="cvdPipeNumber" runat="server" 
                                        ClientValidationFunction="ValidateNumberInt" ControlToValidate="txbPipeNumber" 
                                        Display="Dynamic" ErrorMessage="Ошибка" Font-Size="Smaller"></asp:CustomValidator>
                                </td>
                                <td style="VERTICAL-ALIGN: middle; TEXT-ALIGN: center; width: 15px; height: 20px;">
                                </td>
                                <td style="VERTICAL-ALIGN: middle; TEXT-ALIGN: center; height: 20px;">
                                    <asp:CustomValidator ID="cvdCheck" runat="server" 
                                        ClientValidationFunction="ValidateNumberInt" ControlToValidate="txbCheck" 
                                        Display="Dynamic" ErrorMessage="! ! !" Font-Size="Smaller"></asp:CustomValidator>
                                </td>
                                <td style="height: 20px">
                                </td>
                                <td style="height: 20px">
                                    </td>
                            </tr>
                         
                    </table>  
                <asp:Table ID="tblInfo" runat="server" Font-Size="10pt" Width="690px"  Visible="False">
                            <asp:TableRow ID="TableRow2" runat="server" BackColor="#D0D0D0" Font-Bold="True" Height="24px">
                                <asp:TableCell ID="TableCell12" runat="server" HorizontalAlign="Center" Width="100px">Номер трубы</asp:TableCell>
                                 <asp:TableCell ID="TableCell13" runat="server" HorizontalAlign="Center" Width="100px">Сортамент</asp:TableCell>
                                 <asp:TableCell ID="TableCell14" runat="server" HorizontalAlign="Center" Width="100px">Марка стали</asp:TableCell>
                                 <asp:TableCell ID="TableCell15" runat="server" HorizontalAlign="Center" Width="220px">НТД</asp:TableCell>
                            </asp:TableRow>
                        </asp:Table>                              
                <br />
                <hr/>
                        <b>&nbsp;Журнал испытаний (последние записи):</b>
                        <br />
                        <br />
                        <asp:Table ID="tblHistory" runat="server" Font-Size="8pt" 
                            onprerender="tblHistory_PreRender" Border="1" BorderColor="Black" 
                    BorderStyle="Solid" BorderWidth="1px" CellPadding="2" CellSpacing="0" 
                    GridLines="Both">
                            <asp:TableRow ID="TableRow1" runat="server" BackColor="#D0D0D0" 
                                Font-Bold="True" Height="24px" HorizontalAlign="Center" VerticalAlign="Middle">
                                <asp:TableCell ID="TableCell1" runat="server" HorizontalAlign="Center" Width="100px">Дата 
                                испытания</asp:TableCell>
                                <asp:TableCell ID="TableCell2" runat="server" HorizontalAlign="Center" Width="100px">Номер трубы</asp:TableCell>
                                <asp:TableCell ID="TableCell9" runat="server" HorizontalAlign="Center" Width="100px">Сортамент</asp:TableCell>
                                 <asp:TableCell ID="TableCell10" runat="server" HorizontalAlign="Center" Width="100px">Марка стали</asp:TableCell>
                                 <asp:TableCell ID="TableCell11" runat="server" HorizontalAlign="Center" Width="220px">НТД</asp:TableCell>
                                <asp:TableCell runat="server" Width="100px">Метка<br/>АУЗК сварки</asp:TableCell>
                                <asp:TableCell runat="server" Width="100px">Результат<br/>перепроверки</asp:TableCell>          
                                
                                <asp:TableCell runat="server" Width="100px">Метка<br/>АУЗК кромок</asp:TableCell>
                                <asp:TableCell runat="server" Width="100px">Результат<br/>перепроверки</asp:TableCell>    
                                
                                <asp:TableCell runat="server" Width="100px">Метка<br/>АУЗК шва</asp:TableCell>
                                <asp:TableCell runat="server" Width="100px">Результат<br/>перепроверки</asp:TableCell>

                                <asp:TableCell ID="TableCell3" runat="server" Width="100px">Метка<br/>АУЗК тела</asp:TableCell>
                                <asp:TableCell ID="TableCell4" runat="server" Width="100px">Результат<br/>перепроверки</asp:TableCell>

                                <asp:TableCell ID="TableCell5" runat="server" Width="100px">Метка АУЗК концов левая</asp:TableCell>
                                <asp:TableCell ID="TableCell6" runat="server" Width="100px">Результат<br/>перепроверки</asp:TableCell>

                                <asp:TableCell ID="TableCell7" runat="server" Width="100px">Метка АУЗК концов правая</asp:TableCell>
                                <asp:TableCell ID="TableCell8" runat="server" Width="100px">Результат<br/>перепроверки</asp:TableCell>

                                <asp:TableCell runat="server" Width="100px">Направление</asp:TableCell>
                                <asp:TableCell runat="server"></asp:TableCell>
                            </asp:TableRow>
                        </asp:Table>
                    
                <br />
              </td>
            <td style="vertical-align: top">
                &nbsp;</td>
          </tr>
        </table>
            <br/>
                                </td>
                            </tr>
                        </table>
                    </asp:View>
                    <asp:View ID="vGetChekingNum" runat="server">
                        <br />
                        <table>
                            <tr>
                                <td style="width: 667px; padding-left: 20px; font-size: 11pt;">
                                    <b>Чтобы раccчитать контрольную цифру для №<asp:Label ID="lblNumtrub"
                                        runat="server"></asp:Label>
                                        <br />
                                    введите учетную запись и пароль:</b><br/><br/>
                                    <table style="font-size: 11pt">
                                        <tr>
                                            <td style="width: 121px">
                                                Учетная запись
                                            </td>
                                            <td style="width: 158px">
                                                <asp:TextBox ID="txbLogin" runat="server" TabIndex="1" Width="184px"></asp:TextBox>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td style="width: 121px">
                                                <font size="2">Пароль</font>
                                            </td>
                                            <td style="width: 158px">
                                                <asp:TextBox ID="txbPassword" runat="server" TabIndex="2" TextMode="Password" Width="184px"></asp:TextBox>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td colspan="2" style="font-size: 4px">
                                            </td>
                                        </tr>
                                        <tr>
                                            <td colspan="2" style="text-align: right">
                                                <asp:Button ID="btnLogin" runat="server" Font-Size="10pt" Height="26px" OnClick="btnLogin_Click"
                                                    TabIndex="3" Text="ОК" Width="69px" />
                                                &nbsp;
                                                <asp:Button ID="btnCansel" runat="server" Font-Size="10pt" Height="26px" OnClick="btnCansel_Click"
                                                    TabIndex="4" Text="Отмена" UseSubmitBehavior="False" Width="69px" />
                                            </td>
                                        </tr>
                                    </table>
                                </td>
                            </tr>
                        </table>
                        <br/>
                    </asp:View>
                </asp:MultiView>
            </asp:Panel>
        </asp:View>
    </asp:MultiView>
 
    <br/>
    <asp:Panel ID="pnlEditControls" runat=server BorderColor="#0033CC" 
        BorderStyle="Solid" BorderWidth="1px">
        &nbsp;<br />&nbsp;<asp:TextBox ID="txbCalibrDate" runat="server" Font-Size="9pt" TabIndex="30" Width="134px"></asp:TextBox>
        &nbsp;&nbsp;<asp:DropDownList ID="ddlNtdName" runat="server" Font-Size="9pt" TabIndex="31" Width="144px">
        </asp:DropDownList>
        &nbsp;<asp:Panel ID="pnlSop" runat="server" Width="150px">
            <asp:DropDownList ID="ddlSop" runat="server" AutoPostBack="True" Font-Size="9pt" onselectedindexchanged="ddlSop_SelectedIndexChanged" TabIndex="34" Width="144px">
                <asp:ListItem></asp:ListItem>
            </asp:DropDownList>
            <asp:TextBox ID="txbSop" runat="server" TabIndex="34" Width="144px"></asp:TextBox>
        </asp:Panel>
        &nbsp;<asp:DropDownList ID="ddlTechChart" runat="server" Font-Size="9pt" TabIndex="32" Width="144px"></asp:DropDownList>
        &nbsp;&nbsp; <asp:TextBox ID="txbCh1" runat="server" Font-Size="9pt" TabIndex="36" 
            Width="36px"></asp:TextBox>
        &nbsp;<asp:TextBox ID="txbCh2" runat="server" Font-Size="9pt" Width="36px" 
            TabIndex="38"></asp:TextBox>
        &nbsp;<asp:TextBox ID="txbCh3" runat="server" Font-Size="9pt" Width="36px" 
            TabIndex="40"></asp:TextBox>
        &nbsp;<asp:TextBox ID="txbCh4" runat="server" Font-Size="9pt" Width="36px" 
            TabIndex="42"></asp:TextBox>
        &nbsp;<asp:TextBox ID="txbCh5" runat="server" Font-Size="9pt" Width="36px" 
            TabIndex="44"></asp:TextBox>
        &nbsp;<asp:TextBox ID="txbCh6" runat="server" Font-Size="9pt" Width="36px" 
            TabIndex="46"></asp:TextBox>
        &nbsp;<asp:TextBox ID="txbCh7" runat="server" Font-Size="9pt" Width="36px" 
            TabIndex="48"></asp:TextBox>
        &nbsp;<asp:TextBox ID="txbCh8" runat="server" Font-Size="9pt" Width="36px" 
            TabIndex="50"></asp:TextBox>
        &nbsp;<asp:Panel ID="pnlSort" runat="server" 
            style="display: inline; vertical-align: middle" Width="114px" Wrap="False">
            <asp:DropDownList ID="ddlDiam" runat="server" AutoPostBack="True" 
                Font-Size="9pt" TabIndex="52" Width="50px" 
                onselectedindexchanged="ddlDiam_SelectedIndexChanged">
            </asp:DropDownList>
            x
            <asp:DropDownList ID="ddlThickness" runat="server" AutoPostBack="True" 
                Font-Size="9pt" TabIndex="54" Width="50px" 
                onselectedindexchanged="ddlThickness_SelectedIndexChanged">
            </asp:DropDownList>
        </asp:Panel>
        <br />
        <br />

        </asp:Panel>

        <asp:Panel ID ="pnlInputPipe" runat="server" Height="650px" Width="450px" Visible="False">
            <iframe id="ifrInputPipe" runat="server" src="modal_InputPipeRUsc.aspx" Style="width: 450px; height: 650px"></iframe>
            <asp:Button ID="btnCloseInputPipe" Text="Закрыть" OnClick="btnCloseInputPipe_Click" Style="width: 450px" runat="server"></asp:Button>
        </asp:Panel>


<uc1:PopupWindow ID="PopupWindow1" runat="server" ContentPanelId="pnlInputPipe" 
            Title="Ввод данных по испытанию трубы" />
<asp:HiddenField ID="fldPrevAuscRowId" runat="server" />
</asp:Content>