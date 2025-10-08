using System.IO;


namespace Activity_Interaction_Coefficient_Calculator_UEM1
{
    delegate double Extrapolation_Model(string k, string A, string B, string extrapolationmodel);

    class Ternary_melts
    {
        private (Element M, Element N, double ration) Bbased;
        private double Tem { get; set; }
        private bool entropy { get => _entropy; }
        private bool _cp;
        private bool cp { get => _cp; }
      
        private string state { get => _state;  }
        private static double R = Constant.R;
        private bool _entropy = false;
        private (bool entropy, bool cp) _condition = (false, false);
        private string _state;
        
        public Ternary_melts(double T, string phaseState = "liquid", bool isSE = false)
        {


            this._condition = (isSE, cp);
            this._state = phaseState;
            this.Tem = T;

          
             
            this._entropy = isSE;

        }
        public Ternary_melts()
        {

        }

        public void setTemperature(double Tem)
        {
            this.Tem = Tem;
        }

      

        public void setEntropy(bool entropy)
        {
            this._entropy = entropy;
        }
        public void setState(string state)
        {
            this._state = state;
        }
       
        

      
        /// <summary>        
        /// </summary>
        /// <param name="Ei"></param>
        /// <param name="Ej"></param>
        /// <returns></returns>
        private double fab_func_ContainS(Element Ei, Element Ej)
        {
            double alpha,Rp, Pij, entropy_term,fij;
            double avg_Tm = 1.0 / Ei.Tm + 1.0 / Ej.Tm;
            if (this.state == "liquid")
            {
                alpha = 0.73;
            }
            else
            { alpha = 1.0; }
            if (this.entropy)
            {
                if (this.state == "liquid")
                {
                    entropy_term = 1.0 / 14 * this.Tem * avg_Tm;
                }
                else
                {
                    entropy_term = 1.0 / 15.1 * this.Tem * avg_Tm;
                }

            }
            else
            { entropy_term = 0.0; }

            if (Ei.hybird_factor != "other" || Ej.hybird_factor != "other")
            {
                Rp = (Ei.hybird_factor == Ej.hybird_factor) ? 0.0 : Ei.hybird_Value * Ej.hybird_Value;
            }
            else
            {
                Rp = 0.0;
            }
            Pij = (Ei.isTrans_group && Ej.isTrans_group) ? Constant.P_TT : ((Ei.isTrans_group || Ej.isTrans_group) ? Constant.P_TN : Constant.P_NN);
            fij = 2.0 * Pij * (Constant.QtoP * Pow(Ei.N_WS - Ej.N_WS, 2.0) - Pow(Ei.Phi - Ej.Phi, 2.0) - alpha * Rp) / ((1.0 / Ei.N_WS + 1.0 / Ej.N_WS));
            

            return fij*(1-entropy_term);


        }


        private double Pow(double x, double y)
        {
            return Math.Pow(x, y);
        }
  
        
     
       
        
        /// <summary>
        /// 活度相互作用系数的计算
        /// </summary>
        /// <param name="solv"></param>
        /// <param name="solui"></param>
        /// <param name="soluj"></param>
        /// <param name="geo_Model"></param>
        /// <param name="GeoModel"></param>
        /// <returns></returns>
        public double Activity_Interact_Coefficient_Model(Element solv, Element solui, Element soluj, Extrapolation_Model geo_Model, string state)
        {
          

            double fij = fab_func_ContainS(solui, soluj);
            double fik = fab_func_ContainS(solv, solui);
            double fjk = fab_func_ContainS(solv, soluj);
            string filePath = Environment.CurrentDirectory + "\\" + "Contribution Coefficient\\";
            if (!File.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }

            double aji_ik = 0, aij_jk = 0, aki_ij = 0, akj_ij = 0, aik_jk = 0, ajk_ik = 0;
            double omaga_ij = 0, omaga_ik = 0, omaga_jk = 0, d_omaga_ik_j = 0, d_omaga_jk_i = 0, Via, Vja;


            aji_ik = geo_Model(soluj.Name, solui.Name, solv.Name, state);
            ajk_ik = geo_Model(soluj.Name, solv.Name, solui.Name, state);
            aij_jk = geo_Model(solui.Name, soluj.Name, solv.Name, state);
            aki_ij = geo_Model(solv.Name, solui.Name, soluj.Name, state);
            akj_ij = geo_Model(solv.Name, soluj.Name, solui.Name, state);
            aik_jk = geo_Model(solui.Name, solv.Name, soluj.Name, state);

         

            string fileName = filePath  + "ContributionCoefficient(UEM1).txt";
            string content = string.Format("{0}-{1}: \t {3}, \t {0}-{2}: \t {4} \t in ( {1}-{2})\n" +
                                           "{1}-{0}: \t {8}, \t {1}-{2}: \t {7} \t in ( {2}-{0})\n" +
                                           "{2}-{1}: \t {5}, \t {2}-{0}: \t {6} \t in ( {1}-{0})\n",
                                           solv.Name, solui.Name, soluj.Name, aki_ij, akj_ij, aji_ik, ajk_ik, aij_jk, aik_jk);

            myFunctions.WriteLog(fileName, content);
            if (aki_ij == 0 && akj_ij == 0)
            {
                aki_ij = akj_ij = 0.5;
            }

            Via = (1 + solui.u * (solui.Phi - soluj.Phi) * akj_ij / (aki_ij + akj_ij)) * solui.V;
            Vja = (1 + soluj.u * (soluj.Phi - solui.Phi) * aki_ij / (aki_ij + akj_ij)) * soluj.V;
            omaga_ij = fij * Via * Vja * (aki_ij + akj_ij) / (aki_ij * Via + akj_ij * Vja);
            omaga_ik = fik * solui.V * (1 + solui.u * (solui.Phi - solv.Phi));
            omaga_jk = fjk * soluj.V * (1 + soluj.u * (soluj.Phi - solv.Phi));
            d_omaga_ik_j = aji_ik * omaga_ik * (1 - solui.V / solv.V * (1 + 2 * solui.u * (solui.Phi - solv.Phi)));
            d_omaga_jk_i = aij_jk * omaga_jk * (1 - soluj.V / solv.V * (1 + 2 * soluj.u * (soluj.Phi - solv.Phi)));

        
            
            omaga_ik = fik * solui.V * (1 + solui.u * (solui.Phi - solv.Phi));
            omaga_jk = fjk * soluj.V * (1 + soluj.u * (soluj.Phi - solv.Phi));
            d_omaga_ik_j = aji_ik * omaga_ik * (1 - solui.V / solv.V * (1 + 2 * solui.u * (solui.Phi - solv.Phi)));
            d_omaga_jk_i = aij_jk * omaga_jk * (1 - soluj.V / solv.V * (1 + 2 * soluj.u * (soluj.Phi - solv.Phi)));

      
           

            double chemical_term = omaga_ij - omaga_jk - omaga_ik + d_omaga_ik_j + d_omaga_jk_i;


            

            return 1000 * chemical_term / (R * Tem);


            // Notes：考虑非金属元素的金属态转变会得到失败的结果

        }



    }
}
