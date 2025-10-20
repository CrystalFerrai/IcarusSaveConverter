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

		public bool UseActorId { get; }

		private Options(ProgramMode action, string prospectPath, string partsPath, bool useActorId)
		{
			Action = action;
			ProspectPath = prospectPath;
			PartsPath = partsPath;
			UseActorId = useActorId;
		}

		/// <summary>
		/// Parse and validate arguments
		/// </summary>
		public static bool TryParse(string[] args, Logger logger, [NotNullWhen(true)] out Options? options)
		{
			options = null;

			if (args.Length < ExpectedArgCount)
			{
				return false;
			}

			ProgramMode? action = null;
			string? prospectPath = null;
			string? partsPath = null;

			bool useActorId = false;

			int positionalIndex = 0;
			for (int i = 0; i < args.Length; ++i)
			{
				if (args[i].StartsWith("--"))
				{
					string option = args[i][2..].ToLowerInvariant();
					switch (option)
					{
						case "use-actor-id":
							useActorId = true;
							break;
					}
				}
				else
				{
					switch (positionalIndex)
					{
						case 0:
							if (Enum.TryParse(args[i], true, out ProgramMode value))
							{
								action = value;
							}
							break;
						case 1:
							prospectPath = ParsePath(args[i], logger);
							break;
						case 2:
							partsPath = ParsePath(args[i], logger);
							break;
					}

					++positionalIndex;
				}
			}

			if (!action.HasValue || prospectPath is null || partsPath is null)
			{
				logger.Error("Error parsing arguments");
				return false;
			}

			options = new(action.Value, prospectPath, partsPath, useActorId);
			return true;
		}

		public static void PrintUsage(Logger logger)
		{
			logger.Information(
				"Converts an Icarus propect save file to or from a text-based format\n" +
				"Usage: IcarusSaveConverter [action] [prospect] [parts] [[options]]\n" +
				"\n" +
				"  action    The action to perform. Must be one of the following.\n" +
				"            unpack: Unpack and convert the prospect file to text.\n" +
				"            pack: Convert an unpacked prospect back into a prospect file.\n" +
				"\n" +
				"  prospect  The path to a prospect file to either read or create depending\n" +
				"            on the specified action.\n" +
				"\n" +
				"  parts     The path to a directory of unpacked prospect parts that will\n" +
				"            either be created or read depending on the specified action.\n" +
				"\n" +
				"Options\n" +
				"\n" +
				"  --use-actor-id  File names of recorders will use the actor ID as a prefix\n" +
				"                  instead of the recorder index. This is useful for diffing\n" +
				"                  the output of two versions of the same prospect, but is\n" +
				"                  not recommended for editing and recombining the save. The\n" +
				"                  order of recorders will change, and actors with duplicate\n" +
				"                  IDs will be missing.\n" +
				"                  (Actors without an ID will use the prefix \"_\" followed by\n" +
				"                  the recorder index.)"
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
