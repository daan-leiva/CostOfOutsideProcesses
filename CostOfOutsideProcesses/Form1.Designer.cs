namespace CostOfOutsideProcesses
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.detailsDataGridView = new System.Windows.Forms.DataGridView();
            this.totalsDataGridView = new System.Windows.Forms.DataGridView();
            this.label1 = new System.Windows.Forms.Label();
            this.startDateTimePicker = new System.Windows.Forms.DateTimePicker();
            this.endDateTimePicker = new System.Windows.Forms.DateTimePicker();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.includeAllOpsCheckBox = new System.Windows.Forms.CheckBox();
            this.rowLabel = new System.Windows.Forms.Label();
            this.groupByJobsCheckBox = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.detailsDataGridView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.totalsDataGridView)).BeginInit();
            this.SuspendLayout();
            // 
            // detailsDataGridView
            // 
            this.detailsDataGridView.AllowUserToAddRows = false;
            this.detailsDataGridView.AllowUserToDeleteRows = false;
            this.detailsDataGridView.AllowUserToResizeColumns = false;
            this.detailsDataGridView.AllowUserToResizeRows = false;
            this.detailsDataGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.detailsDataGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.detailsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.detailsDataGridView.Location = new System.Drawing.Point(14, 121);
            this.detailsDataGridView.MultiSelect = false;
            this.detailsDataGridView.Name = "detailsDataGridView";
            this.detailsDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.detailsDataGridView.Size = new System.Drawing.Size(1372, 286);
            this.detailsDataGridView.TabIndex = 0;
            // 
            // totalsDataGridView
            // 
            this.totalsDataGridView.AllowUserToAddRows = false;
            this.totalsDataGridView.AllowUserToDeleteRows = false;
            this.totalsDataGridView.AllowUserToResizeColumns = false;
            this.totalsDataGridView.AllowUserToResizeRows = false;
            this.totalsDataGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.totalsDataGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.totalsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.totalsDataGridView.Location = new System.Drawing.Point(1005, 413);
            this.totalsDataGridView.MultiSelect = false;
            this.totalsDataGridView.Name = "totalsDataGridView";
            this.totalsDataGridView.Size = new System.Drawing.Size(381, 72);
            this.totalsDataGridView.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(16, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(265, 24);
            this.label1.TabIndex = 2;
            this.label1.Text = "Outside Operations Cost Detail";
            // 
            // startDateTimePicker
            // 
            this.startDateTimePicker.Location = new System.Drawing.Point(19, 81);
            this.startDateTimePicker.Margin = new System.Windows.Forms.Padding(2);
            this.startDateTimePicker.Name = "startDateTimePicker";
            this.startDateTimePicker.Size = new System.Drawing.Size(217, 21);
            this.startDateTimePicker.TabIndex = 3;
            // 
            // endDateTimePicker
            // 
            this.endDateTimePicker.Location = new System.Drawing.Point(256, 81);
            this.endDateTimePicker.Margin = new System.Windows.Forms.Padding(2);
            this.endDateTimePicker.Name = "endDateTimePicker";
            this.endDateTimePicker.Size = new System.Drawing.Size(217, 21);
            this.endDateTimePicker.TabIndex = 4;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(19, 64);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(61, 15);
            this.label2.TabIndex = 5;
            this.label2.Text = "Start Date";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(256, 64);
            this.label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(58, 15);
            this.label3.TabIndex = 6;
            this.label3.Text = "End Date";
            // 
            // includeAllOpsCheckBox
            // 
            this.includeAllOpsCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.includeAllOpsCheckBox.AutoSize = true;
            this.includeAllOpsCheckBox.Location = new System.Drawing.Point(1245, 77);
            this.includeAllOpsCheckBox.Name = "includeAllOpsCheckBox";
            this.includeAllOpsCheckBox.Size = new System.Drawing.Size(141, 19);
            this.includeAllOpsCheckBox.TabIndex = 7;
            this.includeAllOpsCheckBox.Text = "Include Previous Ops";
            this.includeAllOpsCheckBox.UseVisualStyleBackColor = true;
            this.includeAllOpsCheckBox.CheckedChanged += new System.EventHandler(this.includeAllOpsCheckBox_CheckedChanged);
            // 
            // rowLabel
            // 
            this.rowLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.rowLabel.AutoSize = true;
            this.rowLabel.Location = new System.Drawing.Point(1308, 490);
            this.rowLabel.Name = "rowLabel";
            this.rowLabel.Size = new System.Drawing.Size(41, 15);
            this.rowLabel.TabIndex = 8;
            this.rowLabel.Text = "Rows:";
            // 
            // groupByJobsCheckBox
            // 
            this.groupByJobsCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.groupByJobsCheckBox.AutoSize = true;
            this.groupByJobsCheckBox.Location = new System.Drawing.Point(1245, 98);
            this.groupByJobsCheckBox.Name = "groupByJobsCheckBox";
            this.groupByJobsCheckBox.Size = new System.Drawing.Size(105, 19);
            this.groupByJobsCheckBox.TabIndex = 9;
            this.groupByJobsCheckBox.Text = "Group By Jobs";
            this.groupByJobsCheckBox.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1400, 513);
            this.Controls.Add(this.groupByJobsCheckBox);
            this.Controls.Add(this.rowLabel);
            this.Controls.Add(this.includeAllOpsCheckBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.endDateTimePicker);
            this.Controls.Add(this.startDateTimePicker);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.totalsDataGridView);
            this.Controls.Add(this.detailsDataGridView);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MinimumSize = new System.Drawing.Size(793, 413);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.detailsDataGridView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.totalsDataGridView)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView detailsDataGridView;
        private System.Windows.Forms.DataGridView totalsDataGridView;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.DateTimePicker startDateTimePicker;
        private System.Windows.Forms.DateTimePicker endDateTimePicker;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox includeAllOpsCheckBox;
        private System.Windows.Forms.Label rowLabel;
        private System.Windows.Forms.CheckBox groupByJobsCheckBox;
    }
}

