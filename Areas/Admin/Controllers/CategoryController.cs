using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Hangfire;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Localization;
using Pika.Domain.Storage.Callables.ValueTypes;
using PikaCore.Areas.Admin.Models.CategoryViewModels;
using PikaCore.Areas.Core.Callables;
using PikaCore.Areas.Core.Commands;
using PikaCore.Areas.Core.Queries;

namespace PikaCore.Areas.Admin.Controllers;

[Area("Admin")]
[Route("[area]/[controller]/[action]")]
public class CategoryController : Controller
{
    private readonly IStringLocalizer<CategoryController> _localizer;
    private readonly IMediator _mediator;
    private readonly IDistributedCache _distributedCache;
    private readonly IMapper _mapper;
    
    public CategoryController(IStringLocalizer<CategoryController> localizer,
        IMediator mediator,
        IDistributedCache cache,
        IMapper mapper)
    {
        this._localizer = localizer;
        this._mediator = mediator;
        this._distributedCache = cache;
        this._mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var listModel = new ListCategoryViewModel();
        var categories = await _mediator.Send(new GetAllCategoriesQuery());
        listModel.Categories = categories;
        return View(listModel);
    }
    
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }
    
    [HttpPost]
    [AutoValidateAntiforgeryToken]
    public IActionResult Create([FromForm] CreateCategoryViewModel createCategoryViewModel)
    {
        if (!ModelState.IsValid)
        {
            ViewData["ReturnMessage"] = _localizer.GetString("Niepoprawne dane dla kategorii").Value;
            return View();
        }
        var callable = new CreateCategoryCallable(_mediator, _distributedCache);
        var parameters = new Dictionary<string, ParameterValueType>()
        {
            ["Name"] = new(createCategoryViewModel.Name),
            ["Description"] = new(createCategoryViewModel.Description),
            ["Mimes"] = new(createCategoryViewModel.GetMimes())
        };

        BackgroundJob.Schedule(() => callable.Execute(parameters), TimeSpan.Zero);
        ViewData["Success"] = true;
        ViewData["ReturnMessage"] = this._localizer.GetString("Kategoria przekazana do utworzenia").Value;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var category = await _mediator.Send(new GetCategoryByIdQuery(id));
        var editViewModel = _mapper.Map<EditCategoryViewModel>(category); 
        return View(editViewModel);
    }
    
    [HttpPost]
    [AutoValidateAntiforgeryToken]
    public async Task<IActionResult> Edit(EditCategoryViewModel editCategoryViewModel)
    {
        await _mediator.Send(_mapper.Map<UpdateCategoryCommand>(editCategoryViewModel));
        ViewData["Success"] = true;
        ViewData["ReturnMessage"] = this._localizer.GetString("Kategoria przekazana do aktualizacji").Value;
        return RedirectPermanent($"/Admin/Category/Edit?id={editCategoryViewModel.Id}");
    } 
}