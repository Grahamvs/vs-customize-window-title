﻿Imports System.Runtime.InteropServices
Imports EnvDTE
Imports EnvDTE80
Imports System.IO
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio
Imports System.Threading
Imports System.Text
Imports System.Text.RegularExpressions

''' <summary>
''' This is the class that implements the package exposed by this assembly.
'''
''' The minimum requirement for a class to be considered a valid package for Visual Studio
''' is to implement the IVsPackage interface and register itself with the shell.
''' This package uses the helper classes defined inside the Managed Package Framework (MPF)
''' to do it: it derives from the Package class that provides the implementation of the 
''' IVsPackage interface and uses the registration attributes defined in the framework to 
''' register itself and its components with the shell.
''' </summary>
' The PackageRegistration attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class
' is a package.
'
' The InstalledProductRegistration attribute is used to register the information needed to show this package
' in the Help/About dialog of Visual Studio.

<PackageRegistration(UseManagedResourcesOnly:=True),
    InstalledProductRegistration("#110", "#112", "1.0", IconResourceID:=400),
    ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string),
    ProvideMenuResource("Menus.ctmenu", 1),
    Guid(GuidList.guidRenameVSWindowTitle3PkgString)>
<ProvideOptionPage(GetType(OptionPageGrid),
                   "Rename VS Window Title", "Rules", 0, 0, True)>
<ProvideOptionPage(GetType(SupportedTagsGrid),
                   "Rename VS Window Title", "Supported tags", 101, 1000, True)>
Public NotInheritable Class RenameVSWindowTitle
    Inherits Package

    'Private dte As EnvDTE.DTE
    Private ReadOnly DTE As DTE2
    Private ReadOnly _events As EnvDTE.Events
    Private ReadOnly _debuggerEvents As DebuggerEvents
    Private ReadOnly _solutionEvents As SolutionEvents
    Private ReadOnly _windowEvents As WindowEvents
    Private ReadOnly _documentEvents As DocumentEvents

    Private IDEName As String

    Private ResetTitleTimer As System.Windows.Forms.Timer
    'Private VersionSpecificAssembly As Assembly

    ''' <summary>
    ''' Default constructor of the package.
    ''' Inside this method you can place any initialization code that does not require 
    ''' any Visual Studio service because at this point the package object is created but 
    ''' not sited yet inside Visual Studio environment. The place to do all the other 
    ''' initialization is the Initialize method.
    ''' </summary>
    Public Sub New()
        Me.DTE = DirectCast(GetGlobalService(GetType(EnvDTE.DTE)), EnvDTE80.DTE2)
        Me._events = Me.DTE.Events
        Me._debuggerEvents = _events.DebuggerEvents
        Me._solutionEvents = _events.SolutionEvents
        Me._windowEvents = _events.WindowEvents
        Me._documentEvents = _events.DocumentEvents
        AddHandler _debuggerEvents.OnEnterBreakMode, New _dispDebuggerEvents_OnEnterBreakModeEventHandler(AddressOf OnIdeEvent)
        AddHandler _debuggerEvents.OnEnterRunMode, New _dispDebuggerEvents_OnEnterRunModeEventHandler(AddressOf OnIdeEvent)
        AddHandler _debuggerEvents.OnEnterDesignMode, New _dispDebuggerEvents_OnEnterDesignModeEventHandler(AddressOf OnIdeEvent)
        AddHandler _debuggerEvents.OnContextChanged, New _dispDebuggerEvents_OnContextChangedEventHandler(AddressOf OnIdeEvent)
        AddHandler _solutionEvents.AfterClosing, New _dispSolutionEvents_AfterClosingEventHandler(AddressOf OnIdeEvent)
        AddHandler _solutionEvents.Opened, New _dispSolutionEvents_OpenedEventHandler(AddressOf OnIdeEvent)
        AddHandler _solutionEvents.Renamed, New _dispSolutionEvents_RenamedEventHandler(AddressOf OnIdeEvent)
        AddHandler _windowEvents.WindowCreated, New _dispWindowEvents_WindowCreatedEventHandler(AddressOf OnIdeEvent)
        AddHandler _windowEvents.WindowClosing, New _dispWindowEvents_WindowClosingEventHandler(AddressOf OnIdeEvent)
        AddHandler _windowEvents.WindowActivated, New _dispWindowEvents_WindowActivatedEventHandler(AddressOf OnIdeEvent)
        AddHandler _documentEvents.DocumentOpened, New _dispDocumentEvents_DocumentOpenedEventHandler(AddressOf OnIdeEvent)
        AddHandler _documentEvents.DocumentClosing, New _dispDocumentEvents_DocumentClosingEventHandler(AddressOf OnIdeEvent)
    End Sub

    Private Sub OnIdeEvent(gotfocus As Window, lostfocus As Window)
        OnIdeEvent()
    End Sub

    Private Sub OnIdeEvent(document As Document)
        OnIdeEvent()
    End Sub

    Private Sub OnIdeEvent(window As Window)
        OnIdeEvent()
    End Sub

    Private Sub OnIdeEvent(oldname As String)
        OnIdeEvent()
    End Sub

    Private Sub OnIdeEvent(reason As dbgEventReason)
        OnIdeEvent()
    End Sub

    Private Sub OnIdeEvent(reason As dbgEventReason, ByRef executionaction As dbgExecutionAction)
        OnIdeEvent()
    End Sub

    Private Sub OnIdeEvent(newProc As EnvDTE.Process, newProg As EnvDTE.Program, newThread As EnvDTE.Thread, newStkFrame As EnvDTE.StackFrame)
        OnIdeEvent()
    End Sub

    Private Sub OnIdeEvent()
        If (Me.Settings.EnableDebugMode) Then
            WriteOutput("Debugger context changed. Updating title.")
        End If
        Me.UpdateWindowTitleAsync(Me, EventArgs.Empty)
    End Sub
    ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    ' Overriden Package Implementation
#Region "Package Members"
    ''' <summary>
    ''' Initialization of the package; this method is called right after the package is sited, so this is the place
    ''' where you can put all the initilaization code that rely on services provided by VisualStudio.
    ''' </summary>
    Protected Overrides Sub Initialize()
        MyBase.Initialize()
        DoInitialize()
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        Me.ResetTitleTimer.Dispose()
        MyBase.Dispose(disposing:=disposing)
    End Sub

#End Region

    Private Sub DoInitialize()
        'Every 5 seconds, we check the window titles in case we missed an event.
        Me.ResetTitleTimer = New System.Windows.Forms.Timer() With {.Interval = 5000}
        AddHandler Me.ResetTitleTimer.Tick, AddressOf Me.UpdateWindowTitleAsync
        'Dim assemblyFilename As FileInfo
        'If IsVisualStudio2010 Then
        '    assemblyFilename = New FileInfo(Path.Combine(Path.GetDirectoryName(Me.GetType().Assembly.Location), "RenameVSWindowTitle.v10.dll"))
        '    VersionSpecificAssembly = Assembly.LoadFrom(assemblyFilename.FullName)
        'ElseIf IsVisualStudio2012 Then
        '    assemblyFilename = New FileInfo(Path.Combine(Path.GetDirectoryName(Me.GetType().Assembly.Location), "RenameVSWindowTitle.v11.dll"))
        '    VersionSpecificAssembly = Assembly.LoadFrom(assemblyFilename.FullName)
        'ElseIf IsVisualStudio2013 Then
        '    assemblyFilename = New FileInfo(Path.Combine(Path.GetDirectoryName(Me.GetType().Assembly.Location), "RenameVSWindowTitle.v12.dll"))
        '    VersionSpecificAssembly = Assembly.LoadFrom(assemblyFilename.FullName)
        'End If
        Me.ResetTitleTimer.Start()
    End Sub

    Private ReadOnly Property Settings As OptionPageGrid
        Get
            Return CType(GetDialogPage(GetType(OptionPageGrid)), OptionPageGrid)
        End Get
    End Property

    Private Function GetIDEName(str As String) As String
        Try
            Dim m = New Regex("^(.*) - (" + Me.DTE.Name + ".*) " + Regex.Escape(Me.Settings.AppendedString) + "$", RegexOptions.RightToLeft).Match(str)
            If (Not m.Success) Then m = New Regex("^(.*) - (" + Me.DTE.Name + ".* \(.+\)) \(.+\)$", RegexOptions.RightToLeft).Match(str)
            If (Not m.Success) Then m = New Regex("^(.*) - (" + Me.DTE.Name + ".*)$", RegexOptions.RightToLeft).Match(str)
            If (Not m.Success) Then m = New Regex("^(" + Me.DTE.Name + ".*)$", RegexOptions.RightToLeft).Match(str)
            If (m.Success) AndAlso m.Groups.Count >= 2 Then
                If (m.Groups.Count >= 3) Then
                    Return m.Groups(2).Captures(0).Value
                ElseIf (m.Groups.Count >= 2) Then
                    Return m.Groups(1).Captures(0).Value
                End If
            Else
                If (Me.Settings.EnableDebugMode) Then WriteOutput("IDE name (" + Me.DTE.Name + ") not found: " & str & ".")
                Return Nothing
            End If
        Catch ex As Exception
            If (Me.Settings.EnableDebugMode) Then _
                WriteOutput("GetIDEName Exception: " & str & ". Details: " + ex.ToString())
            Return Nothing
        End Try
    End Function

    Private Function GetVSSolutionName(str As String) As String
        Try
            Dim m = New Regex("^(.*)\\(.*) - (" + Me.DTE.Name + ".*) " + Regex.Escape(Me.Settings.AppendedString) + "$", RegexOptions.RightToLeft).Match(str)
            If (m.Success) AndAlso m.Groups.Count >= 4 Then
                Dim name = m.Groups(2).Captures(0).Value
                Dim state = GetVSState(str)
                Return name.Substring(0, name.Length - If(String.IsNullOrEmpty(state), 0, state.Length + 3))
            Else
                m = New Regex("^(.*) - (" + Me.DTE.Name + ".*) " + Regex.Escape(Me.Settings.AppendedString) + "$", RegexOptions.RightToLeft).Match(str)
                If (m.Success) AndAlso m.Groups.Count >= 3 Then
                    Dim name = m.Groups(1).Captures(0).Value
                    Dim state = GetVSState(str)
                    Return name.Substring(0, name.Length - If(String.IsNullOrEmpty(state), 0, state.Length + 3))
                Else
                    m = New Regex("^(.*) - (" + Me.DTE.Name + ".*)$", RegexOptions.RightToLeft).Match(str)
                    If (m.Success) AndAlso m.Groups.Count >= 3 Then
                        Dim name = m.Groups(1).Captures(0).Value
                        Dim state = GetVSState(str)
                        Return name.Substring(0, name.Length - If(String.IsNullOrEmpty(state), 0, state.Length + 3))
                    Else
                        If (Me.Settings.EnableDebugMode) Then WriteOutput("VSName not found: " & str & ".")
                        Return Nothing
                    End If
                End If
            End If
        Catch ex As Exception
            If (Me.Settings.EnableDebugMode) Then _
                WriteOutput("GetVSName Exception: " & str & ". Details: " + ex.ToString())
            Return Nothing
        End Try
    End Function

    Private Function GetVSState(str As String) As String
        Try
            Dim m = New Regex(" \((.*)\) - (" + Me.DTE.Name + ".*) " + Regex.Escape(Me.Settings.AppendedString) + "$", RegexOptions.RightToLeft).Match(str)
            If (Not m.Success) Then m = New Regex(" \((.*)\) - (" + Me.DTE.Name + ".*)$", RegexOptions.RightToLeft).Match(str)
            If (m.Success) AndAlso m.Groups.Count >= 3 Then
                Return m.Groups(1).Captures(0).Value
            Else
                If (Me.Settings.EnableDebugMode) Then WriteOutput("VSState not found: " & str & ".")
                Return Nothing
            End If
        Catch ex As Exception
            If (Me.Settings.EnableDebugMode) Then _
                WriteOutput("GetVSState Exception: " & str & ". Details: " + ex.ToString())
            Return Nothing
        End Try
    End Function

    Private Sub UpdateWindowTitleAsync(state As Object, e As EventArgs)
        If (Me.IDEName Is Nothing AndAlso Me.DTE.MainWindow IsNot Nothing) Then
            Me.IDEName = GetIDEName(Me.DTE.MainWindow.Caption)
        End If
        If (Me.IDEName Is Nothing) Then Return
        Tasks.Task.Factory.StartNew(AddressOf UpdateWindowTitle)
    End Sub

    Private ReadOnly UpdateWindowTitleLock As Object = New Object()

    Private Sub UpdateWindowTitle()
        If (Not Monitor.TryEnter(UpdateWindowTitleLock)) Then Return
        Try
            Dim currentInstance = Diagnostics.Process.GetCurrentProcess()
            Dim rewrite = False
            If Me.Settings.AlwaysRewriteTitles Then
                rewrite = True
            Else
                Dim vsInstances As Diagnostics.Process() = Diagnostics.Process.GetProcessesByName("devenv")
                If vsInstances.Count >= Me.Settings.MinNumberOfInstances Then
                    'Check if multiple instances of devenv have identical original names. If so, then rewrite the title of current instance (normally the extension will run on each instance so no need to rewrite them as well). Otherwise do not rewrite the title.
                    'The best would be to get the EnvDTE.DTE object of the other instances, and compare the solution or project names directly instead of relying on window titles (which may be hacked by third party software as well).
                    Dim currentInstanceName = Path.GetFileNameWithoutExtension(Me.DTE.Solution.FullName)
                    If String.IsNullOrEmpty(currentInstanceName) Then
                        rewrite = True
                    ElseIf (From vsInstance In vsInstances Where vsInstance.Id <> currentInstance.Id
                            Select GetVSSolutionName(vsInstance.MainWindowTitle())).Any(Function(vsInstanceName) vsInstanceName IsNot Nothing AndAlso currentInstanceName = vsInstanceName) Then
                        rewrite = True
                    End If
                End If
            End If
            Dim pattern As String
            Dim solution = Me.DTE.Solution
            If (solution Is Nothing OrElse solution.FullName = String.Empty) Then
                Dim document = Me.DTE.ActiveDocument
                Dim window = Me.DTE.ActiveWindow
                If ((document Is Nothing OrElse String.IsNullOrEmpty(document.FullName)) AndAlso (window Is Nothing OrElse String.IsNullOrEmpty(window.Caption))) Then
                    pattern = If(rewrite, Me.Settings.PatternIfNothingOpen, "[ideName]")
                Else
                    pattern = If(rewrite, Me.Settings.PatternIfDocumentButNoSolutionOpen, "[documentName] - [ideName]")
                End If
            Else
                If (Me.DTE.Debugger Is Nothing OrElse Me.DTE.Debugger.CurrentMode = dbgDebugMode.dbgDesignMode) Then
                    pattern = If(rewrite, Me.Settings.PatternIfDesignMode, "[solutionName] - [ideName]")
                ElseIf (Me.DTE.Debugger.CurrentMode = dbgDebugMode.dbgBreakMode) Then
                    pattern = If(rewrite, Me.Settings.PatternIfBreakMode, "[solutionName] (Debugging) - [ideName]")
                ElseIf (Me.DTE.Debugger.CurrentMode = dbgDebugMode.dbgRunMode) Then
                    pattern = If(rewrite, Me.Settings.PatternIfRunningMode, "[solutionName] (Running) - [ideName]")
                Else
                    Throw New Exception("No matching state found")
                End If
            End If
            Me.ChangeWindowTitle(GetNewTitle(pattern:=pattern))
        Catch ex As Exception
            Try
                If (Me.Settings.EnableDebugMode) Then WriteOutput("UpdateWindowTitle exception: " + ex.ToString())
            Catch
            End Try
        Finally
            Monitor.Exit(UpdateWindowTitleLock)
        End Try
    End Sub

    Private Function GetNewTitle(pattern As String) As String
        Dim solution = Me.DTE.Solution
        Dim parentPath = ""
        Dim documentName = ""
        Dim solutionName = ""
        Dim document = Me.DTE.ActiveDocument
        If (document IsNot Nothing) Then
            documentName = Path.GetFileName(document.FullName)
            If (solution Is Nothing OrElse String.IsNullOrEmpty(solution.FullName)) Then
                Dim parents = Path.GetDirectoryName(document.FullName).Split(Path.DirectorySeparatorChar).Reverse().ToArray()
                parentPath = GetParentPath(parents:=parents)
                pattern = ReplaceParentTags(pattern:=pattern, parents:=parents)
            End If
        Else
            Dim window = Me.DTE.ActiveWindow
            If (window IsNot Nothing AndAlso window.Caption <> Me.DTE.MainWindow.Caption) Then
                documentName = window.Caption
            ElseIf (solution Is Nothing OrElse String.IsNullOrEmpty(solution.FullName)) Then
                Return Me.IDEName
            End If
        End If
        If (solution IsNot Nothing AndAlso Not String.IsNullOrEmpty(solution.FullName)) Then
            If (pattern.Contains("[projectName]")) Then
                Dim project = GetActiveProject(Me.DTE)
                If (project IsNot Nothing) Then
                    pattern = pattern.Replace("[projectName]", project.Name)
                End If
            End If
            solutionName = Path.GetFileNameWithoutExtension(solution.FullName)
            Dim parents = Path.GetDirectoryName(Me.DTE.Solution.FullName).Split(Path.DirectorySeparatorChar).Reverse().ToArray()
            parentPath = GetParentPath(parents:=parents)
            pattern = ReplaceParentTags(pattern:=pattern, parents:=parents)
            Dim activeConfig = DTE.Solution.Properties.Item("ActiveConfig").Value
            If (activeConfig IsNot Nothing) Then
                Dim activeConfiguration = activeConfig.ToString().Split(CType("|", Char()))
                If (activeConfiguration.Length = 2) Then
                    Dim configurationName = activeConfiguration(0)
                    Dim platformName = activeConfiguration(1)
                    pattern = pattern.Replace("[configurationName]", configurationName) _
                                     .Replace("[platformName]", platformName)
                End If
            End If
            If (pattern.Contains("[gitBranchName]")) Then
                Try
                    Dim workingDirectory = New FileInfo(solution.FullName).DirectoryName
                    If (Not IsGitRepository(workingDirectory)) Then
                        pattern = pattern.Replace("[gitBranchName]", "")
                    Else
                        pattern = pattern.Replace("[gitBranchName]", GetGitBranch(workingDirectory))
                    End If
                Catch ex As Exception
                    If (Me.Settings.EnableDebugMode) Then WriteOutput("[gitBranchName] Exception: " + ex.ToString())
                End Try
            End If
        End If
        If (pattern.Contains("[workspaceName]")) Then
            Try
                'Dim catalog = New AssemblyCatalog(Me.VersionSpecificAssembly)
                'Dim container = New CompositionContainer(catalog)
                If GetVsMajorVersion() >= 10 Then
                    Dim vce As Object = Me.DTE.GetObject("Microsoft.VisualStudio.TeamFoundation.VersionControl.VersionControlExt") ' , VersionControlExt)
                    If (vce IsNot Nothing AndAlso vce.SolutionWorkspace IsNot Nothing) Then
                        pattern = pattern.Replace("[workspaceName]", vce.SolutionWorkspace.Name)
                    End If
                Else
                    Dim wst As Object = Me.DTE.GetObject("Microsoft.TeamFoundation.VersionControl.Client.Workstation") ' , VersionControlExt)
                    If (wst IsNot Nothing) Then
                        Dim ws = wst.Current.GetLocalWorkspaceInfo()
                        If (ws IsNot Nothing) Then
                            pattern = pattern.Replace("[workspaceName]", ws.Name)
                        End If
                    Else
                    End If
                End If
                pattern = pattern.Replace("[workspaceName]", "")
            Catch ex As Exception
                If (Me.Settings.EnableDebugMode) Then WriteOutput("[workspaceName] Exception: " + ex.ToString())
            End Try
        End If
        Dim vsMajorVersion = GetVsMajorVersion()
        Dim vsMajorVersionYear = GetYearFromVsMajorVersion(vsMajorVersion)
        Return pattern.Replace("[documentName]", documentName) _
                      .Replace("[solutionName]", solutionName) _
                      .Replace("[vsMajorVersion]", vsMajorVersion) _
                      .Replace("[vsMajorVersionYear]", vsMajorVersionYear) _
                      .Replace("[parentPath]", parentPath).Replace("[ideName]", Me.IDEName) + " " + Me.Settings.AppendedString
    End Function

    Private Function GetParentPath(parents As String()) As String
        'TODO: handle drive letter better if (path1.Substring(path1.Length - 1, 1) == ":") path1 += System.IO.Path.DirectorySeparatorChar; http://stackoverflow.com/questions/1527942/why-path-combine-doesnt-add-the-path-directoryseparatorchar-after-the-drive-des?rq=1
        Return Path.Combine(parents.Skip(Me.Settings.ClosestParentDepth - 1).Take(Me.Settings.FarthestParentDepth - Me.Settings.ClosestParentDepth + 1).Reverse().ToArray())
    End Function

    Private Function ReplaceParentTags(pattern As String, parents As String()) As String
        Dim matches = New Regex("\[parent([0-9]+)\]").Matches(pattern)
        For Each m As Match In matches
            If (Not m.Success) Then Continue For
            Dim depth = Integer.Parse(m.Groups(1).Captures(0).Value)
            If (depth <= parents.Length) Then
                pattern = pattern.Replace("[parent" + depth.ToString(Globalization.CultureInfo.InvariantCulture) + "]", parents(depth))
            End If
        Next
        Return pattern
    End Function

    Private Sub ChangeWindowTitle(title As String)
        Try
            Dim dispatcher = System.Windows.Application.Current.Dispatcher
            If (dispatcher IsNot Nothing) Then
                dispatcher.BeginInvoke((Sub()
                                            Try
                                                System.Windows.Application.Current.MainWindow.Title = Me.DTE.MainWindow.Caption
                                                If (System.Windows.Application.Current.MainWindow.Title <> title) Then
                                                    System.Windows.Application.Current.MainWindow.Title = title
                                                End If
                                            Catch
                                            End Try
                                        End Sub))
            End If
        Catch ex As Exception
            If (Me.Settings.EnableDebugMode) Then WriteOutput("SetMainWindowTitle Exception: " + ex.ToString())
        End Try
    End Sub

    Private Shared Sub WriteOutput(str As String)
        Try
            Dim outWindow = TryCast(GetGlobalService(GetType(SVsOutputWindow)), IVsOutputWindow)
            Dim generalPaneGuid As Guid = VSConstants.OutputWindowPaneGuid.DebugPane_guid
            ' P.S. There's also the VSConstants.GUID_OutWindowDebugPane available.
            Dim generalPane As IVsOutputWindowPane = Nothing
            outWindow.GetPane(generalPaneGuid, generalPane)
            generalPane.OutputString("RenameVSWindowTitle: " & str & vbNewLine)
            generalPane.Activate()
        Catch
        End Try
    End Sub

    Private Shared Function GetWindowTitle(hWnd As IntPtr) As String
        Const nChars = 256
        Dim buff As New StringBuilder(nChars)
        If GetWindowText(hWnd, buff, nChars) > 0 Then
            Return buff.ToString()
        End If
        Return Nothing
    End Function

    Private Shared Function GetGitBranch(workingDirectory As String) As String
        'Create process
        'Dim pProcess = New ProcessStartInfo("git.exe")
        Dim pProcess As New Diagnostics.Process()

        'strCommand is path and file name of command to run
        pProcess.StartInfo.FileName = "git.exe"

        'strCommandParameters are parameters to pass to program
        pProcess.StartInfo.Arguments = "symbolic-ref --short -q HEAD" 'As per: http://git-blame.blogspot.sg/2013/06/checking-current-branch-programatically.html. Or: "rev-parse --abbrev-ref HEAD"

        pProcess.StartInfo.UseShellExecute = False

        'Set output of program to be written to process output stream
        pProcess.StartInfo.RedirectStandardOutput = True
        pProcess.StartInfo.CreateNoWindow = True

        'Optional
        pProcess.StartInfo.WorkingDirectory = workingDirectory

        'Start the process
        pProcess.Start()

        'Get program output
        Dim branchName As String = pProcess.StandardOutput.ReadToEnd().TrimEnd(" ", vbLf)

        'Wait for process to finish
        pProcess.WaitForExit()

        Return branchName
    End Function

    Private Shared Function IsGitRepository(workingDirectory As String) As Boolean
        'Create process
        'Dim pProcess = New ProcessStartInfo("git.exe")
        Dim pProcess As New Diagnostics.Process()

        'strCommand is path and file name of command to run
        pProcess.StartInfo.FileName = "git.exe"

        'strCommandParameters are parameters to pass to program
        pProcess.StartInfo.Arguments = "rev-parse --is-inside-work-tree"

        pProcess.StartInfo.UseShellExecute = False

        'Set output of program to be written to process output stream
        pProcess.StartInfo.RedirectStandardOutput = True
        pProcess.StartInfo.CreateNoWindow = True

        'Optional
        pProcess.StartInfo.WorkingDirectory = workingDirectory

        'Start the process
        pProcess.Start()

        'Get program output
        Dim res As String = pProcess.StandardOutput.ReadToEnd().TrimEnd(" ", vbLf)

        'Wait for process to finish
        pProcess.WaitForExit()

        Return res = "true"
    End Function

    Private Shared Function GetActiveProject(dte As DTE2) As Project
        Dim activeProject As Project = Nothing
        Try
            If (dte.ActiveSolutionProjects IsNot Nothing) Then
                Dim activeSolutionProjects = TryCast(dte.ActiveSolutionProjects, Array)
                If activeSolutionProjects IsNot Nothing AndAlso activeSolutionProjects.Length > 0 Then
                    activeProject = TryCast(activeSolutionProjects.GetValue(0), Project)
                End If
            End If
            Return activeProject
        Catch
            Return Nothing
        End Try
    End Function

    <DllImport("user32.dll")>
    Private Shared Function GetWindowText(hWnd As IntPtr, text As StringBuilder, count As Integer) As Integer
    End Function

    Private MaxVsVersion As Integer = 15

    Protected ReadOnly Property IsVisualStudio2010() As Boolean
        Get
            Return GetVsMajorVersion() = 10
        End Get
    End Property

    Protected ReadOnly Property IsVisualStudio2012() As Boolean
        Get
            Return GetVsMajorVersion() = 11
        End Get
    End Property

    Protected ReadOnly Property IsVisualStudio2013() As Boolean
        Get
            Return GetVsMajorVersion() = 12
        End Get
    End Property

    Protected ReadOnly Property IsVisualStudio2015() As Boolean
        Get
            Return GetVsMajorVersion() = 14
        End Get
    End Property

    Private Function GetYearFromVsMajorVersion(version As Integer) As Integer
        Select Case version
            Case 9
                Return 2008
            Case 10
                Return 2010
            Case 11
                Return 2012
            Case 12
                Return 2013
            Case 14
                Return 2015
            Case Else
                Return version
        End Select
    End Function

    Private Function GetVsMajorVersion() As Integer
        If (MaxVsVersion < 15) Then
            Return MaxVsVersion
        End If
        Dim vsVersion As String = Me.DTE.Version
        Dim version__1 As Version
        If Version.TryParse(vsVersion, version__1) Then
            MaxVsVersion = version__1.Major
        Else
            MaxVsVersion = 12
        End If
        Return MaxVsVersion
    End Function
    '<DllImport("user32.dll")>
    'Private Shared Function GetShellWindow() As IntPtr
    'End Function

    'We could use the following to determine if user is an administrator
    '<DllImport("user32.dll", EntryPoint:="IsUserAnAdmin")>
    'Private Shared Function IsUserAnAdministrator() As Boolean
    'End Function
End Class