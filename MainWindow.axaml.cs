using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Metadata;
// using Newtonsoft.Json;

namespace TENKOH2_BEACON_DECODER_Multi_Platform
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
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

            /// TimeStamp
            var currentTime = DateTime.Now;
            string timestamp = currentTime.ToString("yyyy/MM/dd HH:mm:ss");
            TimestampTextBox.Text = timestamp;

            OutputTextBox.Text = $"{timestamp}: {input}\n" + OutputTextBox.Text;

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
                string binary = Convert.ToString(decValue, 2).PadLeft(16, '0');
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

            /// #6 WDU Tempreture
            /// #7 MCU Tempreture
            float ProcessTemperature(string hexValue)
            {
                const float VOLTAGE_CONVERSION_FACTOR = 4.97f / 1024f;
                const float TEMPRETURE_CONVERSION_FACTOR = 147.06f;
                const float KELVIN_TO_CELSIUS_OFFSET = 273.15f;

                float decVolt = Convert.ToInt32(hexValue, 16) * VOLTAGE_CONVERSION_FACTOR;
                float decTemp = decVolt * TEMPRETURE_CONVERSION_FACTOR - KELVIN_TO_CELSIUS_OFFSET;
                return (float)Math.Round((double)decTemp , 2);
            }

            /// #8 Opretion Mode
            string ProcessOperationMode()
            {
                return "TK";
            }

            /// #9 Mode Timer
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
                    string bitResultJam = v == 0 ? "ON" : "OFF";
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

            if (input.Length == 21)
            {
                // Switch to NUM tab when input is 21 characters
                if (MainTabControl != null)
                {
                    MainTabControl.SelectedIndex = 0;  // Index 0 corresponds to NUM tab
                }
                
                string hex1 = input.Substring(0, 2);    /// Read GPIO Expander ID
                string hex2 = input.Substring(2, 3);    /// Status 
                string hex3 = input.Substring(5, 3);    /// Battery Current
                string hex4 = input.Substring(8, 3);   /// Battery Voltage
                string hex5 = input.Substring(11, 3);   /// Battery tempreture
                string hex6 = input.Substring(14, 3);   /// WDU Tempreture
                string hex7 = input.Substring(17, 3);   /// MCU Tempreture
                string hex8 = input.Substring(20, 1);   /// Opreation mode
                
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

                /// #6 WDU Tempreture
                float roundeddec6Temp = ProcessTemperature(hex6);

                /// #7 MCU Tempreture
                float roundeddec7Temp = ProcessTemperature(hex7);

                /// #8 Opretion Mode
                hex8 = ProcessOperationMode();

                /// Rabel
                txtGPIOExpander.Text = dec1.ToString();
                txtBatteryCurrent.Text = roundeddec3Current.ToString();
                txtBatteryStatus.Text = batteryStatus.ToString();
                txtBatteryVoltage.Text = roundeddec4Volt.ToString();
                txtBatteryTemperature.Text = roundeddec5Temp.ToString();
                txtWDUTemperature.Text = roundeddec6Temp.ToString();
                txtMCUTemperature.Text = roundeddec7Temp.ToString();
                txtOperationMode.Text = hex8.ToString();

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
                    WDUTemperature = roundeddec6Temp,
                    MCUTemperature = roundeddec7Temp,
                    OperationMode = hex8,
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
                    }
                };
                SaveLogData(logData);
            }

            else if (input.Length == 33)
            {
                // Switch to JAM tab when input is 33 characters
                if (MainTabControl != null)
                {
                    MainTabControl.SelectedIndex = 1;  // Index 1 corresponds to JAM tab
                }
                
                string hex1 = input.Substring(0, 2);    /// Read GPIO Expander ID
                string hex2 = input.Substring(2, 3);    /// Status 
                string hex3 = input.Substring(5, 3);    /// Battery Current
                string hex4 = input.Substring(8, 3);   /// Battery Voltage
                string hex5 = input.Substring(11, 3);   /// Battery tempreture
                string hex6 = input.Substring(14, 4);   /// Mode Timer
                string hex7 = input.Substring(18, 2);   /// JAMSAT Status
                string hex8 = input.Substring(20, 3);   /// ADC Voltage
                string hex9 = input.Substring(23, 3);   /// RF Input VHF-Band
                string hex10 = input.Substring(26, 3);   /// RF Output UHF-Band
                string hex11 = input.Substring(29, 3);   /// RF Output 58G-Band
                string hex12 = input.Substring(32, 1);   /// Opretion Mode
                
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

                /// #9 Mode Timer
                var (dec6Time, dec6OpMode) = ProcessModeTimer(hex6);

                /// #10 JAMSAT Status 
                var jamStatusResults = ProcessJAMSATStatus(hex7);
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
                float roundeddec8ADCVolt = ProcessADCVoltage(hex8);

                /// #12 RF Input VHF-Band
                float roundeddec9RF = ProcessRFInputVHF(hex9);

                /// #13 RF Output UHF-Band
                object roundeddec10RF = ProcessRFOutputUHF(hex10);
                
                /// #14 RF Output 58G
                object roundeddec11RF = ProcessRFOutput58G(hex11);

                /// #15(8) Opretion Mode
                hex12 = ProcessOperationMode();

                /// Rabel
                txtGPIOExpanderJ.Text = dec1.ToString();
                txtBatteryCurrentJ.Text = roundeddec3Current.ToString();
                txtBatteryStatusJ.Text = batteryStatus.ToString();
                txtBatteryVoltageJ.Text = roundeddec4Volt.ToString();
                txtBatteryTemperatureJ.Text = roundeddec5Temp.ToString();
                txtWDUTemperatureJ.Text = "---";
                txtMCUTemperatureJ.Text = "---";
                txtOperationModeJ.Text = hex12.ToString();

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

                txtModeTimer.Text = dec6Time.ToString();
                txtModeTimerOP.Text = dec6OpMode.ToString();
                txtADCVoltage.Text = roundeddec8ADCVolt.ToString();
                txtRFInput.Text = roundeddec9RF.ToString();
                txtRFOutputUHF.Text = roundeddec10RF.ToString();
                txtRFOutput58G.Text = roundeddec11RF.ToString();

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
                        ADCVoltage = roundeddec8ADCVolt,
                        RFInput = roundeddec9RF,
                        RFOutputUHF = roundeddec10RF,
                        RFOutput58G = roundeddec11RF
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
                txtBatteryTemperature, txtWDUTemperature, txtMCUTemperature, txtOperationMode,
                txt5VCAM, txt5VPL, txt5VNUM, txt3V3JAMSAT, txt3V3ADCS, txt5VOBC, txt5VADCS,
                txt5VCOM, txt12VADCS, txt12VLIU,
                txtGPIOExpanderJ, txtBatteryCurrentJ, txtBatteryStatusJ, txtBatteryVoltageJ, txtBatteryTemperatureJ,
                txtWDUTemperatureJ, txtMCUTemperatureJ, txtOperationModeJ, txt5VCAMJ, txt5VPLJ, txt5VNUMJ,
                txt3V3JAMSATJ, txt3V3ADCSJ, txt5VOBCJ, txt5VADCSJ, txt5VCOMJ, txt12VADCSJ, txt12VLIUJ, 
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
                (txtUHFCWON, statusIndicatorUHFCWON)
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
                string jsonData = System.Text.Json.JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });

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
            if (txtBlock.Text == "ON")
            {
                indicator.Fill = new SolidColorBrush(Colors.Green);
            }
            else if (txtBlock.Text == "OFF")
            {
                indicator.Fill = new SolidColorBrush(Colors.Red);
            }
            else
            {
                indicator.Fill = new SolidColorBrush(Colors.Gray);
            }
        }
    }

    public class LogData
    {
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
        public StatusBitsData StatusBits { get; set; }
        public JamsatStatusBitsData JamsatStatusBits { get; set; }
        public JamsatTelemetryData JamsatTelemetry { get; set; }
    }

    public class JamsatTelemetryData
    {
        public float ADCVoltage { get; set; }
        public float RFInput { get; set; }
        public object RFOutputUHF { get; set; }
        public object RFOutput58G { get; set; }
    }


    public class StatusBitsData
    {
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

    public class JamsatStatusBitsData
    {
        public string _VC1_LOCK { get; set; }
        public string _VC2_LOCK { get; set; }
        public string _7021_LOCK { get; set; }
        public string _58G_LOCK { get; set; }
        public string _VC2_ON { get; set; }
        public string _AMP_EN { get; set; }
        public string _58G_ON { get; set; }
        public string _UHFCW_ON { get; set; }
    }

}