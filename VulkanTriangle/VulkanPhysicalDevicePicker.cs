using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using System.Runtime.InteropServices;

namespace VulkanTriangle;

internal struct QueueFamilyIndices
{
    public uint? GraphicsFamily { get; set; }
    public uint? PresentFamily { get; set; }

    public readonly bool IsComplete()
    {
        return GraphicsFamily.HasValue && PresentFamily.HasValue;
    }
}

internal struct SwapChainSupportDetails
{
    public SurfaceCapabilitiesKHR Capabilities;
    public SurfaceFormatKHR[] Formats;
    public PresentModeKHR[] PresentModes;
}

internal static unsafe class VulkanPhysicalDevicePicker
{
    public static PhysicalDevice PickPhysicalDevice(
        Vk vk,
        Instance instance,
        KhrSurface khrSurface,
        SurfaceKHR surface,
        string[] deviceExtensions
    )
    {
        uint devicesCount = 0;
        _ = vk!.EnumeratePhysicalDevices(instance, ref devicesCount, null);

        if (devicesCount == 0)
        {
            throw new Exception("Failed to find GPUs with Vulkan support!");
        }

        PhysicalDevice[] devices = new PhysicalDevice[devicesCount];
        fixed (PhysicalDevice* devicesPtr = devices)
        {
            _ = vk!.EnumeratePhysicalDevices(instance, ref devicesCount, devicesPtr);
        }

        PhysicalDevice physicalDevice = default;
        foreach (PhysicalDevice device in devices)
        {
            if (IsDeviceSuitable(vk, device, khrSurface, surface, deviceExtensions))
            {
                physicalDevice = device;
                break;
            }
        }

        return physicalDevice.Handle == 0
            ? throw new Exception("Failed to find a suitable GPU!")
            : physicalDevice;
    }

    private static bool IsDeviceSuitable(
        Vk vkInstance,
        PhysicalDevice device,
        KhrSurface khrSurface,
        SurfaceKHR surface,
        string[] deviceExtensions
    )
    {
        QueueFamilyIndices indices = VulkanTools.FindQueueFamilies(
            vkInstance,
            device,
            khrSurface,
            surface
        );

        bool extensionsSupported = CheckDeviceExtensionsSupport(
            vkInstance,
            device,
            deviceExtensions
        );

        bool swapChainAdequate = false;
        if (extensionsSupported)
        {
            SwapChainSupportDetails swapChainSupport = VulkanTools.QuerySwapChainSupport(
                device,
                khrSurface,
                surface
            );
            swapChainAdequate =
                swapChainSupport.Formats.Length != 0 && swapChainSupport.PresentModes.Length != 0;
        }

        return indices.IsComplete() && extensionsSupported && swapChainAdequate;
    }

    private static bool CheckDeviceExtensionsSupport(
        Vk vk,
        PhysicalDevice device,
        string[] deviceExtensions
    )
    {
        uint extensionsCount = 0;
        _ = vk!.EnumerateDeviceExtensionProperties(device, (byte*)null, ref extensionsCount, null);

        ExtensionProperties[] availableExtensions = new ExtensionProperties[extensionsCount];
        fixed (ExtensionProperties* availableExtensionsPtr = availableExtensions)
        {
            _ = vk!.EnumerateDeviceExtensionProperties(
                device,
                (byte*)null,
                ref extensionsCount,
                availableExtensionsPtr
            );
        }

        HashSet<string?> availableExtensionNames = availableExtensions
            .Select(extension => Marshal.PtrToStringAnsi((IntPtr)extension.ExtensionName))
            .ToHashSet();

        return deviceExtensions.All(availableExtensionNames.Contains);
    }
}
