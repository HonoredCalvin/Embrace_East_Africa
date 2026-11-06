@echo off
rem Builds and starts Embrace EA without any IDE (uses the .NET Framework compiler built into Windows).
set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
if not exist bin\Debug mkdir bin\Debug
"%CSC%" /nologo /platform:x86 /target:winexe /out:bin\Debug\EmbraceEA.exe /r:System.dll /r:System.Data.dll /r:System.Xml.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll Program.cs MainForm.cs Db.cs
if errorlevel 1 pause & exit /b
xcopy /E /I /Y /Q Images bin\Debug\Images >nul
start "" bin\Debug\EmbraceEA.exe
