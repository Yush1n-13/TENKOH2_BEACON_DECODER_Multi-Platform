using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media;
// using Newtonsoft.Json;

namespace TENKOH2_BEACON_DECODER_Multi_Platform
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        private const float VOLTAGE_CONVERSION_FACTOR1 = 5.0f / 4096f;
        private const float VOLTAGE_CONVERSION_FACTOR2 = 4.97f / 1024f;
        private const float CURRENT_OFFSET = 2.5f;
        private const float CURRENT_CONVERSION_FACTOR = 200f * 0.001f;
        private const float TEMPRETURE_CONVERSION_FACTOR = 147.06f;
        private const float KELVIN_TO_CELSIUS_OFFSET = 273.15f;
        private const float VOLTAGE_CONVERSION_FACTOR3 = 60f;
        private const float VOLTAGE_OFFSET1 = 515f;
        private const float VOLTAGE_CONVERSION_FACTOR4 = 0.0772f;
        private const float RFINPUT_OFFSET = 153.23f;
        private const float VOLTAGE_CONVERSION_FACTOR5 = 0.0154f;
        private const float RFOUTPUT_OFFSET1 = 16.841f;
        private const float VOLTAGE_CONVERSION_FACTOR6 = 0.009f;
        private const float RFOUTPUT_OFFSET2 = 4.499f + 5.5f;
        
        public MainWindow()
        {
            InitializeComponent();
        }

        private void NUDecodeButton_Click(object sender, RoutedEventArgs e)
        {
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

            if (input.Length == 21)
            {
                string hex1 = input.Substring(0, 2);    /// Read GPIO Expander ID
                string hex2 = input.Substring(2, 3);    /// Status 
                string hex3 = input.Substring(5, 3);    /// Battery Current
                string hex4 = input.Substring(8, 3);   /// Battery Voltage
                string hex5 = input.Substring(11, 3);   /// Battery tempreture
                string hex6 = input.Substring(14, 3);   /// WDU Tempreture
                string hex7 = input.Substring(17, 3);   /// MCU Tempreture
                string hex8 = input.Substring(20, 1);   /// Opreation mode
                
                /// #1 Read GPIO Expander ID
                object dec1;
                bool isGpioExpanderIdFalse = false;
                if (hex1 == "28")
                {
                    dec1 = "True";
                }
                else
                {
                    dec1 = "False";
                    isGpioExpanderIdFalse = true;
                }

                /// #2 Status 
                int dec2 = Convert.ToInt32(hex2, 16);
                string binary = Convert.ToString(dec2, 2).PadLeft(16, '0');
                List<int> binaryList = new List<int>();
                foreach (char bit in binary)
                {
                    binaryList.Add(int.Parse(bit.ToString()));
                }

                var bitResultList = new List<string>();
                foreach (int v in binaryList)
                {   
                    string bitResult = v == 0 ? "ON" : "OFF";
                    bitResultList.Add(bitResult);
                }

                string bitResult5VCAM = bitResultList[0];
                string bitResult5VPL = bitResultList[1];
                string bitResult5VNUM = bitResultList[2];
                string bitResult3V3JAMSAT = bitResultList[3];
                string bitResult3V3ADCS = bitResultList[4];
                string bitResult5VOBC = bitResultList[5];
                string bitResult5VADCS = bitResultList[6];
                string bitResult5VCOM = bitResultList[7];
                string bitResult12VADCS = bitResultList[10];
                string bitResult12VLIU = bitResultList[11];

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
                float dec3Volt = Convert.ToInt32(hex3, 16) * VOLTAGE_CONVERSION_FACTOR1;
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

                /// #4 BatteryVoltage
                float dec4Volt = Convert.ToInt32(hex4, 16) * VOLTAGE_CONVERSION_FACTOR1;
                float roundeddec4Volt = (float)Math.Round((double)dec4Volt , 2);

                /// #5 BatteryTempreture
                float dec5Volt = Convert.ToInt32(hex5, 16) * VOLTAGE_CONVERSION_FACTOR1;
                float dec5Temp = dec5Volt * TEMPRETURE_CONVERSION_FACTOR - KELVIN_TO_CELSIUS_OFFSET;
                float roundeddec5Temp = (float)Math.Round((double)dec5Temp , 2);

                /// #6 WDU Tempreture
                float dec6Volt = Convert.ToInt32(hex6, 16) * VOLTAGE_CONVERSION_FACTOR2;
                float dec6Temp = dec6Volt * TEMPRETURE_CONVERSION_FACTOR - KELVIN_TO_CELSIUS_OFFSET;
                float roundeddec6Temp = (float)Math.Round((double)dec6Temp , 2);

                /// #7 MCU Tempreture
                float dec7Volt = Convert.ToInt32(hex7, 16) * VOLTAGE_CONVERSION_FACTOR2;
                float dec7Temp = dec7Volt * TEMPRETURE_CONVERSION_FACTOR - KELVIN_TO_CELSIUS_OFFSET;
                float roundeddec7Temp = (float)Math.Round((double)dec7Temp , 2);

                /// #8 Opretion MOode

                hex8 = "TK";

                /// Rabel
                txtGPIOExpander.Text = dec1.ToString();
                txtBatteryCurrent.Text = roundeddec3Current.ToString();
                txtBatteryStatus.Text = batteryStatus.ToString();
                txtBatteryVoltage.Text = roundeddec4Volt.ToString();
                txtBatteryTemperature.Text = roundeddec5Temp.ToString();
                txtWDUTemperature.Text = roundeddec6Temp.ToString();
                txtMCUTemperature.Text = roundeddec7Temp.ToString();
                txtOperationMode.Text = hex8.ToString();

                if (!isGpioExpanderIdFalse)
                {
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
                }
                else
                {
                    txt5VCAM.Text = "False";
                    txt5VPL.Text = "False";
                    txt5VNUM.Text = "False";
                    txt3V3JAMSAT.Text = "False";
                    txt3V3ADCS.Text = "False";
                    txt5VOBC.Text = "False";
                    txt5VADCS.Text = "False";
                    txt5VCOM.Text = "False";
                    txt12VADCS.Text = "False";
                    txt12VLIU.Text = "False";
                }

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
                object dec1;
                bool isGpioExpanderIdFalse = false;
                if (hex1 == "28")
                {
                    dec1 = "True";
                }
                else
                {
                    dec1 = "False";
                    isGpioExpanderIdFalse = true;
                }

                /// #2 Status 
                int dec2 = Convert.ToInt32(hex2, 16);
                string binary = Convert.ToString(dec2, 2).PadLeft(16, '0');
                List<int> binaryList = new List<int>();
                foreach (char bit in binary)
                {
                    binaryList.Add(int.Parse(bit.ToString()));
                }

                var bitResultList = new List<string>();
                foreach (int v in binaryList)
                {   
                    string bitResult = v == 0 ? "ON" : "OFF";
                    bitResultList.Add(bitResult);
                }

                string bitResult5VCAM = bitResultList[0];
                string bitResult5VPL = bitResultList[1];
                string bitResult5VNUM = bitResultList[2];
                string bitResult3V3JAMSAT = bitResultList[3];
                string bitResult3V3ADCS = bitResultList[4];
                string bitResult5VOBC = bitResultList[5];
                string bitResult5VADCS = bitResultList[6];
                string bitResult5VCOM = bitResultList[7];
                string bitResult12VADCS = bitResultList[10];
                string bitResult12VLIU = bitResultList[11];

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
                float dec3Volt = Convert.ToInt32(hex3, 16) * VOLTAGE_CONVERSION_FACTOR1;
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

                /// #4 BatteryVoltage
                float dec4Volt = Convert.ToInt32(hex4, 16) * VOLTAGE_CONVERSION_FACTOR1;
                float roundeddec4Volt = (float)Math.Round((double)dec4Volt , 2);

                /// #5 BatteryTempreture
                float dec5Volt = Convert.ToInt32(hex5, 16) * VOLTAGE_CONVERSION_FACTOR1;
                float dec5Temp = dec5Volt * TEMPRETURE_CONVERSION_FACTOR - KELVIN_TO_CELSIUS_OFFSET;
                float roundeddec5Temp = (float)Math.Round((double)dec5Temp , 2);

                /// #6 Mode Timer
                float dec6 = Convert.ToInt32(hex6, 16) / VOLTAGE_CONVERSION_FACTOR3;
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

                /// #7 JAMSAT Status 
                int dec7 = Convert.ToInt32(hex7, 16);
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

                /// Active =>ON, Disabled =>OFF

                string bitResultVC1LOCK = bitResultListJam[0];
                string bitResultVC2LOCK= bitResultListJam[1];
                string bitResult7021LOCK = bitResultListJam[2];
                string bitResult58GLOCK = bitResultListJam[3];
                string bitResultVC2ON = bitResultListJam[4];
                string bitResultAMPEN = bitResultListJam[5];
                string bitResult58GON = bitResultListJam[6];
                string bitResultUHFCWON = bitResultListJam[7];

                /// bit0 | VC1_LOCK
                /// bit1 | VC2_LOCK
                /// bit2 | 7021_LOCK
                /// bit3 | 58G_LOCK
                /// bit4 | VC2_ON
                /// bit5 | ANP_EN
                /// bit6 | 58G_ON
                /// bit7 | UHFCW_ON

                /// #8 ADC Volatge
                float dec8ADCVolt = (2 * Convert.ToInt32(hex8, 16) - VOLTAGE_OFFSET1) / 1000f;
                float roundeddec8ADCVolt = (float)Math.Round((double)dec8ADCVolt , 2);

                /// #9 RF Input VHF-Band
                float dec9RF = VOLTAGE_CONVERSION_FACTOR4 * Convert.ToInt32(hex9, 16) - RFINPUT_OFFSET;
                float roundeddec9RF = (float)Math.Round((double)dec9RF , 2);

                /// #10 RF Output UHF-Band
                object roundeddec10RF;
                if (hex10 == "000")
                {
                    roundeddec10RF = "---";
                }
                else
                {
                    float dec10RF = VOLTAGE_CONVERSION_FACTOR5 * Convert.ToInt32(hex10, 16) + RFOUTPUT_OFFSET1;
                    roundeddec10RF = (float)Math.Round((double)dec10RF , 2);
                }
                
                /// #11 RF Output 58G
                object roundeddec11RF;
                if (hex11 == "000")
                {
                    roundeddec11RF = "---";
                }
                else
                {
                    float dec11RF = VOLTAGE_CONVERSION_FACTOR6 * Convert.ToInt32(hex11, 16) + RFOUTPUT_OFFSET2;
                    roundeddec11RF = (float)Math.Round((double)dec11RF , 2);
                }

                /// #12 Opretion MOode

                hex12 = "TRP";

                /// Rabel
                txtGPIOExpanderJ.Text = dec1.ToString();
                txtBatteryCurrentJ.Text = roundeddec3Current.ToString();
                txtBatteryStatusJ.Text = batteryStatus.ToString();
                txtBatteryVoltageJ.Text = roundeddec4Volt.ToString();
                txtBatteryTemperatureJ.Text = roundeddec5Temp.ToString();
                txtWDUTemperatureJ.Text = "---";
                txtMCUTemperatureJ.Text = "---";
                txtOperationModeJ.Text = hex12.ToString();

                if (!isGpioExpanderIdFalse)
                {
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
                }
                else
                {
                    txt5VCAMJ.Text = "False";
                    txt5VPLJ.Text = "False";
                    txt5VNUMJ.Text = "False";
                    txt3V3JAMSATJ.Text = "False";
                    txt3V3ADCSJ.Text = "False";
                    txt5VOBCJ.Text = "False";
                    txt5VADCSJ.Text = "False";
                    txt5VCOMJ.Text = "False";
                    txt12VADCSJ.Text = "False";
                    txt12VLIUJ.Text = "False";
                }

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
            InputTextBox.Text = string.Empty;
            TimestampTextBox.Text = string.Empty;
            txt12VADCS.Text = string.Empty;
            txt12VADCSJ.Text = string.Empty;
            txt12VLIU.Text = string.Empty;
            txt12VLIUJ.Text = string.Empty;
            txt3V3ADCS.Text = string.Empty;
            txt3V3ADCSJ.Text = string.Empty;
            txt3V3JAMSAT.Text = string.Empty;
            txt3V3JAMSATJ.Text = string.Empty;
            txt58GLOCK.Text = string.Empty;
            txt58GON.Text = string.Empty;
            txt5VADCS.Text = string.Empty;
            txt5VADCSJ.Text = string.Empty;
            txt5VCAM.Text = string.Empty;
            txt5VCAMJ.Text = string.Empty;
            txt5VCOM.Text = string.Empty;
            txt5VCOMJ.Text = string.Empty;
            txt5VNUM.Text = string.Empty;
            txt5VNUMJ.Text = string.Empty;
            txt5VOBC.Text = string.Empty;
            txt5VOBCJ.Text = string.Empty;
            txt5VPL.Text = string.Empty;
            txt5VPLJ.Text = string.Empty;
            txt7021LOCK.Text = string.Empty;
            txtADCVoltage.Text = string.Empty;
            txtAMPEN.Text = string.Empty;
            txtBatteryCurrent.Text = string.Empty;
            txtBatteryCurrentJ.Text = string.Empty;
            txtBatteryStatus.Text = string.Empty;
            txtBatteryStatusJ.Text = string.Empty;
            txtBatteryTemperature.Text = string.Empty;
            txtBatteryTemperatureJ.Text = string.Empty;
            txtBatteryVoltage.Text = string.Empty;
            txtBatteryVoltageJ.Text = string.Empty;
            txtGPIOExpander.Text = string.Empty;
            txtGPIOExpanderJ.Text = string.Empty;
            txtMCUTemperature.Text = string.Empty;
            txtMCUTemperatureJ.Text = string.Empty;
            txtModeTimer.Text = string.Empty;
            txtModeTimerOP.Text = string.Empty;
            txtOperationMode.Text = string.Empty;
            txtOperationModeJ.Text = string.Empty;
            txtRFInput.Text = string.Empty;
            txtRFOutput58G.Text = string.Empty;
            txtRFOutputUHF.Text = string.Empty;
            txtUHFCWON.Text = string.Empty;
            txtVC1LOCK.Text = string.Empty;
            txtVC2LOCK.Text = string.Empty;
            txtVC2ON.Text = string.Empty;
            txtWDUTemperature.Text = string.Empty;
            txtWDUTemperatureJ.Text = string.Empty;

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

            UpdateStatusIndicator(txtVC1LOCK, statusIndicatorVC1LOCK);
            UpdateStatusIndicator(txtVC2LOCK, statusIndicatorVC2LOCK);
            UpdateStatusIndicator(txt7021LOCK, statusIndicator7021LOCK);
            UpdateStatusIndicator(txt58GLOCK, statusIndicator58GLOCK);
            UpdateStatusIndicator(txtVC2ON, statusIndicatorVC2ON);
            UpdateStatusIndicator(txtAMPEN, statusIndicatorAMPEN);
            UpdateStatusIndicator(txt58GON, statusIndicator58GON);
            UpdateStatusIndicator(txtUHFCWON, statusIndicatorUHFCWON);
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