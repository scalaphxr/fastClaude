using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

// Запускается из контекстного меню Проводника. Права администратора
// запрашивает манифест (app.manifest), поэтому здесь нужен только запуск cmd.
static class ClaudeHere
{
    [STAThread]
    static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            MessageBox.Show("Не передан путь к папке.", "ClaudeHere", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }

        // Проводник передаёт корень диска как "C:\", и завершающая \" превращается в кавычку.
        string path = args[0].Trim('"');
        if (path.Length == 2 && path[1] == ':')
            path += "\\";

        if (!Directory.Exists(path))
        {
            MessageBox.Show("Папка недоступна:\n" + path, "ClaudeHere", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }

        // Путь передаётся как рабочая папка, а не в команде: cmd не разбирает & и % в имени.
        Process.Start(new ProcessStartInfo("cmd.exe", "/k claude")
        {
            UseShellExecute = false,
            WorkingDirectory = path
        });
        return 0;
    }
}
