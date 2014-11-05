using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Retweet_Rolling_Window
{
    class Utilities
    {
        public int checkTime(DateTime _time, int _time_window)
        {
            //check if time left is positive or negative as a condition
            DateTime cur = DateTime.Now;
            TimeSpan timespan = cur - _time;
            double diff = timespan.TotalSeconds - (double)_time_window;

            return (int)diff;
        }

        public DateTime calculateTime(int _time)
        {
            //calculate remaining time left
            DateTime deletion_time = DateTime.Now.Add(TimeSpan.FromSeconds(_time));
            return deletion_time;
        }
    }
}
