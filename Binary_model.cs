using MathNet.Numerics;

namespace Activity_Interaction_Coefficient_Calculator_UEM1
{

    class MiedemaModel
    {
        private const double P_TT = Constant.P_TT;
        private const double P_TN = Constant.P_TN;
        private const double P_NN = Constant.P_NN;
        private const double QtoP = Constant.QtoP;
        private Element Ea { get; set; }
        private Element Eb { get; set; }
        private string state { get => _state; }

        private double lammda { get => _lammda;  }
        
        private string _state;
        private enum  _orderDegree  {SS,AMP,IM};
        private double _lammda =0;
        private double T { get; set; }
        private bool isEntropy { get; set; }

        public MiedemaModel() 
        {
           
        }
        public MiedemaModel(Element A, Element B)
        {
            this.Ea = A;
            this.Eb = B;

        }
        public void setTemperature(double Tem)
        {
            this.T = Tem;
        }
        public void setPairElement(string element_a, string element_b)
        {

            this.Ea = new Element(element_a);
            this.Eb = new Element(element_b);
        }
        
        public void setState(string state)
        {
            this._state = state;

        }
        public void setLammda(double n)
        {
            this._lammda = n;

        }
      

        protected double Abs(double x)
        { return Math.Abs(x); }
        protected double pow(double x, double y)
        {
            return Math.Pow(x, y);
        }
        public double fab(Element Ea, Element Eb, string state)
        {
            double diff;
            

            if (Ea.isExist && Eb.isExist)
            {
                double P_AB, RP;
                P_AB = (Ea.isTrans_group && Eb.isTrans_group) ? P_TT : ((Ea.isTrans_group || Eb.isTrans_group) ? P_TN : P_NN);
                RP = hybridization_term(Ea, Eb, state);
                
                diff = 2 * P_AB * (-pow(Ea.Phi - Eb.Phi, 2.0) + QtoP * pow(Ea.N_WS - Eb.N_WS, 2.0) - RP) / (1.0 / Ea.N_WS + 1.0 / Eb.N_WS);
            }
            else { diff = Double.NaN; }
            return diff;


        }
        
     
     

       
        private double hybridization_term(Element _Ea, Element _Eb, string _state)
        {
            double alpha = 0.0;
            if (_state == "solid")
            {
                alpha = 1.0;
            }
            else
            {
                alpha = 0.73;
            }

            return (_Ea.hybird_factor == _Eb.hybird_factor) ? 0.0 : alpha * _Ea.hybird_Value * _Eb.hybird_Value;
        }
        private (double V1, double V2) V_inalloy(Element Ea, Element Eb, double xa, double xb)
        {
            double VAa, VBa;

            double PAx, PBx;

            double new_VAa, new_VBa;
            double ya, yb;
            ya = xa / (xa + xb);
            yb = xb / (xa + xb);

            DateTime start = DateTime.Now;
            double tolerance = 1e-9;
            if (Ea.Name == "H" || Eb.Name == "H")
            {
                //H与其它元素形成有序化合物时，合金中的体积
                VAa = Ea.V;
                VBa = Eb.V;
                do
                {
                    new_VAa = VAa;
                    new_VBa = VBa;
                    PAx = ya * VAa / (ya * VAa + yb * VBa);
                    PBx = yb * VBa / (ya * VAa + yb * VBa);
                    VAa = Ea.V * (1 + Ea.u * PBx * (1 + lammda * Math.Pow(PAx * PBx, 2.0)) * (Ea.Phi - Eb.Phi));
                    VBa = Eb.V * (1 + Eb.u * PAx * (1 + lammda * Math.Pow(PAx * PBx, 2.0)) * (Eb.Phi - Ea.Phi));
                    DateTime stop = DateTime.Now; //获取代码段执行结束时的时间
                    TimeSpan tspan = stop - start;
                    if (tspan.TotalMilliseconds > 15000)
                    {
                        break;
                    }

                } while (Math.Abs(VAa - new_VAa) > tolerance || Math.Abs(VBa - new_VBa) > tolerance);

            }
            else
            {
                VAa = Ea.V * (1 + Ea.u * ya * (Ea.Phi - Eb.Phi));
                VBa = Eb.V * (1 + Eb.u * yb * (Eb.Phi - Ea.Phi));
            }


            return (VAa, VBa);


        }





        /// <summary>
        /// 含气体元素的体系忽略过剩熵，但O与非金属元素的情况除外
        /// 该函数主要用于计算贡献系数相关值，始终按规则考虑过剩熵
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <param name="Xa"></param>
        /// <param name="Xb"></param>
        /// <returns></returns>
        public double Excess_gibbs_Energy(string A, string B, double Xa, double Xb)
        {
            setPairElement(A, B);

            double f_AB = fab(this.Ea, this.Eb, this.state);
            double entropy_term = 0;
            bool setEntropyFlag = myFunctions.EntropyJudge(A,B);
            if (setEntropyFlag)
            {
                double avg_Tm = 1.0 / this.Ea.Tm + 1.0 / this.Eb.Tm;
                if (this.state == "solid")
                {
                    entropy_term = 1.0 / 15.1 * avg_Tm * this.T;
                }
                else
                {
                    entropy_term = 1.0 / 14 * avg_Tm * this.T;
                }

            }
            f_AB = f_AB * (1 - entropy_term);

            double Vaa, Vba;
            (Vaa, Vba) = V_inalloy(this.Ea, this.Eb, Xa, Xb);

            double fB;

            double cA, cB, cAS, cBS;
            cA = Xa / (Xa + Xb);
            cB = Xb / (Xa + Xb);

            cAS = cA * Vaa / (cA * Vaa + cB * Vba);
            cBS = cB * Vba / (cA * Vaa + cB * Vba);
            fB = cBS * (1 + lammda * Math.Pow(cAS * cBS, 2.0));

            double dH_trans = 0.0;

            dH_trans = this.Ea.dH_Trans * Xa / (Xa + Xb) + this.Eb.dH_Trans * Xb / (Xa + Xb);

            return fB * f_AB * cA * Vaa + dH_trans;



        }


        public string asymmetricComponent_Judge(string k, string i, string j) 
        {
            double bik, bjk,bij;
            bik = Excess_gibbs_Energy(k,i,0.5,0.5);
            bjk = Excess_gibbs_Energy(k,j,0.5,0.5);
            bij = Excess_gibbs_Energy(i,j,0.5,0.5);
            bool allSameSign = (bij > 0 && bik > 0 && bjk > 0) || (bij < 0 && bik < 0 && bjk < 0);

            if (allSameSign)
            {
                // --- 情况1: 符号全部相同 ---
                // 找出与绝对值最小的混合焓“相对”的那个组元。
                // k 对应 |bij| (i-j)
                // j 对应 |bik| (i-k)
                // i 对应 |bjk| (j-k)

                double abs_bij = Math.Abs(bij);
                double abs_bik = Math.Abs(bik);
                double abs_bjk = Math.Abs(bjk);

                if (abs_bij <= abs_bik && abs_bij <= abs_bjk)
                {
                    // |bij| 最小，选择 k
                    return k;
                }
                else if (abs_bik <= abs_bij && abs_bik <= abs_bjk)
                {
                    // |bik| 最小，选择 j
                    return j;
                }
                else
                {
                    // |bjk| 最小，选择 i
                    return i;
                }
            }
            else
            {
                // --- 情况2: 符号存在不同 ---
                // 找出与其他两个符号不同的那个二元系中的“独特”组元。
                // 例如，如果 bij 和 bik 符号相同，那么 i 就是它们的公共组元。
                if (bij * bik > 0)
                {
                    // bij(i-j) 和 bik(i-k) 符号相同, 公共组元是 i
                    return i;
                }
                else if (bij * bjk > 0)
                {
                    // bij(i-j) 和 bjk(j-k) 符号相同, 公共组元是 j
                    return j;
                }
                else
                {
                    // 剩下的情况必然是 bik(i-k) 和 bjk(j-k) 符号相同, 公共组元是 k
                    return k;
                }
            }


        }

 
         
        /// <summary>
        /// UEM1中的贡献系数计算，采用无限稀活度系数之差，考虑过剩熵条件下计算的结果
        /// </summary>
        /// <param name="k"></param>
        /// <param name="i"></param>
        /// <param name="T"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public double kexi(string k, string i, double T, string state = "liquid")
        {

           
            Element Ek = new Element(k);
            Element Ei = new Element(i);
            double avg_Tm = 1.0 / Ei.Tm + 1.0 / Ek.Tm;

            double lnyi0_k,sij;

            double fik, dHtrans_i = 0, dHtrans_slv = 0, dHtrans = 0;
           
            if (myFunctions.EntropyJudge(k,i))
            {
                if (state == "liquid")
                {
                    sij = 1.0 / 14 * T * avg_Tm;

                }
                else
                {
                    sij = 1.0 / 15.1 * T * avg_Tm;
                }
            }
            else
            {
                sij = 0;
            }
            

            fik = fab(Ei, Ek, state)*(1-sij);

            List<string> elemets_lst = new List<string>() { "Si", "Ge" };
            dHtrans_i = Ei.dH_Trans;
            dHtrans_slv = Ek.dH_Trans;

            if (state == "liquid")
            {
                if (elemets_lst.Contains(Ei.Name))
                {
                    dHtrans_i = 0;
                }
                

                if (elemets_lst.Contains(Ek.Name))
                {
                    dHtrans_slv = 0;
                }
                
            }

            dHtrans = dHtrans_i-dHtrans_slv;
            
            lnyi0_k = 1000 * fik * Ei.V * (1 + Ei.u * (Ei.Phi - Ek.Phi)) / (Constant.R * T) + 1000 * dHtrans / (Constant.R * T);

            return lnyi0_k;
            
        }
   

        public double get_Dki_Nointeractive(string k, string i, double T, string state = "liquid")
        { 
            double lnyi0_k = kexi(k,i,T,state);
            double lnyk0_i = kexi(i,k,T,state);
            return Abs(lnyk0_i - lnyi0_k);
        }

        public double get_Dki_interactive1(string k, string i, string j, double T, string state = "liquid")
        {
            //j.mol.liq.2020            
            
            this.T = T;
            Func<double, double> gkj = x =>this.Excess_gibbs_Energy(k,j,x,1-x);
            Func<double,double> gij = x=>this.Excess_gibbs_Energy(i,j,x,1-x);
            double Average_gkj = myFunctions.Integrate(gkj, 0, 1) / (Constant.R * T);
            double Average_gij = myFunctions.Integrate(gij, 0, 1) / (Constant.R * T);

            return Abs(Average_gkj-Average_gij)/Abs(Average_gij+Average_gkj);

        }
        /// <summary>
        /// 计算函数围成的图像的中心坐标（x,y)
        /// </summary>
        /// <param name="k">xk</param>
        /// <param name="i">1-xk</param>
        /// <param name="phaseState"></param>
        /// <returns></returns>
        public (double x, double y) get_GraphicCenter(string k, string i, double temperature, string phaseState = "liquid")
        {
           this.setState(phaseState);
            
            Func<double, double> func_x = x => this.Excess_gibbs_Energy(k, i, x, 1 - x) * 1000;
            Func<double, double> xfunc_x = x => x * func_x(x);
            Func<double, double> func_x2 = x => func_x(x) * func_x(x);

            double x_bar;
            double A;
            double y;


            

            x_bar = myFunctions.Integrate(xfunc_x, 0, 1);
            A = myFunctions.Integrate(func_x, 0, 1);
            y = myFunctions.Integrate(func_x2, 0, 1);


            double x_ = x_bar / A;
            double y_ = y / (2.0 * A);

            return (x_ - 0.5, y_);

        }

        
        public double delta_x(double x, double y)
        {
            if ((Abs(x) >= 0 && Abs(x) <= Math.PI / 2.0) && (Abs(y) >= 0 && Abs(y) <= Math.PI / 2.0))
            {
                return 0;
            }
            else if ((Abs(x) >= Math.PI / 2.0 && Abs(x) <= Math.PI) && (Abs(y) >= Math.PI / 2.0 && Abs(y) <= Math.PI))
            {
                return 0;
            }
            else
            {
                return Math.PI / 2.0;
            }


        }


        public double get_Dki_interactive_Adv(string k, string i, string j, double T, string state = "liquid")
        {
            double xkj, xij, ykj, yij;
            (xij, yij) = get_GraphicCenter(i, j,T,state);
            (xkj, ykj) = get_GraphicCenter(k, j, T,state);

            double hx1_x2 = Abs(xij - xkj) / Abs(xij + xkj);
            double ty1_y2 = Math.Exp(Abs(yij - ykj) / Abs(yij + ykj));

            double theta10, theta20, theta11, theta21;
            theta10 = Math.Atan2(xij, yij);
            theta11 = Math.Atan2(yij, xij);
            theta20 = Math.Atan2(xkj, ykj);
            theta21 = Math.Atan2(ykj, xkj);
            double a, b;
            a = Math.Sqrt(xij * xij + yij * yij);
            b = Math.Sqrt(xkj * xkj + ykj * ykj);
            double dki = Abs((Math.PI / 2.0 * (theta10 * theta10 - theta20 * theta20) + delta_x(theta10, theta20)) / (theta10 * theta10 + theta20 * theta20)) * Abs(a - b) / Math.Sqrt(a * a + b * b);
            return dki;

        }

        public double GSM_deviation_Function(string k, string A, string B, double T)
        {
            
            Func<double, double> func = x => this.Excess_gibbs_Energy(A, B, x, 1 - x) -
                this.Excess_gibbs_Energy(A, k, x, 1 - x);

            Func<double, double> func2 = x => func(x) * func(x);
            double f;
            f = myFunctions.Integrate(func2, 0, 1);
            return f;

        }




        /// <summary>
        /// Non-interactive properties difference
        /// </summary>
        /// <param name="k"></param>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public double UEM1(string k, string i, string j, double T, string state)
        {
            double alpha_KA;          
           
            double weight1 = 0;
           
            double df_KI, df_KJ;
            // 非交互作用性质差.
            df_KI = get_Dki_Nointeractive(k, i,  T, state);
            df_KJ = get_Dki_Nointeractive(k, j,  T, state);

            if (df_KI == 0 && df_KJ == 0)
            {
                df_KI = df_KJ = -0.0000000000001;
            }

            df_KI = 1 * pow(df_KI, 1);
            df_KJ = 1 * pow(df_KJ, 1);

            weight1 = df_KJ / (df_KI + df_KJ);

            alpha_KA = Math.Exp(-df_KI) * weight1;
           


            return alpha_KA;
        }

        public double UEM2(string k, string i, string j, double T, string state) 
        {
            //j.mol.liq.2020

            double dki = get_Dki_interactive1(k, i,j, T, state);
            double dkj = get_Dki_interactive1(k, j,i, T, state);

            return dkj/(dkj+dki)*Math.Exp(-dki);
        }
        public double UEM2_Adv(string k, string i, string j, double T, string state)
        {
            double dki = get_Dki_interactive_Adv(k,i,j,T,state);
            double dkj = get_Dki_interactive_Adv(k,j,i,T,state);

            return dkj / (dkj + dki) * Math.Exp(-dki);
        }
        public double Toop_Kohler(string k, string i, string j, double T, string state)
        {
            string asymc = asymmetricComponent_Judge(k, i, j);
            if (asymc == i) { return 0; }
            else if (asymc == j) { return 1.0; }
            else
            {
                return 0;
            }
            
        }
        public double Toop_Muggianu(string k, string i, string j, double T, string state)
        {
            string asymc = asymmetricComponent_Judge(k, i, j);
            if (asymc == i) { return 0; }
            else if (asymc == j) { return 1.0; }
            else
            {
                return 0.5;
            }
        }
        public double GSM(string k, string i, string j, double T, string state)
        {
            double dki = GSM_deviation_Function(k, i, j,T);
            double dkj = GSM_deviation_Function(k, j, i, T);
            return dki/(dki+dkj);
        }
        public double Muggianu(string k, string i, string j, double T, string state)
        {
            return 0.5;
        }



    }
}
