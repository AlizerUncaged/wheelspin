namespace WheelSpinGame;

public static class PrizeNormalizer
{
    public static Dictionary<string, List<PrizeInfo>> NormalizePrizes(Dictionary<string, List<PrizeInfo>> prizes)
    {
        if (prizes == null)
            return new Dictionary<string, List<PrizeInfo>>();

        var normalizedPrizes = new Dictionary<string, List<PrizeInfo>>();

        foreach (var tier in prizes)
        {
            if (tier.Value == null || tier.Value.Count == 0)
                continue;

            var normalizedTier = new List<PrizeInfo>();
            double totalDropRate = 0;
            double totalSliceSize = 0;

            // First pass: Create clean copies and calculate totals
            foreach (var prize in tier.Value)
            {
                var normalizedPrize = new PrizeInfo
                {
                    Id = string.IsNullOrEmpty(prize.Id) ? Guid.NewGuid().ToString("N") : prize.Id,
                    Name = string.IsNullOrEmpty(prize.Name) ? "Prize" : prize.Name,
                    Color = string.IsNullOrEmpty(prize.Color) ? "#4ECDC4" : prize.Color,
                    DropRate = Math.Max(0, prize.DropRate),
                    SliceSize = Math.Max(0, prize.SliceSize)
                };

                totalDropRate += normalizedPrize.DropRate;
                totalSliceSize += normalizedPrize.SliceSize;
                normalizedTier.Add(normalizedPrize);
            }

            // Second pass: Normalize values
            if (normalizedTier.Count > 0)
            {
                // Normalize drop rates
                if (totalDropRate == 0)
                {
                    // If all drop rates are 0, distribute evenly
                    double evenShare = 100.0 / normalizedTier.Count;
                    foreach (var prize in normalizedTier)
                    {
                        prize.DropRate = evenShare;
                    }
                }
                else
                {
                    // Normalize existing drop rates to sum to 100
                    foreach (var prize in normalizedTier)
                    {
                        prize.DropRate = (prize.DropRate / totalDropRate) * 100;
                    }
                }

                // Normalize slice sizes
                if (totalSliceSize == 0)
                {
                    // If all slice sizes are 0, distribute evenly
                    double evenShare = 100.0 / normalizedTier.Count;
                    foreach (var prize in normalizedTier)
                    {
                        prize.SliceSize = evenShare;
                    }
                }
                else
                {
                    // Normalize existing slice sizes to sum to 100
                    foreach (var prize in normalizedTier)
                    {
                        prize.SliceSize = (prize.SliceSize / totalSliceSize) * 100;
                    }
                }

                // Round all values to 2 decimal places
                foreach (var prize in normalizedTier)
                {
                    prize.DropRate = Math.Round(prize.DropRate, 2);
                    prize.SliceSize = Math.Round(prize.SliceSize, 2);
                }

                // Ensure exact 100% total by adjusting the last item
                if (normalizedTier.Count > 0)
                {
                    var lastItem = normalizedTier[normalizedTier.Count - 1];
                    double finalTotalDropRate = normalizedTier.Sum(p => p.DropRate);
                    double finalTotalSliceSize = normalizedTier.Sum(p => p.SliceSize);

                    lastItem.DropRate += Math.Round(100 - finalTotalDropRate, 2);
                    lastItem.SliceSize += Math.Round(100 - finalTotalSliceSize, 2);
                }
            }

            normalizedPrizes[tier.Key] = normalizedTier;
        }

        return normalizedPrizes;
    }
}