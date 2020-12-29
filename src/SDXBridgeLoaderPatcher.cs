using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using SDX.Compiler;
using DMT;

public static class SDXBridgeLoaderPatcher
{
    public static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(SDXBridgeLoaderPatcher));


    // List of assemblies to patch
    public static IEnumerable<string> TargetDLLs { get; } = new[] { "Assembly-CSharp.dll" };

    public static void Initialize()
    {
    }


    public static void Patch(AssemblyDefinition assembly)
    {
        Logger.LogInfo("Initializing SDX Bridge Patcher");
        HookSDX(assembly);
    }

    public static void Finish()
    {
    }


    private static void HookSDX(AssemblyDefinition gameassembly)
    {
        var assemblies = new List<Assembly>();
        var modPath = @"C:\Program Files (x86)\Steam\steamapps\common\7 Days To Die\Mods";
            //Application.platform != RuntimePlatform.OSXPlayer ? (Application.dataPath + "/../Mods") : (Application.dataPath + "/../../Mods");

        if (Directory.Exists(modPath))
        {
            Logger.LogInfo("Start SDX loading: " + modPath);
            string[] directories = Directory.GetDirectories(modPath);

            foreach (string path in directories)
            {
                try
                {
                    var modinfoPath = path + "/ModInfo.xml";
                    var patchscripts = path + "/PatchScripts";

                    if (File.Exists(modinfoPath) && Directory.Exists(patchscripts))
                    {
                        foreach (var file in Directory.GetFiles(patchscripts, "*.dll"))
                        {
                            Logger.LogInfo("DLL found: " + file);
                            var assembly = AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(file));
                            var instances = from t in assembly.GetTypes()
                                            where t.GetInterfaces().Contains(typeof(IPatcherMod))
                                                     && t.GetConstructor(Type.EmptyTypes) != null
                                            select Activator.CreateInstance(t) as IPatcherMod;

                            foreach (var instance in instances)
                            {
                                instance.Patch(gameassembly.MainModule);
                            }
                            //foreach (var instance in instances)
                            //{
                            //    instance.Link(gameassembly.MainModule, patchassembly.MainModule);
                            //}
                            //gameassembly.MainModule.Write(data.LinkedDllLocation);
                            //patchassembly.MainModule.Write(data.ModDllLocation);
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.LogInfo("Failed loading modded DLL from " + Path.GetFileName(path));
                    Logger.LogInfo("\t" + e.ToString());
                }
            }
        }
    }
}