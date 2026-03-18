Write-Host "Starting Docker check and restart..."
Write-Host ""

Write-Host "Stopping containers..."
docker compose down

Write-Host ""
Write-Host "Starting containers..."
docker compose up -d

Write-Host ""
Write-Host "Waiting 20 seconds..."
Start-Sleep -Seconds 20

Write-Host ""
Write-Host "Container status:"
docker compose ps

Write-Host ""
Write-Host "Testing port 3000 (frontend)..."
$test1 = Test-NetConnection -ComputerName localhost -Port 3000 -WarningAction SilentlyContinue
Write-Host "Result: $($test1.TcpTestSucceeded)"

Write-Host ""
Write-Host "Testing port 8080 (API)..."
$test2 = Test-NetConnection -ComputerName localhost -Port 8080 -WarningAction SilentlyContinue
Write-Host "Result: $($test2.TcpTestSucceeded)"

Write-Host ""
Write-Host "Recent API logs:"
docker compose logs api --tail=30

Write-Host ""
Write-Host "Done!"
