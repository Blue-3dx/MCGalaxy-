@echo off
setlocal

:: Set the C# compiler (32-bit)
set CSC="C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"

:: Output GUI EXE
set OUT=MCGalaxyGUI.exe

:: Move to project root
cd /d D:\MCGalaxy-1.9.5.1

echo ===========C                   P                   !
echo ==OO===OO==  o               !   l               t
echo ==OO===OO==    m           g       e           i
echo ===========      p       n           a       a
echo ===O===O===        i   i               s   W
echo ====OOO====          l                   e
echo Made By Blue 3dx

%CSC% /nologo /optimize+ /unsafe /target:winexe /platform:x86 /out:%OUT% ^
    /define:TEN_BIT_BLOCKS ^
    /recurse:GUI\*.cs ^
    /recurse:MCGalaxy\*.cs ^
    /reference:System.dll ^
    /reference:System.Core.dll ^
    /reference:System.Data.dll ^
    /reference:System.Xml.dll ^
    /reference:System.Drawing.dll ^
    /reference:MySql.Data.dll ^
    /reference:Newtonsoft.Json.dll ^
    /reference:System.Data.SQLite.dll

if exist %OUT% (
    echo.
    echo [✓] Build successful: %OUT%
) else (
    echo.
    echo [X] Build failed!
)

pause