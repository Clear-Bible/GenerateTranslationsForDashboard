﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Resources.NetStandard;
using System.Windows;
using System.Windows.Input;

namespace GenerateTranslationsForDashboard
{
    public class MainWindowViewModel: INotifyPropertyChanged
    {
        #region observable props

        private string _tSVfile;
        public string TSVfile 
        { 
            get => _tSVfile; 
            set
            {
                _tSVfile = value;
                NotifyPropertyChanged("TSVfile");
                UpdateEnabled();
            }
        }

        private string _stringsFolderPath;
        public string StringsFolderPath
        {
            get { return _stringsFolderPath; }
            set
            {
                _stringsFolderPath = value;
                NotifyPropertyChanged("StringsFolderPath");
                UpdateEnabled();
            }
        }

        private bool _enabled;
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                _enabled = value;
                NotifyPropertyChanged("Enabled");
            }
        }

        private DataView _gridData;
        public DataView GridData
        {
            get { return _gridData; }
            set
            {
                _gridData = value;
                NotifyPropertyChanged("GridData");
            }
        }

        private ObservableCollection<string> _dupList = new ObservableCollection<string>();
        public ObservableCollection<string> DupList
        {
            get { return _dupList; }
            set
            {
                _dupList = value;
                NotifyPropertyChanged("DupList");
            }
        }

        private ObservableCollection<Results> _ResultList = new ObservableCollection<Results>();
        public ObservableCollection<Results> ResultList
        {
            get { return _ResultList; }
            set
            {
                _ResultList = value;
                NotifyPropertyChanged("ResultList");
            }
        }

        private void UpdateEnabled()
        {
            if (String.IsNullOrEmpty(_tSVfile) || String.IsNullOrEmpty(_stringsFolderPath))
            {
                Enabled = false;
            }
            else
            {
                Enabled = true;
            }
        }


        #endregion

        #region commands

        private ICommand _CloseCommand;
        public ICommand CloseCommand
        {
            get => _CloseCommand;
            set
            {
                _CloseCommand = value;
            }
        }

        private ICommand _selectTSVCommand;
        public ICommand SelectTSVCommand
        {
            get => _selectTSVCommand;
            set
            {
                _selectTSVCommand = value;
            }
        }

        private ICommand _SelectStringsDirCommand;
        public ICommand SelectStringsDirCommand
        {
            get => _SelectStringsDirCommand;
            set
            {
                _SelectStringsDirCommand = value;
            }
        }

        private ICommand _GenerateCommand;
        public ICommand GenerateCommand
        {
            get => _GenerateCommand;
            set
            {
                _GenerateCommand = value;
            }
        }

        private ICommand _ProcessCommand;
        public ICommand ProcessCommand
        {
            get => _ProcessCommand;
            set
            {
                _ProcessCommand = value;
            }
        }

        #endregion



        #region startup

        public MainWindowViewModel()
        {
            // pull in the values from the Settings file
            TSVfile = Properties.Settings.Default.TSVFilePath;
            StringsFolderPath = Properties.Settings.Default.StringsFolderPath;

            // wire up the commands
            CloseCommand = new RelayCommand(CloseApp);
            SelectTSVCommand = new RelayCommand(OpenFile);
            SelectStringsDirCommand = new RelayCommand(SetStringsFolder);
            GenerateCommand = new RelayCommand(Generate);
            ProcessCommand = new RelayCommand(PopulateResources);
        }


        #endregion

        #region methods


        #endregion


        #region inotify

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }

        #endregion


        private void CloseApp(object obj)
        {
            if (obj is Window)
            {
                var window = (Window)obj;
                if (window != null)
                {
                    SaveSettings();
                    window.Close();
                }
            }
        }

        public void OpenFile(object obj)
        {
            // Configure open file dialog box
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.FileName = "translation.tsv"; // Default file name
            dialog.DefaultExt = ".tsv"; // Default file extension
            dialog.Filter = "Text documents (.tsv)|*.tsv"; // Filter files by extension

            // Show open file dialog box
            bool? result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                TSVfile = dialog.FileName;
            }
        }

        public void SetStringsFolder(object obj)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();

                // Process open file dialog box results
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    StringsFolderPath = dialog.SelectedPath;
                }
            }

        }

        public void SaveSettings()
        {
            // pull in the values from the Settings file
            Properties.Settings.Default.TSVFilePath = TSVfile;
            Properties.Settings.Default.StringsFolderPath = StringsFolderPath;

            Properties.Settings.Default.Save();
        }

        public void Generate(object obj)
        {
            ResultList.Clear();
            DupList.Clear();

            List<string> headers = new List<string>();
            // process the TSV file
            DataTable dt = new DataTable();
            using (TextReader tr = File.OpenText(TSVfile))
            {
                string line;
                while ((line = tr.ReadLine()) != null)
                {
                    string[] items = line.Split('\t');
                    if (dt.Columns.Count == 0)
                    {
                        // get the second line
                        line = tr.ReadLine();
                        string[] items2 = line.Split('\t');
                        // set the headers
                        for (int i = 0; i < items.Length; i++)
                        {
                            headers.Add(items[i] + " [" + items2[i] + "]");
                        }

                        // Create the data columns for the data table based on the number of items
                        // on the first line of the file
                        for (int i = 0; i < items.Length; i++)
                        {
                            dt.Columns.Add(new DataColumn("Column" + i, typeof(string)));
                        }

                        dt.Rows.Add(headers.ToArray());
                    }
                    else
                    {
                        // skip header and empty lines
                        if (! items[0].ToString().StartsWith(">"))
                        {
                            if (items[0].ToString() != "")
                            {
                                dt.Rows.Add(items);
                            }
                        }
                    }
                }
            }

            GridData = dt.DefaultView;
        }

        public void PopulateResources(object obj)
        {
            ResultList.Clear();

            //Dictionary<string, string> keys = new Dictionary<string, string>();
            List<string> keys = new List<string>();
            // iterate through the first column and get the keys
            for (int i = 0; i < GridData.Table.Rows.Count; i++)
            {
                var firstRow = _gridData[i];
                keys.Add(firstRow[0].ToString());
            }

            // check for duplicate keys
            var dups = keys.GroupBy(x => x)
                .Where(g => g.Count() > 1)
                .Select(y => y.Key)
                .ToList();

            if (dups.Count > 0)
            {
                // we got dups
                DupList = new ObservableCollection<string>(dups);
                return;
            }


            // loop through each of the other columns and build a dictionary
            for (int c = 1; c < GridData.Table.Columns.Count; c++)
            {
                Dictionary<string, string> lang = new Dictionary<string, string>();
                for (int r = 0; r < GridData.Table.Rows.Count; r++)
                {
                    var dataRow = _gridData[r];
                    lang.Add(keys[r], dataRow[c].ToString());
                }

                // get the resource lang code
                var element = lang.ElementAt(0);
                var code = element.Value;
                // parse out the bracketed code
                code = code.Substring(code.IndexOf("[")).Replace("[","").Replace("]","");

                // Find the corresponding file for this langauge code
                var langFile = Path.Combine(this._stringsFolderPath, $"Resources.{code}.resx");
                if (!File.Exists(langFile))
                {
                    ResultList.Add(new Results
                    {
                        FilePath = langFile,
                        Message = "File not found"
                    });
                    break;
                }

                // https://stackoverflow.com/questions/676312/modifying-resx-file-in-c-sharp
                // Create a ResXResourceReader for the file items.resx.
                Hashtable resourceEntries = new Hashtable();

                //Get existing resources
                ResXResourceReader reader = new ResXResourceReader(langFile);
                if (reader != null)
                {
                    IDictionaryEnumerator id = reader.GetEnumerator();
                    foreach (DictionaryEntry d in reader)
                    {
                        if (d.Value == null)
                            resourceEntries.Add(d.Key.ToString(), "");
                        else
                            resourceEntries.Add(d.Key.ToString(), d.Value.ToString());
                    }
                    reader.Close();
                }

                //Modify resources here...
                foreach (String key in lang.Keys)
                {
                    if (!resourceEntries.ContainsKey(key))
                    {

                        String value = lang[key].ToString();
                        if (value == null) value = "";

                        resourceEntries.Add(key, value);
                    }
                }

                //Write the combined resource file
                ResXResourceWriter resourceWriter = new ResXResourceWriter(langFile);

                foreach (String key in resourceEntries.Keys)
                {
                    resourceWriter.AddResource(key, resourceEntries[key]);
                }
                resourceWriter.Generate();
                resourceWriter.Close();


                ResultList.Add(new Results
                {
                    FilePath = langFile,
                    Message = "Success!"
                });
            }

        }
    }
}
