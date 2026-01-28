using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace CreateProjectOnline
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Controller _controller;
        private string selectOrganization;
        private string selectVersion;
        private string projectName;
        private string projectLocation;
        private string tempRepoName;

        public MainWindow()
        {
            InitializeComponent();
            _controller = new Controller(); 
            PlasticVersion.Content = "Using PlasticSCM version: " + _controller.PlasticVersion();
            _controller.IsPlasticLogedIn();
            SelectOrganizationDdItem.Content = _controller.GetOrganization();
            _controller.LoadDownloadedWorkspace();
            CheckDebugMode();
        }

        private async void CreateProject(object sender, RoutedEventArgs e)
        {
            try
            {
                OperationProgressBar.Visibility = Visibility.Visible;
                ProgressBarComment2.Visibility = Visibility.Visible;
                CreateButton.IsEnabled = false; 
                OperationProgressBar.Value = 0;
                var progress = new Progress<int>(value =>
                {
                    OperationProgressBar.Value = OperationProgressBar.Value + value;
                    ProgressBarComment2.Content = _controller.CommentProgress();
                });
                await Task.Run(() =>
                {
                    _controller.CreateProjectOnline(progress);
                });
            }
            catch (Exception ex)
            {
                _controller.DebugPopup ("" + ex.Message, "Unknown Error!", MessageBoxImage.Error);
            }
            finally
            {
                OperationProgressBar.Value = 100;
                //OperationProgressBar.Visibility = Visibility.Collapsed;
                ProgressBarComment.Visibility = Visibility.Visible;
                ProgressBarComment.Content = _controller.CommentComplete();
                ProgressColorValidate();
                ProgressBarComment2.Visibility = Visibility.Collapsed;
                await _controller.DelayShutdown(5000);
            }
        }

        #region Events

        private void ComboBox_OrganizationSelection(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            string[] selectOrg = {""};
            if (comboBox != null)
            {
                selectOrg = comboBox.SelectedItem.ToString().Split(':');
                selectOrganization = selectOrg[1];
                _controller.PlasticOrganization = selectOrganization;
            }
            Debug.WriteLine("What is in dd:"+selectOrganization);
            _controller.GetAllRepo();
            UpdateCreateButtonState();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            // Corrected code: OpenFolderDialog does not support 'Filter'. Removed the invalid property.  
            var dialog = new OpenFolderDialog
            {
                Title = "Select the project folder"
            };

            if (dialog.ShowDialog() == true)
            {
                string selectedPath = dialog.FolderName; // Use FolderName to get the selected folder path.  
                Debug.WriteLine($"Selected folder: {selectedPath}");
                // Optionally, assign to your projectLocation variable.  
                this.projectLocation = selectedPath;
                ProjectLocationTxt.Text = this.projectLocation;
                _controller.NewProjectLocation = this.projectLocation;
            }
        }

        private void ProjectNameChanged(object sender, TextChangedEventArgs e)
        {
            // Get the TextBox that triggered the event
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                projectName = textBox.Text;
                _controller.NewProjectName = projectName;
                ProjectNameValidation(projectName);
                FolderExistsValidation();
                Debug.WriteLine($"Project Name: {projectName}");
            }
            UpdateCreateButtonState();
        }

        private void ProjectLocationTxt_TextChanged(object sender, TextChangedEventArgs e)
        {
            ProjectNameValidation(projectName);
            FolderExistsValidation();
            UpdateCreateButtonState();
        }

        private void ComboBox_SelectionVersion(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            string[] selectOrg = { "" };
            if (comboBox != null)
            {
                selectOrg = comboBox.SelectedItem.ToString().Split(':');
                selectVersion = selectOrg[1];
                _controller.UnityVersion = selectVersion;
            }
            Debug.WriteLine("What is in version dd:" + selectVersion);
            UpdateCreateButtonState();
        }

        #endregion

        #region HelperMethods

        private void UpdateCreateButtonState()
        {
            CreateButton.IsEnabled =
                !string.IsNullOrWhiteSpace(selectOrganization) &&
                !string.IsNullOrWhiteSpace(projectName) &&
                !string.IsNullOrWhiteSpace(projectLocation) &&
                !(ProjectNameValidate.Content.Equals("Project already exist in PlasticSCM") ||
                ProjectNameValidate.Content.Equals("Folder already exists"));
        }

        private void ProjectNameValidation(string projectName)
        {
            foreach (var repoName in Controller.repositoryNames)
            {
                if (projectName != repoName)
                {
                    ProjectNameValidate.Content = "";
                    continue;
                }
                else
                {
                    ProjectNameValidate.Content = "Project already exist in PlasticSCM";
                    tempRepoName = repoName;
                    break;
                }
            }
        }

        private void FolderExistsValidation()
        {
            if (string.IsNullOrEmpty(projectLocation) || string.IsNullOrEmpty(projectName))
            {
                return;
            }

            string fullPath = Path.Combine(projectLocation, projectName);
            bool folderExists = Directory.Exists(fullPath);

            if (folderExists)
            {
                if (projectName == tempRepoName)
                {
                    ProjectNameValidate.Content = "Project already exist in PlasticSCM";
                }
                else
                {
                    ProjectNameValidate.Content = "Folder already exists";
                }
            }
            else
            {
                // Only clear if not already set by ProjectNameValidation
                if (ProjectNameValidate.Content?.ToString() == "Folder already exists")
                {
                    ProjectNameValidate.Content = "";
                }
            }
        }

        private void ProgressColorValidate()
        {
            if (!_controller.IsProjectDownloaded || _controller.AlreadyEditorOpen)
            {
                OperationProgressBar.Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        private void CheckDebugMode()
        {
            if (_controller.CheckError) 
            {
                DebugMode.Visibility = Visibility.Visible;
            }
            else
            {
                DebugMode.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

    }
}