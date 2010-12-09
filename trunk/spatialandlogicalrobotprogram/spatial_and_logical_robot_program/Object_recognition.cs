using System;
using System.Collections;
using System.Drawing;

namespace spatial_and_logical_robot_program
{
    public class ObjectRecognized : EventArgs
    {
        public Rectangle PointsCenter;
        public ObjectRecognized(Rectangle pointsCenter)
        {
            PointsCenter = pointsCenter;
        }
    }

    public delegate void ObjectRecognizedEventHandler(object source, ObjectRecognized arg);


    class ObjectRecognition
    {
        public event ObjectRecognizedEventHandler ObjectRecognized;
        bool _firstRun;
        Point _previousObjectCenter;
        Size _bitmap;
        byte[] _rgbValues;
        int _index;

            public ObjectRecognition()
            {
                _previousObjectCenter = new Point();
                _firstRun = true;
                _bitmap = new Size();
            }

            public void Recognize(ref byte[] bytesArray, Size bitmapSize)
            {
                _rgbValues = bytesArray;
                _bitmap = bitmapSize;
                Rectangle searchZone;
                if (_firstRun)
                {
                    _firstRun = false;
                    searchZone = new Rectangle(0, 0,_bitmap.Width-1, _bitmap.Height-1);       
                }
                else
                {
                    int k = CoordinatesTransform.Interpolation(0, _bitmap.Height, _previousObjectCenter.Y, 150, 250);
                    searchZone = new Rectangle(_previousObjectCenter.X - k, _previousObjectCenter.Y - k,_previousObjectCenter.X + k, _previousObjectCenter.Y + k);
                    searchZone = CoordinatesCorrection(searchZone, _bitmap);
                }


                var pointsCenter = new Rectangle();
                Color color;
                var arrXRed = new ArrayList();
                var arrYRed = new ArrayList();
                var arrXBlue = new ArrayList();
                var arrYBlue = new ArrayList();

                for (int i = searchZone.Y; i<= searchZone.Height; i++)
                {
                    for (int j = searchZone.X; j <=searchZone.Width; j++)
                    {
                        color = GetPixel(j, i);
                        float hue = color.GetHue();
                        float saturation = color.GetSaturation();
                        float brightness = color.GetBrightness();
                        if (hue > 7 && hue < 20 && saturation > 0.7 && brightness > 0.25)
                        {
                            arrXRed.Add(j);
                            arrYRed.Add(i);
                            SetPixel(j, i, Color.Yellow);
                        }
                        if (hue > 200 && hue < 230 && saturation > 0.4 && brightness > 0.12)
                        {
                            arrXBlue.Add(j);
                            arrYBlue.Add(i);
                            SetPixel(j, i, Color.LimeGreen);
                        }
                    }
                }
                if(arrXRed.Count<=1||arrXBlue.Count<=1||arrYBlue.Count<=1||arrYRed.Count<=1)
                {
                    if (!_firstRun)
                    {
                        _firstRun = true;
                        throw new ApplicationException("Object recognition failed. Can't find red/blue points on image");
                    }
                    _firstRun = true;
                    Recognize(ref bytesArray, bitmapSize);
                    return;
                }
                arrXRed.Sort();
                arrXBlue.Sort();
                arrYRed.Sort();
                arrYBlue.Sort();

                pointsCenter.X = Convert.ToInt32(arrXRed[(arrXRed.Count - 1) / 2]);
                pointsCenter.Y = Convert.ToInt32(arrYRed[(arrYRed.Count - 1) / 2]);
                pointsCenter.Width = Convert.ToInt32(arrXBlue[(arrXBlue.Count - 1) / 2]);
                pointsCenter.Height = Convert.ToInt32(arrYBlue[(arrYBlue.Count - 1) / 2]);

                _previousObjectCenter = new Point(((pointsCenter.X + pointsCenter.Width) / 2), (pointsCenter.Y + pointsCenter.Height) / 2);

                SetPixel(pointsCenter.X, pointsCenter.Y, Color.FromArgb(0, 255, 0));
                SetPixel(pointsCenter.Width , pointsCenter.Height, Color.FromArgb(255, 0, 0));

                _rgbValues = null;
                //bytesArray = null;

                if (ObjectRecognized != null)
                ObjectRecognized(this, new ObjectRecognized(pointsCenter));
                
            }
            
            void SetPixel(int x, int y, Color color)
            {
                _index = (((_bitmap.Height - y - 1) * _bitmap.Width + x) * 3);
                _rgbValues[_index] = color.B;
                _rgbValues[_index + 1] = color.G;
                _rgbValues[_index + 2] = color.R;
            }

            Color GetPixel(int x, int y)
            {
                if (x > _bitmap.Width - 1 || y > _bitmap.Height - 1)
                    throw new ArgumentException();

                _index = (((_bitmap.Height - y - 1) * _bitmap.Width + x) * 3);
                return Color.FromArgb(_rgbValues[_index + 2], _rgbValues[_index + 1], _rgbValues[_index]);
            }

            // коррекция координат, чтоб не выходили за границы диапазона
        static Rectangle CoordinatesCorrection(Rectangle searchZone, Size imageSize)
            {
                if (searchZone.X < 0)
                    searchZone.X = 0;
                else if (searchZone.X >= imageSize.Width)
                    searchZone.X = imageSize.Width - 1;
                if (searchZone.Y < 0)
                    searchZone.Y = 0;
                else if (searchZone.Y >= imageSize.Height)
                    searchZone.Y = imageSize.Height - 1;
                if (searchZone.Width < 0)
                    searchZone.Width = 0;
                else if (searchZone.Width >= imageSize.Width)
                    searchZone.Width = imageSize.Width - 1;
                if (searchZone.Height < 0)
                    searchZone.Height = 0;
                else if (searchZone.Height >= imageSize.Height)
                    searchZone.Height = imageSize.Height - 1;
                return searchZone;

            }
    }
}
