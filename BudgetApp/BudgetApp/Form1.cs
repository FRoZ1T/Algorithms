using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace BudgetApp
{
    public partial class Form1 : Form
    {
        private string xmlFile = "budget.xml";
        private DataTable dtTransactions;
        private int selectedRowId = -1;

        public Form1()
        {
            InitializeComponent();
            InitializeDatabase();
            if (cmbFilterCategory.Items.Count > 0)
            {
                cmbFilterCategory.SelectedIndex = 0;
            }
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
                    try
                    {
                        dtTransactions.ReadXml(xmlFile);
                    }
                    catch
                    {
                        File.Delete(xmlFile);
                    }
                }

                dgvTransactions.AutoGenerateColumns = true;
                dgvTransactions.DataSource = dtTransactions;

                if (dgvTransactions.Columns.Count > 0)
                {
                    dgvTransactions.Columns["Id"].Visible = false;
                    dgvTransactions.Columns["Description"].HeaderText = "Описание";
                    dgvTransactions.Columns["Amount"].HeaderText = "Сумма (₽)";
                    dgvTransactions.Columns["Category"].HeaderText = "Категория";
                    dgvTransactions.Columns["TransactionDate"].HeaderText = "Дата операции";
                }

                UpdateFinancialStats();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка данных: " + ex.Message);
            }
        }

        private void UpdateFinancialStats()
        {
            if (dtTransactions == null) return;

            double totalBalance = 0;
            Dictionary<string, double> expenses = new Dictionary<string, double>();

            foreach (DataRow row in dtTransactions.Rows)
            {
                if (row.RowState == DataRowState.Deleted) continue;

                if (row["Amount"] != DBNull.Value)
                {
                    double amt = Convert.ToDouble(row["Amount"]);
                    totalBalance += amt;

                    if (amt < 0)
                    {
                        string cat = row["Category"].ToString();
                        if (expenses.ContainsKey(cat)) expenses[cat] += Math.Abs(amt);
                        else expenses[cat] = Math.Abs(amt);
                    }
                }
            }

            lblBalance.Text = $"Текущий баланс: {totalBalance:F2} ₽";
            if (totalBalance >= 0) lblBalance.ForeColor = Color.FromArgb(46, 204, 113);
            else lblBalance.ForeColor = Color.FromArgb(231, 76, 60);

            string maxExpenseCategory = "Нет расходов";
            double maxExpenseValue = 0;

            foreach (var kvp in expenses)
            {
                if (kvp.Value > maxExpenseValue)
                {
                    maxExpenseValue = kvp.Value;
                    maxExpenseCategory = kvp.Key;
                }
            }

            if (maxExpenseValue > 0)
            {
                lblAnalytics.Text = $"📊 Аналитика: Больше всего вы тратите на \"{maxExpenseCategory}\" ({maxExpenseValue:F2} ₽)";
            }
            else
            {
                lblAnalytics.Text = "📊 Аналитика расходов: Данных о расходах пока нет";
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtDescription.Text) || cmbCategory.SelectedItem == null || !double.TryParse(txtAmount.Text, out double amount) || amount == 0)
            {
                MessageBox.Show("Заполните корректно все поля!");
                return;
            }

            string category = cmbCategory.SelectedItem.ToString();

            amount = Math.Abs(amount);
            if (category != "Зарплата")
            {
                amount = -amount;
            }

            int nextId = 1;
            if (dtTransactions.Rows.Count > 0)
            {
                object maxObj = dtTransactions.Compute("MAX(Id)", "");
                if (maxObj != DBNull.Value && maxObj != null)
                {
                    nextId = Convert.ToInt32(maxObj) + 1;
                }
            }

            dtTransactions.Rows.Add(nextId, txtDescription.Text, amount, category, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            dtTransactions.WriteXml(xmlFile);

            ClearFields();
            UpdateFinancialStats();
        }

        private void dgvTransactions_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvTransactions.SelectedRows.Count > 0)
            {
                DataGridViewRow row = dgvTransactions.SelectedRows[0];
                if (row.Cells["Id"].Value != DBNull.Value && row.Cells["Id"].Value != null)
                {
                    selectedRowId = Convert.ToInt32(row.Cells["Id"].Value);
                    txtDescription.Text = row.Cells["Description"].Value?.ToString();

                    double amt = Convert.ToDouble(row.Cells["Amount"].Value);
                    txtAmount.Text = Math.Abs(amt).ToString();

                    cmbCategory.SelectedItem = row.Cells["Category"].Value?.ToString();
                }
            }
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (selectedRowId == -1)
            {
                MessageBox.Show("Выберите строку в таблице для изменения!");
                return;
            }

            DataRow[] rows = dtTransactions.Select($"Id = {selectedRowId}");
            if (rows.Length > 0)
            {
                if (string.IsNullOrWhiteSpace(txtDescription.Text) || cmbCategory.SelectedItem == null || !double.TryParse(txtAmount.Text, out double amount) || amount == 0)
                {
                    MessageBox.Show("Заполните корректно все поля!");
                    return;
                }

                string category = cmbCategory.SelectedItem.ToString();
                amount = Math.Abs(amount);
                if (category != "Зарплата")
                {
                    amount = -amount;
                }

                rows[0]["Description"] = txtDescription.Text;
                rows[0]["Amount"] = amount;
                rows[0]["Category"] = category;
                dtTransactions.WriteXml(xmlFile);

                ClearFields();
                UpdateFinancialStats();
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (selectedRowId == -1)
            {
                MessageBox.Show("Выберите строку в таблице для удаления!");
                return;
            }

            DataRow[] rows = dtTransactions.Select($"Id = {selectedRowId}");
            if (rows.Length > 0)
            {
                rows[0].Delete();
                dtTransactions.WriteXml(xmlFile);

                ClearFields();
                UpdateFinancialStats();
            }
        }

        private void btnDeleteAll_Click(object sender, EventArgs e)
        {
            if (dtTransactions.Rows.Count == 0)
            {
                MessageBox.Show("Таблица и так пуста!");
                return;
            }

            var dialogResult = MessageBox.Show("Вы уверены, что хотите полностью очистить всю базу данных трат?", "Подтверждение удаления", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (dialogResult == DialogResult.Yes)
            {
                dtTransactions.Rows.Clear();
                if (File.Exists(xmlFile))
                {
                    File.Delete(xmlFile);
                }
                ClearFields();
                UpdateFinancialStats();
                MessageBox.Show("База данных успешно очищена!");
            }
        }

        private void cmbFilterCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (dtTransactions == null) return;

            if (cmbFilterCategory.SelectedIndex == 0)
            {
                dtTransactions.DefaultView.RowFilter = "";
            }
            else
            {
                string selectedCat = cmbFilterCategory.SelectedItem.ToString();
                dtTransactions.DefaultView.RowFilter = $"Category = '{selectedCat}'";
            }
            if (dgvTransactions.DataSource != null) dgvTransactions.ClearSelection();
        }

        private void ClearFields()
        {
            txtDescription.Clear();
            txtAmount.Clear();
            if (cmbCategory.Items.Count > 0) cmbCategory.SelectedIndex = -1;
            selectedRowId = -1;
            if (dgvTransactions.DataSource != null) dgvTransactions.ClearSelection();
        }

        private void txtAmount_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != ','))
            {
                e.Handled = true;
            }
            if ((e.KeyChar == ',') && ((sender as TextBox).Text.IndexOf(',') > -1))
            {
                e.Handled = true;
            }
        }

        private void lblFilter_Click(object sender, EventArgs e)
        {

        }
    }
}