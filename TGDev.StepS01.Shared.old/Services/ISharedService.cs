using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGDev.StepS01.Shared.Models;

namespace TGDev.StepS01.Shared.Services;

public interface ISharedService
{
    string FeedFetcherJsonResult { get; set; }
    List<NewsItemModel> DatabaseStorageResult { get; set; }
}
