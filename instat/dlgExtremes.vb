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

Imports instat.Translations

Public Class dlgExtremes
    Private clsAttachFunction As New RFunction
    Private clsDetachFunction As New RFunction

    Private clsFevdFunction, clsPriorParamListFunction, clsPlotsFunction, clsConcatenateFunction, clsConfidenceIntervalFunction,
clsInitialListFunction, clsOmitMissingFunction As New RFunction
    'clsLocationScaleResetOperator is not run but affects reset of the check box.Any better method of implementation?
    Private clsLocationScaleResetOperator As New ROperator
    Private clsLocationParamOperator As New ROperator
    Private bFirstLoad As Boolean = True
    Private bReset As Boolean = True
    Private bResettingDialogue As Boolean = False
    Private bResetSubDialogue As Boolean = False
    Private strFirstParam As String = "0.1,10,0.1"
    Private Sub dlgExtremes_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        If bFirstLoad Then
            InitialiseDialog()
            bFirstLoad = False
        End If
        If bReset Then
            SetDefaults()
        End If
        SetRCodeForControls(bReset)
        bReset = False
        autoTranslate(Me)
        TestOkEnabled()
    End Sub

    Private Sub InitialiseDialog()
        Dim dctFevdTypes As New Dictionary(Of String, String)

        ucrInputExtremes.SetParameter(New RParameter("type", 1))
        dctFevdTypes.Add("GEV", Chr(34) & "GEV" & Chr(34))
        dctFevdTypes.Add("GP", Chr(34) & "GP" & Chr(34))
        dctFevdTypes.Add("PP", Chr(34) & "PP" & Chr(34))
        dctFevdTypes.Add("Gumbel", Chr(34) & "Gumbel" & Chr(34))
        dctFevdTypes.Add("Exponential", Chr(34) & "Exponential" & Chr(34))
        ucrInputExtremes.SetItems(dctFevdTypes)
        ucrInputExtremes.SetDropDownStyleAsNonEditable()

        ucrBase.clsRsyntax.iCallType = 2
        ucrBase.clsRsyntax.bExcludeAssignedFunctionOutput = False

        ucrReceiverVariable.Selector = ucrSelectorExtremes
        ucrReceiverVariable.strSelectorHeading = "Variables"
        ucrReceiverVariable.SetMeAsReceiver()
        ucrReceiverVariable.SetParameter(New RParameter("x", 0))
        ucrReceiverVariable.SetParameterIsRFunction()

        ucrChkExplanatoryModelForLocationParameter.SetText("Explanatory Model for Location or Scale")
        ucrChkExplanatoryModelForLocationParameter.AddParameterPresentCondition(True, "scaleLocation", True)
        ucrChkExplanatoryModelForLocationParameter.AddParameterPresentCondition(False, "scaleLocation", False)
        ucrChkExplanatoryModelForLocationParameter.AddToLinkedControls(ucrReceiverExpressionExplanatoryModelForLocParam, {True}, bNewLinkedHideIfParameterMissing:=True, bNewLinkedAddRemoveParameter:=True)

        ucrInputThresholdforLocation.SetValidationTypeAsNumeric()
        ucrInputThresholdforLocation.AddQuotesIfUnrecognised = False
        ucrInputThresholdforLocation.SetLinkedDisplayControl(lblThreshold)

        ucrReceiverExpressionExplanatoryModelForLocParam.SetParameter(New RParameter("locationParam", 1, bNewIncludeArgumentName:=False))
        ucrReceiverExpressionExplanatoryModelForLocParam.SetParameterIsString()
        ucrReceiverExpressionExplanatoryModelForLocParam.SetRDefault(1)
        ucrReceiverExpressionExplanatoryModelForLocParam.bWithQuotes = False
        ucrReceiverExpressionExplanatoryModelForLocParam.Selector = ucrSelectorExtremes

        ucrTryModelling.SetReceiver(ucrReceiverExpressionExplanatoryModelForLocParam)
        ucrTryModelling.SetIsModel()

        ucrSaveExtremes.SetPrefix("extreme")
        ucrSaveExtremes.SetIsComboBox()
        ucrSaveExtremes.SetCheckBoxText("Save Model:")
        ucrSaveExtremes.SetSaveTypeAsModel()
        ucrSaveExtremes.SetDataFrameSelector(ucrSelectorExtremes.ucrAvailableDataFrames)
        ucrSaveExtremes.SetAssignToIfUncheckedValue("last_model")
    End Sub

    Private Sub SetDefaults()
        clsFevdFunction = New RFunction
        clsPlotsFunction = New RFunction
        clsPriorParamListFunction = New RFunction
        clsInitialListFunction = New RFunction
        clsLocationParamOperator = New ROperator
        clsLocationScaleResetOperator = New ROperator
        clsAttachFunction = New RFunction
        clsDetachFunction = New RFunction
        clsConfidenceIntervalFunction = New RFunction
        clsConcatenateFunction = New RFunction
        clsOmitMissingFunction = New RFunction
        ucrBase.clsRsyntax.ClearCodes()

        ucrReceiverVariable.SetMeAsReceiver()
        ucrSelectorExtremes.Reset()
        ucrInputThresholdforLocation.SetText("0")
        ucrSaveExtremes.Reset()
        bResetSubDialogue = True

        clsConcatenateFunction.SetRCommand("c")
        clsConcatenateFunction.AddParameter("first", strFirstParam, iPosition:=0, bIncludeArgumentName:=False)

        clsLocationScaleResetOperator.SetOperation("")
        clsLocationScaleResetOperator.bBrackets = False

        clsLocationParamOperator.SetOperation("~")
        clsLocationParamOperator.AddParameter(strParameterValue:="", iPosition:=0, bIncludeArgumentName:=False)
        clsLocationParamOperator.bSpaceAroundOperation = False


        clsPriorParamListFunction.SetRCommand("list")
        clsPriorParamListFunction.AddParameter("v", clsRFunctionParameter:=clsConcatenateFunction, iPosition:=5)

        clsInitialListFunction.SetRCommand("list")
        clsInitialListFunction.AddParameter("location", "0", iPosition:=0)
        clsInitialListFunction.AddParameter("scale", "0.1", iPosition:=1)
        clsInitialListFunction.AddParameter("shape", "-0.5", iPosition:=2)

        'todo. What's the use of this RFunction?
        clsConfidenceIntervalFunction.SetPackageName("extRemes")
        clsConfidenceIntervalFunction.SetRCommand("ci.fevd")

        clsFevdFunction.SetPackageName("extRemes")
        clsFevdFunction.SetRCommand("fevd")
        clsFevdFunction.AddParameter("type", Chr(34) & "GEV" & Chr(34), iPosition:=0)
        clsFevdFunction.AddParameter("method", Chr(34) & "MLE" & Chr(34), iPosition:=1)
        clsFevdFunction.AddParameter("na.action", "na.omit", iPosition:=3)
        clsFevdFunction.bExcludeAssignedFunctionOutput = False
        clsFevdFunction.SetAssignToOutputObject(strRObjectToAssignTo:="last_model",
                                           strRObjectTypeLabelToAssignTo:=RObjectTypeLabel.Model,
                                           strRObjectFormatToAssignTo:=RObjectFormat.Text,
                                           strRDataFrameNameToAddObjectTo:=ucrSelectorExtremes.strCurrentDataFrame,
                                           strObjectName:="last_model")


        clsPlotsFunction.SetRCommand("plot")
        clsPlotsFunction.bExcludeAssignedFunctionOutput = False

        clsOmitMissingFunction.SetRCommand("na.omit")
        clsOmitMissingFunction.SetPackageName("stats")
        clsOmitMissingFunction.AddParameter("object", clsRFunctionParameter:=ucrSelectorExtremes.ucrAvailableDataFrames.clsCurrDataFrame, iPosition:=0)


        'todo. are they needed?
        'clsAttachFunction.SetRCommand("attach")
        'clsAttachFunction.AddParameter("what", clsRFunctionParameter:=clsOmitMissingFunction, iPosition:=0)

        'clsDetachFunction.SetRCommand("detach")
        'clsDetachFunction.AddParameter("name", clsRFunctionParameter:=clsOmitMissingFunction, iPosition:=0)
        'clsDetachFunction.AddParameter("unload", "TRUE", iPosition:=2)

        'ucrBase.clsRsyntax.AddToBeforeCodes(clsAttachFunction)
        'ucrBase.clsRsyntax.AddToAfterCodes(clsDetachFunction, iPosition:=1)
        ucrBase.clsRsyntax.SetBaseRFunction(clsFevdFunction)
        ucrTryModelling.SetRSyntax(ucrBase.clsRsyntax)
    End Sub


    Private Sub assignToControlsChanged(ucrChangedControl As ucrCore) Handles ucrSaveExtremes.ControlValueChanged
        'model plot output
        clsPlotsFunction.AddParameter("x", strParameterValue:=clsFevdFunction.GetRObjectToAssignTo(), iPosition:=0)
        clsPlotsFunction.SetAssignToOutputObject(strRObjectToAssignTo:="last_graph",
                                     strRObjectTypeLabelToAssignTo:=RObjectTypeLabel.Graph,
                                     strRObjectFormatToAssignTo:=RObjectFormat.Image,
                                     strRDataFrameNameToAddObjectTo:=ucrSelectorExtremes.strCurrentDataFrame,
                                     strObjectName:="last_graph")
    End Sub

    Private Sub SetRCodeForControls(bReset As Boolean)
        ucrInputExtremes.SetRCode(clsFevdFunction, bReset)
        ucrReceiverVariable.SetRCode(clsFevdFunction, bReset)
        ucrSaveExtremes.SetRCode(clsFevdFunction, bReset)
        ucrChkExplanatoryModelForLocationParameter.SetRCode(clsLocationScaleResetOperator, bReset)
        ucrReceiverExpressionExplanatoryModelForLocParam.SetRCode(clsLocationParamOperator, bReset)
    End Sub

    Private Sub ucrBase_ClickReset(sender As Object, e As EventArgs) Handles ucrBase.ClickReset
        bResettingDialogue = True
        SetDefaults()
        SetRCodeForControls(True)
        bResettingDialogue = False
    End Sub

    Private Sub TestOkEnabled()
        ucrBase.OKEnabled(Not ucrReceiverVariable.IsEmpty)
    End Sub
    Private Sub cmdFittingOptions_Click(sender As Object, e As EventArgs) Handles cmdFittingOptions.Click
        sdgExtremesMethod.SetRCode(clsNewFevdFunction:=clsFevdFunction, clsNewPriorParamListFunction:=clsPriorParamListFunction,
                                   clsNewConcatenateFunction:=clsConcatenateFunction, bReset:=bResetSubDialogue,
                                   clsNewPlotFunction:=clsPlotsFunction, clsNewConfidenceIntervalFunction:=clsConfidenceIntervalFunction,
                                   clsNewInitialListFunction:=clsInitialListFunction, clsNewRSyntax:=ucrBase.clsRsyntax)
        sdgExtremesMethod.ShowDialog()
        bResetSubDialogue = False
    End Sub

    Private Sub cmdPlus_Click(sender As Object, e As EventArgs) Handles cmdPlus.Click
        ucrReceiverExpressionExplanatoryModelForLocParam.AddToReceiverAtCursorPosition("+")
    End Sub

    Private Sub cmdColon_Click(sender As Object, e As EventArgs) Handles cmdColon.Click
        ucrReceiverExpressionExplanatoryModelForLocParam.AddToReceiverAtCursorPosition(":")
    End Sub

    Private Sub cmdMultiply_Click(sender As Object, e As EventArgs) Handles cmdMultiply.Click
        ucrReceiverExpressionExplanatoryModelForLocParam.AddToReceiverAtCursorPosition("*")
    End Sub

    Private Sub cmdDiv_Click(sender As Object, e As EventArgs) Handles cmdDiv.Click
        ucrReceiverExpressionExplanatoryModelForLocParam.AddToReceiverAtCursorPosition("/")
    End Sub

    Private Sub cmdDoubleBracket_Click(sender As Object, e As EventArgs) Handles cmdDoubleBracket.Click
        ucrReceiverExpressionExplanatoryModelForLocParam.AddToReceiverAtCursorPosition("( )", 2)
    End Sub

    Private Sub cmdOpeningBracket_Click(sender As Object, e As EventArgs) Handles cmdOpeningBracket.Click
        ucrReceiverExpressionExplanatoryModelForLocParam.AddToReceiverAtCursorPosition("(")
    End Sub

    Private Sub cmdClosingBracket_Click(sender As Object, e As EventArgs) Handles cmdClosingBracket.Click
        ucrReceiverExpressionExplanatoryModelForLocParam.AddToReceiverAtCursorPosition(")")
    End Sub

    Private Sub cmdPower_Click(sender As Object, e As EventArgs) Handles cmdPower.Click
        ucrReceiverExpressionExplanatoryModelForLocParam.AddToReceiverAtCursorPosition("^")
    End Sub

    Private Sub cmdMinus_Click(sender As Object, e As EventArgs) Handles cmdMinus.Click
        ucrReceiverExpressionExplanatoryModelForLocParam.AddToReceiverAtCursorPosition("-")
    End Sub

    Private Sub cmdZero_Click(sender As Object, e As EventArgs) Handles cmdZero.Click
        ucrReceiverExpressionExplanatoryModelForLocParam.AddToReceiverAtCursorPosition("I( )", 1)

    End Sub

    Private Sub cmdClear_Click(sender As Object, e As EventArgs) Handles cmdClear.Click
        ucrReceiverExpressionExplanatoryModelForLocParam.Clear()
    End Sub

    Private Sub cmdSqrt_Click(sender As Object, e As EventArgs) Handles cmdSqrt.Click
        ucrReceiverExpressionExplanatoryModelForLocParam.AddToReceiverAtCursorPosition("sqrt( )", 2)
    End Sub

    Private Sub cmdCos_Click(sender As Object, e As EventArgs) Handles cmdCos.Click
        ucrReceiverExpressionExplanatoryModelForLocParam.AddToReceiverAtCursorPosition("cos( )", 2)
    End Sub

    Private Sub cmdLog_Click(sender As Object, e As EventArgs) Handles cmdLog.Click
        ucrReceiverExpressionExplanatoryModelForLocParam.AddToReceiverAtCursorPosition("log( )", 2)
    End Sub

    Private Sub cmdSin_Click(sender As Object, e As EventArgs) Handles cmdSin.Click
        ucrReceiverExpressionExplanatoryModelForLocParam.AddToReceiverAtCursorPosition("sin( )", 2)
    End Sub

    Private Sub cmdExp_Click(sender As Object, e As EventArgs) Handles cmdExp.Click
        ucrReceiverExpressionExplanatoryModelForLocParam.AddToReceiverAtCursorPosition("exp( )", 2)
    End Sub

    Private Sub cmdTan_Click(sender As Object, e As EventArgs) Handles cmdTan.Click
        ucrReceiverExpressionExplanatoryModelForLocParam.AddToReceiverAtCursorPosition("tan( )", 2)
    End Sub

    Private Sub control_ControlValueChanged(ucrChangedControl As ucrCore) Handles ucrInputExtremes.ControlValueChanged, ucrReceiverVariable.ControlValueChanged, ucrChkExplanatoryModelForLocationParameter.ControlValueChanged, ucrInputThresholdforLocation.ControlValueChanged
        ParameterControl()
        ucrTryModelling.ResetInputTryMessage()
        TestOkEnabled()
    End Sub

    ''' <summary> 
    ''' If the text selected in the extremes combobox is "GP", "PP" or "Exponential", then the 
    ''' threshold for the location textbox is made visible, else it is made invisible.
    ''' <para>
    ''' If the text selected in the extremes combobox is "GP", "PP" or "Exponential", then 
    ''' the "scale.fun" parameter is added to the fevd function and the "location.fun" parameter 
    ''' is removed from the fevd function. 
    ''' </para><para>
    ''' If the text selected in the extremes combobox is "Gumbel" or "GEV", then the "location.fun" 
    ''' parameter is added to the fevd function and the "scale.fun" parameter is removed from 
    ''' the fevd function.
    ''' </para></summary>	   
    Private Sub ParameterControl()
        If ucrInputExtremes.GetText() = "GP" OrElse ucrInputExtremes.GetText() = "PP" OrElse ucrInputExtremes.GetText() = "Exponential" Then
            ucrInputThresholdforLocation.Visible = True
            clsFevdFunction.AddParameter("threshold", ucrInputThresholdforLocation.GetText(), iPosition:=3)
        Else
            ucrInputThresholdforLocation.Visible = False
            clsFevdFunction.RemoveParameterByName("threshold")
        End If
        If ucrChkExplanatoryModelForLocationParameter.Checked Then
            Select Case ucrInputExtremes.GetText()
                Case "GP", "PP", "Exponential"
                    clsFevdFunction.AddParameter("scale.fun", clsROperatorParameter:=clsLocationParamOperator, iPosition:=4)
                    clsFevdFunction.RemoveParameterByName("location.fun")
                Case "Gumbel", "GEV"
                    clsFevdFunction.AddParameter("location.fun", clsROperatorParameter:=clsLocationParamOperator, iPosition:=5)
                    clsFevdFunction.RemoveParameterByName("scale.fun")
            End Select
            If Not bResettingDialogue Then
                clsLocationScaleResetOperator.AddParameter("scaleLocation", clsROperatorParameter:=clsLocationParamOperator, iPosition:=0)
            End If
            grpFirstCalc.Visible = True
            grpSecondCalc.Visible = True
        Else
            ucrReceiverVariable.SetMeAsReceiver()
            clsFevdFunction.RemoveParameterByName("scale.fun")
            clsFevdFunction.RemoveParameterByName("location.fun")
            clsLocationScaleResetOperator.RemoveParameterByName("scaleLocation")

            grpFirstCalc.Visible = False
            grpSecondCalc.Visible = False
        End If
    End Sub

    Private Sub ucrReceiverExpressionExplanatoryModelForLocParam_ControlValueChanged(ucrChangedControl As ucrCore) Handles ucrReceiverExpressionExplanatoryModelForLocParam.ControlValueChanged
        Dim strExplanatory As String = ucrReceiverExpressionExplanatoryModelForLocParam.GetText()
        If ucrChkExplanatoryModelForLocationParameter.Checked _
             AndAlso Not ucrReceiverExpressionExplanatoryModelForLocParam.IsEmpty() Then
            Dim strTempParam As String = strFirstParam
            If strExplanatory.Contains("+") Then
                For i = 0 To strExplanatory.Split("+").Length - 1
                    strTempParam &= ",0.1"
                Next
            ElseIf Not IsNumeric(strExplanatory) Then
                strTempParam &= ",0.1"
            End If
            clsConcatenateFunction.AddParameter("first", strTempParam, iPosition:=0, bIncludeArgumentName:=False)
        Else
            clsConcatenateFunction.AddParameter("first", strFirstParam, iPosition:=0, bIncludeArgumentName:=False)
        End If
    End Sub
End Class
