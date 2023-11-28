using System.Collections.Generic;
using Pika.Domain.Storage.Entity.View;

namespace PikaCore.Areas.Admin.Models.CategoryViewModels;

public class ListCategoryViewModel
{
    public List<CategoriesView> Categories { get; set; }
}