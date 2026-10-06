@echo off
cd /d "%~dp0"

echo Iniciando a API do desafio...
cd Codigo_Projeto_Visual_Studio\desafio_dev\bin\Release\net10.0\win-x64\publish

start /min programa_desafio.exe

timeout /t 2 /nobreak >nul

echo Abrindo o painel web...

start "" "%~dp0Codigo_Projeto_Visual_Studio\desafio_dev\index.html"

exit