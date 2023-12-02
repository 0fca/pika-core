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
using PikaCore.Areas.Core.Commands;
using PikaCore.Areas.Core.Models;
using PikaCore.Areas.Core.Models.DTO;
using PikaCore.Areas.Core.Models.File;
using PikaCore.Areas.Core.Queries;
using PikaCore.Areas.Identity.Attributes;
using PikaCore.Infrastructure.Adapters;
using PikaCore.Infrastructure.Adapters.Filesystem.Commands;
using PikaCore.Infrastructure.Security;
using Serilog;

namespace PikaCore.Areas.Core.Controllers.App
{
    [Area("Core")]
    public class StorageController : Controller
    {
        private readonly IMapper _mapper;
        private readonly IStringLocalizer<StorageController> _stringLocalizer;
        private readonly IMediator _mediator;
        private readonly IStorage _storage;
        private readonly IDistributedCache _cache;
        private readonly IConfiguration _configuration;
        private readonly IdDataProtection _idDataProtection;
        
        #region TempDataMessages


        [TempData(Key = "returnMessage")] private string ReturnMessage { get; set; } = "";

        #endregion

        public StorageController(
            IStringLocalizer<StorageController> stringLocalizer,
            IMediator mediator,
            IMapper mapper,
            IStorage service,
            IDistributedCache cache,
            IConfiguration configuration,
            IdDataProtection protection)
        {
            _stringLocalizer = stringLocalizer;
            _mediator = mediator;
            _mapper = mapper;
            _cache = cache;
            _storage = service;
            _configuration = configuration;
            _idDataProtection = protection;
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("[area]/[controller]/")]
        public async Task<IActionResult> Index(
            [FromQuery(Name = "CurrentBucketName")]
            string currentBucketName = "storage")
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
            var tags = (await _mediator.Send(new GetCategoryByIdQuery(Guid.Parse(categoryId))))
                .Tags;
            return View(new FileResultViewModel
            {
                SelectedTag = tag,
                Tags = tags!.ContainsKey(bucketId) ? tags[bucketId] : new List<string>(),
                CategoryId = categoryId,
                BucketId = bucketId
            });
        }

        [HttpGet]
        [Route("/[area]/[controller]/[action]/{bucketId}/{categoryId}")]
        [ActionName("ShortLink")]
        public async Task<IActionResult> GenerateUrl(
            [FromQuery] string name,
            [FromRoute] string bucketId,
            [FromRoute] string categoryId
        )
        {
            try
            {
                name = _idDataProtection.Decode(name);
                var rId = await _mediator.Send(new GenerateShortLinkCommand(name, bucketId));
                var hash = JsonSerializer.Deserialize<string>(_cache.Get(rId.ToString()));
                await _cache.RemoveAsync(rId.ToString());
                TempData["ShortLink"] = hash;
                TempData["CategoryId"] = categoryId;
                return RedirectToAction(nameof(Browse), new { categoryId, bucketId });
            }
            catch (InvalidOperationException ex)
            {
                Log.Error(ex, "StorageController#GenerateUrl");
                ReturnMessage = ex.Message;
                return BadRequest();
            }
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("[area]/[controller]/[action]")]
        public async Task<IActionResult> ShortLinkDownload(
            [FromQuery] string hash,
            [FromQuery] string categoryId
        )
        {
            var s = await _mediator.Send(new FindShortLinkByHashQuery(hash));
            var encodedObjectName = _idDataProtection.Encode(s.ObjectName);
            return Redirect(
                $"/Core/Storage/Download?bucketId={s.BucketId}&categoryId={categoryId}&objectName={encodedObjectName}"
            );
        }

        [HttpGet]
        [AllowAnonymous]
        [AuthorizeUserBucketAccess]
        public async Task<ActionResult> Download([FromQuery] string bucketId,
            [FromQuery] string categoryId,
            [FromQuery] string objectName)
        {
            var unprotectedObjectName = _idDataProtection.Decode(objectName);
            var bucket = await _mediator.Send(new GetBucketByIdQuery(Guid.Parse(bucketId)));
            if (!await _storage.StatObject(bucket.Name, unprotectedObjectName))
            {
                ReturnMessage = _stringLocalizer.GetString("Zasób nie istnieje").Value;
                return RedirectToAction(nameof(Browse), new { categoryId, bucketId });
            }

            var (memoryStream, returnName, mime) = await _storage.GetObjectAsStream(bucket.Name, unprotectedObjectName);
            return File(memoryStream, "application/octet-stream", returnName);
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

            await _mediator.Send(new SanitizeTemporaryFileCommand(files, bucketId));
            const string actionPath = nameof(Browse);
            var downloadUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/Core/Storage/{actionPath}" +
                              $"?categoryId={categoryId}&bucketId={bucketId}";
            return Accepted(new
            {
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
            var decodedObjectName = _idDataProtection.Decode(objectName);
            if (!await _storage.StatObject(bucket.Name, decodedObjectName))
            {
                ReturnMessage = _stringLocalizer.GetString("Zasób nie istnieje").Value;
                return View(null);
            }

            var objectInfo = await _storage.ObjectInformation(bucket.Name, decodedObjectName);
            var viewModel = _mapper.Map<ResourceInformationViewModel>(objectInfo);
            viewModel.FullName = objectName; 
            viewModel.CategoryId = categoryId;
            viewModel.BucketId = bucketId;
            return View(viewModel);
        }

        #region HelperMethods

        private static long GetFilesSummarySize(IEnumerable<IFormFile> files)
        {
            return files.Aggregate(0L, (i, file) => i + file.Length);
        }

        #endregion
    }
}