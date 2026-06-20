Add-Type -AssemblyName System.Net.Http

$token = (Invoke-RestMethod -Uri "http://localhost:5000/api/security/login" -Method Post -ContentType "application/json" -Body '{"login":"test","password":"12345"}').access_token

$client = New-Object System.Net.Http.HttpClient
$client.DefaultRequestHeaders.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $token)
$client.Timeout = [TimeSpan]::FromSeconds(10)

$summary = @{}

function Run-Test($name, $url, $method, $body) {
    Write-Output ("  " + $name + "...")
    $ok = 0; $err = 0; $err503 = 0; $err429 = 0; $errOther = 0
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
                if ($resp.IsSuccessStatusCode) { $ok++ }
                elseif ([int]$resp.StatusCode -eq 429) { $err429++ }
                elseif ([int]$resp.StatusCode -eq 503) { $err503++ }
                else { $errOther++ }
                $resp.Dispose()
            }
        } catch { $errOther += $rps }
        $elapsed = [int]($sw.Elapsed.TotalMilliseconds % 1000)
        if ($elapsed -lt 1000) { Start-Sleep -Milliseconds (1000 - $elapsed) }
    }
    $sw.Stop()
    $rpsActual = [math]::Round(($ok + $err429) / $sw.Elapsed.TotalSeconds)
    Write-Output ("    OK: " + $ok + ", 429: " + $err429 + ", 503: " + $err503 + ", errors: " + $errOther + ", RPS: " + $rpsActual)
    $summary[$name] = @{ok=$ok; err429=$err429; err503=$err503; errOther=$errOther; rps=$rpsActual}
}

Write-Output "=== Load Test ==="
Write-Output ("DB: 1M users, 5M posts, 3.5M friends, PG master + replica")

Run-Test "Feed (GET)" "http://localhost:5000/api/post/feed?limit=20" "GET" $null
Run-Test "Search users (GET)" "http://localhost:5000/api/user/search?query=test&limit=20" "GET" $null
Run-Test "Chat list (GET)" "http://localhost:5002/api/dialog/list?offset=0&limit=20" "GET" $null
Run-Test "User profile (GET)" "http://localhost:5000/api/user/get/test" "GET" $null
Run-Test "Create post (POST)" "http://localhost:5000/api/post/create" "POST" '{"text":"load test"}'

Write-Output "`n=== RESULTS ==="
Write-Output ("name".PadRight(25) + "OK".PadRight(8) + "429".PadRight(8) + "503".PadRight(8) + "errors".PadRight(8) + "RPS")
foreach ($k in $summary.Keys) {
    $s = $summary[$k]
    $line = $k.PadRight(25) + $s.ok.ToString().PadRight(8) + $s.err429.ToString().PadRight(8) + $s.err503.ToString().PadRight(8) + $s.errOther.ToString().PadRight(8) + $s.rps
    Write-Output $line
}
Write-Output "`nNote: 429 = rate limited (expected), 503 = infrastructure down"
$client.Dispose()
