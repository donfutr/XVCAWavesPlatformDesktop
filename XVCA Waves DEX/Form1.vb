Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports System.Net.Sockets
Imports System.Net
Imports System.IO
Imports System.Threading
Imports System.Diagnostics
Imports Gecko
Public Class Form1
    Dim Myserver As SimpleHTTPServer
    Dim app_dir = Path.GetDirectoryName(Application.ExecutablePath)
    Dim htdocs = Path.Combine(app_dir, "web")
    Dim geckobrowser As GeckoWebBrowser

    Private Sub Form1_FormClosed(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosedEventArgs) Handles Me.FormClosed
        End
    End Sub

    Private Sub Form1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        Myserver = New SimpleHTTPServer(htdocs, 8084)
        Gecko.Xpcom.Initialize(Path.Combine(app_dir, "xulrunner"))
        geckobrowser = New GeckoWebBrowser()
        geckobrowser.Dock = DockStyle.Fill
        geckobrowser.Name = "geckobrowser"
        geckobrowser.NoDefaultContextMenu = True
        Controls.Add(geckobrowser)
        geckobrowser.Navigate("http://127.0.0.1:8084/")
    End Sub
End Class
