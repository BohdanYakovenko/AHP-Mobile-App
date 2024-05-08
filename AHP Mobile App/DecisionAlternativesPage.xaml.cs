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
            HashSet<string> missingDataParents = new HashSet<string>();

            // Check all nodes and record parents missing local priorities
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

            // Process only leaf nodes for ListView display
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

            // For each potential parent node, calculate contribution to global priority
            foreach (var parent in hierarchyData)
            {
                if (parent.Children != null && parent.Children.Contains(node.Name))
                {
                    int index = parent.Children.IndexOf(node.Name);
                    if (parent.LocalPriorities == null || index >= parent.LocalPriorities.Count)
                    {
                        missingData = true;
                        break; // Local priorities data is incomplete
                    }

                    double parentGlobalPriority = (parent == hierarchyData.First()) ? 1.0 : CalculateGlobalPriority(parent, hierarchyData).GetValueOrDefault();
                    globalPriority += parent.LocalPriorities[index] * parentGlobalPriority;
                }
            }

            return missingData ? (double?)null : globalPriority;
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
            if (e.Item is var item && item.GetType().GetProperty("Name")?.GetValue(item) is string childName)
            {
                ((ListView)sender).SelectedItem = null; // Clear the selection

                // Find the Node directly from readyToDisplayNodes
                Node childNode = readyToDisplayNodes.FirstOrDefault(n => n.Name == childName);
                if (childNode != null)
                {
                    await Navigation.PushAsync(new HierarchyPage(childNode));
                }
            }
        }

        private async void SubmitButton_Clicked(object sender, EventArgs e)
        {
            var alternativesWithPriorities = readyToDisplayNodes.Select(node => new
            {
                node.Name,
                Priority = CalculateGlobalPriority(node, App.HierarchyData).GetValueOrDefault().ToString("P2")
            }).ToList();

            string[] recipients = { "ya.bogdan.st@gmail.com" };
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
