using Silk.NET.Vulkan;

namespace VulkanTriangle;

internal static unsafe class VulkanRenderPassCreator
{
    public static RenderPass CreateRenderPass(Vk vk, Format swapChainImageFormat, Device device)
    {
        AttachmentDescription colorAttachment =
            new()
            {
                Format = swapChainImageFormat,
                Samples = SampleCountFlags.Count1Bit,
                LoadOp = AttachmentLoadOp.Clear,
                StoreOp = AttachmentStoreOp.Store,
                StencilLoadOp = AttachmentLoadOp.DontCare,
                InitialLayout = ImageLayout.Undefined,
                FinalLayout = ImageLayout.PresentSrcKhr,
            };

        AttachmentReference colorAttachmentRef =
            new() { Attachment = 0, Layout = ImageLayout.ColorAttachmentOptimal, };

        SubpassDescription subPass =
            new()
            {
                PipelineBindPoint = PipelineBindPoint.Graphics,
                ColorAttachmentCount = 1,
                PColorAttachments = &colorAttachmentRef,
            };

        RenderPassCreateInfo renderPassInfo =
            new()
            {
                SType = StructureType.RenderPassCreateInfo,
                AttachmentCount = 1,
                PAttachments = &colorAttachment,
                SubpassCount = 1,
                PSubpasses = &subPass,
            };

        return
            vk!.CreateRenderPass(device, renderPassInfo, null, out RenderPass renderPass)
            != Result.Success
            ? throw new Exception("Failed to create render pass!")
            : renderPass;
    }
}
