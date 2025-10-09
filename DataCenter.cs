using System.Data;
using System.Data.SQLite;

namespace Activity_Interaction_Coefficient_Calculator_UEM1
{

    /// <summary>
    /// 获取Miedema物性参数,和部分已有的实验值
    /// </summary>
    static class DataCenter
    {

      
        
        static string Miedemadata =  "Miedema1988";
        
        public static void Database(string database)
        {
            Miedemadata = database;
        }
        public static void Database()
        {
            Miedemadata = "Miedema1988";
        }
        /// <summary>
        /// 从数据库读取Miedema参数
        /// </summary>
        /// <param name="E1"></param>
        /// <summary>
        /// 从数据库读取Miedema参数 (已重写以安全处理字符串到具体类型的转换)
        /// </summary>
        /// <param name="E1"></param>
        public static void get_MiedemaData(Element E1)
        {
            string dbpath = "Data Source=data\\BasicData.db";
            // SQL查询语句保持不变
            string cmdTXT = "SELECT phi,nws,V,u,alpha_beta,hybirdvalue,isTrans,dHtrans,mass,Tm,name,Tb FROM " + Miedemadata + " WHERE Symbol = @Symbol";

            using (var conn = new SQLiteConnection(dbpath))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(cmdTXT, conn))
                {
                    cmd.Parameters.AddWithValue("@Symbol", E1.Name);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // 定义一个局部辅助函数，用于从reader安全地获取字符串，避免DBNull异常
                            string GetString(int columnIndex)
                            {
                                if (!reader.IsDBNull(columnIndex))
                                {
                                    // GetValue返回object，ToString确保我们得到字符串
                                    return reader.GetValue(columnIndex).ToString().Trim();
                                }
                                return string.Empty; // 如果是DBNull，返回空字符串
                            }

                            // --- 开始进行数据转换并赋值 ---

                            // 1. 转换为 Double 类型
                            // 使用 double.TryParse，如果转换失败，out参数会得到默认值0.0，程序不会崩溃
                            double.TryParse(GetString(0), out double phi);
                            E1.Phi = phi;

                            double.TryParse(GetString(1), out double nws);
                            E1.N_WS = nws;

                            double.TryParse(GetString(2), out double v);
                            E1.V = v;

                            double.TryParse(GetString(3), out double u);
                            E1.u = u;

                            double.TryParse(GetString(5), out double hybirdValue);
                            E1.hybird_Value = hybirdValue;

                            double.TryParse(GetString(7), out double dHtrans);
                            E1.dH_Trans = dHtrans;

                            double.TryParse(GetString(8), out double mass);
                            E1.M = mass;

                            double.TryParse(GetString(9), out double tm);
                            E1.Tm = tm;

                            double.TryParse(GetString(11), out double tb);
                            E1.Tb = tb;

                            // 2. 转换为 String 类型 (直接赋值)
                            E1.hybird_factor = GetString(4);
                            // E1.Name = GetString(10); // E1.Name 在构造函数中已经赋值，一般不需要重写

                            // 3. 转换为 Boolean 类型
                            string isTransStr = GetString(6).ToLower(); // 转换为小写以便比较
                                                                        // 兼容 "1", "true", "t", "yes" 等常见表示“真”的字符串
                            E1.isTrans_group = (isTransStr == "1" || isTransStr == "true" || isTransStr == "t" || isTransStr == "yes");
                        }
                    }
                }
            }
        }



        /// <summary>
        /// 从SQLite中查询一阶活度相互作用系数
        /// 数据库中的数据全部以string形式存储，除数据格式很明确外
        /// </summary>
        /// <summary>
        /// 从SQLite中查询一阶活度相互作用系数 (已修正为安全、高效的版本)
        /// </summary>
        public static void query_first_order_wagnerIntp(Melt melt)
        {
            // 初始化标志位
            melt.ji_flag = false;
            melt.ij_flag = false;

            string dbpath = "Data Source=data\\myDB.db";
            // 定义一个通用的查询语句模板
            string query_text = "SELECT eji, Rank, sji, T, reference FROM first_order WHERE solv = @solv AND solui = @solui AND soluj = @soluj";

            // 使用 using 语句确保资源被正确释放
            using (var liteConnection = new SQLiteConnection(dbpath))
            {
                liteConnection.Open();
                using (var command = new SQLiteCommand(query_text, liteConnection))
                {
                    // --- 第一次尝试：查询 k-i-j (solv-solui-soluj) ---
                    command.Parameters.AddWithValue("@solv", melt.Based);
                    command.Parameters.AddWithValue("@solui", melt.solui);
                    command.Parameters.AddWithValue("@soluj", melt.soluj);

                    using (var rd = command.ExecuteReader())
                    {
                        if (rd.Read())
                        {
                            melt.ji_flag = true; // 找到 k-i-j
                                                 // 填充 melt 对象的逻辑 (与您原始代码一致)
                            if (string.IsNullOrEmpty(rd.GetString(0)))
                            {
                                melt.eji_str = string.Empty;
                                melt.Rank_firstorder = rd.GetString(1);
                                melt.sji_str = rd.GetString(2);
                                melt.str_T = rd.GetString(3);
                                melt.Ref = !rd.IsDBNull(4) ? rd.GetString(4) : string.Empty;
                            }
                            else
                            {
                                melt.eji_str = rd.GetString(0);
                                melt.Rank_firstorder = rd.GetString(1);
                                melt.sji_str = string.Empty;
                                melt.str_T = rd.GetString(3);
                                melt.Ref = !rd.IsDBNull(4) ? rd.GetString(4) : string.Empty;
                            }
                            return; 
                        }
                    } 

                    // --- 第二次尝试：如果上面没找到，查询 k-j-i (solv-soluj-solui) ---
                    // 清空旧参数，为下一次查询准备
                    command.Parameters.Clear();
                    // 添加新参数，注意 solui 和 soluj 的值进行了交换
                    command.Parameters.AddWithValue("@solv", melt.Based);
                    command.Parameters.AddWithValue("@solui", melt.soluj); // 值交换
                    command.Parameters.AddWithValue("@soluj", melt.solui); // 值交换

                    using (var rd1 = command.ExecuteReader())
                    {
                        if (rd1.Read())
                        {
                            melt.ij_flag = true; // 找到 k-j-i
                                                 // 填充 melt 对象的逻辑 (与您原始代码一致)
                            if (string.IsNullOrEmpty(rd1.GetString(0)))
                            {
                                melt.eij_str = string.Empty;
                                melt.Rank_firstorder = rd1.GetString(1);
                                melt.sij_str = rd1.GetString(2);
                                melt.str_T = rd1.GetString(3);
                                melt.Ref = !rd1.IsDBNull(4) ? rd1.GetString(4) : string.Empty;
                            }
                            else
                            {
                                melt.eij_str = rd1.GetString(0);
                                melt.sij_str = string.Empty;
                                melt.Rank_firstorder = rd1.GetString(1);
                                melt.str_T = rd1.GetString(3);
                                melt.Ref = !rd1.IsDBNull(4) ? rd1.GetString(4) : string.Empty;
                            }
                            return; 
                        }
                    } 
                } 
            } 

            
        }





    }
}
