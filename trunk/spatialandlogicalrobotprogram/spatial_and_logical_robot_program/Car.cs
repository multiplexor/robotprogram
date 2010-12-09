using System;
using System.Drawing;

namespace spatial_and_logical_robot_program
{
    
    class Car
    {
            public CarState CarState;
            public BluetoothDataTransmit DataTransmit;
            public CarMovement CarMovement;
            public CarDrawing CarDrawing;

            public ObjectRecognition ObjectRecognition;
            public CoordinatesTransform CoordsTransform;
            // объект Graphics, ссылающийся на pictureBox, в котором визуализируется машина
            public Graphics Graphics;
            // содержит в себе краткий отчет о текущем состоянии
            public string Dsr;
            //SortedList states;
            public Route MyRoute;

            ControlSignal _selectedControl;


            // КОНСТРУКТОР
            public Car()                                                       
            {
                CarState = new CarState();

                DataTransmit = new BluetoothDataTransmit();
                DataTransmit.DataReceived += ReceivedDataProcessing;

                CarMovement = new CarMovement();
                CarDrawing = new CarDrawing();

                ObjectRecognition = new ObjectRecognition();
                ObjectRecognition.ObjectRecognized += ObjectRecognized;

                CoordsTransform = new CoordinatesTransform();

                MyRoute = new Route();

            }

            ~Car()
            {
                DataTransmit.Send_Data(ControlSignal.Stop);
            }

            void ReceivedDataProcessing(object sender, DatareceivedArgs e)
            {
                string[] splitResult = e.Data.Split('\n');
                foreach (string t in splitResult)
                {
                    switch (t)
                    {
                            //case "eom": { dataTransmit.Send_Data(selected_control); break; }

                        case "r10": { CarState.Obstacles[0] = false; break; }
                        case "r11": { CarState.Obstacles[0] = true; break; }
                        case "r20": { CarState.Obstacles[1] = false; break; }
                        case "r21": { CarState.Obstacles[1] = true; break; }
                        case "r30": { CarState.Obstacles[2] = false; break; }
                        case "r31": { CarState.Obstacles[2] = true; break; }
                        case "r40": { CarState.Obstacles[3] = false; break; }
                        case "r41": { CarState.Obstacles[3] = true; break; }
                        case "r50": { CarState.Obstacles[4] = false; break; }
                        case "r51": { CarState.Obstacles[4] = true; break; }
                        case "r60": { CarState.Obstacles[5] = false; break; }
                        case "r61": { CarState.Obstacles[5] = true; break; }
                        case "r70": { CarState.Obstacles[6] = false; break; }
                        case "r71": { CarState.Obstacles[6] = true; break; }
                        case "r80": { CarState.Obstacles[7] = false; break; }
                        case "r81": { CarState.Obstacles[7] = true; break; }
                        case "l10": { CarState.Obstacles[8] = false; break; }
                        case "l11": { CarState.Obstacles[8] = true; break; }
                        case "l20": { CarState.Obstacles[9] = false; break; }
                        case "l21": { CarState.Obstacles[9] = true; break; }
                        case "l30": { CarState.Obstacles[10] = false; break; }
                        case "l31": { CarState.Obstacles[10] = true; break; }
                        case "l40": { CarState.Obstacles[11] = false; break; }
                        case "l41": { CarState.Obstacles[11] = true; break; }
                        case "l50": { CarState.Obstacles[12] = false; break; }
                        case "l51": { CarState.Obstacles[12] = true; break; }
                        case "l60": { CarState.Obstacles[13] = false; break; }
                        case "l61": { CarState.Obstacles[13] = true; break; }
                        case "l70": { CarState.Obstacles[14] = false; break; }
                        case "l71": { CarState.Obstacles[14] = true; break; }
                        case "l80": { CarState.Obstacles[15] = false; break; }
                        case "l81": { CarState.Obstacles[15] = true; break; }

                        case "b10": { CarState.Buttons[0] = false; break; }
                        case "b11": { CarState.Buttons[0] = true; break; }
                        case "b20": { CarState.Buttons[1] = false; break; }
                        case "b21": { CarState.Buttons[1] = true; break; }
                        case "b30": { CarState.Buttons[2] = false; break; }
                        case "b31": { CarState.Buttons[2] = true; break; }
                        case "b40": { CarState.Buttons[3] = false; break; }
                        case "b41": { CarState.Buttons[3] = true; break; }
                        case "b50": { CarState.Buttons[4] = false; break; }
                        case "b51": { CarState.Buttons[4] = true; break; }
                        case "a10": { CarState.Buttons[5] = false; break; }
                        case "a11": { CarState.Buttons[5] = true; break; }
                        case "a20": { CarState.Buttons[6] = false; break; }
                        case "a21": { CarState.Buttons[6] = true; break; }
                        case "a30": { CarState.Buttons[7] = false; break; }
                        case "a31": { CarState.Buttons[7] = true; break; }
                        case "a40": { CarState.Buttons[8] = false; break; }
                        case "a41": { CarState.Buttons[8] = true; break; }
                        case "a50": { CarState.Buttons[9] = false; break; }
                        case "a51": { CarState.Buttons[9] = true; break; }
                    }
                }
            }

        void ObjectRecognized(object source, ObjectRecognized arg)
            {
            Point a = CoordsTransform.coordinatesTransform(new Point(arg.PointsCenter.X, arg.PointsCenter.Y), true);
                var b = new Point(arg.PointsCenter.Width, arg.PointsCenter.Height);
                b = CoordsTransform.coordinatesTransform(b, true);
                CarState.Coordinates = new Point((a.X + b.X) / 2, (a.Y + b.Y) / 2);
                CarState.Angle = Math.Atan2(a.Y - b.Y, a.X - b.X);

                if (CarMovement.CheckReachingDestinationPoint(CarState.Coordinates))
                    CarMovement.DestinationPoint = MyRoute.GetNewDestinationPoint();

                _selectedControl = CarMovement.ControlSelection(CarState.Coordinates, CarState.Angle, CarState.ControlType);
                if (CarState.MovementType == MovementType.Continious && (CarState.ControlType == ControlType.AutomaticRemoteControlType1||CarState.ControlType == ControlType.AutomaticRemoteControlType2))
                {
                    if (CarMovement.DistanceToDestinationPoint < 60)
                    {
                       // dataTransmit.Send_Data(178);
                    }
                    else
                    {
                        DataTransmit.Send_Data(CarState.Speed);
                    }
                    DataTransmit.Send_Data(_selectedControl);
                }
                else if (CarState.ControlType == ControlType.AutomaticOffLineControl)
                {
                    if (CarMovement.DistanceToDestinationPoint < 60)
                    {
                        //dataTransmit.Send_Data(178);
                    }
                    DataTransmit.SendRecognizedData(CarState.Coordinates, CarState.Angle, CarMovement.DestinationPointRegionOfSpaceForLeha(CarState.Coordinates, CarState.Angle), CarMovement.CarReachedDestinationPoint);
                }
                CarDrawing.CarDraw(Graphics, CarState.MakeCopy(CarState), CarMovement.DestinationPoint, CarMovement.DistanceToDestinationPointLimit, CarMovement.CarReachedDestinationPoint);
                Dsr = MakeDsr();
            }

            public void SetNewOptions(byte speed)
            {
                DataTransmit.Send_Data(speed);
                CarState.Speed = speed;
            }

            public void SetNewOptions(MovementType movementType)
            {
                DataTransmit.Send_Data(movementType);
                CarState.MovementType = movementType;
                if(movementType== MovementType.Quantized)
                DataTransmit.Send_Data(_selectedControl);
            }

            public void SetNewOptions(ControlType controlType)
            {
                DataTransmit.Send_Data(controlType);
                CarState.ControlType = controlType;
            }

            public void SetNewOptions(Point destinationPoint)
            {
                
            }

            public void SendDefaults()
            {
                DataTransmit.Send_Data(CarState.ControlType);
                DataTransmit.Send_Data(CarState.MovementType);
                DataTransmit.Send_Data(CarState.Speed);
            }

        const string ColorTable = @"{\colortbl;\red0\green0\blue0;" +     //1 black
                                    @"\red255\green0\blue0;" +          //2 red          
                                    @"\red0\green128\blue0;}";          //3 green

            string MakeDsr()
            {
                string rtf = @"{\rtf\ansi" + ColorTable + @"\deftab700\cf1 Car coordinates " + @"\cf2 " + CarState.Coordinates + @"\cf1\tab\tab angle = " + @"\cf2 " + Convert.ToInt32((180 / Math.PI) * CarState.Angle) + @"\cf1\tab\tab car " + (CarMovement.CarReachedDestinationPoint ? @"\cf3 reached " : @"\cf2 is far away from ") + @"\cf1 destination point" + @"\line" +
                             @"\cf1 Dest.point:ground basis " + @"\cf2 " + CarMovement.DestinationPoint + @"\cf1\tab car basis " + @"\cf2" + CarMovement.DestinationPointInCarBasis + @"\cf1\tab Dest.point sector " + @"\cf2 " + CarMovement.RegionOfSpace + @"\line" +
                            @"\cf1 object distance = " + @"\cf2 " + CarMovement.DistanceToDestinationPoint + @"\cf1\tab\tab destination zone radius = " + @"\cf2 " + CarMovement.DistanceToDestinationPointLimit + 
 @"}";
                return rtf;
            }


    }
}