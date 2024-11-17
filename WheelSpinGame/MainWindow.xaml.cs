using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;

namespace WheelSpinGame;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly Random random = new Random();
    private string currentTier = "S";
    private bool isSpinning = false;

    private Dictionary<string, List<PrizeInfo>> Prizes { get; set; }
    private List<Path> slicePaths = new List<Path>();


    public MainWindow()
    {
        InitializeComponent();
        InitializeHotkey();
        Prizes = InitializePrizes();
        UpdateTierButtons();
        DrawWheel(currentTier);
    }

    private void InitializeHotkey()
    {
        var currentInput = "";
        var secretCode = "icpep";

        PreviewKeyDown += (s, e) =>
        {
            currentInput += e.Key.ToString().ToLower();
            if (currentInput.Length > secretCode.Length)
            {
                currentInput = currentInput.Substring(1);
            }

            if (currentInput == secretCode)
            {
                ShowPrizeEditor();
                currentInput = "";
            }
        };
    }

    private void ShowPrizeEditor()
    {
        var editorWindow = new PrizeEditorWindow(Prizes, UpdatePrizes);
        var result = editorWindow.ShowDialog();
    }

    private void UpdatePrizes(Dictionary<string, List<PrizeInfo>> newPrizes)
    {
        Prizes = PrizeNormalizer.NormalizePrizes(newPrizes);
        UpdateTierButtons();
        DrawWheel(currentTier);
    }

    private void UpdateTierButtons()
    {
        TierButtonsControl.ItemsSource = Prizes.ToList();
    }


    private Dictionary<string, List<PrizeInfo>> InitializePrizes()
    {
        var defaultPrizes = new Dictionary<string, List<PrizeInfo>>
        {
            ["S"] = new List<PrizeInfo>
            {
                new PrizeInfo { Id = "s1", Name = "Candies", DropRate = 20, SliceSize = 1, Color = "#FF7E6B" },
                //   new PrizeInfo { Id = "s2", Name = "Stickers", DropRate = 2, SliceSize = 1, Color = "#4ECDC4" },
                new PrizeInfo { Id = "s3", Name = "Lanyard", DropRate = 0.1, SliceSize = 1, Color = "#45B7D1" }
            },
            ["A"] = new List<PrizeInfo>
            {
                new PrizeInfo { Id = "a1", Name = "Lanyard", DropRate = 0.5, SliceSize = 1, Color = "#FF7E6B" },
                new PrizeInfo { Id = "a2", Name = "Candies", DropRate = 6, SliceSize = 1, Color = "#4ECDC4" },
                new PrizeInfo { Id = "a3", Name = "Stickers", DropRate = 4, SliceSize = 1, Color = "#45B7D1" }
            },
            ["B"] = new List<PrizeInfo>
            {
                new PrizeInfo { Id = "b1", Name = "Candies", DropRate = 65, SliceSize = 1, Color = "#FF7E6B" },
                new PrizeInfo { Id = "b2", Name = "Stickers", DropRate = 10, SliceSize = 1, Color = "#4ECDC4" },
                new PrizeInfo
                    { Id = "b3", Name = "Better luck next time", DropRate = 25, SliceSize = 1, Color = "#95A5A6" }
            },
            ["C"] = new List<PrizeInfo>
            {
                new PrizeInfo { Id = "c1", Name = "Stickers", DropRate = 1, SliceSize = 1, Color = "#FF7E6B" },
                new PrizeInfo { Id = "c2", Name = "Candies", DropRate = 1, SliceSize = 1, Color = "#4ECDC4" },
                new PrizeInfo
                    { Id = "c3", Name = "Better luck next time", DropRate = 1, SliceSize = 1, Color = "#95A5A6" }
            }
        };

        return PrizeNormalizer.NormalizePrizes(defaultPrizes);
    }


    private void DrawWheel(string tier)
    {
        WheelCanvas.Children.Clear();
        slicePaths.Clear();
        double startAngle = 0;
        double centerX = 300;
        double centerY = 300;
        double radius = 280;

        foreach (var prize in Prizes[tier])
        {
            double sweepAngle = prize.SliceSize * 3.6; // Convert percentage to degrees
            var slice = CreateSlice(startAngle, sweepAngle, prize, centerX, centerY, radius);
            WheelCanvas.Children.Add(slice);
            slicePaths.Add(slice);

            // Add text label
            AddSliceText(prize.Name, startAngle, sweepAngle, centerX, centerY, radius);

            startAngle += sweepAngle;
        }
    }

    private Path CreateSlice(double startAngle, double sweepAngle, PrizeInfo prize, double centerX, double centerY,
        double radius)
    {
        var slice = new Path
        {
            Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(prize.Color)),
            Tag = prize,
            Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString(prize.Color),
                BlurRadius = 10,
                ShadowDepth = 0
            }
        };

        var figure = new PathFigure
        {
            StartPoint = new Point(centerX, centerY),
            IsClosed = true
        };

        double startRad = startAngle * Math.PI / 180;
        double endRad = (startAngle + sweepAngle) * Math.PI / 180;

        figure.Segments.Add(new LineSegment(
            new Point(
                centerX + radius * Math.Cos(startRad),
                centerY + radius * Math.Sin(startRad)
            ), true));

        figure.Segments.Add(new ArcSegment(
            new Point(
                centerX + radius * Math.Cos(endRad),
                centerY + radius * Math.Sin(endRad)
            ),
            new Size(radius, radius),
            0, sweepAngle > 180, SweepDirection.Clockwise,
            true));

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        slice.Data = geometry;

        return slice;
    }

    private void AddSliceText(string text, double startAngle, double sweepAngle, double centerX, double centerY,
        double radius)
    {
        // Calculate the middle angle of the slice
        double midAngle = (startAngle + sweepAngle / 2) * Math.PI / 180;

        // Position text at 60% of radius from center
        double textRadius = radius * 0.6;
        double textX = centerX + textRadius * Math.Cos(midAngle);
        double textY = centerY + textRadius * Math.Sin(midAngle);

        // Create text container Grid for rotation compensation
        var textContainer = new Grid();
        textContainer.RenderTransform = new RotateTransform
        {
            Angle = startAngle + sweepAngle / 2,
            CenterX = 0,
            CenterY = 0
        };

        // Create the TextBlock
        var textBlock = new TextBlock
        {
            Text = text,
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Colors.White),
            TextAlignment = TextAlignment.Center,
            RenderTransform = new RotateTransform
            {
                Angle = -90 - (startAngle + sweepAngle / 2) // Compensate for parent rotation
            }
        };

        textContainer.Children.Add(textBlock);

        // Position the container
        Canvas.SetLeft(textContainer, textX);
        Canvas.SetTop(textContainer, textY);
        WheelCanvas.Children.Add(textContainer);
    }

  
    private void SpinWheel(PrizeInfo selectedPrize)
    {
        if (isSpinning) return;
        isSpinning = true;

        // Calculate the exact angle range for the selected prize
        double startAngle = 0;
        foreach (var prize in Prizes[currentTier])
        {
            if (prize.Id == selectedPrize.Id) break;
            startAngle += prize.SliceSize * 3.6; // Convert percentage to degrees
        }

        double sliceAngle = selectedPrize.SliceSize * 3.6;
        double endAngle = startAngle + sliceAngle;

        // The needle points up (270 degrees), so we need to adjust our target
        // We want the prize to be at the top when the wheel stops
        double targetSliceCenter = startAngle + (sliceAngle / 2);
        
        // Calculate how much we need to rotate to get the slice center to the top (270 degrees)
        double angleToTop = 270 - targetSliceCenter;
        
        // Add multiple full rotations for effect
        double totalRotation = angleToTop + (360 * 5); // 5 full rotations before final position

        // Create the spin animation
        var animation = new DoubleAnimation
        {
            To = totalRotation,
            Duration = TimeSpan.FromSeconds(5),
            EasingFunction = new BackEase 
            { 
                Amplitude = 0.1,
                EasingMode = EasingMode.EaseOut
            }
        };

        animation.Completed += (s, e) =>
        {
            isSpinning = false;
            ShowWinningMessage(selectedPrize); // We can directly use selectedPrize since we know it's the winner
        };

        WheelRotation.BeginAnimation(RotateTransform.AngleProperty, animation);
    }

    private void ShowWinningMessage(PrizeInfo prize)
    {
        // Update win popup text
        WinPrizeText.Text = $"You won: {prize.Name}!";

        // Show the popup with animation
        WinPopup.Visibility = Visibility.Visible;
        WinPopup.Opacity = 0;

        var fadeIn = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromSeconds(0.3)
        };

        WinPopup.BeginAnimation(UIElement.OpacityProperty, fadeIn);
    }

    private void CloseWinPopup_Click(object sender, RoutedEventArgs e)
    {
        // Fade out animation
        var fadeOut = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = TimeSpan.FromSeconds(0.3)
        };

        fadeOut.Completed += (s, args) =>
        {
            WinPopup.Visibility = Visibility.Collapsed;

            // Reset wheel position with animation
            var resetAnimation = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromSeconds(1),
                EasingFunction = new QuadraticEase()
            };

            WheelRotation.BeginAnimation(RotateTransform.AngleProperty, resetAnimation);
        };

        WinPopup.BeginAnimation(UIElement.OpacityProperty, fadeOut);
    }

    private PrizeInfo SelectPrizeByDropRate()
    {
        var prizes = Prizes[currentTier];
        double totalRate = 0;
        foreach (var prize in prizes)
            totalRate += prize.DropRate;

        double randomValue = random.NextDouble() * totalRate;
        double currentSum = 0;

        foreach (var prize in prizes)
        {
            currentSum += prize.DropRate;
            if (randomValue <= currentSum)
                return prize;
        }

        return prizes[prizes.Count - 1];
    }

    private void CheckWinningPrize()
    {
        // Calculate which slice is under the needle
        double needleAngle = 270; // Needle points up at 270 degrees
        double currentRotation = WheelRotation.Angle % 360;
        double effectiveAngle = (needleAngle - currentRotation + 360) % 360;

        double currentAngle = 0;
        foreach (var prize in Prizes[currentTier])
        {
            double sliceAngle = prize.SliceSize * 3.6;
            if (effectiveAngle >= currentAngle && effectiveAngle < (currentAngle + sliceAngle))
            {
                ShowWinningMessage(prize);
                return;
            }

            currentAngle += sliceAngle;
        }
    }

    private void SpinButton_Click(object sender, RoutedEventArgs e)
    {
        if (!isSpinning)
        {
            var selectedPrize = SelectPrizeByDropRate();
            Debug.WriteLine(JsonConvert.SerializeObject(selectedPrize, Formatting.Indented));
            
            SpinWheel(selectedPrize);
        }
    }

    private void WheelCanvas_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        SpinButton_Click(sender, null);
    }

    private void TierButton_Click(object sender, RoutedEventArgs e)
    {
        if (isSpinning) return;

        var button = sender as Button;
        currentTier = button.Tag.ToString();
        DrawWheel(currentTier);
    }
}

public class PrizeInfo
{
    public string Id { get; set; }
    public string Name { get; set; }
    public double DropRate { get; set; }
    public double SliceSize { get; set; }
    public string Color { get; set; }
}