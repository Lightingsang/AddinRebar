Attribute VB_Name = "save_info"
Dim i, j, j1 As Integer
Sub save()
    i = 1
    Do While (Not (IsEmpty(Sheets("save_info").Range("A1").Offset(i - 1, 4))))
        If Sheets("save_info").Range("A1").Offset(i - 1, 4) = Range("B3") Then
            Exit Do
        End If
        i = i + 1
    Loop
    With Sheets("save_info")
        .Range("A1:ZZ1").Offset(i - 1, 0).ClearContents
        For j = 1 To 9
            .Cells(i, 2 * j - 1) = Cells(j, 2)
            .Cells(i, 2 * j) = Cells(j, 7)
        Next j
        .Cells(i, 19) = Range("I1")
        .Cells(i, 20) = Range("I5")
        .Cells(i, 21) = Range("H3")
        .Cells(i, 22) = Range("H5")
        .Cells(i, 23) = Range("B10")
        .Cells(i, 24) = Range("B11")
        .Cells(i, 25) = Range("B12")
        ''dai gia cuong
        .Cells(i, 26) = Range("I8")
        .Cells(i, 27) = Range("J7")
        .Cells(i, 28) = Range("J9")
        j = 29
        ''''
        j1 = 1
        Do While Not (IsEmpty(Range("B10").Offset(1, j1)))
            .Cells(i, j) = Range("B10").Offset(1, j1)
            For k = 1 To 32
                .Cells(i, j + k) = Range("B10").Offset(2 + k, j1)
            Next k
            j = j + 33
            j1 = j1 + 1
        Loop
    End With
End Sub
Sub load_data()
Dim ten_dam As String
    i = 1
    ten_dam = InputBox("Nhap ten dam :")
    ktr = False
    Application.ScreenUpdating = False
    Do While Not (IsEmpty(Sheets("save_info").Range("A1").Offset(i - 1, 4)))
        If Sheets("save_info").Range("A1").Offset(i - 1, 4) = ten_dam Then
            ktr = True
            Range("C11:ZZ44").ClearContents
            Exit Do
        End If
        i = i + 1
    Loop
    If Not ktr Then
         MsgBox "Dam " + ten_dam + " chua duoc SAVE"
         Exit Sub
    End If
    With Sheets("save_info")
        For j = 1 To 9
            If j > 3 Or Not IsEmpty(.Cells(i, 2 * j - 1)) Then Cells(j, 2) = .Cells(i, 2 * j - 1)
            If j > 3 Or Not IsEmpty(.Cells(i, 2 * j)) Then Cells(j, 7) = .Cells(i, 2 * j)
        Next j
        Range("I1") = .Cells(i, 19)
        Range("I5") = .Cells(i, 20)
        Range("H3") = .Cells(i, 21)
        Range("H5") = .Cells(i, 22)
        Range("B10") = .Cells(i, 23)
        Range("B11") = .Cells(i, 24)
        Range("B12") = .Cells(i, 25)
        ''dai gia cuong
        Range("I8") = .Cells(i, 26)
        Range("J7") = .Cells(i, 27)
        Range("J9") = .Cells(i, 28)
        j = 29
        ''''
        j1 = 1
        Do While Not (IsEmpty(.Cells(i, j)))
            Range("B10").Offset(1, j1) = .Cells(i, j)
            For k = 1 To 32
                Range("B10").Offset(2 + k, j1) = .Cells(i, j + k)
            Next k
            j = j + 33
            j1 = j1 + 1
        Loop
    End With
    Application.ScreenUpdating = True
End Sub

Function small_kata(x As Object, k As Byte) As Double
Dim a(10000) As Double
Dim i, j As Integer
    n = 0
    Do While (Not IsEmpty(x(n + 1))) And n < x.Count
        n = n + 1
        a(n) = Val(x(n))
    Loop
    If n = 0 Then Exit Function
    For i = 1 To n - 1
        For j = i + 1 To n
            If a(i) > a(j) Then
                tam = a(i): a(i) = a(j): a(j) = tam
            End If
        Next j
    Next i
    j = 1
    If k > 1 Then
        For i = 2 To n
            If a(i - 1) <> a(i) Then
                j = j + 1
                If j = k Then
                    small_kata = a(i)
                    Exit Function
                End If
            End If
        Next i
        small_kata = 0
    Else
        small_kata = a(1)
    End If
End Function
Function tra_bang_kl(phi As Object) As Double
        Select Case Val(phi)
            Case 6
                tra_bang_kl = 0.222
            Case 8
                tra_bang_kl = 0.395
            Case 10
                tra_bang_kl = 0.617
            Case 12
                tra_bang_kl = 0.888
            Case 14
                tra_bang_kl = 1.21
            Case 16
                tra_bang_kl = 1.58
            Case 18
                tra_bang_kl = 2
            Case 20
                tra_bang_kl = 2.47
            Case 22
                tra_bang_kl = 2.98
            Case 25
                tra_bang_kl = 3.85
            Case 28
                tra_bang_kl = 4.83
            Case 32
                tra_bang_kl = 6.31
            Case 36
                tra_bang_kl = 7.99
            Case 38
                tra_bang_kl = 8.9
            Case 40
                tra_bang_kl = 9.86
            Case 41
                tra_bang_kl = 10.36
            Case 43
                tra_bang_kl = 11.4
            Case 50
                tra_bang_kl = 15.42
            Case 51
                tra_bang_kl = 16.04

        End Select
End Function
