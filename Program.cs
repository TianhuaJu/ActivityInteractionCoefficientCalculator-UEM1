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

            release_Resource();
            SQLitePCL.Batteries.Init();
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
        private static void release_Resource()
        {

           
            byte[] data_Miedema = Properties.Resources.BasicData;
            byte[] data_expDB = Properties.Resources.myDB;
           
            string strPath_db = Application.StartupPath + @"\data\BasicData.db";
            string strPath_expdb = Application.StartupPath + @"\data\myDB.db";
          



            
            create_file_path(data_Miedema, strPath_db);
            create_file_path(data_expDB, strPath_expdb);
          
           

            void create_file_path(byte[] file, string filepath)
            {

                if (!File.Exists(filepath))
                {
                    //判断文件是否存在，不存在执行以下操作
                    if (Directory.Exists(Path.GetDirectoryName(filepath)))
                    {//文件夹存在

                        using FileStream fs = new FileStream(filepath, FileMode.CreateNew);

                        fs.Write(file, 0, file.Length);
                        fs.Flush();
                        fs.Close();
                    }
                    else
                    {
                        //文件夹不存在
                       
                        Directory.CreateDirectory(Path.GetDirectoryName(filepath));

                        using FileStream fs = new FileStream(filepath, FileMode.CreateNew);
                        fs.Write(file, 0, file.Length);
                        fs.Flush();
                        fs.Close();
                    }
                }

            }


        }
    }
}