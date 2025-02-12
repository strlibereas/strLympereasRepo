using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace createAlphabet
{
    public partial class Form1 : Form
    {
        private bool isCurveMode = false; // Flag to check if we are currently drawing
        private List<List<PointF>> allCurves = new List<List<PointF>>(); // Store all curves
        private List<PointF> currentCurve = new List<PointF>(); // Store points of the current curve
        private TextBox charInput;
        private TextBox outputBox;
        private Button btnSubmit, btnClear, btnNewCurve;
        private Panel drawingPanel;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Text = "Drawing App";
            this.Size = new Size(500, 500);
            this.MaximizeBox = false;

            // Initialize UI components
            charInput = new TextBox { Location = new Point(10, 10), Width = 50 };
            btnSubmit = new Button { Text = "Submit", Location = new Point(100, 10) };
            btnClear = new Button { Text = "Clear", Location = new Point(180, 10) };
            btnNewCurve = new Button { Text = "New Curve", Location = new Point(260, 10) };
            outputBox = new TextBox { Location = new Point(10, 200), Width = 460, Height = 280, Multiline = true, ReadOnly = true };
            drawingPanel = new Panel { Location = new Point(10, 50), Size = new Size(460, 150), BorderStyle = BorderStyle.FixedSingle };

            // Event Handlers
            btnClear.Click += (s, e2) => ClearDrawing();
            btnSubmit.Click += SubmitDrawing;
            btnNewCurve.Click += (s, e2) => StartNewCurve();

            drawingPanel.MouseClick += DrawMouse;
            drawingPanel.Paint += (s, e2) => RenderDrawing(e2.Graphics);

            this.Controls.AddRange(new Control[] { charInput, btnSubmit, btnClear, btnNewCurve, outputBox, drawingPanel });

            // Start drawing immediately on form load
            StartNewCurve();
        }

        private void DrawMouse(object sender, MouseEventArgs e)
        {
            if (!isCurveMode) return; // If we're not in drawing mode, do nothing.

            // Capture the point where the user clicked
            Point clientPoint = drawingPanel.PointToClient(Cursor.Position);
            float x = clientPoint.X;
            float y = clientPoint.Y;

            // Add the point to the current curve being drawn
            currentCurve.Add(new PointF(x, y));

            // Redraw the panel to show the new point
            drawingPanel.Invalidate();
        }

        private void RenderDrawing(Graphics g)
        {
            // Loop through all the curves and draw them
            foreach (var curve in allCurves)
            {
                if (curve.Count > 1)
                {
                    g.DrawCurve(Pens.Black, curve.ToArray()); // Draw each curve
                }
            }

            // Draw the current curve (if there is one)
            if (currentCurve.Count > 1)
            {
                g.DrawCurve(Pens.Blue, currentCurve.ToArray()); // Draw current curve in blue
            }
        }

        private void SubmitDrawing(object sender, EventArgs e)
        {
            if (charInput.Text.Length == 1)
            {
                // If there are points in the current curve, save it before submitting
                if (currentCurve.Count > 0)
                {
                    allCurves.Add(new List<PointF>(currentCurve)); // Add the current curve to the list
                    currentCurve.Clear(); // Clear current curve for the next one
                }

                if (allCurves.Count == 0) return;

                // Find the lowest X and Y values
                float minX = allCurves.SelectMany(c => c).Min(p => p.X);
                float minY = allCurves.SelectMany(c => c).Min(p => p.Y);

                // Normalize all curves by subtracting minX and minY
                List<List<PointF>> normalizedCurves = allCurves
                    .Select(curve => curve.Select(p => new PointF(p.X - minX, p.Y - minY)).ToList())
                    .ToList();

                // Collect the normalized points of all curves
                string pointData = string.Join(" | ", normalizedCurves.Select(curve =>
                    string.Join(", ", curve.Select(p => $"({p.X:F1}, {p.Y:F1})"))));

                string s = $"'{charInput.Text}' : {pointData}";
                outputBox.AppendText(s + Environment.NewLine);
            }
        }

        private void StartNewCurve()
        {
            // If there are points in the current curve, store it before starting a new one
            if (currentCurve.Count > 0)
            {
                allCurves.Add(new List<PointF>(currentCurve)); // Store the current curve
                currentCurve.Clear(); // Clear the current curve
            }

            // Start a new curve
            isCurveMode = true; // Allow the user to start drawing a new curve
            drawingPanel.Invalidate(); // Redraw to indicate new curve drawing
        }

        private void ClearDrawing()
        {
            // Clear all the stored curves and reset the current curve
            allCurves.Clear();
            currentCurve.Clear();
            drawingPanel.Invalidate(); // Trigger repaint to clear the panel
        }
    }
}
