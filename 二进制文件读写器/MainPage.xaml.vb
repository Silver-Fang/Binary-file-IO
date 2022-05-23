' The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409
Imports Windows.Storage
Imports Windows.Storage.Streams
Imports System.Text
Imports Windows.Storage.Pickers


''' <summary>
''' An empty page that can be used on its own or navigated to within a Frame.
''' </summary>
Public NotInheritable Class MainPage
	Inherits Page
	Private 文件打开对话框 As New FileOpenPicker
	Private 文件新建对话框 As New FileSavePicker
	Private 保存记录对话框 As New FileSavePicker
	Private 载入记录对话框 As New FileOpenPicker
	Private 当前流 As Stream
	Private 消息列表 As New ObservableCollection(Of String)
	Shared ReadOnly 漫游设置 As ApplicationDataContainer = ApplicationData.Current.RoamingSettings
	Shared ReadOnly 漫游目录 As StorageFolder = ApplicationData.Current.RoamingFolder
	Const 操作记录路径 As String = "操作记录.操作记录"
	ReadOnly 操作记录获取任务 As IAsyncOperation(Of IStorageItem) = 漫游目录.TryGetItemAsync(操作记录路径)
	Shared ReadOnly 最近访问列表 As AccessCache.StorageItemMostRecentlyUsedList = AccessCache.StorageApplicationPermissions.MostRecentlyUsedList
	Private 操作记录流 As Stream
	Private 操作记录读取器 As BinaryReader
	Private 操作记录写出器 As BinaryWriter
	Private 当前流读取器 As BinaryReader
	Private 当前流写出器 As BinaryWriter

	Private Function 参数检查(错误提示位置 As FrameworkElement) As Long
		If 当前流 Is Nothing Then
			错误内容.Text = "未打开任何文件"
			错误提示.ShowAt(错误提示位置)
			Return -1
		Else
			Try
				当前流.Position = Long.Parse(当前位置.Text)
			Catch ex As Exception
				错误内容.Text = ex.Message
				错误提示.ShowAt(错误提示位置)
				Return -1
			End Try
			Try
				Return Long.Parse(读写次数.Text)
			Catch ex As Exception
				错误内容.Text = ex.Message
				错误提示.ShowAt(错误提示位置)
				Return -1
			End Try
		End If
	End Function

	Private Async Sub 设置当前文件(文件操作 As IAsyncOperation(Of StorageFile), Optional 加入最近列表 As Boolean = True)
		Try
			设置当前文件(Await 文件操作, 加入最近列表)
		Catch ex As FileNotFoundException
			Exit Sub
		End Try
	End Sub

	Private Async Sub 设置当前文件(文件 As IStorageFile, Optional 加入最近列表 As Boolean = True)
		If 文件 IsNot Nothing Then
			If 当前流 IsNot Nothing Then
				当前流.Close()
			End If
			Dim 只读 As Boolean = False
			Try
				当前流 = (Await 文件.OpenAsync(FileAccessMode.ReadWrite)).AsStream
			Catch ex As IOException
				添加消息(ex.Message)
				只读 = True
			End Try
			If 只读 Then
				添加消息("将以只读模式打开")
				当前流 = (Await 文件.OpenReadAsync()).AsStreamForRead
			End If
			当前流读取器 = New BinaryReader(当前流)
			If 只读 Then
				当前流写出器 = Nothing
				二进制写出.IsEnabled = False
				文件路径.Text = 文件.Path & "（只读）"
			Else
				当前流写出器 = New BinaryWriter(当前流)
				二进制写出.IsEnabled = True
				文件路径.Text = 文件.Path
			End If
			If 加入最近列表 Then
				漫游设置.Values("上次打开文件令牌") = 最近访问列表.Add(文件)
				当前位置.Text = 当前流.Position
			Else
				当前流.Position = 漫游设置.Values("当前位置")
			End If
		End If
	End Sub

	Private Sub 打开文件_Click(sender As Object, e As RoutedEventArgs) Handles 打开文件.Click
		设置当前文件(文件打开对话框.PickSingleFileAsync)
	End Sub

	Private Sub 添加消息(消息 As String)
		消息列表.Add(消息)
		操作记录写出器.Write(消息)
	End Sub

	Private Sub 常规读入(Of T)(提示词 As String, 单读函数 As Func(Of T), 操作次数 As UInteger)
		Dim 字符串 As New StringBuilder("从位置")
		字符串.Append(当前流.Position).Append(提示词)
		For a As UInteger = 1 To 操作次数
			Try
				字符串.Append(单读函数.Invoke).Append(" ")
			Catch ex As Exception
				添加消息(字符串.ToString)
				添加消息("出错：" & ex.Message)
				Throw ex
			End Try
		Next
		Dim 结果 As String = 字符串.ToString
		添加消息(结果)
	End Sub

	Private Sub 二进制读入_Click(sender As Object, e As RoutedEventArgs) Handles 二进制读入.Click
		Dim 操作次数 As Long = 参数检查(sender)
		Static 操作列表 As Action(Of UInteger)() = {
	Sub(次数 As UInteger)
		常规读入("读入布尔逻辑：", AddressOf 当前流读取器.ReadBoolean, 次数)
	End Sub,
	Sub(次数 As UInteger)
		常规读入("读入8位无符号：", AddressOf 当前流读取器.ReadByte, 次数)
	End Sub,
	Sub(次数 As UInteger)
		常规读入("读入8位有符号：", AddressOf 当前流读取器.ReadSByte, 次数)
	End Sub,
	Sub(次数 As UInteger)
		常规读入("读入16位无符号：", AddressOf 当前流读取器.ReadUInt16, 次数)
	End Sub,
	Sub(次数 As UInteger)
		常规读入("读入16位有符号：", AddressOf 当前流读取器.ReadInt16, 次数)
	End Sub,
	Sub(次数 As UInteger)
		常规读入("读入32位无符号：", AddressOf 当前流读取器.ReadUInt32, 次数)
	End Sub,
	Sub(次数 As UInteger)
		常规读入("读入32位有符号：", AddressOf 当前流读取器.ReadInt32, 次数)
	End Sub,
	Sub(次数 As UInteger)
		常规读入("读入64位无符号：", AddressOf 当前流读取器.ReadUInt64, 次数)
	End Sub,
	Sub(次数 As UInteger)
		常规读入("读入64位有符号：", AddressOf 当前流读取器.ReadInt64, 次数)
	End Sub,
	Sub(次数 As UInteger)
		常规读入("读入十进制：", AddressOf 当前流读取器.ReadDecimal, 次数)
	End Sub,
	Sub(次数 As UInteger)
		常规读入("读入单精度：", AddressOf 当前流读取器.ReadSingle, 次数)
	End Sub,
	Sub(次数 As UInteger)
		常规读入("读入双精度：", AddressOf 当前流读取器.ReadDouble, 次数)
	End Sub,
	Sub(次数 As UInteger)
		Dim 当前位置 As ULong = 当前流.Position
		Dim 字符 As Char() = 当前流读取器.ReadChars(次数)
		添加消息("从位置" & 当前位置 & "读入字符：" & 字符)
	End Sub,
	Sub(次数 As UInteger)
		添加消息("从位置" & 当前流.Position & "读入字符串：")
		For a As UInteger = 1 To 次数
			添加消息("	" + 当前流读取器.ReadString)
		Next
	End Sub}
		If 操作次数 > 0 Then
			Try
				操作列表(数据类型.SelectedIndex).Invoke(操作次数)
			Catch ex As Exception
				错误内容.Text = ex.Message
				错误提示.ShowAt(二进制读入)
			End Try
			If 自动递进位置.IsChecked Then
				当前位置.Text = 当前流.Position
			Else
				当前流.Position = 当前位置.Text
			End If
		End If
	End Sub

	Private Sub 确认扩展名_Click(sender As Object, e As RoutedEventArgs) Handles 确认扩展名.Click
		Try
			文件新建对话框.FileTypeChoices.Item("用户定义类型").Item(0) = 扩展名.Text
		Catch ex As Exception
			错误内容.Text = ex.Message
			错误提示.ShowAt(确认扩展名)
			Exit Sub
		End Try
		设置当前文件(文件新建对话框.PickSaveFileAsync)
	End Sub

	Private Sub 常规写出(Of T)(提示词 As String, Parser As Func(Of String, T), 操作次数 As UInteger, 元素尺寸 As Byte)
		Dim 内容 As String = 写出内容.Text
		Dim Parsed内容 As T = Parser(内容)
		Dim 字节数 As UInteger = 操作次数 * 元素尺寸
		Dim 字节(字节数 - 1) As Byte
		提示词 = "向位置" & 当前流.Position & 提示词 & 内容 & "×" & 操作次数
		Try
			System.Buffer.BlockCopy(Enumerable.Repeat(Parsed内容, 操作次数).ToArray, 0, 字节, 0, 字节数)
		Catch ex As Exception
			添加消息(提示词 & "（出错）")
			添加消息("出错：" & ex.Message)
			Throw ex
		End Try
		当前流.WriteAsync(字节, 0, 字节数)
		当前流.FlushAsync()
		添加消息(提示词)
	End Sub

	Private Sub 二进制写出_Click(sender As Object, e As RoutedEventArgs) Handles 二进制写出.Click
		Dim 操作次数 As Long = 参数检查(sender)
		Static 操作列表 As Action(Of UInteger)() = {
	Sub(次数 As UInteger) 常规写出("写出布尔逻辑：", AddressOf Boolean.Parse, 次数, 1),
	Sub(次数 As UInteger)
		Dim 内容 As String = 写出内容.Text
		Dim 提示词 As String = "向位置" & 当前流.Position & "写出8位无符号：" & 内容 & "×" & 次数
		Try
			当前流.WriteAsync(Enumerable.Repeat(Byte.Parse(内容), 次数).ToArray, 0, 次数)
		Catch ex As Exception
			添加消息(提示词 & "（出错）")
			添加消息("出错：" & ex.Message)
			Throw ex
		End Try
		当前流.FlushAsync()
		添加消息(提示词)
	End Sub,
	Sub(次数 As UInteger) 常规写出("写出8位有符号：", AddressOf SByte.Parse, 次数, 1),
	Sub(次数 As UInteger) 常规写出("写出16位无符号：", AddressOf UShort.Parse, 次数, 2),
	Sub(次数 As UInteger) 常规写出("写出16位有符号：", AddressOf Short.Parse, 次数, 2),
	Sub(次数 As UInteger) 常规写出("写出32位无符号：", AddressOf UInteger.Parse, 次数, 4),
	Sub(次数 As UInteger) 常规写出("写出32位有符号：", AddressOf Integer.Parse, 次数, 4),
	Sub(次数 As UInteger) 常规写出("写出64位无符号：", AddressOf ULong.Parse, 次数, 8),
	Sub(次数 As UInteger) 常规写出("写出64位有符号：", AddressOf Long.Parse, 次数, 8),
	Sub(次数 As UInteger)
		Dim 内容 As String = 写出内容.Text
		Dim Parsed内容 As Decimal = Decimal.Parse(内容)
		For a As UInteger = 1 To 次数
			当前流写出器.Write(Parsed内容)
		Next
		添加消息("写出十进制：" & 内容 & " ×" & 次数)
	End Sub,
	Sub(次数 As UInteger) 常规写出("写出单精度：", AddressOf Single.Parse, 次数, 4),
	Sub(次数 As UInteger) 常规写出("写出双精度：", AddressOf Double.Parse, 次数, 8),
	Sub(次数 As UInteger)
		Dim 内容 As String = 写出内容.Text
		Dim 提示词 As String = "向位置" & 当前流.Position & "写出字符：" & 内容 & "×" & 次数
		Try
			当前流写出器.Write((New StringBuilder).AppendJoin("", Enumerable.Repeat(内容, 次数)).ToString.ToCharArray)
		Catch ex As Exception
			添加消息(提示词 & "（出错）")
			添加消息("出错：" & ex.Message)
			Throw ex
		End Try
		添加消息(提示词)
	End Sub,
	Sub(次数 As UInteger)
		Dim 内容 As String = 写出内容.Text
		Dim 提示词 As String = "向位置" & 当前流.Position & "写出字符串（前缀长度）：" & 内容 & "×" & 次数
		For a As UInteger = 1 To 次数
			当前流写出器.Write(内容)
		Next
		添加消息(提示词)
	End Sub}
		If 操作次数 > 0 Then
			Try
				操作列表(数据类型.SelectedIndex)(操作次数)
			Catch ex As Exception
				错误内容.Text = ex.Message
				错误提示.ShowAt(二进制读入)
			End Try
			If 自动递进位置.IsChecked Then
				当前位置.Text = 当前流.Position
			Else
				当前流.Position = 当前位置.Text
			End If
		End If
	End Sub

	Private Sub 关闭文件_Click() Handles 关闭文件.Click
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
		操作记录写出器.Seek(0, SeekOrigin.Begin)
		操作记录写出器.Write(CUShort(0))
	End Sub

	Private Sub 自动递进位置_Checked(sender As Object, e As RoutedEventArgs) Handles 自动递进位置.Checked, 自动递进位置.Unchecked
		漫游设置.Values("自动递进位置") = 自动递进位置.IsChecked
	End Sub

	Private Sub 关闭处理()
		Dim 流位置 As UInteger = 操作记录流.Position
		操作记录写出器.Seek(0, SeekOrigin.Begin)
		操作记录写出器.Write(CUShort(消息列表.Count))
		操作记录写出器.Seek(流位置, SeekOrigin.Begin)
		漫游设置.Values("数据类型") = 数据类型.SelectedIndex
		漫游设置.Values("当前位置") = 当前流.Position
		漫游设置.Values("写出内容") = 写出内容.Text
		Static 操作次数 As ULong = 1
		ULong.TryParse(读写次数.Text, 操作次数)
		漫游设置.Values("读写次数") = 操作次数
	End Sub

	Private Async Sub 保存记录_Click(sender As Object, e As RoutedEventArgs) Handles 保存记录.Click
		Dim 记录文件 As StorageFile = Await 保存记录对话框.PickSaveFileAsync()
		If 记录文件 IsNot Nothing Then
			Dim 记录写出器 As New BinaryWriter(Await 记录文件.OpenStreamForWriteAsync)
			记录写出器.Write(CUShort(消息列表.Count))
			For L As Short = 0 To 消息列表.Count - 1
				记录写出器.Write(消息列表(L))
			Next
			记录写出器.Close()
		End If
	End Sub

	Private Async Sub 载入记录(覆盖 As Boolean)
		Dim 记录文件 As StorageFile = Await 载入记录对话框.PickSingleFileAsync
		If 记录文件 IsNot Nothing Then
			Dim 记录读入器 As New BinaryReader(Await 记录文件.OpenStreamForWriteAsync)
			If 覆盖 Then
				消息列表.Clear()
				操作记录写出器.Seek(2, SeekOrigin.Begin)
			End If
			For L As UShort = 1 To 记录读入器.ReadUInt16
				添加消息(记录读入器.ReadString)
			Next
			记录读入器.Close()
		End If
	End Sub

	Private Sub 追加记录_Click(sender As Object, e As RoutedEventArgs) Handles 追加记录.Click
		载入记录(False)
	End Sub

	Private Sub 覆盖记录_Click(sender As Object, e As RoutedEventArgs) Handles 覆盖记录.Click
		载入记录(True)
	End Sub

	Private Async Sub MainPage_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
		文件打开对话框.FileTypeFilter.Add("*")
		载入记录对话框.FileTypeFilter.Add(".操作记录")
		文件新建对话框.FileTypeChoices.Add("用户定义类型", New List(Of String) From {".userdefined"})
		保存记录对话框.FileTypeChoices.Add("操作记录文件", New List(Of String) From {".操作记录"})
		操作记录.ItemsSource = 消息列表
		自动递进位置.IsChecked = If(漫游设置.Values("自动递进位置"), True)
		数据类型.SelectedIndex = If(漫游设置.Values("数据类型"), 0)
		读写次数.Text = If(漫游设置.Values("读写次数"), "")
		写出内容.Text = If(漫游设置.Values("写出内容"), "")
		Dim 操作记录文件 As StorageFile = Await 操作记录获取任务
		If 操作记录文件 Is Nothing Then
			操作记录文件 = Await 漫游目录.CreateFileAsync(操作记录路径)
		End If
		操作记录流 = (Await 操作记录文件.OpenAsync(FileAccessMode.ReadWrite)).AsStream
		操作记录读取器 = New BinaryReader(操作记录流)
		操作记录写出器 = New BinaryWriter(操作记录流)
		Dim 记录数 As UShort = 0
		Try
			记录数 = 操作记录读取器.ReadUInt16
		Catch ex As EndOfStreamException
			操作记录写出器.Write(CUShort(0))
		End Try
		Dim L As UShort
		Try
			For L = 1 To 记录数
				'此处不能用添加消息，自己添加给自己？
				消息列表.Add(操作记录读取器.ReadString)
			Next
		Catch ex As FormatException
			Dim 当前流位置 As UInteger = 操作记录流.Position
			操作记录流.Seek(0, SeekOrigin.Begin)
			操作记录写出器.Write(L)
			操作记录流.Seek(当前流位置, SeekOrigin.Begin)
		End Try
		Dim 上次打开文件令牌 As String = 漫游设置.Values("上次打开文件令牌")
		If 上次打开文件令牌 IsNot Nothing Then
			设置当前文件(最近访问列表.GetFileAsync(上次打开文件令牌), False)
		End If
		扩展名.Text = If(漫游设置.Values("扩展名"), ".bin")
		当前位置.Text = If(漫游设置.Values("当前位置"), 0)
		If 当前流 IsNot Nothing Then
			当前流.Seek(当前位置.Text, SeekOrigin.Begin)
		End If
		DirectCast(App.Current, App).注册关闭处理(AddressOf 关闭处理)
	End Sub

	Private Sub 二进制跳过_Click(sender As Object, e As RoutedEventArgs) Handles 二进制跳过.Click
		Static 数据大小 As Byte() = {1, 1, 1, 2, 2, 4, 4, 8, 8, 16, 4, 8}
		Dim 操作次数 As Long = 参数检查(sender)
		If 操作次数 > 0 Then
			Select Case 数据类型.SelectedIndex
				Case 12
					Try
						当前流读取器.ReadChars(操作次数)
					Catch ex As Exception
						错误内容.Text = ex.Message
						错误提示.ShowAt(sender)
					End Try
				Case 13
					Try
						For a = 1 To 操作次数
							当前流读取器.ReadString()
						Next
					Catch ex As Exception
						错误内容.Text = ex.Message
						错误提示.ShowAt(sender)
					End Try
				Case Else
					Try
						当前流.Position += 数据大小(数据类型.SelectedIndex) * 操作次数
					Catch ex As Exception
						错误内容.Text = ex.Message
						错误提示.ShowAt(sender)
					End Try
			End Select
			当前位置.Text = 当前流.Position
		End If
	End Sub
End Class
