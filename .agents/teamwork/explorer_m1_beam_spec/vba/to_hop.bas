Attribute VB_Name = "to_hop"
Type info_thep_cat
    phi As Integer
    dai As Integer
    sl As Long
    mo_ta As String
    du As Integer
End Type
Dim thep_tp() As info_thep_cat
Dim kho_thep(10 To 40) As Collection

Sub update_collection(clt As Collection, dai As Integer, sl As Integer)
Dim i As Integer
Dim text As String
Dim sl1 As Integer
    For i = 1 To clt.Count
        text = clt.Item(i)
        If Val(clt.Item(i)) = dai Then
            sl1 = Val(Right(text, Len(text) - InStr(text, "*")))
            clt.Remove i
            If sl1 + sl <> 0 Then
                If i = clt.Count + 1 Then
                    clt.Add CStr(dai) + "*" + CStr(sl1 + sl)
                Else
                    clt.Add CStr(dai) + "*" + CStr(sl1 + sl), , i
                End If
            End If
            Exit Sub
        End If
    Next
    clt.Add CStr(dai) + "*" + CStr(sl)
End Sub
Sub lay_kho_thep()
Dim dai As Integer
Dim sl As Integer
    For i = LBound(kho_thep) To UBound(kho_thep)
        Set kho_thep(i) = New Collection
        kho_thep(i).Add "11700*1000"
    Next
    With Sheets("Kho_thep").Range("A3")
        j = 0
        Do While Not (IsEmpty(.Offset(-1, j)))
            phi = Val(Right(.Offset(-1, j), 2))
            If 6 <= phi And phi <= 40 Then
                i = 1
                Do While Not (IsEmpty(.Offset(i, j)))
                    dai = .Offset(i, j)
                    sl = .Offset(i, j + 1)
                    Call update_collection(kho_thep(phi), dai, sl)
                    i = i + 1
                Loop
            End If
            j = j + 2
        Loop
    End With
End Sub
Sub up_kho_thep()
Dim cot(10 To 40) As Integer
Dim dong As Integer
    Sheets("Kho_thep").Range("A4:X1000").ClearContents
    With Sheets("Kho_thep").Range("A3")
        j = 0
        Do While Not (IsEmpty(.Offset(-1, j)))
            phi = Val(Right(.Offset(-1, j), 2))
            cot(phi) = j
            j = j + 2
        Loop
        For i = LBound(kho_thep) To UBound(kho_thep)
            'tìm cot phi
            dong = 0
            If kho_thep(i).Item(1) <> "11700*1000" Then
                dong = dong + 1
                text = kho_thep(i).Item(1)
                sl1 = Val(Right(text, Len(text) - InStr(text, "*"))) - 1000
                .Offset(dong, cot(i)) = 11700
                .Offset(dong, cot(i) + 1) = sl1
            End If
            For j = 2 To kho_thep(i).Count
                dong = dong + 1
                text = kho_thep(i).Item(j)
                sl1 = Val(Right(text, Len(text) - InStr(text, "*")))
                .Offset(dong, cot(i)) = Val(text)
                .Offset(dong, cot(i) + 1) = sl1
            Next
        Next
    End With
End Sub
Sub update_kho_thep()
Dim phi As Integer
Dim dai As Integer
Dim sl As Integer
    Call lay_kho_thep
    i = 0
    With Range("Q3")
    Do While Not (IsEmpty(.Offset(i, 0)))
        phi = .Offset(i, 0)
        dai = .Offset(i, 1)
        sl = .Offset(i, 2)
        Call update_collection(kho_thep(phi), dai, -sl)
        dai = .Offset(i, 4)
        If dai > 0 Then Call update_collection(kho_thep(phi), dai, sl)
        i = i + 1
    Loop
    End With
    Call up_kho_thep
End Sub

Sub cap_nhat_thep_tp(ByVal dai As Integer, ByVal phi As Integer, ByVal sl As Integer)
Dim i As Integer
    ktra = False
        For i = 1 To UBound(thep_tp)
            If phi = thep_tp(i).phi And dai = thep_tp(i).dai Then
                thep_tp(i).sl = thep_tp(i).sl + sl
                ktra = True
                Exit For
            End If
        Next
        If Not ktra Then
            ReDim Preserve thep_tp(UBound(thep_tp) + 1)
            thep_tp(UBound(thep_tp)).phi = phi
            thep_tp(UBound(thep_tp)).sl = sl
            thep_tp(UBound(thep_tp)).dai = dai
        End If
End Sub
Function chuyen_text(ByVal text As String, ByVal phi As Integer, ByVal sl As Integer, ByRef tong As Integer) As String
Dim kq As String
Dim so() As Integer
Dim n, i, j, dem, tam As Integer
    ReDim so(1)
     n = 1: so(n) = CStr(Val(text))
    If 0 < InStr(text, "*") And InStr(text, "*") < 4 Then
        dem = so(n) - 1
        text = Right(text, Len(text) - InStr(text, "*"))
        so(n) = CStr(Val(text))
        For i = 1 To dem
            n = n + 1
            ReDim Preserve so(n)
            so(n) = CStr(Val(text))
        Next i
    End If
    Do While InStr(text, "+") > 0
        text = Right(text, Len(text) - InStr(text, "+"))
        n = n + 1
        ReDim Preserve so(n)
        so(n) = CStr(Val(text))
        If 0 < InStr(text, "*") And InStr(text, "*") < 4 Then
            dem = so(n) - 1
            text = Right(text, Len(text) - InStr(text, "*"))
            so(n) = CStr(Val(text))
            For i = 1 To dem
                n = n + 1
                ReDim Preserve so(n)
                so(n) = CStr(Val(text))
            Next i
        End If
    Loop
    'sap xep
    For i = 1 To n - 1
        For j = i + 1 To n
            If so(i) > so(j) Then
                tam = so(i): so(i) = so(j): so(j) = tam
            End If
        Next
    Next
    'don so giong nhau
    i = 1: dem = 1
    tam = so(i)
    Do While i < n
        If tam = so(i + 1) Then
            dem = dem + 1
            i = i + 1
        Else
            Exit Do
        End If
    Loop
    If dem = 1 Then
        chuyen_text = CStr(tam)
    Else
        chuyen_text = CStr(dem) + "*" + CStr(tam)
    End If
    For i = i + 1 To n
        dem = 1
        tam = so(i)
        Do While i < n
            If tam = so(i + 1) Then
                dem = dem + 1
                i = i + 1
            Else
                Exit Do
            End If
        Loop
        If dem = 1 Then
            chuyen_text = chuyen_text + "+" + CStr(tam)
        Else
            chuyen_text = chuyen_text + "+" + CStr(dem) + "*" + CStr(tam)
        End If
    Next
    tong = 0
    For i = 1 To n
        tong = tong + so(i)
        Call cap_nhat_thep_tp(so(i), phi, sl)
    Next
End Function
Function so_sanh(x As info_thep_cat, y As info_thep_cat) As Boolean
    If x.phi > y.phi Then
        so_sanh = True
    ElseIf x.phi = y.phi Then
        If x.dai > y.dai Then
            so_sanh = True
        ElseIf x.dai = y.dai And Val(x.mo_ta) > Val(y.mo_ta) Then
            so_sanh = True
        Else
            so_sanh = False
        End If
    Else
        so_sanh = False
    End If
End Function
Sub update_bang_cat_thep()
Dim x() As info_thep_cat
Dim a As info_thep_cat
Dim tam As info_thep_cat
Dim n, tong As Integer
Dim ktra As Boolean
    i = 0
    n = 0: ReDim x(0)
    ReDim thep_tp(0)
    With Range("B3")
    Do While Not (IsEmpty(.Offset(i, 0)))
        a.phi = .Offset(i, 0)
        a.dai = .Offset(i, 1)
        a.sl = .Offset(i, 2)
        a.mo_ta = chuyen_text(Range("B3").Offset(i, 3), a.phi, a.sl, tong)
        a.du = a.dai - tong
        ktra = False
        For j = 1 To n
            If a.phi = x(j).phi And a.dai = x(j).dai And a.mo_ta = x(j).mo_ta Then
                x(j).sl = x(j).sl + a.sl
                ktra = True
                Exit For
            End If
        Next
        If Not ktra Then
            n = n + 1
            ReDim Preserve x(n)
            x(n) = a
        End If
        i = i + 1
    Loop
    End With
    'sap xep
    For i = 1 To n - 1
        For j = i + 1 To n
            If so_sanh(x(i), x(j)) Then
                tam = x(i): x(i) = x(j): x(j) = tam
            End If
        Next
    Next
    '''''''''''''''
    
    
    ''''''''''
    Range("Q3:Y10000").ClearContents
    For i = 1 To n
        With Range("Q2")
            .Offset(i, 0) = x(i).phi
            .Offset(i, 1) = x(i).dai
            .Offset(i, 2) = x(i).sl
            .Offset(i, 3) = x(i).mo_ta
            .Offset(i, 4) = x(i).du
        End With
    Next
    '''''''''
    For i = 1 To UBound(thep_tp) - 1
        For j = i + 1 To UBound(thep_tp)
            If so_sanh(thep_tp(i), thep_tp(j)) Then
                tam = thep_tp(i): thep_tp(i) = thep_tp(j): thep_tp(j) = tam
            End If
        Next
    Next
    For i = 1 To UBound(thep_tp)
        With Range("W2")
            .Offset(i, 0) = thep_tp(i).phi
            .Offset(i, 1) = thep_tp(i).dai
            .Offset(i, 2) = thep_tp(i).sl
        End With
    Next
End Sub
Sub delete_data()
Range("A3:E10000").ClearContents
Range("I3:M10000").ClearContents
End Sub
