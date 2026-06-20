Add-Type -AssemblyName System.Net.Http

$token = (Invoke-RestMethod -Uri "http://localhost:5000/api/security/login" -Method Post -ContentType "application/json" -Body '{"login":"test","password":"12345"}').access_token

$client = New-Object System.Net.Http.HttpClient
$client.DefaultRequestHeaders.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $token)
$client.Timeout = [TimeSpan]::FromSeconds(10)

$report = @{}

function Run-Test($name, $url, $method, $body) {
    Write-Output ("  " + $name + "...")
    $ok = 0; $err = 0
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $rps = 50
    $duration = 5

    while ($sw.Elapsed.TotalSeconds -lt $duration) {
        $batch = @()
        for ($i = 0; $i -lt $rps; $i++) {
            if ($method -eq "GET") {
                $batch += $client.GetAsync($url)
            } else {
                $content = New-Object System.Net.Http.StringContent($body, [System.Text.Encoding]::UTF8, "application/json")
                $batch += $client.PostAsync($url, $content)
            }
        }
        try {
            $responses = [System.Threading.Tasks.Task]::WhenAll($batch)
            $responses.Wait(5000) | Out-Null
            foreach ($resp in $responses.Result) {
                if ($resp.IsSuccessStatusCode) { $ok++ } else { $err++ }
                $resp.Dispose()
            }
        } catch { $err += $batch.Count }
        $elapsed = [int]($sw.Elapsed.TotalMilliseconds % 1000)
        if ($elapsed -lt 1000) { Start-Sleep -Milliseconds (1000 - $elapsed) }
    }
    $sw.Stop()
    $rpsActual = [math]::Round($ok / $sw.Elapsed.TotalSeconds)
    Write-Output ("    OK: " + $ok + ", Errors: " + $err + ", RPS: " + $rpsActual)
    $report[$name] = @{ok=$ok; err=$err; rps=$rpsActual}
}

Write-Output "=== Load Test (50rps for 5s each) ==="

Run-Test "Feed (GET)" "http://localhost:5002/api/dialog/list?offset=0&limit=20" "GET" $null
Run-Test "Search (GET)" "http://localhost:5000/api/user/search?query=admin&limit=5" "GET" $null

# Post create (rate limited to 10/min - test at 10rps by using fewer calls)
$postBody = '{"text":"load test post"}'
Run-Test "Create post (5rps)" "http://localhost:5000/api/post/create" "POST" $postBody

# Login test
$loginOk = 0; $loginErr = 0
$loginClient = New-Object System.Net.Http.HttpClient
$loginClient.Timeout = [TimeSpan]::FromSeconds(10)
$sw = [System.Diagnostics.Stopwatch]::StartNew()
Write-Output "  Login (10rps)..."
while ($sw.Elapsed.TotalSeconds -lt 3) {
    for ($i = 0; $i -lt 10; $i++) {
        try {
            $c = New-Object System.Net.Http.StringContent('{"login":"test","password":"12345"}', [System.Text.Encoding]::UTF8, "application/json")
            $resp = $loginClient.PostAsync("http://localhost:5000/api/security/login", $c)
            $resp.Wait(5000) | Out-Null
            if ($resp.Result.IsSuccessStatusCode) { $loginOk++ } else { $loginErr++ }
            $resp.Result.Dispose()
        } catch { $loginErr++ }
    }
    Start-Sleep -Milliseconds 900
}
$sw.Stop()
$rpsLogin = [math]::Round($loginOk / $sw.Elapsed.TotalSeconds)
Write-Output ("    OK: " + $loginOk + ", Errors: " + $loginErr + ", RPS: " + $rpsLogin)

Write-Output "`n=== RESULTS ==="
Write-Output ("Login:        " + $loginOk + " OK, " + $loginErr + " errors, " + $rpsLogin + " rps")
foreach ($k in $report.Keys) {
    Write-Output ($k.PadRight(20) + ": " + $report[$k].ok + " OK, " + $report[$k].err + " errors, " + $report[$k].rps + " rps")
}

$client.Dispose()
$loginClient.Dispose()
