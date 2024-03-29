﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Hangfire;
using MediatR;
using Microsoft.AspNetCore.Authorization;
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
[Authorize(Roles = "Administrator")]
[Route("[area]/[controller]/[action]")]
public class CategoryController : Controller
{
    private readonly IDistributedCache _distributedCache;
    private readonly IStringLocalizer<CategoryController> _localizer;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;

    public CategoryController(IStringLocalizer<CategoryController> localizer,
        IMediator mediator,
        IDistributedCache cache,
        IMapper mapper)
    {
        _localizer = localizer;
        _mediator = mediator;
        _distributedCache = cache;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var listModel = new ListCategoryViewModel();
        var categories = await _mediator.Send(new GetAllCategoriesQuery());
        listModel.Categories = categories.ToList();
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

        var callable = new CreateCategoryCallable(_mediator);
        var parameters = new Dictionary<string, ParameterValueType>
        {
            ["Name"] = new(createCategoryViewModel.Name),
            ["Description"] = new(createCategoryViewModel.Description),
            ["Mimes"] = new(createCategoryViewModel.GetMimes())
        };

        BackgroundJob.Schedule(() => callable.Execute(parameters), TimeSpan.Zero);
        ViewData["Success"] = true;
        ViewData["ReturnMessage"] = _localizer.GetString("Kategoria przekazana do utworzenia").Value;
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
        TempData["Success"] = true;
        TempData["ReturnMessage"] = _localizer.GetString("Kategoria przekazana do aktualizacji").Value;
        return RedirectPermanent($"/Admin/Category/Edit?id={editCategoryViewModel.Id}");
    }

    [HttpGet]
    public async Task<IActionResult> Delete(Guid id)
    {
        return View(id);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    public async Task<IActionResult> DeleteSubmit(Guid id)
    {
        var rid = await _mediator.Send(new ArchiveCategoryCommand(id));
        TempData["Success"] = true;
        TempData["ReturnMessage"] = _localizer.GetString($"Kategoria {rid} przekazana do usunięcia").Value;
        return Redirect("/Admin/Category/List");
    }
}