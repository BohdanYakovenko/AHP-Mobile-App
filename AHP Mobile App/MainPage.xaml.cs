using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace AHP_Mobile_App
{
    public partial class MainPage : ContentPage
    {
        public MainMenuItem[] mainMenuItems;
        public MainPage()
        {
            InitializeComponent();
            mainMenuItems = new MainMenuItem[2];
            mainMenuItems[0] = new MainMenuItem { Title = "Hierarchy", Description = "Browse and evaluate" };
            mainMenuItems[1] = new MainMenuItem { Title = "Decision alternatives", Description = "View rating scores" };
            ListView1.ItemsSource = mainMenuItems;

        }

        void ListView1_ItemTapped(System.Object sender, Xamarin.Forms.ItemTappedEventArgs e)
        {
            MainMenuItem item = (MainMenuItem)e.Item;
            if (item.Title == "Hierarchy")
            {
                HierarchyPage p = new HierarchyPage(App.HierarchyData.First());
                Navigation.PushAsync(p);
            }
            else if (item.Title == "Decision alternatives")
            {
                DecisionAlternativesPage p = new DecisionAlternativesPage();
                Navigation.PushAsync(p);
            }
        }
    }

    public class MainMenuItem
    {
        public string Title { get; set; }
        public string Description { get; set; }
    }
}
