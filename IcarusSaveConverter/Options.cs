// Copyright 2025 Crystal Ferrai
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Diagnostics.CodeAnalysis;

namespace IcarusSaveConverter
{
	/// <summary>
	/// Representation of passed in program arguments
	/// </summary>
	internal class Options
	{
		public const int ExpectedArgCount = 3;

		public ProgramMode Action { get; }

		public string ProspectPath { get; }

		public string PartsPath { get; }

		private Options(ProgramMode action, string prospectPath, string partsPath)
		{
			Action = action;
			ProspectPath = prospectPath;
			PartsPath = partsPath;
		}

		/// <summary>
		/// Parse and validate arguments
		/// </summary>
		public static bool TryParse(string[] args, Logger logger, [NotNullWhen(true)] out Options? options)
		{
			options = null;

			if (args.Length != ExpectedArgCount)
			{
				return false;
			}

			ProgramMode action;
			if (!Enum.TryParse(args[0], true, out action))
			{
				logger.Error($"Unrecognized action: {args[0]}");
				return false;
			}

			string? prospectPath = ParsePath(args[1], logger);
			if (prospectPath is null)
			{
				logger.Error($"Invalid prospect path: {args[1]}");
				return false;
			}

			string? partsPath = ParsePath(args[2], logger);
			if (partsPath is null)
			{
				logger.Error($"Invalid parts path: {args[2]}");
				return false;
			}

			options = new(action, prospectPath, partsPath);
			return true;
		}

		public static void PrintUsage(Logger logger)
		{
			logger.Information(
				"Converts an Icarus propect save file to or from a text-based format\n" +
				"Usage: IcarusSaveConverter [action] [prospect] [parts]\n" +
				"\n" +
				"  action    The action to perform. Must be one of the following.\n" +
				"            unpack: Unpack and convert the prospect file to text.\n" +
				"            pack: Convert an unpacked prospect back into a prospect file.\n" +
				"\n" +
				"  prospect  The path to a prospect file to either read or create depending\n" +
				"            on the specified action.\n" +
				"\n" +
				"  parts     The path to a directory of unpacked prospect parts that will\n" +
				"            either be created or read depending on the specified action."
				);
		}

		private static string? ParsePath(string path, Logger logger)
		{
			try
			{
				return Path.GetFullPath(path);
			}
			catch (Exception ex)
			{
				logger.Error($"An error occurred parsing the path: {path}\n[{ex.GetType().FullName}] {ex.Message}");
				return null;
			}
		}
	}

	internal enum ProgramMode
	{
		Unpack,
		Pack
	}
}
