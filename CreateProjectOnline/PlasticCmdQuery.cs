namespace CreateProjectOnline
{
    class PlasticCmdQuery
    {
        private string workspaceList = "cm workspace list";
        private string mainBranch = "/main";
        private string unityUpgradeBranch = "/main/UH-UnityUpgrade";
        private string repositoryList = "cm repo list --server=";
        private string statusHeader = "cm status --header";
        private string statusShort = "cm status --short";
        private string undoAllChanges = "cm undo . -r";
        private string refreshChanges = "cm status --refresh";
        private string notDeductedFiles = "cm status --noheader";
        private string repositoryMake = "cm mkrep";
        private string makeWorkspace = "cm mkworkspace";
        private string addFilesForPush = "cm add . --recursive";
        private string checkinAllChanges = "cm checkin -m \"Get work from changeset # \"";
        private string plasticVersion = "cm version";
        private string plasticLogin = "cm whoami";


        public string DownloadWorkSpace
        {
            get => workspaceList;
        }
        public string MainBranch
        {
            get => mainBranch;
        }
        public string UnityUpgradeBranch
        {
            get => unityUpgradeBranch;
        }
        public string Repository
        {
            get => repositoryList;
        }
        public string Status
        {
            get => statusHeader;
        }
        public string CheckPendingChanges
        {
            get => statusShort;
        }
        public string UndoChanges
        {
            get => undoAllChanges;
        }
        public string RefreshStatus
        {
            get => refreshChanges;
        }
        public string NotDeductedAddedFiles
        {
            get => notDeductedFiles;
        }
        public string CreateRepository
        {
            get => repositoryMake;
        }
        public string CreateWorkspace
        {
            get => makeWorkspace;
        }
        public string AddFiles
        {
            get => addFilesForPush;
        }
        public string PushChanges
        {
            get => checkinAllChanges;
        }
        public string PlasticVersion
        {
            get => plasticVersion;
        }
        public string PlasticLogin
        {
            get => plasticLogin;
        }

        public string FindChangesetsOfBranch(string branchName)
        {
            string ids = "\"{changesetid}\"";
            string findChangesetCmd = $"cm find changeset \"where branch='{branchName}'\" --format={ids}";
            return findChangesetCmd;
        }
        public string SwitchToBranch(string branchName)
        {
            string switchCmd = $"cm switch {branchName}";
            return switchCmd;
        }

    }
}