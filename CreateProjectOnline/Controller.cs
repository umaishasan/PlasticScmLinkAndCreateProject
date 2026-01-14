using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreateProjectOnline
{
    public class Controller
    {
        public string server = "@cloud";
        public string selectOrganization;
        public string projectName;
        public string projectLocation;

        ///All variables from here related to plasticscm 
        private string contentWorkflowProject;
        private string contentWorkflowProjectPath;
        private int contentWorkflowCurrentChangeset;

        public Controller(string selectOrganization, string projectName,string projectLocation)
        {
            this.selectOrganization = selectOrganization;
            this.projectName = projectName;
            this.projectLocation = projectLocation;
        }

        public void CreateProjectOnline()
        {


            // string repoName = "MyAutoRepo";
            /*string serverName = selectOrganization + this.server; // or your server name
            Debug.WriteLine($"Starting Plastic SCM Automation...{serverName}");

            RunCmd($"cm mkrep {projectName}@{serverName}");
            RunCmd($"cm mkws {projectName}_ws {projectLocation} --repository={projectName}@{serverName}");
            // Create default project files
            File.WriteAllText(Path.Combine(projectLocation, "README.md"), "# Default Project");
            RunCmd($"cm add \"{projectLocation}\"");
            RunCmd($"cm checkin -m \"Initial commit\" \"{projectLocation}\"");

            Debug.WriteLine("Automation completed successfully!");*/
            /*Console.ReadKey();
            Console.ReadKey();*/

            ///-------------------------------------------------
            CheckContentWorkflowDownloaded();
            CheckContentWorkflowChangeset();
            ContentWorkflowChangesetMatchToMainChangeset();
        }

        private void CheckContentWorkflowDownloaded()
        {
            var workspaceNames = GetWorkspaceNames();
            foreach (var name in workspaceNames)
            {
                var nameSplited = name.Split('@');
                var pathSplited = name.Split(' ').LastOrDefault();
                if (nameSplited[0] == "DTH_Content_Workflow")
                {
                    contentWorkflowProject = nameSplited[0];
                    contentWorkflowProjectPath = pathSplited;
                    Debug.WriteLine($"Is this same: {nameSplited[0]} or {contentWorkflowProject}");
                    Debug.WriteLine($"Project path: {pathSplited} or {contentWorkflowProjectPath}");
                }
                else
                {
                    Debug.WriteLine("Content Workflow project is not downloaded");
                }
            }
        }

        private void CheckContentWorkflowChangeset()
        {
            var directoryFound = contentWorkflowProjectPath.Split(':');
            RunCmd($"{directoryFound}:");
            RunCmd($"cd \"{contentWorkflowProjectPath}\"");

            var tempFile = Path.Combine(Path.GetTempPath(), "plastic_selector.txt");
            RunCmdWithOutput($"type .plastic\\plastic.selector > \"{tempFile}\"", contentWorkflowProjectPath);
            var output = RunCmdWithOutput($"type \"{tempFile}\"");
            Debug.WriteLine(output);

            var outputSplited = output.Split("/");
            var getNumber = outputSplited.LastOrDefault().Split('\"');
            contentWorkflowCurrentChangeset = int.Parse(getNumber[2]);
            Debug.WriteLine("Get the number: "+ contentWorkflowCurrentChangeset);
        }

        private void ContentWorkflowChangesetMatchToMainChangeset()
        {
            
        }

        #region CommonMethod

        private List<string> GetWorkspaceNames()
        {
            var workspaceNames = new List<string>();
            var output = RunCmdWithOutput("cm workspace list");

            /// Each line typically contains workspace info, e.g.: WorkspaceName  C:\Path\To\Workspace  
            /// Repository@Server
            foreach (var line in output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            {
                workspaceNames.Add(line);
                /*var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                {
                    //TO-DO: process parts[0] as workspace name
                }*/
            }
            return workspaceNames;
        }

        private List<int> GetMainBranchChangeset()
        {
            var workspaceNames = new List<int>();
            var output = RunCmdWithOutput("cm find changeset \"where branch='main'\" --format=\"{changesetid}\"");

            /// Each line typically contains workspace info, e.g.: WorkspaceName  C:\Path\To\Workspace  
            /// Repository@Server
            foreach (var line in output)
            {
                workspaceNames.Add(line);
            }
            return workspaceNames;
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
