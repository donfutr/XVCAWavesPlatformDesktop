Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports System.Net.Sockets
Imports System.Net
Imports System.IO
Imports System.Threading
Imports System.Diagnostics
Public Class SimpleHTTPServer
    Private ReadOnly _indexFiles As String() = {"index.html", "index.htm", "default.html", "default.htm"}
#Region "extension to MIME type list"
#End Region
    Private Shared _mimeTypeMappings As IDictionary(Of String, String) = New Dictionary(Of String, String)(StringComparer.InvariantCultureIgnoreCase) From { _
     {".asf", "video/x-ms-asf"}, _
     {".asx", "video/x-ms-asf"}, _
     {".avi", "video/x-msvideo"}, _
     {".bin", "application/octet-stream"}, _
     {".cco", "application/x-cocoa"}, _
     {".crt", "application/x-x509-ca-cert"}, _
     {".css", "text/css"}, _
     {".deb", "application/octet-stream"}, _
     {".der", "application/x-x509-ca-cert"}, _
     {".dll", "application/octet-stream"}, _
     {".dmg", "application/octet-stream"}, _
     {".ear", "application/java-archive"}, _
     {".eot", "application/octet-stream"}, _
     {".exe", "application/octet-stream"}, _
     {".flv", "video/x-flv"}, _
     {".gif", "image/gif"}, _
     {".hqx", "application/mac-binhex40"}, _
     {".htc", "text/x-component"}, _
     {".htm", "text/html"}, _
     {".html", "text/html"}, _
     {".ico", "image/x-icon"}, _
     {".img", "application/octet-stream"}, _
     {".iso", "application/octet-stream"}, _
     {".jar", "application/java-archive"}, _
     {".jardiff", "application/x-java-archive-diff"}, _
     {".jng", "image/x-jng"}, _
     {".jnlp", "application/x-java-jnlp-file"}, _
     {".jpeg", "image/jpeg"}, _
     {".jpg", "image/jpeg"}, _
     {".js", "application/x-javascript"}, _
     {".mml", "text/mathml"}, _
     {".mng", "video/x-mng"}, _
     {".mov", "video/quicktime"}, _
     {".mp3", "audio/mpeg"}, _
     {".mpeg", "video/mpeg"}, _
     {".mpg", "video/mpeg"}, _
     {".msi", "application/octet-stream"}, _
     {".msm", "application/octet-stream"}, _
     {".msp", "application/octet-stream"}, _
     {".pdb", "application/x-pilot"}, _
     {".pdf", "application/pdf"}, _
     {".pem", "application/x-x509-ca-cert"}, _
     {".pl", "application/x-perl"}, _
     {".pm", "application/x-perl"}, _
     {".png", "image/png"}, _
     {".prc", "application/x-pilot"}, _
     {".ra", "audio/x-realaudio"}, _
     {".rar", "application/x-rar-compressed"}, _
     {".rpm", "application/x-redhat-package-manager"}, _
     {".rss", "text/xml"}, _
     {".run", "application/x-makeself"}, _
     {".sea", "application/x-sea"}, _
     {".shtml", "text/html"}, _
     {".sit", "application/x-stuffit"}, _
     {".swf", "application/x-shockwave-flash"}, _
     {".tcl", "application/x-tcl"}, _
     {".tk", "application/x-tcl"}, _
     {".txt", "text/plain"}, _
     {".war", "application/java-archive"}, _
     {".wbmp", "image/vnd.wap.wbmp"}, _
     {".wmv", "video/x-ms-wmv"}, _
     {".xml", "text/xml"}, _
     {".xpi", "application/x-xpinstall"}, _
     {".zip", "application/zip"}, _
     {".svg", "image/svg+xml"}, _
     {".ttf", "application/x-font-truetype"}, _
     {".woff", "application/font-woff"}, _
     {".woff2", "application/font-woff2"} _
    }
    Private _serverThread As Thread
    Private _rootDirectory As String
    Private _listener As HttpListener
    Private _port As Integer

    Public Property Port() As Integer
        Get
            Return _port
        End Get
        Private Set(ByVal value As Integer)
        End Set
    End Property
    ''' <summary>
    ''' Construct server with given port.
    ''' </summary>
    ''' <param name="path">Directory path to serve.</param>
    ''' <param name="port">Port of the server.</param>
    Public Sub New(ByVal path As String, ByVal port As Integer)
        Me.Initialize(path, port)
    End Sub

    ''' <summary>
    ''' Construct server with suitable port.
    ''' </summary>
    ''' <param name="path">Directory path to serve.</param>
    Public Sub New(ByVal path As String)
        'get an empty port
        Dim l As New TcpListener(IPAddress.Loopback, 0)
        l.Start()
        Dim port As Integer = DirectCast(l.LocalEndpoint, IPEndPoint).Port
        l.[Stop]()
        Me.Initialize(path, port)
    End Sub

    ''' <summary>
    ''' Stop server and dispose all functions.
    ''' </summary>
    Public Sub [Stop]()
        _serverThread.Abort()
        _listener.[Stop]()
    End Sub

    Private Sub Listen()
        _listener = New HttpListener()
        _listener.Prefixes.Add("http://*:" + _port.ToString() + "/")
        _listener.Start()
        While True
            Try
                Dim context As HttpListenerContext = _listener.GetContext()
                Process(context)

            Catch ex As Exception
            End Try
        End While
    End Sub

    Private Sub Process(ByVal context As HttpListenerContext)
        Dim filename As String = context.Request.Url.AbsolutePath
        Console.WriteLine(filename)
        filename = filename.Substring(1)

        If String.IsNullOrEmpty(filename) Then
            For Each indexFile As String In _indexFiles
                If File.Exists(Path.Combine(_rootDirectory, indexFile)) Then
                    filename = indexFile
                    Exit For
                End If
            Next
        End If

        filename = Path.Combine(_rootDirectory, filename)

        If File.Exists(filename) Then
            Try
                Dim input As Stream = New FileStream(filename, FileMode.Open)

                'Adding permanent http response headers
                Dim mime As String
                context.Response.ContentType = If(_mimeTypeMappings.TryGetValue(Path.GetExtension(filename), mime), mime, "application/octet-stream")
                context.Response.ContentLength64 = input.Length
                context.Response.AddHeader("Date", DateTime.Now.ToString("r"))
                context.Response.AddHeader("Last-Modified", System.IO.File.GetLastWriteTime(filename).ToString("r"))

                Dim buffer As Byte() = New Byte(1024 * 16 - 1) {}
                Dim nbytes As Integer
                While (InlineAssignHelper(nbytes, input.Read(buffer, 0, buffer.Length))) > 0
                    context.Response.OutputStream.Write(buffer, 0, nbytes)
                End While
                input.Close()

                context.Response.StatusCode = CInt(HttpStatusCode.OK)
                context.Response.OutputStream.Flush()
            Catch ex As Exception
                context.Response.StatusCode = CInt(HttpStatusCode.InternalServerError)

            End Try
        Else
            context.Response.StatusCode = CInt(HttpStatusCode.NotFound)
        End If

        context.Response.OutputStream.Close()
    End Sub

    Private Sub Initialize(ByVal path As String, ByVal port As Integer)
        Me._rootDirectory = path
        Me._port = port
        _serverThread = New Thread(AddressOf Me.Listen)
        _serverThread.Start()
    End Sub
    Private Shared Function InlineAssignHelper(Of T)(ByRef target As T, ByVal value As T) As T
        target = value
        Return value
    End Function
End Class
