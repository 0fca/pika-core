using System;
using System.Collections.Generic;
using System.Linq;

namespace PikaCore.Areas.Admin.Models.CategoryViewModels;

public class EditCategoryViewModel
{
   public Guid Id 
   { 
      get;
      set; 
   }
   public string Name
   {
      get;
      set;
   }

   public string Mimes
   {
      get; 
      set;
   }

   public string Description
   {
      get;
      set;
   }

   public List<string> GetMimes()
   {
      return this.Mimes.Split(";").ToList();
   }
}