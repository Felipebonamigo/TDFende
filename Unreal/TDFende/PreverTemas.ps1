# Pré-visualização dos climas (tema): roda o jogo com cada -TdfTema (padrao, tarde, nevoa, outono, noite) no mesmo instante e monta\r\n# Unreal\TDFende\Saved\U0\temas_folha.png lado a lado, para o Felipe escolher. Só luz, névoa e cor: a arte do bioma entra na U3.\r\n#   powershell -ExecutionPolicy Bypass -File Unreal\TDFende\PreverTemas.ps1\r\nSet-Location (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
$ue = 'C:\Program Files\Epic Games\UE_5.8\Engine\Binaries\Win64\UnrealEditor.exe'
$uproj = (Resolve-Path Unreal\TDFende\TDFende.uproject).Path
$out = (Resolve-Path Unreal\TDFende\Saved\U0).Path
$replay = (Resolve-Path Tools\SimBridge\smoke\demo_replay.txt).Path
$temas = 'padrao', 'tarde', 'nevoa', 'outono', 'noite'
foreach ($t in $temas) {
    $jogo = @("`"$uproj`"", '/Engine/Maps/Entry?game=/Script/TDFende.TdfDemoGameMode', '-game', '-windowed', '-ResX=1600', '-ResY=900', '-nosplash', '-unattended', '-NoSound',
        "-abslog=`"$out\tema_$t.log`"", "-TdfReplay=`"$replay`"", "-TdfTema=$t", "-TdfShot=`"$out\tema_$t.png`"", '-TdfShotTicks=1200', '-TdfQuitTick=1260')
    $p = Start-Process $ue -ArgumentList $jogo -PassThru
    if (-not $p.WaitForExit(300 * 1000)) { $p.Kill(); "$t : TRAVOU" }
    "$t : " + (Test-Path "$out\tema_${t}_1200.png")
}
Add-Type -AssemblyName System.Drawing
$w = 800; $h = 450
$folha = New-Object System.Drawing.Bitmap ($w * 2), ($h * 3)
$g = [System.Drawing.Graphics]::FromImage($folha)
$g.Clear([System.Drawing.Color]::FromArgb(20, 20, 20))
$fonte = New-Object System.Drawing.Font 'Segoe UI', 20, ([System.Drawing.FontStyle]::Bold)
$i = 0
$nomes = @{ padrao = 'Padrão: campo verde, sol alto'; tarde = 'Fim de tarde dourado'; nevoa = 'Manhã de névoa'; outono = 'Outono'; noite = 'Noite de lua' }
foreach ($t in $temas) {
    $pth = "$out\tema_${t}_1200.png"
    if (Test-Path $pth) {
        $img = [System.Drawing.Image]::FromFile($pth)
        $x = ($i % 2) * $w; $y = [math]::Floor($i / 2) * $h
        $g.DrawImage($img, $x, $y, $w, $h); $img.Dispose()
        $g.FillRectangle((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(190, 0, 0, 0))), $x, $y + $h - 46, $w, 46)
        $g.DrawString(("{0}  [{1}]" -f $nomes[$t], $t), $fonte, [System.Drawing.Brushes]::White, $x + 12, $y + $h - 40)
    }
    $i++
}
$folha.Save("$out\temas_folha.png"); $g.Dispose(); $folha.Dispose()
