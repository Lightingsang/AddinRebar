Set WshShell = WScript.CreateObject("WScript.Shell")
If WshShell.AppActivate("Autodesk Revit 2026") Then
    WScript.Sleep 500
    WshShell.SendKeys "{ENTER}"
    WScript.Sleep 500
    WshShell.SendKeys " "
    WScript.Echo "Sent Enter to Revit"
ElseIf WshShell.AppActivate("Revit") Then
    WScript.Sleep 500
    WshShell.SendKeys "{ENTER}"
    WScript.Sleep 500
    WshShell.SendKeys " "
    WScript.Echo "Sent Enter to Revit"
Else
    WScript.Echo "Revit not found"
End If
