using MathNet.Numerics.Integration;
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
        private static readonly HashSet<string> GaseousElements = new HashSet<string>
    {
        "H", "He", "N", "O", "F", "Ne", "Cl", "Ar", "Kr", "Xe", "Rn"
    };
        private static readonly HashSet<string> SolidOrLiquidNonMetals = new HashSet<string>
    {
        "C", "P", "S", "Se", "Br", "I", "At"
    };
        public static bool EntropyJudge(params string[] elementSymbols)
        {
            if (elementSymbols == null || !elementSymbols.Any())
            {
                // 如果列表为空或为null，属于“其他情况”，返回True
                return true;
            }

            // 使用Linq来判断是否存在指定类型的元素，效率更高
            bool hasGas = elementSymbols.Any(symbol => GaseousElements.Contains(symbol));
            bool hasSolidOrLiquidNonMetal = elementSymbols.Any(symbol => SolidOrLiquidNonMetals.Contains(symbol));

            // 现在根据逻辑规则判断返回值
            if (hasGas && hasSolidOrLiquidNonMetal)
            {
                // 规则2: 当非金属元素与气体元素同时存在时，返回True
                return true;
            }
            else if (hasGas) // 此条件意味着 hasSolidOrLiquidNonMetal 为 false
            {
                // 规则1: 当传入参数中有气体元素时(但没有非气态非金属)，返回False
                return false;
            }
            else
            {
                // 规则3: 其他情况均返回True (例如，没有气体元素)
                return true;
            }
        }
        public static double Integrate(Func<double, double> function,
                                       double lowerBound,
                                       double upperBound,
                                       double relativeTolerance = 1e-8)
        {
            double error, L1Norm;
            return GaussKronrodRule.Integrate(
                function,
                lowerBound,
                upperBound,                
                out error,
                out L1Norm,
                targetRelativeError:relativeTolerance

            );
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
