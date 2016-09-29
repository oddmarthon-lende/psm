[String] $computername = (Get-WmiObject Win32_ComputerSystem).Name

$computers = New-Object System.Collections.ArrayList

Set-Interval (60 * 60 * 1000) # 1 Hour
Set-Namespace "computers.servers.$computername.time_diff"

foreach($line in $(net view)) {
    
    If($line -match "\\\\(.+?)\b")
    {
        If($Matches[0].IndexOf($computername) -eq -1)
        {
            $null = $computers.Add($Matches[1])   
        }
    }
}

foreach($name in $computers)
{

    $query = $null
    

    Try {
        $query = (Get-WmiObject -ClassName Win32_LocalTime -ComputerName $name)
    }
    Catch
    {
		Push-Data $name "N/A"
        continue
    }
    

    If($query -ne $null)
    {

        $now = [DateTime]::Now
        $date = New-Object -TypeName System.DateTime -ArgumentList $($query.Year, $query.Month, $query.Day, $query.Hour, $query.Minute, $query.Second)
        
        Push-Data $name ($date.Subtract($now)).ToString()
    }
    Else
    {
        Push-Data $name "N/A"
    }  
    

    
}

