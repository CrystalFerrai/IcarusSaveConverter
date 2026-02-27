// Copyright 2026 Crystal Ferrai
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

using IcarusSaveLib;
using Newtonsoft.Json;
using UeSaveGame;
using UeSaveGame.Json;
using UeSaveGame.PropertyTypes;

namespace IcarusSaveConverter.Serializers
{
	/// <summary>
	/// Splits and combines mount saves
	/// </summary>
	internal static class MountsSerializer
	{
		/// <summary>
		/// Split a mount save
		/// </summary>
		public static int SplitSave(MountsSave mounts, string partsPath, bool useActorId, Logger logger)
		{
			JsonSerializer serializer = new();
			serializer.Formatting = Formatting.Indented;
			serializer.ContractResolver = new IgnorePropertiesResolver(["RecorderBlob"]);

			logger.Information($"Writing {mounts.SavedMounts.Count} mounts...");

			int digitCount = (int)Math.Floor(Math.Log10(mounts.SavedMounts.Count) + 1);
			for (int i = 0; i < mounts.SavedMounts.Count; ++i)
			{
				SavedMount mount = mounts.SavedMounts[i];

				string prefix;
				if (useActorId)
				{
					IntProperty? actorGuidProperty = mount.RecorderData?.Properties.FirstOrDefault(p => p.Name.Equals("IcarusActorGUID"))?.Property as IntProperty;
					if (actorGuidProperty is not null)
					{
						prefix = actorGuidProperty.Value.ToString().PadLeft(7, '0');
					}
					else
					{
						logger.Debug($"Recorder at index {i} is missing an IcarusActorGUID property");
						prefix = "_" + i.ToString().PadLeft(digitCount, '0');
					}
				}
				else
				{
					prefix = i.ToString().PadLeft(digitCount, '0');
				}

				string outPath = Path.Combine(partsPath, $"{prefix}_{mount.SaveData.MountName}.json");
				try
				{
					using (FileStream stream = File.Create(outPath))
					using (StreamWriter writer = new(stream))
					using (JsonWriter jsonWriter = new JsonTextWriter(writer) { Formatting = Formatting.Indented, IndentChar = ' ', Indentation = 2 })
					{
						jsonWriter.WriteStartObject();

						jsonWriter.WritePropertyName(nameof(SavedMount.SaveData));
						serializer.Serialize(jsonWriter, mount.SaveData);

						jsonWriter.WritePropertyName("RecorderClass");
						if (mount.RecorderData is not null)
						{
							jsonWriter.WriteValue(mount.RecorderData.ComponentClassName);
						}
						else
						{
							jsonWriter.WriteNull();
						}

						jsonWriter.WritePropertyName(nameof(SavedMount.RecorderData));
						if (mount.RecorderData is not null)
						{
							jsonWriter.WriteStartArray();
							foreach (FPropertyTag property in mount.RecorderData.Properties)
							{
								PropertiesSerializer.WriteProperty(property, jsonWriter);
							}
							jsonWriter.WriteEndArray();
						}
						else
						{
							jsonWriter.WriteNull();
						}

						jsonWriter.WriteEndObject();
					}
				}
				catch (Exception ex)
				{
					logger.Error($"Error writing mount at index {i} ({mount.SaveData.MountName}). [{ex.GetType().FullName}] {ex.Message}");
					return 1;
				}
			}

			logger.Information("Done");
			return 0;
		}

		/// <summary>
		/// Combine a mount save
		/// </summary>
		public static int CombineSave(string partsPath, string outPath, Logger logger)
		{
			MountsSave mounts = new();

			JsonSerializer serializer = new();
			serializer.Formatting = Formatting.Indented;
			serializer.NullValueHandling = NullValueHandling.Ignore;

			string[] mountFiles;
			try
			{
				mountFiles = Directory.GetFiles(partsPath);
			}
			catch (Exception ex)
			{
				logger.Error($"Error reading directory: '{partsPath}'. [{ex.GetType().FullName}] {ex.Message}");
				return 1;
			}

			logger.Information($"Reading {mountFiles.Length} mount files...");
			foreach (string mountPath in mountFiles)
			{
				try
				{
					FMountSaveData saveData = default;
					string? recorderClass = null;
					List<FPropertyTag> recorderProperties = new();

					using (FileStream stream = File.OpenRead(mountPath))
					using (StreamReader reader = new(stream))
					using (JsonReader jsonReader = new JsonTextReader(reader))
					{
						while (jsonReader.Read())
						{
							if (jsonReader.TokenType == JsonToken.PropertyName)
							{
								if (jsonReader.Value!.Equals(nameof(SavedMount.SaveData)))
								{
									jsonReader.Read();
									saveData = serializer.Deserialize<FMountSaveData>(jsonReader);
								}
								else if (jsonReader.Value!.Equals("RecorderClass"))
								{
									jsonReader.Read();
									recorderClass = (string)jsonReader.Value;
								}
								else if (jsonReader.Value!.Equals(nameof(SavedMount.RecorderData)))
								{
									while (jsonReader.Read())
									{
										if (jsonReader.TokenType == JsonToken.StartObject)
										{
											recorderProperties.Add(PropertiesSerializer.ReadProperty(jsonReader)!);
										}
									}
									break;
								}
							}
						}
					}

					SavedMountRecorderData? recorderData = null;
					if (recorderClass is not null)
					{
						recorderData = new(recorderClass);
						foreach (FPropertyTag property in recorderProperties)
						{
							recorderData.Properties.Add(property);
						}
					}
					mounts.SavedMounts.Add(new() { SaveData = saveData, RecorderData = recorderData });
				}
				catch (Exception ex)
				{
					logger.Error($"Error reading mount file: '{mountPath}'. [{ex.GetType().FullName}] {ex.Message}");
					return 1;
				}
			}

			logger.Information("Creating mounts save...");
			try
			{
				using (FileStream stream = File.Create(outPath))
				{
					mounts.Save(stream);
				}
			}
			catch (Exception ex)
			{
				logger.Error($"Error writing output file: '{outPath}'. [{ex.GetType().FullName}] {ex.Message}");
				return 1;
			}

			logger.Information("Done");
			return 0;
		}

		/// <summary>
		/// Returns whether the given directory appears to contain a split mounts save
		/// </summary>
		public static bool IsMountsParts(string path)
		{
			string[] files;
			try
			{
				files = Directory.GetFiles(path, "*_*.json");
			}
			catch (Exception)
			{
				return false;
			}
			return files.Length > 0;
		}
	}
}
