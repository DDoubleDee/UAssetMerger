using System.Security.Cryptography;
using System.Text;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;
using UAssetAPI.UnrealTypes;
using UAssetAPI.Unversioned;

namespace UAssetMergerLibrary
{
    public class UAssetMerger
    {

        static PropertyData GetTrueProp(UAsset orgAsset, UAsset modAsset, PropertyData prop)
        {
            if (prop is ObjectPropertyData)
            {
                if (int.TryParse(prop.RawValue.ToString(), out int intValue))
                {
                    if (intValue < 0)
                    {
                        int index = GetImportIndex(orgAsset, modAsset.Imports[-intValue - 1]);
                        return new ObjectPropertyData(new FName(orgAsset, prop.Name.Value.Value))
                        {
                            RawValue = FPackageIndex.FromRawIndex(-index - 1)
                        };
                    }
                    else if (intValue > 0)
                    {
                        int index = GetExportIndex(orgAsset, modAsset.Exports[intValue - 1]);
                        return new ObjectPropertyData(new FName(orgAsset, prop.Name.Value.Value))
                        {
                            RawValue = FPackageIndex.FromRawIndex(index + 1)
                        };
                    }
                }
            }
            return prop;
        }

        static int GetExportIndex(UAsset asset, Export export)
        {
            return asset.Exports.FindIndex(e =>
                e.ObjectName.Value == export.ObjectName.Value &&
                e.ObjectName.Number == export.ObjectName.Number
            );
        }

        static int GetImportIndex(UAsset asset, Import import)
        {
            return asset.Imports.FindIndex(i =>
                i.ObjectName.Value == import.ObjectName.Value &&
                i.ObjectName.Number == import.ObjectName.Number &&
                i.ClassName.Value == import.ClassName.Value &&
                i.ClassPackage.Value == import.ClassPackage.Value
            );
        }

        static string GetImportString(int index, UAsset asset)
        {
            return asset.Imports[-index - 1].ObjectName.Value.Value + asset.Imports[-index - 1].ObjectName.Number.ToString() + asset.Imports[-index - 1].ClassName.Value.Value + asset.Imports[-index - 1].ClassPackage.Value.Value;
        }

        static string GetExportString(int index, UAsset asset)
        {
            return asset.Exports[index - 1].ObjectName.Value.Value + asset.Exports[index - 1].ObjectName.Number.ToString();
        }

        static string GetPropertyHash(PropertyData prop, UAsset asset)
        {
            using var ms = new MemoryStream();
            var writer = new AssetBinaryWriter(ms, asset);
            prop.Write(writer, false);
            byte[] hash = MD5.HashData(ms.ToArray());
            return Convert.ToHexString(hash);
        }
        static void HandlePropertyData(UAsset orgAsset, List<StructPropertyData> orgData, UAsset modAsset, List<StructPropertyData> modData)
        {
            modData.ForEach(modRow =>
            {
                File.AppendAllLines(Path.Combine(AppContext.BaseDirectory, "logs.txt"), [modRow.Name.Value.Value]);
                bool existing = false;
                orgData.ForEach(orgRow =>
                {
                    if (orgRow.Name.Value.Value == modRow.Name.Value.Value)
                    {
                        HandlePropertyData(orgAsset, orgRow.Value, modAsset, modRow.Value);
                        existing = true;
                        return;
                    }
                });
                if (!existing)
                {
                    orgData.Add(modRow);
                }
            });
        }

        static void HandlePropertyData(UAsset orgAsset, List<PropertyData> orgData, UAsset modAsset, List<PropertyData> modData, bool replaceObj = false)
        {
            modData.ForEach(modRow =>
            {
                bool existing = false;
                orgData.ForEach(orgRow =>
                {
                    if (orgRow.Name.Value.Value == modRow.Name.Value.Value)
                    {
                        if (orgRow is StructPropertyData orgStruct && modRow is StructPropertyData modStruct)
                            HandlePropertyData(orgAsset, orgStruct.Value, modAsset, modStruct.Value, replaceObj);
                        else if (orgRow is DoublePropertyData orgDouble && modRow is DoublePropertyData modDouble)
                            orgDouble.Value = modDouble.Value;
                        else if (orgRow is BoolPropertyData orgBool && modRow is BoolPropertyData modBool)
                            orgBool.Value = modBool.Value;
                        else if (orgRow is IntPropertyData orgInt && modRow is IntPropertyData modInt)
                            orgInt.Value = modInt.Value;
                        else if (orgRow is FloatPropertyData orgFloat && modRow is FloatPropertyData modFloat)
                            orgFloat.Value = modFloat.Value;
                        else if (orgRow is StrPropertyData orgStr && modRow is StrPropertyData modStr)
                            orgStr.Value = modStr.Value;
                        else if (orgRow is TextPropertyData orgText && modRow is TextPropertyData modText)
                            orgText.Value = modText.Value;
                        else if (orgRow is EnumPropertyData orgEnum && modRow is EnumPropertyData modEnum)
                            orgEnum.Value = new FName(orgAsset, modEnum.Value.Value);
                        else if (orgRow is ObjectPropertyData orgObj && modRow is ObjectPropertyData modObj)
                        {
                            orgObj = (ObjectPropertyData)GetTrueProp(orgAsset, modAsset, modObj);
                        }
                        else if (orgRow is MapPropertyData orgMap && modRow is MapPropertyData modMap)
                        {
                            Dictionary<string, PropertyData> keyHashes = [];
                            bool isObjProp = false;
                            foreach (var orgItem in orgMap.Value)
                            {
                                if (orgItem.Key is ObjectPropertyData)
                                {
                                    isObjProp = true;
                                    if (int.TryParse(orgItem.Key.RawValue.ToString(), out int intValue))
                                    {
                                        if (intValue < 0)
                                        {
                                            if (replaceObj)
                                                keyHashes.Add(intValue.ToString(), orgItem.Key);
                                            else
                                                keyHashes.Add(GetImportString(intValue, orgAsset), orgItem.Key);
                                        }
                                        else if (intValue > 0)
                                        {
                                            if (replaceObj)
                                                keyHashes.Add(intValue.ToString(), orgItem.Key);
                                            else
                                                keyHashes.Add(GetExportString(intValue, orgAsset), orgItem.Key);
                                        }
                                    }
                                }
                                else
                                    keyHashes.Add(GetPropertyHash(orgItem.Key, orgAsset), orgItem.Key);
                            }
                            foreach (var modItem in modMap.Value)
                            {
                                if (modItem.Key is ObjectPropertyData && isObjProp)
                                {
                                    if (int.TryParse(modItem.Key.RawValue.ToString(), out int intValue))
                                    {
                                        if (intValue < 0)
                                        {
                                            if (keyHashes.TryGetValue(GetImportString(intValue, modAsset), out PropertyData? valueByString) && !replaceObj)
                                            {
                                                orgMap.Value[valueByString] = GetTrueProp(orgAsset, modAsset, modItem.Value);
                                            }
                                            else if (keyHashes.TryGetValue(intValue.ToString(), out PropertyData? valueByIndex) && replaceObj)
                                            {
                                                orgMap.Value.Remove(valueByIndex);
                                                int index = GetImportIndex(orgAsset, modAsset.Imports[-intValue - 1]);
                                                ObjectPropertyData propertyData = new(new FName(orgAsset, modItem.Key.Name.Value.Value))
                                                {
                                                    RawValue = FPackageIndex.FromRawIndex(-index - 1)
                                                };
                                                orgMap.Value.Add(propertyData, GetTrueProp(orgAsset, modAsset, modItem.Value));
                                            }
                                            else if (!replaceObj)
                                            {
                                                int index = GetImportIndex(orgAsset, modAsset.Imports[-intValue - 1]);
                                                ObjectPropertyData propertyData = new(new FName(orgAsset, modItem.Key.Name.Value.Value))
                                                {
                                                    RawValue = FPackageIndex.FromRawIndex(-index - 1)
                                                };
                                                orgMap.Value.Add(propertyData, GetTrueProp(orgAsset, modAsset, modItem.Value));
                                            }
                                        }
                                        else if (intValue > 0)
                                        {
                                            if (keyHashes.TryGetValue(GetExportString(intValue, modAsset), out PropertyData? valueByString) && !replaceObj)
                                            {
                                                orgMap.Value[valueByString] = GetTrueProp(orgAsset, modAsset, modItem.Value);
                                            }
                                            else if (keyHashes.TryGetValue(intValue.ToString(), out PropertyData? valueByIndex) && replaceObj)
                                            {
                                                orgMap.Value.Remove(valueByIndex);
                                                int index = GetExportIndex(orgAsset, modAsset.Exports[intValue - 1]);
                                                ObjectPropertyData propertyData = new(new FName(orgAsset, modItem.Key.Name.Value.Value))
                                                {
                                                    RawValue = FPackageIndex.FromRawIndex(index + 1)
                                                };
                                                orgMap.Value.Add(propertyData, GetTrueProp(orgAsset, modAsset, modItem.Value));
                                            }
                                            else if (!replaceObj)
                                            {
                                                int index = GetExportIndex(orgAsset, modAsset.Exports[intValue - 1]);
                                                ObjectPropertyData propertyData = new(new FName(orgAsset, modItem.Key.Name.Value.Value))
                                                {
                                                    RawValue = FPackageIndex.FromRawIndex(index + 1)
                                                };
                                                orgMap.Value.Add(propertyData, GetTrueProp(orgAsset, modAsset, modItem.Value));
                                            }
                                        }
                                    }
                                }
                                else if (isObjProp)
                                {
                                    Console.WriteLine($"Map {modMap.Name.Value.Value} key types misaligned. Aborting.");
                                    return;
                                }
                                else
                                {
                                    if (keyHashes.TryGetValue(GetPropertyHash(modItem.Key, modAsset), out PropertyData? value))
                                    {
                                        orgMap.Value[value] = GetTrueProp(orgAsset, modAsset, modItem.Value);
                                    }
                                    else
                                    {
                                        orgMap.Value.Add(modItem.Key, GetTrueProp(orgAsset, modAsset, modItem.Value));
                                    }
                                }
                            }
                        }
                        else if (orgRow is ArrayPropertyData orgArray && modRow is ArrayPropertyData modArray)
                        {
                            if (modArray.ArrayType.Value.Value == "ObjectProperty" && orgArray.ArrayType.Value.Value == "ObjectProperty")
                            {
                                string[] orgObjects = [];
                                foreach (var orgItem in orgArray.Value)
                                {
                                    if (int.TryParse(orgItem.RawValue.ToString(), out int intValue))
                                    {
                                        if (intValue < 0)
                                            orgObjects = [.. orgObjects, GetImportString(intValue, orgAsset)];
                                        else if (intValue > 0)
                                            orgObjects = [.. orgObjects, GetExportString(intValue, orgAsset)];
                                    }
                                }
                                foreach (var modItem in modArray.Value)
                                {
                                    if (int.TryParse(modItem.RawValue.ToString(), out int intValue))
                                    {
                                        if (intValue == 0)
                                            continue;
                                        if (intValue < 0)
                                        {
                                            if (orgObjects.Contains(GetImportString(intValue, modAsset)))
                                                continue;
                                            int index = GetImportIndex(orgAsset, modAsset.Imports[-intValue - 1]);
                                            ObjectPropertyData propertyData = new(new FName(orgAsset, modItem.Name.Value.Value))
                                            {
                                                RawValue = FPackageIndex.FromRawIndex(-index - 1)
                                            };
                                            orgArray.Value = [.. orgArray.Value, propertyData];
                                        }
                                        else if (intValue > 0)
                                        {
                                            if (orgObjects.Contains(GetExportString(intValue, modAsset)))
                                                continue;
                                            int index = GetExportIndex(orgAsset, modAsset.Exports[intValue - 1]);
                                            ObjectPropertyData propertyData = new(new FName(orgAsset, modItem.Name.Value.Value))
                                            {
                                                RawValue = FPackageIndex.FromRawIndex(index + 1)
                                            };
                                            orgArray.Value = [.. orgArray.Value, propertyData];
                                        }
                                    }
                                }
                            }
                            else //if (modArray.ArrayType.Value.Value == "StructProperty" && orgArray.ArrayType.Value.Value == "StructProperty")
                            {
                                File.AppendAllLines(Path.Combine(AppContext.BaseDirectory, "logs.txt"), [modArray.Name.Value.Value]);
                                List<string> orgHashes = [];
                                foreach (var orgItem in orgArray.Value)
                                {
                                    orgHashes.Add(GetPropertyHash(orgItem, orgAsset));
                                }
                                List<string> modHashes = [];
                                foreach (var modItem in modArray.Value)
                                {
                                    string modHash = GetPropertyHash(modItem, modAsset);
                                    modHashes.Add(modHash);
                                    if (orgHashes.Contains(modHash))
                                        continue;
                                    orgArray.Value = [.. orgArray.Value, (PropertyData)modItem.Clone()];
                                    HandlePropertyData(orgAsset, [orgArray.Value.Last()], modAsset, [modItem], true);
                                }
                                File.AppendAllLines(Path.Combine(AppContext.BaseDirectory, "logs.txt"), orgHashes);
                                File.AppendAllLines(Path.Combine(AppContext.BaseDirectory, "logs.txt"), ["+++++++++++"]);
                                File.AppendAllLines(Path.Combine(AppContext.BaseDirectory, "logs.txt"), modHashes);
                            }
                        }
                        existing = true;
                        return;
                    }
                });
                if (!existing)
                {
                    orgData.Add(modRow);
                }
            });
        }

        static int ImportHelper(UAsset originalAsset, UAsset modifiedAsset, Import modifiedImport)
        {
            int existing = GetImportIndex(originalAsset, modifiedImport);
            if (existing == -1)
            {
                int newImport = 0;
                if (modifiedImport.OuterIndex.ToString() != "0")
                    newImport = ImportHelper(originalAsset, modifiedAsset, modifiedImport.OuterIndex.ToImport(modifiedAsset));
                modifiedImport.OuterIndex = FPackageIndex.FromRawIndex(newImport);
                originalAsset.Imports.Add(new Import(new FName(originalAsset, modifiedImport.ClassPackage.Value.Value), new FName(originalAsset, modifiedImport.ClassName.Value.Value), FPackageIndex.FromRawIndex(newImport), new FName(originalAsset, modifiedImport.ObjectName.Value.Value), false));
                return -originalAsset.Imports.Count;
            }
            return -existing - 1;
        }

        static void Main()
        {
            try
            {
                //Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                Console.WriteLine("END.");
                Console.ReadKey();
            }
        }

        public static void Run()
        {
            Console.WriteLine("START.");

            string scriptPath = AppContext.BaseDirectory;
            Usmap usmap = new(Path.Combine(scriptPath, "mappings.usmap"));
            Console.WriteLine("Loaded mappings file.");

            string configPath = Path.Combine(scriptPath, "scriptConfig.txt");
            string gameDirectoryName = "Project Silverfish";
            string workingDirectoryName = "SilverFish";
            if (File.Exists(configPath))
            {
                string[] configLines = File.ReadAllLines(configPath);
                gameDirectoryName = configLines[0];
                workingDirectoryName = configLines[1];
                Console.WriteLine("Loaded config.");
            }

            DirectoryInfo? current = new(scriptPath);
            string? originalRoot = null;
            string sourceRoot = Path.Combine(scriptPath, "Mods");

            string modOrderPath = Path.Combine(scriptPath, "scriptModOrder.txt");
            List<string> modOrder = [];
            foreach (var item in File.ReadAllLines(modOrderPath))
            {
                string[] split = item.Split(" = ");
                if (Directory.Exists(Path.Combine(sourceRoot, item)) && split[1].Contains('1') || split[1].Contains("True"))
                {
                    modOrder.Add(split[0]);
                }
            }
            string replaceListPath = Path.Combine(scriptPath, "scriptReplaceList.txt");
            string[] replaceList = [];
            File.WriteAllLines(Path.Combine(AppContext.BaseDirectory, "logs.txt"), ["start"]);
            if (File.Exists(replaceListPath))
            {
                replaceList = File.ReadAllLines(replaceListPath);
                Console.WriteLine("Loaded replace list.");
            }

            while (current != null)
            {
                if (current.Name.Equals(gameDirectoryName))//Project Silverfish
                {
                    originalRoot = current.FullName;
                    break;
                }
                current = current.Parent;
            }

            if (originalRoot == null)
            {
                Console.WriteLine($"{gameDirectoryName} directory not found.");
                return;
            }
            string targetRoot = Path.Combine(scriptPath, "Generated");

            string[] finalHashStrings = [];

            foreach (string modRoot in modOrder)
            {
                Console.WriteLine($"Processing mod: {modRoot}");
                string modReplaceListPath = Path.Combine(modRoot, "scriptReplaceList.txt");
                string[] modReplaceList = [];
                if (File.Exists(modReplaceListPath))
                {
                    modReplaceList = File.ReadAllLines(modReplaceListPath);
                    Console.WriteLine($"Loaded mod {modRoot} replace list.");
                }
                foreach (string sourceFile in Directory.GetFiles(modRoot, "*", SearchOption.AllDirectories))
                {
                    if (sourceFile.Contains("scripthash") || sourceFile.Contains("scriptfinalhash"))
                    {
                        Console.WriteLine($"Skipping file {sourceFile} as it is a script hash file.");
                        continue;
                    }
                    string relativePath = Path.GetRelativePath(modRoot, sourceFile);
                    if ((!relativePath.StartsWith("Content") && !relativePath.StartsWith(workingDirectoryName)))
                    {
                        Console.WriteLine($"Skipping file {relativePath} as it does not have the correct file path.");
                        continue;
                    }
                    if (!relativePath.StartsWith(workingDirectoryName))
                        originalRoot = Path.Combine(originalRoot, workingDirectoryName);
                    string originalFile = Path.Combine(originalRoot, relativePath.Replace(".toReplace", ""));
                    string targetFile = Path.Combine(targetRoot, relativePath.Replace(".toReplace", ""));
                    string? targetDirectory = Path.GetDirectoryName(targetFile);
                    if (targetDirectory != null)
                    {
                        Directory.CreateDirectory(targetDirectory);
                    }
                    if ((!File.Exists(targetFile) || relativePath.Contains(".toReplace") || modReplaceList.Contains("ReplaceEverything") || modReplaceList.Contains(Path.GetFileName(relativePath))) && File.Exists(originalFile))
                        File.Copy(originalFile, targetFile, true);
                    else if ((!File.Exists(targetFile) || relativePath.Contains(".toReplace") || modReplaceList.Contains("ReplaceEverything") || modReplaceList.Contains(Path.GetFileName(relativePath))) && !File.Exists(originalFile))
                        File.Copy(sourceFile, targetFile, true);
                }

                foreach (string sourceFile in Directory.GetFiles(modRoot, "*.uasset", SearchOption.AllDirectories))
                {
                    string relativePath = Path.GetRelativePath(modRoot, sourceFile);

                    string targetFile = Path.Combine(targetRoot, relativePath.Replace(".uasset", ""));

                    string modifiedFile = sourceFile.Replace(".uasset", "");

                    Console.WriteLine($"Copying: {modifiedFile} to {targetFile}");

                    UAsset originalAsset = new(targetFile + ".uasset", EngineVersion.VER_UE5_4, usmap);
                    UAsset modifiedAsset = new(modifiedFile + ".uasset", EngineVersion.VER_UE5_4, usmap);

                    modifiedAsset.Imports.ForEach(i =>
                    {
                        ImportHelper(originalAsset, modifiedAsset, i);
                    });
                    Dictionary<string, Export> exportStrings = [];
                    originalAsset.Exports.ForEach(orgExport =>
                    {
                        exportStrings[orgExport.ObjectName.Value.Value + orgExport.ObjectName.Number.ToString()] = orgExport;
                    });
                    modifiedAsset.Exports.ForEach(modExport =>
                    {
                        if (!exportStrings.ContainsKey(modExport.ObjectName.Value.Value + modExport.ObjectName.Number.ToString()))
                        {
                            Export newExport = (Export)modExport.Clone();
                            originalAsset.Exports.Add(newExport);
                            exportStrings[modExport.ObjectName.Value.Value + modExport.ObjectName.Number.ToString()] = newExport;
                            Console.WriteLine($"Export {modExport.ObjectName.Value.Value} added.");
                        }
                        else
                            Console.WriteLine($"Export {modExport.ObjectName.Value.Value} already exists in original asset, check.");
                    });
                    modifiedAsset.Exports.ForEach(modExport =>
                    {
                        Console.WriteLine($"Export {modExport.ObjectName.Value.Value} looked at, type: {modExport.GetType()}.");
                        var orgExport = exportStrings[modExport.ObjectName.Value.Value + modExport.ObjectName.Number.ToString()];
                        if (orgExport == null)
                        {
                            Console.WriteLine($"Export {modExport.ObjectName.Value.Value} not found in original asset, aborting merge.");
                            return;
                        }
                        if (modExport is DataTableExport dtModExport && orgExport is DataTableExport dtOrgExport)
                        {
                            HandlePropertyData(originalAsset, dtOrgExport.Table.Data, modifiedAsset, dtModExport.Table.Data);
                        }
                        else if (modExport is NormalExport nModExport && orgExport is NormalExport nOrgExport)
                        {
                            HandlePropertyData(originalAsset, nOrgExport.Data, modifiedAsset, nModExport.Data);
                        }
                        else if (modExport is RawExport)
                        {
                            Console.WriteLine($"Export {modExport.ObjectName.Value.Value} {originalAsset.Imports[GetImportIndex(originalAsset, modifiedAsset.Imports[-modExport.TemplateIndex.Index - 1])].ObjectName.Value.Value} is raw and thus cannot be correctly automatically merged.");
                        }
                    });

                    originalAsset.Write(targetFile + ".uasset");
                }
            }
        }
    }
}
