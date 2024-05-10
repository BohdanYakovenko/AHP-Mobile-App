using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace AHP_Mobile_App
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DecisionAlternativesPage : ContentPage
    {
        // Holds the list of nodes that will be binded to ListView
        private List<Node> readyToDisplayNodes = new List<Node>();

        public DecisionAlternativesPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadDecisionAlternatives();
        }

        private void LoadDecisionAlternatives()
        {
            var hierarchyData = App.HierarchyData;
            this.Title = hierarchyData.First().Name;

            var readyToDisplay = new List<object>();

            // Hashset : data type which ensures that no duplicate values are stored
            HashSet<string> missingDataParents = new HashSet<string>();

            // Checks all nodes and record parents missing local priorities
            foreach (var node in hierarchyData)
            {
                if (node.Children != null && node.Children.Count > 0)
                {
                    if (node.LocalPriorities == null || node.LocalPriorities.Count == 0)
                    {
                        missingDataParents.Add(node.Name);
                    }
                }
            }

            // Process only leaf nodes for ListView
            var leafNodes = hierarchyData.Where(node => node.Children == null || node.Children.Count == 0);
            bool anyMissingGlobalPriority = false;
            foreach (var leaf in leafNodes)
            {
                double? globalPriority = CalculateGlobalPriority(leaf, hierarchyData);
                if (!globalPriority.HasValue)
                {
                    anyMissingGlobalPriority = true;
                }
                string priorityDisplay = globalPriority.HasValue ? $"{globalPriority:P2}" : "";
                readyToDisplay.Add(new { Name = leaf.Name, Priority = priorityDisplay });
                readyToDisplayNodes.Add(leaf);
            }

            ListView1.ItemsSource = readyToDisplay;


            // Update labels based on availability of global priorities
            if (anyMissingGlobalPriority)
            {
                HeaderLabel.Text = "Ratings of the alternatives are not available:";
                if (missingDataParents.Count > 0)
                {
                    SetInfoLabel(missingDataParents);
                }
            }
            else
            {
                HeaderLabel.Text = "Ratings of the alternatives:";
                InfoLabel.Text = "";
                InfoLabel.IsVisible = false;
                SubmitButton.IsVisible = true;
            }
        }


        private double? CalculateGlobalPriority(Node node, List<Node> hierarchyData)
        {
            double globalPriority = 0;
            bool missingData = false;

            foreach (var parent in hierarchyData)
            {
                // finds the parent node of the node
                if (parent.Children != null && parent.Children.Contains(node.Name))
                {
                    // Finds the position of the child node in the parent's children list
                    int index = parent.Children.IndexOf(node.Name);

                    // local priorities aren't defined and the index is invalid
                    if (parent.LocalPriorities == null || index >= parent.LocalPriorities.Count)
                    {
                        missingData = true;
                        break; // Local priorities data is incomplete
                    }

                    double parentGlobalPriority;
                    if (parent == hierarchyData.First())
                    {
                        parentGlobalPriority = 1.0; // Root node's global priority is 1.0
                    }
                    else
                    {
                        parentGlobalPriority = CalculateGlobalPriority(parent, hierarchyData).GetValueOrDefault(); // Recursively calculate if not the root
                    }

                    globalPriority += parent.LocalPriorities[index] * parentGlobalPriority;
                }
            }

            if (missingData)
            {
                return null; // Return null if any required data is missing
            }
            else
            {
                return globalPriority; // Return the calculated global priority otherwise
            }

        }


        private void SetInfoLabel(HashSet<string> missingDataParents)
        {
            string firstMissing = missingDataParents.First();
            int additionalMissingCount = missingDataParents.Count - 1;
            InfoLabel.Text = $"'{firstMissing}' and {additionalMissingCount} more nodes require evaluation.";
            InfoLabel.IsVisible = true;
        }


        private async void OnChildNodeTapped(object sender, ItemTappedEventArgs e)
        {

            // e.Item is var item: Confirms that the item exists and assigns it to item
            // item.GetType().GetProperty("Name")?.GetValue(item) is string childName: Gets the Name property of the item
            // ? : checks if the left-hand side is null before accessing the right-hand side
            if (e.Item is var item && item.GetType().GetProperty("Name")?.GetValue(item) is string childName)
            {
                ((ListView)sender).SelectedItem = null; // Clear the selection

                // Finds the first node with the given name
                Node childNode = readyToDisplayNodes.FirstOrDefault(n => n.Name == childName);
                if (childNode != null)
                {
                    await Navigation.PushAsync(new HierarchyPage(childNode));
                }
            }
        }


        private async void SubmitButton_Clicked(object sender, EventArgs e)
        {
            // List of anonymous objects with Name and Rating properties
            var alternativesWithPriorities = readyToDisplayNodes.Select(node => new
            {
                node.Name,
                Rating = CalculateGlobalPriority(node, App.HierarchyData).GetValueOrDefault().ToString("P2")
            }).ToList();

            string[] recipients = { "grigory.pishchulov@manchester.ac.uk" };
            string subject = $"AHP results for {this.Title}";
            string body = JsonConvert.SerializeObject(alternativesWithPriorities, Formatting.Indented);

            EmailMessage message;
            try
            {
                message = new EmailMessage(subject, body, recipients);
                await Email.ComposeAsync(message);
            }
            catch (FeatureNotSupportedException ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}
