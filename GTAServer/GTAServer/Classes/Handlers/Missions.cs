using System.Text.RegularExpressions;

#nullable disable

namespace GTAServer
{
    public class Missions
    {
        private static readonly Regex FilePathFormat = new Regex(@"^/cloud/11/cloudservices/ugc/gta5mission/[^/]+/(1_0.jpg|1_1.jpg|0_0_en.json)$", RegexOptions.IgnoreCase);

        public static string GetPath(string absolutePath)
        {
            Match match = FilePathFormat.Match(absolutePath);

            if (match.Success)
            {
                string filePath = string.Format("bin/missions/{0}",
                    absolutePath.Replace("/cloud/11/cloudservices/ugc/gta5mission/", ""));

                if (File.Exists(filePath))
                {
                    return filePath;
                }

                return "DoesNotExist";
            }

            return string.Empty;
        }
    }
}
