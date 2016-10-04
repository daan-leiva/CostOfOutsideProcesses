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

            // set up scrap type combobox
            scrapTypeComboBox.Items.Add("Outside Ops");
            scrapTypeComboBox.Items.Add("Inside Ops");
            scrapTypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            scrapTypeComboBox.SelectedIndexChanged += UpdateDetailsDataGridview;
            scrapTypeComboBox.SelectedIndexChanged += UpdateTotalsDataGridview;
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

                if (scrapTypeComboBox.SelectedItem.ToString().Contains("Outside"))
                {
                    if (includeAllOpsCheckBox.Checked)
                    {
                        if (groupByJobsCheckBox.Checked)
                        {
                            query =
                                "SELECT ncT_formatedT.NC_num, job_operation_formattedT.Job, ncT_formatedT.NCR_Date, job_operation_formattedT.Last_Updated, ncT_formatedT.Operation_Num AS scrap_Operation, ncT_formatedT.Qty_Scrap, job_operation_formattedT.Sequence AS scrap_sequence,\n" +
                                "SUM(previous_operationsT.Act_Run_Labor + previous_operationsT.Act_Labor_Burden + previous_operationsT.Act_Machine_Burden) AS total_run_cost, run_qtyT.max_run_qty,\n" +
                                "SUM(CASE\n" +
                                    "\tWHEN run_qtyT.max_run_qty = 0\n" +
                                    "\tTHEN 0\n" +
                                    "\tELSE(previous_operationsT.Act_Run_Labor + previous_operationsT.Act_Labor_Burden + previous_operationsT.Act_Machine_Burden) / run_qtyT.max_run_qty * ncT_formatedT.Qty_Scrap\n" +
                                "END) AS Scrap_Cost\n" +
                                "FROM\n" +
                                    "\t(SELECT NCR AS NC_num, ncT.Job AS Job, ncT.Operation, ncT.Reference, ncT.NCR_Date, ncT.Qty_Scrap,\n" +
                                    "\tCASE\n" +
                                        "\t\tWHEN ISNUMERIC(ncT.Operation) = 1\n" +
                                            "\t\t\tAND CHARINDEX('.', ncT.Operation) = 0\n" +
                                            "\t\t\tTHEN CONVERT(INT, ncT.Operation)\n" +
                                        "\t\tWHEN ISNUMERIC(ncT.Reference) = 1\n" +
                                            "\t\t\tAND CHARINDEX('.', ncT.Reference) = 0\n" +
                                            "\t\t\tTHEN CONVERT(INT, ncT.Reference)\n" +
                                        "\t\tWHEN PATINDEX('%[0-9]%', ncT.Reference) > 0 AND PATINDEX('%[-*R]%', ncT.Reference) = 0\n" +
                                            "\t\t\tAND ISNUMERIC(SUBSTRING(ncT.Reference, PATINDEX('%[0-9]%', ncT.Reference), LEN(ncT.Reference))) = 1\n" +
                                            "\t\t\tAND CHARINDEX('.', SUBSTRING(ncT.Reference, PATINDEX('%[0-9]%', ncT.Reference), LEN(ncT.Reference))) = 0\n" +
                                            "\t\t\tTHEN CONVERT(INT, SUBSTRING(ncT.Reference, PATINDEX('%[0-9]%', ncT.Reference), LEN(ncT.Reference)))\n" +
                                        "\t\tWHEN PATINDEX('%[0-9]%', ncT.Reference) > 0 AND PATINDEX('%[-*R]%', ncT.Reference) > 0\n" +
                                            "\t\t\tAND ISNUMERIC(SUBSTRING(ncT.Reference, PATINDEX('%[0-9]%', ncT.Reference), PATINDEX('%[-*R]%', ncT.Reference) - PATINDEX('%[0-9]%', ncT.Reference))) = 1\n" +
                                            "\t\t\tAND CHARINDEX('.', SUBSTRING(ncT.Reference, PATINDEX('%[0-9]%', ncT.Reference), PATINDEX('%[-*R]%', ncT.Reference) - PATINDEX('%[0-9]%', ncT.Reference))) = 0\n" +
                                            "\t\t\tTHEN CONVERT(INT, SUBSTRING(ncT.Reference, PATINDEX('%[0-9]%', ncT.Reference), PATINDEX('%[-*R]%', ncT.Reference) - PATINDEX('%[0-9]%', ncT.Reference)))\n" +
                                        "\t\tWHEN PATINDEX('%[R]%', ncT.Reference) > 1 AND ISNUMERIC(SUBSTRING(ncT.Reference, 1, PATINDEX('%[R]%', ncT.Reference) - 1)) = 1\n" +
                                            "\t\t\tAND ISNUMERIC(SUBSTRING(ncT.Reference, 1, PATINDEX('%[R]%', ncT.Reference) - 1)) = 1\n" +
                                            "\t\t\tAND CHARINDEX('.', SUBSTRING(ncT.Reference, 1, PATINDEX('%[R]%', ncT.Reference) - 1)) = 0\n" +
                                            "\t\t\tTHEN SUBSTRING(ncT.Reference, 1, PATINDEX('%[R]%', ncT.Reference) - 1)\n" +
                                        "\t\tWHEN PATINDEX('%[R*.]%', ncT.Operation) > 1\n" +
                                            "\t\t\tAND ISNUMERIC(SUBSTRING(ncT.Operation, 1, PATINDEX('%[R*.]%', ncT.Operation) - 1)) = 1\n" +
                                            "\t\t\tAND CHARINDEX('.', SUBSTRING(ncT.Operation, 1, PATINDEX('%[R*.]%', ncT.Operation) - 1)) = 0\n" +
                                            "\t\t\tTHEN SUBSTRING(ncT.Operation, 1, PATINDEX('%[R*.]%', ncT.Operation) - 1)\n" +
                                        "\t\tWHEN PATINDEX('%[/&]%', ncT.Operation) > 1 AND ISNUMERIC(SUBSTRING(ncT.Operation, PATINDEX('%[/&]%', ncT.Operation) + 1, LEN(ncT.Operation))) = 1\n" +
                                            "\t\t\tAND CHARINDEX('.', SUBSTRING(ncT.Operation, PATINDEX('%[/&]%', ncT.Operation) + 1, LEN(ncT.Operation))) = 0\n" +
                                            "\t\t\tTHEN SUBSTRING(ncT.Operation, PATINDEX('%[/&]%', ncT.Operation) + 1, LEN(ncT.Operation))\n" +
                                        "\t\tWHEN PATINDEX('%[/&]%', ncT.Reference) > 1 AND ISNUMERIC(SUBSTRING(ncT.Reference, PATINDEX('%[/&]%', ncT.Reference) + 1, LEN(ncT.Reference))) = 1\n" +
                                            "\t\t\tAND CHARINDEX('.', SUBSTRING(ncT.Reference, PATINDEX('%[/&]%', ncT.Reference) + 1, LEN(ncT.Reference))) = 0\n" +
                                            "\t\t\tTHEN SUBSTRING(ncT.Reference, PATINDEX('%[/&]%', ncT.Reference) + 1, LEN(ncT.Reference))\n" +
                                        "\t\tELSE NULL\n" +
                                    "\tEND AS[Operation_Num]\n" +
                                    "\tFROM uniPoint_Live.dbo.PT_NC AS ncT\n" +
                                    "\tWHERE ncT.NCR_Date >= CONVERT(DATETIME, '6/13/2013 12:00:00 AM')\n" +
                                        "\t\tAND ncT.NC_type = 'In Process' AND ncT.Status = 'Closed' AND ncT.Qty_Scrap > 0\n" +
                                    "\t) AS ncT_formatedT\n" +
                                "LEFT JOIN\n" +
                                    "\t(SELECT job_operation_formattedT_inner.Job, job_operation_formattedT_inner.Operation_Num, job_operation_formattedT_inner.Sequence, job_operation_formattedT_inner.Last_Updated\n" +
                                    "\tFROM\n" +
                                        "\t\t(SELECT job_operationT.Job, job_operationT.Sequence, job_operationT.Last_Updated,\n" +
                                        "\t\tCASE\n" +
                                            "\t\t\tWHEN ISNUMERIC(job_operationT.Operation_Service) = 1 AND CHARINDEX('.', job_operationT.Operation_Service) = 0\n" +
                                                "\t\t\t\tTHEN CONVERT(INT, job_operationT.Operation_Service)\n" +
                                            "\t\t\tWHEN PATINDEX('%[-*.RABCTSXED#]%', job_operationT.Operation_Service) > 1 AND ISNUMERIC(SUBSTRING(job_operationT.Operation_Service, 1, PATINDEX('%[-*.RABCTSXED#]%', job_operationT.Operation_Service) - 1)) = 1 AND CHARINDEX('.', SUBSTRING(job_operationT.Operation_Service, 1, PATINDEX('%[-*.RABCTSXED#]%', job_operationT.Operation_Service) - 1)) = 0\n" +
                                                "\t\t\t\tTHEN CONVERT(INT, SUBSTRING(job_operationT.Operation_Service, 1, PATINDEX('%[-*.RABCTSXED#]%', job_operationT.Operation_Service) - 1))\n" +
                                            "\t\t\tWHEN PATINDEX('%[-*.RABCSXED#]%', job_operationT.Operation_Service) = 1 AND ISNUMERIC(SUBSTRING(job_operationT.Operation_Service, 2, LEN(job_operationT.Operation_Service))) = 1 AND CHARINDEX('.', SUBSTRING(job_operationT.Operation_Service, 2, LEN(job_operationT.Operation_Service))) = 0\n" +
                                                "\t\t\t\tTHEN CONVERT(INT, SUBSTRING(job_operationT.Operation_Service, 2, LEN(job_operationT.Operation_Service)))\n" +
                                            "\t\t\tWHEN PATINDEX('%/%', job_operationT.Operation_Service) > 1 AND ISNUMERIC(SUBSTRING(job_operationT.Operation_Service, PATINDEX('%/%', job_operationT.Operation_Service) + 1, LEN(job_operationT.Operation_Service))) = 1 AND CHARINDEX('.', SUBSTRING(job_operationT.Operation_Service, PATINDEX('%/%', job_operationT.Operation_Service) + 1, LEN(job_operationT.Operation_Service))) = 0\n" +
                                                "\t\t\t\tTHEN CONVERT(INT, SUBSTRING(job_operationT.Operation_Service, PATINDEX('%/%', job_operationT.Operation_Service) + 1, LEN(job_operationT.Operation_Service)))\n" +
                                            "\t\t\tWHEN PATINDEX('%[-*.RABCTSXED#]%', job_operationT.Operation_Service) = 1 AND PATINDEX('%[-*.RABCTSXED#]%', SUBSTRING(job_operationT.Operation_Service, 2, LEN(job_operationT.Operation_Service))) > 0\n" +
                                                "\t\t\t\tAND ISNUMERIC(SUBSTRING(job_operationT.Operation_Service, 2, PATINDEX('%[-*.RABCTSXED#]%', SUBSTRING(job_operationT.Operation_Service, 2, LEN(job_operationT.Operation_Service))) - 1)) = 1\n" +
                                                "\t\t\t\tAND CHARINDEX('.', SUBSTRING(job_operationT.Operation_Service, 2, PATINDEX('%[-*.RABCTSXED#]%', SUBSTRING(job_operationT.Operation_Service, 2, LEN(job_operationT.Operation_Service))) - 1)) = 0\n" +
                                                "\t\t\t\tTHEN CONVERT(INT, SUBSTRING(job_operationT.Operation_Service, 2, PATINDEX('%[-*.RABCTSXED#]%', SUBSTRING(job_operationT.Operation_Service, 2, LEN(job_operationT.Operation_Service))) - 1))\n" +
                                            "\t\t\tWHEN PATINDEX('%[-*.RABCTSXED#]%', job_operationT.Operation_Service) = 1 AND PATINDEX('%[-*.RABCTSXED#]%', SUBSTRING(job_operationT.Operation_Service, 2, LEN(job_operationT.Operation_Service))) = 1\n" +
                                                "\t\t\t\tAND ISNUMERIC(SUBSTRING(job_operationT.Operation_Service, 3, LEN(job_operationT.Operation_Service))) = 1 AND CHARINDEX('.', SUBSTRING(job_operationT.Operation_Service, 3, LEN(job_operationT.Operation_Service))) = 0\n" +
                                                "\t\t\t\tTHEN CONVERT(INT, SUBSTRING(job_operationT.Operation_Service, 3, LEN(job_operationT.Operation_Service)))\n" +
                                            "\t\t\tWHEN CHARINDEX('MT#', job_operationT.Operation_Service) > 0\n" +
                                                "\t\t\t\tAND ISNUMERIC(SUBSTRING(job_operationT.Operation_Service, 4, LEN(job_operationT.Operation_Service))) = 1 AND\n" +
                                                "\t\t\t\tCHARINDEX('.', SUBSTRING(job_operationT.Operation_Service, 4, LEN(job_operationT.Operation_Service))) = 0\n" +
                                                "\t\t\t\tTHEN CONVERT(INT, SUBSTRING(job_operationT.Operation_Service, 4, LEN(job_operationT.Operation_Service)))\n" +
                                            "\t\t\tElSE NULL\n" +
                                        "\t\tEND AS Operation_Num, job_operationT.Operation_Service\n" +
                                        "\t\tFROM PRODUCTION.dbo.Job_Operation AS job_operationT\n" +
                                        "\t\tWHERE Operation_Service IS NOT NULL AND Operation_Service LIKE '%[0-9]%' AND Operation_Service NOT LIKE '%LU%' AND Operation_Service NOT LIKE '%SV50%'\n" +
                                        "\t\t) AS job_operation_formattedT_inner\n" +
                                    "\t) AS job_operation_formattedT\n" +
                                "ON job_operation_formattedT.Job = ncT_formatedT.Job AND job_operation_formattedT.Operation_Num = ncT_formatedT.Operation_Num\n" +
                                "LEFT JOIN PRODUCTION.dbo.Job_Operation AS previous_operationsT\n" +
                                "ON previous_operationsT.Job = job_operation_formattedT.Job AND previous_operationsT.Sequence <= job_operation_formattedT.Sequence\n" +
                                "LEFT JOIN\n" +
                                    "\t(\n" +
                                    "\tSELECT t.Job AS Job, MAX(t.Act_Run_Qty) AS max_run_qty\n" +
                                    "\tFROM PRODUCTION.dbo.Job_Operation AS t\n" +
                                    "\tGROUP BY Job\n" +
                                    "\t) AS run_qtyT\n" +
                                "ON run_qtyT.job = job_operation_formattedT.Job\n" +
                                "WHERE job_operation_formattedT.Sequence IS NOT NULL\n" +
                                    "\tAND job_operation_formattedT.Last_Updated >= CONVERT(DATE, '01/01/2015')\n" +
                                    "\tAND job_operation_formattedT.Last_Updated < CONVERT(DATE, '01/01/2016')\n" +
                                "GROUP BY ncT_formatedT.NC_num, job_operation_formattedT.Job, ncT_formatedT.NCR_Date, job_operation_formattedT.Last_Updated,ncT_formatedT.Operation_Num, ncT_formatedT.Qty_Scrap, job_operation_formattedT.Sequence, run_qtyT.max_run_qty\n" +
                                "ORDER BY ncT_formatedT.NC_num ,job_operation_formattedT.Job;";
                        }
                        else
                        {
                            query =
                                "SELECT ncT_formatedT.NC_num, ncT_formatedT.Job, ncT_formatedT.NCR_Date, job_operation_formattedT.Last_Updated, ncT_formatedT.Operation_Num AS operationNum_NC, job_operation_formattedT.Operation_Num AS operationNum_Job, ncT_formatedT.Qty_Scrap, job_operation_formattedT.Sequence AS scrap_sequence, previous_operationsT.Sequence AS previous_sequence,\n" +
                                "(previous_operationsT.Act_Run_Labor + previous_operationsT.Act_Labor_Burden + previous_operationsT.Act_Machine_Burden) AS run_cost, run_qtyT.max_run_qty,\n" +
                                "CASE\n" +
                                    "\tWHEN run_qtyT.max_run_qty = 0\n" +
                                    "\tTHEN 0\n" +
                                    "\tELSE(previous_operationsT.Act_Run_Labor + previous_operationsT.Act_Labor_Burden + previous_operationsT.Act_Machine_Burden) / run_qtyT.max_run_qty * ncT_formatedT.Qty_Scrap\n" +
                                "END AS Scrap_Cost\n" +
                                "FROM\n" +
                                    "\t(SELECT NCR AS NC_num, ncT.Job AS Job, ncT.Operation, ncT.Reference, ncT.NCR_Date, ncT.Qty_Scrap,\n" +
                                    "\tCASE\n" +
                                        "\t\tWHEN ISNUMERIC(ncT.Operation) = 1\n" +
                                            "\t\t\tAND CHARINDEX('.', ncT.Operation) = 0\n" +
                                            "\t\t\tTHEN CONVERT(INT, ncT.Operation)\n" +
                                        "\t\tWHEN ISNUMERIC(ncT.Reference) = 1\n" +
                                            "\t\t\tAND CHARINDEX('.', ncT.Reference) = 0\n" +
                                            "\t\t\tTHEN CONVERT(INT, ncT.Reference)\n" +
                                        "\t\tWHEN PATINDEX('%[0-9]%', ncT.Reference) > 0 AND PATINDEX('%[-*R]%', ncT.Reference) = 0\n" +
                                            "\t\t\tAND ISNUMERIC(SUBSTRING(ncT.Reference, PATINDEX('%[0-9]%', ncT.Reference), LEN(ncT.Reference))) = 1\n" +
                                            "\t\t\tAND CHARINDEX('.', SUBSTRING(ncT.Reference, PATINDEX('%[0-9]%', ncT.Reference), LEN(ncT.Reference))) = 0\n" +
                                            "\t\t\tTHEN CONVERT(INT, SUBSTRING(ncT.Reference, PATINDEX('%[0-9]%', ncT.Reference), LEN(ncT.Reference)))\n" +
                                        "\t\tWHEN PATINDEX('%[0-9]%', ncT.Reference) > 0 AND PATINDEX('%[-*R]%', ncT.Reference) > 0\n" +
                                            "\t\t\tAND ISNUMERIC(SUBSTRING(ncT.Reference, PATINDEX('%[0-9]%', ncT.Reference), PATINDEX('%[-*R]%', ncT.Reference) - PATINDEX('%[0-9]%', ncT.Reference))) = 1\n" +
                                            "\t\t\tAND CHARINDEX('.', SUBSTRING(ncT.Reference, PATINDEX('%[0-9]%', ncT.Reference), PATINDEX('%[-*R]%', ncT.Reference) - PATINDEX('%[0-9]%', ncT.Reference))) = 0\n" +
                                            "\t\t\tTHEN CONVERT(INT, SUBSTRING(ncT.Reference, PATINDEX('%[0-9]%', ncT.Reference), PATINDEX('%[-*R]%', ncT.Reference) - PATINDEX('%[0-9]%', ncT.Reference)))\n" +
                                        "\t\tWHEN PATINDEX('%[R]%', ncT.Reference) > 1 AND ISNUMERIC(SUBSTRING(ncT.Reference, 1, PATINDEX('%[R]%', ncT.Reference) - 1)) = 1\n" +
                                            "\t\t\tAND ISNUMERIC(SUBSTRING(ncT.Reference, 1, PATINDEX('%[R]%', ncT.Reference) - 1)) = 1\n" +
                                            "\t\t\tAND CHARINDEX('.', SUBSTRING(ncT.Reference, 1, PATINDEX('%[R]%', ncT.Reference) - 1)) = 0\n" +
                                            "\t\t\tTHEN SUBSTRING(ncT.Reference, 1, PATINDEX('%[R]%', ncT.Reference) - 1)\n" +
                                        "\t\tWHEN PATINDEX('%[R*.]%', ncT.Operation) > 1\n" +
                                            "\t\t\tAND ISNUMERIC(SUBSTRING(ncT.Operation, 1, PATINDEX('%[R*.]%', ncT.Operation) - 1)) = 1\n" +
                                            "\t\t\tAND CHARINDEX('.', SUBSTRING(ncT.Operation, 1, PATINDEX('%[R*.]%', ncT.Operation) - 1)) = 0\n" +
                                            "\t\t\tTHEN SUBSTRING(ncT.Operation, 1, PATINDEX('%[R*.]%', ncT.Operation) - 1)\n" +
                                        "\t\tWHEN PATINDEX('%[/&]%', ncT.Operation) > 1 AND ISNUMERIC(SUBSTRING(ncT.Operation, PATINDEX('%[/&]%', ncT.Operation) + 1, LEN(ncT.Operation))) = 1\n" +
                                            "\t\t\tAND CHARINDEX('.', SUBSTRING(ncT.Operation, PATINDEX('%[/&]%', ncT.Operation) + 1, LEN(ncT.Operation))) = 0\n" +
                                            "\t\t\tTHEN SUBSTRING(ncT.Operation, PATINDEX('%[/&]%', ncT.Operation) + 1, LEN(ncT.Operation))\n" +
                                        "\t\tWHEN PATINDEX('%[/&]%', ncT.Reference) > 1 AND ISNUMERIC(SUBSTRING(ncT.Reference, PATINDEX('%[/&]%', ncT.Reference) + 1, LEN(ncT.Reference))) = 1\n" +
                                            "\t\t\tAND CHARINDEX('.', SUBSTRING(ncT.Reference, PATINDEX('%[/&]%', ncT.Reference) + 1, LEN(ncT.Reference))) = 0\n" +
                                            "\t\t\tTHEN SUBSTRING(ncT.Reference, PATINDEX('%[/&]%', ncT.Reference) + 1, LEN(ncT.Reference))\n" +
                                        "\t\tELSE NULL\n" +
                                    "\tEND AS[Operation_Num]\n" +
                                    "\tFROM uniPoint_Live.dbo.PT_NC AS ncT\n" +
                                    "\tWHERE ncT.NCR_Date >= CONVERT(DATETIME, '6/13/2013 12:00:00 AM')\n" +
                                        "\t\tAND ncT.NC_type = 'In Process' AND ncT.Status = 'Closed' AND ncT.Qty_Scrap > 0\n" +
                                    "\t) AS ncT_formatedT\n" +
                                "LEFT JOIN\n" +
                                    "\t(SELECT job_operation_formattedT_inner.Job, job_operation_formattedT_inner.Operation_Num, job_operation_formattedT_inner.Sequence, job_operation_formattedT_inner.Last_Updated\n" +
                                    "\tFROM\n" +
                                        "\t\t(SELECT job_operationT.Job, job_operationT.Sequence, job_operationT.Last_Updated,\n" +
                                        "\t\tCASE\n" +
                                            "\t\t\tWHEN ISNUMERIC(job_operationT.Operation_Service) = 1 AND CHARINDEX('.', job_operationT.Operation_Service) = 0\n" +
                                                "\t\t\t\tTHEN CONVERT(INT, job_operationT.Operation_Service)\n" +
                                            "\t\t\tWHEN PATINDEX('%[-*.RABCTSXED#]%', job_operationT.Operation_Service) > 1 AND ISNUMERIC(SUBSTRING(job_operationT.Operation_Service, 1, PATINDEX('%[-*.RABCTSXED#]%', job_operationT.Operation_Service) - 1)) = 1 AND CHARINDEX('.', SUBSTRING(job_operationT.Operation_Service, 1, PATINDEX('%[-*.RABCTSXED#]%', job_operationT.Operation_Service) - 1)) = 0\n" +
                                                "\t\t\t\tTHEN CONVERT(INT, SUBSTRING(job_operationT.Operation_Service, 1, PATINDEX('%[-*.RABCTSXED#]%', job_operationT.Operation_Service) - 1))\n" +
                                            "\t\t\tWHEN PATINDEX('%[-*.RABCSXED#]%', job_operationT.Operation_Service) = 1 AND ISNUMERIC(SUBSTRING(job_operationT.Operation_Service, 2, LEN(job_operationT.Operation_Service))) = 1 AND CHARINDEX('.', SUBSTRING(job_operationT.Operation_Service, 2, LEN(job_operationT.Operation_Service))) = 0\n" +
                                                "\t\t\t\tTHEN CONVERT(INT, SUBSTRING(job_operationT.Operation_Service, 2, LEN(job_operationT.Operation_Service)))\n" +
                                            "\t\t\tWHEN PATINDEX('%/%', job_operationT.Operation_Service) > 1 AND ISNUMERIC(SUBSTRING(job_operationT.Operation_Service, PATINDEX('%/%', job_operationT.Operation_Service) + 1, LEN(job_operationT.Operation_Service))) = 1 AND CHARINDEX('.', SUBSTRING(job_operationT.Operation_Service, PATINDEX('%/%', job_operationT.Operation_Service) + 1, LEN(job_operationT.Operation_Service))) = 0\n" +
                                                "\t\t\t\tTHEN CONVERT(INT, SUBSTRING(job_operationT.Operation_Service, PATINDEX('%/%', job_operationT.Operation_Service) + 1, LEN(job_operationT.Operation_Service)))\n" +
                                            "\t\t\tWHEN PATINDEX('%[-*.RABCTSXED#]%', job_operationT.Operation_Service) = 1 AND PATINDEX('%[-*.RABCTSXED#]%', SUBSTRING(job_operationT.Operation_Service, 2, LEN(job_operationT.Operation_Service))) > 0\n" +
                                                "\t\t\t\tAND ISNUMERIC(SUBSTRING(job_operationT.Operation_Service, 2, PATINDEX('%[-*.RABCTSXED#]%', SUBSTRING(job_operationT.Operation_Service, 2, LEN(job_operationT.Operation_Service))) - 1)) = 1\n" +
                                                "\t\t\t\tAND CHARINDEX('.', SUBSTRING(job_operationT.Operation_Service, 2, PATINDEX('%[-*.RABCTSXED#]%', SUBSTRING(job_operationT.Operation_Service, 2, LEN(job_operationT.Operation_Service))) - 1)) = 0\n" +
                                                "\t\t\t\tTHEN CONVERT(INT, SUBSTRING(job_operationT.Operation_Service, 2, PATINDEX('%[-*.RABCTSXED#]%', SUBSTRING(job_operationT.Operation_Service, 2, LEN(job_operationT.Operation_Service))) - 1))\n" +
                                            "\t\t\tWHEN PATINDEX('%[-*.RABCTSXED#]%', job_operationT.Operation_Service) = 1 AND PATINDEX('%[-*.RABCTSXED#]%', SUBSTRING(job_operationT.Operation_Service, 2, LEN(job_operationT.Operation_Service))) = 1\n" +
                                                "\t\t\t\tAND ISNUMERIC(SUBSTRING(job_operationT.Operation_Service, 3, LEN(job_operationT.Operation_Service))) = 1 AND CHARINDEX('.', SUBSTRING(job_operationT.Operation_Service, 3, LEN(job_operationT.Operation_Service))) = 0\n" +
                                                "\t\t\t\tTHEN CONVERT(INT, SUBSTRING(job_operationT.Operation_Service, 3, LEN(job_operationT.Operation_Service)))\n" +
                                            "\t\t\tWHEN CHARINDEX('MT#', job_operationT.Operation_Service) > 0\n" +
                                                "\t\t\t\tAND ISNUMERIC(SUBSTRING(job_operationT.Operation_Service, 4, LEN(job_operationT.Operation_Service))) = 1 AND\n" +
                                                "\t\t\t\tCHARINDEX('.', SUBSTRING(job_operationT.Operation_Service, 4, LEN(job_operationT.Operation_Service))) = 0\n" +
                                                "\t\t\t\tTHEN CONVERT(INT, SUBSTRING(job_operationT.Operation_Service, 4, LEN(job_operationT.Operation_Service)))\n" +
                                            "\t\t\tElSE NULL\n" +
                                        "\t\tEND AS Operation_Num, job_operationT.Operation_Service\n" +
                                        "\t\tFROM PRODUCTION.dbo.Job_Operation AS job_operationT\n" +
                                        "\t\tWHERE Operation_Service IS NOT NULL AND Operation_Service LIKE '%[0-9]%' AND Operation_Service NOT LIKE '%LU%' AND Operation_Service NOT LIKE '%SV50%'\n" +
                                        "\t\t) AS job_operation_formattedT_inner\n" +
                                    "\t) AS job_operation_formattedT\n" +
                                "ON job_operation_formattedT.Job = ncT_formatedT.Job AND job_operation_formattedT.Operation_Num = ncT_formatedT.Operation_Num\n" +
                                "LEFT JOIN PRODUCTION.dbo.Job_Operation AS previous_operationsT\n" +
                                "ON previous_operationsT.Job = job_operation_formattedT.Job AND previous_operationsT.Sequence <= job_operation_formattedT.Sequence\n" +
                                "LEFT JOIN\n" +
                                    "\t(\n" +
                                    "\tSELECT t.Job AS Job, MAX(t.Act_Run_Qty) AS max_run_qty\n" +
                                    "\tFROM PRODUCTION.dbo.Job_Operation AS t\n" +
                                    "\tGROUP BY Job\n" +
                                    "\t) AS run_qtyT\n" +
                                "ON run_qtyT.job = job_operation_formattedT.Job\n" +
                                "WHERE job_operation_formattedT.Sequence IS NOT NULL\n" +
                                    "\tAND job_operation_formattedT.Last_Updated >= CONVERT(DATE, '01/01/2015')\n" +
                                    "\tAND job_operation_formattedT.Last_Updated < CONVERT(DATE, '01/01/2016')\n" +
                                "ORDER BY ncT_formatedT.NC_num ,job_operation_formattedT.Job, previous_operationsT.Sequence;";
                        }
                    }
                    else
                    {
                        query =
                            "SELECT ncT_formatedT.NC_num, ncT_formatedT.Job, ncT_formatedT.NCR_Date, job_operation_formattedT.Last_Updated, ncT_formatedT.Operation_Num AS operationNum_NC, job_operation_formattedT.Operation_Num AS operationNum_Job, ncT_formatedT.Qty_Scrap, job_operation_formattedT.Sequence AS scrap_sequence, previous_operationsT.Sequence AS previous_sequence,\n" +
                            "(previous_operationsT.Act_Run_Labor + previous_operationsT.Act_Labor_Burden + previous_operationsT.Act_Machine_Burden) AS run_cost, run_qtyT.max_run_qty,\n" +
                            "CASE\n" +
                                "\tWHEN run_qtyT.max_run_qty = 0\n" +
                                "\tTHEN 0\n" +
                                "\tELSE(previous_operationsT.Act_Run_Labor + previous_operationsT.Act_Labor_Burden + previous_operationsT.Act_Machine_Burden) / run_qtyT.max_run_qty * ncT_formatedT.Qty_Scrap\n" +
                            "END AS Scrap_Cost\n" +
                            "FROM\n" +
                                "\t(SELECT NCR AS NC_num, ncT.Job AS Job, ncT.Operation, ncT.Reference, ncT.NCR_Date, ncT.Qty_Scrap,\n" +
                                "\tCASE\n" +
                                    "\t\tWHEN ISNUMERIC(ncT.Operation) = 1\n" +
                                        "\t\t\tAND CHARINDEX('.', ncT.Operation) = 0\n" +
                                        "\t\t\tTHEN CONVERT(INT, ncT.Operation)\n" +
                                    "\t\tWHEN ISNUMERIC(ncT.Reference) = 1\n" +
                                        "\t\t\tAND CHARINDEX('.', ncT.Reference) = 0\n" +
                                        "\t\t\tTHEN CONVERT(INT, ncT.Reference)\n" +
                                    "\t\tWHEN PATINDEX('%[0-9]%', ncT.Reference) > 0 AND PATINDEX('%[-*R]%', ncT.Reference) = 0\n" +
                                        "\t\t\tAND ISNUMERIC(SUBSTRING(ncT.Reference, PATINDEX('%[0-9]%', ncT.Reference), LEN(ncT.Reference))) = 1\n" +
                                        "\t\t\tAND CHARINDEX('.', SUBSTRING(ncT.Reference, PATINDEX('%[0-9]%', ncT.Reference), LEN(ncT.Reference))) = 0\n" +
                                        "\t\t\tTHEN CONVERT(INT, SUBSTRING(ncT.Reference, PATINDEX('%[0-9]%', ncT.Reference), LEN(ncT.Reference)))\n" +
                                    "\t\tWHEN PATINDEX('%[0-9]%', ncT.Reference) > 0 AND PATINDEX('%[-*R]%', ncT.Reference) > 0\n" +
                                        "\t\t\tAND ISNUMERIC(SUBSTRING(ncT.Reference, PATINDEX('%[0-9]%', ncT.Reference), PATINDEX('%[-*R]%', ncT.Reference) - PATINDEX('%[0-9]%', ncT.Reference))) = 1\n" +
                                        "\t\t\tAND CHARINDEX('.', SUBSTRING(ncT.Reference, PATINDEX('%[0-9]%', ncT.Reference), PATINDEX('%[-*R]%', ncT.Reference) - PATINDEX('%[0-9]%', ncT.Reference))) = 0\n" +
                                        "\t\t\tTHEN CONVERT(INT, SUBSTRING(ncT.Reference, PATINDEX('%[0-9]%', ncT.Reference), PATINDEX('%[-*R]%', ncT.Reference) - PATINDEX('%[0-9]%', ncT.Reference)))\n" +
                                    "\t\tWHEN PATINDEX('%[R]%', ncT.Reference) > 1 AND ISNUMERIC(SUBSTRING(ncT.Reference, 1, PATINDEX('%[R]%', ncT.Reference) - 1)) = 1\n" +
                                        "\t\t\tAND ISNUMERIC(SUBSTRING(ncT.Reference, 1, PATINDEX('%[R]%', ncT.Reference) - 1)) = 1\n" +
                                        "\t\t\tAND CHARINDEX('.', SUBSTRING(ncT.Reference, 1, PATINDEX('%[R]%', ncT.Reference) - 1)) = 0\n" +
                                        "\t\t\tTHEN SUBSTRING(ncT.Reference, 1, PATINDEX('%[R]%', ncT.Reference) - 1)\n" +
                                    "\t\tWHEN PATINDEX('%[R*.]%', ncT.Operation) > 1\n" +
                                        "\t\t\tAND ISNUMERIC(SUBSTRING(ncT.Operation, 1, PATINDEX('%[R*.]%', ncT.Operation) - 1)) = 1\n" +
                                        "\t\t\tAND CHARINDEX('.', SUBSTRING(ncT.Operation, 1, PATINDEX('%[R*.]%', ncT.Operation) - 1)) = 0\n" +
                                        "\t\t\tTHEN SUBSTRING(ncT.Operation, 1, PATINDEX('%[R*.]%', ncT.Operation) - 1)\n" +
                                    "\t\tWHEN PATINDEX('%[/&]%', ncT.Operation) > 1 AND ISNUMERIC(SUBSTRING(ncT.Operation, PATINDEX('%[/&]%', ncT.Operation) + 1, LEN(ncT.Operation))) = 1\n" +
                                        "\t\t\tAND CHARINDEX('.', SUBSTRING(ncT.Operation, PATINDEX('%[/&]%', ncT.Operation) + 1, LEN(ncT.Operation))) = 0\n" +
                                        "\t\t\tTHEN SUBSTRING(ncT.Operation, PATINDEX('%[/&]%', ncT.Operation) + 1, LEN(ncT.Operation))\n" +
                                    "\t\tWHEN PATINDEX('%[/&]%', ncT.Reference) > 1 AND ISNUMERIC(SUBSTRING(ncT.Reference, PATINDEX('%[/&]%', ncT.Reference) + 1, LEN(ncT.Reference))) = 1\n" +
                                        "\t\t\tAND CHARINDEX('.', SUBSTRING(ncT.Reference, PATINDEX('%[/&]%', ncT.Reference) + 1, LEN(ncT.Reference))) = 0\n" +
                                        "\t\t\tTHEN SUBSTRING(ncT.Reference, PATINDEX('%[/&]%', ncT.Reference) + 1, LEN(ncT.Reference))\n" +
                                    "\t\tELSE NULL\n" +
                                "\tEND AS[Operation_Num]\n" +
                                "\tFROM uniPoint_Live.dbo.PT_NC AS ncT\n" +
                                "\tWHERE ncT.NCR_Date >= CONVERT(DATETIME, '6/13/2013 12:00:00 AM')\n" +
                                    "\t\tAND ncT.NC_type = 'In Process' AND ncT.Status = 'Closed' AND ncT.Qty_Scrap > 0\n" +
                                "\t) AS ncT_formatedT\n" +
                            "LEFT JOIN\n" +
                                "\t(SELECT job_operation_formattedT_inner.Job, job_operation_formattedT_inner.Operation_Num, job_operation_formattedT_inner.Sequence, job_operation_formattedT_inner.Last_Updated\n" +
                                "\tFROM\n" +
                                    "\t\t(SELECT job_operationT.Job, job_operationT.Sequence, job_operationT.Last_Updated,\n" +
                                    "\t\tCASE\n" +
                                        "\t\t\tWHEN ISNUMERIC(job_operationT.Operation_Service) = 1 AND CHARINDEX('.', job_operationT.Operation_Service) = 0\n" +
                                            "\t\t\t\tTHEN CONVERT(INT, job_operationT.Operation_Service)\n" +
                                        "\t\t\tWHEN PATINDEX('%[-*.RABCTSXED#]%', job_operationT.Operation_Service) > 1 AND ISNUMERIC(SUBSTRING(job_operationT.Operation_Service, 1, PATINDEX('%[-*.RABCTSXED#]%', job_operationT.Operation_Service) - 1)) = 1 AND CHARINDEX('.', SUBSTRING(job_operationT.Operation_Service, 1, PATINDEX('%[-*.RABCTSXED#]%', job_operationT.Operation_Service) - 1)) = 0\n" +
                                            "\t\t\t\tTHEN CONVERT(INT, SUBSTRING(job_operationT.Operation_Service, 1, PATINDEX('%[-*.RABCTSXED#]%', job_operationT.Operation_Service) - 1))\n" +
                                        "\t\t\tWHEN PATINDEX('%[-*.RABCSXED#]%', job_operationT.Operation_Service) = 1 AND ISNUMERIC(SUBSTRING(job_operationT.Operation_Service, 2, LEN(job_operationT.Operation_Service))) = 1 AND CHARINDEX('.', SUBSTRING(job_operationT.Operation_Service, 2, LEN(job_operationT.Operation_Service))) = 0\n" +
                                            "\t\t\t\tTHEN CONVERT(INT, SUBSTRING(job_operationT.Operation_Service, 2, LEN(job_operationT.Operation_Service)))\n" +
                                        "\t\t\tWHEN PATINDEX('%/%', job_operationT.Operation_Service) > 1 AND ISNUMERIC(SUBSTRING(job_operationT.Operation_Service, PATINDEX('%/%', job_operationT.Operation_Service) + 1, LEN(job_operationT.Operation_Service))) = 1 AND CHARINDEX('.', SUBSTRING(job_operationT.Operation_Service, PATINDEX('%/%', job_operationT.Operation_Service) + 1, LEN(job_operationT.Operation_Service))) = 0\n" +
                                            "\t\t\t\tTHEN CONVERT(INT, SUBSTRING(job_operationT.Operation_Service, PATINDEX('%/%', job_operationT.Operation_Service) + 1, LEN(job_operationT.Operation_Service)))\n" +
                                        "\t\t\tWHEN PATINDEX('%[-*.RABCTSXED#]%', job_operationT.Operation_Service) = 1 AND PATINDEX('%[-*.RABCTSXED#]%', SUBSTRING(job_operationT.Operation_Service, 2, LEN(job_operationT.Operation_Service))) > 0\n" +
                                            "\t\t\t\tAND ISNUMERIC(SUBSTRING(job_operationT.Operation_Service, 2, PATINDEX('%[-*.RABCTSXED#]%', SUBSTRING(job_operationT.Operation_Service, 2, LEN(job_operationT.Operation_Service))) - 1)) = 1\n" +
                                            "\t\t\t\tAND CHARINDEX('.', SUBSTRING(job_operationT.Operation_Service, 2, PATINDEX('%[-*.RABCTSXED#]%', SUBSTRING(job_operationT.Operation_Service, 2, LEN(job_operationT.Operation_Service))) - 1)) = 0\n" +
                                            "\t\t\t\tTHEN CONVERT(INT, SUBSTRING(job_operationT.Operation_Service, 2, PATINDEX('%[-*.RABCTSXED#]%', SUBSTRING(job_operationT.Operation_Service, 2, LEN(job_operationT.Operation_Service))) - 1))\n" +
                                        "\t\t\tWHEN PATINDEX('%[-*.RABCTSXED#]%', job_operationT.Operation_Service) = 1 AND PATINDEX('%[-*.RABCTSXED#]%', SUBSTRING(job_operationT.Operation_Service, 2, LEN(job_operationT.Operation_Service))) = 1\n" +
                                            "\t\t\t\tAND ISNUMERIC(SUBSTRING(job_operationT.Operation_Service, 3, LEN(job_operationT.Operation_Service))) = 1 AND CHARINDEX('.', SUBSTRING(job_operationT.Operation_Service, 3, LEN(job_operationT.Operation_Service))) = 0\n" +
                                            "\t\t\t\tTHEN CONVERT(INT, SUBSTRING(job_operationT.Operation_Service, 3, LEN(job_operationT.Operation_Service)))\n" +
                                        "\t\t\tWHEN CHARINDEX('MT#', job_operationT.Operation_Service) > 0\n" +
                                            "\t\t\t\tAND ISNUMERIC(SUBSTRING(job_operationT.Operation_Service, 4, LEN(job_operationT.Operation_Service))) = 1 AND\n" +
                                            "\t\t\t\tCHARINDEX('.', SUBSTRING(job_operationT.Operation_Service, 4, LEN(job_operationT.Operation_Service))) = 0\n" +
                                            "\t\t\t\tTHEN CONVERT(INT, SUBSTRING(job_operationT.Operation_Service, 4, LEN(job_operationT.Operation_Service)))\n" +
                                        "\t\t\tElSE NULL\n" +
                                    "\t\tEND AS Operation_Num, job_operationT.Operation_Service\n" +
                                    "\t\tFROM PRODUCTION.dbo.Job_Operation AS job_operationT\n" +
                                    "\t\tWHERE Operation_Service IS NOT NULL AND Operation_Service LIKE '%[0-9]%' AND Operation_Service NOT LIKE '%LU%' AND Operation_Service NOT LIKE '%SV50%'\n" +
                                    "\t\t) AS job_operation_formattedT_inner\n" +
                                "\t) AS job_operation_formattedT\n" +
                            "ON job_operation_formattedT.Job = ncT_formatedT.Job AND job_operation_formattedT.Operation_Num = ncT_formatedT.Operation_Num\n" +
                            "LEFT JOIN PRODUCTION.dbo.Job_Operation AS previous_operationsT\n" +
                            "ON previous_operationsT.Job = job_operation_formattedT.Job AND previous_operationsT.Sequence = job_operation_formattedT.Sequence\n" +
                            "LEFT JOIN\n" +
                                "\t(\n" +
                                "\tSELECT t.Job AS Job, MAX(t.Act_Run_Qty) AS max_run_qty\n" +
                                "\tFROM PRODUCTION.dbo.Job_Operation AS t\n" +
                                "\tGROUP BY Job\n" +
                                "\t) AS run_qtyT\n" +
                            "ON run_qtyT.job = job_operation_formattedT.Job\n" +
                            "WHERE job_operation_formattedT.Sequence IS NOT NULL\n" +
                                "\tAND job_operation_formattedT.Last_Updated >= CONVERT(DATE, '01/01/2015')\n" +
                                "\tAND job_operation_formattedT.Last_Updated < CONVERT(DATE, '01/01/2016')\n" +
                            "ORDER BY ncT_formatedT.NC_num ,job_operation_formattedT.Job, previous_operationsT.Sequence;";
                    }
                }
                else
                {
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
                                "ncT.Qty_Scrap AS 'NC Scrap Qty',\n" +
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
                if (scrapTypeComboBox.SelectedItem.ToString().Contains("Outside"))
                {
                    if (includeAllOpsCheckBox.Checked)
                    {
                        query =
                                                        "SELECT COUNT(DISTINCT ncT_formatedT.NC_num) AS nc_count, COUNT(DISTINCT job_operation_formattedT.Job) AS job_count, MAX(ncT_formatedT.NCR_Date) AS max_nc_date,\n" +
                            "MIN(ncT_formatedT.NCR_Date) AS main_nc_date, MAX(job_operation_formattedT.Last_Updated)max_job_operation_date,\n" +
                            "MIN(job_operation_formattedT.Last_Updated) min_job_operation_date,\n" +
                            "COUNT(previous_operationsT.Sequence) AS total_ops, SUM(ncT_formatedT.Qty_Scrap) AS total_scrap_parts,\n" +
                            "SUM(previous_operationsT.Act_Run_Labor + previous_operationsT.Act_Labor_Burden + previous_operationsT.Act_Machine_Burden) AS total_run_cost,\n" +
                            "SUM(run_qtyT.max_run_qty) AS total_ran_parts,\n" +
                            "SUM(CASE\n" +
                                "\tWHEN run_qtyT.max_run_qty = 0\n" +
                                "\tTHEN 0\n" +
                                "\tELSE(previous_operationsT.Act_Run_Labor + previous_operationsT.Act_Labor_Burden + previous_operationsT.Act_Machine_Burden) / run_qtyT.max_run_qty * ncT_formatedT.Qty_Scrap\n" +
                            "END) AS total_scrap_cost\n" +
                            "FROM\n" +
                                "\t(SELECT NCR AS NC_num, ncT.Job AS Job, ncT.Operation, ncT.Reference, ncT.NCR_Date, ncT.Qty_Scrap,\n" +
                                "\tCASE\n" +
                                    "\t\tWHEN ISNUMERIC(ncT.Operation) = 1\n" +
                                        "\t\t\tAND CHARINDEX('.', ncT.Operation) = 0\n" +
                                        "\t\t\tTHEN CONVERT(INT, ncT.Operation)\n" +
                                    "\t\tWHEN ISNUMERIC(ncT.Reference) = 1\n" +
                                        "\t\t\tAND CHARINDEX('.', ncT.Reference) = 0\n" +
                                        "\t\t\tTHEN CONVERT(INT, ncT.Reference)\n" +
                                    "\t\tWHEN PATINDEX('%[0-9]%', ncT.Reference) > 0 AND PATINDEX('%[-*R]%', ncT.Reference) = 0\n" +
                                        "\t\t\tAND ISNUMERIC(SUBSTRING(ncT.Reference, PATINDEX('%[0-9]%', ncT.Reference), LEN(ncT.Reference))) = 1\n" +
                                        "\t\t\tAND CHARINDEX('.', SUBSTRING(ncT.Reference, PATINDEX('%[0-9]%', ncT.Reference), LEN(ncT.Reference))) = 0\n" +
                                        "\t\t\tTHEN CONVERT(INT, SUBSTRING(ncT.Reference, PATINDEX('%[0-9]%', ncT.Reference), LEN(ncT.Reference)))\n" +
                                    "\t\tWHEN PATINDEX('%[0-9]%', ncT.Reference) > 0 AND PATINDEX('%[-*R]%', ncT.Reference) > 0\n" +
                                        "\t\t\tAND ISNUMERIC(SUBSTRING(ncT.Reference, PATINDEX('%[0-9]%', ncT.Reference), PATINDEX('%[-*R]%', ncT.Reference) - PATINDEX('%[0-9]%', ncT.Reference))) = 1\n" +
                                        "\t\t\tAND CHARINDEX('.', SUBSTRING(ncT.Reference, PATINDEX('%[0-9]%', ncT.Reference), PATINDEX('%[-*R]%', ncT.Reference) - PATINDEX('%[0-9]%', ncT.Reference))) = 0\n" +
                                        "\t\t\tTHEN CONVERT(INT, SUBSTRING(ncT.Reference, PATINDEX('%[0-9]%', ncT.Reference), PATINDEX('%[-*R]%', ncT.Reference) - PATINDEX('%[0-9]%', ncT.Reference)))\n" +
                                    "\t\tWHEN PATINDEX('%[R]%', ncT.Reference) > 1 AND ISNUMERIC(SUBSTRING(ncT.Reference, 1, PATINDEX('%[R]%', ncT.Reference) - 1)) = 1\n" +
                                        "\t\t\tAND ISNUMERIC(SUBSTRING(ncT.Reference, 1, PATINDEX('%[R]%', ncT.Reference) - 1)) = 1\n" +
                                        "\t\t\tAND CHARINDEX('.', SUBSTRING(ncT.Reference, 1, PATINDEX('%[R]%', ncT.Reference) - 1)) = 0\n" +
                                        "\t\t\tTHEN SUBSTRING(ncT.Reference, 1, PATINDEX('%[R]%', ncT.Reference) - 1)\n" +
                                    "\t\tWHEN PATINDEX('%[R*.]%', ncT.Operation) > 1\n" +
                                        "\t\t\tAND ISNUMERIC(SUBSTRING(ncT.Operation, 1, PATINDEX('%[R*.]%', ncT.Operation) - 1)) = 1\n" +
                                        "\t\t\tAND CHARINDEX('.', SUBSTRING(ncT.Operation, 1, PATINDEX('%[R*.]%', ncT.Operation) - 1)) = 0\n" +
                                        "\t\t\tTHEN SUBSTRING(ncT.Operation, 1, PATINDEX('%[R*.]%', ncT.Operation) - 1)\n" +
                                    "\t\tWHEN PATINDEX('%[/&]%', ncT.Operation) > 1 AND ISNUMERIC(SUBSTRING(ncT.Operation, PATINDEX('%[/&]%', ncT.Operation) + 1, LEN(ncT.Operation))) = 1\n" +
                                        "\t\t\tAND CHARINDEX('.', SUBSTRING(ncT.Operation, PATINDEX('%[/&]%', ncT.Operation) + 1, LEN(ncT.Operation))) = 0\n" +
                                        "\t\t\tTHEN SUBSTRING(ncT.Operation, PATINDEX('%[/&]%', ncT.Operation) + 1, LEN(ncT.Operation))\n" +
                                    "\t\tWHEN PATINDEX('%[/&]%', ncT.Reference) > 1 AND ISNUMERIC(SUBSTRING(ncT.Reference, PATINDEX('%[/&]%', ncT.Reference) + 1, LEN(ncT.Reference))) = 1\n" +
                                        "\t\t\tAND CHARINDEX('.', SUBSTRING(ncT.Reference, PATINDEX('%[/&]%', ncT.Reference) + 1, LEN(ncT.Reference))) = 0\n" +
                                        "\t\t\tTHEN SUBSTRING(ncT.Reference, PATINDEX('%[/&]%', ncT.Reference) + 1, LEN(ncT.Reference))\n" +
                                    "\t\tELSE NULL\n" +
                                "\tEND AS[Operation_Num]\n" +
                                "\tFROM uniPoint_Live.dbo.PT_NC AS ncT\n" +
                                "\tWHERE ncT.NCR_Date >= CONVERT(DATETIME, '6/13/2013 12:00:00 AM')\n" +
                                    "\t\tAND ncT.NC_type = 'In Process' AND ncT.Status = 'Closed' AND ncT.Qty_Scrap > 0\n" +
                                "\t) AS ncT_formatedT\n" +
                            "LEFT JOIN\n" +
                                "\t(SELECT job_operation_formattedT_inner.Job, job_operation_formattedT_inner.Operation_Num, job_operation_formattedT_inner.Sequence, job_operation_formattedT_inner.Last_Updated\n" +
                                "\tFROM\n" +
                                    "\t\t(SELECT job_operationT.Job, job_operationT.Sequence, job_operationT.Last_Updated,\n" +
                                    "\t\tCASE\n" +
                                        "\t\t\tWHEN ISNUMERIC(job_operationT.Operation_Service) = 1 AND CHARINDEX('.', job_operationT.Operation_Service) = 0\n" +
                                            "\t\t\t\tTHEN CONVERT(INT, job_operationT.Operation_Service)\n" +
                                        "\t\t\tWHEN PATINDEX('%[-*.RABCTSXED#]%', job_operationT.Operation_Service) > 1 AND ISNUMERIC(SUBSTRING(job_operationT.Operation_Service, 1, PATINDEX('%[-*.RABCTSXED#]%', job_operationT.Operation_Service) - 1)) = 1 AND CHARINDEX('.', SUBSTRING(job_operationT.Operation_Service, 1, PATINDEX('%[-*.RABCTSXED#]%', job_operationT.Operation_Service) - 1)) = 0\n" +
                                            "\t\t\t\tTHEN CONVERT(INT, SUBSTRING(job_operationT.Operation_Service, 1, PATINDEX('%[-*.RABCTSXED#]%', job_operationT.Operation_Service) - 1))\n" +
                                        "\t\t\tWHEN PATINDEX('%[-*.RABCSXED#]%', job_operationT.Operation_Service) = 1 AND ISNUMERIC(SUBSTRING(job_operationT.Operation_Service, 2, LEN(job_operationT.Operation_Service))) = 1 AND CHARINDEX('.', SUBSTRING(job_operationT.Operation_Service, 2, LEN(job_operationT.Operation_Service))) = 0\n" +
                                            "\t\t\t\tTHEN CONVERT(INT, SUBSTRING(job_operationT.Operation_Service, 2, LEN(job_operationT.Operation_Service)))\n" +
                                        "\t\t\tWHEN PATINDEX('%/%', job_operationT.Operation_Service) > 1 AND ISNUMERIC(SUBSTRING(job_operationT.Operation_Service, PATINDEX('%/%', job_operationT.Operation_Service) + 1, LEN(job_operationT.Operation_Service))) = 1 AND CHARINDEX('.', SUBSTRING(job_operationT.Operation_Service, PATINDEX('%/%', job_operationT.Operation_Service) + 1, LEN(job_operationT.Operation_Service))) = 0\n" +
                                            "\t\t\t\tTHEN CONVERT(INT, SUBSTRING(job_operationT.Operation_Service, PATINDEX('%/%', job_operationT.Operation_Service) + 1, LEN(job_operationT.Operation_Service)))\n" +
                                        "\t\t\tWHEN PATINDEX('%[-*.RABCTSXED#]%', job_operationT.Operation_Service) = 1 AND PATINDEX('%[-*.RABCTSXED#]%', SUBSTRING(job_operationT.Operation_Service, 2, LEN(job_operationT.Operation_Service))) > 0\n" +
                                            "\t\t\t\tAND ISNUMERIC(SUBSTRING(job_operationT.Operation_Service, 2, PATINDEX('%[-*.RABCTSXED#]%', SUBSTRING(job_operationT.Operation_Service, 2, LEN(job_operationT.Operation_Service))) - 1)) = 1\n" +
                                            "\t\t\t\tAND CHARINDEX('.', SUBSTRING(job_operationT.Operation_Service, 2, PATINDEX('%[-*.RABCTSXED#]%', SUBSTRING(job_operationT.Operation_Service, 2, LEN(job_operationT.Operation_Service))) - 1)) = 0\n" +
                                            "\t\t\t\tTHEN CONVERT(INT, SUBSTRING(job_operationT.Operation_Service, 2, PATINDEX('%[-*.RABCTSXED#]%', SUBSTRING(job_operationT.Operation_Service, 2, LEN(job_operationT.Operation_Service))) - 1))\n" +
                                        "\t\t\tWHEN PATINDEX('%[-*.RABCTSXED#]%', job_operationT.Operation_Service) = 1 AND PATINDEX('%[-*.RABCTSXED#]%', SUBSTRING(job_operationT.Operation_Service, 2, LEN(job_operationT.Operation_Service))) = 1\n" +
                                            "\t\t\t\tAND ISNUMERIC(SUBSTRING(job_operationT.Operation_Service, 3, LEN(job_operationT.Operation_Service))) = 1 AND CHARINDEX('.', SUBSTRING(job_operationT.Operation_Service, 3, LEN(job_operationT.Operation_Service))) = 0\n" +
                                            "\t\t\t\tTHEN CONVERT(INT, SUBSTRING(job_operationT.Operation_Service, 3, LEN(job_operationT.Operation_Service)))\n" +
                                        "\t\t\tWHEN CHARINDEX('MT#', job_operationT.Operation_Service) > 0\n" +
                                            "\t\t\t\tAND ISNUMERIC(SUBSTRING(job_operationT.Operation_Service, 4, LEN(job_operationT.Operation_Service))) = 1 AND\n" +
                                            "\t\t\t\tCHARINDEX('.', SUBSTRING(job_operationT.Operation_Service, 4, LEN(job_operationT.Operation_Service))) = 0\n" +
                                            "\t\t\t\tTHEN CONVERT(INT, SUBSTRING(job_operationT.Operation_Service, 4, LEN(job_operationT.Operation_Service)))\n" +
                                        "\t\t\tElSE NULL\n" +
                                    "\t\tEND AS Operation_Num, job_operationT.Operation_Service\n" +
                                    "\t\tFROM PRODUCTION.dbo.Job_Operation AS job_operationT\n" +
                                    "\t\tWHERE Operation_Service IS NOT NULL AND Operation_Service LIKE '%[0-9]%' AND Operation_Service NOT LIKE '%LU%' AND Operation_Service NOT LIKE '%SV50%'\n" +
                                    "\t\t) AS job_operation_formattedT_inner\n" +
                                "\t) AS job_operation_formattedT\n" +
                            "ON job_operation_formattedT.Job = ncT_formatedT.Job AND job_operation_formattedT.Operation_Num = ncT_formatedT.Operation_Num\n" +
                            "LEFT JOIN PRODUCTION.dbo.Job_Operation AS previous_operationsT\n" +
                            "ON previous_operationsT.Job = job_operation_formattedT.Job AND previous_operationsT.Sequence <= job_operation_formattedT.Sequence\n" +
                            "LEFT JOIN\n" +
                                "\t(\n" +
                                "\tSELECT t.Job AS Job, MAX(t.Act_Run_Qty) AS max_run_qty\n" +
                                "\tFROM PRODUCTION.dbo.Job_Operation AS t\n" +
                                "\tGROUP BY Job\n" +
                                "\t) AS run_qtyT\n" +
                            "ON run_qtyT.job = job_operation_formattedT.Job\n" +
                            "WHERE job_operation_formattedT.Sequence IS NOT NULL\n" +
                                "\tAND job_operation_formattedT.Last_Updated >= CONVERT(DATE, '01/01/2015')\n" +
                                "\tAND job_operation_formattedT.Last_Updated < CONVERT(DATE, '01/01/2016');\n";
                    }
                    else
                    {
                        query =
                            "SELECT COUNT(DISTINCT ncT_formatedT.NC_num) AS nc_count, COUNT(DISTINCT job_operation_formattedT.Job) AS job_count, MAX(ncT_formatedT.NCR_Date) AS max_nc_date,\n" +
                            "MIN(ncT_formatedT.NCR_Date) AS main_nc_date, MAX(job_operation_formattedT.Last_Updated)max_job_operation_date,\n" +
                            "MIN(job_operation_formattedT.Last_Updated) min_job_operation_date,\n" +
                            "COUNT(previous_operationsT.Sequence) AS total_ops, SUM(ncT_formatedT.Qty_Scrap) AS total_scrap_parts,\n" +
                            "SUM(previous_operationsT.Act_Run_Labor + previous_operationsT.Act_Labor_Burden + previous_operationsT.Act_Machine_Burden) AS total_run_cost,\n" +
                            "SUM(run_qtyT.max_run_qty) AS total_ran_parts,\n" +
                            "SUM(CASE\n" +
                                "\tWHEN run_qtyT.max_run_qty = 0\n" +
                                "\tTHEN 0\n" +
                                "\tELSE(previous_operationsT.Act_Run_Labor + previous_operationsT.Act_Labor_Burden + previous_operationsT.Act_Machine_Burden) / run_qtyT.max_run_qty * ncT_formatedT.Qty_Scrap\n" +
                            "END) AS total_scrap_cost\n" +
                            "FROM\n" +
                                "\t(SELECT NCR AS NC_num, ncT.Job AS Job, ncT.Operation, ncT.Reference, ncT.NCR_Date, ncT.Qty_Scrap,\n" +
                                "\tCASE\n" +
                                    "\t\tWHEN ISNUMERIC(ncT.Operation) = 1\n" +
                                        "\t\t\tAND CHARINDEX('.', ncT.Operation) = 0\n" +
                                        "\t\t\tTHEN CONVERT(INT, ncT.Operation)\n" +
                                    "\t\tWHEN ISNUMERIC(ncT.Reference) = 1\n" +
                                        "\t\t\tAND CHARINDEX('.', ncT.Reference) = 0\n" +
                                        "\t\t\tTHEN CONVERT(INT, ncT.Reference)\n" +
                                    "\t\tWHEN PATINDEX('%[0-9]%', ncT.Reference) > 0 AND PATINDEX('%[-*R]%', ncT.Reference) = 0\n" +
                                        "\t\t\tAND ISNUMERIC(SUBSTRING(ncT.Reference, PATINDEX('%[0-9]%', ncT.Reference), LEN(ncT.Reference))) = 1\n" +
                                        "\t\t\tAND CHARINDEX('.', SUBSTRING(ncT.Reference, PATINDEX('%[0-9]%', ncT.Reference), LEN(ncT.Reference))) = 0\n" +
                                        "\t\t\tTHEN CONVERT(INT, SUBSTRING(ncT.Reference, PATINDEX('%[0-9]%', ncT.Reference), LEN(ncT.Reference)))\n" +
                                    "\t\tWHEN PATINDEX('%[0-9]%', ncT.Reference) > 0 AND PATINDEX('%[-*R]%', ncT.Reference) > 0\n" +
                                        "\t\t\tAND ISNUMERIC(SUBSTRING(ncT.Reference, PATINDEX('%[0-9]%', ncT.Reference), PATINDEX('%[-*R]%', ncT.Reference) - PATINDEX('%[0-9]%', ncT.Reference))) = 1\n" +
                                        "\t\t\tAND CHARINDEX('.', SUBSTRING(ncT.Reference, PATINDEX('%[0-9]%', ncT.Reference), PATINDEX('%[-*R]%', ncT.Reference) - PATINDEX('%[0-9]%', ncT.Reference))) = 0\n" +
                                        "\t\t\tTHEN CONVERT(INT, SUBSTRING(ncT.Reference, PATINDEX('%[0-9]%', ncT.Reference), PATINDEX('%[-*R]%', ncT.Reference) - PATINDEX('%[0-9]%', ncT.Reference)))\n" +
                                    "\t\tWHEN PATINDEX('%[R]%', ncT.Reference) > 1 AND ISNUMERIC(SUBSTRING(ncT.Reference, 1, PATINDEX('%[R]%', ncT.Reference) - 1)) = 1\n" +
                                        "\t\t\tAND ISNUMERIC(SUBSTRING(ncT.Reference, 1, PATINDEX('%[R]%', ncT.Reference) - 1)) = 1\n" +
                                        "\t\t\tAND CHARINDEX('.', SUBSTRING(ncT.Reference, 1, PATINDEX('%[R]%', ncT.Reference) - 1)) = 0\n" +
                                        "\t\t\tTHEN SUBSTRING(ncT.Reference, 1, PATINDEX('%[R]%', ncT.Reference) - 1)\n" +
                                    "\t\tWHEN PATINDEX('%[R*.]%', ncT.Operation) > 1\n" +
                                        "\t\t\tAND ISNUMERIC(SUBSTRING(ncT.Operation, 1, PATINDEX('%[R*.]%', ncT.Operation) - 1)) = 1\n" +
                                        "\t\t\tAND CHARINDEX('.', SUBSTRING(ncT.Operation, 1, PATINDEX('%[R*.]%', ncT.Operation) - 1)) = 0\n" +
                                        "\t\t\tTHEN SUBSTRING(ncT.Operation, 1, PATINDEX('%[R*.]%', ncT.Operation) - 1)\n" +
                                    "\t\tWHEN PATINDEX('%[/&]%', ncT.Operation) > 1 AND ISNUMERIC(SUBSTRING(ncT.Operation, PATINDEX('%[/&]%', ncT.Operation) + 1, LEN(ncT.Operation))) = 1\n" +
                                        "\t\t\tAND CHARINDEX('.', SUBSTRING(ncT.Operation, PATINDEX('%[/&]%', ncT.Operation) + 1, LEN(ncT.Operation))) = 0\n" +
                                        "\t\t\tTHEN SUBSTRING(ncT.Operation, PATINDEX('%[/&]%', ncT.Operation) + 1, LEN(ncT.Operation))\n" +
                                    "\t\tWHEN PATINDEX('%[/&]%', ncT.Reference) > 1 AND ISNUMERIC(SUBSTRING(ncT.Reference, PATINDEX('%[/&]%', ncT.Reference) + 1, LEN(ncT.Reference))) = 1\n" +
                                        "\t\t\tAND CHARINDEX('.', SUBSTRING(ncT.Reference, PATINDEX('%[/&]%', ncT.Reference) + 1, LEN(ncT.Reference))) = 0\n" +
                                        "\t\t\tTHEN SUBSTRING(ncT.Reference, PATINDEX('%[/&]%', ncT.Reference) + 1, LEN(ncT.Reference))\n" +
                                    "\t\tELSE NULL\n" +
                                "\tEND AS[Operation_Num]\n" +
                                "\tFROM uniPoint_Live.dbo.PT_NC AS ncT\n" +
                                "\tWHERE ncT.NCR_Date >= CONVERT(DATETIME, '6/13/2013 12:00:00 AM')\n" +
                                    "\t\tAND ncT.NC_type = 'In Process' AND ncT.Status = 'Closed' AND ncT.Qty_Scrap > 0\n" +
                                "\t) AS ncT_formatedT\n" +
                            "LEFT JOIN\n" +
                                "\t(SELECT job_operation_formattedT_inner.Job, job_operation_formattedT_inner.Operation_Num, job_operation_formattedT_inner.Sequence, job_operation_formattedT_inner.Last_Updated\n" +
                                "\tFROM\n" +
                                    "\t\t(SELECT job_operationT.Job, job_operationT.Sequence, job_operationT.Last_Updated,\n" +
                                    "\t\tCASE\n" +
                                        "\t\t\tWHEN ISNUMERIC(job_operationT.Operation_Service) = 1 AND CHARINDEX('.', job_operationT.Operation_Service) = 0\n" +
                                            "\t\t\t\tTHEN CONVERT(INT, job_operationT.Operation_Service)\n" +
                                        "\t\t\tWHEN PATINDEX('%[-*.RABCTSXED#]%', job_operationT.Operation_Service) > 1 AND ISNUMERIC(SUBSTRING(job_operationT.Operation_Service, 1, PATINDEX('%[-*.RABCTSXED#]%', job_operationT.Operation_Service) - 1)) = 1 AND CHARINDEX('.', SUBSTRING(job_operationT.Operation_Service, 1, PATINDEX('%[-*.RABCTSXED#]%', job_operationT.Operation_Service) - 1)) = 0\n" +
                                            "\t\t\t\tTHEN CONVERT(INT, SUBSTRING(job_operationT.Operation_Service, 1, PATINDEX('%[-*.RABCTSXED#]%', job_operationT.Operation_Service) - 1))\n" +
                                        "\t\t\tWHEN PATINDEX('%[-*.RABCSXED#]%', job_operationT.Operation_Service) = 1 AND ISNUMERIC(SUBSTRING(job_operationT.Operation_Service, 2, LEN(job_operationT.Operation_Service))) = 1 AND CHARINDEX('.', SUBSTRING(job_operationT.Operation_Service, 2, LEN(job_operationT.Operation_Service))) = 0\n" +
                                            "\t\t\t\tTHEN CONVERT(INT, SUBSTRING(job_operationT.Operation_Service, 2, LEN(job_operationT.Operation_Service)))\n" +
                                        "\t\t\tWHEN PATINDEX('%/%', job_operationT.Operation_Service) > 1 AND ISNUMERIC(SUBSTRING(job_operationT.Operation_Service, PATINDEX('%/%', job_operationT.Operation_Service) + 1, LEN(job_operationT.Operation_Service))) = 1 AND CHARINDEX('.', SUBSTRING(job_operationT.Operation_Service, PATINDEX('%/%', job_operationT.Operation_Service) + 1, LEN(job_operationT.Operation_Service))) = 0\n" +
                                            "\t\t\t\tTHEN CONVERT(INT, SUBSTRING(job_operationT.Operation_Service, PATINDEX('%/%', job_operationT.Operation_Service) + 1, LEN(job_operationT.Operation_Service)))\n" +
                                        "\t\t\tWHEN PATINDEX('%[-*.RABCTSXED#]%', job_operationT.Operation_Service) = 1 AND PATINDEX('%[-*.RABCTSXED#]%', SUBSTRING(job_operationT.Operation_Service, 2, LEN(job_operationT.Operation_Service))) > 0\n" +
                                            "\t\t\t\tAND ISNUMERIC(SUBSTRING(job_operationT.Operation_Service, 2, PATINDEX('%[-*.RABCTSXED#]%', SUBSTRING(job_operationT.Operation_Service, 2, LEN(job_operationT.Operation_Service))) - 1)) = 1\n" +
                                            "\t\t\t\tAND CHARINDEX('.', SUBSTRING(job_operationT.Operation_Service, 2, PATINDEX('%[-*.RABCTSXED#]%', SUBSTRING(job_operationT.Operation_Service, 2, LEN(job_operationT.Operation_Service))) - 1)) = 0\n" +
                                            "\t\t\t\tTHEN CONVERT(INT, SUBSTRING(job_operationT.Operation_Service, 2, PATINDEX('%[-*.RABCTSXED#]%', SUBSTRING(job_operationT.Operation_Service, 2, LEN(job_operationT.Operation_Service))) - 1))\n" +
                                        "\t\t\tWHEN PATINDEX('%[-*.RABCTSXED#]%', job_operationT.Operation_Service) = 1 AND PATINDEX('%[-*.RABCTSXED#]%', SUBSTRING(job_operationT.Operation_Service, 2, LEN(job_operationT.Operation_Service))) = 1\n" +
                                            "\t\t\t\tAND ISNUMERIC(SUBSTRING(job_operationT.Operation_Service, 3, LEN(job_operationT.Operation_Service))) = 1 AND CHARINDEX('.', SUBSTRING(job_operationT.Operation_Service, 3, LEN(job_operationT.Operation_Service))) = 0\n" +
                                            "\t\t\t\tTHEN CONVERT(INT, SUBSTRING(job_operationT.Operation_Service, 3, LEN(job_operationT.Operation_Service)))\n" +
                                        "\t\t\tWHEN CHARINDEX('MT#', job_operationT.Operation_Service) > 0\n" +
                                            "\t\t\t\tAND ISNUMERIC(SUBSTRING(job_operationT.Operation_Service, 4, LEN(job_operationT.Operation_Service))) = 1 AND\n" +
                                            "\t\t\t\tCHARINDEX('.', SUBSTRING(job_operationT.Operation_Service, 4, LEN(job_operationT.Operation_Service))) = 0\n" +
                                            "\t\t\t\tTHEN CONVERT(INT, SUBSTRING(job_operationT.Operation_Service, 4, LEN(job_operationT.Operation_Service)))\n" +
                                        "\t\t\tElSE NULL\n" +
                                    "\t\tEND AS Operation_Num, job_operationT.Operation_Service\n" +
                                    "\t\tFROM PRODUCTION.dbo.Job_Operation AS job_operationT\n" +
                                    "\t\tWHERE Operation_Service IS NOT NULL AND Operation_Service LIKE '%[0-9]%' AND Operation_Service NOT LIKE '%LU%' AND Operation_Service NOT LIKE '%SV50%'\n" +
                                    "\t\t) AS job_operation_formattedT_inner\n" +
                                "\t) AS job_operation_formattedT\n" +
                            "ON job_operation_formattedT.Job = ncT_formatedT.Job AND job_operation_formattedT.Operation_Num = ncT_formatedT.Operation_Num\n" +
                            "LEFT JOIN PRODUCTION.dbo.Job_Operation AS previous_operationsT\n" +
                            "ON previous_operationsT.Job = job_operation_formattedT.Job AND previous_operationsT.Sequence = job_operation_formattedT.Sequence\n" +
                            "LEFT JOIN\n" +
                                "\t(\n" +
                                "\tSELECT t.Job AS Job, MAX(t.Act_Run_Qty) AS max_run_qty\n" +
                                "\tFROM PRODUCTION.dbo.Job_Operation AS t\n" +
                                "\tGROUP BY Job\n" +
                                "\t) AS run_qtyT\n" +
                            "ON run_qtyT.job = job_operation_formattedT.Job\n" +
                            "WHERE job_operation_formattedT.Sequence IS NOT NULL\n" +
                                "\tAND job_operation_formattedT.Last_Updated >= CONVERT(DATE, '01/01/2015')\n" +
                                "\tAND job_operation_formattedT.Last_Updated < CONVERT(DATE, '01/01/2016');\n";
                    }
                }
                else
                {
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
