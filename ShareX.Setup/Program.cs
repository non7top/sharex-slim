#region License Information (GPL v3)

/*
    ShareX - A program that allows you to take screenshots and share any file type
    Copyright (c) 2007-2026 ShareX Team

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/

#endregion License Information (GPL v3)

using ShareX.HelpersLib;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace ShareX.Setup
{
    internal class Program
    {
        [Flags]
        private enum SetupJobs
        {
            None = 0,
            CreateSetup = 1,
            CreatePortable = 1 << 1,
            CreateChecksumFile = 1 << 2,
            OpenOutputDirectory = 1 << 3,

            Release = CreateSetup | CreatePortable | CreateChecksumFile | OpenOutputDirectory
        }

        private const string AppName = "ShareX-slim";
        private const string Configuration = "Release";
        private const string Platform = "x64";
        private const string RuntimeId = "win-x64";
        private const string ISSFileName = "ShareX-slim-setup.iss";
        private const string InnoSetupCompilerPath = @"C:\Program Files (x86)\Inno Setup 6\ISCC.exe";

        private static SetupJobs Job { get; set; } = SetupJobs.Release;
        private static bool Silent { get; set; } = false;

        private static string ParentDir;
        private static string AppVersion;

        private static string SolutionPath => Path.Combine(ParentDir, "ShareX.Slim.sln");
        private static string BinDir => Path.Combine(ParentDir, "ShareX", "bin", Configuration, RuntimeId);
        private static string ExecutablePath => Path.Combine(BinDir, AppName + ".exe");

        private static string OutputDir => Path.Combine(ParentDir, "Output");
        private static string PortableOutputDir => Path.Combine(OutputDir, AppName + "-portable");

        private static string SetupDir => Path.Combine(ParentDir, "ShareX.Setup");
        private static string InnoSetupDir => Path.Combine(SetupDir, "InnoSetup");

        private static string SetupPath => Path.Combine(OutputDir, $"{AppName}-{AppVersion}-setup-{Platform}.exe");
        private static string PortableZipPath => Path.Combine(OutputDir, $"{AppName}-{AppVersion}-portable-{Platform}.zip");

        private static int Main(string[] args)
        {
            Console.WriteLine(AppName + " setup started.");

            CheckArgs(args);

            Console.WriteLine("Job: " + Job);

            if (!UpdatePaths())
            {
                return 1;
            }

            if (Directory.Exists(OutputDir))
            {
                Console.WriteLine("Cleaning output directory: " + OutputDir);

                Directory.Delete(OutputDir, true);
            }

            Directory.CreateDirectory(OutputDir);

            if (Job.HasFlag(SetupJobs.CreateSetup) && !CompileSetup())
            {
                return 1;
            }

            if (Job.HasFlag(SetupJobs.CreatePortable))
            {
                CreatePortableFolder(BinDir, PortableOutputDir);

                CreateZipFile(PortableOutputDir, PortableZipPath);
            }

            if (!Silent && Job.HasFlag(SetupJobs.OpenOutputDirectory))
            {
                FileHelpers.OpenFolder(OutputDir, false);
            }

            Console.WriteLine(AppName + " setup successfully completed.");

            return 0;
        }

        private static void CheckArgs(string[] args)
        {
            CLIManager cli = new CLIManager(args);
            cli.ParseCommands();

            Silent = cli.IsCommandExist("Silent");

            if (Silent)
            {
                Console.WriteLine("Silent: " + Silent);
            }

            CLICommand command = cli.GetCommand("Job");

            if (command != null)
            {
                if (Enum.TryParse(command.Parameter, out SetupJobs job))
                {
                    Job = job;
                }
                else
                {
                    Console.WriteLine("Invalid job: " + command.Parameter);

                    Environment.Exit(1);
                }
            }
        }

        private static bool UpdatePaths()
        {
            ParentDir = Directory.GetCurrentDirectory();

            if (!File.Exists(SolutionPath))
            {
                Console.WriteLine("Invalid parent directory: " + ParentDir);

                ParentDir = FileHelpers.GetAbsolutePath(@"..\..\..\..\");

                if (!File.Exists(SolutionPath))
                {
                    Console.WriteLine("Invalid parent directory: " + ParentDir);

                    return false;
                }
            }

            Console.WriteLine("Parent directory: " + ParentDir);

            if (!File.Exists(ExecutablePath))
            {
                Console.WriteLine($"Build output is missing: {ExecutablePath}");
                Console.WriteLine($"Build the {Configuration}|{Platform} configuration first.");

                return false;
            }

            FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(ExecutablePath);
            AppVersion = versionInfo.ProductVersion;

            Console.WriteLine("Application version: " + AppVersion);

            return true;
        }

        private static bool CompileSetup()
        {
            if (!File.Exists(InnoSetupCompilerPath))
            {
                Console.WriteLine("Inno Setup compiler is missing: " + InnoSetupCompilerPath);

                return false;
            }

            Console.WriteLine("Compiling setup file: " + ISSFileName);

            using (Process process = new Process())
            {
                process.StartInfo = new ProcessStartInfo()
                {
                    FileName = InnoSetupCompilerPath,
                    WorkingDirectory = InnoSetupDir,
                    Arguments = $"/Q \"{ISSFileName}\"",
                    UseShellExecute = false
                };

                process.Start();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    Console.WriteLine($"Inno Setup compiler failed with exit code {process.ExitCode}.");

                    return false;
                }
            }

            if (!File.Exists(SetupPath))
            {
                Console.WriteLine("Setup file was not produced: " + SetupPath);

                return false;
            }

            Console.WriteLine("Setup file compiled: " + SetupPath);

            CreateChecksumFile(SetupPath);

            return true;
        }

        private static void CreatePortableFolder(string source, string destination)
        {
            Console.WriteLine("Creating portable folder: " + destination);

            if (Directory.Exists(destination))
            {
                Directory.Delete(destination, true);
            }

            Directory.CreateDirectory(destination);

            FileHelpers.CopyFiles(source, destination, "*.exe");
            FileHelpers.CopyFiles(source, destination, "*.dll");
            FileHelpers.CopyFiles(source, destination, "*.json");

            FileHelpers.CopyFiles(Path.Combine(ParentDir, "Licenses"), Path.Combine(destination, "Licenses"), "*.txt");

            FileHelpers.CopyFiles(Path.Combine(source, "ShareX_File_Icon.ico"), destination);

            foreach (string directory in Directory.GetDirectories(source))
            {
                string language = Path.GetFileName(directory);

                if (Regex.IsMatch(language, "^[a-z]{2}(?:-[A-Z]{2})?$"))
                {
                    FileHelpers.CopyFiles(Path.Combine(source, language), Path.Combine(destination, "Languages", language), "*.resources.dll");
                }
            }

            // The marker file that makes the app keep its settings next to the executable.
            FileHelpers.CreateEmptyFile(Path.Combine(destination, "Portable"));

            Console.WriteLine("Portable folder created: " + destination);
        }

        private static void CreateZipFile(string source, string archivePath)
        {
            Console.WriteLine("Creating zip file: " + archivePath);

            ZipManager.Compress(source, archivePath);

            CreateChecksumFile(archivePath);
        }

        private static void CreateChecksumFile(string filePath)
        {
            if (Job.HasFlag(SetupJobs.CreateChecksumFile))
            {
                Console.WriteLine("Creating checksum file: " + filePath);

                Helpers.CreateChecksumFile(filePath);
            }
        }
    }
}
