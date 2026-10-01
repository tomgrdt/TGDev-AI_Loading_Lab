using Microsoft.SemanticKernel;
using System.ComponentModel;
using TGDev.StepS01.Shared.Services;

namespace TGDev.StepS01.Shared.Plugins;

public class NewsItemPlugin
{
    private readonly NewsItemService _newsItemService;

    public NewsItemPlugin(NewsItemService newsItemService)
    {
        _newsItemService = newsItemService;
    }

    [KernelFunction, Description("Give the current time")]
    public string GetCurrentTime() => DateTime.Now.ToString("F");
}
