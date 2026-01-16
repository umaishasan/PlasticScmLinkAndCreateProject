using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CreateProjectOnline
{
    public class Controller
    {
        public string server = "@cloud";
        public string fullServerName;
        public string selectOrganization;
        public string projectName;
        public string projectLocation;
        public List<string> workspaceName = new();

        ///All variables from here related to plasticscm 
        private string contentWorkflowProject;
        private string contentWorkflowProjectPath;
        private int contentWorkflowCurrentChangeset;
        private int contentWorkflowMainLatest;

        public Controller(string selectOrganization, string projectName,string projectLocation)
        {
            this.selectOrganization = selectOrganization;
            this.projectName = projectName;
            this.projectLocation = projectLocation;
        }

        public async Task CreateProjectOnline(IProgress<int> progress)
        {
            progress.Report(10);
            CheckContentWorkflowDownloaded();
            progress.Report(20);
            CheckContentWorkflowChangeset();
            progress.Report(20);
            ContentWorkflowChangesetMatchToMainChangeset();
            progress.Report(10);
            CreateNewRepository();
            progress.Report(20);
            await CopyingAllFilesInNewRepository(contentWorkflowProjectPath, projectLocation);
            Debug.WriteLine("Copying files and folder successfully");
            progress.Report(15);
            await AddAndCheckinFilesInNewRepository();
            progress.Report(5);
            Debug.WriteLine("Add and Checkin files successfully");
        }

        private void CheckContentWorkflowDownloaded()
        {
            var output = RunCmdWithOutput("cm workspace list");
            bool found = false;

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
                    found = true;
                    break; // Stop searching after finding the desired workspace
                }
            }

            if (!found)
            {
                MessageBox.Show(
                    "DTH_Content_Workflow project is not downloaded. Download from main's latest.",
                    "Content Workflow Not Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }
        }

        private void CheckContentWorkflowChangeset()
        {
            var directoryFound = contentWorkflowProjectPath.Split(':');
            RunCmd($"{directoryFound}:");
            RunCmd($"cd \"{contentWorkflowProjectPath}\"");
            var output = RunCmdWithOutput($"cm status --header", contentWorkflowProjectPath);
            var outputSplited = output.Split("@");
            if (outputSplited.FirstOrDefault() == "/main")
            {
                Debug.WriteLine("Already in main branch: "+ output);
                return;
            }
            else
            {
                var lastOutput = outputSplited.FirstOrDefault().Split(':');
                contentWorkflowCurrentChangeset = int.Parse(lastOutput[1]);
                Debug.WriteLine("Get the number: "+ contentWorkflowCurrentChangeset);
            }
        }

        private void ContentWorkflowChangesetMatchToMainChangeset()
        {
            var changesetNo = GetMainBranchChangeset();
            var lastChangesetNo = changesetNo.LastOrDefault();
            contentWorkflowMainLatest = int.Parse(lastChangesetNo.ToString());
            if(contentWorkflowCurrentChangeset != contentWorkflowMainLatest)
            {
                Debug.WriteLine($"Main latest: {contentWorkflowMainLatest}, current: {contentWorkflowCurrentChangeset} => Not Match.");
                
                ///undo all changes
                RunCmdWithOutput("cm undo --all && cm clean", contentWorkflowProjectPath);
                RunCmdWithOutput("cm status --refresh", contentWorkflowProjectPath);
                Debug.WriteLine($"Undo all changes: ");

                ///Try to switch in main latest
                var switchOutput = RunCmdWithOutput("cm switch main", contentWorkflowProjectPath);
                var statusOutput = RunCmdWithOutput("cm status --refresh", contentWorkflowProjectPath);
                
                Debug.WriteLine($"Switch output: {switchOutput}");
                //Debug.WriteLine($"Status output: {statusOutput}");
                Debug.WriteLine("Switch to main latest successfully");
            }
            else
            {
                Debug.WriteLine($"Main latest: {contentWorkflowMainLatest}, current: {contentWorkflowCurrentChangeset} => Already in main's latest.");
                return;
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
                File.Copy(file, destFile, true);
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
            RunCmdWithOutput($"cm checkin -m \"Get work from this {contentWorkflowMainLatest} changeset.\"", projectLocation);
        }

        #region CommonMethod

        private List<int> GetMainBranchChangeset()
        {
            var changesetIds = new List<int>();
            var output = RunCmdWithOutput("cm find changeset \"where branch='main'\" --format=\"{changesetid}\"", contentWorkflowProjectPath);
            //Debug.WriteLine(output);

            foreach (var line in output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(line.Trim(), out int id))
                {
                    changesetIds.Add(id);
                }
            }
            return changesetIds;
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
