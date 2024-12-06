using System.Data;
using Microsoft.Data.Sqlite;

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
        public static  void get_MiedemaData(Element E1)
        {
            
            string dbpath = "Data Source =" + "data\\BasicData.db";
            string cmdTXT = "SELECT phi,nws,V,u,alpha_beta,hybirdvalue,isTrans,dHtrans,mass,Tm,name,Tb FROM "+ Miedemadata+" WHERE Symbol ='"+E1.Name+"'";
            SqliteConnection conn = new SqliteConnection(dbpath);
            conn.Open();
            SqliteCommand cmd = new SqliteCommand(cmdTXT, conn);
            SqliteDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                E1.Phi = reader.GetDouble(0);
                E1.N_WS = reader.GetDouble(1);
                E1.V = reader.GetDouble(2);
                E1.u = reader.GetDouble(3);
                E1.hybird_factor = reader.GetString(4);
                E1.hybird_Value = reader.GetDouble(5);
                E1.isTrans_group = reader.GetBoolean(6);
                E1.dH_Trans = reader.GetDouble(7);
                E1.M = reader.GetDouble(8);
                E1.Tm = reader.GetDouble(9);
                //E1.Name = reader.GetString(10);
                E1.Tb = reader.GetDouble(11);
                
            }
             
            
            if (!reader.IsClosed )
            {
                reader.Close();
            }
            cmd.Dispose();

            if (conn.State == ConnectionState.Open)
            {
                 
                conn.Close();
            }
           


        }
        
       

        /// <summary>
        /// 从SQLite中查询一阶活度相互作用系数
        /// 数据库中的数据全部以string形式存储，除数据格式很明确外
        /// </summary>
        public static void query_first_order_wagnerIntp(Melt melt)
        {
             
            SqliteConnection? liteConnection = null;
            string dbpath = "Data Source ="+"data\\myDB.db";//数据库中存储的eji或sji,默认是包含温度的字符串，y = a/T+b
            string cmd1 = "SELECT eji,Rank,sji,T,reference FROM first_order WHERE solv = '" + melt.Based + "' and solui = '" + melt.solui + "' and soluj='" + melt.soluj + "'";
            string cmd2 = "SELECT eji,Rank,sji,T,reference FROM first_order WHERE solv = '" + melt.Based + "' and solui = '" + melt.soluj + "' and soluj='" + melt.solui + "'";
            liteConnection = new SqliteConnection(dbpath);
            liteConnection.Open();
            SqliteCommand command1 = new SqliteCommand(cmd1, liteConnection);
            SqliteDataReader rd = command1.ExecuteReader();

            if (rd.Read())
            {
                melt.ji_flag = true;
                //查询到k-i-j,执行eji、sji赋值
                if (string.IsNullOrEmpty(rd.GetString(0)))
                {
                    melt.eji_str = string.Empty;
                    melt.Rank_firstorder = rd.GetString(1);
                    melt.sji_str = rd.GetString(2);
                    melt.str_T = rd.GetString(3);
                    if (!rd.IsDBNull(4))
                    {
                        melt.Ref = rd.GetString(4);
                    }
                    else
                    {
                        melt.Ref = String.Empty;
                    }
                }
                else
                {
                    melt.eji_str = rd.GetString(0);
                    melt.Rank_firstorder = rd.GetString(1);
                    melt.sji_str = string.Empty;
                    melt.str_T = rd.GetString(3);
                    if (!rd.IsDBNull(4))
                    {
                        melt.Ref = rd.GetString(4);
                    }
                    else
                    {
                        melt.Ref = String.Empty;
                    }
                }
                rd.Close();
            }
            else
            {
                
                //未查询到k-i-j,执行k-j-i查询
                SqliteCommand commd2 = new SqliteCommand(cmd2, liteConnection);
                SqliteDataReader rd1 = commd2.ExecuteReader();
                if (rd1.Read())
                {
                    //查询到k-j-i
                    melt.ij_flag = true;
                    
                    if (string.IsNullOrEmpty(rd1.GetString(0)))
                    {
                        melt.eij_str = string.Empty;
                        melt.Rank_firstorder = rd1.GetString(1);
                        melt.sij_str = rd1.GetString(2);                        
                        melt.str_T = rd1.GetString(3);
                        if (!rd1.IsDBNull(4))
                        {
                            melt.Ref = rd1.GetString(4);
                        }
                        else
                        {
                            melt.Ref = String.Empty;
                        }

                    }
                    else
                    {
                       
                        melt.eij_str = rd1.GetString(0);
                        melt.sij_str = string.Empty;
                        melt.Rank_firstorder = rd1.GetString(1);
                        melt.str_T = rd1.GetString(3);
                        if (!rd1.IsDBNull(4))
                        {
                            melt.Ref = rd1.GetString(4);
                        }
                        else
                        {
                            melt.Ref = String.Empty;
                        }


                    }




                }
                else
                {
                    //未查询到k-j-i 
                    melt.ij_flag = false;
                    melt.ji_flag = false;
                }
                rd1.Close();
            }
            
            if (liteConnection.State == ConnectionState.Open)
            {
                liteConnection.Close();
            }


        }
       

      


    }
}
