Attribute VB_Name = "Module1"
Sub TaoTemThepHangLoat()

    Dim wsTK As Worksheet
    Dim wsTem As Worksheet
    Dim lastRow As Long
    Dim i As Long
    Dim temIndex As Long
    Dim startRow As Long

    Set wsTK = ThisWorkbook.Sheets("Thong_ke")
    Set wsTem = ThisWorkbook.Sheets("In_tem_thep")
    
    Call ResetSheetInTemThep(wsTem)

    lastRow = wsTK.Cells(wsTK.Rows.Count, "A").End(xlUp).Row
    temIndex = 0

    Application.ScreenUpdating = False

    For i = 5 To lastRow

        ' M?i tem cao 16 dòng
        startRow = 1 + temIndex * 12

        ' Copy template
        wsTem.Range("A1:F12").Copy
        wsTem.Range("K" & startRow & ":P" & startRow + 11).PasteSpecial xlPasteAll
        
        Call CopyColumnWidth(wsTem.Range("A1:F1"), wsTem.Range("K1:P1"))
        Call CopyRowHeight(wsTem.Range("A1:A12"), wsTem.Range("K" & startRow & ":K" & startRow + 11))
        ' =========================
        ' GÁN D? LI?U CHU?N M?I
        ' =========================
        wsTem.Range("K" & startRow + 11).Value = wsTK.Range("A" & i).Value ' A5 -> K12
        wsTem.Range("K" & startRow + 5).Value = wsTK.Range("C" & i).Value  ' C5 -> K6

        wsTem.Range("N" & startRow + 8).Value = wsTK.Range("K" & i).Value  ' K5 -> N9
        wsTem.Range("N" & startRow + 9).Value = wsTK.Range("M" & i).Value  ' M5 -> N10
        wsTem.Range("N" & startRow + 7).Value = wsTK.Range("O" & i).Value  ' O5 -> N8
        wsTem.Range("N" & startRow + 10).Value = wsTK.Range("R" & i).Value ' R5 -> N11
        ' =========================

        temIndex = temIndex + 1

    Next i

    Application.CutCopyMode = False
    Application.ScreenUpdating = True

End Sub
Sub ResetSheetInTemThep(ws As Worksheet)

    Dim rng As Range
    Set rng = ws.Range("K:P")

    With rng
        ' Unmerge các ô trong K:P
        On Error Resume Next
        .UnMerge
        On Error GoTo 0

        ' Xoá n?i dung
        .ClearContents

        ' Xoá border
        .Borders.LineStyle = xlNone
    End With

End Sub
Sub CopyColumnWidth(src As Range, dest As Range)

    Dim i As Long
    For i = 1 To src.Columns.Count
        dest.Columns(i).ColumnWidth = src.Columns(i).ColumnWidth
    Next i

End Sub
Sub CopyRowHeight(src As Range, dest As Range)

    Dim i As Long
    For i = 1 To src.Rows.Count
        dest.Rows(i).RowHeight = src.Rows(i).RowHeight
    Next i

End Sub
