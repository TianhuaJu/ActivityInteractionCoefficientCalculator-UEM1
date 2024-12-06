using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.IO;
using System.Windows.Forms;

namespace Activity_Interaction_Coefficient_Calculator_UEM1
{
    public delegate double fx(double x);
    public delegate double fy(double y,double step_length);
    /// <summary>
    /// 自定义常用函数集
    /// </summary>
    static class myFunctions
    {
        private static double pow(double x, double y)
        {
            return Math.Pow(x, y);
        }
        
        public static void WriteLog (string logFile, string content)
        {
            if (Path.GetDirectoryName(logFile) != "")
            {
                if (!Directory.Exists(Path.GetDirectoryName(logFile)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(logFile));
                }
                string strNewsPath = logFile;
                string smb = content;
                StreamWriter sw = new StreamWriter(strNewsPath, true);
                sw.WriteLine(smb);
                sw.Close();

            }
            else
            {
                string strNewsPath = logFile;
                string smb = content;
                StreamWriter sw = new StreamWriter(strNewsPath, true);
                sw.WriteLine(smb);
                sw.Close();

            }
            
            
           
        }
     
        /// <summary>
        /// 将datagridView表格里的数据转换成DataTable格式
        /// </summary>
        /// <param name="dg"></param>
        /// <returns></returns>
        private  static DataTable dgViewToDt (DataGridView dg)
            {
            DataTable DT = new DataTable( "SaveResult" );
            List<string> lst1 = new List<string>();
            for (int k = 0 ; k < dg.ColumnCount ; k++)
                {

                DT.Columns.Add( dg.Columns[k].HeaderText );

                }
            foreach (DataGridViewRow dgRow in dg.Rows)
                {
                if (dgRow.IsNewRow)
                    {
                    continue;
                    }
                DataRow dtrow = DT.NewRow();
                for (int i = 0 ; i < dg.Columns.Count  ; i++)
                    {
                    dtrow[i] = (dgRow.Cells[i].Value == null) ? "None" : dgRow.Cells[i].Value;
                    }
                DT.Rows.Add( dtrow );

                }

            return DT;

            }
    
        /// <summary>
        /// 一阶相互作用系数单位转换,m to w
        /// </summary>
        /// <param name="sji">摩尔分数表示的相互作用系数,j on i</param>
        /// <param name="Ej">溶质j</param>
        /// <param name="matrix">基体</param>
        /// <returns></returns>
        public static double first_order_mTow(double sji, Element Ej, Element matrix)
        {
            double w;

            w = (sji - 1 + Ej.M / matrix.M) * matrix.M / (230 * Ej.M);

            return w;
        }
        /// <summary>
        /// 一阶相互作用系数转换，w to M
        /// </summary>
        /// <param name="eji">质量分数表示的相互作用系数，j on i</param>
        /// <param name="Ej">溶质j</param>
        /// <param name="matrix">基体</param>
        /// <returns></returns>
        public static double first_order_w2m(double eji, Element Ej, Element matrix)
        {
            double sij = 0.0;
            sij = 230 * eji * Ej.M / matrix.M + (1 - Ej.M / matrix.M);

            return Math.Round(sij, 2);

        }
       
        
      
    }
    }
