#requires -Version 7.0
<#
.SYNOPSIS
Checks rendered MVC navigation and API responses without creating a test project.
.EXAMPLE
pwsh -File ./Check-PublicNavigation.ps1 -BaseUrl http://localhost:5000/
.NOTES
Use a locally running application with election records. The script only makes
HTTP requests and uses a fresh cookie jar; voting-guide changes affect only its
own anonymous session. It does not write application database records.
#>
param([Parameter(Mandatory)][uri] $BaseUrl)

$ErrorActionPreference = 'Stop'
$handler = [Net.Http.HttpClientHandler]::new()
$handler.AllowAutoRedirect = $false
$handler.CookieContainer = [Net.CookieContainer]::new()
$client = [Net.Http.HttpClient]::new($handler)
$client.BaseAddress = [uri]($BaseUrl.AbsoluteUri.TrimEnd('/') + '/')
$client.Timeout = [TimeSpan]::FromSeconds(15)
$client.DefaultRequestHeaders.Accept.ParseAdd('text/html')
$script:checks = 0

function Assert-Check([bool] $condition, [string] $message) {
    if (-not $condition) { throw $message }
    $script:checks++
}

function Request([string] $href, [int] $status = 200, [string] $mediaType = 'text/html') {
    $target = [uri]::new($client.BaseAddress, $href)
    Assert-Check ($target.Authority -eq $client.BaseAddress.Authority) 'Navigation left the application host.'
    $response = $client.GetAsync($target).GetAwaiter().GetResult()
    try {
        Assert-Check ([int]$response.StatusCode -eq $status) "GET $href returned $([int]$response.StatusCode), expected $status."
        if ($mediaType) {
            Assert-Check ($response.Content.Headers.ContentType.MediaType -eq $mediaType) "GET $href returned $($response.Content.Headers.ContentType), expected $mediaType."
        }
        return [pscustomobject]@{
            Body = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
            Location = [string]$response.Headers.Location
        }
    } finally { $response.Dispose() }
}

function Links([string] $html, [string] $text) {
    foreach ($match in [regex]::Matches($html, '(?is)<a\b(?<attributes>[^>]*)>(?<text>.*?)</a>')) {
        $label = [Net.WebUtility]::HtmlDecode([regex]::Replace($match.Groups['text'].Value, '<[^>]+>', ''))
        $label = [regex]::Replace($label, '\s+', ' ').Trim()
        if ($label -ne $text) { continue }
        $href = [regex]::Match($match.Groups['attributes'].Value, '(?is)\bhref\s*=\s*"(?<href>[^"]*)"')
        if ($href.Success) { [Net.WebUtility]::HtmlDecode($href.Groups['href'].Value) }
    }
}

function Public-Link([string] $href, [string] $controller, [string] $action = '') {
    $path = [uri]::new($client.BaseAddress, $href).AbsolutePath
    $prefix = $client.BaseAddress.AbsolutePath.TrimEnd('/')
    $expected = "$prefix/$controller"
    if ($action) { $expected += "/$action" }
    Assert-Check ($path -eq $expected -or $path.StartsWith("$expected/", [StringComparison]::OrdinalIgnoreCase)) "Public link '$href' must target $expected, never an API endpoint."
}

try {
    $municipalities = @((Request 'api/municipalities' -mediaType 'application/json').Body | ConvertFrom-Json)
    $candidates = @((Request 'api/candidates' -mediaType 'application/json').Body | ConvertFrom-Json)
    Assert-Check ($municipalities.Count -gt 0 -and $candidates.Count -gt 0) 'Navigation checks require existing municipality and candidate records.'

    $homepage = (Request '').Body
    $municipalityListLink = @(Links $homepage 'View Municipalities')
    Assert-Check ($municipalityListLink.Count -eq 1) 'Homepage must offer View Municipalities.'
    Public-Link $municipalityListLink[0] 'Municipalities'
    $municipalityList = (Request $municipalityListLink[0]).Body

    $homeDetails = @(Links $homepage 'View Candidates')
    $listDetails = @(Links $municipalityList 'View Details & Candidates')
    Assert-Check ($homeDetails.Count -eq $municipalities.Count) 'Homepage must link to every municipality.'
    Assert-Check ($listDetails.Count -eq $municipalities.Count) 'Municipality directory must link to every municipality.'

    foreach ($href in @($homeDetails + $listDetails)) {
        Public-Link $href 'Municipalities' 'Details'
        $details = (Request $href).Body
        foreach ($candidateHref in @(Links $details 'View Profile')) {
            Public-Link $candidateHref 'Candidates' 'Details'
            $null = Request $candidateHref
        }
    }
    Write-Output 'PASS: homepage and municipality links lead to HTML municipality/candidate details.'

    foreach ($municipality in $municipalities) {
        $api = (Request "api/municipalities/$($municipality.id)" -mediaType 'application/json').Body | ConvertFrom-Json
        Assert-Check ($api.id -eq $municipality.id) 'Municipality API returned an unexpected record.'
        $null = Request "Municipalities/Details/$($municipality.id)"
    }
    foreach ($candidate in $candidates) {
        $api = (Request "api/candidates/$($candidate.id)" -mediaType 'application/json').Body | ConvertFrom-Json
        Assert-Check ($api.id -eq $candidate.id) 'Candidate API returned an unexpected record.'
        $null = Request "Candidates/Details/$($candidate.id)"
    }
    Write-Output 'PASS: MVC details return text/html; unchanged API detail routes return application/json.'

    $directoryLink = @(Links $homepage 'Candidate Directory')
    Assert-Check ($directoryLink.Count -eq 1) 'Navigation must offer Candidate Directory.'
    Public-Link $directoryLink[0] 'Candidates'
    $directory = (Request $directoryLink[0]).Body
    $profileLinks = @(Links $directory 'View Profile')
    Assert-Check ($profileLinks.Count -eq $candidates.Count) 'Candidate directory must link to all candidate profiles.'
    foreach ($href in $profileLinks) { Public-Link $href 'Candidates' 'Details' }
    $candidateProfile = (Request $profileLinks[0]).Body
    $back = @(Links $candidateProfile 'Back to Directory')
    Assert-Check ($back.Count -eq 1) 'Candidate details must offer Back to Directory.'
    Public-Link $back[0] 'Candidates'
    $null = Request $back[0]

    $addLinks = @(Links $candidateProfile '+ Add to My Voting Guide')
    Assert-Check ($addLinks.Count -eq 1) 'Candidate details must offer Add to My Voting Guide.'
    Public-Link $addLinks[0] 'VotingGuide' 'Add'
    $added = Request $addLinks[0] -status 302 -mediaType ''
    Public-Link $added.Location 'Candidates'
    $null = Request $added.Location
    $guideLink = @(Links $homepage 'My Voting Guide')
    Assert-Check ($guideLink.Count -eq 1) 'Navigation must offer My Voting Guide.'
    Public-Link $guideLink[0] 'VotingGuide'
    $guide = (Request $guideLink[0]).Body
    $printLinks = @(Links $guide 'Print My Guide')
    Assert-Check ($printLinks.Count -eq 1) 'Added candidate must appear in a printable voting guide.'
    Public-Link $printLinks[0] 'VotingGuide' 'Print'
    $printed = (Request $printLinks[0]).Body
    $printBack = @(Links $printed 'Back to Guide')
    Assert-Check ($printBack.Count -eq 1) 'Print page must link back to the voting guide.'
    Public-Link $printBack[0] 'VotingGuide'
    $null = Request $printBack[0]
    $remove = @(Links $guide 'Remove')
    Assert-Check ($remove.Count -eq 1) 'This fresh session must contain exactly the added candidate.'
    Public-Link $remove[0] 'VotingGuide' 'Remove'
    $removed = Request $remove[0] -status 302 -mediaType ''
    Public-Link $removed.Location 'VotingGuide'
    $emptyGuide = (Request $removed.Location).Body
    $emptyDirectory = @(Links $emptyGuide 'Candidate Directory')
    Assert-Check ($emptyDirectory.Count -gt 0) 'Empty voting guide must link back to the candidate directory.'
    foreach ($href in $emptyDirectory) { Public-Link $href 'Candidates' }
    Write-Output 'PASS: candidate directory, profile, back links, and anonymous voting-guide add/print/remove navigation.'

    $admin = @(Links $homepage 'Administration')
    Assert-Check ($admin.Count -eq 1) 'Navigation must retain Administration.'
    Public-Link $admin[0] 'Admin'
    $redirect = Request $admin[0] -status 302 -mediaType ''
    Public-Link $redirect.Location 'Admin' 'Account'
    $login = (Request $redirect.Location).Body
    Assert-Check ($login -match 'name="Username"' -and $login -match 'name="Password"') 'Admin login form was not rendered.'
    Write-Output 'PASS: Admin navigation preserves the login redirect and HTML login form.'
    Write-Output "All $script:checks navigation assertions passed."
} finally {
    $client.Dispose()
    $handler.Dispose()
}

