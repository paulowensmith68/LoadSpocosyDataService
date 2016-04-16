Public Class frmMain
    Private Sub btnProcess_Click(sender As Object, e As EventArgs) Handles btnProcess.Click

        ' Insert event's and other reference data
        Dim SpocosyDatabaseClass1 As New SpocosyDatabaseClass()
        SpocosyDatabaseClass1.InsertEventsAndOtherData()
        SpocosyDatabaseClass1 = Nothing

        ' Insert outcome
        Dim SpocosyDatabaseClass2 As New SpocosyDatabaseClass()
        SpocosyDatabaseClass2.InsertOutcomes()
        SpocosyDatabaseClass2 = Nothing

        ' Insert bettingoffer
        Dim SpocosyDatabaseClass3 As New SpocosyDatabaseClass()
        SpocosyDatabaseClass3.InsertBettingOffers()
        SpocosyDatabaseClass3 = Nothing

    End Sub
End Class
