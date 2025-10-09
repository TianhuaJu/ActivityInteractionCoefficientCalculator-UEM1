using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Activity_Interaction_Coefficient_Calculator_UEM1
{
    [Serializable]
    public  class Element
    {
       
        public double Phi { get; set; }
        public double N_WS { get; set; }
        public double V { get; set; }
        public double u { get; set; }
        public double M { get; set; }
        public double dH_Trans { get; set; }
        public double hybird_Value { get; set; }
        public double Tm { get; set; }
        public double Tb { get; set; }
        public bool isExist { get; set; }
        public String hybird_factor
        {
            get;
            set;
        }
        public string Name { get; set; }
        public Boolean isTrans_group { get; set; }
        
      
     
     
      
      

        public Element( string name )
        {
            
            if (Constant.periodicTable.ContainsKey(name))
            {
               this.isExist = true;
                this.Name = name;
                
                DataCenter.get_MiedemaData(this);
               

            }
            else
            {
                this.isExist = false;
            }
            


        }
     
       
        
       
       

    }
}
