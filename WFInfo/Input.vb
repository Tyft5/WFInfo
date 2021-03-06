﻿Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Net
Imports System.IO
Public Class Input
    Declare Function SetForegroundWindow Lib "user32.dll" (ByVal hwnd As Integer) As Integer
    Private InitialStyle As Integer
    Dim PercentVisible As Decimal
    Dim busy As Boolean = False
    Dim screenWidth As Integer = Screen.PrimaryScreen.Bounds.Width
    Dim screenHeight As Integer = Screen.PrimaryScreen.Bounds.Height
    Dim GetWarframe As Boolean = False 'If true then warframe was open when input was opened and should be switch to after
    Dim WFhWnd As String = ""
    Private Sub Input_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        UpdateColors(Me)
        Me.Location = New Point(screenWidth * 3, screenHeight * 3)
    End Sub

    '_________________________________________________________________________
    'Style stuff
    '_________________________________________________________________________

    Public Enum GWL As Integer
        ExStyle = -20
    End Enum

    Public Enum WS_EX As Integer
        Transparent = &H20
        Layered = &H80000
    End Enum

    Public Enum LWA As Integer
        ColorKey = &H1
        Alpha = &H2
    End Enum


    '_________________________________________________________________________
    'Used to target warframe and switch between input and warframe control
    '_________________________________________________________________________
    <DllImport("user32.dll", EntryPoint:="GetWindowLong")>
    Public Shared Function GetWindowLong(
        ByVal hWnd As IntPtr,
        ByVal nIndex As GWL
            ) As Integer
    End Function

    <DllImport("user32.dll", EntryPoint:="SetWindowLong")>
    Public Shared Function SetWindowLong(
        ByVal hWnd As IntPtr,
        ByVal nIndex As GWL,
        ByVal dwNewLong As WS_EX
            ) As Integer
    End Function

    Private Declare Function GetForegroundWindow Lib "user32" () As Long

    <DllImport("user32.dll", CharSet:=CharSet.Auto, SetLastError:=True)>
    Private Shared Function GetWindowText(hWnd As IntPtr, text As StringBuilder, count As Integer) As Integer
    End Function

    <DllImport("user32.dll", CharSet:=CharSet.Auto, SetLastError:=True)>
    Private Shared Function GetWindowTextLength(hWnd As IntPtr) As Integer
    End Function

    <DllImport("user32.dll", EntryPoint:="GetWindowRect")>
    Private Shared Function GetWindowRect(ByVal hWnd As IntPtr, ByRef lpRect As Rectangle) As <MarshalAs(UnmanagedType.Bool)> Boolean
    End Function

    <DllImport("user32.dll",
      EntryPoint:="SetLayeredWindowAttributes")>
    Public Shared Function SetLayeredWindowAttributes(
        ByVal hWnd As IntPtr,
        ByVal crKey As Integer,
        ByVal alpha As Byte,
        ByVal dwFlags As LWA
            ) As Boolean
    End Function

    Private Declare Sub mouse_event Lib "user32" (ByVal dwFlags As Integer,
      ByVal dx As Integer, ByVal dy As Integer, ByVal cButtons As Integer,
      ByVal dwExtraInfo As Integer)

    Private Sub Input_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        '_________________________________________________________________________
        'Initial setup
        '_________________________________________________________________________
        Me.Refresh()
        Me.Location = New Point(screenWidth * 0.7, screenHeight * 0.94)
        Me.Width = screenWidth * 0.3
        Me.Height = screenHeight * 0.06
        btnAccept.Location = New Point(Me.Width * 2, Me.Height * 2)
        Dim size As Integer = screenHeight * 0.011
        Dim xLoc As Integer = Me.Height * 0.9
        Dim yLoc As Integer = Me.Width * 0.33
        tbCommand.Location = New Point(yLoc, xLoc)
        tbCommand.Width = Me.Width * 0.33
        tbCommand.Font = New Font(tbCommand.Font.FontFamily, size, FontStyle.Bold)
        tbCommand.Visible = True
        InitialStyle = GetWindowLong(Me.Handle, GWL.ExStyle)
        PercentVisible = 0.9
        SetWindowLong(Me.Handle, GWL.ExStyle, InitialStyle Or WS_EX.Layered Or WS_EX.Transparent)
        SetLayeredWindowAttributes(Me.Handle, 0, 255 * PercentVisible, LWA.Alpha)
        Me.TransparencyKey = Color.LightBlue
        Me.BackColor = Color.LightBlue
        Me.TopMost = True
        '_________________________________________________________________________
        'Gives focus to tbCommand (input closes if the tb loses focus)
        tbCommand.Focus()
    End Sub

    Private Sub btnAccept_Click(sender As Object, e As EventArgs) Handles btnAccept.Click
        busy = True
        Try
            Dim command As String = tbCommand.Text.ToLower()
            Me.Hide()
            Tray.Clear()
            Try
                '_________________________________________________________________________
                'Gives control back to warframe
                '_________________________________________________________________________
                If GetWarframe Then
                    While Not ActiveWindowName() = "WARFRAME"
                        AppActivate("WARFRAME")
                    End While

                    'Get WAREFRAME screen position
                    Dim wr As Rectangle
                    GetWindowRect(WFhWnd, wr)

                    'Give WARFRAME mouse control
                    Dim prevPos As Point = Cursor.Position
                    Cursor.Position = New Point(wr.Left, wr.Top)
                    mouse_event(&H2, 0, 0, 0, 0)
                    mouse_event(&H4, 0, 0, 0, 0)
                    Cursor.Position = prevPos
                End If
            Catch ex As Exception
            End Try


            '_________________________________________________________________________
            'just a little easter egg
            '_________________________________________________________________________
            If command.Contains("duck") Or command.Contains("quack") Then
                Tray.quack()
                Me.Close()
                tbCommand.Text = ""
                Exit Try
            End If


            '_________________________________________________________________________
            'Enables the WIP dev only (kinda) cheap listing alert
            If command.Contains("enable alert") Then
                Me.Close()
                tbCommand.Text = ""
                Main.devCheck = True
                Exit Try
            End If


            '_________________________________________________________________________
            'Process the string and determine what the user wants to do
            '_________________________________________________________________________
            '
            'Get pricing for a mod
            '_________________________________________________________________________
            If command.Split(" ")(0) = "mod" Or command.Split(" ")(0) = "m" Then
                Dim modStr As String = StrConv(nth(command, 0), VbStrConv.ProperCase)
                qItems.Add(vbNewLine & modStr & vbNewLine & "    Plat: " & GetPlat(modStr, True, True) & vbNewLine)
                Tray.Display()
                Me.Close()
                tbCommand.Text = ""

                '_________________________________________________________________________
                'Gets location of resources
                '_________________________________________________________________________
            ElseIf command.Split(" ")(0) = "where" Or command.Split(" ")(0) = "w" Then
                If command.Split(" ")(0) = "w" Then
                    tbCommand.Text = "where" & command.Remove(0, 1)
                End If
                Dim cmdRes As String = command.Replace("where ", "")
                Dim ResInd As Integer = -1
                Dim Found As Boolean = False
                For Each str As String In My.Settings.Resources.Split(vbNewLine)
                    If Not Found Then
                        ResInd += 1
                        For Each substr As String In str.Split(",")(0).Split(" ")
                            If LevDist(substr, cmdRes) <= 1 Then
                                Found = True
                            End If
                        Next
                    End If
                Next
                Dim lowestLev As Integer = 9999
                If Not Found Then
                    ResInd = 0
                    Dim levCount As Integer = -1
                    For Each str As String In My.Settings.Resources.Split(vbNewLine)
                        levCount += 1
                        Dim dist As Integer = LevDist(str.Split(",")(0), cmdRes)
                        If dist < lowestLev Then
                            ResInd = levCount
                            lowestLev = dist
                        End If
                    Next
                End If
                qItems.Add(My.Settings.Resources.Split(vbNewLine)(ResInd).Split(",")(0) & vbNewLine & My.Settings.Resources.Split(vbNewLine)(ResInd).Split(",")(1) & vbNewLine)
                Tray.Display()
                Me.Close()

                '_________________________________________________________________________
                'Checks users mastery list (WFInfo only) to see if a weapon/prime is mastered
                '_________________________________________________________________________
            ElseIf command.Split(" ")(0) = "e" Then
                Dim found As Boolean = False
                Dim foundItem As String = ""
                Dim checkItem As String = nth(command, 0)
                If checkItem.Substring(checkItem.LastIndexOf(" ") + 1) = "p" Or checkItem.Substring(checkItem.LastIndexOf(" ") + 1) = "prime" Then
                    checkItem = checkItem.Substring(0, checkItem.LastIndexOf(" ")) & " prime"
                End If

                For Each item As String In Equipment.Split(",")
                    If item = checkItem Then
                        found = True
                        foundItem = StrConv(item, VbStrConv.ProperCase)
                    End If
                Next
                If found Then
                    qItems.Add(vbNewLine & foundItem & vbNewLine & "Already Leveled")
                Else
                    qItems.Add(vbNewLine & "Not Leveled")
                End If
                Tray.Display()
                Me.Close()
                tbCommand.Text = ""

                '_________________________________________________________________________
                'Adds a weapon/prime to the locally stored mastery list
                '_________________________________________________________________________
            ElseIf command.Split(" ")(0) = "ea" Then
                Dim item As String = nth(command, 0)
                If item.Substring(item.LastIndexOf(" ") + 1) = "p" Then
                    item = item.Substring(0, item.LastIndexOf(" ")) & " prime"
                End If
                Equipment = Equipment & item & ","
                Me.Close()
                tbCommand.Text = ""

                '_________________________________________________________________________
                'Removes weapon/prime from the locally stored mastery list
                '_________________________________________________________________________
            ElseIf command.Split(" ")(0) = "er" Then
                Dim found As Boolean = False
                Dim checkItem As String = nth(command, 0)
                If checkItem.Substring(checkItem.LastIndexOf(" ") + 1) = "p" Or checkItem.Substring(checkItem.LastIndexOf(" ") + 1) = "prime" Then
                    checkItem = checkItem.Substring(0, checkItem.LastIndexOf(" ")) & " prime"
                End If
                For Each item As String In Equipment.Split(",")
                    If item = checkItem Then
                        Equipment = Equipment.Replace(item & ",", "")
                        found = True
                    End If
                Next
                If Not found Then
                    qItems.Add(vbNewLine & "Item Not Found")
                    Tray.Display()
                End If
                Me.Close()
                tbCommand.Text = ""

                '_________________________________________________________________________
                'Copies the locally stored mastery list to clipboard
                '_________________________________________________________________________
            ElseIf command.Split(" ")(0) = "el" Then
                Dim clipString As String = ""
                For Each item As String In Equipment.Split(",")
                    clipString &= item & vbNewLine
                Next
                Clipboard.SetText(clipString)
                qItems.Add(vbNewLine & "Equipment Coppied" & vbNewLine & "To Clipboard")
                Tray.Display()
                Me.Close()
                tbCommand.Text = ""

                '_________________________________________________________________________
                'Completely clears the mastery list
                '_________________________________________________________________________
            ElseIf command.Split(" ")(0) = "ec" Then
                Equipment = ""
                qItems.Add(vbNewLine & "Equipment Cleared")
                Tray.Display()
                Me.Close()
                tbCommand.Text = ""


                '_________________________________________________________________________
                'Posts a part, set, or mod to Warframe.Market for sale
                '_________________________________________________________________________
            ElseIf command.Split(" ")(0) = "sell" Then
                Try
                    If cookie = "" Then
                        Exit Try
                    End If

                    Dim getMod As Boolean = False
                    If command.Split(" ")(1) = "m" Or command.Split(" ")(1) = "mod" Then
                        getMod = True
                    End If
                    command = command.Replace(" p ", " prime ")
                    command = command.Replace("bp", "blueprint")
                    command = command.Replace(" m ", " ").Replace(" mod ", " ")
                    command = command.Replace("sell ", "")
                    Dim listPrice As String = command.Split(" ")(command.Split(" ").Count - 1)
                    command = command.Replace(" " + listPrice, "")
                    tbCommand.Text = command

                    Dim found As Boolean = False
                    Dim guess As String = ""
                    For i = 0 To Names.Count - 1
                        If Names(i).ToLower.Contains(command) Then
                            found = True
                            guess = Names(i)
                        End If
                    Next
                    If Not found Then
                        guess = Names(check(command))
                    End If

                    Dim ID As String = " "
                    Dim DisplayName As String = " "
                    Dim JSON As String = ""
                    If getMod Then
                        ID = GetPlat(command, getID:=True)
                        DisplayName = vbNewLine + StrConv(command, VbStrConv.ProperCase)
                        JSON = "{""item_id"":""" & ID & """,""order_type"":""sell"",""platinum"":""" & listPrice & """,""quantity"":""1"",""mod_rank"":""0""}"
                    Else
                        ID = GetPlat(Main.KClean(guess), getID:=True)
                        DisplayName = Main.KClean(guess)
                        JSON = "{""item_id"":""" & ID & """,""order_type"":""sell"",""platinum"":""" & listPrice & """,""quantity"":""1""}"
                    End If

                    Dim jsonDataBytes = Encoding.UTF8.GetBytes(JSON)
                    Dim uri As New Uri("https://api.warframe.market/v1/profile/orders")
                    Dim req As WebRequest = WebRequest.Create(uri)
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
                    req.ContentType = "application/json"
                    req.Method = "POST"
                    req.ContentLength = jsonDataBytes.Length
                    req.Headers.Add("Cookie", Glob.cookie)
                    req.Headers.Add("x-csrftoken", Glob.xcsrf)
                    req.Headers.Add("platform", "pc")
                    req.Headers.Add("language", "en")

                    Dim stream = req.GetRequestStream()
                    stream.Write(jsonDataBytes, 0, jsonDataBytes.Length)
                    stream.Close()

                    Dim response = req.GetResponse().GetResponseStream()

                    Dim reader As New StreamReader(response)
                    Dim res = reader.ReadToEnd()
                    reader.Close()
                    response.Close()

                    qItems.Add(DisplayName & vbNewLine & vbNewLine & "Listed For: " & vbNewLine & listPrice & " Platinum" & vbNewLine)

                    Tray.Display()
                Catch ex As Exception
                    Main.addLog(ex.ToString)
                    Main.lbStatus.ForeColor = Color.Yellow
                    qItems.Add(vbNewLine & "ERROR" & vbNewLine & vbNewLine & "Check logs for details." & vbNewLine & vbNewLine & vbNewLine & "Format:" & vbNewLine & """sell rhino p chass 1234""")
                    Tray.Display()
                End Try
                Me.Close()
                tbCommand.Text = ""


                '_________________________________________________________________________
                'Retrieves the platinum price and seller for a single part or set
                '_________________________________________________________________________
            Else
                command = command.Replace(" p ", " prime ")
                command = command.Replace("bp", "blueprint")
                tbCommand.Text = command
                If command = "clear" Then
                    Tray.Clear()
                    tbCommand.Text = ""
                Else
                    Dim found As Boolean = False
                    Dim guess As String = ""
                    If command.Contains("set") Then
                        guess = checkSet(tbCommand.Text)
                    Else
                        For i = 0 To Names.Count - 1
                            If Names(i).ToLower.Contains(command) Then
                                found = True
                                guess = Names(i)
                            End If
                        Next
                        If Not found Then
                            guess = Names(check(tbCommand.Text))
                        End If
                    End If
                    If Not guess = "Forma Blueprint" Then
                        Dim plat As String = GetPlat(Main.KClean(guess), getUser:=True)
                        Dim duck As String
                        If command.Contains("set") Then
                            duck = ""
                        Else
                            duck = "    Ducks: " & Ducks(check(guess)) & vbNewLine
                        End If
                        If Main.KClean(guess).Length > 27 Then
                            qItems.Add(Main.KClean(guess).Substring(0, 27) & "..." & vbNewLine & duck & "    Plat: " & plat & vbNewLine)
                        Else
                            qItems.Add(Main.KClean(guess) & vbNewLine & duck & "    Plat: " & plat & vbNewLine)
                        End If
                    Else
                        qItems.Add(vbNewLine & guess & vbNewLine)
                    End If
                    Tray.Display()
                End If
                Me.Close()
                tbCommand.Text = ""
            End If
        Catch ex As Exception
            Me.Close()
            tbCommand.Text = ""
        End Try
    End Sub
    Public Function nth(s As String, n As Integer) As String
        For i = 0 To n
            s = s.Substring(s.IndexOf(" ") + 1)
        Next
        Return s
    End Function
    Public Sub Display()

        'Checks for WF as active, sets boolean and hWnd
        If ActiveWindowName() = "WARFRAME" Then
            GetWarframe = True
            WFhWnd = GetForegroundWindow()
        End If

        Me.Refresh()
        Me.TransparencyKey = Color.LightBlue
        Me.BackColor = Color.LightBlue
        Me.TopMost = True
        Me.WindowState = FormWindowState.Maximized
        Me.Show()
        Me.Refresh()
        Me.Select()
    End Sub

    Private Sub tbCommand_LostFocus(sender As Object, e As EventArgs) Handles tbCommand.LostFocus
        '_________________________________________________________________________
        'Closes input if tbCommand loses focus
        '_________________________________________________________________________
        If busy Then
            Me.Hide()
        Else
            Me.Close()
        End If
    End Sub

    Private Sub tbCommand_KeyDown(sender As Object, e As KeyEventArgs) Handles tbCommand.KeyDown
        '_________________________________________________________________________
        'Escape hotkey to close input
        '_________________________________________________________________________
        If e.KeyCode = Keys.Escape Then
            e.Handled = True
            If GetWarframe Then
                While Not ActiveWindowName() = "WARFRAME"
                    AppActivate("WARFRAME")
                End While

                'Get WAREFRAME screen position
                Dim wr As Rectangle
                GetWindowRect(WFhWnd, wr)

                'Give WARFRAME mouse control
                Dim prevPos As Point = Cursor.Position
                Cursor.Position = New Point(wr.Left, wr.Top)
                mouse_event(&H2, 0, 0, 0, 0)
                mouse_event(&H4, 0, 0, 0, 0)
                Cursor.Position = prevPos
            End If
            Me.Close()
        End If
    End Sub

    Private Sub tActivate_Tick(sender As Object, e As EventArgs) Handles tActivate.Tick
        '_________________________________________________________________________
        'Quickly activates input as to not close instantly after opening it
        '_________________________________________________________________________
        If Me.Visible Then
            If Not GetForegroundWindow() = Me.Handle.ToString Then
                'SendKeys.Send(Keys.LMenu)
                Me.Activate()
            End If
            tActivate.Enabled = False
            tActivate.Stop()
        End If
    End Sub

    Private Function ActiveWindowName() As String
        Dim strTitle As String = String.Empty
        Dim handle As IntPtr = GetForegroundWindow()
        ' Obtain the length of the text   
        Dim intLength As Integer = GetWindowTextLength(handle) + 1
        Dim stringBuilder As New StringBuilder(intLength)
        If GetWindowText(handle, stringBuilder, intLength) > 0 Then
            strTitle = stringBuilder.ToString()
        End If
        Return strTitle
    End Function

End Class