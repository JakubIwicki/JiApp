# JiApp WSL2 Firewall Rules
# Opens inbound TCP for all JiApp microservice ports.
#
# Usage: Right-click PowerShell → Run as Administrator, then:
#   powershell -ExecutionPolicy Bypass -File set-up-firewall.ps1
#
# Or copy the script into the Admin PowerShell window directly.

Get-NetFirewallRule -DisplayName "JiApp*" | Remove-NetFirewallRule

6700, 6701, 6702, 6703, 6704 | ForEach-Object {
    New-NetFirewallRule -DisplayName "JiApp Port $_" -Direction Inbound -LocalPort $_ -Protocol TCP -Action Allow -Profile Any
    Write-Host "Port $_ : open" -ForegroundColor Green
}

Write-Host ""
Write-Host "Done. Test: https://localhost:6700/health" -ForegroundColor Cyan
Read-Host "Press Enter to exit"
