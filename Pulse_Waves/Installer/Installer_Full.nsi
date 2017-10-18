;Pulse Waves NSIS Installer
;Install Pulse Waves
;Written by Rico Castelo

; HISTORY
; -----------------------------------------------------------------
; Date            Initials    Version    Comments
; -----------------------------------------------------------------
; 01/07/2015      RC          0.0.1      Initial installer.
; 02/19/2015      RC          0.0.2      Initial installer.
; 10/16/2015      RC          0.0.3      Initial installer.
; 11/05/2015      RC          1.0.0      Initial installer.  Added Sqlite and Pulse_Display.
; 11/05/2015      RC          1.1.0      Change version.
;

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; Include Modern UI
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

  !include "MUI2.nsh"

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; General
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
	;Name and file
	Name "Rowe Technology Inc. - Pulse Waves"
	OutFile "Pulse.Waves.Installer.v.1.2.1.Full.exe"

	;Default installation folder
	InstallDir "$PROGRAMFILES\Rowe Technology Inc\Pulse Waves"
  
	;Get installation folder from registry if available
	InstallDirRegKey HKCU "Software\Rowe Technology Inc - Pulse Waves" ""

	;Request application privileges for Windows Vista
	RequestExecutionLevel admin


;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; Variables
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

Var /GLOBAL VERSION_NUM
Var /GLOBAL VERSION_MAJOR
Var /GLOBAL VERSION_MINOR

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; Interface Settings
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
	!define MUI_ABORTWARNING
	!define MUI_ICON "${NSISDIR}\Contrib\Graphics\Icons\modern-install.ico"

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; Pages
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
	!insertmacro MUI_PAGE_LICENSE "License.txt"
	!insertmacro MUI_PAGE_COMPONENTS
	!insertmacro MUI_PAGE_DIRECTORY
	!insertmacro MUI_PAGE_INSTFILES
  
	!insertmacro MUI_UNPAGE_CONFIRM
	!insertmacro MUI_UNPAGE_INSTFILES
  
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; Languages
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
	!insertmacro MUI_LANGUAGE "English"

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; Installer Sections
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; Install Main Application and all DLL
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
Section "Core" SecCore

	StrCpy $VERSION_NUM "1.2.0" 
	StrCpy $VERSION_MAJOR "1"
	StrCpy $VERSION_MINOR "2"

	SetOutPath $INSTDIR

	; Add Files
	DetailPrint "Installing Pulse Waves."
	CreateDirectory "$INSTDIR\x64"
	CreateDirectory "$INSTDIR\x86"
	File "..\bin\Release\Pulse_Waves.exe"
	File "..\bin\Release\Pulse_Display.dll"
	File "..\bin\Release\RTI.dll"
	File "/oname=x64\SQLite.Interop.dll" "..\..\packages\System.Data.SQLite.Core.1.0.99.0\build\net451\x64\SQLite.Interop.dll"
	File "/oname=x86\SQLite.Interop.dll" "..\..\packages\System.Data.SQLite.Core.1.0.99.0\build\net451\x86\SQLite.Interop.dll"
	File "..\bin\Release\Licenses.txt"
	File "..\bin\Release\EndUserRights.txt"
	File "..\..\User Guide\RTI - Pulse Waves User Guide.pdf"
	
	; Create shortcut in start menu
	CreateDirectory "$SMPROGRAMS\Pulse Waves"
	CreateShortCut "$SMPROGRAMS\Pulse Waves\Pulse Waves.lnk" "$INSTDIR\Pulse_Waves.exe"
	CreateShortCut "$SMPROGRAMS\Pulse Waves\Uninstall.lnk" "$INSTDIR\uninstall.exe"
	CreateShortCut "$SMPROGRAMS\Pulse Waves\Pulse Waves User Guide.lnk" "$INSTDIR\RTI - Pulse Waves User Guide.pdf"
	
	; Store installation folder
	WriteRegStr HKCU "Software\Rowe Technology Inc - Pulse Waves" "" $INSTDIR
	WriteRegStr HKCU "Software\Rowe Technology Inc - Pulse Waves" "Version" "$VERSION_MAJOR.$VERSION_MINOR"

	; Create uninstaller
	WriteUninstaller "$INSTDIR\Uninstall.exe"
	
	; Add to Add/Remove
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Pulse" \
				 "DisplayName" "Rowe Technology Inc - Pulse Waves"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Pulse" \
				 "UninstallString" "$\"$INSTDIR\uninstall.exe$\""

	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Pulse" \
				 "Publisher" "Rowe Technology Inc."

	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Pulse" \
				 "DisplayVersion" $VERSION_NUM

	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Pulse" \
				 "VersionMajor" $VERSION_MAJOR

	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Pulse" \
				 "VersionMinor" $VERSION_MINOR

SectionEnd

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; .Net 4.5.1 Full
; .Net 4.5.1 Installer instructions: http://msdn.microsoft.com/library/ee942965%28v=VS.100%29.aspx
; .Net 4.5.1 Installer commandline options: http://msdn.microsoft.com/library/ee942965%28v=VS.100%29.aspx#command_line_options
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
Section ".Net 4.5.1 Full" SecDotNet45Full

	SetOutPath "$INSTDIR"
 
    ; Magic numbers from http://msdn.microsoft.com/en-us/library/ee942965.aspx
    ClearErrors
    ReadRegDWORD $0 HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" "Release"

    IfErrors NotDetected

    ${If} $0 >= 378389
        DetailPrint "Microsoft .NET Framework 4.5 is installed ($0)"
    ${Else}
    NotDetected:
        DetailPrint "Installing Microsoft .NET Framework 4.5"
        SetDetailsPrint listonly
		File "..\..\packages\DotNet4.5.1\dotNetFx4-5-1-Full-x86-x64-AllOS-ENU.exe"
		ExecWait '"$INSTDIR\Tools\dotNetFx45_Full_setup.exe" /passive /norestart /showfinalerror' $0
		Goto endDotNet40

		endDotNet40:
			Delete "$INSTDIR\dotNetFx4-5-1-Full-x86-x64-AllOS-ENU.exe"

        ${If} $0 == 3010 
        ${OrIf} $0 == 1641
            DetailPrint "Microsoft .NET Framework 4.5 installer requested reboot"
            SetRebootFlag true
        ${EndIf}
        SetDetailsPrint lastused
        DetailPrint "Microsoft .NET Framework 4.5 installer returned $0"
    ${EndIf}

SectionEnd

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; Descriptions
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; Language strings
LangString DESC_SecCore ${LANG_ENGLISH} "Core files."
LangString DESC_SecDotNet ${LANG_ENGLISH} "Install .Net 4.5.1 Full."

; Assign language strings to sections
!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
	!insertmacro MUI_DESCRIPTION_TEXT ${SecCore} $(DESC_SecCore)
	!insertmacro MUI_DESCRIPTION_TEXT ${SecDotNet45Full} $(DESC_SecDotNet)
!insertmacro MUI_FUNCTION_DESCRIPTION_END

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; Uninstaller Section
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
Section "Uninstall"

	Delete "$INSTDIR\Pulse_Waves.exe"
	Delete "$INSTDIR\RTI.dll"
	Delete "$INSTDIR\Pulse_Display.dll"
	Delete "$INSTDIR\x64\SQLite.Interop.dll"
	Delete "$INSTDIR\x86\SQLite.Interop.dll"
    Delete "$INSTDIR\Licenses.txt"
	Delete "$INSTDIR\EndUserRights.txt"
	Delete "$INSTDIR\RTI - Pulse Waves User Guide.pdf"

	Delete "$INSTDIR\Uninstall.exe"

	; Remove the install directory
	RMDir "$INSTDIR\x64"
	RMDir "$INSTDIR\x86"
	RMDir "$INSTDIR"
	RMDir "$PROGRAMFILES\Rowe Technology Inc\Pulse"
	RMDir "$PROGRAMFILES\Rowe Technology Inc"

	; Remove the program data
	Delete "C:\ProgramData\RTI\Pulse_Waves\PulseErrorLog.log" 
	Delete "C:\ProgramData\RTI\Pulse_Waves\PulseOptions.json"
	RMDir "C:\ProgramData\RTI\Pulse_Waves"
	RMDir "C:\ProgramData\RTI"

	; Remove registry key for SQLite
	;DeleteRegKey HKCU "Software\System.Data.SQLite"

	; Remove registery key
	DeleteRegKey /ifempty HKCU "Software\Rowe Technology Inc - Pulse Waves"
	
	; Remove Uninstall 
	DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Pulse"

	; Remove menu short cuts
	Delete "$SMPROGRAMS\Pulse Waves\Pulse Waves.lnk"
	Delete "$SMPROGRAMS\Pulse Waves\Uninstall.lnk"
	Delete "$SMPROGRAMS\Pulse Waves\Pulse Waves User Guide.lnk"
	RMDir "$SMPROGRAMS\Pulse Waves"

SectionEnd