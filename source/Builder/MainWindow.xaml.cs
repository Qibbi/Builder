using Builder.Build;
using Microsoft.CodeDom.Providers.DotNetCompilerPlatform;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Builder
{
    public partial class MainWindow : Window, IBuildGUI
    {
        private const string _buildScript = "buildscript.cs";

        private ErrorTreeViewItemCategory _treeViewItemCritical;
        private ErrorTreeViewItemCategory _treeViewItemError;
        private ErrorTreeViewItemCategory _treeViewItemWarning;
        private List<Project> _availableProjects = new List<Project>();
        private List<GUIElementData> _inputGuiData;
        private Dictionary<string, FrameworkElement> _generatedGuiData = new Dictionary<string, FrameworkElement>();

        private object _brushStandard;
        private System.Windows.Media.Color _colorCritical;
        private System.Windows.Media.Color _colorError;
        private System.Windows.Media.Color _colorWarning;
        private SolidColorBrush _solidColorBrushGreen;
        private SolidColorBrush _solidColorBrushRed;
        private SolidColorBrush _solidColorBrushCritical;
        private SolidColorBrush _solidColorBrushError;
        private SolidColorBrush _solidColorBrushWarning;
        private string _selectedProject;
        private bool _isLastPart;
        private bool _isFailed;
        private long _buildTimeStart;
        private long _stepTimeStart;
        private int _currentStep;
        private Process _currentRunningProcess;
        private bool _isProcessRunning;
        private bool _isRedirectingOutput;
        private Thread _currentProcessOutputReader;

        public IBuildScript ScriptInterface { get; private set; }
        public bool CanBuild => _selectedProject != null;
        public string SelectedProject
        {
            get => _selectedProject;
            set
            {
                if (_selectedProject != value)
                {
                    _selectedProject = value;
                    try
                    {
                        _generatedGuiData = GenerateGuiData(GridBuildOptions, ScriptInterface.GuiData, null, out _inputGuiData);
                    }
                    catch (Exception ex)
                    {
                        DisplayException(ex);
                    }
                }
            }
        }
        public SolidColorBrush SolidColorBrushGreen
        {
            get
            {
                if (_solidColorBrushGreen == null)
                {
                    _solidColorBrushGreen = new SolidColorBrush(Colors.Green);
                }
                return _solidColorBrushGreen;
            }
        }
        public SolidColorBrush SolidColorBrushRed
        {
            get
            {
                if (_solidColorBrushRed == null)
                {
                    _solidColorBrushRed = new SolidColorBrush(Colors.Red);
                }
                return _solidColorBrushRed;
            }
        }
        public SolidColorBrush SolidColorBrushCritical
        {
            get
            {
                if (_solidColorBrushCritical == null)
                {
                    _solidColorBrushCritical = new SolidColorBrush(_colorCritical);
                }
                return _solidColorBrushCritical;
            }
        }
        public SolidColorBrush SolidColorBrushError
        {
            get
            {
                if (_solidColorBrushError == null)
                {
                    _solidColorBrushError = new SolidColorBrush(_colorError);
                }
                return _solidColorBrushError;
            }
        }
        public SolidColorBrush SolidColorBrushWarning
        {
            get
            {
                if (_solidColorBrushWarning == null)
                {
                    _solidColorBrushWarning = new SolidColorBrush(_colorWarning);
                }
                return _solidColorBrushWarning;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoadProjectNames()
        {
            if (!Directory.Exists(StaticPaths.ProjectFolderPath))
                throw new FileNotFoundException($"Project folder does not exist. Make sure to place your projects in '{Path.Combine(Environment.CurrentDirectory, StaticPaths.ProjectFolderPath)}'.");
            foreach (string directory in Directory.GetDirectories(StaticPaths.ProjectFolderPath))
            {
                _availableProjects.Add(new Project(Path.GetFileName(directory)));
            }
        }

        private void LoadProjects()
        {
            foreach (Project project in _availableProjects)
            {
                string path = StaticPaths.ConvertNameToBasePath(project.Name);
                if (Directory.Exists(path))
                {
                    foreach (string directory in Directory.GetDirectories(path))
                    {
                        if (Path.GetFileName(directory) != StaticPaths.CommonLanguageFolderPath)
                        {
                            project.Languages.Add(Path.GetFileName(directory));
                        }
                    }
                }
                path = StaticPaths.ConvertNameToOfficialMapsPath(project.Name);
                if (Directory.Exists(path))
                {
                    foreach (string directory in Directory.GetDirectories(path))
                    {
                        project.Maps.Add(Path.GetFileName(directory));
                    }
                }
            }
        }

        private void LoadBuildScript()
        {
            CompilerResults results = new CSharpCodeProvider().CompileAssemblyFromFile(new CompilerParameters(new[] { "System.dll", "System.Core.dll", "Builder.exe" })
            {
                GenerateExecutable = false,
                GenerateInMemory = true
            }, _buildScript);
            if (results.Errors.Count > 0)
            {
                foreach (CompilerError error in results.Errors)
                {
                    DisplayLine(error.ErrorText);
                }
            }
            else
            {
                Type[] types = results.CompiledAssembly.GetTypes();
                for (int idx = 0; idx < types.Length; ++idx)
                {
                    foreach (Type type in types[idx].GetInterfaces())
                    {
                        if (type == typeof(IBuildScript))
                        {
                            ScriptInterface = types[idx].GetConstructor(Type.EmptyTypes).Invoke(Type.EmptyTypes) as IBuildScript;
                        }
                    }
                }
            }
            if (ScriptInterface == null) throw new InvalidOperationException($"No type found in {_buildScript} which implements interface '{nameof(IBuildScript)}'.");
        }

        private void ProjectSelectionChanged(object sender, RoutedEventArgs args)
        {
            SelectedProject = (sender as ComboBox).SelectedItem as string;
        }

        private void LoadSelectedProject(ComboBox comboBox)
        {
            if (_availableProjects.Count > 0)
            {
                foreach (Project project in _availableProjects)
                {
                    comboBox.Items.Add(project.Name);
                }
                if (_selectedProject == null)
                {
                    comboBox.SelectedIndex = 0;
                    _selectedProject = comboBox.SelectedItem as string;
                }
                else
                {
                    comboBox.SelectedItem = _selectedProject;
                }
            }
        }

        private void LoadSelectedProjectLanguage(ComboBox comboBox)
        {
            comboBox.Items.Clear();
            if (_selectedProject != null)
            {
                foreach (string language in _availableProjects.Find(x => x.Name == _selectedProject).Languages)
                {
                    comboBox.Items.Add(language);
                }
            }
        }

        private FrameworkElement GenerateElement(string name, GUIElementDataType type)
        {
            if (type == GUIElementDataType.CheckBox)
            {
                return new CheckBox
                {
                    Content = name
                };
            }
            if (type == GUIElementDataType.Input)
            {
                return new TextBox
                {
                    Text = name
                };
            }
            if (type == GUIElementDataType.ComboBox)
            {
                ComboBox comboBox = new ComboBox();
                if (name == "Project")
                {
                    LoadSelectedProject(comboBox);
                }
                else if (name == "Language")
                {
                    LoadSelectedProjectLanguage(comboBox);
                }
                return comboBox;
            }
            if (type == GUIElementDataType.Button)
            {
                return new Button
                {
                    Content = name
                };
            }
            if (type == GUIElementDataType.Label)
            {
                return new TextBlock
                {
                    Text = name
                };
            }
            if (type == GUIElementDataType.Separator)
            {
                return new Separator();
            }
            return null;
        }

        private FrameworkElement GenerateElement(string name, GUIElementDataType type, object status)
        {
            FrameworkElement result = GenerateElement(name, type);
            if (type == GUIElementDataType.CheckBox)
            {
                (result as CheckBox).IsChecked = (bool)status;
            }
            else if (type == GUIElementDataType.Input)
            {
                (result as TextBox).Text = (string)status;
            }
            else if (type == GUIElementDataType.ComboBox)
            {
                (result as ComboBox).SelectedItem = status;
                if (name == "Project")
                {
                    _selectedProject = (string)(result as ComboBox).SelectedItem;
                }
            }
            return result;
        }

        private object RetrieveGuiElementResult(GUIElementData elementData, Dictionary<string, FrameworkElement> guiData)
        {
            if (elementData.ControlType == GUIElementDataType.CheckBox)
            {
                return (guiData[elementData.DataKey] as CheckBox).IsChecked;
            }
            if (elementData.ControlType == GUIElementDataType.Input)
            {
                return (guiData[elementData.DataKey] as TextBox).Text;
            }
            if (elementData.ControlType == GUIElementDataType.ComboBox)
            {
                return (guiData[elementData.DataKey] as ComboBox).SelectedItem;
            }
            return null;
        }

        private Dictionary<string, object> GenerateGuiResult(List<GUIElementData> data, Dictionary<string, FrameworkElement> currentGuiData)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (GUIElementData elementData in data)
            {
                object obj = RetrieveGuiElementResult(elementData, currentGuiData);
                if (obj != null)
                {
                    result.Add(elementData.DataKey, obj);
                }
            }
            return result;
        }

        private void GeneratedButtonClick(object sender, RoutedEventArgs args)
        {
            ScriptInterface.SetGUIData(GenerateGuiResult(_inputGuiData, _generatedGuiData));
            GUIElementData elementData = _inputGuiData.Find(x => x.DataKey == ((sender as Button).Tag as string));
            elementData.ExecuteAction(elementData);
        }

        private Dictionary<string, FrameworkElement> GenerateGuiData(Grid grid, List<GUIElementData> data, Dictionary<string, object> storedGuiData, out List<GUIElementData> newInputData)
        {
            GridProject.Children.Clear();
            GridProject.RowDefinitions.Clear();
            grid.Children.Clear();
            grid.RowDefinitions.Clear();
            Dictionary<string, FrameworkElement> result = new Dictionary<string, FrameworkElement>();
            newInputData = new List<GUIElementData>();
            int idx = 0;
            RowDefinition row;
            FrameworkElement element;
            foreach (GUIElementData elementData in data)
            {
                if (elementData.DataKey == "project")
                {
                    element = storedGuiData == null || !storedGuiData.ContainsKey(elementData.DataKey) ? GenerateElement(elementData.Label, elementData.ControlType)
                                                                                                       : GenerateElement(elementData.Label, elementData.ControlType, storedGuiData[elementData.DataKey]);
                    (element as ComboBox).SelectionChanged += ProjectSelectionChanged;
                    result.Add(elementData.DataKey, element);
                    GridProject.Children.Add(element);
                    newInputData.Add(elementData);
                    continue;
                }
                if (elementData.DataKey == "map")
                {
                    if (_selectedProject != null)
                    {
                        foreach (string map in _availableProjects.Find(x => x.Name == _selectedProject).Maps)
                        {
                            string key = elementData.DataKey + map;
                            row = new RowDefinition();
                            grid.RowDefinitions.Add(row);
                            element = storedGuiData == null || !storedGuiData.ContainsKey(key) ? GenerateElement(map.Replace('_', ' '), elementData.ControlType)
                                                                                               : GenerateElement(map.Replace('_', ' '), elementData.ControlType, storedGuiData[key]);
                            result.Add(key, element);
                            grid.Children.Add(element);
                            Grid.SetRow(element, idx++);
                            newInputData.Add(new GUIElementData(key, map.Replace('_', ' '), elementData.ControlType));
                        }
                    }
                    continue;
                }
                row = new RowDefinition();
                grid.RowDefinitions.Add(row);
                if (elementData.ControlType == GUIElementDataType.Separator)
                {
                    row.Height = new GridLength(10.0);
                }
                element = storedGuiData == null || !storedGuiData.ContainsKey(elementData.DataKey) ? GenerateElement(elementData.Label, elementData.ControlType)
                                                                                                   : GenerateElement(elementData.Label, elementData.ControlType, storedGuiData[elementData.DataKey]);
                if (elementData.ControlType != GUIElementDataType.Separator && elementData.ControlType != GUIElementDataType.Button && elementData.ControlType != GUIElementDataType.Label)
                {
                    result.Add(elementData.DataKey, element);
                }
                if (elementData.ControlType == GUIElementDataType.Button)
                {
                    Button button = element as Button;
                    button.Tag = elementData.DataKey;
                    button.Click += GeneratedButtonClick;
                }
                grid.Children.Add(element);
                Grid.SetRow(element, idx++);
                newInputData.Add(elementData);
            }
            return result;
        }

        private void WindowLoaded(object sender, RoutedEventArgs args)
        {
            try
            {
                BuildHelper.SetBuildGUI(this);
                ImageSource iconError = Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Error.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                ImageSource iconExclamation = Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Exclamation.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                ImageSource iconInformation = Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Information.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                ErrorTreeViewItemLog errorLog = new ErrorTreeViewItemLog();
                errorLog.Children.Add(_treeViewItemCritical = new ErrorTreeViewItemCategory(iconError, "Critical Errors"));
                errorLog.Children.Add(_treeViewItemError = new ErrorTreeViewItemCategory(iconExclamation, "Errors"));
                errorLog.Children.Add(_treeViewItemWarning = new ErrorTreeViewItemCategory(iconInformation, "Warnings"));
                TreeViewErrorLog.DataContext = new TreeViewItemLogViewModel(errorLog);
                foreach (TreeViewItem item in TreeViewErrorLog.Items)
                {
                    item.IsExpanded = true;
                }
            }
            catch (Exception ex)
            {
                RichTextBoxLog.AppendText(ex.Message + '\r' + ex.StackTrace);
                return;
            }
            try
            {
                LoadProjectNames();
                LoadProjects();
            }
            catch (Exception ex)
            {
                DisplayException(ex);
                return;
            }
            Dictionary<string, object> storedGuiData = null;
            try
            {
                LoadBuildScript();
                string path = _buildScript + ".guidata";
                if (File.Exists(path))
                {
                    SHA1 sha1 = new SHA1CryptoServiceProvider();
                    byte[] hash;
                    using (FileStream stream = new FileStream(_buildScript, FileMode.Open, FileAccess.Read))
                    {
                        hash = sha1.ComputeHash(stream);
                    }
                    using (FileStream stream = new FileStream(path, FileMode.Open))
                    {
                        BinaryFormatter formatter = new BinaryFormatter();
                        byte[] data = formatter.Deserialize(stream) as byte[];
                        if (data.Length == hash.Length)
                        {
                            bool isValid = true;
                            for (int idx = 0; idx < data.Length; ++idx)
                            {
                                if (data[idx] != hash[idx])
                                {
                                    isValid = false;
                                    break;
                                }
                            }
                            if (isValid)
                            {
                                storedGuiData = formatter.Deserialize(stream) as Dictionary<string, object>;
                            }
                        }
                    }
                }
                ScriptInterface.Initialize();
            }
            catch (Exception ex)
            {
                DisplayException(ex);
                return;
            }
            try
            {
                _generatedGuiData = GenerateGuiData(GridBuildOptions, ScriptInterface.GuiData, storedGuiData, out _inputGuiData);
            }
            catch (Exception ex)
            {
                DisplayException(ex);
            }
        }

        private void WindowClosing(object sender, CancelEventArgs args)
        {
            if (ScriptInterface == null)
            {
                return;
            }
            Dictionary<string, object> guiResult = GenerateGuiResult(_inputGuiData, _generatedGuiData);
            string path = _buildScript + ".guidata";
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            SHA1 sha1 = new SHA1CryptoServiceProvider();
            byte[] hash;
            using (FileStream stream = new FileStream(_buildScript, FileMode.Open, FileAccess.Read))
            {
                hash = sha1.ComputeHash(stream);
            }
            using (FileStream stream = new FileStream(path, FileMode.Create))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, hash);
                formatter.Serialize(stream, guiResult);
            }
        }

        private void OnBuildStart()
        {
            if (BuildHelper.UseBenchmarking)
            {
                _buildTimeStart = DateTime.UtcNow.Ticks;
            }
        }

        private void CompileClick(object sender, RoutedEventArgs args)
        {
            OnBuildStart();
            BuildHelper.StartBuild(_selectedProject);
            DisplayGuiLine($"Building Mod {_selectedProject}...");
            ScriptInterface.SetGUIData(GenerateGuiResult(_inputGuiData, _generatedGuiData));
            ScriptInterface.Build(_selectedProject);
        }

        private void ErrorLogDoubleClick(object sender, MouseButtonEventArgs args)
        {
            if (args.Source is TreeViewItem tvi && tvi.Header is TreeViewItemErrorViewModel vm)
            {
                RichTextBoxLog.ScrollToHome();
                RichTextBoxLog.ScrollToVerticalOffset(vm.Y);
                TabControlLog.SelectedIndex = 0;
                args.Handled = true;
            }
        }

        private bool IsBreakingLine(string line)
        {
            return line.Contains("Critical:") || line.Contains("Error:") || line.Contains("Warning:");
        }

        private string ProcessLine(string line)
        {
            if (line.Contains("Critical:"))
            {
                _isFailed = true;
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, (Action<string>)((dump) =>
                {
                    DisplayCriticalLine(dump);
                }), line);
                return string.Empty;
            }
            if (line.Contains("Error:"))
            {
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, (Action<string>)((dump) =>
                {
                    DisplayErrorLine(dump);
                }), line);
                return string.Empty;
            }
            if (line.Contains("Warning:"))
            {
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, (Action<string>)((dump) =>
                {
                    DisplayWarningLine(dump);
                }), line);
                return string.Empty;
            }
            return line + '\r';
        }

        private void OnStepSuccess()
        {
            if (BuildHelper.UseBenchmarking)
            {
                DisplayGuiLine($"Step time taken: {TimeSpan.FromTicks(DateTime.UtcNow.Ticks - _stepTimeStart)}.");
            }
            if (_isLastPart)
            {
                ScriptInterface.OnStepSuccess();
            }
            else
            {
                ScriptInterface.OnPartSuccess();
            }
        }

        private void OnStepFailure()
        {
            _isFailed = true;
            ScriptInterface.OnStepFailure();
        }

        private void ExecutableExited(object sender, EventArgs args)
        {
            _isProcessRunning = false;
            if (_isRedirectingOutput)
            {
                string output = string.Empty;
                string line;
                while ((line = _currentRunningProcess.StandardOutput.ReadLine()) != null)
                {
                    if (IsBreakingLine(line))
                    {
                        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, (Action<string>)((dump) =>
                        {
                            DisplayDataLine(dump);
                        }), output);
                        output = string.Empty;
                    }
                    output += ProcessLine(line);
                }
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, (Action<string>)((dump) =>
                {
                    DisplayDataLine(dump);
                }), output);
            }
            if (_isFailed)
            {
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, (Action)(() =>
                {
                    OnStepFailure();
                }));
            }
            else
            {
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, (Action)(() =>
                {
                    OnStepSuccess();
                }));
            }
        }

        private void ReadProcessOutput()
        {
            try
            {
                string text = string.Empty;
                string line;
                for (int idx = 0; (line = _currentRunningProcess.StandardOutput.ReadLine()) != null && idx < 15 && !IsBreakingLine(line); ++idx)
                {
                    text += ProcessLine(line);
                }
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, (Action<string>)((dump) =>
                {
                    DisplayDataLine(dump);
                }), text);
                if (line == null)
                {
                    return;
                }
                ProcessLine(line);
            }
            catch (Exception ex)
            {
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, (Action)(() =>
                {
                    DisplayException(ex);
                }));
            }
        }

        private void ReadProcessOutputThread()
        {
            while (_isProcessRunning)
            {
                ReadProcessOutput();
                Thread.Sleep(125);
            }
        }

        private bool LaunchExecutable(RunExecutableArguments args)
        {
            if (!File.Exists(args.FileName))
            {
                ProcessLine($"Critical: Failed to find executable '{args.FileName}'.");
                return false;
            }
            _currentRunningProcess = new Process();
            _currentRunningProcess.StartInfo.FileName = args.FileName;
            _currentRunningProcess.StartInfo.Arguments = args.Args;
            _currentRunningProcess.StartInfo.CreateNoWindow = !args.IsCreatingWindow;
            _currentRunningProcess.StartInfo.UseShellExecute = args.IsCreatingWindow;
            _currentRunningProcess.StartInfo.RedirectStandardError = args.IsRedirectingOutput;
            _currentRunningProcess.StartInfo.RedirectStandardOutput = args.IsRedirectingOutput;
            _currentRunningProcess.Exited += ExecutableExited;
            _currentRunningProcess.EnableRaisingEvents = true;
            _isRedirectingOutput = args.IsRedirectingOutput;
            _currentRunningProcess.Start();
            _isProcessRunning = true;
            if (args.IsRedirectingOutput)
            {
                _currentProcessOutputReader = new Thread(new ThreadStart(ReadProcessOutputThread));
                _currentProcessOutputReader.Start();
            }
            return true;
        }

        private void CopyFiles(CopyFilesArguments args)
        {
            string sourceDirectoryName = args.Source;
            if (!Directory.Exists(sourceDirectoryName))
            {
                DisplayWarningLine($"Directory '{sourceDirectoryName}' does not exist.");
                OnStepSuccess();
                return;
            }
            string targetDirectoryName = args.Target;
            try
            {
                if (!Directory.Exists(targetDirectoryName))
                {
                    Directory.CreateDirectory(targetDirectoryName);
                    DisplayLine($"Directory '{targetDirectoryName}' created.");
                }
                foreach (string file in Directory.EnumerateFiles(sourceDirectoryName, args.Include, SearchOption.AllDirectories).Where(x => !x.Contains(args.Exclude)))
                {
                    string fileName = Path.GetFileName(file);
                    string outputPath = Path.Combine(targetDirectoryName, Path.GetDirectoryName(file).Substring(sourceDirectoryName.Length).TrimStart(Path.DirectorySeparatorChar));
                    if (!Directory.Exists(outputPath))
                    {
                        Directory.CreateDirectory(outputPath);
                        DisplayLine($"Directory '{outputPath}' created.");
                    }
                    File.Copy(file, Path.Combine(outputPath, fileName), true);
                    DisplayLine($"File '{file}' copied.");
                }
                OnStepSuccess();
            }
            catch (Exception ex)
            {
                DisplayException(ex);
                OnStepFailure();
            }
        }

        private void DeleteFilesInternal(DirectoryInfo directory)
        {
            foreach (DirectoryInfo subDirectory in directory.EnumerateDirectories())
            {
                DeleteFilesInternal(subDirectory);
                subDirectory.Delete();
            }
            foreach (FileInfo file in directory.EnumerateFiles())
            {
                file.Delete();
            }
        }

        private void DeleteFiles(DeleteFilesArguments args)
        {
            string directoryName = args.Target;
            if (Directory.Exists(directoryName))
            {
                DeleteFilesInternal(new DirectoryInfo(directoryName));
            }
            else
            {
                DisplayWarningLine($"Directory '{directoryName}' does not exist.");
            }
            OnStepSuccess();
        }

        private void WriteFile(WriteFileArguments args)
        {
            try
            {
                string directoryName = Path.GetDirectoryName(args.Target);
                if (!Directory.Exists(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                    DisplayLine($"Directory '{directoryName}' created.");
                }
                File.WriteAllText(args.Target, args.Content);
                DisplayLine($"File '{args.Target}' written.");
                OnStepSuccess();
            }
            catch (Exception ex)
            {
                DisplayException(ex);
                OnStepFailure();
            }
        }

        private void MergeFiles(MergeFilesArguments args)
        {
            try
            {
                string directoryName = Path.GetDirectoryName(args.Target);
                if (!Directory.Exists(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                    DisplayLine($"Directory '{directoryName}' created.");
                }
                using (FileStream stream = new FileStream(args.Target, FileMode.Create, FileAccess.Write))
                {
                    foreach (string file in args.Sources)
                    {
                        using (FileStream input = new FileStream(file, FileMode.Open, FileAccess.Read))
                        {
                            input.CopyTo(stream);
                        }
                    }
                }
                DisplayLine($"File '{args.Target}' written.");
                OnStepSuccess();
            }
            catch (Exception ex)
            {
                DisplayException(ex);
                OnStepFailure();
            }
        }

        private void CreateStandalone(CreateStandaloneArguments args)
        {
            string installPath = args.InstallPath;
            if (string.Equals(installPath, "null"))
            {
                DisplayWarningLine("No install path detected.");
                OnStepSuccess();
                return;
            }
            try
            {
                StringBuilder skuDef = new StringBuilder();
                skuDef.Append(args.SkuDefText);
                string[] originalSkuDef = File.ReadAllLines(Path.Combine(installPath, $"CNC3_{args.Language}_1.9.SkuDef"));
                for (int idx = 1; idx < originalSkuDef.Length; ++idx)
                {
                    skuDef.AppendLine(originalSkuDef[idx]);
                }
                string skuDefPath = Path.Combine(installPath, $"CNC3_{args.Language}_{args.Version}.SkuDef");
                File.WriteAllText(skuDefPath, skuDef.ToString());
                DisplayLine($"File '{skuDefPath}' written.");
                // if (!Directory.Exists(outputRetailExe))
                // {
                //     Directory.CreateDirectory(outputRetailExe);
                //     DisplayLine($"Directory '{outputRetailExe}' created.");
                // }
                // foreach (string file in Directory.EnumerateFiles(Path.Combine(installPath, gameRetailExe)))
                // {
                //     File.Copy(file, Path.Combine(outputRetailExe, Path.GetFileName(file)), true);
                //     DisplayLine($"File '{file}' copied.");
                // }
                // // TODO: remove when build is more advanced and does not need og data at all
                // string originalSkuDefPath = Path.Combine(installPath, $"CNC3_{args.Language}_1.9.SkuDef");
                // StringBuilder skuDefBuilder = new StringBuilder();
                // skuDefBuilder.AppendLine($"set-exe {Path.Combine("RetailExe", "cnc3game.dat")}");
                // skuDefBuilder.AppendLine($"add-search-path {args.Target.Substring(args.BaseTarget.Length + 1)};{args.LanguageTarget.Substring(args.BaseTarget.Length + 1)}");
                // string[] originalSkuDef = File.ReadAllLines(originalSkuDefPath);
                // for (int idx = 1; idx < originalSkuDef.Length; ++idx)
                // {
                //     skuDefBuilder.AppendLine(originalSkuDef[idx]);
                // }
                // File.WriteAllText(Path.Combine(args.BaseTarget, $"CNC3_{args.Language}_{args.Version}.SkuDef"), skuDefBuilder.ToString());
                // DisplayLine($"File '{Path.Combine(args.BaseTarget, $"CNC3_{args.Language}_{args.Version}.SkuDef")}' written.");
                OnStepSuccess();
            }
            catch (Exception ex)
            {
                DisplayException(ex);
                OnStepFailure();
            }
        }

        private void RunBuildStepInternal(int step, StepType stepType, object args)
        {
            if (BuildHelper.UseBenchmarking)
            {
                _stepTimeStart = DateTime.UtcNow.Ticks;
            }
            _currentStep = step;
            if (stepType == StepType.StepOver)
            {
                OnStepSuccess();
                return;
            }
            if (args == null)
            {
                ProcessLine("Critical: Invalid arguments.");
                OnStepFailure();
                return;
            }
            switch (stepType)
            {
                case StepType.RunExecutable:
                    if (!LaunchExecutable((RunExecutableArguments)args))
                    {
                        OnStepFailure();
                    }
                    break;
                case StepType.CopyFiles:
                    CopyFiles((CopyFilesArguments)args);
                    break;
                case StepType.DeleteFiles:
                    DeleteFiles((DeleteFilesArguments)args);
                    break;
                case StepType.WriteFile:
                    WriteFile((WriteFileArguments)args);
                    break;
                case StepType.MergeFiles:
                    MergeFiles((MergeFilesArguments)args);
                    break;
                case StepType.CreateStandalone:
                    CreateStandalone((CreateStandaloneArguments)args);
                    break;
                default:
                    throw new InvalidOperationException($"Critical: Operation {stepType} has not been implemented.");
            }
        }

        public void DisplayException(Exception ex)
        {
            ErrorTreeViewItemError error = new ErrorTreeViewItemError(ex.Message)
            {
                Y = RichTextBoxLog.Document.ContentEnd.GetLineStartPosition(0).GetCharacterRect(LogicalDirection.Backward).Y
            };
            error.Children.Add(new ErrorTreeViewItemErrorStackTrace(ex.StackTrace));
            _treeViewItemCritical.Children.Add(error);
            TextRange range = new TextRange(RichTextBoxLog.Document.ContentEnd, RichTextBoxLog.Document.ContentEnd)
            {
                Text = ex.Message + "\r"
            };
            if (_brushStandard == null)
            {
                _brushStandard = range.GetPropertyValue(TextElement.ForegroundProperty);
            }
            range.ApplyPropertyValue(TextElement.ForegroundProperty, SolidColorBrushCritical);
            RichTextBoxLog.ScrollToEnd();
        }

        public void DisplayDataLine(string text)
        {
            TextRange range = new TextRange(RichTextBoxLog.Document.ContentEnd, RichTextBoxLog.Document.ContentEnd)
            {
                Text = text
            };
            if (_brushStandard == null)
            {
                _brushStandard = range.GetPropertyValue(TextElement.ForegroundProperty);
            }
            range.ApplyPropertyValue(TextElement.ForegroundProperty, _brushStandard);
            RichTextBoxLog.ScrollToEnd();
        }

        public void DisplayGuiLine(string text)
        {
            TextRange range = new TextRange(RichTextBoxLog.Document.ContentEnd, RichTextBoxLog.Document.ContentEnd)
            {
                Text = text + "\r"
            };
            if (_brushStandard == null)
            {
                _brushStandard = range.GetPropertyValue(TextElement.ForegroundProperty);
            }
            range.ApplyPropertyValue(TextElement.ForegroundProperty, SolidColorBrushGreen);
            RichTextBoxLog.ScrollToEnd();
        }

        public void DisplayCriticalLine(string text)
        {
            _treeViewItemCritical.Children.Add(new ErrorTreeViewItemError(text.Substring(text.IndexOf(':') + 1))
            {
                Y = RichTextBoxLog.Document.ContentEnd.GetLineStartPosition(0).GetCharacterRect(LogicalDirection.Backward).Y
            });
            TextRange range = new TextRange(RichTextBoxLog.Document.ContentEnd, RichTextBoxLog.Document.ContentEnd)
            {
                Text = text + "\r"
            };
            if (_brushStandard == null)
            {
                _brushStandard = range.GetPropertyValue(TextElement.ForegroundProperty);
            }
            range.ApplyPropertyValue(TextElement.ForegroundProperty, SolidColorBrushCritical);
            RichTextBoxLog.ScrollToEnd();
        }

        public void DisplayErrorLine(string text)
        {
            _treeViewItemError.Children.Add(new ErrorTreeViewItemError(text.Substring(text.IndexOf(':') + 1))
            {
                Y = RichTextBoxLog.Document.ContentEnd.GetLineStartPosition(0).GetCharacterRect(LogicalDirection.Backward).Y
            });
            TextRange range = new TextRange(RichTextBoxLog.Document.ContentEnd, RichTextBoxLog.Document.ContentEnd)
            {
                Text = text + "\r"
            };
            if (_brushStandard == null)
            {
                _brushStandard = range.GetPropertyValue(TextElement.ForegroundProperty);
            }
            range.ApplyPropertyValue(TextElement.ForegroundProperty, SolidColorBrushError);
            RichTextBoxLog.ScrollToEnd();
        }

        public void DisplayWarningLine(string text)
        {
            _treeViewItemWarning.Children.Add(new ErrorTreeViewItemError(text.Substring(text.IndexOf(':') + 1))
            {
                Y = RichTextBoxLog.Document.ContentEnd.GetLineStartPosition(0).GetCharacterRect(LogicalDirection.Backward).Y
            });
            TextRange range = new TextRange(RichTextBoxLog.Document.ContentEnd, RichTextBoxLog.Document.ContentEnd)
            {
                Text = text + "\r"
            };
            if (_brushStandard == null)
            {
                _brushStandard = range.GetPropertyValue(TextElement.ForegroundProperty);
            }
            range.ApplyPropertyValue(TextElement.ForegroundProperty, SolidColorBrushWarning);
            RichTextBoxLog.ScrollToEnd();
        }

        public void RunBuildStep(int step, StepType stepType, object args)
        {
            _isLastPart = true;
            RunBuildStepInternal(step, stepType, args);
        }

        public bool RunBuildStepPart(int step, bool isLastPart, StepType stepType, object args)
        {
            throw new System.NotImplementedException();
        }

        public void Reset()
        {
            _isFailed = false;
        }

        public void DisplayLine(string text)
        {
            TextRange range = new TextRange(RichTextBoxLog.Document.ContentEnd, RichTextBoxLog.Document.ContentEnd)
            {
                Text = text + "\r"
            };
            if (_brushStandard != null)
            {
                range.ApplyPropertyValue(TextElement.ForegroundProperty, _brushStandard);
            }
            RichTextBoxLog.ScrollToEnd();
        }

        public void ResetBuildLog()
        {
            RichTextBoxLog.Document.Blocks.Clear();
            RichTextBoxLog.ScrollToHome();
            _treeViewItemCritical.Children.Clear();
            _treeViewItemError.Children.Clear();
            _treeViewItemWarning.Children.Clear();
        }

        public void OnBuildComplete()
        {
            DisplayGuiLine("Build completed.");
            if (BuildHelper.UseBenchmarking)
            {
                DisplayGuiLine($"Build time: {TimeSpan.FromTicks(DateTime.UtcNow.Ticks - _buildTimeStart)}.");
            }
        }

        public void SetColors(System.Windows.Media.Color critical, System.Windows.Media.Color error, System.Windows.Media.Color warning)
        {
            _colorCritical = critical;
            _colorError = error;
            _colorWarning = warning;
        }

        public void SelectMap(string map)
        {
            throw new System.NotImplementedException();
        }

        public void DeselectMap(string map)
        {
            throw new System.NotImplementedException();
        }
    }
}
