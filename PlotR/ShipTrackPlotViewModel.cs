﻿using GMap.NET;
using GMap.NET.WindowsForms.Markers;
using GMap.NET.WindowsPresentation;
using Newtonsoft.Json.Linq;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Common;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shapes;

namespace PlotR
{
    class ShipTrackPlotViewModel : PlotViewModel
    {

        #region Class

        public class ShipTrackData
        {
            /// <summary>
            /// Magnitude scale.  This value is mulitplied to the magnitude
            /// value to increase or decrease the line length for visual representation.
            /// </summary>
            public double MagScale { get; set; }

            /// <summary>
            /// Line series to hold the ship track information.
            /// </summary>
            public LineSeries ShipSeries { get; set; }

            /// <summary>
            /// Line series to hold the Water Magnitude.
            /// </summary>
            public LineSeries WaterSeries { get; set; }

            /// <summary>
            /// Initialize.
            /// </summary>
            public ShipTrackData()
            {
                MagScale = 1.0;
                ShipSeries = new LineSeries();
                WaterSeries = new LineSeries();
            }
        }

        #endregion

        #region Properites

        #region Plot Scale

        /// <summary>
        /// Magnitude scale.  This value is mulitplied to the magnitude
        /// value to increase or decrease the line length for visual representation.
        /// </summary>
        private int _MagScale;
        /// <summary>
        /// Magnitude scale.  This value is mulitplied to the magnitude
        /// value to increase or decrease the line length for visual representation.
        /// </summary>
        public int MagScale
        {
            get { return _MagScale; }
            set
            {
                _MagScale = value;
                NotifyOfPropertyChange(() => MagScale);

                ReplotData();
            }
        }

        /// <summary>
        /// Set the minimum value.
        /// </summary>
        private double _MinValue;
        /// <summary>
        /// Set the minimum value.
        /// </summary>
        public double MinValue
        {
            get { return _MinValue; }
            set
            {
                _MinValue = value;
                NotifyOfPropertyChange(() => MinValue);

                // Replot the data
                ReplotData();

                // Set the color map canvas
                ColorMapCanvas = ColorHM.GetColorMapCanvas(_MinValue, _MaxValue);
                NotifyOfPropertyChange(() => ColorMapCanvas);
            }
        }

        /// <summary>
        /// Set the maximum value.
        /// </summary>
        private double _MaxValue;
        /// <summary>
        /// Set the maximum value.
        /// </summary>
        public double MaxValue
        {
            get { return _MaxValue; }
            set
            {
                _MaxValue = value;
                NotifyOfPropertyChange(() => MaxValue);

                // Replot the data
                ReplotData();

                // Set the color map canvas
                ColorMapCanvas = ColorHM.GetColorMapCanvas(_MinValue, _MaxValue);
                NotifyOfPropertyChange(() => ColorMapCanvas);
            }
        }

        #endregion

        #endregion

        #region GMap

        /// <summary>
        /// List of all the markers.
        /// </summary>
        public ObservableCollection<GMapMarker> Markers { get; set; }

        /// <summary>
        /// Current position on the map.
        /// </summary>
        private GMap.NET.PointLatLng _Position;
        /// <summary>
        /// Current position on the map.
        /// </summary>
        public GMap.NET.PointLatLng Position
        {
            get { return _Position; }
            set
            {
                _Position = value;
                NotifyOfPropertyChange(() => Position);
            }
        }

        /// <summary>
        /// Zoom level of the map.
        /// </summary>
        private int _Zoom;
        /// <summary>
        /// Zoom level of the map.
        /// </summary>
        public int Zoom
        {
            get { return _Zoom; }
            set
            {
                _Zoom = value;
                NotifyOfPropertyChange(() => Zoom);
            }
        }

        /// <summary>
        /// Map provider.
        /// </summary>
        public GMap.NET.MapProviders.GoogleMapProvider MapProvider { get; set; }

        /// <summary>
        /// Zoom type.
        /// </summary>
        public GMap.NET.MouseWheelZoomType MouseWheelZoomType { get; set; }


        /// <summary>
        /// Convert the values to a color based off a color map.
        /// </summary>
        public ColorHeatMap ColorHM { get; set; }

        /// <summary>
        /// Color map canvas to display the options.
        /// </summary>
        public System.Windows.Controls.Canvas ColorMapCanvas { get; set; }

        #endregion

        public ShipTrackPlotViewModel()
        {
            // Create the plot
            Plot = CreatePlot();

            ColorHM = new ColorHeatMap(0x80);      // 50% alpha

            // Create GMap
            GMap.NET.GMaps.Instance.Mode = GMap.NET.AccessMode.ServerAndCache;
            MapProvider = GMap.NET.MapProviders.GoogleMapProvider.Instance;
            Markers = new ObservableCollection<GMapMarker>();
            Position = new PointLatLng();
            Zoom = 1;
            _MagScale = 5;
            _MinValue = 0.0;
            _MaxValue = 2.0;
            NotifyOfPropertyChange(() => MagScale);
            NotifyOfPropertyChange(() => MinValue);
            NotifyOfPropertyChange(() => MaxValue);

            ColorMapCanvas = ColorHM.GetColorMapCanvas(_MinValue, _MaxValue);       // Set the color map canvas

            // To force shutdown of the GMAP
            //MapView.Manager.CancelTileCaching();
        }

        #region Create Plot

        /// <summary>
        /// Create the plot.
        /// </summary>
        /// <returns></returns>
        private ViewResolvingPlotModel CreatePlot()
        {
            ViewResolvingPlotModel temp = new ViewResolvingPlotModel();

            temp.IsLegendVisible = true;

            //temp.Background = OxyColors.Black;
            //temp.TextColor = OxyColors.White;
            //temp.PlotAreaBorderColor = OxyColors.White;

            temp.Title = "Distance Made Good";

            // Setup the axis
            //var c = OxyColors.White;
            temp.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                //Minimum = 0,
                //StartPosition = 1,                                              // This will invert the axis to start at the top with minimum value
                //EndPosition = 0
                //TicklineColor = OxyColors.White,
                //MajorGridlineStyle = LineStyle.Solid,
                //MinorGridlineStyle = LineStyle.Solid,
                //MajorGridlineColor = OxyColor.FromAColor(40, c),
                //MinorGridlineColor = OxyColor.FromAColor(20, c),
                //IntervalLength = 5,
                MinimumPadding = 0.1,                                               // Pad the top and bottom of the plot so min/max lines can be seen
                MaximumPadding = 0.1,                                               // Pad the top and bottom of the plot so min/max lines can be seen
                Unit = "latitude"
            });
            temp.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                //MajorStep = 1
                //Minimum = 0,
                //Maximum = _maxDataSets,
                //TicklineColor = OxyColors.White,
                //MajorGridlineStyle = LineStyle.Solid,
                //MinorGridlineStyle = LineStyle.Solid,
                //MajorGridlineColor = OxyColor.FromAColor(40, c),
                //MinorGridlineColor = OxyColor.FromAColor(20, c),
                //IntervalLength = 5,
                //TickStyle = OxyPlot.Axes.TickStyle.None,
                //IsAxisVisible = false,
                Unit = "longitude"
            });

            temp.Series.Add(new LineSeries() { Color = OxyColors.Chartreuse, StrokeThickness = 1, Title = "Ship Track" });

            return temp;
        }

        #endregion

        #region Load Project

        /// <summary>
        /// Load the project.  Use the selected min and max index to select the ensemble range to display.
        /// </summary>
        /// <param name="fileName">Project file path.</param>
        /// <param name="minIndex">Minimum Ensemble index.</param>
        /// <param name="maxIndex">Maximum Ensemble index.</param>
        public override void LoadProject(string fileName, int minIndex = 0, int maxIndex = 0)
        {
            // Load the base calls
            base.LoadProject(fileName, minIndex, maxIndex);

            // Plot the data
            ReplotData(minIndex, maxIndex);
        }

        #endregion

        #region Replot Data

        /// <summary>
        /// Implement reploting the data.
        /// </summary>
        /// <param name="minIndex">Minimum Index.</param>
        /// <param name="maxIndex">Maximum Index.</param>
        public override void ReplotData(int minIndex, int maxIndex)
        {
            DrawPlot(minIndex, maxIndex);
        }

        /// <summary>
        /// Implement replotting the data.
        /// </summary>
        public override void ReplotData()
        {
            // Replot the data
            ReplotData(0, 0);
        }

        #endregion

        #region Draw Plot

        private void DrawPlot(int minIndex, int maxIndex)
        {
            // Clear the current markers
            Markers.Clear();

            // Verify a file was given
            if (!string.IsNullOrEmpty(_ProjectFilePath))
            {
                // Verify the file exist
                if (File.Exists(_ProjectFilePath))
                {
                    // Create data Source string
                    string dataSource = string.Format("Data Source={0};Version=3;", _ProjectFilePath);

                    try
                    {
                        // Create a new database connection:
                        using (SQLiteConnection sqlite_conn = new SQLiteConnection(dataSource))
                        {
                            // Open the connection:
                            sqlite_conn.Open();

                            // Get total number of ensembles in the project
                            TotalNumEnsembles = GetNumEnsembles(sqlite_conn);

                            // If this is the first time loading
                            // show the entire plot
                            if (_firstLoad)
                            {
                                _firstLoad = false;
                                minIndex = 1;
                                maxIndex = TotalNumEnsembles;
                            }

                            // Get the data from the project
                            ShipTrackData data = null;
                            data = GetData(sqlite_conn, _MagScale, minIndex, maxIndex);

                            // If there is no data, do not plot
                            if (data != null)
                            {
                                // Update status
                                StatusMsg = "Drawing Plot";

                                // Plot the data from the project
                                PlotSeriesData(data);
                            }
                            else
                            {
                                StatusMsg = "No data to plot";
                            }

                            // Close connection
                            sqlite_conn.Close();
                        }
                    }
                    catch (SQLiteException e)
                    {
                        Debug.WriteLine("Error using database", e);
                        return;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("Error using database", e);
                        return;
                    }
                }
            }
        }

        #endregion

        #region Get Data

        /// <summary>
        /// Get the data based off the selected data type.
        /// </summary>
        /// <param name="cnn">SQLite connection.</param>
        /// <param name="magScale">Magnitude scale.</param>
        /// <param name="minIndex">Minimum Ensemble index.</param>
        /// <param name="maxIndex">Maximum Ensemble index.</param>
        /// <returns>The selected for each ensemble and bin.</returns>
        private ShipTrackData GetData(SQLiteConnection cnn, double magScale, int minIndex = 0, int maxIndex = 0)
        {
            //StatusProgressMax = TotalNumEnsembles;
            StatusProgress = 0;

            string datasetColumnName = "Position";

            // Get the number of ensembles
            int numEnsembles = GetNumEnsembles(cnn, string.Format("SELECT COUNT(*) FROM tblEnsemble WHERE ({0} IS NOT NULL) {1} {2};",
                                                                    datasetColumnName,
                                                                    GenerateQueryFileList(),
                                                                    GenerateQuerySubsystemList()));
            // Update the progress bar
            StatusProgressMax = numEnsembles;

            // If min and max are used, set the limit and offset
            LimitOffset lo = CalcLimitOffset(numEnsembles, minIndex, maxIndex);
            numEnsembles = lo.Limit;

            // Get data
            string query = string.Format("SELECT ID,EnsembleNum,DateTime,EarthVelocityDS,{0} FROM tblEnsemble WHERE ({1} IS NOT NULL) {2} {3} LIMIT {4} OFFSET {5};",
                                            datasetColumnName,
                                            datasetColumnName,
                                            GenerateQueryFileList(),
                                            GenerateQuerySubsystemList(),
                                            lo.Limit,
                                            lo.Offset);

            // Get the data to plot
            return QueryDataFromDb(cnn, query, magScale, minIndex, maxIndex);
        }

        /// <summary>
        /// Query the data from the database.
        /// </summary>
        /// <param name="cnn">SQLite connection.</param>
        /// <param name="query">Query for the data.</param>
        /// <param name="magScale">Magnitude scale.</param>
        /// <param name="minIndex">Minimum index.</param>
        /// <param name="maxIndex">Maximum index.</param>
        /// <returns></returns>
        private ShipTrackData QueryDataFromDb(SQLiteConnection cnn, string query, double magScale, int minIndex = 0, int maxIndex = 0)
        {
            // Init list
            int ensIndex = 0;

            // Init the new series data
            ShipTrackData stData = new ShipTrackData();
            stData.MagScale = magScale;

            // Ensure a connection was made
            if (cnn == null)
            {
                return null;
            }

            using (DbCommand cmd = cnn.CreateCommand())
            {
                cmd.CommandText = query;

                // Get Result
                DbDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    // Init the value
                    double avgMag = 0.0;
                    double avgDir = 0.0;

                    // Plot the lat/lon
                    string lat_lon = StatusMsg = reader["Position"].ToString();

                    // Get the magnitude data
                    string jsonEarth = reader["EarthVelocityDS"].ToString();
                    if (!string.IsNullOrEmpty(jsonEarth))
                    {
                        // Convert to a JSON object
                        JObject ensEarth = JObject.Parse(jsonEarth);

                        // Average the data
                        avgMag = GetAvgMag(ensEarth);
                        avgDir = GetAvgDir(ensEarth);
                    }

                    if (!string.IsNullOrEmpty(lat_lon))
                    {
                        // Separate the position by comma
                        string[] lat_lon_items = lat_lon.Split(',');

                        if (lat_lon_items.Length >= 2)
                        {
                            // Parse the data
                            double lat = 0.0;
                            double lon = 0.0;
                            double.TryParse(lat_lon_items[0], out lat);
                            double.TryParse(lat_lon_items[1], out lon);

                            // Add it to the series
                            stData.ShipSeries.Points.Add(new DataPoint(lat, lon));

                            // Convert the value to color from the color map
                            System.Windows.Media.SolidColorBrush brush = new System.Windows.Media.SolidColorBrush(ColorHM.GetColorForValue(avgMag, _MinValue, _MaxValue));

                            // Mark
                            //GMarkerGoogle marker = new GMarkerGoogle(new GMap.NET.PointLatLng(lat, lon), GMarkerGoogleType.blue);
                            //GMapMarker marker = new GMarkerGoogle(new GMap.NET.PointLatLng(lat, lon), GMarkerGoogleType.blue);
                            GMapMarker marker = new GMapMarker(new GMap.NET.PointLatLng(lat, lon));
                            System.Windows.Media.BrushConverter converter = new System.Windows.Media.BrushConverter();
                            //System.Windows.Media.Brush brush = (System.Windows.Media.Brush)converter.ConvertFromString("#80FF0000");  // 50 Alpha Red
                            
                            //marker.Shape = new Rectangle
                            //{
                            //    Width = 20,
                            //    Height = 20,
                            //    //Stroke = Brushes.Black,
                            //    //StrokeThickness = 1.5,
                            //    Fill = brush,
                            //    Stroke = brush
                            //};

                            marker.Shape = new Line
                            {
                                X1 = 0,
                                Y1 = 0,
                                X2 = (Math.Abs(avgMag) * magScale) * Math.Cos(avgDir),
                                Y2 = (Math.Abs(avgMag) * magScale) * Math.Sin(avgDir),
                                StrokeThickness = 3,
                                Stroke = brush
                            };

                            Markers.Add(marker);
                        }
                    }

                    ensIndex++;
                }

                // Set the center position
                Position = GetCenterPoint(Markers);
                Zoom = 17;
            }

            return stData;
        }

        /// <summary>
        /// Find the center point.
        /// </summary>
        /// <param name="markers">List of markers.</param>
        /// <returns>Center point.</returns>
        private PointLatLng GetCenterPoint(ObservableCollection<GMapMarker> markers)
        {
            PointLatLng centroide = new PointLatLng();
            int sum = 0;
            double lat = 0.0;
            double lng = 0.0;
            foreach (GMapMarker pts in markers)
            {
                sum++;
                lat += pts.Position.Lat;
                lng += pts.Position.Lng;

            }
            lat = lat / sum;
            lng = lng / sum;

            centroide.Lat = lat;
            centroide.Lng = lng;

            return centroide;
        }

        #endregion

        #region Velocity Vector Average

        /// <summary>
        /// Get the average magnitude from the earth velocity data.
        /// </summary>
        /// <param name="ensEarth">Earth JSON object.</param>
        /// <returns>Average of the magnitude data.</returns>
        public double GetAvgMag(JObject ensEarth)
        {
            double avg = 0.0;
            int count = 0;

            // Get the number of bins
            int numBins = ensEarth["NumElements"].ToObject<int>();
            for (int bin = 0; bin < numBins; bin++)
            {
                // Get the velocity vector magntidue from the JSON object and add it to the array
                double data = ensEarth["VelocityVectors"][bin]["Magnitude"].ToObject<double>();

                // Verify its good data
                if(Math.Round(data, 3) != BAD_VELOCITY)
                {
                    avg += data;
                    count++;
                }
            }

            // Take the average
            if(count > 0)
            {
                return avg / count;
            }

            return avg;
        }

        /// <summary>
        /// Get the average direction from the earth velocity data.
        /// </summary>
        /// <param name="ensEarth">Earth JSON object.</param>
        /// <returns>Average of the direction data.</returns>
        public double GetAvgDir(JObject ensEarth)
        {
            double avg = 0.0;
            int count = 0;

            // Get the number of bins
            int numBins = ensEarth["NumElements"].ToObject<int>();
            for (int bin = 0; bin < numBins; bin++)
            {
                // Get the velocity vector direction from the JSON object and add it to the array
                double data = ensEarth["VelocityVectors"][bin]["DirectionXNorth"].ToObject<double>();

                // Verify its good data
                if (Math.Round(data, 3) != BAD_VELOCITY)
                {
                    avg += data;
                    count++;
                }
            }

            // Take the average
            if (count > 0)
            {
                return avg / count;
            }

            return avg;
        }

        #endregion

        #region Plot Series

        /// <summary>
        /// Plot the series.  This will remove all the old series.  Then add the 
        /// new series lines.
        /// </summary>
        /// <param name="stData">Ship Track data.</param>
        private void PlotSeriesData(ShipTrackData stData)
        {
            lock (Plot.SyncRoot)
            {
                Plot.Series.Clear();

                // Add series to the plot
                Plot.Series.Add(stData.ShipSeries);
                Plot.Series.Add(stData.WaterSeries);
            }
            Plot.InvalidatePlot(true);

            //foreach(var marker in Markers)
            //    MapView.Markers.Add(marker);
        }



        #endregion

    }
}