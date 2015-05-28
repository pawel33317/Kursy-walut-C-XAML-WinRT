using KursyWalut.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Net.Http;
using System.Xml.Linq;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.System.Threading;
using Windows.UI.Input.Inking;
using Windows.UI;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Media.Imaging;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;

namespace KursyWalut
{
    public sealed partial class history : Page
    {
        //do wykresu
        InkManager _inkManager = new Windows.UI.Input.Inking.InkManager();
        int leftDrawingMargin = 40;
        int rightDrawingMargin = 20;
        int topDrawingMargin = 20;
        int bottomDrawingMargin = 40;

        String[] CurrentFileNameList;//nazwy plików a024z020304
        String[] separator = new String[] { "###" };//separator daty i nazwy waluty
        int LastselectedIndex = -1;
        /// <summary>
        /// ma 3 pola
        /// link do pliku
        /// date
        /// i wartosc = null; jak jest potrzebna do wykresu to jest pobierana
        /// </summary>
        List<WalutaHelper> files = new List<WalutaHelper>();

        String SYMBOL_WALUTY;
        //data początkowa wykresu z pierwszego listboxa 
        String CurrentDate;
        List<Record> records = new List<Record>();

        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        public history()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;
        }
        private void navigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            e.PageState["SYMBOL_WALUTY"] = SYMBOL_WALUTY;
            e.PageState["CurrentDate"] = CurrentDate;
            e.PageState["LastselectedIndex"] = LastselectedIndex;
        }
        #region NavigationHelper registration

        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// 
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="GridCS.Common.NavigationHelper.LoadState"/>
        /// and <see cref="GridCS.Common.NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion
        /// <summary>
        /// Populates the page with content passed during navigation. Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session. The state will be null the first time a page is visited.</param>
        private void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            //data i nazwa waluty pobrana z poprzedniej strony
            String data = e.NavigationParameter as String;
            //jeżeli aplikacja była suspend i data zostałą zmieniona na drugiej karcie 
            //Przywraca po suspendzie datę początkową i nazwę waluty
            if (e.PageState != null && e.PageState.ContainsKey("SYMBOL_WALUTY") && e.PageState.ContainsKey("CurrentDate"))
            {
                data = e.PageState["SYMBOL_WALUTY"].ToString() + separator[0] + e.PageState["CurrentDate"].ToString();
            }
            //jeżeli zaznaczona data końcowa inna niż standardowa 
            if (e.PageState != null && e.PageState.ContainsKey("LastselectedIndex"))
            {
                LastselectedIndex = int.Parse(e.PageState["LastselectedIndex"].ToString());
            }
            initt(data);
        }
        /// <summary>
        /// Pobiera nazwy plikó z kursami
        /// Ustawia daty końcową i początkową
        /// </summary>
        /// <param name="data">Data i nazwa waluty oddzielone ###</param>
        async private void initt(String data)
        {
            //dzieli data i nazwe waluty
            String[] splitted = data.Split(separator, StringSplitOptions.None);
            if (!string.IsNullOrWhiteSpace(data))
            {
                pageTitle.Text = "Historia Waluty " + splitted[0];
                SYMBOL_WALUTY = splitted[0];
                CurrentDate = splitted[1];
            }
            else
            {
                pageTitle.Text = "ERROR :-(";
            }
            datainfo.Text = CurrentDate;
            String responseBody;

            //HTTP klient
            HttpClient client = new HttpClient();
            //Zapytanie tylu GET
            HttpResponseMessage response = await client.GetAsync(new System.Uri("http://www.nbp.pl/kursy/xml/dir.txt"));
            response.EnsureSuccessStatusCode();
            //Przypisanie do zmiennej listy listy nazw plików z kursami oddzielone przez \n
            responseBody = await response.Content.ReadAsStringAsync();
            //lista z nazwami plików oddzielona ze stringa 
            CurrentFileNameList = responseBody.Split('\n');
            for (int s = 0; s < CurrentFileNameList.Length; s++)   //the last one is empty
                CurrentFileNameList[s] = CurrentFileNameList[s].Trim('\r');

            int startDateSelectedIndex = 0;
            bool startDateSelectedIndexB = true;
            //petla przelatuje przez każdy plik z walutą 
            for (int s = 0; s < CurrentFileNameList.Length - 1; s++)   //the last one is empty
            {
                //jeżeli nazwa pliku nie zaczyna się na a olej ten plik 
                if (!CurrentFileNameList[s].Substring(0, 1).Equals("a"))
                    continue;

                string xml_url = @"http://www.nbp.pl/kursy/xml/" + CurrentFileNameList[s] + @".xml";
                //data początkowa
                listBox_daty.Items.Add("20" + CurrentFileNameList[s].Substring(5, 2) + "-" + CurrentFileNameList[s].Substring(7, 2) + "-" + CurrentFileNameList[s].Substring(9, 2));
                //data końcowa
                listBox_datystop.Items.Add("20" + CurrentFileNameList[s].Substring(5, 2) + "-" + CurrentFileNameList[s].Substring(7, 2) + "-" + CurrentFileNameList[s].Substring(9, 2));

                //jeżeli ta data co była zaznaczona na poprzedniej stronie 
                if ("20" + CurrentFileNameList[s].Substring(5, 2) + "-" + CurrentFileNameList[s].Substring(7, 2) + "-" + CurrentFileNameList[s].Substring(9, 2) == CurrentDate)
                {
                    listBox_daty.SelectedIndex = this.listBox_daty.Items.Count - 1;
                    startDateSelectedIndexB = false;
                }
                //oblicza index daty startowej
                if (startDateSelectedIndexB)
                {
                    startDateSelectedIndex++;
                }
                files.Add(new WalutaHelper(xml_url, CurrentFileNameList[s].Substring(5, 6)));
            }
            //ustawia datę startową
            listBox_daty.SelectedIndex = listBox_daty.Items.Count - (listBox_daty.Items.Count - startDateSelectedIndex);
            //ustawia datę końcową czyli startowa + 10 dni lub ostatnia else przywróci z suspended
            if (LastselectedIndex < 0)
            {
                int tmppp = ((listBox_datystop.Items.Count - (listBox_datystop.Items.Count - startDateSelectedIndex + 10) + 10) < (listBox_datystop.Items.Count - 1)) ? (listBox_datystop.Items.Count - (listBox_datystop.Items.Count - startDateSelectedIndex - 10)) : (listBox_datystop.Items.Count - 1);
                listBox_datystop.SelectedIndex = tmppp;
            }
            else
            {
                listBox_datystop.SelectedIndex = LastselectedIndex;
            }
            iloscElementow.Text = "Elementów: " + files.Count;
        }
        /// <summary>
        /// pobiera kurs waluty
        /// </summary>
        /// <param name="xml_url">link do pliku z kursem</param>
        /// <returns></returns>
        private string ProccedWithXML(String xml_url)
        {
            //ładuje dokument xml
            XDocument loadedXML = XDocument.Load(xml_url);
            //textbox info
            //myTextBlock.Text = "Data publikacji: " + (string)loadedXML.Descendants("tabela_kursow").ElementAt(0).Element("data_publikacji");

            //W liście bedzie tylko 1 obiekt string który będzie posiadał wartość waluty z danego dnia
            var data = from query in loadedXML.Descendants("pozycja")
                       where (string)query.Element("kod_waluty") == SYMBOL_WALUTY
                       select (string)query.Element("kurs_sredni");
            foreach (String w in data)
            {
                return w;
            }
            return "ERROR";
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        //interfejs w którym będzie odpalone pobieranie plików i ładowanie punktów do rysowania
        IAsyncAction m_workItem;

        /// <summary>
        /// ładuje potrzebne pliki do narysowania wykresu w osobnym wątku
        /// </summary>
        private void Button_Click_1(object sender, RoutedEventArgs e)//ładowanie wszystkich plików
        {
            //jak już odpalony to zabij
            if (m_workItem != null)
            {
                m_workItem.Cancel();
            }
            int start = listBox_daty.SelectedIndex;
            int stop = listBox_datystop.SelectedIndex;
            int allFileToLoad = stop - start + 1;
            pgbar.Minimum = 1;
            pgbar.Maximum = allFileToLoad;


            //odpalamy asynchronicznie żeby nie zawieszać aplikacji
            m_workItem = ThreadPool.RunAsync((workItem) =>
                {
                    //czyści listę punktów do rysowania
                    records.Clear();
                    //info ile elementów do załadowania
                    CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                      CoreDispatcherPriority.High, new DispatchedHandler(() =>
                      {
                          iloscElementow.Text = "Elementów: " + allFileToLoad.ToString();
                      }));

                    int ti = 0;
                    foreach (WalutaHelper walH in files)
                    {
                        if (ti >= start && ti <= stop)
                        {
                            //jeżeli jeszcze nie ma pobranej wartości to ją pobierz
                            if (walH.wartosc == null)
                            {
                                walH.wartosc = ProccedWithXML(@"" + walH.url);
                                //info1.Text += "[licze]" + walH.wartosc + "[/licze]";
                            }
                            int rok = int.Parse("20" + walH.data.Substring(0, 2));
                            int miesiac = int.Parse(walH.data.Substring(2, 2));
                            int dzien = int.Parse(walH.data.Substring(4, 2));
                            //dodaj do listy rysowanych punktów jeżeli pobbrało ok CZYLI JEZELI WALUTA ISTNIEJE !!!!!!!!!!!!!!
                            if (walH.wartosc != "ERROR")
                                records.Add(new Record(new DateTime(rok, miesiac, dzien), float.Parse(walH.wartosc.Replace(",", "."))));
                            else
                            {
                                CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                                    CoreDispatcherPriority.High, new DispatchedHandler(() =>
                                    {
                                        datainfostop.Text = "Podana waluta nie istnieje w podanym okresie czasu";
                                    }));
                                break;
                            }
                            //progress bar i ile załadowano
                            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                                CoreDispatcherPriority.High, new DispatchedHandler(() =>
                                {
                                    zaladowane.Text = "Zaladowane: " + (ti - start).ToString();
                                    pgbar.Value = ti - start;

                                }));
                        }
                        else if (ti > stop) { break; }
                        ti++;
                    }
                    CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                        CoreDispatcherPriority.High, new DispatchedHandler(() =>
                        {
                            panelcanvas.Children.Clear();
                            DrawAxis(Colors.Green, 4.0);
                            DrawCurrencyHistory(Colors.Red, 6.0);
                        }));
                });
        }
        /// <summary>
        /// Zapisuje canvas jako jpg
        /// </summary>
        /// <param name="canvas">obiekt kanvas który będzie zapisany do obrazka</param>
        /// <returns></returns>
        private async Task CreateSaveBitmapAsync(Canvas canvas)
        {
            if (canvas != null)
            {
                //tworzy bitmape
                RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap();
                await renderTargetBitmap.RenderAsync(canvas);
                var pixels = await renderTargetBitmap.GetPixelsAsync();
                //filepicker wybieramy gdzie zapisać
                var picker = new FileSavePicker();
                picker.FileTypeChoices.Add("JPEG Image", new string[] { ".jpg" });
                StorageFile file = await picker.PickSaveFileAsync();
                info1.Text = canvas.ActualWidth + "---" + canvas.ActualHeight;
                if (file != null)
                {
                    using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
                        //var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.BmpEncoderId, stream);
                        byte[] bytes = pixels.ToArray();
                        encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)canvas.ActualWidth, (uint)canvas.ActualHeight, 96, 96, bytes);
                        await encoder.FlushAsync();
                    }
                }
            }
        }
        /// <summary>
        /// Zmiana daty początkowej
        /// </summary>
        private void listBox_daty_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CurrentDate = (string)listBox_daty.SelectedItem;
            datainfo.Text = "Data początkowa: " + CurrentDate;
        }
        /// <summary>
        /// Zmiana daty końcowej
        /// </summary>
        private void listBox_datstopy_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            String CurrentDates = (string)listBox_datystop.SelectedItem;
            LastselectedIndex = listBox_datystop.SelectedIndex;
            datainfostop.Text = "Data końcowa: " + CurrentDates;
        }
        /// <summary>
        /// Zapisz obraz
        /// </summary>
        async private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            CreateSaveBitmapAsync(panelcanvas);
        }
        void GetMinMax(out double min, out double max)
        {
            min = 0;
            max = 0;
            if (records.Count < 2)
                return;
            min = records[0].Price;
            max = records[0].Price;
            foreach (Record r in records)
            {
                if (r.Price < min)
                    min = r.Price;
                if (r.Price > max)
                    max = r.Price;
            }
        }
        void DrawAxis(Color _col, double _thick)
        {
            panelcanvas.Children.Add(new Line()
            {
                X1 = leftDrawingMargin,
                Y1 = topDrawingMargin,
                X2 = leftDrawingMargin,
                Y2 = panelcanvas.RenderSize.Height - bottomDrawingMargin + 10,
                StrokeThickness = _thick,
                Stroke = new SolidColorBrush(_col)
            });
            panelcanvas.Children.Add(new Line()
            {
                X1 = leftDrawingMargin,
                Y1 = topDrawingMargin,
                X2 = leftDrawingMargin - 10,
                Y2 = topDrawingMargin + 10,
                StrokeThickness = _thick,
                Stroke = new SolidColorBrush(_col)
            });
            panelcanvas.Children.Add(new Line()
            {
                X1 = leftDrawingMargin,
                Y1 = topDrawingMargin,
                X2 = leftDrawingMargin + 10,
                Y2 = topDrawingMargin + 10,
                StrokeThickness = _thick,
                Stroke = new SolidColorBrush(_col)
            });
            panelcanvas.Children.Add(new Line()
            {
                X1 = leftDrawingMargin - 10,
                Y1 = panelcanvas.RenderSize.Height - bottomDrawingMargin,
                X2 = panelcanvas.RenderSize.Width - rightDrawingMargin,
                Y2 = panelcanvas.RenderSize.Height - bottomDrawingMargin,
                StrokeThickness = _thick,
                Stroke = new SolidColorBrush(_col)
            });
            panelcanvas.Children.Add(new Line()
            {
                X1 = panelcanvas.RenderSize.Width - rightDrawingMargin,
                Y1 = panelcanvas.RenderSize.Height - bottomDrawingMargin,
                X2 = panelcanvas.RenderSize.Width - rightDrawingMargin - 10,
                Y2 = panelcanvas.RenderSize.Height - bottomDrawingMargin + 10,
                StrokeThickness = _thick,
                Stroke = new SolidColorBrush(_col)
            });
            panelcanvas.Children.Add(new Line()
            {
                X1 = panelcanvas.RenderSize.Width - rightDrawingMargin,
                Y1 = panelcanvas.RenderSize.Height - bottomDrawingMargin,
                X2 = panelcanvas.RenderSize.Width - rightDrawingMargin - 10,
                Y2 = panelcanvas.RenderSize.Height - bottomDrawingMargin - 10,
                StrokeThickness = _thick,
                Stroke = new SolidColorBrush(_col)
            });
        }
        void DrawLevel(Color _col, double _y, string _val)
        {
            panelcanvas.Children.Add(new Line()
            {
                X1 = leftDrawingMargin - 4,
                Y1 = _y,
                X2 = panelcanvas.RenderSize.Width - rightDrawingMargin - 10,
                Y2 = _y,
                StrokeThickness = 1.0,
                Stroke = new SolidColorBrush(_col)
            });
            TextBlock textBlock = new TextBlock();
            textBlock.Text = _val;
            textBlock.Foreground = new SolidColorBrush(Colors.Blue);
            Canvas.SetLeft(textBlock, leftDrawingMargin - 25);
            Canvas.SetTop(textBlock, _y - 7);
            panelcanvas.Children.Add(textBlock);
        }
        void DrawCurrencyHistory(Color _col, double _thick)
        {
            if (records.Count < 2)
                return;
            double Cmin, Cmax;
            GetMinMax(out Cmin, out Cmax);
            double Xmin = 2 * leftDrawingMargin;
            double Xmax = panelcanvas.RenderSize.Width - (leftDrawingMargin + rightDrawingMargin);
            double Ymin = 2 * topDrawingMargin;
            double Ymax = panelcanvas.RenderSize.Height - (topDrawingMargin + bottomDrawingMargin);
            double stepX = (Xmax - Xmin) / (records.Count - 1);
            double stepY = (Ymax - Ymin) / (Cmax - Cmin);

            Record prev = null;
            int i = 0;
            foreach (Record r in records)
            {
                if (prev == null)
                {
                    prev = records[0];
                    continue;
                }
                i++;
                panelcanvas.Children.Add(new Line()
                {
                    X1 = Xmin + (i - 1) * stepX,
                    Y1 = -1 * (prev.Price - Cmin) * stepY + Ymax,
                    X2 = Xmin + i * stepX,
                    Y2 = -1 * (r.Price - Cmin) * stepY + Ymax,
                    StrokeThickness = _thick,
                    Stroke = new SolidColorBrush(_col)
                });
                prev = r;
            }
            DrawLevel(Colors.Blue, Ymax, Cmin.ToString("0.00"));
            DrawLevel(Colors.Blue, Ymin, Cmax.ToString("0.00"));
        }
    }
}

