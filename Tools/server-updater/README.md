# Автообновление Windows-сервера

Скрипт раз в минуту смотрит релиз `latest` на GitHub. Если там новая сборка, он останавливает службу сервера, обновляет файлы и запускает службу снова. Папки и файлы из `-Keep` (по умолчанию `data` и `server_config.toml`) не перезаписываются.

## Установка (один раз, на сервере, PowerShell от администратора)

1. Положи `update-server.ps1` в `C:\SS14\updater\`.
2. Сервер должен работать как служба. Через NSSM (nssm.cc):
   ```
   nssm install SS14Server "C:\SS14\server\Robust.Server.exe"
   nssm set SS14Server AppDirectory "C:\SS14\server"
   nssm set SS14Server Start SERVICE_AUTO_START
   nssm start SS14Server
   ```
   Если у сервера другой запускаемый файл, укажи его вместо `Robust.Server.exe`.
3. Если репозиторий приватный, создай токен только на чтение (Fine-grained, Contents: Read-only для этого репозитория) и задай его системной переменной:
   ```
   [Environment]::SetEnvironmentVariable("GITHUB_TOKEN", "<токен>", "Machine")
   ```
4. Проверь вручную, потом зарегистрируй задачу:
   ```
   powershell -ExecutionPolicy Bypass -File C:\SS14\updater\update-server.ps1 -ServerDir C:\SS14\server
   ```
   ```
   $a = New-ScheduledTaskAction -Execute "powershell.exe" -Argument "-NoProfile -ExecutionPolicy Bypass -File C:\SS14\updater\update-server.ps1 -ServerDir C:\SS14\server"
   $t = New-ScheduledTaskTrigger -Once -At (Get-Date) -RepetitionInterval (New-TimeSpan -Minutes 1)
   Register-ScheduledTask -TaskName "SS14ServerUpdater" -Action $a -Trigger $t -User "SYSTEM" -RunLevel Highest
   ```

Лог: `C:\SS14\updater\updater.log`. Последняя применённая сборка: `C:\SS14\updater\last-sha.txt`.

## Параметры

`-Repo`, `-Tag`, `-Asset`, `-ServerDir`, `-ServiceName`, `-Keep`, `-Token`.

## Откат

Удали `last-sha.txt` и пересоздай релиз `latest` из нужного коммита (запуск `Release build` вручную из нужной ветки). Скрипт поставит эту сборку.
