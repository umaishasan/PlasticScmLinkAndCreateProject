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
        public string[] Versions = { " Unity 2022", " Unity 06" };
        public List<string> workspaceName = new();
        public static List<string> repositoryNames = new();
        public string comment = "";
        public bool isContentWorkflowDownloaded;

        private string selectOrganization { get; set; }
        private string projectName { get; set; }
        private string projectLocation { get; set; }
        private string unityVersion { get; set; }

        private string contentWorkflowProject;
        private string contentWorkflowProjectPath;
        private int contentWorkflowCurrentChangeset;
        private int contentWorkflowMainLatest;
        private int contentWorkflowSixLatest;
        private bool isUndoChangeset = false;

        #endregion

        public Controller(){}

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
                    comment = "Switch to main's latest changeset.";
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

        private void CheckContentWorkflowDownloaded()
        {
            var output = RunCmdWithOutput("cm workspace list");
            bool found = isContentWorkflowDownloaded = false;

            foreach (var line in output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            {
                //Debug.WriteLine("Workspace: " + line);
                workspaceName.Add(line);

                var nameSplited = line.Split('@');
                var pathSplited = line.Split(' ').LastOrDefault();
                //Debug.WriteLine("Workspace path: " + pathSplited);

                if (nameSplited[0] == "DTH_Content_Workflow")
                {
                    contentWorkflowProject = nameSplited[0];
                    contentWorkflowProjectPath = pathSplited;
                    Debug.WriteLine($"Is this same: {nameSplited[0]} or {contentWorkflowProject}");
                    Debug.WriteLine($"Project path: {pathSplited} or {contentWorkflowProjectPath}");
                    found = isContentWorkflowDownloaded = true;
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
        }

        private void CheckContentWorkflowChangeset()
        {
            var directoryFound = contentWorkflowProjectPath.Split(':');
            RunCmd($"{directoryFound}:");
            RunCmd($"cd \"{contentWorkflowProjectPath}\"");
            var output = RunCmdWithOutput($"cm status --header", contentWorkflowProjectPath);
            var outputSplited = output.Split("@");

            if(unityVersion == Versions[0])
            {
                Debug.WriteLine("Unity version: " + unityVersion);
                if (outputSplited.FirstOrDefault() == "/main")
                {
                    Debug.WriteLine("Already in main branch: " + output);
                    return;
                }
                else
                {
                    var lastOutput = outputSplited.FirstOrDefault().Split(':');
                    contentWorkflowCurrentChangeset = int.Parse(lastOutput[1]);
                    Debug.WriteLine("Get the number: " + contentWorkflowCurrentChangeset);
                }
            } 
            else if (unityVersion == Versions[1])
            {
                Debug.WriteLine("Unity version: " + unityVersion);
                if (outputSplited.FirstOrDefault() == GetBranchChangeset().LastOrDefault().ToString())
                {
                    Debug.WriteLine("Already in 6 latest branch: " + output);
                    return;
                }
                else
                {
                    var lastOutput = outputSplited.FirstOrDefault().Split(':');
                    contentWorkflowCurrentChangeset = int.Parse(lastOutput[1]);
                    Debug.WriteLine("Get the number: " + contentWorkflowCurrentChangeset);
                }
            }
        }

        private void UndoChangeset()
        {
            var changesetNo = GetBranchChangeset();
            var lastChangesetNo = changesetNo.LastOrDefault();

            if (unityVersion == Versions[0])
            {
                Debug.WriteLine("Unity version: " + unityVersion);
                contentWorkflowMainLatest = int.Parse(lastChangesetNo.ToString());
                if (contentWorkflowCurrentChangeset != contentWorkflowMainLatest)
                {
                    Debug.WriteLine($"Main latest: {contentWorkflowMainLatest}, current: {contentWorkflowCurrentChangeset} => Not Match.");

                    ///undo all changes
                    var result = MessageBox.Show("The current changeset of DTH_Content_Workflow will undo all the work.", "Undo changeset", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    isUndoChangeset = (result == MessageBoxResult.Yes);
                    if (isUndoChangeset)
                    {
                        RunCmdWithOutput("cm undo . -r", contentWorkflowProjectPath);
                        RunCmdWithOutput("cm status --refresh", contentWorkflowProjectPath);
                        Debug.WriteLine($"Undo all changes: ");

                        // Find and delete all files in "Added" state
                        var statusOutput = RunCmdWithOutput("cm status --noheader", contentWorkflowProjectPath);
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
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"Main latest: {contentWorkflowMainLatest}, current: {contentWorkflowCurrentChangeset} => Already in main's latest.");
                        return;
                    }
                }
                else
                {
                    Debug.WriteLine($"Main latest: {contentWorkflowMainLatest}, current: {contentWorkflowCurrentChangeset} => Already in main's latest.");
                    return;
                }
            }
            else if(unityVersion == Versions[1])
            {
                Debug.WriteLine("Unity version: " + unityVersion);
                contentWorkflowSixLatest = int.Parse(lastChangesetNo.ToString());
                if (contentWorkflowCurrentChangeset != contentWorkflowSixLatest)
                {
                    Debug.WriteLine($"Main latest: {contentWorkflowSixLatest}, current: {contentWorkflowCurrentChangeset} => Not Match.");

                    ///undo all changes
                    var result = MessageBox.Show("The current changeset of DTH_Content_Workflow will undo all the work.", "Undo changeset", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    isUndoChangeset = (result == MessageBoxResult.Yes);
                    if (isUndoChangeset)
                    {
                        RunCmdWithOutput("cm undo . -r", contentWorkflowProjectPath);
                        RunCmdWithOutput("cm status --refresh", contentWorkflowProjectPath);
                        Debug.WriteLine($"Undo all changes: ");

                        // Find and delete all files in "Added" state
                        var statusOutput = RunCmdWithOutput("cm status --noheader", contentWorkflowProjectPath);
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
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"Main latest: {contentWorkflowSixLatest}, current: {contentWorkflowCurrentChangeset} => Already in main's latest.");
                        return;
                    }
                }
                else
                {
                    Debug.WriteLine($"Main latest: {contentWorkflowSixLatest}, current: {contentWorkflowCurrentChangeset} => Already in main's latest.");
                    return;
                }
            }
        }

        private void SwitchToMainChangeset()
        {
            if (unityVersion == Versions[0])
            {
                var switchOutput = RunCmdWithOutput("cm switch main", contentWorkflowProjectPath);
                var statusOutput = RunCmdWithOutput("cm status --refresh", contentWorkflowProjectPath);

                Debug.WriteLine($"Switch output: {switchOutput}");
                //Debug.WriteLine($"Status output: {statusOutput}");
                Debug.WriteLine("Switch to main latest successfully");
            }
            else if (unityVersion == Versions[1])
            {
                var switchOutput = RunCmdWithOutput("cm switch /main/UH-UnityUpgrade", contentWorkflowProjectPath);
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
            RunCmd($"cm mkrep {projectName}@{fullServerName}");
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
            RunCmd($"cm mkworkspace {projectName} {projectLocation} {projectName}@{fullServerName}");
            RunCmdWithOutput($"cm add . --recursive", projectLocation);

            if (unityVersion == Versions[0])
            {
                if (isUndoChangeset)
                {
                    Debug.WriteLine("Checkin from main latest changeset.");
                    RunCmdWithOutput($"cm checkin -m \"Get work from this {contentWorkflowMainLatest} changeset.\"", projectLocation);
                }
                else
                {
                    Debug.WriteLine("Checkin from current latest changeset.");
                    RunCmdWithOutput($"cm checkin -m \"Get work from this {contentWorkflowCurrentChangeset} changeset.\"", projectLocation);
                }
            }
            else if (unityVersion == Versions[1])
            {
                if (isUndoChangeset)
                {
                    Debug.WriteLine("Checkin from main latest changeset.");
                    RunCmdWithOutput($"cm checkin -m \"Get work from this {contentWorkflowSixLatest} changeset.\"", projectLocation);
                }
                else
                {
                    Debug.WriteLine("Checkin from current latest changeset.");
                    RunCmdWithOutput($"cm checkin -m \"Get work from this {contentWorkflowCurrentChangeset} changeset.\"", projectLocation);
                }
            }
        }

        public string PlasticVersion()
        {
            var output = RunCmdOut("cm version");
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
            var output = RunCmdOut("cm whoami");
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

        #region CommonMethod

        private List<int> GetBranchChangeset()
        {
            var changesetIds = new List<int>();
            if(unityVersion == Versions[0])
            {
                var output = RunCmdWithOutput("cm find changeset \"where branch='main'\" --format=\"{changesetid}\"", contentWorkflowProjectPath);
                //Debug.WriteLine(output);
                foreach (var line in output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (int.TryParse(line.Trim(), out int id))
                    {
                        changesetIds.Add(id);
                    }
                }
            }
            else if(unityVersion == Versions[1])
            {
                var output = RunCmdWithOutput("cm find changeset \"where branch='/main/UH-UnityUpgrade'\" --format=\"{changesetid}\"", contentWorkflowProjectPath);
                //Debug.WriteLine(output);

                foreach (var line in output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (int.TryParse(line.Trim(), out int id))
                    {
                        changesetIds.Add(id);
                    }
                }
            }
            
            return changesetIds;
        }

        public void GetAllRepo()
        {
            repositoryNames.Clear();
            var removeSpace = selectOrganization.Replace(" ", "");
            var output = RunCmdOut($"cm repo list --server={removeSpace}{server}");
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
