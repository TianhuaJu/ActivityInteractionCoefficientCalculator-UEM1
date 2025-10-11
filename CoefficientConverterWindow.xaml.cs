using System;
using System.Windows;
using System.Windows.Controls;

namespace Activity_Interaction_Coefficient_Calculator_UEM1
{
    /// <summary>
    /// 活度相互作用系数转换窗口
    /// 用于在摩尔分数表示和质量分数表示的活度相互作用系数之间进行转换
    /// </summary>
    public partial class CoefficientConverterWindow : Window
    {
        public CoefficientConverterWindow()
        {
            InitializeComponent();

            // 初始化系数符号显示
            UpdateCoefficientSymbol();

            // 为ComboBox添加SelectionChanged事件以动态更新符号
            // 注意：ComboBox没有TextChanged事件，只有SelectionChanged
            SoluteI_ComboBox.SelectionChanged += (s, e) => UpdateCoefficientSymbol();
            SoluteJ_ComboBox.SelectionChanged += (s, e) => UpdateCoefficientSymbol();

            // 如果需要监听可编辑ComboBox的文本输入，需要访问其内部TextBox
            if (SoluteI_ComboBox.Template != null)
            {
                SoluteI_ComboBox.Loaded += (s, e) =>
                {
                    var textBox = SoluteI_ComboBox.Template.FindName("PART_EditableTextBox", SoluteI_ComboBox) as TextBox;
                    if (textBox != null)
                    {
                        textBox.TextChanged += (sender, args) => UpdateCoefficientSymbol();
                    }
                };
            }

            if (SoluteJ_ComboBox.Template != null)
            {
                SoluteJ_ComboBox.Loaded += (s, e) =>
                {
                    var textBox = SoluteJ_ComboBox.Template.FindName("PART_EditableTextBox", SoluteJ_ComboBox) as TextBox;
                    if (textBox != null)
                    {
                        textBox.TextChanged += (sender, args) => UpdateCoefficientSymbol();
                    }
                };
            }
        }

        /// <summary>
        /// 转换按钮点击事件
        /// </summary>
        private void ConvertBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 验证输入
                if (!ValidateInputs(out string errorMessage))
                {
                    MessageBox.Show(errorMessage, "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 获取输入参数
                string matrixElement = Matrix_ComboBox.Text.Trim();
                string soluteI = SoluteI_ComboBox.Text.Trim();
                string soluteJ = SoluteJ_ComboBox.Text.Trim();
                double inputCoefficient = double.Parse(InputCoefficient_TextBox.Text.Trim());

                // 确定转换方向
                bool isMoleToMass = MoleToMass_RadioButton.IsChecked == true;

                // 执行转换
                double result = PerformConversion(matrixElement, soluteI, soluteJ, inputCoefficient, isMoleToMass);

                // 显示结果
                DisplayResult(matrixElement, soluteI, soluteJ, inputCoefficient, result, isMoleToMass);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"转换过程中发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 验证用户输入
        /// </summary>
        private bool ValidateInputs(out string errorMessage)
        {
            errorMessage = string.Empty;

            // 检查基体组分
            if (string.IsNullOrWhiteSpace(Matrix_ComboBox.Text))
            {
                errorMessage = "请输入基体组分 (Matrix k)";
                return false;
            }

            // 检查溶质组分 i
            if (string.IsNullOrWhiteSpace(SoluteI_ComboBox.Text))
            {
                errorMessage = "请输入溶质组分 i";
                return false;
            }

            // 检查溶质组分 j
            if (string.IsNullOrWhiteSpace(SoluteJ_ComboBox.Text))
            {
                errorMessage = "请输入溶质组分 j";
                return false;
            }

            // 检查系数值
            if (string.IsNullOrWhiteSpace(InputCoefficient_TextBox.Text))
            {
                errorMessage = "请输入待转换的系数值";
                return false;
            }

            // 验证系数值是否为有效数字
            if (!double.TryParse(InputCoefficient_TextBox.Text.Trim(), out _))
            {
                errorMessage = "系数值必须是有效的数字";
                return false;
            }

            // 验证元素是否存在于数据库中
            Element matrix = new Element(Matrix_ComboBox.Text.Trim());
            Element solI = new Element(SoluteI_ComboBox.Text.Trim());
            Element solJ = new Element(SoluteJ_ComboBox.Text.Trim());

            if (!matrix.isExist)
            {
                errorMessage = $"基体组分 '{Matrix_ComboBox.Text.Trim()}' 在数据库中不存在";
                return false;
            }

            if (!solI.isExist)
            {
                errorMessage = $"溶质组分 i '{SoluteI_ComboBox.Text.Trim()}' 在数据库中不存在";
                return false;
            }

            if (!solJ.isExist)
            {
                errorMessage = $"溶质组分 j '{SoluteJ_ComboBox.Text.Trim()}' 在数据库中不存在";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 执行活度相互作用系数转换
        /// </summary>
        /// <param name="matrixElement">基体组分</param>
        /// <param name="soluteI">溶质组分 i</param>
        /// <param name="soluteJ">溶质组分 j</param>
        /// <param name="inputCoefficient">待转换的系数值</param>
        /// <param name="isMoleToMass">true: 摩尔分数→质量分数; false: 质量分数→摩尔分数</param>
        /// <returns>转换后的系数值</returns>
        private double PerformConversion(string matrixElement, string soluteI, string soluteJ,
                                        double inputCoefficient, bool isMoleToMass)
        {
            // 创建元素对象
            Element matrix = new Element(matrixElement);
            Element solI = new Element(soluteI);
            Element solJ = new Element(soluteJ);

            double result;

            if (isMoleToMass)
            {
                // 摩尔分数 → 质量分数
                result = myFunctions.first_order_mTow(inputCoefficient, solJ, matrix);
            }
            else
            {
                // 质量分数 → 摩尔分数
                result = myFunctions.first_order_w2m(inputCoefficient, solJ, matrix);
            }

            return result;
        }

        /// <summary>
        /// 显示转换结果
        /// </summary>
        private void DisplayResult(string matrixElement, string soluteI, string soluteJ,
                                  double inputCoefficient, double result, bool isMoleToMass)
        {
            // 显示输入信息
            string inputType = isMoleToMass ? "摩尔分数表示" : "质量分数表示";
            string symbol = isMoleToMass ? "ε" : "e";
            InputInfo_Run.Text = $"{symbol}{soluteJ}{soluteI}({matrixElement}) = {inputCoefficient:F6} ({inputType})";

            // 显示转换方向
            ConversionDirection_Run.Text = isMoleToMass ? "摩尔分数 → 质量分数" : "质量分数 → 摩尔分数";

            // 显示结果
            ResultValue_Run.Text = result.ToString("F6");

            // 显示计算详情
            Element matrix = new Element(matrixElement);
            Element solI = new Element(soluteI);
            Element solJ = new Element(soluteJ);

            string details = $"基体: {matrixElement} (M = {matrix.M:F2} g/mol), " +
                           $"溶质i: {soluteI} (M = {solI.M:F2} g/mol), " +
                           $"溶质j: {soluteJ} (M = {solJ.M:F2} g/mol)";

            CalculationDetails_TextBlock.Text = details;
        }

        /// <summary>
        /// 清空按钮点击事件
        /// </summary>
        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            // 清空输入
            Matrix_ComboBox.Text = string.Empty;
            SoluteI_ComboBox.Text = string.Empty;
            SoluteJ_ComboBox.Text = string.Empty;
            InputCoefficient_TextBox.Text = string.Empty;

            // 重置转换方向为默认值
            MoleToMass_RadioButton.IsChecked = true;

            // 清空结果显示
            InputInfo_Run.Text = "--";
            ConversionDirection_Run.Text = "--";
            ResultValue_Run.Text = "--";
            CalculationDetails_TextBlock.Text = string.Empty;

            // 重置符号显示
            UpdateCoefficientSymbol();
        }

        /// <summary>
        /// 关闭按钮点击事件
        /// </summary>
        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// 转换方向改变时的事件处理
        /// </summary>
        private void ConversionDirection_Changed(object sender, RoutedEventArgs e)
        {
            UpdateCoefficientSymbol();
        }

        /// <summary>
        /// 更新系数符号显示（带严格上下标）
        /// 显示为 ε^j_i 或 e^j_i 的形式
        /// </summary>
        private void UpdateCoefficientSymbol()
        {
            if (MainSymbol_TextBlock == null || Subscript_TextBlock == null || Superscript_TextBlock == null)
                return;

            string soluteI = SoluteI_ComboBox?.Text?.Trim() ?? "";
            string soluteJ = SoluteJ_ComboBox?.Text?.Trim() ?? "";

            // 确定使用 ε 还是 e
            if (MoleToMass_RadioButton?.IsChecked == true)
            {
                // 摩尔分数 → 质量分数：使用 ε (epsilon)
                MainSymbol_TextBlock.Text = "ε";
            }
            else
            {
                // 质量分数 → 摩尔分数：使用 e
                MainSymbol_TextBlock.Text = "e";
            }

            // 更新下标（溶质i）
            if (!string.IsNullOrEmpty(soluteI))
            {
                Subscript_TextBlock.Text = soluteI;
            }
            else
            {
                Subscript_TextBlock.Text = "i";
            }

            // 更新上标（溶质j）
            if (!string.IsNullOrEmpty(soluteJ))
            {
                Superscript_TextBlock.Text = soluteJ;
            }
            else
            {
                Superscript_TextBlock.Text = "j";
            }
        }
    }
}
