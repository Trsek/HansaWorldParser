using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HansaWorldParser
{
    class Program
    {
        static string global_txt = AppDomain.CurrentDomain.BaseDirectory + "global.txt";
        static string start_dir = AppDomain.CurrentDomain.BaseDirectory;
        static string error_txt = "error.txt";
        static string global_text = "global";
        static string external_text = "external";
        static string inner_text = "inner";
        static string outer_text = "outer";
        static string updating_text = "updating ";

        static string GetVersion()
        {
            System.Reflection.Assembly executingAssembly = System.Reflection.Assembly.GetExecutingAssembly();
            var fieVersionInfo = FileVersionInfo.GetVersionInfo(executingAssembly.Location);
            var version = fieVersionInfo.FileVersion;
            return version;
        }
        static void Help()
        {
            Console.WriteLine("HansaWorlParser ver. {0}.", GetVersion());
            Console.WriteLine("Syntax: ");
            Console.WriteLine(" -g (generate): generate file with all available global functions in actual directory (and subdirectory), store to file Global.txt");
            Console.WriteLine(" -gFileTxt: like -g but generate to <FileTxt>");
            Console.WriteLine(" -s (start directory): path where it start");
            Console.WriteLine(" -t (test): test external use of global functions in actual directory (and subdirectory), use global.txt, mistake store to error.txt file");
            Console.WriteLine(" -tFileHal: test external use of global functions for <FileHal>, use global.txt");
            Console.WriteLine(" -uFileTxt: use <FileTxt> no Global.txt");
            Console.WriteLine("");
            Console.WriteLine("Example:");
            Console.WriteLine(" HansaWorlParser -g");
            Console.WriteLine(" HansaWorlParser -t");
            Console.WriteLine(" HansaWorlParser -tFT4000Commands.hal");
            Console.WriteLine(" HansaWorlParser -uLocal.txt -tFT4000Commands.hal");
            Console.WriteLine("                                                  Software by Zdeno Sekerák 2019");
        }

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Help();
                return;
            }

            Console.WriteLine("HansaWorlParser (for help -?) ver. {0}.", GetVersion());

            if (args[0].Substring(0, 2) == "-x")
                args = File.ReadAllLines(args[0].Substring(2, args[0].Length - 2));

            foreach (var arg_value in args)
            {
                if (arg_value.Length < 2 || arg_value[0] != '-')
                    continue;

                string value = arg_value.Substring(2, arg_value.Length - 2).Trim();

                switch (arg_value[1])
                {
                    case '?':
                    case 'h':
                        Help();
                        break;

                    case 's':
                        start_dir = value;
                        break;

                    case 'g':
                        GenerateGlobal(start_dir);
                        break;

                    case 't':
                        CheckExternal();
                        break;

                    case 'u':
                        global_txt = value;
                        break;
                    default:
                        break;
                }
            }
        }

        public static string[] DirectorySearch(string dir)
        {
            List<string> files = new List<string>();
            try
            {
                files.AddRange(Directory.GetFiles(dir, "*.hal"));
                foreach (string d in Directory.GetDirectories(dir))
                {
                    files.AddRange(DirectorySearch(d));
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return files.ToArray();
        }

        static string RemoveArguments(string proc_name)
        {
            var func = Regex.Match(proc_name, @"\b[^()]+\((.*)\)");
            string fncName = func.Groups[0].Value.Split(new char[] { '(' })[0];
            var paramTags = Regex.Matches(func.Groups[1].Value, @"([^,]+\(.+?\))|([^,]+)");

            string [] fncParam = new string[paramTags.Count];
            for (int i = 0; i < fncParam.Length; i++)
            {
                string str = paramTags[i].ToString().Trim();
                fncParam[i] = str.Substring(0, str.LastIndexOf(" ") < 0 ? 0 : str.LastIndexOf(" "));
            }

            proc_name = fncName + "(" + string.Join(",", fncParam ) + ")";
            proc_name = proc_name.Replace("  ", " ").Replace(", ", ",");
            return proc_name;
        }

        static string[] GetAllFunct_hall(string file_path)
        {
            List<string> funct_global = new List<string>();
            string[] lines = File.ReadAllLines(file_path);

            for(int i=0; i<lines.Length - 1; i++)
            {
                if( lines[i].Equals(global_text, StringComparison.InvariantCultureIgnoreCase))
                {
                    string proc_name = lines[i + 1].Replace(updating_text, "").Trim();
                    proc_name = RemoveArguments(proc_name);

                    funct_global.Add(proc_name);
                }
            }
            return funct_global.ToArray();
        }

        static string[] CheckFunct_hall(string file_path, string[] funct_hall)
        {
            List<string> funct_mistake = new List<string>();
            string[] lines = File.ReadAllLines(file_path);

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains(external_text))
                {
                    string proc_name = lines[i].Trim();
                    proc_name = proc_name.Substring(external_text.Length, proc_name.Length - external_text.Length - 1).Trim();
                    proc_name = proc_name.Replace(inner_text, "").Replace(outer_text, "");
                    proc_name = proc_name.Replace("  ", " ").Replace(", ", ",").Trim();

                    var results = Array.FindAll(funct_hall, s => s.Equals(proc_name));
                    if(results.Length == 0)
                    {
                        //funct_mistake.Add(lines[i].Trim());
                        funct_mistake.Add(proc_name);
                    }
                }
            }
            return funct_mistake.ToArray();
        }

        static void GenerateGlobal(string start_dir)
        {
            List<string> funct_hall = new List<string>();
            string[] files = DirectorySearch(start_dir);

            Console.WriteLine("generating ...");
            foreach (string file_path in files)
            {
                Console.WriteLine(file_path);
                string[] funct_local = GetAllFunct_hall(file_path);
                funct_hall.AddRange(funct_local);
            }

            funct_hall.Sort();
            funct_hall = funct_hall.Distinct().ToList();
            System.IO.File.WriteAllLines(global_txt, funct_hall.ToArray());
        }

        static void CheckExternal()
        {
            List<string> funct_mistake = new List<string>();
            string[] files = DirectorySearch(start_dir);
            string[] funct_hall = System.IO.File.ReadAllLines(global_txt);

            Console.WriteLine("checking ...");
            foreach (string file_path in files)
            {
                Console.WriteLine(file_path);
                string[] funct_local = CheckFunct_hall(file_path, funct_hall);

                if(funct_local.Length > 0)
                {
                    funct_mistake.Add(">>> in file " + file_path);
                    funct_mistake.AddRange(funct_local);
                    funct_mistake.Add("");

                    foreach(string funct_local_line in funct_local)
                        Console.WriteLine(funct_local_line);
                    Console.WriteLine("");
                }
            }

            System.IO.File.WriteAllLines(error_txt, funct_mistake.ToArray());
        }
    }
}
