using Silk.NET.Core;
using Silk.NET.Core.Contexts;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Spectre.Console;
using System.Runtime.InteropServices;

namespace VulkanTriangle;

internal static unsafe class VulkanInstanceCreator
{
    public static Instance CreateInstance(
        Vk vk,
        IVkSurface vkSurface,
        bool enableValidationLayers,
        string[] validationLayers
    )
    {
        if (enableValidationLayers && !CheckValidationLayerSupport(vk, validationLayers))
        {
            throw new Exception("Validation layers requested, but not available!");
        }

        ApplicationInfo appInfo =
            new()
            {
                SType = StructureType.ApplicationInfo,
                PApplicationName = (byte*)Marshal.StringToHGlobalAnsi("Hello Triangle"),
                ApplicationVersion = new Version32(1, 0, 0),
                PEngineName = (byte*)Marshal.StringToHGlobalAnsi("No Engine"),
                EngineVersion = new Version32(1, 0, 0),
                ApiVersion = Vk.Version12
            };

        InstanceCreateInfo createInfo =
            new() { SType = StructureType.InstanceCreateInfo, PApplicationInfo = &appInfo };

        string[] extensions = GetRequiredExtensions(vkSurface, enableValidationLayers);
        createInfo.EnabledExtensionCount = (uint)extensions.Length;
        createInfo.PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(extensions);
        ;

        if (enableValidationLayers)
        {
            createInfo.EnabledLayerCount = (uint)validationLayers.Length;
            createInfo.PpEnabledLayerNames = (byte**)
                SilkMarshal.StringArrayToPtr(validationLayers);

            DebugUtilsMessengerCreateInfoEXT debugCreateInfo = new();
            PopulateDebugMessengerCreateInfo(ref debugCreateInfo);
            createInfo.PNext = &debugCreateInfo;
        }
        else
        {
            createInfo.EnabledLayerCount = 0;
            createInfo.PNext = null;
        }

        if (vk.CreateInstance(createInfo, null, out Instance instance) != Result.Success)
        {
            throw new Exception("failed to create instance!");
        }

        Marshal.FreeHGlobal((IntPtr)appInfo.PApplicationName);
        Marshal.FreeHGlobal((IntPtr)appInfo.PEngineName);
        _ = SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);

        if (enableValidationLayers)
        {
            _ = SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
        }

        return instance;
    }

    private static string[] GetRequiredExtensions(
        IVkSurface vkSurface,
        bool enableValidationLayers
    )
    {
        byte** glfwExtensions = vkSurface.GetRequiredExtensions(out uint glfwExtensionCount);
        string[] extensions = SilkMarshal.PtrToStringArray(
            (nint)glfwExtensions,
            (int)glfwExtensionCount
        );

        return enableValidationLayers
            ? [.. extensions, ExtDebugUtils.ExtensionName]
            : extensions;
    }

    private static bool CheckValidationLayerSupport(Vk vk, string[] validationLayers)
    {
        uint layerCount = 0;
        _ = vk!.EnumerateInstanceLayerProperties(ref layerCount, null);
        LayerProperties[] availableLayers = new LayerProperties[layerCount];
        fixed (LayerProperties* availableLayersPtr = availableLayers)
        {
            _ = vk!.EnumerateInstanceLayerProperties(ref layerCount, availableLayersPtr);
        }

        HashSet<string?> availableLayerNames = availableLayers
            .Select(layer => Marshal.PtrToStringAnsi((IntPtr)layer.LayerName))
            .ToHashSet();

        return validationLayers.All(availableLayerNames.Contains);
    }

    private static void PopulateDebugMessengerCreateInfo(
        ref DebugUtilsMessengerCreateInfoEXT createInfo
    )
    {
        createInfo.SType = StructureType.DebugUtilsMessengerCreateInfoExt;
        createInfo.MessageSeverity =
            DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt
            | DebugUtilsMessageSeverityFlagsEXT.WarningBitExt
            | DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt;
        createInfo.MessageType =
            DebugUtilsMessageTypeFlagsEXT.GeneralBitExt
            | DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt
            | DebugUtilsMessageTypeFlagsEXT.ValidationBitExt;
        createInfo.PfnUserCallback = new PfnDebugUtilsMessengerCallbackEXT(DebugCallback);
    }

    private static uint DebugCallback(
        DebugUtilsMessageSeverityFlagsEXT messageSeverity,
        DebugUtilsMessageTypeFlagsEXT messageTypes,
        DebugUtilsMessengerCallbackDataEXT* pCallbackData,
        void* pUserData
    )
    {
        string level = messageSeverity switch
        {
            DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt => "[blue]Diagnostic[/]",
            DebugUtilsMessageSeverityFlagsEXT.WarningBitExt => "[yellow]Warning[/]",
            DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt => "[red]Error[/]",
            _ => "[red]Unknown level[/]"
        };

        string type = messageTypes switch
        {
            DebugUtilsMessageTypeFlagsEXT.ValidationBitExt
                => "[darkmagenta]Possible error or specification mistake[/]",
            DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt
                => "[darkmagenta]Not optimal Vulkan using[/]",
            DebugUtilsMessageTypeFlagsEXT.GeneralBitExt => "[darkmagenta]General info[/]",
            _ => "[darkmagenta]Unknown type[/]"
        };

        string? message = Marshal.PtrToStringAnsi((nint)pCallbackData->PMessage);

        AnsiConsole.Markup(
            $"Validation layer {level} message - {type}:"
        );
        Console.Write(message + "\n");

        return Vk.False;
    }

    public static (ExtDebugUtils?, DebugUtilsMessengerEXT) SetupDebugMessenger(
        bool enableValidationLayers,
        Vk vk,
        Instance instance
    )
    {
        if (!enableValidationLayers)
        {
            return (null, default);
        }

        if (!vk!.TryGetInstanceExtension(instance, out ExtDebugUtils debugUtils))
        {
            return (debugUtils, default);
        }

        DebugUtilsMessengerCreateInfoEXT createInfo = new();
        PopulateDebugMessengerCreateInfo(ref createInfo);

        return
            debugUtils!.CreateDebugUtilsMessenger(
                instance,
                in createInfo,
                null,
                out DebugUtilsMessengerEXT debugMessenger
            ) != Result.Success
            ? throw new Exception("failed to set up debug messenger!")
            : ((ExtDebugUtils?, DebugUtilsMessengerEXT))(debugUtils, debugMessenger);
    }
}
