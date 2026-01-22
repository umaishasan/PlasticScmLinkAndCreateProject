using System.Diagnostics;
using System.IO;
using System.Windows;

namespace CreateProjectOnline
{
    public class Controller
    {
        #region Variables

        public string server = "@cloud";
        public string fullServerName;
        public string comment = "";
        private string contentWorkflowProject;
        private string contentWorkflowProjectPath;
        private string contentWorkflowProjPathDirective;
        private int contentWorkflowCurrentChangeset;
        private int contentWorkflowMainLatest;
        private int contentWorkflowSixLatest;

        private bool isUndoChangeset = false;
        public bool isContentWorkflowDownloaded;
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

        #endregion

        public async Task CreateProjectOnline(IProgress<int> progress)
        {
            progress.Report(5);
            comment = "Check DTH_Content_Workflow project downloaded or not.";
            CheckContentWorkflowDownloaded();
            if (isContentWorkflowDownloaded)
            {
                progress.Report(5);
                comment = "Createing folder for new project.";
                CreateFolderForNewProject();
                progress.Report(20);
                comment = "Check DTH_Content_Workflow current changeset number.";
                CheckContentWorkflowChangeset();
                progress.Report(10);
                comment = "Undo changeset.";
                UndoChangeset();
                progress.Report(10); //50
                if (isUndoChangeset)
                {
                    comment = unityVersion == Versions[0] ? "Switch to main's latest changeset." : "Switch to Unity6's latest changeset.";
                    SwitchToMainChangeset();
                }
                progress.Report(10);
                comment = "Create new repository for the new project.";
                CreateNewRepository();
                progress.Report(15);
                comment = "Copying all files from DTH_Content_Workflow to new repository.";
                await CopyingAllFilesInNewRepository(contentWorkflowProjectPath, projectLocation);
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
                MessageBox.Show("Checking DTH_Content_Workflow project downloaded or not.", "Checking", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            bool found = isContentWorkflowDownloaded = false;
            foreach (var line in downloadedWorkspaces)
            {
                //Debug.WriteLine("Workspace: " + line);
                var nameSplited = line.Split('@');
                var pathSplited = line.Split(':').LastOrDefault();
                var directiveSplited = line.Split(':').FirstOrDefault().ToString().Split(' ').LastOrDefault();
                //Debug.WriteLine("Workspace path: " + pathSplited);
                if (nameSplited[0] == "DTH_Content_Workflow")
                {
                    contentWorkflowProject = nameSplited[0];
                    string pathContentWorkflow = pathSplited;
                    contentWorkflowProjPathDirective = directiveSplited;
                    contentWorkflowProjectPath = contentWorkflowProjPathDirective + ":" + pathContentWorkflow;
                    if (isErrorBool)
                    {
                        MessageBox.Show($"ProjectName: {contentWorkflowProject}, ProjectPath: {contentWorkflowProjectPath}", "Project Found!", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    found = isContentWorkflowDownloaded = true;
                    Debug.WriteLine($"Is this same: {nameSplited[0]} or {contentWorkflowProject}");
                    Debug.WriteLine($"Project path: {pathSplited} or {contentWorkflowProjectPath}");
                    break; // Stop searching after finding the desired workspace
                }
            }
            if (!found)
            {
                isContentWorkflowDownloaded = false;
                var result = MessageBox.Show("The DTH_Content_Workflow project is not downloaded. Please download the latest version from main.", "DTH_Content_Workflow Not Found",MessageBoxButton.OK,MessageBoxImage.Warning);                if (result == MessageBoxResult.OK)
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
            if (isContentWorkflowDownloaded)
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

        private void CheckContentWorkflowChangeset()
        {
            RunCmd($"cd \"{contentWorkflowProjectPath}\"");
            var output = RunCmdWithOutput(plasticCmdQuery.Status, contentWorkflowProjectPath);
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
                if (outputSplited.FirstOrDefault() == "/main")
                {
                    Debug.WriteLine("Already in main branch: " + output);
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
                    if (outputSplited.FirstOrDefault() == "/main/UH-UnityUpgrade")
                    {
                        contentWorkflowCurrentChangeset = sixChangesets.LastOrDefault();
                        if (isErrorBool)
                        {
                            MessageBox.Show($"Already 6 latest changeset: {contentWorkflowCurrentChangeset}", "Stand In UnityUpgrade Branch", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        Debug.WriteLine("Get the number: " + contentWorkflowCurrentChangeset);
                    }
                    ///when you stand another changeset
                    else
                    {
                        var lastOutput = outputSplited.FirstOrDefault().Split(':');
                        contentWorkflowCurrentChangeset = int.Parse(lastOutput[1]);
                        if (isErrorBool)
                        {
                            MessageBox.Show($"Current changeset: {contentWorkflowCurrentChangeset}", "Stand Another Branch", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        Debug.WriteLine("Get the number: " + contentWorkflowCurrentChangeset);
                    }
                }
            } 
            ///unity 06
            else if (unityVersion == Versions[1])
            {
                //Debug.WriteLine($"Unity version: {unityVersion}, and ChangesetNo. {outputSplited.FirstOrDefault()}");
                ///when you stand 6 latest changeset
                if (outputSplited.FirstOrDefault() == "/main/UH-UnityUpgrade")
                {                                  
                    Debug.WriteLine("Already in 6 latest branch: " + output);
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
                    if (outputSplited.FirstOrDefault() == "/main")
                    {
                        contentWorkflowCurrentChangeset = mainChangesets.LastOrDefault();
                        if (isErrorBool)
                        {
                            MessageBox.Show($"Already Main latest changeset: {contentWorkflowCurrentChangeset}", "Stand In Main Branch", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        Debug.WriteLine("Get the number: " + contentWorkflowCurrentChangeset);
                    }
                    ///when you stand another changeset
                    else
                    {
                        var lastOutput = outputSplited.FirstOrDefault().Split(':');
                        contentWorkflowCurrentChangeset = int.Parse(lastOutput[1]);
                        if (isErrorBool)
                        {
                            MessageBox.Show($"Current changeset: {contentWorkflowCurrentChangeset}", "Stand Another Branch", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        Debug.WriteLine("Get the number: " + contentWorkflowCurrentChangeset);
                    }
                }
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
                contentWorkflowMainLatest = mainChangesets.LastOrDefault();
                ///check current changeset and main latest changeset not equal then undo
                if (contentWorkflowCurrentChangeset != contentWorkflowMainLatest)
                {
                    if (isErrorBool)
                    {
                        MessageBox.Show("Current changeset & Main changeset not match", "Not Match", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    Debug.WriteLine($"Main latest: {contentWorkflowMainLatest}, current: {contentWorkflowCurrentChangeset} => Not Match.");

                    ///undo all changes
                    var result = MessageBox.Show("The current changeset of DTH_Content_Workflow will undo all the work.", "Undo changeset", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    isUndoChangeset = (result == MessageBoxResult.Yes);
                    if (isUndoChangeset)
                    {
                        RunCmdWithOutput(plasticCmdQuery.UndoChanges, contentWorkflowProjectPath);
                        RunCmdWithOutput(plasticCmdQuery.RefreshStatus, contentWorkflowProjectPath);
                        Debug.WriteLine($"Undo all changes: ");

                        // Find and delete all files in "Added" state
                        var statusOutput = RunCmdWithOutput(plasticCmdQuery.NotDeductedAddedFiles, contentWorkflowProjectPath);
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
                                    var fullPath = Path.Combine(contentWorkflowProjectPath, "Assets" + assetPath);
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
                        Debug.WriteLine($"Main latest: {contentWorkflowMainLatest}, current: {contentWorkflowCurrentChangeset} => Already in main's latest.");
                        return;
                    }
                }
                else
                {
                    if (isErrorBool)
                    {
                        MessageBox.Show("Current changeset & Main changeset Matched", "Matched", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    Debug.WriteLine($"Main latest: {contentWorkflowMainLatest}, current: {contentWorkflowCurrentChangeset} => Already in main's latest.");
                    return;
                }
            }
            ///Unity 06
            else if(unityVersion == Versions[1])
            {
                Debug.WriteLine("Unity version: " + unityVersion);
                contentWorkflowSixLatest = sixChangesets.LastOrDefault();
                if (contentWorkflowCurrentChangeset != contentWorkflowSixLatest)
                {
                    Debug.WriteLine($"6 latest: {contentWorkflowSixLatest}, current: {contentWorkflowCurrentChangeset} => Not Match.");
                    if (isErrorBool)
                    {
                        MessageBox.Show("Current changeset & 6 changeset not match", "Not Match", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    ///undo all changes
                    var result = MessageBox.Show("The current changeset of DTH_Content_Workflow will undo all the work.", "Undo changeset", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    isUndoChangeset = (result == MessageBoxResult.Yes);
                    if (isUndoChangeset)
                    {
                        RunCmdWithOutput(plasticCmdQuery.UndoChanges, contentWorkflowProjectPath);
                        RunCmdWithOutput(plasticCmdQuery.RefreshStatus, contentWorkflowProjectPath);
                        Debug.WriteLine($"Undo all changes: ");

                        // Find and delete all files in "Added" state
                        var statusOutput = RunCmdWithOutput(plasticCmdQuery.NotDeductedAddedFiles, contentWorkflowProjectPath);
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
                                    var fullPath = Path.Combine(contentWorkflowProjectPath, "Assets" + assetPath);
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
                        Debug.WriteLine($"Main latest: {contentWorkflowSixLatest}, current: {contentWorkflowCurrentChangeset} => Already in main's latest.");
                        if (isErrorBool)
                        {
                            MessageBox.Show("You select 'No' from undo popup", "Do not Undo", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        return;
                    }
                }
                else
                {
                    Debug.WriteLine($"Main latest: {contentWorkflowSixLatest}, current: {contentWorkflowCurrentChangeset} => Already in main's latest.");
                    if (isErrorBool)
                    {
                        MessageBox.Show("Current changeset & 6 changeset Matched", "Matched", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    return;
                }
            }
        }

        private void SwitchToMainChangeset()
        {
            if (unityVersion == Versions[0])
            {
                var switchOutput = RunCmdWithOutput(plasticCmdQuery.SwitchUnity2022, contentWorkflowProjectPath);
                var statusOutput = RunCmdWithOutput(plasticCmdQuery.RefreshStatus, contentWorkflowProjectPath);

                Debug.WriteLine($"Switch output: {switchOutput}");
                //Debug.WriteLine($"Status output: {statusOutput}");
                Debug.WriteLine("Switch to main latest successfully");
            }
            else if (unityVersion == Versions[1])
            {
                var switchOutput = RunCmdWithOutput(plasticCmdQuery.SwitchUnity06, contentWorkflowProjectPath);
                var statusOutput = RunCmdWithOutput("cm status --refresh", contentWorkflowProjectPath);
                Debug.WriteLine($"Switch output: {switchOutput}");
                //Debug.WriteLine($"Status output: {statusOutput}");
                Debug.WriteLine("Switch to main latest successfully");
            }
        }

        private void CreateNewRepository()
        {
            var removeSpace = selectOrganization.Replace(" ", "");
            fullServerName = removeSpace + this.server;

            ///Create new repository
            RunCmd($"{plasticCmdQuery.CreateRepository} {projectName}@{fullServerName}");
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
            RunCmd($"{plasticCmdQuery.CreateWorkspace} {projectName} {projectLocation} {projectName}@{fullServerName}");
            RunCmdWithOutput(plasticCmdQuery.AddFiles, projectLocation);

            if (unityVersion == Versions[0])
            {
                if (isUndoChangeset)
                {
                    Debug.WriteLine("Checkin from main latest changeset.");
                    RunCmdWithOutput($"{plasticCmdQuery.PushChanges}{contentWorkflowMainLatest}", projectLocation);
                }
                else
                {
                    Debug.WriteLine("Checkin from current latest changeset.");
                    RunCmdWithOutput($"{plasticCmdQuery.PushChanges}{contentWorkflowCurrentChangeset}", projectLocation);
                }
            }
            else if (unityVersion == Versions[1])
            {
                if (isUndoChangeset)
                {
                    Debug.WriteLine("Checkin from Unity6 latest changeset.");
                    RunCmdWithOutput($"{plasticCmdQuery.PushChanges}{contentWorkflowSixLatest}", projectLocation);
                }
                else
                {
                    Debug.WriteLine("Checkin from current latest changeset.");
                    RunCmdWithOutput($"{plasticCmdQuery.PushChanges}{contentWorkflowCurrentChangeset}", projectLocation);
                }
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
            var output = RunCmdWithOutput(plasticCmdQuery.MainChangeset, contentWorkflowProjectPath);
            //Debug.WriteLine(output);
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
            var output = RunCmdWithOutput(plasticCmdQuery.UnityUpgradeChangeset, contentWorkflowProjectPath);
            //Debug.WriteLine(output);

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
            if (isContentWorkflowDownloaded)
            {
                complete = "Completed!";
            }
            else
            {
                complete = "Operation Failed!";
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

        #endregion
    }
}
