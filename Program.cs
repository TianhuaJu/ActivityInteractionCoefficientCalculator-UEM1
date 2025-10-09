using System.IO;

namespace Activity_Interaction_Coefficient_Calculator_UEM1
{
    internal static class Program
    {

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.


            SQLitePCL.Batteries.Init();

            var app = new System.Windows.Application();

            // 创建你的 Window1 实例
            var window = new Window1();

            // 运行 WPF Application
            app.Run(window);
        }

    }
}