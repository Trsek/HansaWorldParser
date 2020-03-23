using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Data.SqlTypes;
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
        static string remote_text = "remote";
        static string updating_text = "updating";
        static string inner_text = "inner";
        static string outer_text = "outer";
        static string begin_text = "begin";
        static bool with_filename = false;
        static int INDEXOF_NONE = -1;

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
            Console.WriteLine(" -h this help");
            Console.WriteLine(" -g (generate): generate file with all available global functions in actual directory (and subdirectory), store to file global.txt");
            Console.WriteLine(" -gFileTxt: like -g but generate to <FileTxt>");
            Console.WriteLine(" -n (with filename): file name store to file global.txt with functions");
            Console.WriteLine(" -s (start directory or file): path where it start");
            Console.WriteLine(" -t (test): test external use of global functions in actual file or directory and subdirectory, use global.txt, mistake store to error.txt file");
            Console.WriteLine(" -uFileTxt: use <FileTxt> no Global.txt");
            Console.WriteLine(" -e (exception file): exception file from testing");
            Console.WriteLine(" -xSetup: parameters store in file <Setup>");
            Console.WriteLine("");
            Console.WriteLine("Example:");
            Console.WriteLine(" HansaWorlParser -g");
            Console.WriteLine(" HansaWorlParser -shalcust -g");
            Console.WriteLine(" HansaWorlParser -sPrinters -t");
            Console.WriteLine(" HansaWorlParser -s.. -eorigin_windows.hal -t");
            Console.WriteLine(" HansaWorlParser -sFT4000Commands.hal -t");
            Console.WriteLine(" HansaWorlParser -uLocal.txt -sFT4000Commands.hal -t");
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

            List<string> except_files = new List<string>();

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

                    case 'e':
                        except_files.Add(value);
                        break;

                    case 'n':
                        with_filename = true;
                        break;

                    case 'g':
                        GenerateGlobal(start_dir);
                        break;

                    case 't':
                        CheckExternal(start_dir, except_files);
                        break;

                    case 'u':
                        global_txt = value;
                        break;
                    default:
                        break;
                }
            }
        }

        public static string[] DirectorySearch(string dir_or_file)
        {
            List<string> files = new List<string>();
            try
            {
                if (File.Exists(dir_or_file))
                {
                    files.Add(dir_or_file);
                }
                else
                {
                    files.AddRange(Directory.GetFiles(dir_or_file, "*.hal"));
                    foreach (string d in Directory.GetDirectories(dir_or_file))
                    {
                        files.AddRange(DirectorySearch(d));
                    }
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return files.ToArray();
        }

        static string RemoveArgumentsName(string proc_name)
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

            proc_name = fncName.Replace(updating_text, "").Trim() + "(" + string.Join(",", fncParam ) + ")";
            proc_name = proc_name.Replace("  ", " ").Replace(", ", ",");
            return proc_name;
        }

        static string StripComments(string code)
        {
            var re = @"(@(?:""[^""]*"")+|""(?:[^""\n\\]+|\\.)*""|'(?:[^'\n\\]+|\\.)*')|//.*|/\*(?s:.*?)\*/";
            return Regex.Replace(code, re, "$1");
        }

        static bool IsLeftString(string line, string text)
        {
            line = line.Trim();
            if ((line.Length == text.Length)
             && (line.Equals(text, StringComparison.InvariantCultureIgnoreCase)))
                return true;

            if ((line.Length > text.Length)
             && (line.IndexOf(text + " ", StringComparison.InvariantCultureIgnoreCase) == 0))
                return true;

            return false;
        }

        static string[] ClearPathSource(string[] funct_hall)
        {
            for(int i=0; i<funct_hall.Length; i++)
            {
                funct_hall[i] = funct_hall[i].Split('\t')[0];
            }

            return funct_hall;
        }

        // remove global, begin
        static string GetNormProcedure(string proc_name)
        {
            string proc_name_norm = "";
            foreach (string name in proc_name.Split(' '))
            {
                if (name.Equals(global_text, StringComparison.InvariantCultureIgnoreCase))
                    continue;

                if (name.Equals(begin_text, StringComparison.InvariantCultureIgnoreCase))
                    break;

                if (name.Trim().Length > 0)
                    proc_name_norm += name.Trim() + " ";
            }
            return proc_name_norm;
        }

        static string GetProcedureName(string proc_name)
        {
            var func = Regex.Match(proc_name, @"\b[^()]+\((.*)\);");
            string withoutPar = func.Groups[0].Value.Split(new char[] { '(' })[0];
            return withoutPar.Split(' ').Last();
        }

        static string[] GetAllFunct_hall(string file_path)
        {
            List<string> funct_global = new List<string>();
            string[] lines = File.ReadAllLines(file_path);

            for (int i=0; i<lines.Length - 1; i++)
            {
                if (IsLeftString(lines[i], global_text))
                {
                    // find text begin
                    string proc_name = "";
                    do
                    {
                        lines[i] = StripComments(lines[i]);
                        proc_name += lines[i] + " ";
                    }
                    while ((i < (lines.Length - 1))
                           && !lines[i].Contains(")") 
                           && (lines[i++].IndexOf(begin_text, StringComparison.InvariantCultureIgnoreCase) == INDEXOF_NONE));

                    proc_name = GetNormProcedure(proc_name);
                    proc_name = proc_name.Contains("(")
                            ? RemoveArgumentsName(proc_name)
                            : proc_name.Trim();

                    funct_global.Add(proc_name + (with_filename? ("\t[" + file_path + "]"): ""));
                }
            }
            return funct_global.ToArray();
        }

        static List<string> CheckFunct_hall(string file_path, string[] funct_hall)
        {
            List<string> funct_mistake = new List<string>();
            string[] lines = File.ReadAllLines(file_path);

            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = StripComments(lines[i]);
                if (lines[i].Contains(external_text))
                {
                    string proc_name = lines[i].Trim();
                    while (!proc_name.Contains(")"))
                    {
                        i++;    // je to prasarna
                        proc_name += " " + StripComments(lines[i]).Trim();
                    }

                    proc_name = proc_name.Substring(external_text.Length, proc_name.Length - external_text.Length - 1).Trim();
                    proc_name = proc_name.Replace(updating_text, "").Replace(inner_text, "").Replace(outer_text, "").Replace(remote_text, "");
                    proc_name = proc_name.Replace("  ", " ").Replace(", ", ",").Trim();

                    var results = Array.FindAll(funct_hall, s => s.Equals(proc_name));
                    if(results.Length == 0)
                    {
                        funct_mistake.Add(file_path + "(" + (i + 1) + "): Error 1:" + proc_name);
                    }
                }
            }
            return funct_mistake;
        }

        static List<string> CheckUnusedFunct_hall(string file_path)
        {
            List<string> funct_warning = new List<string>();
            List<string> funct_duplicity = new List<string>();
            string[] lines = File.ReadAllLines(file_path);

            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = StripComments(lines[i]);
                if (lines[i].Contains(external_text))
                {
                    string proc_name = lines[i].Trim();
                    while (!proc_name.Contains(")"))
                    {
                        i++;    // je to prasarna
                        proc_name += " " + StripComments(lines[i]).Trim();
                    }
                    proc_name = GetProcedureName(proc_name);

                    var results = Array.FindAll(lines, s => s.Contains(proc_name));
                    if (results.Length <= 1)
                    {
                        funct_warning.Add(file_path + "(" + (i + 1) + "): Warning 1: unused " + external_text + " function " + proc_name + "()");
                    }

                    if (funct_duplicity.Contains(proc_name))
                    {
                        funct_warning.Add(file_path + "(" + (i + 1) + "): Warning 2: duplicity external " + proc_name + "()");
                    }
                    funct_duplicity.Add(proc_name);
                }
            }
            return funct_warning;
        }

        static void GenerateGlobal(string start_dir_or_file)
        {
            List<string> funct_hall = new List<string>();
            string[] files = DirectorySearch(start_dir_or_file);

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

        static void CheckExternal(string start_dir_or_file, List<string> except_files)
        {
            List<string> funct_mistake = new List<string>();
            string[] files = DirectorySearch(start_dir_or_file);
            string[] funct_hall = ClearPathSource(System.IO.File.ReadAllLines(global_txt));

            Console.WriteLine("checking ...");
            foreach (string file_path in files)
            {
                if (except_files.Contains(Path.GetFileName(file_path)))
                    continue;

                Console.WriteLine(file_path);
                List<string> funct_local = CheckFunct_hall(file_path, funct_hall);
                List<string> funct_unused = CheckUnusedFunct_hall(file_path);

                funct_local.AddRange(funct_unused);
                if (funct_local.Count > 0)
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
