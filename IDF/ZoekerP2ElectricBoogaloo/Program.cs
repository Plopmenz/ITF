using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZoekerP2ElectricBoogaloo
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Form1 GUI = new Form1();
            QueryProcessor processor = new QueryProcessor();
            GUI.confirmButton.Click += (object sender, EventArgs e) => GUI.output.Text = processor.Process(GUI.input.Text);
            Application.Run(GUI);
        }
    }
}
