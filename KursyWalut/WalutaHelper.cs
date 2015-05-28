using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KursyWalut
{
    class WalutaHelper
    {
        public string url;
        public string data;
        public string wartosc;
        public WalutaHelper(string url_, string data_, string wartosc_)
        {
            url = url_;
            data = data_;
            wartosc = wartosc_;
        }
        public WalutaHelper(string url_,string data_)
        {
            url = url_;
            data = data_;
        }
    }
}
