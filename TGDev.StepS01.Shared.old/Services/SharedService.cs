using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGDev.StepS01.Shared.Models;

namespace TGDev.StepS01.Shared.Services;

public class SharedService : ISharedService
{
    public string FeedFetcherJsonResult { get; set; } = string.Empty;
    public List<NewsItemModel> DatabaseStorageResult { get; set; } = new List<NewsItemModel>();

    public SharedService(){}
}
