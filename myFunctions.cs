using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.IO;


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
        public static double asymtermJudge(double a, double b, double c)
        {
            double t;

            if ((a > 0 && b > 0 && c > 0) || (a < 0 && b < 0 && c < 0))
            {
                if (a * b * c > 0)
                {

                    t = a > b ? b : a;
                    return (t > c) ? c : t;
                }
                else
                {


                    t = a > b ? a : b;
                    return (t > c) ? t : c;
                }

            }
            else
            {


                return (a * b > 0) ? c : (a * c > 0 ? b : a);
            }
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
