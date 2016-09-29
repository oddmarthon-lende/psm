[String] $computername = (Get-WmiObject Win32_ComputerSystem).Name

Set-Interval 5000

$sys = Get-WmiObject Win32_OperatingSystem

    Set-Namespace "computers.servers.$computername.memory.TotalVisibleMemorySize"

    $size = $sys.TotalVisibleMemorySize

    Push-Data "KB" ([Convert]::ToInt64($size))
    Push-Data "MB" ($size / 1024)

    Set-Namespace "computers.servers.$computername.memory.UsedPhysicalMemory"

    $size = $sys.TotalVisibleMemorySize - $sys.FreePhysicalMemory

    Push-Data "KB" ([Convert]::ToInt64($size))
    Push-Data "MB" ($size / 1024)

    Set-Namespace "computers.servers.$computername.memory.FreePhysicalMemory"

    $size = $sys.FreePhysicalMemory

    Push-Data "KB" ([Convert]::ToInt64($size))
    Push-Data "MB" ($size / 1024)

    Set-Namespace "computers.servers.$computername.memory.TotalVirtualMemorySize"

    $size = $sys.TotalVirtualMemorySize

    Push-Data "KB" ([Convert]::ToInt64($size))
    Push-Data "MB" ($size / 1024)

    Set-Namespace "computers.servers.$computername.memory.FreeVirtualMemory"

    $size = $sys.FreeVirtualMemory

    Push-Data "KB" ([Convert]::ToInt64($size))
    Push-Data "MB" ($size / 1024)