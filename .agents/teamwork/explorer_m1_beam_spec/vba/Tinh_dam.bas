Attribute VB_Name = "Tinh_dam"
Function anfa(M, b, ho) As Double
    Rb = Range("I10") / 10
        anfa = 100 * Abs(M) / Rb / b / ho ^ 2
End Function
Function lamda(anfa) As Double
    lamda = 1 - Sqr(1 - 2 * anfa)
End Function
Function Att(lamda, b, ho) As Double
    Rb = Range("I10") / 10
    Rs = Range("I11") / 10
        Att = lamda * Rb / Rs * b * ho
End Function
Function qd(x, n) As Double
    Call chuyen_dai(x, D, U)
    Rsw = Range("I12") / 10
    If x <> "" Then
        qd = Rsw * n * 3.1416 * D ^ 2 / 4 / U / 10
    Else
        qd = 0
    End If
End Function
Function Qdb(b, ho, qd) As Double
    Rbt = Range("N10") / 10
        Qdb = ho * Sqr(8 * Rbt * b * qd)
End Function
Sub chuyen_dai(x, phi, a)   'x=Ø10a100-->phi=10, a=100
    phi = 8: a = 100
    If Len(x) > 4 Then
        phi = Val(Mid(x, 2, InStr(x, "a") - InStr(x, "Ø") - 1))
        a = Val(Right(x, Len(x) - InStr(x, "a")))
    End If
End Sub
Function dt_thep(x) As Double 'x=5f20-->sl=5, phi=20
Dim f As Integer
    For i = 1 To x.Count
        If Len(x(i)) > 2 Then
            f = InStr(x(i), "Ø")
            If f = 0 Then f = InStr(x(i), "f")
            If f = 0 Then f = InStr(x(i), "F")
            sl = Val(Left(x(i), f - 1))
            phi = Val(Right(x(i), Len(x(i)) - f))
        Else
            sl = 0
        End If
        dt_thep = dt_thep + sl * dt1(phi)
    Next
End Function
Function tinh_a(abv, h, x) As Double
Dim f, i As Integer
Dim sl_dt, sl_a_dt As Double
    If Len(x(1)) < 2 Then
        tinh_a = h / 10
        Exit Function
    End If
    T = abv
    For i = 1 To x.Count
        If Len(x(i)) > 2 Then
            f = InStr(x(i), "Ø")
            If f = 0 Then f = InStr(x(i), "f")
            If f = 0 Then f = InStr(x(i), "F")
            sl = Val(Left(x(i), f - 1))
            phi = Val(Right(x(i), Len(x(i)) - f))
        Else
            sl = 0
        End If
        If i <> 2 Then T = T + phi / 2
        sl_a_dt = sl_a_dt + T * sl * dt1(phi)
        sl_dt = sl_dt + sl * dt1(phi)
        If i <> 1 Then T = T + phi / 2 + 25
    Next
    tinh_a = sl_a_dt / sl_dt / 10
End Function

Function dt1(D) As Double
    dt1 = D ^ 2 / 400 * 3.1416
End Function


