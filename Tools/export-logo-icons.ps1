# Export browser icon formats from the generated logo; no creative changes.
Add-Type -AssemblyName System.Drawing
$docsPath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../docs'))
$source = [Drawing.Image]::FromFile((Join-Path $docsPath 'images/math-runner-logo.png'))
function Export-PngBytes([int]$size) {
    $bitmap = New-Object Drawing.Bitmap($size, $size)
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    $graphics.CompositingQuality = [Drawing.Drawing2D.CompositingQuality]::HighQuality
    $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.DrawImage($source, 0, 0, $size, $size)
    $stream = New-Object IO.MemoryStream
    $bitmap.Save($stream, [Drawing.Imaging.ImageFormat]::Png)
    $bytes = $stream.ToArray()
    $graphics.Dispose(); $bitmap.Dispose(); $stream.Dispose()
    return ,$bytes
}
try {
    foreach ($size in @(16,32,48,192,512)) {
        [IO.File]::WriteAllBytes((Join-Path $docsPath "favicon-$size.png"), (Export-PngBytes $size))
    }
    [IO.File]::WriteAllBytes((Join-Path $docsPath 'apple-touch-icon.png'), (Export-PngBytes 180))
    $sizes = @(16,32,48)
    $images = @($sizes | ForEach-Object { ,(Export-PngBytes $_) })
    $stream = New-Object IO.MemoryStream
    $writer = New-Object IO.BinaryWriter($stream)
    $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$sizes.Count)
    $offset = 6 + 16 * $sizes.Count
    for ($i=0; $i -lt $sizes.Count; $i++) {
        $writer.Write([byte]$sizes[$i]); $writer.Write([byte]$sizes[$i])
        $writer.Write([byte]0); $writer.Write([byte]0)
        $writer.Write([uint16]1); $writer.Write([uint16]32)
        $writer.Write([uint32]$images[$i].Length); $writer.Write([uint32]$offset)
        $offset += $images[$i].Length
    }
    foreach ($bytes in $images) { $writer.Write([byte[]]$bytes) }
    [IO.File]::WriteAllBytes((Join-Path $docsPath 'favicon.ico'), $stream.ToArray())
    $writer.Dispose(); $stream.Dispose()
} finally { $source.Dispose() }
