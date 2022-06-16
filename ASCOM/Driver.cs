//tabs=4
// --------------------------------------------------------------------------------
// 
//
// ASCOM CoverCalibrtaor driver for ArduinoFlatbox
//
// Description:	Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam 
//				nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam 
//				erat, sed diam voluptua. At vero eos et accusam et justo duo 
//				dolores et ea rebum. Stet clita kasd gubergren, no sea takimata 
//				sanctus est Lorem ipsum dolor sit amet.
//
// Implements:	ASCOM CoverCalibration interface version: 1
// Author:		(SMG) Steven Gill <smgill75@gmail.com>
//
// Edit Log:
//
// Date			Who	Vers	Description
// -----------	---	-----	-------------------------------------------------------
// 2022-03-05	SMG	0.1.0	Initial edit, created from ASCOM driver template
// --------------------------------------------------------------------------------
//


// This is used to define code in the template that is specific to one class implementation
// unused code can be deleted and this definition removed.
//
#define ArduinoFlatbox

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;

using ASCOM;
using ASCOM.Utilities;
using ASCOM.DeviceInterface;
using System.Globalization;
using System.Collections;

using System.Threading;
using System.Management;
using System.Net.Sockets;
using System.Net;

namespace ASCOM.ArduinoFlatbox
{
    //
    // Your driver's DeviceID is ASCOM.TEMPLATEDEVICENAME.TEMPLATEDEVICECLASS
    //
    // The Guid attribute sets the CLSID for ASCOM.TEMPLATEDEVICENAME.TEMPLATEDEVICECLASS
    // The ClassInterface/None attribute prevents an empty interface called
    // _TEMPLATEDEVICENAME from being created and used as the [default] interface
    //
    // TODO Replace the not implemented exceptions with code to implement the function or
    // throw the appropriate ASCOM exception.
    //

    /// <summary>
    /// ASCOM TEMPLATEDEVICECLASS Driver for TEMPLATEDEVICENAME.
    /// </summary>
    [Guid("3A02C211-FA08-4747-B0BD-4B00EB159297")]
    [ProgId("ASCOM.ArduinoFlatbox")]
    [ClassInterface(ClassInterfaceType.None)]
    public class ArduinoFlatbox: ICoverCalibratorV1
    {
       
        internal static string driverID = "ASCOM.ArduinoFlatbox";
        private static string driverDescription = "AdruinoFlatbox";

        internal static string comPortProfileName = "COM Port"; // Constants used for Profile persistence
        internal static string comPortDefault = "COM1";
        internal static string comPort; // Variables to hold the current device configuration
        
        internal static string connectionTypeName = "Connection Type";
        internal static string connectionTypeDefault = "serial";
        internal static string connectionType;
        internal static string iphostName = "host";
        internal static string iphostDefault = "127.0.0.1";
        internal static string iphost;
        internal static string portName = "port";
        internal static int portDefault = 2390;
        internal static int port;
        internal static int transmit_retries = 3;

        internal static string traceStateProfileName = "Trace Level";
        internal static string traceStateDefault = "false";
        internal static ASCOM.Utilities.Serial serialport = new ASCOM.Utilities.Serial();
  

        internal static int brightnesslevel = 0;
        internal static int maxbrightness = 255;
        internal static bool calibratoron = false;


        /// <summary>
        /// Private variable to hold the connected state
        /// </summary>
        private bool connectedState;
       
        private Util utilities;
        //private AstroUtils astroUtilities;

        
        internal TraceLogger tl;

        
        public ArduinoFlatbox()
        {
            tl = new TraceLogger("", "ArduinoFlatbox");
            connectedState = false; 
            ReadProfile(); 


            tl.LogMessage("ArduinoFlatbox", "Starting initialisation");

            utilities = new Util(); //Initialise util object
            //astroUtilities = new AstroUtils(); // Initialise astro-utilities object

            tl.LogMessage("AdruinoFlatbox", "Completed initialisation");
        }

        private bool connect_network_udp()
        {

            string message = send_command_network_udp("PING:#");

                
            if (message == "PONG:#")
            {
                LogMessage("State", "Received state {0}", message);
                connectedState = true;
                LogMessage("Connected Set", "Connecting to port {0}", comPort);
                return true;
            }
            else
            {
                connectedState = false;
                return false;
            }
            
        }
      

        private bool connect_serial()
        {
            try
            {
                serialport.PortName = ArduinoFlatbox.comPort;
                serialport.Speed = SerialSpeed.ps9600;
                serialport.StopBits = SerialStopBits.One;
                serialport.Parity = SerialParity.None;
                serialport.DataBits = 8;
                serialport.Handshake = SerialHandshake.None;

                serialport.DTREnable = true;
                serialport.ReceiveTimeout = 5;
                serialport.Connected = true;
            }
            catch (Exception ex)
            {
                string error_message = "Error opening port: " + ex.Message;
                throw new ASCOM.DriverException(error_message);
            }

            try
            {
                LogMessage("Connected set", "Sending Ping");

                Thread.Sleep(1000);


                serialport.Transmit("PING:#");

                string message = "empty";
                message = serialport.ReceiveTerminated("#");
            
                if (message == "PONG:#")
                {
                    LogMessage("State", "Received state {0}", message);
                    connectedState = true;
                    LogMessage("Connected Set", "Connecting to port {0}", comPort);
                    return true;
                }
                else
                {
                    connectedState = false;
                    return false;
                }
            }
            catch (Exception ex)
            {
                string error_message = "Error sending / receiving: " + ex.Message;
                System.Windows.Forms.MessageBox.Show(error_message);
                return false;
            }
        }

        private string send_command_serial(string command)
        {
            serialport.Transmit(command);
            return serialport.ReceiveTerminated("#");
        }

        private string send_command_network_udp(string command)
        {
            int retry_counter = transmit_retries;
            string message = "";
            UdpClient udpClient = new UdpClient();
            while (message == "" && retry_counter > 0)
            {

                try
                {
                    udpClient.Connect(iphost, port);

                    // Sends a message to the host to which you have connected.
                    Byte[] sendBytes = ASCIIEncoding.ASCII.GetBytes(command);

                    udpClient.Send(sendBytes, sendBytes.Length);

                    //IPEndPoint object will allow us to read datagrams sent from any source.
                    IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);

                    // Blocks until a message returns on this socket from a remote host.
                    udpClient.Client.ReceiveTimeout = 5000;
                    Byte[] receiveBytes = udpClient.Receive(ref RemoteIpEndPoint);
                    message = Encoding.ASCII.GetString(receiveBytes);                    

                }
                
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    //System.Windows.Forms.MessageBox.Show(e.ToString());
                    retry_counter--;
                }
                return message;
            }
            if (retry_counter <= 0)
            {
                string error_message = "Error connecting to flatbox, please ensure the device is on and connected to the network.";
                throw new ASCOM.DriverException(error_message);
            }
            return "";
        }


        private int getBrightness()
        {
            string message = "empty";
            
            if (connectionType == "serial")
            {
                message = send_command_serial("BRIGHT:#");
            }
            if (connectionType == "network")
            {
                message = send_command_network_udp("BRIGHT:#");
            }

            
            
            string[] subs = message.Split(':');

            int templevel = Int32.Parse(subs[1]);

            if (templevel >= 0 && templevel <= 255) {
                return templevel;
            }
            else
            {
                String error_message = "Brightness not between 0 and 255. Attemping to set brightness to 0.";
                setBrightness(0);
                throw new ASCOM.DriverException(error_message);
            }

        }


        private int setBrightness(int level)
        {
            if (level >= 0 && level <= 255)
            {
                string message = "empty";
                string xmit = "SET:" + level.ToString() + ":#";

                if (connectionType == "serial")
                {
                    message = send_command_serial(xmit);
                }
                if (connectionType == "network")
                {
                    message = send_command_network_udp(xmit);
                }

                string[] subs = message.Split(':');

                int templevel = Int32.Parse(subs[1]);

                if (templevel == level)
                {
                    return Int32.Parse(subs[1]);

                }
                else
                {
                    return 0;
                }
            }
            else
            {
                send_command_network_udp("0");
                String error_message = "Bug in imaging program: Invalid brightness requested (" + level.ToString() + "), must set brightness between 0 and" + maxbrightness.ToString() + ". Brightness being set to 0." ;
                
                throw new ASCOM.DriverException(error_message);
                //System.Windows.Forms.MessageBox.Show(error_message);
            }

        }
        //
        // PUBLIC COM INTERFACE ITEMPLATEDEVICEINTERFACE IMPLEMENTATION
        //

        #region Common properties and methods.

        /// <summary>
        /// Displays the Setup Dialog form.
        /// If the user clicks the OK button to dismiss the form, then
        /// the new settings are saved, otherwise the old values are reloaded.
        /// THIS IS THE ONLY PLACE WHERE SHOWING USER INTERFACE IS ALLOWED!
        /// </summary>
        public void SetupDialog()
        {
            // consider only showing the setup dialog if not connected
            // or call a different dialog if connected
            if (IsConnected)
                System.Windows.Forms.MessageBox.Show("Already connected, just press OK");

            using (SetupDialogForm F = new SetupDialogForm(tl))
            {
                var result = F.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    WriteProfile(); // Persist device configuration values to the ASCOM Profile store
                }
            }
        }

        public ArrayList SupportedActions
        {
            get
            {
                tl.LogMessage("SupportedActions Get", "Returning empty arraylist");
                return new ArrayList();
            }
        }

        public string Action(string actionName, string actionParameters)
        {
            LogMessage("", "Action {0}, parameters {1} not implemented", actionName, actionParameters);
            throw new ASCOM.ActionNotImplementedException("Action " + actionName + " is not implemented by this driver");
        }

        public void CommandBlind(string command, bool raw)
        {
            CheckConnected("CommandBlind");
            throw new ASCOM.MethodNotImplementedException("CommandBlind");
        }

        public bool CommandBool(string command, bool raw)
        {
            CheckConnected("CommandBool");
            throw new ASCOM.MethodNotImplementedException("CommandBool");
        }

        public string CommandString(string command, bool raw)
        {
            CheckConnected("CommandString");
            throw new ASCOM.MethodNotImplementedException("CommandString");
        }

        public void Dispose()
        {
            // Clean up the trace logger and util objects
            tl.Enabled = false;
            tl.Dispose();
            tl = null;
            utilities.Dispose();
            utilities = null;
            //astroUtilities.Dispose();
            //astroUtilities = null;
        }

        public bool Connected
        {
            get
            {
                LogMessage("Connected", "Get {0}", IsConnected);
                return IsConnected;
            }
            set
            {
                

                    tl.LogMessage("Connected", "Set {0}", value);
                if (value == IsConnected)
                    return;

                if (value)
                {
                    if (connectionType == "serial")
                    {
                        connectedState = connect_serial();
                    }

                    if (connectionType == "network")
                    {
                        //connectedState = connect_network();
                        connectedState = connect_network_udp();

                    }

                }
                else
                {
                    
                    connectedState = false;
                    LogMessage("Connected Set", "Disconnecting from port {0}", comPort);
                    //close serial port
                    serialport.Connected = false;
                }
            }
        }

        public string Description
        {
            get
            {
                tl.LogMessage("Description Get", driverDescription);
                return driverDescription;
            }
        }

        public string DriverInfo
        {
            get
            {
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                // TODO customise this driver description
                string driverInfo = "Flatbox implemented on Arduino and it's companion firmware. Version: " + String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
                tl.LogMessage("DriverInfo Get", driverInfo);
                return driverInfo;
            }
        }

        public string DriverVersion
        {
            get
            {
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                string driverVersion = String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
                tl.LogMessage("DriverVersion Get", driverVersion);
                return driverVersion;
            }
        }

        public short InterfaceVersion
        {
            // set by the driver wizard
            get
            {
                LogMessage("InterfaceVersion Get", "TEMPLATEINTERFACEVERSION");
                return Convert.ToInt16("TEMPLATEINTERFACEVERSION");
            }
        }

        public string Name
        {
            get
            {
                string name = "Arduino Flatbox";
                tl.LogMessage("Name Get", name);
                return name;
            }
        }

        #endregion

        //INTERFACECODEINSERTIONPOINT
        #region Private properties and methods

        #region ICoverCalibrator Implementation

        /// <summary>
        /// Returns the state of the device cover, if present, otherwise returns "NotPresent"
        /// </summary>
        public CoverStatus CoverState
        {
            get
            {
                return CoverStatus.NotPresent;
            }
        }

        /// <summary>
        /// Initiates cover opening if a cover is present
        /// </summary>
        public void OpenCover()
        {
            tl.LogMessage("OpenCover", "Not implemented");
            throw new ASCOM.MethodNotImplementedException("OpenCover");
        }

        /// <summary>
        /// Initiates cover closing if a cover is present
        /// </summary>
        public void CloseCover()
        {
            tl.LogMessage("CloseCover", "Not implemented");
            throw new ASCOM.MethodNotImplementedException("CloseCover");
        }

        /// <summary>
        /// Stops any cover movement that may be in progress if a cover is present and cover movement can be interrupted.
        /// </summary>
        public void HaltCover()
        {
            tl.LogMessage("HaltCover", "Not implemented");
            throw new ASCOM.MethodNotImplementedException("HaltCover");
        }

        /// <summary>
        /// Returns the state of the calibration device, if present, otherwise returns "NotPresent"
        /// </summary>
        public CalibratorStatus CalibratorState
        {
            get
            {
                return CalibratorStatus.Ready;
            }
        }

        /// <summary>
        /// Returns the current calibrator brightness in the range 0 (completely off) to <see cref="MaxBrightness"/> (fully on)
        /// </summary>
        public int Brightness
        {
            get
            {
                return getBrightness();
            }
        }

        /// <summary>
        /// The Brightness value that makes the calibrator deliver its maximum illumination.
        /// </summary>
        public int MaxBrightness
        {
            get
            {
                return maxbrightness;
            }
        }

        /// <summary>
        /// Turns the calibrator on at the specified brightness if the device has calibration capability
        /// </summary>
        /// <param name="Brightness"></param>
        public void CalibratorOn(int Brightness)
        {
            //brightnesslevel = Brightness;
            brightnesslevel = setBrightness(Brightness);
            calibratoron = true;
        }

        /// <summary>
        /// Turns the calibrator off if the device has calibration capability
        /// </summary>
        public void CalibratorOff()
        {
            brightnesslevel = setBrightness(0);
            calibratoron = false;
        }

        #endregion


        #region ASCOM Registration

        /// <param name="bRegister">If <c>true</c>, registers the driver, otherwise unregisters it.</param>
        private static void RegUnregASCOM(bool bRegister)
        {
            using (var P = new ASCOM.Utilities.Profile())
            {
                P.DeviceType = "CoverCalibrator";
                if (bRegister)
                {
                    P.Register(driverID, driverDescription);
                }
                else
                {
                    P.Unregister(driverID);
                }
            }
        }

  
        [ComRegisterFunction]
        public static void RegisterASCOM(Type t)
        {
            RegUnregASCOM(true);
        }

        [ComUnregisterFunction]
        public static void UnregisterASCOM(Type t)
        {
            RegUnregASCOM(false);
        }

        #endregion

        /// <summary>
        /// Returns true if there is a valid connection to the driver hardware
        /// </summary>
        private bool IsConnected
        {
            get
            {
                // TODO check that the driver hardware connection exists and is connected to the hardware
                return connectedState;
            }
        }

        /// <summary>
        /// Use this function to throw an exception if we aren't connected to the hardware
        /// </summary>
        /// <param name="message"></param>
        private void CheckConnected(string message)
        {
            if (!IsConnected)
            {
                throw new ASCOM.NotConnectedException(message);
            }
        }

        /// <summary>
        /// Read the device configuration from the ASCOM Profile store
        /// </summary>
        internal void ReadProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "CoverCalibrator";
                tl.Enabled = Convert.ToBoolean(driverProfile.GetValue(driverID, traceStateProfileName, string.Empty, traceStateDefault));
                comPort = driverProfile.GetValue(driverID, comPortProfileName, string.Empty, comPortDefault);
                connectionType = driverProfile.GetValue(driverID, connectionTypeName, string.Empty, connectionTypeDefault);
                iphost = driverProfile.GetValue(driverID, iphostName, string.Empty, iphostDefault);
                port = Int32.Parse(driverProfile.GetValue(driverID, portName, string.Empty, portDefault.ToString()));
                 
            }
        }

        /// <summary>
        /// Write the device configuration to the  ASCOM  Profile store
        /// </summary>
        internal void WriteProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "CoverCalibrator";
                driverProfile.WriteValue(driverID, traceStateProfileName, tl.Enabled.ToString());
                driverProfile.WriteValue(driverID, comPortProfileName, comPort.ToString());
                driverProfile.WriteValue(driverID, connectionTypeName, connectionType.ToString());
                driverProfile.WriteValue(driverID, iphostName, iphost.ToString());
                driverProfile.WriteValue(driverID, portName, port.ToString());
            }
        }

        /// <summary>
        /// Log helper function that takes formatted strings and arguments
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        internal void LogMessage(string identifier, string message, params object[] args)
        {
            var msg = string.Format(message, args);
            tl.LogMessage(identifier, msg);
        }
        #endregion
    }
}
