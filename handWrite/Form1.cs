using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace handWrite
{
    public class Symbols
    {
        public char Letter;
        public List<Point[]> BasePath; // Change this to a non-static field

        public static Dictionary<char, Symbols> SymbolDefinitions = new Dictionary<char, Symbols>();

        // Constructor now properly initializes BasePath
        public Symbols(char letter, List<Point[]> basePath)
        {
            Letter = letter;
            BasePath = basePath;
        }

        public static void LoadSymbolsFromFile(string filePath)
        {
            if (!File.Exists(filePath)) return;

            string[] lines = File.ReadAllLines(filePath);
            Regex regex = new Regex(@"'(.+?)'\s*:\s*(.+)");

            foreach (string line in lines)
            {
                Match match = regex.Match(line);
                if (!match.Success) continue;

                char letter = match.Groups[1].Value[0];
                string pointsData = match.Groups[2].Value;

                List<Point[]> basePath = new List<Point[]>();
                List<Point> currentPoints = new List<Point>();

                Regex pointRegex = new Regex(@"\((\d+),0,\s*(\d+),0\)(?:\s*\|\s*)?");
                foreach (Match pointMatch in pointRegex.Matches(pointsData))
                {
                    int x = int.Parse(pointMatch.Groups[1].Value);
                    int y = int.Parse(pointMatch.Groups[2].Value);
                    currentPoints.Add(new Point(x, y)); // Add point to the current list

                    // Check if the next point is preceded by a pipe character
                    if (pointMatch.Value.Contains("|"))
                    {
                        // Add current points as a new Point[] to the basePath list
                        basePath.Add(currentPoints.ToArray());
                        currentPoints.Clear(); // Clear the list to start adding points for the next group
                    }
                }

                // If there are any remaining points after the last group, add them to basePath
                if (currentPoints.Count > 0)
                {
                    basePath.Add(currentPoints.ToArray());
                }

                // Create a new Symbols object and add it to the dictionary
                SymbolDefinitions[letter] = new Symbols(letter, basePath);
            }
        }
    }

    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private TextBox inputTextBox;
        private Button processButton;
        private Button exportButton;
        private PictureBox canvas;
        private Label scaleLabel;
        private TrackBar scaleBar;
        private TrackBar devBar;
        private TrackBar spaceBar;
        private Label thicknessLabel;
        private TrackBar thicknessBar;
        private Label minthicknessLabel;
        private TrackBar minthicknessBar;
        private Label deviationLabel;
        private Label spaceLabel;
        private CheckBox reverseThikness;
        private Random random = new Random();

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Text = "Letter Drawer";
            this.Size = new Size(1450, 900);

            inputTextBox = new TextBox { Location = new Point(20, 20), Width = 300 };
            processButton = new Button { Text = "Draw", Location = new Point(330, 18) };
            exportButton = new Button { Text = "export", Location = new Point(405, 18) };
            scaleLabel = new Label();
            thicknessLabel = new Label();
            thicknessLabel.Text = "max line thickness";
            thicknessLabel.Location = new Point(430, 60);
            minthicknessLabel = new Label();
            minthicknessLabel.Text = "min line thickness";
            minthicknessLabel.Location = new Point(530, 60);
            thicknessBar = new TrackBar { Location = new Point(430, 80), Minimum = 3, Maximum = 10, Value = 5 };
            minthicknessBar = new TrackBar { Location = new Point(530, 80), Minimum = 1, Maximum = 3, Value = 2 };
            scaleLabel.Text = "scale";
            deviationLabel = new Label();
            deviationLabel.Text = "απόκλιση";
            spaceLabel = new Label();
            spaceLabel.Text = "διάστημα μεταξύ γραμμάτων";
            spaceLabel.AutoSize = true;
            spaceLabel.Location = new Point(260, 60);
            deviationLabel.Location = new Point(150, 60);
            scaleLabel.Location = new Point(30, 60);
            reverseThikness = new CheckBox();
            reverseThikness.Text = "reverse thickness";
            reverseThikness.AutoSize = true;
            reverseThikness.Location = new Point(630, 80);
            scaleBar = new TrackBar { Location = new Point(20, 80), Minimum = 1, Maximum = 10, Value = 5 };
            canvas = new PictureBox { Location = new Point(20, 130), Size = new Size(1400, 700), BorderStyle = BorderStyle.FixedSingle };
            spaceBar = new TrackBar { Location = new Point(280, 80), Minimum = 1, Maximum = 100, Value = 50 };
            devBar = new TrackBar { Location = new Point(120, 80), Minimum = 1, Maximum = 50, Value = 10 };

            processButton.Click += (sender2, e2) => DrawText();
            exportButton.Click += (sender2, e2) => export();
            Symbols.LoadSymbolsFromFile("symbols.txt");

            this.Controls.Add(reverseThikness);
            this.Controls.Add(thicknessLabel);
            this.Controls.Add(thicknessBar);
            this.Controls.Add(minthicknessLabel);
            this.Controls.Add(minthicknessBar);
            this.Controls.Add(inputTextBox);
            this.Controls.Add(processButton);
            this.Controls.Add(scaleBar);
            this.Controls.Add(canvas);
            this.Controls.Add(scaleLabel);
            this.Controls.Add(deviationLabel);
            this.Controls.Add(devBar);
            this.Controls.Add(spaceLabel);
            this.Controls.Add(spaceBar);
            this.Controls.Add(exportButton);
        }

        private void export()
        {
            Image bitmap = new Bitmap(canvas.Width, canvas.Height);
            bitmap = canvas.Image;
            System.Windows.Forms.SaveFileDialog saveFileDialog1;
            saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            DialogResult dr = saveFileDialog1.ShowDialog();
            if (dr == DialogResult.OK)
            {
                string filename = saveFileDialog1.FileName;
                bitmap.Save(filename + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
            }
        }

        private void DrawText()
        {
            int deviation = devBar.Value;
            string text = inputTextBox.Text.Normalize(NormalizationForm.FormC);
            if (string.IsNullOrWhiteSpace(text)) return;

            float scale = scaleBar.Value / 10.0f;

            Bitmap bitmap = new Bitmap(canvas.Width, canvas.Height);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.White);
                int offsetX = 10;
                int offsetY = 10;
                int maxWidth = canvas.Width - 50;
                int lineHeight = (int)(160 * scale);

                foreach (char letter in text)
                {
                    if (offsetX > maxWidth)
                    {
                        offsetX = 10;
                        offsetY += lineHeight;
                    }

                    if (letter == ' ')
                    {
                        offsetX += (int)(40 * scale);
                        continue;
                    }

                    if (!Symbols.SymbolDefinitions.ContainsKey(letter)) continue;

                    Symbols symbol = Symbols.SymbolDefinitions[letter];

                    int minX = symbol.BasePath.SelectMany(p => p).Min(p => p.X);
                    int maxX = symbol.BasePath.SelectMany(p => p).Max(p => p.X);
                    int symbolWidth = (int)((maxX - minX) * scale);

                    foreach (Point[] item in symbol.BasePath)
                    {
                        Point[] points = item.Select(p =>
                            new Point(
                                (int)(offsetX + p.X * scale + random.Next(-deviation, deviation) * scale),
                                (int)(offsetY + p.Y * scale + random.Next(-deviation, deviation) * scale)
                            )
                        ).ToArray();

                        int steps = points.Length - 1; // Number of segments
                        float maxThickness = (float)(thicknessBar.Value); // Maximum thickness
                        float minThickness = (float)(minthicknessBar.Value); // Minimum thickness

                        if (!reverseThikness.Checked)
                        {
                            for (int i = 0; i < steps; i++)
                            {
                                float thickness = minThickness + (maxThickness - minThickness) * (i / (float)steps);
                                using (Pen pen = new Pen(Color.Black, thickness))
                                {
                                    g.DrawLine(pen, points[i], points[i + 1]);
                                }
                            }
                        }
                        else
                        {
                            for (int i = 0; i < steps; i++)
                            {
                                float t = i / (float)steps;
                                t = 1 - t; // Reverse thickness transition
                                float thickness = minThickness + (maxThickness - minThickness) * t;
                                using (Pen pen = new Pen(Color.Black, thickness))
                                {
                                    g.DrawLine(pen, points[i], points[i + 1]);
                                }
                            }
                        }
                    }


                    offsetX += symbolWidth + (int)(spaceBar.Value * scale) + (int)(random.Next(-5, +5) * scale);
                }
            }
            canvas.Image = bitmap;
        }
    }




}

