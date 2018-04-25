﻿/*
 * Copyright © 2011 
 * Rowe Technology Inc.
 * All rights reserved.
 * http://www.rowetechinc.com
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification is NOT permitted.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
 * LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS
 * FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE 
 * COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT,
 * INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES INCLUDING,
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; 
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER 
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT 
 * LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN 
 * ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE 
 * POSSIBILITY OF SUCH DAMAGE.
 * 
 * HISTORY
 * -----------------------------------------------------------------
 * Date            Initials    Version    Comments
 * -----------------------------------------------------------------
 * 04/25/2018      RC          1.0.0       Initial coding    
 * 
 */

using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace PlotR
{
    /// <summary>
    /// Heat map plot.
    /// </summary>
    class HeatmapPlotViewModel : Caliburn.Micro.PropertyChangedBase
    {
        #region Enum

        /// <summary>
        /// Different Plot types.
        /// </summary>
        public enum PlotDataType
        {
            Magnitude,
            Amplitude,
        }

        #endregion

        #region Variables

        /// <summary>
        /// Max Ensembles to display.
        /// </summary>
        private int MAX_ENS = 510000;

        /// <summary>
        /// Bad velocity value.
        /// </summary>
        private double BAD_VELOCITY = 88.888;

        #endregion

        #region Properties

        #region Plot

        /// <summary>
        /// The plot for the view model.  This will be the plot
        /// that will be updated by the user.
        /// </summary>
        private PlotModel _plot;
        /// <summary>
        /// The plot for the view model.  This will be the plot
        /// that will be updated by the user.
        /// </summary>
        public PlotModel Plot
        {
            get { return _plot; }
            set
            {
                _plot = value;
                this.NotifyOfPropertyChange(() => this.Plot);
            }
        }

        #endregion

        #region Status

        /// <summary>
        /// File name of sqlite file.
        /// </summary>
        private string _FileName;

        public string FileName
        {
            get { return _FileName; }
            set
            {
                _FileName = value;
                NotifyOfPropertyChange(() => FileName);
            }
        }

        /// <summary>
        /// Number of ensembles.
        /// </summary>
        private int _TotalNumEnsembles;

        /// <summary>
        /// Number of ensembles.
        /// </summary>
        public int TotalNumEnsembles
        {
            get { return _TotalNumEnsembles; }
            set
            {
                _TotalNumEnsembles = value;
                NotifyOfPropertyChange(() => TotalNumEnsembles);
            }
        }

        /// <summary>
        /// Status Message.
        /// </summary>
        private string _StatusMsg;

        /// <summary>
        /// Status Message.
        /// </summary>
        public string StatusMsg
        {
            get { return _StatusMsg; }
            set
            {
                _StatusMsg = value;
                NotifyOfPropertyChange(() => StatusMsg);
            }
        }

        /// <summary>
        /// Status Progress count.
        /// </summary>
        private int _StatusProgress;

        /// <summary>
        /// Status progress count.
        /// </summary>
        public int StatusProgress
        {
            get { return _StatusProgress; }
            set
            {
                _StatusProgress = value;
                NotifyOfPropertyChange(() => StatusProgress);
            }
        }

        /// <summary>
        /// Status Progress max count.
        /// </summary>
        private int _StatusProgressMax;

        /// <summary>
        /// Status progress max count.
        /// </summary>
        public int StatusProgressMax
        {
            get { return _StatusProgressMax; }
            set
            {
                _StatusProgressMax = value;
                NotifyOfPropertyChange(() => StatusProgressMax);
            }
        }

        #endregion

        #region Plot Types

        /// <summary>
        /// List of all the plot types.
        /// </summary>
        public List<PlotDataType> PlotTypeList { get; set; }

        /// <summary>
        /// Selected Plot type.
        /// </summary>
        private PlotDataType _SelectedPlotType;
        /// <summary>
        /// Selected Plot type.
        /// </summary>
        public PlotDataType SelectedPlotType
        {
            get { return _SelectedPlotType; }
            set
            {
                _SelectedPlotType = value;
                NotifyOfPropertyChange(() => SelectedPlotType);

                // Replot data
                ReplotData(_SelectedPlotType);
            }
        }

        /// <summary>
        /// Magnitude Plot Selected.
        /// </summary>
        private bool _IsMagnitude;
        /// <summary>
        /// Magnitude Plot Selected.
        /// </summary>
        public bool IsMagnitude
        {
            get { return _IsMagnitude; }
            set
            {
                _IsMagnitude = value;
                NotifyOfPropertyChange(() => IsMagnitude);

                if (value)
                {
                    // Replot data
                    ReplotData(PlotDataType.Magnitude);
                }
            }
        }

        /// <summary>
        /// Amplitude Plot Selected.
        /// </summary>
        private bool _IsAmplitude;
        /// <summary>
        /// Amplitude Plot Selected.
        /// </summary>
        public bool IsAmplitude
        {
            get { return _IsAmplitude; }
            set
            {
                _IsAmplitude = value;
                NotifyOfPropertyChange(() => IsAmplitude);

                if (value)
                {
                    // Replot data
                    ReplotData(PlotDataType.Amplitude);
                }
            }
        }

        #endregion

        #region Bottom Track Line

        /// <summary>
        /// Bottom Track Line selection.
        /// </summary>
        private bool _IsBottomTrackLine;
        /// <summary>
        /// Bottom Track Line selection.
        /// </summary>
        public bool IsBottomTrackLine
        {
            get { return _IsBottomTrackLine; }
            set
            {
                _IsBottomTrackLine = value;
                NotifyOfPropertyChange(() => IsBottomTrackLine);

                if (value)
                {
                    // Replot data
                    //ReplotData(PlotDataType.Amplitude);
                }
            }
        }

        #endregion

        #endregion

        #region Commands

        /// <summary>
        /// Command to select an SQLite file.
        /// </summary>
        public ReactiveCommand<Unit, Unit> OpenCommand { get; private set; }

        #endregion

        /// <summary>
        /// Initialize the plot.
        /// </summary>
        public HeatmapPlotViewModel()
        {
            // Initialize
            FileName = "Open a DB file...";
            Plot = CreatePlot();

            // Selected Plot Type
            //PlotTypeList = Enum.GetValues(typeof(PlotDataType)).Cast<PlotDataType>().ToList();
            SelectedPlotType = PlotDataType.Magnitude;
            IsMagnitude = true;

            // Bottom Track Line
            IsBottomTrackLine = true;

            // Status
            StatusMsg = "";
            StatusProgress = 0;
            StatusProgressMax = 100;

            // Setup commands
            this.OpenCommand = ReactiveCommand.Create(() => OpenFile());
        }

        #region Open File

        /// <summary>
        /// Select the file to open.
        /// </summary>
        private void OpenFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "project db files (*.db)|*.db|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                // Set the file name
                FileName = openFileDialog.FileName;

                // Load the project
                LoadProject(FileName, _SelectedPlotType);
            }
        }

        #endregion

        #region Load Project

        /// <summary>
        /// Load the project and get all the data.
        /// </summary>
        /// <param name="fileName">File path of the project.</param>
        /// <param name="selectedPlotType">Selected Plot type.</param>
        private async void LoadProject(string fileName, PlotDataType selectedPlotType)
        {
            // Data to get from the project
            double[,] data = null;

            // Create data Source string
            string dataSource = string.Format("Data Source={0};Version=3;", fileName);

            try
            {
                // Create a new database connection:
                using (SQLiteConnection sqlite_conn = new SQLiteConnection(dataSource))
                {
                    // Open the connection:
                    sqlite_conn.Open();

                    // Get total number of ensembles in the project
                    TotalNumEnsembles = GetNumEnsembles(sqlite_conn);

                    // Get the magnitude data
                    await Task.Run(() => data = GetData(sqlite_conn, TotalNumEnsembles, selectedPlotType));

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

            // If there is no data, do not plot
            if (data != null)
            {
                // Update status
                StatusMsg = "Drawing Plot";

                // Plot the data from the project
                await Task.Run(() => PlotData(data));
            }
            else
            {
                StatusMsg = "No data to plot";
            }
        }

        #endregion

        #region Number of Ensembles

        /// <summary>
        /// Get the number of ensembles in the project database.
        /// </summary>
        /// <param name="cnn">SQLite connection.</param>
        /// <returns>Number of ensembles.</returns>
        private int GetNumEnsembles(SQLiteConnection cnn)
        {
            string query = string.Format("SELECT COUNT(*) FROM {0};", "tblEnsemble");

            // Ensure a connection was made
            if (cnn == null)
            {
                return -1;
            }

            int result = 0;
            using (DbCommand cmd = cnn.CreateCommand())
            {
                cmd.CommandText = query;

                // Get Result
                object resultValue = cmd.ExecuteScalar();
                result = Convert.ToInt32(resultValue.ToString());

            }

            return result;
        }

        /// <summary>
        /// Get the number of ensembles in the project database.
        /// </summary>
        /// <param name="cnn">SQLite connection.</param>
        /// <param name="query">Query string for number of ensembles.</param>
        /// <returns>Number of ensembles.</returns>
        private int GetNumEnsembles(SQLiteConnection cnn, string query)
        {
            // Ensure a connection was made
            if (cnn == null)
            {
                return -1;
            }

            int result = 0;
            using (DbCommand cmd = cnn.CreateCommand())
            {
                cmd.CommandText = query;

                // Get Result
                object resultValue = cmd.ExecuteScalar();
                result = Convert.ToInt32(resultValue.ToString());

            }

            return result;
        }

        #endregion

        #region Get Data

        /// <summary>
        /// Get the data based off the selected data type.
        /// </summary>
        /// <param name="ePlotDataType">Selected data type.</param>
        /// <param name="cnn">SQLite connection.</param>
        /// <param name="maxNumEnsembles">Max number of ensembles to display.</param>
        /// <param name="selectedPlotType">Selected Plot type.</param>
        /// <returns>The selected for each ensemble and bin.</returns>
        private double[,] GetData(SQLiteConnection cnn, int maxNumEnsembles, PlotDataType selectedPlotType)
        {
            StatusProgressMax = TotalNumEnsembles;
            StatusProgress = 0;

            switch (selectedPlotType)
            {
                case PlotDataType.Magnitude:
                { 
                    // Get the number of ensembles
                    int numEnsembles = GetNumEnsembles(cnn, string.Format("SELECT COUNT(*) FROM {0} WHERE {1} IS NOT NULL;;", "tblEnsemble", "EarthVelocityDS"));
                    StatusProgressMax = numEnsembles;

                    // Get data
                    string query = string.Format("SELECT ID,EnsembleNum,DateTime,{0} FROM tblEnsemble WHERE {1} IS NOT NULL;", "EarthVelocityDS", "EarthVelocityDS");
                    return GetDataFromDb(cnn, numEnsembles, query, selectedPlotType);
                }
                case PlotDataType.Amplitude:
                {
                    // Get the number of ensembles
                    int numEnsembles = GetNumEnsembles(cnn, string.Format("SELECT COUNT(*) FROM {0} WHERE {1} IS NOT NULL;;", "tblEnsemble", "AmplitudeDS"));
                    StatusProgressMax = numEnsembles;
                    
                    // Get data
                    string query = string.Format("SELECT ID,EnsembleNum,DateTime,{0} FROM tblEnsemble WHERE {1} IS NOT NULL;", "AmplitudeDS", "AmplitudeDS");
                    return GetDataFromDb(cnn, numEnsembles, query, selectedPlotType);
                }
                default:
                    return null;
            }
        }

        /// <summary>
        /// Get the data from the project DB.
        /// </summary>
        /// <param name="cnn">Database connection.</param>
        /// <param name="numEnsembles">Number of ensembles.</param>
        /// <param name="query">Query string to retreive the data.</param>
        /// <param name="selectedPlotType">Selected Plot Type.</param>
        /// <returns>Magnitude data in (NumEns X NumBin) format.</returns>
        private double[,] GetDataFromDb(SQLiteConnection cnn, int numEnsembles, string query, PlotDataType selectedPlotType)
        {
            // Init list
            double[,] result = null;
            int ensIndex = 0;

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
                    // Convert the Earth JSON to an object
                    //Debug.WriteLine(reader["EnsembleNum"]);
                    // Update the status
                    StatusProgress++;
                    StatusMsg = reader["EnsembleNum"].ToString();

                    // Verify there is more data available
                    if (reader == null)
                    {
                        break;
                    }

                    // Ensure we do not exceed the number of ensembles
                    if(ensIndex >= numEnsembles)
                    {
                        break;
                    }

                    // Parse the data from the db
                    // This will be select which type of data to plot
                    double[] data = ParseData(reader, selectedPlotType);

                    // Verify we have data
                    if (data != null)
                    {
                        // If the array has not be created, created now
                        if (result == null)
                        {
                            // Create the array if this is the first entry
                            // NumEnsembles X NumBins
                            result = new double[numEnsembles, data.Length];
                        }

                        // Add the data to the array
                        for (int x = 0; x < data.Length; x++)
                        {
                            result[ensIndex, x] = data[x];
                        }

                        ensIndex++;
                    }

                }
            }

            return result;
        }

        #endregion

        #region Parse Data

        /// <summary>
        /// Select which parser to use based off the selected plot.
        /// </summary>
        /// <param name="reader">Reader holds a single row (ensemble).</param>
        /// <param name="selectedPlotType">Selected Plot Type.</param>
        /// <returns>Data selected for the row.</returns>
        private double[] ParseData(DbDataReader reader, PlotDataType selectedPlotType)
        {
            switch (selectedPlotType)
            {
                case PlotDataType.Magnitude:
                    return ParseMagData(reader);
                case PlotDataType.Amplitude:
                    return ParseAmpData(reader);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Process the row from the DB.  A row represents an ensemble.
        /// </summary>
        /// <param name="reader">Database connection data.</param>
        /// <returns>Magnitude data for a row.</returns>
        private double[] ParseMagData(DbDataReader reader)
        {
            try
            {
                // Get the earth data as a JSON string
                string jsonEarth = reader["EarthVelocityDS"].ToString();

                if (!string.IsNullOrEmpty(jsonEarth))
                {
                    // Convert to a JSON object
                    JObject ensEarth = JObject.Parse(jsonEarth);

                    // Get the number of bins
                    int numBins = ensEarth["NumElements"].ToObject<int>();
                    //Debug.WriteLine("Num Bins: " + numBins);

                    //Debug.WriteLine(ensEarth["VelocityVectors"][0]["Magnitude"]);
                    double[] data = new double[numBins];
                    for (int x = 0; x < numBins; x++)
                    {
                        // Get the velocity vector magntidue from the JSON object and add it to the array
                        data[x] = ensEarth["VelocityVectors"][x]["Magnitude"].ToObject<double>();
                    }

                    return data;
                }

                return null;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error parsing the Earth Velocity Magnitude data row", e);
                return null;
            }
        }

        /// <summary>
        /// Process the row from the DB.  A row represents an ensemble.
        /// </summary>
        /// <param name="reader">Database connection data.</param>
        /// <returns>Magnitude data for a row.</returns>
        private double[] ParseAmpData(DbDataReader reader)
        {
            try
            {
                // Get the data as a JSON string
                string jsonData = reader["AmplitudeDS"].ToString();

                if (!string.IsNullOrEmpty(jsonData))
                {
                    // Convert to a JSON object
                    JObject ensData = JObject.Parse(jsonData);

                    // Get the number of bins
                    int numBins = ensData["NumElements"].ToObject<int>();
                    int numBeams = ensData["ElementsMultiplier"].ToObject<int>();

                    double[] data = new double[numBins];
                    for (int bin = 0; bin < numBins; bin++)
                    {
                        int avgCnt = 0;
                        double avg = 0.0;

                        // Average the amplitude for each beam data together
                        for (int beam = 0; beam < numBeams; beam++)
                        {
                            if (ensData["AmplitudeData"][bin][beam].ToObject<double>() != BAD_VELOCITY)
                            {
                                avgCnt++;
                                avg += ensData["AmplitudeData"][bin][beam].ToObject<double>();
                            }
                        }

                        // Add average data to the array
                        if (avgCnt > 0)
                        {
                            data[bin] = avg/avgCnt;
                        }
                        else
                        {
                            data[bin] = BAD_VELOCITY;
                        }
                    }

                    return data;
                }

                return null;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error parsing the Amplitude data row", e);
                return null;
            }
        }

        #endregion

        #region Create Plot

        /// <summary>
        /// Create the plot.
        /// </summary>
        /// <returns>Plot created.</returns>
        private PlotModel CreatePlot()
        {
            PlotModel temp = new PlotModel();

            // Color Legend
            var linearColorAxis1 = new LinearColorAxis();
            linearColorAxis1.HighColor = OxyColors.Black;
            linearColorAxis1.LowColor = OxyColors.Black;
            linearColorAxis1.Palette = OxyPalettes.Jet(64);
            linearColorAxis1.Position = AxisPosition.Right;
            linearColorAxis1.Minimum = 0.0;
            linearColorAxis1.Maximum = 2.0;
            temp.Axes.Add(linearColorAxis1);

            // Bottom Axis 
            // Ensembles 
            var linearAxis2 = new LinearAxis();
            linearAxis2.Position = AxisPosition.Bottom;
            linearAxis2.Unit = "Ensembles";
            linearAxis2.Key = "Ensembles";
            temp.Axes.Add(linearAxis2);

            // Left axis in Bins
            temp.Axes.Add(CreatePlotAxis(AxisPosition.Left, "bins"));

            // Right axis in Meters
            temp.Axes.Add(CreatePlotAxis(AxisPosition.Left, "meters", 2));

            return temp;
        }

        /// <summary>
        /// Create the plot axis.  Set the values for the plot axis.
        /// If you do not want to set a value, set the value to NULL.
        /// </summary>
        /// <param name="position">Position of the axis.</param>
        /// <param name="majorStep">Minimum value.</param>
        /// <param name="unit">Label for the axis.</param>
        /// <returns>LinearAxis for the plot.</returns>
        private LinearAxis CreatePlotAxis(AxisPosition position, string unit, int positionTier = 0)
        {
            // Create the axis
            LinearAxis axis = new LinearAxis();

            // Standard options
            axis.TicklineColor = OxyColors.White;
            axis.MajorGridlineStyle = LineStyle.Solid;
            axis.MinorGridlineStyle = LineStyle.Solid;
            axis.MajorGridlineColor = OxyColor.FromAColor(40, OxyColors.White);
            axis.MinorGridlineColor = OxyColor.FromAColor(20, OxyColors.White);
            axis.EndPosition = 0;
            axis.StartPosition = 1;
            axis.Position = position;
            axis.Key = unit;
            axis.PositionTier = positionTier;

            // Set the axis label
            axis.Unit = unit;

            return axis;
        }

        /// <summary>
        /// Set the minimum and maximum value for the color legend.
        /// </summary>
        /// <param name="min">Minimum value.</param>
        /// <param name="max">Maximum value.</param>
        private void SetMinMaxColorAxis(double min, double max)
        {
            // Find the LinearColorAxis to set the min and max value
            foreach(var axis in Plot.Axes)
            {
                if(axis.GetType() == typeof(LinearColorAxis))
                {
                    ((LinearColorAxis)axis).Minimum = min;
                    ((LinearColorAxis)axis).Maximum = max;
                }
            }
        }

        #endregion

        #region Plot Data

        /// <summary>
        /// Plot the given data.
        /// </summary>
        /// <param name="data">Data to plot by creating a series.</param>
        private void PlotData(double[,] data)
        {
            // Update the plots in the dispatcher thread
            try
            {
                // Lock the plot for an update
                lock (Plot.SyncRoot)
                {
                    // Clear any current series
                    StatusMsg = "Clear old Plot";
                    Plot.Series.Clear();

                    // Create a heatmap series
                    HeatMapSeries series = new HeatMapSeries();
                    series.X0 = 0;                          // Left starts 0
                    series.X1 = data.GetLength(0);          // Right (num ensembles)
                    series.Y0 = 0;                          // Top starts 0
                    series.Y1 = data.GetLength(1);          // Bottom end (num bins)
                    series.Data = data;
                    series.Interpolate = false;

                    // Add the series to the plot
                    StatusMsg = "Add Plot data";
                    Plot.Series.Add(series);
                }
            }
            catch (Exception ex)
            {
                // When shutting down, can get a null reference
                Debug.WriteLine("Error updating Heatmap Plot", ex);
            }

            // After the line series have been updated
            // Refresh the plot with the latest data.
            StatusMsg = "Drawing Plot";
            Plot.InvalidatePlot(true);

            StatusMsg = "Drawing complete.  Total Ensembles: " + data.GetLength(0);
        }


        #endregion

        #region Replot Data

        /// <summary>
        /// Replot the data based off a settings chage
        /// </summary>
        /// <param name="eplotDataType"></param>
        private void ReplotData(PlotDataType eplotDataType)
        {
            switch(eplotDataType)
            {
                case PlotDataType.Magnitude:
                    IsAmplitude = false;
                    SetMinMaxColorAxis(0, 2);
                    break;
                case PlotDataType.Amplitude:
                    IsMagnitude = false;
                    SetMinMaxColorAxis(0, 120);
                    break;
                default:
                    break;
            }

            // Replot the data
            if (!string.IsNullOrEmpty(FileName))
            {
                LoadProject(FileName, eplotDataType);
            }

        }

        #endregion
    }
}