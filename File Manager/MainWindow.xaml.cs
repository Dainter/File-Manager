using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media;
using File_Manager.Model;


namespace File_Manager
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow
    {
        DispatcherTimer StatusUpadteTimer;
        List<SysFileIf> CurrentFiles;
        bool isFormLoaded = false;

        public MainWindow()
        {
            InitializeComponent();
            isFormLoaded = true;
        }

        private void FrmMain_Loaded(object sender, RoutedEventArgs e)
        {
            InitCustomerData();
            StatusUpdateTimer_Init();
        }

        public void InitCustomerData()
        {
            string DesktopFolder;

            BuildPreviewString();
            //根据当前用户PC具体环境，选择桌面作为初始目录
            DesktopFolder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            MainLayout.Resources["CurrentPath"] = DesktopFolder;
            CurrentFiles = new List<SysFileIf>();
            UpdateFileList(DesktopFolder);
        }

        #region StatusTimer
        private void StatusUpdateTimer_Init()
        {
            StatusUpadteTimer = new DispatcherTimer();
            StatusUpadteTimer.Interval = new TimeSpan(0, 0, 3);
            StatusUpadteTimer.Tick += new EventHandler(StatusUpdateTimer_Tick);
            StatusUpadteTimer.IsEnabled = false;
        }

        private void StatusUpdateTimer_Tick(object sender, EventArgs e)
        {
            StatusLabel.Content = "Ready";
            StatusUpadteTimer.IsEnabled = false;
        }

        public void ShowStatus(string sStatus, bool isLongTime = false)
        {
            StatusLabel.Content = sStatus;
            if (isLongTime == true)
            {
                return;
            }
            StatusUpadteTimer.Start();
        }
        #endregion

        #region FileList
        private void OpenCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog opendialog;
            string strPath;

            //初始化对话框，文件类型，过滤器，初始路径等设置
            opendialog = new System.Windows.Forms.FolderBrowserDialog();
            //成功选取文件后，根据文件类型执行读取函数
            if (opendialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }
            strPath = opendialog.SelectedPath;
            //更新系统资源
            MainLayout.Resources["CurrentPath"] = strPath;
            //更新当前目录下文件列表
            UpdateFileList(strPath);
        }

        private void UpdateFileList(string strPath)
        {
            if (strPath == "")
            {
                return;
            }
            try
            {
                FileListView.ItemsSource = null;
                DirectoryInfo curDir = new DirectoryInfo(strPath);
                CurrentFiles = GetFileList(curDir);
                FileListView.ItemsSource = CurrentFiles;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<SysFileIf> GetFileList(DirectoryInfo curDir)
        {
            DirectoryInfo[] dirs = curDir.GetDirectories(); //获取目录
            FileInfo[] files = curDir.GetFiles();   //获取文件
            List<SysFileIf> fileList = new List<SysFileIf>();

            //列出文件夹
            foreach (DirectoryInfo dir in dirs)
            {
                SysDirectory newDirItem = new SysDirectory(dir);
                fileList.Add(newDirItem);
            }
            //列出文件
            foreach (FileInfo file in files)
            {
                SysFile newFileItem = new SysFile(file);
                fileList.Add(newFileItem);
            }
            return fileList;
        }

        private void ExitCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();
        }

        private void FileListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FileListView.SelectedItems.Count < 1)
            {
                ShowStatus("Ready");
                return;
            }
            ShowStatus(FileListView.SelectedItems.Count.ToString() + " Items been Selected", true);
            if (FileListView.SelectedItems.Count > 1)
            {
                return;
            }
            UpdateFileInfo((SysFileIf)FileListView.SelectedItem);
            return;
        }

        private void FileListView_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            FileListView.SelectedItems.Clear();
        }

        private void UpdateFileInfo(SysFileIf file)
        {
            NameLabel.Text = file.Name;
            TypeLabel.Text = file.Type;
            SizeLabel.Text = file.Size;
            FullPathLabel.Text = file.FullPath;
            CreateTimeLabel.Text = file.CreateTime;
            LastWriteTimeLabel.Text = file.WriteTime;
            LastAccessTimeLabel.Text = file.AccessTime;
        }

        private void RefreshCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            UpdateFileList(PathBox.Text);
        }

        private void GoUpButton_Click(object sender, RoutedEventArgs e)
        {
            DirectoryInfo currentDir = new DirectoryInfo(PathBox.Text);
            if( currentDir.Parent == null )
            {
                return;
            }
            PathBox.Text = currentDir.Parent.FullName;
            UpdateFileList(PathBox.Text);
        }

        private void FileListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if( ((ListView) sender).SelectedItem == null )
            {
                return;
            }
            SysFileIf currentFile = (SysFileIf)((ListView)sender).SelectedItem;
            if (currentFile.Type == "Folder")
            {
                PathBox.Text = currentFile.FullPath;
                UpdateFileList(PathBox.Text);
            }
        }
        #endregion

        #region FileSort
        private void MoveUpCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            object curItem = SortPriorityList.SelectedItem;
            int index = SortPriorityList.SelectedIndex;
            if (index == 0)
            {
                return;
            }
            SortPriorityList.Items.Remove(curItem);
            SortPriorityList.Items.Insert(index-1, curItem);
            SortPriorityList.SelectedIndex = index - 1;
        }

        private void MoveUpCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SortPriorityList.SelectedIndex > -1)
            {
                e.CanExecute = true;
                return;
            }
            e.CanExecute = false;
            return;
        }

        private void MoveDownCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            object curItem = SortPriorityList.SelectedItem;
            int index = SortPriorityList.SelectedIndex;
            if (index == SortPriorityList.Items.Count -1)
            {
                return;
            }
            SortPriorityList.Items.Remove(curItem);
            SortPriorityList.Items.Insert(index + 1, curItem);
            SortPriorityList.SelectedIndex = index + 1;
        }

        private void MoveDownCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SortPriorityList.SelectedIndex > -1)
            {
                e.CanExecute = true;
                return;
            }
            e.CanExecute = false;
            return;
        }

        private void SortFileCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Sort("FileOnly");
        }

        private void SortFolderCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Sort("FolderOnly");
        }

        private void SortAllCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Sort("All");
        }

        private void Sort(string sSortMode)
        {
            List<SysFileIf> sortList;
            List<SysSorter> condList;

            condList = BuildCondition();
            sortList = SelectSortFiles(CurrentFiles, sSortMode);
            FileComparer newComparer = new FileComparer(condList);
            sortList.Sort(newComparer);
            FileListView.ItemsSource = null;
            AdjustList(sSortMode, sortList);
            FileListView.ItemsSource = CurrentFiles;

            ShowStatus("Sort Completed");
        }

        private List<SysFileIf> SelectSortFiles(List<SysFileIf> list, string sSortMode)
        {
            List<SysFileIf> sortList = new List<SysFileIf>();

            foreach (SysFileIf curItem in list)
            {
                switch (sSortMode)
                {
                    case "FolderOnly":
                        if (curItem.Type == "Folder")
                        {
                            sortList.Add(curItem);
                        }
                        break;
                    case "All":
                        sortList.Add(curItem);
                        break;
                    default:
                        if (curItem.Type != "Folder")
                        {
                            sortList.Add(curItem);
                        }
                        break;
                }
            }
            foreach (SysFileIf curItem in sortList)
            {
                list.Remove(curItem);
            }
            return sortList;
        }

        private List<SysSorter> BuildCondition()
        {
            List<SysSorter> resultList = new List<SysSorter>();

            foreach (StackPanel curItem in SortPriorityList.Items)
            {
                SysSorter newCond;

                if (((TextBlock)curItem.Children[0]).Text == "Name")
                {
                    if (((CheckBox)curItem.Children[1]).IsChecked == false)
                    {
                        newCond = new SysSorter(SortField.Name, SortDirectrion.Ascending);
                    }
                    else
                    {
                        newCond = new SysSorter(SortField.Name, SortDirectrion.Descending);
                    }
                    resultList.Add(newCond);
                    continue;
                }

                if (((TextBlock)curItem.Children[0]).Text == "Type")
                {
                    if (((CheckBox)curItem.Children[1]).IsChecked == false)
                    {
                        newCond = new SysSorter(SortField.Type, SortDirectrion.Ascending);
                    }
                    else
                    {
                        newCond = new SysSorter(SortField.Type, SortDirectrion.Descending);
                    }
                    resultList.Add(newCond);
                    continue;
                }

                if (((TextBlock)curItem.Children[0]).Text == "Size")
                {
                    if (((CheckBox)curItem.Children[1]).IsChecked == false)
                    {
                        newCond = new SysSorter(SortField.Size, SortDirectrion.Ascending);
                    }
                    else
                    {
                        newCond = new SysSorter(SortField.Size, SortDirectrion.Descending);
                    }
                    resultList.Add(newCond);
                    continue;
                }

                if (((TextBlock)curItem.Children[0]).Text == "Create Time")
                {
                    if (((CheckBox)curItem.Children[1]).IsChecked == false)
                    {
                        newCond = new SysSorter(SortField.CreateTime, SortDirectrion.Ascending);
                    }
                    else
                    {
                        newCond = new SysSorter(SortField.CreateTime, SortDirectrion.Descending);
                    }
                    resultList.Add(newCond);
                    continue;
                }
                
                if (((TextBlock)curItem.Children[0]).Text == "Write Time")
                {
                    if (((CheckBox)curItem.Children[1]).IsChecked == false)
                    {
                        newCond = new SysSorter(SortField.WriteTime, SortDirectrion.Ascending);
                    }
                    else
                    {
                        newCond = new SysSorter(SortField.WriteTime, SortDirectrion.Descending);
                    }
                    resultList.Add(newCond);
                    continue;
                }
            }

            return resultList;
        }

        private void AdjustList(string sSortMode, List<SysFileIf> targetList)
        {
            int index = 0;
            if (sSortMode == "All" || sSortMode == "FileOnly")
            {
                foreach (SysFileIf curItem in targetList)
                {
                    CurrentFiles.Add(curItem);
                }
                return;
            }
            foreach (SysFileIf curItem in targetList)
            {
                CurrentFiles.Insert(index, curItem);
                index++;
            }
            return;
        }
        #endregion

        #region Rename
        private void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            if (PreviewButton.IsChecked == true)
            {
                PreviewLabel.Visibility = Visibility.Visible;
                return;
            }
            PreviewLabel.Visibility = Visibility.Hidden;
            return;
        }

        private void PrefixBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isFormLoaded == false)
            {
                return;
            }
            BuildPreviewString();
        }

        private void SuffixBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isFormLoaded == false)
            {
                return;
            }
            BuildPreviewString();
        }

        private void StartBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int intResult;
            if (isFormLoaded == false)
            {
                return;
            }
            if (int.TryParse(StartBox.Text, out intResult) == false)
            {
                ShowStatus("Please input number in the Start box");
                return;
            }
            BuildPreviewString();
        }

        private void StepBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int intResult;
            if (isFormLoaded == false)
            {
                return;
            }
            if (int.TryParse(StartBox.Text, out intResult) == false)
            {
                ShowStatus("Please input number in the Step box");
                return;
            }
            BuildPreviewString();
        }

        private void BuildPreviewString()
        {
            string strPrefix = PrefixBox.Text;
            string strSuffix = SuffixBox.Text;
            int intStart, intStep;
            string strPreview = "";

            int.TryParse(StartBox.Text, out intStart);
            int.TryParse(StepBox.Text, out intStep);
            for (int index = 0; index < 2; index++)
            {
                strPreview += strPrefix + intStart.ToString() + strSuffix + " ";
                intStart += intStep;
            }
            strPreview += "...";
            PreviewLabel.Content = "Preview: "+strPreview;
        }

        private void RenameFileCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Rename("FileOnly");
        }

        private void RenameFolderCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Rename("FolderOnly");
        }

        private void RenameAllCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Rename("All");
        }

        private void Rename(string sRenameMode)
        {
            List<SysFileIf> renameList;
            string strPrefix = PrefixBox.Text;
            string strSuffix = SuffixBox.Text;
            int intStart, intStep, index;
            string strNewName;
            string strOldPath, strNewPath;
            string strFormat;

            renameList = SelectRenameFile(sRenameMode);
            strFormat = BuildFormatString(renameList.Count);
            int.TryParse(StartBox.Text, out intStart);
            int.TryParse(StepBox.Text, out intStep);
            index = 0;
            foreach (SysFileIf curFile in renameList)
            {
                strOldPath = curFile.FullPath;
                strNewName = strPrefix + (intStart + intStep * index).ToString(strFormat) + strSuffix;
                strNewPath = curFile.NewFilePath(strNewName);
                if (curFile.Type == "Folder")
                {
                    if (Directory.Exists(strNewPath) == true)
                    {
                        ShowStatus("New folder is conflict with an old one, please change it.");
                        return;
                    }
                }
                else
                {
                    if (File.Exists(strNewPath) == true)
                    {
                        ShowStatus("New file is conflict with an old one, please change it.");
                        return;
                    }
                }
                index++;
            }
            index = 0;
            foreach (SysFileIf curFile in renameList)
            {
                strOldPath = curFile.FullPath;
                strNewName = strPrefix + (intStart + intStep * index).ToString(strFormat) + strSuffix;
                strNewPath = curFile.NewFilePath(strNewName);
                if (curFile.Type == "Folder")
                {
                    Directory.Move(strOldPath, strNewPath);
                }
                else
                {
                    File.Move(strOldPath, strNewPath);
                }
                index++;
            }
            UpdateFileList(PathBox.Text);
        }

        private List<SysFileIf> SelectRenameFile(string sRenameMode)
        {
            List<SysFileIf> renameList = new List<SysFileIf>();

            foreach (SysFileIf curItem in CurrentFiles)
            {
                switch (sRenameMode)
                {
                    case "FolderOnly":
                        if (curItem.Type == "Folder")
                        {
                            renameList.Add(curItem);
                        }
                        break;
                    case "All":
                        renameList.Add(curItem);
                        break;
                    default:
                        if (curItem.Type != "Folder")
                        {
                            renameList.Add(curItem);
                        }
                        break;
                }
            }
            return renameList;
        }

        private string BuildFormatString(int iCount)
        {
            int len = iCount.ToString().Length;
            return "D" + len.ToString();
        }

        #endregion

        #region Tools

        private void FormatFileCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            FormatName("FileOnly");
        }

        private void FormatFolderCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            FormatName("FolderOnly");
        }

        private void FormatAllCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            FormatName("All");
        }

        private void FormatName(string sRenameMode)
        {
            List<SysFileIf> renameList;
            string strNewName;
            string strOldPath, strNewPath;
            string strFormat;

            renameList = SelectRenameFile(sRenameMode);
            strFormat = BuildFormatString(renameList.Count);

            foreach (SysFileIf curFile in renameList)
            {
                strOldPath = curFile.FullPath;
                strNewName = FormatName( curFile.DisplayName, strFormat );
                strNewPath = curFile.NewFilePath(strNewName);
                if (curFile.Type == "Folder")
                {
                    Directory.Move(strOldPath, strNewPath);
                }
                else
                {
                    File.Move(strOldPath, strNewPath);
                }

            }
            UpdateFileList(PathBox.Text);
        }

        private string FormatName(string oldName, string strFormat)
        {
            const string pattern = @"[\d]+";
            Regex rex = new Regex( pattern );
            string newName = oldName;
            var match = rex.Match( oldName );
            int index;
            int.TryParse( match.Value, NumberStyles.Integer, new CultureInfo( "en-US" ), out index  );
            newName = newName.Replace( match.Value, index.ToString( strFormat ) );
            return newName;
        }

        private void RecursiveModifyCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            List<SysSorter> condition = BuildCondition();
            DirectoryInfo curDir = new DirectoryInfo( PathBox.Text );

            foreach( var curItem in curDir.GetDirectories())
            {
                List<SysFileIf> sortList = SelectFilesInFolder(curItem, condition);
                if( sortList.Count == 0 )
                {
                    continue;
                }
                RenameFilesInFolder(sortList);
            }
        }

        private List<SysFileIf> SelectFilesInFolder(DirectoryInfo curDir, List<SysSorter> condition)
        {
            List<SysFileIf> sortList = GetFileList( curDir );
            
            sortList = SelectSortFiles(sortList, "FileOnly");
            FileComparer newComparer = new FileComparer(condition);
            sortList.Sort(newComparer);
            return sortList;
        }

        private void RenameFilesInFolder(List<SysFileIf> renameList)
        {
            string strPrefix = PrefixBox.Text;
            string strSuffix = SuffixBox.Text;
            int intStart, intStep;

            var strFormat = BuildFormatString(renameList.Count);
            int.TryParse(StartBox.Text, out intStart);
            int.TryParse(StepBox.Text, out intStep);
            var index = 0;
            foreach (SysFileIf curFile in renameList)
            {
                var strOldPath = curFile.FullPath;
                var strNewName = strPrefix + (intStart + intStep * index).ToString(strFormat) + strSuffix;
                var strNewPath = curFile.NewFilePath(strNewName);
                File.Move(strOldPath, strNewPath);
                index++;
            }
        }

        private void RecursiveDeleteCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            List<SysSorter> condition = BuildCondition();
            DirectoryInfo curDir = new DirectoryInfo(PathBox.Text);

            foreach (var curItem in curDir.GetDirectories())
            {
                List<SysFileIf> sortList = SelectFilesInFolder(curItem, condition);
                if (sortList.Count == 0)
                {
                    continue;
                }
                DeletePngInFolder(sortList);
            }
        }

        private void DeletePngInFolder(List<SysFileIf> renameList)
        {
            foreach (SysFileIf curFile in renameList)
            {
                if(curFile.GetType() != typeof(SysFile))
                {
                    continue;
                }
                if((curFile as SysFile).Extension.ToLower() != ".png")
                {
                    continue;
                }
                File.Delete(curFile.FullPath);
            }
        }

        private void BatchGenerateCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            BatchGenerateWindow batchGenerate = new BatchGenerateWindow();
            batchGenerate.ShowDialog();
        }
        #endregion

        #region Remove Name Text
        private void RemoveFileNameTextCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            RemoveFileNameText();
        }

        private void RemoveFolderNameTextCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            RemoveFolderNameText();
        }

        private void RemoveAllNameTextCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            RemoveFileNameText();
            RemoveFolderNameText();
        }

        private void RemoveFileNameText()
        {
            string removeText = RemoveTextBox.Text;
            if (RemoveTextBox.Text == "")
            {
                return;
            }
            DirectoryInfo curDir = new DirectoryInfo(PathBox.Text);
            RecursiveRemoveFileText(removeText, curDir);
            UpdateFileList(PathBox.Text);
        }

        private void RecursiveRemoveFileText(string removeText, DirectoryInfo currentDirectoryInfo)
        {
            var directories = currentDirectoryInfo.GetDirectories();
            foreach (var curItem in directories)
            {
                RecursiveRemoveFileText(removeText, curItem);
            }

            var files = currentDirectoryInfo.GetFiles();
            foreach (var curFile in files)
            {
                string oldPath = curFile.FullName;
                string newPath = curFile.Name.Replace(removeText, "").Trim();
                if (!IsFileNameLegal(newPath))
                {
                    continue;
                }
                File.Move(oldPath, curFile.DirectoryName+ @"\" + newPath);
            }
        }

        bool IsFileNameLegal(string name)
        {
            if (name.Length < 1)
            {
                return false;
            }

            int pos = name.LastIndexOf('.', 0);
            if (pos == 0 || pos == name.Length -1)
            {
                return false;
            }
            return true;
        }

        private void RemoveFolderNameText()
        {
            string removeText = RemoveTextBox.Text;
            if (RemoveTextBox.Text == "")
            {
                return;
            }
            DirectoryInfo curDir = new DirectoryInfo(PathBox.Text);
            RecursiveRemoveFolderText(removeText, curDir);
            UpdateFileList(PathBox.Text);
        }

        private void RecursiveRemoveFolderText(string removeText, DirectoryInfo currentDirectoryInfo)
        {
            var directories = currentDirectoryInfo.GetDirectories();
            foreach (var curItem in directories)
            {
                RecursiveRemoveFolderText(removeText, curItem);
                string oldPath = curItem.FullName;
                string newPath = curItem.Name.Replace(removeText, "").Trim();
                if (newPath.Length < 1 || (curItem.Parent.FullName + @"\" + newPath) == oldPath)
                {
                    continue;
                }

                try
                {
                    Directory.Move(oldPath, curItem.Parent.FullName + @"\" + newPath);
                }
                catch (Exception e)
                {
                    throw;
                }
            }
        }
        #endregion


    }
}
