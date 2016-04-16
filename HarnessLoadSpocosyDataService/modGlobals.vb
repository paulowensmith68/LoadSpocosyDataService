Module modGlobals
    '-------------------------------------------------
    '-   OddsMatching database connection string                             -
    '-------------------------------------------------
    Public globalConnectionString = My.Settings.ConnectionString

    '-------------------------------------------------
    '-   Logging objects                             -
    '-------------------------------------------------
    Public gobjEvent As EventLogger = New EventLogger

    '-------------------------------------------------
    '-   Global                                      -
    '-------------------------------------------------
    Public gintMaximumExecThressholdMillisecs As Integer
    Public gintKillRuntimeThressholdMillisecs As Integer
    Public gblnProcessHasExited As Boolean

End Module
