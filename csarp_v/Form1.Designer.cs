using System.Windows.Forms;

namespace TaskSchedulerDP
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // Form1
            // 
            this.ClientSize = Screen.PrimaryScreen.WorkingArea.Size;
            this.Name = "Form1";
            this.Text = "Task Scheduler";
            this.ResumeLayout(false);
        }
    }
}
