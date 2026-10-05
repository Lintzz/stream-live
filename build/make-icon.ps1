# Gera o Assets\app_icon.ico a partir da arte de 1024 px (Assets\app_icon_source_1024.png).
# Precisa do ImageMagick 7 (magick) no PATH. Rode da raiz do repositório:
#   powershell -ExecutionPolicy Bypass -File build\make-icon.ps1
#
# A arte tem muita margem em volta do símbolo: reduzida a 16/24/32 px o traço vira um borrão
# cinza na barra de tarefas. Esses três tamanhos saem de um recorte central (o símbolo ocupa
# mais do quadro) com o canto arredondado refeito; de 48 para cima vai a arte inteira. Cada
# tamanho é reduzido direto da fonte, nunca um do outro, para o traço não borrar em cascata.
$ErrorActionPreference = 'Stop'
$assets = Join-Path $PSScriptRoot '..\src\StreamLiveApp\Assets'
$source = Join-Path $assets 'app_icon_source_1024.png'
$target = Join-Path $assets 'app_icon.ico'

& magick $source `
    `( -clone 0 -resize 256x256 `) `
    `( -clone 0 -resize 128x128 `) `
    `( -clone 0 -resize 64x64 `) `
    `( -clone 0 -resize 48x48 `) `
    `( -clone 0 -gravity center -crop 720x720+0+0 +repage -alpha off `
       `( -size 720x720 xc:none -fill white -draw "roundrectangle 0,0 719,719 160,160" `) `
       -alpha off -compose CopyOpacity -composite -write mpr:small +delete `) `
    `( mpr:small -resize 32x32 `) `
    `( mpr:small -resize 24x24 `) `
    `( mpr:small -resize 16x16 `) `
    -delete 0 $target
if ($LASTEXITCODE -ne 0) { throw "magick falhou ($LASTEXITCODE)" }
& magick identify $target
