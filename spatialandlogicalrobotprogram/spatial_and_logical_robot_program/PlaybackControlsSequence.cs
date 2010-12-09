using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace spatial_and_logical_robot_program
{
    class PlaybackControlsSequence
    {
        /*SortedList sequence_of_controls;
        // таймер для управления временем отправки управляющих воздействий
        System.Timers.Timer timer2 = new System.Timers.Timer();
        string[] loaded_points;
        public struct ControlAndWaitingTime
        {
            public ControlAndWaitingTime(string control, int waiting_time)
            {
                this.control = control;
                this.waiting_time = waiting_time;
            }
            string control;
            int waiting_time;
        }

        timer2.Elapsed += new ElapsedEventHandler(OnTimedEvent3);

        void PlaybackControlsSequence()
        {
            
        }

        public void Sequence_of_controls_load()
        {
            FileStream fs = File.Open("Route capture.txt", FileMode.Open, FileAccess.Read);
            if (fs == null)
                return;
            StreamReader sr = new StreamReader(fs);
            loaded_points = sr.ReadToEnd().Split('\n');
            fs.Close();
            for (int i = 0; i < loaded_points.Length - 2; i += 2)
                sequence_of_controls.Add(i, new ControlAndWaitingTime(loaded_points[i].Trim(), Convert.ToInt32(loaded_points[i + 1].Trim())));
            //timer2.Interval = ;
            timer2.Enabled = true;
        }

        void time_to_send_next_control(object source, ElapsedEventArgs e)
        {
            /* int tick;
             if (counter < loaded_points.Length - 4)
             {
                 serialPort1.Write(loaded_points[counter - 1]);
                 textBox3.Text += loaded_points[counter - 1];
                 tick = Convert.ToInt32(loaded_points[counter + 2]) - Convert.ToInt32(loaded_points[counter]);
                 if (tick == 0)
                     timer2.Interval = 2;
                 else
                     timer2.Interval = tick;
                 counter += 2;
             }
             if (counter == loaded_points.Length - 3)
             {
                 serialPort1.Write(loaded_points[counter - 1]);
                 textBox3.Text += loaded_points[counter - 1];
                 loaded_points = null;
                 timer2.Enabled = false;
             }  
        }  */
    }
}
