/*
Copyright (C) 2024 Michael Gautier

This source code is free software; you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation; either version 2.1 of the License, or (at your option) any later version.

This source code is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public License along with this library. If not, see <http://www.gnu.org/licenses/>.

Author: Michael Gautier <michaelgautier.wordpress.com>
Initial Description: https://gautiertalkstechnology.wordpress.com/2024/06/01/convert-space-delimited-to-tabs/
Video: https://youtu.be/bn8qYvQmDnY
*/
using System.Diagnostics;

namespace FileTabifyer_Spaces
{
    internal static class Program
    {
        private static string _PathToSourcePDFs = string.Empty;
        private static string _PathToOutputDir = string.Empty;

        private const string _ExeNamePDFConvertToText = "pdftotext.exe";
        private const string _PDFConvertToTextExeFlags = "-table -eol dos -f 3 -l 3";

        static void Main(string[] args)
        {
            InterpretConsoleParameters(args);

            bool AllConditionsValid = ValidateStartupConditions();

            if (AllConditionsValid)
            {
                string[] PDFFilePaths = Directory.GetFiles(_PathToSourcePDFs, "*.pdf");

                foreach (string PDFFilePath in PDFFilePaths)
                {
                    string FileName = Path.GetFileNameWithoutExtension(PDFFilePath);

                    string OutputFilePathSpaceDelimited = Path.Combine(_PathToOutputDir, $"{FileName}.spt");
                    string OutputFilePathTabDelimited = Path.Combine(_PathToOutputDir, $"{FileName}.tsv");

                    ProcessStartInfo ProcessInfo = new(_ExeNamePDFConvertToText, $"{_PDFConvertToTextExeFlags} \"{PDFFilePath}\" \"{OutputFilePathSpaceDelimited}\"");

                    Process? ProcessResult = Process.Start(ProcessInfo);

                    bool ProcessExitSuccess = ProcessResult?.WaitForExit(120000) ?? false;

                    if (ProcessExitSuccess == false)
                    {
                        Console.WriteLine("Process Failed.");
                    }
                    else
                    {
                        using StreamReader Reader = new(OutputFilePathSpaceDelimited);
                        using StreamWriter Writer = new(OutputFilePathTabDelimited);

                        while(Reader.EndOfStream == false)
                        {
                            string FileLine = Reader.ReadLine() ?? string.Empty;

                            string OutputLine = TabifySpaceDelimitedText(FileLine);

                            Writer.WriteLine(OutputLine);
                        }

                        Reader.Close();

                        Writer.Flush();
                        Writer.Close();
                    }
                }
            }

            return;
        }

        private static string TabifySpaceDelimitedText(string text)
        {
            string Output = string.Empty;

            List<string> Result = [];

            int IndexOfSpace = -1;
            int IndexOfFirstLetter = -1;
            int IndexOfRecentLetter = -1;

            int TextLength = text.Length;

            for (int IndexOfChar = 0; IndexOfChar < TextLength; IndexOfChar++)
            {
                char CurrentChar = text[IndexOfChar];

                if(CurrentChar == ' ')
                {
                    IndexOfSpace = IndexOfChar;
                }
                else
                {
                    IndexOfRecentLetter = IndexOfChar;

                    if(IndexOfFirstLetter < 0)
                    {
                        IndexOfFirstLetter = IndexOfChar;
                    }
                }

                bool IsLastChar = (IndexOfChar + 1 == TextLength);
                bool Is2ndSpaceFollowingLetters = IndexOfRecentLetter > 0 && (IndexOfRecentLetter + 2 == IndexOfSpace);

                if (IndexOfRecentLetter > 0 && (Is2ndSpaceFollowingLetters || IsLastChar))
                {
                    int TextSpanLength = (IndexOfRecentLetter - IndexOfFirstLetter) + 1;

                    string TextSpan = text.Substring(IndexOfFirstLetter, TextSpanLength);

                    IndexOfFirstLetter = -1;
                    IndexOfRecentLetter = -1;

                    Result.Add(TextSpan);

                    IndexOfSpace = -1;
                }
            }

            Output = string.Join('\t', Result);

            return Output;
        }

        private static bool ValidateStartupConditions()
        {
            int InvalidConditionsCount = 0;

            if (Path.IsPathFullyQualified(_PathToSourcePDFs))
            {
                Directory.CreateDirectory(_PathToSourcePDFs);
            }

            if (Path.IsPathFullyQualified(_PathToOutputDir))
            {
                Directory.CreateDirectory(_PathToOutputDir);
            }

            if (Directory.Exists(_PathToSourcePDFs) == false)
            {
                InvalidConditionsCount++;
                Console.WriteLine("Invalid Output Directory specified.");
            }

            if (Directory.Exists(_PathToOutputDir) == false)
            {
                InvalidConditionsCount++;
                Console.WriteLine("Invalid Output Directory specified.");
            }

            return InvalidConditionsCount == 0;
        }

        private static void InterpretConsoleParameters(string[] args)
        {
            if (args.Length > 3)
            {
                string[] type_args = { args[0], args[2] };
                string[] value_args = { args[1], args[3] };

                if (type_args.Length < args.Length / 2)
                {
                    throw new InvalidOperationException($"{args.Length} Console parameters provided but only {type_args.Length} encoded for argument type.");
                }

                for (int i = 0; i < type_args.Length; i++)
                {
                    string type_arg = type_args[i];

                    switch (type_arg)
                    {
                        case "--pdfs_dir":
                            _PathToSourcePDFs = value_args[i];
                            break;
                        case "--output_dir":
                            _PathToOutputDir = value_args[i];
                            break;
                    }
                }
            }

            return;
        }
    }
}
