using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Input;
using Avalonia.Threading;
using System.Timers;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace TENKOH2_BEACON_DECODER_Multi_Platform
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            try
            {
                var configFilePath = "UserSettings.json";

                if (File.Exists(configFilePath))
                {
                    var json = File.ReadAllText(configFilePath);
                    var config = JsonConvert.DeserializeObject<AppConfig>(json);
                    Console.WriteLine(json);

                    _targetString = config.targetString;
                    _ReferencedFilePath = config.ReferencedFilePath;
                    _extractedDataLength = config.extractedDataLength;
                }
                else
                {
                    LoadDefaultSettings();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex}");
            }
        }

        private void LoadDefaultSettings()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "TENKOH2_BEACON_DECODER_Multi_Platform.AppConfigure.json";

            using Stream stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                Console.WriteLine($"Resource {resourceName} not found.");
                return;
            }
            using StreamReader reader = new StreamReader(stream);
            var json = reader.ReadToEnd();

            var config = JsonConvert.DeserializeObject<AppConfig>(json);
            Console.WriteLine(json);

            _targetString = config.targetString;
            _ReferencedFilePath = config.ReferencedFilePath;
            _extractedDataLength = config.extractedDataLength;
            // Handle saveLogData if required
        }

        private void NUDecodeButton_Click(object sender, RoutedEventArgs e)
        {
            ResetUI();

            if (InputTextBox?.Text == null)
            {
                Console.WriteLine("InputTextBox is null!");
                return;
            }

            string input = InputTextBox.Text;

            if (!Regex.IsMatch(input, "^[a-fA-F0-9]+$"))
            {
                Console.WriteLine("Invalid hexadecimal value.");
                return;
            }

            /// TimeStamp
            var currentTime = DateTime.Now;
            string timestamp = currentTime.ToString("yyyy/MM/dd HH:mm:ss");
            TimestampTextBox.Text = timestamp;

            OutputTextBox.Text = $"{timestamp}: \n{input}\n" + OutputTextBox.Text;

            /// #1 Read GPIO Expander ID
            (object, bool) ProcessGpioExpanderId(string hexValue)
            {
                object decValue;
                bool isGpioExpanderIdFalse = false;

                if (hexValue == "28")
                {
                    decValue = "True";
                }
                else
                {
                    decValue = "False";
                    isGpioExpanderIdFalse = true;
                }

                return (decValue, isGpioExpanderIdFalse);
            }

            /// #2 Status
            Dictionary<string, string> ProcessStatus(string hexValue, bool isGpioExpanderIdFalse)
            {
                int decValue = Convert.ToInt32(hexValue, 16);
                string binary = Convert.ToString(decValue, 2).PadRight(16, '0');
                List<int> binaryList = new List<int>();
                foreach (char bit in binary)
                {
                    binaryList.Add(int.Parse(bit.ToString()));
                }

                var bitResultList = new List<string>();
                if (isGpioExpanderIdFalse)
                {
                    for (int i = 0; i < 16; i++)
                    {
                        bitResultList.Add("False");
                    }
                }
                else
                {
                    foreach (int v in binaryList)
                    {
                        string bitResult = v == 0 ? "ON" : "OFF";
                        bitResultList.Add(bitResult);
                    }
                }

                var statusResults = new Dictionary<string, string>
                {
                    {"bitResult5VCAM", bitResultList[0]},
                    {"bitResult5VPL", bitResultList[1]},
                    {"bitResult5VNUM", bitResultList[2]},
                    {"bitResult3V3JAMSAT", bitResultList[3]},
                    {"bitResult3V3ADCS", bitResultList[4]},
                    {"bitResult5VOBC", bitResultList[5]},
                    {"bitResult5VADCS", bitResultList[6]},
                    {"bitResult5VCOM", bitResultList[7]},
                    {"bitResult12VADCS", bitResultList[10]},
                    {"bitResult12VLIU", bitResultList[11]}
                };

                return statusResults;
            }

            /// #3 Batterycurrent
            (float, string) ProcessBatteryCurrent(string hexValue)
            {
                const float VOLTAGE_CONVERSION_FACTOR = 5.0f / 4096f;
                const float CURRENT_OFFSET = 2.5f;
                const float CURRENT_CONVERSION_FACTOR = 200f * 0.001f;

                float dec3Volt = Convert.ToInt32(hexValue, 16) * VOLTAGE_CONVERSION_FACTOR;
                float dec3Current = (dec3Volt - CURRENT_OFFSET) / CURRENT_CONVERSION_FACTOR;
                float roundeddec3Current = (float)Math.Round((double)dec3Current , 2);

                string batteryStatus;
                if (dec3Current < 0)
                {
                    batteryStatus = "Charging";
                }
                else if (dec3Current > 0)
                {
                    batteryStatus = "Discharging";
                }
                else
                {
                    batteryStatus = "False";
                }

                return (roundeddec3Current, batteryStatus);
            }

            /// #4 BatteryVoltage
            float ProcessBatteryVoltage(string hexValue)
            {
                const float VOLTAGE_CONVERSION_FACTOR = 5.0f / 4096f;
                float dec4Volt = Convert.ToInt32(hexValue, 16) * VOLTAGE_CONVERSION_FACTOR;
                return (float)Math.Round((double)dec4Volt , 2);
            }

            /// #5 BatteryTempreture
            float ProcessBatteryTemperature(string hexValue)
            {
                const float VOLTAGE_CONVERSION_FACTOR = 5.0f / 4096f;
                const float TEMPRETURE_CONVERSION_FACTOR = 147.06f;
                const float KELVIN_TO_CELSIUS_OFFSET = 273.15f;

                float dec5Volt = Convert.ToInt32(hexValue, 16) * VOLTAGE_CONVERSION_FACTOR;
                float dec5Temp = dec5Volt * TEMPRETURE_CONVERSION_FACTOR - KELVIN_TO_CELSIUS_OFFSET;
                return (float)Math.Round((double)dec5Temp , 2);
            }

            /// #6 EPS Controller Status
            string ProcessEPSControllerStatus(string hexValue)
            {
                string mode;

                switch (hexValue)
                {
                    case "2":
                        mode = "NormalMode";
                        break;
                    case "3":
                        mode = "MissionMode";
                        break;
                    case "4":
                        mode = "EmergencyMode";
                        break;
                    default:
                        mode = "Invalid";
                        break;
                }

                return mode;
            }

            /// #7 Subsystem Interface Status
            Dictionary<string, string> ProcessSubsystemInterfaceStatus(string hexValue)
            {
                int decValue = Convert.ToInt32(hexValue, 16);
                string binary = Convert.ToString(decValue, 2).PadRight(16, '0');
                List<int> binaryList = new List<int>();
                foreach (char bit in binary)
                {
                    binaryList.Add(int.Parse(bit.ToString()));
                }

                var bitResultList = new List<string>();
                foreach (int v in binaryList)
                {
                    string bitResult = v == 0 ? "FAULT" : "STABLE";
                    bitResultList.Add(bitResult);
                }

                var statusResults = new Dictionary<string, string>
                {
                    {"I2C_RTC", bitResultList[0]},
                    {"I2C_MEM", bitResultList[1]},
                    {"I2C_EPSC", bitResultList[2]},
                    {"I2C_COM", bitResultList[3]},
                    {"I2C_ANT", bitResultList[4]},
                    {"I2C_IFPV", bitResultList[5]},
                    {"I2C_ADCS", bitResultList[6]},
                    {"I2C_CAM", bitResultList[7]},
                    {"I2C_MATLIU", bitResultList[8]},
                    {"I2C_NU", bitResultList[9]},
                    {"UART_JAMSAT", bitResultList[10]}
                };

                bool allSystemsOk = true;
                foreach (var status in statusResults)
                {
                    if (status.Value == "FAULT")
                    {
                        allSystemsOk = false;
                        break;
                    }
                }

                if (allSystemsOk)
                {
                    statusResults.Add("System Check", "All Systems Operational");
                }
                else
                {
                    statusResults.Add("System Check", "Subsystem Interface Error Detected");
                }

                return statusResults;
            }

            /// #8 WDU Tempreture
            /// #9 MCU Tempreture
            float ProcessTemperature(string hexValue)
            {
                const float VOLTAGE_CONVERSION_FACTOR = 4.97f / 1024f;
                const float TEMPRETURE_CONVERSION_FACTOR = 147.06f;
                const float KELVIN_TO_CELSIUS_OFFSET = 273.15f;

                float decVolt = Convert.ToInt32(hexValue, 16) * VOLTAGE_CONVERSION_FACTOR;
                float decTemp = decVolt * TEMPRETURE_CONVERSION_FACTOR - KELVIN_TO_CELSIUS_OFFSET;
                return (float)Math.Round((double)decTemp , 2);
            }

            /// #10 Opretion Mode
            string ProcessOperationMode()
            {
                return "TK";
            }

            /// #11 Mode Timer
            (int, object) ProcessModeTimer(string hexValue)
            {
                const float VOLTAGE_CONVERSION_FACTOR = 60f;
                float dec6 = Convert.ToInt32(hexValue, 16) / VOLTAGE_CONVERSION_FACTOR;
                int dec6Time = (int)Math.Round(dec6);
                object dec6OpMode;
                if (dec6Time <= 1440)
                {
                    dec6OpMode = "TRP";
                }
                else
                {
                    dec6OpMode = "58G";
                }
                return (dec6Time, dec6OpMode);
            }

            ///#10 JAMSAT Status
            Dictionary<string, string> ProcessJAMSATStatus(string hexValue)
            {
                int dec7 = Convert.ToInt32(hexValue, 16);
                string binaryJam = Convert.ToString(dec7, 2).PadLeft(8, '0');
                List<int> binaryListJam = new List<int>();
                foreach (char bitJam in binaryJam)
                {
                    binaryListJam.Add(int.Parse(bitJam.ToString()));
                }

                var bitResultListJam = new List<string>();
                foreach (int v in binaryListJam)
                {
                    string bitResultJam = v == 0 ? "ACTIVE" : "INACTIVE";
                    bitResultListJam.Add(bitResultJam);
                }

                var statusResults = new Dictionary<string, string>
                {
                    {"bitResultVC1LOCK", bitResultListJam[0]},
                    {"bitResultVC2LOCK", bitResultListJam[1]},
                    {"bitResult7021LOCK", bitResultListJam[2]},
                    {"bitResult58GLOCK", bitResultListJam[3]},
                    {"bitResultVC2ON", bitResultListJam[4]},
                    {"bitResultAMPEN", bitResultListJam[5]},
                    {"bitResult58GON", bitResultListJam[6]},
                    {"bitResultUHFCWON", bitResultListJam[7]}
                };

                return statusResults;
            }

            /// #11 ADC Volatge
            float ProcessADCVoltage(string hexValue)
            {
                const float VOLTAGE_OFFSET = 515f;
                float dec8ADCVolt = (2 * Convert.ToInt32(hexValue, 16) - VOLTAGE_OFFSET) / 1000f;
                return (float)Math.Round((double)dec8ADCVolt , 2);
            }

            /// #12 RF Input VHF-Band
            float ProcessRFInputVHF(string hexValue)
            {
                const float VOLTAGE_CONVERSION_FACTOR = 0.0772f;
                const float RFINPUT_OFFSET = 153.23f;
                float dec9RF = VOLTAGE_CONVERSION_FACTOR * Convert.ToInt32(hexValue, 16) - RFINPUT_OFFSET;
                return (float)Math.Round((double)dec9RF , 2);
            }

            /// #13 RF Output UHF-Band
            object ProcessRFOutputUHF(string hexValue)
            {
                const float VOLTAGE_CONVERSION_FACTOR5 = 0.0154f;
                const float RFOUTPUT_OFFSET1 = 16.841f;

                if (hexValue == "000")
                {
                    return "---";
                }
                else
                {
                    float decRF = VOLTAGE_CONVERSION_FACTOR5 * Convert.ToInt32(hexValue, 16) + RFOUTPUT_OFFSET1;
                    return (float)Math.Round((double)decRF , 2);
                }
            }

            /// #14 RF Output 58G
            object ProcessRFOutput58G(string hexValue)
            {
                const float VOLTAGE_CONVERSION_FACTOR6 = 0.009f;
                const float RFOUTPUT_OFFSET2 = 4.499f + 5.5f;

                if (hexValue == "000")
                {
                    return "---";
                }
                else
                {
                    float decRF = VOLTAGE_CONVERSION_FACTOR6 * Convert.ToInt32(hexValue, 16) + RFOUTPUT_OFFSET2;
                    return (float)Math.Round((double)decRF , 2);
                }
            }

            /// Nominal NUM Mode
            if (input.Length == 25)
            {
                // Switch to NUM tab when input is 21 characters
                if (MainTabControl != null)
                {
                    MainTabControl.SelectedIndex = 0;  // Index 0 corresponds to NUM tab
                }

                string hex1 = input.Substring(0, 2);    /// Read GPIO Expander ID
                string hex2 = input.Substring(2, 3);    /// Status
                string hex3 = input.Substring(5, 3);    /// Battery Current
                string hex4 = input.Substring(8, 3);    /// Battery Voltage
                string hex5 = input.Substring(11, 3);   /// Battery tempreture
                string hex6 = input.Substring(14, 1);   /// EPS Controller Status
                string hex7 = input.Substring(15, 3);   /// Subsystem INterface Status
                string hex8 = input.Substring(18, 3);   /// WDU Tempreture
                string hex9 = input.Substring(21, 3);   /// MCU Tempreture
                string hex10 = input.Substring(24, 1);   /// Opreation mode

                /// #1 Read GPIO Expander ID
                (object dec1, bool isGpioExpanderIdFalse) = ProcessGpioExpanderId(hex1);


                /// #2 Status
                var statusResults = ProcessStatus(hex2, isGpioExpanderIdFalse);
                string bitResult5VCAM = statusResults["bitResult5VCAM"];
                string bitResult5VPL = statusResults["bitResult5VPL"];
                string bitResult5VNUM = statusResults["bitResult5VNUM"];
                string bitResult3V3JAMSAT = statusResults["bitResult3V3JAMSAT"];
                string bitResult3V3ADCS = statusResults["bitResult3V3ADCS"];
                string bitResult5VOBC = statusResults["bitResult5VOBC"];
                string bitResult5VADCS = statusResults["bitResult5VADCS"];
                string bitResult5VCOM = statusResults["bitResult5VCOM"];
                string bitResult12VADCS = statusResults["bitResult12VADCS"];
                string bitResult12VLIU = statusResults["bitResult12VLIU"];

                /// bit0 | 5V_CAM
                /// bit1 | 5N_PL
                /// bit2 | 5V_NUM
                /// bit3 | 3V3_JAMSAT
                /// bit4 | 3V3_ADCS
                /// bit5 | 5V_OBC
                /// bit6 | 5V_ADCS
                /// bit7 | 5V_COM
                /// bit8~9 | No Data
                /// bit10 | 12V_ADCS
                /// bit11 | 12V_LIU

                /// #3 Batterycurrent
                var (roundeddec3Current, batteryStatus) = ProcessBatteryCurrent(hex3);

                /// #4 BatteryVoltage
                float roundeddec4Volt = ProcessBatteryVoltage(hex4);

                /// #5 BatteryTempreture
                float roundeddec5Temp = ProcessBatteryTemperature(hex5);

                /// #6 EPS Controller Status
                string dec6 = ProcessEPSControllerStatus(hex6);

                /// #7 Subsystem Interface Status
                var subsystemInterfaceStatusResults = ProcessSubsystemInterfaceStatus(hex7);
                string bitResultI2CRTC = subsystemInterfaceStatusResults["I2C_RTC"];
                string bitResultI2CMEM = subsystemInterfaceStatusResults["I2C_MEM"];
                string bitResultI2CEPSC = subsystemInterfaceStatusResults["I2C_EPSC"];
                string bitResultI2CCOM = subsystemInterfaceStatusResults["I2C_COM"];
                string bitResultI2CANT = subsystemInterfaceStatusResults["I2C_ANT"];
                string bitResultI2CIFPV = subsystemInterfaceStatusResults["I2C_IFPV"];
                string bitResultI2CADCS = subsystemInterfaceStatusResults["I2C_ADCS"];
                string bitResultI2CCAM = subsystemInterfaceStatusResults["I2C_CAM"];
                string bitResultI2CMATLIU = subsystemInterfaceStatusResults["I2C_MATLIU"];
                string bitResultI2CNU = subsystemInterfaceStatusResults["I2C_NU"];
                string bitResultUARTJAMSAT = subsystemInterfaceStatusResults["UART_JAMSAT"];
                string bitResultSystemcheck = subsystemInterfaceStatusResults["System Check"];

                /// #8 WDU Tempreture
                float roundeddec8Temp = ProcessTemperature(hex8);

                /// #9 MCU Tempreture
                float roundeddec9Temp = ProcessTemperature(hex9);

                /// #8 Opretion Mode
                hex10 = ProcessOperationMode();

                /// Rabel
                txtGPIOExpander.Text = dec1.ToString();
                txtBatteryCurrent.Text = roundeddec3Current.ToString();
                txtBatteryStatus.Text = batteryStatus.ToString();
                txtBatteryVoltage.Text = roundeddec4Volt.ToString();
                txtBatteryTemperature.Text = roundeddec5Temp.ToString();
                txtWDUTemperature.Text = roundeddec8Temp.ToString();
                txtMCUTemperature.Text = roundeddec9Temp.ToString();
                txtEPSControllerStatus.Text = dec6.ToString();
                txtOperationMode.Text = hex10.ToString();

                txt5VCAM.Text = bitResult5VCAM.ToString();
                txt5VPL.Text = bitResult5VPL.ToString();
                txt5VNUM.Text = bitResult5VNUM.ToString();
                txt3V3JAMSAT.Text = bitResult3V3JAMSAT.ToString();
                txt3V3ADCS.Text = bitResult3V3ADCS.ToString();
                txt5VOBC.Text = bitResult5VOBC.ToString();
                txt5VADCS.Text = bitResult5VADCS.ToString();
                txt5VCOM.Text = bitResult5VCOM.ToString();
                txt12VADCS.Text = bitResult12VADCS.ToString();
                txt12VLIU.Text = bitResult12VLIU.ToString();

                UpdateStatusIndicator(txt5VCAM, statusIndicator5VCAM);
                UpdateStatusIndicator(txt5VPL, statusIndicator5VPL);
                UpdateStatusIndicator(txt5VNUM, statusIndicator5VNUM);
                UpdateStatusIndicator(txt3V3JAMSAT, statusIndicator3V3JAMSAT);
                UpdateStatusIndicator(txt3V3ADCS, statusIndicator3V3ADCS);
                UpdateStatusIndicator(txt5VOBC, statusIndicator5VOBC);
                UpdateStatusIndicator(txt5VADCS, statusIndicator5VADCS);
                UpdateStatusIndicator(txt5VCOM, statusIndicator5VCOM);
                UpdateStatusIndicator(txt12VADCS, statusIndicator12VADCS);
                UpdateStatusIndicator(txt12VLIU, statusIndicator12VLIU);

                txtSystemCheck.Text = bitResultSystemcheck.ToString();

                txtI2CRTC.Text = bitResultI2CRTC.ToString();
                txtI2CMEM.Text = bitResultI2CMEM.ToString();
                txtI2CEPSC.Text = bitResultI2CEPSC.ToString();
                txtI2CCOM.Text = bitResultI2CCOM.ToString();
                txtI2CANT.Text = bitResultI2CANT.ToString();
                txtI2CIFPV.Text = bitResultI2CIFPV.ToString();
                txtI2CADCS.Text = bitResultI2CADCS.ToString();
                txtI2CCAM.Text = bitResultI2CCAM.ToString();
                txtI2CMATLIU.Text = bitResultI2CMATLIU.ToString();
                txtI2CNU.Text = bitResultI2CNU.ToString();
                txtUARTJAMSAT.Text = bitResultUARTJAMSAT.ToString();

                UpdateStatusIndicator(txtI2CRTC, statusIndicatorI2CRTC);
                UpdateStatusIndicator(txtI2CMEM, statusIndicatorI2CMEM);
                UpdateStatusIndicator(txtI2CEPSC, statusIndicatorI2CEPSC);
                UpdateStatusIndicator(txtI2CCOM, statusIndicatorI2CCOM);
                UpdateStatusIndicator(txtI2CANT, statusIndicatorI2CANT);
                UpdateStatusIndicator(txtI2CIFPV, statusIndicatorI2CIFPV);
                UpdateStatusIndicator(txtI2CADCS, statusIndicatorI2CADCS);
                UpdateStatusIndicator(txtI2CCAM, statusIndicatorI2CCAM);
                UpdateStatusIndicator(txtI2CMATLIU, statusIndicatorI2CMATLIU);
                UpdateStatusIndicator(txtI2CNU, statusIndicatorI2CNU);
                UpdateStatusIndicator(txtUARTJAMSAT, statusIndicatorUARTJAMSAT);


                LogData logData = new LogData
                {
                    Callsign = "JS1YKI",
                    Input = input,
                    TimeStamp = timestamp,
                    GPIOExpander = dec1,
                    BatteryCurrent = roundeddec3Current,
                    BatteryStatus = batteryStatus,
                    BatteryVoltage = roundeddec4Volt,
                    BatteryTemperature = roundeddec5Temp,
                    WDUTemperature = roundeddec8Temp,
                    MCUTemperature = roundeddec9Temp,
                    EPSControllerStatus = dec6,
                    OperationMode = hex10,
                    StatusBits = new StatusBitsData
                    {
                        _5V_CAM = bitResult5VCAM,
                        _5V_PL = bitResult5VPL,
                        _5V_NUM = bitResult5VNUM,
                        _3V3_JAMSAT = bitResult3V3JAMSAT,
                        _3V3_ADCS = bitResult3V3ADCS,
                        _5V_OBC = bitResult5VOBC,
                        _5V_ADCS = bitResult5VADCS,
                        _5V_COM = bitResult5VCOM,
                        _12V_ADCS = bitResult12VADCS,
                        _12V_LIU = bitResult12VLIU
                    },
                    SubsystemInterfaceStatusBits = new SubsystemInterfaceStatusBitsData
                    {
                        SystemCheck = bitResultSystemcheck,
                        _I2C_RTC = bitResultI2CRTC,
                        _I2C_MEM = bitResultI2CMEM,
                        _I2C_EPSC = bitResultI2CEPSC,
                        _I2C_COM = bitResultI2CCOM,
                        _I2C_ANT = bitResultI2CANT,
                        _I2C_IFPV = bitResultI2CIFPV,
                        _I2C_ADCS = bitResultI2CADCS,
                        _I2C_CAM = bitResultI2CCAM,
                        _I2C__MATLIU = bitResultI2CMATLIU,
                        _I2C_NU = bitResultI2CNU,
                        _UART_JAMSAT = bitResultUARTJAMSAT
                    }
                };
                SaveLogData(logData);
            }

            /// JAMSAT Mission Mode
            else if (input.Length == 37)
            {
                // Switch to JAM tab when input is 33 characters
                if (MainTabControl != null)
                {
                    MainTabControl.SelectedIndex = 1;  // Index 1 corresponds to JAM tab
                }

                string hex1 = input.Substring(0, 2);     /// Read GPIO Expander ID
                string hex2 = input.Substring(2, 3);     /// Status
                string hex3 = input.Substring(5, 3);     /// Battery Current
                string hex4 = input.Substring(8, 3);     /// Battery Voltage
                string hex5 = input.Substring(11, 3);    /// Battery tempreture
                string hex6 = input.Substring(14, 1);    /// Battery tempreture
                string hex7 = input.Substring(15, 3);    /// Battery tempreture
                string hex8 = input.Substring(18, 4);    /// Mode Timer
                string hex9 = input.Substring(22, 2);    /// JAMSAT Status
                string hex10 = input.Substring(24, 3);    /// ADC Voltage
                string hex11 = input.Substring(27, 3);    /// RF Input VHF-Band
                string hex12 = input.Substring(30, 3);   /// RF Output UHF-Band
                string hex13 = input.Substring(33, 3);   /// RF Output 58G-Band
                string hex14 = input.Substring(36, 1);   /// Opretion Mode

                /// #1 Read GPIO Expander ID
                (object dec1, bool isGpioExpanderIdFalse) = ProcessGpioExpanderId(hex1);


                /// #2 Status
                var statusResults = ProcessStatus(hex2, isGpioExpanderIdFalse);
                string bitResult5VCAM = statusResults["bitResult5VCAM"];
                string bitResult5VPL = statusResults["bitResult5VPL"];
                string bitResult5VNUM = statusResults["bitResult5VNUM"];
                string bitResult3V3JAMSAT = statusResults["bitResult3V3JAMSAT"];
                string bitResult3V3ADCS = statusResults["bitResult3V3ADCS"];
                string bitResult5VOBC = statusResults["bitResult5VOBC"];
                string bitResult5VADCS = statusResults["bitResult5VADCS"];
                string bitResult5VCOM = statusResults["bitResult5VCOM"];
                string bitResult12VADCS = statusResults["bitResult12VADCS"];
                string bitResult12VLIU = statusResults["bitResult12VLIU"];

                /// bit0 | 5V_CAM
                /// bit1 | 5N_PL
                /// bit2 | 5V_NUM
                /// bit3 | 3V3_JAMSAT
                /// bit4 | 3V3_ADCS
                /// bit5 | 5V_OBC
                /// bit6 | 5V_ADCS
                /// bit7 | 5V_COM
                /// bit8~9 | No Data
                /// bit10 | 12V_ADCS
                /// bit11 | 12V_LIU

                /// #3 Batterycurrent
                var (roundeddec3Current, batteryStatus) = ProcessBatteryCurrent(hex3);

                /// #4 BatteryVoltage
                float roundeddec4Volt = ProcessBatteryVoltage(hex4);

                /// #5 BatteryTempreture
                float roundeddec5Temp = ProcessBatteryTemperature(hex5);

                /// #6 EPS Controller Status
                string dec6 = ProcessEPSControllerStatus(hex6);

                /// #7 Subsystem Interface Status
                var subsystemInterfaceStatusResults = ProcessSubsystemInterfaceStatus(hex7);
                string bitResultI2CRTC = subsystemInterfaceStatusResults["I2C_RTC"];
                string bitResultI2CMEM = subsystemInterfaceStatusResults["I2C_MEM"];
                string bitResultI2CEPSC = subsystemInterfaceStatusResults["I2C_EPSC"];
                string bitResultI2CCOM = subsystemInterfaceStatusResults["I2C_COM"];
                string bitResultI2CANT = subsystemInterfaceStatusResults["I2C_ANT"];
                string bitResultI2CIFPV = subsystemInterfaceStatusResults["I2C_IFPV"];
                string bitResultI2CADCS = subsystemInterfaceStatusResults["I2C_ADCS"];
                string bitResultI2CCAM = subsystemInterfaceStatusResults["I2C_CAM"];
                string bitResultI2CMATLIU = subsystemInterfaceStatusResults["I2C_MATLIU"];
                string bitResultI2CNU = subsystemInterfaceStatusResults["I2C_NU"];
                string bitResultUARTJAMSAT = subsystemInterfaceStatusResults["UART_JAMSAT"];
                string bitResultSystemcheck = subsystemInterfaceStatusResults["System Check"];

                /// #9 Mode Timer
                var (dec6Time, dec6OpMode) = ProcessModeTimer(hex8);

                /// #10 JAMSAT Status
                var jamStatusResults = ProcessJAMSATStatus(hex9);
                string bitResultVC1LOCK = jamStatusResults["bitResultVC1LOCK"];
                string bitResultVC2LOCK = jamStatusResults["bitResultVC2LOCK"];
                string bitResult7021LOCK = jamStatusResults["bitResult7021LOCK"];
                string bitResult58GLOCK = jamStatusResults["bitResult58GLOCK"];
                string bitResultVC2ON = jamStatusResults["bitResultVC2ON"];
                string bitResultAMPEN = jamStatusResults["bitResultAMPEN"];
                string bitResult58GON = jamStatusResults["bitResult58GON"];
                string bitResultUHFCWON = jamStatusResults["bitResultUHFCWON"];

                /// bit0 | VC1_LOCK
                /// bit1 | VC2_LOCK
                /// bit2 | 7021_LOCK
                /// bit3 | 58G_LOCK
                /// bit4 | VC2_ON
                /// bit5 | ANP_EN
                /// bit6 | 58G_ON
                /// bit7 | UHFCW_ON

                /// #11 ADC Volatge
                float roundeddec10ADCVolt = ProcessADCVoltage(hex10);

                /// #12 RF Input VHF-Band
                float roundeddec11RF = ProcessRFInputVHF(hex11);

                /// #13 RF Output UHF-Band
                object roundeddec12RF = ProcessRFOutputUHF(hex12);

                /// #14 RF Output 58G
                object roundeddec13RF = ProcessRFOutput58G(hex13);

                /// #15(8) Opretion Mode
                hex14 = ProcessOperationMode();

                /// Rabel
                txtGPIOExpanderJ.Text = dec1.ToString();
                txtBatteryCurrentJ.Text = roundeddec3Current.ToString();
                txtBatteryStatusJ.Text = batteryStatus.ToString();
                txtBatteryVoltageJ.Text = roundeddec4Volt.ToString();
                txtBatteryTemperatureJ.Text = roundeddec5Temp.ToString();
                txtEPSControllerStatusJ.Text = dec6.ToString();
                txtWDUTemperatureJ.Text = "---";
                txtMCUTemperatureJ.Text = "---";
                txtOperationModeJ.Text = hex14.ToString();

                txt5VCAMJ.Text = bitResult5VCAM.ToString();
                txt5VPLJ.Text = bitResult5VPL.ToString();
                txt5VNUMJ.Text = bitResult5VNUM.ToString();
                txt3V3JAMSATJ.Text = bitResult3V3JAMSAT.ToString();
                txt3V3ADCSJ.Text = bitResult3V3ADCS.ToString();
                txt5VOBCJ.Text = bitResult5VOBC.ToString();
                txt5VADCSJ.Text = bitResult5VADCS.ToString();
                txt5VCOMJ.Text = bitResult5VCOM.ToString();
                txt12VADCSJ.Text = bitResult12VADCS.ToString();
                txt12VLIUJ.Text = bitResult12VLIU.ToString();

                UpdateStatusIndicator(txt5VCAMJ, statusIndicator5VCAMJ);
                UpdateStatusIndicator(txt5VPLJ, statusIndicator5VPLJ);
                UpdateStatusIndicator(txt5VNUMJ, statusIndicator5VNUMJ);
                UpdateStatusIndicator(txt3V3JAMSATJ, statusIndicator3V3JAMSATJ);
                UpdateStatusIndicator(txt3V3ADCSJ, statusIndicator3V3ADCSJ);
                UpdateStatusIndicator(txt5VOBCJ, statusIndicator5VOBCJ);
                UpdateStatusIndicator(txt5VADCSJ, statusIndicator5VADCSJ);
                UpdateStatusIndicator(txt5VCOMJ, statusIndicator5VCOMJ);
                UpdateStatusIndicator(txt12VADCSJ, statusIndicator12VADCSJ);
                UpdateStatusIndicator(txt12VLIUJ, statusIndicator12VLIUJ);

                txtSystemCheckJ.Text = bitResultSystemcheck.ToString();

                txtI2CRTCJ.Text = bitResultI2CRTC.ToString();
                txtI2CMEMJ.Text = bitResultI2CMEM.ToString();
                txtI2CEPSCJ.Text = bitResultI2CEPSC.ToString();
                txtI2CCOMJ.Text = bitResultI2CCOM.ToString();
                txtI2CANTJ.Text = bitResultI2CANT.ToString();
                txtI2CIFPVJ.Text = bitResultI2CIFPV.ToString();
                txtI2CADCSJ.Text = bitResultI2CADCS.ToString();
                txtI2CCAMJ.Text = bitResultI2CCAM.ToString();
                txtI2CMATLIUJ.Text = bitResultI2CMATLIU.ToString();
                txtI2CNUJ.Text = bitResultI2CNU.ToString();
                txtUARTJAMSATJ.Text = bitResultUARTJAMSAT.ToString();

                UpdateStatusIndicator(txtI2CRTCJ, statusIndicatorI2CRTCJ);
                UpdateStatusIndicator(txtI2CMEMJ, statusIndicatorI2CMEMJ);
                UpdateStatusIndicator(txtI2CEPSCJ, statusIndicatorI2CEPSCJ);
                UpdateStatusIndicator(txtI2CCOMJ, statusIndicatorI2CCOMJ);
                UpdateStatusIndicator(txtI2CANTJ, statusIndicatorI2CANTJ);
                UpdateStatusIndicator(txtI2CIFPVJ, statusIndicatorI2CIFPVJ);
                UpdateStatusIndicator(txtI2CADCSJ, statusIndicatorI2CADCSJ);
                UpdateStatusIndicator(txtI2CCAMJ, statusIndicatorI2CCAMJ);
                UpdateStatusIndicator(txtI2CMATLIUJ, statusIndicatorI2CMATLIUJ);
                UpdateStatusIndicator(txtI2CNUJ, statusIndicatorI2CNUJ);
                UpdateStatusIndicator(txtUARTJAMSATJ, statusIndicatorUARTJAMSATJ);

                txtModeTimer.Text = dec6Time.ToString();
                txtModeTimerOP.Text = dec6OpMode.ToString();
                txtADCVoltage.Text = roundeddec10ADCVolt.ToString();
                txtRFInput.Text = roundeddec11RF.ToString();
                txtRFOutputUHF.Text = roundeddec12RF.ToString();
                txtRFOutput58G.Text = roundeddec13RF.ToString();

                txtVC1LOCK.Text = bitResultVC1LOCK.ToString();
                txtVC2LOCK.Text = bitResultVC2LOCK.ToString();
                txt7021LOCK.Text = bitResult7021LOCK.ToString();
                txt58GLOCK.Text = bitResult58GLOCK.ToString();
                txtVC2ON.Text = bitResultVC2ON.ToString();
                txtAMPEN.Text = bitResultAMPEN.ToString();
                txt58GON.Text = bitResult58GON.ToString();
                txtUHFCWON.Text = bitResultUHFCWON.ToString();

                UpdateStatusIndicator(txtVC1LOCK, statusIndicatorVC1LOCK);
                UpdateStatusIndicator(txtVC2LOCK, statusIndicatorVC2LOCK);
                UpdateStatusIndicator(txt7021LOCK, statusIndicator7021LOCK);
                UpdateStatusIndicator(txt58GLOCK, statusIndicator58GLOCK);
                UpdateStatusIndicator(txtVC2ON, statusIndicatorVC2ON);
                UpdateStatusIndicator(txtAMPEN, statusIndicatorAMPEN);
                UpdateStatusIndicator(txt58GON, statusIndicator58GON);
                UpdateStatusIndicator(txtUHFCWON, statusIndicatorUHFCWON);

                LogData logData = new LogData
                {
                    Callsign = "JS1YKI",
                    Input = input,
                    TimeStamp = timestamp,
                    GPIOExpander = dec1,
                    BatteryCurrent = roundeddec3Current,
                    BatteryStatus = batteryStatus,
                    BatteryVoltage = roundeddec4Volt,
                    BatteryTemperature = roundeddec5Temp,
                    WDUTemperature = "---", // Data not provided.
                    MCUTemperature = "---", // Data not provided.
                    OperationMode = hex12,
                    EPSControllerStatus = dec6,
                    StatusBits = new StatusBitsData
                    {
                        _5V_CAM = bitResult5VCAM,
                        _5V_PL = bitResult5VPL,
                        _5V_NUM = bitResult5VNUM,
                        _3V3_JAMSAT = bitResult3V3JAMSAT,
                        _3V3_ADCS = bitResult3V3ADCS,
                        _5V_OBC = bitResult5VOBC,
                        _5V_ADCS = bitResult5VADCS,
                        _5V_COM = bitResult5VCOM,
                        _12V_ADCS = bitResult12VADCS,
                        _12V_LIU = bitResult12VLIU
                    },
                    SubsystemInterfaceStatusBits = new SubsystemInterfaceStatusBitsData
                    {
                        SystemCheck = bitResultSystemcheck,
                        _I2C_RTC = bitResultI2CRTC,
                        _I2C_MEM = bitResultI2CMEM,
                        _I2C_EPSC = bitResultI2CEPSC,
                        _I2C_COM = bitResultI2CCOM,
                        _I2C_ANT = bitResultI2CANT,
                        _I2C_IFPV = bitResultI2CIFPV,
                        _I2C_ADCS = bitResultI2CADCS,
                        _I2C_CAM = bitResultI2CCAM,
                        _I2C__MATLIU = bitResultI2CMATLIU,
                        _I2C_NU = bitResultI2CNU,
                        _UART_JAMSAT = bitResultUARTJAMSAT
                    },
                    JamsatStatusBits = new JamsatStatusBitsData
                    {
                        _VC1_LOCK = bitResultVC1LOCK,
                        _VC2_LOCK = bitResultVC2LOCK,
                        _7021_LOCK = bitResult7021LOCK,
                        _58G_LOCK = bitResult58GLOCK,
                        _VC2_ON = bitResultVC2ON,
                        _AMP_EN = bitResultAMPEN,
                        _58G_ON = bitResult58GON,
                        _UHFCW_ON = bitResultUHFCWON
                    },
                    JamsatTelemetry = new JamsatTelemetryData
                    {
                        ADCVoltage = roundeddec10ADCVolt,
                        RFInput = roundeddec11RF,
                        RFOutputUHF = roundeddec12RF,
                        RFOutput58G = roundeddec13RF
                    }
                };

                SaveLogData(logData);
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ResetUI();
            InputTextBox.Text = string.Empty;
        }

        private void ClearTextControls(params Control[] controls)
        {
            foreach (var control in controls)
            {
                if (control is TextBox textBox)
                {
                    textBox.Text = string.Empty;
                }
                else if (control is TextBlock textBlock)
                {
                    textBlock.Text = string.Empty;
                }
            }
        }

        private void UpdateStatusIndicators(params (TextBlock textBlock, Ellipse indicator)[] pairs)
        {
            foreach (var (textBlock, indicator) in pairs)
            {
                UpdateStatusIndicator(textBlock, indicator);
            }
        }

        public void ResetUI()
        {
            ClearTextControls(
                TimestampTextBox,
                txtGPIOExpander, txtBatteryCurrent, txtBatteryStatus, txtBatteryVoltage,
                txtBatteryTemperature, txtWDUTemperature, txtMCUTemperature, txtEPSControllerStatus, txtOperationMode,
                txt5VCAM, txt5VPL, txt5VNUM, txt3V3JAMSAT, txt3V3ADCS, txt5VOBC, txt5VADCS, txt5VCOM, txt12VADCS, txt12VLIU,
                txtSystemCheck,
                txtI2CRTC, txtI2CMEM, txtI2CEPSC, txtI2CCOM, txtI2CANT, txtI2CIFPV, txtI2CADCS, txtI2CCAM, txtI2CMATLIU, txtI2CNU, txtUARTJAMSAT,
                txtGPIOExpanderJ, txtBatteryCurrentJ, txtBatteryStatusJ, txtBatteryVoltageJ,
                txtBatteryTemperatureJ, txtWDUTemperatureJ, txtMCUTemperatureJ, txtEPSControllerStatusJ, txtOperationModeJ, 
                txtSystemCheckJ,
                txtI2CRTCJ, txtI2CMEMJ, txtI2CEPSCJ, txtI2CCOMJ, txtI2CANTJ, txtI2CIFPVJ, txtI2CADCSJ, txtI2CCAMJ, txtI2CMATLIUJ, txtI2CNUJ, txtUARTJAMSATJ,               
                txt5VCAMJ, txt5VPLJ, txt5VNUMJ, txt3V3JAMSATJ, txt3V3ADCSJ, txt5VOBCJ, txt5VADCSJ, txt5VCOMJ, txt12VADCSJ, txt12VLIUJ, 
                txtModeTimer, txtModeTimerOP, txtADCVoltage, txtRFInput, txtRFOutputUHF, txtRFOutput58G,
                txtVC1LOCK, txtVC2LOCK, txt7021LOCK, txt58GLOCK, txtVC2ON, txtAMPEN, txt58GON, txtUHFCWON
            );

            UpdateStatusIndicators(
                (txt5VCAM, statusIndicator5VCAM), 
                (txt5VPL, statusIndicator5VPL),
                (txt5VNUM, statusIndicator5VNUM),
                (txt3V3JAMSAT, statusIndicator3V3JAMSAT),
                (txt3V3ADCS, statusIndicator3V3ADCS),
                (txt5VOBC, statusIndicator5VOBC),
                (txt5VADCS, statusIndicator5VADCS),
                (txt5VCOM, statusIndicator5VCOM),
                (txt12VADCS, statusIndicator12VADCS),
                (txt12VLIU, statusIndicator12VLIU),
                (txt5VCAMJ, statusIndicator5VCAMJ),
                (txt5VPLJ, statusIndicator5VPLJ),
                (txt5VNUMJ, statusIndicator5VNUMJ),
                (txt3V3JAMSATJ, statusIndicator3V3JAMSATJ),
                (txt3V3ADCSJ, statusIndicator3V3ADCSJ),
                (txt5VOBCJ, statusIndicator5VOBCJ),
                (txt5VADCSJ, statusIndicator5VADCSJ),
                (txt5VCOMJ, statusIndicator5VCOMJ),
                (txt12VADCSJ, statusIndicator12VADCSJ),
                (txt12VLIUJ, statusIndicator12VLIUJ),
                (txtVC1LOCK, statusIndicatorVC1LOCK),
                (txtVC2LOCK, statusIndicatorVC2LOCK),
                (txt7021LOCK, statusIndicator7021LOCK),
                (txt58GLOCK, statusIndicator58GLOCK),
                (txtVC2ON, statusIndicatorVC2ON),
                (txtAMPEN, statusIndicatorAMPEN),
                (txt58GON, statusIndicator58GON),
                (txtUHFCWON, statusIndicatorUHFCWON),
                (txtI2CRTC, statusIndicatorI2CRTC),
                (txtI2CMEM, statusIndicatorI2CMEM),
                (txtI2CEPSC, statusIndicatorI2CEPSC),
                (txtI2CCOM, statusIndicatorI2CCOM),
                (txtI2CANT, statusIndicatorI2CANT),
                (txtI2CIFPV, statusIndicatorI2CIFPV),
                (txtI2CADCS, statusIndicatorI2CADCS),
                (txtI2CCAM, statusIndicatorI2CCAM),
                (txtI2CMATLIU, statusIndicatorI2CMATLIU),
                (txtI2CNU, statusIndicatorI2CNU),
                (txtUARTJAMSAT, statusIndicatorUARTJAMSAT),
                (txtI2CRTCJ, statusIndicatorI2CRTCJ),
                (txtI2CMEMJ, statusIndicatorI2CMEMJ),
                (txtI2CEPSCJ, statusIndicatorI2CEPSCJ),
                (txtI2CCOMJ, statusIndicatorI2CCOMJ),
                (txtI2CANTJ, statusIndicatorI2CANTJ),
                (txtI2CIFPVJ, statusIndicatorI2CIFPVJ),
                (txtI2CADCSJ, statusIndicatorI2CADCSJ),
                (txtI2CCAMJ, statusIndicatorI2CCAMJ),
                (txtI2CMATLIUJ, statusIndicatorI2CMATLIUJ),
                (txtI2CNUJ, statusIndicatorI2CNUJ),
                (txtUARTJAMSATJ, statusIndicatorUARTJAMSATJ)
            );
        }

        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            OutputTextBox.Text = string.Empty;
        }

        public void SaveLogData(object data)
        {
            try
            {    
                if (data is LogData logData)
                {
                    var dummyAccess = logData.Callsign;
                    dummyAccess = logData.Input;
                    dummyAccess = logData.TimeStamp;
                    var dummyObjectAccess = logData.GPIOExpander;
                    var dummyFloatAccess = logData.BatteryCurrent;
                    dummyAccess = logData.BatteryStatus;
                    dummyFloatAccess = logData.BatteryVoltage;
                    dummyFloatAccess = logData.BatteryTemperature;
                    dummyObjectAccess = logData.WDUTemperature;
                    dummyObjectAccess = logData.MCUTemperature;
                    dummyAccess = logData.OperationMode;
                    dummyAccess = logData.EPSControllerStatus;

                    if (logData.JamsatTelemetry != null)
                        {
                            dummyFloatAccess = logData.JamsatTelemetry.ADCVoltage;
                            dummyFloatAccess = logData.JamsatTelemetry.RFInput;
                            dummyObjectAccess = logData.JamsatTelemetry.RFOutputUHF;
                            dummyObjectAccess = logData.JamsatTelemetry.RFOutput58G;
                        }

                    dummyAccess = logData.StatusBits?._5V_CAM;
                    dummyAccess = logData.StatusBits?._5V_PL;
                    dummyAccess = logData.StatusBits?._5V_NUM;
                    dummyAccess = logData.StatusBits?._3V3_JAMSAT;
                    dummyAccess = logData.StatusBits?._3V3_ADCS;
                    dummyAccess = logData.StatusBits?._5V_OBC;
                    dummyAccess = logData.StatusBits?._5V_ADCS;
                    dummyAccess = logData.StatusBits?._5V_COM;
                    dummyAccess = logData.StatusBits?._12V_ADCS;
                    dummyAccess = logData.StatusBits?._12V_LIU;

                    dummyAccess = logData.SubsystemInterfaceStatusBits?.SystemCheck;
                    dummyAccess = logData.SubsystemInterfaceStatusBits?._I2C_RTC;
                    dummyAccess = logData.SubsystemInterfaceStatusBits?._I2C_MEM;
                    dummyAccess = logData.SubsystemInterfaceStatusBits?._I2C_EPSC;
                    dummyAccess = logData.SubsystemInterfaceStatusBits?._I2C_COM;
                    dummyAccess = logData.SubsystemInterfaceStatusBits?._I2C_ANT;
                    dummyAccess = logData.SubsystemInterfaceStatusBits?._I2C_IFPV;
                    dummyAccess = logData.SubsystemInterfaceStatusBits?._I2C_ADCS;
                    dummyAccess = logData.SubsystemInterfaceStatusBits?._I2C_CAM;
                    dummyAccess = logData.SubsystemInterfaceStatusBits?._I2C__MATLIU;
                    dummyAccess = logData.SubsystemInterfaceStatusBits?._I2C_NU;
                    dummyAccess = logData.SubsystemInterfaceStatusBits?._UART_JAMSAT;

                    dummyAccess = logData.JamsatStatusBits?._VC1_LOCK;
                    dummyAccess = logData.JamsatStatusBits?._VC2_LOCK;
                    dummyAccess = logData.JamsatStatusBits?._7021_LOCK;
                    dummyAccess = logData.JamsatStatusBits?._58G_LOCK;
                    dummyAccess = logData.JamsatStatusBits?._VC2_ON;
                    dummyAccess = logData.JamsatStatusBits?._AMP_EN;
                    dummyAccess = logData.JamsatStatusBits?._58G_ON;
                    dummyAccess = logData.JamsatStatusBits?._UHFCW_ON;

                }
                
                // 1. Generate the path to the log directory based on the application's execution directory
                string appDirectory = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                string logsDirectory = System.IO.Path.Combine(appDirectory, "Logs");

                Console.WriteLine($"App Directory: {appDirectory}");


                // 2. If the directory doesn't exist, create it
                if (!Directory.Exists(logsDirectory))
                {
                    Directory.CreateDirectory(logsDirectory);
                }

                // 3. Dynamically generate the log file's name using the current date
                string currentDate = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string logFilePath = System.IO.Path.Combine(logsDirectory, $"Log_{currentDate}.json");

                // 4. Serialize the data to JSON format
                string jsonData = JsonConvert.SerializeObject(data, Formatting.Indented, JsonSettings.JsonSerializerSettings);

                // 5. Save the JSON data to a file
                File.WriteAllText(logFilePath, jsonData);
                Console.WriteLine(jsonData);
            }
            catch (Exception ex)
            {
                // Log the exception or show a message to the user
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private void UpdateStatusIndicator(TextBlock txtBlock, Ellipse indicator)
        {
            if (txtBlock.Text == "ON" || txtBlock.Text == "ACTIVE" || txtBlock.Text == "STABLE")
            {
                indicator.Fill = new SolidColorBrush(Colors.Green);
            }
            else if (txtBlock.Text == "OFF" || txtBlock.Text == "INACTIVE" || txtBlock.Text == "FAULT")
            {
                indicator.Fill = new SolidColorBrush(Colors.Red);
            }
            else
            {
                indicator.Fill = new SolidColorBrush(Colors.Gray);
            }
        }

        private void OnLinkClicked(object sender, PointerPressedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://forms.gle/mt5ZkfrqArZmmfVv8",
                UseShellExecute = true
            });
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;
            if (radioButton == null) return;

            if (radioButton == ManualInputRadio)
            {
                Console.WriteLine("ManualInputRadio selected");
                InputTextBox.IsEnabled = true;
                InputTextBox.Text = "";
                ResetUI();
                _timer.Stop();
            }
            else if (radioButton == AutomaticInputRadio)
            {
                Console.WriteLine("AutomaticInputRadio selected");
                InputTextBox.IsEnabled = false;
                InputTextBox.Text = "";
                ResetUI();

                if (!File.Exists("UserSettings.json"))
                {
                    SettingsButton_Click(null,null);
                }
                _lastProcessedTime = DateTime.Now;

                LoadSettings();
                StartPolling();
            }
        }

        // private void RadioButton_Unchecked(object sender, RoutedEventArgs e)
        // {
        //     Console.WriteLine($"{(sender as RadioButton).Name} is unchecked.");
        // }

        // AutomaticInputRadio selected Section
        enum DataState
        {
            Initial,
            WaitingForNewData,
            DataFound
        }

        private DataState _currentState = DataState.Initial;
        private int _extractedDataLength;
        private string _targetString;
        private string _ReferencedFilePath;
        private Timer _timer;
        private DateTime _lastProcessedTime;

        private void StartPolling()
        {
            Console.WriteLine("StartPolling");
            _timer = new Timer(500); // 2 seconds
            _timer.Elapsed += PollFile;
            _timer.AutoReset = true;
            _timer.Start();
        }

        private void PollFile(object sender, ElapsedEventArgs e)
        {
            Console.WriteLine($"PollFile called at {DateTime.Now}");
            // Console.WriteLine(_targetString + _extractedDataLength + "\n" + _ReferencedFilePath);

            if (string.IsNullOrWhiteSpace(_ReferencedFilePath))
            {
                Timer_Elapsed(null,null);
                return;
            }

            string TargetFilePath = _ReferencedFilePath;

            if (!System.IO.File.Exists(TargetFilePath))
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    OutputTextBox.Text = $"File does not exist:\n{TargetFilePath}\n" + OutputTextBox.Text;
                });
                Timer_Elapsed(null,null);
                return;
            }

            if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(TargetFilePath)))
            {
                Console.WriteLine($"Directory does not exist: {System.IO.Path.GetDirectoryName(TargetFilePath)}");
                Timer_Elapsed(null,null);
                return;
            }

            var content = ReadFileContentWithRetry(TargetFilePath);
            string separator = " : ";
            int lastIndex = content.LastIndexOf(_targetString + separator);
            int separatorlength = separator.Length;
            int targetStringlength = _targetString.Length;

            if (lastIndex != -1)
            {
                string postTargetContent = content.Substring(lastIndex + _targetString.Length + separator.Length);

                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    InputTextBox.Text = postTargetContent;
                });
                Console.WriteLine(postTargetContent);

                if (content.Length >= lastIndex + targetStringlength + separatorlength + _extractedDataLength)
                {
                    string extractedData = content.Substring(lastIndex + targetStringlength + separatorlength, _extractedDataLength);

                    switch (_currentState)
                    {
                        case DataState.Initial:
                            _lastProcessedTime = DateTime.Now;
                            Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                InputTextBox.Text = "";
                                ResetUI();
                                System.Threading.Thread.Sleep(500);
                                InputTextBox.Text = extractedData;
                                NUDecodeButton_Click(null, null);
                            });
                            _currentState = DataState.WaitingForNewData;
                            break;

                        case DataState.WaitingForNewData:
                            _currentState = DataState.DataFound;
                            break;

                        case DataState.DataFound:
                            if (content.Substring(lastIndex + targetStringlength + separatorlength, _extractedDataLength) != extractedData)
                            {
                                _currentState = DataState.Initial;
                                return;
                            }
                            break;
                    }

                    // After 2 minutes without updates in Automatic mode, reverts back to Manual mode.
                    TimeSpan _sessionTimeout = TimeSpan.FromMinutes(2);

                    // Check the elapsed time
                    if (DateTime.Now - _lastProcessedTime > _sessionTimeout)
                    {
                        Timer_Elapsed(null, null);
                    }
                }
                else
                {
                    _currentState = DataState.Initial;
                }
            }

        }

        private string ReadFileContentWithRetry(string path, int maxRetries = 3, int delayOnRetry = 500)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {

                    using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var streamReader = new StreamReader(fileStream))
                    {
                        return streamReader.ReadToEnd();
                    }
                }
                catch (DirectoryNotFoundException)
                {
                    Console.WriteLine($"Directory not found: {path}");
                }
                catch (FileNotFoundException)
                {
                    Console.WriteLine($"File not found: {path}");
                }
                catch (IOException ex)
                {
                    if (i == maxRetries - 1)
                        throw;
                    System.Threading.Thread.Sleep(delayOnRetry);
                    Console.WriteLine($"IOException encountered: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception encountered: {ex.Message}");
                }
            }
            return null;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.UIThread.InvokeAsync(() => {
                ManualInputRadio.IsChecked = true;
                //RadioButton_Checked(ManualInputRadio, null);
            });

            _timer.Stop();
            _timer.Dispose();
            Console.WriteLine("StopPolling");
            Console.WriteLine("Session Timed Out: Reverting to Manual Input Mode.");
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                OutputTextBox.Text = "Session Timed Out \n" + OutputTextBox.Text;
            });
        }

        // SettingWindow Function
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Elapsed -= PollFile;
                _timer.Dispose();
                _timer = null;
            }

            var settingsWindow = new SettingsWindow();

            settingsWindow.Closed += (s, args) =>
            {
                if (AutomaticInputRadio.IsChecked == true)
                {
                    LoadSettings();
                    StartPolling();
                }
            };
            settingsWindow.ShowDialog(this);

        }

        public static class JsonSettings
        {
            public static JsonSerializerSettings JsonSerializerSettings { get; } = new JsonSerializerSettings
            {
                ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
                {
                    DefaultMembersSearchFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                }
            };
        }
    }

    public class LogData
    {
        public LogData() { }
        public string Callsign { get; set; } = "JS1YKI";
        public string Input { get; set; }
        public string TimeStamp { get; set; }
        public object GPIOExpander { get; set; }
        public float BatteryCurrent { get; set; }
        public string BatteryStatus { get; set; }
        public float BatteryVoltage { get; set; }
        public float BatteryTemperature { get; set; }
        public object WDUTemperature { get; set; }
        public object MCUTemperature { get; set; }
        public string OperationMode { get; set; }
        public string EPSControllerStatus { get; set; }
        public SubsystemInterfaceStatusBitsData SubsystemInterfaceStatusBits { get; set; }
        public StatusBitsData StatusBits { get; set; }
        public JamsatStatusBitsData JamsatStatusBits { get; set; }
        public JamsatTelemetryData JamsatTelemetry { get; set; }
    }

    public class JamsatTelemetryData
    {
        public JamsatTelemetryData() { }
        public float ADCVoltage { get; set; }
        public float RFInput { get; set; }
        public object RFOutputUHF { get; set; }
        public object RFOutput58G { get; set; }
    }

    public class StatusBitsData
    {
        public StatusBitsData() { }
        public string _5V_CAM { get; set; }
        public string _5V_PL { get; set; }
        public string _5V_NUM { get; set; }
        public string _3V3_JAMSAT { get; set; }
        public string _3V3_ADCS { get; set; }
        public string _5V_OBC { get; set; }
        public string _5V_ADCS { get; set; }
        public string _5V_COM { get; set; }
        public string _12V_ADCS { get; set; }
        public string _12V_LIU { get; set; }
    }

    public class SubsystemInterfaceStatusBitsData
    {
        public SubsystemInterfaceStatusBitsData() { }
        public string SystemCheck { get; set; }
        public string _I2C_RTC { get; set; }
        public string _I2C_MEM { get; set; }
        public string _I2C_EPSC { get; set; }
        public string _I2C_COM { get; set; }
        public string _I2C_ANT { get; set; }
        public string _I2C_IFPV { get; set; }
        public string _I2C_ADCS { get; set; }
        public string _I2C_CAM { get; set; }
        public string _I2C__MATLIU { get; set; }
        public string _I2C_NU { get; set; }
        public string _UART_JAMSAT { get; set; }
    }

    public class JamsatStatusBitsData
    {
        public JamsatStatusBitsData() { }
        public string _VC1_LOCK { get; set; }
        public string _VC2_LOCK { get; set; }
        public string _7021_LOCK { get; set; }
        public string _58G_LOCK { get; set; }
        public string _VC2_ON { get; set; }
        public string _AMP_EN { get; set; }
        public string _58G_ON { get; set; }
        public string _UHFCW_ON { get; set; }
    }

    public class AppConfig
    {
        [JsonConstructor]
        public AppConfig(){}
        public string targetString { get; set; }
        public string ReferencedFilePath { get; set; }
        public string ReferencedFolderPath { get; set;}
        public int extractedDataLength { get; set; }
    }
}