using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Yarn.Unity;
using Yarn.Compiler;
using System;
using UnityEditor;

namespace AceV
{
    public class YarnRuntimeStoryImporter
    {
        public static void RunAllStoriesImport(DialogueRunner dialogueRunner)
        {
            // Get all Yarn files and create a CompilationJob for the import
            string[] files = Directory.GetFiles(Path.Combine(Application.dataPath, "Assets"), "*.yarn", SearchOption.AllDirectories);
            List<string> yarnFiles = new List<string>(files);
            CompilationJob job = CompilationJob.CreateFromFiles(yarnFiles);
            job.Library = Actions.GetLibrary();

            // Compile the yarn files! Report any errors
            CompilationResult cResult;
            try
            {
                cResult = Compiler.Compile(job);
            }
            catch (System.Exception e)
            {
                var errorMessage = $"Encountered an unhandled exception during compilation: {e.Message}";
                Debug.LogError(errorMessage);
                return;
            }
            var errors = cResult.Diagnostics.Where(d => d.Severity == Diagnostic.DiagnosticSeverity.Error);
            if (errors.Count() > 0)
            {
                var errorGroups = errors.GroupBy(e => e.FileName);
                foreach (var errorGroup in errorGroups)
                {
                    if (errorGroup.Key == null)
                    {
                        // ok so we have no file for some reason
                        // so these are errors currently not tied to a file
                        // so we instead need to just log the errors and move on
                        foreach (var error in errorGroup)
                        {
                            Debug.LogError($"Error compiling project: {error.Message}");
                        }
                        continue;
                    }

                    foreach (var error in errorGroup)
                    {
                        Debug.LogError($"Error compiling project ({error.FileName}) at line {error.Range.Start.Line+1}: {error.Message})");
                    }
                }
                return;
            }
            if (cResult.Program == null)
            {
                Debug.LogError("Internal error: Failed to compile: resulting program was null, but compiler did not report errors.");
                return;
            }

            // Gather all localization (TEXT) in the yarn project
            GatherAllLocalizationAssets(dialogueRunner.yarnProject, cResult);
            // dialogueRunner.yarnProject.localizationType = LocalizationType.YarnInternal;

            // Write the compiled program bites to the Yarn project
            byte[] compiledBytes = null;
            using (var memoryStream = new MemoryStream())
            using (var outputStream = new Google.Protobuf.CodedOutputStream(memoryStream))
            {
                // Serialize the compiled program to memory
                cResult.Program.WriteTo(outputStream);
                outputStream.Flush();

                compiledBytes = memoryStream.ToArray();
            }
            dialogueRunner.yarnProject.compiledYarnProgram = compiledBytes;

            // Something we need to do manually - store the serialized-cached compiled program into the project via reflection
            FieldInfo f = typeof(YarnProject).GetField("cachedProgram", BindingFlags.NonPublic | BindingFlags.Instance);
            f.SetValue(dialogueRunner.yarnProject, null);

            // Stupid but necessary
            dialogueRunner.SetProject(dialogueRunner.yarnProject);
        }


        private static void GatherAllLocalizationAssets(YarnProject yarnProject, CompilationResult compResult)
        {
            // YarnProjects can contain all sorts of localization information that are pre-configured
            // in the Unity editor. For our sake, we'll force a default localization (BaseLanguage)
            // of English ("en-US"). I don't expect to support custom story localization in the future.
            yarnProject.localizations.Clear();

            string BaseLanguage = "en-US";

            Localization newLocalization = ScriptableObject.CreateInstance<Localization>();
            newLocalization.LocaleCode = BaseLanguage;
            newLocalization.name = BaseLanguage;

            foreach (var entry in compResult.StringTable)
            {
                newLocalization.AddLocalizedString(entry.Key, entry.Value.text);
            }

            yarnProject.localizations.Add(newLocalization);

            // Since this is our default language, set it as such
            yarnProject.baseLocalization = newLocalization;
            // And since this is the default language, also populate the line metadata.
            // (Need to perform reflection to access this one, shit's internal)
            ConstructorInfo ci = typeof(LineMetadata).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0];
            LineMetadata lm = (LineMetadata) ci.Invoke(new[] { LineMetadataTableEntriesFromCompilationResult(compResult) });

            yarnProject.lineMetadata = lm;

            /*
            // Will we need to create a default localization? This variable
            // will be set to false if any of the languages we've
            // configured in languagesToSourceAssets is the default
            // language.
            var shouldAddDefaultLocalization = true;

            foreach (var localisationInfo in importData.localizations)
            {
                // Don't create a localization if the language ID was not
                // provided
                if (string.IsNullOrEmpty(localisationInfo.languageID))
                {
                    Debug.LogWarning($"Not creating a localization for {projectAsset.name} because the language ID wasn't provided.");
                    continue;
                }

                IEnumerable<StringTableEntry> stringTable;

                // Where do we get our strings from? If it's the default
                // language, we'll pull it from the scripts. If it's from
                // any other source, we'll pull it from the CSVs.
                if (localisationInfo.languageID == importData.baseLanguageName)
                {
                    // No strings file needed - we'll use the program-supplied string table.
                    stringTable = GenerateStringsTable();

                    // We don't need to add a default localization.
                    shouldAddDefaultLocalization = false;
                }
                else
                {
                    // No strings file provided
                    if (localisationInfo.stringsFile == null) {
                        Debug.LogWarning($"Not creating a localisation for {localisationInfo.languageID} in the Yarn project {projectAsset.name} because a strings file was not specified, and {localisationInfo.languageID} is not the project's base language");
                        continue;
                    }
                    try
                    {
                        stringTable = StringTableEntry.ParseFromCSV(localisationInfo.stringsFile.text);
                    }
                    catch (System.ArgumentException e)
                    {
                        Debug.LogWarning($"Not creating a localization for {localisationInfo.languageID} in the Yarn Project {projectAsset.name} because an error was encountered during text parsing: {e}");
                        continue;
                    }
                } 

                var newLocalization = ScriptableObject.CreateInstance<Localization>();
                newLocalization.LocaleCode = localisationInfo.languageID;

                // Add these new lines to the localisation's asset
                foreach (var entry in stringTable) {
                    newLocalization.AddLocalisedStringToAsset(entry.ID, entry.Text);
                }

                projectAsset.localizations.Add(newLocalization);
                newLocalization.name = localisationInfo.languageID;

                if (localisationInfo.assetsFolder != null) {
                    newLocalization.ContainsLocalizedAssets = true;

#if USE_ADDRESSABLES
                    const bool addressablesAvailable = true;
#else
                    const bool addressablesAvailable = false;
#endif

                    if (addressablesAvailable && useAddressableAssets)
                    {
                        // We only need to flag that the assets
                        // required by this localization are accessed
                        // via the Addressables system. (Call
                        // YarnProjectUtility.UpdateAssetAddresses to
                        // ensure that the appropriate assets have the
                        // appropriate addresses.)
                        newLocalization.UsesAddressableAssets = true;
                    }
                    else
                    {
                        // We need to find the assets used by this
                        // localization now, and assign them to the
                        // Localization object.
#if YARNSPINNER_DEBUG
                        // This can take some time, so we'll measure
                        // how long it takes.
                        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
#endif

                        // Get the line IDs.
                        IEnumerable<string> lineIDs = stringTable.Select(s => s.ID);

                        // Map each line ID to its asset path.
                        var stringIDsToAssetPaths = YarnProjectUtility.FindAssetPathsForLineIDs(lineIDs, AssetDatabase.GetAssetPath(localisationInfo.assetsFolder));

                        // Load the asset, so we can assign the reference.
                        var assetPaths = stringIDsToAssetPaths
                            .Select(a => new KeyValuePair<string, Object>(a.Key, AssetDatabase.LoadAssetAtPath<Object>(a.Value)));

                        newLocalization.AddLocalizedObjects(assetPaths);

#if YARNSPINNER_DEBUG
                        stopwatch.Stop();
                        Debug.Log($"Imported {stringIDsToAssetPaths.Count()} assets for {project.name} \"{pair.languageID}\" in {stopwatch.ElapsedMilliseconds}ms");
#endif
                    }
                
                }

                ctx.AddObjectToAsset("localization-" + localisationInfo.languageID, newLocalization);

                if (localisationInfo.languageID == importData.baseLanguageName)
                {
                    // If this is our default language, set it as such
                    projectAsset.baseLocalization = newLocalization;

                    // Since this is the default language, also populate the line metadata.
                    projectAsset.lineMetadata = new LineMetadata(LineMetadataTableEntriesFromCompilationResult(compilationResult));
                }
                else if (localisationInfo.stringsFile != null)
                {
                    // This localization depends upon a source asset. Make
                    // this asset get re-imported if this source asset was
                    // modified
                    ctx.DependsOnSourceAsset(AssetDatabase.GetAssetPath(localisationInfo.stringsFile));
                }
            }

            if (shouldAddDefaultLocalization)
            {
                // We didn't add a localization for the default language.
                // Create one for it now.
                var stringTableEntries = GetStringTableEntries(compilationResult);

                var developmentLocalization = ScriptableObject.CreateInstance<Localization>();
                developmentLocalization.name = $"Default ({importData.baseLanguageName})";
                developmentLocalization.LocaleCode = importData.baseLanguageName;


                // Add these new lines to the development localisation's asset
                foreach (var entry in stringTableEntries)
                {
                    developmentLocalization.AddLocalisedStringToAsset(entry.ID, entry.Text);
                }

                projectAsset.baseLocalization = developmentLocalization;
                projectAsset.localizations.Add(projectAsset.baseLocalization);
                ctx.AddObjectToAsset("default-language", developmentLocalization);

                // Since this is the default language, also populate the line metadata.
                projectAsset.lineMetadata = new LineMetadata(LineMetadataTableEntriesFromCompilationResult(compilationResult));
            }
            */
        }

        private static IEnumerable LineMetadataTableEntriesFromCompilationResult(CompilationResult result)
        {
            Type t = typeof(DialogueRunner).Assembly.GetType("Yarn.Unity.LineMetadataTableEntry");
            // foreach (ConstructorInfo c in t.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            // {
            //     Debug.Log(c);
            // }
            // ConstructorInfo ci = t.GetConstructors(BindingFlags.Public | BindingFlags.Instance)[0];
            IList results = (IList) Activator.CreateInstance(typeof(List<>).MakeGenericType(t));
            foreach (var st in result.StringTable)
            {
                string[] metadata = RemoveLineIDFromMetadata(st.Value.metadata).ToArray();
                if (metadata.Length > 0)
                {
                    // dynamic lineMetadataTableEntry = Convert.ChangeType(Activator.CreateInstance(t), t);
                    // object lineMetadataTableEntry = ci.Invoke(null);
                    object lineMetadataTableEntry = Activator.CreateInstance(t);
                    t.GetField("ID").SetValue(lineMetadataTableEntry, st.Key);
                    t.GetField("File").SetValue(lineMetadataTableEntry, st.Value.fileName);
                    t.GetField("Node").SetValue(lineMetadataTableEntry, st.Value.nodeName);
                    t.GetField("LineNumber").SetValue(lineMetadataTableEntry, st.Value.lineNumber.ToString());
                    t.GetField("Metadata").SetValue(lineMetadataTableEntry, metadata);
                    results.Add(Convert.ChangeType(lineMetadataTableEntry, t));
                }
            }

            return results;
            // return result.StringTable.Select(x => new LineMetadataTableEntry
            // {
            //     ID = x.Key,
            //     File = x.Value.fileName,
            //     Node = x.Value.nodeName,
            //     LineNumber = x.Value.lineNumber.ToString(),
            //     Metadata = RemoveLineIDFromMetadata(x.Value.metadata).ToArray(),
            // }).Where(x => x.Metadata.Length > 0);
        }

        private static IEnumerable<string> RemoveLineIDFromMetadata(string[] metadata)
        {
            return metadata.Where(x => !x.StartsWith("line:"));
        }
    }
}