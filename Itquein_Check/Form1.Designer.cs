namespace Itquein_Check
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
            btn_iniciar = new Button();
            btn_detener = new Button();
            timer1 = new System.Windows.Forms.Timer(components);
            richTextBox1 = new RichTextBox();
            SuspendLayout();
            // 
            // btn_iniciar
            // 
            btn_iniciar.Location = new Point(392, 400);
            btn_iniciar.Name = "btn_iniciar";
            btn_iniciar.Size = new Size(94, 29);
            btn_iniciar.TabIndex = 0;
            btn_iniciar.Text = "Iniciar Servicio";
            btn_iniciar.UseVisualStyleBackColor = true;
            btn_iniciar.Click += btn_iniciar_Click;
            // 
            // btn_detener
            // 
            btn_detener.Location = new Point(292, 400);
            btn_detener.Name = "btn_detener";
            btn_detener.Size = new Size(94, 29);
            btn_detener.TabIndex = 1;
            btn_detener.Text = "Detener Servicio";
            btn_detener.UseVisualStyleBackColor = true;
            btn_detener.Click += btn_detener_Click;
            // 
            // timer1
            // 
            timer1.Tick += timer1_Tick;
            // 
            // richTextBox1
            // 
            richTextBox1.Location = new Point(12, 57);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.Size = new Size(474, 337);
            richTextBox1.TabIndex = 2;
            richTextBox1.Text = "";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(498, 450);
            Controls.Add(richTextBox1);
            Controls.Add(btn_detener);
            Controls.Add(btn_iniciar);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
        }

        #endregion

        private Button btn_iniciar;
        private Button btn_detener;
        private System.Windows.Forms.Timer timer1;
        private RichTextBox richTextBox1;
    }
}
