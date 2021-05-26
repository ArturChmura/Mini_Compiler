using Xunit;
using Mini_Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

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
                ret.Add(new[] { Path.GetFullPath(".\\GoodPrograms\\" + fileName) });
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
                ret.Add(new[] { Path.GetFullPath(".\\BadPrograms\\" + fileName) });
            }
            return ret;
        }

        [Theory]
        [MemberData(nameof(GetGoodPrograms))]
        public void GoodProgramTest(string filePath)
        {
            int result = Program.Main(new[] { filePath });
            Assert.Equal(0, result);
        }


        [Theory]
        [MemberData(nameof(GetBadPrograms))]
        public void BadProgramTest(string filePath)
        {
            int result = Program.Main(new[] { filePath });
            Assert.True(result>0);
        }
    }
}