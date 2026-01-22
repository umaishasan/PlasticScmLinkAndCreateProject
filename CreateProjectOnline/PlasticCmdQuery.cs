namespace CreateProjectOnline
{
    class PlasticCmdQuery
    {
        private string workspaceList = "cm workspace list";
        private string mainChangesetList = "cm find changeset \"where branch='main'\" --format=\"{changesetid}\"";
        private string sixChangesetList = "cm find changeset \"where branch='/main/UH-UnityUpgrade'\" --format=\"{changesetid}\"";
        private string repositoryList = "cm repo list --server=";
        private string statusHeader = "cm status --header";
        private string undoAllChanges = "cm undo . -r";
        private string refreshChanges = "cm status --refresh";
        private string notDeductedFiles = "cm status --noheader";
        private string switchToMain = "cm switch main";
        private string switchToUnityUpgrade = "cm switch /main/UH-UnityUpgrade";
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
        public string MainChangeset
        {
            get => mainChangesetList;
        }
        public string UnityUpgradeChangeset
        {
            get => sixChangesetList;
        }
        public string Repository
        {
            get => repositoryList;
        }
        public string Status
        {
            get => statusHeader;
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
        public string SwitchUnity2022
        {
            get => switchToMain;
        }
        public string SwitchUnity06
        {
            get => switchToUnityUpgrade;

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

    }
}