using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZoekerP2ElectricBoogaloo
{
    public partial class Form1 : Form
    {
        QueryProcessor processor = new QueryProcessor();
        public Form1()
        {
            InitializeComponent();
            
        }
        
        

        private void confirmButton_Click(object sender, EventArgs e)
        {
            output.Show();
            if (string.IsNullOrEmpty(input.Text))
            {
                string outp = "";
                outp += "k = " + topKBox.Value + ",";
                if (!string.IsNullOrEmpty(accelBox.Text))
                {
                    outp += ", acceleration = " + accelBox.Text;
                }
                if (!string.IsNullOrEmpty(cylinderBox.Text))
                {
                    outp += ", cylinders = " + cylinderBox.Text;
                }
                if (!string.IsNullOrEmpty(displaceBox.Text))
                {
                    outp += ", displacement = " + displaceBox.Text;
                }
                if (!string.IsNullOrEmpty(horseBox.Text))
                {
                    outp += ", horsepower = " + horseBox.Text;
                }
                if (!string.IsNullOrEmpty(mgpBox.Text))
                {
                    outp += ", mgp = " + mgpBox.Text;
                }
                if (!string.IsNullOrEmpty(modelYearBox.Text))
                {
                    outp += ", model_year = " + modelYearBox.Text;
                }
                if (!string.IsNullOrEmpty(weightBox.Text))
                {
                    outp += ", weight = " + weightBox.Text;
                }
                if (!string.IsNullOrEmpty(brandBox.Text))
                {
                    outp += ", brand = '" + brandBox.Text + "'";
                }
                if (!string.IsNullOrEmpty(modelBox.Text))
                {
                    outp += ", model = '" + modelBox.Text + "'";
                }
                if (!string.IsNullOrEmpty(originBox.Text))
                {
                    outp += ", origin = '" + originBox.Text + "'";
                }
                if (!string.IsNullOrEmpty(typeBox.Text))
                {
                    outp += ", type = '" + typeBox.Text + "'";
                }
                outp += ";";
                output.Text = processor.Process(outp);
            }
            else
            {
                output.Text = processor.Process(input.Text);
            }
        }
    }
}
