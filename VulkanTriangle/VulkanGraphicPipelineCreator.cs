using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace VulkanTriangle;

internal static unsafe class VulkanGraphicPipelineCreator
{
    public static (Pipeline, PipelineLayout) CreateGraphicsPipeline(
        Vk vk,
        RenderPass renderPass,
        Device device,
        Extent2D swapChainExtent
    )
    {
        byte[] vertShaderCode = File.ReadAllBytes(
            "D:\\GitHub\\VulkanTriangle\\VulkanTriangle\\Shaders\\vert.spv"
        );
        byte[] fragShaderCode = File.ReadAllBytes(
            "D:\\GitHub\\VulkanTriangle\\VulkanTriangle\\Shaders\\frag.spv"
        );

        ShaderModule vertShaderModule = CreateShaderModule(vk, device, vertShaderCode);
        ShaderModule fragShaderModule = CreateShaderModule(vk, device, fragShaderCode);

        PipelineShaderStageCreateInfo vertShaderStageInfo =
            new()
            {
                SType = StructureType.PipelineShaderStageCreateInfo,
                Stage = ShaderStageFlags.VertexBit,
                Module = vertShaderModule,
                PName = (byte*)SilkMarshal.StringToPtr("main")
            };

        PipelineShaderStageCreateInfo fragShaderStageInfo =
            new()
            {
                SType = StructureType.PipelineShaderStageCreateInfo,
                Stage = ShaderStageFlags.FragmentBit,
                Module = fragShaderModule,
                PName = (byte*)SilkMarshal.StringToPtr("main")
            };

        PipelineShaderStageCreateInfo* shaderStages = stackalloc[] {
            vertShaderStageInfo,
            fragShaderStageInfo
        };

        VertexInputBindingDescription bindingDescription = Vertex.GetBindingDescription();
        VertexInputAttributeDescription[] attributeDescriptions = Vertex.GetAttributeDescriptions();

        Pipeline graphicsPipeline;
        PipelineLayout pipelineLayout;

        fixed (VertexInputAttributeDescription* attributeDescriptionsPtr = attributeDescriptions)
        {
            PipelineVertexInputStateCreateInfo vertexInputInfo =
                new()
                {
                    SType = StructureType.PipelineVertexInputStateCreateInfo,
                    VertexBindingDescriptionCount = 1,
                    VertexAttributeDescriptionCount = (uint)attributeDescriptions.Length,
                    PVertexBindingDescriptions = &bindingDescription,
                    PVertexAttributeDescriptions = attributeDescriptionsPtr,
                };

            PipelineInputAssemblyStateCreateInfo inputAssembly =
                new()
                {
                    SType = StructureType.PipelineInputAssemblyStateCreateInfo,
                    Topology = PrimitiveTopology.TriangleList,
                    PrimitiveRestartEnable = false,
                };

            Viewport viewport =
                new()
                {
                    X = 0,
                    Y = 0,
                    Width = swapChainExtent.Width,
                    Height = swapChainExtent.Height,
                    MinDepth = 0,
                    MaxDepth = 1,
                };

            Rect2D scissor = new() { Offset = { X = 0, Y = 0 }, Extent = swapChainExtent, };

            PipelineViewportStateCreateInfo viewportState =
                new()
                {
                    SType = StructureType.PipelineViewportStateCreateInfo,
                    ViewportCount = 1,
                    PViewports = &viewport,
                    ScissorCount = 1,
                    PScissors = &scissor,
                };

            PipelineRasterizationStateCreateInfo rasterizer =
                new()
                {
                    SType = StructureType.PipelineRasterizationStateCreateInfo,
                    DepthClampEnable = false,
                    RasterizerDiscardEnable = false,
                    PolygonMode = PolygonMode.Fill,
                    LineWidth = 1,
                    CullMode = CullModeFlags.BackBit,
                    FrontFace = FrontFace.Clockwise,
                    DepthBiasEnable = false,
                };

            PipelineMultisampleStateCreateInfo multisampling =
                new()
                {
                    SType = StructureType.PipelineMultisampleStateCreateInfo,
                    SampleShadingEnable = false,
                    RasterizationSamples = SampleCountFlags.Count1Bit,
                };

            PipelineColorBlendAttachmentState colorBlendAttachment =
                new()
                {
                    ColorWriteMask =
                        ColorComponentFlags.RBit
                        | ColorComponentFlags.GBit
                        | ColorComponentFlags.BBit
                        | ColorComponentFlags.ABit,
                    BlendEnable = false,
                };

            PipelineColorBlendStateCreateInfo colorBlending =
                new()
                {
                    SType = StructureType.PipelineColorBlendStateCreateInfo,
                    LogicOpEnable = false,
                    LogicOp = LogicOp.Copy,
                    AttachmentCount = 1,
                    PAttachments = &colorBlendAttachment,
                };

            colorBlending.BlendConstants[0] = 0;
            colorBlending.BlendConstants[1] = 0;
            colorBlending.BlendConstants[2] = 0;
            colorBlending.BlendConstants[3] = 0;

            PipelineLayoutCreateInfo pipelineLayoutInfo =
                new()
                {
                    SType = StructureType.PipelineLayoutCreateInfo,
                    SetLayoutCount = 0,
                    PushConstantRangeCount = 0,
                };

            if (
                vk!.CreatePipelineLayout(device, pipelineLayoutInfo, null, out pipelineLayout)
                != Result.Success
            )
            {
                throw new Exception("failed to create pipeline layout!");
            }

            GraphicsPipelineCreateInfo pipelineInfo =
                new()
                {
                    SType = StructureType.GraphicsPipelineCreateInfo,
                    StageCount = 2,
                    PStages = shaderStages,
                    PVertexInputState = &vertexInputInfo,
                    PInputAssemblyState = &inputAssembly,
                    PViewportState = &viewportState,
                    PRasterizationState = &rasterizer,
                    PMultisampleState = &multisampling,
                    PColorBlendState = &colorBlending,
                    Layout = pipelineLayout,
                    RenderPass = renderPass,
                    Subpass = 0,
                    BasePipelineHandle = default
                };

            if (
                vk!.CreateGraphicsPipelines(
                    device,
                    default,
                    1,
                    pipelineInfo,
                    null,
                    out graphicsPipeline
                ) != Result.Success
            )
            {
                throw new Exception("failed to create graphics pipeline!");
            }
        }

        vk!.DestroyShaderModule(device, fragShaderModule, null);
        vk!.DestroyShaderModule(device, vertShaderModule, null);

        _ = SilkMarshal.Free((nint)vertShaderStageInfo.PName);
        _ = SilkMarshal.Free((nint)fragShaderStageInfo.PName);

        return (graphicsPipeline, pipelineLayout);
    }

    private static ShaderModule CreateShaderModule(Vk vk, Device device, byte[] code)
    {
        ShaderModuleCreateInfo createInfo =
            new() { SType = StructureType.ShaderModuleCreateInfo, CodeSize = (nuint)code.Length, };

        ShaderModule shaderModule;

        fixed (byte* codePtr = code)
        {
            createInfo.PCode = (uint*)codePtr;

            if (
                vk!.CreateShaderModule(device, createInfo, null, out shaderModule) != Result.Success
            )
            {
                throw new Exception();
            }
        }

        return shaderModule;
    }
}
