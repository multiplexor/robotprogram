using System;
using System.Collections;
using System.Drawing;

namespace spatial_and_logical_robot_program
{
    /*class Basis
    {
       Point TrainingGround;
       Point PictureBox;
       Point CarBasis;

       public Basis()
       {
           TrainingGround = new Point();
           PictureBox = new Point();
           CarBasis = new Point();
       }
    }*/

    internal class CarMovement
    {
        public bool CarReachedDestinationPoint;
        // определяет радиус зоны точки назначения, в которую должен попасть объект
        public readonly int DistanceToDestinationPointLimit;
        public Point DestinationPoint;
        public Point DestinationPointInCarBasis;

        public int RegionOfSpace;
        public int DistanceToDestinationPoint;

        // вспомогательная точка, являющаяся цетром окружности, по которой двигался бы объект, если б он двигался вперед и вправо
        private readonly Point _auxiliaryLeftPointInCarBasis;
        // вспомогательная точка, являющаяся цетром окружности, по которой двигался бы объект, если б он двигался вперед и влево
        private readonly Point _auxiliaryRightPointInCarBasis;
        // радиус поворота машины
        private readonly int _radiusOfCurvature;

        // координаты точки, к которой должен двигаться объект в трех базисах
        //Basis DestinationPoint;

        // список движений в порядке приоритета для каждого из секторов 
        private readonly SortedList[] _priorities;
        private readonly SortedList[] _prioritiesLeha;

        // КОНСТРУКТОР
        public CarMovement()
        {
            _priorities = new SortedList[8];

            for (int i = 0; i < 8; i++)
            {
                _priorities[i] = new SortedList();
            }

            _prioritiesLeha = new SortedList[10];

            for (int i = 0; i < 10; i++)
            {
                _prioritiesLeha[i] = new SortedList();
            }

            PrioritiesInitialize();

            DistanceToDestinationPointLimit = 15;
            DestinationPoint = new Point();
            DestinationPointInCarBasis = new Point();

            _auxiliaryLeftPointInCarBasis = new Point(0, 60);
            _auxiliaryRightPointInCarBasis = new Point(0, -60);
            _radiusOfCurvature = 45;
        }

        // заполнение таблиц приоритетов
        private void PrioritiesInitialize()
        {
            _priorities[0].Add(100, ControlSignal.Forward);
            _priorities[0].Add(80, ControlSignal.ForwardRight);
            _priorities[0].Add(60, ControlSignal.ForwardLeft);
            _priorities[0].Add(40, ControlSignal.Backward);
            _priorities[0].Add(20, ControlSignal.BackwardLeft);
            _priorities[0].Add(10, ControlSignal.BackwardRight);

            _priorities[1].Add(100, ControlSignal.ForwardRight);
            _priorities[1].Add(80, ControlSignal.BackwardLeft);
            _priorities[1].Add(60, ControlSignal.Backward);
            _priorities[1].Add(40, ControlSignal.Forward);
            _priorities[1].Add(20, ControlSignal.ForwardLeft);
            _priorities[1].Add(10, ControlSignal.BackwardRight);

            _priorities[2].Add(100, ControlSignal.BackwardRight);
            _priorities[2].Add(80, ControlSignal.ForwardLeft);
            _priorities[2].Add(60, ControlSignal.BackwardLeft);
            _priorities[2].Add(40, ControlSignal.Backward);
            _priorities[2].Add(20, ControlSignal.ForwardRight);
            _priorities[2].Add(10, ControlSignal.Forward);

            _priorities[3].Add(100, ControlSignal.Backward);
            _priorities[3].Add(80, ControlSignal.BackwardLeft);
            _priorities[3].Add(60, ControlSignal.BackwardRight);
            _priorities[3].Add(40, ControlSignal.Forward);
            _priorities[3].Add(20, ControlSignal.ForwardLeft);
            _priorities[3].Add(10, ControlSignal.ForwardRight);

            _priorities[4].Add(100, ControlSignal.BackwardLeft);
            _priorities[4].Add(80, ControlSignal.ForwardRight);
            _priorities[4].Add(60, ControlSignal.BackwardRight);
            _priorities[4].Add(40, ControlSignal.Forward);
            _priorities[4].Add(20, ControlSignal.ForwardLeft);
            _priorities[4].Add(10, ControlSignal.Backward);

            _priorities[5].Add(100, ControlSignal.ForwardLeft);
            _priorities[5].Add(80, ControlSignal.BackwardRight);
            _priorities[5].Add(60, ControlSignal.BackwardLeft);
            _priorities[5].Add(40, ControlSignal.ForwardRight);
            _priorities[5].Add(20, ControlSignal.Forward);
            _priorities[5].Add(10, ControlSignal.Backward);

            _priorities[6].Add(100, ControlSignal.BackwardRight);
            _priorities[6].Add(80, ControlSignal.ForwardRight);
            _priorities[6].Add(60, ControlSignal.Forward);
            _priorities[6].Add(40, ControlSignal.Backward);
            _priorities[6].Add(20, ControlSignal.ForwardLeft);
            _priorities[6].Add(10, ControlSignal.BackwardLeft);

            _priorities[7].Add(100, ControlSignal.BackwardLeft);
            _priorities[7].Add(80, ControlSignal.ForwardLeft);
            _priorities[7].Add(60, ControlSignal.Forward);
            _priorities[7].Add(40, ControlSignal.Backward);
            _priorities[7].Add(20, ControlSignal.ForwardRight);
            _priorities[7].Add(10, ControlSignal.BackwardRight);
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            _prioritiesLeha[0].Add(100, ControlSignal.Forward);
            _prioritiesLeha[0].Add(80, ControlSignal.ForwardRight);
            _prioritiesLeha[0].Add(60, ControlSignal.ForwardLeft);
            _prioritiesLeha[0].Add(40, ControlSignal.Backward);
            _prioritiesLeha[0].Add(20, ControlSignal.BackwardLeft);
            _prioritiesLeha[0].Add(10, ControlSignal.BackwardRight);

            _prioritiesLeha[1].Add(100, ControlSignal.ForwardRight);
            _prioritiesLeha[1].Add(80, ControlSignal.Forward);
            _prioritiesLeha[1].Add(60, ControlSignal.BackwardLeft);
            _prioritiesLeha[1].Add(40, ControlSignal.BackwardRight);
            _prioritiesLeha[1].Add(20, ControlSignal.Backward);
            _prioritiesLeha[1].Add(10, ControlSignal.ForwardLeft);

            _prioritiesLeha[2].Add(100, ControlSignal.BackwardLeft);
            _prioritiesLeha[2].Add(80, ControlSignal.ForwardRight);
            _prioritiesLeha[2].Add(60, ControlSignal.BackwardRight);
            _prioritiesLeha[2].Add(40, ControlSignal.Backward);
            _prioritiesLeha[2].Add(20, ControlSignal.ForwardLeft);
            _prioritiesLeha[2].Add(10, ControlSignal.Forward);

            _prioritiesLeha[3].Add(100, ControlSignal.ForwardLeft);
            _prioritiesLeha[3].Add(80, ControlSignal.BackwardRight);
            _prioritiesLeha[3].Add(60, ControlSignal.Backward);
            _prioritiesLeha[3].Add(40, ControlSignal.BackwardLeft);
            _prioritiesLeha[3].Add(20, ControlSignal.Forward);
            _prioritiesLeha[3].Add(10, ControlSignal.ForwardRight);

            _prioritiesLeha[4].Add(100, ControlSignal.BackwardRight);
            _prioritiesLeha[4].Add(80, ControlSignal.ForwardLeft);
            _prioritiesLeha[4].Add(60, ControlSignal.Backward);
            _prioritiesLeha[4].Add(40, ControlSignal.BackwardLeft);
            _prioritiesLeha[4].Add(20, ControlSignal.Forward);
            _prioritiesLeha[4].Add(10, ControlSignal.ForwardRight);

            _prioritiesLeha[5].Add(100, ControlSignal.Backward);
            _prioritiesLeha[5].Add(80, ControlSignal.BackwardLeft);
            _prioritiesLeha[5].Add(60, ControlSignal.BackwardRight);
            _prioritiesLeha[5].Add(40, ControlSignal.ForwardLeft);
            _prioritiesLeha[5].Add(20, ControlSignal.ForwardRight);
            _prioritiesLeha[5].Add(10, ControlSignal.Forward);

            _prioritiesLeha[6].Add(100, ControlSignal.BackwardLeft);
            _prioritiesLeha[6].Add(80, ControlSignal.ForwardRight);
            _prioritiesLeha[6].Add(60, ControlSignal.ForwardLeft);
            _prioritiesLeha[6].Add(40, ControlSignal.Forward);
            _prioritiesLeha[6].Add(20, ControlSignal.BackwardRight);
            _prioritiesLeha[6].Add(10, ControlSignal.Backward);

            _prioritiesLeha[7].Add(100, ControlSignal.ForwardRight);
            _prioritiesLeha[7].Add(80, ControlSignal.BackwardLeft);
            _prioritiesLeha[7].Add(60, ControlSignal.ForwardLeft);
            _prioritiesLeha[7].Add(40, ControlSignal.Forward);
            _prioritiesLeha[7].Add(20, ControlSignal.BackwardRight);
            _prioritiesLeha[7].Add(10, ControlSignal.BackwardLeft);

            _prioritiesLeha[8].Add(100, ControlSignal.BackwardRight);
            _prioritiesLeha[8].Add(80, ControlSignal.ForwardLeft);
            _prioritiesLeha[8].Add(60, ControlSignal.Forward);
            _prioritiesLeha[8].Add(40, ControlSignal.ForwardRight);
            _prioritiesLeha[8].Add(20, ControlSignal.Backward);
            _prioritiesLeha[8].Add(10, ControlSignal.BackwardLeft);

            _prioritiesLeha[9].Add(100, ControlSignal.ForwardLeft);
            _prioritiesLeha[9].Add(80, ControlSignal.BackwardRight);
            _prioritiesLeha[9].Add(60, ControlSignal.Forward);
            _prioritiesLeha[9].Add(40, ControlSignal.ForwardRight);
            _prioritiesLeha[9].Add(20, ControlSignal.Backward);
            _prioritiesLeha[9].Add(10, ControlSignal.ForwardRight);
        }

        public int DestinationPointRegionOfSpaceForLeha(Point carCoordinates, double angle)
        {
            DestinationPointInCarBasis = CoordinatesTransformFromTrainingGroundToCarBasis(DestinationPoint,
                                                                                                     carCoordinates,
                                                                                                     angle);
            // угол, между вектором направления к точке назначения и вектором направления движения машины (в градусах)
            angle =
                Convert.ToInt32((180/Math.PI)*
                                Math.Atan2(DestinationPointInCarBasis.Y, DestinationPointInCarBasis.X));
            if (angle <= 15 && angle >= -15)
                return 0;
            if (angle >= 15 && angle <= 48)
                return 9;
            if (angle >= 48 && angle <= 90)
                return 8;
            if (angle >= 90 && angle <= 123)
                return 7;
            if (angle >= 123 && angle <= 165)
                return 6;
            if (angle >= 165 || angle <= -165)
                return 5;
            if (angle >= -165 && angle <= -132)
                return 4;
            if (angle >= -132 && angle <= -90)
                return 3;
            if (angle >= -90 && angle <= -48)
                return 2;
            if (angle >= -48 && angle <= -15)
                return 1;
            return -1;
        }

        private int DestinationPointRegionOfSpace(Point carCoordinates, double angle)
        {
            DestinationPointInCarBasis = CoordinatesTransformFromTrainingGroundToCarBasis(DestinationPoint,
                                                                                                     carCoordinates,
                                                                                                     angle);
            angle = (180/Math.PI)*Math.Atan2(DestinationPointInCarBasis.Y, DestinationPointInCarBasis.X);
            if (angle < 15 && angle > -15)
                return 0;
            if (angle > 165 || angle < -165)
                return 3;
            /*if (Math.Abs(destination_point_in_car_basis.Y) < 15)
            {
                if (destination_point_in_car_basis.X >= 0)
                {
                    return 0;
                }
                else
                {
                    return 3;
                }
            }*/
            if (
                CoordinatesTransform.SegmentLength(DestinationPointInCarBasis, _auxiliaryLeftPointInCarBasis) >=
                _radiusOfCurvature &&
                CoordinatesTransform.SegmentLength(DestinationPointInCarBasis, _auxiliaryRightPointInCarBasis) >=
                _radiusOfCurvature)
            {
                if (DestinationPointInCarBasis.X > 0 && DestinationPointInCarBasis.Y < 0)
                {
                    return 1;
                }
                if (DestinationPointInCarBasis.X > 0 && DestinationPointInCarBasis.Y > 0)
                {
                    return 5;
                }
                if (DestinationPointInCarBasis.X < 0 && DestinationPointInCarBasis.Y < 0)
                {
                    return 2;
                }
                if (DestinationPointInCarBasis.X < 0 && DestinationPointInCarBasis.Y > 0)
                {
                    return 4;
                }
            }
            else if (
                CoordinatesTransform.SegmentLength(DestinationPointInCarBasis,
                                                     _auxiliaryLeftPointInCarBasis) < _radiusOfCurvature)
            {
                return 6;
            }
            else if (
                CoordinatesTransform.SegmentLength(DestinationPointInCarBasis,
                                                     _auxiliaryRightPointInCarBasis) < _radiusOfCurvature)
            {
                return 7;
            }
            return -1;
        }

        public ControlSignal ControlSelection(Point carCoordinates, double angle, ControlType controlType)
        {
            if (CarReachedDestinationPoint)
                return ControlSignal.Stop;
            ControlSignal selectedControl = ControlSignal.Stop;
            for (int i = 5; i != -1; i--)
            {
                if (controlType == ControlType.AutomaticRemoteControlType1)
                {
                    RegionOfSpace = DestinationPointRegionOfSpaceForLeha(carCoordinates, angle);
                    selectedControl = (ControlSignal) _prioritiesLeha[RegionOfSpace].GetByIndex(i);
                    return selectedControl;
                }
                if (controlType == ControlType.AutomaticRemoteControlType2)
                {
                    RegionOfSpace = DestinationPointRegionOfSpace(carCoordinates, angle);
                    selectedControl = (ControlSignal) _priorities[RegionOfSpace].GetByIndex(i);
                    return selectedControl;
                }
                Point a = CoordinatesTransformFromCarBasisToTrainingGround(carCoordinates,
                                                                                  FutureCoordinates(selectedControl),
                                                                                  angle);
                if (a.X < 0 || a.Y < 0 || a.X > 194 || a.Y > 273)
                    continue;
                return selectedControl;
            }
            return ControlSignal.Stop;
        }

        public bool CheckReachingDestinationPoint(Point carCoordinates)
        {
            DistanceToDestinationPoint =
                (int) CoordinatesTransform.SegmentLength(DestinationPoint, carCoordinates);
            if (DistanceToDestinationPoint < DistanceToDestinationPointLimit)
            {
                CarReachedDestinationPoint = true;
                return true;
            }
            CarReachedDestinationPoint = false;
            return false;
        }

        // приблизительные координаты объекта, которые будут после отработки следующего управляющего воздействия
        private static Point FutureCoordinates(ControlSignal selectedControl)
        {
            var offset = new Point();
            switch (selectedControl)
            {
                case ControlSignal.Backward:
                    {
                        offset = new Point(-30, 0);
                        break;
                    }
                case ControlSignal.BackwardLeft:
                    {
                        offset = new Point(-15, 30);
                        break;
                    }
                case ControlSignal.BackwardRight:
                    {
                        offset = new Point(-30, -30);
                        break;
                    }
                case ControlSignal.Forward:
                    {
                        offset = new Point(30, 0);
                        break;
                    }
                case ControlSignal.ForwardLeft:
                    {
                        offset = new Point(30, 30);
                        break;
                    }
                case ControlSignal.ForwardRight:
                    {
                        offset = new Point(30, -30);
                        break;
                    }
            }
            return offset;
        }

        private static Point CoordinatesTransformFromCarBasisToTrainingGround(Point carCoordinates,
                                                                                    Point pointInCarBasis, double angle)
        {
            return
                new Point(
                    Convert.ToInt32(pointInCarBasis.X*Math.Cos(angle) - pointInCarBasis.Y*Math.Sin(angle) +
                                    carCoordinates.X),
                    Convert.ToInt32(pointInCarBasis.X*Math.Sin(angle) + pointInCarBasis.Y*Math.Cos(angle) +
                                    carCoordinates.Y));
        }

        private static Point CoordinatesTransformFromTrainingGroundToCarBasis(Point ground, Point car,
                                                                                    double angle)
        {
            return new Point(Convert.ToInt32((ground.X - car.X)*Math.Cos(angle) + (ground.Y - car.Y)*Math.Sin(angle)),
                             Convert.ToInt32((car.X - ground.X)*Math.Sin(angle) + (ground.Y - car.Y)*Math.Cos(angle)));
        }

        public static Point CoordinatesTransformFromPictureBoxToTrainingGround(Point a)
        {
            return new Point((a.X - 80)/2, 273 - (a.Y - 80)/2);
        }

        public static Point CoordinatesTransformFromTrainingGroundToPictureBox(Point a)
        {
            return new Point(a.X*2 + 80, 2*(-a.Y + 273) + 80);
        }
    }
}

// первый метод управления (4 сектора, полный отстой)
/* void OnTimedEvent1(object source, ElapsedEventArgs e)
     {
         //Console.WriteLine("The Elapsed event was raised at {0}", e.SignalTime);
         if (segment_length(destination_point, car_coordinates) < distance_to_destination_point_limit)
         {
             car_reached_destination_point = true;
             timer1.Stop();
             return;
         }
         destination_point_in_car_basis = new Point(destination_point.X - car_coordinates.X, destination_point.Y - car_coordinates.Y); // здесь происходит первый этап перехода к новой системе координат - перемещение.
         destination_point_in_car_basis = new Point(Convert.ToInt32(destination_point_in_car_basis.X * Math.Cos(angle) + destination_point_in_car_basis.Y * Math.Sin(angle)), Convert.ToInt32(-destination_point_in_car_basis.X * Math.Sin(angle) + destination_point_in_car_basis.Y * Math.Cos(angle)));// здесь происходит первый этап перехода к новой системе координат - поворот.
         if (Math.Abs(destination_point_in_car_basis.Y) < 10)
         {
             if (destination_point_in_car_basis.X >= 0)
             {
                 Send_Data("d");
                 return;
             }
             else
             {
                 Send_Data("a");
                 return;
             }
         }
         else if (destination_point_in_car_basis.X > 0 && destination_point_in_car_basis.Y < 0)
         {
             Send_Data("f");
             return;
         }
         else if (destination_point_in_car_basis.X > 0 && destination_point_in_car_basis.Y > 0)
         {
             Send_Data("e");
             return;
         }
         else if (destination_point_in_car_basis.X < 0 && destination_point_in_car_basis.Y < 0)
         {
             Send_Data("c");
             return;
         }
         else if (destination_point_in_car_basis.X < 0 && destination_point_in_car_basis.Y > 0)
         {
             Send_Data("b");
             return;
         }
     }

     // мой метод управления (необходимо потестировать)
     public void movement_to_destination_point_method_2()
     {
         if (car_reached_destination_point == true)
         {
             Send_Data("g");
             Send_Data("m");
             //return;
         }
         if (segment_length(destination_point, car_coordinates) < distance_to_destination_point_limit)
         {
             car_reached_destination_point = true;
             return;
         }
         destination_point_in_car_basis = new Point(destination_point.X - car_coordinates.X, destination_point.Y - car_coordinates.Y); // здесь происходит первый этап перехода к новой системе координат - перемещение.
         destination_point_in_car_basis = new Point(Convert.ToInt32(destination_point_in_car_basis.X * Math.Cos(angle) + destination_point_in_car_basis.Y * Math.Sin(angle)), Convert.ToInt32(-destination_point_in_car_basis.X * Math.Sin(angle) + destination_point_in_car_basis.Y * Math.Cos(angle)));// здесь происходит первый этап перехода к новой системе координат - поворот.
         if (segment_length(destination_point_in_car_basis, auxiliary_left_point_in_car_basis) >= radius_of_curvature && segment_length(destination_point_in_car_basis, auxiliary_right_point_in_car_basis) >= radius_of_curvature)
         {
             if (Math.Abs(destination_point_in_car_basis.Y) < 10)
             {
                 if (destination_point_in_car_basis.X >= 0)
                 {
                     Send_Data("m");
                     Send_Data("v");
                     return;
                 }
                 else
                 {
                     Send_Data("m");
                     Send_Data("n");
                     return;
                 }
             }
             else if (destination_point_in_car_basis.X > 0 && destination_point_in_car_basis.Y < 0)
             {
                 Send_Data("v");
                 Send_Data("r");
                 return;
             }
             else if (destination_point_in_car_basis.X > 0 && destination_point_in_car_basis.Y > 0)
             {
                 Send_Data("v");
                 Send_Data("l");
                 return;
             }
             else if (destination_point_in_car_basis.X < 0 && destination_point_in_car_basis.Y < 0)
             {
                 Send_Data("n");
                 Send_Data("r");
                 return;
             }
             else if (destination_point_in_car_basis.X < 0 && destination_point_in_car_basis.Y > 0)
             {
                 Send_Data("n");
                 Send_Data("l");
                 return;
             }
         }
         else if (segment_length(destination_point_in_car_basis, auxiliary_left_point_in_car_basis) < radius_of_curvature)
         {
             Send_Data("v");
             Send_Data("r");
         }
         else if (segment_length(destination_point_in_car_basis, auxiliary_right_point_in_car_basis) < radius_of_curvature)
         {
             Send_Data("v");
             Send_Data("l");
         }

     }*/
