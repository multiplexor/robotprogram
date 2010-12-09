using System;
using System.IO.Ports;
using System.Windows.Forms;
using System.Drawing;

namespace spatial_and_logical_robot_program
{
    /// <summary>
    /// Структура. Хранит текущее состояние четырех клавиш направления
    /// </summary>
    struct KeyboardState
    {
        public bool Up, Down, Left, Right;
    }

    public class DatareceivedArgs: EventArgs
    {
        public string Data;
        public DatareceivedArgs(string data)
        {
            Data = data;
        }
    }
    
    public delegate void DataReceivedEventHandler(object source, DatareceivedArgs arg);

    /// <summary>
    /// Класс, занимающий передачей/приемом данных по bluetooh.
    /// В составе класа есть функции обработки нажатий клавиш на клавиатуре, которые в результате отправляют по bluetooth управляющие воздействия.
    /// </summary>
    class BluetoothDataTransmit
    {
        /// <summary>
        /// Объект, работающий с виртуальным com портом для передачи/приема данных по bluetooth 
        /// </summary>
        readonly SerialPort _serialPort1;
        KeyboardState _keys;
        public string ReceivedData;
        public string SendedData;
        
        public event DataReceivedEventHandler DataReceived;

        public BluetoothDataTransmit()
        {
            // инициализация COM порта
            _serialPort1 = new SerialPort {BaudRate = 19200, PortName = "COM32", ReceivedBytesThreshold = 4};
            _serialPort1.DataReceived += SerialPort1DataReceived;
            try { _serialPort1.Open(); }
            catch { MessageBox.Show(@"Can't open port " + _serialPort1.PortName); }
        }

        ~BluetoothDataTransmit()
        {
            _serialPort1.Close();
        }

        void SerialPort1DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string temp = _serialPort1.ReadExisting();
            ReceivedData += temp;
            if (DataReceived != null)
                DataReceived(this, new DatareceivedArgs(temp));
        }

        public void Send_Data(ControlSignal controlSignal)
        {
            if (!_serialPort1.IsOpen || !_serialPort1.CtsHolding) return;
            switch (controlSignal)
            {
                case ControlSignal.Forward: { _serialPort1.Write("d"); SendedData += " d"; break; }
                case ControlSignal.ForwardLeft: { _serialPort1.Write("e"); SendedData += " e"; break; }
                case ControlSignal.ForwardRight: { _serialPort1.Write("f"); SendedData += " f"; break; }
                case ControlSignal.Backward: { _serialPort1.Write("a"); SendedData += " a"; break; }
                case ControlSignal.BackwardLeft: { _serialPort1.Write("b"); SendedData += " b"; break; }
                case ControlSignal.BackwardRight: { _serialPort1.Write("c"); SendedData += " c"; break; }
                case ControlSignal.Stop: { _serialPort1.Write("g"); SendedData += " g"; break; }
            }
        }

        public void Send_Data(ControlType controlType)
        {
            if (!_serialPort1.IsOpen || !_serialPort1.CtsHolding) return;
            if (controlType == ControlType.AutomaticOffLineControl)
            {
                _serialPort1.Write("i");
                SendedData += " i";
            }
            else
            {
                _serialPort1.Write("xe");
                SendedData += " xe";
            }
        }

        public void Send_Data(MovementType movementType)
        {
            if (_serialPort1.IsOpen && _serialPort1.CtsHolding)
            {
                if (movementType == MovementType.Continious)
                {
                    _serialPort1.Write("xc");
                    SendedData += " xc";
                }
                else
                {
                    _serialPort1.Write("xq");
                    SendedData += (" xq");
                }
            }
        }

        public void Send_Data(byte speed)
        {
            if (_serialPort1.IsOpen && _serialPort1.CtsHolding)
            {
                var buffer = new[] { speed };
                _serialPort1.Write("s");
                _serialPort1.Write(buffer, 0, 1);
                SendedData += " s" + speed;
            }
        }

        public void SendRecognizedData(Point carCoordinates, double angle, int regionOfSpace, bool carReachedDestinationPoint)
        {
            if (_serialPort1.IsOpen && _serialPort1.CtsHolding)
            {
                try
                {
                    var buffer = new byte[4];
                    buffer[0] = Convert.ToByte(carCoordinates.X / 2);
                    buffer[1] = Convert.ToByte(carCoordinates.Y / 2);

                    if(carReachedDestinationPoint)
                        buffer[3] = 11;
                    else
                    buffer[3] = Convert.ToByte(regionOfSpace + 1);

                    if (angle > 0)
                        buffer[2] = Convert.ToByte((180 / Math.PI) * (angle / 2));
                    else
                        buffer[2] = Convert.ToByte((90 / Math.PI) * (2 * Math.PI + angle));
                    _serialPort1.Write("px");
                    _serialPort1.Write(buffer, 0, 1);

                    _serialPort1.Write("py");
                    _serialPort1.Write(buffer, 1, 1);

                    _serialPort1.Write("pa");
                    _serialPort1.Write(buffer, 2, 1);


                    _serialPort1.Write("ps");
                    _serialPort1.Write(buffer, 3, 1);

                    SendedData += " px" + buffer[0] + " py" + buffer[1] + " pa" + buffer[2] + " ps" + buffer[3];
                }
                catch { }
            }
        }

        static ControlSignal KeyboardStateToControlSignal(KeyboardState keys)
        {
            if (keys.Up && !keys.Down && keys.Left && !keys.Right)
                return ControlSignal.ForwardLeft;
            if (keys.Up && !keys.Down && !keys.Left && !keys.Right)
                return ControlSignal.Forward;
            if (keys.Up && !keys.Down && !keys.Left && keys.Right)
                return ControlSignal.ForwardRight;
            if (!keys.Up && keys.Down && !keys.Left && !keys.Right)
                return ControlSignal.Backward;
            if (!keys.Up && keys.Down && keys.Left && !keys.Right)
                return ControlSignal.BackwardLeft;
            if (!keys.Up && keys.Down&& !keys.Left && keys.Right)
                return ControlSignal.BackwardRight;
            return ControlSignal.Stop;
        }

        public bool KeyboardDataProcessing(int keyValue, bool keyUp, ControlType controlType, MovementType movementType)
        {
            if (controlType != ControlType.ManualControl || movementType != MovementType.Continious)
                return false;

            switch (keyValue)
            {
                case 37: if (_keys.Left == keyUp) { _keys.Left = !keyUp; Send_Data((KeyboardStateToControlSignal(_keys))); return true; } break;
                case 38: if (_keys.Up == keyUp) { _keys.Up = !keyUp; Send_Data(KeyboardStateToControlSignal(_keys)); return true; } break;
                case 39: if (_keys.Right == keyUp) { _keys.Right = !keyUp; Send_Data(KeyboardStateToControlSignal(_keys)); return true; } break;
                case 40: if (_keys.Down == keyUp) { _keys.Down = !keyUp; Send_Data(KeyboardStateToControlSignal(_keys)); return true; } break;
            }
            return false;
        }

        public void KeyboardDataProcessing(char keyValue, ControlType controlType, MovementType movementType)
        {
            if (controlType != ControlType.ManualControl || movementType != MovementType.Quantized)
                return;

            switch (keyValue)
            {
                case '7': { Send_Data(ControlSignal.ForwardLeft); break; }
                case '8': { Send_Data(ControlSignal.Forward); break; }
                case '9': { Send_Data(ControlSignal.ForwardRight); break; }
                case '4': { Send_Data(ControlSignal.BackwardLeft); break; }
                case '5': { Send_Data(ControlSignal.Backward); break; }
                case '6': { Send_Data(ControlSignal.BackwardRight); break; }
            }
        }
    }
}
