using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace AHP_Mobile_App
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DecisionAlternativesPage : ContentPage
    {
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
            var readyToDisplay = new List<object>();
            HashSet<string> missingDataParents = new HashSet<string>();

            for (int i = 0; i < hierarchyData.Count; i++)
            {
                Node node = hierarchyData[i];
                double globalPriority = double.NaN; // Assume we can't calculate it by default

                if (node.Children != null && node.Children.Count > 0)
                {
                    if (node.LocalPriorities == null || node.LocalPriorities.Count == 0)
                    {
                        missingDataParents.Add(node.Name);
                    }
                    else
                    {
                        globalPriority = CalculateGlobalPriority(node, missingDataParents);
                    }
                }

                // Always add node to display; show global priority if calculable
                string priorityDisplay = double.IsNaN(globalPriority) ? "N/A" : $"{globalPriority:P2}";
                readyToDisplay.Add(new { Name = node.Name, Priority = priorityDisplay });
            }

            ListView1.ItemsSource = readyToDisplay;

            if (missingDataParents.Count > 0)
            {
                SetInfoLabel(missingDataParents);
            }
        }

        private double CalculateGlobalPriority(Node node, HashSet<string> missingDataParents)
        {
            double globalPriority = 1.0;
            Node currentNode = node;
            bool missingData = false;

            while (currentNode != null)
            {
                Node parentNode = null;
                foreach (var potentialParent in App.HierarchyData)
                {
                    if (potentialParent.Children != null && potentialParent.Children.Contains(currentNode.Name))
                    {
                        parentNode = potentialParent;
                        break;
                    }
                }

                if (parentNode == null || parentNode == App.HierarchyData.First()) break;

                if (parentNode.LocalPriorities == null || parentNode.LocalPriorities.Count == 0)
                {
                    missingDataParents.Add(parentNode.Name);
                    missingData = true;
                    break; // Do not continue calculating if data is missing
                }

                if (!missingData)
                {
                    int index = parentNode.Children.IndexOf(currentNode.Name);
                    globalPriority *= parentNode.LocalPriorities[index];
                }

                currentNode = parentNode;
            }

            return missingData ? double.NaN : globalPriority;
        }

        private void SetInfoLabel(HashSet<string> missingDataParents)
        {
            string firstMissing = missingDataParents.First();
            int additionalMissingCount = missingDataParents.Count - 1;
            InfoLabel.Text = $"Ratings of the alternatives are not available for '{firstMissing}' and {additionalMissingCount} more nodes require evaluation.";
            InfoLabel.IsVisible = true;
        }
    }
}
