using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Documents;

namespace Activity_Interaction_Coefficient_Calculator_UEM1
{
    public partial class Window1 : Window
    {
        public ObservableCollection<ResultRow> Results { get; set; }

        public Window1()
        {
            InitializeComponent();
            Results = new ObservableCollection<ResultRow>();
            ResultDataGrid.ItemsSource = Results;

            // 初始化 ln γᵢ⁰ 显示
            UpdateGammaSubscript();
        }

        private string getState()
        {
            if (solidradiobtn.IsChecked == true) return "solid";
            return "liquid";
        }

        private bool getEntropy()
        {
            if (yesentropyradiobtn.IsChecked == true) return true;
            return false;
        }

        private void filldata_dgV(string k, string i, string j, (string state, bool entropy, double Tem) info, string modelName)
        {
            if (string.IsNullOrWhiteSpace(k) || string.IsNullOrWhiteSpace(i) || string.IsNullOrWhiteSpace(j))
            {
                System.Windows.MessageBox.Show("Please enter values for Matrix, Solute (i), and Solute (j).");
                return;
            }

            Element solv = new Element(k);
            Element solui = new Element(i);
            Element soluj = new Element(j);

            if (!solv.isExist || !solui.isExist || !soluj.isExist)
            {
                System.Windows.MessageBox.Show("One or more elements could not be found in the database.");
                return;
            }

            Ternary_melts wagner_ = new Ternary_melts(info.Tem, info.state, info.entropy);
            MiedemaModel miedemaModel = new MiedemaModel();
            miedemaModel.setState(info.state);
            miedemaModel.setTemperature(info.Tem);
            miedemaModel.setEntropy(info.entropy);

            Extrapolation_Model selectedModelDelegate = GetModelDelegate(modelName, miedemaModel);
            if (selectedModelDelegate == null)
            {
                System.Windows.MessageBox.Show($"未找到名为 {modelName} 的模型实现。");
                return;
            }

            double sij_UEM1 = wagner_.Activity_Interact_Coefficient_Model(solv, solui, soluj, selectedModelDelegate, info.state);
            Melt m1 = new Melt(k, i, j, info.Tem);

            Results.Add(new ResultRow
            {
                Solvent = k,
                I = i,
                J = j,
                Entropy = getEntropy() ? "Yes" : "No",
                JSPS = m1.sji.ToString(),
                Rank = m1.Rank_firstorder,
                Calculate = sij_UEM1,
                Temp = info.Tem,
                Model = modelName
            });


        }

        private Extrapolation_Model GetModelDelegate(string modelName, MiedemaModel modelInstance)
        {
            switch (modelName)
            {
                case "UEM1": return modelInstance.UEM1;
                case "UEM2": return modelInstance.UEM2;
                case "UEM2_Adv": return modelInstance.UEM2_Adv;
                case "Toop-Kohler": return modelInstance.Toop_Kohler;
                case "Toop-Muggianu": return modelInstance.Toop_Muggianu;
                case "Muggianu": return modelInstance.Muggianu;
                case "GSM": return modelInstance.GSM;
                default: return null;
            }
        }

        private void display(string k, string i, string j)
        {
            if (string.IsNullOrWhiteSpace(k) || string.IsNullOrWhiteSpace(i) || string.IsNullOrWhiteSpace(j)) return;

            Element Ek = new Element(k);
            Element Ei = new Element(i);
            Element Ej = new Element(j);

            if (Ek.isExist)
            {
                kPhi_text.Text = Ek.Phi.ToString("F2");
                kNws_text.Text = Ek.N_WS.ToString("F2");
                kV_text.Text = Ek.V.ToString("F2");
            }
            if (Ei.isExist)
            {
                iPhi_text.Text = Ei.Phi.ToString("F2");
                iNws_text.Text = Ei.N_WS.ToString("F2");
                iV_text.Text = Ei.V.ToString("F2");
            }
            if (Ej.isExist)
            {
                jPhi_text.Text = Ej.Phi.ToString("F2");
                jNws_text.Text = Ej.N_WS.ToString("F2");
                jV_text.Text = Ej.V.ToString("F2");
            }
        }

        private void calbtn_Click(object sender, RoutedEventArgs e)
        {
            DataCenter.Database();
            string k = k_comboBox.Text.Trim();
            if (string.IsNullOrEmpty(k)) { k = "Fe"; k_comboBox.Text = k; }

            string i = i_comboBox.Text.Trim();
            string j = j_comboBox.Text.Trim();

            double Tem;
            if (!double.TryParse(T_comboBox.Text, out Tem)) { Tem = 1873.0; T_comboBox.Text = Tem.ToString(); }

            string modelName = (model_comboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "UEM1";
            (string phase, bool entropy, double Tem) info = (getState(), getEntropy(), Tem);

            display(k, i, j);
            filldata_dgV(k, i, j, info, modelName);

            // 在计算按钮点击后，更新下标和数值
            UpdateGammaSubscript();
            UpdateGammaValue(k, i, info);
        }

        private void clearbtn_Click(object sender, RoutedEventArgs e)
        {
            k_comboBox.Text = string.Empty;
            i_comboBox.Text = string.Empty;
            j_comboBox.Text = string.Empty;
            T_comboBox.Text = string.Empty;
            model_comboBox.SelectedIndex = 0;

            // 清空参数显示
            kPhi_text.Text = string.Empty;
            kNws_text.Text = string.Empty;
            kV_text.Text = string.Empty;
            iPhi_text.Text = string.Empty;
            iNws_text.Text = string.Empty;
            iV_text.Text = string.Empty;
            jPhi_text.Text = string.Empty;
            jNws_text.Text = string.Empty;
            jV_text.Text = string.Empty;

            // 清空 ln γᵢ⁰ 显示
            gammaSubscript_run.Text = "i";
            gammaValue_run.Text = "--";

            Results.Clear();
        }

        private void ManageDatabase_Click(object sender, RoutedEventArgs e)
        {
            DatabaseManagerWindow dbManager = new DatabaseManagerWindow();
            dbManager.Owner = this;
            dbManager.ShowDialog();
        }

        #region ln γᵢ⁰ 显示功能

        // i_comboBox_SelectionChanged 和 i_comboBox_TextInput 方法已被删除

        /// <summary>
        /// 更新 ln γᵢ⁰ 显示中的下标 i
        /// 根据 Solute (i) 输入框的当前值自动更新下标
        /// </summary>
        private void UpdateGammaSubscript()
        {
            if (i_comboBox != null && gammaSubscript_run != null)
            {
                string selectedElement = i_comboBox.Text?.Trim();

                if (!string.IsNullOrEmpty(selectedElement))
                {
                    gammaSubscript_run.Text = selectedElement;
                }
                else
                {
                    gammaSubscript_run.Text = "i";
                }
            }
        }

        /// <summary>
        /// 更新 ln γᵢ⁰ 的计算值
        /// </summary>
        private void UpdateGammaValue(string k, string i, (string state, bool entropy, double Tem) info)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(k) || string.IsNullOrWhiteSpace(i))
                {
                    gammaValue_run.Text = "--";
                    return;
                }

                Element solvent = new Element(k);
                Element solute = new Element(i);

                if (!solvent.isExist || !solute.isExist)
                {
                    gammaValue_run.Text = "--";
                    return;
                }

                double lnGamma = CalculateActivityCoefficient(solvent, solute, info);
                gammaValue_run.Text = lnGamma.ToString("F3");
            }
            catch
            {
                gammaValue_run.Text = "--";
            }
        }

        /// <summary>
        /// 计算活度系数 ln γᵢ⁰ 的具体实现
        /// </summary>
        private double CalculateActivityCoefficient(Element solvent, Element solute, (string state, bool entropy, double Tem) info)
        {
            Ternary_melts melts = new Ternary_melts(info.Tem, info.state, info.entropy);
            double lnyi0 = melts.lngama_i0(solvent.Name, solute.Name, info.Tem, info.state);
            return lnyi0;
        }

        #endregion
    }

    public class ResultRow
    {
        public string Solvent { get; set; }
        public string I { get; set; }
        public string J { get; set; }
        public double Calculate { get; set; }
        public string JSPS { get; set; }
        public string Rank { get; set; }
        public string Entropy { get; set; }
        public double Temp { get; set; }
        public string Model { get; set; }
    }
}