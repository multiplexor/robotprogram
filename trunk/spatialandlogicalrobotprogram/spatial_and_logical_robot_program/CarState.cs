using System;
using System.Drawing;

namespace spatial_and_logical_robot_program
{
    public enum ControlSignal
    {
        Forward,
        ForwardRight,
        ForwardLeft,
        Backward,
        BackwardLeft,
        BackwardRight,
        Stop
    };

    public enum ControlType
    {
        AutomaticOffLineControl,
        AutomaticRemoteControlType1,
        AutomaticRemoteControlType2,
        ManualControl
    };

    public enum MovementType { Continious, Quantized };

    public struct Location
    {
        public Point Coordinates;
        public double Angle;
        public Location(Point coordinates, double angle)
        {
            Coordinates = coordinates;
            Angle = angle;
        }
    }

    public class CarState
    {
        // значение true/false означает, что состояние данной кнопки - НАЖАТА/НЕ НАЖАТА
        public bool[] Buttons;
        // значение true/false означает, что состояние данного датчика - ЕСТЬ ПРЕПЯТСТВИЕ/НЕТ ПРЕПЯТСТВИЯ
        public bool[] Obstacles;
        // координаты объекта
        public Point Coordinates;
        // угол поворота объекта относительно оси абсцисс. В радианах.
        public double Angle;
        // 0 - минимальная скорость, 255 - максимальная. Величина нелинейная.
        public byte Speed;
        public ControlType ControlType;
        public MovementType MovementType;

        public CarState()
        {
            Buttons = new bool[10];
            Obstacles = new bool[16];
            Coordinates = new Point();
            Speed = 173;
            ControlType = ControlType.ManualControl;
            MovementType = MovementType.Continious;
        }

        public CarState MakeCopy(CarState carState)
        {
            var carStateCopy = new CarState();
            Array.Copy(Buttons, carStateCopy.Buttons, 10);
            Array.Copy(Obstacles, carStateCopy.Obstacles, 16);
            carStateCopy.Coordinates = Coordinates;
            carStateCopy.Angle = Angle;
            carStateCopy.Speed = Speed;
            carStateCopy.ControlType = ControlType;
            carStateCopy.MovementType = MovementType;
            return carStateCopy;
        }
    }
}


                /*if (value >= 0 && value <= 255)
                {
                    Send_Data(value);
                    speed = value;
                }
                else throw new ApplicationException("Speed out-of-range exception");*/

 /*public Control_type ControlType
        {
            get { return control_type; }
            set
            {
                if ((control_type == Control_type.Automatic_off_line_control && value != Control_type.Automatic_off_line_control) || (control_type != Control_type.Automatic_off_line_control && value == Control_type.Automatic_off_line_control))
                {
                    Send_Data(value);
                }
                control_type = value;
            }
        }*/
        /*public Control_type ControlType
        {
            get { return control_type; }
            set
            {
                if ((control_type == Control_type.Automatic_off_line_control && value != Control_type.Automatic_off_line_control) || (control_type != Control_type.Automatic_off_line_control && value == Control_type.Automatic_off_line_control))
                {
                    Send_Data(value);
                }
                control_type = value;
            }
        }*/