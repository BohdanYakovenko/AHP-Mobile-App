using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.Net.Http;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace AHP_Mobile_App
{
    public partial class App : Application
    {
        public static List<Node> HierarchyData { get; private set; }

        public App()
        {
            InitializeComponent();
            MainPage = new NavigationPage(new MainPage());
        }

        private async Task LoadHierarchyDataAsync()
        {
            var url = "https://personalpages.manchester.ac.uk/staff/grigory.pishchulov/Hierarchy.json";
            using (var client = new HttpClient())
            {
                var response = await client.GetStringAsync(url);
                HierarchyData = JsonConvert.DeserializeObject<List<Node>>(response);
            }

            MainPage = new NavigationPage(new HierarchyPage(HierarchyData.First()));
        }

        protected override async void OnStart()
        {
            await LoadHierarchyDataAsync();
            if (HierarchyData != null && HierarchyData.Any())
            {
                MainPage = new NavigationPage(new MainPage());
            }
            else
            {
                MainPage.DisplayAlert("Error", "Failed to load hierarchy data.", "OK");
            }
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }

    public class Node
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> Children { get; set; }
        public List<double> LocalPriorities { get; set; } = new List<double>();
    }
}
