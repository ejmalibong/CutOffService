Imports System.IO

Public Class WriteLogs

    Public Shared Sub ServiceLog(ByVal message As String)
        Dim strWriter As IO.StreamWriter = Nothing

        Try
            strWriter = New IO.StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\ServiceLogs.txt", True)
            strWriter.Write(message)
            strWriter.Flush()
            strWriter.Close()
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine(ex.Message)
        End Try
    End Sub

    Public Shared Sub ErrorLog(ByVal message As String)
        Dim strWriter As IO.StreamWriter = Nothing

        Try
            strWriter = New IO.StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\ErrorLogs.txt", True)
            strWriter.Write(message)
            strWriter.Flush()
            strWriter.Close()
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine(ex.Message)
        End Try
    End Sub

End Class
