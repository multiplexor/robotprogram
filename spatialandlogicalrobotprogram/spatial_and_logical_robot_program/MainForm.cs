using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;
using DShowNET;
using DShowNET.Device;
// для сериализации в двоичном формате
using System.Runtime.Serialization.Formatters.Binary; 

namespace spatial_and_logical_robot_program
{
    public partial class Form1 : Form, ISampleGrabberCB
    {
        /// <summary>
        /// таймер, сначала задает задержку перед началом захвата фреймов с камеры, затем задает частоту обновления DSR(отчет) 
        /// </summary>
        readonly Timer timer = new Timer();
        // для определения первого события таймера
        bool _firstTick = true;
        bool _cameraPositioningMode;
        /// <summary>
        /// хранит в себе количество распознанных кадров в единицу времени
        /// </summary>
        int _updateRate;
        byte _callNumber;
        bool _makeSnapshot;
        int _failedRecognitions;

        //System.Timers.Timer timer1 = new System.Timers.Timer(100);

        // ссылка на метод, позволяющий установить значение элемента формы TextBox потокобезопасным методом
        delegate void SetTextCallback(string text, bool txtBox3);

        /// <summary> flag to detect first Form appearance </summary>
        private bool _firstActive;

        /// <summary> base filter of the actually used video devices. </summary>
        private IBaseFilter _capFilter;

        /// <summary> graph builder interface. </summary>
        private IGraphBuilder graphBuilder;

        /// <summary> capture graph builder interface. </summary>
        private ICaptureGraphBuilder2 _capGraph;

        private ISampleGrabber SampGrabber;
        /// <summary> control interface. </summary>
        private IMediaControl mediaCtrl;

        /// <summary> event interface. </summary>
        private IMediaEventEx _mediaEvt;

        /// <summary> video window interface. </summary>
        private IVideoWindow _videoWin;

        /// <summary> grabber filter interface. </summary>
        private IBaseFilter _baseGrabFlt;

        /// <summary> structure describing the bitmap to grab. </summary>
        private VideoInfoHeader _videoInfoHeader;
        //private bool captured = true;
        //private int bufferedSize;

        /// <summary> buffer for bitmap data. </summary>
        private byte[] _savedArray;

        /// <summary> list of installed video devices. </summary>
        private ArrayList _capDevices;

        private const int WM_GRAPHNOTIFY = 0x00008001;	// message from graph

        private const int WS_CHILD = 0x40000000;	// attributes for video window
        private const int WS_CLIPCHILDREN = 0x02000000;
        private const int WS_CLIPSIBLINGS = 0x04000000;

#if DEBUG
        private int rotCookie = 0;

#endif

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                CloseInterfaces();

                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        private int Start()
        {
            if (!DsUtils.IsCorrectDirectXVersion())
            {
                MessageBox.Show(this, @"DirectX 8.1 NOT installed!", @"DirectShow.NET", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return -1;
            }

            if (!DsDev.GetDevicesOfCat(FilterCategory.VideoInputDevice, out _capDevices))
            {
                MessageBox.Show(this, @"No video capture devices found!", @"DirectShow.NET", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return -1;
            }

            DsDevice dev;
            if (_capDevices.Count == 1)
            dev = _capDevices[0] as DsDevice;
            else
            {
                var selector = new DeviceSelector(_capDevices);
                selector.ShowDialog();
                dev = selector.SelectedDevice;
            }
            if (dev == null)
            {
                return -1;
            }

            if (!StartupVideo(dev.Mon))
                return -1;
            return 0;
        }

        bool SetupGraph()
        {
            int hr;
            try
            {
                hr = _capGraph.SetFiltergraph(graphBuilder);
                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);

                hr = graphBuilder.AddFilter(_capFilter, "Ds.NET Video Capture Device");
                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);

                DsUtils.ShowCapPinDialog(_capGraph, _capFilter, this.Handle);

                var media = new AMMediaType
                                        {
                                            majorType = MediaType.Video,
                                            subType = MediaSubType.RGB24,
                                            formatType = FormatType.VideoInfo
                                        };
                hr = SampGrabber.SetMediaType(media);
                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);

                hr = graphBuilder.AddFilter(_baseGrabFlt, "Ds.NET Grabber");
                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);

                Guid cat = PinCategory.Capture;
                Guid med = MediaType.Video;
                hr = _capGraph.RenderStream(ref cat, ref med, _capFilter, null, _baseGrabFlt); // baseGrabFlt 
                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);

                media = new AMMediaType();
                hr = SampGrabber.GetConnectedMediaType(media);
                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);
                if ((media.formatType != FormatType.VideoInfo) || (media.formatPtr == IntPtr.Zero))
                    throw new NotSupportedException("Unknown Grabber Media Format");

                _videoInfoHeader = (VideoInfoHeader)Marshal.PtrToStructure(media.formatPtr, typeof(VideoInfoHeader));
                Marshal.FreeCoTaskMem(media.formatPtr); media.formatPtr = IntPtr.Zero;

                hr = SampGrabber.SetBufferSamples(false);
                if (hr == 0)
                    hr = SampGrabber.SetOneShot(false);
                if (hr == 0)
                    hr = SampGrabber.SetCallback(null, 0);
                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);

                return true;
            }
            catch
            {
                return false;
            }
        }

        bool StartupVideo(UCOMIMoniker mon)
        {
            int hr;
            try
            {
                if (!CreateCaptureDevice(mon))
                    return false;

                if (!GetInterfaces())
                    return false;

                if (!SetupGraph())
                    return false;

#if DEBUG
                DsROT.AddGraphToRot(graphBuilder, out rotCookie);		// graphBuilder capGraph
#endif

                hr = mediaCtrl.Run();
                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);

                bool hasTuner = DsUtils.ShowTunerPinDialog(_capGraph, _capFilter, this.Handle);

                return true;
            }
            catch
            {
                return false;
            }
        }

        bool GetInterfaces()
        {
            Type comType = null;
            object comObj = null;
            try
            {
                comType = Type.GetTypeFromCLSID(Clsid.FilterGraph);
                if (comType == null)
                    throw new NotImplementedException(@"DirectShow FilterGraph not installed/registered!");
                comObj = Activator.CreateInstance(comType);
                graphBuilder = (IGraphBuilder)comObj; comObj = null;

                Guid clsid = Clsid.CaptureGraphBuilder2;
                Guid riid = typeof(ICaptureGraphBuilder2).GUID;
                comObj = DsBugWO.CreateDsInstance(ref clsid, ref riid);
                _capGraph = (ICaptureGraphBuilder2)comObj; comObj = null;

                comType = Type.GetTypeFromCLSID(Clsid.SampleGrabber);
                if (comType == null)
                    throw new NotImplementedException(@"DirectShow SampleGrabber not installed/registered!");
                comObj = Activator.CreateInstance(comType);
                SampGrabber = (ISampleGrabber)comObj; comObj = null;

                mediaCtrl = (IMediaControl)graphBuilder;
                _videoWin = (IVideoWindow)graphBuilder;
                _mediaEvt = (IMediaEventEx)graphBuilder;
                _baseGrabFlt = (IBaseFilter)SampGrabber;
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (comObj != null)
                    Marshal.ReleaseComObject(comObj); comObj = null;
            }
        }

        bool CreateCaptureDevice(UCOMIMoniker mon)
        {
            object capObj = null;
            try
            {
                Guid gbf = typeof(IBaseFilter).GUID;
                mon.BindToObject(null, null, ref gbf, out capObj);
                _capFilter = (IBaseFilter)capObj; capObj = null;
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (capObj != null)
                    Marshal.ReleaseComObject(capObj); capObj = null;
            }

        }

        public void CloseInterfaces()
        {
            int hr;
            try
            {
#if DEBUG
                if (rotCookie != 0)
                    DsROT.RemoveGraphFromRot(ref rotCookie);
#endif

                if (mediaCtrl != null)
                {
                    hr = mediaCtrl.Stop();
                    mediaCtrl = null;
                }

                if (_mediaEvt != null)
                {
                    hr = _mediaEvt.SetNotifyWindow(IntPtr.Zero, WM_GRAPHNOTIFY, IntPtr.Zero);
                    _mediaEvt = null;
                }

                if (_videoWin != null)
                {
                    hr = _videoWin.put_Visible(DsHlp.OAFALSE);
                    hr = _videoWin.put_Owner(IntPtr.Zero);
                    _videoWin = null;
                }

                _baseGrabFlt = null;
                if (SampGrabber != null)
                    Marshal.ReleaseComObject(SampGrabber); SampGrabber = null;

                if (_capGraph != null)
                    Marshal.ReleaseComObject(_capGraph); _capGraph = null;

                if (graphBuilder != null)
                    Marshal.ReleaseComObject(graphBuilder); graphBuilder = null;

                if (_capFilter != null)
                    Marshal.ReleaseComObject(_capFilter); _capFilter = null;

                if (_capDevices != null)
                {
                    foreach (DsDevice d in _capDevices)
                        d.Dispose();
                    _capDevices = null;
                }
            }
            catch
            { }
        }

        void OnGraphNotify()
        {
            DsEvCode code;
            int p1, p2, hr = 0;
            do
            {
                hr = _mediaEvt.GetEvent(out code, out p1, out p2, 0);
                if (hr < 0)
                    break;
                hr = _mediaEvt.FreeEventParams(code, p1, p2);
            }
            while (hr == 0);
        }

        int ISampleGrabberCB.SampleCB(double SampleTime, IMediaSample pSample)
        {
            Trace.WriteLine("!!CB: ISampleGrabberCB.SampleCB");
            return 0;
        }

        int ISampleGrabberCB.BufferCB(double SampleTime, IntPtr pBuffer, int BufferLen)
        {
            if (backgroundWorker1.IsBusy)
                return 0;
           if ((pBuffer != IntPtr.Zero) && (BufferLen > 1000))
           {
               Marshal.Copy(pBuffer, _savedArray, 0, BufferLen);
           }
           backgroundWorker1.RunWorkerAsync();
            return 0;
        }

/// <summary>
/// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/// </summary>
/// 
        Car car;
        // КОНСТРУКТОР ФОРМЫ
        public Form1()
        {
            InitializeComponent();

            timer.Tick += timer_elapsed;
            car = new Car {Graphics = Graphics.FromHwnd(pictureBox2.Handle)};

            remoteControl1TypeToolStripMenuItem.Enabled = car.CarState.ControlType != ControlType.AutomaticRemoteControlType1;
            remoteControl2TypeToolStripMenuItem.Enabled = car.CarState.ControlType != ControlType.AutomaticRemoteControlType2;
            offlineControlToolStripMenuItem.Enabled = car.CarState.ControlType != ControlType.AutomaticOffLineControl;
            manualToolStripMenuItem.Enabled = car.CarState.ControlType != ControlType.ManualControl;
            continiousToolStripMenuItem.Enabled = car.CarState.MovementType != MovementType.Continious;
            quantizedToolStripMenuItem.Enabled = car.CarState.MovementType != MovementType.Quantized;

            textBox1.Text = "SPEED\n\r" + car.CarState.Speed.ToString();
            trackBar1.Value = car.CarState.Speed;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
           CloseInterfaces();
        }

        private void Form1_Activated(object sender, EventArgs e)
        {
            if (_firstActive)
                return;
            _firstActive = true;
            int result = Start();
            if(result<0)
            Close();
            timer.Interval = 2000;
            timer.Enabled = true;
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = car.DataTransmit.KeyboardDataProcessing(e.KeyValue,false, car.CarState.ControlType, car.CarState.MovementType);
        }

        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            car.DataTransmit.KeyboardDataProcessing(e.KeyChar, car.CarState.ControlType, car.CarState.MovementType);
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            car.DataTransmit.KeyboardDataProcessing(e.KeyValue, true, car.CarState.ControlType, car.CarState.MovementType);
        }

        private void trackBar1_MouseUp(object sender, MouseEventArgs e)
        {
            car.SetNewOptions(Convert.ToByte(trackBar1.Value));
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            textBox1.Text = @"SPEED
" + trackBar1.Value;
        }

        // Элементы главного меню 
        private void continiousToolStripMenuItem_Click(object sender, EventArgs e)
        {

            if (!quantizedToolStripMenuItem.Enabled)
            {
                continiousToolStripMenuItem.Enabled = false;
                quantizedToolStripMenuItem.Enabled = true;
                car.SetNewOptions(MovementType.Continious);
            }
        }

        private void quantizedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!continiousToolStripMenuItem.Enabled)
            {
                continiousToolStripMenuItem.Enabled = true;
                quantizedToolStripMenuItem.Enabled = false;
                car.SetNewOptions(MovementType.Quantized);
            }
        }

        private void manualToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool oneItemNotEnabled=false;
            foreach (ToolStripItem item in automaticToolStripMenuItem.DropDownItems)
            {
                if (!item.Enabled)
                {
                    oneItemNotEnabled = true;
                    break;
                }
            }
            if (oneItemNotEnabled)
            {
                foreach (ToolStripItem item in automaticToolStripMenuItem.DropDownItems)
                    item.Enabled = true;
                manualToolStripMenuItem.Enabled = false;
                car.SetNewOptions(ControlType.ManualControl);
            }
        }

        private void offlineControlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool oneItemNotEnabled = false;
            foreach (ToolStripItem item in automaticToolStripMenuItem.DropDownItems)
            {
                if (!item.Enabled)
                {
                    oneItemNotEnabled = true;
                    break;
                }
            }

            if (!manualToolStripMenuItem.Enabled||oneItemNotEnabled)
            {
                foreach (ToolStripItem item in automaticToolStripMenuItem.DropDownItems)
                    item.Enabled = true;
                offlineControlToolStripMenuItem.Enabled  = false;
                manualToolStripMenuItem.Enabled = true;
                car.SetNewOptions(ControlType.AutomaticOffLineControl);
            }
        }

        private void remoteControl1TypeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool one_item_not_enabled = false;
            foreach (ToolStripItem item in automaticToolStripMenuItem.DropDownItems)
            {
                if (!item.Enabled)
                {
                    one_item_not_enabled = true;
                    break;
                }
            }

            if (!manualToolStripMenuItem.Enabled||one_item_not_enabled)
            {
                foreach (ToolStripItem item in automaticToolStripMenuItem.DropDownItems)
                    item.Enabled = true;
                remoteControl1TypeToolStripMenuItem.Enabled = false;
                manualToolStripMenuItem.Enabled = true;
                car.SetNewOptions(ControlType.AutomaticRemoteControlType1);
            }
        }

        private void remoteControl2TypeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool one_item_not_enabled = false;
            foreach (ToolStripItem item in automaticToolStripMenuItem.DropDownItems)
            {
                if (!item.Enabled)
                {
                    one_item_not_enabled = true;
                    break;
                }
            }

            if (!manualToolStripMenuItem.Enabled || one_item_not_enabled)
            {
                foreach (ToolStripItem item in automaticToolStripMenuItem.DropDownItems)
                    item.Enabled = true;
                remoteControl2TypeToolStripMenuItem.Enabled = false;
                manualToolStripMenuItem.Enabled = true;
                car.SetNewOptions(ControlType.AutomaticRemoteControlType2);
            }
        }

        private void showPreviewWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (showPreviewWindowToolStripMenuItem.Checked)
            {
                pictureBox3.Visible = false;
                showPreviewWindowToolStripMenuItem.Checked = false;
            }
            else
            {
                pictureBox3.Visible = true;
                showPreviewWindowToolStripMenuItem.Checked = true;
            }
        }

        private void cameraSetupModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (cameraSetupModeToolStripMenuItem.Checked)
            {
                Graphics gr = Graphics.FromHwnd(this.Handle);
                gr.Clear(Color.LightGray);
                _cameraPositioningMode = false;
                cameraSetupModeToolStripMenuItem.Checked = false;
                groupBox1.Visible = true;
                groupBox2.Visible = true;
                richTextBox1.Visible = true;
            }
            else
            {
                _cameraPositioningMode = true;
                cameraSetupModeToolStripMenuItem.Checked = true;
                groupBox1.Visible = false;
                groupBox2.Visible = false;
                richTextBox1.Visible = false;
            }
        }

        private void pictureBox2_MouseMove(object sender, MouseEventArgs e)
        {
            toolStripStatusLabel1.Text = "coordinates " + CarMovement.CoordinatesTransformFromPictureBoxToTrainingGround(new Point(e.X,e.Y)).ToString();
        }

        private void pictureBox2_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            car.SetNewOptions(CarMovement.CoordinatesTransformFromPictureBoxToTrainingGround(new Point(e.X, e.Y)));
        }

        private void pictureBox3_MouseDoubleClick(object sender, MouseEventArgs e)
        {
           car.MyRoute.PointAdd(car.CoordsTransform.coordinatesTransform( (new Point(2*e.X, (int)(2.133*e.Y))), false));
        }

        private void showTransmittingDaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (showTransmittingDaToolStripMenuItem.Checked)
            {
                showTransmittingDaToolStripMenuItem.Checked = false;
                groupBox2.Visible = false;
            }
            else
            {
                showTransmittingDaToolStripMenuItem.Checked = true;
                groupBox2.Visible = true;
            }
        }

        private void showToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (showToolStripMenuItem.Checked)
            {
                showToolStripMenuItem.Checked = false;
                richTextBox1.Visible = false;
            }
            else
            {
                showToolStripMenuItem.Checked = true;
                richTextBox1.Visible = true;
            }
        }
        // Элементы главного меню

        void timer_elapsed(object source, EventArgs e)
        {
            if (_firstTick)
            {
                car.SendDefaults();
                _savedArray = new byte[_videoInfoHeader.BmiHeader.ImageSize];
                int hr = SampGrabber.SetCallback(this, 1);
                _firstTick = false;
                timer.Interval = 100;
            }

            _callNumber++;
            if (_callNumber % 10 ==0)
            {
                toolStripStatusLabel2.Text = @"recognition update rate " + _updateRate + @" fps";
                _updateRate = 0;
                _callNumber = 0;
            }

            if (car.DataTransmit.ReceivedData != "")
                SetText(car.DataTransmit.ReceivedData, true);

            if (car.Dsr != "")
                richTextBox1.Rtf = car.Dsr;

            if (car.DataTransmit.SendedData != "")
                SetText(car.DataTransmit.SendedData, false);

            car.DataTransmit.ReceivedData = "";
            car.DataTransmit.SendedData = "";
            car.Dsr = "";
        }

        void SetText(string text, bool txtBox3)
        {
            if (txtBox3)
            {
                if (textBox3.InvokeRequired)
                {
                    SetTextCallback d = new SetTextCallback(SetText);
                    this.Invoke(d, new object[] { text, txtBox3 });
                }
                else
                {
                    this.textBox3.Text += text;
                }
            }
            else
            {
                if (textBox2.InvokeRequired)
                {
                    SetTextCallback d = new SetTextCallback(SetText);
                    this.Invoke(d, new object[] { text, txtBox3 });
                }
                else
                {
                    this.textBox2.Text += text;
                }
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
                if (!_cameraPositioningMode)
                {
                    try
                    {
                        car.ObjectRecognition.Recognize(ref _savedArray, new Size(_videoInfoHeader.BmiHeader.Width, _videoInfoHeader.BmiHeader.Height));
                        _updateRate++;
                    }
                    catch (ApplicationException exception) { _failedRecognitions++; toolStripStatusLabel3.Text = @"quantity of non recognized frames = "+_failedRecognitions.ToString(); }
                }
                // создается bitmap из массива байтов
                int w = _videoInfoHeader.BmiHeader.Width;
                int h = _videoInfoHeader.BmiHeader.Height;
                int stride = w * 3;
                GCHandle handle = GCHandle.Alloc(_savedArray, GCHandleType.Pinned);
                int scan0 = (int)handle.AddrOfPinnedObject();
                scan0 += (h - 1) * stride;
                var b = new Bitmap(w, h, -stride, PixelFormat.Format24bppRgb, (IntPtr)scan0);

                if (_makeSnapshot)
                {
                    b.Save(DateTime.Now.Year + "_" + DateTime.Now.Month + "_" + DateTime.Now.Day + " " +
                           DateTime.Now.Hour + "-" + DateTime.Now.Minute + "-" + DateTime.Now.Second + "-" +
                           DateTime.Now.Millisecond + ".bmp");
                    _makeSnapshot = false;
                }

                if (_cameraPositioningMode)
                {
                    Graphics gr = Graphics.FromHwnd(this.Handle);
                    Graphics gr1 = Graphics.FromImage(b);
                    gr1.DrawLine(new Pen(Color.Green, 2), 290, 105, 975, 91);    // контур площадки 
                    gr1.DrawLine(new Pen(Color.Green, 2), 975, 91, 1261, 915);   // контур площадки 
                    gr1.DrawLine(new Pen(Color.Green, 2), 1261, 915, 20, 912);   // контур площадки 
                    gr1.DrawLine(new Pen(Color.Green, 2), 20, 912, 290, 105);    // контур площадки 
                    gr1.DrawLine(new Pen(Color.Green, 2), 1030, 936, 858, 94);   // желтая линия
                    gr1.DrawLine(new Pen(Color.Green, 2), 239, 64, 220, 0);      // плинтус
                    gr1.DrawLine(new Pen(Color.Green, 2), 251, 77, 1, 644);      // ребро стены
                    gr1.DrawLine(new Pen(Color.Green, 1), 1279, 264, 1117, 0);
                    gr1.DrawLine(new Pen(Color.Green, 1), 1101, 0, 1279, 311);
                    gr.DrawImage(b, 0, 0);
                }

                if (!_cameraPositioningMode)
                {
                    Graphics gr = Graphics.FromHwnd(this.pictureBox3.Handle);
                    gr.DrawImage(b, new Rectangle(0, 0, pictureBox3.Size.Width, pictureBox3.Size.Height));
                }

                handle.Free();
                b.Dispose();
            }

        private void makeSnapsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _makeSnapshot = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FileStream r1=File.Open("path.txt", FileMode.Create, FileAccess.Write);
				if(r1!=null)
				{
					var w = new StreamWriter(r1);
					for(int i=0; i<car.CarDrawing.PointsQuantity;i++)
					{
                        Point temp = CarMovement.CoordinatesTransformFromPictureBoxToTrainingGround(car.CarDrawing.PointsForRouteDraw[i]);
						w.WriteLine(temp.X+" "+temp.Y);
					}
					w.Flush();
					r1.Close();
				}
        }

        private void button2_Click(object sender, EventArgs e)
        {
            FileStream myStream = File.Open("route.bin", FileMode.Create, FileAccess.Write);
            var myBinaryFormat = new BinaryFormatter();
            myBinaryFormat.Serialize(myStream, car.MyRoute);
            myStream.Close();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            FileStream myStream = File.Open("route.bin", FileMode.Open, FileAccess.Read);
            var myBinaryFormat = new BinaryFormatter();
            car.MyRoute = (Route)myBinaryFormat.Deserialize(myStream);
            myStream.Close();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            FileStream r1 = File.Open("checkpoints.txt", FileMode.Create, FileAccess.Write);
            if (r1 != null)
            {
                var w = new StreamWriter(r1);
                for (int i = 0; i < 50; i++)
                {

                    w.WriteLine(car.MyRoute.Route1[i].X + " " + car.MyRoute.Route1[i].Y);
                }
                w.Flush();
                r1.Close();
            }
        }
    }
}

/* private void button1_Click(object sender, EventArgs e)
 {
     int hr = sampGrabber.SetCallback(this, 1);
 }*/

/* private void button2_Click(object sender, EventArgs e)
 {
 * // чтоб отключить callback
    // hr = sampGrabber.SetCallback(null, 0);

     int w = videoInfoHeader.BmiHeader.Width;
     int h = videoInfoHeader.BmiHeader.Height;
     int stride = w * 3;
     GCHandle handle = GCHandle.Alloc(savedArray, GCHandleType.Pinned);
     int scan0 = (int)handle.AddrOfPinnedObject();
     scan0 += (h - 1) * stride;
     Bitmap b = new Bitmap(w, h, -stride, PixelFormat.Format24bppRgb, (IntPtr)scan0);
     handle.Free();
     Graphics gr = Graphics.FromHwnd(this.Handle);
     gr.DrawImage(b, new Point(0, 0));
     b.Dispose();
     savedArray = null;
 }*/
