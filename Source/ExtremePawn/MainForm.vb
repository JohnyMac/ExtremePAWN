﻿'Copyright (C) 2014  Ahmad45123

'This program is free software: you can redistribute it and/or modify
'it under the terms of the GNU General Public License as published by
'the Free Software Foundation, either version 3 of the License, or
'(at your option) any later version.

'This program is distributed in the hope that it will be useful,
'but WITHOUT ANY WARRANTY; without even the implied warranty of
'MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
'GNU General Public License for more details.

'You should have received a copy of the GNU General Public License
'along with this program.  If not, see <http://www.gnu.org/licenses/>.

Imports System.IO
Imports FastColoredTextBoxNS
Imports System.Text.RegularExpressions
Imports FarsiLibrary.Win
Imports System.Threading

Public Class MainForm
    'Styles :
    Dim BlueItalicStyle As Style = New TextStyle(Brushes.Blue, Nothing, FontStyle.Italic)
    Dim BlueStyle As Style = New TextStyle(Brushes.Blue, Nothing, FontStyle.Regular)
    Dim GreenStyle As Style = New TextStyle(Brushes.Green, Nothing, FontStyle.Italic)
    Dim BoldStyle As Style = New TextStyle(Brushes.Black, Nothing, FontStyle.Bold + FontStyle.Underline)
    Dim TextStyle As Style = New TextStyle(Brushes.Indigo, Nothing, FontStyle.Regular)
    Dim NumberStyle As Style = New TextStyle(Brushes.Fuchsia, Nothing, FontStyle.Regular)

    Public Property CurrentTB() As FastColoredTextBox 'Returns the current opened object of FastColoredTextBox
        Get
            Dim result As FastColoredTextBox
            If TabStrip.SelectedItem Is Nothing Then
                result = Nothing
            Else
                result = TryCast(TabStrip.SelectedItem.Controls(0), FastColoredTextBox)
            End If
            Return result
        End Get
        Set(ByVal value As FastColoredTextBox)
            TabStrip.SelectedItem = TryCast(value.Parent, FATabStripItem)
            value.Focus()
        End Set
    End Property

    Private Sub Form1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        'Load Settings
        Functions.CreateTab(Nothing)
        Functions.LoadIncs()
        Functions.LoadSettings()

        'Load all files in the args.
        For Each Arg As String In My.Application.CommandLineArgs
            If Arg IsNot Nothing Then
                Functions.CreateTab(Arg)
            End If
        Next
    End Sub

    Private Sub MainForm_KeyDown(ByVal sender As System.Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles MyBase.KeyDown
        If e.Control = True And e.KeyValue = Keys.S Then
            ToolStripButton4.PerformClick() 'Save

        End If
    End Sub

    Private Sub IdleMaker_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles IdleMaker.Tick
        IdleMaker.Stop()
        Status.Text = "Idle"

    End Sub

    Private Sub Form1_FormClosing(ByVal sender As System.Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles MyBase.FormClosing
        'Saving before closing.
        Dim list As List(Of FATabStripItem) = New List(Of FATabStripItem)()
        For Each tab As FATabStripItem In TabStrip.Items
            list.Add(tab)
        Next
        For Each tab As FATabStripItem In list
            Dim args As TabStripItemClosingEventArgs = New TabStripItemClosingEventArgs(tab)
            Me.FaTabStrip1_TabStripItemClosing(args)
            If args.Cancel Then
                e.Cancel = True
                Exit For
            End If
            TabStrip.RemoveTab(tab)
        Next
    End Sub

    Public Sub Code_TextD(ByVal sender As System.Object, ByVal e As FastColoredTextBoxNS.TextChangedEventArgs) 'Text delayed event. Handled on any Instance of FastColoredTextbox created by CreateTab.
        If CurrentTB IsNot Nothing Then
            ThreadPool.QueueUserWorkItem(Sub(o As Object)
                                             ReBuildObjectExplorer(CurrentTB.Text)
                                         End Sub)
        End If
    End Sub

    Public Sub Code_TextChanged(ByVal sender As System.Object, ByVal e As FastColoredTextBoxNS.TextChangedEventArgs) Handles SplitEditorCode.TextChanged
        Dim range As Range = TryCast(sender, FastColoredTextBox).VisibleRange
        range.ClearStyle(GreenStyle)
        range.SetStyle(GreenStyle, "(/\*.*?\*/)|(/\*.*)", RegexOptions.Singleline)
        range.SetStyle(GreenStyle, "(/\*.*?\*/)|(.*\*/)", RegexOptions.Singleline And RegexOptions.RightToLeft)

        'Start syntax highliting code : ((Currently some of them are commented as we use
        'the C# builtin highlighter temporary untill we create our own.))

        e.ChangedRange.ClearStyle({BlueItalicStyle, BoldStyle})
        e.ChangedRange.SetStyle(BlueItalicStyle, "#.*$", RegexOptions.Multiline)
        e.ChangedRange.SetStyle(BoldStyle, "\b(public|stock|enum)\s+(?<range>[\w_]+?)\b")
        'e.ChangedRange.SetStyle(GreenStyle, "//.*$", RegexOptions.Multiline)
        'e.ChangedRange.SetStyle(BlueStyle, "\b(public|stock|new|enum|return|if|else|for|break|continue)\b", RegexOptions.Multiline)
        'e.ChangedRange.SetStyle(TextStyle, Chr(34) + ".*" + Chr(34), RegexOptions.Multiline)
    End Sub

    Private Sub AutoSaver_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles AutoSaver.Tick
        ToolStripButton4.PerformClick()

    End Sub

    Private Sub TreeView1_DoubleClick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles IncludeTreeView.DoubleClick
        Dim Func As String = IncludeTreeView.SelectedNode.Text
        Func = Func.Replace("      ", "")
        Dim Index As Integer = Functions.SyntaxOfInc.FindString(Func, -1)
        If Not Index = -1 Then
            Dim Format As String = Functions.SyntaxOfInc.Items.Item(Index)
            CurrentTB.InsertText(Format)
        End If
    End Sub

    Private Sub TreeView1_AfterSelect(ByVal sender As System.Object, ByVal e As System.Windows.Forms.TreeViewEventArgs) Handles IncludeTreeView.AfterSelect
        Dim Func As String = IncludeTreeView.SelectedNode.Text
        Func = Func.Replace("      ", "")
        Dim Index As Integer = Functions.SyntaxOfInc.FindString(Func, -1)
        If Not Index = -1 Then
            Dim Format As String = Functions.SyntaxOfInc.Items.Item(Index)
            Status.Text = Format
        End If
    End Sub

    Private Sub HelpMenu_Selected(ByVal sender As System.Object, ByVal e As AutocompleteMenuNS.SelectedEventArgs) Handles HelpMenu.Selected
        Dim Func As String = e.Item.Text
        Func = Func.Replace("      ", "")
        Dim Index As Integer = Functions.SyntaxOfInc.FindString(Func, -1)
        If Not Index = -1 Then
            Dim Format As String = Functions.SyntaxOfInc.Items.Item(Index)
            Status.Text = Format
        End If
    End Sub

    Private Sub ToolStripButton13_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ToolStripButton13.Click
        ColorChoice.Show()

    End Sub

    Private Sub ToolStripButton12_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ToolStripButton12.Click
        CurrentTB.Paste()
    End Sub

    Private Sub ToolStripButton11_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ToolStripButton11.Click
        CurrentTB.Cut()
    End Sub

    Private Sub ToolStripButton10_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ToolStripButton10.Click
        CurrentTB.Copy()
    End Sub

    Private Sub ToolStripButton9_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ToolStripButton9.Click
        CurrentTB.Redo()
    End Sub

    Private Sub ToolStripButton8_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ToolStripButton8.Click
        CurrentTB.Undo()
    End Sub

    Private Sub ToolStripButton5_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ToolStripButton5.Click
        CurrentTB.ShowFindDialog()

    End Sub

    Private Sub ToolStripButton6_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ToolStripButton6.Click
        CurrentTB.ShowReplaceDialog()

    End Sub

    Private Sub ToolStripButton2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ToolStripButton2.Click
        Functions.CreateTab(Nothing)
    End Sub

    Private Sub ToolStripButton4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ToolStripButton4.Click
        If Me.TabStrip.SelectedItem IsNot Nothing Then
            Functions.Save(Me.TabStrip.SelectedItem)
        End If
        Status.Text = "Saved"
        IdleMaker.Start()
    End Sub

    Private Sub ToolStripButton3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ToolStripButton3.Click
        If OpenFileDialog.ShowDialog = Windows.Forms.DialogResult.OK Then
            Functions.CreateTab(OpenFileDialog.FileName, True)
        End If
        IdleMaker.Start()
    End Sub

    Private Sub ToolStripButton7_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ToolStripButton7.Click
        If CurrentTB IsNot Nothing Then
            Functions.Compile(CurrentTB)
        End If
    End Sub

    Private Sub ToolsBarToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        Dim OldSize As Size
        If ToolStrip.Visible = True Then
            ToolStrip.Visible = False

            OldSize = CurrentTB.Size
            OldSize.Height += 28
            CurrentTB.Size = OldSize
            CurrentTB.Top -= 30

            OldSize = IncludeTreeView.Size
            OldSize.Height += 28
            IncludeTreeView.Size = OldSize
            IncludeTreeView.Top -= 30
        Else
            ToolStrip.Visible = True

            OldSize = CurrentTB.Size
            OldSize.Height -= 28
            CurrentTB.Size = OldSize
            CurrentTB.Top += 30

            OldSize = IncludeTreeView.Size
            OldSize.Height -= 28
            IncludeTreeView.Size = OldSize
            IncludeTreeView.Top += 30
        End If
    End Sub

    Private Sub NewToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles NewToolStripMenuItem.Click
        ToolStripButton2.PerformClick()
    End Sub

    Private Sub OpenToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles OpenToolStripMenuItem.Click
        If OpenFileDialog.ShowDialog = Windows.Forms.DialogResult.OK Then
            Functions.CreateTab(OpenFileDialog.FileName)
        End If
        IdleMaker.Start()
    End Sub

    Private Sub SaveToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles SaveToolStripMenuItem.Click
        ToolStripButton4.PerformClick()
    End Sub

    Private Sub ExitToolStripMenuItem1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ExitToolStripMenuItem1.Click
        Setting.ShowDialog()
    End Sub

    Private Sub ExitToolStripMenuItem2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ExitToolStripMenuItem2.Click
        Me.Close()
    End Sub

    Private Sub AboutToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles AboutToolStripMenuItem.Click
        MsgBox("Developed By Ahmad45123 AKA Johny Mac" + vbCrLf + vbCrLf + "For any suggestions or bug reports, Dont hesitate in emailing me at ahmad.gasser@gamil.com" + vbCrLf + vbCrLf + "Thanks for using this editor.")
    End Sub

    Private Sub FontToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles FontToolStripMenuItem.Click
        If CodeFont.ShowDialog = DialogResult.OK Then
            CurrentTB.Font = CodeFont.Font
            SplitEditorCode.Font = CodeFont.Font

        End If
    End Sub

    Private Sub UndoToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles UndoToolStripMenuItem.Click
        CurrentTB.Undo()

    End Sub

    Private Sub RedoToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RedoToolStripMenuItem.Click
        CurrentTB.Redo()
    End Sub

    Private Sub CutToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CutToolStripMenuItem.Click
        CurrentTB.Cut()
    End Sub

    Private Sub CopyToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CopyToolStripMenuItem.Click
        CurrentTB.Copy()
    End Sub

    Private Sub PasteToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles PasteToolStripMenuItem.Click
        CurrentTB.Paste()
    End Sub

    Private Sub FindToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles FindToolStripMenuItem.Click
        CurrentTB.ShowFindDialog()
    End Sub

    Private Sub ReplaceToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ReplaceToolStripMenuItem.Click
        CurrentTB.ShowReplaceDialog()

    End Sub

    Private Sub GoToToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles GoToToolStripMenuItem.Click
        CurrentTB.ShowGoToDialog()

    End Sub

    Private Sub SelectAllToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles SelectAllToolStripMenuItem.Click
        CurrentTB.SelectAll()

    End Sub

    Private Sub ToolStripButton14_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ToolStripButton14.Click
        CurrentTB.IncreaseIndent()

    End Sub

    Private Sub ToolStripButton15_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ToolStripButton15.Click
        CurrentTB.DecreaseIndent()

    End Sub

    Private Sub ToolStripButton16_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ToolStripButton16.Click
        CurrentTB.DoAutoIndent()

    End Sub

    Private Sub CopyToolStripMenuItem1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CopyToolStripMenuItem1.Click
        CurrentTB.Copy()

    End Sub

    Private Sub CutToolStripMenuItem1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CutToolStripMenuItem1.Click
        CurrentTB.Cut()

    End Sub

    Private Sub PasteToolStripMenuItem1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles PasteToolStripMenuItem1.Click
        CurrentTB.Paste()

    End Sub

    'Function ReBuildObjectExplorer to rebuild the object explorer contents.
    Public Sub ReBuildObjectExplorer(ByVal text As String)
        Try
            text = text.Replace("#", "")
            Dim list As List(Of ObjectExplorerClass.ExplorerItem) = New List(Of ObjectExplorerClass.ExplorerItem)()
            Dim lastClassIndex As Integer = -1
            Dim regex As Regex = New Regex("^\s*(public|stock|define)[^\n]+(\n?\s*{|;)?", RegexOptions.Multiline)
            For Each r As Match In regex.Matches(text)
                Try
                    Dim s As String = r.Value
                    Dim i As Integer = s.IndexOfAny(New Char() {"=", "{", ";"})
                    If i >= 0 Then
                        s = s.Substring(0, i)
                    End If
                    s = s.Trim()
                    Dim item As ObjectExplorerClass.ExplorerItem = New ObjectExplorerClass.ExplorerItem() With {.title = s, .position = r.Index}
                    If regex.IsMatch(item.title, "\b(public|stock)\b") Then
                        item.title = item.title.Substring(item.title.IndexOf(" ")).Trim()
                        item.type = ObjectExplorerClass.ExplorerItemType.[Class]
                        list.Sort(lastClassIndex + 1, list.Count - (lastClassIndex + 1), New ObjectExplorerClass.ExplorerItemComparer())
                        lastClassIndex = list.Count
                    ElseIf regex.IsMatch(item.title, "\b(define)\b") Then
                        item.title = item.title.Substring(item.title.IndexOf(" ")).Trim()
                        Dim tst As String() = item.title.Split(" ")
                        item.title = tst(0)
                        item.type = ObjectExplorerClass.ExplorerItemType.Property
                        list.Sort(lastClassIndex + 1, list.Count - (lastClassIndex + 1), New ObjectExplorerClass.ExplorerItemComparer())
                        lastClassIndex = list.Count
                    End If
                    list.Add(item)
                Catch ex_2BF As Exception
                    Console.WriteLine(ex_2BF)
                End Try
            Next
            list.Sort(lastClassIndex + 1, list.Count - (lastClassIndex + 1), New ObjectExplorerClass.ExplorerItemComparer())
            MyBase.BeginInvoke(Sub()
                                   ObjectExplorerClass.explorerList = list
                                   ObjectExplorer.RowCount = ObjectExplorerClass.explorerList.Count
                                   ObjectExplorer.Invalidate()
                               End Sub)
        Catch ex_332 As Exception
            Console.WriteLine(ex_332)
        End Try
    End Sub

    Private Sub ObjectExplorer_CellValueNeeded(ByVal sender As Object, ByVal e As DataGridViewCellValueEventArgs) Handles ObjectExplorer.CellValueNeeded
        Try
            Dim item As ObjectExplorerClass.ExplorerItem = ObjectExplorerClass.explorerList(e.RowIndex)
            If e.ColumnIndex = 1 Then
                e.Value = item.title
            Else
                Select Case item.type
                    Case ObjectExplorerClass.ExplorerItemType.[Class]
                        e.Value = My.Resources.class_libraries
                    Case ObjectExplorerClass.ExplorerItemType.[Property]
                        e.Value = My.Resources._property
                End Select
            End If
        Catch ex_8D As Exception
        End Try
    End Sub

    Private Sub ObjectExplorer_CellMouseDoubleClick(ByVal sender As Object, ByVal e As DataGridViewCellMouseEventArgs) Handles ObjectExplorer.CellMouseDoubleClick
        If Me.CurrentTB IsNot Nothing Then
            Dim item As ObjectExplorerClass.ExplorerItem = ObjectExplorerClass.explorerList(e.RowIndex)
            Me.CurrentTB.GoEnd()
            Me.CurrentTB.SelectionStart = item.position
            Me.CurrentTB.DoSelectionVisible()
            Me.CurrentTB.Focus()
        End If
    End Sub

    Private Sub FaTabStrip1_TabStripItemClosing(ByVal e As FarsiLibrary.Win.TabStripItemClosingEventArgs) Handles TabStrip.TabStripItemClosing
        If CurrentTB IsNot Nothing Then
            CurrentTB.CloseBindingFile()

        End If
        If TryCast(e.Item.Controls(0), FastColoredTextBox).IsChanged Then
            Dim dialogResult As DialogResult = MessageBox.Show("Do you want save " + e.Item.Title + " ?", "Save", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Asterisk)
            If dialogResult <> dialogResult.Cancel Then
                If dialogResult = dialogResult.Yes Then
                    If Not Functions.Save(e.Item) Then
                        e.Cancel = True
                    End If
                End If
            Else
                e.Cancel = True
            End If
        End If
    End Sub

    Private Sub ToolStripButton17_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ToolStripButton17.Click
        If SplitEditorCode.Visible = False Then
            SplitEditorCode.Visible = True
            Dim Size As Size = TabStrip.Size
            Size.Height = Size.Height - 250
            TabStrip.Size = Size
            SplitEditorCode.SourceTextBox = CurrentTB
        Else
            SplitEditorCode.Visible = False
            Dim Size As Size = TabStrip.Size
            Size.Height = Size.Height + 250
            TabStrip.Size = Size
        End If
    End Sub

    Private Sub FaTabStrip1_TabStripItemSelectionChanged(ByVal e As FarsiLibrary.Win.TabStripItemChangedEventArgs) Handles TabStrip.TabStripItemSelectionChanged
        If SplitEditorCode.Visible = True Then
            SplitEditorCode.SourceTextBox = CurrentTB
        End If

        If Me.CurrentTB IsNot Nothing Then
            Me.CurrentTB.Focus()
            Dim text As String = Me.CurrentTB.Text
            ThreadPool.QueueUserWorkItem(Sub(o As Object)
                                             ReBuildObjectExplorer(text)
                                         End Sub)
            DocumentMap.Target = CurrentTB
        End If
    End Sub

    Private Sub FindToolStripMenuItem1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles FindToolStripMenuItem1.Click
        CurrentTB.ShowFindDialog()

    End Sub

    Private Sub ReplaceToolStripMenuItem1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ReplaceToolStripMenuItem1.Click
        CurrentTB.ShowReplaceDialog()

    End Sub

    Private Sub GotoToolStripMenuItem1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles GotoToolStripMenuItem1.Click
        CurrentTB.ShowGoToDialog()

    End Sub

    Private Sub GotoBookmarkToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles GotoBookmarkToolStripMenuItem.Click
        Functions.NavigateForward()
    End Sub

    Private Sub AddBookmarkToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles AddBookmarkToolStripMenuItem.Click
        If Me.CurrentTB IsNot Nothing Then
            Dim id As Integer = Me.CurrentTB(Me.CurrentTB.Selection.Start.iLine).UniqueId
            CurrentTB.Bookmarks.Add(id)
        End If
    End Sub

    Private Sub RemoveBookmarkToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RemoveBookmarkToolStripMenuItem.Click
        If Me.CurrentTB IsNot Nothing Then
            Dim id As Integer = Me.CurrentTB(Me.CurrentTB.Selection.Start.iLine).UniqueId
            CurrentTB.Bookmarks.Remove(id)
        End If
    End Sub

    Private Sub ToolStripMenuItem7_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ToolStripMenuItem7.Click
        Functions.NavigateBackward()

    End Sub

    Private Sub ToolStripMenuItem8_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ToolStripMenuItem8.Click
        If OpenFileDialog.ShowDialog = Windows.Forms.DialogResult.OK Then
            Functions.CreateTab(OpenFileDialog.FileName, True)
        End If
        IdleMaker.Start()
    End Sub

    Private Sub CommentToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CommentToolStripMenuItem.Click
        If CurrentTB IsNot Nothing Then
            CurrentTB.InsertLinePrefix("//")
        End If
    End Sub

    Private Sub UnCommentToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles UnCommentToolStripMenuItem.Click
        If CurrentTB IsNot Nothing Then
            CurrentTB.RemoveLinePrefix("//")
        End If
    End Sub
End Class
