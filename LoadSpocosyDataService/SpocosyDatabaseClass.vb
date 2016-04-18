Imports System.Xml
Imports System.IO
Imports System.Text
Imports MySql.Data
Imports MySql.Data.MySqlClient

Public Class SpocosyDatabaseClass

    ' Holds the connection string to the database used.
    Public connectionString As String = globalConnectionString

    ' Holds name of key to delete data from after inserts
    Public strDeleteKeyType As String = ""

    'Holds message received back from class
    Public returnMessage As String = ""

    'Vars used for output message
    Private insertCount As Integer = 0
    Private updateCount As Integer = 0

    'Vars used to control cursor
    Public intCursorCount As Integer = 0

    'List that hold info on which nodes and attributes to use.
    'These lists are populated in populateLists() function
    Private nodeList As String = ""
    Private attribList As New Dictionary(Of String, String)()

    'ID of XML file. This id is generated after push when xml file is saved.
    Public id As Integer = 0

    'ID of the 
    Public intXmlDataId As Integer = 0

    'String that holds the xml data. Only used in push. When parsed myXml is used.
    Public xmlData As String = ""

    'XmlDocument var that is used for parsing
    Public myXml As New XmlDocument()

    Public Sub InsertEventsAndOtherData()
        '-----------------------------------------------------------------------*
        ' Sub Routine parameters                                                *
        ' -----------------------                                               *
        '-----------------------------------------------------------------------*
        Dim cno As MySqlConnection = New MySqlConnection(connectionString)
        Dim drXmlLoad As MySqlDataReader
        Dim cmdXmlLoad As New MySqlCommand

        ' Populate lists
        populateLists()

        ' Sets type of deletion to perform later
        strDeleteKeyType = "xmlNodeId"

        ' Reset cursor counter
        intCursorCount = 0

        ' /----------------------------------------------------------------\
        ' | MySql Select                                                   |
        ' | Get all rows for nodeName from bookmaker_xml_nodes             |
        ' \----------------------------------------------------------------/
        cmdXmlLoad.CommandText = "SELECT id, xmlData FROM oddsmatching.bookmaker_xml_nodes where nodeName not in(""outcome"", ""bettingoffer"") " &
                                 "LIMIT @limit "
        cmdXmlLoad.Parameters.AddWithValue("limit", My.Settings.LimitEventRows)
        cmdXmlLoad.Connection = cno

        Try
            cno.Open()
            drXmlLoad = cmdXmlLoad.ExecuteReader

            If drXmlLoad.HasRows Then

                While drXmlLoad.Read()

                    ' Increment counter
                    intCursorCount = intCursorCount + 1

                    intXmlDataId = drXmlLoad.GetInt64(0)
                    Dim strXmlData As String = drXmlLoad.GetString(1)

                    ' Load to xml
                    Me.myXml.LoadXml(strXmlData)

                    ' Parse and insert into tables
                    parseData()

                End While ' End: Outer Loop

            End If

            ' Close the Data reader
            drXmlLoad.Close()

            gobjEvent.WriteToEventLog("SpocosyDatabaseClass:  Processing InsertEventsAndOtherData, number of rows: " + intCursorCount.ToString)

        Catch ex As Exception

            gobjEvent.WriteToEventLog("SpocosyDatabaseClass:  Processing InsertEventsAndOtherData exception: " + ex.Message, EventLogEntryType.Error)

        Finally
            cno.Close()
        End Try

    End Sub
    Public Sub InsertOutcomes()
        '-----------------------------------------------------------------------*
        ' Sub Routine parameters                                                *
        ' -----------------------                                               *
        '-----------------------------------------------------------------------*
        Dim cno As MySqlConnection = New MySqlConnection(connectionString)
        Dim drXmlLoad As MySqlDataReader
        Dim cmdXmlLoad As New MySqlCommand

        ' Populate lists
        populateLists()

        ' Sets type of deletion to perform later
        strDeleteKeyType = "xmlNodeId"

        ' Reset cursor counter
        intCursorCount = 0

        ' /----------------------------------------------------------------\
        ' | MySql Select                                                   |
        ' | Get all rows for nodeName from bookmaker_xml_nodes             |
        ' \----------------------------------------------------------------/
        cmdXmlLoad.CommandText = "SELECT bxn.`id`, bxn.`xmlData` FROM oddsmatching.bookmaker_xml_nodes AS bxn " &
                                 "INNER JOIN Event As ev ON bxn.`event_id` = ev.`id` " &
                                 "INNER JOIN outcome AS ou ON bxn.`outcome_id`=ou.`id` " &
                                 "WHERE ou.`object`=""event"" AND bxn.`nodeName` = @nodeName AND " &
                                 "ev.startdate >= str_to_date(@startDate, '%Y-%m-%d %H:%i:%s') AND " &
                                 "ev.startdate < str_to_date(@endDate, '%Y-%m-%d %H:%i:%s') " &
                                 "LIMIT @limit "
        cmdXmlLoad.Parameters.AddWithValue("nodeName", "outcome")

        ' Get start and end date
        Dim currentDateTime As DateTime = DateTime.UtcNow
        Dim dtStartDate As DateTime
        Dim dtEndDate As DateTime
        Dim strStartDate As String
        Dim strEndDate As String

        ' Calculate start date/time and To date/time
        dtStartDate = DateAdd(DateInterval.Hour, My.Settings.HoursOffsetStreamFrom, currentDateTime)
        dtEndDate = DateAdd(DateInterval.Hour, My.Settings.HoursOffsetStreamTo, currentDateTime)
        Dim centralEuropeZoneId As String = "Central Europe Standard Time"
        Dim centralEuropeZone As TimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(centralEuropeZoneId)
        strStartDate = TimeZoneInfo.ConvertTimeFromUtc(dtStartDate, centralEuropeZone).ToString("yyyy-MM-dd HH:mm:ss")
        strEndDate = TimeZoneInfo.ConvertTimeFromUtc(dtEndDate, centralEuropeZone).ToString("yyyy-MM-dd HH:mm:ss")

        cmdXmlLoad.Parameters.AddWithValue("startDate", strStartDate)
        cmdXmlLoad.Parameters.AddWithValue("endDate", strEndDate)
        cmdXmlLoad.Parameters.AddWithValue("limit", My.Settings.LimitOutcomeRows)

        cmdXmlLoad.Connection = cno

        Try
            cno.Open()
            drXmlLoad = cmdXmlLoad.ExecuteReader

            If drXmlLoad.HasRows Then

                While drXmlLoad.Read()

                    ' Increment counter
                    intCursorCount = intCursorCount + 1

                    intXmlDataId = drXmlLoad.GetInt64(0)
                    Dim strXmlData As String = drXmlLoad.GetString(1)

                    ' Load to xml
                    Me.myXml.LoadXml(strXmlData)

                    ' Parse and insert into tables
                    parseData()

                End While ' End: Outer Loop

            End If

            ' Close the Data reader
            drXmlLoad.Close()

            gobjEvent.WriteToEventLog("SpocosyDatabaseClass:  Processed InsertOutcomes, number of rows: " + intCursorCount.ToString)

        Catch ex As Exception

            gobjEvent.WriteToEventLog("SpocosyDatabaseClass:  Processing InsertOutcomes exception: " + ex.Message, EventLogEntryType.Error)

        Finally
            cno.Close()
        End Try

    End Sub

    Public Sub InsertBettingOffers()
        '-----------------------------------------------------------------------*
        ' Sub Routine parameters                                                *
        ' -----------------------                                               *
        '-----------------------------------------------------------------------*
        Dim cno As MySqlConnection = New MySqlConnection(connectionString)
        Dim drXmlLoad As MySqlDataReader
        Dim cmdXmlLoad As New MySqlCommand

        ' Populate lists
        populateLists()

        ' Sets type of deletion to perform later
        strDeleteKeyType = "outcomeId"

        ' Reset cursor counter
        intCursorCount = 0

        ' /----------------------------------------------------------------\
        ' | MySql Select                                                   |
        ' | Get all rows for nodeName from bookmaker_xml_nodes             |
        ' \----------------------------------------------------------------/
        cmdXmlLoad.CommandText = "SELECT outcome_id, max(node_n), bxn.id, bxn.xmlData FROM oddsmatching.bookmaker_xml_nodes AS bxn " &
                                 "INNER Join outcome As ou On bxn.`outcome_id`=ou.`id` " &
                                 "INNER Join event AS ev ON ou.objectFK = ev.`id` " &
                                 "WHERE ou.`object`=""event"" AND bxn.`nodeName` = @nodeName AND " &
                                 "ev.startdate >= str_to_date(@startDate, '%Y-%m-%d %H:%i:%s') AND " &
                                 "ev.startdate < str_to_date(@endDate, '%Y-%m-%d %H:%i:%s') " &
                                 "GROUP BY outcome_id " &
                                 "LIMIT @limit "
        cmdXmlLoad.Parameters.AddWithValue("nodeName", "bettingoffer")

        ' Get start and end date
        Dim currentDateTime As DateTime = DateTime.UtcNow
        Dim dtStartDate As DateTime
        Dim dtEndDate As DateTime
        Dim strStartDate As String
        Dim strEndDate As String

        ' Calculate start date/time and To date/time
        dtStartDate = DateAdd(DateInterval.Hour, My.Settings.HoursOffsetStreamFrom, currentDateTime)
        dtEndDate = DateAdd(DateInterval.Hour, My.Settings.HoursOffsetStreamTo, currentDateTime)
        Dim centralEuropeZoneId As String = "Central Europe Standard Time"
        Dim centralEuropeZone As TimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(centralEuropeZoneId)
        strStartDate = TimeZoneInfo.ConvertTimeFromUtc(dtStartDate, centralEuropeZone).ToString("yyyy-MM-dd HH:mm:ss")
        strEndDate = TimeZoneInfo.ConvertTimeFromUtc(dtEndDate, centralEuropeZone).ToString("yyyy-MM-dd HH:mm:ss")

        cmdXmlLoad.Parameters.AddWithValue("startDate", strStartDate)
        cmdXmlLoad.Parameters.AddWithValue("endDate", strEndDate)
        cmdXmlLoad.Parameters.AddWithValue("limit", My.Settings.LimitBettingOfferRows)

        cmdXmlLoad.Connection = cno

        Try
            cno.Open()
            drXmlLoad = cmdXmlLoad.ExecuteReader

            If drXmlLoad.HasRows Then

                While drXmlLoad.Read()

                    ' Increment counter
                    intCursorCount = intCursorCount + 1

                    ' Use the outcome id to delete all other outcomes aswell
                    intXmlDataId = drXmlLoad.GetInt64(0)
                    Dim strXmlData As String = drXmlLoad.GetString(3)

                    ' Load to xml
                    Me.myXml.LoadXml(strXmlData)

                    ' Parse and insert into tables
                    parseData()

                End While ' End: Outer Loop

            End If

            ' Close the Data reader
            drXmlLoad.Close()

            gobjEvent.WriteToEventLog("SpocosyDatabaseClass:  Processed InsertBettingOffers, number of rows: " + intCursorCount.ToString)

        Catch ex As Exception

            gobjEvent.WriteToEventLog("SpocosyDatabaseClass:  Processing InsertBettingOffers exception: " + ex.Message, EventLogEntryType.Error)

        Finally
            cno.Close()
        End Try

    End Sub

    'Parse and save the XML Data
    Public Sub parseData()
        For Each node As XmlNode In Me.myXml.ChildNodes
            nodeLoop(node, 0)
        Next
    End Sub

    'Loops through all nodes in the XML file.
    'This function loops itself to traverse through the XML tree.
    Private Sub nodeLoop(node As XmlNode, lvl As Integer)

        'Parse node if the name is contained in defined nodeList
        If Me.inList(Me.nodeList, node.Name) Then
            parseNode(node)
        End If

        'Loop through childNodes of current node
        For Each childNode As XmlNode In node.ChildNodes

            nodeLoop(childNode, lvl + 1)
        Next
    End Sub

    Private Sub parseNode(node As XmlNode)

        'Hardcode to switch name "event_participant" to "event_participants"
        Dim nodeName As String = If((node.Name = "event_participant"), "event_participants", node.Name)
        Dim n_xml As Integer = Convert.ToInt32(node.Attributes("n").Value)
        Dim id_xml As Integer = Convert.ToInt32(node.Attributes("id").Value)

        'check if node is already in the database and if it has changed
        Dim myConnection As New MySqlConnection(Me.connectionString)
        Dim myCommand1 As New MySqlCommand((Convert.ToString("select IF(count(*)=0, -1, n) as n from ") & nodeName) + " where id=@id")
        Dim myCommand2 As New MySqlCommand()
        Dim myCommand3 As New MySqlCommand()
        If strDeleteKeyType = "outcomeId" Then
            myCommand3.CommandText = "delete from oddsmatching.bookmaker_xml_nodes where outcome_id = @xmlNodesId"
        Else
            myCommand3.CommandText = "delete from oddsmatching.bookmaker_xml_nodes where id = @xmlNodesId"
        End If
        Dim SQLtrans As MySqlTransaction = Nothing
        Dim num As Integer = 0
        Dim num_del As Integer = 0
        Dim i As Integer = 0
        Dim msg As String = ""

        Try
            myCommand1.CommandType = CommandType.Text
            myCommand1.Connection = myConnection
            myCommand1.Parameters.Add(New MySqlParameter("id", id_xml))
            myCommand1.CommandTimeout = 300
            myConnection.Open()

            'Must open connection before starting transaction.
            SQLtrans = myConnection.BeginTransaction()
            myCommand1.Transaction = SQLtrans

            Dim n_db As Integer = Convert.ToInt32(myCommand1.ExecuteScalar())

            'Only insert or update node if it does'nt exist or it has changed
            If n_xml > n_db Then

                'Holds values to be saved
                Dim values As New Dictionary(Of String, String)()

                'String that holds fieldNames for query
                Dim queryFields As New StringBuilder()

                'Tablename. Hardcode "event_participant" to "event_participants"
                Dim tableName As String = If((node.Name = "event_participant"), "event_participants", node.Name)

                'Loop through attributes in node and check if attribute name exists in this.attribList
                For Each attrib As XmlAttribute In node.Attributes
                    If inList(attribList(tableName), attrib.Name) OrElse inList(attribList("ALL"), attrib.Name) Then
                        values.Add(attrib.Name, attrib.Value.ToString())
                        queryFields.Append(attrib.Name + (If((n_db > -1), "=@" + attrib.Name, "")) + ",")
                    End If
                Next

                'Remove last comma
                queryFields.Length = queryFields.Length - 1

                myCommand2.CommandType = CommandType.Text
                myCommand2.Connection = myConnection
                myCommand2.CommandTimeout = 300

                If n_db = -1 Then
                    myCommand2.CommandText = (Convert.ToString("INSERT INTO ") & tableName) + " (" + queryFields.ToString() + ") VALUES(@" + queryFields.Replace(",", ",@").ToString() + ")"
                    Me.insertCount += 1
                Else
                    myCommand2.CommandText = (Convert.ToString("UPDATE ") & tableName) + " SET " + queryFields.ToString() + " WHERE id=@id"
                    Me.updateCount += 1
                End If

                'Add mysql parameters to query (values to be inserted/updated)
                For Each key As String In values.Keys
                    myCommand2.Parameters.Add(New MySqlParameter(key, values(key)))
                Next

                myCommand2.ExecuteNonQuery()

            End If

            ' Now delete the original row from oddsmatching.bookmaker_xml_nodes
            myCommand3.CommandType = CommandType.Text
            myCommand3.Connection = myConnection
            myCommand3.Parameters.Add(New MySqlParameter("xmlNodesId", intXmlDataId))
            myCommand3.CommandTimeout = 300

            myCommand3.ExecuteNonQuery()

            'We are done. Now commit the transaction - actually change the DB.

            SQLtrans.Commit()


        Catch e As Exception
            'If anything went wrong attempt to rollback transaction
            Try
                SQLtrans.Rollback()
            Catch
            End Try

            ' LOG SOMETHING

        Finally
            Try
                'Whatever happens, you will land here and attempt to close the connection.
                myConnection.Close()
            Catch

            End Try
        End Try


    End Sub

    'Populate lists
    Private Sub populateLists()
        'Only these nodes should be parsed
        ' Changed removed result  Me.nodeList = "event_participant,country,status_desc,result_type,incident_type,event_incident_type,event_incident_type_text,lineup_type,offence_type,standing_type,standing_type_param,standing_config,language_type,sport,participant,tournament_template,tournament,tournament_stage,event,event_participants,outcome,bettingoffer,object_participants,lineup,incident,event_incident,event_incident_detail,result,standing,standing_participants,standing_data,property,language,image,reference,reference_type,odds_provider,scope_type,scope_data_type,event_scope,event_scope_detail,scope_result,lineup_scope_result,venue_data,venue_data_type,venue,venue_type"
        Me.nodeList = "event_participant,country,status_desc,result_type,incident_type,event_incident_type,event_incident_type_text,lineup_type,offence_type,standing_type,standing_type_param,standing_config,language_type,sport,participant,tournament_template,tournament,tournament_stage,event,event_participants,outcome,bettingoffer,lineup,incident,event_incident,event_incident_detail,standing,standing_participants,standing_data,property,language,image,reference,reference_type,odds_provider,scope_type,scope_data_type,event_scope,event_scope_detail,scope_result,lineup_scope_result,venue_data,venue_data_type,venue,venue_type"

        'Attributes that should always be included
        Me.attribList.Add("ALL", "id,n,ut,del")

        '
        '             * List of tables, which each contain a list of attributes, remove or add
        '             * attributes here to have it inculded in the database 
        '             * (make sure it the field exists in the database when adding attributes)
        '             

        Me.attribList.Add("bettingoffer", "outcomeFK,odds_providerFK,odds,odds_old,active,is_back,is_single,is_live,volume,currency,couponKey")
        Me.attribList.Add("country", "name")
        Me.attribList.Add("event", "name,tournament_stageFK,startdate,eventstatusFK,status_type,status_descFK")
        Me.attribList.Add("event_incident", "eventFK,sportFK,event_incident_typeFK,elapsed,elapsed_plus,comment,sortorder")
        Me.attribList.Add("event_incident_detail", "type,event_incidentFK,participantFK,value")
        Me.attribList.Add("event_incident_type", "player1,player2,team,comment,subtype1,subtype2,name,type,comment_type,player2_type")
        Me.attribList.Add("event_incident_type_text", "event_incident_typeFK,name")
        Me.attribList.Add("event_participants", "number,participantFK,eventFK")
        Me.attribList.Add("image", "object,objectFK,type,contenttype,name,value")
        Me.attribList.Add("incident", "event_participantsFK,incident_typeFK,incident_code,elapsed,sortorder,ref_participantFK")
        Me.attribList.Add("incident_type", "name,subtype")
        Me.attribList.Add("language", "object,objectFK,language_typeFK,name")
        Me.attribList.Add("language_type", "name,description")
        Me.attribList.Add("lineup", "event_participantsFK,participantFK,lineup_typeFK,shirt_number,pos")
        Me.attribList.Add("lineup_type", "name")
        Me.attribList.Add("object_participants", "object,objectFK,participantFK,participant_type,active")
        Me.attribList.Add("offence_type", "name")
        Me.attribList.Add("odds_provider", "name,url,bookmaker,preferred,betex,active")
        Me.attribList.Add("outcome", "object,objectFK,type,event_participant_number,scope,subtype,iparam,iparam2,dparam,dparam2,sparam")
        Me.attribList.Add("participant", "name,gender,type,countryFK,enetID,enetSportID")
        Me.attribList.Add("property", "object,objectFK,type,name,value")
        Me.attribList.Add("reference", "object,objectFK,refers_to,name")
        Me.attribList.Add("reference_type", "name,description")
        Me.attribList.Add("result", "event_participantsFK,result_typeFK,result_code,value")
        Me.attribList.Add("result_type", "name,code")
        Me.attribList.Add("sport", "name")
        Me.attribList.Add("standing", "object,objectFK,standing_typeFK,name")
        Me.attribList.Add("standing_config", "standingFK,standing_type_paramFK,value,sub_param")
        Me.attribList.Add("standing_data", "standing_participantsFK,standing_type_paramFK,value,code,sub_param")
        Me.attribList.Add("standing_participants", "standingFK,participantFK,rank")
        Me.attribList.Add("standing_type", "name,description")
        Me.attribList.Add("standing_type_param", "standing_typeFK,code,name,type,value")
        Me.attribList.Add("status_desc", "name,status_type")
        Me.attribList.Add("tournament", "name,tournament_templateFK")
        Me.attribList.Add("tournament_stage", "name,tournamentFK,gender,countryFK,startdate,enddate")
        Me.attribList.Add("tournament_template", "name,sportFK,gender")
        Me.attribList.Add("scope_type", "name,description")
        Me.attribList.Add("scope_data_type", "name,description")
        Me.attribList.Add("event_scope", "eventFK,scope_typeFK")
        Me.attribList.Add("event_scope_detail", "event_scopeFK,name,value")
        Me.attribList.Add("scope_result", "event_participantsFK,event_scopeFK,scope_data_typeFK,value")
        Me.attribList.Add("lineup_scope_result", "lineupFK,event_scopeFK,scope_data_typeFK,value")
        Me.attribList.Add("venue_data", "value,venue_data_typeFK,venueFK")
        Me.attribList.Add("venue_data_type", "name")
        Me.attribList.Add("venue", "name,countryFK,venue_typeFK")
        Me.attribList.Add("venue_type", "name")
    End Sub

    'Helper functions
    'Check if value is in comma seperated list
    Private Function inList(list As String, checkString As String) As Boolean
        Return (list.StartsWith(checkString) OrElse list.IndexOf(Convert.ToString(",") & checkString) <> -1)
    End Function

    Public Shared Function ReplaceDoubleDoubleQuotes(ByVal Line As String) As String
        Dim NewLine As String


        Dim strFindText As String = Chr(34) & Chr(34)
        Dim strReplaceText As String = Chr(34)

        NewLine = Replace(Line, strFindText, strReplaceText)


        Return NewLine

    End Function
End Class
