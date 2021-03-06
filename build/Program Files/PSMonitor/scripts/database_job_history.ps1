
[String] $computername = (Get-WmiObject Win32_ComputerSystem).Name

Set-Interval (1000 * 60 * 60) # 1 Hour

Function db-query ()
{
    [CmdletBinding()]
    Param(
    [Parameter(Position=0,Mandatory=$true)][String]$SQLServer,
    [Parameter(Position=1,Mandatory=$true)][String]$SQLDBName,
    [Parameter(Position=2,Mandatory=$true)][String]$SQLQuery
    )
   
        $SqlConnection = New-Object System.Data.SqlClient.SqlConnection 
        $SqlConnection.ConnectionString = "Server=$SQLServer;Database=$SQLDBName;Integrated Security=True"
         
        $SqlCmd = New-Object System.Data.SqlClient.SqlCommand 
        $SqlCmd.CommandText = $SQLQuery 
        $SqlCmd.Connection = $SqlConnection
         
        $SqlAdapter = New-Object System.Data.SqlClient.SqlDataAdapter 
        $SqlAdapter.SelectCommand = $SqlCmd
         
        $DataSet = New-Object System.Data.DataSet
         
        [void]$SqlAdapter.Fill($DataSet)
        [void]$SqlConnection.Close()
        
        ,$DataSet.Tables
        
        
}

If($stop -is [DateTime]) { }
Else
{
    Try {
        $query = "select max(stop_execution_date) stop from msdb.dbo.sysjobactivity;"
        $tables = (db-query "(local)\advantage2005" "advantage" $query)
        $stop = $tables[0].Rows[0].stop
        $stop.ToString("s")
    }
    Catch
    { 
        Exit 1001
    }
}

$query = @"

    select jobs.name, history.run_status, activity.start_execution_date, activity.stop_execution_date
        from msdb.dbo.sysjobhistory history
        left join msdb.dbo.sysjobs jobs on(jobs.job_id = history.job_id)
        left join msdb.dbo.sysjobactivity activity on (history.instance_id = activity.job_history_id)
        where history.step_id = 0 and activity.stop_execution_date is not null and activity.stop_execution_date > '$($stop.ToString("s"))'
        order by activity.stop_execution_date asc;
    
"@

$tables = (db-query "(local)\advantage2005" "advantage" $query)

foreach ( $row in $tables[0].Rows )
 
{

    Set-Namespace "computers.servers.$computername.database.job_history.$($row.name.Replace(".", "_"))"
    Push-Data "run_status" $row.run_status $row.stop_execution_date
    
}
