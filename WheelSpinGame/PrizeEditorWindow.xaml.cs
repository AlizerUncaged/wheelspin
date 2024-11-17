using System.Windows;
using Newtonsoft.Json;

namespace WheelSpinGame;

public partial class PrizeEditorWindow : Window
{
    private readonly Action<Dictionary<string, List<PrizeInfo>>> onSave;
    private Dictionary<string, List<PrizeInfo>> currentPrizes;

    public PrizeEditorWindow(Dictionary<string, List<PrizeInfo>> prizes,
        Action<Dictionary<string, List<PrizeInfo>>> onSaveCallback)
    {
        InitializeComponent();
        currentPrizes = prizes;
        onSave = onSaveCallback;

        // Format and display the current prizes
        var jsonString = JsonConvert.SerializeObject(prizes, Formatting.Indented);
        JsonEditor.Text = jsonString;
    }

    private void FormatJson_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Parse and reformat the JSON
            var obj = JsonConvert.DeserializeObject(JsonEditor.Text);
            JsonEditor.Text = JsonConvert.SerializeObject(obj, Formatting.Indented);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Invalid JSON format: {ex.Message}", "Format Error", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void SaveChanges_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var newPrizes = JsonConvert.DeserializeObject<Dictionary<string, List<PrizeInfo>>>(JsonEditor.Text);
            var normalizedPrizes = PrizeNormalizer.NormalizePrizes(newPrizes);

            // Update the JSON editor with the normalized values
            JsonEditor.Text = JsonConvert.SerializeObject(normalizedPrizes, Formatting.Indented);


            onSave(normalizedPrizes);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error processing JSON: {ex.Message}", "Error", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private Dictionary<string, List<PrizeInfo>> ValidatePrizes(Dictionary<string, List<PrizeInfo>> prizes)
    {
        if (prizes == null)
            return new Dictionary<string, List<PrizeInfo>>();

        var fixedPrizes = new Dictionary<string, List<PrizeInfo>>();

        foreach (var tier in prizes)
        {
            if (tier.Value == null || tier.Value.Count == 0)
                continue;

            var fixedTier = new List<PrizeInfo>();
            double totalOriginalDropRate = 0;
            double totalOriginalSliceSize = 0;

            // First pass: Clean up basic data and sum totals
            foreach (var prize in tier.Value)
            {
                // Fix missing or invalid basic data
                var fixedPrize = new PrizeInfo
                {
                    Id = string.IsNullOrEmpty(prize.Id) ? Guid.NewGuid().ToString("N") : prize.Id,
                    Name = string.IsNullOrEmpty(prize.Name) ? "Prize" : prize.Name,
                    Color = string.IsNullOrEmpty(prize.Color) ? "#4ECDC4" : prize.Color,
                    DropRate = Math.Max(0, prize.DropRate), // Ensure non-negative
                    SliceSize = Math.Max(0, prize.SliceSize) // Ensure non-negative
                };

                totalOriginalDropRate += fixedPrize.DropRate;
                totalOriginalSliceSize += fixedPrize.SliceSize;
                fixedTier.Add(fixedPrize);
            }

            // Second pass: Normalize drop rates and slice sizes
            if (fixedTier.Count > 0)
            {
                // If all drop rates are 0, distribute evenly
                if (totalOriginalDropRate == 0)
                {
                    double evenShare = 100.0 / fixedTier.Count;
                    foreach (var prize in fixedTier)
                    {
                        prize.DropRate = evenShare;
                    }
                }
                else
                {
                    // Normalize drop rates to sum to 100
                    foreach (var prize in fixedTier)
                    {
                        prize.DropRate = (prize.DropRate / totalOriginalDropRate) * 100;
                    }
                }

                // If all slice sizes are 0 or don't sum to 100, distribute evenly
                if (totalOriginalSliceSize == 0 || Math.Abs(totalOriginalSliceSize - 100) > 0.01)
                {
                    double evenShare = 100.0 / fixedTier.Count;
                    foreach (var prize in fixedTier)
                    {
                        prize.SliceSize = evenShare;
                    }
                }
                else
                {
                    // Normalize slice sizes to sum to 100
                    foreach (var prize in fixedTier)
                    {
                        prize.SliceSize = (prize.SliceSize / totalOriginalSliceSize) * 100;
                    }
                }

                // Round to 2 decimal places
                foreach (var prize in fixedTier)
                {
                    prize.DropRate = Math.Round(prize.DropRate, 2);
                    prize.SliceSize = Math.Round(prize.SliceSize, 2);
                }

                // Adjust last item to ensure exact 100% total (handle rounding errors)
                if (fixedTier.Count > 0)
                {
                    var lastItem = fixedTier[fixedTier.Count - 1];
                    double totalDropRate = fixedTier.Sum(p => p.DropRate);
                    double totalSliceSize = fixedTier.Sum(p => p.SliceSize);

                    lastItem.DropRate += Math.Round(100 - totalDropRate, 2);
                    lastItem.SliceSize += Math.Round(100 - totalSliceSize, 2);
                }
            }

            fixedPrizes[tier.Key] = fixedTier;
        }

        return fixedPrizes;
    }


    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}