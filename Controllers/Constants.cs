namespace FMS2.Controllers{
    public sealed class Constants{
        public static string RootPath {get; set;}
        public static string Tmp {get;} = "/srv/fms/";
        public static bool IsDevelopment  {get; set;} = false;
    }
}