using Silk.NET.Core;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace VulkanTriangle;

internal static unsafe class VulkanTools
{
    public static QueueFamilyIndices FindQueueFamilies(
        Vk vk,
        PhysicalDevice device,
        KhrSurface khrSurface,
        SurfaceKHR surface
    )
    {
        QueueFamilyIndices indices = new();

        uint queueFamilyCount = 0;
        vk!.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilyCount, null);

        QueueFamilyProperties[] queueFamilies = new QueueFamilyProperties[queueFamilyCount];
        fixed (QueueFamilyProperties* queueFamiliesPtr = queueFamilies)
        {
            vk!.GetPhysicalDeviceQueueFamilyProperties(
                device,
                ref queueFamilyCount,
                queueFamiliesPtr
            );
        }

        uint i = 0;
        foreach (QueueFamilyProperties queueFamily in queueFamilies)
        {
            if (queueFamily.QueueFlags.HasFlag(QueueFlags.GraphicsBit))
            {
                indices.GraphicsFamily = i;
            }

            _ = khrSurface!.GetPhysicalDeviceSurfaceSupport(
                device,
                i,
                surface,
                out Bool32 presentSupport
            );

            if (presentSupport)
            {
                indices.PresentFamily = i;
            }

            if (indices.IsComplete())
            {
                break;
            }

            i++;
        }

        return indices;
    }

    public static SwapChainSupportDetails QuerySwapChainSupport(
        PhysicalDevice physicalDevice,
        KhrSurface khrSurface,
        SurfaceKHR surface
    )
    {
        SwapChainSupportDetails details = new();

        _ = khrSurface!.GetPhysicalDeviceSurfaceCapabilities(
            physicalDevice,
            surface,
            out details.Capabilities
        );

        uint formatCount = 0;
        _ = khrSurface.GetPhysicalDeviceSurfaceFormats(
            physicalDevice,
            surface,
            ref formatCount,
            null
        );

        if (formatCount != 0)
        {
            details.Formats = new SurfaceFormatKHR[formatCount];
            fixed (SurfaceFormatKHR* formatsPtr = details.Formats)
            {
                _ = khrSurface.GetPhysicalDeviceSurfaceFormats(
                    physicalDevice,
                    surface,
                    ref formatCount,
                    formatsPtr
                );
            }
        }
        else
        {
            details.Formats = [];
        }

        uint presentModeCount = 0;
        _ = khrSurface.GetPhysicalDeviceSurfacePresentModes(
            physicalDevice,
            surface,
            ref presentModeCount,
            null
        );

        if (presentModeCount != 0)
        {
            details.PresentModes = new PresentModeKHR[presentModeCount];
            fixed (PresentModeKHR* formatsPtr = details.PresentModes)
            {
                _ = khrSurface.GetPhysicalDeviceSurfacePresentModes(
                    physicalDevice,
                    surface,
                    ref presentModeCount,
                    formatsPtr
                );
            }
        }
        else
        {
            details.PresentModes = [];
        }

        return details;
    }
}
