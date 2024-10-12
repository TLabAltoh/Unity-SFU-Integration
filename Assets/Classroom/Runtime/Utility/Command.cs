using System.Collections.Generic;

namespace TLab.SFU.Network.Util
{
    public static class Command
    {
        public static string THIS_NAME => "[Command] ";

        public static Dictionary<string, string> ParseCommand(string argment)
        {
            var commandDic = new Dictionary<string, string>();

            // ex: -a 123 -b 123 -c 123 ...
            var splited = argment.Split("-");

            for (int i = 1; i < splited.Length; i++)
            {
                var key = splited[i].Split(" ")[0];
                var value = splited[i].Split(" ")[1];

                commandDic[key] = value;
            }

            return commandDic;
        }
    }
}
