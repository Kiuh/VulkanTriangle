using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace VulkanTriangle;

internal static unsafe class VulkanCommandTools
{
    public static CommandPool CreateCommandPool(
        Vk vk,
        Device device,
        PhysicalDevice physicalDevice,
        KhrSurface khrSurface,
        SurfaceKHR surface
    )
    {
        QueueFamilyIndices queueFamilyIndices = VulkanTools.FindQueueFamilies(
            vk,
            physicalDevice,
            khrSurface,
            surface
        );

        CommandPoolCreateInfo poolInfo =
            new()
            {
                SType = StructureType.CommandPoolCreateInfo,
                QueueFamilyIndex = queueFamilyIndices.GraphicsFamily!.Value,
            };

        return
            vk!.CreateCommandPool(device, poolInfo, null, out CommandPool commandPool)
            != Result.Success
            ? throw new Exception("Failed to create command pool!")
            : commandPool;
    }

    public static CommandBuffer[] CreateCommandBuffers(
        Vk vk,
        Framebuffer[] swapChainFramebuffers,
        CommandPool commandPool,
        Device device,
        RenderPass renderPass,
        Extent2D swapChainExtent,
        Pipeline graphicsPipeline,
        Buffer vertexBuffer,
        Vertex[] vertices,
        Buffer indexBuffer,
        ushort[] indices
    )
    {
        CommandBuffer[] commandBuffers = new CommandBuffer[swapChainFramebuffers.Length];

        CommandBufferAllocateInfo allocInfo =
            new()
            {
                SType = StructureType.CommandBufferAllocateInfo,
                CommandPool = commandPool,
                Level = CommandBufferLevel.Primary,
                CommandBufferCount = (uint)commandBuffers.Length,
            };

        fixed (CommandBuffer* commandBuffersPtr = commandBuffers)
        {
            if (vk!.AllocateCommandBuffers(device, allocInfo, commandBuffersPtr) != Result.Success)
            {
                throw new Exception("Failed to allocate command buffers!");
            }
        }

        for (int i = 0; i < commandBuffers.Length; i++)
        {
            CommandBufferBeginInfo beginInfo =
                new() { SType = StructureType.CommandBufferBeginInfo, };

            if (vk!.BeginCommandBuffer(commandBuffers[i], beginInfo) != Result.Success)
            {
                throw new Exception("Failed to begin recording command buffer!");
            }

            ClearValue clearColor =
                new()
                {
                    Color = new()
                    {
                        Float32_0 = 0,
                        Float32_1 = 0,
                        Float32_2 = 0,
                        Float32_3 = 1
                    },
                };

            RenderPassBeginInfo renderPassInfo =
                new()
                {
                    SType = StructureType.RenderPassBeginInfo,
                    RenderPass = renderPass,
                    Framebuffer = swapChainFramebuffers[i],
                    RenderArea = { Offset = { X = 0, Y = 0 }, Extent = swapChainExtent, },
                    ClearValueCount = 1,
                    PClearValues = &clearColor
                };

            vk!.CmdBeginRenderPass(commandBuffers[i], &renderPassInfo, SubpassContents.Inline);

            vk!.CmdBindPipeline(commandBuffers[i], PipelineBindPoint.Graphics, graphicsPipeline);

            Buffer[] vertexBuffers = [vertexBuffer];
            ulong[] offsets = [0];

            vk!.CmdBindVertexBuffers(commandBuffers[i], 0, 1, vertexBuffers.AsSpan(), offsets.AsSpan());

            //vk!.CmdDraw(commandBuffers[i], (uint)vertices.Length, 1, 0, 0);
            vk!.CmdBindIndexBuffer(commandBuffers[i], indexBuffer, 0, IndexType.Uint16);

            vk!.CmdDrawIndexed(commandBuffers[i], (uint)indices.Length, 1, 0, 0, 0);

            vk!.CmdEndRenderPass(commandBuffers[i]);

            if (vk!.EndCommandBuffer(commandBuffers[i]) != Result.Success)
            {
                throw new Exception("Failed to record command buffer!");
            }
        }

        return commandBuffers;
    }
}
