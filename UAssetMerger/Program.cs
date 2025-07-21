
using System.Security.Cryptography;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;
using UAssetAPI.UnrealTypes;
using UAssetAPI.Unversioned;

namespace UAssetMerger;

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

    static string GetAssetHash(string assetPath)
    {
        return Convert.ToHexString(MD5.HashData(File.ReadAllBytes(assetPath)));
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
                                        keyHashes.Add(GetImportString(intValue, orgAsset), orgItem.Key);
                                        keyHashes.Add(intValue.ToString(), orgItem.Key);
                                    }
                                    else if (intValue > 0)
                                    {
                                        keyHashes.Add(GetExportString(intValue, orgAsset), orgItem.Key);
                                        keyHashes.Add(intValue.ToString(), orgItem.Key);
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
                            List<string> orgHashes = [];
                            foreach (var orgItem in orgArray.Value)
                            {
                                orgHashes.Add(GetPropertyHash(orgItem, orgAsset));
                            }
                            foreach (var modItem in modArray.Value)
                            {
                                string modHash = GetPropertyHash(modItem, modAsset);
                                if (orgHashes.Contains(modHash))
                                    continue;
                                orgArray.Value = [.. orgArray.Value, (PropertyData)modItem.Clone()];
                                HandlePropertyData(orgAsset, [orgArray.Value.Last()], modAsset, [modItem], true);
                            }
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
            Console.WriteLine($"Import {modifiedImport.ObjectName.Value.Value} {modifiedImport.OuterIndex} {newImport} {-originalAsset.Imports.Count-1} added.");
            modifiedImport.OuterIndex = FPackageIndex.FromRawIndex(newImport);
            originalAsset.Imports.Add(modifiedImport);
            return -originalAsset.Imports.Count;
        }
        return -existing - 1;
    }

    static void Main()
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

        string replaceListPath = Path.Combine(scriptPath, "scriptReplaceList.txt");
        string[] replaceList = [];
        if (File.Exists(replaceListPath))
        {
            replaceList = File.ReadAllLines(replaceListPath);
            Console.WriteLine("Loaded replace list.");
        }

        DirectoryInfo? current = new(scriptPath);
        string? targetRoot = null;
        string sourceRoot = Path.Combine(scriptPath, "Mods");

        string modOrderPath = Path.Combine(scriptPath, "scriptModOrder.txt");
        string[] modOrder = Directory.GetDirectories(sourceRoot);
        if (File.Exists(modOrderPath))
        {
            string[] mods = File.ReadAllLines(modOrderPath);
            foreach (string mod in modOrder)
            {
                if (Directory.Exists(Path.Combine(sourceRoot, mod)))
                {
                    modOrder = [.. modOrder, Path.Combine(sourceRoot, mod)];
                }
            }
            Console.WriteLine("Loaded mod order.");
        }

        while (current != null)
        {
            if (current.Name.Equals(gameDirectoryName))//Project Silverfish
            {
                targetRoot = current.FullName;
                break;
            }
            current = current.Parent;
        }

        if (targetRoot == null)
        {
            Console.WriteLine($"{gameDirectoryName} directory not found.");
            return;
        }

        foreach (string modRoot in modOrder)
        {
            Console.WriteLine($"Processing mod: {modRoot}");
            string modReplaceListPath = Path.Combine(modRoot, "scriptReplaceList.txt");
            if (File.Exists(modReplaceListPath))
            {
                replaceList = [.. replaceList, .. File.ReadAllLines(modReplaceListPath)];
                Console.WriteLine($"Loaded mod {modRoot} replace list.");
            }
            string[] replacedFiles = [];
            foreach (string sourceFile in Directory.GetFiles(modRoot, "*", SearchOption.AllDirectories))
            {
                if (sourceFile.Contains("scripthash.txt") || sourceFile.Contains("scriptfinalhash.txt"))
                {
                    Console.WriteLine($"Skipping file {sourceFile} as it is a script hash file.");
                    continue;
                }
                string relativePath = Path.GetRelativePath(modRoot, sourceFile);

                if (relativePath.StartsWith("Content") || relativePath.StartsWith("Binaries"))
                {
                    relativePath = Path.Combine(workingDirectoryName, relativePath);
                }
                else if (!relativePath.StartsWith("Content") && !relativePath.StartsWith("Binaries") && !relativePath.StartsWith(workingDirectoryName))
                {
                    Console.WriteLine($"Skipping file {relativePath} as it does not have the correct file path.");
                    continue;
                }

                string targetFile = Path.Combine(targetRoot, relativePath.Replace(".toReplace", ""));

                string? directoryPath = Path.GetDirectoryName(targetFile);
                if (directoryPath != null)
                {
                    Directory.CreateDirectory(directoryPath);
                }
                if (File.Exists(targetFile) && (replaceList.Contains(relativePath) || relativePath.Contains(".toReplace") || replaceList.Contains("replaceEverything")))
                {
                    replacedFiles = [.. replacedFiles, sourceFile];
                    File.Copy(sourceFile, targetFile, overwrite: true);
                }
                else if (!File.Exists(targetFile))
                {
                    replacedFiles = [.. replacedFiles, sourceFile];
                    File.Copy(sourceFile, targetFile);
                    if (!replaceList.Contains(relativePath))
                        File.AppendAllLines(replaceListPath, [relativePath]);
                }
            }

            foreach (string sourceFile in Directory.GetFiles(modRoot, "*.uasset", SearchOption.AllDirectories))
            {
                if (replacedFiles.Contains(sourceFile))
                {
                    Console.WriteLine($"Skipping {sourceFile} as it was already replaced.");
                    continue;
                }
                string relativePath = Path.GetRelativePath(modRoot, sourceFile);

                string originalFile = Path.Combine(targetRoot, relativePath).Replace(".uasset", "");

                string modifiedFile = sourceFile.Replace(".uasset", "");

                Console.WriteLine($"Copying: {modifiedFile} to {originalFile}");
                string originalAssetPath = originalFile; 
                string modifiedAssetPath = modifiedFile;

                UAsset originalAsset = new(originalAssetPath + ".uasset", EngineVersion.VER_UE5_4, usmap);
                UAsset modifiedAsset = new(modifiedAssetPath + ".uasset", EngineVersion.VER_UE5_4, usmap);
                string originalAssetHash = GetAssetHash(originalAssetPath + ".uasset");
                string modifiedAssetHash = GetAssetHash(modifiedAssetPath + ".uasset");
                string originalBackupHash = "";
                string modifiedBackupHash = "";
                string finalAssetHash = "";

                if (File.Exists(originalAssetPath + "_scriptfinalhash.txt"))
                {
                    finalAssetHash = File.ReadAllLines(originalAssetPath + "_scriptfinalhash.txt")[0];
                }
                if (!File.Exists(modifiedAssetPath + "_scripthash.txt"))
                {
                    modifiedBackupHash = GetAssetHash(modifiedAssetPath + ".uasset");
                    File.WriteAllLines(modifiedAssetPath + "_scripthash.txt", [modifiedBackupHash]);
                }
                else
                {
                    modifiedBackupHash = File.ReadAllLines(modifiedAssetPath + "_scripthash.txt")[0];
                }
                if (!File.Exists(originalAssetPath + "_scriptbak.uasset"))
                {
                    originalAsset.Write(originalAssetPath + "_scriptbak.uasset");
                    originalBackupHash = originalAssetHash;
                }
                else
                {
                    originalBackupHash = GetAssetHash(originalAssetPath + "_scriptbak.uasset");
                    if (originalBackupHash != originalAssetHash && originalAssetHash != finalAssetHash)
                    {
                        originalAsset.Write(originalAssetPath + "_scriptbak.uasset");
                        originalBackupHash = GetAssetHash(originalAssetPath + "_scriptbak.uasset");
                    }
                }
                if (originalAssetHash == modifiedAssetHash)
                {
                    Console.WriteLine("No changes detected, skipping.");
                    continue;
                }
                if (finalAssetHash == originalAssetHash && modifiedAssetHash == modifiedBackupHash)
                {
                    Console.WriteLine("No changes detected, skipping.");
                    continue;
                }
                modifiedAsset.Imports.ForEach(i =>
                {
                    ImportHelper(originalAsset, modifiedAsset, i);
                });
                modifiedAsset.Exports.ForEach(modExport =>
                {
                    var orgExport = originalAsset[modExport.ObjectName.Value.Value];
                    if (orgExport == null)
                    {
                        originalAsset.Exports.Add((Export)modExport.Clone());
                        orgExport = originalAsset[modExport.ObjectName.Value.Value];
                        Console.WriteLine($"Export {modExport.ObjectName.Value.Value} added.");
                    }
                });
                modifiedAsset.Exports.ForEach(modExport =>
                {
                    var orgExport = originalAsset[modExport.ObjectName.Value.Value];
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
                        Console.WriteLine($"Export {modExport.ObjectName.Value.Value} is raw and thus cannot be correctly automatically merged.");
                    }
                });

                originalAsset.Write(originalAssetPath + ".uasset");
                finalAssetHash = GetAssetHash(originalAssetPath + ".uasset");
                File.WriteAllLines(originalAssetPath + "_scriptfinalhash.txt", [finalAssetHash]);
                modifiedBackupHash = GetAssetHash(modifiedAssetPath + ".uasset");
                File.WriteAllLines(modifiedAssetPath + "_scripthash.txt", [modifiedBackupHash]);
            }
        }
    }
}