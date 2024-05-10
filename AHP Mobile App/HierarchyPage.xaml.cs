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
            parentNode = node;
            this.BindingContext = parentNode;
        }

        private async void OnChildNodeTapped(object sender, ItemTappedEventArgs e)
        {
            // e.Item is var item: Confirms that the item exists and assigns it to item
            // item.GetType().GetProperty("Name")?.GetValue(item) is string childName: Gets the Name property of the item
            // ? : checks if the left-hand side is null before accessing the right-hand side
            if (e.Item is var item && item.GetType().GetProperty("Name")?.GetValue(item) is string childName)
            {
                // Clear the selection
                ((ListView)sender).SelectedItem = null;

                // Finds the Node with the given name
                Node childNode = FindNodeByName(App.HierarchyData, childName);
                if (childNode != null)
                {
                    // Navigates to a new HierarchyPage with the found node
                    await Navigation.PushAsync(new HierarchyPage(childNode));
                }
            }
        }


        // IEnumerable : A non-generic collection of objects that can be enumerated.
        private Node FindNodeByName(IEnumerable<Node> nodes, string name)
        {
            foreach (var node in nodes)
            {
                // If the node's found immediately, returs it
                if (node.Name == name) return node;

                if (node.Children != null)
                {
                    foreach (var childName in node.Children)
                    {

                        Node foundNode = null;
                        foreach (var n in nodes)
                        {
                            if (n.Name == childName)
                            {
                                foundNode = n;
                                break; // Exits the loop once we have found the node
                            }
                        }

                        // If a node is found, we then recursively call FindNodeByName
                        if (foundNode != null)
                        {
                            // new[] { foundNode } : Creates an array with a single element as FindNodeByName method expects an IEnumerable<Node>
                            var found = FindNodeByName(new[] { foundNode }, name);
                            if (found != null) return found; // If the recursive call found the node, return it
                        }
                    }
                }
            }

            return null; // If no node is found, returns null
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Sets button visibility based on the presence of children
            EvaluateButton.IsVisible = parentNode.Children != null && parentNode.Children.Any();

            // Only update the ListView if the Evaluate button is visible (i.e., there are children)
            if (EvaluateButton.IsVisible)
            {
                UpdateListViewItemsSource();
            }
            else
            {
                // Clear the ListView for leaf nodes
                ListView1.ItemsSource = new List<object>();
            }
        }

        private void UpdateListViewItemsSource()
        {
            if (parentNode.LocalPriorities != null && parentNode.LocalPriorities.Any())
            {
                // uses LINQ to transform the children and their priorities into a separate type
                // LINQ : Language Integrated Query - resembles SQL queries
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
                // Handles the case where LocalPriorities might not be evaluated yet
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
            // Navigates to PairwiseComparisonPage, pass the parentNode for which to evaluate
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