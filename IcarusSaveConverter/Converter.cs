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

using IcarusSaveConverter.Serializers;
using IcarusSaveLib;

namespace IcarusSaveConverter
{
	/// <summary>
	/// Converts prospects
	/// </summary>
	internal static class Converter
	{
		/// <summary>
		/// Run the converter with the given options and return an exit code
		/// </summary>
		public static int Run(Options options, Logger logger)
		{
			switch (options.Action)
			{
				case ProgramMode.Unpack:
					return SplitSave(options.SavePath, options.PartsPath, options.UseActorId, logger);
				case ProgramMode.Pack:
					return CombineSave(options.PartsPath, options.SavePath, logger);
				default:
					logger.Error($"Unrecofgnized action '{options.Action}'");
					return 1;
			}
		}

		private static int SplitSave(string inPath, string partsPath, bool useActorId, Logger logger)
		{
			logger.Information("Loading save file...");

			FileStream file;
			try
			{
				file = File.OpenRead(inPath);
			}
			catch (Exception ex)
			{
				logger.Error($"Error reading input file. [{ex.GetType().FullName}] {ex.Message}");
				return 1;
			}

			try
			{
				if (ProspectSave.TryLoad(file, out ProspectSave? prospect))
				{
					if (!CreatePartsDirectory(partsPath, logger))
					{
						return 1;
					}

					return ProspectSerializer.SplitSave(prospect, partsPath, useActorId, logger);
				}

				if (MountsSave.TryLoad(file, out MountsSave? mounts))
				{
					if (!CreatePartsDirectory(partsPath, logger))
					{
						return 1;
					}

					return MountsSerializer.SplitSave(mounts, partsPath, useActorId, logger);
				}

				logger.Error("Unrecognized input file");
				return 1;
			}
			finally
			{
				file.Dispose();
			}
		}

		public static int CombineSave(string partsPath, string outPath, Logger logger)
		{
			if (ProspectSerializer.IsProspectParts(partsPath))
			{
				return ProspectSerializer.CombineSave(partsPath, outPath, logger);
			}

			if (MountsSerializer.IsMountsParts(partsPath))
			{
				return MountsSerializer.CombineSave(partsPath, outPath, logger);
			}

			logger.Error("Unrecognized parts directory");
			return 1;
		}

		private static bool CreatePartsDirectory(string partsPath, Logger logger)
		{
			logger.Information("Creating/clearing parts directory...");
			try
			{
				if (Directory.Exists(partsPath))
				{
					Directory.Delete(partsPath, true);
				}
				Directory.CreateDirectory(partsPath);
				return true;
			}
			catch (Exception ex)
			{
				logger.Error($"Error setting up parts directory. [{ex.GetType().FullName}] {ex.Message}");
				return false;
			}
		}
	}
}
