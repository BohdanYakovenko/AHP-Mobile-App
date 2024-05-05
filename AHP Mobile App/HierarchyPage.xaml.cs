using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace AHP_Mobile_App
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class HierarchyPage : ContentPage
    {
        private Node parentNode;

        public HierarchyPage(Node node)
        {
            InitializeComponent();
            parentNode = node;  // Initialize parentNode with the constructor argument
            this.BindingContext = parentNode;
        }

        private async void OnChildNodeTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.Item is var item && item.GetType().GetProperty("Name")?.GetValue(item) is string childName)
            {
                // Clear the selection
                ((ListView)sender).SelectedItem = null;

                // Find the Node with the given name
                Node childNode = FindNodeByName(App.HierarchyData, childName);
                if (childNode != null)
                {
                    // Navigate to a new HierarchyPage with the found node
                    await Navigation.PushAsync(new HierarchyPage(childNode));
                }
            }
        }


        private Node FindNodeByName(IEnumerable<Node> nodes, string name)
        {
            foreach (var node in nodes)
            {
                if (node.Name == name) return node;

                if (node.Children != null)
                {
                    foreach (var childName in node.Children)
                    {
                        // Instead of using LINQ to find the first matching node,
                        // we iterate through the nodes manually.
                        Node foundNode = null;
                        foreach (var n in nodes)
                        {
                            if (n.Name == childName)
                            {
                                foundNode = n;
                                break; // Exit the loop once we've found the node
                            }
                        }

                        // If a node is found, we then recursively call FindNodeByName
                        if (foundNode != null)
                        {
                            var found = FindNodeByName(new[] { foundNode }, name);
                            if (found != null) return found; // If the recursive call found the node, return it
                        }
                    }
                }
            }

            return null; // If no node is found, return null
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Set button visibility based on the presence of children
            EvaluateButton.IsVisible = parentNode.Children != null && parentNode.Children.Any();

            // Only update the ListView if the Evaluate button is visible (i.e., there are children)
            if (EvaluateButton.IsVisible)
            {
                UpdateListViewItemsSource();
            }
            else
            {
                // Clear the ListView or handle it appropriately for leaf nodes
                ListView1.ItemsSource = new List<object>();
            }
        }

        private void UpdateListViewItemsSource()
        {
            if (parentNode.LocalPriorities != null && parentNode.LocalPriorities.Any())
            {
                var childrenWithPriorities = parentNode.Children.Select((child, index) => new
                {
                    Name = child,
                    Priority = parentNode.LocalPriorities[index].ToString("P2") // Convert to percentage string
                }).ToList();

                ListView1.ItemsSource = null;
                ListView1.ItemsSource = childrenWithPriorities;
            }
            else
            {
                // Handle the case where LocalPriorities might not be evaluated yet
                var childrenWithoutPriorities = parentNode.Children.Select(child => new
                {
                    Name = child,
                    Priority = ""
                }).ToList();

                ListView1.ItemsSource = null;
                ListView1.ItemsSource = childrenWithoutPriorities;
            }
        }



        private async void EvaluateButton_Clicked(object sender, EventArgs e)
        {
            // Navigate to PairwiseComparisonPage, pass the parentNode for which to evaluate
            if (parentNode != null)
            {
                await Navigation.PushAsync(new PairwiseComparisonPage(parentNode));
            }
            else
            {
                await DisplayAlert("Error", "No parent node is selected or available for evaluation.", "OK");
            }
        }


    }



}