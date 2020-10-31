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
        public string workfolder { get; set; }
        public string setfoldername { get; set; } //textBox1.Tex
        public string excludefilename { get; set; }
        public string selectfilename { get; set; }
        public string beforeFolder { get; set; }
        public string afterFolder { get; set; }

        /// <summary>
        /// フルパスから最後のフォルダ名を取得
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public string GetLastFolderName(string path)
        {
            string lastFolder = null;
            try
            {
                string[] folders = path.Split('\\');
                if (folders.Length > 0)
                    lastFolder = folders[folders.Length - 1];
                else
                    lastFolder = null;
            }
            catch (Exception)
            {
                throw;
            }
            return lastFolder;
        }
        /// <summary>
        /// 引数のListから比較対象外のファイルを削除する
        /// 設定ファイル exclude.txt
        /// </summary>
        public List<string> GetReadyList(List<String> folderlist)
        {
            if (!(folderlist?.Count > 0))
            {
                return folderlist; //空なら抜ける
            }
            List<string> extension = new List<string>();//管理しない拡張子
            List<string> unnecessary = new List<string>();//管理しないファイル

            string FileName = Path.Combine(workfolder, excludefilename);//除外設定ファイル
            if (File.Exists(FileName))
            {
                IEnumerable<string> lines = File.ReadLines(FileName, SJIS);
                foreach (var line in lines.Where(c => c.Length > 2).Where(c => c.Substring(0, 2) == "*."))
                {
                    extension.Add(line);
                }

                foreach (var line in lines.Where(c => c.Length > 2).Where(c => c.Substring(0, 2) == "$$"))
                {
                    unnecessary.Add(line.Substring(2));
                }
            }
            else
            {
                return folderlist;
            }

            foreach (var sdat in extension) //指定された拡張子のファイルを削除する
            {
                folderlist.RemoveAll(c => Path.GetExtension(c).ToLower() == sdat.Substring(1).TrimEnd().ToLower());
            }

            foreach (var sdat in unnecessary) //管理しないファイルを削除する
            {
                folderlist.RemoveAll(c => Path.GetFileName(c).ToLower() == sdat.ToLower());
            }
            folderlist.RemoveAll(c => c.IndexOf("workarea", StringComparison.OrdinalIgnoreCase) >= 0);
            return folderlist;
        }

        /// <summary>
        /// ファイルコピー
        /// </summary>
        /// <param name="folder"></param>コピー先フォルダ名
        /// <param name="fname"></param>コピー元ファイル名
        public void fileCopy(string folder, string fname)
        {
            string fromfname = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), fname);//実行ファイルと同じフォルダ
            if (Directory.Exists(folder) & File.Exists(fromfname))
            {
                File.Copy(fromfname, Path.Combine(folder, fname), true);
            }
        }

        /// <summary>
        /// ファイル一覧list(FileSetDatas)を作成
        /// 比較対象外のファイルを削除する
        /// 指定されたフォルダの一覧を作成して
        /// 不要なフォルダ名を消してFileSetDatasを作成する
        /// </summary>
        public List<FileSetdata.FileSetdata> ListOfFiles()
        {
            List<FileSetdata.FileSetdata> filesetdatas = new List<FileSetdata.FileSetdata>();

            foreach (var sdat in GetReadyList(
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
        /// コピー先のフォルダを作成してからファイルをコピーする
        /// </summary>
        /// <param name="copyfilelist"></param>コピー対象List
        /// <param name="dest_str"></param>コピー先フォルダ
        /// <param name="Before"></param>作業前
        public void FolderCopy(List<FileSetdata.FileSetdata> copyfilelist, string dest_str, Boolean before)
        {
            string LastFolderName = GetLastFolderName(setfoldername);
            //string CopyfolderName = before ? beforeFolder : afterFolder;
            string Cdest_str = dest_str + (before ? beforeFolder : afterFolder);
            Directory.CreateDirectory(Cdest_str);

            foreach (var sdata in copyfilelist)
            {
                Directory.CreateDirectory(Cdest_str + @"\" + LastFolderName + sdata.FolderName);
                string fromfname = setfoldername + Path.Combine(sdata.FolderName, sdata.FileName);
                string tofname = Cdest_str + LastFolderName + Path.Combine(sdata.FolderName, sdata.FileName);
                if (File.Exists(fromfname))
                {
                    File.Copy(fromfname, tofname, true);
                }
            }
        }

        /// <summary>
        /// filesetdatsとselectfileのファイルを比較して同一のものをコピー対象List(copyfilelist)を作成
        /// </summary>
        /// <param name="filesetdatas"></param>ファイル一覧List
        /// <param name="before"></param>作業前
        public List<FileSetdata.FileSetdata> CopyFileListCreate(List<FileSetdata.FileSetdata> filesetdatas, Boolean before)
        {
            var copyfilelist = new List<FileSetdata.FileSetdata>();//コピー対象ファイル

            if (!File.Exists(Path.Combine(workfolder, selectfilename)) || (String.IsNullOrWhiteSpace(setfoldername)))
            {
                return copyfilelist; ;
            }

            foreach (var selectdata in File.ReadLines(Path.Combine(workfolder, selectfilename), SJIS))
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
