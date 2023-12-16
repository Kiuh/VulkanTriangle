using Silk.NET.Vulkan;

namespace VulkanTriangle
{
    internal static unsafe class VulkanFrameBuffersCreator
    {
        public static Framebuffer[] CreateFramebuffers(
            Vk vk,
            ImageView[] swapChainImageViews,
            RenderPass renderPass,
            Extent2D swapChainExtent,
            Device device
        )
        {
            Framebuffer[] swapChainFramebuffers = new Framebuffer[swapChainImageViews!.Length];

            for (int i = 0; i < swapChainImageViews.Length; i++)
            {
                ImageView attachment = swapChainImageViews[i];

                FramebufferCreateInfo framebufferInfo =
                    new()
                    {
                        SType = StructureType.FramebufferCreateInfo,
                        RenderPass = renderPass,
                        AttachmentCount = 1,
                        PAttachments = &attachment,
                        Width = swapChainExtent.Width,
                        Height = swapChainExtent.Height,
                        Layers = 1,
                    };

                if (
                    vk!.CreateFramebuffer(
                        device,
                        framebufferInfo,
                        null,
                        out swapChainFramebuffers[i]
                    ) != Result.Success
                )
                {
                    throw new Exception("failed to create framebuffer!");
                }
            }

            return swapChainFramebuffers;
        }
    }
}
