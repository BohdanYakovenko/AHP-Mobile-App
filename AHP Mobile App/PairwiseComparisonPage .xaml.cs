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

        // Holds the list of paired comparisons
        public List<PairedComparison> Comparisons { get; private set; }

        // Holds the list of preference strengths as options for the picker
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

            // Loop over each child node
            for (int i = 0; i < children.Count; i++)
            {
                // Loop over each subsequent child node to form a pair with the i-th child
                for (int j = i + 1; j < children.Count; j++)
                {
                    Comparisons.Add(new PairedComparison
                    {
                        Node1 = children[i],
                        Node2 = children[j],
                        IsToggled = false,  // Intially false
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


        // checks if all preferences are set
        private bool AreAllPreferencesSet()
        {
            return Comparisons.All(c => c.PreferenceStrength != null);
        }


        // ref ensures that any changes made to matrix inside the method are reflected in the original matrix passed to it.
        void PopulateMatrix(ref double[,] matrix, List<PairedComparison> comparisons)
        {
            int n = parentNode.Children.Count;

            for (int i = 0; i < n; i++)
            {
                matrix[i, i] = 1; // Diagonal is always 1

                // This loop starts from 'i + 1' to ensure comparisons are only made once per pair
                for (int j = i + 1; j < n; j++)
                {
                    // => Lambda expression - 'goes to' operator
                    // The left side of the operator specifies the input parameters, and the right side is an expression or a statement block that uses these parameters.
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

            // Calculate column totals
            double[] columnTotals = new double[size];
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

            // Calculate local priorities
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
