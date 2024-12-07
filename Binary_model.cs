using MathNet.Numerics;
using System.Text.RegularExpressions;

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
        public void setEntropy(bool is_sE)
        {
            this.isEntropy = is_sE;
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
                RP = rp(Ea, Eb, state);
                
                diff = 2 * P_AB * (-pow(Ea.Phi - Eb.Phi, 2.0) + QtoP * pow(Ea.N_WS - Eb.N_WS, 2.0) - RP) / (1.0 / Ea.N_WS + 1.0 / Eb.N_WS);
            }
            else { diff = Double.NaN; }
            return diff;


        }
        
     
     

       
        private double rp(Element _Ea, Element _Eb, string _state)
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
            if (_Ea.hybird_factor == "other" || _Eb.hybird_factor == "other")
            {
                return 0.0;
            }
            else
            {
                return (_Ea.hybird_factor == _Eb.hybird_factor) ? 0.0 : alpha * _Ea.hybird_Value * _Eb.hybird_Value;
            }
        }

        /// <summary>
        /// H(X,Y)二元相互作用项，考虑过剩熵时返回过剩吉布斯自由能，否则返回混合焓,kJ/mol
        /// </summary>
        /// <param name="A">元素A</param>
        /// <param name="B">元素B</param>
        /// <param name="Xa">A的摩尔组成</param>
        /// <param name="Xb">B的摩尔组成</param>
        /// <returns>二元系性质kJ/mol</returns>
        public double binary_Model(string A, string B, double Xa, double Xb)
        {
            setPairElement(A, B);

            double f_AB = fab(this.Ea, this.Eb, this.state);
            double entropy_term = 0;
            if (this.isEntropy)
            {
                double avg_Tm = 1.0/this.Ea.Tm + 1.0/this.Eb.Tm;
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
            
            return fB * f_AB*cA*Vaa + dH_trans;


           
        }
       
    
       
        private (double V1, double V2) V_inalloy(Element Ea,Element Eb, double xa, double xb)
        {
            double VAa, VBa;
          
                double PAx, PBx;

                double new_VAa, new_VBa;
            double ya, yb;
            ya = xa/(xa+xb);
            yb = xb/(xb+xb);
                
            DateTime start = DateTime.Now;
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

                } while (VAa != new_VAa && VBa != new_VBa);

            }
            else
            {
                VAa = Ea.V*(1+Ea.u*ya*(Ea.Phi-Eb.Phi));
                VBa = Eb.V*(1+Eb.u*yb*(Eb.Phi-Ea.Phi));
            }

            
            return (VAa, VBa);
            

        }
     
 
        static Dictionary<string, double> df_UEM2 = new Dictionary<string, double>();
        public double kexi(string k, string i, double T, string state = "liquid")
        {

           
            Element Ek = new Element(k);
            Element Ei = new Element(i);
            double avg_Tm = 1.0 / Ei.Tm + 1.0 / Ek.Tm;

            double lnyi0_k,sij;

            double fik, dHtrans_i = 0, dHtrans_slv = 0, dHtrans = 0;
           
            if (!new[] { "H", "O", "N" }.Contains(i) && !new[] { "H", "O", "N" }.Contains(k))
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

        public double get_Dki(string k, string i, double T, string state = "liquid")
        { 
            double lnyi0_k = kexi(k,i,T,state);
            double lnyk0_i = kexi(i,k,T,state);
            return Abs(lnyk0_i - lnyi0_k);
        }




        /// <summary>
        /// 交互作用性质差法
        /// </summary>
        /// <param name="k"></param>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public double UEM1(string k, string i, string j, string state)
        {
            double alpha_KA;
          


            Ternary_melts? ternary_ = new Ternary_melts();

            ternary_.setState(state);
            ternary_.setTemperature(T);
            ternary_.setEntropy(false);
            double weight1 = 0;
           
            double df_KI, df_KJ;
            // 非交互作用性质差.
            df_KI = get_Dki(k, i,  T, state);
            df_KJ = get_Dki(k, j,  T, state);

            if (df_KI == 0 && df_KJ == 0)
            {
                df_KI = df_KJ = -0.0000000000001;
            }

            df_KI = 1 * pow(df_KI, 1);
            df_KJ = 1 * pow(df_KJ, 1);

            weight1 = df_KJ / (df_KI + df_KJ);

            alpha_KA = Math.Exp(-df_KI) * weight1;
            System.GC.Collect();


            return alpha_KA;
        }
      

    }
}
