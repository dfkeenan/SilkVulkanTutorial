using System.Drawing;
using System.Runtime.InteropServices;
using Silk.NET.Core;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;


var app = new HelloTriangleApplication();
app.Run();

unsafe class HelloTriangleApplication
{
    const int WIDTH = 800;
    const int HEIGHT = 600;

    private IWindow? window;
    private Vk? vk;
    private Instance instance;

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
    }

    private void CreateInstance()
    {
        vk = Vk.GetApi();

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

        var glfwExtensions = window!.VkSurface!.GetRequiredExtensions(out var glfwExtensionCount);

        createInfo.EnabledExtensionCount = glfwExtensionCount;
        createInfo.PpEnabledExtensionNames = glfwExtensions;
        createInfo.EnabledLayerCount = 0;

        fixed (Instance* i = &instance)
        {
            if (vk.CreateInstance(&createInfo, null, i) != Result.Success)
            {
                throw new Exception("failed to create instance!");
            }
        }

        Marshal.FreeHGlobal((nint)appInfo.PApplicationName);
        Marshal.FreeHGlobal((nint)appInfo.PEngineName);
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
        vk?.DestroyInstance(instance, null);
        vk?.Dispose();
        window?.Dispose();
    }
}