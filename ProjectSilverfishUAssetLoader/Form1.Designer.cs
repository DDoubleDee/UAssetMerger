namespace ProjectSilverfishUAssetLoader
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            btnLoadModOrder = new Button();
            lbModOrder = new CheckedListBox();
            btnUpButton = new Button();
            btnDownButton = new Button();
            btnGenerate = new Button();
            btnOverwrite = new Button();
            btnCleanGenerated = new Button();
            btnRestore = new Button();
            timer = new System.Windows.Forms.Timer(components);
            btnCleanBackups = new Button();
            toolTip1 = new ToolTip(components);
            SuspendLayout();
            // 
            // btnLoadModOrder
            // 
            btnLoadModOrder.Location = new Point(477, 12);
            btnLoadModOrder.Name = "btnLoadModOrder";
            btnLoadModOrder.Size = new Size(115, 25);
            btnLoadModOrder.TabIndex = 0;
            btnLoadModOrder.Text = "Load Mod Order";
            toolTip1.SetToolTip(btnLoadModOrder, "Load mod order from file and Mods folder");
            btnLoadModOrder.UseVisualStyleBackColor = true;
            btnLoadModOrder.Click += btnLoadModOrder_Click;
            // 
            // lbModOrder
            // 
            lbModOrder.CheckOnClick = true;
            lbModOrder.FormattingEnabled = true;
            lbModOrder.Location = new Point(41, 13);
            lbModOrder.Name = "lbModOrder";
            lbModOrder.ScrollAlwaysVisible = true;
            lbModOrder.Size = new Size(430, 293);
            lbModOrder.TabIndex = 3;
            lbModOrder.SelectedIndexChanged += lbModOrder_SelectedIndexChanged;
            // 
            // btnUpButton
            // 
            btnUpButton.Location = new Point(12, 13);
            btnUpButton.Name = "btnUpButton";
            btnUpButton.Padding = new Padding(1, 0, 0, 0);
            btnUpButton.Size = new Size(23, 25);
            btnUpButton.TabIndex = 4;
            btnUpButton.Text = "↑";
            btnUpButton.UseVisualStyleBackColor = true;
            btnUpButton.Click += btnUpButton_Click;
            // 
            // btnDownButton
            // 
            btnDownButton.Location = new Point(12, 44);
            btnDownButton.Name = "btnDownButton";
            btnDownButton.Padding = new Padding(1, 0, 0, 0);
            btnDownButton.Size = new Size(23, 25);
            btnDownButton.TabIndex = 5;
            btnDownButton.Text = "↓";
            btnDownButton.UseVisualStyleBackColor = true;
            btnDownButton.Click += btnDownButton_Click;
            // 
            // btnGenerate
            // 
            btnGenerate.Location = new Point(477, 43);
            btnGenerate.Name = "btnGenerate";
            btnGenerate.Size = new Size(115, 25);
            btnGenerate.TabIndex = 7;
            btnGenerate.Text = "Generate Files";
            toolTip1.SetToolTip(btnGenerate, "Generate the folder that contains all merged files");
            btnGenerate.UseVisualStyleBackColor = true;
            btnGenerate.Click += btnGenerate_Click;
            // 
            // btnOverwrite
            // 
            btnOverwrite.Font = new Font("Arial", 9.75F, FontStyle.Bold | FontStyle.Underline, GraphicsUnit.Point, 0);
            btnOverwrite.Location = new Point(477, 74);
            btnOverwrite.Name = "btnOverwrite";
            btnOverwrite.Size = new Size(115, 25);
            btnOverwrite.TabIndex = 8;
            btnOverwrite.Text = "Overwrite";
            toolTip1.SetToolTip(btnOverwrite, "Overwrite files from the Generated folder into the game folder, backup is created in Backups folder");
            btnOverwrite.UseVisualStyleBackColor = true;
            btnOverwrite.Click += btnOverwrite_Click;
            // 
            // btnCleanGenerated
            // 
            btnCleanGenerated.Location = new Point(477, 105);
            btnCleanGenerated.Name = "btnCleanGenerated";
            btnCleanGenerated.Size = new Size(115, 25);
            btnCleanGenerated.TabIndex = 9;
            btnCleanGenerated.Text = "Clean Generated";
            toolTip1.SetToolTip(btnCleanGenerated, "Delete the Generated folder to generate fresh files");
            btnCleanGenerated.UseVisualStyleBackColor = true;
            btnCleanGenerated.Click += btnCleanGenerated_Click;
            // 
            // btnRestore
            // 
            btnRestore.Location = new Point(477, 136);
            btnRestore.Name = "btnRestore";
            btnRestore.Size = new Size(115, 25);
            btnRestore.TabIndex = 10;
            btnRestore.Text = "Restore Backups";
            toolTip1.SetToolTip(btnRestore, "Revert the Overwrite action by deleting every new file and placing back replaced files");
            btnRestore.UseVisualStyleBackColor = true;
            btnRestore.Click += btnRestore_Click;
            // 
            // timer
            // 
            timer.Enabled = true;
            timer.Tick += timer_Tick;
            // 
            // btnCleanBackups
            // 
            btnCleanBackups.Location = new Point(477, 167);
            btnCleanBackups.Name = "btnCleanBackups";
            btnCleanBackups.Size = new Size(115, 25);
            btnCleanBackups.TabIndex = 11;
            btnCleanBackups.Text = "Clean Backups";
            toolTip1.SetToolTip(btnCleanBackups, "Clean the backups folder. Usually only used when you have restored backups, or in case you recently updated your game");
            btnCleanBackups.UseVisualStyleBackColor = true;
            btnCleanBackups.Click += btnCleanBackups_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 16F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(604, 317);
            Controls.Add(btnCleanBackups);
            Controls.Add(btnRestore);
            Controls.Add(btnCleanGenerated);
            Controls.Add(btnOverwrite);
            Controls.Add(btnGenerate);
            Controls.Add(btnDownButton);
            Controls.Add(btnUpButton);
            Controls.Add(lbModOrder);
            Controls.Add(btnLoadModOrder);
            Font = new Font("Arial", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Name = "Form1";
            Text = "Project Silverfish Mod Loader";
            ResumeLayout(false);
        }

        #endregion

        private Button btnLoadModOrder;
        private CheckedListBox lbModOrder;
        private Button btnUpButton;
        private Button btnDownButton;
        private Button btnGenerate;
        private Button btnOverwrite;
        private Button btnCleanGenerated;
        private Button btnRestore;
        private System.Windows.Forms.Timer timer;
        private Button btnCleanBackups;
        private ToolTip toolTip1;
    }
}
