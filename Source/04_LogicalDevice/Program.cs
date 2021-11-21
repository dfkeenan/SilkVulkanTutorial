using System.Drawing;
using System.Runtime.InteropServices;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Windowing;


var app = new HelloTriangleApplication();
app.Run();

unsafe class HelloTriangleApplication
{
    const int WIDTH = 800;
    const int HEIGHT = 600;

    bool EnableValidationLayers = true;

    private readonly string[] validationLayers = new []
    {
        "VK_LAYER_KHRONOS_validation"
    };

    private IWindow? window;
    private Vk? vk;
    private Instance instance;
    private ExtDebugUtils? debugUtils;
    private DebugUtilsMessengerEXT debugMessenger;
    private PhysicalDevice physicalDevice;
    private Device device;
    private Queue graphicsQueue;

    public void Run()
    {
        InitWindow();
        InitVulkan();
        MainLoop();
        CleanUp();
    }

    private void InitWindow()
    {
        //Create a window.
        var options = WindowOptions.DefaultVulkan with
        {
            Size = new Vector2D<int>(WIDTH, HEIGHT),
            Title = "Vulkan",
        };

        window = Window.Create(options);
        window.Initialize();

        if (window.VkSurface is null)
        {
            throw new Exception("Windowing platform doesn't support Vulkan.");
        }
    }
    private void InitVulkan()
    {
        CreateInstance();
        SetupDebugMessenger();
        PickPhysicalDevice();
        CreateLogicalDevice();
    }

    private void CreateInstance()
    {
        vk = Vk.GetApi();

        if (EnableValidationLayers && !CheckValidationLayerSupport())
        {
            throw new Exception("validation layers requested, but not available!");
        }

        ApplicationInfo appInfo = new()
        {
            SType = StructureType.ApplicationInfo,
            PApplicationName = (byte*)Marshal.StringToHGlobalAnsi("Hello Triangle"),
            ApplicationVersion = new Version32(1, 0, 0),
            PEngineName = (byte*)Marshal.StringToHGlobalAnsi("No Engine"),
            EngineVersion = new Version32(1, 0, 0),
            ApiVersion = Vk.Version11
        };

        InstanceCreateInfo createInfo = new()
        {
            SType = StructureType.InstanceCreateInfo,
            PApplicationInfo = &appInfo
        };

        var extensions = GetRequiredExtensions();
        createInfo.EnabledExtensionCount = (uint)extensions.Length;
        createInfo.PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(extensions); ;
        
        if (EnableValidationLayers)
        {
            createInfo.EnabledLayerCount = (uint)validationLayers.Length;
            createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(validationLayers);

            DebugUtilsMessengerCreateInfoEXT debugCreateInfo = new ();
            PopulateDebugMessengerCreateInfo(ref debugCreateInfo);
            createInfo.PNext = &debugCreateInfo;
        }
        else 
        {
            createInfo.EnabledLayerCount = 0;
            createInfo.PNext = null;
        }

        fixed (Instance* i = &instance)
        {
            if (vk.CreateInstance(&createInfo, null, i) != Result.Success)
            {
                throw new Exception("failed to create instance!");
            }
        }

        Marshal.FreeHGlobal((nint)appInfo.PApplicationName);
        Marshal.FreeHGlobal((nint)appInfo.PEngineName);
        SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);

        if (EnableValidationLayers)
        {
            SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
        }
    }

    private void PopulateDebugMessengerCreateInfo(ref DebugUtilsMessengerCreateInfoEXT createInfo)
    {
        createInfo.SType = StructureType.DebugUtilsMessengerCreateInfoExt;
        createInfo.MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityVerboseBitExt |
                                     DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityWarningBitExt |
                                     DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityErrorBitExt;
        createInfo.MessageType = DebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypeGeneralBitExt |
                                 DebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypePerformanceBitExt |
                                 DebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypeValidationBitExt;
        createInfo.PfnUserCallback = (DebugUtilsMessengerCallbackFunctionEXT)DebugCallback;
    }

    private void SetupDebugMessenger()
    {
        if (!EnableValidationLayers) return;

        //TryGetInstanceExtension equivilant to method CreateDebugUtilsMessengerEXT from original tutorial.
        if (!vk!.TryGetInstanceExtension(instance, out debugUtils)) return;

        DebugUtilsMessengerCreateInfoEXT createInfo = new();
        PopulateDebugMessengerCreateInfo(ref createInfo);

        if (debugUtils!.CreateDebugUtilsMessenger(instance, in createInfo, null, out debugMessenger) != Result.Success)
        {
            throw new Exception("failed to set up debug messenger!");
        }
    }

    private void PickPhysicalDevice()
    {
        uint devicedCount = 0;
        vk!.EnumeratePhysicalDevices(instance, ref devicedCount, null);

        if (devicedCount == 0)
        {
            throw new Exception("failed to find GPUs with Vulkan support!");
        }

        var devices = new PhysicalDevice[devicedCount];
        vk!.EnumeratePhysicalDevices(instance, ref devicedCount, ref devices[0]);

        foreach (var device in devices)
        {
            if (IsDeviceSuitable(device))
            {
                physicalDevice = device;
                break;
            }
        }

        if (physicalDevice.Handle == 0)
        {
            throw new Exception("failed to find a suitable GPU!");
        }
    }

    private void CreateLogicalDevice()
    {
        var indicies = FindQueueFamilies(physicalDevice);

        DeviceQueueCreateInfo queueCreateInfo = new()
        {
            SType = StructureType.DeviceQueueCreateInfo,
            QueueFamilyIndex = indicies.GraphicsFamily!.Value,
            QueueCount = 1
        };

        float queuePriority = 1.0f;
        queueCreateInfo.PQueuePriorities = &queuePriority;

        PhysicalDeviceFeatures deviceFeatures = new();

        DeviceCreateInfo createInfo = new()
        {
            SType = StructureType.DeviceCreateInfo,
            PQueueCreateInfos = &queueCreateInfo,
            QueueCreateInfoCount = 1,

            PEnabledFeatures = &deviceFeatures,

            EnabledExtensionCount = 0
        };

        if (EnableValidationLayers)
        {
            createInfo.EnabledLayerCount = (uint)validationLayers.Length;
            createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(validationLayers);
        }
        else
        {
            createInfo.EnabledLayerCount = 0;
        }

        if (vk!.CreateDevice(physicalDevice, in createInfo, null, out device) != Result.Success)
        {
            throw new Exception("failed to create logical device!");
        }

        vk!.GetDeviceQueue(device, indicies.GraphicsFamily!.Value, 0, out graphicsQueue);
    }

    private bool IsDeviceSuitable(PhysicalDevice device)
    {
        var indicies = FindQueueFamilies(device);

        return indicies.IsComplete();
    }

    private QueueFamilyIndices FindQueueFamilies(PhysicalDevice device)
    {
        var indices = new QueueFamilyIndices();

        uint queueFamilityCount = 0;
        vk!.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilityCount, null);

        var queueFamilies = new QueueFamilyProperties[queueFamilityCount];
        vk!.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilityCount, out queueFamilies[0]);
        
        
        uint i = 0;
        foreach (var queueFamily in queueFamilies)
        {
            if (queueFamily.QueueFlags.HasFlag(QueueFlags.QueueGraphicsBit))
            {
                indices.GraphicsFamily = i;
            }

            if (indices.IsComplete())
            {
                break;
            }

            i++;
        }

        return indices;
    }

    public struct QueueFamilyIndices
    {
        public uint? GraphicsFamily { get; set; }
        public bool IsComplete()
        {
            return GraphicsFamily.HasValue;
        }
    }


    private string[] GetRequiredExtensions()
    {
        var glfwExtensions = window!.VkSurface!.GetRequiredExtensions(out var glfwExtensionCount);
        var extensions = SilkMarshal.PtrToStringArray((nint)glfwExtensions, (int)glfwExtensionCount);

        if (EnableValidationLayers)
        {
            return extensions.Append(ExtDebugUtils.ExtensionName).ToArray();
        }

        return extensions;
    }

    private bool CheckValidationLayerSupport()
    {
        uint layerCount = 0;
        vk!.EnumerateInstanceLayerProperties(ref layerCount, null);
        var availableLayers = new LayerProperties[layerCount];
        vk!.EnumerateInstanceLayerProperties(ref layerCount, ref availableLayers[0]);

        var availableLayerNames = availableLayers.Select(layer => Marshal.PtrToStringAnsi((IntPtr)layer.LayerName)).ToHashSet();

        return validationLayers.All(availableLayerNames.Contains);
    }

    private uint DebugCallback(DebugUtilsMessageSeverityFlagsEXT messageSeverity, DebugUtilsMessageTypeFlagsEXT messageTypes, DebugUtilsMessengerCallbackDataEXT* pCallbackData, void* pUserData)
    {
        Console.WriteLine($"validation layer:" + Marshal.PtrToStringAnsi((nint)pCallbackData->PMessage));

        return Vk.False;
    }

    private void MainLoop()
    {
        window!.Render += DrawFrame;
        window!.Run();
    }

    private void DrawFrame(double obj)
    {

    }

    private void CleanUp()
    {
        vk!.DestroyDevice(device, null);

        if (EnableValidationLayers)
        {
            //DestroyDebugUtilsMessenger equivilant to method DestroyDebugUtilsMessengerEXT from original tutorial.
            debugUtils!.DestroyDebugUtilsMessenger(instance, debugMessenger, null);
        }

        vk?.DestroyInstance(instance, null);
        vk?.Dispose();
        window?.Dispose();
    }
}