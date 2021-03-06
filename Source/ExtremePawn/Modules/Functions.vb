﻿Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Threading
Imports ScintillaNET
Imports System.Text
Imports System.ComponentModel

Public Class Functions
    Inherits Form

    'Functions Save to Save the file.
    Public Function Save(ByVal tab As Editor) As Boolean
        Dim result As Boolean
        If tab.Tag Is Nothing Then
            If MainForm.SaveFileDialog.ShowDialog() <> DialogResult.OK Then
                result = False
                Return result
            End If
            tab.TabText = Path.GetFileName(MainForm.SaveFileDialog.FileName)
            tab.SplitEditorCode.Tag = MainForm.SaveFileDialog.FileName

        End If
        Try
            My.Computer.FileSystem.WriteAllText(tab.Tag, tab.Controls(0).Text, False, System.Text.Encoding.Default)
        Catch ex As Exception
            If MessageBox.Show(ex.Message, "Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Hand) = DialogResult.Retry Then
                result = Save(tab)
                Return result
            End If
            result = False
            Return result
        End Try
        tab.Controls(0).Invalidate()
        result = True
        Return result
    End Function

    'Function Compile, Self explantory, To compile the file.
    Public Function Compile(ByVal CurrentTB As Scintilla)
        MainForm.Enabled = False

        Dim NumOfErrors As Integer
        Dim NumOfWarns As Integer

        MainForm.ToolStripButton4.PerformClick() 'Save

        MainForm.Status.Text = "Compiling"

        If CurrentTB.Tag Is Nothing Then
            MainForm.Enabled = True
            MsgBox("The current file is not saved, Please save it first.")
            Return False
        End If

        Dim FileName As String = System.IO.Path.GetFileName(CurrentTB.Tag.ToString)
        Dim path As String = System.IO.Path.GetDirectoryName(CurrentTB.Tag.ToString)

        Dim Args As String = Settings.AgrumentsTxt.Text.Replace("[FILE]", FileName)
        Dim pawncc As String = Settings.PawnccPath.Text.Replace("[APP]", Application.StartupPath)

        Dim consoleApp As New Process
        With consoleApp
            .StartInfo.UseShellExecute = False
            .StartInfo.RedirectStandardError = True
            .StartInfo.CreateNoWindow = True
            .StartInfo.FileName = pawncc
            .StartInfo.WorkingDirectory = path
            .StartInfo.Arguments = Args
            .Start()
            .WaitForExit()
        End With

        Dim output As String = consoleApp.StandardError.ReadToEnd()
        Dim errs() As String
        errs = output.Split(vbCrLf)

        ErrorsFrm.ErrorDataGridView.Rows.Clear()

        For Each s As String In errs
            Dim Type As Image = Nothing
            Dim ErrorTexte As String
            Dim File As String
            Dim LineNumbers As String
            If s.Contains("error") Then
                NumOfErrors += 1
                Type = My.Resources.ErrorImage
            ElseIf s.Contains("warn") Then
                NumOfWarns += 1
                Type = My.Resources.WarningImage
            End If

            Try
                File = s.Remove(s.IndexOf("("))

                ErrorTexte = s.Remove(0, s.IndexOf(" : ") + 1)
                ErrorTexte = ErrorTexte.Remove(0, ErrorTexte.IndexOf(":") + 1)
                ErrorTexte = ErrorTexte.Remove(0, ErrorTexte.IndexOf(":") + 1)

                LineNumbers = s.Remove(s.IndexOf(")"))
                LineNumbers = LineNumbers.Remove(0, LineNumbers.IndexOf("(") + 1)
                Dim Row() As Object = {Type, ErrorTexte, File, LineNumbers}
                ErrorsFrm.ErrorDataGridView.Rows.Add(Row)

                ErrorsFrm.ErrorDataGridView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells)

            Catch ex As Exception
                'Do nothing on exption.
            End Try

        Next
        If NumOfErrors = 0 Then
            MainForm.Status.Text = "Successfully Compiled With 0 Errors And " + NumOfWarns.ToString + " Warnings"
            MainForm.IdleMaker.Start()
            MainForm.Enabled = True
            Return True
        Else
            MainForm.Status.Text = "Compiling Failed With " + NumOfErrors.ToString + " Errors And " + NumOfWarns.ToString + " Warnings"
            MainForm.IdleMaker.Start()
            MainForm.Enabled = True
            Return False
        End If
    End Function

    'Functions LoadSettings to load the settings.
    Public Sub LoadSettings()
        Settings.AutoSaving.Checked = Convert.ToBoolean(SettingsHandler.GetSetting("AutoSave"))
        Settings.LineNumber.Checked = Convert.ToBoolean(SettingsHandler.GetSetting("LineNumber"))
        Settings.AutoCompletion.Checked = Convert.ToBoolean(SettingsHandler.GetSetting("AutoComplete"))
        Settings.SAMPServerDir.Text = Convert.ToString(SettingsHandler.GetSetting("SAMPServer"))
        Settings.SAMPClient.Text = Convert.ToString(SettingsHandler.GetSetting("SAMPClient"))
        Settings.AgrumentsTxt.Text = Convert.ToString(SettingsHandler.GetSetting("CompilerArgs"))
        Settings.PawnccPath.Text = Convert.ToString(SettingsHandler.GetSetting("PawnCCPath"))

        'Loading the key
        Dim Cnv As New KeysConverter
        Dim dta As String = Convert.ToString(SettingsHandler.GetSetting("CompileKey"))
        Settings.KEY_COMPILE = Cnv.ConvertFromString(dta)
        Settings.CompileLabel.Text = dta
    End Sub

    'Sub SetDefaultSettings To set the default values.
    Public Sub SetDefaultSettings(ByVal tb As Scintilla)
        tb.ContextMenuStrip = MainForm.RightClickMenu
        tb.Encoding = New UTF8Encoding(False)
        Control.CheckForIllegalCrossThreadCalls = False
    End Sub

    'Function CreateTab to create a new file and add it to the TabStrip.
    Public Function CreateTab(ByVal fileName As String, Optional ByVal SourceText As Scintilla = Nothing)
        Try
            Dim tb As New Editor
            SetDefaultSettings(tb.SplitEditorCode)
            If fileName IsNot Nothing Then
                tb.TabText = Path.GetFileName(fileName)
            Else
                tb.TabText = "[new]"
            End If
            tb.Tag = fileName
            If fileName <> Nothing Then
                tb.SplitEditorCode.Text = My.Computer.FileSystem.ReadAllText(fileName, System.Text.Encoding.Default)
                tb.SplitEditorCode.Tag = fileName
            Else
                tb.SplitEditorCode.Text = My.Computer.FileSystem.ReadAllText(Application.StartupPath + "\gamemodes\new.pwn")
            End If
            tb.Show(MainForm.MainDockPanel)
            tb.Focus()

            MainForm.ReBuildAutoComplete.RunWorkerAsync()

            Return (tb)
        Catch ex As Exception
            If MessageBox.Show(ex.Message, "Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Hand) = DialogResult.Retry Then
                CreateTab(fileName)
            End If
        End Try
        Return 1
    End Function

    'Includes List Loading
    Dim PathsOfInc As New List(Of String) 'Used while loading the inc.
    Public Sub LoadIncs()
        IncludeListFrm.IncludeTreeView.Nodes.Clear()
        Dim di As New IO.DirectoryInfo(Application.StartupPath + "\include")
        Dim diar1 As IO.FileInfo() = di.GetFiles()
        Dim dra As IO.FileInfo

        For Each dra In diar1
            If dra.FullName.Contains(".inc") Then
                PathsOfInc.Add(dra.ToString)
            End If
        Next
        Dim Num As Integer = 0

        For Each FILE_NAME As String In PathsOfInc
            IncludeListFrm.IncludeTreeView.Nodes.Add(FILE_NAME)

            Dim objReader As New System.IO.StreamReader(Application.StartupPath + "/include/" + FILE_NAME)

            Do While objReader.Peek() <> -1
                Dim Line As String
                Line = objReader.ReadLine()
                Try
                    Dim tmp As String = Line
                    If Line.Contains("native ") And Line.Length > 8 Then
                        Line = Line.Replace("native ", "").Trim()
                        MainForm.SyntaxOfInc.Add(Line)
                        Line = Line.Remove(Line.IndexOf("("))
                        MainForm.Includes.Add(Line)
                        If Line.Contains(":") Then
                            Line = Line.Remove(0, Line.IndexOf(":") + 1)
                        End If
                        IncludeListFrm.IncludeTreeView.Nodes.Item(Num).Nodes.Add(Line)
                    End If
                Catch ex As Exception

                End Try
            Loop
            Num += 1
        Next
    End Sub

    'CreateProject Sub
    Public Sub CreateProject()
        MainForm.FolderBrowser.ShowNewFolderButton = True
        If MainForm.FolderBrowser.ShowDialog = Windows.Forms.DialogResult.OK Then
            If Not My.Computer.FileSystem.DirectoryExists(MainForm.FolderBrowser.SelectedPath) Then
                My.Computer.FileSystem.CreateDirectory(MainForm.FolderBrowser.SelectedPath)
            End If
            My.Computer.FileSystem.CreateDirectory(MainForm.FolderBrowser.SelectedPath + "/Scripts")
            My.Computer.FileSystem.WriteAllText(MainForm.FolderBrowser.SelectedPath + "/Main.pwn", "/*" + vbCrLf + "    This page has been generated by ExtremePAWN" + vbCrLf + "*/" + vbCrLf + vbCrLf + "//Includes" + vbCrLf + "#include <a_samp>" + vbCrLf + vbCrLf + "//Scripts", False)
            MainForm.CurrentProjectPath = MainForm.FolderBrowser.SelectedPath
            CreateTab(MainForm.FolderBrowser.SelectedPath + "/Main.pwn")
        End If
    End Sub

    Public Sub LoadProject()
        MainForm.FolderBrowser.ShowNewFolderButton = False
        If MainForm.FolderBrowser.ShowDialog = Windows.Forms.DialogResult.OK Then
            If My.Computer.FileSystem.FileExists(MainForm.FolderBrowser.SelectedPath + "/Main.pwn") And My.Computer.FileSystem.DirectoryExists(MainForm.FolderBrowser.SelectedPath + "/Scripts") Then
                MainForm.CurrentProjectPath = MainForm.FolderBrowser.SelectedPath
                For Each File As String In My.Computer.FileSystem.GetFiles(MainForm.CurrentProjectPath + "/Scripts")
                    Dim FileName As String = Path.GetFileNameWithoutExtension(File)
                    ProjectExplorerFrm.ProjectExplorer.Nodes(1).Nodes.Add(FileName)
                Next
                CreateTab(MainForm.CurrentProjectPath + "/Main.pwn")
            Else
                MsgBox("This folder isn't a vaild ExtremePAWN project folder.")
            End If
        End If
    End Sub

    Public Sub CreateFile(ByVal Name As String)
        If Not MainForm.CurrentProjectPath = Nothing Then
            My.Computer.FileSystem.CopyFile(Application.StartupPath + "/gamemodes/new.pwn", MainForm.CurrentProjectPath + "/Scripts/" + Name + ".pwn")
            ProjectExplorerFrm.ProjectExplorer.Nodes(1).Nodes.Add(Name)
            MainForm.CurrentTB.InsertText("#include " + Chr(34) + "Scripts/" + Name + ".pwn" + Chr(34))
        Else
            MsgBox("You don't have a project loaded to add a new file.")
        End If
    End Sub
End Class
