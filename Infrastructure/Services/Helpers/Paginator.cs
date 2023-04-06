using System.Collections.Generic;

namespace PikaCore.Infrastructure.Services.Helpers;

public static class Paginator<T>
{
   public static List<T> Paginate(List<T> collection, int offset, int count = 50)
   {
      var total = collection.Count;
      return total > offset + count 
         ? collection.GetRange(offset, count) 
         : collection.GetRange(offset, total - offset);
   }
}