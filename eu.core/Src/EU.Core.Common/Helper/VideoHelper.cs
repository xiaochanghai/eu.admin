using System.Diagnostics;

namespace EU.Core.Common.Helper;

public class VideoHelper
{

    public static async Task<object> FileMerge(string lastModified, string fileExts, string NewfileName)
    {
        string erro = "";
        bool ok = false;
        try
        {
            string wwwroot = "E:\\EU\\EU.Core\\Admin\\EU.Core.Api\\wwwroot\\files\\upload\\";
            //wwwroot = $"{Directory.GetCurrentDirectory()}/wwwroot/";
            var temporary = Path.Combine(wwwroot, lastModified);//临时文件夹
                                                                //fileName = Request.Form["fileName"];//文件名
            string fileExt = fileExts;//获取文件后缀
            var files = Directory.GetFiles(temporary);//获得下面的所有文件
            DirectoryInfo di = new DirectoryInfo(wwwroot + NewfileName + "/");
            if (!di.Exists)
            {
                di.Create();
            }
            var finalPath = Path.Combine(wwwroot + lastModified + "/", NewfileName + fileExt);//最终的文件名（demo中保存的是它上传时候的文件名，实际操作肯定不能这样）
            var fs = new FileStream(finalPath, FileMode.Create);
            foreach (var part in files.OrderBy(x => x.Length).ThenBy(x => x))//排一下序，保证从0-N Write
            {
                var bytes = File.ReadAllBytes(part);
                await fs.WriteAsync(bytes, 0, bytes.Length);
                bytes = null;
                //System.IO.File.Delete(part);//删除分块
            }
            fs.Close();

            SplitVideo(finalPath, wwwroot);

            //Directory.Delete(temporary);//删除文件夹
            ok = true;
        }
        catch (Exception ex)
        {
            erro = ex.Message;
        }
        return ok;
    }

    /// <summary>
    /// 转成m3u8格式
    /// </summary>
    /// <param name="vFileName"></param>
    /// <param name="ExportName"></param>
    /// <param name="ffmpegPath"></param>
    /// <returns></returns>
    public static bool Convert2Flv(string vFileName, string ExportName, string ffmpegPath)
    {
        try
        {
            string Command = @" -i " + vFileName + " -profile:v baseline -level 3.0 -start_number 0 -hls_time 10 -hls_list_size 0 -f hls " + ExportName + "ylky.m3u8"; //m3u8格式   
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.FileName = ffmpegPath;
            p.StartInfo.Arguments = Command;
            p.StartInfo.WorkingDirectory = $"{Environment.CurrentDirectory}";

            #region cmd执行
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            p.Start();//启动线程
            p.WaitForExit();//等待完成
            p.Close();//关闭进程
            p.Dispose();//释放资源

            // convertVideoImage(vFileName, ExportName,ffmpegPath);
            #endregion
        }
        catch (Exception e)
        {
            throw e;
        }
        return true;
    }

    public static void SplitVideo(string inputVideoPath, string outputDirectory)
    {
          inputVideoPath = @"E:\EU\EU.Core\Admin\EU.Core.Api\wwwroot\files\upload\diA74tZENm59rWP3rY5tRfj9fVpJLJKB\2405261413315635680043040733927.mp4";
        int Threads = 1; //线程数
        string InputFile = "";//要切割的视频文件
        string OutputRuler = @"E:\EU\EU.Core\Admin\EU.Core.Api\wwwroot\files\upload\diA74tZENm59rWP3rY5tRfj9fVpJLJKB\" + Utility.GetSysID() + ".mp4";//输出文件名
        int TotalTime;//文件总时长(单位s)
        int SpiltTime = 20;//?时间切割一次(单位s)
        bool IsFailed = false; //失败标识
        object obj = new object();//线程锁
        int SpiltedTime = 0;//已切割时间
        int SpiltedFileCount = 0;//已切割文件数量 

        var p = new Process();
        p.StartInfo.FileName = "C:\\Users\\Administrator.CH-202209031648\\Desktop\\ffmpeg.exe";
        p.StartInfo.Arguments = $"-threads {Threads} -ss {SpiltedTime.ToString()} -i \"{inputVideoPath}\" -c copy -t {SpiltTime} \"{OutputRuler.Substring(0, OutputRuler.LastIndexOf("."))}-part{SpiltedFileCount}{OutputRuler.Substring(OutputRuler.LastIndexOf("."))}\" ".Trim();
        p.StartInfo.CreateNoWindow = true;
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.RedirectStandardError = true;
        p.StartInfo.RedirectStandardInput = true;
        p.StartInfo.RedirectStandardOutput = true;
        p.OutputDataReceived += Output;
        p.ErrorDataReceived += Output;
        p.Start();
        p.Exited += P_Exited;
        p.StandardInput.AutoFlush = true;
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();
        p.WaitForExit();
    }
    protected static void Output(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            
        }
    }

    private static void P_Exited(object sender, EventArgs e)
    { 
    }
}

