using Xunit;
using Mini_Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace Mini_Compiler.Tests
{
    public class ProgramTests
    {
        public static IEnumerable<object[]> GetGoodPrograms()
        {
            List<string> filesNames = new List<string>();
            DirectoryInfo d = new DirectoryInfo(@".\GoodPrograms");//Assuming Test is your Folder
            FileInfo[] Files = d.GetFiles("*.txt"); //Getting Text files
            foreach (FileInfo file in Files)
            {
                filesNames.Add(file.Name);
            }
            List<object[]> ret = new List<object[]>();
            foreach (var fileName in filesNames)
            {
                ret.Add(new[] {".\\GoodPrograms\\" + fileName });
            }
            return ret;
        }

        public static IEnumerable<object[]> GetBadPrograms()
        {
            List<string> filesNames = new List<string>();
            DirectoryInfo d = new DirectoryInfo(@".\BadPrograms");//Assuming Test is your Folder
            FileInfo[] Files = d.GetFiles("*.txt"); //Getting Text files
            foreach (FileInfo file in Files)
            {
                filesNames.Add(file.Name);
            }
            List<object[]> ret = new List<object[]>();
            foreach (var fileName in filesNames)
            {
                ret.Add(new[] { ".\\BadPrograms\\" + fileName });
            }
            return ret;
        }

        [Theory]
        [MemberData(nameof(GetGoodPrograms))]
        public void GoodProgramTest(string filePath)
        {
            int result = Program.Main(new[] { filePath });
            Assert.Equal(0, result);

            ProcessStartInfo processStartInfo =
            new ProcessStartInfo(".\\lli.exe", filePath + ".ll");
            processStartInfo.RedirectStandardError = true;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.CreateNoWindow = true;
            processStartInfo.UseShellExecute = false;
            Process process = Process.Start(processStartInfo);

            process.WaitForExit(); //wait for 20 sec
            int exitCode = process.ExitCode;
            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();

            Assert.Equal(0, exitCode);
        }


        [Theory]
        [MemberData(nameof(GetBadPrograms))]
        public void BadProgramTest(string filePath)
        {
            int result = Program.Main(new[] { Path.GetFullPath(filePath) });
            Assert.True(result>0);
        }
    }
}