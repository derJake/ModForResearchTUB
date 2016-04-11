using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;

namespace ModForResearchTUB
{
    static class DrawDiagram
    {
        public static void renderDiagramToDisk(List<Tuple<int, int>> data)
        {
            using (Chart chart = new Chart() { Height = 1600, Width = 2400 })
            {
                // Konfiguration
                chart.Titles.Add("Amount of Keypresses per length pressed");
                chart.ChartAreas.Add(new ChartArea("statistic")
                {
                    AxisX = new Axis()
                    {
                        MajorGrid = new Grid() { Enabled = false }
                    },
                    AxisY = new Axis()
                    {
                        MajorGrid = new Grid() { LineColor = Color.LightGray, LineDashStyle = ChartDashStyle.Dot },
                        Title = "amount"
                    }
                });
                chart.Series.Add(new Series("data") { ChartType = SeriesChartType.Column });
                // Daten
                foreach (Tuple<int, int> entry in data) {
                    chart.Series["data"].Points.Add(
                        new DataPoint() {
                            AxisLabel = entry.Item1.ToString(),
                            YValues = new double[] { entry.Item2 }
                        }
                    );
                }
                // Ausgabe
                chart.SaveImage(DateTime.Now.ToString("yyyy-MM-dd") + "-keypress-lengths.png", ChartImageFormat.Png);
            }
         }
    }
}

