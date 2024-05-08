﻿using System;
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
            foreach (var leaf in leafNodes)
            {
                double? globalPriority = CalculateGlobalPriority(leaf, hierarchyData);
                string priorityDisplay = globalPriority.HasValue ? $"{globalPriority:P2}" : "";
                readyToDisplay.Add(new { Name = leaf.Name, Priority = priorityDisplay });
            }

            ListView1.ItemsSource = readyToDisplay;

            if (missingDataParents.Count > 0)
            {
                SetInfoLabel(missingDataParents);
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
            InfoLabel.Text = $"Ratings of the alternatives are not available for '{firstMissing}' and {additionalMissingCount} more nodes require evaluation.";
            InfoLabel.IsVisible = true;
        }
    }
}
