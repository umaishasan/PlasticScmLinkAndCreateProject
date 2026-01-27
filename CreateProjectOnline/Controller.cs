using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Management;

namespace CreateProjectOnline
{
    public class Controller
    {
        #region Variables

        public string server = "@cloud";
        public string fullServerName;
        public string comment = "";
        public string templateProjectName = "DTH_Content_Workflow";

        private string templateProject;
        private string templateProjectPath;
        private string templateProjectPathDirective;
        private string templateProjectChangeset;

        private bool isUndoChangeset = false;
        private bool istemplateProjectDownloaded;
        private bool isTherePendingChanges = false;
        private bool isEditorOpen = false;
        private bool isErrorBool = true;

        public string[] Versions = { " Unity 2022", " Unity 06" };
        public List<string> downloadedWorkspaces = new();
        public static List<string> repositoryNames = new();
        public List<int> mainChangesets = new();
        public List<int> sixChangesets = new();

        private string selectOrganization;
        private string projectName;
        private string projectLocation;
        private string unityVersion;

        private PlasticCmdQuery plasticCmdQuery;

        #endregion

        #region Constructor

        public Controller()
        {
            plasticCmdQuery = new PlasticCmdQuery();
        }
        public string PlasticOrganization
        {
            get => selectOrganization;
            set
            {
                selectOrganization = value;
            }
        }
        public string NewProjectName
        {
            get => projectName;
            set
            {
                projectName = value;
            }
        }
        public string NewProjectLocation
        {
            get => projectLocation;
            set
            {
                projectLocation = value;
            }
        }
        public string UnityVersion
        {
            get => unityVersion;
            set
            {
                unityVersion = value;
            }
        }
        public bool CheckError
        {
            get => isErrorBool;
            set
            {
                isErrorBool = value;
            }
        }
        public bool IsProjectDownloaded
        {
            get => istemplateProjectDownloaded;
            set
            {
                istemplateProjectDownloaded = value;
            }
        }
        public bool AlreadyEditorOpen
        {
            get => isEditorOpen;
            set
            {
                isEditorOpen = value;
            }
        }

        #endregion

        public async Task CreateProjectOnline(IProgress<int> progress)
        {
            ///When Dth_Content_Workflow editor is not open
            
            progress.Report(10);
            comment = $"Check {templateProjectName} project downloaded or not.";
            CheckContentWorkflowDownloaded();
            IsContentWorkflowEditorOpen();
            
            if (istemplateProjectDownloaded && !isEditorOpen)
            {
                progress.Report(10);
                comment = "Createing folder for new project.";
                CreateFolderForNewProject();
                progress.Report(10);
                comment = "Check pending changes.";
                CheckPendingChanges();
                progress.Report(10);
                comment = "Undo changeset.";
                UndoChangeset();
                progress.Report(10); //50
                comment = unityVersion == Versions[0] ? "Switch to main's latest changeset." : "Switch to Unity6's latest changeset.";
                SwitchToMainChangeset();
                progress.Report(5);
                comment = $"Check {templateProjectName} current changeset number.";
                CheckContentWorkflowChangeset();
                progress.Report(5);
                comment = "Create new repository for the new project.";
                CreateNewRepository();
                progress.Report(15);
                comment = $"Copying all files from {templateProjectName} to new repository.";
                await CopyingAllFilesInNewRepository(templateProjectPath, projectLocation);
                progress.Report(5);
                comment = "Copying files and folder successfully!";
                Debug.WriteLine("Copying files and folder successfully");
                progress.Report(15);
                comment = "Adding and Checking (push) files to new repository.";
                await AddAndCheckinFilesInNewRepository();
                progress.Report(5);
                Debug.WriteLine("Add and Checkin files successfully");
            }
        }

        #region CustomMethod

        private void CheckContentWorkflowDownloaded()
        {
            if (isErrorBool)
            {
                MessageBox.Show($"Checking {templateProjectName} project downloaded or not.", "Checking", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            bool found = istemplateProjectDownloaded = false;
            foreach (var line in downloadedWorkspaces)
            {
                //Debug.WriteLine("Workspace: " + line);
                var nameSplited = line.Split('@');
                var pathSplited = line.Split(':').LastOrDefault();
                var directiveSplited = line.Split(':').FirstOrDefault().ToString().Split(' ').LastOrDefault();
                //Debug.WriteLine("Workspace path: " + pathSplited);
                if (nameSplited[0] == templateProjectName)
                {
                    templateProject = nameSplited[0];
                    string pathContentWorkflow = pathSplited;
                    templateProjectPathDirective = directiveSplited;
                    templateProjectPath = templateProjectPathDirective + ":" + pathContentWorkflow;
                    if (isErrorBool)
                    {
                        MessageBox.Show($"ProjectName: {templateProject}, ProjectPath: {templateProjectPath}", "Project Found!", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    found = istemplateProjectDownloaded = true;
                    Debug.WriteLine($"Is this same: {nameSplited[0]} or {templateProject}");
                    Debug.WriteLine($"Project path: {pathSplited} or {templateProjectPath}");
                    break; // Stop searching after finding the desired workspace
                }
            }
            if (!found)
            {
                istemplateProjectDownloaded = false;
                var result = MessageBox.Show($"The {templateProjectName} project is not downloaded. Please download the latest version from main.", $"{templateProjectName} Not Found",MessageBoxButton.OK,MessageBoxImage.Warning);                
                
                if(result == MessageBoxResult.OK)
                {
                    Debug.WriteLine("Is this main window close ? "+result);
                    return;
                }
            }
            GetMainChangeset();
            GetSixChangeset();
        }

        private void CreateFolderForNewProject()
        {
            if (istemplateProjectDownloaded)
            {
                string projectPath = projectLocation + "\\" + projectName;
                if (!Directory.Exists(projectPath))
                {
                    Directory.CreateDirectory(projectPath);
                    projectLocation = projectPath;
                }
            }
            if (isErrorBool)
            {
                MessageBox.Show("Folder Creation for new project", "Folder Create", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CheckPendingChanges()
        {
            var result = RunCmdWithOutput(plasticCmdQuery.UndoChanges, templateProjectPath);
            if(isErrorBool)
            {
                MessageBox.Show("Now You are in CheckPendingChanges method", "Check Pending Changes", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            if (string.IsNullOrEmpty(result))
            {
                if (isErrorBool)
                {
                    MessageBox.Show("No pending changes found.", "Check Pending Changes", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                isTherePendingChanges = false;
                Debug.WriteLine("No pending changes found. "+ isTherePendingChanges);
            }
            else
            {
                if (isErrorBool)
                {
                    MessageBox.Show("Pending changes exist.", "Check Pending Changes", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                isTherePendingChanges = true;
                Debug.WriteLine("Pending changes exist. "+ isTherePendingChanges);
            }
        }

        private void UndoChangeset()
        {
            if (isErrorBool)
            {
                MessageBox.Show("Now You are in Undo method", "Undo Changeset", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            ///Unity 2022
            if (unityVersion == Versions[0])
            {
                Debug.WriteLine("Unity version: " + unityVersion);
                ///check current changeset and main latest changeset not equal then undo
                if (isTherePendingChanges)
                {
                    if (isErrorBool)
                    {
                        MessageBox.Show($"isTherePendingChanges: {isTherePendingChanges}", "Pending Changes Avail", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    ///undo all changes
                    var result = MessageBox.Show($"The current changeset of {templateProjectName} will undo all the work.", "Undo changeset", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    isUndoChangeset = (result == MessageBoxResult.Yes);
                    if (isUndoChangeset)
                    {
                        RunCmdWithOutput(plasticCmdQuery.UndoChanges, templateProjectPath);
                        RunCmdWithOutput(plasticCmdQuery.RefreshStatus, templateProjectPath);
                        Debug.WriteLine($"Undo all changes: ");

                        // Find and delete all files in "Added" state
                        var statusOutput = RunCmdWithOutput(plasticCmdQuery.NotDeductedAddedFiles, templateProjectPath);
                        bool inAddedSection = false;
                        foreach (var line in statusOutput.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            var trimmed = line.Trim();
                            if (trimmed.Equals("Added", StringComparison.OrdinalIgnoreCase))
                            {
                                inAddedSection = true;
                                continue;
                            }

                            //Debug.WriteLine("----------> Processing line: " + trimmed);
                            if (inAddedSection && trimmed.StartsWith("Private"))
                            {
                                var parts = trimmed.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length > 0)
                                {
                                    var assetPath = trimmed.Split("Assets").LastOrDefault();
                                    var fullPath = Path.Combine(templateProjectPath, "Assets" + assetPath);
                                    try
                                    {
                                        //Debug.WriteLine("Full path to delete: " + fullPath);
                                        if (File.Exists(fullPath))
                                        {
                                            File.Delete(fullPath);
                                            Debug.WriteLine($"Deleted added file: {fullPath}");
                                        }
                                        if (Directory.Exists(fullPath))
                                        {
                                            Directory.Delete(fullPath, true);
                                            Debug.WriteLine($"Deleted added directory: {fullPath}");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"Failed to delete {fullPath}: {ex.Message}");
                                        if (isErrorBool)
                                        {
                                            MessageBox.Show("Failed to delete " + fullPath + ": " + ex.Message, "Deletion Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (isErrorBool)
                        {
                            MessageBox.Show("You select 'No' from undo popup", "Do not Undo", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        Debug.WriteLine($"Main latest: {templateProjectChangeset} => Already in main's latest.");
                    }
                }
                else
                {
                    if (isErrorBool)
                    {
                        MessageBox.Show($"isTherePendingChanges: False", "Pending Changes Avail", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    return;
                }
            }
            ///Unity 06
            else if (unityVersion == Versions[1])
            {
                Debug.WriteLine("Unity version: " + unityVersion);
                ///undo all changes
                if (isTherePendingChanges)
                {
                    if (isErrorBool)
                    {
                        MessageBox.Show($"isTherePendingChanges: {isTherePendingChanges}", "Pending Changes Avail", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    var result = MessageBox.Show($"The current changeset of {templateProjectName} will undo all the work.", "Undo changeset", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    isUndoChangeset = (result == MessageBoxResult.Yes);
                    if (isUndoChangeset)
                    {
                        RunCmdWithOutput(plasticCmdQuery.UndoChanges, templateProjectPath);
                        RunCmdWithOutput(plasticCmdQuery.RefreshStatus, templateProjectPath);
                        Debug.WriteLine($"Undo all changes: ");

                        // Find and delete all files in "Added" state
                        var statusOutput = RunCmdWithOutput(plasticCmdQuery.NotDeductedAddedFiles, templateProjectPath);
                        bool inAddedSection = false;
                        foreach (var line in statusOutput.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            var trimmed = line.Trim();
                            if (trimmed.Equals("Added", StringComparison.OrdinalIgnoreCase))
                            {
                                inAddedSection = true;
                                continue;
                            }

                            //Debug.WriteLine("----------> Processing line: " + trimmed);
                            if (inAddedSection && trimmed.StartsWith("Private"))
                            {
                                var parts = trimmed.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length > 0)
                                {
                                    var assetPath = trimmed.Split("Assets").LastOrDefault();
                                    var fullPath = Path.Combine(templateProjectPath, "Assets" + assetPath);
                                    try
                                    {
                                        //Debug.WriteLine("Full path to delete: " + fullPath);
                                        if (File.Exists(fullPath))
                                        {
                                            File.Delete(fullPath);
                                            Debug.WriteLine($"Deleted added file: {fullPath}");
                                        }
                                        if (Directory.Exists(fullPath))
                                        {
                                            Directory.Delete(fullPath, true);
                                            Debug.WriteLine($"Deleted added directory: {fullPath}");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"Failed to delete {fullPath}: {ex.Message}");
                                        if (isErrorBool)
                                        {
                                            MessageBox.Show("Failed to delete " + fullPath + ": " + ex.Message, "Deletion Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"Main latest: {templateProjectChangeset} => Already in main's latest.");
                        if (isErrorBool)
                        {
                            MessageBox.Show("You select 'No' from undo popup", "Do not Undo", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                else
                {
                    if (isErrorBool)
                    {
                        MessageBox.Show($"isTherePendingChanges: False", "Pending Changes Avail", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    return;
                }
            }
        }

        private void SwitchToMainChangeset()
        {
            if (unityVersion == Versions[0])
            {
                bool checkPending = (isTherePendingChanges == false && isUndoChangeset == false) || 
                                    (isTherePendingChanges == true && isUndoChangeset == true);
                if(checkPending)
                {
                    if (isErrorBool)
                    {
                        MessageBox.Show($"checkPending bool => {checkPending}", "Switch Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    var switchOutput = RunCmdWithOutput(plasticCmdQuery.SwitchToBranch(plasticCmdQuery.MainBranch), templateProjectPath);
                    var statusOutput = RunCmdWithOutput(plasticCmdQuery.RefreshStatus, templateProjectPath);

                    //Debug.WriteLine($"Switch output: {switchOutput}");
                    //Debug.WriteLine($"Status output: {statusOutput}");
                    Debug.WriteLine("Switch successfully");
                }
                else if(isTherePendingChanges == true && isUndoChangeset == false)
                {
                    if (isErrorBool)
                    {
                        MessageBox.Show("checkPending false: ", "Switch Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    return;
                }
            }
            else if (unityVersion == Versions[1])
            {
                bool checkPending = (isTherePendingChanges == false && isUndoChangeset == false) ||
                                    (isTherePendingChanges == true && isUndoChangeset == true);
                if (checkPending)
                {
                    if (isErrorBool)
                    {
                        MessageBox.Show($"checkPending bool => {checkPending}", "Switch Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    var switchOutput = RunCmdWithOutput(plasticCmdQuery.SwitchToBranch(plasticCmdQuery.UnityUpgradeBranch), templateProjectPath);
                    var statusOutput = RunCmdWithOutput("cm status --refresh", templateProjectPath);
                    //Debug.WriteLine($"Switch output: {switchOutput}");
                    //Debug.WriteLine($"Status output: {statusOutput}");
                    Debug.WriteLine("Switch to main latest successfully");
                }
                else if (isTherePendingChanges == true && isUndoChangeset == false)
                {      
                    if (isErrorBool)
                    {
                        MessageBox.Show("checkPending false: ", "Switch Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    return;
                }
            }
        }

        private void CheckContentWorkflowChangeset()
        {
            RunCmd($"cd \"{templateProjectPath}\"");
            var output = RunCmdWithOutput(plasticCmdQuery.Status, templateProjectPath);
            var outputSplited = output.Split("@");
            if (isErrorBool)
            {
                MessageBox.Show("Check Content_Workflow changeset", "Checking Changeset", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            ///unity 2022
            if (unityVersion == Versions[0])
            {
                //Debug.WriteLine($"Unity version: {unityVersion}, and ChangesetNo. {outputSplited.FirstOrDefault()}");
                ///when you stand main latest changeset 
                int templateProjectMainLatest = 0;
                if (outputSplited.FirstOrDefault() == plasticCmdQuery.MainBranch)
                {
                    Debug.WriteLine("Already in main branch's latest changeset : " + output);
                    templateProjectMainLatest = mainChangesets.LastOrDefault();
                    templateProjectChangeset = templateProjectMainLatest.ToString();
                    if (isErrorBool)
                    {
                        MessageBox.Show("Already main latest changeset", "Stand In Main Branch", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    return;
                }
                ///when you stand main's previous changeset or another changeset
                else
                {
                    ///when you stand 6's latest changeset
                    int templateProjectCurrentChangeset = 0;
                    if (outputSplited.FirstOrDefault() == plasticCmdQuery.UnityUpgradeBranch)
                    {
                        templateProjectCurrentChangeset = sixChangesets.LastOrDefault();
                        templateProjectChangeset = templateProjectCurrentChangeset.ToString();
                        if (isErrorBool)
                        {
                            MessageBox.Show($"Already 6 latest changeset: {templateProjectCurrentChangeset}", "Stand In UnityUpgrade Branch", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        Debug.WriteLine("Get the number: " + templateProjectCurrentChangeset);
                    }
                    ///when you stand another changeset
                    else
                    {
                        var lastOutput = outputSplited.FirstOrDefault().Split(':');
                        templateProjectCurrentChangeset = int.Parse(lastOutput[1]);
                        templateProjectChangeset = templateProjectCurrentChangeset.ToString();
                        if (isErrorBool)
                        {
                            MessageBox.Show($"Current changeset: {templateProjectCurrentChangeset}", "Stand Another Branch", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        Debug.WriteLine("Get the number: " + templateProjectCurrentChangeset);
                    }
                }
            }
            ///unity 06
            else if (unityVersion == Versions[1])
            {
                //Debug.WriteLine($"Unity version: {unityVersion}, and ChangesetNo. {outputSplited.FirstOrDefault()}");
                ///when you stand 6 latest changeset
                int templateProjectSixLatest = 0;
                if (outputSplited.FirstOrDefault() == plasticCmdQuery.UnityUpgradeBranch)
                {
                    Debug.WriteLine("Already in 6 latest branch: " + output);
                    templateProjectSixLatest = sixChangesets.LastOrDefault();
                    templateProjectChangeset = templateProjectSixLatest.ToString();
                    if (isErrorBool)
                    {
                        MessageBox.Show("Already 6 latest changeset", "Stand In UnityUpgrade Branch", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    return;
                }
                ///when you stand 6's previous changeset or another changeset
                else
                {
                    ///when you stand main's latest changeset
                    int templateProjectCurrentChangeset = 0;
                    if (outputSplited.FirstOrDefault() == plasticCmdQuery.MainBranch)
                    {
                        templateProjectCurrentChangeset = mainChangesets.LastOrDefault();
                        templateProjectChangeset = templateProjectCurrentChangeset.ToString();
                        if (isErrorBool)
                        {
                            MessageBox.Show($"Already Main latest changeset: {templateProjectCurrentChangeset}", "Stand In Main Branch", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        Debug.WriteLine("Get the number: " + templateProjectCurrentChangeset);
                    }
                    ///when you stand another changeset
                    else
                    {
                        var lastOutput = outputSplited.FirstOrDefault().Split(':');
                        templateProjectCurrentChangeset = int.Parse(lastOutput[1]);
                        templateProjectChangeset = templateProjectCurrentChangeset.ToString();
                        if (isErrorBool)
                        {
                            MessageBox.Show($"Current changeset: {templateProjectCurrentChangeset}", "Stand Another Branch", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        Debug.WriteLine("Get the number: " + templateProjectCurrentChangeset);
                    }
                }
            }
        }


        private void CreateNewRepository()
        {
            var removeSpace = selectOrganization.Replace(" ", "");
            fullServerName = removeSpace + this.server;

            ///Create new repository
            RunCmd($"{plasticCmdQuery.CreateRepository} {projectName}@{fullServerName}");
            if (isErrorBool)
            {
                MessageBox.Show("Create Repository successfully", "Repository Create", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            Debug.WriteLine("Create Repository successfully");
        }

        private async Task CopyingAllFilesInNewRepository(string sourceDir, string targetDir)
        {
            if (Path.GetFileName(sourceDir).Equals(".plastic", StringComparison.OrdinalIgnoreCase))
                return;
            if (Path.GetFileName(sourceDir).Equals("Library", StringComparison.OrdinalIgnoreCase))
                return;

            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            var files = Directory.GetFiles(sourceDir);
            Parallel.ForEach(files, file =>
            {
                var fileName = Path.GetFileName(file);
                if (fileName.StartsWith(".plastic", StringComparison.OrdinalIgnoreCase) ||
                    fileName.StartsWith("plastic.selector", StringComparison.OrdinalIgnoreCase) ||
                    fileName.StartsWith("plastic.wktree", StringComparison.OrdinalIgnoreCase) ||
                    fileName.StartsWith("plastic.workspace", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                var destFile = Path.Combine(targetDir, fileName);
                File.Copy(file, destFile, false);
            });

            var directories = Directory.GetDirectories(sourceDir);
            Parallel.ForEach(directories, async directory =>
            {
                if (Path.GetFileName(directory).Equals(".plastic", StringComparison.OrdinalIgnoreCase))
                    return;

                var dirName = Path.GetFileName(directory);
                var destSubDir = Path.Combine(targetDir, dirName);
                await CopyingAllFilesInNewRepository(directory, destSubDir);
            });
        }

        private async Task AddAndCheckinFilesInNewRepository()
        {
            if (isErrorBool)
            {
                MessageBox.Show($"AddAndCheckinFilesInNewRepository() Project location {projectLocation} Method", "Checkin", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            RunCmdWithOutput($"{plasticCmdQuery.CreateWorkspace} {projectName} \"{projectLocation}\" {projectName}@{fullServerName}", projectLocation);
            if (isErrorBool)
            {
                MessageBox.Show($"Project location {projectLocation} where checkin", "Checkin Folder Check", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            RunCmdWithOutput(plasticCmdQuery.AddFiles, projectLocation);


            if (unityVersion == Versions[0])
            {                
                if(isErrorBool)
                {
                    MessageBox.Show("Checkin from main latest changeset", "Checkin", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                RunCmdWithOutput($"{plasticCmdQuery.PushChanges}{templateProjectChangeset}", projectLocation);
                Debug.WriteLine("Checkin from main latest changeset.");
            }
            else if (unityVersion == Versions[1])
            {
                if (isErrorBool)
                {
                    MessageBox.Show("Checkin from 6 changeset", "Checkin", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                RunCmdWithOutput($"{plasticCmdQuery.PushChanges}{templateProjectChangeset}", projectLocation);
                Debug.WriteLine("Checkin from 6 latest changeset.");
            }
        }

        public string PlasticVersion()
        {
            var output = RunCmdOut(plasticCmdQuery.PlasticVersion);
            Debug.WriteLine(output);
            return output;
        }

        public string GetOrganization()
        {
            string cloudRegionsSrcPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "plastic4", "cloudregions.conf");
            string cloudRegionsDesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "plastic4", "cloudregions.conf.txt");
            string lastValidLine = null;
            Debug.WriteLine(cloudRegionsSrcPath);
            File.Copy(cloudRegionsSrcPath, cloudRegionsDesPath, true);
            foreach (var line in File.ReadLines(cloudRegionsDesPath))
            {
                var trimmedLine = line.Trim();
                if (!trimmedLine.StartsWith("//") && !string.IsNullOrWhiteSpace(trimmedLine))
                {
                    lastValidLine = trimmedLine;
                }
            }
            string[] lastValidLineSplit = lastValidLine.Split('=');
            string[] lastValidLineSplited = lastValidLineSplit[1].Split('@');
            Debug.WriteLine(lastValidLineSplited[0]);
            return lastValidLineSplited[0];
        }

        public bool IsPlasticLogedIn()
        {
            var output = RunCmdOut(plasticCmdQuery.PlasticLogin);
            Debug.WriteLine(output);
            if (!string.IsNullOrEmpty(output))
            {
                Debug.WriteLine("Already loged in plasticscm: " + output);
                return true;
            }
            MessageBox.Show("You are not logged in to Plastic SCM. Please log in and try again.", "Not Logged In", MessageBoxButton.OK, MessageBoxImage.Warning);
            MainWindow.GetWindow(Application.Current.MainWindow).Close();
            return false;
        }

        public void IsContentWorkflowEditorOpen()
        {
            if(isErrorBool)
            {
                MessageBox.Show($"IsContentWorkflowEditorOpen() method.", "Checking Editor", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            if (!string.IsNullOrEmpty(templateProjectPath))
            {
                // Check both possible lockfile locations
                string tempLockFile = Path.Combine(templateProjectPath, "Temp", "UnityLockfile");
                string libraryLockFile = Path.Combine(templateProjectPath, "Library", "UnityLockfile");

                bool lockFileExists = File.Exists(tempLockFile) || File.Exists(libraryLockFile);

                // Optionally, check for Unity process with this project open
                bool unityProcessOpen = false;
                foreach (var process in Process.GetProcessesByName("Unity"))
                {
                    try
                    {
                        string cmdLine = GetCommandLine(process);
                        if (!string.IsNullOrEmpty(cmdLine) && cmdLine.Contains(templateProjectPath, StringComparison.OrdinalIgnoreCase))
                        {
                            unityProcessOpen = true;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        /* Ignore access denied */
                        MessageBox.Show("" + ex.Message, "Unknown Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                if (lockFileExists || unityProcessOpen)
                {
                    isEditorOpen = true;
                    MessageBox.Show($"Please close {templateProjectName} Unity editor before creating the project.","Unity Editor Open",MessageBoxButton.OK,MessageBoxImage.Warning);
                    return;
                }
                else
                {
                    isEditorOpen = false;
                }
            }
        } 

        #endregion

        #region CommonMethod

        public void LoadDownloadedWorkspace()
        {
            downloadedWorkspaces.Clear();
            var output = RunCmdWithOutput(plasticCmdQuery.DownloadWorkSpace);
            foreach (var line in output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            {
                //Debug.WriteLine("Workspace: " + line);
                downloadedWorkspaces.Add(line);
            }
            if (isErrorBool)
            {
                MessageBox.Show("You are in LoadDownloadedWorkspace method", "Getting Downloaded Workspace", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public void GetMainChangeset()
        {
            mainChangesets.Clear();
            var output = RunCmdWithOutput(plasticCmdQuery.FindChangesetsOfBranch(plasticCmdQuery.MainBranch), templateProjectPath);
            Debug.WriteLine("GetMainChangeset()" + output);
            foreach (var line in output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(line.Trim(), out int id))
                {
                    mainChangesets.Add(id);
                }
            }
            if (isErrorBool)
            {
                MessageBox.Show("You are in GetMainChangeset method", "Getting Changeset", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public void GetSixChangeset()
        {
            sixChangesets.Clear();
            var output = RunCmdWithOutput(plasticCmdQuery.FindChangesetsOfBranch(plasticCmdQuery.UnityUpgradeBranch), templateProjectPath);
            Debug.WriteLine("GetSixChangeset()" + output);

            foreach (var line in output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(line.Trim(), out int id))
                {
                    sixChangesets.Add(id);
                }
            }
            if (isErrorBool)
            {
                MessageBox.Show("You are in GetSixChangeset method", "Getting Changeset", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public void GetAllRepo()
        {
            if (isErrorBool)
            {
                MessageBox.Show("You are in getting repository method", "Getting Repository", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            repositoryNames.Clear();
            var removeSpace = selectOrganization.Replace(" ", "");
            var output = RunCmdOut($"{plasticCmdQuery.Repository}{removeSpace}{server}");
            //var output = RunCmdOut($"cm repo list --server=LocLab_Consulting_GmbH@Cloud");
            Debug.WriteLine(output);

            foreach (var line in output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            {
                var repoName = line.Split('@').FirstOrDefault();
                repositoryNames.Add(repoName);
                //Debug.WriteLine(repoName);
            }
        }

        public string CommentProgress()
        {
            //Debug.WriteLine(comment);
            return comment;
        }

        public string CommentComplete()
        {
            string complete = "";

            if (isEditorOpen)
            {
                complete = "Operation Failed!";
            }
            else
            {
                if (istemplateProjectDownloaded)
                {
                    complete = "Completed!";
                }
                else
                {
                    complete = "Operation Failed!";
                }
            }
            return complete;
        }

        public async Task DelayShutdown(int milliseconds)
        {
            await Task.Delay(milliseconds);
            MainWindow.GetWindow(Application.Current.MainWindow).Close();
        }

        private void RunCmd(string command)
        {
            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/c " + command;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.Start();
            Console.WriteLine(process.StandardOutput.ReadToEnd());
            process.WaitForExit();
        }

        private string RunCmdWithOutput(string command)
        {
            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/c " + command;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output;
        }

        public static string RunCmdOut(string command)
        {
            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/c " + command;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output;
        }

        private string RunCmdWithOutput(string command, string workingDirectory = null)
        {
            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/c " + command;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            if (!string.IsNullOrEmpty(workingDirectory))
            {
                process.StartInfo.WorkingDirectory = workingDirectory;
            }
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output;
        }

        private string GetCommandLine(Process process)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher($"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}"))
                {
                    foreach (var @object in searcher.Get())
                    {
                        return @object["CommandLine"]?.ToString();
                    }
                }
            }
            catch (Exception ex) 
            {
                MessageBox.Show("" + ex.Message, "Unknown Error!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return null;
        }

        #endregion
    }
}
