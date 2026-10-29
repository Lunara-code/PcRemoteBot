using System.Diagnostics;

internal static class ProgramLauncher
{ 
    internal static void Start(string target)
    {
        var info = new ProcessStartInfo //инициализируем процесс(класс), где поля заполняются сами при создании
        {
            FileName = target, //что открываем
            UseShellExecute= true //отдаем запуск оболочке винды
        };

        Process.Start(info);
    }
}