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
using System.Xml.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace KursyWalut
{

    public sealed partial class MainPage : Page
    {

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
        public MainPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;
        }
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
        async private void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            //Tu są dobre jaja 
            //wygląda na to że po suspendzie, mimo że ładuję page history to i tak wykonuje tą funkcję więc ustawia listboxa 
            //na podstawie którego jest page history więc się automatycznie konfiguruje tylko ze zmianami z pierwszej karty

            //Przywraca po suspendzie index listboxa który był zaznaczony ładuje tabelkę i zaznacza tą datę
            if (e.PageState != null && e.PageState.ContainsKey("listBox_datySelectedIndex"))
            {
                int SelectedIndex = int.Parse(e.PageState["listBox_datySelectedIndex"].ToString());
                myTextBlock.Text = "downloading...";
                await GetDates();
                myTextBlock.Text = "finished";
                listBox_daty.SelectedIndex = SelectedIndex;
            }
        }
        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void navigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            e.PageState["listBox_datySelectedIndex"] = listBox_daty.SelectedIndex;
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

        //Tablica nazwa plików kursów walut
        String[] CurrentFileNameList;

        /// <summary>
        /// Dodaje do CurrentFileNameList nazwy plików do pobrania a024z020402
        /// Wypełnia listboxa listBox_daty
        /// </summary>
        /// <returns></returns>
        private async Task GetDates()
        {
            //Lista nazwa plików kursów walut
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
            //petla przelatuje przez każdy plik z walutą 
            for (int s = 0; s < CurrentFileNameList.Length - 1; s++)   //the last one is empty
            {
                //jeżeli nazwa pliku nie zaczyna się na a olej ten plik 
                if (!CurrentFileNameList[s].Substring(0, 1).Equals("a"))
                    continue;
                string xml_url = @"http://www.nbp.pl/kursy/xml/" + CurrentFileNameList[s] + @".xml";
                //dodaje item w postaci daty do listboxa
                listBox_daty.Items.Add("20" + CurrentFileNameList[s].Substring(5, 2) + "-" + CurrentFileNameList[s].Substring(7, 2) + "-" + CurrentFileNameList[s].Substring(9, 2));
            }
        }
        /// <summary>
        /// Pobiera kurs na dany dzień i wypełnia listBox_waluty kursami na dany dzień
        /// </summary>
        /// <param name="xml_url">plnk do pliku xml z kursami</param>
        /// <param name="formatting">czy plik jest w nowym formatowaniu czy starym</param>
        private void ProccedWithXML(String xml_url, bool formatting)
        {
            //ładuje dokument xml
            XDocument loadedXML = XDocument.Load(xml_url);
            //textbox info
            myTextBlock.Text = "Data publikacji: " + (string)loadedXML.Descendants("tabela_kursow").ElementAt(0).Element("data_publikacji");
            //robi tablice obiektów Waluta o nazwie data data 
            //04.05.2004
            if (!formatting)
            {
                var data = from query in loadedXML.Descendants("pozycja")
                           select new Waluta
                           {
                               NazwaKraju = (string)query.Element("nazwa_kraju"),
                               KodWaluty = (string)query.Element("kod_waluty"),
                               KursSredni = (string)query.Element("kurs_sredni")
                           };
                //ustawia listBox z kursami
                listBox_waluty.ItemsSource = data;
            }
            else
            {
                var data = from query in loadedXML.Descendants("pozycja")
                           select new Waluta
                           {
                               NazwaKraju = (string)query.Element("nazwa_waluty"),
                               KodWaluty = (string)query.Element("kod_waluty"),
                               KursSredni = (string)query.Element("kurs_sredni")
                           };
                //ustawia listBox z kursami
                listBox_waluty.ItemsSource = data;
            }
        }
        /// <summary>
        /// Przycisk pobierz dane 
        /// </summary>
        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            myTextBlock.Text = "downloading...";
            await GetDates();
            myTextBlock.Text = "finished";
            listBox_daty.SelectedIndex = this.listBox_daty.Items.Count - 1;
        }
        /// <summary>
        /// Zmiana daty na listboxie 
        /// powoduje pobranie nowego pliku na dany dzień i odpala funkcję 
        /// </summary>
        private void listBox_daty_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //zaznaczona data
            string tmpS = (string)listBox_daty.SelectedItem;
            bool oldVSnewFile = false;
            //tworzy nazwę pliku/iteamu z listboxa
            tmpS = tmpS.Substring(2, 2) + tmpS.Substring(5, 2) + tmpS.Substring(8, 2);
            foreach (string ss in CurrentFileNameList)
            {
                //jeżeli nie zaczyna się na A olej 
                if (!ss.Substring(0, 1).Equals("a"))
                    continue;
                //jeżeli to plik z naszej daty
                if (ss.Substring(5, 6).Equals(tmpS)) //a002z020103
                {
                    //stara czy nowe formatowanie
                    oldVSnewFile = int.Parse(ss.Substring(5, 6)) >= 40504 ? true : false;

                    infoo2.Text = ss.Substring(5, 6);
                    if (oldVSnewFile) infoo.Text = "nowa";
                    if (!oldVSnewFile) infoo.Text = "stara";

                    tmpS = ss;
                    break;
                }
            }
            ProccedWithXML(@"http://www.nbp.pl/kursy/xml/" + tmpS + @".xml", oldVSnewFile);
        }
        /// <summary>
        /// Kliknięcie na walutę
        /// </summary>
        private void listBox_waluty_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Waluta tmpS = (Waluta)listBox_waluty.SelectedItem;
            string data = (string)listBox_daty.SelectedItem;
            if (this.Frame != null)
            {
                this.Frame.Navigate(typeof(history), tmpS.KodWaluty + "###" + data);
            }
        }
        /// <summary>
        /// Przycisk Exit
        /// </summary>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }
    }
}
