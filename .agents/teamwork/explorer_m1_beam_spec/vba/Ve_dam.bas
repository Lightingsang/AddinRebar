Attribute VB_Name = "Ve_dam"
Public Function GetCadApplication() As Object

    Dim app As Object
    Dim progIds As Variant
    Dim p As Variant

    'Th? CAD nào dang m? tru?c
    progIds = Array( _
        "AutoCAD.Application", _
        "AutoCAD.Application.24", _
        "AutoCAD.Application.25", _
        "AutoCAD.Application.26", _
        "GCAD.Application", _
        "GstarCAD.Application", _
        "GCAD.Application.24", _
        "ZwCAD.Application" _
    )

    On Error Resume Next

    For Each p In progIds

        Err.Clear
        Set app = GetObject(, CStr(p))

        If Not app Is Nothing Then
            On Error GoTo 0
            Set GetCadApplication = app
            Exit Function
        End If

    Next p

    On Error GoTo 0

    Set GetCadApplication = Nothing

End Function
Sub laychieudai()
    Dim point1 As Variant
    Dim point2 As Variant
    Dim kc, x, y, x1, y1 As Double
On Error GoTo out:
    'Goi autocad tu excel
        On Error Resume Next
     Set acadapp = GetCadApplication()
     If acadapp Is Nothing Then
          MsgBox _
            "Khong tim thay AutoCAD, ZWCAD hoac GstarCAD dang chay." & vbCrLf & _
            "Vui long mo CAD truoc roi thuc hien lai.", _
            vbExclamation, _
            "KATA"

        Exit Sub
     End If
    acadapp.Visible = True
    Set AcadUt = acadapp.ActiveDocument.Utility
    point1 = AcadUt.GetPoint(, vbLf & "Select the first point:")
    point2 = AcadUt.GetPoint(point1, vbLf & "select the next point:")
    '''''''''''''''''''
    Set excelapp = GetObject(, "Excel.Application")
    With excelapp
        kc = Round(Sqr((point1(0) - point2(0)) ^ 2 + (point1(1) - point2(1)) ^ 2) / 5) * 5
        Select Case .Selection.Row
            Case 11
                .Selection = kc
                .Selection.Offset(0, 1).Select
                Do While kc > 0
                    point1 = point2
                    point2 = AcadUt.GetPoint(point1, vbLf & "select the next point:")
                    kc = Round(Sqr((point1(0) - point2(0)) ^ 2 + (point1(1) - point2(1)) ^ 2))
                    If kc > 0 Then
                        .Selection = kc
                        .Selection.Offset(0, 1).Select
                    End If
                Loop
            Case 20
                If .Selection.Column Mod 2 = 1 Then
                    Do While kc > 0
                        x = (point1(0) + point2(0)) / 2
                        y = (point1(1) + point2(1)) / 2
                        .Selection = kc
                        .Selection.Offset(1, 0).Select
                        point1 = AcadUt.GetPoint(, vbLf & "Chon diem dau cot:")
                        point2 = AcadUt.GetPoint(point1, vbLf & "Chon diem cuoi cot:")
                        x1 = (point1(0) + point2(0)) / 2
                        y1 = (point1(1) + point2(1)) / 2
                        kc = Round(Sqr((x - x1) ^ 2 + (y - y1) ^ 2))
                        If x + y > x1 + y1 Then
                            .Selection = kc
                        Else
                            .Selection = -kc
                        End If
                        .Selection.Offset(-1, 2).Select
                        point1 = Nothing
                        point1 = AcadUt.GetPoint(, vbLf & "Chon diem dau dam giao ke tiep:")
                        If IsEmpty(point1) Then Exit Do
                        point2 = AcadUt.GetPoint(point1, vbLf & "Chon diem cuoi dam giao:")
                        kc = Round(Sqr((point1(0) - point2(0)) ^ 2 + (point1(1) - point2(1)) ^ 2))
                    Loop
                End If
            Case 23
                If .Selection.Column Mod 2 = 1 Then
                    Do While kc > 0
                        x1 = (point1(0) + point2(0)) / 2
                        y1 = (point1(1) + point2(1)) / 2
                        point2 = Nothing
                        point2 = AcadUt.GetPoint(point1, vbLf & "Chon diem tren truc:")
                        If IsEmpty(point2) Then Exit Do
                        x = point2(0): y = point2(1)
                        kc = Round(Sqr((x - x1) ^ 2 + (y - y1) ^ 2))
                        If x + y > x1 + y1 Then
                            .Selection = kc
                        Else
                            .Selection = -kc
                        End If
                        .Selection.Offset(0, 2).Select
                        point1 = AcadUt.GetPoint(, vbLf & "Chon diem dau cot:")
                        If IsEmpty(point1) Then Exit Do
                        point2 = AcadUt.GetPoint(point1, vbLf & "Chon diem cuoi cot:")
                    Loop
                End If
            Case Else
                MsgBox "Ban chon vi tri dau tien chua hop ly!", vbOKOnly, "Thong bao"
        End Select
    End With
out:
End Sub

Sub lay_chdai()
    Call laychieudai
End Sub
Sub chuyen_thep(x, sl, phi) 'x=5f20-->sl=5, phi=20
Dim f As Integer
    If Len(x) > 2 Then
        f = InStr(x, "f")
        If f = 0 Then f = InStr(x, "F")
        sl = Val(Left(x, f - 1))
        phi = Val(Right(x, Len(x) - f))
    Else
        sl = 0
    End If
End Sub
Sub chuyen_ht(x, delta_h, sl, phi) 'x=-50;5f20-->sl=5, phi=20
Dim f, ph As Integer
    f = InStr(x, "f")
    If f = 0 Then f = InStr(x, "F")
    ph = InStr(x, ";")
    If ph > 0 Then
        delta_h = Val(Left(x, f - 1))
        sl = Val(Mid(x, ph + 1, f - ph))
        phi = Val(Right(x, Len(x) - f))
    ElseIf Len(x) > 0 Then
        delta_h = Val(x)
    End If
End Sub
Sub abc()
Dim ht, hd, sl_t, phi_t, sl_d, phi_d, i As Integer
    Application.ScreenUpdating = False
    n = Range("A28")
    Call chuyen_thep(Range("B11"), sl_t, phi_t) 'so cay thep chiu luc phia tren
    Call chuyen_thep(Range("B12"), sl_d, phi_d) 'so cay thep chiu luc phia duoi
    With Range("B9")
        For i = 1 To n
            Call chuyen_ht(.Offset(10, i * 2), ht, sl_t, phi_t) 'giat dam phia tren
            Call chuyen_ht(.Offset(12, i * 2), hd, sl_d, phi_d)   'giat dam phia duoi
        Next i
    End With
    Range("B61") = Range("B5") + ht - hd
    Range("B63") = CStr(sl_t) + "f" + CStr(phi_t)
    Range("B64") = CStr(sl_d) + "f" + CStr(phi_d)
    Do While IsEmpty(Range("A24").Offset(1, 2 * n)) And IsEmpty(Range("A24").Offset(0, 2 * n)) And n > 1
        n = n - 1
    Loop
    For i = 1 To 20
        Range("E60").Offset(i, 0) = Range("A24").Offset(i, 2 * n)
        Range("E60").Offset(i, 1) = Range("A24").Offset(i, 2 * n + 1)
    Next i
    '''''''''''''''
    Dim sc As SeriesCollection
    ActiveSheet.ChartObjects("Chart 38").Activate
    Set sc = ActiveChart.SeriesCollection
    bieudo1 = bieudo(Range("E61:F80"), sc)
    Application.ScreenUpdating = True
End Sub
Function bieudo(a As Range, sc As Object) As Boolean
    Dim sr As Object
    Dim ktra As Boolean
    Dim D, d1, i As Integer
    For D = 1 To a.Rows.Count
        If IsEmpty(a(D, 2)) Then
            For i = 6 To sc.Count
                If sc(i).Name = "Dai" + CStr(D) Then
                    sc(i).Delete
                    Exit For
                End If
            Next i
        Else
            ktra = True
            For i = 6 To sc.Count
                If sc(i).Name = "Dai" + CStr(D) Then
                    ktra = False
                    Set sr = sc(i)
                    Exit For
                End If
            Next i
            If ktra Then
                sc.NewSeries
                Set sr = sc(sc.Count)
                sr.Name = "Dai" + CStr(D)
            End If
            d1 = 60 + D
            If Right(a(D, 1), 1) = "U" Then
                sr.XValues = "=Dam!$I$" + CStr(d1) + ":$L$" + CStr(d1)
                sr.Values = "=Dam!$N$" + CStr(d1) + ":$Q$" + CStr(d1)
            ElseIf Right(a(D, 1), 1) = "C" Then
                sr.XValues = "=Dam!$I$" + CStr(d1) + ":$M$" + CStr(d1)
                sr.Values = "=Dam!$N$" + CStr(d1) + ":$R$" + CStr(d1)
            Else
                sr.XValues = "=Dam!$I$" + CStr(d1) + ":$M$" + CStr(d1)
                sr.Values = "=Dam!$N$" + CStr(d1) + ":$R$" + CStr(d1)
            End If
        End If
    Next D
End Function
Sub xemten_bd()
    MsgBox ActiveChart.Name
End Sub




