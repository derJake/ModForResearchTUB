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
        public static void renderDiagramToDisk(List<Tuple<String, double>> data, String title, String yAxisTitle, String diagramtype)
        {
            using (Chart chart = new Chart() { Height = 2400, Width = 4800 })
            {
                // Konfiguration
                chart.Titles.Add(title);
                chart.ChartAreas.Add(new ChartArea("statistic")
                {
                    AxisX = new Axis()
                    {
                        MajorGrid = new Grid() { Enabled = false }, TitleFont = new Font("Helvetica", 18.0f)
                    },
                    AxisY = new Axis()
                    {
                        MajorGrid = new Grid() { LineColor = Color.LightGray, LineDashStyle = ChartDashStyle.Dot },
                        Title = yAxisTitle,
                        TitleFont = new Font("Helvetica", 18.0f)
                    }
                });
                chart.Series.Add(new Series("data") { ChartType = SeriesChartType.Column });
                // Daten
                foreach (Tuple<String, double> entry in data) {
                    chart.Series["data"].Points.Add(
                        new DataPoint() {
                            AxisLabel = entry.Item1.ToString(),
                            YValues = new double[] { entry.Item2 }
                        }
                    );
                }
                // Ausgabe
                chart.SaveImage(DateTime.Now.ToString("yyyy-MM-dd") + diagramtype + ".png", ChartImageFormat.Png);
            }
         }
    }
}

