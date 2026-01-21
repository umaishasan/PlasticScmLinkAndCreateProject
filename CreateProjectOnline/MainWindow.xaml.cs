using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Diagnostics;

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

        public MainWindow()
        {
            _controller = new Controller(); 
            InitializeComponent();
            PlasticVersion.Content = "Using PlasticSCM version: " + _controller.PlasticVersion();
            _controller.IsPlasticLogedIn();
            SelectOrganizationDdItem.Content = _controller.GetOrganization();
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
                MessageBox.Show(ex.Message, "Error");
            }
            finally
            {
                OperationProgressBar.Value = 100;
                //OperationProgressBar.Visibility = Visibility.Collapsed;
                ProgressBarComment.Visibility = Visibility.Visible;
                ProgressBarComment.Content = _controller.CommentComplete();
                ProgressColorValidate();
                ProgressBarComment2.Visibility = Visibility.Collapsed;
                await _controller.DelayShutdown(3000);
            }
        }

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
            var dialog = new Microsoft.Win32.OpenFolderDialog
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
                //Debug.WriteLine($"Project Name: {projectName}");
            }
            UpdateCreateButtonState();
        }

        private void ProjectLocationTxt_TextChanged(object sender, TextChangedEventArgs e)
        {
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

        #region HelperMethods

        private void UpdateCreateButtonState()
        {
            CreateButton.IsEnabled =
                !string.IsNullOrWhiteSpace(selectOrganization) &&
                !string.IsNullOrWhiteSpace(projectName) &&
                !string.IsNullOrWhiteSpace(projectLocation)&&
                !ProjectNameValidate.Content.Equals("Project name already exist.");
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
                ProjectNameValidate.Content = "Project name already exist.";
                break;
            }
        }

        private void ProgressColorValidate()
        {
            if (!_controller.isContentWorkflowDownloaded)
            {
                OperationProgressBar.Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        #endregion

    }
}