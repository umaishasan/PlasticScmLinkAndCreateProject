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

        public Controller(string selectOrganization, string projectName,string projectLocation)
        {
            this.selectOrganization = selectOrganization;
            this.projectName = projectName;
            this.projectLocation = projectLocation;
        }

        public void CreateProjectOnline()
        {
            // string repoName = "MyAutoRepo";
            string serverName = selectOrganization + this.server; // or your server name
            Debug.WriteLine($"Starting Plastic SCM Automation...{serverName}");

            RunCmd($"cm mkrep {projectName}@{serverName}");
            RunCmd($"cm mkws {projectName}_ws {projectLocation} --repository={projectName}@{serverName}");
            // Create default project files
            File.WriteAllText(Path.Combine(projectLocation, "README.md"), "# Default Project");
            RunCmd($"cm add \"{projectLocation}\"");
            RunCmd($"cm checkin -m \"Initial commit\" \"{projectLocation}\"");

            Debug.WriteLine("Automation completed successfully!");
            /*Console.ReadKey();
            Console.ReadKey();*/
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
    }
}
