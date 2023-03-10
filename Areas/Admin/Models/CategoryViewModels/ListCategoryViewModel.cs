using System.Collections.Generic;
using Pika.Domain.Storage.Entity;

namespace PikaCore.Areas.Admin.Models.CategoryViewModels;

public class ListCategoryViewModel
{
    public List<Category>  Categories { get; set; }
}