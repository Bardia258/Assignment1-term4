#requires -Version 7.0
<#
.SYNOPSIS
Verifies pm provisioning, password preservation, and municipality authorization.
.NOTES
Uses only new temporary SQLite databases and generated test credentials. No test
project is created. Existing databases are never opened, reset, or deleted.
Run after building the solution in Release.
#>
$ErrorActionPreference = 'Stop'
$webRoot = Join-Path $PSScriptRoot 'MunicipalElections.Web'
$assembly = Join-Path $webRoot 'bin/Release/net10.0/MunicipalElections.Web.dll'
if (-not (Test-Path -LiteralPath $assembly)) { throw 'Build the Web project in Release first.' }
$scratchRoot = Join-Path ([IO.Path]::GetTempPath()) ('municipal-pm-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $scratchRoot | Out-Null
$jwtKey = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(64))
$pmPassword = 'Ab1!' + [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(2))
$superPassword = 'Ab1!' + [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(12))
$changedPassword = 'Cd2!' + [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(2))
$script:assertions = 0
$clients = [Collections.Generic.List[Net.Http.HttpClient]]::new()

function Assert-Check([bool] $condition, [string] $message) {
    if (-not $condition) { throw $message }
    $script:assertions++
}
function Launch-App([string[]] $arguments = @(), [string] $username = 'pm',
    [string] $email = 'pm@example.invalid', [string] $password = $pmPassword) {
    $info = [Diagnostics.ProcessStartInfo]::new((Get-Command dotnet).Source)
    $info.WorkingDirectory = $webRoot
    $info.UseShellExecute = $false
    $info.CreateNoWindow = $true
    $info.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    $info.ArgumentList.Add($assembly)
    foreach ($argument in $arguments) { $info.ArgumentList.Add($argument) }
    $info.Environment['ASPNETCORE_ENVIRONMENT'] = 'Production'
    $info.Environment['DOTNET_ENVIRONMENT'] = 'Production'
    $info.Environment['ConnectionStrings__DefaultConnection'] = "Data Source=$script:dbPath;Pooling=False"
    $info.Environment['Uploads__RootPath'] = Join-Path $scratchRoot 'uploads'
    $info.Environment['Jwt__SigningKey'] = $jwtKey
    $info.Environment['ASPNETCORE_URLS'] = $script:baseUrl
    $info.Environment['BootstrapAdmin__Username'] = $username
    $info.Environment['BootstrapAdmin__Email'] = $email
    $info.Environment['BootstrapAdmin__Password'] = $password
    $process = [Diagnostics.Process]::Start($info)
    return @{ Process=$process; Output=$process.StandardOutput.ReadToEndAsync(); Error=$process.StandardError.ReadToEndAsync() }
}
function Run-Command([string] $command, [int] $expected = 0, [string] $username = 'pm',
    [string] $email = 'pm@example.invalid', [string] $password = $pmPassword) {
    $operation = Launch-App @($command) -username $username -email $email -password $password
    if (-not $operation.Process.WaitForExit(30000)) {
        $operation.Process.Kill($true)
        throw 'Initialization did not exit promptly.'
    }
    $text = $operation.Output.Result + $operation.Error.Result
    foreach ($secret in @($pmPassword, $superPassword, $changedPassword)) {
        Assert-Check (-not $text.Contains($secret)) 'Credentials appeared in command output.'
    }
    Assert-Check (-not $text.Contains('Now listening on')) 'Initialization started a web server.'
    Assert-Check ($operation.Process.ExitCode -eq $expected) "Unexpected exit code for $command."
    return $text
}
function New-Client {
    $handler = [Net.Http.HttpClientHandler]::new()
    $handler.AllowAutoRedirect = $false
    $handler.CookieContainer = [Net.CookieContainer]::new()
    $client = [Net.Http.HttpClient]::new($handler)
    $client.BaseAddress = [uri]$script:baseUrl
    $client.Timeout = [TimeSpan]::FromSeconds(10)
    $clients.Add($client)
    return $client
}
function Get-Page([Net.Http.HttpClient] $client, [string] $path, [int] $expected = 200) {
    $response = $client.GetAsync($path).GetAwaiter().GetResult()
    try {
        Assert-Check ([int]$response.StatusCode -eq $expected) "Unexpected HTTP status for $path."
        return $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    } finally { $response.Dispose() }
}
function Token([string] $html) {
    $match = [regex]::Match($html, '<input[^>]*name="__RequestVerificationToken"[^>]*value="([^"]+)"')
    Assert-Check $match.Success 'Antiforgery token missing.'
    return [Net.WebUtility]::HtmlDecode($match.Groups[1].Value)
}
function Login([string] $username, [string] $password, [bool] $expectedSuccess = $true) {
    $client = New-Client
    $html = Get-Page $client 'Admin/Account/Login'
    $fields = [Collections.Generic.Dictionary[string,string]]::new()
    $fields.Add('Username', $username); $fields.Add('Password', $password)
    $fields.Add('ReturnUrl', '/Admin'); $fields.Add('__RequestVerificationToken', (Token $html))
    $content = [Net.Http.FormUrlEncodedContent]::new($fields)
    $response = $client.PostAsync('Admin/Account/Login', $content).GetAwaiter().GetResult()
    try {
        $expected = if ($expectedSuccess) { 302 } else { 200 }
        Assert-Check ([int]$response.StatusCode -eq $expected) 'Administrator authentication did not behave as expected.'
    } finally { $response.Dispose(); $content.Dispose() }
    $null = Get-Page $client 'Admin' $(if ($expectedSuccess) { 200 } else { 302 })
    return $client
}
function Start-Server {
    $server = Launch-App
    $probe = New-Client
    for ($attempt = 0; $attempt -lt 100; $attempt++) {
        try {
            $response = $probe.GetAsync('api/municipalities').GetAwaiter().GetResult()
            $ready = [int]$response.StatusCode -eq 200
            $response.Dispose()
            if ($ready) { return $server }
        } catch { }
        if ($server.Process.HasExited) { throw 'Local application exited during startup.' }
        Start-Sleep -Milliseconds 100
    }
    $server.Process.Kill($true)
    throw 'Local application startup timed out.'
}
function Stop-Server($server) {
    if ($server -and -not $server.Process.HasExited) { $server.Process.Kill($true); $server.Process.WaitForExit() }
}
function Set-Port {
    $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
    $listener.Start(); $port = $listener.LocalEndpoint.Port; $listener.Stop()
    $script:baseUrl = "http://127.0.0.1:$port/"
}
$server = $null
try {
    Set-Port
    $script:dbPath = Join-Path $scratchRoot 'isolated.db'
    $null = Run-Command '--provision-pm-admin' -expected 1
    Assert-Check (-not (Test-Path -LiteralPath $script:dbPath)) 'Missing database was created by provisioning.'
    $null = Run-Command '--migrate-database'
    $null = Run-Command '--provision-pm-admin' -expected 1
    $null = Run-Command '--seed-demo-data'
    $null = Run-Command '--provision-super-admin' -username 'super' -email 'super@example.invalid' -password $superPassword
    $null = Run-Command '--provision-pm-admin' -expected 1 -password 'invalid'
    $null = Run-Command '--provision-pm-admin'
    $null = Run-Command '--provision-pm-admin' -password $changedPassword
    $null = Run-Command '--provision-pm-admin' -password ''
    $null = Run-Command '--provision-pm-admin' -expected 1 -username 'super'
    $null = Run-Command '--provision-pm-admin' -expected 1 -email 'changed@example.invalid'
    Write-Output 'PASS: explicit provisioning, database checks, password-policy rollback, and repeat/conflict handling.'

    $server = Start-Server
    $pmClient = Login 'pm' $pmPassword
    $null = Login 'pm' $changedPassword -expectedSuccess $false
    $superClient = Login 'super' $superPassword
    $anonymous = New-Client
    $candidateJson = Get-Page $anonymous 'api/candidates'
    $municipalities = @(Get-Page $anonymous 'api/municipalities' | ConvertFrom-Json)
    $positions = @(Get-Page $anonymous 'api/positions' | ConvertFrom-Json)
    $candidates = @($candidateJson | ConvertFrom-Json)
    Assert-Check ($municipalities.Count -eq 3 -and $positions.Count -eq 9 -and $candidates.Count -eq 12) 'Election records were not preserved.'
    foreach ($candidate in $candidates) {
        $expected = if ($candidate.municipalityName -eq 'Pitt Meadows') { 200 } else { 403 }
        foreach ($action in @('Details', 'Edit', 'Delete')) {
            $null = Get-Page $pmClient "Admin/Candidates/$action/$($candidate.id)" $expected
        }
        $null = Get-Page $superClient "Admin/Candidates/Details/$($candidate.id)"
    }
    foreach ($position in $positions) {
        $expected = if ($position.municipalityName -eq 'Pitt Meadows') { 200 } else { 403 }
        $null = Get-Page $pmClient "Admin/Positions/Edit/$($position.id)" $expected
    }
    $null = Get-Page $pmClient 'Admin/Municipalities' 403
    $null = Get-Page $superClient 'Admin/Municipalities'
    $otherCity = @($municipalities | Where-Object name -ne 'Pitt Meadows')[0]
    $fields = [Collections.Generic.Dictionary[string,string]]::new()
    $fields.Add('Name', 'Forbidden position'); $fields.Add('SeatsAvailable', '1')
    $fields.Add('MunicipalityId', [string]$otherCity.id)
    $fields.Add('__RequestVerificationToken', (Token (Get-Page $pmClient 'Admin/Positions/Create')))
    $content = [Net.Http.FormUrlEncodedContent]::new($fields)
    $response = $pmClient.PostAsync('Admin/Positions/Create', $content).GetAwaiter().GetResult()
    try { Assert-Check ([int]$response.StatusCode -eq 403) 'pm could post a position for another municipality.' }
    finally { $response.Dispose(); $content.Dispose() }
    Assert-Check ((Get-Page $anonymous 'api/candidates') -eq $candidateJson) 'Existing candidate data changed.'
    Write-Output 'PASS: pm authentication, preserved password, Pitt Meadows access, cross-municipality denials, and existing Super Admin access.'
    Stop-Server $server; $server = $null

    Set-Port
    $script:dbPath = Join-Path $scratchRoot 'unrelated-account.db'
    $null = Run-Command '--migrate-database'
    $null = Run-Command '--seed-demo-data'
    $server = Start-Server
    $anonymous = New-Client
    $json = @{ username='pm'; email='pm@example.invalid'; password=$pmPassword } | ConvertTo-Json -Compress
    $content = [Net.Http.StringContent]::new($json, [Text.Encoding]::UTF8, 'application/json')
    $response = $anonymous.PostAsync('api/auth/register', $content).GetAwaiter().GetResult()
    try { Assert-Check ([int]$response.StatusCode -eq 201) 'Could not create isolated ordinary-user fixture.' }
    finally { $response.Dispose(); $content.Dispose() }
    Stop-Server $server; $server = $null
    $null = Run-Command '--provision-pm-admin' -expected 1
    $server = Start-Server
    $null = Login 'pm' $pmPassword -expectedSuccess $false
    Write-Output 'PASS: an unrelated existing pm account cannot be adopted or promoted.'
    Write-Output "All $script:assertions assertions passed. Only newly created temporary databases were used."
} finally {
    Stop-Server $server
    foreach ($client in $clients) { $client.Dispose() }
}

