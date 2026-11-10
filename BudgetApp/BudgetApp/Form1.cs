using System;
using System.Data;
using System.IO;
using System.Windows.Forms;

namespace BudgetApp
{
    public partial class Form1 : Form
    {
        private string xmlFile = "budget.xml";
        private DataTable dtTransactions;

        public Form1()
        {
            InitializeComponent();
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            try
            {
                dtTransactions = new DataTable("Transactions");
                dtTransactions.Columns.Add("Id", typeof(int));
                dtTransactions.Columns.Add("Description", typeof(string));
                dtTransactions.Columns.Add("Amount", typeof(double));
                dtTransactions.Columns.Add("Category", typeof(string));
                dtTransactions.Columns.Add("TransactionDate", typeof(string));

                if (File.Exists(xmlFile))
                {
                    dtTransactions.ReadXml(xmlFile);
                }

                dgvTransactions.DataSource = dtTransactions;
                CalculateBalance();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка инициализации данных: " + ex.Message);
            }
        }

        private void CalculateBalance()
        {
            double balance = 0;
            foreach (DataRow row in dtTransactions.Rows)
            {
                if (row["Amount"] != DBNull.Value)
                {
                    balance += Convert.ToDouble(row["Amount"]);
                }
            }
            lblBalance.Text = $"Текущий баланс: {balance:F2} руб.";
        }

        private void InsertTransaction(string description, double amount, string category)
        {
            int nextId = dtTransactions.Rows.Count + 1;
            dtTransactions.Rows.Add(nextId, description, amount, category, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            dtTransactions.WriteXml(xmlFile);
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtDescription.Text))
            {
                MessageBox.Show("Введите описание транзакции!");
                return;
            }

            if (cmbCategory.SelectedItem == null)
            {
                MessageBox.Show("Выберите категорию!");
                return;
            }

            if (!double.TryParse(txtAmount.Text, out double amount) || amount == 0)
            {
                MessageBox.Show("Введите корректную сумму (не равную 0)!");
                return;
            }

            InsertTransaction(txtDescription.Text, amount, cmbCategory.SelectedItem.ToString());

            txtDescription.Clear();
            txtAmount.Clear();
            cmbCategory.SelectedIndex = -1;

            CalculateBalance();
        }

        private void txtAmount_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != ',') && (e.KeyChar != '-'))
            {
                e.Handled = true;
            }

            if ((e.KeyChar == ',') && ((sender as TextBox).Text.IndexOf(',') > -1))
            {
                e.Handled = true;
            }

            if ((e.KeyChar == '-') && ((sender as TextBox).Text.Length > 0))
            {
                e.Handled = true;
            }
        }
    }
}
