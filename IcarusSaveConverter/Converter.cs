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

using IcarusSaveLib;
using Newtonsoft.Json;
using UeSaveGame;
using UeSaveGame.Json;
using UeSaveGame.PropertyTypes;
using UeSaveGame.StructData;

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
					return SplitSave(options.ProspectPath, options.PartsPath, options.UseActorId, logger);
				case ProgramMode.Pack:
					return CombineSave(options.PartsPath, options.ProspectPath, logger);
				default:
					logger.Error($"Unrecofgnized action '{options.Action}'");
					return 1;
			}
		}

		private static int SplitSave(string inPath, string partsPath, bool useActorId, Logger logger)
		{
			logger.Information("Loading prospect...");

			ProspectSave? prospect;
			try
			{
				using (FileStream file = File.OpenRead(inPath))
				{
					prospect = ProspectSave.Load(file);
				}
			}
			catch (Exception ex)
			{
				logger.Error($"Error reading input file. [{ex.GetType().FullName}] {ex.Message}");
				return 1;
			}

			if (prospect is null)
			{
				logger.Error("Error reading input file");
				return 1;
			}

			logger.Information("Creating/clearing parts directory...");
			try
			{
				if (Directory.Exists(partsPath))
				{
					Directory.Delete(partsPath, true);
				}
				Directory.CreateDirectory(partsPath);
			}
			catch (Exception ex)
			{
				logger.Error($"Error setting up parts directory. [{ex.GetType().FullName}] {ex.Message}");
				return 1;
			}

			JsonSerializer serializer = new();
			serializer.Formatting = Formatting.Indented;
			serializer.NullValueHandling = NullValueHandling.Ignore;

			logger.Information("Writing ProspectInfo...");

			try
			{
				using (FileStream stream = File.Create(Path.Combine(partsPath, "ProspectInfo.json")))
				using (StreamWriter writer = new(stream))
				{
					serializer.Serialize(writer, prospect.ProspectInfo);
				}
			}
			catch (Exception ex)
			{
				logger.Error($"Error writing PropsectInfo. [{ex.GetType().FullName}] {ex.Message}");
				return 1;
			}

			if (prospect.ProspectData.Count == 0)
			{
				logger.Warning("Prospect is missing data");
				return 0;
			}

			logger.Information("Writing ProspectData...");

			try
			{
				using (FileStream stream = File.Create(Path.Combine(partsPath, "ProspectData.json")))
				using (StreamWriter writer = new(stream))
				using (JsonWriter jsonWriter = new JsonTextWriter(writer) { Formatting = Formatting.Indented, IndentChar = ' ', Indentation = 2 })
				{
					jsonWriter.WriteStartArray();
					foreach (FPropertyTag property in prospect.ProspectData.Skip(1))
					{
						PropertiesSerializer.WriteProperty(property, jsonWriter);
					}
					jsonWriter.WriteEndArray();
				}
			}
			catch (Exception ex)
			{
				logger.Error($"Error writing ProspectData. [{ex.GetType().FullName}] {ex.Message}");
				return 1;
			}

			ArrayProperty recordersArray = (ArrayProperty)prospect.ProspectData[0].Property!;

			logger.Information($"Writing {recordersArray.Value!.Length} Recorders...");

			try
			{
				string recordersPath = Path.Combine(partsPath, "Recorders");
				Directory.CreateDirectory(recordersPath);

				int digitCount = (int)Math.Floor(Math.Log10(recordersArray.Value!.Length) + 1);
				for (int i = 0; i < recordersArray.Value!.Length; ++i)
				{
					FProperty recorder = (FProperty)recordersArray.Value!.GetValue(i)!;

					PropertiesStruct recorderStruct = (PropertiesStruct)((StructProperty)recorder).Value!;
					FString recorderName = (FString)recorderStruct.Properties[0].Property!.Value!;
					IList<FPropertyTag> recorderProperties = ProspectSerlializationUtil.DeserializeRecorderData(recorderStruct.Properties[1]);

					string prefix;
					if (useActorId)
					{
						IntProperty? actorGuidProperty = recorderProperties.FirstOrDefault(p => p.Name.Equals("IcarusActorGUID"))?.Property as IntProperty;
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

					string basePath = Path.Combine(recordersPath, $"{prefix}_{Path.GetFileName(recorderName)}");
					string outPath = $"{basePath}.json";
					for (int j = 0; File.Exists(outPath); ++j)
					{
						outPath = $"{basePath}_{j:00}.json";
					}

					using (FileStream stream = File.Open(outPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
					using (StreamWriter writer = new(stream))
					using (JsonWriter jsonWriter = new JsonTextWriter(writer) { Formatting = Formatting.Indented, IndentChar = ' ', Indentation = 2 })
					{
						jsonWriter.WriteStartObject();

						jsonWriter.WritePropertyName("Name");
						jsonWriter.WriteValue(recorderName);

						jsonWriter.WritePropertyName("Data");
						jsonWriter.WriteStartArray();
						foreach (FPropertyTag recorderProperty in recorderProperties)
						{
							PropertiesSerializer.WriteProperty(recorderProperty, jsonWriter);
						}
						jsonWriter.WriteEndArray();

						jsonWriter.WriteEndObject();
					}
				}
			}
			catch (Exception ex)
			{
				logger.Error($"Error writing recorders. [{ex.GetType().FullName}] {ex.Message}");
				return 1;
			}

			logger.Information("Done");
			return 0;
		}

		private static int CombineSave(string partsPath, string outPath, Logger logger)
		{
			ProspectSave prospect = new();
			FPropertyTag recorderBlobProperty = new(new("StateRecorderBlobs"), new(new(nameof(ArrayProperty))), EPropertyTagFlags.None);
			prospect.ProspectData.Add(recorderBlobProperty);

			JsonSerializer serializer = new();
			serializer.Formatting = Formatting.Indented;
			serializer.NullValueHandling = NullValueHandling.Ignore;

			logger.Information("Reading ProspectInfo...");
			try
			{
				using (FileStream stream = File.OpenRead(Path.Combine(partsPath, "ProspectInfo.json")))
				using (StreamReader reader = new(stream))
				using (JsonReader jsonReader = new JsonTextReader(reader))
				{
					prospect.ProspectInfo = serializer.Deserialize<FProspectInfo>(jsonReader);
				}
			}
			catch (Exception ex)
			{
				logger.Error($"Error reading ProspectInfo. [{ex.GetType().FullName}] {ex.Message}");
				return 1;
			}

			logger.Information("Reading ProspectData...");
			try
			{
				using (FileStream stream = File.OpenRead(Path.Combine(partsPath, "ProspectData.json")))
				using (StreamReader reader = new(stream))
				using (JsonReader jsonReader = new JsonTextReader(reader))
				{
					while (jsonReader.Read())
					{
						if (jsonReader.TokenType == JsonToken.StartObject)
						{
							prospect.ProspectData.Add(PropertiesSerializer.ReadProperty(jsonReader)!);
						}
					}
				}
			}
			catch (Exception ex)
			{
				logger.Error($"Error reading ProspectData. [{ex.GetType().FullName}] {ex.Message}");
				return 1;
			}

			string recordersPath = Path.Combine(partsPath, "Recorders");
			string[] recorderFiles;
			try
			{
				recorderFiles = Directory.GetFiles(recordersPath);
			}
			catch (Exception ex)
			{
				logger.Error($"Error reading directory: '{recordersPath}'. [{ex.GetType().FullName}] {ex.Message}");
				return 1;
			}

			logger.Information($"Reading {recorderFiles.Length} Recorders...");

			List<FProperty> recorders = new();

			FPropertyTypeName recorderType = new(new("StateRecorderBlob"));

			FString recorderClassName = new("ComponentClassName");
			FPropertyTypeName recorderClassType = new(new(nameof(StrProperty)));

			FString recorderDataName = new("BinaryData");
			FPropertyTypeName recorderDataType = new(new(nameof(ArrayProperty)));
			FPropertyTypeName recorderDataArrayType = new(new(nameof(ByteProperty)));

			FPropertyTypeName structType = new(new(nameof(StructProperty)));

			foreach (string recorderPath in recorderFiles)
			{
				try
				{
					using (FileStream stream = File.OpenRead(recorderPath))
					using (StreamReader reader = new(stream))
					using (JsonReader jsonReader = new JsonTextReader(reader))
					{
						FString? recorderName = null;
						List<FPropertyTag> recorderProperties = new();
						while (jsonReader.Read())
						{
							if (jsonReader.TokenType == JsonToken.PropertyName)
							{
								if (jsonReader.Value!.Equals("Name"))
								{
									recorderName = new(jsonReader.ReadAsString()!);
								}
								else if (jsonReader.Value!.Equals("Data"))
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

						if (recorderName is null) throw new InvalidDataException();

						FPropertyTag recorderNameProperty = new(recorderClassName, recorderClassType, EPropertyTagFlags.None)
						{
							Property = new StrProperty(recorderClassName) { Value = recorderName }
						};
						FPropertyTag recorderDataProperty = new(recorderDataName, recorderDataType, EPropertyTagFlags.None)
						{
							Property = new ArrayProperty(recorderDataName) { ItemType = recorderDataArrayType }
						};

						PropertiesStruct recorderStruct = new();
						recorderStruct.Properties.Add(recorderNameProperty);
						recorderStruct.Properties.Add(ProspectSerlializationUtil.SerializeRecorderData(recorderDataProperty, recorderProperties));

						StructProperty recorderProperty = new(recorderBlobProperty.Name)
						{
							StructType = recorderType,
							Value = recorderStruct
						};

						recorders.Add(recorderProperty);
					}
				}
				catch (Exception ex)
				{
					logger.Error($"Error reading recorder '{Path.GetFileNameWithoutExtension(recorderPath)}'. [{ex.GetType().FullName}] {ex.Message}");
					return 1;
				}
			}

			FPropertyTag recorderPrototype = new(recorderBlobProperty.Name, structType, EPropertyTagFlags.None)
			{
				Property = new StructProperty(recorderBlobProperty.Name)
				{
					StructType = recorderType
				}
			};

			ArrayProperty recorderBlobsArray = new(recorderBlobProperty.Name, structType, recorderPrototype)
			{
				ItemType = structType,
				Value = recorders.ToArray()
			};
			recorderBlobProperty.Property = recorderBlobsArray;

			logger.Information("Creating prospect...");
			try
			{
				using (FileStream stream = File.Create(outPath))
				{
					prospect.Save(stream);
				}
			}
			catch (Exception ex)
			{
				logger.Error($"Error creating prospect. [{ex.GetType().FullName}] {ex.Message}");
				return 1;
			}

			logger.Information("Done");
			return 0;
		}
	}
}
