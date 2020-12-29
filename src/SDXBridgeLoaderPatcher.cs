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

    public static void Patch(AssemblyDefinition assembly)
    {
        Logger.LogInfo("Initializing SDX Bridge Patcher");
        HookSDX(assembly);
    }

    private static void HookSDX(AssemblyDefinition gameassembly)
    {
        var modPath = BepInEx.Paths.GameRootPath + @"\Mods";

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
                            var modassembly = AssemblyDefinition.ReadAssembly(file);
                            var instances = from t in AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(file)).GetTypes()
                                            where t.GetInterfaces().Contains(typeof(IPatcherMod))
                                                     && t.GetConstructor(Type.EmptyTypes) != null
                                            select Activator.CreateInstance(t) as IPatcherMod;

                            foreach (var instance in instances)
                            {
                                instance.Patch(gameassembly.MainModule);
                            }
                            //Pulled from DMT, but causes BepInex to not log anything, not sure it's even applying. Not sure the intended purpose of this.
                            //foreach (var instance in instances)
                            //{
                            //    instance.Link(gameassembly.MainModule, modassembly.MainModule);
                            //}
                            //Pulled from DMT LinkedAssemblyTask, but not sure what they do or how to implement in this context yet.
                            //gameassembly.MainModule.Write(data.LinkedDllLocation);
                            //modassembly.MainModule.Write(data.ModDllLocation);
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