using System.Text.RegularExpressions;

namespace Activity_Interaction_Coefficient_Calculator_UEM1
{
    /// <summary>
    /// 熔体类，用于接收熔体中组成间的活度相互作用系数
    /// </summary>
    class Melt
    {
        private double _tem;

        private double _eji;
        private double _eij;
        private double _lnYi0;
        private double _Yi0;
        private double _sji;
        private double _sij;
      
      
        
       

        public bool ij_flag;//判断查询到的字符串是ij类型？还是ji类型？还是空值？
        public bool ji_flag;

        /// <summary>
        /// 溶液名
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 基体
        /// </summary>
        public string Based { get => solv; }
        private string solv { get; set; }
        public string solui { get; set; }
        public string soluj { get; set; }
        private double Yi0 { get => _Yi0; }
        /// <summary>
        /// 无限稀活度系数对数
        /// </summary>
        public double lnYi
        {
            get
            {
                if (!object.Equals(this.Yi0, double.NaN))
                {
                    return ln(this.Yi0);
                }
                else
                {
                    return _lnYi0;
                }
            }
        }
        /// <summary>
        /// 以质量分数表示的一阶活度相互作用系数，j on i
        /// </summary>
        public double eji { get => _eji; }
        public double eij { get => _eij; }
        /// <summary>
        /// 以摩尔分数表示的一阶活度相互作用系数，j on i
        /// </summary>
        public double sji {get => _sji;}
        public double sij { get => _sij; }
        
        /// <summary>
        /// 实验值温度
        /// </summary>

        public string str_T { get; set; }
        /// <summary>
        /// 一阶相互作用系数实验值精度等级
        /// </summary>
        public string Rank_firstorder { get; set; }
        
        public string eji_str { get; set; }
        public string T_str { get; set; }
        public string sji_str { get; set; }
        public string eij_str { get; set; }
        public string sij_str { get; set; }

       
        public (string lnYi0, string T) str_lnYi0 { get; set; }
        public (string Yi0, string T) str_Yi0 { get; set; }
        public (string rji, string T) str_rji { get; set; }
        public (string pji, string T) str_pji { get; set; }
        public string Ref { get; set; }

        /// <summary>
        /// 构造熔体
        /// </summary>
        /// <param name="Solv">基体/溶剂</param>
        /// <param name="Solui">溶质i</param>
        /// <param name="Soluj">溶质j</param>
        /// <param name="T">熔体温度（K）</param>
        public Melt(string Solv, string Solui, string Soluj, double T)
        {
            this.Name = Solv + Solui + Soluj;
            this.solv = Solv;
            this.solui = Solui;
            this.soluj = Soluj;
            this._tem = T;
            DataCenter.query_first_order_wagnerIntp(this);
           
            //DataCenter.query_second_order_wagnerIntp(this);
            if (this.ij_flag)
            {
                if (this.eij_str != null)
                {
                    this._eij = Processdata(textinfo: (this.eij_str, this.str_T), T: T);                    
                    this._sij = myFunctions.first_order_w2m(this._eij, new Element(Solui), new Element(Solv)); ;
                    this._sji = this.sij;
                    this._eji = myFunctions.first_order_mTow(this._sji, new Element(soluj), new Element(solv));
                }
                else
                {
                    this._sij = Processdata((this.sij_str,this.str_T),T);
                    this._eij = myFunctions.first_order_mTow(this.sij, new Element(Solui), new Element(solv));
                    this._sji = this.sij;
                    this._eji = myFunctions.first_order_mTow(this._sji,new Element(soluj), new Element(solv));                    
                    
    
                }
            }
            else if (this.ji_flag)
            {
                if (this.eji_str != null)
                {
                    this._eji = Processdata(textinfo: (this.eji_str, this.str_T), T);
                    this._sji = myFunctions.first_order_w2m(this.eji, new Element(soluj), new Element(solv));
                    this._sij = this.sji;
                    this._eij = myFunctions.first_order_mTow(this.sij, new Element(Solui), new Element(solv));
                }
                else
                {
                    this._sji = Processdata(textinfo: (this.sji_str, this.str_T), T);
                    this._eji = myFunctions.first_order_mTow(this.sji,new Element(soluj), new Element(solv));
                    this._sij= this.sji;
                    this._eij = myFunctions.first_order_mTow(this.sij, new Element(solui), new Element(solv));

                }

            }
            else
            { 
                this._eij= this._eji = this._sij = this._sji = double.NaN;
                
            }

            this._Yi0 = Processdata(this.str_Yi0, T);
            this._lnYi0 = Processdata(this.str_lnYi0, T);
         


        }
     
      
        private double Processdata((string valuetext, string T) textinfo, double T)
        {
            double a, b, data;
            Regex_Extend re = new Regex_Extend(@"^([-]?\d*\.?\d*)([\/])([T])(([\+]|[\-]?)\d*\.?\d*)");//y = a/T +/- b

            bool b1 = textinfo.valuetext == null, b2 = textinfo.T == null, b3 = textinfo.valuetext == string.Empty;

            if (!b1 && !b2 && !b3)
            {
                if (textinfo.T == "T")
                {
                    


                    GroupCollection groups = re.group(text: textinfo.valuetext);
                    double.TryParse(groups[1].Value, out a);
                    double.TryParse(groups[4].Value, out b);
                    data = a / T + b;


                }
                else
                {
                    
                    if (double.Parse(textinfo.T) == T)
                    {
                        //same
                        data = double.Parse(textinfo.valuetext);

                    }
                    else
                    {
                        //not same
                        data = double.NaN;
                    }
                }
            }
            else
            {
                data = double.NaN;
            }
            return data;

        }
        private double ln(double x)
        {
            if (x > 0)
            {
                return Math.Log(x);
            }
            else if (x == 0.0)
            {
                return double.NegativeInfinity;

            }
            else
            {
                return double.NaN;
            }

        }


    }
}
