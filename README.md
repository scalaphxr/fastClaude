# fastClaude

Открывайте любую папку в [Claude Code](https://claude.com/claude-code) в один клик: правая кнопка мыши → **«Открыть с помощью Claude»**. Откроется cmd от имени администратора, и Claude Code запустится сразу в этой папке.

[English version below](#english)

## Установка

**[⬇ Скачать fastClaude-Setup.exe](https://github.com/scalaphxr/fastClaude/releases/latest/download/fastClaude-Setup.exe)**

1. Запустите скачанный файл.
2. Если появится окно «Windows защитила ваш компьютер», нажмите **Подробнее → Выполнить в любом случае**. Оно появляется потому, что файл не подписан платным сертификатом.
3. Нажмите **Установить**.

Права администратора для установки не нужны. Перезагрузка тоже не нужна.

Claude Code должен быть уже [установлен](https://claude.com/claude-code). Подойдут Windows 10 и 11, дополнительно ничего ставить не надо.

## Как пользоваться

Нажмите правой кнопкой на папку, на диск или на пустое место внутри папки и выберите **«Открыть с помощью Claude»**. Подтвердите запрос Windows на права администратора.

В Windows 11 пункт находится в **«Показать дополнительные параметры»**. Чтобы он был виден сразу, отметьте при установке галочку «Вернуть классическое меню Windows». Она меняет вид меню правого клика для всех программ, а при удалении fastClaude меню вернётся как было.

## Важно

Claude Code будет работать с правами администратора, то есть сможет менять любые файлы и настройки системы. Не открывайте так папки, которым не доверяете: файлы из них могут влиять на поведение Claude.

## Удаление

**Параметры → Приложения → Установленные приложения → fastClaude → Удалить**.

## Сборка из исходников

```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
```

Весь код находится в [`fastClaude.cs`](fastClaude.cs). Готовый файл появится в `dist\`.

fastClaude — неофициальный проект, он не связан с Anthropic.

---

## English

Open any folder in [Claude Code](https://claude.com/claude-code) with one click: right-click → **Open with Claude**. An administrator cmd window opens with Claude Code already running in that folder.

### Install

**[⬇ Download fastClaude-Setup.exe](https://github.com/scalaphxr/fastClaude/releases/latest/download/fastClaude-Setup.exe)**

1. Run the downloaded file.
2. If you see "Windows protected your PC", click **More info → Run anyway**. It appears because the file isn't signed with a paid certificate.
3. Click **Install**.

No administrator rights or restart needed for setup. Claude Code must already be [installed](https://claude.com/claude-code). Works on Windows 10 and 11 with nothing else to install.

### Use

Right-click a folder, a drive, or empty space inside a folder, choose **Open with Claude**, and approve the Windows administrator prompt.

On Windows 11 the item is under **Show more options**. To see it right away, tick "Restore the classic Windows menu" during setup. This changes the right-click menu for all apps, and removing fastClaude puts it back.

### Important

Claude Code runs with administrator rights, so it can change any file or system setting. Don't open folders you don't trust this way: their files can influence what Claude does.

### Remove

**Settings → Apps → Installed apps → fastClaude → Uninstall**.

### Build from source

```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
```

All the code is in [`fastClaude.cs`](fastClaude.cs). The result appears in `dist\`.

fastClaude is an unofficial project and isn't affiliated with Anthropic.
