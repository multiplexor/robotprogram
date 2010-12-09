using System;
using System.Drawing;

namespace spatial_and_logical_robot_program
{
    /// <summary>
    /// Класс, занимающийся отрисовкой текущей ситуации: положение объекта в пространстве, состояние его датчиков
    /// </summary>
    class CarDrawing
    {
        /// <summary>
        /// bitmap, в котором сначала рисуется изображение, а затем выводится на экран
        /// </summary>
        readonly Bitmap _bitmap;
        /// <summary>
        /// массив структур ПРЯМОУГОЛЬНИК, в котором хрянятся координаты точек для рисования сенсоров. Каждый ПРЯМУГОЛЬНИК хранит в себе две точки для рисования отрезка, изображающего данный сенсор
        /// </summary>
        readonly Rectangle[] _pointsForSensorsDraw;
        /// <summary>
        /// массив структур ПРЯМОУГОЛЬНИК, в котором хрянятся координаты точек для рисования кнопок.   Каждый ПРЯМУГОЛЬНИК хранит в себе две точки для рисования отрезка, изображающего данную кнопку
        /// </summary>
        readonly Rectangle[] _pointsForButtonsDraw;
        /// <summary>
        /// контур объекта
        /// </summary>
        readonly Rectangle _carContour;
        /// <summary>
        /// контур объекта для заливки
        /// </summary>
        readonly Rectangle _carContourForFill;
        /// <summary>
        /// прямоугольная площадка (ограничивает территорию, доступную для передвижения объекта)
        /// </summary>
        readonly Rectangle _testingGround;
        /// <summary>
        /// направляющий маркер
        /// </summary>
        readonly Point[] _directionMarker;
        readonly Pen _sensorDrawPen;
        readonly Pen _buttonDrawPen;
        readonly Pen _buttonDrawPen1;
        Graphics _gr;

        public Point[] PointsForRouteDraw;
        public int PointsQuantity;

        public CarDrawing()
        {
            _bitmap = new Bitmap(548, 706);

            _pointsForSensorsDraw = new Rectangle[16]      
            {
            new Rectangle(2, -48, 8, -48),      // сенсор№1
            new Rectangle(35, -35, 31, -41),    // сенсор№2
            new Rectangle(42, -16, 42, -10),    // сенсор№3
            new Rectangle(42, -2, 42, -8),      // сенсор№4
            new Rectangle(42, 2, 42, 8),        // сенсор№5
            new Rectangle(42, 16, 42, 10),      // сенсор№6
            new Rectangle(35, 35, 31, 41),      // сенсор№7
            new Rectangle(2, 48, 8, 48),        // сенсор№8
            new Rectangle(-2, 48, -8, 48),      // сенсор№9
            new Rectangle(-35, 35, -31, 41),    // сенсор№10
            new Rectangle(-42, 16, -42, 10),    // сенсор№11
            new Rectangle(-42, 2, -42, 8),      // сенсор№12
            new Rectangle(-42, -2, -42, -8),    // сенсор№13
            new Rectangle(-42, -16, -42, -10),  // сенсор№14
            new Rectangle(-35, -35, -31, -41),  // сенсор№15
            new Rectangle(-2, -48, -8, -48)     // сенсор№16
            };

            _pointsForButtonsDraw = new Rectangle[10]      
            {
            new Rectangle(10, -18, 10, -22),    // кнопка№1
            new Rectangle(13, -16, 15, -16),    // кнопка№2
            new Rectangle(13, 0, 15, 0),        // кнопка№3
            new Rectangle(13, 16, 15, 16),      // кнопка№4
            new Rectangle(10, 19, 10, 22),      // кнопка№5
            new Rectangle(-10, 19, -10, 22),    // кнопка№6
            new Rectangle(-12, 16, -15, 16),    // кнопка№7
            new Rectangle(-12, 0, -15, 0),      // кнопка№8
            new Rectangle(-12, -16, -15, -16),  // кнопка№9
            new Rectangle(-10, -18, -10, -22)   // кнопка№10
            };

            _carContour = new Rectangle(-12, -18, 24, 36);

            _carContourForFill = new Rectangle(-12, -18, 25, 37);

            _testingGround = new Rectangle(80, 80, 388, 546);

            _directionMarker = new Point[2]
            {
                new Point(0, 0),
                new Point(0, -18)
            };

            _sensorDrawPen = new Pen(Color.Red, 2);
            _buttonDrawPen = new Pen(Color.Red, 3);
            _buttonDrawPen1 = new Pen(Color.Green, 3);

            PointsForRouteDraw = new Point[10000];
        }

        public void CarDraw(Graphics graph, CarState carState, Point destinationPoint, int distanceToDestinationPointLimit, bool carReachedDestPoint)
        {
            _gr = Graphics.FromImage(_bitmap);
            destinationPoint = CarMovement.CoordinatesTransformFromTrainingGroundToPictureBox(destinationPoint);

            PointsForRouteDraw[PointsQuantity] = new Point(carState.Coordinates.X * 2 + 80, (-carState.Coordinates.Y + 273) * 2 + 80);
            PointsQuantity++;
            if (PointsQuantity >= 2)
            {
                for (int i = 0; i < PointsQuantity - 1; i++)
                {
                    _gr.DrawLine(new Pen(Color.Green, 1), PointsForRouteDraw[i], PointsForRouteDraw[i + 1]);
                }
            }
            if (!carReachedDestPoint)
            {
                // рисует точку назначения, к которой должен двигаться объект
                _gr.DrawLine(new Pen(Color.Red, 3), destinationPoint, new Point(destinationPoint.X + 2, destinationPoint.Y + 2));
                // рисует окружность (зону, в которую должен попасть объект)
                _gr.DrawEllipse(new Pen(Color.Red, 1), destinationPoint.X - 2*distanceToDestinationPointLimit, destinationPoint.Y - 2*distanceToDestinationPointLimit, 4 * distanceToDestinationPointLimit, 4 * distanceToDestinationPointLimit);
            }
            else
            {
                _gr.DrawLine(new Pen(Color.Green, 3), destinationPoint, new Point(destinationPoint.X + 2, destinationPoint.Y + 2));
                _gr.DrawEllipse(new Pen(Color.Green, 1), destinationPoint.X - 2*distanceToDestinationPointLimit, destinationPoint.Y - 2*distanceToDestinationPointLimit, 4 * distanceToDestinationPointLimit, 4 * distanceToDestinationPointLimit);
            } 
            // прямоугольная площадка (ограничивает территорию, доступную для передвижения объекта)
            _gr.DrawRectangle(new Pen(Color.Black), _testingGround);
            _gr.TranslateTransform(carState.Coordinates.X * 2 + 80, (-carState.Coordinates.Y + 273) * 2 + 80);
            _gr.RotateTransform(Convert.ToInt32(-(carState.Angle * 180 / Math.PI - 90)));
            _gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            // контур машины
            _gr.DrawRectangle(new Pen(Color.Green), _carContour);
            foreach (bool state in carState.Buttons)
            {
                if (!state) continue;
                //если нажата одна из кнопок на машине
                _gr.FillRectangle(new SolidBrush(Color.Red), _carContourForFill);
                break;
            }
            // направляющий маркер машины
            _gr.DrawLine(new Pen(Color.Black, 6), _directionMarker[0], _directionMarker[1]);
            //button_draw_pen.DashCap = System.Drawing.Drawing2D.DashCap.Round;

            for (int i = 0; i < 16; i++)
            {
                if (carState.Obstacles[i])
                {
                    _gr.DrawLine(_sensorDrawPen, _pointsForSensorsDraw[i].X, _pointsForSensorsDraw[i].Y, _pointsForSensorsDraw[i].Width, _pointsForSensorsDraw[i].Height);
                }
            }
            for (int i = 0; i < 10; i++)
            {
                _gr.DrawLine(carState.Buttons[i] ? _buttonDrawPen : _buttonDrawPen1, _pointsForButtonsDraw[i].X,
                             _pointsForButtonsDraw[i].Y, _pointsForButtonsDraw[i].Width, _pointsForButtonsDraw[i].Height);
            }
           // gr.DrawString(points_quantity.ToString(), new Font("Arial", 10), System.Drawing.Brushes.Red, new Point(30, 30));
            graph.DrawImage(_bitmap, 0, 0);
            _gr.Clear(Color.White);
        }
    }
}