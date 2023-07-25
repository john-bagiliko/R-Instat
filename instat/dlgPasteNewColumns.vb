﻿' R- Instat
' Copyright (C) 2015-2017
'
' This program is free software: you can redistribute it and/or modify
' it under the terms of the GNU General Public License as published by
' the Free Software Foundation, either version 3 of the License, or
' (at your option) any later version.
'
' This program is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY; without even the implied warranty of
' MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
' GNU General Public License for more details.
'
' You should have received a copy of the GNU General Public License 
' along with this program.  If not, see <http://www.gnu.org/licenses/>.
Imports RDotNet
Imports instat.Translations
Public Class dlgPasteNewColumns
    Private bFirstLoad As Boolean = True
    Private bReset As Boolean = True
    'Private clsImportColsToNewDFRFunction, clsImportNewDataListRFunction As New RFunction
    Private clsReadClipBoardDataRFunction As New RFunction
    Private clsImportColsToExistingDFRFunction As RFunction
    'used to prevent TestOkEnabled from being called multiple times when loading the dialog. 
    Private bValidatePasteData As Boolean = False

    Private Sub dlgPasteNewColumns_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        If bFirstLoad Then
            InitialiseDialog()
            bFirstLoad = False
        End If
        If bReset Then
            SetDefaults()
        End If
        SetClipBoardDataParameter()  'reset the clip board data parameter value
        SetRCodeForControls(bReset)
        bReset = False
        bValidatePasteData = True
        TestOkEnabled()
        autoTranslate(Me)
    End Sub

    Private Sub dlgPasteNewColumns_Closed(sender As Object, e As EventArgs) Handles Me.Closed
        bValidatePasteData = False
    End Sub

    Private Sub InitialiseDialog()
        'todo. attach the help id later
        'ucrBase.iHelpTopicID = 

        '----------------------------
        ucrPnl.AddRadioButton(rdoDataFrame)
        ucrPnl.AddRadioButton(rdoColumns)
        ucrPnl.AddFunctionNamesCondition(rdoDataFrame, frmMain.clsRLink.strInstatDataObject & "$add_columns_to_data", bNewIsPositive:=False)
        ucrPnl.AddFunctionNamesCondition(rdoColumns, frmMain.clsRLink.strInstatDataObject & "$add_columns_to_data", bNewIsPositive:=True)
        ucrPnl.AddToLinkedControls(ucrSaveNewDFName, {rdoDataFrame}, bNewLinkedAddRemoveParameter:=True, bNewLinkedHideIfParameterMissing:=True)
        ucrPnl.AddToLinkedControls(ucrDFSelected, {rdoColumns}, bNewLinkedAddRemoveParameter:=True, bNewLinkedHideIfParameterMissing:=True)

        ucrChkRowHeader.SetText("First row is header")
        ucrChkRowHeader.SetParameter(New RParameter("header", 1))

        ucrNudPreviewLines.SetMinMax(iNewMin:=10, iNewMax:=1000)
        '----------------------------

        'paste as data frame
        '----------------------------
        ucrSaveNewDFName.SetIsTextBox()
        ucrSaveNewDFName.SetSaveTypeAsDataFrame()
        ucrSaveNewDFName.SetLabelText("New Data Frame Name:")
        ucrSaveNewDFName.SetPrefix("data")
        '----------------------------

        'paste as column
        '----------------------------
        ucrDFSelected.SetText("Paste copied data to:")
        ucrDFSelected.SetParameter(New RParameter("data_name", 0))
        ucrDFSelected.SetParameterIsString()
        '----------------------------

        ucrNudPreviewLines.Minimum = 10
    End Sub

    Private Sub SetDefaults()
        clsImportColsToExistingDFRFunction = New RFunction
        clsReadClipBoardDataRFunction = New RFunction

        ucrNudPreviewLines.Value = 10
        ucrSaveNewDFName.Reset()
        ucrDFSelected.Reset()

        clsReadClipBoardDataRFunction.SetPackageName("clipr")
        clsReadClipBoardDataRFunction.SetRCommand("read_clip_tbl")
        'todo. change to false
        clsReadClipBoardDataRFunction.AddParameter("header", "FALSE", iPosition:=1)

        clsImportColsToExistingDFRFunction.SetRCommand(frmMain.clsRLink.strInstatDataObject & "$add_columns_to_data")
        clsImportColsToExistingDFRFunction.AddParameter("col_data", clsRFunctionParameter:=clsReadClipBoardDataRFunction, iPosition:=1)
        clsImportColsToExistingDFRFunction.AddParameter("use_col_name_as_prefix", strParameterValue:="TRUE", iPosition:=2)


        ucrBase.clsRsyntax.SetBaseRFunction(clsReadClipBoardDataRFunction)
    End Sub

    Private Sub SetRCodeForControls(bReset As Boolean)
        ucrChkRowHeader.SetRCode(clsReadClipBoardDataRFunction, bReset)
        ucrDFSelected.SetRCode(clsImportColsToExistingDFRFunction, bReset)
        ucrSaveNewDFName.SetRCode(clsReadClipBoardDataRFunction, bReset)

        ucrPnl.SetRCode(ucrBase.clsRsyntax.clsBaseFunction, bReset)
    End Sub

    Private Sub SetClipBoardDataParameter()
        'please note addition of this parameter makes the execution of the R code take longer
        'compared to letting R read from the clipboard.
        'However this has been added to achieve reproducibility in future
        Try
            'clipboard data may have an empty line which is ignored by clipr so just trim it here to get accurate length
            'escape any double quotes because of how clipr is implemented. See issue #7199 for more details
            Dim clipBoardText As String = My.Computer.Clipboard.GetText.Trim().Replace("""", "\""")
            Dim arrStrTemp() As String = clipBoardText.Split(New String() {Environment.NewLine}, StringSplitOptions.None)
            If arrStrTemp.Length > 1000 Then
                MsgBox("Requested clipboard data has more than 1000 rows. Only a maximum of 1000 rows can be pasted")
                clsReadClipBoardDataRFunction.AddParameter("x", Chr(34) & "" & Chr(34), iPosition:=0)
            Else
                clsReadClipBoardDataRFunction.AddParameter("x", Chr(34) & clipBoardText & Chr(34), iPosition:=0)
            End If
        Catch ex As Exception
            'this error could be due to large clipboard data 
            MsgBox("Requested clipboard operation did not succeed. Large data detected")
        End Try
    End Sub


    ''' <summary>
    ''' validates copied data and displays it for preview.
    ''' </summary>
    ''' <returns>returns true if copied data can be pasted to the selected data frame or false if not</returns>
    Private Function ValidateAndPreviewCopiedData() As Boolean
        Try
            'reset feedback controls default states
            panelNoDataPreview.Visible = True
            lblConfirmText.Text = ""
            lblConfirmText.ForeColor = Color.Red

            Dim clsTempReadClipBoardDataRFunction As RFunction = clsReadClipBoardDataRFunction.Clone()

            clsTempReadClipBoardDataRFunction.AddParameter("nrows", ucrNudPreviewLines.Value)  'limit the rows to those set in the ucrNudPreviewLines control
            clsTempReadClipBoardDataRFunction.RemoveAssignTo() 'remove assign to before getting the script
            Dim dfTemp As DataFrame = frmMain.clsRLink.RunInternalScriptGetValue(clsTempReadClipBoardDataRFunction.ToScript(), bSilent:=True)?.AsDataFrame
            If dfTemp Is Nothing OrElse dfTemp.RowCount = 0 Then
                Return False
            End If

            'try to show preview the data only
            frmMain.clsGrids.FillSheet(dfTemp, "temp", grdDataPreview, bIncludeDataTypes:=False, iColMax:=frmMain.clsGrids.iMaxCols)
            lblConfirmText.Text = "Number of columns: " & dfTemp.ColumnCount & Environment.NewLine &
                                  "Number of rows: " & dfTemp.RowCount & Environment.NewLine

            If rdoDataFrame.Checked Then
                lblConfirmText.Text = lblConfirmText.Text & "Click Ok to paste data to new data frame."
            ElseIf rdoColumns.Checked Then
                'validate allowed number of rows
                If dfTemp.RowCount < ucrDFSelected.iDataFrameLength Then
                    lblConfirmText.Text = lblConfirmText.Text & "Too few rows to paste into this data frame. This data frame requires " & ucrDFSelected.iDataFrameLength & " rows."
                    'please note, we could allow few rows to be pasted. we can do that ammending add columns R code
                    'but as stated in issue #5991 by Danny this is unlikely to be the correct solution.
                    'Left here for reference
                    'lblConfirmText.Text = lblConfirmText.Text & Environment.NewLine &  (ucrDataFrameSelected.iDataFrameLength - dfTemp.RowCount) & " missing values will be added."
                    Return False
                ElseIf dfTemp.RowCount > ucrDFSelected.iDataFrameLength Then
                    lblConfirmText.Text = lblConfirmText.Text & "Too many rows to paste into this data frame. This data frame requires " & ucrDFSelected.iDataFrameLength & " rows."
                    Return False
                Else
                    lblConfirmText.Text = lblConfirmText.Text & "Click Ok to paste data to selected data frame."
                End If
            End If
            lblConfirmText.ForeColor = Color.Green
            panelNoDataPreview.Visible = False
            Return True
        Catch ex As Exception
            lblConfirmText.Text = "Could not preview data. Data cannot be pasted."
            panelNoDataPreview.Visible = True
            Return False
        End Try

    End Function

    Private Sub TestOkEnabled(Optional bValidateCopiedData As Boolean = True)
        Dim enableOK As Boolean = False
        If rdoDataFrame.Checked Then
            enableOK = ucrSaveNewDFName.IsComplete()
        ElseIf rdoColumns.Checked Then
            enableOK = Not String.IsNullOrEmpty(ucrDFSelected.strCurrDataFrame)
        End If
        If bValidateCopiedData Then
            enableOK = ValidateAndPreviewCopiedData()
        End If
        ucrBase.OKEnabled(enableOK)
    End Sub

    Private Sub ucrPnl_ControlValueChanged(ucrChangedControl As ucrCore) Handles ucrPnl.ControlValueChanged
        If rdoDataFrame.Checked Then
            ucrBase.clsRsyntax.SetBaseRFunction(clsReadClipBoardDataRFunction)
        ElseIf rdoColumns.Checked Then
            clsReadClipBoardDataRFunction.SetAssignToObject("data")
            ucrBase.clsRsyntax.SetBaseRFunction(clsImportColsToExistingDFRFunction)
        End If

        If bValidatePasteData Then
            TestOkEnabled()
        End If
    End Sub

    Private Sub btnRefreshPreview_Click(sender As Object, e As EventArgs) Handles btnRefreshPreview.Click
        SetClipBoardDataParameter()
        TestOkEnabled()
    End Sub

    Private Sub ucrControls_ControlContentsChanged(ucrchangedControl As ucrCore) Handles ucrChkRowHeader.ControlContentsChanged, ucrDFSelected.ControlContentsChanged, ucrSaveNewDFName.ControlContentsChanged, ucrNudPreviewLines.ControlContentsChanged
        If bValidatePasteData Then
            'disabled unnecessary validation of copied data because it may take long for large datasets
            TestOkEnabled(bValidateCopiedData:=ucrchangedControl IsNot ucrSaveNewDFName AndAlso ucrchangedControl IsNot ucrNudPreviewLines)
        End If
    End Sub

    Private Sub ucrBase_ClickReset(sender As Object, e As EventArgs) Handles ucrBase.ClickReset
        bValidatePasteData = False
        SetDefaults()
        SetClipBoardDataParameter()
        SetRCodeForControls(True)
        bValidatePasteData = True
        TestOkEnabled()
    End Sub


End Class