Imports System.Configuration
Imports System.Data.SqlClient

Public Class Connection
    Private con As SqlConnection

    Public Function LocalConnection() As String
        Return ConfigurationManager.ConnectionStrings("CutOffService.My.MySettings.LeaveFilingConnectionString").ConnectionString
    End Function

    Public Function JeonsoftConnection() As String
        Return ConfigurationManager.ConnectionStrings("CutOffService.My.MySettings.NBCTECHDBConnectionString").ConnectionString
    End Function

End Class