using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KursyWalut
{
    class Record
    {
        DateTime date;
        float price;

        public DateTime Date
        {
            get { return date; }
            set { date = value; }
        }
        public float Price
        {
            get { return price; }
            set { price = value; }
        }

        public Record(DateTime _date, float _price)
        {
            date = _date;
            price = _price;
        }

        public int DaysFrom(DateTime start)
        {
            //System.TimeSpan diff = date.Subtract(start);
            return (int)date.Subtract(start).TotalDays;
        }
    }
}
