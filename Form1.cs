namespace Activity_Interaction_Coefficient_Calculator_UEM1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void tableLayoutPanel2_Paint(object sender, PaintEventArgs e)
        {

        }
        private string getState()
        { 
            if (solidradiobtn.Checked)
            {
                return "solid";
            }
            else return "liquid";
        }
        private bool getEntropy()
        {
            if (yesentropyradiobtn.Checked)
            {
                return true;
            }
            else return false;
        }

        private void filldata_dgV(string k, string i, string j, (string state, bool entropy, double Tem) info, ref int row)
        {
            double Tem = info.Tem;
            bool t = false;
            if (k != string.Empty && i != string.Empty && j != string.Empty)
            {
                Element solv = new Element(k);
                Element solui = new Element(i);
                Element soluj = new Element(j);

                Ternary_melts wagner_ = new Ternary_melts(Tem, info.state, info.entropy);

                MiedemaModel miedemal = new MiedemaModel();
                miedemal.setState(info.state);
                miedemal.setTemperature(info.Tem);
                miedemal.setEntropy(info.entropy);

                double sij_UEM1 = 0;


                sij_UEM1 = wagner_.Activity_Interact_Coefficient_Model(solv, solui, soluj, miedemal.UEM1, info.state);


                Melt m1 = new Melt(k, i, j, Tem);
                t = true;
                if (t)
                {
                    row = +dataGridView1.Rows.Add();
                    dataGridView1["matrix", row].Value = k;
                    dataGridView1["solute_i", row].Value = i;
                    dataGridView1["solute_j", row].Value = j;

                    dataGridView1["entropy", row].Value = getEntropy() ? "Yes" : "No";
                    dataGridView1["JSPS", row].Value = m1.sji;
                    dataGridView1["rank", row].Value = m1.Rank_firstorder;

                    dataGridView1["calculate", row].Value = sij_UEM1;


                    dataGridView1["Temp", row].Value = info.Tem;


                    dataGridView1.Update();


                }

#pragma warning disable CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。
                m1 = null;
#pragma warning restore CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。
#pragma warning disable CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。
                solv = null;
#pragma warning restore CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。
#pragma warning disable CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。
                solui = null;
#pragma warning restore CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。
#pragma warning disable CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。
                soluj = null;
#pragma warning restore CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。
                System.GC.Collect();

            }

        }


        private void display(string k, string i, string j)
        {
            Element Ei = new Element(i);
            Element Ej = new Element(j);
            Element Ek = new Element(k);

            ipbtn.Text = Ei.Phi.ToString();
            inbtn.Text = Ei.N_WS.ToString();
            ivbtn.Text = Ei.V.ToString();


            jpbtn.Text = Ej.Phi.ToString();
            jnbtn.Text = Ej.N_WS.ToString();
            jvbtn.Text = Ej.V.ToString();

            kpbtn.Text = Ek.Phi.ToString();
            knbtn.Text = Ek.N_WS.ToString();
            kvbtn.Text = Ek.V.ToString();

        }
        int row = 0;
        private void calbtn_Click(object sender, EventArgs e)
        {
            DataCenter.Database();
            string k = k_comboBox.Text.Trim();
            bool t1;
            double Tem;

            t1 = double.TryParse(T_comboBox.Text, out Tem);
            if (!t1) { Tem = 1873.0; }
            (string phase, bool entropy, double Tem) info = (getState(), getEntropy(), Tem);
            if (k == string.Empty)
            {
                k = "Fe";
            }
            string i, j;
            i = i_comboBox.Text.Trim();
            j = j_comboBox.Text.Trim();

            display(k, i, j);//显示各元素的Miedema 物性参数

            filldata_dgV(k, i, j, info, ref row);

        }

        private void clearbtn_Click(object sender, EventArgs e)
        {
            inbtn.Text = string.Empty;
            ipbtn.Text = string.Empty;
            ivbtn.Text = string.Empty;
            jnbtn.Text = string.Empty;
            jvbtn.Text = string.Empty;
            jpbtn.Text = string.Empty;
            knbtn.Text = string.Empty;
            kvbtn.Text = string.Empty;
            kpbtn.Text = string.Empty;
            k_comboBox.Text = string.Empty;
            i_comboBox.Text = string.Empty;
            j_comboBox.Text = string.Empty;
            T_comboBox.Text = string.Empty;
            dataGridView1.Rows.Clear();

        }

        private void yesentropyradiobtn_Click(object sender, EventArgs e)
        {
            yesentropyradiobtn.Checked = true;
            noentropyradiobtn.Checked = false;
        }

        private void noentropyradiobtn_Click(object sender, EventArgs e)
        {
            noentropyradiobtn.Checked = true;
            yesentropyradiobtn.Checked = false;
        }

        private void liquidradioButton_Click(object sender, EventArgs e)
        {
            liquidradioButton.Checked = true;
            solidradiobtn.Checked = false;
        }

        private void solidradiobtn_Click(object sender, EventArgs e)
        {
            liquidradioButton.Checked = false;
            solidradiobtn.Checked = true;
        }
    }
}