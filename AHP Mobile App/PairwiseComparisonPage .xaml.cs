using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace AHP_Mobile_App
{

    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PairwiseComparisonPage : ContentPage
    {
        private Node parentNode;
        public List<PairedComparison> Comparisons { get; private set; }
        public static List<Preference> PreferencesList { get; } = new List<Preference>
        {
            new Preference(1, "1: Equal importance"),
            new Preference(2, "2: Equal importance"),
            new Preference(3, "3: Moderate importance"),
            new Preference(4, "4: Moderate importance"),
            new Preference(5, "5: Strong importance"),
            new Preference(6, "6: Strong importance"),
            new Preference(7, "7: Very strong importance"),
            new Preference(8, "8: Very strong importance"),
            new Preference(9, "9: Extreme importance")
        };

        public PairwiseComparisonPage(Node node)
        {
            InitializeComponent();
            parentNode = node;
            Title = parentNode.Name;
            InitializeComparisons();
            BindingContext = this;
        }

        private void InitializeComparisons()
        {
            Comparisons = new List<PairedComparison>();
            var children = parentNode.Children;
            for (int i = 0; i < children.Count; i++)
            {
                for (int j = i + 1; j < children.Count; j++)
                {
                    Comparisons.Add(new PairedComparison
                    {
                        Node1 = children[i],
                        Node2 = children[j],
                        IsToggled = false,
                        PreferenceStrength = null  // Initially no preference selected
                    });
                }
            }
        }

        private async void SaveButton_Clicked(object sender, EventArgs e)
        {
            if (AreAllPreferencesSet())
            {
                int n = parentNode.Children.Count;
                double[,] matrix = new double[n, n];
                PopulateMatrix(ref matrix, Comparisons);
                List<double> localPriorities = CalculateLocalPriorities(matrix, n);
                parentNode.LocalPriorities = localPriorities;
            }
            await Navigation.PopAsync();
        }

        private bool AreAllPreferencesSet()
        {
            return Comparisons.All(c => c.PreferenceStrength != null);
        }

        void PopulateMatrix(ref double[,] matrix, List<PairedComparison> comparisons)
        {
            int n = parentNode.Children.Count;
            for (int i = 0; i < n; i++)
            {
                matrix[i, i] = 1; // Diagonal is always 1
                for (int j = i + 1; j < n; j++)
                {
                    var comparison = comparisons.Find(c => (c.Node1 == parentNode.Children[i] && c.Node2 == parentNode.Children[j]));
                    if (comparison != null && comparison.PreferenceStrength != null)
                    {
                        double strength = comparison.PreferenceStrength.Value;
                        if (!comparison.IsToggled)
                        {
                            matrix[i, j] = strength;
                            matrix[j, i] = 1.0 / strength;
                        }
                        else
                        {
                            matrix[j, i] = strength;
                            matrix[i, j] = 1.0 / strength;
                        }
                    }
                }
            }
        }


        public List<double> CalculateLocalPriorities(double[,] matrix, int size)
        {
            List<double> localPriorities = new List<double>();
            double[] columnTotals = new double[size];

            // Calculate column totals
            for (int j = 0; j < size; j++)
            {
                for (int i = 0; i < size; i++)
                {
                    columnTotals[j] += matrix[i, j];
                }
            }

            // Normalize the matrix and calculate row totals
            double[] rowTotals = new double[size];
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    matrix[i, j] /= columnTotals[j];
                    rowTotals[i] += matrix[i, j];
                }
            }

            // Calculate local priorities by normalizing row totals
            double totalRowSum = rowTotals.Sum();
            for (int i = 0; i < size; i++)
            {
                localPriorities.Add(rowTotals[i] / totalRowSum);
            }

            return localPriorities;
        }


    }

    public class PairedComparison
    {
        public string Node1 { get; set; }
        public string Node2 { get; set; }
        public bool IsToggled { get; set; }
        public Preference PreferenceStrength { get; set; }
    }

    public class Preference
    {
        public int Value { get; }
        public string Description { get; }

        public Preference(int value, string description)
        {
            Value = value;
            Description = description;
        }
    }
}
