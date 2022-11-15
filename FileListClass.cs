using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;

namespace FileListLibrary
{
    public class FileListClass
    {
        Encoding SJIS = Encoding.GetEncoding("Shift_JIS");
        public string workfolder { get; set; }    //コピー先のフォルダ名
        public string setfoldername { get; set; } //指定されたフォルダ名
        public string excludefilename { get; set; }
        public string selectfilename { get; set; }
        public string beforeFolder { get; set; }
        public string afterFolder { get; set; }

        /// <summary>
        /// フルパスから最後のフォルダ名を取得
        /// </summary>
        /// <param name="path">フルパス名</param>
        /// <returns>フルパスの最後のフォルダー名</returns>
        public string GetLastFolderName(string path)
        {
            string lastFolder = null;
            if(!String.IsNullOrWhiteSpace(path))
            {
                string[] folders = path.Split('\\');
                lastFolder = folders.Length > 0 ? folders[folders.Length - 1] : null;
            }
            return lastFolder;
        }
        
        /// <summary>
        /// 引数のListから比較対象外のファイルを削除する
        /// 設定ファイル exclude.txt
        /// <param name="WorkFolder">ワーク用フォルダ</param>
        /// <param name="folderlist"/>指定されたフォルダのファイル一覧List</param>
        /// </summary>
        public List<string> GetReadyList(string WorkFolder,  List<String> folderlist)
        {
            if (!(folderlist?.Count > 0))
            {
                return folderlist; //空なら抜ける
            }
 
            string FileName = Path.Combine(WorkFolder, excludefilename);//除外設定ファイル
            if (!File.Exists(FileName))
            {
                return folderlist;
            }
            
            IEnumerable<string> lines = File.ReadLines(FileName, SJIS);
            List<string> extension = new List<string>();//管理しない拡張子
            
            foreach (var line in lines.Where(c => c.Length > 2).Where(c => c.Substring(0, 2) == "*."))
            {
                extension.Add(line);
            }
            List<string> unnecessary = new List<string>();//管理しないファイル
            foreach (var line in lines.Where(c => c.Length > 2).Where(c => c.Substring(0, 2) == "$$"))
            {
                unnecessary.Add(line.Substring(2));
            }
           
            foreach (var sdat in extension) //管理しない拡張子のファイルを削除する
            {
                folderlist.RemoveAll(c => Path.GetExtension(c).ToLower() == sdat.Substring(1).TrimEnd().ToLower());
            }

            foreach (var sdat in unnecessary) //管理しないファイルを削除する
            {
                folderlist.RemoveAll(c => Path.GetFileName(c).ToLower() == sdat.ToLower());
            }
            folderlist.RemoveAll(c => c.IndexOf("workarea", StringComparison.OrdinalIgnoreCase) >= 0);//WorkAreaフォルダ削除
            return folderlist;
        }

        /// <summary>
        /// 実行フォルダに有るファイルをWorkフォルダへコピーする
        /// </summary>
        /// <param name="WorkFolder"></param>コピー先フォルダ名
        /// <param name="fname"></param>コピー元ファイル名
        public void fileCopy(string WorkFolder, string fname)
        {
            string fromfname = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), fname);//実行フォルダ
            if (Directory.Exists(WorkFolder) & File.Exists(fromfname))
            {
                File.Copy(fromfname, Path.Combine(WorkFolder, fname), true);
            }
        }

        /// <summary>
        /// ファイル一覧list(FileSetDatas)を作成
        /// setfoldername 指定されたフォルダ名
        /// 比較対象外のファイルを削除する
        /// 指定されたフォルダの一覧を作成して
        /// 不要なフォルダ名を消してFileSetDatasを作成する
        /// </summary>
        public List<FileSetdata.FileSetdata> ListOfFiles()
        {
            List<FileSetdata.FileSetdata> filesetdatas = new List<FileSetdata.FileSetdata>();
            foreach (var sdat in GetReadyList(workfolder,
                Directory.EnumerateFiles(setfoldername, "*.*", SearchOption.AllDirectories).ToList())
                .Select(c => c.Substring(setfoldername.Length)))
            {
                filesetdatas.Add(new FileSetdata.FileSetdata(
                   Path.GetFileName(sdat),
                   Path.GetDirectoryName(sdat),
                   Path.GetExtension(sdat)
                   ));
            }
            return filesetdatas;
        }

        /// <summary>
        /// copyfile内のファイルをコピー先のフォルダにコピーする
        /// </summary>
        /// <param name="copyfilelist"></param>コピー対象List
        /// <param name="WorkFolder"></param>コピー先フォルダ名
        /// <param name="Before"></param>作業前・作業後の判断
        public void FolderCopy(List<FileSetdata.FileSetdata> copyfilelist, string WorkFolder, Boolean before)
        {
            string lastfoldername = GetLastFolderName(setfoldername);
            string workfoldernamer = WorkFolder + (before ? beforeFolder : afterFolder);
            Directory.CreateDirectory(workfoldernamer);
            foreach (var sdata in copyfilelist)
            {
                Directory.CreateDirectory(workfoldernamer + @"\" + lastfoldername + sdata.FolderName);
                string fromfname = setfoldername + Path.Combine(sdata.FolderName, sdata.FileName);
                string tofname = workfoldernamer + lastfoldername + Path.Combine(sdata.FolderName, sdata.FileName);
                if (File.Exists(fromfname))
                {
                    File.Copy(fromfname, tofname, true);
                }
            }
        }

        /// <summary>
        /// filesetdatsとselectfileのファイルを比較して同一のものをコピー対象List(copyfilelist)を作成
        /// </summary>
        /// <param name="wfoldr"></param> WorkFolder
        /// /// <param name="filesetdatas"></param>ファイル一覧List
        public List<FileSetdata.FileSetdata> CopyFileListCreate(string WorkFolder, List<FileSetdata.FileSetdata> filesetdatas)
        {
            var copyfilelist = new List<FileSetdata.FileSetdata>();//コピー対象ファイル

            if (!File.Exists(Path.Combine(WorkFolder, selectfilename)))
            {
                return copyfilelist; ;
            }

            foreach (var selectdata in File.ReadLines(Path.Combine(WorkFolder, selectfilename), SJIS))
            {
                foreach (var sdata in filesetdatas.Where(c => Path.Combine(c.FolderName, c.FileName).Substring(1) == selectdata))
                {
                    copyfilelist.Add(sdata);
                }
            }
            return copyfilelist;
        }
    }
}
