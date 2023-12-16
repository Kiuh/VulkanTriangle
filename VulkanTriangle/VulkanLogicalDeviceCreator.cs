using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using System.Runtime.CompilerServices;

namespace VulkanTriangle;

internal static unsafe class VulkanLogicalDeviceCreator
{
    public static (Device, Queue, Queue) CreateLogicalDevice(
        Vk vkInstance,
        PhysicalDevice physicalDevice,
        bool enableValidationLayers,
        string[] validationLayers,
        string[] deviceExtensions,
        KhrSurface khrSurface,
        SurfaceKHR surface
    )
    {
        QueueFamilyIndices indices = VulkanTools.FindQueueFamilies(
            vkInstance,
            physicalDevice,
            khrSurface,
            surface
        );

        uint[] uniqueQueueFamilies =
        [
            indices.GraphicsFamily!.Value,
            indices.PresentFamily!.Value
        ];
        uniqueQueueFamilies = uniqueQueueFamilies.Distinct().ToArray();

        using GlobalMemory mem = GlobalMemory.Allocate(
            uniqueQueueFamilies.Length * sizeof(DeviceQueueCreateInfo)
        );
        DeviceQueueCreateInfo* queueCreateInfos = (DeviceQueueCreateInfo*)
            Unsafe.AsPointer(ref mem.GetPinnableReference());

        float queuePriority = 1.0f;
        for (int i = 0; i < uniqueQueueFamilies.Length; i++)
        {
            queueCreateInfos[i] = new()
            {
                SType = StructureType.DeviceQueueCreateInfo,
                QueueFamilyIndex = uniqueQueueFamilies[i],
                QueueCount = 1,
                PQueuePriorities = &queuePriority
            };
        }

        PhysicalDeviceFeatures deviceFeatures = new();

        DeviceCreateInfo createInfo =
            new()
            {
                SType = StructureType.DeviceCreateInfo,
                QueueCreateInfoCount = (uint)uniqueQueueFamilies.Length,
                PQueueCreateInfos = queueCreateInfos,
                PEnabledFeatures = &deviceFeatures,
                EnabledExtensionCount = (uint)deviceExtensions.Length,
                PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(deviceExtensions)
            };

        if (enableValidationLayers)
        {
            createInfo.EnabledLayerCount = (uint)validationLayers.Length;
            createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(validationLayers);
        }
        else
        {
            createInfo.EnabledLayerCount = 0;
        }

        if (
            vkInstance!.CreateDevice(physicalDevice, in createInfo, null, out Device device)
            != Result.Success
        )
        {
            throw new Exception("failed to create logical device!");
        }

        vkInstance!.GetDeviceQueue(device, indices.GraphicsFamily!.Value, 0, out Queue graphicsQueue);
        vkInstance!.GetDeviceQueue(device, indices.PresentFamily!.Value, 0, out Queue presentQueue);

        if (enableValidationLayers)
        {
            _ = SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
        }

        return (device, graphicsQueue, presentQueue);
    }
}
