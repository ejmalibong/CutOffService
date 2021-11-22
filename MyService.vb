Imports System.Data.SqlClient
Imports BlackCoffeeLibrary
Imports System.Net.Mail
Imports System.ComponentModel

Public Class MyService
    Private connection As New Connection
    Private dbLeaveFiling As New SqlDbMethod(connection.LocalConnection)
    Private serverDate As DateTime = dbLeaveFiling.GetServerDate
    Private tmr As System.Timers.Timer
    Dim currentDatetime As DateTime = Nothing
    Dim cutOffDate As DateTime = Nothing
    'check email if already sent
    Private Shared mailSent As Boolean = False
    'email sender
    Private systemEmailAddress As String = String.Empty
    Private systemEmailPassword As String = String.Empty

    Protected Overrides Sub OnStart(ByVal args() As String)
        ' Add code here to start your service. This method should set things
        ' in motion so your service can do its work.
        tmr = New Timers.Timer
        tmr.Interval = 1000
        AddHandler tmr.Elapsed, AddressOf tmrElapsedHandler
        tmr.Enabled = True
        WriteLogs.ServiceLog(serverDate.ToString("MM/dd/yyyy HH:mm:ss") & vbTab & "Service started successfully." & Environment.NewLine)

        GetEmailSettings(1)

        currentDatetime = GetCutOffDate(DateTime.Now.Date)
        cutOffDate = New DateTime(currentDatetime.Year, currentDatetime.Month, currentDatetime.Day, 0, 0, 0)
    End Sub

    Protected Overrides Sub OnStop()
        ' Add code here to perform any tear-down necessary to stop your service.
        Me.tmr.Enabled = False
        WriteLogs.ServiceLog(serverDate.ToString("MM/dd/yyyy HH:mm:ss") & vbTab & "Service stopped successfully." & Environment.NewLine)
    End Sub

    Private Sub tmrElapsedHandler(ByVal sender As Object, ByVal e As System.Timers.ElapsedEventArgs)
        'If DateTime.Now.ToString("HH:mm:ss").Equals("11:15:00") Then

        'End If

        If DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss").Equals(New DateTime(cutOffDate.Date.Year, cutOffDate.Date.Month, cutOffDate.Date.Day, 0, 0, 0).AddDays(1)) Then
            Dim _day As Integer = cutOffDate.Day
            Dim _prmDate(2) As SqlParameter
            Dim _affectedRows As Integer = 0

            If _day >= 6 AndAlso _day <= 20 Then
                _prmDate(0) = New SqlParameter("@DateCreatedFrom", SqlDbType.Date)
                _prmDate(0).Value = New DateTime(cutOffDate.Date.Year, cutOffDate.Date.AddMonths(-1).Month, 21)
                _prmDate(1) = New SqlParameter("@DateCreatedTo", SqlDbType.Date)
                _prmDate(1).Value = New DateTime(cutOffDate.Date.Year, cutOffDate.Date.Month, 5)

            ElseIf _day >= 21 Then
                _prmDate(0) = New SqlParameter("@DateCreatedFrom", SqlDbType.Date)
                _prmDate(0).Value = New DateTime(cutOffDate.Date.Year, cutOffDate.Date.Month, 6)
                _prmDate(1) = New SqlParameter("@DateCreatedTo", SqlDbType.Date)
                _prmDate(1).Value = New DateTime(cutOffDate.Date.Year, cutOffDate.Date.Month, 20)

            ElseIf _day <= 5 Then
                _prmDate(0) = New SqlParameter("@DateCreatedFrom", SqlDbType.Date)
                _prmDate(0).Value = New DateTime(cutOffDate.Date.Year, cutOffDate.Date.AddMonths(-1).Month, 6)
                _prmDate(1) = New SqlParameter("@DateCreatedTo", SqlDbType.Date)
                _prmDate(1).Value = New DateTime(cutOffDate.Date.Year, cutOffDate.Date.AddMonths(-1).Month, 20)

            End If

            _prmDate(2) = New SqlParameter("TotalCount", SqlDbType.Int)
            _prmDate(2).Direction = ParameterDirection.Output

            dbLeaveFiling.ExecuteNonQuery("UpdLeaveFilingCutOff", CommandType.StoredProcedure, _prmDate)
            _affectedRows = _prmDate(2).Value

            cutOffDate = GetCutOffDate(DateTime.Now.Date)

            Dim _client As New SmtpClient()
            Dim _message As New MailMessage()
            Dim _messageBody As String = String.Empty

            _message.From = New MailAddress(systemEmailAddress, "NBC Leave Notification")
            _message.Subject = "Leave Notification"
            _message.IsBodyHtml = True 'set email as html to attach hyperlink

            _messageBody += "<font size=""3"" face=""Segoe UI"" color=""black"">" & _
                            "Leave Application Cut-Off Service executed successfully. " & _affectedRows & " rows marked as late approved. <br> <br> "

            _message.To.Add("it1@nbc-p.com")
            _message.To.Add("catherine.delapena@nbc-p.com")

            _message.Subject = "Leave Notification"
            _message.IsBodyHtml = True
            _message.Body = _messageBody

            _client.Host = "smtp.gmail.com"
            _client.Port = 587
            _client.EnableSsl = True
            _client.UseDefaultCredentials = False
            _client.Credentials = New Net.NetworkCredential(systemEmailAddress, systemEmailPassword)

            Dim _userState As String = "userState"
            AddHandler _client.SendCompleted, AddressOf SendCompletedCallback

            _client.SendAsync(_message, _userState)

            WriteLogs.ServiceLog(cutOffDate.ToString("MM/dd/yyyy HH:mm:ss") & vbTab & "Leave Application Cut-Off Service executed successfully. " & Environment.NewLine)
            WriteLogs.ServiceLog(cutOffDate.ToString("MM/dd/yyyy HH:mm:ss") & vbTab & _affectedRows & " rows marked as late approved. Next cut-off date is on " & _
                                 cutOffDate.ToString("MMMM dd, yyyy") & "." & Environment.NewLine)
        End If
    End Sub

    Private Async Sub SendCompletedCallback(ByVal sender As Object, ByVal e As AsyncCompletedEventArgs)
        Try
            Dim token As String = CStr(e.UserState)

            If e.Cancelled Then
                WriteLogs.ServiceLog(serverDate.ToString("MM/dd/yyyy HH:mm:ss") & vbTab & "Sending canceled." & Environment.NewLine)
            End If

            If e.Error IsNot Nothing Then
                WriteLogs.ServiceLog(serverDate.ToString("MM/dd/yyyy HH:mm:ss") & vbTab & e.Error.ToString & Environment.NewLine)
            Else
                WriteLogs.ServiceLog(serverDate.ToString("MM/dd/yyyy HH:mm:ss") & vbTab & "Email sent." & Environment.NewLine)
            End If

            Await HideStatus()

            mailSent = True
        Catch ex As Exception
            WriteLogs.ErrorLog(serverDate.ToString("MM/dd/yyyy HH:mm:ss") & Environment.NewLine & ex.Message)
        End Try
    End Sub

    Private Async Function HideStatus() As Task(Of Boolean)
        Await Task.Delay(2000)
        Return True
    End Function

    'get email settings to be use for sending email notifications
    Private Sub GetEmailSettings(ByVal _settingsId As Integer)
        Try
            Dim _prmSettingsId(0) As SqlParameter
            _prmSettingsId(0) = New SqlParameter("@SettingsId", SqlDbType.Int)
            _prmSettingsId(0).Value = _settingsId

            Dim _reader As IDataReader = dbLeaveFiling.ExecuteReader("SELECT TRIM(EmailAddress) AS EmailAddress, TRIM(EmailPassword) AS EmailPassword " & _
                                                                     "FROM dbo.Settings WHERE SettingsId = @SettingsId", _
                                                                     CommandType.Text, _prmSettingsId)

            While _reader.Read
                systemEmailAddress = _reader.Item("EmailAddress").ToString.Trim
                systemEmailPassword = _reader.Item("EmailPassword").ToString.Trim
            End While
            _reader.Close()
        Catch ex As Exception
            WriteLogs.ErrorLog(serverDate.ToString("MM/dd/yyyy HH:mm:ss") & Environment.NewLine & ex.Message)
        End Try
    End Sub

    'get the cut-off date of current cut-off period
    Private Function GetCutOffDate(ByVal _currentDate As Date) As Date
        Dim _cutOff As Date = Nothing

        If _currentDate.Date.Day >= 6 AndAlso _currentDate.Date.Day <= 20 Then
            _cutOff = New DateTime(serverDate.Date.Year, serverDate.Date.Month, 23)
        ElseIf _currentDate.Date.Day >= 21 Then
            _cutOff = New DateTime(serverDate.Date.Year, serverDate.Date.Month, 8)
        ElseIf _currentDate.Date.Day <= 5 Then
            _cutOff = New DateTime(serverDate.Date.Year, serverDate.Date.Month, 8)
        End If

        Try
            While IsHoliday(_cutOff) = True Or IsWeekend(_cutOff) = True
                _cutOff = _cutOff.AddDays(1)
            End While
        Catch ex As Exception
            WriteLogs.ErrorLog(serverDate.ToString("MM/dd/yyyy HH:mm:ss") & Environment.NewLine & ex.Message)
        End Try

        Return _cutOff

    End Function

    'check if sunday
    Private Function IsWeekend(ByVal _date As Date) As Boolean
        If _date.DayOfWeek.Equals(DayOfWeek.Sunday) Then
            Return True
        Else
            Return False
        End If
    End Function

    'check if included to holiday list
    Private Function IsHoliday(ByVal _date As Date) As Boolean
        Dim _count As Integer

        Try
            Dim _paramHoliday(0) As SqlParameter
            _paramHoliday(0) = New SqlParameter("@HolidayDate", SqlDbType.Date)
            _paramHoliday(0).Value = _date.ToShortDateString
            _count = 0
            _count = dbLeaveFiling.ExecuteScalar("SELECT COUNT(HolidayId) FROM Holiday WHERE HolidayDate = @HolidayDate", CommandType.Text, _paramHoliday)
        Catch ex As Exception
            WriteLogs.ErrorLog(serverDate.ToString("MM/dd/yyyy HH:mm:ss") & Environment.NewLine & ex.Message)
        End Try

        If _count > 0 Then
            Return True
        Else
            Return False
        End If
    End Function

End Class
