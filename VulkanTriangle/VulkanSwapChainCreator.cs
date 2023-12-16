using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;

namespace VulkanTriangle;

internal static unsafe class VulkanSwapChainCreator
{
    public static (KhrSwapchain, SwapchainKHR, Image[], Format, Extent2D) CreateSwapChain(
        Vk vk,
        PhysicalDevice physicalDevice,
        KhrSurface khrSurface,
        SurfaceKHR surface,
        Instance instance,
        Device device,
        IWindow window
    )
    {
        SwapChainSupportDetails swapChainSupport = VulkanTools.QuerySwapChainSupport(
            physicalDevice,
            khrSurface,
            surface
        );

        SurfaceFormatKHR surfaceFormat = ChooseSwapSurfaceFormat(swapChainSupport.Formats);
        PresentModeKHR presentMode = ChoosePresentMode(swapChainSupport.PresentModes);
        Extent2D extent = ChooseSwapExtent(swapChainSupport.Capabilities, window);

        uint imageCount = swapChainSupport.Capabilities.MinImageCount + 1;
        if (
            swapChainSupport.Capabilities.MaxImageCount > 0
            && imageCount > swapChainSupport.Capabilities.MaxImageCount
        )
        {
            imageCount = swapChainSupport.Capabilities.MaxImageCount;
        }

        SwapchainCreateInfoKHR creationInfo =
            new()
            {
                SType = StructureType.SwapchainCreateInfoKhr,
                Surface = surface,
                MinImageCount = imageCount,
                ImageFormat = surfaceFormat.Format,
                ImageColorSpace = surfaceFormat.ColorSpace,
                ImageExtent = extent,
                ImageArrayLayers = 1,
                ImageUsage = ImageUsageFlags.ColorAttachmentBit,
            };

        QueueFamilyIndices indices = VulkanTools.FindQueueFamilies(
            vk,
            physicalDevice,
            khrSurface,
            surface
        );
        uint* queueFamilyIndices = stackalloc[] {
            indices.GraphicsFamily!.Value,
            indices.PresentFamily!.Value
        };

        if (indices.GraphicsFamily != indices.PresentFamily)
        {
            creationInfo = creationInfo with
            {
                ImageSharingMode = SharingMode.Concurrent,
                QueueFamilyIndexCount = 2,
                PQueueFamilyIndices = queueFamilyIndices,
            };
        }
        else
        {
            creationInfo.ImageSharingMode = SharingMode.Exclusive;
        }

        creationInfo = creationInfo with
        {
            PreTransform = swapChainSupport.Capabilities.CurrentTransform,
            CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,
            PresentMode = presentMode,
            Clipped = true,
            OldSwapchain = default
        };

        if (!vk!.TryGetDeviceExtension(instance, device, out KhrSwapchain khrSwapChain))
        {
            throw new NotSupportedException("VK_KHR_swapchain extension not found.");
        }

        Result result = khrSwapChain!.CreateSwapchain(
            device,
            creationInfo,
            null,
            out SwapchainKHR swapChain
        );

        if (result != Result.Success)
        {
            throw new Exception("failed to create swap chain!");
        }

        _ = khrSwapChain.GetSwapchainImages(device, swapChain, ref imageCount, null);
        Image[] swapChainImages = new Image[imageCount];
        fixed (Image* swapChainImagesPtr = swapChainImages)
        {
            _ = khrSwapChain.GetSwapchainImages(
                device,
                swapChain,
                ref imageCount,
                swapChainImagesPtr
            );
        }
        return (khrSwapChain, swapChain, swapChainImages, surfaceFormat.Format, extent);
    }

    private static SurfaceFormatKHR ChooseSwapSurfaceFormat(
        IReadOnlyList<SurfaceFormatKHR> availableFormats
    )
    {
        foreach (SurfaceFormatKHR availableFormat in availableFormats)
        {
            //Console.WriteLine(availableFormat.Format);
            if (
                availableFormat.Format == Format.B8G8R8A8Srgb
                && availableFormat.ColorSpace == ColorSpaceKHR.SpaceSrgbNonlinearKhr
            )
            {
                //Console.WriteLine("Found needed SurfaceFormatKHR");
                return availableFormat;
            }
        }

        return availableFormats[0];
    }

    private static PresentModeKHR ChoosePresentMode(
        IReadOnlyList<PresentModeKHR> availablePresentModes
    )
    {
        foreach (PresentModeKHR availablePresentMode in availablePresentModes)
        {
            if (availablePresentMode == PresentModeKHR.MailboxKhr)
            {
                return availablePresentMode;
            }
        }

        return PresentModeKHR.FifoKhr;
    }

    private static Extent2D ChooseSwapExtent(SurfaceCapabilitiesKHR capabilities, IWindow window)
    {
        if (capabilities.CurrentExtent.Width != uint.MaxValue)
        {
            return capabilities.CurrentExtent;
        }
        else
        {
            Vector2D<int> framebufferSize = window!.FramebufferSize;

            Extent2D actualExtent =
                new() { Width = (uint)framebufferSize.X, Height = (uint)framebufferSize.Y };

            actualExtent.Width = Math.Clamp(
                actualExtent.Width,
                capabilities.MinImageExtent.Width,
                capabilities.MaxImageExtent.Width
            );
            actualExtent.Height = Math.Clamp(
                actualExtent.Height,
                capabilities.MinImageExtent.Height,
                capabilities.MaxImageExtent.Height
            );

            return actualExtent;
        }
    }
}
