using Silk.NET.Vulkan;

namespace VulkanTriangle;

internal static unsafe class VulkanImageViewsCreator
{
    public static ImageView[] CreateImageViews(
        Vk vk,
        Image[] swapChainImages,
        Format swapChainImageFormat,
        Device device
    )
    {
        ImageView[] swapChainImageViews = new ImageView[swapChainImages!.Length];

        for (int i = 0; i < swapChainImages.Length; i++)
        {
            ImageViewCreateInfo createInfo =
                new()
                {
                    SType = StructureType.ImageViewCreateInfo,
                    Image = swapChainImages[i],
                    ViewType = ImageViewType.Type2D,
                    Format = swapChainImageFormat,
                    Components =
                    {
                        R = ComponentSwizzle.Identity,
                        G = ComponentSwizzle.Identity,
                        B = ComponentSwizzle.Identity,
                        A = ComponentSwizzle.Identity,
                    },
                    SubresourceRange =
                    {
                        AspectMask = ImageAspectFlags.ColorBit,
                        BaseMipLevel = 0,
                        LevelCount = 1,
                        BaseArrayLayer = 0,
                        LayerCount = 1,
                    }
                };

            if (
                vk!.CreateImageView(device, createInfo, null, out swapChainImageViews[i])
                != Result.Success
            )
            {
                throw new Exception("Failed to create image views!");
            }
        }

        return swapChainImageViews;
    }
}
