using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.Odbc;
using System.Data.SqlClient;

namespace CostOfOutsideProcesses
{
    public partial class Form1 : Form
    {
        private BindingSource detailsBindingSource = new BindingSource();
        private BindingSource totalsBindingSource = new BindingSource();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            detailsDataGridView.DataSource = detailsBindingSource;
            totalsDataGridView.DataSource = totalsBindingSource;

            includeAllOpsCheckBox.Checked = true;
            groupByJobsCheckBox.Checked = true;

            // set up date time pickers
            startDateTimePicker.Value = DateTime.Now.AddYears(-1).AddMonths(-3).AddHours(-DateTime.Now.Hour).AddMinutes(-DateTime.Now.Minute).AddSeconds(-DateTime.Now.Second);
            endDateTimePicker.Value = startDateTimePicker.Value.AddYears(1);

            startDateTimePicker.ValueChanged += UpdateDetailsDataGridview;
            startDateTimePicker.ValueChanged += UpdateTotalsDataGridview;
            endDateTimePicker.ValueChanged += UpdateDetailsDataGridview;
            endDateTimePicker.ValueChanged += UpdateTotalsDataGridview;

            includeAllOpsCheckBox.CheckedChanged += UpdateDetailsDataGridview;
            includeAllOpsCheckBox.CheckedChanged += UpdateTotalsDataGridview;

            groupByJobsCheckBox.CheckedChanged += UpdateDetailsDataGridview;
            groupByJobsCheckBox.CheckedChanged += UpdateTotalsDataGridview;

            UpdateDetailsDataGridview(new object(), new EventArgs());
            UpdateTotalsDataGridview(new object(), new EventArgs());
        }

        private void UpdateDetailsDataGridview(object obj, EventArgs args)
        {
            if (startDateTimePicker.Value > endDateTimePicker.Value)
            {
                MessageBox.Show("Start date cannot be after end date", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            detailsDataGridView.Refresh();

            using (OdbcConnection conn = new OdbcConnection(Globals.odbc_connection_string))
            {
                conn.Open();

                string query = string.Empty;

                if (includeAllOpsCheckBox.Checked)
                {
                    if (groupByJobsCheckBox.Checked)
                    {
                        query =
                            "SELECT ncT.NCR AS 'NC No', ncT.NCR_Date AS 'NC Date', ncT.Job, '' AS [Op No], '' AS Sequence,\n" +
                            "ran_qty_job_operationT.ran_Qty AS 'Job Run Qty', '' AS 'Work Center',\n" +
                            "'' AS 'Outside Op',\n" +
                            "ncT.Qty_Scrap AS 'NC Scrap Qty',\n" +
                            "SUM(CASE\n" +
                                "\tWHEN job_operationT.Sequence = lower_sequence_job_operationT.Sequence THEN po_detailT.Act_Cost / po_detailT.Order_Quantity * ncT.Qty_Scrap\n" +
                                "\tWHEN lower_sequence_job_operationT.Act_Run_Qty + lower_sequence_job_operationT.Act_Scrap_Qty = 0 THEN 0\n" +
                                "\tELSE(lower_sequence_job_operationT.Act_Run_Labor + lower_sequence_job_operationT.Act_Machine_Burden + lower_sequence_job_operationT.Act_Labor_Burden) / (lower_sequence_job_operationT.Act_Run_Qty + lower_sequence_job_operationT.Act_Scrap_Qty) * ncT.Qty_Scrap END\n" +
                            ") AS 'Total Scrap Cost',\n" +
                            "SUM(CASE\n" +
                                "\tWHEN job_operationT.Sequence = lower_sequence_job_operationT.Sequence THEN(po_detailT.Act_Cost + po_detailT.Addl_Cost_Act_Amt1 + po_detailT.Addl_Cost_Est_Amt2) / po_detailT.Order_Quantity * ncT.Qty_Scrap\n" +
                                "\tWHEN lower_sequence_job_operationT.Act_Run_Qty + lower_sequence_job_operationT.Act_Scrap_Qty = 0 THEN 0\n" +
                                "\tELSE(lower_sequence_job_operationT.Act_Run_Labor + lower_sequence_job_operationT.Act_Machine_Burden + lower_sequence_job_operationT.Act_Labor_Burden) / (lower_sequence_job_operationT.Act_Run_Qty + lower_sequence_job_operationT.Act_Scrap_Qty) * ncT.Qty_Scrap END\n" +
                            ") AS 'Total Scrap Cost (Including Extras)'\n" +
                            "FROM uniPoint_Live.dbo.PT_NC AS ncT\n" +
                            "LEFT JOIN PRODUCTION.dbo.PO_Detail AS po_detailT\n" +
                            "ON po_detailT.PO = RTRIM(LTRIM(ncT.PO))\n" +
                            "LEFT JOIN PRODUCTION.dbo.Source AS sourceT\n" +
                            "ON sourceT.PO_Detail = po_detailT.PO_Detail\n" +
                            "LEFT JOIN PRODUCTION.dbo.Job_Operation AS job_operationT\n" +
                            "ON job_operationT.Job_Operation = sourceT.Job_Operation\n" +
                            "LEFT JOIN PRODUCTION.dbo.Job_Operation AS lower_sequence_job_operationT\n" +
                            "ON job_operationT.Job = lower_sequence_job_operationT.Job AND lower_sequence_job_operationT.Sequence <= job_operationT.Sequence\n" +
                            "LEFT JOIN\n" +
                                "\t(\n" +
                                "\tSELECT innerTJob_operation.Job, MAX(innerTJob_operation.Run_Qty) AS ran_Qty\n" +
                                "\tFROM PRODUCTION.dbo.Job_Operation AS innerTJob_operation\n" +
                                "\tGROUP BY innerTJob_operation.Job\n" +
                                "\t) AS ran_qty_job_operationT\n" +
                            "ON ran_qty_job_operationT.Job = job_operationT.Job\n" +
                            "WHERE ncT.NCR_Date >= CONVERT(DATETIME, '" + startDateTimePicker.Value.ToString() + "') AND ncT.NCR_Date < CONVERT(DATETIME, '" + endDateTimePicker.Value.ToString() + "') AND ncT.NC_type LIKE '%Vendor%' AND ncT.Status = 'Closed' AND ncT.Qty_Scrap > 0\n" +
                                "\tAND ncT.Job = job_operationT.Job\n" +
                            "GROUP BY ncT.NCR, ncT.NCR_Date, ncT.Job, ran_qty_job_operationT.ran_Qty, ncT.Qty_Scrap\n" +
                            "ORDER BY ncT.NCR;";
                    }
                    else
                    {
                        query =
                            "SELECT ncT.NCR AS 'NC No', ncT.NCR_Date AS 'NC Date', ncT.Job, lower_sequence_job_operationT.Operation_Service AS 'Op No', lower_sequence_job_operationT.Sequence,\n" +
                            "lower_sequence_job_operationT.Act_Run_Qty AS 'Op Run Qty', lower_sequence_job_operationT.Work_Center AS 'Work Center',\n" +
                            "(CASE\n" +
                                "\tWHEN job_operationT.Sequence = lower_sequence_job_operationT.Sequence\n" +
                                "\tTHEN 'Yes'\n" +
                                "\tELSE 'No' END\n" +
                            ") AS 'Outside Op',\n" +
                            "ncT.Qty_Scrap AS 'NC Scrap Qty',\n"+
                            "(CASE\n" +
                                "\tWHEN job_operationT.Sequence = lower_sequence_job_operationT.Sequence THEN po_detailT.Act_Cost / po_detailT.Order_Quantity * ncT.Qty_Scrap\n" +
                                "\tWHEN lower_sequence_job_operationT.Act_Run_Qty + lower_sequence_job_operationT.Act_Scrap_Qty = 0 THEN 0\n" +
                                "\tELSE(lower_sequence_job_operationT.Act_Run_Labor + lower_sequence_job_operationT.Act_Machine_Burden + lower_sequence_job_operationT.Act_Labor_Burden) / (lower_sequence_job_operationT.Act_Run_Qty + lower_sequence_job_operationT.Act_Scrap_Qty) * ncT.Qty_Scrap END\n" +
                            ") AS 'Total Scrap Cost',\n" +
                            "(CASE\n" +
                                "\tWHEN job_operationT.Sequence = lower_sequence_job_operationT.Sequence THEN(po_detailT.Act_Cost + po_detailT.Addl_Cost_Act_Amt1 + po_detailT.Addl_Cost_Est_Amt2) / po_detailT.Order_Quantity * ncT.Qty_Scrap\n" +
                                "\tWHEN lower_sequence_job_operationT.Act_Run_Qty + lower_sequence_job_operationT.Act_Scrap_Qty = 0 THEN 0\n" +
                                "\tELSE(lower_sequence_job_operationT.Act_Run_Labor + lower_sequence_job_operationT.Act_Machine_Burden + lower_sequence_job_operationT.Act_Labor_Burden) / (lower_sequence_job_operationT.Act_Run_Qty + lower_sequence_job_operationT.Act_Scrap_Qty) * ncT.Qty_Scrap END\n" +
                            ") AS 'Total Scrap Cost (Including Extras)'\n" +
                            "FROM uniPoint_Live.dbo.PT_NC AS ncT\n" +
                            "LEFT JOIN PRODUCTION.dbo.PO_Detail AS po_detailT\n" +
                            "ON po_detailT.PO = RTRIM(LTRIM(ncT.PO))\n" +
                            "LEFT JOIN PRODUCTION.dbo.Source AS sourceT\n" +
                            "ON sourceT.PO_Detail = po_detailT.PO_Detail\n" +
                            "LEFT JOIN PRODUCTION.dbo.Job_Operation AS job_operationT\n" +
                            "ON job_operationT.Job_Operation = sourceT.Job_Operation\n" +
                            "LEFT JOIN PRODUCTION.dbo.Job_Operation AS lower_sequence_job_operationT\n" +
                            "ON job_operationT.Job = lower_sequence_job_operationT.Job AND lower_sequence_job_operationT.Sequence <= job_operationT.Sequence\n" +
                            "WHERE ncT.NCR_Date >= CONVERT(DATETIME, '" + startDateTimePicker.Value.ToString() + "') AND ncT.NCR_Date < CONVERT(DATETIME, '" + endDateTimePicker.Value.ToString() + "') AND ncT.NC_type LIKE '%Vendor%' AND ncT.Status = 'Closed' AND ncT.Qty_Scrap > 0\n" +
                                "\tAND ncT.Job = job_operationT.Job\n" +
                            "ORDER BY ncT.NCR, lower_sequence_job_operationT.Sequence;";
                    }
                }
                else
                {
                    query =
                    "SELECT ncT.NCR AS 'NC No', ncT.NCR_Date AS 'NC Date', ncT.Qty_total AS 'NC Total Qty', ncT.Disposition, ncT.Qty_Scrap AS 'Scrap Qty', ncT.PO, po_detailT.PO_Detail AS 'PO Detail No',\n" +
                    "sourceT.SourceKey, job_operationT.Job, job_operationT.Operation_Service AS 'Op No', job_operationT.Sequence,\n" +
                    "po_detailT.Vendor_Reference AS 'Vendor Reference', po_detailT.Act_Cost AS 'Total Cost', po_detailT.Unit_Cost AS 'UnitW Cost', po_detailT.Order_Quantity AS 'Order Qty', po_detailT.Act_Cost / po_detailT.Order_Quantity * ncT.Qty_Scrap  AS 'Total Scrap Cost',\n" +
                    "(po_detailT.Act_Cost + po_detailT.Addl_Cost_Act_Amt1 + po_detailT.Addl_Cost_Est_Amt2) / po_detailT.Order_Quantity * ncT.Qty_Scrap AS 'Total Scrap Cost (Including extras)'\n" +
                    "FROM uniPoint_Live.dbo.PT_NC AS ncT\n" +
                    "LEFT JOIN PRODUCTION.dbo.PO_Detail AS po_detailT\n" +
                    "ON po_detailT.PO = RTRIM(LTRIM(ncT.PO))\n" +
                    "LEFT JOIN PRODUCTION.dbo.Source AS sourceT\n" +
                    "ON sourceT.PO_Detail = po_detailT.PO_Detail\n" +
                    "LEFT JOIN PRODUCTION.dbo.Job_Operation AS job_operationT\n" +
                    "ON job_operationT.Job_Operation = sourceT.Job_Operation\n" +
                    "WHERE ncT.NCR_Date >= CONVERT(DATETIME, '" + startDateTimePicker.Value.ToString() + "') AND ncT.NCR_Date < CONVERT(DATETIME, '" + endDateTimePicker.Value + "') AND ncT.NC_type LIKE '%Vendor%' AND ncT.Status = 'Closed' AND ncT.Qty_Scrap > 0\n" +
                    "\tAND ncT.Job = job_operationT.Job\n" +
                    "ORDER BY ncT.NCR;";
                }

                detailsBindingSource.DataSource = null;

                SqlDataAdapter dataAdapter = new SqlDataAdapter(query, Globals.binding_connection_string);

                SqlCommandBuilder commandBuilder = new SqlCommandBuilder(dataAdapter);
                DataTable dt = new DataTable();
                dataAdapter.Fill(dt);
                detailsBindingSource.DataSource = dt;

                detailsDataGridView.Columns["Total Scrap Cost"].DefaultCellStyle.Format = "c";
                detailsDataGridView.Columns["Total Scrap Cost (Including Extras)"].DefaultCellStyle.Format = "c";

                rowLabel.Text = "Rows: " + detailsDataGridView.Rows.Count;
            }
        }

        private void UpdateTotalsDataGridview(object obj, EventArgs args)
        {
            if (startDateTimePicker.Value > endDateTimePicker.Value)
            {
                MessageBox.Show("Start date cannot be after end date", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (OdbcConnection conn = new OdbcConnection(Globals.odbc_connection_string))
            {
                conn.Open();

                string query = string.Empty;

                if (includeAllOpsCheckBox.Checked)
                {
                    query =
                        "SELECT\n" +
                        "SUM(CASE\n" +
                            "\tWHEN job_operationT.Sequence = lower_sequence_job_operationT.Sequence THEN po_detailT.Act_Cost / po_detailT.Order_Quantity * ncT.Qty_Scrap\n" +
                            "\tWHEN lower_sequence_job_operationT.Act_Run_Qty + lower_sequence_job_operationT.Act_Scrap_Qty = 0 THEN 0\n" +
                            "\tELSE(lower_sequence_job_operationT.Act_Run_Labor + lower_sequence_job_operationT.Act_Machine_Burden + lower_sequence_job_operationT.Act_Labor_Burden) / (lower_sequence_job_operationT.Act_Run_Qty + lower_sequence_job_operationT.Act_Scrap_Qty) * ncT.Qty_Scrap END\n" +
                        ") AS 'Total Scrap Cost',\n" +
                        "SUM(CASE\n" +
                            "\tWHEN job_operationT.Sequence = lower_sequence_job_operationT.Sequence THEN(po_detailT.Act_Cost + po_detailT.Addl_Cost_Act_Amt1 + po_detailT.Addl_Cost_Est_Amt2) / po_detailT.Order_Quantity * ncT.Qty_Scrap\n" +
                            "\tWHEN lower_sequence_job_operationT.Act_Run_Qty + lower_sequence_job_operationT.Act_Scrap_Qty = 0 THEN 0\n" +
                            "\tELSE(lower_sequence_job_operationT.Act_Run_Labor + lower_sequence_job_operationT.Act_Machine_Burden + lower_sequence_job_operationT.Act_Labor_Burden) / (lower_sequence_job_operationT.Act_Run_Qty + lower_sequence_job_operationT.Act_Scrap_Qty) * ncT.Qty_Scrap END\n" +
                        ") AS 'Total Scrap Cost (Including Extras)'\n" +
                        "FROM uniPoint_Live.dbo.PT_NC AS ncT\n" +
                        "LEFT JOIN PRODUCTION.dbo.PO_Detail AS po_detailT\n" +
                        "ON po_detailT.PO = RTRIM(LTRIM(ncT.PO))\n" +
                        "LEFT JOIN PRODUCTION.dbo.Source AS sourceT\n" +
                        "ON sourceT.PO_Detail = po_detailT.PO_Detail\n" +
                        "LEFT JOIN PRODUCTION.dbo.Job_Operation AS job_operationT\n" +
                        "ON job_operationT.Job_Operation = sourceT.Job_Operation\n" +
                        "LEFT JOIN PRODUCTION.dbo.Job_Operation AS lower_sequence_job_operationT\n" +
                        "ON job_operationT.Job = lower_sequence_job_operationT.Job AND lower_sequence_job_operationT.Sequence <= job_operationT.Sequence\n" +
                        "WHERE ncT.NCR_Date >= CONVERT(DATETIME, '" + startDateTimePicker.Value.ToString() + "') AND ncT.NCR_Date < CONVERT(DATETIME, '" + endDateTimePicker.Value.ToString() + "') AND ncT.NC_type LIKE '%Vendor%' AND ncT.Status = 'Closed' AND ncT.Qty_Scrap > 0\n" +
                            "\tAND ncT.Job = job_operationT.Job;";
                }
                else
                {
                    query =
                        "SELECT SUM(po_detailT.Act_Cost / po_detailT.Order_Quantity * ncT.Qty_Scrap) AS 'Total Scrap Cost',\n" +
                        "SUM((po_detailT.Act_Cost + po_detailT.Addl_Cost_Act_Amt1 + po_detailT.Addl_Cost_Est_Amt2) / po_detailT.Order_Quantity * ncT.Qty_Scrap) AS 'Total Scrap Cost (Including Extras)'\n" +
                        "FROM uniPoint_Live.dbo.PT_NC AS ncT\n" +
                        "LEFT JOIN PRODUCTION.dbo.PO_Detail AS po_detailT\n" +
                        "ON po_detailT.PO = RTRIM(LTRIM(ncT.PO))\n" +
                        "LEFT JOIN PRODUCTION.dbo.Source AS sourceT\n" +
                        "ON sourceT.PO_Detail = po_detailT.PO_Detail\n" +
                        "LEFT JOIN PRODUCTION.dbo.Job_Operation AS job_operationT\n" +
                        "ON job_operationT.Job_Operation = sourceT.Job_Operation\n" +
                        "WHERE ncT.NCR_Date >= CONVERT(DATETIME, '" + startDateTimePicker.Value.ToString() + "') AND ncT.NCR_Date < CONVERT(DATETIME, '" + endDateTimePicker.Value.ToString() + "') AND ncT.NC_type LIKE '%Vendor%' AND ncT.Status = 'Closed' AND ncT.Qty_Scrap > 0\n" +
                        "AND ncT.Job = job_operationT.Job;";
                }

                SqlDataAdapter dataAdapter = new SqlDataAdapter(query, Globals.binding_connection_string);

                SqlCommandBuilder commandBuilder = new SqlCommandBuilder(dataAdapter);
                DataTable dt = new DataTable();
                dataAdapter.Fill(dt);
                totalsBindingSource.DataSource = dt;

                totalsDataGridView.Columns["Total Scrap Cost"].DefaultCellStyle.Format = "c";
                totalsDataGridView.Columns["Total Scrap Cost (Including Extras)"].DefaultCellStyle.Format = "c";
            }
        }

        private void includeAllOpsCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (!includeAllOpsCheckBox.Checked)
            {
                groupByJobsCheckBox.CheckedChanged -= UpdateTotalsDataGridview;
                groupByJobsCheckBox.CheckedChanged -= UpdateDetailsDataGridview;
                groupByJobsCheckBox.Checked = true;
                groupByJobsCheckBox.CheckedChanged += UpdateDetailsDataGridview;
                groupByJobsCheckBox.CheckedChanged += UpdateTotalsDataGridview;
                groupByJobsCheckBox.Enabled = false;
            }
            else
            {
                groupByJobsCheckBox.Enabled = true;
            }
        }
    }
}
