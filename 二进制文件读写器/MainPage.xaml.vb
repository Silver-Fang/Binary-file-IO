' The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409
Imports Windows.Storage
Imports Windows.Storage.Streams
Imports System.Text

''' <summary>
''' An empty page that can be used on its own or navigated to within a Frame.
''' </summary>
Public NotInheritable Class MainPage
	Inherits Page
	Private 文件打开对话框 As New Pickers.FileOpenPicker
	Private 文件新建对话框 As New Pickers.FileSavePicker
	Private 当前流 As Stream
	Private 消息列表 As New ObservableCollection(Of String)
	Sub New()

		' This call is required by the designer.
		InitializeComponent()

		' Add any initialization after the InitializeComponent() call.
		文件打开对话框.FileTypeFilter.Add("*")
		文件新建对话框.FileTypeChoices.Add("用户定义类型", New List(Of String) From {".userdefined"})
		操作记录.ItemsSource = 消息列表
	End Sub
	Private Function 参数检查() As Long
		If 当前流 Is Nothing Then
			错误内容.Text = "未打开任何文件"
			错误提示.ShowAt(打开文件)
			Return -1
		Else
			Try
				当前流.Position = Long.Parse(当前位置.Text)
			Catch ex As Exception
				错误内容.Text = ex.Message
				错误提示.ShowAt(当前位置)
				Return -1
			End Try
			Try
				Return Long.Parse(读写次数.Text)
			Catch ex As Exception
				错误内容.Text = ex.Message
				错误提示.ShowAt(读写次数)
				Return -1
			End Try
		End If
	End Function
	Private Async Sub 设置当前文件(文件操作 As IAsyncOperation(Of StorageFile))
		Dim 文件 As StorageFile = Await 文件操作
		If 文件 IsNot Nothing Then
			If 当前流 IsNot Nothing Then
				当前流.Close()
			End If
			当前流 = (Await 文件.OpenAsync(FileAccessMode.ReadWrite)).AsStream
			文件路径.Text = 文件.Path
			当前位置.Text = 当前流.Position
		End If
	End Sub
	Private Sub 打开文件_Click(sender As Object, e As RoutedEventArgs) Handles 打开文件.Click
		设置当前文件(文件打开对话框.PickSingleFileAsync)
	End Sub
	Private Sub 常规读入(Of T)(提示词 As String, 单读函数 As Func(Of T), 操作次数 As UInteger)
		Dim 字符串 As New StringBuilder("从位置")
		字符串.Append(当前流.Position).Append(提示词)
		For a As UInteger = 1 To 操作次数
			Try
				字符串.Append(单读函数.Invoke).Append(" ")
			Catch ex As Exception
				消息列表.Add(字符串.ToString)
				消息列表.Add("出错：" & ex.Message)
				Throw ex
			End Try
		Next
		消息列表.Add(字符串.ToString)
	End Sub
	Private Sub 二进制读入_Click(sender As Object, e As RoutedEventArgs) Handles 二进制读入.Click
		Dim 操作次数 As Long = 参数检查()
		Static 操作列表 As Action(Of UInteger)() = {
	Sub(次数 As UInteger)
		常规读入("读入布尔逻辑：", AddressOf New BinaryReader(当前流).ReadBoolean, 次数)
	End Sub,
	Sub(次数 As UInteger)
		常规读入("读入8位无符号：", AddressOf New BinaryReader(当前流).ReadByte, 次数)
	End Sub,
	Sub(次数 As UInteger)
		常规读入("读入8位有符号：", AddressOf New BinaryReader(当前流).ReadSByte, 次数)
	End Sub,
	Sub(次数 As UInteger)
		常规读入("读入16位无符号：", AddressOf New BinaryReader(当前流).ReadUInt16, 次数)
	End Sub,
	Sub(次数 As UInteger)
		常规读入("读入16位有符号：", AddressOf New BinaryReader(当前流).ReadInt16, 次数)
	End Sub,
	Sub(次数 As UInteger)
		常规读入("读入32位无符号：", AddressOf New BinaryReader(当前流).ReadUInt32, 次数)
	End Sub,
	Sub(次数 As UInteger)
		常规读入("读入32位有符号：", AddressOf New BinaryReader(当前流).ReadInt32, 次数)
	End Sub,
	Sub(次数 As UInteger)
		常规读入("读入64位无符号：", AddressOf New BinaryReader(当前流).ReadUInt64, 次数)
	End Sub,
	Sub(次数 As UInteger)
		常规读入("读入64位有符号：", AddressOf New BinaryReader(当前流).ReadInt64, 次数)
	End Sub,
	Sub(次数 As UInteger)
		常规读入("读入十进制：", AddressOf New BinaryReader(当前流).ReadDecimal, 次数)
	End Sub,
	Sub(次数 As UInteger)
		常规读入("读入单精度：", AddressOf New BinaryReader(当前流).ReadSingle, 次数)
	End Sub,
	Sub(次数 As UInteger)
		常规读入("读入双精度：", AddressOf New BinaryReader(当前流).ReadDouble, 次数)
	End Sub,
	Sub(次数 As UInteger)
		Dim 字符 As Char() = New BinaryReader(当前流).ReadChars(次数)
		消息列表.Add("读入字符：" & 字符)
	End Sub,
	Sub(次数 As UInteger)
		Dim 读取器 As New BinaryReader(当前流)
		消息列表.Add("从位置" & 当前流.Position & "读入字符串：")
		For a As UInteger = 1 To 次数
			消息列表.Add("	" + 读取器.ReadString)
		Next
	End Sub}
		If 操作次数 > 0 Then
			Try
				操作列表(数据类型.SelectedIndex).Invoke(操作次数)
			Catch ex As Exception
				错误内容.Text = ex.Message
				错误提示.ShowAt(二进制读入)
			End Try
			当前位置.Text = 当前流.Position
		End If
	End Sub

	Private Sub 确认扩展名_Click(sender As Object, e As RoutedEventArgs) Handles 确认扩展名.Click
		文件新建对话框.FileTypeChoices.Item("用户定义类型").Item(0) = 扩展名.Text
		设置当前文件(文件新建对话框.PickSaveFileAsync)
	End Sub
	Private Sub 常规写出(Of T)(提示词 As String, Parser As Func(Of String, T), 操作次数 As UInteger, 元素尺寸 As Byte)
		Dim 内容 As String = 写出内容.Text
		Dim Parsed内容 As T = Parser(内容)
		Dim 字节数 As UInteger = 操作次数 * 元素尺寸
		Dim 字节(字节数 - 1) As Byte
		Try
			System.Buffer.BlockCopy(Enumerable.Repeat(Parsed内容, 操作次数).ToArray, 0, 字节, 0, 字节数)
		Catch ex As Exception
			消息列表.Add(提示词 & 内容 & " ×" & 操作次数 & "（出错）")
			消息列表.Add("出错：" & ex.Message)
			Throw ex
		End Try
		当前流.WriteAsync(字节, 0, 字节数)
		当前流.FlushAsync()
		消息列表.Add(提示词 & 内容 & " ×" & 操作次数)
	End Sub
	Private Sub 二进制写出_Click(sender As Object, e As RoutedEventArgs) Handles 二进制写出.Click
		Dim 操作次数 As Long = 参数检查()
		Static 操作列表 As Action(Of UInteger)() = {
	Sub(次数 As UInteger) 常规写出("写出布尔逻辑：", AddressOf Boolean.Parse, 次数, 1),
	Sub(次数 As UInteger)
		Dim 内容 As String = 写出内容.Text
		Try
			当前流.WriteAsync(Enumerable.Repeat(Byte.Parse(内容), 次数).ToArray, 0, 次数)
		Catch ex As Exception
			消息列表.Add("写出8位无符号：" & 内容 & " ×" & 次数 & "（出错）")
			消息列表.Add("出错：" & ex.Message)
			Throw ex
		End Try
		当前流.FlushAsync()
		消息列表.Add("写出8位无符号：" & 内容 & " ×" & 次数)
	End Sub,
	Sub(次数 As UInteger) 常规写出("写出8位有符号：", AddressOf SByte.Parse, 次数, 1),
	Sub(次数 As UInteger) 常规写出("写出16位无符号：", AddressOf UShort.Parse, 次数, 2),
	Sub(次数 As UInteger) 常规写出("写出16位有符号：", AddressOf Short.Parse, 次数, 2),
	Sub(次数 As UInteger) 常规写出("写出32位无符号：", AddressOf UInteger.Parse, 次数, 4),
	Sub(次数 As UInteger) 常规写出("写出32位有符号：", AddressOf Integer.Parse, 次数, 4),
	Sub(次数 As UInteger) 常规写出("写出64位无符号：", AddressOf ULong.Parse, 次数, 8),
	Sub(次数 As UInteger) 常规写出("写出64位有符号：", AddressOf Long.Parse, 次数, 8),
	Sub(次数 As UInteger)
		Dim 写出器 As New BinaryWriter(当前流)
		Dim 内容 As String = 写出内容.Text
		Dim Parsed内容 As Decimal = Decimal.Parse(内容)
		For a As UInteger = 1 To 次数
			写出器.Write(Parsed内容)
		Next
		消息列表.Add("写出十进制：" & 内容 & " ×" & 次数)
	End Sub,
	Sub(次数 As UInteger) 常规写出("写出单精度：", AddressOf Single.Parse, 次数, 4),
	Sub(次数 As UInteger) 常规写出("写出双精度：", AddressOf Double.Parse, 次数, 8),
	Sub(次数 As UInteger)
		Dim 内容 As String = 写出内容.Text
		Dim 写出器 As New BinaryWriter(当前流)
		Try
			写出器.Write(Enumerable.Repeat(Char.Parse(内容), 次数).ToArray)
		Catch ex As Exception
			消息列表.Add("写出字符：" & 内容 & " ×" & 次数 & "（出错）")
			消息列表.Add("出错：" & ex.Message)
			Throw ex
		End Try
		消息列表.Add("写出字符：" & 内容 & " ×" & 次数)
	End Sub,
	Sub(次数 As UInteger)
		Dim 写出器 As New BinaryWriter(当前流)
		Dim 内容 As String = 写出内容.Text
		For a As UInteger = 1 To 次数
			写出器.Write(内容)
		Next
		消息列表.Add("写出字符串：" & 内容 & " ×" & 次数)
	End Sub}
		If 操作次数 > 0 Then
			Try
				操作列表(数据类型.SelectedIndex)(操作次数)
			Catch ex As Exception
				错误内容.Text = ex.Message
				错误提示.ShowAt(二进制读入)
			End Try
			当前位置.Text = 当前流.Position
		End If
	End Sub

	Private Sub 关闭文件_Click(sender As Object, e As RoutedEventArgs) Handles 关闭文件.Click
		If 当前流 Is Nothing Then
			错误内容.Text = "未打开任何文件"
			错误提示.ShowAt(打开文件)
		Else
			当前流.Close()
			当前流 = Nothing
		End If
		当前位置.Text = ""
		文件路径.Text = ""
	End Sub

	Private Sub 清除记录_Click(sender As Object, e As RoutedEventArgs) Handles 清除记录.Click
		消息列表.Clear()
	End Sub
End Class
