using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using ASCOM.Utilities;
using ASCOM.ArduinoFlatbox;
using System.Management;

namespace ASCOM.ArduinoFlatbox
{
    [ComVisible(false)]					// Form not registered for COM!
    public partial class SetupDialogForm : Form
    {
        TraceLogger tl; // Holder for a reference to the driver's trace logger

        public SetupDialogForm(TraceLogger tlDriver)
        {
            InitializeComponent();
            
            // Save the provided trace logger for use within the setup dialogue
            tl = tlDriver;

            if (ArduinoFlatbox.connectionType == "network")
            {
                rdwifi.Checked = true;
            }
            else
            {
                rdSerial.Checked = true;
            }
            txtport.Text = ArduinoFlatbox.port.ToString();
            txtIP.Text = ArduinoFlatbox.iphost.ToString();
            // Initialise current values of user settings from the ASCOM Profile
            InitUI();
        }

        private void cmdOK_Click(object sender, EventArgs e) // OK button event handler
        {
            // Place any validation constraint checks here
            // Update the state variables with results from the dialogue
            if (rdwifi.Checked == true)
            {
                ArduinoFlatbox.connectionType = "network";
                ArduinoFlatbox.iphost = txtIP.Text;
                ArduinoFlatbox.port = Int32.Parse(txtport.Text);
            }
            else if (rdSerial.Checked == true && comboBoxComPort.Items.Count > 0) 
            {
                ArduinoFlatbox.connectionType = "serial";
                ArduinoFlatbox.comPort = (string)comboBoxComPort.SelectedItem;
            }

        }

        private void cmdCancel_Click(object sender, EventArgs e) // Cancel button event handler
        {
            Close();
        }

        private void BrowseToAscom(object sender, EventArgs e) // Click on ASCOM logo event handler
        {
            try
            {
                System.Diagnostics.Process.Start("https://ascom-standards.org/");
            }
            catch (System.ComponentModel.Win32Exception noBrowser)
            {
                if (noBrowser.ErrorCode == -2147467259)
                    MessageBox.Show(noBrowser.Message);
            }
            catch (System.Exception other)
            {
                MessageBox.Show(other.Message);
            }
        }

        private void InitUI()
        {
            // set the list of com ports to those that are currently available
            string[] comports = System.IO.Ports.SerialPort.GetPortNames(); // use System.IO because it's static
            comboBoxComPort.Items.Clear();
            if (comports.Length > 0)
            {
                comboBoxComPort.Items.AddRange(comports);
            }
            // select the current port if possible

            if (comboBoxComPort.Items.Contains(ArduinoFlatbox.comPort))
            {
                comboBoxComPort.SelectedItem = ArduinoFlatbox.comPort;
            }
            else if (comports.Length > 0 )
            {
                comboBoxComPort.SelectedItem = comboBoxComPort.Items[0];
            }
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            comboBoxComPort.Enabled = true;
            txtIP.Enabled = false;
            txtport.Enabled = false;
        }

        private void rdwifi_CheckedChanged(object sender, EventArgs e)
        {
            txtIP.Enabled = true;
            txtport.Enabled = true;
            comboBoxComPort.Enabled=false;
        }
    }
}