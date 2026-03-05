#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Nex.Starter
{
    // Provide setup tips for MDK usages.
    internal class NexStarter : EditorWindow
    {
        // Presenting NexStarter.
        [MenuItem("Nex/Nex Starter")]
        private static void ShowWindow()
        {
            var window = GetWindow<NexStarter>();
            window.titleContent = new GUIContent("Nex Starter");
            window.Show();
        }

        private class StarterState : ScriptableObject
        {
            public string accessToken = "";
        }

        private StarterState activeState = null!;
        private SerializedObject activeStateSerializedObject = null!;

        private void OnEnable()
        {
            activeState = CreateInstance<StarterState>();
            activeStateSerializedObject = new SerializedObject(activeState);
        }

        private void OnDisable()
        {
            activeStateSerializedObject = null!;
            DestroyImmediate(activeState);
            activeState = null!;
        }

        private void CreateGUI()
        {
            var root = rootVisualElement!;
            root.Add(new Label("Welcome to Nex Starter")
            {
                style = { fontSize = 20 }
            });
            root.Add(new Label("You can set up your Unity for using the Playground SDK here."));
            var stepIndex = 0;
            root.Add(ConfigureUpmConfigStep(ref stepIndex));
            root.Add(CreateScopedRegistryStep(ref stepIndex));
            root.Add(CreateAddMdkStep(ref stepIndex));
            root.Add(CreateMdkVerificationStep(ref stepIndex));
            root.Add(CreateConfigureProjectSettingsStep(ref stepIndex));
            root.Add(CreateAddHandPoseStep(ref stepIndex));
            root.Add(CreateAddFaceLandmarkStep(ref stepIndex));
        }

        #region Package Manager

        private class PackageManager
        {
            private static PackageManager? instance;
            public static PackageManager Instance => instance ??= new PackageManager();

            private readonly Type clientType = typeof(Client);
            private readonly MethodInfo getRegistriesMethod = null!;
            private readonly MethodInfo addScopedRegistryMethod = null!;
            private readonly MethodInfo addScopedRegistryMethod5 = null!; // The function definition in unity 6000.2 has 5 params

            private abstract class Task
            {
                protected readonly PackageManager manager;

                protected Task(PackageManager manager)
                {
                    this.manager = manager;
                }

                public abstract Request? Execute(Request? prevRequest);
            }

            private class GetRegistriesTask : Task
            {
                public GetRegistriesTask(PackageManager manager) : base(manager)
                {
                }

                public override Request? Execute(Request? prevRequest)
                {
                    switch (prevRequest)
                    {
                        case null:
                        {
                            return manager.getRegistriesMethod.Invoke(manager.clientType, null) as Request;
                        }
                        case Request<RegistryInfo[]> typedRequest:
                        {
                            var registries = typedRequest.Result;
                            foreach (var registry in registries)
                            {
                                Debug.Log($"{registry.name} {registry.url}");
                            }

                            break;
                        }
                        default:
                            return null;
                    }
                    return null;
                }
            }

            public readonly struct RegistrySpec
            {
                public readonly string registryName;
                public readonly string url;
                public readonly string[] scopes;

                public RegistrySpec(string registryName, string url, string[] scopes)
                {
                    this.registryName = registryName;
                    this.url = url;
                    this.scopes = scopes;
                }
            }

            private class AddScopedRegistriesTask : Task
            {
                private readonly RegistrySpec[] specs;
                private RegistryInfo[]? existingRegistries;
                private int index;

                public AddScopedRegistriesTask(PackageManager manager, params RegistrySpec[] specs) : base(manager)
                {
                    this.specs = specs;
                }

                private Request? TryAddRegistry()
                {
                    // Find the first entry that is not already in existing Registries.
                    while (index < specs.Length)
                    {
                        var spec = specs[index++];
                        var existed = false;
                        if (existingRegistries != null)
                        {
                            var url = spec.url;
                            foreach (var registry in existingRegistries)
                            {
                                // The default registry has a name of "". We should ignore it.
                                if (registry.name == "" || registry.url != url) continue;
                                existed = true;
                                break;
                            }
                        }

                        if (existed) {
                            Debug.Log($"Registry already registered: {spec.url}");
                            continue;  // Search for the next one.
                        }

                        if (manager.addScopedRegistryMethod != null)
                        {
                            // Let's add the next one.
                            var args = new object[] { spec.registryName, spec.url, spec.scopes };
                            return manager.addScopedRegistryMethod.Invoke(manager.clientType, args) as Request;
                        }
                        else if (manager.addScopedRegistryMethod5 != null)
                        {
                            long operationId = 0;
                            var args = new object[] { operationId, spec.registryName, spec.url, spec.scopes, false };
                            return manager.addScopedRegistryMethod5.Invoke(manager.clientType, args) as Request;
                        }
                    }

                    Client.Resolve();

                    return null;
                }

                public override Request? Execute(Request? prevRequest)
                {
                    // First we get the existing repositories.
                    switch (prevRequest)
                    {
                        case null:
                        {
                            return manager.getRegistriesMethod.Invoke(manager.clientType, null) as Request;
                        }
                        case Request<RegistryInfo[]> registriesRequest:
                        {
                            existingRegistries = registriesRequest.Result;
                            return TryAddRegistry();
                        }
                        case Request<RegistryInfo> addRequest:
                        {
                            var registered = addRequest.Result!;
                            Debug.Log($"Registered {registered.name}");
                            return TryAddRegistry();
                        }
                    }

                    return null;
                }
            }

            private class AddPackagesTask : Task
            {
                private readonly string[] packageNames;

                public AddPackagesTask(PackageManager manager, params string[] packageNames) : base(manager)
                {
                    this.packageNames = packageNames;
                }

                public override Request? Execute(Request? prevRequest)
                {
                    switch (prevRequest)
                    {
                        case null:
                        {
                            return Client.AddAndRemove(packageNames, Array.Empty<string>());
                        }
                        case AddAndRemoveRequest addAndRemoveRequest:
                        {
                            var result = addAndRemoveRequest.Result!;
                            Debug.Log($"Added {result.Count()} packages.");
                            Client.Resolve();
                            return null;
                        }
                    }

                    return null;
                }
            }

            private PackageManager()
            {
                var methods = clientType.GetMethods(BindingFlags.Static | BindingFlags.NonPublic);
                foreach (var method in methods)
                {
                    switch (method.Name)
                    {
                        case "AddScopedRegistry":
                            if (method.GetParameters().Length == 3)
                            {
                                addScopedRegistryMethod = method;
                            }
                            else if (method.GetParameters().Length == 5)
                            {
                                addScopedRegistryMethod5 = method;
                            }
                            break;
                        case "GetRegistries":
                            if (method.GetParameters().Length == 0)
                            {
                                getRegistriesMethod = method;
                            }
                            break;
                    }
                }
            }

            // ReSharper disable once UnusedMember.Local
            public void GetRegistries()
            {
                AddTask(new GetRegistriesTask(this));
            }

            public void AddScopedRegistries(params RegistrySpec[] specs)
            {
                AddTask(new AddScopedRegistriesTask(this, specs));
            }

            public void AddPackages(params string[] packageNames)
            {
                AddTask(new AddPackagesTask(this, packageNames));
            }

            private readonly Queue<Task> tasks = new();
            private Request? activeRequest;

            private void AddTask(params Task[] newTasks)
            {
                if (newTasks.Length == 0) return;

                foreach (var task in newTasks)
                {
                    tasks.Enqueue(task);
                }

                // Check if there is an activeRequest going on.
                // If there is one, we can just wait for the executor to finish our new task.
                if (activeRequest != null) return;

                // There is no active request.
                EditorApplication.update += Update;
                Step();
            }

            private void Step()
            {
                // Move one step.
                while (activeRequest == null && tasks.TryPeek(out var firstTask))
                {
                    var nextRequest = firstTask.Execute(activeRequest);
                    if (nextRequest == null)
                    {
                        // The first task is done.
                        tasks.Dequeue();
                    }
                    else
                    {
                        activeRequest = nextRequest;
                        // Something is going on now.
                        return;
                    }
                }
                // Everything is done. No remaining tasks.
                activeRequest = null;
                EditorApplication.update -= Update;
            }

            private void Update()
            {
                // Called periodically to clear remaining tasks.
                if (activeRequest == null)  // This shouldn't normally happen.
                {
                    EditorApplication.update -= Update;
                    return;
                }

                // Wait for activeRequest to be done.
                if (!activeRequest.IsCompleted) return;  // Keep waiting.

                // Here, activeRequest is completed.
                if (!tasks.TryPeek(out var firstTask))
                {
                    // Again, shouldn't happen.
                    EditorApplication.update -= Update;
                    return;
                }

                activeRequest = firstTask.Execute(activeRequest);
                if (activeRequest != null) return;
                // So the first task is done.
                tasks.Dequeue();
                Step();
            }
        }

        #endregion

        #region UPMConfig

        private VisualElement ConfigureUpmConfigStep(ref int stepIndex)
        {
            var foldout = new Foldout
            {
                text = $"{++stepIndex}. Set up permission for Nex Packages.",
                value = false
            };
            var container = foldout.contentContainer!;
            container.Add(new Label("Make sure you have access right to Nex package server."));
            container.Add(new Label("(Only require to setup once per machine.)"));
            container.Add(new Label("Input the access token you received from our Nex representative."));
            var field = new TextField("Access Token");
            field.BindProperty(activeStateSerializedObject.FindProperty(nameof(StarterState.accessToken)));
            container.Add(field);
            var button = new Button(ConfigureUpmConfig)
            {
                text = "Configure .upmconfig.toml"
            };
            container.Add(button);
            return foldout;
        }

        private static class UpmConfigWriter
        {
            // The format of the upmconfig is
            // [npmAuth."https://packages.nex.inc"]
            // alwaysAuth = true
            // token = <TOKEN>
            // email = "invalid@email.com"
            private const string sectionHeader = "[npmAuth.\"https://packages.nex.inc\"]";
            private const string prefix = "alwaysAuth = true";
            private const string suffix = "email = \"invalid@email.com\"";

            public static void Merge(string accessToken)
            {
                var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var upmconfigPath = Path.Combine(homePath, ".upmconfig.toml");
                // Check if we can exit early because the file already contains what we need.
                var tokenLine = $"token = \"{accessToken}\"";
                do
                {
                    if (!File.Exists(upmconfigPath)) break;
                    var lines = File.ReadAllLines(upmconfigPath);
                    var numLines = lines.Length;
                    for (var i = 0; i < numLines - 3; ++i)
                    {
                        if (lines[i] != sectionHeader) continue;
                        // Check if the next few lines are what we are expecting.
                        // At this point, the section header is found.
                        // We expect the next three lines to be [prefix, tokenLine and suffix].
                        if (lines[i + 1] == prefix && lines[i + 2] == tokenLine && lines[i + 3] == suffix) return;
                        // If it is not a perfect match, we need to find the next segment and remove everything in between.
                        var j = i + 1;
                        while (j < numLines && !lines[j].StartsWith("[")) ++j;
                        // Now we want to replace [i, j) with prefix, tokenLine and suffix.
                        var newLines = new List<string>(numLines - (j - i) + 3);
                        newLines.AddRange(lines[..(i + 1)]);
                        newLines.Add(prefix);
                        newLines.Add(tokenLine);
                        newLines.Add(suffix);
                        newLines.AddRange(lines[j..]);
                        File.WriteAllLines(upmconfigPath, newLines);
                        return;
                    }
                } while (false);
                // This is where we append to the file.
                File.AppendAllLines(upmconfigPath, new[]
                {
                    sectionHeader,
                    prefix,
                    tokenLine,
                    suffix
                });
            }
        }

        private void ConfigureUpmConfig()
        {
            var accessToken = activeState.accessToken;
            UpmConfigWriter.Merge(accessToken);
        }

        #endregion

        #region ScopedRegistry

        private static VisualElement CreateScopedRegistryStep(ref int stepIndex)
        {
            var foldout = new Foldout()
            {
                text = $"{++stepIndex}. Configure Scoped Registry.",
                value = false
            };

            var container = foldout.contentContainer;
            container.Add(new Label("Add scoped registries so that you can import Nex packages through Unity Package Manager."));
            container.Add(new Label("(Only require to setup once per project.)"));
            var button = new Button(ConfigureScopedRegistry)
            {
                text = "Update Package Manager Settings."
            };
            container.Add(button);
            return foldout;
        }

        private static void ConfigureScopedRegistry()
        {
            var manager = PackageManager.Instance;
            manager.AddScopedRegistries(
                new PackageManager.RegistrySpec("Nex Packages", "https://packages.nex.inc",
                    new[]
                    {
                        "team.nex",
                        // The ones below are MDK dependencies.
                        "com.cysharp.unitask",
                        "com.dbrizov.naughtyattributes",
                        "com.textus-games.serialized-reference-ui",
                        "net.tnrd.serializableinterface",
                    }));
        }

        #endregion

        #region Add MDK

        private static VisualElement CreateAddMdkStep(ref int stepIndex)
        {
            var foldout = new Foldout
            {
                text = $"{++stepIndex}. Add/Update MDK through Unity Package Manager.",
                value = false
            };
            var container = foldout.contentContainer;
            container.Add(new Label("Add/Update team.nex.mdk.body and related packages through Unity Package Manager."));
            var button = new Button(AddMdk)
            {
                text = "Add/Update MDK to project."
            };
            container.Add(button);
            return foldout;
        }

        private static void AddMdk()
        {
            var manager = PackageManager.Instance;
            manager.AddPackages("team.nex.mdk.body", "team.nex.nex-opencv-for-unity", "team.nex.ml-models");
        }

        #endregion

        #region Verify MDK installation

        [Serializable]
        private class MdkVerifier
        {
            public string version = "";

            private Label resultLabel;

            public MdkVerifier(Label resultLabel)
            {
                this.resultLabel = resultLabel;
            }

            public void Run()
            {
                try
                {
                    var asset = AssetDatabase.LoadAssetAtPath<TextAsset>("Packages/team.nex.mdk/Resources/mdk-info.json");
                    if (asset == null)
                    {
                        resultLabel.text = "Cannot load mdk-info.json. MDK not installed.";
                        return;
                    }
                    EditorJsonUtility.FromJsonOverwrite(asset.text, this);
                    resultLabel.text = $"MDK {version} installed.";
                }
                catch (Exception ex)
                {
                    resultLabel.text = $"Error loading mdk-info.json, exception={ex}.";
                }
            }
        }

        private static VisualElement CreateMdkVerificationStep(ref int stepIndex)
        {
            var foldout = new Foldout
            {
                text = $"{++stepIndex}. Verify MDK installation.",
                value = false
            };
            var container = foldout.contentContainer;
            container.Add(new Label("Verify MDK installation by checking mdk-info.json."));
            var resultLabel = new Label("Test not run yet.");
            var verifier = new MdkVerifier(resultLabel);
            var button = new Button(verifier.Run)
            {
                text = "Run verification"
            };
            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                }
            };
            row.Add(button);
            row.Add(resultLabel);
            container.Add(row);
            return foldout;
        }

        #endregion

        #region Configure Project Settings

        private VisualElement CreateConfigureProjectSettingsStep(ref int stepIndex)
        {
            var foldout = new Foldout
            {
                text = $"{++stepIndex}. Configure Project Settings.",
                value = false
            };
            var container = foldout.contentContainer;
            container.Add(new Label("Configure project settings with recommendations."));
            container.Add(new Label("Please refer to <a href=\"https://developer.nex.inc/docs/playground-adaptation-guide/\">Playground Adaptation Guide</a> for more details."));
            var button = new Button(ConfigureProjectSettings)
            {
                text = "Configure"
            };
            container.Add(button);
            return foldout;
        }

        private static void ConfigureProjectSettings()
        {
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ETC2;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel30;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Android, ApiCompatibilityLevel.NET_Standard);

            PlayerSettings.SetStackTraceLogType(LogType.Error, StackTraceLogType.Full);
            PlayerSettings.SetStackTraceLogType(LogType.Assert, StackTraceLogType.ScriptOnly);
            PlayerSettings.SetStackTraceLogType(LogType.Warning, StackTraceLogType.ScriptOnly);
            PlayerSettings.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            PlayerSettings.SetStackTraceLogType(LogType.Exception, StackTraceLogType.Full);

#if UNITY_6000_0_OR_NEWER
            PlayerSettings.Android.textureCompressionFormats = new[] { TextureCompressionFormat.ETC2 };
            PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;
#endif
        }

        #endregion

        #region Add Hand Pose

        private VisualElement CreateAddHandPoseStep(ref int stepIndex)
        {
            var foldout = new Foldout
            {
                text = $"{++stepIndex}. (Optional) Add/Update Hand Pose Support.",
                value = false
            };
            var container = foldout.contentContainer;
            container.Add(new Label("Add/Update hand pose packages to this project."));
            container.Add(new HelpBox(
                "Enabling hand pose features globally will restrict this game to PlayOS 1.8+ only.\n" +
                "If you prefer, you may install and configure the dependencies manually instead. " +
                "Please refer to the <a href=\"https://developer.nex.inc/docs/tutorials/hand-pose/#installation\">Hand Pose Documentation</a> for more details. " +
                "Otherwise, we recommend installing via the button below.",
                HelpBoxMessageType.Warning));
            var button = new Button(AddHandPose)
            {
                text = "Install hand pose packages"
            };
            container.Add(button);
            return foldout;
        }

        private static void AddHandPose()
        {
            var manager = PackageManager.Instance;
            if (IsMinApiLevelReadyForHandPoseAndFaceLandmark())
            {
                manager.AddPackages("team.nex.mdk.hand");
            }
            else
            {
                manager.AddPackages("team.nex.min-playos-api-level@1.2.1", "team.nex.mdk.hand");
            }
        }

        private static bool IsMinApiLevelReadyForHandPoseAndFaceLandmark()
        {
            PackageInfo[] packages = PackageInfo.GetAllRegisteredPackages();
            var package = packages.FirstOrDefault(p => p.name == "team.nex.min-playos-api-level");

            if (package == null) return false;

            var versionSplit = package.version.Split('.', 3);
            if (versionSplit.Length < 3) return false;

            if (!int.TryParse(versionSplit[0], out var major) || major < 1) return false;
            if (!int.TryParse(versionSplit[1], out var minor) || minor < 2) return false;
            if (!int.TryParse(versionSplit[2], out var patch) || patch < 1) return false;

            return true;
        }

        #endregion

        #region Add Face Landmark

        private VisualElement CreateAddFaceLandmarkStep(ref int stepIndex)
        {
            var foldout = new Foldout
            {
                text = $"{++stepIndex}. (Optional) Add/Update Face Landmark Support.", value = false
            };
            var container = foldout.contentContainer;
            container.Add(new Label("Add/Update face landmark packages to this project."));
            container.Add(new HelpBox(
                "Enabling face landmark features globally will restrict this game to PlayOS 1.8+ only.\n" +
                "If you prefer, you may install and configure the dependencies manually instead. " +
                "Please refer to the <a href=\"https://developer.nex.inc/docs/tutorials/face-landmark/#installation\">Face Landmark Documentation</a> for more details. " +
                "Otherwise, we recommend installing via the button below.", HelpBoxMessageType.Warning));
            var button = new Button(AddFaceLandmark) { text = "Install face landmark packages" };
            container.Add(button);
            return foldout;
        }

        private static void AddFaceLandmark()
        {
            var manager = PackageManager.Instance;
            if (IsMinApiLevelReadyForHandPoseAndFaceLandmark())
            {
                manager.AddPackages("team.nex.mdk.face");
            }
            else
            {
                manager.AddPackages("team.nex.min-playos-api-level@1.2.1", "team.nex.mdk.face");
            }
        }

        #endregion
    }
}
