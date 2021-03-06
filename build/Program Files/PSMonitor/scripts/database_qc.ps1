
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

$query = @"

    declare @ds_name varchar(100);
    declare @ts1 datetime, @ts2 datetime, 
    		@runno int, @runno2 int, 
    		@q_start datetime, 
    		@wlbrName varchar(100), @wlbrName2 varchar(100),
    		@wlbrId varchar(13), @wlbrId2 varchar(13);

    select @ds_name = 'GenTime', @q_start = current_timestamp;

    declare c1 cursor for
    	select top 2 [GenDataIndex].[Time], [GenDataSet].[RunNo], [Wellbore].[WLBR_NAME], [Wellbore].[WLBR_IDENTIFIER]
    	from [GenDataset], [GenDataIndex], [Wellbore]
    	where [GenDataSet].[Name] = @ds_name
    		and [GenDataIndex].[GenDatasetId] = [GenDataset].[Id] 
    		and [Wellbore].[WLBR_IDENTIFIER] = [GenDataSet].[WellboreId] 
    	order by [GenDataIndex].[Time] desc;

    open c1;

    if @@cursor_rows != 2
    begin
    	goto finalize;
    end

    fetch next from c1 into @ts1, @runno, @wlbrName, @wlbrId;
    fetch next from c1 into @ts2, @runno2, @wlbrName2, @wlbrId2;

    finalize: 

    close c1;
    deallocate c1;

    select  @wlbrId as wlbr_id,
    		@wlbrName as wlbr_name, 
    		@ds_name as ds_name, 
    		datediff(millisecond, @ts2, @ts1) as sampling_interval_ms, 
    		@runno as ds_runno, 
    		datediff(millisecond, @q_start, current_timestamp) as query_time_ms,
    		@ts1 as last_update_time;

    select max(mwru_number) as [mwru_no_max],
    		case when (@runno = max([mwru_number])) then 1 else 0 end as [mwru_no_max_active]
    from [mwd_run] where [wlbr_identifier] = @wlbrId;
    
    
    declare @svy_tbl  table (
    	srsc_count int not null,
    	srsp_count int,
    	srsp_duplicate_count int,
    	srsp_null_timestamp int,
    	srsp_first_not_zero bit
    	
    );

    declare @svySectId varchar(13), @srsp_time datetime, @srsp_mdepth float, @srsp_incl float, @srsp_azi float;

    insert into @svy_tbl
    	select count(*) as [srsc_count], 
    		null, null, null, null
    	from [survey_section] where [wlbr_identifier] = @wlbrId;

    if exists (select * from @svy_tbl where [srsc_count] = 1)
    begin

    	select @svySectId = [srsc_identifier] from [survey_section] where [wlbr_identifier] = @wlbrId;
    	
    	select top 1 
    		@srsp_time = [srsp_time], 
    		@srsp_mdepth = [srsp_mdepth], 
    		@srsp_incl = [srsp_incl], 
    		@srsp_azi = [srsp_azi] 
    	from [survey_station] 
    	where [srsc_identifier] = @svySectId 
    	order by [srsp_mdepth] asc;
    	
    	update @svy_tbl set [srsp_count] = (select count(*) from [survey_station] where [srsc_identifier] = @svySectId);

    	update @svy_tbl set [srsp_first_not_zero] = (@srsp_mdepth + @srsp_incl + @srsp_azi);
    	
    	update @svy_tbl set [srsp_duplicate_count] = (
    		select max([srsp_count]) - count(distinct [srsp_mdepth]) from [survey_station], @svy_tbl where [srsc_identifier] = @svySectId);

    	update @svy_tbl set [srsp_null_timestamp] = (
    		select count(*) from [survey_station] where [srsc_identifier] = @svySectId and [srsp_time] is null
    	);	

    end

    select * from @svy_tbl;
    
"@

$tables = (db-query "(local)\advantage2005" "advantage" $query)

[String]$ds_name

# GenTime check
foreach ( $row in $tables[0].Rows )
 
{
    $ds_name = $row.ds_name.toLower()
    
    
    Set-Namespace "computers.servers.$computername.database.qc.advantage.$ds_name"
    Push-Data "wlbr_name" $row.wlbr_name
    Push-Data "sampling_interval_ms" $row.sampling_interval_ms
    Push-Data "ds_runno" $row.ds_runno
    Push-Data "last_update_time" $row.last_update_time
}

# Last Run Number is active check
foreach ( $row in $tables[1].Rows )
 
{
    

    Push-Data "mwru_no_max" $row.mwru_no_max
    Push-Data "mwru_no_active" $row.mwru_no_active
}

# Survey check
foreach ( $row in $tables[2].Rows )
 
{
    Set-Namespace "computers.servers.$computername.database.qc.advantage.survey"
    
    Push-Data "srsc_count" $row.srsc_count
    Push-Data "srsp_count" $row.srsp_count
    Push-Data "srsp_duplicate_count" $row.srsp_duplicate_count
    Push-Data "srsp_null_timestamp" $row.srsp_null_timestamp
    Push-Data "srsp_first_no_zero" $row.srsp_first_no_zero

}
