

        //ôóíêöèÿ ïðîâåðêè ïðàâèëüíîñòè ââîäà âñåõ äàííûõ ôîðìû ïîèñêà
        //ïàðàìåòð allowEmpty=true - ðàçðåøàòü ïóñòûå ïîëÿ
        function CheckFindInputs(allowEmpty) {
            //óäàëåíèå ëèøíèõ ïðîáåëîâ â çíà÷åíèÿõ 
            Year = Trim(document.getElementById(prefix + "txbYear").value);
            PipeNumber = Trim(document.getElementById(prefix + "txbPipeNumber").value);
            Check = Trim(document.getElementById(prefix + "txbCheck").value);

            //ïðîâåðêà êðèòåðèåâ ïîèñêà - äîëæíû áûòü âñå ïàðàìåòðû
            if ((Year == "") | (PipeNumber == "") | (Check == "")) {
                alert("Äëÿ ïîèñêà òðóáû íåîáõîäèìî óêàçàòü å¸ íîìåð è êîíòðîëüíóþ öèôðó");
                return false;
            }

            //ïðîâåðêà êîððåêòíîñòè ââîäà äàííûõ
            msg = "";
            if (!IsNumber(Year, false))
                msg = msg + "Íåâåðíî óêàçàíî çíà÷åíèå â ïîëå \"Ãîä\"\n";
            if (!IsNumber(PipeNumber, false))
                msg = msg + "Íåâåðíî óêàçàíî çíà÷åíèå â ïîëå \"¹ Òðóáû\"\n";
            if (!IsNumber(Check, false))
                msg = msg + "Íåâåðíî óêàçàíî çíà÷åíèå â ïîëå \"Êîíòðîëüíàÿ öèôðà\"\n";
            if (msg != "") {
                alert(msg + "\nÄëÿ ïðîäîëæåíèÿ íåîáõîäèìî ïðàâèëüíî óêàçàòü ïåðå÷èñëåííûå çíà÷åíèÿ.");
                return false;
            }

            //ïðîâåðêà íàëè÷èÿ ââîäà âñåõ äàííûõ
            msg = "";
            if (!allowEmpty) {
                if (Year == "")
                    msg = msg + "Íå óêàçàíî çíà÷åíèå â ïîëå \"Ãîäe\"\n";
                if (PipeNumber == "")
                    msg = msg + "Íå óêàçàíî çíà÷åíèå â ïîëå \"¹ Òðóáû\"\n";
                if (Check == "")
                    msg = msg + "Íå óêàçàíî çíà÷åíèå â ïîëå \"Êîíòðîëüíàÿ öèôðà\"\n";
                if (msg != "") {
                    alert(msg + "\nÄëÿ ïðîäîëæåíèÿ íåîáõîäèìî ïðàâèëüíî óêàçàòü ïåðå÷èñëåííûå çíà÷åíèÿ.");
                    return false;
                }
            }
            return true;
        }

            OuterDefect = Trim(document.getElementById(prefix + "ddlOuterDefect").value);
            InnerDefect = Trim(document.getElementById(prefix + "ddlInnerDefect").value);
            WorkPlace = Trim(document.getElementById(prefix + "ddlWorkPlace").value);
            EndDefect = Trim(document.getElementById(prefix + "ddlEndDefect").value);
            //Ïðîâåðêà íàëè÷èÿ äàííûõ   
            if ((PartNum == "") & (DiamPKpSH == "") & (DiamPKpT == "") & (DiamTpSH == "") & (DiamTpT == "") & (DiamZKpSH == "") & (DiamZKpT == "")
                & (Dlina == "") & (KosinaPTor == "") & (KosinaZTor == "") & (Krivizna1mT == "") & (KriviznaVciaT == "")
                & (OstatokVnutGrata == "") & (OstatokNarujGrata == "") & (SmeschKrom == "") & (TolSten == "") & (TolSten2 == "") & (ShirinaTorKolPTorMin == "")
                & (ShirinaTorKolPTorMax == "") & (ShirinaTorKolZTorMin == "") & (ShirinaTorKolZTorMax == "")) {
                alert("Íå óêàçàíî íè îäíîãî çíà÷åíèÿ.\nÄëÿ ïðîäîëæåíèÿ íåîáõîäèìî èõ óêàçàòü.");
                return false;
            }

            //ïðîâåðêà çàïîëíåíèÿ âñåõ äàííûõ
            msg = "";

            if (PartNum == "")
                msg = msg + "Íåîáõîäèìî óêàçàòü çíà÷åíèå â ïîëå \"Íîìåð ïàðòèè\"\n";
            if (Dlina == "")
                msg = msg + "Íåîáõîäèìî óêàçàòü çíà÷åíèå â ïîëå \"Äëèíà òðóáû\"\n";
            if (KosinaPTor == "")
                msg = msg + "Íåîáõîäèìî óêàçàòü çíà÷åíèå â ïîëå \"Êîñèíà ðåçà (ïåðåäíèé òîðåö)\"\n";
            if (KosinaZTor == "")
                msg = msg + "Íåîáõîäèìî óêàçàòü çíà÷åíèå â ïîëå \"Êîñèíà ðåçà (çàäíèé òîðåö)\"\n";
            if (Krivizna1mT == "")
                msg = msg + "Íåîáõîäèìî óêàçàòü çíà÷åíèå â ïîëå \"Êðèâèçíà íà 1 ì òðóáû\"\n";
            if (KriviznaVciaT == "")
                msg = msg + "Íåîáõîäèìî óêàçàòü çíà÷åíèå â ïîëå \"Êðèâèçíà òðóáû ïî âñåé äëèíå\"\n";
            if (OstatokVnutGrata == "")
                msg = msg + "Íåîáõîäèìî óêàçàòü çíà÷åíèå â ïîëå \"Îñòàòîê âíóòðåííåãî ãðàòà\"\n";
            if (OstatokNarujGrata == "")
                msg = msg + "Íåîáõîäèìî óêàçàòü çíà÷åíèå â ïîëå \"Îñòàòîê íàðóæíîãî ãðàòà\"\n";
            if (SmeschKrom == "")
                msg = msg + "Íåîáõîäèìî óêàçàòü çíà÷åíèå â ïîëå \"Ñìåùåíèå êðîìîê òðóáû\"\n";
            if (TolSten == "")
                msg = msg + "Íåîáõîäèìî óêàçàòü çíà÷åíèå â ïîëå \"Òîëùèíà ñòåíêè òðóáû (çàìåð 1)\"\n";
            if (TolSten2 == "")
                msg = msg + "Íåîáõîäèìî óêàçàòü çíà÷åíèå â ïîëå \"Òîëùèíà ñòåíêè òðóáû (çàìåð 2)\"\n";

            if (DiametrOnFrontEnd == "")
                msg = msg + "Íåîáõîäèìî óêàçàòü çíà÷åíèå â ïîëå \"Äèàìåòð ïî ïåðåäíåìó êîíöó\"\n";
            if (DiametrOnBackEnd == "")
                msg = msg + "Íåîáõîäèìî óêàçàòü çíà÷åíèå â ïîëå \"Äèàìåòð ïî çàäíåìó êîíöó\"\n";
            if (DiametrOnBody == "")
                msg = msg + "Íåîáõîäèìî óêàçàòü çíà÷åíèå â ïîëå \"Äèàìåòð ïî òåëó\"\n";

            if (ShirinaTorKolPTorMin == "")
                msg = msg + "Íåîáõîäèìî óêàçàòü çíà÷åíèå â ïîëå \"Øèðèíà òîðöåâîãî êîëüöà ïåðåäíèé òîðåö - ìèíèìàëüíûé\"\n";

            if (ShirinaTorKolPTorMax == "")
                msg = msg + "Íåîáõîäèìî óêàçàòü çíà÷åíèå â ïîëå \"Øèðèíà òîðöåâîãî êîëüöà ïåðåäíèé òîðåö - ìàêñèìàëüíûé\"\n";

            if (ShirinaTorKolZTorMin == "")
                msg = msg + "Íåîáõîäèìî óêàçàòü çíà÷åíèå â ïîëå \"Øèðèíà òîðöåâîãî êîëüöà çàäíèé òîðåö - ìèíèìàëüíûé\"\n";

            if (ShirinaTorKolZTorMax == "")
                msg = msg + "Íåîáõîäèìî óêàçàòü çíà÷åíèå â ïîëå \"Øèðèíà òîðöåâîãî êîëüöà çàäíèé òîðåö - ìàêñèìàëüíûé\"\n";

            if (OuterDefect == "")
                msg = msg + "Íåîáõîäèìî çàïîëíèòü ïîëå \"Ñîñòîÿíèå íàðóæíîé ïîâåðõíîñòè\". Åñëè äåôåêò îòñóòñòâóåò, òî íåîáõîäèìî âûáðàòü \"Óä\"\n";

            if (InnerDefect == "")
                msg = msg + "Íåîáõîäèìî çàïîëíèòü ïîëå \"Ñîñòîÿíèå âíóòðåííåé ïîâåðõíîñòè\". Åñëè äåôåêò îòñóòñòâóåò, òî íåîáõîäèìî âûáðàòü \"Óä\"\n";

            if (WorkPlace == "-1")
                msg = msg + "Íåîáõîäèìî çàïîëíèòü ïîëå \"Íîìåð èíñïåêöèîííîé ðåøåòêè\"\n";

            if (EndDefect == "")
                msg = msg + "Íåîáõîäèìî çàïîëíèòü ïîëå \"Çàóñåíöû íà òîðöàõ\". Åñëè äåôåêò îòñóòñòâóåò, òî íåîáõîäèìî âûáðàòü \"Óä\"\n";


            if (msg != "") {
                alert(msg + "\nÄëÿ ïðîäîëæåíèÿ íåîáõîäèìî óêàçàòü ïåðå÷èñëåííûå çíà÷åíèÿ.");
                return false;
            }

            //ïðîâåðêà ïðàâèëüíîñòè ââîäà âñåõ äàííûõ
            msg = "";

            if (!IsNumber(PartNum, false))
                msg = msg + "Íåâåðíî óêàçàíî çíà÷åíèå â ïîëå \"Íîìåð ïàðòèè\"\n";
            if (!IsNumber(DiamPKpSH, false))
                msg = msg + "Íåâåðíî óêàçàíî çíà÷åíèå â ïîëå \"Äèàìåòð ëîêàëüíûé ïî ïåðåäíåìó êîíöó òðóáû ìàêñèìàëüíûé, ìì\"\n";
            if (!IsNumber(DiamPKpT, false))
                msg = msg + "Íåâåðíî óêàçàíî çíà÷åíèå â ïîëå \"Äèàìåòð ëîêàëüíûé ïî ïåðåäíåìó êîíöó òðóáû ìèíèìàëüíûé\"\n";
            if (!IsNumber(DiamTpSH, false))
                msg = msg + "Íåâåðíî óêàçàíî çíà÷åíèå â ïîëå \"Äèàìåòð ëîêàëüíûé ïî òåëó òðóáû ìàêñèìàëüíûé\"\n";
            if (!IsNumber(DiamTpT, false))
                msg = msg + "Íåâåðíî óêàçàíî çíà÷åíèå â ïîëå \"Äèàìåòð ëîêàëüíûé ïî òåëó òðóáû ìèíèìàëüíûé\"\n";
            if (!IsNumber(DiamZKpSH, false))
                msg = msg + "Íåâåðíî óêàçàíî çíà÷åíèå â ïîëå \"Äèàìåòð ëîêàëüíûé ïî çàäíåìó êîíöó òðóáû ìàêñèìàëüíûé\"\n";
            if (!IsNumber(DiamZKpT, false))
                msg = msg + "Íåâåðíî óêàçàíî çíà÷åíèå â ïîëå \"Äèàìåòð ëîêàëüíûé ïî çàäíåìó êîíöó òðóáû ìèíèìàëüíûé\"\n";
            if (!IsNumber(Dlina, false))
                msg = msg + "Íåâåðíî óêàçàíî çíà÷åíèå â ïîëå \"Äëèíà òðóáû\"\n";
            if (!IsNumber(KosinaPTor, false))
                msg = msg + "Íåâåðíî óêàçàíî çíà÷åíèå â ïîëå \"Êîñèíà ðåçà (ïåðåäíèé òîðåö)\"\n";
            if (!IsNumber(KosinaZTor, false))
                msg = msg + "Íåâåðíî óêàçàíî çíà÷åíèå â ïîëå \"Êîñèíà ðåçà (çàäíèé òîðåö)\"\n";
            if (!IsNumber(Krivizna1mT, false))
                msg = msg + "Íåâåðíî óêàçàíî çíà÷åíèå â ïîëå \"Êðèâèçíà íà 1 ì òðóáû\"\n";
            if (!IsNumber(KriviznaVciaT, false))
                msg = msg + "Íåâåðíî óêàçàíî çíà÷åíèå â ïîëå \"Êðèâèçíà òðóáû ïî âñåé äëèíå\"\n";
            if (!IsNumber(OstatokVnutGrata, false))
                msg = msg + "Íåâåðíî óêàçàíî çíà÷åíèå â ïîëå \"Îñòàòîê âíóòðåííåãî ãðàòà\"\n";
            if (!IsNumber(OstatokNarujGrata, false))
                msg = msg + "Íåâåðíî óêàçàíî çíà÷åíèå â ïîëå \"Îñòàòîê íàðóæíîãî ãðàòà\"\n";
            if (!IsNumber(SmeschKrom, false))
                msg = msg + "Íåâåðíî óêàçàíî çíà÷åíèå â ïîëå \"Ñìåùåíèå êðîìîê òðóáû\"\n";
            if (!IsNumber(TolSten, false))
                msg = msg + "Íåâåðíî óêàçàíî çíà÷åíèå â ïîëå \"Òîëùèíà ñòåíêè òðóáû (çàìåð 1)\"\n";
            if (!IsNumber(TolSten2, false))
                msg = msg + "Íåâåðíî óêàçàíî çíà÷åíèå â ïîëå \"Òîëùèíà ñòåíêè òðóáû (çàìåð 2)\"\n";

            if (!IsNumber(DiametrOnFrontEnd, false))
                msg = msg + "Íåâåðíî óêàçàíî çíà÷åíèå â ïîëå \"Äèàìåòð ïî ïåðåäíåìó êîíöó\"\n";
            if (!IsNumber(DiametrOnBackEnd, false))
                msg = msg + "Íåâåðíî óêàçàíî çíà÷åíèå â ïîëå \"Äèàìåòð ïî çàäíåìó êîíöó\"\n";
            if (!IsNumber(DiametrOnBody, false))
                msg = msg + "Íåâåðíî óêàçàíî çíà÷åíèå â ïîëå \"Äèàìåòð ïî òåëó\"\n";
            if (msg != "") {
                alert(msg + "\nÄëÿ ïðîäîëæåíèÿ íåîáõîäèìî ïðàâèëüíî óêàçàòü ïåðå÷èñëåííûå çíà÷åíèÿ.");
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
                                <td style="vertical-align: middle; text-align: left; font-size: 10pt;">Ãîä</td>
                                <td style="vertical-align: middle; text-align: left;"></td>
                                <td style="vertical-align: middle; text-align: left; font-size: 10pt;">¹ Òðóáû</td>
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
                                        Style="display: inline" TabIndex="4" Text="Ââåñòè äàííûå" Width="107px" />
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
                                        ControlToValidate="txbPipeNumber" ErrorMessage="Îøèáêà" Font-Size="Smaller"></asp:CustomValidator>
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
                        <td style="width: 371px; height: 26px; font-size: 10pt;">Ñïèñîê ñóùåñòâóþùèõ çàïèñåé ïî ãåîìåòðè÷åñêèì çàìåðàì</td>
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
                                Text="Äîáàâèòü íîâóþ çàïèñü" Width="163px" Font-Size="10pt"
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
                        <font size="2">×òîáû ðàcc÷èòàòü êîíòðîëüíóþ öèôðó äëÿ ¹<asp:Label 
                            ID="lblNumtrub" runat="server"></asp:Label>
                        ââäåäèòå ñâîé ëîãèí è ïàðîëü</font>
                        <br style="font-size: 10pt" />
                        <table>
                            <tr>
                                <td>
                                    <font size="2">Ëîãèí</font>
                                </td>
                                <td style="width: 158px">
                                    <asp:TextBox ID="txbLogin" runat="server" TabIndex="1" Width="184px"></asp:TextBox>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <font size="2">Ïàðîëü</font>
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
                                        OnClick="btnCansel_Click" TabIndex="4" Text="Îòìåíà" UseSubmitBehavior="False"
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
                                    <b>Òðóáà ¹
                                    <asp:Label ID="txtPipeNumberDubl" runat="server" Width="24%"></asp:Label>
                                    </b>
                                </td>
                                <td style="text-align: left; width: 900px;">
                                    <strong>Íîìåð ïàðòèè</strong>&nbsp;
                                    <asp:TextBox ID="txtPartNumber" runat="server" MaxLength="5" TabIndex="1"
                                        Width="57px"></asp:TextBox>
                                    &nbsp; &nbsp;<asp:CustomValidator ID="CustomValidator4" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="txtPartNumber" ErrorMessage="Îøèáêà" Font-Size="Smaller"
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
                    <td bgcolor="#e0e0e0">Ñòðîêà çàäàíèÿ íà êàìïàíèþ</td>
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
                    <td>Íîìåð èíñïåêöèîííîé ðåøåòêè</td>
                    <td>
                        <asp:DropDownList
                            ID="ddlWorkPlace" runat="server" AutoPostBack="True" Font-Bold="True" TabIndex="3" OnSelectedIndexChanged="ddlWorkPlace_SelectedIndexChanged">
                            <asp:ListItem Value="-1">(âûáåðèòå)</asp:ListItem>
                            <asp:ListItem Value="0">ÏÄÎ</asp:ListItem>
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
                            <asp:ListItem Value="7">Ó÷àñòîê ðåìîíòà</asp:ListItem>
                        </asp:DropDownList>
                    </td>
                </tr>
                <tr>
                    <td>
                        <asp:Button ID="btnLoad" runat="server" Height="23px"
                            OnClientClick="" TabIndex="3" Text="Çàïîëíèòü çíà÷åíèÿ"
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
                                    Äèàìåòð ëîêàëüíûé ïî ïåðåäíåìó êîíöó òðóáû, ìì</td>
                                <td style="width: 900px; text-align: left" rowspan="2">Îâàëüíîñòü ïî ïåðåäíåìó
                                    <br />
                                    êîíöó òðóáû, ìì
                                </td>
                                <td style="width: 900px; text-align: left" rowspan="2"></td>
                            </tr>
                            <tr>
                                <td style="font-size: 10pt; text-align: left; width: 900px;">ìèíèìàëüíûé</td>
                                <td style="text-align: left; width: 900px;">ìàêñèìàëüíûé</td>
                            </tr>
                            <tr>
                                <td style="text-align: left; width: 900px; font-size: 12pt; color: #000000;">
                                    <asp:TextBox ID="txtDiamPKpT" runat="server" MaxLength="6" TabIndex="3"
                                        Width="180px" AutoPostBack="True" OnTextChanged="txtOvalPK_SelectedIndexChanged"></asp:TextBox>
                                    &nbsp;
                                    <asp:CustomValidator ID="CusVal_txtDiamPKpT" runat="server"
                                        ClientValidationFunction="ValidateNumberOvalPK" ControlToValidate="txtDiamPKpT"
                                        ErrorMessage="Îøèáêà" Font-Size="Smaller" ValidateEmptyText="True"></asp:CustomValidator>
                                </td>
                                <td style="text-align: left; width: 900px; font-size: 12pt;">
                                    <asp:TextBox ID="txtDiamPKpSH" runat="server" MaxLength="6" TabIndex="4"
                                        Width="180px" AutoPostBack="True" OnTextChanged="txtOvalPK_SelectedIndexChanged"></asp:TextBox>
                                    &nbsp;
                                    <asp:CustomValidator ID="CusVal_txtDiamPKpSH" runat="server"
                                        ClientValidationFunction="ValidateNumberOvalPK" ControlToValidate="txtDiamPKpSH"
                                        ErrorMessage="Îøèáêà" Font-Size="Smaller" ValidateEmptyText="True"></asp:CustomValidator>
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
                                    Äèàìåòð ëîêàëüíûé ïî çàäíåìó êîíöó òðóáû, ìì</td>
                                <td style="width: 900px; text-align: left" rowspan="2">Îâàëüíîñòü ïî çàäíåìó
                                    <br />
                                    êîíöó òðóáû, ìì
                                </td>
                                <td style="width: 900px; text-align: left" rowspan="2"></td>
                            </tr>
                            <tr style="font-size: 10pt; color: #000000">
                                <td style="text-align: left; width: 900px;">ìèíèìàëüíûé</td>
                                <td style="text-align: left; width: 900px;">ìàêñèìàëüíûé</td>
                            </tr>
                            <tr style="font-size: 12pt">
                                <td style="text-align: left; width: 900px; font-size: 12pt; color: #000000; height: 29px;">
                                    <asp:TextBox ID="txtDiamZKpT" runat="server" MaxLength="6" TabIndex="5"
                                        Width="180px" AutoPostBack="True" OnTextChanged="txtOvalZK_SelectedIndexChanged"></asp:TextBox>
                                    &nbsp;
                                    <asp:CustomValidator ID="CusVal_txtDiamZKpT" runat="server"
                                        ClientValidationFunction="ValidateNumberOvalZK" ControlToValidate="txtDiamZKpT"
                                        ErrorMessage="Îøèáêà" Font-Size="Smaller" ValidateEmptyText="True"></asp:CustomValidator>
                                </td>
                                <td style="text-align: left; width: 900px; height: 29px;">
                                    <asp:TextBox ID="txtDiamZKpSH" runat="server" MaxLength="6" TabIndex="6"
                                        Width="180px" AutoPostBack="True" OnTextChanged="txtOvalZK_SelectedIndexChanged"></asp:TextBox>
                                    &nbsp;
                                    <asp:CustomValidator ID="CusVal_txtDiamZKpSH" runat="server"
                                        ClientValidationFunction="ValidateNumberOvalZK" ControlToValidate="txtDiamZKpSH"
                                        ErrorMessage="Îøèáêà" Font-Size="Smaller" ValidateEmptyText="True"></asp:CustomValidator>
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
                                    Äèàìåòð ëîêàëüíûé ïî òåëó òðóáû, ìì</td>
                                <td style="width: 900px; text-align: left;" rowspan="2">Îâàëüíîñòü ïî òåëó
                                    <br />
                                    òðóáû, ìì
                                </td>
                                <td style="width: 900px; text-align: left;" rowspan="2"></td>
                            </tr>
                            <tr style="color: #000000">
                                <td style="font-size: 10pt; text-align: left; width: 900px;">ìèíèìàëüíûé</td>
                                <td style="text-align: left; width: 900px;">ìàêñèìàëüíûé</td>
                            </tr>
                            <tr>
                                <td style="text-align: left; font-size: 12pt; width: 900px; color: #000000;">
                                    <asp:TextBox ID="txtDiamTpT" runat="server" MaxLength="6" TabIndex="7"
                                        Width="180px" AutoPostBack="True" OnTextChanged="txtOvalT_SelectedIndexChanged"></asp:TextBox>
                                    &nbsp;
                                    <asp:CustomValidator ID="CusVal_txtDiamTpT" runat="server"
                                        ClientValidationFunction="ValidateNumberOvalT" ControlToValidate="txtDiamTpT"
                                        ErrorMessage="Îøèáêà" Font-Size="Smaller" ValidateEmptyText="True"></asp:CustomValidator>
                                </td>
                                <td style="text-align: left; width: 900px;">
                                    <asp:TextBox ID="txtDiamTpSH" runat="server" MaxLength="6" TabIndex="8"
                                        Width="180px" AutoPostBack="True" OnTextChanged="txtOvalT_SelectedIndexChanged"></asp:TextBox>
                                    &nbsp; &nbsp;<asp:CustomValidator ID="CusVal_txtDiamTpSH" runat="server"
                                        ClientValidationFunction="ValidateNumberOvalT" ControlToValidate="txtDiamTpSH"
                                        ErrorMessage="Îøèáêà" Font-Size="Smaller" ValidateEmptyText="True"></asp:CustomValidator>
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
                                <td style="font-size: 10pt; text-align: left; width: 900px;">Äèàìåòð ïî ïåðåäíåìó<br />
                                    êîíöó, ìì</td>
                                <td style="text-align: left; width: 900px;">Äèàìåòð ïî çàäíåìó<br />
                                    êîíöó, ìì</td>
                                <td style="text-align: left; width: 900px;">Äèàìåòð ïî òåëó, ìì</td>
                                <td style="text-align: left; width: 900px;"></td>
                            </tr>
                            <tr>
                                <td style="text-align: left; width: 900px;">
                                    <asp:TextBox ID="txtDiametrOnFrontEnd" runat="server" MaxLength="8"
                                        TabIndex="9" Width="180px"></asp:TextBox>
                                    &nbsp; &nbsp;<asp:CustomValidator ID="CusVal_txtDiametrOnFrontEnd" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="txtDiametrOnFrontEnd" ErrorMessage="Îøèáêà"
                                        Font-Size="Smaller"></asp:CustomValidator>
                                    &nbsp;</td>
                                <td style="text-align: left; width: 900px;">
                                    <asp:TextBox ID="txtDiametrOnBackEnd" runat="server" MaxLength="8"
                                        TabIndex="10" Width="180px"></asp:TextBox>
                                    &nbsp;
                                    <asp:CustomValidator ID="CusVal_txtDiametrOnBackEnd" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="txtDiametrOnBackEnd" ErrorMessage="Îøèáêà"
                                        Font-Size="Smaller"></asp:CustomValidator></td>
                                <td style="text-align: left; width: 900px;">
                                    <asp:TextBox ID="txtDiametrOnBody" runat="server" MaxLength="8"
                                        TabIndex="11" Width="180px"></asp:TextBox>
                                    &nbsp; &nbsp;<asp:CustomValidator ID="CusVal_txtDiametrOnBodyEnd" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="txtDiametrOnBody" ErrorMessage="Îøèáêà"
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
                                <td style="font-size: 10pt; text-align: left; width: 900px;">Äëèíà òðóáû, ì</td>
                                <td style="text-align: left; width: 900px;">
                                    <img src="Images/CX.png" />
                                    Òîëùèíà ñòåíêè, ìì<br />
                                    (çàìåð 1 / çàìåð 2)</td>
                                <td style="text-align: left; width: 900px;">Ñìåùåíèå êðîìîê òðóáû, ìì</td>
                                <td style="text-align: left; width: 900px;"></td>
                            </tr>
                            <tr>
                                <td style="text-align: left; width: 900px;">
                                    <asp:TextBox ID="txtDlina" runat="server" MaxLength="5" TabIndex="12"
                                        Width="180px"></asp:TextBox>
                                    &nbsp; &nbsp;<asp:CustomValidator ID="CusVal_txtDlina" runat="server"
                                        ClientValidationFunction="fncClientValidation" ControlToValidate="txtDlina"
                                        ErrorMessage="Îøèáêà" Font-Size="Smaller"></asp:CustomValidator>
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
                                                    ErrorMessage="Îøèáêà" Font-Size="Smaller"></asp:CustomValidator>
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
                                        ControlToValidate="txtSmeschKrom" ErrorMessage="Îøèáêà" Font-Size="Smaller"></asp:CustomValidator>
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
                                <td style="text-align: left; width: 900px;">Êîñèíà ðåçà
                                    <br />
                                    (ïåðåäíèé òîðåö), ìì</td>
                                <td style="text-align: left; width: 900px;">Êîñèíà ðåçà
                                    <br />
                                    (çàäíèé òîðåö), ìì</td>
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
                                        ControlToValidate="txtKosinaPTor" ErrorMessage="Îøèáêà" Font-Size="Smaller"></asp:CustomValidator>
                                </td>
                                <td style="text-align: left; font-size: 12pt; width: 900px; color: #000000;">
                                    <asp:TextBox ID="txtKosinaZTor" runat="server" MaxLength="3" TabIndex="17"
                                        Width="180px"></asp:TextBox>
                                    &nbsp;
                                    <asp:CustomValidator ID="CusVal_txtKosinaZTor" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="txtKosinaZTor" ErrorMessage="Îøèáêà" Font-Size="Smaller"></asp:CustomValidator>
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
                                <td style="text-align: left; width: 900px;">Êðèâèçíà íà 1 ì
                                    <br />
                                    òðóáû, ìì</td>
                                <td style="text-align: left; width: 900px;">Êðèâèçíà òðóáû ïî âñåé
                                    <br />
                                    äëèíå, ìì</td>
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
                                        ControlToValidate="txtKrivizna1mT" ErrorMessage="Îøèáêà" Font-Size="Smaller"></asp:CustomValidator>
                                </td>
                                <td style="text-align: left; font-size: 12pt; width: 900px; color: #000000;">
                                    <asp:TextBox ID="txtKriviznaVciaT" runat="server" MaxLength="4" TabIndex="19"
                                        Width="180px"></asp:TextBox>
                                    &nbsp;
                                    <asp:CustomValidator ID="CusVal_txtKriviznaVciaT" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="txtKriviznaVciaT" ErrorMessage="Îøèáêà" Font-Size="Smaller"></asp:CustomValidator>
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
                                <td style="text-align: left; width: 900px;">Îñòàòîê âíóòðåííåãî
                                    <br />
                                    ãðàòà, ìì</td>
                                <td style="text-align: left; width: 900px;">Îñòàòîê íàðóæíîãî
                                    <br />
                                    ãðàòà, ìì</td>
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
                                        ControlToValidate="txtOstatokVnutGrata" ErrorMessage="Îøèáêà"
                                        Font-Size="Smaller"></asp:CustomValidator>
                                </td>
                                <td style="text-align: left; width: 900px;">
                                    <asp:TextBox ID="txtOstatokNarujGrata" runat="server" MaxLength="3"
                                        TabIndex="21" Width="180px"></asp:TextBox>
                                    &nbsp;
                                    <asp:CustomValidator ID="CusVal_txtOstatokNarujGrata" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="txtOstatokNarujGrata" ErrorMessage="Îøèáêà"
                                        Font-Size="Smaller"></asp:CustomValidator>
                                </td>
                                <td style="text-align: left; width: 900px;"></td>
                                <td style="text-align: left; width: 900px;"></td>
                            </tr>
                            <%-- Äîáàâëåíû íîâûå ïîëÿ 05.10.17 Ðîìàíîâ Ñ.À.--%>
                            <tr>
                                <td style="text-align: left; font-size: 10px; width: 900px;"></td>
                                <td style="text-align: left; font-size: 10px; width: 900px;"></td>
                                <td style="text-align: left; font-size: 10px; width: 900px;"></td>
                                <td style="text-align: left; font-size: 10px; width: 900px;"></td>
                            </tr>
                            <tr>
                                <td style="font-size: 10pt; text-align: left" colspan="2">Óãîë ñêîñà ôàñêè ïåðåäíèé òîðåö, ãðàä</td>
                                <td style="text-align: left" colspan="2">Óãîë ñêîñà ôàñêè çàäíèé òîðåö, ãðàä</td>
                            </tr>
                            <tr>
                                <td style="font-size: 10pt; text-align: left; width: 900px;">ìèíèìàëüíûé</td>
                                <td style="text-align: left; width: 900px;">ìàêñèìàëüíûé</td>
                                <td style="font-size: 10pt; text-align: left; width: 900px;">ìèíèìàëüíûé</td>
                                <td style="text-align: left; width: 900px;">ìàêñèìàëüíûé</td>
                            </tr>
                            <tr>
                                <td style="text-align: left; width: 900px;">
                                    <asp:TextBox ID="txtYgolSkosaFaskiPTorMin" runat="server" MaxLength="8"
                                        TabIndex="22" Width="180px"></asp:TextBox>
                                    &nbsp; &nbsp;<asp:CustomValidator ID="CusVal_txtYgolSkosaFaskiPTorMin" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="txtYgolSkosaFaskiPTorMin" ErrorMessage="Îøèáêà"
                                        Font-Size="Smaller"></asp:CustomValidator>
                                    &nbsp;
                                </td>
                                <td style="text-align: left; width: 900px;">
                                    <asp:TextBox ID="txtYgolSkosaFaskiPTorMax" runat="server" MaxLength="8"
                                        TabIndex="23" Width="180px"></asp:TextBox>
                                    &nbsp;
                                    <asp:CustomValidator ID="CusVal_txtYgolSkosaFaskiPTorMax" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="txtYgolSkosaFaskiPTorMax" ErrorMessage="Îøèáêà"
                                        Font-Size="Smaller"></asp:CustomValidator>
                                </td>
                                <td style="text-align: left; width: 900px;">
                                    <asp:TextBox ID="txtYgolSkosaFaskiZTorMin" runat="server" MaxLength="8"
                                        TabIndex="24" Width="180px"></asp:TextBox>
                                    &nbsp;
                                    <asp:CustomValidator ID="CusVal_txtYgolSkosaFaskiZTorMin" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="txtYgolSkosaFaskiZTorMin" ErrorMessage="Îøèáêà"
                                        Font-Size="Smaller"></asp:CustomValidator>
                                </td>
                                <td style="text-align: left; width: 900px;">
                                    <asp:TextBox ID="txtYgolSkosaFaskiZTorMax" runat="server" MaxLength="8"
                                        TabIndex="25" Width="180px"></asp:TextBox>
                                    &nbsp;
                                    <asp:CustomValidator ID="CusVal_txtYgolSkosaFaskiZTorMax" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="txtYgolSkosaFaskiZTorMax" ErrorMessage="Îøèáêà"
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
                                <td style="font-size: 10pt; text-align: left; height: 20px;" colspan="2">Øèðèíà òîðöåâîãî êîëüöà ïåðåäíèé òîðåö, ìì</td>
                                <td style="text-align: left; height: 20px;" colspan="2">Øèðèíà òîðöåâîãî êîëüöà çàäíèé òîðåö, ìì</td>
                            </tr>
                            <tr>
                                <td style="font-size: 10pt; text-align: left; width: 900px;">ìèíèìàëüíûé</td>
                                <td style="text-align: left; width: 900px;">ìàêñèìàëüíûé</td>
                                <td style="font-size: 10pt; text-align: left; width: 900px;">ìèíèìàëüíûé</td>
                                <td style="text-align: left; width: 900px;">ìàêñèìàëüíûé</td>
                            </tr>
                            <tr>
                                <td style="text-align: left; width: 900px;">
                                    <asp:TextBox ID="txtShirinaTorKolPTorMin" runat="server" MaxLength="8"
                                        TabIndex="26" Width="180px"></asp:TextBox>
                                    &nbsp; &nbsp;<asp:CustomValidator ID="CusVal_txtShirinaTorKolPTorMin" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="txtShirinaTorKolPTorMin" ErrorMessage="Îøèáêà"
                                        Font-Size="Smaller"></asp:CustomValidator>

                                </td>
                                <td style="text-align: left; width: 900px;">
                                    <asp:TextBox ID="txtShirinaTorKolPTorMax" runat="server" MaxLength="8"
                                        TabIndex="27" Width="180px"></asp:TextBox>
                                    &nbsp;
                                    <asp:CustomValidator ID="CusVal_txtShirinaTorKolPTorMax" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="txtShirinaTorKolPTorMax" ErrorMessage="Îøèáêà"
                                        Font-Size="Smaller"></asp:CustomValidator>
                                </td>
                                <td style="text-align: left; width: 900px;">
                                    <asp:TextBox ID="txtShirinaTorKolZTorMin" runat="server" MaxLength="8"
                                        TabIndex="28" Width="180px"></asp:TextBox>
                                    &nbsp;
                                    <asp:CustomValidator ID="CusVal_txtShirinaTorKolZTorMin" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="txtShirinaTorKolZTorMin" ErrorMessage="Îøèáêà"
                                        Font-Size="Smaller"></asp:CustomValidator>
                                </td>
                                <td style="text-align: left; width: 900px;">
                                    <asp:TextBox ID="txtShirinaTorKolZTorMax" runat="server" MaxLength="8"
                                        TabIndex="29" Width="180px"></asp:TextBox>
                                    &nbsp; &nbsp;<asp:CustomValidator ID="CusVal_txtShirinaTorKolZTorMax" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="txtShirinaTorKolZTorMax" ErrorMessage="Îøèáêà"
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
                                <td style="font-size: 10pt; text-align: left" colspan="2">Êðèâèçíà êîíöåâàÿ ïåðåäíèé êîíåö òðóáû, ìì</td>
                                <td style="font-size: 10pt; text-align: left" colspan="2">Êðèâèçíà êîíöåâàÿ çàäíèé êîíåö òðóáû, ìì</td>
                            </tr>
                            <tr>
                                <td style="font-size: 10pt; text-align: left; width: 900px;">íà 1 ìåòðå</td>
                                <td style="text-align: left; width: 900px;">íà 1,5 ìåòðàõ</td>
                                <td style="font-size: 10pt; text-align: left; width: 900px;">íà 1 ìåòðå</td>
                                <td style="text-align: left; width: 900px;">íà 1,5 ìåòðàõ</td>
                            </tr>
                            <tr>
                                <td style="text-align: left; width: 900px;">
                                    <asp:TextBox ID="txtCURVATURE_FRONT_END_1000MM" runat="server" MaxLength="8"
                                        TabIndex="30" Width="180px"></asp:TextBox>
                                    &nbsp; &nbsp;<asp:CustomValidator ID="CusVal_txtCURVATURE_FRONT_END_1000MM" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="txtCURVATURE_FRONT_END_1000MM" ErrorMessage="Îøèáêà"
                                        Font-Size="Smaller"></asp:CustomValidator>
                                    &nbsp;
                                </td>
                                <td style="text-align: left; width: 900px;">
                                    <asp:TextBox ID="txtCURVATURE_FRONT_END_1500MM" runat="server" MaxLength="8"
                                        TabIndex="31" Width="180px"></asp:TextBox>
                                    &nbsp; &nbsp;<asp:CustomValidator ID="CusVal_txtCURVATURE_FRONT_END_1500MM" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="txtCURVATURE_FRONT_END_1500MM" ErrorMessage="Îøèáêà"
                                        Font-Size="Smaller"></asp:CustomValidator>
                                    &nbsp;
                                </td>
                                <td style="text-align: left; width: 900px;">
                                    <asp:TextBox ID="CURVATURE_BACK_END_1000MM" runat="server" MaxLength="8"
                                        TabIndex="32" Width="180px"></asp:TextBox>
                                    &nbsp; &nbsp;<asp:CustomValidator ID="CusVal_CURVATURE_BACK_END_1000MM" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="CURVATURE_BACK_END_1000MM" ErrorMessage="Îøèáêà"
                                        Font-Size="Smaller"></asp:CustomValidator>
                                    &nbsp;
                                </td>
                                <td style="text-align: left; width: 900px;">
                                    <asp:TextBox ID="CURVATURE_BACK_END_1500MM" runat="server" MaxLength="8"
                                        TabIndex="33" Width="180px"></asp:TextBox>
                                    &nbsp; &nbsp;<asp:CustomValidator ID="CusVal_CURVATURE_BACK_END_1500MM" runat="server"
                                        ClientValidationFunction="fncClientValidation"
                                        ControlToValidate="CURVATURE_BACK_END_1500MM" ErrorMessage="Îøèáêà"
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
                                <td style="text-align: left; width: 900px;">Ñîñòîÿíèå íàðóæíîé ïîâåðõíîñòè:</td>
                                <td colspan="3" style="text-align: left;">
                                    <asp:DropDownList ID="ddlOuterDefect" runat="server" TabIndex="34">
                                    </asp:DropDownList>
                                </td>
                            </tr>
                            <tr>
                                <td style="text-align: left; width: 900px;">Ñîñòîÿíèå âíóòðåííåé ïîâåðõíîñòè:</td>
                                <td colspan="3" style="text-align: left;">
                                    <asp:DropDownList ID="ddlInnerDefect" runat="server" TabIndex="35">
                                    </asp:DropDownList>
                                </td>
                            </tr>
                            <tr>
                                <td style="text-align: left; width: 900px;">Çàóñåíöû íà òîðöàõ:</td>
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
                            OnClientClick="return fncSave(this);" TabIndex="37" Text="Ñîõðàíèòü"
                            UseSubmitBehavior="False" Width="109px" Font-Size="10pt" />
                        <asp:Button ID="btnBack" runat="server" Height="23px" OnClick="btnBack_Click"
                            TabIndex="38" Text="Îòìåíà" UseSubmitBehavior="False" Width="110px"
                            Font-Size="10pt" />
                    </td>
                </tr>
            </table>
        </asp:View>
        <asp:View ID="vContenueAfterSave" runat="server">
            <table>
                <tr>
                    <td style="width: 667px; height: 28px; vertical-align: middle; text-align: left;">
                        <font size="2">Äàííûå óñïåøíî ñîõðàíåíû</font>&nbsp; &nbsp;<asp:Button
                            ID="btnContinue" runat="server" Height="23px" OnClick="btnContinue_Click"
                            TabIndex="1" Text="Ïðîäîëæèòü" Font-Size="10pt" />
                    </td>
                </tr>
            </table>
        </asp:View>
    </asp:MultiView>
    &nbsp; 
</asp:Content>
