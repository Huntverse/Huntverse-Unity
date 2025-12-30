@echo off
echo [Build Protobuf]

if exist .\generated\bin (
	rmdir /s /q ".\generated\bin"
)
mkdir generated\bin
call CSV_SerializeData.exe

:: deploy bin files for server/client
[Build End Protobuf]