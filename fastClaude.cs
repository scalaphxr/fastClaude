using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("fastClaude")]
[assembly: AssemblyProduct("fastClaude")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

// Один exe на всё: без аргументов это установщик, с "open <папка>" - запуск Claude.
static class FastClaude
{
    public const string Version = "1.0.0";
    const string ElevatedFlag = "--relaunched";
    const string MenuKey = "OpenWithClaude";
    const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\fastClaude";
    const string ClassicMenuKey = @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}";
    const string ClassicMenuMarker = "ClassicMenuEnabledByInstaller";
    public const string ClaudeUrl = "https://claude.com/claude-code";

    // Directory - клик по папке, Directory\Background - по пустому месту внутри папки, Drive - по диску.
    static readonly string[] ShellClasses = { "Directory", @"Directory\Background", "Drive" };

    static readonly bool Russian = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ru";
    public static string T(string ru, string en) { return Russian ? ru : en; }

    static string ExePath { get { return Assembly.GetExecutingAssembly().Location; } }
    static string InstallDir { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\fastClaude"); } }
    static string InstalledExe { get { return Path.Combine(InstallDir, "fastClaude.exe"); } }

    [STAThread]
    static int Main(string[] args)
    {
        string mode = args.Length > 0 ? args[0] : "";
        bool silent = Array.IndexOf(args, "--silent") >= 0;

        if (mode == "open")
            return args.Length > 1 ? Open(args[1], Array.IndexOf(args, ElevatedFlag) >= 0) : 1;

        if (mode == "install" && silent)
        {
            Install(false);
            return 0;
        }

        if (mode == "uninstall")
        {
            if (!silent && !Confirm(T("Удалить fastClaude?", "Remove fastClaude?")))
                return 0;
            Uninstall();
            if (!silent)
                Info(T("fastClaude удалён.", "fastClaude has been removed."));
            return 0;
        }

        Application.EnableVisualStyles();
        Application.Run(new SetupForm());
        return 0;
    }

    static int Open(string rawPath, bool alreadyRelaunched)
    {
        // Проводник передаёт корень диска как "C:\", и завершающая \" превращается в кавычку.
        string path = rawPath.Trim('"').TrimEnd('\\');

        // Флаг нужен против бесконечного перезапуска: при выключенном UAC runas может не дать прав.
        if (!alreadyRelaunched && !IsElevated())
        {
            try
            {
                Process.Start(new ProcessStartInfo(ExePath, "open \"" + path + "\" " + ElevatedFlag) { UseShellExecute = true, Verb = "runas" });
            }
            catch (Win32Exception e)
            {
                const int ErrorCancelled = 1223; // пользователь нажал «Нет» в запросе UAC
                if (e.NativeErrorCode != ErrorCancelled)
                    Error(e.Message);
            }
            return 0;
        }

        if (path.Length == 2 && path[1] == ':')
            path += "\\";

        if (!Directory.Exists(path))
        {
            Error(T("Папка недоступна:\n", "Folder is not available:\n") + path);
            return 1;
        }

        // Путь передаётся как рабочая папка, а не в команде: cmd не разбирает & и % в имени.
        Process.Start(new ProcessStartInfo("cmd.exe", "/k claude") { UseShellExecute = false, WorkingDirectory = path });
        return 0;
    }

    static bool IsElevated()
    {
        return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
    }

    public static void Install(bool enableClassicMenu)
    {
        Directory.CreateDirectory(InstallDir);
        if (!string.Equals(Path.GetFullPath(ExePath), Path.GetFullPath(InstalledExe), StringComparison.OrdinalIgnoreCase))
            File.Copy(ExePath, InstalledExe, true);

        string icon = FindOnPath("claude.exe") ?? InstalledExe;
        foreach (string shellClass in ShellClasses)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\" + shellClass + @"\shell\" + MenuKey))
            using (RegistryKey command = key.CreateSubKey("command"))
            {
                key.SetValue("", T("Открыть с помощью Claude", "Open with Claude"));
                key.SetValue("Icon", icon);
                command.SetValue("", "\"" + InstalledExe + "\" open \"%V\"");
            }
        }

        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(UninstallKey))
        {
            key.SetValue("DisplayName", "fastClaude");
            key.SetValue("DisplayVersion", Version);
            key.SetValue("Publisher", "scalaphxr");
            key.SetValue("DisplayIcon", InstalledExe);
            key.SetValue("InstallLocation", InstallDir);
            key.SetValue("UninstallString", "\"" + InstalledExe + "\" uninstall");
            key.SetValue("NoModify", 1, RegistryValueKind.DWord);
            key.SetValue("NoRepair", 1, RegistryValueKind.DWord);

            if (enableClassicMenu && !IsClassicMenuEnabled())
            {
                using (RegistryKey inproc = Registry.CurrentUser.CreateSubKey(ClassicMenuKey + @"\InprocServer32"))
                    inproc.SetValue("", "");
                // Запоминаем, что включили сами: при удалении вернём только своё изменение.
                key.SetValue(ClassicMenuMarker, 1, RegistryValueKind.DWord);
                RestartExplorer();
            }
        }
    }

    public static void Uninstall()
    {
        bool restoreMenu = false;
        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(UninstallKey))
            restoreMenu = key != null && key.GetValue(ClassicMenuMarker) != null;

        foreach (string shellClass in ShellClasses)
            Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\" + shellClass + @"\shell\" + MenuKey, false);
        Registry.CurrentUser.DeleteSubKeyTree(UninstallKey, false);

        if (restoreMenu)
        {
            Registry.CurrentUser.DeleteSubKeyTree(ClassicMenuKey, false);
            RestartExplorer();
        }

        // Запущенный exe не может удалить сам себя, поэтому папку удаляет cmd после нашего выхода.
        Process.Start(new ProcessStartInfo("cmd.exe", "/c ping -n 3 127.0.0.1 >nul & rmdir /s /q \"" + InstallDir + "\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetTempPath()
        });
    }

    public static bool IsInstalled()
    {
        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(UninstallKey))
            return key != null;
    }

    public static bool IsClaudeAvailable()
    {
        return FindOnPath("claude.exe") != null || FindOnPath("claude.cmd") != null;
    }

    public static bool IsWindows11()
    {
        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
        {
            int build;
            return key != null && int.TryParse(key.GetValue("CurrentBuild") as string, out build) && build >= 22000;
        }
    }

    public static bool IsClassicMenuEnabled()
    {
        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(ClassicMenuKey + @"\InprocServer32"))
            return key != null;
    }

    static string FindOnPath(string fileName)
    {
        string dirs = (Environment.GetEnvironmentVariable("PATH") ?? "") + ";" +
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".local\bin");
        foreach (string dir in dirs.Split(';'))
        {
            try
            {
                if (dir.Length > 0 && File.Exists(Path.Combine(dir, fileName)))
                    return Path.Combine(dir, fileName);
            }
            catch (ArgumentException) { } // в PATH бывают некорректные записи
        }
        return null;
    }

    static void RestartExplorer()
    {
        foreach (Process explorer in Process.GetProcessesByName("explorer"))
            explorer.Kill();
        Thread.Sleep(1500);
        // Обычно Windows перезапускает оболочку сама.
        if (Process.GetProcessesByName("explorer").Length == 0)
            Process.Start("explorer.exe");
    }

    public static void Info(string text) { MessageBox.Show(text, "fastClaude", MessageBoxButtons.OK, MessageBoxIcon.Information); }
    public static void Error(string text) { MessageBox.Show(text, "fastClaude", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    public static bool Confirm(string text) { return MessageBox.Show(text, "fastClaude", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes; }
}

class SetupForm : Form
{
    readonly CheckBox classicMenu;

    public SetupForm()
    {
        Text = "fastClaude " + FastClaude.Version;
        Font = SystemFonts.MessageBoxFont;
        Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        int textWidth;
        using (Graphics g = CreateGraphics())
            textWidth = (int)(440 * g.DpiX / 96);

        var layout = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, Padding = new Padding(16) };
        Controls.Add(layout);

        layout.Controls.Add(new Label
        {
            AutoSize = true,
            MaximumSize = new Size(textWidth, 0),
            Margin = new Padding(0, 0, 0, 12),
            Text = FastClaude.T(
                "Добавляет в меню правого клика по папке пункт «Открыть с помощью Claude». Он открывает cmd от имени администратора и запускает Claude Code в этой папке.",
                "Adds \"Open with Claude\" to the right-click menu of folders. It opens an administrator cmd window and starts Claude Code in that folder.")
        });

        if (!FastClaude.IsClaudeAvailable())
        {
            layout.Controls.Add(new Label
            {
                AutoSize = true,
                MaximumSize = new Size(textWidth, 0),
                Margin = new Padding(0, 0, 0, 12),
                ForeColor = Color.Firebrick,
                Text = FastClaude.T(
                    "Claude Code не найден на этом компьютере. Установите его с " + FastClaude.ClaudeUrl + ", иначе пункт меню не сработает.",
                    "Claude Code was not found on this computer. Install it from " + FastClaude.ClaudeUrl + ", otherwise the menu item will not work.")
            });
        }

        if (FastClaude.IsWindows11() && !FastClaude.IsClassicMenuEnabled())
        {
            classicMenu = new CheckBox
            {
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 2),
                Text = FastClaude.T("Вернуть классическое меню Windows", "Restore the classic Windows menu")
            };
            layout.Controls.Add(classicMenu);
            layout.Controls.Add(new Label
            {
                AutoSize = true,
                MaximumSize = new Size(textWidth, 0),
                Margin = new Padding(0, 0, 0, 12),
                ForeColor = SystemColors.GrayText,
                Text = FastClaude.T(
                    "Тогда пункт будет виден сразу, а не в «Показать дополнительные параметры». Проводник перезапустится, открытые окна папок закроются.",
                    "The item will then be visible right away, not under \"Show more options\". Explorer will restart and open folder windows will close.")
            });
        }

        var buttons = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0), WrapContents = false };
        layout.Controls.Add(buttons);

        var install = new Button { AutoSize = true, Margin = new Padding(0, 0, 8, 0), Padding = new Padding(8, 2, 8, 2), Text = FastClaude.IsInstalled() ? FastClaude.T("Переустановить", "Reinstall") : FastClaude.T("Установить", "Install") };
        install.Click += delegate { OnInstall(); };
        buttons.Controls.Add(install);
        AcceptButton = install;

        if (FastClaude.IsInstalled())
        {
            var uninstall = new Button { AutoSize = true, Margin = new Padding(0, 0, 8, 0), Padding = new Padding(8, 2, 8, 2), Text = FastClaude.T("Удалить", "Remove") };
            uninstall.Click += delegate { OnUninstall(); };
            buttons.Controls.Add(uninstall);
        }

        var close = new Button { AutoSize = true, Margin = new Padding(0), Padding = new Padding(8, 2, 8, 2), Text = FastClaude.T("Закрыть", "Close") };
        close.Click += delegate { Close(); };
        buttons.Controls.Add(close);
        CancelButton = close;
    }

    void OnInstall()
    {
        bool enableClassicMenu = classicMenu != null && classicMenu.Checked;
        try
        {
            FastClaude.Install(enableClassicMenu);
        }
        catch (Exception e)
        {
            FastClaude.Error(e.Message);
            return;
        }

        string message = FastClaude.T("Готово! Нажмите правой кнопкой на любую папку и выберите «Открыть с помощью Claude».", "Done! Right-click any folder and choose \"Open with Claude\".");
        if (FastClaude.IsWindows11() && !FastClaude.IsClassicMenuEnabled())
            message += FastClaude.T("\n\nВ Windows 11 пункт находится в «Показать дополнительные параметры».", "\n\nOn Windows 11 the item is under \"Show more options\".");
        FastClaude.Info(message);
        Close();
    }

    void OnUninstall()
    {
        if (!FastClaude.Confirm(FastClaude.T("Удалить fastClaude?", "Remove fastClaude?")))
            return;
        try
        {
            FastClaude.Uninstall();
        }
        catch (Exception e)
        {
            FastClaude.Error(e.Message);
            return;
        }
        FastClaude.Info(FastClaude.T("fastClaude удалён.", "fastClaude has been removed."));
        Close();
    }
}
