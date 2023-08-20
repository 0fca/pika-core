using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Pika.Domain.Security;
using Pika.Domain.Storage.Data;
using PikaCore.Areas.Core.Data;
using PikaCore.Areas.Core.Models;
using PikaCore.Areas.Core.Models.DTO;
using PikaCore.Areas.Core.Models.File;
using PikaCore.Areas.Core.Queries;
using PikaCore.Areas.Core.Services;
using PikaCore.Areas.Identity.Attributes;
using PikaCore.Infrastructure.Adapters;
using PikaCore.Infrastructure.Adapters.Filesystem.Commands;
using PikaCore.Infrastructure.Security;
using PikaCore.Infrastructure.Services.Helpers;
using Serilog;

namespace PikaCore.Areas.Core.Controllers.App
{
    [Area("Core")]
    [ResponseCache(CacheProfileName = "Default")]
    public class StorageController : Controller
    {
        private readonly IUrlGenerator _urlGeneratorService;
        private readonly IMapper _mapper;
        private readonly IdDataProtection _idDataProtection;
        private readonly IStringLocalizer<StorageController> _stringLocalizer;
        private readonly IMediator _mediator;
        private readonly StorageIndexContext _storageIndexContext;
        private readonly IStorage _storage;
        private readonly IDistributedCache _cache;
        private readonly IConfiguration _configuration;

        #region TempDataMessages

        [TempData(Key = "showGenerateUrlPartial")]
        public bool ShowGenerateUrlPartial { get; set; }

        [TempData(Key = "returnMessage")] public string ReturnMessage { get; set; } = "";

        #endregion

        public StorageController(IUrlGenerator iUrlGenerator,
            StorageIndexContext storageIndexContext,
            IdDataProtection idDataProtection,
            IStringLocalizer<StorageController> stringLocalizer,
            IMediator mediator,
            IMapper mapper,
            IStorage service,
            IDistributedCache cache,
            IConfiguration configuration)
        {
            _urlGeneratorService = iUrlGenerator;
            _storageIndexContext = storageIndexContext;
            _idDataProtection = idDataProtection;
            _stringLocalizer = stringLocalizer;
            _mediator = mediator;
            _mapper = mapper;
            _cache = cache;
            _storage = service;
            _configuration = configuration;
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("[area]/[controller]/")]
        public async Task<IActionResult> Index(
            [FromQuery(Name = "CurrentBucketName")] string currentBucketName = "storage")
        {
            var role = HttpContext.User.Claims.FirstOrDefault(c => c.Type.Equals(ClaimTypes.Role));
            var buckets = await _storage.GetBucketsForRole(role?.Value ?? RoleString.User);
            if (buckets.Count == 0)
            {
                ViewData["ReturnMessage"] = _stringLocalizer.GetString("Wystąpił problem z ładowaniem bucketów").Value;
                return View();
            }

            var currentBucket = buckets.FirstOrDefault(b => b.Name.Equals(currentBucketName));
            if (currentBucket == null)
            {
                ViewData["ReturnMessage"] = _stringLocalizer.GetString("Wystąpił problem z ładowaniem bucketów").Value;
                return View();
            }

            var categoriesViews = await _storage.GetCategoriesForBucket(currentBucket.Id);
            var model = new IndexViewModel
            {
                CurrentBucketId = currentBucket.Id,
                CurrentBucketName = currentBucketName,
                Categories = categoriesViews.ConvertAll(c => _mapper.Map<CategoryDTO>(c)),
                Buckets = buckets.ToList().ConvertAll(b => _mapper.Map<BucketDTO>(b))
            };
            return View(model);
        }

        [HttpGet]
        [Route("[area]/[controller]/[action]")]
        [AuthorizeUserBucketAccess]
        [AllowAnonymous]
        public async Task<IActionResult> Browse([FromQuery] string categoryId,
            [FromQuery] string bucketId,
            [FromQuery] int offset,
            [FromQuery] int count = 10,
            [FromQuery] string? tag = null)
        {
            var objects = JsonSerializer
                .Deserialize<List<ObjectInfo>>(
                    await _cache.GetStringAsync($"{bucketId}.category.contents.{categoryId}") ?? "[]"
                );
            var total = objects!.Count;
            if (objects!.Count > int.Parse(_configuration.GetSection("Storage")["OnePageMaxTotal"] ?? "2000"))
            {
                objects = Paginator<ObjectInfo>.Paginate(objects, offset, count);
            }

            var tags = (await _mediator.Send(new GetCategoryByIdQuery(Guid.Parse(categoryId))))
                .Tags;
            var lrmv = new FileResultViewModel
            {
                Objects = objects,
                SelectedTag = tag,
                SelectedPage = (offset + count) / count,
                PerPage = count,
                Total = total,
                OnePageMaxTotal = int.Parse(_configuration.GetSection("Storage")["OnePageMaxTotal"] ?? "2000"),
                Tags = tags!.ContainsKey(bucketId) ? tags[bucketId] : new List<string>(),
                CategoryId = categoryId,
                BucketId = bucketId
            };
            return View(lrmv);
        }

        [HttpGet]
        //[Route("/[controller]/[action]/{name?}")]
        public async Task<IActionResult> GenerateUrl(string name, string returnUrl)
        {
            var entryName = _idDataProtection.Decode(name);
            var offset = int.Parse(Get("Offset"));
            var count = int.Parse(Get("Count"));

            if (!string.IsNullOrEmpty(entryName))
            {
                ShowGenerateUrlPartial = false;
                try
                {
                    var s = _storageIndexContext.IndexStorage.ToList()
                        .Find(record => record.AbsolutePath.Equals(entryName));

                    if (s == null)
                    {
                        s = new StorageIndexRecord
                        {
                            AbsolutePath = entryName,
                            Urlhash = _urlGeneratorService.GenerateId(entryName),
                            UserId = IdentifyUser(),
                            Expires = true
                        };
                    }
                    else if (s.ExpireDate.Date <= DateTime.Now.Date)
                    {
                        s.ExpireDate = StorageIndexRecord.ComputeDateTime();
                    }

                    _storageIndexContext.Update(s);
                    await _storageIndexContext.SaveChangesAsync();

                    TempData["urlhash"] = s.Urlhash;
                    ShowGenerateUrlPartial = true;
                    var port = HttpContext.Request.Host.Port;
                    TempData["host"] = HttpContext.Request.Host.Host +
                                       (port != null ? ":" + HttpContext.Request.Host.Port : "");
                    TempData["protocol"] = "https";
                    TempData["returnUrl"] = returnUrl;
                    return RedirectToAction(nameof(Browse), new { @path = returnUrl, offset, count });
                }
                catch (InvalidOperationException ex)
                {
                    Log.Error(ex, "StorageController#GenerateUrl");
                    ReturnMessage = ex.Message;
                    return RedirectToAction(nameof(Browse), new { @path = returnUrl, offset, count });
                }
            }

            ReturnMessage = _stringLocalizer.GetString("Couldn't generate token for that resource").Value;
            return RedirectToAction(nameof(Browse), new { @path = returnUrl });
        }

        [HttpGet]
        [AllowAnonymous]
        [AuthorizeUserBucketAccess]
        public async Task<ActionResult> Download([FromQuery] string bucketId,
            [FromQuery] string categoryId,
            [FromQuery] string objectName)
        {
            var bucket = await _mediator.Send(new GetBucketByIdQuery(Guid.Parse(bucketId)));
            if (!await _storage.StatObject(bucket.Name, objectName))
            {
                ReturnMessage = _stringLocalizer.GetString("Zasób nie istnieje").Value;
                return RedirectToAction(nameof(Browse), new { categoryId, bucketId });
            }

            var (memoryStream, returnName, mime) = await _storage.GetObjectAsStream(bucket.Name, objectName);
            return File(memoryStream, mime, returnName);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        [Authorize(Roles = "Moderator, Administrator")]
        [AuthorizeUserBucketAccess]
        [Route("[area]/[controller]/[action]")]
        public async Task<IActionResult> Upload([FromForm] List<IFormFile> files,
            [FromQuery] string bucketId,
            [FromQuery] string categoryId
        )
        {
            var size = GetFilesSummarySize(files);
            if (size >= long.Parse(this._configuration.GetSection("Storage")["MaxUploadSize"] ?? "0"))
            {
                return BadRequest();
            }
            
            var response = await _mediator.Send(new SanitizeTemporaryFileCommand(files, bucketId));
            
            var message = _stringLocalizer.GetString( "Pomyślnie dodano pliki do kolejki.").Value;
            // TODO: Retrieve data from cache
            var downloadUrl = $"{response}";
            return Accepted(new
            {
                message,
                downloadUrl
            });
        }

        [HttpGet]
        [AllowAnonymous]
        [AuthorizeUserBucketAccess]
        public async Task<IActionResult> Information([FromQuery] string bucketId,
            [FromQuery] string categoryId,
            [FromQuery] string objectName)
        {
            var bucket = await _mediator.Send(new GetBucketByIdQuery(Guid.Parse(bucketId)));
            if (!await _storage.StatObject(bucket.Name, objectName))
            {
                ReturnMessage = _stringLocalizer.GetString("Zasób nie istnieje").Value;
                return View();
            }

            var objectInfo = await _storage.ObjectInformation(bucket.Name, objectName);
            var viewModel = _mapper.Map<ResourceInformationViewModel>(objectInfo);
            viewModel.CategoryId = categoryId;
            viewModel.BucketId = bucketId;
            return View(viewModel);
        }

        #region HelperMethods

        private static string IdentifyUser()
        {
            return "User";
        }

        private static long GetFilesSummarySize(IEnumerable<IFormFile> files)
        {
            return files.Aggregate(0L, (i, file) => i + file.Length);
        }

        #endregion

        #region CookieHelperMethods

        private string Get(string key)
        {
            return HttpContext.Request.Cookies[key] ?? "";
        }

        #endregion
    }
}