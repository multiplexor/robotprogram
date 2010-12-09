using System;
using System.Drawing;
using System.IO;

namespace spatial_and_logical_robot_program
{

    public struct Line
    {
        public Line(float a, float b, float c)
        {
            A = a;
            B = b;
            C = c;
        }
        public float A;
        public float B;
        public float C;
    }

    class CoordinatesTransform
    {
        Point[,] _horizontalRows;
        // 0,1 - горизонтальные ряды на изображении. 0 - верхний. 1 - нижний
        // 2,3 - соответствующие им ряды на поверхности пола
        Point[,] _verticalRows;

        Point _perspective;
        Line _line1, _line2, _line3, _line4;
        int _midpointY;
        int[] _array,_array1, _array2, _array3, _array4, _array5, _array6;

        // КОНСТРУКТОР
        public CoordinatesTransform()
        {
            ReadPointsFromFile();

            PrepareDataToCoordinatesTransform();
        }

        public Point coordinatesTransform(Point pointOnImage, bool carTop)
        {
            if(carTop)
            pointOnImage.Y += 50;

            var pointOnTheFloorSurface = new Point();
            Point intersectionWithHorizontal;
            Line parallelLine;

            // определение абсциссы точки //
            Line line5 = LineEquationCoefficients(_perspective, pointOnImage);
            
            if (pointOnImage.Y > _midpointY)
            {
                intersectionWithHorizontal = IntersectionPointOfTwoLines(line5, _line4);
                int binSearchRes = Array.BinarySearch(_array, intersectionWithHorizontal.X);
                if (binSearchRes >= 0)
                {
                    pointOnTheFloorSurface.X = _horizontalRows[binSearchRes, 3].X;
                }
                else if (~binSearchRes == 0)
                {
                    double k = SegmentLength(_horizontalRows[0, 1], intersectionWithHorizontal) / SegmentLength(_horizontalRows[0, 1], _horizontalRows[1, 1]);
                    pointOnTheFloorSurface.X = Convert.ToInt32(-k * (_horizontalRows[1, 3].X));
                }
                else if (~binSearchRes == _array.Length)
                {
                    double k = SegmentLength(_horizontalRows[_horizontalRows.GetLength(0) - 1, 1], intersectionWithHorizontal) / SegmentLength(_horizontalRows[_horizontalRows.GetLength(0) - 1, 1], _horizontalRows[_horizontalRows.GetLength(0) - 2, 1]);
                    pointOnTheFloorSurface.X = Convert.ToInt32(_horizontalRows[_horizontalRows.GetLength(0) - 1, 3].X + k * (_horizontalRows[1, 3].X));
                }
                else if (~binSearchRes < _array.Length)
                {
                    double k = SegmentLength(_horizontalRows[~binSearchRes - 1, 1], intersectionWithHorizontal) / SegmentLength(_horizontalRows[~binSearchRes - 1, 1], _horizontalRows[~binSearchRes, 1]);
                    pointOnTheFloorSurface.X = Convert.ToInt32(_horizontalRows[~binSearchRes - 1, 3].X + (_horizontalRows[~binSearchRes, 3].X - _horizontalRows[~binSearchRes - 1, 3].X) * k);
                }
            }
            else
            {
                intersectionWithHorizontal = IntersectionPointOfTwoLines(line5, _line2);
                int binSearchRes = Array.BinarySearch(_array1, intersectionWithHorizontal.X);
                if (binSearchRes >= 0)
                {
                    pointOnTheFloorSurface.X = _horizontalRows[binSearchRes, 2].X;
                }
                else if (~binSearchRes == 0)
                {
                    double k = SegmentLength(_horizontalRows[0, 0], intersectionWithHorizontal) / SegmentLength(_horizontalRows[0, 0], _horizontalRows[1, 0]);
                    pointOnTheFloorSurface.X = Convert.ToInt32(-k * (_horizontalRows[1, 2]).X);
                }
                else if (~binSearchRes == _array.Length)
                {
                    double k = SegmentLength(_horizontalRows[_horizontalRows.GetLength(0) - 1, 0], intersectionWithHorizontal) / SegmentLength(_horizontalRows[_horizontalRows.GetLength(0) - 1, 0], _horizontalRows[_horizontalRows.GetLength(0) - 2, 0]);
                    pointOnTheFloorSurface.X = Convert.ToInt32(_horizontalRows[_horizontalRows.GetLength(0) - 1, 2].X + k * (_horizontalRows[1, 2].X));
                }
                else if (~binSearchRes < _array.Length)
                {
                    double k = SegmentLength(_horizontalRows[~binSearchRes - 1, 0], intersectionWithHorizontal) / SegmentLength(_horizontalRows[~binSearchRes - 1, 0], _horizontalRows[~binSearchRes, 0]);
                    pointOnTheFloorSurface.X = Convert.ToInt32(_horizontalRows[~binSearchRes - 1, 2].X + (_horizontalRows[~binSearchRes, 2].X - _horizontalRows[~binSearchRes - 1, 2].X) * k);
                }
            }
            // определение абсциссы точки //

            // определение ординаты точки //

            int binSearchRes1 = Array.BinarySearch(_array2, pointOnImage.Y);

            if (binSearchRes1 >= 0)
                parallelLine = EquationOfParallelLine(new PointF(_array5[binSearchRes1] - _array6[binSearchRes1], _array2[binSearchRes1] - _array4[binSearchRes1]), pointOnImage);
            else if (~binSearchRes1 == _array2.Length)
                parallelLine = EquationOfParallelLine(new PointF(_array5[~binSearchRes1 - 1] - _array6[~binSearchRes1 - 1], _array2[~binSearchRes1 - 1] - _array4[~binSearchRes1 - 1]), pointOnImage);
            else
                parallelLine = EquationOfParallelLine(new PointF(_array5[~binSearchRes1] - _array6[~binSearchRes1], _array2[~binSearchRes1] - _array4[~binSearchRes1]), pointOnImage);

            int elementNumber;
            if (binSearchRes1 > 0)
                elementNumber = binSearchRes1;
            else if (binSearchRes1 == 0)
                elementNumber = 1;
            else if (~binSearchRes1 == 0)
                elementNumber = 1;
            else if (~binSearchRes1 == _array2.Length)
                elementNumber = _array2.Length - 1;
            else elementNumber = ~binSearchRes1;

            Point intersectionWithVertical = IntersectionPointOfTwoLines(_line1, parallelLine);
            pointOnTheFloorSurface.Y = Interpolation(_array2[elementNumber - 1], _array2[elementNumber], intersectionWithVertical.Y, _array3[elementNumber - 1], _array3[elementNumber]);
            // определение ординаты точки //

            return pointOnTheFloorSurface;
        }

        void ReadPointsFromFile()
        {
            FileStream r = File.Open("coordinates.txt", FileMode.Open, FileAccess.Read);
            if (r == null)
                throw new ApplicationException("File read error");

            var reader = new StreamReader(r);
            string[] points = reader.ReadLine().Split('x');
            var widthHeight = new Point(Convert.ToInt32(points[0]), Convert.ToInt32(points[1]) - 2);

            _horizontalRows = new Point[widthHeight.X, 4];
            // 0,1 - горизонтальные ряды на изображении. 0 - верхний. 1 - нижний
            // 2,3 - соответствующие им ряды на поверхности земли
            _verticalRows = new Point[widthHeight.Y, 4];
            // 0-3 - -//- для вертикальных рядов

            points = reader.ReadLine().Split(' ');
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = points[i].TrimEnd(';');
                switch (i % 4)
                {
                    case 0: _horizontalRows[Convert.ToInt32(i / 4), 0].X = Convert.ToInt32(points[i]); break;
                    case 1: _horizontalRows[Convert.ToInt32(i / 4), 0].Y = Convert.ToInt32(points[i]); break;
                    case 2: _horizontalRows[Convert.ToInt32(i / 4), 2].X = Convert.ToInt32(points[i]); break;
                    case 3: _horizontalRows[Convert.ToInt32(i / 4), 2].Y = Convert.ToInt32(points[i]); break;
                }
            }
            points = reader.ReadLine().Split(' ');
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = points[i].TrimEnd(';');
                switch (i % 4)
                {
                    case 0: _horizontalRows[Convert.ToInt32(i / 4), 1].X = Convert.ToInt32(points[i]); break;
                    case 1: _horizontalRows[Convert.ToInt32(i / 4), 1].Y = Convert.ToInt32(points[i]); break;
                    case 2: _horizontalRows[Convert.ToInt32(i / 4), 3].X = Convert.ToInt32(points[i]); break;
                    case 3: _horizontalRows[Convert.ToInt32(i / 4), 3].Y = Convert.ToInt32(points[i]); break;
                }
            }
            for (int j = 0; j < widthHeight.Y; j++)
            {
                points = reader.ReadLine().Split(' ');
                for (int i = 0; i < points.Length; i++)
                {
                    points[i] = points[i].TrimEnd(';');
                    switch (i % 8)
                    {
                        case 0: _verticalRows[j, 0].X = Convert.ToInt32(points[i]); break;
                        case 1: _verticalRows[j, 0].Y = Convert.ToInt32(points[i]); break;
                        case 2: _verticalRows[j, 2].X = Convert.ToInt32(points[i]); break;
                        case 3: _verticalRows[j, 2].Y = Convert.ToInt32(points[i]); break;
                        case 4: _verticalRows[j, 1].X = Convert.ToInt32(points[i]); break;
                        case 5: _verticalRows[j, 1].Y = Convert.ToInt32(points[i]); break;
                        case 6: _verticalRows[j, 3].X = Convert.ToInt32(points[i]); break;
                        case 7: _verticalRows[j, 3].Y = Convert.ToInt32(points[i]); break;
                    }
                }
            }
            r.Close();
        }

        void PrepareDataToCoordinatesTransform()
        {
            _perspective = new Point();

            // уравнения прямых из которых состоит прямоугольный контур площадки (line1- левая вертикальная и т.д. по часовой стрелке)
            _line1 = LineEquationCoefficients(_horizontalRows[0, 0], _horizontalRows[0, 1]);
            _line2 = LineEquationCoefficients(_horizontalRows[0, 0], _horizontalRows[_horizontalRows.GetLength(0) - 1, 0]);
            _line3 = LineEquationCoefficients(_horizontalRows[_horizontalRows.GetLength(0) - 1, 1], _horizontalRows[_horizontalRows.GetLength(0) - 1, 0]);
            _line4 = LineEquationCoefficients(_horizontalRows[0, 1], _horizontalRows[_horizontalRows.GetLength(0) - 1, 1]);

            // Перспектива
            _perspective = IntersectionPointOfTwoLines(_line1, _line3);

            _midpointY = (_horizontalRows[0, 0].Y + _horizontalRows[0, 1].Y) / 2;

            _array = new int[_horizontalRows.GetLength(0)];     // нижний горизонтальный ряд на изображении (абсциссы)
            _array1 = new int[_horizontalRows.GetLength(0)];    // верхний горизонтальный ряд на изображении (абсциссы)
            _array2 = new int[_verticalRows.GetLength(0) + 2];  // левый вертикальный ряд на изображении (ординаты)
            _array3 = new int[_verticalRows.GetLength(0) + 2];  // левый вертикальный ряд на поверхности земли (ординаты)
            _array4 = new int[_verticalRows.GetLength(0) + 2];  // правый вертикальный ряд на изображении (ординаты)
            _array5 = new int[_verticalRows.GetLength(0) + 2];  // левый вертикальный ряд на изображении (абсциссы)
            _array6 = new int[_verticalRows.GetLength(0) + 2];  // правый вертикальный ряд на изображении (абсциссы)

            for (int i = 0; i < _horizontalRows.GetLength(0); i++)
                _array[i] = _horizontalRows[i, 1].X;
            for (int i = 0; i < _horizontalRows.GetLength(0); i++)
                _array1[i] = _horizontalRows[i, 0].X;
            for (int i = 1; i < _verticalRows.GetLength(0) + 1; i++)
                _array2[i] = _verticalRows[i - 1, 0].Y;
            for (int i = 1; i < _verticalRows.GetLength(0) + 1; i++)
                _array3[i] = _verticalRows[i - 1, 2].Y;
            for (int i = 1; i < _verticalRows.GetLength(0) + 1; i++)
                _array4[i] = _verticalRows[i - 1, 1].Y;
            for (int i = 1; i < _verticalRows.GetLength(0) + 1; i++)
                _array5[i] = _verticalRows[i - 1, 0].X;
            for (int i = 1; i < _verticalRows.GetLength(0) + 1; i++)
                _array6[i] = _verticalRows[i - 1, 1].X;

            _array2[0] = _horizontalRows[0, 0].Y;
            _array2[_verticalRows.GetLength(0) + 1] = _horizontalRows[0, 1].Y;
            _array3[0] = _horizontalRows[0, 2].Y;
            _array3[_verticalRows.GetLength(0) + 1] = _horizontalRows[0, 3].Y;
            _array4[0] = _horizontalRows[_horizontalRows.GetLength(0) - 1, 0].Y;
            _array4[_verticalRows.GetLength(0) + 1] = _horizontalRows[_horizontalRows.GetLength(0) - 1, 1].Y;
            _array5[0] = _horizontalRows[0, 0].X;
            _array5[_verticalRows.GetLength(0) + 1] = _horizontalRows[0, 1].X;
            _array6[0] = _horizontalRows[_horizontalRows.GetLength(0) - 1, 0].X;
            _array6[_verticalRows.GetLength(0) + 1] = _horizontalRows[_horizontalRows.GetLength(0) - 1, 1].X;
        }

        public static int Interpolation(int a, int b, int x, int a1, int b1)
        {
            return a1 + (x - a) * (b1 - a1) / (b - a);
        }

        static Point IntersectionPointOfTwoLines(Line line1, Line line2)
        {
            var intersectionPoint = new Point();
            float d = line1.A * line2.B - line2.A * line1.B;
            if (d == 0)
                throw new ApplicationException("This two lines are parallel");
            intersectionPoint.X = Convert.ToInt32((line2.C * line1.B - line1.C * line2.B) / d);
            intersectionPoint.Y = Convert.ToInt32((line1.C * line2.A - line2.C * line1.A) / d);
            return intersectionPoint;
        }

        static Line LineEquationCoefficients(PointF a, PointF b)
        {
            var line = new Line();
            if (b.X - a.X != 0)
            {
                line.A = (b.Y - a.Y) / (b.X - a.X);
                line.B = -1;
                line.C = a.Y - a.X * (b.Y - a.Y) / (b.X - a.X);
            }
            else
            {
                line.A = 1;
                line.B = 0;
                line.C = -a.X;
            }
            return line;
        }

        static Line EquationOfParallelLine(PointF directionVector, PointF point)
        {
            var line = new Line();
            if (directionVector.X == 0)
            {
                line.A = 1;
                line.B = 0;
                line.C = -point.X;
            }
            else if (directionVector.Y == 0)
            {
                line.A = 0;
                line.B = 1;
                line.C = -point.Y;
            }
            else
            {
                line.A = 1 / directionVector.X;
                line.B = -1 / directionVector.Y;
                line.C = -point.X / directionVector.X + point.Y / directionVector.Y;
            }
            return line;
        }

        public static double SegmentLength(Point a, Point b)
        {
            return Convert.ToDouble(Math.Pow(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2), 0.5));
        }

    }
}
