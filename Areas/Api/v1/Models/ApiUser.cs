using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace PikaCore.Areas.Api.v1.Models
{
    public class ApiUser
    {
        [Required]
        [NotNull]
        [JsonPropertyName("username")]
        [DataType(DataType.Text)]
        public string Username { get; set; } = "";

        [Required]
        [NotNull]
        [JsonPropertyName("password")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";
    }
}