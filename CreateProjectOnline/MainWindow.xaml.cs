using System.Text;
using System.Windows;
using CreateProjectOnline;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Diagnostics;

namespace CreateProjectOnline
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Controller _controller;
        public string selectOrganization;
        public string projectName;
        public string projectLocation;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            string[] selectOrg = {""};
            if (comboBox != null)
            {
                selectOrg = comboBox.SelectedItem.ToString().Split(':');
                selectOrganization = selectOrg[1];
            }
            Debug.WriteLine("What is in dd: "+selectOrganization);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
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
            }
        }

        private void ProjectNameChanged(object sender, TextChangedEventArgs e)
        {
            // Get the TextBox that triggered the event
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                projectName = textBox.Text;
                Debug.WriteLine($"Project Name: {projectName}");
            }
        }

        private void CreateProject(object sender, RoutedEventArgs e)
        {
            _controller = new Controller(selectOrganization, projectName, projectLocation);
            _controller.CreateProjectOnline();
        }
    }
}