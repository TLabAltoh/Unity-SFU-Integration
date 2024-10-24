using System.Collections.Generic;

namespace TLab.VRProjct
{
    public static class CommandLine
    {
        public static string THIS_NAME => "[CommandLine] ";

        public static (string, Dictionary<string, string>) Parse(string command)
        {
            var splitIndex = command.IndexOf(" ");
            var program = command.Substring(0, splitIndex - 1);
            var argments = command.Substring(splitIndex);

            return (program, ParseArgments(argments));
        }

        public static Dictionary<string, string> ParseArgments(string argment)
        {
            var options = new Dictionary<string, string>();

            // ex: -a 123 -b 123 -c 123 ...
            var splited = argment.Split("-");

            for (int i = 1; i < splited.Length; i++)
            {
                var key = splited[i].Split(" ")[0];
                var value = splited[i].Split(" ")[1];

                options[key] = value;
            }

            return options;
        }
    }
}
