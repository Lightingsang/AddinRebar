$asm = [System.Reflection.Assembly]::LoadFrom("C:\kata_pro\Kata_Class_Lib.dll")
Write-Host "Total types in Kata_Class_Lib: " $asm.GetTypes().Count

$t = $asm.GetType("Kata_Class_Lib.Pub_Function")
if ($t) {
    Write-Host "Pub_Function methods:"
    $t.GetMethods() | ForEach-Object { Write-Host "  " $_.Name }
}

$t2 = $asm.GetType("Kata_Class_Lib.Bien_chung")
if ($t2) {
    Write-Host "Bien_chung static fields:"
    $t2.GetFields() | ForEach-Object {
        if ($_.IsStatic) {
            Write-Host "  " $_.Name "=" $_.GetValue($null)
        }
    }
}

$t3 = $asm.GetType("Kata_Class_Lib.Bien_chung_cad")
if ($t3) {
    Write-Host "Bien_chung_cad static fields:"
    $t3.GetFields() | ForEach-Object {
        if ($_.IsStatic) {
            Write-Host "  " $_.Name "=" $_.GetValue($null)
        }
    }
}
